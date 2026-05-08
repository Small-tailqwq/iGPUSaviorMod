# MOD 设置系统文档

## 核心流程

1. `ModSettingsIntegration` Harmony Patch → SettingUI.Setup()
2. 克隆 Credits 标签页 → 创建 MOD 标签页
3. `ModSettingsManager.RegisterMod()` + `AddToggle/Dropdown/InputField()` → 注册设置项
4. `RebuildUI()` → 用 Cloner 组件克隆游戏原生 UI 模板，构建完整 MOD 设置界面
5. 每个设置自动获得 `EnforceLayout()` 对齐和 `ModLocalizer` 多语言支持

## 源码速查

| 文件 | 用途 |
|------|------|
| `iGPU Savior/UI/ModSettingsIntegration.cs` | Harmony 注入，创建 MOD 标签，管理标签切换 |
| `iGPU Savior/UI/ModSettingsManager.cs` | 单例管理器，API 入口，UI 构建编排 |
| `iGPU Savior/UI/ModToggleCloner.cs` | 克隆游戏原生 ON/OFF 开关 |
| `iGPU Savior/UI/ModPulldownCloner.cs` | 克隆游戏原生下拉框（含动态 ScrollRect） |
| `iGPU Savior/UI/ModInputFieldCloner.cs` | 克隆游戏原生文本输入框 |
| `iGPU Savior/UI/Localization/ModLocalizer.cs` | 多语言文本+字体自动切换 |
| `iGPU Savior/UI/Localization/ModTranslationManager.cs` | 静态翻译字典 (EN/JA/ZH) |
| `iGPU Savior/Configuration/ConfigurationManager.cs` | BepInEx ConfigEntry 定义 |
| `iGPU Savior/Core/Constants.cs` | UI 路径常量、下拉框尺寸限制 |

## 文档索引

### API 使用

- [MOD_SETTINGS_USAGE.md](./mod_settings/MOD_SETTINGS_USAGE.md) — `RegisterMod` / `AddToggle` / `AddDropdown` / `AddInputField` / `RegisterTranslation` / `RebuildUI` 完整 API 参考
- [MIGRATION_GUIDE_FOR_EXTERNAL_MODS.md](./MIGRATION_GUIDE_FOR_EXTERNAL_MODS.md) — 外部 MOD 如何接入这个 API（旧文档，仅供参考）

### 架构与技术细节

- [MOD_SETTINGS_SYSTEM_SUMMARY.md](./mod_settings/MOD_SETTINGS_SYSTEM_SUMMARY.md) — 完整架构：Harmony 注入、Cloner 机制、布局强制对齐、多语言流程
- [MOD_SETTINGS_STYLE_REFERENCE.md](./mod_settings/MOD_SETTINGS_STYLE_REFERENCE.md) — UI 布局常量（380px 标签宽度、60px 最小行高等）

### 其他

- [QUICK_SUMMARY.md](./QUICK_SUMMARY.md) — 系统概述
- [FAQ.md](./FAQ.md) — 常见问题
- [PULLDOWN_CLONER_USAGE.md](./PULLDOWN_CLONER_USAGE.md) — 下拉框 Cloner 独立使用指南
- [PORTRAIT_MODE_FEATURE.md](./PORTRAIT_MODE_FEATURE.md) — 竖屏模式功能说明
- [NATIVE_DROPDOWN_IMPROVEMENTS.md](./NATIVE_DROPDOWN_IMPROVEMENTS.md) — 原生下拉框改进记录
- [EXTERNAL_MOD_STYLE_IMPROVEMENT_GUIDE.md](./EXTERNAL_MOD_STYLE_IMPROVEMENT_GUIDE.md) — 外部 MOD 样式改进指南（旧）
- [MOD_EXTERNAL_INTEGRATION_ANALYSIS.md](./MOD_EXTERNAL_INTEGRATION_ANALYSIS.md) — 外部 MOD 集成分析（旧）
- [001-251203.md](./001-251203.md) — 项目技术交接备忘录

### 子目录

- `mod_settings/` — MOD 设置系统遗留文档（API、样式、排错）
- `superpowers/specs/` — 功能设计文档
- `superpowers/plans/` — 实现计划
- `mcp/unity-explorer/` — UnityExplorer MCP 工具说明
- `refactoring/` — 代码重构记录
