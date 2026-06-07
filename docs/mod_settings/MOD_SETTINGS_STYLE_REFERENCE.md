# MOD 设置 UI 样式参考

## 原生样式适配机制

MOD 设置页的 UI 由三个层次共同决定样式：

1. **克隆 General 页面** — MOD 内容页继承原版设置页的标题、背景、滚动区域和遮罩
2. **克隆原生设置行** — 开关、下拉框和输入框继承游戏的材质、shader、字体和控件尺寸
3. **ModSettingsStyle 适配** — 只统一动态列表所需的行高、横向拉伸、标题宽度和间距

不要在克隆后删除整行的本地化组件或手动摆放控件。原生模板结构发生变化时，应优先改模板查找与适配逻辑。

## 页面布局

- 内容页模板：`SettingUI._generalParent`
- 列表间距：4px
- 列表内边距：顶部 8px、右侧 16px、底部 24px
- Viewport 顶部内缩 56px，内容始终裁切在标题和 BackLine 下方
- 标签栏：所有原版标签和 MOD 标签等宽自适应
- 原版初始化按钮：在 MOD 页面隐藏
- 分节标题：64px 高，使用原版字体/材质并带低透明度分隔线

`ModSettingsStyle.PrepareRow()` 优先保留控件已有的 `LayoutElement.preferredHeight`，否则使用原生 RectTransform 高度，最后才回退到 60.9px。
所有动态行、分节标题和 Divider 都必须从创建时使用 `RectTransform`，并设置 `LayoutElement.ignoreLayout = false`，保证视觉顺序与 sibling 顺序一致。

### 标题适配

- 优先查找直接子节点 `TitleText`
- 排除按钮内部文本，避免误改 ON/OFF 或下拉选项
- 最小宽度 360px
- 单行显示，过长时使用省略号
- MOD 文案使用 `ModLocalizer`

## Cloner 各自的行为

### ModToggleCloner

- **模板查找**：依次扫描 General、Graphics、MusicAudio，选择按钮名含 `OnButton` / `OffButton` 的原生设置行
- **本地化**：只替换标题的 `TextLocalizationBehaviour`；保留 ON/OFF 按钮自己的原版本地化组件
- **布局**：保留模板内部结构，不销毁布局组件、不写死坐标
- **视觉反馈**：继续使用原版 `InteractableUI` 激活/反激活效果

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

由于 UI 是克隆游戏原生模板，控件背景色、文本色、边框色都由游戏自己决定。`ModSettingsStyle` 不修改设置行的颜色。

节标题（SectionHeader）由 `CreateSectionHeader()` 创建：
- 标题：`size=24`，使用原版标题字体和材质
- 版本号：`size=18`，`#888888`
- 分隔线：标题色，18% 透明度

## 分节与间距

- `CreateSectionHeader()` — 64px 高，BottomLeft 对齐
- `CreateDivider()` — 12px 高的透明间隔条
- 设置行之间由内容区 `VerticalLayoutGroup` 提供 4px 间距
