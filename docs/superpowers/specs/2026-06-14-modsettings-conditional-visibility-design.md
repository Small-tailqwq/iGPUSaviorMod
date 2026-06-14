# ModSettingsManager 条件可见性接口设计

## 背景与目标

iGPU Savior 目前已向其他 MOD 公开 `ModShared` 命名空间下的 `ModSettingsManager`，提供 `AddToggle` / `AddDropdown` / `AddInputField` 三个基础接口。外部 MOD（如 EnvSync）提出需求：希望根据某个下拉框或开关的当前值，动态显示/隐藏一组相关配置项。

**本次目标**：在保留现有 `Add*` 方法签名、不破坏正常程序集调用二进制兼容性的前提下，为 `ModSettingsManager` 增加声明式条件可见性能力，使外部 MOD 可以用一行条件声明实现配置项联动显隐。

## 成功标准

1. 外部 MOD 可自然写出如下代码：
   ```csharp
   manager.AddInputField(labelCity, location, onCityChanged,
       visibleWhen: VisibleWhen.DropdownOption("ENV_SYNC_PROVIDER", "Seniverse"));
   ```
2. 旧 MOD 不重新编译，且通过正常方法调用或按完整参数签名反射调用现有 `Add*` 方法时，仍可加载运行。
3. 下拉框/开关值变化时，被依赖项实时显示/隐藏，布局自动刷新。
4. 不同 MOD 使用相同 `labelOrKey` 时互不影响。
5. 新代码通过 `dotnet test` 与指定 `GameDir` 的 Release 构建验证。

## 公开 API（`ModShared` 命名空间）

### 新增条件类型

新增 `iGPU Savior/UI/VisibleWhen.cs`。条件对象不可变，只允许通过 `VisibleWhen` 工厂创建；外部 MOD 不需要也不能自行实现条件子类。

```csharp
namespace ModShared
{
    public abstract class VisibleWhenCondition
    {
        public string TargetKey { get; }

        internal VisibleWhenCondition(string targetKey)
        {
            TargetKey = targetKey;
        }
    }

    public static class VisibleWhen
    {
        public static VisibleWhenCondition DropdownOption(string targetKey, string expectedOption);
        public static VisibleWhenCondition DropdownIndex(string targetKey, int expectedIndex);
        public static VisibleWhenCondition Toggle(string targetKey, bool expectedValue);
    }
}
```

- `targetKey` 指向**同一 MOD 内**另一个配置项的 `labelOrKey`。
- `DropdownOption` 使用下拉框原始 `options` 中的字符串进行 `StringComparison.Ordinal` 比较；若选项是本地化 key，则传入 key，而不是翻译后的显示文本。
- 工厂方法拒绝空 `targetKey`；`DropdownOption` 还拒绝 `null expectedOption`。
- 具体条件实现类型设为 `internal sealed`，属于内部实现细节。
- 条件匹配采用 `ModSettingsManager` 跟踪的当前 UI 值，不是初始默认值。
- 外部代码若绕过 UI 直接修改自己的配置值，Manager 不会自动感知；本次不新增外部值同步接口。

### Add* 方法重载

现有签名和现有参数名保持不动，新增带 `VisibleWhenCondition` 的重载：

```csharp
public void AddToggle(string labelOrKey, bool defaultValue, Action<bool> onValueChanged);
public void AddToggle(string labelOrKey, bool defaultValue, Action<bool> onValueChanged,
                      VisibleWhenCondition visibleWhen);

public void AddDropdown(string labelOrKey, List<string> options, int defaultIndex,
                        Action<int> onValueChanged);
public void AddDropdown(string labelOrKey, List<string> options, int defaultIndex,
                        Action<int> onValueChanged,
                        VisibleWhenCondition visibleWhen);

// 保留现有参数名 labelText，避免影响使用命名参数的旧源码。
public void AddInputField(string labelText, string defaultValue, Action<string> onValueChanged);
public void AddInputField(string labelText, string defaultValue, Action<string> onValueChanged,
                          VisibleWhenCondition visibleWhen);
```

- 旧三参数/四参数重载内部转调新重载，并传入 `null` 条件。
- 新重载接受 `visibleWhen == null`，此时配置项始终可见。
- 为保持调用一致性，三类配置项自身都可以带条件。

## 内部机制

### 数据模型调整

`SettingItemDef` 增加识别 key 和条件。`Label` 继续兼任显示文本、本地化 key 与识别 key，本次不新增显式 key 参数。

```csharp
private abstract class SettingItemDef
{
    public string Label;
    public string Key => Label;
    public VisibleWhenCondition Condition;
}

private class DropdownDef : SettingItemDef
{
    public List<string> Options;
    public int DefaultIndex;
    public int CurrentIndex;
    public Action<int> OnValueChanged;
}

private class ToggleDef : SettingItemDef
{
    public bool DefaultValue;
    public bool CurrentValue;
    public Action<bool> OnValueChanged;
}
```

- 注册配置项时，立即令 `CurrentValue = DefaultValue`、`CurrentIndex = DefaultIndex`，因此首次构建 UI 前已有明确状态。
- `CurrentIndex` 可以暂时保留无效的调用方输入；求值时必须进行边界检查，不能直接索引 `Options[CurrentIndex]`。
- 同一 MOD 内，作为条件目标的 `Key` 必须唯一。若目标 key 找到零个或多个控制器，条件 fail-open 并输出一次警告。
- 不同 MOD 可以安全使用相同 `Key`。

### UI 对象映射

UI 对象按定义实例映射，不按字符串全局映射：

```csharp
private readonly Dictionary<SettingItemDef, GameObject> _itemUIs =
    new Dictionary<SettingItemDef, GameObject>();
```

这样不会因不同 MOD 使用相同 Label 而互相覆盖，也不会让重复 Label 直接破坏 UI 对象记录。

`RebuildUI` / `BuildSequence` 的行为：

1. 构建开始时清空 `_itemUIs`，然后销毁旧 UI。
2. 每一行创建成功后保存 `_itemUIs[item] = rowGameObject`。
3. Toggle 和 InputField 可在外层保存；Dropdown 在 `CreateDropdownSequence` 创建并挂载根对象后保存。
4. 所有 MOD、所有行创建完成后，调用 `ApplyInitialVisibility()`。
5. 只有可见状态实际发生变化时才强制刷新布局。

### 所属 MOD 与依赖查找

条件只能解析同一 `ModData` 内的控制器。运行时不得使用 `_currentRegisteringMod` 判断所属 MOD，因为它只表示最近一次注册目标，并不代表用户当前操作的配置项。

创建 UI 回调时必须捕获该项所属的 `ModData`：

```csharp
RefreshDependents(ModData owner, SettingItemDef controller);
EvaluateVisibility(ModData owner, SettingItemDef dependent);
```

控制器查找规则：

1. 仅扫描 `owner.Items`。
2. 找到唯一 `item.Key == condition.TargetKey` 的项后求值。
3. 找不到或找到多个时 fail-open，并针对该问题输出一次 `LogWarning`。
4. 控制器类型与条件类型不匹配时 fail-open，并输出一次警告。

### 回调与刷新顺序

Toggle / Dropdown 的 UI 回调由 Manager 包装，顺序固定为：

1. 更新 `CurrentValue` / `CurrentIndex`。
2. 刷新同一 MOD 内依赖此控制器的配置项。
3. 调用外部 MOD 原始 `OnValueChanged`。

先更新和刷新内部状态，可以保证即使第三方回调抛出异常，Manager 的可见性状态仍与用户刚刚选择的 UI 值一致。调用外部回调时仍应沿用现有 UI 层的异常处理策略，避免第三方异常破坏后续交互。

### 纯条件求值核心

新增无 Unity 依赖的 `iGPU Savior/UI/VisibleWhenEvaluator.cs`，用于集中实现条件求值。该文件只依赖条件对象和一个简单的控制器值快照，不依赖 `MonoBehaviour`、`GameObject` 或私有 UI 定义。

建议内部模型：

```csharp
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
```

求值规则：

```csharp
internal static bool Evaluate(
    VisibleWhenCondition condition,
    SettingValueSnapshot controller,
    out string failureReason);
```

- 匹配成功或不匹配时返回正常布尔结果，`failureReason = null`。
- 控制器不存在、目标不唯一、类型不匹配、下拉列表为空或索引越界都视为无法可靠求值：返回 `true`（fail-open），并提供 `failureReason` 给 Manager 做一次性警告。
- `DropdownOption` 使用原始 option 字符串和 `StringComparison.Ordinal`。
- 未识别的条件类型同样 fail-open。

控制器查找仍由 `ModSettingsManager` 完成；纯 evaluator 只负责“给定条件和控制器快照是否匹配”，便于离线测试。

### 可见性与布局刷新

`ApplyVisibility` 执行以下操作：

1. 从 `_itemUIs` 获取依赖项对应的行对象；UI 创建失败或对象已销毁时跳过。
2. 如果目标可见状态与 `activeSelf` 相同，直接返回。
3. 隐藏 Dropdown 前，最佳努力调用已有 `ClosePullDown(false)` 反射路径关闭展开列表；关闭失败只警告，不阻止隐藏。
4. 调用 `row.SetActive(visible)`。
5. 使用 `_contentParent as RectTransform` 调用 `LayoutRebuilder.ForceRebuildLayoutImmediate`，必要时配合 `Canvas.ForceUpdateCanvases()`。

本次只控制配置行本身，不自动隐藏 MOD section header 或 divider。

### 级联与依赖刷新

- 条件变化仅由控制器本身的 UI 值变化触发。
- 刷新时扫描所属 `ModData.Items`，找到 `Condition.TargetKey == controller.Key` 的项重新计算，因此支持一个控制器控制多个依赖项。
- 本次不支持条件链语义：带条件的配置项不应再作为其他条件的控制器。注册或首次构建时若检测到这种关系，输出一次警告；不做拓扑排序或可见性传播。

## Fail-open 与日志策略

条件配置错误时优先保持配置项可见，避免用户因隐藏项无法自行修复配置。

以下情况统一 fail-open：

- `targetKey` 在所属 MOD 中不存在或不唯一。
- 条件类型与控制器类型不匹配。
- Dropdown 的 options 为 `null`、空列表或当前索引越界。
- 遇到未知条件实现。

警告需要“每个 MOD + 依赖项 key + 原因”只输出一次，避免每次点击或 RebuildUI 刷屏。UI 对象暂时不存在不属于条件配置错误，无需警告。

## 兼容性

- **正常二进制调用兼容**：保留原 `Add*` 方法的名称、参数类型和参数数量，老插件已有的程序集方法引用仍可解析。
- **源码兼容**：保留旧重载和旧参数名；新增重载不影响普通旧调用。
- **行为兼容**：不传 `visibleWhen` 时行为与原来一致。
- **反射兼容边界**：新增同名重载后，第三方若使用 `GetMethod("AddToggle")` 这类只按名称查找的方法，可能得到 `AmbiguousMatchException`。已知外部 MOD 和文档示例应使用参数类型数组按完整签名查找；手动验证阶段需要特别检查 EnvSync。无法同时提供同名重载并保证“只按名称反射”的行为不变。

## 是否需要分析游戏源码

本次改动完全基于已有的 Harmony 注入点（`SettingUI.Setup`）和我们自己克隆/创建的 UI 控件，不需要新增对游戏内部类型的反射或字段访问，因此不需要 ilspy 深剖游戏源码。

隐藏展开中的 Dropdown 时会复用 `ModPulldownCloner` 已有的 `PulldownListUI` 类型发现和 `ClosePullDown` 反射路径；若实际验证发现关闭行为异常，再针对这一处分析游戏类型。

## 文件与工程调整

新增：

- `iGPU Savior/UI/VisibleWhen.cs`：公开条件 API 与内部具体条件类型。
- `iGPU Savior/UI/VisibleWhenEvaluator.cs`：无 Unity 依赖的纯求值逻辑。
- `iGPU Savior.Tests/VisibleWhenConditionTests.cs`：条件工厂与求值测试。

修改：

- `iGPU Savior/UI/ModSettingsManager.cs`：重载、当前值跟踪、所属 MOD 捕获、UI 映射和刷新。
- `iGPU Savior/UI/ModPulldownCloner.cs`：如有必要，提取可复用的最佳努力关闭方法。
- 两个 `.csproj`：显式加入新增源码；测试工程链接 `VisibleWhen.cs` 和 `VisibleWhenEvaluator.cs`。

## 测试计划

### 单元测试

`VisibleWhenConditionTests` 在不引用 Unity 的测试工程内验证：

- 三种工厂正确保存目标与预期值。
- `DropdownOption` 使用原始 option key 且按 ordinal 比较。
- `DropdownIndex` 匹配当前索引通过/失败。
- `Toggle` 匹配当前 bool 通过/失败。
- 控制器不存在时 fail-open。
- 类型不匹配时 fail-open。
- Dropdown options 为空或当前索引越界时 fail-open。
- 未识别条件时 fail-open（若测试程序集可构造）。

纯 evaluator 不负责“同 MOD 唯一目标查找”，因此该部分通过针对 Manager 提取出的无 Unity 辅助逻辑测试，或在手动验证中覆盖：

- 两个不同 MOD 使用相同 key 时互不影响。
- 同一 MOD 内目标 key 重复时 fail-open 并警告。

### 自动验证

```powershell
dotnet test "iGPU Savior.Tests\iGPU Savior.Tests.csproj"

dotnet msbuild "iGPU Savior\iGPU Savior.csproj" /t:Build /p:Configuration=Release /p:GameDir="D:\SteamLibrary\steamapps\common\Chill with You Lo-Fi Story"
```

构建时必须将 `GameDir` 替换为实际游戏安装目录。

### 手动验证

使用 EnvSync 或最小测试 MOD 注册以下场景：

1. Provider Dropdown 控制多个 InputField，切换时即时显隐。
2. Toggle 控制一个 Dropdown。
3. 两个 MOD 使用相同 key，切换任一控制器不会影响另一个 MOD。
4. 重开设置页触发 RebuildUI 后，初始可见性仍正确。
5. Dropdown 展开时被上游条件隐藏，不遗留悬浮列表或异常 sorting order。
6. 条件目标不存在、重复、类型不匹配时，依赖项保持可见且只警告一次。
7. 已知外部 MOD 的旧 `Add*` 调用与反射调用仍能工作。

## 风险与范围边界

- `Label` 继续兼任显示文本、本地化 key 与识别 key，同一 MOD 内作为条件目标时必须唯一；本次通过 fail-open 降低误配置影响，未来有实际需求再引入显式 `key` 参数。
- 新增同名重载无法兼容只按方法名称反射的第三方代码，需通过已知外部 MOD 手动验证控制风险。
- 当前值只跟踪共享设置 UI 内的用户操作；外部直接修改配置不会触发条件刷新。
- 本次不支持条件组合（AND / OR / NOT）、跨 MOD 条件、条件链、section header 自动显隐或运行时动态增删配置项。
