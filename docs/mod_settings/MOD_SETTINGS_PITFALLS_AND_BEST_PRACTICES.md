# 🚨 MOD 设置集成 - 常见陷阱 & 最佳实践

本文档列出开发者在集成 MOD 设置时最容易犯的错误，以及如何避免它们。

---

## 🔴 陷阱 1: 时序问题 - 过早初始化

### ❌ 错误做法
```csharp
public class MyMod : BaseUnityPlugin
{
    void Awake()
    {
        // ❌ 问题：此时 ModSettingsManager 还未初始化！
        ModSettingsManager.Instance.AddToggle(...);
    }
}
```

**结果**: `NullReferenceException` 或设置未添加

### ✅ 正确做法

**方法A: 使用 Coroutine（推荐）**
```csharp
void Start()
{
    StartCoroutine(InitializeSettings());
}

IEnumerator InitializeSettings()
{
    // 等待一帧，让所有系统初始化
    yield return null;

    var mgr = ModSettingsManager.Instance;
    if (mgr != null && mgr.IsInitialized)
    {
        AddMySettings();
    }
    else
    {
        Logger.LogWarning("ModSettingsManager 未准备好");
    }
}

void AddMySettings()
{
    // 添加设置的代码
}
```

**方法B: 轮询检查**
```csharp
private bool _initialized = false;

void Update()
{
    if (!_initialized)
    {
        var mgr = ModSettingsManager.Instance;
        if (mgr?.IsInitialized == true)
        {
            AddMySettings();
            _initialized = true;
        }
    }
}
```

**方法C: 使用 WaitUntil**
```csharp
void Start()
{
    StartCoroutine(WaitForInitialization());
}

IEnumerator WaitForInitialization()
{
    yield return new WaitUntil(() => 
        ModSettingsManager.Instance?.IsInitialized == true);
    
    AddMySettings();
}
```

---

## 🔴 陷阱 2: 错误的回调签名

### ❌ 错误做法
```csharp
// ❌ Toggle 的回调应该是 Action<bool>，不是 Action
mgr.AddToggle(parent, "Test", true, () => Debug.Log("Clicked"));

// ❌ Dropdown 的回调应该接收 int (索引)，而不是 string (内容)
mgr.AddDropdown(
    parent, "Choose", 
    new List<string> { "A", "B" }, 
    0,
    (selectedText) => Debug.Log($"Selected: {selectedText}") // 错误！
);
```

### ✅ 正确做法
```csharp
// ✅ Toggle：参数是 bool
mgr.AddToggle(
    parent, 
    "Test", 
    true, 
    (isEnabled) => Debug.Log($"Enabled: {isEnabled}")
);

// ✅ Dropdown：参数是选中的索引（int）
mgr.AddDropdown(
    parent, 
    "Choose", 
    new List<string> { "Low", "Medium", "High" }, 
    0,
    (selectedIndex) => Debug.Log($"Selected index: {selectedIndex}")
);
```

---

## 🔴 陷阱 3: 使用错误的父容器

### ❌ 错误做法
```csharp
var mgr = ModSettingsManager.Instance;

// ❌ 创建了新的空容器
var newContainer = new GameObject("MySettings");
mgr.AddToggle(newContainer, "Test", true, null); // 这是孤立的！

// ❌ 使用了 ModTabButton（按钮），而不是 ModContentParent（内容）
mgr.AddToggle(mgr.ModTabButton, "Test", true, null); // 错误位置
```

### ✅ 正确做法
```csharp
var mgr = ModSettingsManager.Instance;

// ✅ 始终使用 ModContentParent 作为父容器
mgr.AddToggle(mgr.ModContentParent, "Test", true, null);
mgr.AddDropdown(mgr.ModContentParent, "Choose", options, 0, null);

// ✅ 如果需要子组织，创建子容器并添加到 ModContentParent
var groupContainer = new GameObject("AudioSettings");
groupContainer.transform.SetParent(mgr.ModContentParent.transform, false);
mgr.AddToggle(groupContainer, "Master Volume", true, null);
```

---

## 🔴 陷阱 4: 忘记检查初始化状态

### ❌ 错误做法
```csharp
void Setup()
{
    // ❌ 直接假设已初始化
    var mgr = ModSettingsManager.Instance;
    mgr.AddToggle(mgr.ModContentParent, "Test", true, null);
    // 如果 mgr 为 null 会崩溃！
}
```

### ✅ 正确做法
```csharp
void Setup()
{
    // ✅ 方法1: 使用安全导航操作符
    ModSettingsManager.Instance?.AddToggle(
        ModSettingsManager.Instance.ModContentParent, 
        "Test", true, null
    );

    // ✅ 方法2: 显式检查
    var mgr = ModSettingsManager.Instance;
    if (mgr != null && mgr.IsInitialized)
    {
        mgr.AddToggle(mgr.ModContentParent, "Test", true, null);
    }

    // ✅ 方法3: 完整的防御性编程
    var mgr = ModSettingsManager.Instance;
    if (mgr == null)
    {
        Logger.LogError("ModSettingsManager 不可用");
        return;
    }

    if (!mgr.IsInitialized)
    {
        Logger.LogWarning("设置管理器未初始化");
        return;
    }

    var parent = mgr.ModContentParent;
    if (parent == null)
    {
        Logger.LogError("ModContentParent 为 null");
        return;
    }

    mgr.AddToggle(parent, "Test", true, null);
}
```

---

## 🔴 陷阱 5: 不保存/恢复用户设置

### ❌ 错误做法
```csharp
void AddSettings()
{
    var mgr = ModSettingsManager.Instance;
    
    // ❌ 每次初始化都用默认值，用户的改动会丢失！
    mgr.AddToggle(
        mgr.ModContentParent,
        "启用功能",
        true,  // 总是用默认值
        (value) => ApplyFeature(value)
    );
}
```

**结果**: 用户修改的设置在重启游戏后消失

### ✅ 正确做法
```csharp
void AddSettings()
{
    var mgr = ModSettingsManager.Instance;
    
    // ✅ 从存储中读取保存的值
    bool savedValue = LoadSetting("FeatureEnabled", true);
    
    mgr.AddToggle(
        mgr.ModContentParent,
        "启用功能",
        savedValue,  // 使用保存的值
        (value) =>
        {
            ApplyFeature(value);
            SaveSetting("FeatureEnabled", value);  // 保存新值
        }
    );
}

bool LoadSetting(string key, bool defaultValue)
{
    return PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) == 1;
}

void SaveSetting(string key, bool value)
{
    PlayerPrefs.SetInt(key, value ? 1 : 0);
    PlayerPrefs.Save();
}
```

---

## 🔴 陷阱 6: 处理设置变化的副作用不当

### ❌ 错误做法
```csharp
mgr.AddDropdown(
    mgr.ModContentParent,
    "渲染质量",
    new List<string> { "Low", "Medium", "High" },
    1,
    (index) =>
    {
        // ❌ 直接修改设置，但没有处理可能的长时间操作或错误
        ApplyQuality(index);
    }
);

void ApplyQuality(int level)
{
    // 这个操作可能很耗时或导致错误
    DestroyAllGameObjects();
    ReloadAllAssets();
    ReinitializeGraphics();
}
```

**问题**:
- 用户界面可能冻结
- 没有错误处理
- 没有反馈机制

### ✅ 正确做法
```csharp
mgr.AddDropdown(
    mgr.ModContentParent,
    "渲染质量",
    new List<string> { "Low", "Medium", "High" },
    1,
    (index) =>
    {
        // ✅ 异步处理，提供反馈
        StartCoroutine(ApplyQualityAsync(index));
    }
);

IEnumerator ApplyQualityAsync(int level)
{
    try
    {
        // 异步操作
        yield return StartCoroutine(DestroyGameObjectsAsync());
        yield return StartCoroutine(ReloadAssetsAsync());
        yield return ReinitializeGraphicsAsync();
        
        Debug.Log($"质量设置已应用: {level}");
        SaveSetting("RenderQuality", level);
    }
    catch (System.Exception e)
    {
        Logger.LogError($"应用质量设置失败: {e.Message}");
        // 恢复到之前的状态
        yield return RollbackQualitySettings();
    }
}
```

---

## 🔴 陷阱 7: 没有验证用户输入

### ❌ 错误做法
```csharp
mgr.AddDropdown(
    mgr.ModContentParent,
    "音量",
    new List<string> { "10%", "50%", "100%" },
    1,
    (index) =>
    {
        // ❌ 假设 index 总是有效的
        float[] volumes = { 0.1f, 0.5f, 1.0f };
        SetVolume(volumes[index]);  // 如果索引越界会崩溃
    }
);
```

### ✅ 正确做法
```csharp
mgr.AddDropdown(
    mgr.ModContentParent,
    "音量",
    new List<string> { "10%", "50%", "100%" },
    1,
    (index) =>
    {
        // ✅ 验证索引
        float[] volumes = { 0.1f, 0.5f, 1.0f };
        
        if (index >= 0 && index < volumes.Length)
        {
            SetVolume(volumes[index]);
        }
        else
        {
            Logger.LogError($"无效的音量索引: {index}");
            SetVolume(0.5f);  // 使用默认值
        }
    }
);
```

---

## ✅ 最佳实践汇总

### 1. 初始化检查清单
```csharp
void SafeInitializeSettings()
{
    // 检查步骤 1: 管理器存在
    if (ModSettingsManager.Instance == null)
    {
        Logger.LogError("ModSettingsManager 不存在");
        return;
    }

    // 检查步骤 2: 已初始化
    if (!ModSettingsManager.Instance.IsInitialized)
    {
        Logger.LogWarning("ModSettingsManager 未初始化");
        return;
    }

    // 检查步骤 3: 容器存在
    if (ModSettingsManager.Instance.ModContentParent == null)
    {
        Logger.LogError("ModContentParent 不存在");
        return;
    }

    // 现在安全地添加设置
    AddSettings();
}
```

### 2. 标准化的初始化模式
```csharp
public class MyModPlugin : BaseUnityPlugin
{
    private bool _settingsInitialized = false;

    void Start()
    {
        StartCoroutine(InitializeSettingsCoroutine());
    }

    IEnumerator InitializeSettingsCoroutine()
    {
        // 等待一帧
        yield return null;

        // 等待管理器初始化（最多 5 秒）
        float timeout = 0f;
        while (ModSettingsManager.Instance?.IsInitialized != true && timeout < 5f)
        {
            yield return new WaitForSeconds(0.1f);
            timeout += 0.1f;
        }

        if (ModSettingsManager.Instance?.IsInitialized == true)
        {
            AddMySettings();
            _settingsInitialized = true;
        }
        else
        {
            Logger.LogError("设置初始化超时");
        }
    }

    void AddMySettings()
    {
        var mgr = ModSettingsManager.Instance;
        var parent = mgr.ModContentParent;

        // 添加你的设置
        mgr.AddToggle(parent, "功能1", LoadBool("feature1", true), 
            (v) => SaveBool("feature1", v));
    }

    bool LoadBool(string key, bool defaultValue) =>
        PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) == 1;

    void SaveBool(string key, bool value) =>
        PlayerPrefs.SetInt(key, value ? 1 : 0);
}
```

### 3. 错误记录和日志
```csharp
void AddSettings()
{
    var mgr = ModSettingsManager.Instance;

    try
    {
        Logger.LogInfo("开始添加 MOD 设置...");

        if (mgr?.IsInitialized != true)
        {
            throw new System.InvalidOperationException("ModSettingsManager 未初始化");
        }

        mgr.AddToggle(mgr.ModContentParent, "测试", true, OnSettingChanged);
        
        Logger.LogInfo("✓ MOD 设置添加成功");
    }
    catch (System.Exception e)
    {
        Logger.LogError($"✗ 添加 MOD 设置失败: {e.Message}\n{e.StackTrace}");
    }
}

void OnSettingChanged(bool value)
{
    Logger.LogDebug($"设置已更改: {value}");
}
```

---

## 📋 调试检查清单

在发布你的 MOD 前，确认以下所有项目：

- [ ] 所有设置在首次加载时显示
- [ ] 用户修改的设置在重启后保存
- [ ] 没有 null 引用异常
- [ ] 设置变化的回调被正确触发
- [ ] 没有 UI 错位或重叠
- [ ] 日志中没有警告或错误
- [ ] 与其他 MOD 兼容（多个 MOD 设置同时显示）
- [ ] 设置文本清晰准确
- [ ] 初始化时序正确（使用 Coroutine）

---

## 🔗 相关资源

- [MOD 设置集成指南](MOD_SETTINGS_INTEGRATION_GUIDE.md) - 完整文档
- [API 速查表](MOD_SETTINGS_API_QUICK_REFERENCE.md) - 快速参考
- [示例代码](../iGPU%20Savior/UI/ModSettingsManager.cs) - 源代码实现

---

祝你开发顺利！如有任何问题，欢迎反馈！🚀
