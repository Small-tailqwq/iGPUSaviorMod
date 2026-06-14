# ModSettingsManager 条件可见性实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为 `ModShared.ModSettingsManager` 增加声明式条件可见性，使外部 MOD 可以用 `VisibleWhen.DropdownOption(...)` 等条件控制配置项显隐。

**Architecture:** 新增 `VisibleWhen` 公开工厂、`VisibleWhenCondition` 抽象条件、无 Unity 依赖的 `VisibleWhenEvaluator` 求值器；`ModSettingsManager` 记录每个配置项当前值、所属 MOD 和条件，构建 UI 后缓存行对象，控制器值变化时扫描同 MOD 依赖项并刷新可见性与布局。

**Tech Stack:** C# (.NET Framework 4.7.2 / .NET 8 test runner), UnityEngine.UI, Harmony, xUnit, MSBuild.

---

## 约定

- 所有新文件路径均位于 `iGPU Savior/UI/`（主工程）或 `iGPU Savior.Tests/`（测试工程）。
- 主工程 csproj 中显式列出 `<Compile Include="...">`；测试工程中使用 `<Compile Include="..\iGPU Savior\UI\..." Link="...">` 链接可在 xUnit 下编译的无 Unity 依赖源码。
- 代码以大改小为原则；不修改现有 `Add*` 方法签名。

---

## Task 1: 创建公开条件 API

**Files:**
- Create: `iGPU Savior/UI/VisibleWhen.cs`
- Modify: `iGPU Savior/iGPU Savior.csproj`
- Test: `iGPU Savior.Tests/VisibleWhenFactoryTests.cs`
- Modify: `iGPU Savior.Tests/iGPU Savior.Tests.csproj`

- [ ] **Step 1: 创建 `VisibleWhen.cs`**

```csharp
using System;

namespace ModShared
{
    public abstract class VisibleWhenCondition
    {
        public string TargetKey { get; }

        internal VisibleWhenCondition(string targetKey)
        {
            if (string.IsNullOrEmpty(targetKey))
                throw new ArgumentException("targetKey cannot be null or empty", nameof(targetKey));
            TargetKey = targetKey;
        }
    }

    public static class VisibleWhen
    {
        public static VisibleWhenCondition DropdownOption(string targetKey, string expectedOption)
        {
            if (expectedOption == null)
                throw new ArgumentNullException(nameof(expectedOption));
            return new DropdownOptionCondition(targetKey, expectedOption);
        }

        public static VisibleWhenCondition DropdownIndex(string targetKey, int expectedIndex)
            => new DropdownIndexCondition(targetKey, expectedIndex);

        public static VisibleWhenCondition Toggle(string targetKey, bool expectedValue)
            => new ToggleCondition(targetKey, expectedValue);
    }

    internal sealed class DropdownOptionCondition : VisibleWhenCondition
    {
        public string ExpectedOption { get; }
        internal DropdownOptionCondition(string targetKey, string expectedOption) : base(targetKey)
            => ExpectedOption = expectedOption;
    }

    internal sealed class DropdownIndexCondition : VisibleWhenCondition
    {
        public int ExpectedIndex { get; }
        internal DropdownIndexCondition(string targetKey, int expectedIndex) : base(targetKey)
            => ExpectedIndex = expectedIndex;
    }

    internal sealed class ToggleCondition : VisibleWhenCondition
    {
        public bool ExpectedValue { get; }
        internal ToggleCondition(string targetKey, bool expectedValue) : base(targetKey)
            => ExpectedValue = expectedValue;
    }
}
```

- [ ] **Step 2: 添加到主 csproj**

在 `iGPU Savior/iGPU Savior.csproj` 的 `<!-- UI -->` 分组中新增一行：

```xml
<Compile Include="UI\VisibleWhen.cs" />
```

- [ ] **Step 3: 编写工厂测试（先写会失败的版本，验证 API 形状）**

创建 `iGPU Savior.Tests/VisibleWhenFactoryTests.cs`：

```csharp
using System;
using ModShared;
using Xunit;

namespace IGPUSavior.Tests
{
    public class VisibleWhenFactoryTests
    {
        [Fact]
        public void DropdownOption_PreservesTargetAndExpectedOption()
        {
            var cond = VisibleWhen.DropdownOption("Provider", "OpenMeteo");
            Assert.Equal("Provider", cond.TargetKey);
            var typed = Assert.IsType<DropdownOptionCondition>(cond);
            Assert.Equal("OpenMeteo", typed.ExpectedOption);
        }

        [Fact]
        public void DropdownIndex_PreservesTargetAndExpectedIndex()
        {
            var cond = VisibleWhen.DropdownIndex("Provider", 2);
            Assert.Equal("Provider", cond.TargetKey);
            var typed = Assert.IsType<DropdownIndexCondition>(cond);
            Assert.Equal(2, typed.ExpectedIndex);
        }

        [Fact]
        public void Toggle_PreservesTargetAndExpectedValue()
        {
            var cond = VisibleWhen.Toggle("EnableAdvanced", true);
            Assert.Equal("EnableAdvanced", cond.TargetKey);
            var typed = Assert.IsType<ToggleCondition>(cond);
            Assert.True(typed.ExpectedValue);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void DropdownOption_RejectsEmptyTargetKey(string targetKey)
        {
            Assert.Throws<ArgumentException>(() => VisibleWhen.DropdownOption(targetKey, "OpenMeteo"));
        }

        [Fact]
        public void DropdownOption_RejectsNullExpectedOption()
        {
            Assert.Throws<ArgumentNullException>(() => VisibleWhen.DropdownOption("Provider", null));
        }
    }
}
```

> 注意：`VisibleWhen.DropdownOptionCondition` 等类型在 `VisibleWhen.cs` 中设为 `internal sealed`。由于测试工程链接该文件编译，测试程序集可见到这些 internal 类型。

- [ ] **Step 4: 添加到测试 csproj**

在 `iGPU Savior.Tests/iGPU Savior.Tests.csproj` 的 `ItemGroup` 中新增：

```xml
<Compile Include="VisibleWhenFactoryTests.cs" />
<Compile Include="..\iGPU Savior\UI\VisibleWhen.cs" Link="VisibleWhen.cs" />
```

- [ ] **Step 5: 运行测试**

Run:

```powershell
dotnet test "iGPU Savior.Tests\iGPU Savior.Tests.csproj" --filter "FullyQualifiedName~VisibleWhenFactoryTests"
```

Expected: 5 passed.

- [ ] **Step 6: Commit**

```bash
git add "iGPU Savior/UI/VisibleWhen.cs"
git add "iGPU Savior/iGPU Savior.csproj"
git add "iGPU Savior.Tests/VisibleWhenFactoryTests.cs"
git add "iGPU Savior.Tests/iGPU Savior.Tests.csproj"
git commit -m "feat(modshared): 新增 VisibleWhen 条件 API 与工厂测试"
```

---

## Task 2: 创建无 Unity 依赖的条件求值器

**Files:**
- Create: `iGPU Savior/UI/VisibleWhenEvaluator.cs`
- Modify: `iGPU Savior/iGPU Savior.csproj`
- Test: `iGPU Savior.Tests/VisibleWhenEvaluatorTests.cs`
- Modify: `iGPU Savior.Tests/iGPU Savior.Tests.csproj`

- [ ] **Step 1: 创建求值器骨架（先让测试编译但失败）**

创建 `iGPU Savior/UI/VisibleWhenEvaluator.cs`：

```csharp
using System;
using System.Collections.Generic;

namespace ModShared
{
    internal enum SettingValueKind
    {
        Toggle,
        Dropdown
    }

    internal sealed class SettingValueSnapshot
    {
        public SettingValueKind Kind;
        public bool ToggleValue;
        public int DropdownIndex;
        public IReadOnlyList<string> DropdownOptions;
    }

    internal static class VisibleWhenEvaluator
    {
        internal static bool Evaluate(
            VisibleWhenCondition condition,
            SettingValueSnapshot controller,
            out string failureReason)
        {
            throw new NotImplementedException();
        }
    }
}
```

- [ ] **Step 2: 添加到主 csproj**

在 `iGPU Savior/iGPU Savior.csproj` 的 `<!-- UI -->` 分组中新增：

```xml
<Compile Include="UI\VisibleWhenEvaluator.cs" />
```

- [ ] **Step 3: 编写求值器测试**

创建 `iGPU Savior.Tests/VisibleWhenEvaluatorTests.cs`：

```csharp
using System;
using System.Collections.Generic;
using ModShared;
using Xunit;

namespace IGPUSavior.Tests
{
    public class VisibleWhenEvaluatorTests
    {
        [Fact]
        public void DropdownOption_MatchingOption_ReturnsTrue()
        {
            var cond = VisibleWhen.DropdownOption("Provider", "OpenMeteo");
            var snapshot = Snapshot(
                SettingValueKind.Dropdown,
                options: new[] { "Seniverse", "OpenMeteo" },
                index: 1);

            bool result = VisibleWhenEvaluator.Evaluate(cond, snapshot, out string reason);

            Assert.True(result);
            Assert.Null(reason);
        }

        [Fact]
        public void DropdownOption_NonMatchingOption_ReturnsFalse()
        {
            var cond = VisibleWhen.DropdownOption("Provider", "Seniverse");
            var snapshot = Snapshot(
                SettingValueKind.Dropdown,
                options: new[] { "Seniverse", "OpenMeteo" },
                index: 1);

            bool result = VisibleWhenEvaluator.Evaluate(cond, snapshot, out string reason);

            Assert.False(result);
            Assert.Null(reason);
        }

        [Fact]
        public void DropdownIndex_MatchingIndex_ReturnsTrue()
        {
            var cond = VisibleWhen.DropdownIndex("Provider", 0);
            var snapshot = Snapshot(
                SettingValueKind.Dropdown,
                options: new[] { "Seniverse", "OpenMeteo" },
                index: 0);

            bool result = VisibleWhenEvaluator.Evaluate(cond, snapshot, out string reason);

            Assert.True(result);
            Assert.Null(reason);
        }

        [Fact]
        public void Toggle_MatchingValue_ReturnsTrue()
        {
            var cond = VisibleWhen.Toggle("EnableAdvanced", true);
            var snapshot = Snapshot(SettingValueKind.Toggle, toggleValue: true);

            bool result = VisibleWhenEvaluator.Evaluate(cond, snapshot, out string reason);

            Assert.True(result);
            Assert.Null(reason);
        }

        [Fact]
        public void MissingController_FailsOpenWithReason()
        {
            var cond = VisibleWhen.Toggle("EnableAdvanced", true);

            bool result = VisibleWhenEvaluator.Evaluate(cond, null, out string reason);

            Assert.True(result);
            Assert.Equal("Controller snapshot is null", reason);
        }

        [Fact]
        public void TypeMismatch_FailsOpenWithReason()
        {
            var cond = VisibleWhen.Toggle("Provider", true);
            var snapshot = Snapshot(
                SettingValueKind.Dropdown,
                options: new[] { "A", "B" },
                index: 0);

            bool result = VisibleWhenEvaluator.Evaluate(cond, snapshot, out string reason);

            Assert.True(result);
            Assert.Contains("Expected toggle controller", reason);
        }

        [Fact]
        public void DropdownEmptyOptions_FailsOpenWithReason()
        {
            var cond = VisibleWhen.DropdownOption("Provider", "A");
            var snapshot = Snapshot(
                SettingValueKind.Dropdown,
                options: Array.Empty<string>(),
                index: 0);

            bool result = VisibleWhenEvaluator.Evaluate(cond, snapshot, out string reason);

            Assert.True(result);
            Assert.Contains("empty", reason, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void DropdownIndexOutOfRange_FailsOpenWithReason()
        {
            var cond = VisibleWhen.DropdownIndex("Provider", 5);
            var snapshot = Snapshot(
                SettingValueKind.Dropdown,
                options: new[] { "A", "B" },
                index: 5);

            bool result = VisibleWhenEvaluator.Evaluate(cond, snapshot, out string reason);

            Assert.True(result);
            Assert.Contains("out of range", reason, StringComparison.OrdinalIgnoreCase);
        }

        private static SettingValueSnapshot Snapshot(
            SettingValueKind kind,
            IReadOnlyList<string> options = null,
            int index = 0,
            bool toggleValue = false)
        {
            return new SettingValueSnapshot
            {
                Kind = kind,
                DropdownOptions = options,
                DropdownIndex = index,
                ToggleValue = toggleValue
            };
        }
    }
}
```

- [ ] **Step 4: 链接到测试 csproj**

在 `iGPU Savior.Tests/iGPU Savior.Tests.csproj` 的 `ItemGroup` 中新增：

```xml
<Compile Include="VisibleWhenEvaluatorTests.cs" />
<Compile Include="..\iGPU Savior\UI\VisibleWhenEvaluator.cs" Link="VisibleWhenEvaluator.cs" />
```

- [ ] **Step 5: 运行测试，确认失败**

Run:

```powershell
dotnet test "iGPU Savior.Tests\iGPU Savior.Tests.csproj" --filter "FullyQualifiedName~VisibleWhenEvaluatorTests"
```

Expected: tests fail with `NotImplementedException`.

- [ ] **Step 6: 实现求值器完整逻辑**

替换 `iGPU Savior/UI/VisibleWhenEvaluator.cs` 内容：

```csharp
using System;
using System.Collections.Generic;

namespace ModShared
{
    internal enum SettingValueKind
    {
        Toggle,
        Dropdown
    }

    internal sealed class SettingValueSnapshot
    {
        public SettingValueKind Kind;
        public bool ToggleValue;
        public int DropdownIndex;
        public IReadOnlyList<string> DropdownOptions;
    }

    internal static class VisibleWhenEvaluator
    {
        internal static bool Evaluate(
            VisibleWhenCondition condition,
            SettingValueSnapshot controller,
            out string failureReason)
        {
            failureReason = null;
            if (condition == null)
                return true;

            if (controller == null)
            {
                failureReason = "Controller snapshot is null";
                return true;
            }

            switch (condition)
            {
                case DropdownOptionCondition doc:
                    return EvaluateDropdownOption(doc, controller, out failureReason);
                case DropdownIndexCondition dic:
                    return EvaluateDropdownIndex(dic, controller, out failureReason);
                case ToggleCondition tc:
                    return EvaluateToggle(tc, controller, out failureReason);
                default:
                    failureReason = $"Unsupported condition type {condition.GetType().Name}";
                    return true;
            }
        }

        private static bool EvaluateDropdownOption(
            DropdownOptionCondition condition,
            SettingValueSnapshot controller,
            out string failureReason)
        {
            if (!ValidateDropdownController(condition.TargetKey, controller, out failureReason))
                return true;

            return string.Equals(
                controller.DropdownOptions[controller.DropdownIndex],
                condition.ExpectedOption,
                StringComparison.Ordinal);
        }

        private static bool EvaluateDropdownIndex(
            DropdownIndexCondition condition,
            SettingValueSnapshot controller,
            out string failureReason)
        {
            if (!ValidateDropdownController(condition.TargetKey, controller, out failureReason))
                return true;

            return controller.DropdownIndex == condition.ExpectedIndex;
        }

        private static bool EvaluateToggle(
            ToggleCondition condition,
            SettingValueSnapshot controller,
            out string failureReason)
        {
            failureReason = null;
            if (controller.Kind != SettingValueKind.Toggle)
            {
                failureReason = $"Expected toggle controller for key '{condition.TargetKey}', got {controller.Kind}";
                return true;
            }

            return controller.ToggleValue == condition.ExpectedValue;
        }

        private static bool ValidateDropdownController(
            string targetKey,
            SettingValueSnapshot controller,
            out string failureReason)
        {
            failureReason = null;
            if (controller.Kind != SettingValueKind.Dropdown)
            {
                failureReason = $"Expected dropdown controller for key '{targetKey}', got {controller.Kind}";
                return false;
            }

            if (controller.DropdownOptions == null || controller.DropdownOptions.Count == 0)
            {
                failureReason = "Dropdown options are empty";
                return false;
            }

            if (controller.DropdownIndex < 0 || controller.DropdownIndex >= controller.DropdownOptions.Count)
            {
                failureReason = $"Dropdown index {controller.DropdownIndex} is out of range";
                return false;
            }

            return true;
        }
    }
}
```

- [ ] **Step 7: 运行测试，确认通过**

Run:

```powershell
dotnet test "iGPU Savior.Tests\iGPU Savior.Tests.csproj" --filter "FullyQualifiedName~VisibleWhenEvaluatorTests"
```

Expected: 8 passed.

- [ ] **Step 8: Commit**

```bash
git add "iGPU Savior/UI/VisibleWhenEvaluator.cs"
git add "iGPU Savior/iGPU Savior.csproj"
git add "iGPU Savior.Tests/VisibleWhenEvaluatorTests.cs"
git add "iGPU Savior.Tests/iGPU Savior.Tests.csproj"
git commit -m "feat(modshared): 新增 VisibleWhenEvaluator 条件求值器与单元测试"
```

---

## Task 3: 在 `ModSettingsManager` 中集成条件可见性

**Files:**
- Modify: `iGPU Savior/UI/ModSettingsManager.cs`
- Modify: `iGPU Savior/UI/ModPulldownCloner.cs`
- Modify: `iGPU Savior.Tests/iGPU Savior.Tests.csproj`（如新增 ModSettingsManager 的测试）

- [ ] **Step 1: 更新数据模型以支持条件、当前值与所属 MOD**

在 `iGPU Savior/UI/ModSettingsManager.cs` 中，修改私有定义类：

```csharp
private abstract class SettingItemDef
{
    public string Label;
    public string Key => Label;
    public ModData Owner;
    public VisibleWhenCondition Condition;
}

private class ToggleDef : SettingItemDef
{
    public bool DefaultValue;
    public bool CurrentValue;
    public Action<bool> OnValueChanged;
}

private class DropdownDef : SettingItemDef
{
    public List<string> Options;
    public int DefaultIndex;
    public int CurrentIndex;
    public Action<int> OnValueChanged;
}
```

- [ ] **Step 2: 添加必要的字段与 using**

在 `ModSettingsManager` 类顶部添加：

```csharp
using System.Linq;
// 已有 using 保持不变

private readonly Dictionary<SettingItemDef, GameObject> _itemUIs =
    new Dictionary<SettingItemDef, GameObject>();
private readonly HashSet<string> _visibilityWarnings = new HashSet<string>();
```

- [ ] **Step 3: 让 `EnsureCurrentMod` 返回当前 MOD**

将 `private void EnsureCurrentMod()` 改为：

```csharp
private ModData EnsureCurrentMod()
{
    if (_currentRegisteringMod == null)
    {
        RegisterMod("General Settings", "");
    }
    return _currentRegisteringMod;
}
```

- [ ] **Step 4: 为 `Add*` 方法增加重载并设置 Owner 和 CurrentValue**

将现有 `AddToggle` / `AddDropdown` / `AddInputField` 保留，并各自新增一个带 `VisibleWhenCondition` 参数的重载：

```csharp
public void AddToggle(string labelOrKey, bool defaultValue, Action<bool> onValueChanged)
    => AddToggle(labelOrKey, defaultValue, onValueChanged, null);

public void AddToggle(string labelOrKey, bool defaultValue, Action<bool> onValueChanged,
                      VisibleWhenCondition visibleWhen)
{
    var owner = EnsureCurrentMod();
    var toggle = new ToggleDef
    {
        Label = labelOrKey,
        Owner = owner,
        DefaultValue = defaultValue,
        CurrentValue = defaultValue,
        OnValueChanged = onValueChanged,
        Condition = visibleWhen
    };
    owner.Items.Add(toggle);
}

public void AddDropdown(string labelOrKey, List<string> options, int defaultIndex,
                        Action<int> onValueChanged)
    => AddDropdown(labelOrKey, options, defaultIndex, onValueChanged, null);

public void AddDropdown(string labelOrKey, List<string> options, int defaultIndex,
                        Action<int> onValueChanged,
                        VisibleWhenCondition visibleWhen)
{
    var owner = EnsureCurrentMod();
    var dropdown = new DropdownDef
    {
        Label = labelOrKey,
        Owner = owner,
        Options = options ?? new List<string>(),
        DefaultIndex = defaultIndex,
        CurrentIndex = defaultIndex,
        OnValueChanged = onValueChanged,
        Condition = visibleWhen
    };
    owner.Items.Add(dropdown);
}

public void AddInputField(string labelText, string defaultValue, Action<string> onValueChanged)
    => AddInputField(labelText, defaultValue, onValueChanged, null);

public void AddInputField(string labelText, string defaultValue, Action<string> onValueChanged,
                          VisibleWhenCondition visibleWhen)
{
    var owner = EnsureCurrentMod();
    var input = new InputFieldDef
    {
        Label = labelText,
        Owner = owner,
        DefaultValue = defaultValue,
        OnValueChanged = onValueChanged,
        Condition = visibleWhen
    };
    owner.Items.Add(input);
}
```

- [ ] **Step 5: 包装回调以刷新依赖项**

添加私有方法，构建时替换原始 `OnValueChanged`：

```csharp
private Action<bool> WrapToggleCallback(ToggleDef toggle)
{
    Action<bool> original = toggle.OnValueChanged;
    return (value) =>
    {
        toggle.CurrentValue = value;
        try
        {
            RefreshDependents(toggle.Owner, toggle);
        }
        catch (Exception e)
        {
            PotatoOptimization.Core.PotatoPlugin.Log.LogWarning($"[ModSettings] 刷新依赖失败: {e.Message}");
        }
        original?.Invoke(value);
    };
}

private Action<int> WrapDropdownCallback(DropdownDef dropdown)
{
    Action<int> original = dropdown.OnValueChanged;
    return (index) =>
    {
        dropdown.CurrentIndex = index;
        try
        {
            RefreshDependents(dropdown.Owner, dropdown);
        }
        catch (Exception e)
        {
            PotatoOptimization.Core.PotatoPlugin.Log.LogWarning($"[ModSettings] 刷新依赖失败: {e.Message}");
        }
        original?.Invoke(index);
    };
}
```

- [ ] **Step 6: 修改 `BuildSequence` 缓存 UI 行对象并在构建后应用初始可见性**

在 `BuildSequence()` 开头，在清空子物体后清空映射：

```csharp
private IEnumerator BuildSequence()
{
    _isBuildingUI = true;
    _itemUIs.Clear();
    foreach (Transform child in _contentParent) Destroy(child.gameObject);
    yield return null;

    foreach (var mod in _registeredMods)
    {
        if (mod.Name != "General Settings" || !string.IsNullOrEmpty(mod.Version))
        {
            CreateSectionHeader(mod.Name, mod.Version);
        }

        foreach (var item in mod.Items)
        {
            GameObject row = null;
            if (item is ToggleDef toggle)
            {
                Action<bool> wrapped = WrapToggleCallback(toggle);
                row = ModToggleCloner.CreateToggle(_settingUIRoot, toggle.Label, toggle.DefaultValue, wrapped);
                if (row != null)
                {
                    row.transform.SetParent(_contentParent, false);
                    EnforceLayout(row);
                    row.SetActive(true);
                }
            }
            else if (item is DropdownDef dropdown)
            {
                yield return CreateDropdownSequence(dropdown, (created) =>
                {
                    _itemUIs[dropdown] = created;
                });
            }
            else if (item is InputFieldDef inputDef)
            {
                Transform graphicsContent = _settingUIRoot.Find("Graphics/ScrollView/Viewport/Content");
                if (graphicsContent == null)
                {
                    PotatoOptimization.Core.PotatoPlugin.Log.LogError("[Manager] Graphics Content not found!");
                    continue;
                }

                row = ModInputFieldCloner.CreateInputField(
                    graphicsContent,
                    inputDef.Label,
                    inputDef.DefaultValue,
                    inputDef.OnValueChanged);

                if (row != null)
                {
                    row.transform.SetParent(_contentParent, false);
                    EnforceLayout(row);
                    row.SetActive(true);
                }
                else
                {
                    PotatoOptimization.Core.PotatoPlugin.Log.LogWarning($"[Manager] Failed to create input field: {inputDef.Label}");
                }
            }

            if (!(item is DropdownDef) && row != null)
            {
                _itemUIs[item] = row;
            }
        }

        yield return null; // Dropdown layout 同步
        ApplyInitialVisibilityForMod(mod);
        CreateDivider();
    }

    Canvas.ForceUpdateCanvases();
    LayoutRebuilder.ForceRebuildLayoutImmediate(_contentParent as RectTransform);
    Canvas.ForceUpdateCanvases();

    var scrollRect = _contentParent.GetComponentInParent<ScrollRect>();
    if (scrollRect != null)
    {
        ModSettingsStyle.ConfigureScrollViewport(scrollRect);
        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.GetComponent<RectTransform>());
        scrollRect.StopMovement();
        scrollRect.verticalNormalizedPosition = 1f;
    }

    _isBuildingUI = false;
}
```

注意：这里对下拉框的创建采用回调方式缓存对象。需要在下一步调整 `CreateDropdownSequence` 签名。

- [ ] **Step 7: 修改 `CreateDropdownSequence` 接受缓存回调并包装下拉回调**

将签名改为：

```csharp
private IEnumerator CreateDropdownSequence(DropdownDef def, Action<GameObject> onRootCreated)
```

在方法内，下拉框创建成功后通过回调把根对象传回 `BuildSequence`：

```csharp
private IEnumerator CreateDropdownSequence(DropdownDef def, Action<GameObject> onRootCreated)
{
    GameObject pulldownClone = ModPulldownCloner.CloneAndClearPulldown(_settingUIRoot);
    if (pulldownClone == null) yield break;

    var paths = new[] { "TitleText", "Title/Text", "Text" };
    foreach (var p in paths)
    {
        var t = pulldownClone.transform.Find(p);
        if (t != null)
        {
            var tmp = t.GetComponent<TMP_Text>();
            if (tmp)
            {
                tmp.text = def.Label;
                var loc = t.GetComponent<ModLocalizer>();
                if (loc != null) loc.Key = def.Label;
                break;
            }
        }
    }

    Action<int> wrapped = WrapDropdownCallback(def);
    GameObject buttonTemplate = ModPulldownCloner.GetSelectButtonTemplate(_settingUIRoot);
    for (int i = 0; i < def.Options.Count; i++)
    {
        int idx = i;
        ModPulldownCloner.AddOption(pulldownClone, buttonTemplate, def.Options[i], () => wrapped?.Invoke(idx));
    }

    if (def.DefaultIndex >= 0 && def.DefaultIndex < def.Options.Count)
        UpdatePulldownSelectedText(pulldownClone, def.Options[def.DefaultIndex]);

    Destroy(buttonTemplate);
    pulldownClone.transform.SetParent(_contentParent, false);

    EnforceLayout(pulldownClone);
    pulldownClone.SetActive(true);

    onRootCreated?.Invoke(pulldownClone);

    Transform content = pulldownClone.transform.Find("PulldownList/Pulldown/CurrentSelectText (TMP)/Content");
    LayoutRebuilder.ForceRebuildLayoutImmediate(content as RectTransform);
    Canvas.ForceUpdateCanvases();
    yield return null;
    LayoutRebuilder.ForceRebuildLayoutImmediate(content as RectTransform);
    yield return null;

    float contentHeight = (content as RectTransform).sizeDelta.y;
    if (contentHeight < 40f) contentHeight = def.Options.Count * 40f;

    Transform originalPulldown = _settingUIRoot.Find("Graphics/ScrollView/Viewport/Content/GraphicQualityPulldownList");
    ModPulldownCloner.EnsurePulldownListUI(pulldownClone, originalPulldown, content, contentHeight);

    yield return new WaitForSeconds(0.05f);
}
```

- [ ] **Step 8: 实现可见性求值与刷新逻辑**

在 `ModSettingsManager` 类末尾添加：

```csharp
private void ApplyInitialVisibilityForMod(ModData mod)
{
    foreach (var item in mod.Items)
    {
        if (item.Condition == null) continue;
        bool visible = EvaluateVisibility(mod, item, out _);
        ApplyVisibility(item, visible);
    }
}

private void RefreshDependents(ModData owner, SettingItemDef controller)
{
    var dependents = owner.Items.Where(i => i.Condition != null && i.Condition.TargetKey == controller.Key);
    foreach (var dependent in dependents)
    {
        bool visible = EvaluateVisibility(owner, dependent, out _);
        ApplyVisibility(dependent, visible);
    }
}

private bool EvaluateVisibility(ModData owner, SettingItemDef dependent, out string failureReason)
{
    failureReason = null;
    if (dependent.Condition == null)
        return true;

    var targets = owner.Items.Where(i => i.Key == dependent.Condition.TargetKey).ToList();
    if (targets.Count != 1)
    {
        failureReason = targets.Count == 0
            ? $"Condition target '{dependent.Condition.TargetKey}' not found"
            : $"Condition target '{dependent.Condition.TargetKey}' is ambiguous ({targets.Count} matches)";
        LogVisibilityWarningOnce(dependent, failureReason);
        return true;
    }

    var controller = targets[0];
    var snapshot = ToSnapshot(controller);
    bool result = VisibleWhenEvaluator.Evaluate(dependent.Condition, snapshot, out failureReason);
    if (!string.IsNullOrEmpty(failureReason))
    {
        LogVisibilityWarningOnce(dependent, failureReason);
    }
    return result;
}

private SettingValueSnapshot ToSnapshot(SettingItemDef item)
{
    if (item is ToggleDef toggle)
    {
        return new SettingValueSnapshot
        {
            Kind = SettingValueKind.Toggle,
            ToggleValue = toggle.CurrentValue
        };
    }

    if (item is DropdownDef dropdown)
    {
        return new SettingValueSnapshot
        {
            Kind = SettingValueKind.Dropdown,
            DropdownIndex = dropdown.CurrentIndex,
            DropdownOptions = dropdown.Options
        };
    }

    return null;
}

private void ApplyVisibility(SettingItemDef item, bool visible)
{
    if (!_itemUIs.TryGetValue(item, out GameObject row) || row == null) return;
    if (row.activeSelf == visible) return;

    if (!visible)
    {
        ModPulldownCloner.TryClosePulldown(row);
    }

    row.SetActive(visible);
    LayoutRebuilder.ForceRebuildLayoutImmediate(_contentParent as RectTransform);
}

private void LogVisibilityWarningOnce(SettingItemDef item, string reason)
{
    string key = $"{item.Owner?.Name}:{item.Key}:{reason}";
    if (_visibilityWarnings.Add(key))
    {
        PotatoOptimization.Core.PotatoPlugin.Log.LogWarning($"[ModSettings] 条件可见性错误（{key}）：{reason}，该项将保持可见。");
    }
}
```

- [ ] **Step 9: 在 `ModPulldownCloner` 中新增安全的关闭方法**

在 `ModPulldownCloner.cs` 中已有 `GetPulldownUIType()` 的辅助方法基础上，添加：

```csharp
public static bool TryClosePulldown(GameObject pulldownClone)
{
    if (pulldownClone == null) return false;

    try
    {
        Type pulldownType = GetPulldownUIType();
        if (pulldownType == null) return false;

        var pulldownUI = pulldownClone.GetComponent(pulldownType)
            ?? pulldownClone.GetComponentInChildren(pulldownType, true);
        if (pulldownUI == null) return false;

        var closeMethod = pulldownType.GetMethod("ClosePullDown",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (closeMethod == null) return false;

        closeMethod.Invoke(pulldownUI, new object[] { false });
        return true;
    }
    catch (Exception e)
    {
        PotatoOptimization.Core.PotatoPlugin.Log.LogWarning($"[ModPulldownCloner] TryClosePulldown failed: {e.Message}");
        return false;
    }
}
```

确认 `ModPulldownCloner.cs` 文件顶部已包含 `using System.Reflection;` 和 `using PotatoOptimization.Core;`。

- [ ] **Step 10: 在主 csproj 中确认 VisibleWhenEvaluator.cs 已加入**

已在 Task 2 中添加。如 Task 2 未完成，请确保 `iGPU Savior/iGPU Savior.csproj` 包含：

```xml
<Compile Include="UI\VisibleWhen.cs" />
<Compile Include="UI\VisibleWhenEvaluator.cs" />
```

- [ ] **Step 11: 运行 Release 构建验证**

```powershell
dotnet msbuild "iGPU Savior\iGPU Savior.csproj" /t:Build /p:Configuration=Release /p:GameDir="D:\SteamLibrary\steamapps\common\Chill with You Lo-Fi Story"
```

将 `GameDir` 替换为实际游戏目录。

Expected: Build succeeded with 0 errors.

- [ ] **Step 12: 运行测试工程**

```powershell
dotnet test "iGPU Savior.Tests\iGPU Savior.Tests.csproj"
```

Expected: 所有测试通过（原快照测试 1 + SettingUI 测试 + VisibleWhen 相关测试）。

- [ ] **Step 13: Commit**

```bash
git add "iGPU Savior/UI/ModSettingsManager.cs"
git add "iGPU Savior/UI/ModPulldownCloner.cs"
git commit -m "feat(modsettings): 集成条件可见性与依赖刷新"
```

---

## Task 4: 用最小 MOD/E2E 手动验证

- [ ] **Step 1: 自己 MOD 内使用新接口做最小集成示例**

在 `iGPU Savior/UI/ModSettingsIntegration.cs` 的 `RegisterCurrentMod` 末尾，可临时添加一个示例pod（验证通过后应删除或保留用于展示均可）:

```csharp
manager.AddDropdown("TASK4_EXAMPLE_PROVIDER", new List<string> { "Provider_A", "Provider_B" }, 0, idx => { });
manager.AddInputField("TASK4_EXAMPLE_A_DETAIL", "", _ => { },
    VisibleWhen.DropdownOption("TASK4_EXAMPLE_PROVIDER", "Provider_A"));
manager.AddInputField("TASK4_EXAMPLE_B_DETAIL", "", _ => { },
    VisibleWhen.DropdownOption("TASK4_EXAMPLE_PROVIDER", "Provider_B"));
```

这仅用于手动观察；建议提交前删除或注释。

- [ ] **Step 2: 进游戏打开设置 → MOD 标签页**

切换 `TASK4_EXAMPLE_PROVIDER` 选项，确认对应 InputField 即时显示/隐藏。观察 Console/BepInEx 日志无异常。

- [ ] **Step 3: 边界场景验证**

1. 同一 MOD 内两个配置项 Label 重复时，条件 fail-open 并只警告一次。
2. 条件指向不存在的 key 时，依赖项保持可见且只警告一次。
3. Dropdown 展开时切换到隐藏状态，不留悬浮列表。
4. 关闭设置页再打开，初始可见性正确。

- [ ] **Step 4: 清理临时验证代码并提交（如用于保留示例则保留）**

```bash
git add "iGPU Savior/UI/ModSettingsIntegration.cs"
git commit -m "chore(modsettings): 最小集成示例用于手动验证" # 或删除后不提交
```

---

## Task 5: 版本同步与最终检查

- [ ] **Step 1: 确认 version.json 是否需要提升**

本次为功能新增，通常提升次版本号或补丁号。运行：

```powershell
scripts/sync-version.ps1
```

如版本变更，确认 `Constants.cs`、`thunderstore/manifest.json`、`CHANGELOG.md` 已被脚本同步。

- [ ] **Step 2: 最终验证**

```powershell
dotnet test "iGPU Savior.Tests\iGPU Savior.Tests.csproj"

dotnet msbuild "iGPU Savior\iGPU Savior.csproj" /t:Build /p:Configuration=Release /p:GameDir="D:\SteamLibrary\steamapps\common\Chill with You Lo-Fi Story"
```

- [ ] **Step 3: Commit 版本同步（如有）**

```bash
git add version.json scripts/sync-version.ps1 "iGPU Savior/Core/Constants.cs" thunderstore/manifest.json CHANGELOG.md
git commit -m "chore(release): 同步版本号"
```
