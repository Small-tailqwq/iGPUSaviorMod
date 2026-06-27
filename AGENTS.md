# iGPU Savior — 开发备忘录

## 构建与测试
```powershell
# 主项目（必须指定 GameDir 指向你的游戏安装目录）
dotnet msbuild "iGPU Savior\iGPU Savior.csproj" /t:Build /p:Configuration=Release /p:GameDir="YOUR_GAME_DIR"
# 或简写
dotnet build "iGPU Savior\iGPU Savior.csproj" -c Release

# 测试（独立 xUnit 工程，无 Unity 依赖，可离线运行）
dotnet test "iGPU Savior.Tests\iGPU Savior.Tests.csproj"
```
- csproj 默认 `GameDir=E:\SteamLibrary\...`，**必须改成你本机的路径**
- 运行时依赖来自 `GameDir` 下的本地 DLL；主项目只使用 build-only `BepInEx.AssemblyPublicizer.MSBuild` NuGet 包，测试工程使用 xUnit 相关 NuGet 包

### 版本号管理
- `version.json` 为唯一真实来源，运行 `scripts/sync-version.ps1` 同步到 `Constants.cs`、`thunderstore/manifest.json`、`CHANGELOG.md`
- 发版前执行 `scripts/sync-version.ps1 -CheckOnly` 做一致性检查

## 命名空间与模块边界
```
PotatoOptimization.Core       — 入口 + 主循环（PotatoPlugin / PotatoController）
PotatoOptimization.Configuration  — ConfigEntry 管理
PotatoOptimization.Features    — 功能实现（RenderQuality, CameraMirror, PortraitMode, WindowState, Audio, NoteExport...）
PotatoOptimization.Patches     — Harmony 补丁
PotatoOptimization.UI          — MOD 设置面板注入与克隆控件
PotatoOptimization.Utilities   — Win32 API, 反射, 工具类
ModShared                      — ModSettingsManager（外部 MOD 通过此命名空间接入）
```
- 入口：`PotatoPlugin.Awake()` → Config → typeof() 强制加载补丁 → Harmony.PatchAll() → 创建 Controller
- `PotatoController` 是 `MonoBehaviour`，挂载在 `DontDestroyOnLoad` 的 `PotatoRunner` 上
- 功能模块在 `InitializeManagers()` 中创建：RenderQualityManager, CameraMirrorManager, PortraitModeManager, WindowStateManager, PiPGuiManager, AudioManager

## Harmony 补丁陷阱（极易踩坑）
`harmony.PatchAll()` 只扫描**已加载到运行时**的类型，所以 `ApplyHarmonyPatches()` 中必须先用 `typeof(SomePatch)` 强制加载每个补丁类，再调 `PatchAll()`，否则补丁静默不生效。PotatoPlugin.cs 中已有 12 个 typeof() 加载调用——新增补丁后记得补上。

## MOD 设置面板系统
- `ModSettingsManager`（`ModShared` 命名空间）是单例，提供 `RegisterMod()`/`AddToggle()`/`AddDropdown()` 供外部 MOD 接入
- 注入点：`ModSettingsIntegration`（Harmony 补丁，Patch `SettingUI.Setup()`）
- 控件克隆自原版 UI：`ModPulldownCloner`（下拉框）+ `ModToggleCloner`（开关）
- 关键枚举：`WindowScaleRatio`（OneThird=3/OneFourth=4/OneFifth=5）、`DragMode`（Ctrl_LeftClick / Alt_LeftClick / RightClick_Hold）
- 游戏使用 VContainer DI：`NestopiSystem.DIContainers.ProjectLifetimeScope.Resolve<T>()`

## 关键游戏内部 API（反编译情报）
- `Bulbul.SettingService` — 画质总控
- `Bulbul.SettingData` — `.Value` 写入即时生效
- `Bulbul.GraphicQualityLevel` — Low / Medium / High
- `Bulbul.RoomCameraManager` — 故事模式 `OnStartStory()` / `OnEndStory()`
- `Bulbul.RoomGameManager.Update()` — 调度 UpdatePlatform / UpdateFacility / UpdateHeroineAI
- URP: `GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset`
- `NoteUI` / `SelectPageListUI` / `CurrentPageUI` 不在 `Bulbul.` 命名空间下（全局空间）

## 快捷键
- `F2` — 土豆模式 / `F3` — 无边框小窗 / `F4` — 相机镜像 / `F5` — 竖屏优化

## 提交规范
- 提交信息（commit message）必须使用中文

## MCP 工具
- `ilspy-mcp`：程序集反编译，注意 `NoteUI` 等不属于 `Bulbul.` 命名空间，使用非限定名搜索
- `unity-explorer`：运行时 UI 检视（BepInEx 插件，游戏内 F7），`componentType: "Text"` 自动兼容 TMP
