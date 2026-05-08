# 外部 MOD 接入指南

## 概述

`ModSettingsManager` 是单例，外部 MOD 通过它往共享 MOD 标签页注册设置项。所有 UI 由系统自动构建，无需手动创建控件。

## 注册流程

```csharp
// 1. 注册 MOD
ModSettingsManager.Instance.RegisterMod("My Mod", "1.0.0");

// 2. (可选) 注册翻译
ModSettingsManager.Instance.RegisterTranslation("KEY", "EN", "JA", "ZH");

// 3. 添加设置项
ModSettingsManager.Instance.AddToggle("Enable Feature", true, val => ...);
ModSettingsManager.Instance.AddDropdown("Quality", opts, 0, idx => ...);
ModSettingsManager.Instance.AddInputField("FPS", "60", val => ...);

// 4. RebuildUI 由 ModSettingsIntegration 在标签切换时自动调用，无需手动触发
```

## API 签名（当前版本）

```csharp
void RegisterMod(string modName, string modVersion);
void RegisterTranslation(string key, string en, string ja, string zh);
void AddToggle(string labelOrKey, bool defaultValue, Action<bool> onValueChanged);
void AddDropdown(string labelOrKey, List<string> options, int defaultIndex, Action<int> onValueChanged);
void AddInputField(string labelText, string defaultValue, Action<string> onValueChanged);
```

注意：API 不包含 parent/container 参数。控件由系统自动放入 MOD 内容面板。

## 兼容性检查

```csharp
if (ModSettingsManager.Instance != null && ModSettingsManager.Instance.IsInitialized)
{
    ModSettingsManager.Instance.RegisterMod("My Mod", "1.0");
    // ...
}
```

## 注意事项

- `RegisterMod` 必须在 `AddToggle/Dropdown/InputField` 之前调用。未调用时系统自动归入 "General Settings" 匿名组（不显示标题）
- `RegisterTranslation` 必须在相关 `Add*` 调用之前执行，否则翻译不会生效
- 同名 MOD 重复 `RegisterMod` 不会覆盖已有条目，后续 Add 调用会追加到该条目下
- 已在当前标签页时动态增删设置项，需要手动调用 `RebuildUI()`

## 相关源码

- `ModSettingsManager.cs` — API 定义和实现
- `ModTranslationManager.cs` — 翻译字典
