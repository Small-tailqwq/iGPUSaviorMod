# MOD 设置系统概述

## 它是什么

iGPU Savior 在游戏原生设置界面中注入了"MOD"标签页。这个标签页由一个共享的 `ModSettingsManager` 单例管理，允许 iGPU Savior 自身及任何外部 MOD 注册设置项（开关、下拉框、文本输入），系统自动克隆游戏原生 UI 控件来构建界面。

## 怎么工作的

1. **Harmony 注入** — `ModSettingsIntegration` patch 了 `SettingUI.Setup()`，克隆 Credits 标签页来创建 MOD 标签页
2. **标签切换** — 点击其他标签时自动隐藏 MOD 内容面板，点击 MOD 标签时调用 `RebuildUI()` 重建
3. **UI 克隆** — 不从零绘制 UI，而是从游戏场景中 Instantiate 原生控件模板（如 `GraphicQualityPulldownList`），清空子项后填入自定义数据和回调
4. **布局强制** — `EnforceLayout()` 统一处理锚点对齐、标签宽度、最小行高
5. **多语言** — `ModLocalizer` 组件监听语言切换事件，自动更新 TMP_Text 内容和字体

## 关键文件

| 文件 | 职责 |
|------|------|
| `UI/ModSettingsIntegration.cs` | Harmony 注入、标签创建、标签切换逻辑 |
| `UI/ModSettingsManager.cs` | 单例 API、设置项注册、UI 构建编排 |
| `UI/ModToggleCloner.cs` | 克隆 ON/OFF 开关模板 |
| `UI/ModPulldownCloner.cs` | 克隆下拉框模板 + 动态 ScrollRect |
| `UI/ModInputFieldCloner.cs` | 克隆文本输入框模板 |
| `UI/Localization/ModLocalizer.cs` | per-GameObject 多语言自动更新 |
| `UI/Localization/ModTranslationManager.cs` | EN/JA/ZH 翻译字典 |
| `Configuration/ConfigurationManager.cs` | BepInEx 持久化配置 |
| `Core/Constants.cs` | UI 路径与尺寸常量 |

## API 入口

```csharp
ModSettingsManager.Instance.RegisterMod("My Mod", "1.0.0");
ModSettingsManager.Instance.AddToggle("Enable Feature", true, val => ...);
ModSettingsManager.Instance.AddDropdown("Quality", opts, 0, idx => ...);
ModSettingsManager.Instance.AddInputField("FPS", "60", val => ...);
// RebuildUI 由 ModSettingsIntegration 自动调用
```

详见 [MOD_SETTINGS_USAGE.md](./mod_settings/MOD_SETTINGS_USAGE.md)。
