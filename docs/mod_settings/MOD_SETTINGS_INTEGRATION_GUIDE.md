# MOD 设置集成指南

这份文档面向外部 MOD 作者：如何把自己的配置项接入 iGPU Savior 提供的共享 `MOD` 设置标签页。

核心原则很简单：外部 MOD 只注册设置项定义，iGPU Savior 负责克隆游戏原生 UI、布局、多语言刷新和条件显隐。

## 快速开始

### 1. 引用 iGPU Savior

在你的 `.csproj` 中引用已安装的 `iGPU Savior.dll`：

```xml
<ItemGroup>
  <Reference Include="iGPU Savior">
    <HintPath>..\BepInEx\plugins\iGPU Savior.dll</HintPath>
  </Reference>
</ItemGroup>
```

然后导入命名空间：

```csharp
using ModShared;
using System.Collections;
using System.Collections.Generic;
```

### 2. 等待设置管理器初始化

```csharp
private bool _settingsRegistered;

private void Start()
{
    StartCoroutine(RegisterSettingsWhenReady());
}

private IEnumerator RegisterSettingsWhenReady()
{
    while (ModSettingsManager.Instance?.IsInitialized != true)
        yield return null;

    if (_settingsRegistered) yield break;

    RegisterSettings(ModSettingsManager.Instance);
    _settingsRegistered = true;
}
```

### 3. 注册你的设置

```csharp
private void RegisterSettings(ModSettingsManager manager)
{
    manager.RegisterMod("My Mod", "1.0.0");

    manager.RegisterTranslation("MYMOD_ENABLE", "Enable Feature", "機能を有効化", "启用功能");
    manager.RegisterTranslation("MYMOD_MODE", "Mode", "モード", "模式");
    manager.RegisterTranslation("MYMOD_MODE_A", "Mode A", "モード A", "模式 A");
    manager.RegisterTranslation("MYMOD_MODE_B", "Mode B", "モード B", "模式 B");

    manager.AddToggle("MYMOD_ENABLE", true, enabled =>
    {
        // Config.Enable.Value = enabled;
    });

    manager.AddDropdown(
        "MYMOD_MODE",
        new List<string> { "MYMOD_MODE_A", "MYMOD_MODE_B" },
        0,
        index =>
        {
            // Config.Mode.Value = index;
        });
}
```

## 当前 API

### RegisterMod

```csharp
void RegisterMod(string modName, string modVersion);
```

注册一个 MOD 分组。后续 `AddToggle`、`AddDropdown`、`AddInputField` 都会归属到最近一次注册的 MOD 分组。

同名 MOD 再次注册会复用已有分组，后续 `Add*()` 会继续追加。

### RegisterTranslation

```csharp
void RegisterTranslation(string key, string en, string ja, string zh);
```

注册多语言文本。`labelOrKey` 和下拉框选项都支持翻译 key。未注册时会直接显示传入字符串。

### AddToggle

```csharp
void AddToggle(string labelOrKey, bool defaultValue, Action<bool> onValueChanged);
void AddToggle(string labelOrKey, bool defaultValue, Action<bool> onValueChanged,
               VisibleWhenCondition visibleWhen);
```

添加一个开关。回调参数是新的 bool 值。

### AddDropdown

```csharp
void AddDropdown(string labelOrKey, List<string> options, int defaultIndex,
                 Action<int> onValueChanged);
void AddDropdown(string labelOrKey, List<string> options, int defaultIndex,
                 Action<int> onValueChanged, VisibleWhenCondition visibleWhen);
```

添加一个下拉框。回调参数是选中项索引。`options` 中的字符串也可以是翻译 key。

### AddInputField

```csharp
void AddInputField(string labelOrKey, string defaultValue, Action<string> onValueChanged);
void AddInputField(string labelOrKey, string defaultValue, Action<string> onValueChanged,
                   VisibleWhenCondition visibleWhen);
```

添加一个单行文本输入框。回调在结束编辑时触发。

## 条件可见性

给 `Add*()` 传入 `visibleWhen` 后，该设置项会根据同一 MOD 分组内的另一个设置项实时显示或隐藏。

```csharp
private void RegisterWeatherSettings(ModSettingsManager manager)
{
    manager.RegisterMod("Weather Sync", "2.0.0");

    manager.RegisterTranslation("WEATHER_PROVIDER", "Provider", "プロバイダー", "天气源");
    manager.RegisterTranslation("WEATHER_OPENMETEO", "Open-Meteo", "Open-Meteo", "Open-Meteo");
    manager.RegisterTranslation("WEATHER_SENIVERSE", "Seniverse", "心知天气", "心知天气");
    manager.RegisterTranslation("WEATHER_LATITUDE", "Latitude", "緯度", "纬度");
    manager.RegisterTranslation("WEATHER_CITY", "City", "都市", "城市");

    manager.AddDropdown(
        "WEATHER_PROVIDER",
        new List<string> { "WEATHER_OPENMETEO", "WEATHER_SENIVERSE" },
        0,
        index => { /* SaveProvider(index); */ });

    manager.AddInputField(
        "WEATHER_LATITUDE",
        "",
        value => { /* SaveLatitude(value); */ },
        VisibleWhen.DropdownOption("WEATHER_PROVIDER", "WEATHER_OPENMETEO"));

    manager.AddInputField(
        "WEATHER_CITY",
        "",
        value => { /* SaveCity(value); */ },
        VisibleWhen.DropdownOption("WEATHER_PROVIDER", "WEATHER_SENIVERSE"));
}
```

可用条件：

| 方法 | 显示条件 |
| --- | --- |
| `VisibleWhen.DropdownOption(targetKey, expectedOption)` | 目标下拉框当前选项等于 `expectedOption` |
| `VisibleWhen.DropdownIndex(targetKey, expectedIndex)` | 目标下拉框当前索引等于 `expectedIndex` |
| `VisibleWhen.Toggle(targetKey, expectedValue)` | 目标开关当前值等于 `expectedValue` |

条件目标只在同一个 MOD 分组内查找。目标 key 不存在、重复或类型不匹配时，被控制项会保持可见，并输出一次警告。

## 完整示例

```csharp
using BepInEx;
using ModShared;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[BepInPlugin("example.weather", "Example Weather", "1.0.0")]
public class ExampleWeatherPlugin : BaseUnityPlugin
{
    private bool _settingsRegistered;

    private void Start()
    {
        StartCoroutine(RegisterSettingsWhenReady());
    }

    private IEnumerator RegisterSettingsWhenReady()
    {
        while (ModSettingsManager.Instance?.IsInitialized != true)
            yield return null;

        if (_settingsRegistered) yield break;

        RegisterSettings(ModSettingsManager.Instance);
        _settingsRegistered = true;
    }

    private void RegisterSettings(ModSettingsManager manager)
    {
        manager.RegisterMod("Example Weather", "1.0.0");

        manager.RegisterTranslation("EX_ENABLE", "Enable Sync", "同期を有効化", "启用同步");
        manager.RegisterTranslation("EX_PROVIDER", "Provider", "プロバイダー", "天气源");
        manager.RegisterTranslation("EX_OPENMETEO", "Open-Meteo", "Open-Meteo", "Open-Meteo");
        manager.RegisterTranslation("EX_SENIVERSE", "Seniverse", "心知天气", "心知天气");
        manager.RegisterTranslation("EX_LATITUDE", "Latitude", "緯度", "纬度");
        manager.RegisterTranslation("EX_CITY", "City", "都市", "城市");

        manager.AddToggle("EX_ENABLE", true, enabled =>
        {
            Logger.LogInfo($"Sync enabled: {enabled}");
        });

        manager.AddDropdown(
            "EX_PROVIDER",
            new List<string> { "EX_OPENMETEO", "EX_SENIVERSE" },
            0,
            index => Logger.LogInfo($"Provider index: {index}"));

        manager.AddInputField(
            "EX_LATITUDE",
            "35.6812",
            value => Logger.LogInfo($"Latitude: {value}"),
            VisibleWhen.DropdownOption("EX_PROVIDER", "EX_OPENMETEO"));

        manager.AddInputField(
            "EX_CITY",
            "Tokyo",
            value => Logger.LogInfo($"City: {value}"),
            VisibleWhen.DropdownOption("EX_PROVIDER", "EX_SENIVERSE"));
    }
}
```

## 最佳实践

- 使用带 MOD 前缀的 key，例如 `WEATHER_PROVIDER`，避免和其他 MOD 冲突。
- `RegisterTranslation()` 放在对应 `Add*()` 之前。
- 保存用户配置时，优先使用 BepInEx `ConfigEntry`。
- 默认值应来自当前配置，而不是硬编码。
- 不要重复注册；用 `_settingsRegistered` 之类的标记保护。
- 不要手动调用或依赖内部 UI 容器，公开 API 没有 parent 参数。
- 条件目标 key 在同一 MOD 内保持唯一。
- 不要依赖条件链；需要复杂联动时先拆成单层控制。

## 常见问题

### 为什么找不到 `ModContentParent`？

当前版本不再暴露 `ModContentParent`。旧文档里的 parent 参数已经过时。外部 MOD 只需要调用无 parent 参数的 `AddToggle`、`AddDropdown`、`AddInputField`。

### 设置项为什么重复出现？

通常是重复注册了。确认你的初始化逻辑只执行一次。

### 条件项为什么没有隐藏？

检查三件事：

1. `TargetKey` 是否和控制项的 `labelOrKey` 完全一致。
2. 下拉框选项如果使用翻译 key，`DropdownOption` 也要传同一个 key。
3. 控制项和被控制项是否在同一个 `RegisterMod()` 分组内。

### iGPU Savior 未安装时怎么办？

如果直接引用 DLL，BepInEx 会要求依赖存在。若想做可选依赖，需要改用反射调用，并在找不到 `ModShared.ModSettingsManager` 时跳过设置页接入。

## 调试建议

- 查看 BepInEx 日志中 `[ModManager] Mod 注册` 和 `[ModSettings] 条件可见性错误`。
- 先用一个 Toggle 和一个 Dropdown 做最小接入，确认显示后再加复杂逻辑。
- 条件可见性建议先用 `DropdownIndex` 验证，再换成翻译 key 方案。
