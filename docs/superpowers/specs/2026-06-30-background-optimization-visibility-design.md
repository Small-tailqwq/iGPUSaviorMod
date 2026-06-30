# 后台省电模式：基于窗口可见性的触发改造

## 背景与目标

iGPU Savior 当前的后台省电模式仅依赖 Unity 的 `OnApplicationFocus` 回调：窗口失焦即把渲染切到 `Background` 模式（限帧 10fps + renderScale 0.1 + 关闭阴影），获焦即恢复。这在多显示器场景下问题明显——游戏窗口在 B 屏完全可见、用户只是在 A 屏操作，也会被降级成马赛克画质。

**本次目标**：把触发条件从"窗口失焦"改为"游戏窗口在屏幕上不可见"，并正确处理多显示器场景，避免焦点与可见性解耦时的误判。

## 成功标准

1. 游戏窗口在任一显示器上肉眼可见时，即使焦点在其他窗口，也保持 `Normal` 画质。
2. 以下任一情况触发 `Background` 降级：
   - 游戏窗口最小化；
   - 游戏窗口被虚拟桌面隐藏（Win+Tab 切走）；
   - 游戏窗口完全拖出所有显示器的可视范围；
   - 游戏窗口所在显示器上存在其他程序的全屏窗口（粗粒度遮挡代理，不做精细遮挡计算）。
3. 多显示器下，游戏在 B 屏窗口化、A 屏有全屏视频时，**不**触发降级。
4. 用户恢复游戏窗口可见后，画质自动回到 `Normal`。
5. 现有 `Potato`（F2 手动土豆模式）和 `PiP`（F3 小窗）的优先级豁免逻辑保持不变。
6. 新代码通过 `dotnet test` 与 Release 构建验证。

## 现状回顾

- `PotatoController.OnApplicationFocus` (`Core/PotatoController.cs:54`) 直接调用 `_renderManager.OnFocusChanged(hasFocus)`。
- `RenderQualityManager.OnFocusChanged` (`Features/RenderQualityManager.cs:106`)：失焦且开关开启 → `SetMode(Background)`；获焦 → `SetMode(Normal)`。`Potato` 模式优先级最高，不受焦点影响。
- `RenderQualityManager.Update` (`Features/RenderQualityManager.cs:128`) 仅做周期性 `ApplyCurrentMode` 刷新，不重新决策模式。
- 配置项 `CfgEnableBackgroundOptimization` (`Configuration/ConfigurationManager.cs:71`)，描述为"窗口失焦时自动省电"。
- `Utilities/WindowManager.cs` 已封装多个 Win32 窗口 API（`FindWindow`、`GetWindowRect`、`GetCursorPos`、`SetWindowPos` 等），是本次扩展的天然落点。

## 架构设计

### 方案选型

采用**事件信号 + 轮询兜底**：

- `OnApplicationFocus` 保留但弱化：**获焦 → 立即恢复 `Normal`**（获焦必然可见，事件驱动零延迟）；**失焦 → 不直接降级**，仅触发一次可见性检查并标记"需要轮询"。
- `RenderQualityManager.Update` 内新增可见性轮询（节流 ~0.5s），调用 `WindowVisibilityChecker.IsGameWindowVisible()` 决定 `Normal`/`Background`。

理由：纯轮询会让获焦恢复有最长 0.5s 延迟；纯事件无法覆盖最小化/拖出/全屏等无 Unity 事件的状态变化。事件 + 轮询兼顾响应速度与场景覆盖。

### 新增组件：`WindowVisibilityChecker`

位置：`iGPU Savior/Utilities/WindowVisibilityChecker.cs`，命名空间 `PotatoOptimization.Utilities`。

职责：封装"游戏窗口是否对用户可见"的 Win32 判定，返回单一布尔结果。所有 P/Invoke 集中于此。

```csharp
namespace PotatoOptimization.Utilities
{
    /// <summary>
    /// 判定游戏窗口是否在屏幕上对用户可见。
    /// 不可见的判定（任一命中即不可见）：
    ///   1. 窗口最小化（IsIconic）
    ///   2. 虚拟桌面隐藏（DWMWA_CLOAKED）
    ///   3. 窗口矩形与虚拟屏幕无交集（完全拖出所有显示器）
    ///   4. 游戏所在显示器上存在其他程序的全屏窗口
    /// </summary>
    public sealed class WindowVisibilityChecker
    {
        public bool IsGameWindowVisible(IntPtr gameHwnd);
    }
}
```

#### 判定细节

**判定 1 — 最小化**：`IsIconic(gameHwnd)` 返回 true → 不可见。

**判定 2 — 虚拟桌面隐藏**：`DwmGetWindowAttribute(gameHwnd, DWMWA_CLOAKED=14, out int cloaked, sizeof(int))`，`cloaked != 0` → 不可见。覆盖 Win+Tab 切换虚拟桌面、任务栏预览等cloak 场景。

**判定 3 — 拖出屏幕**：`GetWindowRect(gameHwnd, out RECT r)` 取窗口矩形；`GetSystemMetrics(SM_XVIRTUALSCREEN)` / `SM_YVIRTUALSCREEN` / `SM_CXVIRTUALSCREEN` / `SM_CYVIRTUALSCREEN` 取虚拟屏幕范围（覆盖所有显示器）。矩形无交集（`r.Right <= vScreen.Left || r.Left >= vScreen.Right || r.Bottom <= vScreen.Top || r.Top >= vScreen.Bottom`）→ 不可见。

**判定 4 — 同显示器全屏遮挡**：前 3 项都判定可见时才执行（短路）。
1. `MonitorFromWindow(gameHwnd, MONITOR_DEFAULTTONEAREST)` 取游戏所在显示器句柄，`GetMonitorInfo` 取其 `rcMonitor` 矩形。
2. `EnumWindows` 枚举所有顶层窗口，对每个窗口 `h`：
   - `h == gameHwnd` → 跳过自身；
   - `IsWindowVisible(h)` 为 false → 跳过；
   - `GetWindowRect(h, out RECT hr)`，若 `hr` 覆盖游戏显示器 `rcMonitor`（`hr.Left <= mon.Left && hr.Right >= mon.Right && hr.Top <= mon.Top && hr.Bottom >= mon.Bottom`）→ 判为全屏窗口；
   - 用 `MonitorFromWindow(h, MONITOR_DEFAULTTONEAREST)` 确认该全屏窗口与游戏在同一显示器；
   - 命中任一满足条件的 `h` → 不可见。
3. 枚举完毕无命中 → 可见。

> 说明：不区分"真全屏独占"与"无边框窗口全屏"，二者均按全屏处理。这符合"游戏存在的桌面有其他程序全屏了就激活"的需求口径，且避免引入 D3D/DXGI 全屏检测的复杂度。

### 可测试性设计

为使纯逻辑可单测，抽出接口采集原始信号，判定逻辑独立为 `VisibilityDecider`：

```csharp
public struct VisibilitySignals
{
    public bool IsIconic;
    public bool IsCloaked;
    public RECT WindowRect;
    public RECT VirtualScreen;
    public MonitorRect? GameMonitor;          // null 表示无法获取
    public IReadOnlyList<FullscreenWindowInfo> FullscreenWindowsOnGameMonitor;
}

public static class VisibilityDecider
{
    // 纯函数，无 P/Invoke，可单测
    public static bool IsVisible(VisibilitySignals signals);
}
```

`WindowVisibilityChecker.IsGameWindowVisible` 内部：采集 `VisibilitySignals` → 调 `VisibilityDecider.IsVisible`。P/Invoke 采集部分不单测，`VisibilityDecider` 单测覆盖 4 项判定的各分支组合。

### 改造 `RenderQualityManager`

1. 构造函数注入 `WindowVisibilityChecker`（由 `PotatoController.InitializeManagers` 创建，复用 `WindowManager.GetCurrentWindowHandle()`）。
2. 新增字段：
   - `private float _lastVisibilityCheckTime;`
   - `private bool _pendingVisibilityCheck;`（失焦事件置位，驱动首次轮询立即执行）
4. 改造 `OnFocusChanged(bool focused)`（保留方法名与签名，由 `PotatoController.OnApplicationFocus` 继续调用）：
   - `focused == true` → `SetMode(Normal)`（获焦必然可见，事件驱动零延迟恢复；受 Potato/PiP 优先级约束）；
   - `focused == false` → 不切模式，置 `_pendingVisibilityCheck = true`，记录 `_lastVisibilityCheckTime = 0`（迫使下次 Update 立即执行可见性检查）。
5. 改造 `Update()`：在现有"防止游戏覆盖设置"的刷新逻辑之前，插入可见性轮询块——
   - 节流条件：`_pendingVisibilityCheck` 为真，或 `Time.realtimeSinceStartup - _lastVisibilityCheckTime > 0.5f`；
   - 不满足节流条件则跳过整个轮询块（不复位 `_pendingVisibilityCheck`）；
   - 满足条件后执行：`_pendingVisibilityCheck = false`（先复位，避免本帧后续异常导致永久挂起），`_lastVisibilityCheckTime = Time.realtimeSinceStartup`；
   - `Potato` 模式 / `PiP` 活跃时：跳过可见性判定（保持现有豁免），但 `_pendingVisibilityCheck` 与时间戳仍按上述复位；
   - 开关 `CfgEnableBackgroundOptimization` 关闭时：跳过降级判定，但若当前为 `Background` 则恢复 `Normal`（保持现有逻辑），复位照常；
   - 开启且非豁免时：调 `_visibilityChecker.IsGameWindowVisible(hwnd)`，不可见且当前 `Normal` → `SetMode(Background)`，可见且当前 `Background` → `SetMode(Normal)`。
6. `SetPiPModeActive` 中对 `Application.isFocused` 的引用改为调 `_visibilityChecker.IsGameWindowVisible`，避免 PiP 退出时误用焦点状态。
7. `TogglePotatoMode`（关闭土豆模式时的分支）中 `Application.isFocused` 的引用同样改为 `_visibilityChecker.IsGameWindowVisible`：可见 → `Normal`，不可见且开关开启 → `Background`。与 `OnFocusChanged`/轮询保持同一判定口径，避免关闭土豆模式时因失焦误切 `Background`。

### 改造 `PotatoController`

- `OnApplicationFocus`：继续调用 `_renderManager.OnFocusChanged(hasFocus)`（签名不变），行为变化由 `RenderQualityManager` 内部承接：
  - `hasFocus == true` → 内部立即恢复 `Normal`；
  - `hasFocus == false` → 内部仅标记待检查，不降级。
- 日志触发条件与文案更新（当前 `OnApplicationFocus` 第 62-63 行用 `_renderManager?.IsBackgroundMode == true` 作为失焦日志条件，新逻辑下失焦不再立即切 `Background`，该条件永不成立会导致静默）：
  - 获焦日志：`hasFocus && wasBackgroundMode` → 打印 "窗口获焦 → 恢复前台模式"（条件不变，`wasBackgroundMode` 取自失焦期间轮询的结果）；
  - 失焦日志：`!hasFocus` → 无条件打印 "窗口失焦 → 等待可见性确认"（不再依赖 `IsBackgroundMode`），告知用户已进入待检查状态；实际降级发生时由 `RenderQualityManager.SetMode` 内部日志补一条 "窗口不可见 → 后台省电模式"。
- `InitializeManagers`：创建 `WindowVisibilityChecker` 并传入 `RenderQualityManager`。

### 配置项

`CfgEnableBackgroundOptimization` 键名保持 `EnableBackgroundOptimization`（兼容旧配置），描述更新为：

> "游戏窗口不可见时自动省电（最小化/虚拟桌面切换/拖出屏幕/同显示器有全屏程序时触发：限帧10fps+渲染分辨率0.1+关闭阴影）"

### Win32 API 新增清单

集中声明于 `WindowVisibilityChecker.cs`：

| API | 用途 |
|-----|------|
| `IsIconic` | 判定 1 最小化 |
| `DwmGetWindowAttribute` (DWMWA_CLOAKED=14) | 判定 2 虚拟桌面隐藏 |
| `GetSystemMetrics` (SM_XVIRTUALSCREEN/Y/CX/CY) | 判定 3 虚拟屏幕范围 |
| `MonitorFromWindow` (MONITOR_DEFAULTTONEAREST) | 判定 4 取游戏所在显示器 |
| `GetMonitorInfoW` | 判定 4 取显示器矩形 |
| `EnumWindows` + 回调 | 判定 4 枚举顶层窗口 |

`RECT` 结构体复用 `WindowManager` 中已有的公开定义，避免重复。

## 错误处理

- 任一 P/Invoke 失败（返回 false / 句柄为零）：该判定项按"不触发不可见"处理（保守倾向保持画质，避免误降级）。
- `gameHwnd` 无效（零或 `IsWindow` false）：`IsGameWindowVisible` 返回 true（不降级），并记录一次警告日志。
- `EnumWindows` 回调内异常：捕获并跳过该窗口，不中断枚举。

## 性能

- 轮询间隔 0.5s；失焦事件触发后首次检查无延迟。
- 判定 1/2/3 为单次轻量 P/Invoke。
- 判定 4（EnumWindows）仅在前 3 项均判定可见后执行；枚举顶层窗口数量通常有限，单次约微秒级，每秒最多 2 次，可接受。
- 不引入后台线程，全部在主线程 `Update` 内完成，避免 Unity API 跨线程问题。

## 不改动的部分

- `Potato` 模式（F2）优先级最高、`PiP`（F3）小窗豁免逻辑、`Background` 模式的限帧/renderScale 参数（`Constants.BackgroundTargetFPS=10`、`Constants.BackgroundRenderScale=0.1`）。
- `RenderQualityManager` 的三级模式枚举、`SetMode`/`ApplyCurrentMode`/`Restore*Mode` 内部实现。
- `WindowManager` 现有 API（仅复用 `RECT`、`GetCurrentWindowHandle`、`GetWindowBounds`）。

## 测试策略

1. **单元测试**（`iGPU Savior.Tests`，xUnit，无 Unity 依赖）：
   - 新增 `VisibilityDeciderTests`，覆盖：
     - 4 项判定各自命中 → 不可见；
     - 4 项全不命中 → 可见；
     - 边界：窗口矩形与虚拟屏幕刚好相切（有交集）→ 可见；
     - `GameMonitor` 为 null 时判定 4 跳过，仅看 1/2/3；
     - 全屏窗口列表为空 → 判定 4 不触发不可见；
     - 全屏窗口在另一显示器 → 不触发不可见。
   - 参考 `NoteExportFolderPickerTests` 的 stub 模式，`VisibilitySignals` 用纯数据结构直接构造。
2. **构建验证**：
   - `dotnet test "iGPU Savior.Tests\iGPU Savior.Tests.csproj"`
   - `dotnet build "iGPU Savior\iGPU Savior.csproj" -c Release`（需 `GameDir` 指向本机游戏目录）
3. **手动验证场景**（用户在多显示器环境确认）：
   - 游戏在 B 屏、焦点切到 A 屏普通窗口 → 保持正常画质；
   - 最小化游戏 → 降级；还原 → 恢复；
   - Win+Tab 切到另一虚拟桌面 → 降级；切回 → 恢复；
   - 把游戏窗口拖出所有屏幕边缘 → 降级；拖回 → 恢复；
   - 游戏所在显示器开另一程序全屏 → 降级；退出全屏 → 恢复；
   - A 屏开全屏视频、游戏在 B 屏窗口化 → 保持正常画质。

## 风险与缓解

- **EnumWindows 性能**：已用短路 + 节流缓解；若极端环境下窗口数量巨大，可在后续迭代改为缓存全屏窗口列表。本次不预先优化。
- **全屏判定口径**：无边框窗口全屏（如浏览器 F11）会被算作全屏，符合需求；真窗口化的最大化窗口不算全屏（矩形可能等于显示器矩形，但 `IsZoomed` 与全屏不同）——本设计按"矩形覆盖显示器"判定，最大化窗口也会命中。这是可接受的粗粒度，符合"有全屏就激活"的需求。若用户反馈误判，后续可加 `WS_POPUP` 样式过滤。
- **配置兼容性**：键名不变，旧配置文件无需迁移；描述变更不影响运行时行为。
