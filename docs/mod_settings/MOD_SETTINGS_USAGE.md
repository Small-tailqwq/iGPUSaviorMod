# MOD 设置 API 使用指南

## 概述

`ModSettingsManager` 是一个单例，管理游戏设置界面中的"MOD"标签页。多个 MOD 共享同一个标签页，每个 MOD 注册自己的设置项（Toggle、Dropdown、InputField），系统自动克隆游戏原生 UI 模板来构建界面，保证视觉风格与游戏完全一致。

## 完整流程

```csharp
// 1. 注册 MOD
ModSettingsManager.Instance.RegisterMod("My Mod", "1.0.0");

// 2. (可选) 注册多语言翻译
ModSettingsManager.Instance.RegisterTranslation("SETTING_ENABLE_FEATURE", 
    "Enable Feature", "機能を有効化", "启用功能");

// 3. 添加设置项 — 支持翻译 Key 或直接写文本
ModSettingsManager.Instance.AddToggle("SETTING_ENABLE_FEATURE", true, val => Config.Enabled.Value = val);
ModSettingsManager.Instance.AddDropdown("SETTING_QUALITY", 
    new List<string> { "Low", "Medium", "High" }, 1, idx => SetQuality(idx));
ModSettingsManager.Instance.AddInputField("Frame Rate", "60", val => SetFPS(val));

// 4. 构建 UI（由 ModSettingsIntegration 自动调用，或手动触发）
ModSettingsManager.Instance.RebuildUI(contentParent, settingUIRoot);
```

## API 参考

### RegisterMod

```csharp
void RegisterMod(string modName, string modVersion)
```

注册一个 MOD 到共享标签页。重名 MOD 会复用已有条目，参数不会被覆盖。注册后进入"当前 MOD"状态，后续的 `AddToggle/Dropdown/InputField` 调用都将归属到这个 MOD。

### RegisterTranslation

```csharp
void RegisterTranslation(string key, string en, string ja, string zh)
```

注册一个翻译键。在所有 `AddToggle/Dropdown/InputField` 之前调用，之后调用 `AddXxx(key, ...)` 时系统会自动根据当前语言查找翻译文本。未找到翻译时回退显示 key 原始值。

### AddToggle

```csharp
void AddToggle(string labelOrKey, bool defaultValue, Action<bool> onValueChanged)
```

创建一个 ON/OFF 开关。系统会克隆游戏原生 Audio 设置中的 PomodoroSound 开关模板。labelOrKey 可以是翻译 key（需先 RegisterTranslation）或直接文本字符串。

### AddDropdown

```csharp
void AddDropdown(string labelOrKey, List<string> options, int defaultIndex, Action<int> onValueChanged)
```

创建一个下拉选择框。系统会克隆游戏原生 Graphics 设置中的 GraphicQuality 下拉框模板。选项数量 > 6 时自动创建 ScrollRect 滚动区域。

options 列表中的字符串同样支持翻译 key：若已在 `RegisterTranslation` 注册过，自动翻译显示文本。

### AddInputField

```csharp
void AddInputField(string labelText, string defaultValue, Action<string> onValueChanged)
```

创建一个文本输入框。系统会克隆游戏原生 Graphics 设置中的 FrameRate 输入框模板。

### RebuildUI

```csharp
void RebuildUI(Transform contentParent, Transform settingUIRoot)
```

构建（或重建）整个 MOD 标签页 UI。`contentParent` 是 MOD 内容面板的 Transform，`settingUIRoot` 是游戏设置界面的根 Transform（用于查找克隆模板）。此方法由 `ModSettingsIntegration` 在标签切换时自动调用，但也可手动触发。

### IsInitialized (兼容性属性)

```csharp
bool IsInitialized { get; }
```

返回 true 表示 `ModSettingsManager.Instance` 可用。用于外部 MOD 兼容性检测。`Instance` 本身通过 `DontDestroyOnLoad` 保证跨场景存活。

## 内部机制

### UI 构建

`RebuildUI()` 启动 `BuildSequence()` 协程，对每个注册的 MOD：

1. `CreateSectionHeader()` — 创建加粗标题栏（如 "iGPU Savior v1.6.0"）
2. 遍历设置项，分派给对应 Cloner：
   - ToggleDef → `ModToggleCloner.CreateToggle()`
   - DropdownDef → `CreateDropdownSequence()` → `ModPulldownCloner`
   - InputFieldDef → `ModInputFieldCloner.CreateInputField()`
3. `EnforceLayout()` — 强制左上锚点、380px 标签宽度、60px 最小行高
4. `CreateDivider()` — 添加 20px 分隔线

### Cloner 机制

系统不手动绘制 UI，而是从游戏运行时场景中找到既有的设置控件模板并 Instantiate 克隆，然后清空子项、替换数据和回调。好处是视觉风格与游戏原生完全一致，无需手动维护样式参数。

- **ModToggleCloner** — 查找 Graphics→Content 下的 `PomodoroSoundOnOffButtons` 作为模板
- **ModPulldownCloner** — 查找 Graphics→Content 下的 `GraphicQualityPulldownList` 作为模板
- **ModInputFieldCloner** — 查找 Graphics→Content 下的 `FrameRate` 行作为模板

### 布局强制

`EnforceLayout()` 对每个克隆后的控件进行：

- 强制 anchorMin/Max 为 (0, 1)（左上对齐，适配 VerticalLayoutGroup）
- 将名字含 Title/Label/Text 的子 TMP_Text 强制 `minWidth=380f`，确保与左侧标签对齐
- 设置根 `LayoutElement.minHeight=60f`，防止被压成 0

### 未注册 MOD 的回退处理

如果外部 MOD 直接调用 `AddToggle()` 而未先调用 `RegisterMod()`，系统自动将其设置归入名为 "General Settings" 的匿名 MOD 组（不显示标题），确保向后兼容。

## 当前已注册的设置

iGPU Savior 在 `ModSettingsIntegration.RegisterCurrentMod()` 中注册了：

| 设置 | 类型 | 翻译 Key |
|------|------|----------|
| 镜像模式自动开启 | Toggle | `SETTING_MIRROR_AUTO` |
| 竖屏模式自动开启 | Toggle | `SETTING_PORTRAIT_AUTO` |
| 删除确认 | Toggle | `SETTING_DELETE_CONFIRM` |
| 小窗缩放 | Dropdown | `SETTING_MINI_SCALE` |
| 拖动模式 | Dropdown | `SETTING_DRAG_MODE` |
| 土豆模式热键 | Dropdown | `SETTING_KEY_POTATO` |
| 小窗模式热键 | Dropdown | `SETTING_KEY_PIP` |
| 镜像模式热键 | Dropdown | `SETTING_KEY_MIRROR` |
| 竖屏模式热键 | Dropdown | `SETTING_KEY_PORTRAIT` |

具体配置值定义在 `Configuration/ConfigurationManager.cs` 中。
