# iGPU Savior — Agent 参考

## 项目与验证

BepInEx 5 插件，为《Chill with You: Lo-Fi Story》提供性能优化、窗口/PiP、镜像、
竖屏、笔记导出和共享 MOD 设置 UI。主项目目标为 .NET Framework 4.7.2；
纯逻辑 xUnit 测试目标为 net8.0。仓库包含 `iGPU Savior.slnx`。

```powershell
dotnet build "iGPU Savior/iGPU Savior.csproj" -c Release
dotnet test "iGPU Savior.Tests/iGPU Savior.Tests.csproj"
pwsh -File scripts/sync-version.ps1 -CheckOnly
```

主项目通过 `$(GameDir)` 引用本地游戏程序集；默认路径不可用时为构建命令添加
`-p:GameDir="<游戏目录>"`。测试项目不依赖 Unity 或游戏安装。

## 架构边界

- `Core/PotatoPlugin.cs`：入口、配置初始化、Harmony 注册和 Controller 创建。
- `Core/PotatoController.cs`：协调 RenderQuality、CameraMirror、PortraitMode、
  WindowState、PiPGui 和 Audio manager。
- `Configuration/`：BepInEx 配置；`Features/`：独立功能；`Utilities/`：Win32 和纯逻辑工具。
- `Patches/`：游戏行为补丁及笔记导出/删除确认；`UI/`：共享 MOD 设置系统和原生控件克隆。
- 游戏程序集通过 build-only Assembly Publicizer 暴露内部成员；已有强类型 API 时不要重新引入反射。

## 关键不变量

- `Harmony.PatchAll()` 只能发现已加载的补丁类型。新增补丁时，在
  `PotatoPlugin.ApplyHarmonyPatches()` 中用 `typeof(...)` 强制加载后再调用 `PatchAll()`；
  不要在文档中记录易漂移的补丁数量。
- 后台可见性判定保持分层：`WindowVisibilityChecker` 收集 Win32 信号，
  `VisibilityDecider` 只做可单测的纯逻辑决策。
- 笔记导出状态和 UI 注入集中在 `Features/NoteExport*`、`Patches/NoteExport*`；
  修改选择模式时同时验证标题/正文输入、拖拽、删除和按钮状态恢复。
- `ModSettingsManager` 是 `ModShared` 命名空间下的共享入口，提供
  `RegisterMod`、`AddToggle`、`AddDropdown`、`AddInputField`；条件显示使用 `VisibleWhen`。
- 游戏服务通过 `NestopiSystem.DIContainers.ProjectLifetimeScope.Resolve<T>()` 获取。

## 版本与提交

- `version.json` 是版本单一事实源。发布或同步版本前先让用户确认目标版本，再运行
  `scripts/sync-version.ps1 -Version <版本>`；提交前运行 `-CheckOnly`。
- 提交信息使用中文，格式 `<类型>：<描述>`；常用类型为
  `功能`、`修复`、`重构`、`文档`、`测试`、`维护`。
- 格式与换行遵循 `.editorconfig` 和 `.gitattributes`；不要在功能提交中夹带整文件格式化。
