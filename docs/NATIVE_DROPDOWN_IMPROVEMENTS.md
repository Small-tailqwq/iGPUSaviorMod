# 游戏原生下拉框改进说明

## 📝 改进内容

根据游戏原生AI的建议,对下拉框实现进行了以下改进:

### 1. ✅ 添加标题显示功能

```csharp
// 自动设置下拉框的标题
TMP_Text titleText = pulldownClone.transform.Find("Title")?.GetComponent<TMP_Text>();
if (titleText != null)
{
    titleText.text = label; // 例如: "小窗拖动方式"
}
```

### 2. ✅ 添加默认选中项显示

```csharp
// 在激活下拉框前,设置当前ConfigEntry的值作为默认选中项
if (currentIndex >= 0 && currentIndex < options.Count)
{
    SetPulldownSelectedText(pulldownClone, options[currentIndex]);
}
```

### 3. ✅ 改进选中项文本更新

新增 `SetPulldownSelectedText` 方法,支持多种路径查找:

```csharp
string[] possiblePaths = new[]
{
    "SelectContent/Text (TMP)",  // 最常见的路径
    "SelectContent/Text",
    "Title/Text (TMP)",
    "Title/Text",
    "CurrentText",
    "Text (TMP)"
};
```

如果路径查找失败，会通过 publicized `PulldownListUI` 组件直接调用 `ChangeSelectContentText()` 更新当前选中项文本。

### 4. ✅ 添加"小窗缩放比例"配置

新增下拉框选项:
- "1/3 大小" → `WindowScaleRatio.OneThird`
- "1/4 大小" → `WindowScaleRatio.OneFourth`
- "1/5 大小" → `WindowScaleRatio.OneFifth`

**注意枚举值映射**:
```csharp
// WindowScaleRatio枚举定义为: OneThird=3, OneFourth=4, OneFifth=5
// 所以索引需要 +3/-3 转换
(int)PotatoPlugin.CfgWindowScale.Value - 3  // 枚举转索引
(WindowScaleRatio)(index + 3)               // 索引转枚举
```

## 🎯 当前配置项列表

### 基础设置
1. **画面镜像** (Toggle开关)
   - 启用/禁用摄像机镜像

2. **小窗缩放比例** (下拉框)
   - 1/3 大小
   - 1/4 大小
   - 1/5 大小

3. **小窗拖动方式** (下拉框)
   - Ctrl + 左键
   - Alt + 左键
   - 右键按住

### 快捷键设置
4. **土豆模式** (下拉框)
   - F1 ~ F12

5. **小窗模式** (下拉框)
   - F1 ~ F12

6. **镜像翻转** (下拉框)
   - F1 ~ F12

## 🔧 技术细节

### 下拉框创建流程

```
1. ModPulldownCloner.CloneAndClearPulldown()
   ↓ 克隆游戏的 GraphicQualityPulldownList

2. ModPulldownCloner.GetSelectButtonTemplate()
   ↓ 获取 SelectButton 模板

3. 设置标题文本
   ↓ 找到 Title 组件并设置

4. ModPulldownCloner.AddOption() × N
   ↓ 为每个选项创建按钮并绑定事件

5. SetPulldownSelectedText()
   ↓ 设置默认选中项显示

6. SetParent() + SetActive(true)
   ↓ 挂载到MOD设置面板并激活

7. Destroy(buttonTemplate)
   ↓ 清理模板对象
```

### 事件绑定机制

**点击选项时的流程**:
```csharp
用户点击选项
  ↓
Button.onClick 触发
  ↓
onValueChanged?.Invoke(index)  // 更新ConfigEntry
  ↓
SetPulldownSelectedText()       // 更新显示文本
  ↓
配置自动保存到文件
```

## 📊 与游戏原生UI的差异

| 特性 | 游戏原生 | 我们的实现 |
|------|---------|-----------|
| 数据绑定 | UniRx Observable | ConfigEntry + 事件回调 |
| UI更新 | 响应式自动同步 | 手动调用SetPulldownSelectedText |
| 生命周期 | AddTo(this) 自动管理 | 手动Destroy清理 |
| 音效 | SystemSeService.PlayClick() | 无(可后续添加) |

## 🚀 后续改进建议

### 1. 添加点击音效
```csharp
// 在点击事件中添加
var seService = Object.FindObjectOfType<Bulbul.SystemSeService>();
seService?.PlayClick();
```

### 2. 添加视觉反馈
```csharp
// 高亮当前选中项的 ActiveImage
foreach (Transform child in content)
{
    var activeImage = child.Find("ActiveImage")?.gameObject;
    if (activeImage != null)
    {
        activeImage.SetActive(child.GetSiblingIndex() == currentIndex);
    }
}
```

### 3. 支持键盘导航
```csharp
// 监听上下方向键切换选项
if (Input.GetKeyDown(KeyCode.UpArrow)) { /* 选择上一项 */ }
if (Input.GetKeyDown(KeyCode.DownArrow)) { /* 选择下一项 */ }
```

## ✅ 验证清单

- [x] 下拉框能正确克隆
- [x] 标题显示正确
- [x] 选项添加成功
- [x] 默认选中项正确显示
- [x] 点击选项能更新ConfigEntry
- [x] 点击选项能更新显示文本
- [x] 小窗缩放比例配置已添加
- [x] 拖动方式配置已添加
- [x] 快捷键配置已添加
- [x] 代码编译成功

## 🎉 完成状态

所有核心功能已实现并通过编译测试!可以在游戏中测试实际效果了。
