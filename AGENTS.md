# iGPU Savior — 开发备忘录

## Build
```powershell
# 主项目（Release，带 CI 输出目录覆盖）
dotnet msbuild "iGPU Savior\iGPU Savior.csproj" /t:Build /p:Configuration=Release /p:GameDir="D:\SteamLibrary\steamapps\common\Chill with You Lo-Fi Story" /p:BaseIntermediateOutputPath="obj_ci\\" /p:IntermediateOutputPath="obj_ci\Release\\" /p:OutDir="artifacts\\build\\"

# 测试项目（独立 xUnit 工程，linked-source 无 Unity 依赖）
dotnet test "iGPU Savior.Tests\iGPU Savior.Tests.csproj"
```
- 依赖全部来自 `GameDir` 下的本地 DLL，无 NuGet 包
- csproj 默认 `GameDir=E:\SteamLibrary\...`，**必须改成你本机的路径**

## 关键路径
- 入口：`PotatoOptimization.Core.PotatoPlugin.Awake()` → 初始化 Config → Harmony PatchAll → 创建 Controller
- 命名空间 `PotatoOptimization.*`，输出 `iGPU Savior.dll`
- 游戏 DLL 路径：`D:\SteamLibrary\steamapps\common\Chill with You Lo-Fi Story\Chill With You_Data\Managed\`

## Harmony 补丁陷阱
`harmony.PatchAll()` 只扫描**已加载到运行时**的类型，所以 `ApplyHarmonyPatches()` 中必须先用 `typeof(SomePatch)` 强制加载每个补丁类，再调 `PatchAll()`，否则补丁不会生效。

## 开发分支（worktree）
- Note Export 功能实现位于独立 worktree：`C:\Users\Ko_teiru\.config\superpowers\worktrees\iGPUSaviorMod\note-export`
- 分支：`note-export`，基线 commit：`5bb516c`
- 设计文档：`docs/superpowers/specs/2026-05-07-note-export-design.md`
- 实现计划：`docs/superpowers/plans/2026-05-07-note-export.md`
- 产出 DLL：`iGPU Savior\artifacts\build\iGPU Savior.dll`

## Note Export 实现要点
- 仅 Windows；用 COM `IFileOpenDialog` + `FOS_PICKFOLDERS`
- 导出编码：`new UTF8Encoding(true)`（UTF-8 with BOM）
- 文件名规则：剥离 `<>:"/\|?*` + ASCII control chars → `{清理标题}_{yyyyMMdd_HHmmss}.txt`，空标题回退 `note_{时间戳}.txt`
- async folder picker：后台 STA 线程，结果回主线程继续导出（避免卡 UI）
- 选择模式 UI 注射补丁：`NoteExportPatch.cs` / `NoteExportCardState` / `NoteExportUiContext`
- 文案走 `ModTranslationManager`
- 测试：纯逻辑用 linked-source xUnit（`iGPU Savior.Tests/`），UI 补丁靠手动烟测

## 关键游戏内部 API（反编译情报，代码里看不出来）
- `Bulbul.SettingService` — 画质总控，`ApplyGraphicQuality()` / `ApplyRenderScale()` / `ApplyVerticalSync()`
- `Bulbul.SettingData` — 配置数据持有者，直接写 `.Value` 即可生效（自动触发 URP + FSR）
- `Bulbul.GraphicQualityLevel` — `Low` / `Medium` / `High`
- `Bulbul.RoomCameraManager` — 主相机控制；`OnStartStory()` / `OnEndStory()` 故事模式切换
- `Bulbul.HeroineService._animator` — 可调 `animator.speed` 降低更新频率
- `Bulbul.RoomGameManager` — 主状态机，`Update()` 内调度 `UpdatePlatform()` / `UpdateFacility()` / `UpdateHeroineAI()`
- URP: `GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset` 直接强转（无需反射）
- `NoteUI.Setup()` → `_selectPageListUI.Setup()` + `_currentPageUI.Setup()`
- `SelectPageListUI` 不是一个带 `Bulbul.` 前缀的 namespace — 类型在全局空间
- `SelectPageUI._dragUICanvasGroup` — 拖动点视觉，别只禁 `reorderTrigger`
- `NoteUI._facilityOpenButton` / `_noteCloseButton` — 标题栏参考元素

## UI 布局参考（运行时坐标，可用 unity-explorer 检查）
- 笔记窗口标题栏元素路径：`Paremt/PCPlatform/Canvas/UI/ChangeOrderObjects/UI_FacilityNote/...`
- 关闭按钮锚点：右上，`anchorMin=anchorMax=pivot=(1, 0.5)`，`posY` 约 -24
- 标题文本锚点：左上，`anchorMin=anchorMax=pivot=(0, 0.5)`，`posY` 约 -46.8
- 导出按钮 Y 对齐标题文本，X 对齐关闭按钮左侧

## 快捷键
- `F2` — 土豆模式
- `F3` — 无边框小窗切换
- `F4` — 相机镜像（RenderTexture + UV 翻转）
- `F5` — 竖屏优化

## MCP 工具
### ilspy-mcp（程序集反编译）
- Gamedir：`D:\SteamLibrary\steamapps\common\Chill with You Lo-Fi Story\Chill With You_Data\Managed\`
- 工具可用：`decompile_type` / `list_assembly_types` / `search_members_by_name` / `get_type_members` / `analyze_assembly` / `decompile_method` / `find_type_hierarchy`
- 注意：`NoteUI` / `SelectPageListUI` / `CurrentPageUI` 不在 `Bulbul.` 命名空间下，使用非限定名搜索

### unity-explorer（运行时 UI 检视）
- 安装：下载 UnityExplorer.BepInEx5.Mono.zip → 解压到 `BepInEx\plugins\`，进游戏按 F7
- 自带 MCP 接口。工具可用：
  - `search_elements(query, componentType?)` — 按名称/组件搜索
  - `inspect_element(path)` — 查看组件属性
  - `get_ui_hierarchy(canvasName?)` — 获取完整 UI 树
- 路径返回完整层级，（2026-05-08 版起已支持）
- `componentType: "Text"` 自动兼容 `TextMeshProUGUI`
