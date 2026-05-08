# 🎨 外部 MOD 样式改进指南

## 问题描述

现在外部 MOD (如 Chill Env Sync) 的设置可以正确显示，但样式与 iGPU Savior 的设置不一致：
- **文字样式不同** - 字体大小、颜色
- **按钮样式不同** - 背景色、边框、悬停效果
- **间距不同** - 行高、边距、间距

---

## 📐 统一的样式规范

### 行高度
```
标准行高: 72px
所有设置项应该保持一致的高度
```

### 间距
```
水平边距: 20px (左右)
竖直边距: 12px (上下)
行间距: 16px (VerticalLayoutGroup spacing)
```

### 文本样式

#### 标题文本
```
字体: TextMeshPro (TMP_FontAsset)
字体大小: 32
颜色: #CCCCCC (RGB: 0.8, 0.8, 0.8) - 浅灰色
对齐: 左对齐
加粗: 是 (可选)
```

#### 选项文本
```
字体: TextMeshPro (TMP_FontAsset)
字体大小: 28
颜色: #AAAAAA (RGB: 0.667, 0.667, 0.667) - 更浅的灰
对齐: 居中
```

### 按钮样式

#### 背景色
```
正常状态: #4A5568 (RGB: 0.29, 0.33, 0.41) - 深灰蓝
悬停状态: #5A6578 (RGB: 0.35, 0.40, 0.47) - 略亮
按下状态: #3A4558 (RGB: 0.23, 0.27, 0.35) - 略暗
禁用状态: #3A3A3A (RGB: 0.23, 0.23, 0.23) - 深灰
```

#### 边框
```
边框宽: 1-2px
边框颜色: 深色边框或无边框（取决于设计）
圆角半径: 4-6px
```

### 边距和填充
```
行内水平边距: 20px
行内竖直边距: 12px
容器内部填充: padding(32, 32, 24, 24)
子元素间距: 16px
```

---

## 🔧 改进建议

### 对 Chill Env Sync MOD 的建议

修改 `ModSettingsIntegration.cs` 中的 `AddToggleSafe()` 方法，在创建 Toggle 后应用统一样式：

```csharp
private bool AddToggleSafe(object managerInstance, System.Reflection.MethodInfo addToggleMethod,
    GameObject contentParent, string label, bool defaultValue, Action<bool> callback)
{
    try
    {
        Action<bool> safeCallback = (value) =>
        {
            try
            {
                callback?.Invoke(value);
            }
            catch (Exception ex)
            {
                ChillEnvPlugin.Log?.LogError($"❌ 设置 '{label}' 的回调异常: {ex.Message}");
            }
        };

        addToggleMethod.Invoke(managerInstance, new object[] {
            contentParent,
            label,
            defaultValue,
            safeCallback
        });

        // ✅ 获取刚添加的行元素
        var addedRow = contentParent.transform.GetChild(contentParent.transform.childCount - 1);
        if (addedRow != null)
        {
            ApplyUnifiedStyle(addedRow.gameObject);
        }

        ChillEnvPlugin.Log?.LogInfo($"✅ 已添加设置: '{label}'");
        return true;
    }
    catch (Exception ex)
    {
        ChillEnvPlugin.Log?.LogError($"❌ 添加设置 '{label}' 失败: {ex.Message}");
        return false;
    }
}

/// <summary>
/// 应用统一的样式到 MOD 设置行
/// </summary>
private void ApplyUnifiedStyle(GameObject rowObject)
{
    try
    {
        // 1. 设置行的高度
        var layoutElement = rowObject.GetComponent<LayoutElement>() ?? rowObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 72f;

        // 2. 设置行的边距
        var vGroup = rowObject.GetComponent<VerticalLayoutGroup>();
        if (vGroup != null)
        {
            vGroup.padding = new RectOffset(20, 20, 12, 12);
            vGroup.spacing = 12f;
        }

        // 3. 统一文本样式
        var textElements = rowObject.GetComponentsInChildren<TextMeshProUGUI>();
        foreach (var textElement in textElements)
        {
            if (textElement.text.Length > 0)
            {
                // 标题文本
                if (!textElement.text.Contains(":"))
                {
                    textElement.fontSize = 32;
                    textElement.color = new Color(0.8f, 0.8f, 0.8f, 1f);
                }
                // 其他文本（如 ON/OFF）
                else
                {
                    textElement.fontSize = 28;
                    textElement.color = new Color(0.667f, 0.667f, 0.667f, 1f);
                }
            }
        }

        // 4. 统一按钮样式
        var buttons = rowObject.GetComponentsInChildren<Button>();
        foreach (var button in buttons)
        {
            if (button != null && button.image != null)
            {
                // 设置按钮背景色
                button.image.color = new Color(0.29f, 0.33f, 0.41f, 1f);  // 深灰蓝
                
                // 设置按钮的 ColorBlock (悬停、按下等)
                var colors = button.colors;
                colors.normalColor = new Color(0.29f, 0.33f, 0.41f, 1f);
                colors.highlightedColor = new Color(0.35f, 0.40f, 0.47f, 1f);
                colors.pressedColor = new Color(0.23f, 0.27f, 0.35f, 1f);
                colors.disabledColor = new Color(0.23f, 0.23f, 0.23f, 1f);
                button.colors = colors;
            }
        }

        ChillEnvPlugin.Log?.LogInfo($"✅ 已应用统一样式到: {rowObject.name}");
    }
    catch (Exception ex)
    {
        ChillEnvPlugin.Log?.LogError($"❌ 应用样式失败: {ex.Message}");
    }
}
```

---

## 📋 样式检查清单

在完成样式改进后，检查以下项目：

- [ ] **文本对齐**
  - [ ] 标题文本左对齐
  - [ ] 选项文本居中对齐

- [ ] **字体大小**
  - [ ] 标题 32pt
  - [ ] 选项 28pt

- [ ] **颜色**
  - [ ] 标题文本 #CCCCCC
  - [ ] 选项文本 #AAAAAA
  - [ ] 按钮背景 #4A5568

- [ ] **间距**
  - [ ] 行高 72px
  - [ ] 行间距 16px
  - [ ] 边距 20px (左右), 12px (上下)

- [ ] **按钮交互**
  - [ ] 悬停效果正确
  - [ ] 按下效果正确
  - [ ] 禁用状态正确

- [ ] **整体外观**
  - [ ] 与 iGPU Savior 的设置视觉一致
  - [ ] 没有突兀的样式差异

---

## 🔄 集成流程

### 对于外部 MOD 开发者

1. **检查当前样式**
   ```
   查看 MOD 设置界面
   对比 iGPU Savior 和你的 MOD 的设置样式
   ```

2. **应用样式改进**
   ```
   参考上面的代码示例
   在创建 Toggle 后调用 ApplyUnifiedStyle()
   ```

3. **验证效果**
   ```
   重新编译 MOD
   打开游戏设置
   检查样式是否一致
   ```

4. **调整细节**
   ```
   如果还有差异，微调颜色/大小值
   反复测试直到满意
   ```

---

## 📊 对比参考

### 现状
```
iGPU Savior 设置：蓝灰色按钮, 32pt 文本
外部 MOD 设置：其他颜色和大小
外观不统一 ❌
```

### 目标
```
iGPU Savior 设置：蓝灰色按钮, 32pt 文本
外部 MOD 设置：相同的蓝灰色, 相同的 32pt 文本
外观统一 ✅
```

---

## 💡 其他建议

1. **提供一个公共样式库**
   - 在 ModSettingsManager 中创建静态方法
   - 所有外部 MOD 都可以调用

2. **提供预制件 (Prefab)**
   - 创建一个标准的 Toggle 预制件
   - 外部 MOD 可以直接使用

3. **提供配置文件**
   - 定义标准的颜色、字体大小等
   - 允许外部 MOD 读取并应用

---

**版本**: v1.0  
**日期**: 2025-12-04  
**针对**: 外部 MOD 开发者（Chill Env Sync 等）  
**状态**: 指南完成 ✅

