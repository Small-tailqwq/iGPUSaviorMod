# 🐛 MOD 设置集成 - 快速问题诊断

如果你的 MOD 按照文档但设置项没有显示，按照本指南逐步排查。

---

## ❓ 症状诊断

### 症状 A: 完全没有看到设置项

#### 原因可能是:
1. **iGPU Savior MOD 未加载**
2. **你的 MOD 初始化太早** (ModSettingsManager 还没准备好)
3. **传入了错误的 parent 参数**

#### 快速修复:
```csharp
// ❌ 旧的 (不安全)
void Start()
{
    mgr.AddToggle(parent, "test", true, null);
}

// ✅ 新的 (安全)
void Start()
{
    StartCoroutine(SafeAddToggle());
}

IEnumerator SafeAddToggle()
{
    yield return null;  // 等待一帧
    
    var mgr = ModSettingsManager.Instance;
    if (mgr?.IsInitialized == true)
    {
        mgr.AddToggle(mgr.ModContentParent, "test", true, null);
        
        // 强制重建布局
        var rect = mgr.ModContentParent.GetComponent<RectTransform>();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }
    else
    {
        Debug.LogError("❌ ModSettingsManager 未就绪!");
    }
}
```

---

### 症状 B: 看到设置项但位置错误/重叠

#### 原因可能是:
1. **没有强制重建 UI 布局**
2. **RectTransform 配置不对**

#### 快速修复:
```csharp
void AddMySettings()
{
    var mgr = ModSettingsManager.Instance;
    
    // 添加设置
    mgr.AddToggle(mgr.ModContentParent, "功能 1", true, OnToggle1);
    mgr.AddToggle(mgr.ModContentParent, "功能 2", false, OnToggle2);
    
    // ✅ 关键：强制重建布局
    StartCoroutine(RebuildUI());
}

IEnumerator RebuildUI()
{
    yield return null;  // 等待元素创建
    
    var mgr = ModSettingsManager.Instance;
    var rect = mgr.ModContentParent.GetComponent<RectTransform>();
    
    // 强制重建
    LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
}
```

---

### 症状 C: 回调函数没被触发

#### 原因可能是:
1. **回调函数签名错误**
2. **回调函数抛出异常** (被吞掉了)

#### 快速修复:
```csharp
// ❌ 错误的回调
mgr.AddToggle(parent, "test", true,
    () => Debug.Log("Clicked")  // 错误！应该有 bool 参数
);

// ✅ 正确的回调
mgr.AddToggle(parent, "test", true,
    (isOn) => Debug.Log($"Value: {isOn}")  // 正确！
);

// ✅ 添加异常处理
mgr.AddDropdown(parent, "choose",
    new List<string> { "A", "B", "C" }, 0,
    (index) =>
    {
        try
        {
            Debug.Log($"Selected: {index}");
            // 你的代码
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ 异常: {ex.Message}");
        }
    }
);
```

---

## 📋 逐步排查清单

### 第 1 步：验证 ModSettingsManager 存在

在 Unity 控制台运行：
```csharp
Debug.Log(ModSettingsManager.Instance != null ? "✅ 存在" : "❌ 不存在");
```

**如果显示 ❌**: 
- 确保 iGPU Savior 已安装
- 确保 iGPU Savior 已启用 (BepInEx 配置)
- 重启游戏

---

### 第 2 步：验证初始化状态

在 Start() 中添加：
```csharp
IEnumerator CheckInit()
{
    for (int i = 0; i < 50; i++)  // 最多等待 5 秒
    {
        if (ModSettingsManager.Instance?.IsInitialized == true)
        {
            Debug.Log("✅ ModSettingsManager 已初始化");
            break;
        }
        yield return new WaitForSeconds(0.1f);
    }
}
```

**如果超时**:
- iGPU Savior 可能没有正确初始化
- 检查 BepInEx 日志 (`LogOutput.log`)

---

### 第 3 步：验证容器参数

添加调试代码：
```csharp
var mgr = ModSettingsManager.Instance;
if (mgr?.IsInitialized == true)
{
    var parent = mgr.ModContentParent;
    Debug.Log($"ModContentParent: {parent}");
    Debug.Log($"ModContentParent.name: {parent.name}");
    Debug.Log($"ModContentParent.activeSelf: {parent.activeSelf}");
    
    // 添加一个测试元素
    mgr.AddToggle(parent, "[测试]", true, 
        (v) => Debug.Log($"测试回调: {v}"));
}
```

**如果 ModContentParent 为 null**:
- iGPU Savior 的 MOD 标签页未创建
- 检查是否在打开设置界面时才能访问

---

### 第 4 步：验证布局重建

关键代码：
```csharp
// 添加设置后，必须调用这个
LayoutRebuilder.ForceRebuildLayoutImmediate(
    mgr.ModContentParent.GetComponent<RectTransform>()
);
```

---

### 第 5 步：检查游戏日志

打开 `BepInEx/LogOutput.log`，搜索：
```
[ModSettings]
[Potato Mode Optimization]
```

查找错误信息，例如：
- `Failed to apply Harmony patches`
- `Mod Settings tab initialized`
- 你自己的 MOD 的日志

---

## 🛠️ 完整的"保险"初始化代码

复制这段代码到你的 MOD，确保 100% 工作：

```csharp
using BepInEx;
using UnityEngine;
using System.Collections;
using ModShared;

[BepInPlugin("example.yourmod", "Your MOD", "1.0.0")]
public class YourMod : BaseUnityPlugin
{
    public static ManualLogSource Log;
    private bool _settingsAdded = false;

    void Awake()
    {
        Log = Logger;
        Log.LogInfo("你的 MOD 已加载");
    }

    void Start()
    {
        StartCoroutine(InitializeSettings());
    }

    IEnumerator InitializeSettings()
    {
        // 步骤 1: 等待一帧，让其他系统初始化
        yield return null;
        
        // 步骤 2: 轮询等待 ModSettingsManager 准备好
        float waitTime = 0f;
        while (ModSettingsManager.Instance == null && waitTime < 5f)
        {
            yield return new WaitForSeconds(0.1f);
            waitTime += 0.1f;
        }
        
        if (ModSettingsManager.Instance == null)
        {
            Log.LogError("❌ 无法找到 ModSettingsManager (iGPU Savior 未加载?)");
            yield break;
        }
        
        // 步骤 3: 等待初始化完成
        waitTime = 0f;
        while (!ModSettingsManager.Instance.IsInitialized && waitTime < 5f)
        {
            yield return new WaitForSeconds(0.1f);
            waitTime += 0.1f;
        }
        
        if (!ModSettingsManager.Instance.IsInitialized)
        {
            Log.LogError("❌ ModSettingsManager 初始化失败");
            yield break;
        }
        
        Log.LogInfo("✅ ModSettingsManager 已准备好，开始添加设置");
        
        // 步骤 4: 添加你的设置
        AddSettings();
        
        // 步骤 5: 强制重建 UI 布局
        yield return null;  // 等待一帧让 UI 元素创建
        
        var rect = ModSettingsManager.Instance.ModContentParent.GetComponent<RectTransform>();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        
        _settingsAdded = true;
        Log.LogInfo("✅ 设置项已添加");
    }

    void AddSettings()
    {
        var mgr = ModSettingsManager.Instance;
        if (mgr == null || !mgr.IsInitialized)
        {
            Log.LogError("❌ 无法添加设置：管理器未就绪");
            return;
        }

        var parent = mgr.ModContentParent;
        
        try
        {
            // 添加开关
            mgr.AddToggle(
                parent,
                "启用我的功能",
                true,
                (isEnabled) =>
                {
                    try
                    {
                        Log.LogInfo($"功能状态: {isEnabled}");
                        // 你的逻辑代码
                    }
                    catch (System.Exception ex)
                    {
                        Log.LogError($"❌ 回调异常: {ex}");
                    }
                }
            );
            
            // 添加下拉菜单
            mgr.AddDropdown(
                parent,
                "选择模式",
                new System.Collections.Generic.List<string> { "模式A", "模式B", "模式C" },
                0,
                (index) =>
                {
                    try
                    {
                        Log.LogInfo($"选择了: {index}");
                        // 你的逻辑代码
                    }
                    catch (System.Exception ex)
                    {
                        Log.LogError($"❌ 回调异常: {ex}");
                    }
                }
            );
        }
        catch (System.Exception ex)
        {
            Log.LogError($"❌ 添加设置失败: {ex}");
        }
    }
}
```

---

## ✅ 验证成功

如果看到以下日志，说明一切正常：

```
[Your MOD] 你的 MOD 已加载
[ModSettings] Mod Settings tab initialized!
[Your MOD] ✅ ModSettingsManager 已准备好，开始添加设置
[Your MOD] ✅ 设置项已添加
```

并且在游戏中打开设置界面，MOD 标签页中应该能看到你的设置项。

---

## 📞 如果还是不行

请提供以下信息：

1. **完整的日志输出** (BepInEx/LogOutput.log)
2. **你的 MOD 代码** (AddSettings 部分)
3. **游戏版本和平台** (Windows/Linux, 游戏版本号)
4. **已安装的其他 MOD** (列表)

这样可以快速定位问题！

