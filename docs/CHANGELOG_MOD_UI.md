# MOD设置UI - 更新日志

## v1.1 - 修复标签切换问题 (2025-01-02)

### 🐛 修复的问题

**问题1: MOD标签图层错误**
- **症状**: 点击其他标签(常规/图形/音频/制作人员)后,MOD标签的内容仍然显示,导致UI重叠
- **原因**: MOD标签只在点击MOD按钮时隐藏其他标签,但其他标签被点击时没有隐藏MOD标签
- **修复**: 通过Harmony钩子订阅游戏原生的`_settingService.SettingType`变化事件,当切换到其他标签时自动隐藏MOD标签

### 🔧 技术实现

#### 修改的文件
- `ModSettingsIntegration.cs`
  - 添加静态字段保存MOD UI引用
  - 新增`HookIntoTabSwitching()`方法
  - 使用反射订阅游戏的R3 Observable

#### 工作流程
```
用户点击"常规"标签
    ↓
游戏调用 _settingService.SelectSettingType(SettingType.General)
    ↓
_settingService.SettingType 触发变化事件
    ↓
游戏原生逻辑隐藏所有标签 → 显示"常规"标签
    ↓
我们的钩子检测到变化 → 隐藏MOD标签 ✅
```

### 📝 代码片段

关键实现:
```csharp
// 订阅游戏原生的标签切换事件
static void HookIntoTabSwitching(SettingUI settingUI)
{
    var settingService = AccessTools.Field(typeof(SettingUI), "_settingService").GetValue(settingUI);
    var settingTypeProperty = settingService.GetType().GetProperty("SettingType");
    var settingTypeObservable = settingTypeProperty.GetValue(settingService);
    
    // 创建回调:当标签切换时隐藏MOD标签
    System.Action<object> onTabChanged = (settingType) => {
        if (modContentParent != null && modContentParent.activeSelf)
        {
            modContentParent.SetActive(false);
            modInteractableUI?.DeactivateUseUI(false);
        }
    };
    
    // 动态订阅
    subscribeMethod.Invoke(settingTypeObservable, new object[] { onTabChanged });
}
```

### ✅ 测试清单

- [x] 点击MOD标签,其他标签隐藏
- [x] 点击常规标签,MOD标签隐藏
- [x] 点击图形标签,MOD标签隐藏
- [x] 点击音频标签,MOD标签隐藏
- [x] 点击制作人员标签,MOD标签隐藏
- [ ] MOD标签切换时滚动条重置到顶部
- [ ] 按钮视觉状态正确切换(高亮/非高亮)

### 🔮 下一步

**问题2: Toggle和Dropdown控件不生效**
- 需要研究游戏原生的Toggle/Dropdown实现
- 可能需要使用游戏的PulldownListUI组件而不是Unity原生Dropdown
- 需要绑定正确的事件和样式

---

## v1.0 - 初始版本

- ✅ 创建MOD标签页
- ✅ 克隆Credits标签UI
- ✅ 添加5个设置项
- ✅ BepInEx Config绑定
- ❌ 标签切换逻辑(已在v1.1修复)
