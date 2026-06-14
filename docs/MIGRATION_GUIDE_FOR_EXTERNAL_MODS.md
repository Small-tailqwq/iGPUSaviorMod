# 外部 MOD 接入 iGPU Savior 设置页指南

`ModSettingsManager` 会把多个 MOD 的配置项统一放进游戏设置界面的 `MOD` 标签页。外部 MOD 只需要注册“数据定义”，不需要自己创建 Unity UI，也不需要传入 parent/container。

## 依赖与命名空间

推荐在你的 MOD 工程里引用 `iGPU Savior.dll`，然后直接使用 `ModShared` 命名空间：

```xml
<ItemGroup>
  <Reference Include="iGPU Savior">
    <HintPath>path\to\BepInEx\plugins\iGPU Savior.dll</HintPath>
  </Reference>
</ItemGroup>
```

```csharp
using ModShared;
```

如果希望 iGPU Savior 是可选依赖，也可以用反射调用；但直接引用最简单，也能获得 `VisibleWhen` 的编译期检查。

## 最小接入流程

```csharp
private bool _settingsRegistered;

private void Update()
{
    if (_settingsRegistered) return;

    var manager = ModSettingsManager.Instance;
    if (manager?.IsInitialized != true) return;

    RegisterModSettings(manager);
    _settingsRegistered = true;
}

private void RegisterModSettings(ModSettingsManager manager)
{
    manager.RegisterMod("My Weather Mod", "1.0.0");

    manager.RegisterTranslation("MYMOD_ENABLE", "Enable", "有効化", "启用");
    manager.RegisterTranslation("MYMOD_PROVIDER", "Provider", "プロバイダー", "天气源");
    manager.RegisterTranslation("MYMOD_PROVIDER_OPENMETEO", "Open-Meteo", "Open-Meteo", "Open-Meteo");
    manager.RegisterTranslation("MYMOD_PROVIDER_SENIVERSE", "Seniverse", "心知天气", "心知天气");
    manager.RegisterTranslation("MYMOD_LATITUDE", "Latitude", "緯度", "纬度");
    manager.RegisterTranslation("MYMOD_CITY", "City", "都市", "城市");

    manager.AddToggle("MYMOD_ENABLE", true, enabled =>
    {
        // SaveConfig(enabled);
    });

    var providers = new List<string>
    {
        "MYMOD_PROVIDER_OPENMETEO",
        "MYMOD_PROVIDER_SENIVERSE"
    };

    manager.AddDropdown("MYMOD_PROVIDER", providers, 0, index =>
    {
        // SaveProvider(index);
    });

    manager.AddInputField(
        "MYMOD_LATITUDE",
        "35.6812",
        value => { /* SaveLatitude(value); */ },
        VisibleWhen.DropdownOption("MYMOD_PROVIDER", "MYMOD_PROVIDER_OPENMETEO"));

    manager.AddInputField(
        "MYMOD_CITY",
        "Tokyo",
        value => { /* SaveCity(value); */ },
        VisibleWhen.DropdownOption("MYMOD_PROVIDER", "MYMOD_PROVIDER_SENIVERSE"));
}
```

## 当前公开 API

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

注意：公开 API 不包含 `ModContentParent`、`ModTabButton`、`ModScrollRect` 或任何 parent 参数。UI 容器由 iGPU Savior 内部管理。

## 条件可见性

条件可见性通过最后一个可选参数 `visibleWhen` 声明。控制项和被控制项必须属于同一个 `RegisterMod()` 分组，且 `TargetKey` 必须唯一。

```csharp
manager.AddDropdown("MODE", new List<string> { "A", "B" }, 0, OnModeChanged);

manager.AddInputField(
    "A_VALUE",
    "",
    OnAChanged,
    VisibleWhen.DropdownOption("MODE", "A"));

manager.AddInputField(
    "B_VALUE",
    "",
    OnBChanged,
    VisibleWhen.DropdownIndex("MODE", 1));

manager.AddToggle(
    "ADVANCED_ONLY",
    false,
    OnAdvancedChanged,
    VisibleWhen.Toggle("ENABLE_ADVANCED", true));
```

可用条件：

| 工厂方法 | 说明 |
| --- | --- |
| `VisibleWhen.DropdownOption(targetKey, expectedOption)` | 下拉框当前选项文本或翻译 key 等于 `expectedOption` 时显示 |
| `VisibleWhen.DropdownIndex(targetKey, expectedIndex)` | 下拉框当前索引等于 `expectedIndex` 时显示 |
| `VisibleWhen.Toggle(targetKey, expectedValue)` | 开关值等于 `expectedValue` 时显示 |

配置错误采用 fail-open 策略：目标不存在、目标 key 重复、条件类型不匹配时，被控制项保持可见，并在日志中对同一问题只警告一次。

## 多语言建议

`labelOrKey` 和下拉框 `options` 都可以是翻译 key。建议第三方 MOD 使用带命名空间前缀的 key，避免和其他 MOD 冲突：

```csharp
manager.RegisterTranslation("MYMOD_ENABLE_SYNC", "Enable Sync", "同期を有効化", "启用同步");
manager.AddToggle("MYMOD_ENABLE_SYNC", true, OnEnableChanged);
```

## 接入注意事项

- 等到 `ModSettingsManager.Instance?.IsInitialized == true` 后再注册。
- 每个 MOD 只注册一次设置项；重复注册会追加重复 UI 行。
- `RegisterMod()` 要在所有 `Add*()` 调用前执行。
- `RegisterTranslation()` 要在使用对应 key 的 `Add*()` 调用前执行。
- 下拉框回调参数是索引；输入框回调在结束编辑时触发。
- 条件链暂不支持：一个设置项既被条件隐藏，又作为另一个条件的控制项时会输出警告。
- 已经打开设置页后动态增删设置项不是常规路径；建议在插件初始化阶段完成注册。

## 相关源码

- `iGPU Savior/UI/ModSettingsManager.cs`：公开 API 与条件刷新逻辑
- `iGPU Savior/UI/VisibleWhen.cs`：条件工厂
- `iGPU Savior/UI/ModSettingsIntegration.cs`：游戏设置页注入与 UI 构建入口
