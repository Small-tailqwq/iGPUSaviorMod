# ⚡ MOD 设置 API 速查表

快速参考，用于开发者集成设置到游戏界面。

---

## 导入

```csharp
using ModShared;
```

---

## 单例访问

```csharp
ModSettingsManager manager = ModSettingsManager.Instance;
```

| 属性 | 说明 |
|------|------|
| `IsInitialized` | 是否已初始化（`bool`） |
| `ModContentParent` | 设置容器（`GameObject`） |
| `ModTabButton` | MOD 标签按钮（`GameObject`） |
| `ModScrollRect` | 滚动视图（`ScrollRect`） |

---

## 方法

### AddToggle - 添加开关

```csharp
manager.AddToggle(
    GameObject parent,
    string label,
    bool defaultValue,
    Action<bool> onValueChanged
);
```

**示例**:
```csharp
manager.AddToggle(
    manager.ModContentParent,
    "启用后处理",
    true,
    (enabled) => Debug.Log(enabled)
);
```

---

### AddDropdown - 添加下拉菜单

```csharp
manager.AddDropdown(
    GameObject parent,
    string label,
    List<string> options,
    int defaultIndex,
    Action<int> onValueChanged
);
```

**示例**:
```csharp
manager.AddDropdown(
    manager.ModContentParent,
    "质量等级",
    new List<string> { "低", "中", "高" },
    1,
    (index) => Debug.Log($"选中: {index}")
);
```

---

## 初始化模式

### 推荐：使用 Coroutine

```csharp
void Start()
{
    StartCoroutine(InitSettings());
}

IEnumerator InitSettings()
{
    yield return null;
    
    var mgr = ModSettingsManager.Instance;
    if (mgr?.IsInitialized == true)
    {
        mgr.AddToggle(mgr.ModContentParent, "测试", true, null);
    }
}
```

### 简单：使用 Update

```csharp
bool initialized = false;

void Update()
{
    if (!initialized && ModSettingsManager.Instance?.IsInitialized == true)
    {
        AddSettings();
        initialized = true;
    }
}

void AddSettings()
{
    var mgr = ModSettingsManager.Instance;
    mgr.AddToggle(mgr.ModContentParent, "测试", true, null);
}
```

---

## 常用模式

### 保存设置到 PlayerPrefs

```csharp
manager.AddToggle(
    manager.ModContentParent,
    "启用功能",
    PlayerPrefs.GetInt("feature_enabled", 1) == 1,
    (value) =>
    {
        PlayerPrefs.SetInt("feature_enabled", value ? 1 : 0);
        PlayerPrefs.Save();
    }
);
```

### 链式调用多个设置

```csharp
var mgr = ModSettingsManager.Instance;
var parent = mgr.ModContentParent;

mgr.AddToggle(parent, "功能A", true, OnAChanged);
mgr.AddToggle(parent, "功能B", false, OnBChanged);
mgr.AddDropdown(parent, "模式", modes, 0, OnModeChanged);
```

### 设置变化时应用效果

```csharp
manager.AddDropdown(
    manager.ModContentParent,
    "分辨率",
    new List<string> { "720p", "1080p", "4K" },
    1,
    (index) =>
    {
        ApplyResolution(index);
        ReinitializeGraphics();
    }
);
```

---

## 安全检查清单

- [ ] 导入 `using ModShared;`
- [ ] 检查 `ModSettingsManager.Instance != null`
- [ ] 检查 `IsInitialized == true`
- [ ] 使用 `ModContentParent` 作为父容器
- [ ] 回调函数签名正确（`bool` 或 `int`）
- [ ] 测试初始化时序（使用 Coroutine 延迟）

---

## 故障排查

| 问题 | 解决方案 |
|------|--------|
| `null` 引用异常 | 使用安全导航: `Instance?.IsInitialized` |
| 设置未显示 | 检查 `IsInitialized` 和 `parent` 参数 |
| 回调不触发 | 检查回调签名：`bool` 或 `int` |
| UI 错位 | 调用 `LayoutRebuilder.ForceRebuildLayoutImmediate()` |

---

## 最小示例

```csharp
using BepInEx;
using ModShared;
using UnityEngine;
using System.Collections;

[BepInPlugin("example.mod", "Example", "1.0")]
public class ExampleMod : BaseUnityPlugin
{
    void Start() => StartCoroutine(Setup());

    IEnumerator Setup()
    {
        yield return null;
        
        var mgr = ModSettingsManager.Instance;
        if (mgr?.IsInitialized == true)
        {
            mgr.AddToggle(mgr.ModContentParent, "My Setting", true, 
                (v) => Debug.Log($"Value: {v}"));
        }
    }
}
```

---

**更多详情请查看完整文档**: `MOD_SETTINGS_INTEGRATION_GUIDE.md`
