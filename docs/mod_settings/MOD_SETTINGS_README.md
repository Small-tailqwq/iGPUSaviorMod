# MOD 设置集成文档中心

这是 iGPU Savior 为外部 MOD 提供的共享设置页接入文档。当前公开 API 是“注册设置项定义”，不需要也不能传入 UI parent/container。

## 从这里开始

| 目标 | 文档 |
| --- | --- |
| 第一次接入 | [MOD 设置集成指南](MOD_SETTINGS_INTEGRATION_GUIDE.md) |
| 快速查方法签名 | [API 速查表](MOD_SETTINGS_API_QUICK_REFERENCE.md) |
| 排查初始化/重复注册/布局问题 | [常见陷阱 & 最佳实践](MOD_SETTINGS_PITFALLS_AND_BEST_PRACTICES.md) |
| 理解内部实现 | [系统架构 & 流程图](MOD_SETTINGS_ARCHITECTURE.md) |

## 最小示例

```csharp
using ModShared;

private bool _settingsRegistered;

private void Update()
{
    if (_settingsRegistered) return;

    var manager = ModSettingsManager.Instance;
    if (manager?.IsInitialized != true) return;

    manager.RegisterMod("My Mod", "1.0.0");
    manager.RegisterTranslation("MYMOD_ENABLE", "Enable", "有効化", "启用");
    manager.AddToggle("MYMOD_ENABLE", true, enabled =>
    {
        // Save enabled.
    });

    _settingsRegistered = true;
}
```

## 常见任务

### 添加一个开关

```csharp
manager.AddToggle("MYMOD_ENABLE", savedValue, enabled => SaveEnabled(enabled));
```

### 添加一个下拉菜单

```csharp
manager.AddDropdown(
    "MYMOD_MODE",
    new List<string> { "MYMOD_MODE_A", "MYMOD_MODE_B" },
    defaultIndex,
    index => SaveMode(index));
```

### 添加条件可见输入框

```csharp
manager.AddDropdown("MYMOD_PROVIDER", new List<string> { "A", "B" }, 0, SaveProvider);

manager.AddInputField(
    "MYMOD_A_TOKEN",
    "",
    SaveAToken,
    VisibleWhen.DropdownOption("MYMOD_PROVIDER", "A"));

manager.AddInputField(
    "MYMOD_B_TOKEN",
    "",
    SaveBToken,
    VisibleWhen.DropdownOption("MYMOD_PROVIDER", "B"));
```

## 当前能力

- `RegisterMod()`：注册一个 MOD 分组。
- `RegisterTranslation()`：注册 EN/JA/ZH 文案。
- `AddToggle()`：添加开关。
- `AddDropdown()`：添加下拉框。
- `AddInputField()`：添加单行输入框。
- `VisibleWhen.*`：声明条件可见性。

## 重要注意

- 等到 `ModSettingsManager.Instance?.IsInitialized == true` 后再注册。
- 每个 MOD 只注册一次，避免重复 UI 行。
- 先 `RegisterMod()`，再注册翻译和设置项。
- 不要使用旧文档里的 `ModContentParent` 或 parent 参数；这些不是当前公开 API。
- 条件目标 key 必须在同一 MOD 分组内唯一。

最后更新：2026-06-14
