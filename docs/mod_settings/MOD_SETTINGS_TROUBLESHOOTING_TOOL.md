# 🔧 MOD 设置集成 - 故障排查工具

如果你的 MOD 按照文档集成但设置项没有显示，使用这个诊断工具来排查问题。

---

## 🚨 常见问题诊断

### 问题 1: 设置项完全不显示

**诊断代码**:
```csharp
void DiagnoseSettingsNotShowing()
{
    var mgr = ModSettingsManager.Instance;
    
    // 检查 1: 管理器是否存在
    if (mgr == null)
    {
        Debug.LogError("❌ ModSettingsManager.Instance 为 null");
        return;
    }
    Debug.Log("✅ ModSettingsManager.Instance 存在");
    
    // 检查 2: 是否已初始化
    if (!mgr.IsInitialized)
    {
        Debug.LogError("❌ ModSettingsManager 未初始化");
        return;
    }
    Debug.Log("✅ ModSettingsManager 已初始化");
    
    // 检查 3: ModContentParent 是否有效
    if (mgr.ModContentParent == null)
    {
        Debug.LogError("❌ ModContentParent 为 null");
        return;
    }
    Debug.Log($"✅ ModContentParent 存在: {mgr.ModContentParent.name}");
    
    // 检查 4: ModContentParent 是否 active
    if (!mgr.ModContentParent.activeSelf)
    {
        Debug.LogError("❌ ModContentParent 未激活 (activeSelf == false)");
        return;
    }
    Debug.Log("✅ ModContentParent 已激活");
    
    // 检查 5: ScrollRect 是否存在
    var scrollRect = mgr.ModScrollRect;
    if (scrollRect == null)
    {
        Debug.LogError("❌ ModScrollRect 为 null");
        return;
    }
    Debug.Log("✅ ModScrollRect 存在");
    
    // 检查 6: ScrollRect.content 是否有效
    if (scrollRect.content == null)
    {
        Debug.LogError("❌ ScrollRect.content 为 null");
        return;
    }
    Debug.Log($"✅ ScrollRect.content 存在: {scrollRect.content.name}");
    
    // 检查 7: ScrollRect.content 的子物体数
    int childCount = scrollRect.content.childCount;
    Debug.Log($"ℹ️  ScrollRect.content 当前有 {childCount} 个子物体");
    
    Debug.Log("✅ 所有诊断检查通过！问题可能在你的代码中。");
}
```

**如何使用**:
1. 在你的 MOD 中添加上述代码
2. 在 Awake/Start 中调用 `DiagnoseSettingsNotShowing()`
3. 查看 Unity 控制台的输出
4. 找到第一个 ❌ 的信息，根据下面的解决方案修复

---

## 🔍 详细排查步骤

### 步骤 1: 验证初始化时序

**症状**: `ModSettingsManager 未初始化`

**原因**: 你的 MOD 的初始化代码在 iGPU Savior 之前执行

**解决方案**:
```csharp
// ❌ 错误
void Awake()
{
    AddSettings();  // 此时 ModSettingsManager 还没初始化！
}

// ✅ 正确
void Start()
{
    StartCoroutine(WaitForInitialization());
}

IEnumerator WaitForInitialization()
{
    // 等待一帧
    yield return null;
    
    // 等待初始化（最多 5 秒）
    float timeout = 0f;
    while (ModSettingsManager.Instance?.IsInitialized != true && timeout < 5f)
    {
        yield return new WaitForSeconds(0.1f);
        timeout += 0.1f;
    }
    
    if (ModSettingsManager.Instance?.IsInitialized == true)
    {
        AddSettings();
    }
    else
    {
        Debug.LogError("❌ ModSettingsManager 初始化超时，可能 iGPU Savior 未加载");
    }
}
```

---

### 步骤 2: 验证容器参数

**症状**: 设置项提交了但显示在错误位置

**原因**: 传入了错误的 `parent` 参数

**诊断代码**:
```csharp
void DebugContainerInfo()
{
    var mgr = ModSettingsManager.Instance;
    if (mgr?.IsInitialized != true) return;
    
    Debug.Log("=== MOD 容器信息 ===");
    Debug.Log($"ModContentParent: {mgr.ModContentParent}");
    Debug.Log($"ModContentParent.name: {mgr.ModContentParent.name}");
    Debug.Log($"ModContentParent.activeSelf: {mgr.ModContentParent.activeSelf}");
    Debug.Log($"ModContentParent.activeInHierarchy: {mgr.ModContentParent.activeInHierarchy}");
    
    var scrollRect = mgr.ModScrollRect;
    if (scrollRect != null)
    {
        Debug.Log($"ScrollRect.content: {scrollRect.content}");
        Debug.Log($"ScrollRect.content.childCount: {scrollRect.content.childCount}");
    }
}
```

**常见错误**:
```csharp
// ❌ 错误：传入了 ModTabButton（按钮）
mgr.AddToggle(mgr.ModTabButton, "Test", true, null);

// ❌ 错误：创建了自己的容器
var myContainer = new GameObject("MySettings");
mgr.AddToggle(myContainer, "Test", true, null);

// ✅ 正确：始终用 ModContentParent
mgr.AddToggle(mgr.ModContentParent, "Test", true, null);
```

---

### 步骤 3: 验证布局重建

**症状**: 设置项添加了但位置不对或重叠

**原因**: UI 布局没有重新计算

**解决方案**:
```csharp
void AddSettingsWithProperLayout()
{
    var mgr = ModSettingsManager.Instance;
    var parent = mgr.ModContentParent;
    
    // 添加设置
    mgr.AddToggle(parent, "测试1", true, null);
    mgr.AddToggle(parent, "测试2", false, null);
    
    // ✅ 关键：强制重建布局
    StartCoroutine(RebuildLayoutNextFrame());
}

IEnumerator RebuildLayoutNextFrame()
{
    yield return null;  // 等待 UI 元素创建
    
    var mgr = ModSettingsManager.Instance;
    var rectTransform = mgr.ModContentParent.GetComponent<RectTransform>();
    
    // 强制重建布局
    LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    
    Debug.Log("✅ 布局已重建");
}
```

---

### 步骤 4: 验证回调函数

**症状**: 设置项显示了但改变时没有反应

**原因**: 回调函数签名错误或异常

**诊断代码**:
```csharp
// ❌ 错误：Toggle 回调应该接收 bool，不是 string
mgr.AddToggle(parent, "Test", true, 
    (value) => Debug.Log($"New: {value}")  // ✅ 这是对的
);

// ❌ 错误：Dropdown 回调应该接收 int（索引），不是 string
mgr.AddDropdown(parent, "Choose", 
    new List<string> { "A", "B" }, 0,
    (index) => Debug.Log($"Selected: {index}")  // ✅ 这是对的
);
```

**带异常处理的回调**:
```csharp
mgr.AddToggle(parent, "测试", true,
    (value) =>
    {
        try
        {
            Debug.Log($"设置已改变: {value}");
            // 你的逻辑代码
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ 回调异常: {e.Message}\n{e.StackTrace}");
        }
    }
);
```

---

## 📋 完整诊断脚本

复制这个脚本到你的 MOD，调用 `FullDiagnostics()` 来获得完整的诊断报告：

```csharp
using ModShared;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ModSettingsDiagnostics : MonoBehaviour
{
    public void FullDiagnostics()
    {
        Debug.Log("=== 开始 MOD 设置诊断 ===");
        
        DiagnoseManager();
        DiagnoseInitialization();
        DiagnoseContainers();
        DiagnoseScrollRect();
        DiagnoseChildren();
        DiagnoseHierarchy();
        
        Debug.Log("=== 诊断完成 ===");
    }
    
    void DiagnoseManager()
    {
        Debug.Log("\n📍 检查: ModSettingsManager");
        
        var mgr = ModSettingsManager.Instance;
        if (mgr == null)
        {
            Debug.LogError("❌ ModSettingsManager.Instance 为 null");
            return;
        }
        Debug.Log("✅ ModSettingsManager.Instance 存在");
        Debug.Log($"   类型: {mgr.GetType().Name}");
        Debug.Log($"   GameObject: {mgr.gameObject.name}");
    }
    
    void DiagnoseInitialization()
    {
        Debug.Log("\n📍 检查: 初始化状态");
        
        var mgr = ModSettingsManager.Instance;
        if (mgr == null) return;
        
        Debug.Log($"   IsInitialized: {mgr.IsInitialized}");
        if (!mgr.IsInitialized)
        {
            Debug.LogError("❌ ModSettingsManager 未初始化");
        }
    }
    
    void DiagnoseContainers()
    {
        Debug.Log("\n📍 检查: 容器");
        
        var mgr = ModSettingsManager.Instance;
        if (mgr == null || !mgr.IsInitialized) return;
        
        var parent = mgr.ModContentParent;
        var button = mgr.ModTabButton;
        
        Debug.Log($"   ModContentParent: {(parent ? "✅ 存在" : "❌ 为 null")}");
        if (parent)
        {
            Debug.Log($"      name: {parent.name}");
            Debug.Log($"      activeSelf: {parent.activeSelf}");
            Debug.Log($"      activeInHierarchy: {parent.activeInHierarchy}");
        }
        
        Debug.Log($"   ModTabButton: {(button ? "✅ 存在" : "❌ 为 null")}");
        if (button)
        {
            Debug.Log($"      name: {button.name}");
            Debug.Log($"      activeSelf: {button.activeSelf}");
        }
    }
    
    void DiagnoseScrollRect()
    {
        Debug.Log("\n📍 检查: ScrollRect");
        
        var mgr = ModSettingsManager.Instance;
        if (mgr == null || !mgr.IsInitialized) return;
        
        var sr = mgr.ModScrollRect;
        Debug.Log($"   ModScrollRect: {(sr ? "✅ 存在" : "❌ 为 null")}");
        
        if (sr && sr.content)
        {
            Debug.Log($"      content: {sr.content.name}");
            Debug.Log($"      content.childCount: {sr.content.childCount}");
            Debug.Log($"      verticalNormalizedPosition: {sr.verticalNormalizedPosition}");
        }
    }
    
    void DiagnoseChildren()
    {
        Debug.Log("\n📍 检查: 子物体");
        
        var mgr = ModSettingsManager.Instance;
        if (mgr == null || !mgr.IsInitialized) return;
        
        var content = mgr.ModScrollRect?.content;
        if (content == null)
        {
            Debug.LogWarning("⚠️  content 为 null，无法列举子物体");
            return;
        }
        
        Debug.Log($"   ScrollRect.content 有 {content.childCount} 个子物体:");
        for (int i = 0; i < content.childCount; i++)
        {
            var child = content.GetChild(i);
            Debug.Log($"      [{i}] {child.name} (active: {child.gameObject.activeSelf})");
        }
    }
    
    void DiagnoseHierarchy()
    {
        Debug.Log("\n📍 检查: 层级结构");
        
        var mgr = ModSettingsManager.Instance;
        if (mgr == null || !mgr.IsInitialized) return;
        
        var parent = mgr.ModContentParent;
        if (parent == null) return;
        
        Debug.Log($"   ModContentParent 层级:");
        Debug.Log($"      {parent.name}");
        Debug.Log($"      ├─ parent: {(parent.parent ? parent.parent.name : "null")}");
        
        var sr = parent.GetComponentInChildren<ScrollRect>();
        if (sr)
        {
            Debug.Log($"      └─ ScrollRect: ✅ 存在");
        }
    }
    
    // 使用示例
    public void TestAddToggle()
    {
        var mgr = ModSettingsManager.Instance;
        if (mgr?.IsInitialized != true)
        {
            Debug.LogError("❌ 未初始化");
            return;
        }
        
        Debug.Log("📝 测试添加 Toggle...");
        mgr.AddToggle(
            mgr.ModContentParent,
            "诊断测试开关",
            true,
            (value) => Debug.Log($"✅ Toggle 改变: {value}")
        );
        
        // 强制重建布局
        StartCoroutine(RebuildLayout());
    }
    
    IEnumerator RebuildLayout()
    {
        yield return null;
        
        var mgr = ModSettingsManager.Instance;
        if (mgr?.ModContentParent != null)
        {
            var rect = mgr.ModContentParent.GetComponent<RectTransform>();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            Debug.Log("✅ 布局已重建");
        }
    }
}
```

**使用**:
```csharp
// 在你的 MOD 中
var diagnostics = gameObject.AddComponent<ModSettingsDiagnostics>();
diagnostics.FullDiagnostics();
diagnostics.TestAddToggle();
```

---

## 🔧 快速修复清单

| 问题 | 修复 |
|------|------|
| `ModSettingsManager.Instance` 为 null | ✅ 确保 iGPU Savior 已加载且在你的 MOD 之前 |
| `IsInitialized` 为 false | ✅ 延迟初始化，使用 Coroutine 等待 |
| 设置项不显示 | ✅ 检查 `ModContentParent` 是否 active，强制重建布局 |
| 设置项显示位置错误 | ✅ 确保传入的是 `ModContentParent`，不是其他容器 |
| 回调不触发 | ✅ 检查回调签名（bool 或 int），添加 try-catch 捕获异常 |
| UI 重叠或错位 | ✅ 调用 `LayoutRebuilder.ForceRebuildLayoutImmediate()` |

---

## 💡 提示

如果诊断显示所有检查都通过（✅），但设置项仍然不显示，可能是：

1. **检查游戏实际运行的代码** - 确保你编译后的 DLL 包含最新代码
2. **检查 BepInEx 日志** - 查看是否有其他错误或异常
3. **检查加载顺序** - 在 BepInEx.cfg 中验证插件加载顺序
4. **检查其他 MOD 冲突** - 临时禁用其他 MOD 测试

---

**如果问题依然存在，请提供诊断报告的完整输出！** 📋
