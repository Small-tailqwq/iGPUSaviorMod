# 后台省电模式：基于窗口可见性的触发改造 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 把后台省电触发条件从"窗口失焦"改为"游戏窗口在屏幕上不可见"，正确处理多显示器，避免焦点与可见性解耦时的误判。

**Architecture:** 新增纯逻辑 `VisibilityDecider`（可单测）+ Win32 采集器 `WindowVisibilityChecker`；`RenderQualityManager` 改为事件信号 + 0.5s 轮询兜底（获焦立即恢复，失焦仅标记待检查）；`PotatoController` 调整日志触发条件；配置项描述更新。

**Tech Stack:** C# (.NET Framework 4.7.2 主工程 / .NET 8 测试工程), Win32 P/Invoke, Unity 2019.4.x, BepInEx, xUnit, MSBuild.

## 全局约束

- 主工程 `iGPU Savior\iGPU Savior.csproj` 为 SDK 风格、默认 globbing，新建 .cs 文件自动纳入编译，无需改 csproj。
- 测试工程 `iGPU Savior.Tests\iGPU Savior.Tests.csproj` 设 `EnableDefaultCompileItems=false`，每个测试文件与被测源文件都必须显式 `<Compile Include>`，源文件用 `Link=` 链接。
- 被链接进测试工程的源文件**不得** `using UnityEngine;` 或引用 `PotatoOptimization.Core`（含 `PotatoPlugin`），否则 net8.0 测试工程无法编译。
- 提交信息使用中文。
- 构建命令需 `GameDir` 指向本机游戏目录；测试工程无此依赖，可离线运行。
- 测试命令：`dotnet test "iGPU Savior.Tests\iGPU Savior.Tests.csproj"`
- 构建命令：`dotnet build "iGPU Savior\iGPU Savior.csproj" -c Release`（须确保 csproj 中 `GameDir` 默认值或显式传入指向本机游戏目录）

## 对 spec 的有意偏差

1. **`VisRect` 独立结构体**：spec 提"复用 `WindowManager.RECT`"。但 `WindowManager.cs` 含 `PotatoOptimization.Core` 依赖（`PotatoPlugin.Log`），无法链接进无 Unity 依赖的测试工程。故在 `VisibilityDecider.cs` 内定义纯 `VisRect`，`WindowVisibilityChecker` 负责在采集时做 `WindowManager.RECT → VisRect` 映射。判定逻辑与 Win32 完全解耦。
2. **`FullscreenWindows` + `IsOnGameMonitor`**：spec 字段名为 `FullscreenWindowsOnGameMonitor`（暗示预过滤）。改为 `FullscreenWindows`（所有覆盖游戏显示器矩形的候选窗口）+ 每项带 `IsOnGameMonitor` 布尔，由 `VisibilityDecider` 过滤。这样"全屏窗口在另一显示器 → 可见"可在 decider 单测中直接构造 `IsOnGameMonitor=false` 验证，且 decider 的过滤逻辑在生产行为一致。

## 文件结构

| 文件 | 责任 | 测试工程链接 |
|------|------|-------------|
| `iGPU Savior/Utilities/VisibilityDecider.cs` | 纯逻辑判定（4 项 OR）+ 信号/结构体定义，无 Unity 依赖 | 是 |
| `iGPU Savior/Utilities/WindowVisibilityChecker.cs` | Win32 P/Invoke 采集信号，调 `VisibilityDecider` | 否（有 P/Invoke + PotatoPlugin.Log） |
| `iGPU Savior/Features/RenderQualityManager.cs` | 改造触发链：事件 + 轮询 | 否 |
| `iGPU Savior/Core/PotatoController.cs` | 接线 + 日志条件 | 否 |
| `iGPU Savior/Core/Constants.cs` | 新增轮询间隔常量 | 否 |
| `iGPU Savior/Configuration/ConfigurationManager.cs` | 配置描述更新 | 否 |
| `iGPU Savior.Tests/VisibilityDeciderTests.cs` | 纯逻辑单测 | — |

> 说明：为使 `VisibilityDecider` 可在 net8.0 测试工程独立编译，信号结构体使用自带的 `VisRect` / `MonitorBounds` / `FullscreenWindowInfo`（而非 `WindowManager.RECT`），由 `WindowVisibilityChecker` 负责从 `WindowManager.RECT` 映射。这是对 spec "复用 RECT" 的测试性精化，避免把 `WindowManager.cs`（含 `PotatoPlugin.Log` 引用）拖入测试工程。

---

## Task 1: 纯逻辑判定器 `VisibilityDecider`

**Files:**
- Create: `iGPU Savior/Utilities/VisibilityDecider.cs`
- Create: `iGPU Savior.Tests/VisibilityDeciderTests.cs`
- Modify: `iGPU Savior.Tests/iGPU Savior.Tests.csproj`（新增两个 `<Compile Include>`）

**Interfaces:**
- Produces: `PotatoOptimization.Utilities.VisRect`、`MonitorBounds`、`FullscreenWindowInfo`、`VisibilitySignals`、`static class VisibilityDecider.IsVisible(VisibilitySignals) → bool`

- [ ] **Step 1: 在测试工程 csproj 登记新文件**

在 `iGPU Savior.Tests/iGPU Savior.Tests.csproj` 的 `<ItemGroup>` 末尾（第 42 行 `VisibleWhenEvaluatorTests.cs` 之后、闭合 `</ItemGroup>` 之前）追加：

```xml
    <Compile Include="VisibilityDeciderTests.cs" />
    <Compile Include="..\iGPU Savior\Utilities\VisibilityDecider.cs" Link="VisibilityDecider.cs" />
```

- [ ] **Step 2: 写失败测试**

创建 `iGPU Savior.Tests/VisibilityDeciderTests.cs`：

```csharp
using System.Collections.Generic;
using PotatoOptimization.Utilities;
using Xunit;

namespace IGPUSavior.Tests
{
    public class VisibilityDeciderTests
    {
        private static readonly VisRect VScreen = new VisRect(0, 0, 1920, 1080);
        private static readonly VisRect InScreen = new VisRect(100, 100, 800, 600);
        private static readonly MonitorBounds GameMon = new MonitorBounds { Bounds = VScreen };

        [Fact]
        public void IsVisible_Minimized_ReturnsFalse()
        {
            var s = NewSignals();
            s.IsIconic = true;
            Assert.False(VisibilityDecider.IsVisible(s));
        }

        [Fact]
        public void IsVisible_Cloaked_ReturnsFalse()
        {
            var s = NewSignals();
            s.IsCloaked = true;
            Assert.False(VisibilityDecider.IsVisible(s));
        }

        [Fact]
        public void IsVisible_DraggedOffscreen_ReturnsFalse()
        {
            var s = NewSignals();
            s.WindowRect = new VisRect(-5000, -5000, -4000, -4000);
            Assert.False(VisibilityDecider.IsVisible(s));
        }

        [Fact]
        public void IsVisible_TouchingVirtualScreenEdge_ReturnsTrue()
        {
            // 窗口右边恰好贴到虚拟屏幕左边（相切），按"有交集"判为可见
            var s = NewSignals();
            s.WindowRect = new VisRect(-100, 0, 0, 100);
            Assert.True(VisibilityDecider.IsVisible(s));
        }

        [Fact]
        public void IsVisible_GameMonitorNull_NoFullscreen_ReturnsTrue()
        {
            var s = NewSignals();
            s.GameMonitor = null;
            s.FullscreenWindows = new List<FullscreenWindowInfo>();
            Assert.True(VisibilityDecider.IsVisible(s));
        }

        [Fact]
        public void IsVisible_FullscreenOnGameMonitorCovering_ReturnsFalse()
        {
            var s = NewSignals();
            s.FullscreenWindows = new List<FullscreenWindowInfo>
            {
                new FullscreenWindowInfo { Rect = VScreen, IsOnGameMonitor = true }
            };
            Assert.False(VisibilityDecider.IsVisible(s));
        }

        [Fact]
        public void IsVisible_FullscreenOnOtherMonitor_ReturnsTrue()
        {
            var s = NewSignals();
            s.FullscreenWindows = new List<FullscreenWindowInfo>
            {
                new FullscreenWindowInfo { Rect = VScreen, IsOnGameMonitor = false }
            };
            Assert.True(VisibilityDecider.IsVisible(s));
        }

        [Fact]
        public void IsVisible_FullscreenOnGameMonitorNotCovering_ReturnsTrue()
        {
            var s = NewSignals();
            s.FullscreenWindows = new List<FullscreenWindowInfo>
            {
                new FullscreenWindowInfo { Rect = new VisRect(0, 0, 100, 100), IsOnGameMonitor = true }
            };
            Assert.True(VisibilityDecider.IsVisible(s));
        }

        [Fact]
        public void IsVisible_FullscreenListNull_ReturnsTrue()
        {
            var s = NewSignals();
            s.FullscreenWindows = null;
            Assert.True(VisibilityDecider.IsVisible(s));
        }

        private static VisibilitySignals NewSignals() =>
            new VisibilitySignals
            {
                IsIconic = false,
                IsCloaked = false,
                WindowRect = InScreen,
                VirtualScreen = VScreen,
                GameMonitor = GameMon,
                FullscreenWindows = new List<FullscreenWindowInfo>()
            };
    }
}
```

- [ ] **Step 3: 运行测试确认失败**

Run: `dotnet test "iGPU Savior.Tests\iGPU Savior.Tests.csproj"`
Expected: 编译失败（`VisibilityDecider` / `VisRect` 等类型未定义）

- [ ] **Step 4: 写最小实现**

创建 `iGPU Savior/Utilities/VisibilityDecider.cs`：

```csharp
using System.Collections.Generic;

namespace PotatoOptimization.Utilities
{
    public struct VisRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public VisRect(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }
    }

    public struct MonitorBounds
    {
        public VisRect Bounds;
    }

    public struct FullscreenWindowInfo
    {
        public VisRect Rect;
        public bool IsOnGameMonitor;
    }

    public struct VisibilitySignals
    {
        public bool IsIconic;
        public bool IsCloaked;
        public VisRect WindowRect;
        public VisRect VirtualScreen;
        public MonitorBounds? GameMonitor;
        public IReadOnlyList<FullscreenWindowInfo> FullscreenWindows;
    }

    /// <summary>
    /// 纯逻辑判定：游戏窗口是否对用户可见。无 P/Invoke，可单测。
    /// 不可见（任一命中）：最小化 / 虚拟桌面隐藏 / 完全拖出虚拟屏幕 /
    /// 游戏所在显示器有其他全屏窗口覆盖。
    /// </summary>
    public static class VisibilityDecider
    {
        public static bool IsVisible(VisibilitySignals s)
        {
            if (s.IsIconic) return false;
            if (s.IsCloaked) return false;
            if (!RectsIntersect(s.WindowRect, s.VirtualScreen)) return false;
            if (!s.GameMonitor.HasValue) return true;

            var gameBounds = s.GameMonitor.Value.Bounds;
            var list = s.FullscreenWindows;
            if (list == null) return true;

            foreach (var fw in list)
            {
                if (!fw.IsOnGameMonitor) continue;
                if (Covers(fw.Rect, gameBounds)) return false;
            }
            return true;
        }

        // 相切（边缘重合）视为有交集 → 可见，故用严格不等号判定"完全在外"
        private static bool RectsIntersect(VisRect a, VisRect b)
        {
            return !(a.Right < b.Left || a.Left > b.Right ||
                     a.Bottom < b.Top || a.Top > b.Bottom);
        }

        private static bool Covers(VisRect cover, VisRect target)
        {
            return cover.Left <= target.Left && cover.Right >= target.Right &&
                   cover.Top <= target.Top && cover.Bottom >= target.Bottom;
        }
    }
}
```

- [ ] **Step 5: 运行测试确认通过**

Run: `dotnet test "iGPU Savior.Tests\iGPU Savior.Tests.csproj"`
Expected: 全部 9 个 `VisibilityDeciderTests` 用例 PASS

- [ ] **Step 6: 提交**

```bash
git add "iGPU Savior/Utilities/VisibilityDecider.cs" "iGPU Savior.Tests/VisibilityDeciderTests.cs" "iGPU Savior.Tests/iGPU Savior.Tests.csproj"
git commit -m "功能：新增窗口可见性纯逻辑判定器 VisibilityDecider 及单测"
```

---

## Task 2: Win32 采集器 `WindowVisibilityChecker`

**Files:**
- Create: `iGPU Savior/Utilities/WindowVisibilityChecker.cs`

**Interfaces:**
- Consumes: `PotatoOptimization.Utilities.VisibilityDecider`、`VisibilitySignals`、`VisRect`、`MonitorBounds`、`FullscreenWindowInfo`（Task 1）；`PotatoOptimization.Utilities.WindowManager.RECT` / `GetCurrentWindowHandle()`（现有）；`PotatoOptimization.Core.PotatoPlugin.Log`（现有）
- Produces: `public sealed class WindowVisibilityChecker` with `ctor(Func<IntPtr> getGameWindowHandle)` and `bool IsGameWindowVisible()`

- [ ] **Step 1: 写实现**

创建 `iGPU Savior/Utilities/WindowVisibilityChecker.cs`：

```csharp
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using PotatoOptimization.Core;

namespace PotatoOptimization.Utilities
{
    /// <summary>
    /// 通过 Win32 判定游戏窗口是否在屏幕上对用户可见。
    /// 采集 <see cref="VisibilitySignals"/> 后委托 <see cref="VisibilityDecider"/> 判定。
    /// </summary>
    public sealed class WindowVisibilityChecker
    {
        private const int DWMWA_CLOAKED = 14;
        private const int SM_XVIRTUALSCREEN = 76;
        private const int SM_YVIRTUALSCREEN = 77;
        private const int SM_CXVIRTUALSCREEN = 78;
        private const int SM_CYVIRTUALSCREEN = 79;
        private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out WindowManager.RECT lpRect);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("dwmapi.dll")]
        private static extern int DwmGetWindowAttribute(IntPtr hwnd, int attribute, ref int pvAttribute, int cbAttribute);

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;
            public WindowManager.RECT rcMonitor;
            public WindowManager.RECT rcWork;
            public uint dwFlags;
        }

        private readonly Func<IntPtr> _getGameWindowHandle;

        public WindowVisibilityChecker(Func<IntPtr> getGameWindowHandle)
        {
            _getGameWindowHandle = getGameWindowHandle;
        }

        public bool IsGameWindowVisible()
        {
            IntPtr hwnd = _getGameWindowHandle();
            if (hwnd == IntPtr.Zero || !IsWindow(hwnd))
            {
                PotatoPlugin.Log?.LogWarning("WindowVisibilityChecker: 游戏窗口句柄无效，按可见处理");
                return true;
            }

            IntPtr gameMonHandle = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            MonitorBounds? gameMonitor = GetMonitorBounds(gameMonHandle);

            var signals = new VisibilitySignals
            {
                IsIconic = SafeIsIconic(hwnd),
                IsCloaked = GetCloaked(hwnd),
                WindowRect = ToVisRect(SafeGetWindowRect(hwnd)),
                VirtualScreen = GetVirtualScreen(),
                GameMonitor = gameMonitor,
                FullscreenWindows = EnumerateFullscreenWindows(hwnd, gameMonHandle, gameMonitor)
            };

            return VisibilityDecider.IsVisible(signals);
        }

        private static bool SafeIsIconic(IntPtr hwnd)
        {
            try { return IsIconic(hwnd); }
            catch { return false; }
        }

        private static bool GetCloaked(IntPtr hwnd)
        {
            try
            {
                int cloaked = 0;
                int hr = DwmGetWindowAttribute(hwnd, DWMWA_CLOAKED, ref cloaked, sizeof(int));
                return hr >= 0 && cloaked != 0;
            }
            catch { return false; }
        }

        private static WindowManager.RECT SafeGetWindowRect(IntPtr hwnd)
        {
            if (GetWindowRect(hwnd, out WindowManager.RECT r)) return r;
            return new WindowManager.RECT();
        }

        private static VisRect GetVirtualScreen()
        {
            int x = GetSystemMetrics(SM_XVIRTUALSCREEN);
            int y = GetSystemMetrics(SM_YVIRTUALSCREEN);
            int w = GetSystemMetrics(SM_CXVIRTUALSCREEN);
            int h = GetSystemMetrics(SM_CYVIRTUALSCREEN);
            return new VisRect(x, y, x + w, y + h);
        }

        private static MonitorBounds? GetMonitorBounds(IntPtr hmon)
        {
            if (hmon == IntPtr.Zero) return null;
            var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            if (!GetMonitorInfo(hmon, ref info)) return null;
            return new MonitorBounds { Bounds = ToVisRect(info.rcMonitor) };
        }

        private static List<FullscreenWindowInfo> EnumerateFullscreenWindows(
            IntPtr gameHwnd, IntPtr gameMonHandle, MonitorBounds? gameMonitor)
        {
            var list = new List<FullscreenWindowInfo>();
            if (!gameMonitor.HasValue || gameMonHandle == IntPtr.Zero) return list;

            VisRect gameBounds = gameMonitor.Value.Bounds;
            EnumWindowsProc callback = (IntPtr h, IntPtr lp) =>
            {
                try
                {
                    if (h == gameHwnd) return true;
                    if (!IsWindowVisible(h)) return true;
                    if (!GetWindowRect(h, out WindowManager.RECT r)) return true;

                    VisRect vr = ToVisRect(r);
                    if (!Covers(vr, gameBounds)) return true;

                    IntPtr hmon = MonitorFromWindow(h, MONITOR_DEFAULTTONEAREST);
                    list.Add(new FullscreenWindowInfo
                    {
                        Rect = vr,
                        IsOnGameMonitor = hmon == gameMonHandle
                    });
                }
                catch { }
                return true;
            };

            EnumWindows(callback, IntPtr.Zero);
            return list;
        }

        private static VisRect ToVisRect(WindowManager.RECT r) =>
            new VisRect(r.Left, r.Top, r.Right, r.Bottom);

        private static bool Covers(VisRect cover, VisRect target) =>
            cover.Left <= target.Left && cover.Right >= target.Right &&
            cover.Top <= target.Top && cover.Bottom >= target.Bottom;
    }
}
```

- [ ] **Step 2: 构建主工程确认编译通过**

Run: `dotnet build "iGPU Savior\iGPU Savior.csproj" -c Release`
Expected: 成功，无错误（`GameDir` 须指向本机游戏目录；若默认值不存在，命令加 `/p:GameDir="你的游戏目录"`）

- [ ] **Step 3: 提交**

```bash
git add "iGPU Savior/Utilities/WindowVisibilityChecker.cs"
git commit -m "功能：新增 Win32 窗口可见性采集器 WindowVisibilityChecker"
```

---

## Task 3: 改造 `RenderQualityManager`（事件 + 轮询）

**Files:**
- Modify: `iGPU Savior/Core/Constants.cs`（新增常量）
- Modify: `iGPU Savior/Features/RenderQualityManager.cs`（构造函数、字段、`OnFocusChanged`、`Update`、`SetPiPModeActive`、`TogglePotatoMode`）

**Interfaces:**
- Consumes: `PotatoOptimization.Utilities.WindowVisibilityChecker`（Task 2）、`PotatoOptimization.Core.Constants`（现有）
- Produces: `RenderQualityManager` 构造函数签名变更为 `ctor(WindowVisibilityChecker)`

- [ ] **Step 1: 在 `Constants.cs` 新增轮询间隔常量**

在 `iGPU Savior/Core/Constants.cs` 第 48 行 `BackgroundRunInterval` 之后追加：

```csharp
    // 可见性轮询间隔（秒）— 失焦后用此频率检查窗口是否真的不可见
    public const float VisibilityCheckInterval = 0.5f;
```

- [ ] **Step 2: 改造 `RenderQualityManager` 字段与构造函数**

在 `iGPU Savior/Features/RenderQualityManager.cs` 顶部 using 区追加：

```csharp
using PotatoOptimization.Utilities;
```

在类内字段区（第 45 行 `_isPiPModeActive` 之后）追加：

```csharp
        private readonly WindowVisibilityChecker _visibilityChecker;
        private float _lastVisibilityCheckTime;
        private bool _pendingVisibilityCheck;
```

将默认构造函数替换为注入构造函数。在 `IsBackgroundMode` 属性（第 57 行）之后插入：

```csharp
        public RenderQualityManager(WindowVisibilityChecker visibilityChecker)
        {
            _visibilityChecker = visibilityChecker;
        }
```

- [ ] **Step 3: 改造 `SetPiPModeActive` 中的 `Application.isFocused` 引用**

将 `iGPU Savior/Features/RenderQualityManager.cs` 第 71-77 行：

```csharp
            else if (!active &&
                     _currentMode == RenderMode.Normal &&
                     !Application.isFocused &&
                     PotatoPlugin.Config.CfgEnableBackgroundOptimization.Value)
            {
                SetMode(RenderMode.Background);
            }
```

替换为：

```csharp
            else if (!active &&
                     _currentMode == RenderMode.Normal &&
                     !_visibilityChecker.IsGameWindowVisible() &&
                     PotatoPlugin.Config.CfgEnableBackgroundOptimization.Value)
            {
                SetMode(RenderMode.Background);
            }
```

- [ ] **Step 4: 改造 `TogglePotatoMode` 中的 `Application.isFocused` 引用**

将第 88-90 行：

```csharp
                if (_isPiPModeActive ||
                    Application.isFocused ||
                    !PotatoPlugin.Config.CfgEnableBackgroundOptimization.Value)
                    SetMode(RenderMode.Normal);
```

替换为：

```csharp
                if (_isPiPModeActive ||
                    _visibilityChecker.IsGameWindowVisible() ||
                    !PotatoPlugin.Config.CfgEnableBackgroundOptimization.Value)
                    SetMode(RenderMode.Normal);
```

- [ ] **Step 5: 改造 `OnFocusChanged`（失焦不再直接降级）**

将第 106-123 行整个 `OnFocusChanged` 方法替换为：

```csharp
        public void OnFocusChanged(bool focused)
        {
            if (_currentMode == RenderMode.Potato)
                return; // 土豆模式优先级最高，不受焦点影响

            if (_isPiPModeActive)
            {
                if (_currentMode == RenderMode.Background)
                    SetMode(RenderMode.Normal);
                return;
            }

            if (focused)
            {
                // 获焦必然可见，事件驱动零延迟恢复
                SetMode(RenderMode.Normal);
            }
            else
            {
                // 失焦不直接降级，标记待可见性检查（Update 轮询承接）
                _pendingVisibilityCheck = true;
                _lastVisibilityCheckTime = 0f;
            }
        }
```

- [ ] **Step 6: 改造 `Update`（新增可见性轮询块）**

将第 128-149 行整个 `Update` 方法替换为：

```csharp
        public void Update()
        {
            // 可见性轮询（失焦后由轮询决定是否真正降级）
            bool timeToCheck = _pendingVisibilityCheck ||
                (Time.realtimeSinceStartup - _lastVisibilityCheckTime) > Constants.VisibilityCheckInterval;
            if (timeToCheck)
            {
                _pendingVisibilityCheck = false;
                _lastVisibilityCheckTime = Time.realtimeSinceStartup;
                PollVisibility();
            }

            // 开关关闭或 PiP 时，若仍处后台模式则恢复
            if (_currentMode == RenderMode.Background &&
                (!PotatoPlugin.Config.CfgEnableBackgroundOptimization.Value || _isPiPModeActive))
            {
                SetMode(RenderMode.Normal);
                return;
            }

            if (_currentMode == RenderMode.Normal)
                return;

            float interval = _currentMode == RenderMode.Background
                ? Constants.BackgroundRunInterval
                : Constants.DefaultRunInterval;

            if (Time.realtimeSinceStartup - _lastRunTime > interval)
            {
                ApplyCurrentMode();
                _lastRunTime = Time.realtimeSinceStartup;
            }
        }

        private void PollVisibility()
        {
            if (_currentMode == RenderMode.Potato) return;
            if (_isPiPModeActive) return;
            if (!PotatoPlugin.Config.CfgEnableBackgroundOptimization.Value) return;

            bool visible = _visibilityChecker.IsGameWindowVisible();
            if (!visible && _currentMode == RenderMode.Normal)
            {
                SetMode(RenderMode.Background);
                PotatoPlugin.Log.LogWarning(">>> 窗口不可见 → 后台省电模式 <<<");
            }
            else if (visible && _currentMode == RenderMode.Background)
            {
                SetMode(RenderMode.Normal);
                PotatoPlugin.Log.LogWarning(">>> 窗口恢复可见 → 前台模式 <<<");
            }
        }
```

- [ ] **Step 7: 构建主工程确认编译通过**

Run: `dotnet build "iGPU Savior\iGPU Savior.csproj" -c Release`
Expected: 成功（`PotatoController` 尚未传参，本步暂不构建——见 Task 4 一起构建）。改为仅做语法校验：先跳到 Task 4 完成接线后统一构建。

> 注：本 Task 改动了 `RenderQualityManager` 构造签名，`PotatoController` 仍用旧的无参构造，编译会失败。这是预期的中间状态，Task 4 Step 1 会修复接线并在 Step 2 统一构建。本 Task 不单独提交，与 Task 4 合并提交。

- [ ] **Step 8: 暂不提交，继续 Task 4**

---

## Task 4: `PotatoController` 接线 + 日志条件 + 配置描述

**Files:**
- Modify: `iGPU Savior/Core/PotatoController.cs`（`InitializeManagers`、`OnApplicationFocus`）
- Modify: `iGPU Savior/Configuration/ConfigurationManager.cs`（配置描述）

**Interfaces:**
- Consumes: `PotatoOptimization.Utilities.WindowVisibilityChecker`（Task 2）、`PotatoOptimization.Utilities.WindowManager`（现有）、`RenderQualityManager` 新构造（Task 3）

- [ ] **Step 1: 接线 `WindowVisibilityChecker`**

在 `iGPU Savior/Core/PotatoController.cs` 顶部 using 区追加：

```csharp
using PotatoOptimization.Utilities;
```

将 `InitializeManagers` 方法（第 75-84 行）中的：

```csharp
            _renderManager = new RenderQualityManager();
```

替换为：

```csharp
            _renderManager = new RenderQualityManager(
                new WindowVisibilityChecker(WindowManager.GetCurrentWindowHandle));
```

- [ ] **Step 2: 更新 `OnApplicationFocus` 日志触发条件**

将 `OnApplicationFocus` 方法（第 54-64 行）替换为：

```csharp
        void OnApplicationFocus(bool hasFocus)
        {
            // 焦点变化通知渲染管理器：获焦立即恢复，失焦仅标记待可见性检查
            bool wasBackgroundMode = _renderManager?.IsBackgroundMode == true;
            _renderManager?.OnFocusChanged(hasFocus);

            if (hasFocus && wasBackgroundMode)
                PotatoPlugin.Log.LogWarning(">>> 窗口获焦 → 恢复前台模式 <<<");
            else if (!hasFocus)
                PotatoPlugin.Log.LogWarning(">>> 窗口失焦 → 等待可见性确认 <<<");
        }
```

> 关键变更：失焦日志原条件 `!hasFocus && _renderManager?.IsBackgroundMode == true` 在新逻辑下永不成立（失焦不再立即切 Background），会静默。改为 `!hasFocus` 无条件打印。实际降级日志由 `PollVisibility` 内部打印。

- [ ] **Step 3: 更新配置项描述**

将 `iGPU Savior/Configuration/ConfigurationManager.cs` 第 71-72 行：

```csharp
            CfgEnableBackgroundOptimization = _config.Bind("Performance", "EnableBackgroundOptimization", true,
                "窗口失焦时自动省电(限帧10fps+渲染分辨率0.1+关闭阴影)");
```

替换为：

```csharp
            CfgEnableBackgroundOptimization = _config.Bind("Performance", "EnableBackgroundOptimization", true,
                "游戏窗口不可见时自动省电(最小化/虚拟桌面切换/拖出屏幕/同显示器有全屏程序时触发:限帧10fps+渲染分辨率0.1+关闭阴影)");
```

- [ ] **Step 4: 构建主工程确认编译通过**

Run: `dotnet build "iGPU Savior\iGPU Savior.csproj" -c Release`
Expected: 成功，无错误（`GameDir` 须指向本机游戏目录）

- [ ] **Step 5: 提交 Task 3 + Task 4 合并改动**

```bash
git add "iGPU Savior/Core/Constants.cs" "iGPU Savior/Features/RenderQualityManager.cs" "iGPU Savior/Core/PotatoController.cs" "iGPU Savior/Configuration/ConfigurationManager.cs"
git commit -m "功能：后台省电改为基于窗口可见性触发（事件+轮询，多显示器不再误判）"
```

---

## Task 5: 全量验证

**Files:** 无（仅验证）

- [ ] **Step 1: 运行全部单元测试**

Run: `dotnet test "iGPU Savior.Tests\iGPU Savior.Tests.csproj"`
Expected: 全部通过，含原有用例 + 9 个新 `VisibilityDeciderTests` 用例

- [ ] **Step 2: Release 构建主工程**

Run: `dotnet build "iGPU Savior\iGPU Savior.csproj" -c Release`
Expected: 成功，无错误无警告（`GameDir` 须指向本机游戏目录）

- [ ] **Step 3: 人工验证清单（用户在多显示器环境确认）**

对照 spec `## 测试策略` 第 3 节的 6 个场景逐项确认：
1. 游戏在 B 屏、焦点切到 A 屏普通窗口 → 保持正常画质
2. 最小化游戏 → 降级；还原 → 恢复
3. Win+Tab 切到另一虚拟桌面 → 降级；切回 → 恢复
4. 把游戏窗口拖出所有屏幕边缘 → 降级；拖回 → 恢复
5. 游戏所在显示器开另一程序全屏 → 降级；退出全屏 → 恢复
6. A 屏开全屏视频、游戏在 B 屏窗口化 → 保持正常画质

- [ ] **Step 4: 若人工验证全部通过，无需额外提交；如有问题回到对应 Task 修正**

---

## 自审记录

- **Spec 覆盖**：判定 1-4 → Task 2 采集 + Task 1 判定；事件+轮询 → Task 3；日志静默修正 → Task 4 Step 2；`_pendingVisibilityCheck` 复位 → Task 3 Step 6（先复位再判定）；`TogglePotatoMode` 残留 `Application.isFocused` → Task 3 Step 4；`SetPiPModeActive` 残留 → Task 3 Step 3；配置描述 → Task 4 Step 3；单测 → Task 1。全部覆盖。
- **占位符**：无 TBD/TODO，每步含完整代码。
- **类型一致性**：`VisRect`/`MonitorBounds`/`FullscreenWindowInfo`/`VisibilitySignals`/`VisibilityDecider.IsVisible` 在 Task 1 定义，Task 2 消费，命名一致；`WindowVisibilityChecker` 构造 `Func<IntPtr>` 在 Task 2 定义、Task 4 消费（`WindowManager.GetCurrentWindowHandle` 方法组转换），一致；`RenderQualityManager` 新构造在 Task 3 定义、Task 4 消费，一致。
- **中间状态**：Task 3 改构造签名后 `PotatoController` 暂不匹配，故 Task 3 不单独构建/提交，与 Task 4 合并。已在 Task 3 Step 7/8 与 Task 4 Step 5 说明。
