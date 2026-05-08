# FAQ

## 架构相关

### 系统是怎么把 MOD 标签加进游戏设置的？

通过 Harmony Patch `ModSettingsIntegration.SetupHook`（Postfix on `SettingUI.Setup()`），克隆 Credits 标签页来创建 MOD 标签。按钮、内容面板、ScrollRect 都从 Credits 模板复制。

### MOD 标签和其他标签的切换逻辑是怎样的？

- 点 MOD 标签 → `SwitchToModTab()` 隐藏所有原生内容页，显示 MOD 面板，调用 `RebuildUI()`
- 点任何原生标签 → 反射注入的委托隐藏 MOD 面板，恢复原生切换逻辑
- 重新打开设置窗口 → `ModSettingsActivateHandler`（Activate Postfix）强制切回 General 标签

### 为什么 UI 是克隆出来的，而不是手动创建的？

游戏原生 UI 的样式由反编译难以提取的参数决定（材质、shader、Canvas 层序）。直接 Instantiate 原生控件模板再替换数据和回调，能保证视觉 100% 一致，无需手动追踪参数变更。

## API 使用

### 怎么注册设置？

```csharp
ModSettingsManager.Instance.RegisterMod("My Mod", "1.0");
ModSettingsManager.Instance.AddToggle("Enable", true, v => ...);
ModSettingsManager.Instance.AddDropdown("Quality", opts, 0, i => ...);
// RebuildUI 由标签切换时自动调用
```

### 必须调用 RebuildUI 吗？

通常不需要。`ModSettingsIntegration` 在每次切换到 MOD 标签时自动调用。除非你在设置窗口已打开时动态增删设置项，否则不需要手动触发。

### 怎么加翻译？

```csharp
ModSettingsManager.Instance.RegisterTranslation("MY_KEY", "English", "日本語", "简体中文");
// 在所有 AddToggle/AddDropdown/AddInputField 之前调用
```

然后用 key 作为 labelOrKey 参数。未匹配到翻译时回退为原始 key 文本。

### 我没有调用 RegisterMod 就 AddToggle，会怎样？

系统自动将你的设置项归入 "General Settings" 匿名组（不显示标题），确保向后兼容。

### 下拉框里的选项文本也支持翻译吗？

支持。options 字符串列表中的每个元素都作为 key 去 `ModTranslationManager` 查找，未匹配则原样显示。

## 样式相关

### UI 样式是在哪里控制的？

样式由两个机制共同决定：
1. **克隆模板** — 从游戏原生控件 Instantiate，继承所有原生样式参数
2. **EnforceLayout()** — 对每个克隆后的控件强制锚点对齐、标签宽度（380px）、最小行高（60px）

没有独立的硬编码样式表。不要手动修改样式——改克隆模板或 EnforceLayout。

### 为什么不同的设置项看起来高度不同？

Toggle 和 InputField 的模板来自游戏原生 UI 的不同控件，天然有不同的内部结构。`EnforceLayout` 仅保证最小高度和对齐，不强制统一视觉效果。但所有控件都继承游戏原生风格，整体协调。

## 兼容性

### 外部 MOD 怎么判断系统是否可用？

```csharp
if (ModSettingsManager.Instance != null && ModSettingsManager.Instance.IsInitialized)
    // 可以注册
```

`Instance` 通过 `DontDestroyOnLoad` 跨场景存活。

### 如果同一个 MOD 被注册两次会怎样？

`RegisterMod` 检测到同名 MOD 时复用已有条目，参数不会被覆盖，后续 Add 调用会追加到同一个 MOD 下。
