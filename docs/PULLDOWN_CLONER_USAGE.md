# ModPulldownCloner 使用指南

## 概述

`ModPulldownCloner` 提供了克隆游戏原生下拉框UI的功能,可以复用游戏的UI样式来创建MOD设置。

## 功能特性

1. **克隆游戏原生下拉框** - 完整复制游戏的图形质量下拉框
2. **清空原有选项** - 自动清理原版的选项按钮
3. **添加自定义选项** - 使用模板创建新的选项按钮
4. **事件绑定** - 将选项点击事件绑定到ConfigEntry

## 使用示例

### 基础用法

在 `ModSettingsIntegration.cs` 的 `Postfix` 方法中添加:

```csharp
static void Postfix(SettingUI __instance)
{
    try
    {
        // ... 现有代码 ...
        
        // 使用克隆器创建下拉框
        CreateClonedPulldown();
    }
    catch (System.Exception e)
    {
        PotatoPlugin.Log.LogError($"MOD设置集成失败: {e.Message}");
    }
}

static void CreateClonedPulldown()
{
    // 1. 克隆下拉框并清空原有选项
    GameObject pulldownClone = ModPulldownCloner.CloneAndClearPulldown();
    if (pulldownClone == null)
    {
        PotatoPlugin.Log.LogError("克隆下拉框失败");
        return;
    }

    // 2. 获取 SelectButton 模板
    GameObject buttonTemplate = ModPulldownCloner.GetSelectButtonTemplate();
    if (buttonTemplate == null)
    {
        PotatoPlugin.Log.LogError("获取 SelectButton 模板失败");
        Object.Destroy(pulldownClone);
        return;
    }

    // 3. 添加自定义选项
    ModPulldownCloner.AddOption(pulldownClone, buttonTemplate, "Ctrl + 左键", () =>
    {
        PotatoPlugin.Log.LogInfo("选择了: Ctrl + 左键");
        PotatoPlugin.CfgDragMode.Value = DragMode.Ctrl_LeftClick;
    });

    ModPulldownCloner.AddOption(pulldownClone, buttonTemplate, "Alt + 左键", () =>
    {
        PotatoPlugin.Log.LogInfo("选择了: Alt + 左键");
        PotatoPlugin.CfgDragMode.Value = DragMode.Alt_LeftClick;
    });

    ModPulldownCloner.AddOption(pulldownClone, buttonTemplate, "右键按住", () =>
    {
        PotatoPlugin.Log.LogInfo("选择了: 右键按住");
        PotatoPlugin.CfgDragMode.Value = DragMode.RightClick_Hold;
    });

    // 4. 挂载到设置界面
    ModPulldownCloner.MountPulldown(pulldownClone, "Graphics/ScrollView/Viewport/Content");

    // 5. 清理模板
    Object.Destroy(buttonTemplate);
    
    PotatoPlugin.Log.LogInfo("克隆下拉框创建成功!");
}
```

### 与 ConfigEntry 双向绑定

```csharp
static void CreateClonedPulldown()
{
    GameObject pulldownClone = ModPulldownCloner.CloneAndClearPulldown();
    GameObject buttonTemplate = ModPulldownCloner.GetSelectButtonTemplate();
    
    if (pulldownClone == null || buttonTemplate == null) return;

    // 选项定义
    var options = new[]
    {
        ("Ctrl + 左键", DragMode.Ctrl_LeftClick),
        ("Alt + 左键", DragMode.Alt_LeftClick),
        ("右键按住", DragMode.RightClick_Hold)
    };

    // 添加选项并绑定到ConfigEntry
    foreach (var (text, mode) in options)
    {
        ModPulldownCloner.AddOption(pulldownClone, buttonTemplate, text, () =>
        {
            PotatoPlugin.CfgDragMode.Value = mode;
            PotatoPlugin.Log.LogInfo($"拖动模式已更改为: {text}");
        });
    }

    // 设置初始选中项
    var pulldownUI = pulldownClone.GetComponent<Bulbul.PulldownListUI>();
    if (pulldownUI != null)
    {
        int currentIndex = (int)PotatoPlugin.CfgDragMode.Value;
        if (currentIndex < options.Length)
        {
            pulldownUI.ChangeSelectContentText(options[currentIndex].Item1);
        }
    }

    ModPulldownCloner.MountPulldown(pulldownClone, "Graphics/ScrollView/Viewport/Content");
    Object.Destroy(buttonTemplate);
}
```

## API 参考

### CloneAndClearPulldown()
克隆图形质量下拉框并清空原有选项。

**返回值:** `GameObject` - 克隆后的下拉框(已禁用)

### GetSelectButtonTemplate()
获取 SelectButton 模板用于创建新选项。

**返回值:** `GameObject` - SelectButton模板(已禁用)

### AddOption(pulldownClone, buttonTemplate, optionText, onClick)
在克隆的下拉框中添加一个新选项。

**参数:**
- `pulldownClone` - 克隆的下拉框GameObject
- `buttonTemplate` - SelectButton模板
- `optionText` - 选项显示文本
- `onClick` - 点击时的回调函数

### MountPulldown(pulldownClone, parentPath)
将克隆的下拉框挂载到设置界面。

**参数:**
- `pulldownClone` - 克隆的下拉框GameObject
- `parentPath` - 父物体路径,例如 "Graphics/ScrollView/Viewport/Content"

## 注意事项

1. **游戏场景依赖** - 必须在游戏的设置界面 (UI_FacilitySetting) 加载后才能使用
2. **清理模板** - 使用完 SelectButton 模板后记得销毁以释放内存
3. **事件清理** - AddOption 会自动清空原有的点击事件,避免调用游戏原逻辑
4. **路径正确性** - 确保挂载路径存在,否则会失败

## 故障排查

### 错误: "未找到 UI_FacilitySetting"
- **原因:** 设置界面尚未加载
- **解决:** 在 `SettingUI.Setup()` Postfix 中调用

### 错误: "未找到 GraphicQualityPulldownList"
- **原因:** 游戏UI结构已改变
- **解决:** 使用 UnityExplorer 检查实际路径

### 选项文本未显示
- **原因:** TMP_Text 组件未找到
- **解决:** 检查 SelectButton 的子物体结构
