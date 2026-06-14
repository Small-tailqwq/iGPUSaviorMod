# MOD 设置 API 速查表

用于外部 MOD 快速接入 iGPU Savior 的共享 `MOD` 设置标签页。

## 导入

```csharp
using ModShared;
using System.Collections.Generic;
```

## 初始化

```csharp
var manager = ModSettingsManager.Instance;
if (manager?.IsInitialized == true)
{
    manager.RegisterMod("My Mod", "1.0.0");
}
```

只注册一次。不要在每帧重复调用 `Add*()`。

## 方法签名

```csharp
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

没有 parent/container 参数；UI 放置由 iGPU Savior 自动处理。

## 最小示例

```csharp
private bool registered;

private void Update()
{
    if (registered) return;

    var manager = ModSettingsManager.Instance;
    if (manager?.IsInitialized != true) return;

    manager.RegisterMod("Example Mod", "1.0.0");
    manager.RegisterTranslation("EXAMPLE_ENABLE", "Enable", "有効化", "启用");
    manager.AddToggle("EXAMPLE_ENABLE", true, value =>
    {
        // Config.Enable.Value = value;
    });

    registered = true;
}
```

## 下拉框 + 条件输入框

```csharp
manager.RegisterTranslation("EX_PROVIDER", "Provider", "プロバイダー", "服务商");
manager.RegisterTranslation("EX_A", "Provider A", "A", "服务商 A");
manager.RegisterTranslation("EX_B", "Provider B", "B", "服务商 B");
manager.RegisterTranslation("EX_A_TOKEN", "A Token", "A Token", "A Token");
manager.RegisterTranslation("EX_B_TOKEN", "B Token", "B Token", "B Token");

manager.AddDropdown("EX_PROVIDER", new List<string> { "EX_A", "EX_B" }, 0, index =>
{
    // Save provider index.
});

manager.AddInputField(
    "EX_A_TOKEN",
    "",
    value => { /* Save A token. */ },
    VisibleWhen.DropdownOption("EX_PROVIDER", "EX_A"));

manager.AddInputField(
    "EX_B_TOKEN",
    "",
    value => { /* Save B token. */ },
    VisibleWhen.DropdownOption("EX_PROVIDER", "EX_B"));
```

## 条件可见性

| 写法 | 含义 |
| --- | --- |
| `VisibleWhen.DropdownOption("KEY", "OPTION")` | `KEY` 下拉框当前选项等于 `OPTION` 时显示 |
| `VisibleWhen.DropdownIndex("KEY", 1)` | `KEY` 下拉框当前索引为 `1` 时显示 |
| `VisibleWhen.Toggle("KEY", true)` | `KEY` 开关为 `true` 时显示 |

限制：

- 控制项和被控制项必须在同一个 MOD 分组内。
- 条件目标 key 在同一 MOD 内必须唯一。
- 目标不存在、重复或类型不匹配时保持可见，并输出限流警告。
- 暂不支持条件链。

## 检查清单

- [ ] 已引用 `iGPU Savior.dll`，并 `using ModShared;`
- [ ] 等到 `Instance?.IsInitialized == true`
- [ ] 先 `RegisterMod()`，再 `RegisterTranslation()` 和 `Add*()`
- [ ] 每个设置 key 在同一 MOD 内唯一
- [ ] 下拉框选项如果用翻译 key，条件里也使用同一个 key
- [ ] 只注册一次，避免重复 UI 行
