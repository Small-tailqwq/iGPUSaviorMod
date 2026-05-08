# MOD 设置系统架构

## 整体流程

```
SettingUI.Setup() 被调用
    → ModSettingsIntegration.Hook (Harmony Postfix)
        → CreateModSettingsTab()
            → 克隆 Credits 标签 → 创建 MOD 标签按钮 + Content 面板
            → ConfigureContentLayout() — 设置 ScrollRect、VerticalLayoutGroup
            → HookIntoTabButtons() — 反射挂钩，使原版标签切换时隐藏 MOD 面板
            → RegisterCurrentMod() — 注册 iGPU Savior 自己的设置项
                → RegisterMod("iGPU Savior", version)
                → AddToggle / AddDropdown / AddInputField × N
        → ModSettingsManager.RebuildUI(contentParent, settingUIRoot)
            → BuildSequence() 协程
                → 清空 contentParent
                → foreach mod in _registeredMods:
                    → CreateSectionHeader(mod.Name, mod.Version)
                    → foreach item in mod.Items:
                        → ToggleDef     → ModToggleCloner.CreateToggle()
                        → DropdownDef   → CreateDropdownSequence()
                        → InputFieldDef → ModInputFieldCloner.CreateInputField()
                    → EnforceLayout(obj) — 强制锚点、标签宽度、最小行高
                    → CreateDivider() — 20px 间距
                → LayoutRebuilder.ForceRebuildLayoutImmediate()
```

## 标签切换机制

当用户点击 MOD 标签时：
1. `SwitchToModTab()` 被调用
2. 隐藏所有游戏原生内容页（General/Graphics/Audio/Credits）
3. 显示 MOD 内容面板
4. 调用 `RebuildUI()` 重建（或初次构建）

当用户点击任何游戏原生标签时：
1. `HookIntoTabButtons()` 注入的委托被触发
2. 隐藏 MOD 内容面板、重置 MOD 标签按钮状态
3. 原版标签切换流程正常执行

`ModSettingsActivateHandler` patch（`SettingUI.Activate` Postfix）处理窗口重新打开时重置到 General 标签，避免 MOD 标签被预选。

## Cloner 机制

三个 Cloner 类从游戏运行时场景中查找模板并 Instantiate 克隆：

### ModToggleCloner

- **模板路径**: `Audio/ScrollView/Viewport/Content/PomodoroSoundOnOffButtons`（通过 Constants.cs 定义）
- **流程**: Instantiate 模板 → 销毁原生 `TextLocalizationBehaviour` → 附加 `ModLocalizer` → 设置标签文本 → 绑定 ON/OFF 按钮回调 + `InteractableUI` 激活/反激活 → 调整位置
- **位置**: 按钮容器移至 `(197.5, 0)`，保持垂直居中

### ModPulldownCloner

- **模板路径**: `Graphics/ScrollView/Viewport/Content/GraphicQualityPulldownList`
- **流程**:
  1. `CloneAndClearPulldown()` — Instantiate 模板，清空现有选项
  2. `GetSelectButtonTemplate()` — 从原模板获取第一个选项按钮作为克隆样板
  3. `AddOption()` — 为每个选项 Instantiate 按钮，绑定 `ModLocalizer`，注入点击回调
  4. `EnsurePulldownListUI()` — 反射添加 `PulldownListUI` 组件，选项 > 6 时动态创建 ScrollRect+Viewport 结构，设置 Canvas overlay 排序（sortingOrder=30000）
- **附: `PulldownLayerController`** — 监听 `_isOpen` 状态，展开时提升 Canvas 层级确保在 UI 最顶层渲染

### ModInputFieldCloner

- **模板路径**: `Graphics/ScrollView/Viewport/Content/FrameRate` 行
- **流程**: Instantiate → 销毁 `DeactiveFrameRate` 和 `ParentTitle` 多余节点 → 移除 `MonoBehaviour` 脚本 → 销毁所有 LayoutGroup（改用手动坐标定位）→ 设置 `TMP_InputField.onEndEdit` 回调
- **坐标**: 标签 `(-306, 40)`，输入框 `(40, 40)`，行高 50px

## 布局强制 (EnforceLayout)

`ModSettingsManager.EnforceLayout(GameObject obj)` 对每个克隆后的控件执行：

```
1. RectTransform 强制 anchor (0,1) / (0,1)，pivot (0.5, 1)
2. 找到名为 Title/Label/Text 的 TMP_Text：
   → LayoutElement.minWidth = 380f / preferredWidth = 380f / flexibleWidth = 0
   → alignment = MidlineLeft
3. 根 LayoutElement: minHeight = 60f / preferredHeight = 60f
```

## 多语言系统

- `ModTranslationManager` — 静态字典，key → {en, ja, zh} 映射
- `ModLocalizer` — 挂载在 GameObject 上的组件，设置 `Key` 后自动调用 `ModTranslationManager.Get(key, lang)` 更新文本，同时通过 `FontSupplier` 切换字体
- 外部 MOD 可通过 `RegisterTranslation(key, en, ja, zh)` 注册新字符串

## 兼容性设计

- **未注册 MOD 回退**: 如果外部 MOD 直接调用 `AddToggle()` 而未先 `RegisterMod()`，自动将设置归入 "General Settings" 匿名组
- **IsInitialized 属性**: 外部 MOD 可检查 `ModSettingsManager.Instance.IsInitialized` 判断是否可注册
- **重建而非增量**: 每次 `RebuildUI()` 都清空 contentParent 并完整重建，避免增量更新的状态管理问题

## 依赖类型解析

`TypeHelper.cs` 提供缓存的反射类型查找。游戏内的 `PulldownListUI` 和 `SettingUI` 类型可能位于不同程序集名称下（取决于游戏版本），TypeHelper 尝试多个可能的程序集名直到命中。

## 配置持久化

所有设置项的值通过 BepInEx `ConfigEntry<T>` 持久化到磁盘。`ConfigurationManager.cs` 定义了所有 ConfigEntry：

- `KeyPotatoMode` / `KeyPiPMode` / `KeyCameraMirror` / `KeyPortraitMode` — `ConfigEntry<KeyCode>`，section "Hotkeys"
- `CfgEnableMirror` / `CfgEnablePortraitMode` — `ConfigEntry<bool>`，section "Camera"
- `CfgEnableDeleteConfirm` — `ConfigEntry<bool>`，section "General"
- `CfgWindowScale` — `ConfigEntry<WindowScaleRatio>`，section "Window"
- `CfgDragMode` — `ConfigEntry<DragMode>`，section "Window"
