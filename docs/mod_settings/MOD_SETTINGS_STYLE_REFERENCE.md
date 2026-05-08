# MOD 设置 UI 样式参考

## 双重样式机制

MOD 设置页的 UI 由两个层次共同决定样式：

1. **克隆模板** — 从游戏原生控件 Instantiate，继承游戏的所有材质、shader、字体、间距参数
2. **EnforceLayout()** — 对每个克隆后的控件进行布局强制修正

## EnforceLayout 常量

代码位于 `ModSettingsManager.cs:272-324`：

| 常量 | 值 | 作用 |
|------|-----|------|
| `LABEL_WIDTH` | 380f | 标签 TMP_Text 强制 min/preferredWidth |
| `rootLE.minHeight` | 60f | 控件根 RectTransform 最小行高 |
| anchor | (0, 1) | 强制左上角对齐（适应 VerticalLayoutGroup） |
| pivot | (0.5, 1) | X 轴居中，Y 轴顶部 |

### 标签对齐规则

EnforceLayout 遍历子 TMP_Text，匹配名称含 `Title` / `Label` / `Text` 的文本组件，应用：

```
LayoutElement.minWidth = 380f
LayoutElement.preferredWidth = 380f
LayoutElement.flexibleWidth = 0
alignment = TextAlignmentOptions.MidlineLeft
```

## Cloner 各自的行为

### ModToggleCloner

- **模板**：Audio Content 下的 `PomodoroSoundOnOffButtons`
- **按钮容器位置**：`(197.5, 0)`，垂直居中
- **额外处理**：销毁原生 `TextLocalizationBehaviour`，添加 `ModLocalizer`；基于 `InteractableUI` 组件实现 ON/OFF 视觉反馈

### ModPulldownCloner

- **模板**：Graphics Content 下的 `GraphicQualityPulldownList`
- **选项按钮**：从原模板的第一个选项按钮 Instantiate
- **ScrollRect**：选项 > 6 时动态创建 Viewport + ScrollRect 结构
- **渲染层级**：`PulldownLayerController` 在展开时将 Canvas sortingOrder 提升到 30000
- **MaxVisibleDropdownItems**：由 `Constants.MaxVisibleDropdownItems` 控制（默认 6）

### ModInputFieldCloner

- **模板**：Graphics Content 下的 `FrameRate` 行
- **清理**：销毁 `DeactiveFrameRate`、`ParentTitle` 多余节点，移除所有 MonoBehaviour 脚本
- **定位**：手动坐标放置。标签 `(-306, 40)`，输入框 `(40, 40)`，行高 50px

## 颜色说明

由于 UI 是克隆游戏原生模板，控件背景色、文本色、边框色都由游戏自己决定，不在本 MOD 的控制范围内。`EnforceLayout` 不修改任何颜色参数。

节标题（SectionHeader）由 `CreateSectionHeader()` 创建：
- 标题：`size=24`，粗体白色
- 版本号：`size=18`，`#888888`

## 分节与间距

- `CreateSectionHeader()` — 55px 高，BottomLeft 对齐
- `CreateDivider()` — 20px 高的透明间隔条
- 各节之间自动间隔没有额外的 margin/padding（由 VerticalLayoutGroup 的子元素提交流程决定）
