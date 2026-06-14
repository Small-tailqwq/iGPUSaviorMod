# MOD 设置 API 使用指南

`ModSettingsManager` 是一个单例，负责把外部 MOD 的设置项加入游戏设置界面的 `MOD` 标签页。外部 MOD 只注册设置数据，UI 构建、布局、多语言刷新和条件显隐由 iGPU Savior 自动处理。

## 基本流程

```csharp
var manager = ModSettingsManager.Instance;
if (manager?.IsInitialized != true) return;

manager.RegisterMod("My Mod", "1.0.0");

manager.RegisterTranslation("MYMOD_ENABLE", "Enable Feature", "機能を有効化", "启用功能");
manager.RegisterTranslation("MYMOD_QUALITY", "Quality", "品質", "质量");
manager.RegisterTranslation("MYMOD_LOW", "Low", "低", "低");
manager.RegisterTranslation("MYMOD_HIGH", "High", "高", "高");

manager.AddToggle("MYMOD_ENABLE", true, value => SaveEnable(value));
manager.AddDropdown("MYMOD_QUALITY", new List<string> { "MYMOD_LOW", "MYMOD_HIGH" }, 1,
    index => SaveQuality(index));
manager.AddInputField("MYMOD_FPS", "60", value => SaveFps(value));
```

## 公开 API

```csharp
public static ModSettingsManager Instance { get; }
public bool IsInitialized { get; }

void RegisterMod(string modName, string modVersion);
void RegisterTranslation(string key, string en, string ja, string zh);

void AddToggle(string labelOrKey, bool defaultValue, Action<bool> onValueChanged);
void AddToggle(string labelOrKey, bool defaultValue, Action<bool> onValueChanged,
               VisibleWhenCondition visibleWhen);

void AddDropdown(string labelOrKey, List<string> options, int defaultIndex,
                 Action<int> onValueChanged);
void AddDropdown(string labelOrKey, List<string> options, int defaultIndex,
                 Action<int> onValueChanged, VisibleWhenCondition visibleWhen);

void AddInputField(string labelOrKey, string defaultValue, Action<string> onValueChanged);
void AddInputField(string labelOrKey, string defaultValue, Action<string> onValueChanged,
                   VisibleWhenCondition visibleWhen);
```

`RebuildUI(Transform contentParent, Transform settingUIRoot)` 是内部构建入口，通常由 `ModSettingsIntegration` 自动调用；外部 MOD 不应依赖内部 UI 容器。

## 条件可见性

```csharp
manager.AddToggle("MYMOD_ADVANCED", false, SaveAdvanced);

manager.AddInputField(
    "MYMOD_ADVANCED_VALUE",
    "",
    SaveAdvancedValue,
    VisibleWhen.Toggle("MYMOD_ADVANCED", true));
```

```csharp
manager.AddDropdown("MYMOD_PROVIDER", new List<string> { "OPENMETEO", "SENIVERSE" }, 0, SaveProvider);

manager.AddInputField(
    "MYMOD_LATITUDE",
    "",
    SaveLatitude,
    VisibleWhen.DropdownOption("MYMOD_PROVIDER", "OPENMETEO"));

manager.AddInputField(
    "MYMOD_CITY",
    "",
    SaveCity,
    VisibleWhen.DropdownIndex("MYMOD_PROVIDER", 1));
```

## 条件规则

- `VisibleWhen.DropdownOption(targetKey, expectedOption)`：下拉当前选项等于指定选项时显示。
- `VisibleWhen.DropdownIndex(targetKey, expectedIndex)`：下拉当前索引等于指定索引时显示。
- `VisibleWhen.Toggle(targetKey, expectedValue)`：开关当前值等于指定值时显示。

条件目标在同一个 MOD 分组内查找。目标不存在、重复或类型不匹配时采用 fail-open：设置项保持可见，并输出一次警告。

## 内部机制简述

`RebuildUI()` 会按 MOD 分组遍历注册项，使用游戏原生模板构建控件：

- Toggle：`ModToggleCloner`
- Dropdown：`ModPulldownCloner`
- InputField：`ModInputFieldCloner`

每个控件会经过 `ModSettingsStyle.PrepareRow()` 对齐，文本会通过 `ModLocalizer` 响应语言切换。

## 注意事项

- 未调用 `RegisterMod()` 直接 `Add*()` 时，设置会进入匿名 `General Settings` 分组；建议外部 MOD 显式调用。
- `RegisterTranslation()` 必须在使用对应 key 的 `Add*()` 之前调用。
- 同一 MOD 分组内 key 保持唯一，尤其是条件控制项。
- 注册逻辑只执行一次，避免重复行。
- 回调中做耗时操作时建议自行异步处理，避免卡 UI。
