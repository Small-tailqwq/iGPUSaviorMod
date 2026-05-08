# 🏘️ MOD 设置集成指南

欢迎其他 MOD 作者！这份文档将帮助你轻松地将你的设置集成到游戏自带的设置界面。

**如果你对我们的 MOD 框架有依赖，你可以在设置界面中添加你自己的设置项，就像在一个共享的"房间"里装修一样。**

---

## 📋 目录
1. [快速开始](#快速开始)
2. [核心概念](#核心概念)
3. [API 文档](#api-文档)
4. [完整示例](#完整示例)
5. [最佳实践](#最佳实践)
6. [常见问题](#常见问题)
7. [故障排查](#故障排查)

---

## 🚀 快速开始

### 1. 添加项目引用
在你的 `.csproj` 文件中添加对 `ModShared` 命名空间的引用：

```xml
<ItemGroup>
    <Reference Include="ModShared">
        <HintPath>../iGPU Savior/bin/Release/iGPU Savior.dll</HintPath>
    </Reference>
</ItemGroup>
```

### 2. 导入命名空间
```csharp
using ModShared;
```

### 3. 在你的插件初始化时添加设置
```csharp
public class YourModPlugin
{
    void Awake()
    {
        // 等待设置界面初始化
        if (ModSettingsManager.Instance != null && ModSettingsManager.Instance.IsInitialized)
        {
            AddYourSettings();
        }
    }

    void AddYourSettings()
    {
        var modManager = ModSettingsManager.Instance;
        var contentParent = modManager.ModContentParent;

        // 添加开关
        modManager.AddToggle(contentParent, "启用你的功能", true, (value) =>
        {
            Debug.Log($"功能状态: {value}");
        });

        // 添加下拉菜单
        modManager.AddDropdown(contentParent, "选择模式", 
            new List<string> { "模式A", "模式B", "模式C" }, 
            0,
            (selectedIndex) =>
            {
                Debug.Log($"选中: {selectedIndex}");
            });
    }
}
```

---

## 💡 核心概念

### 架构设计

```
游戏设置界面 (SettingUI)
├── 常规 (General)
├── 图形 (Graphic)
├── 音频 (Audio)
├── 制作人员 (Credits)
└── MOD ⭐ (我们的共享区域)
    ├── 你的 MOD 设置
    ├── 他人的 MOD 设置
    └── ...
```

### 生命周期

1. **游戏启动** → iGPU Savior 插件初始化
2. **SettingUI.Setup()** → Harmony 补丁创建 MOD 标签页
3. **ModSettingsManager 初始化** → 设置界面准备就绪
4. **其他 MOD 插件** → 检查 `ModSettingsManager.Instance.IsInitialized`
5. **添加设置项** → 调用 `AddToggle()` / `AddDropdown()` 等方法

---

## 📚 API 文档

### ModSettingsManager

#### 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Instance` | `ModSettingsManager` | 单例实例（静态） |
| `ModContentParent` | `GameObject` | 设置内容的根容器 |
| `ModTabButton` | `GameObject` | MOD 标签按钮 |
| `ModScrollRect` | `ScrollRect` | 滚动视图（用于动态内容布局） |
| `IsInitialized` | `bool` | 是否已初始化 |

#### 方法

##### `AddToggle()`
添加一个开关/复选框。

```csharp
public GameObject AddToggle(
    GameObject parent,              // 父容器（通常是 ModContentParent）
    string label,                   // 显示的标签文本
    bool defaultValue,              // 默认状态
    Action<bool> onValueChanged     // 值变化时的回调
)
```

**返回值**: 设置行的 GameObject，包含标签和开关。

**示例**:
```csharp
modManager.AddToggle(
    contentParent, 
    "启用后处理效果", 
    true, 
    (isEnabled) =>
    {
        SaveYourSetting("PostProcessing", isEnabled);
    }
);
```

##### `AddDropdown()`
添加一个下拉选择菜单。

```csharp
public GameObject AddDropdown(
    GameObject parent,              // 父容器
    string label,                   // 显示的标签文本
    List<string> options,           // 选项列表
    int defaultIndex,               // 默认选中的索引
    Action<int> onValueChanged      // 选项变化时的回调（参数是选中索引）
)
```

**返回值**: 设置行的 GameObject。

**示例**:
```csharp
modManager.AddDropdown(
    contentParent,
    "画质预设",
    new List<string> { "低", "中", "高", "超高" },
    2,
    (selectedIndex) =>
    {
        string[] qualities = { "Low", "Medium", "High", "Ultra" };
        ApplyQuality(qualities[selectedIndex]);
    }
);
```

---

## 🎨 完整示例

### 示例 1: 简单的功能开关 MOD

```csharp
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using ModShared;

namespace MyAwesomeMod
{
    [BepInPlugin("com.example.mymod", "My Awesome MOD", "1.0.0")]
    public class MyAwesomeModPlugin : BaseUnityPlugin
    {
        public static ManualLogSource Log;

        void Awake()
        {
            Log = Logger;
            Log.LogInfo("MyAwesomeMod 已加载！");
        }

        void Start()
        {
            // 延迟一帧，确保设置管理器已初始化
            StartCoroutine(InitializeSettings());
        }

        System.Collections.IEnumerator InitializeSettings()
        {
            yield return null;

            if (ModSettingsManager.Instance == null)
            {
                Log.LogError("ModSettingsManager 未找到！");
                yield break;
            }

            if (!ModSettingsManager.Instance.IsInitialized)
            {
                Log.LogWarning("设置管理器未初始化，等待中...");
                for (int i = 0; i < 100; i++)
                {
                    yield return new WaitForSeconds(0.1f);
                    if (ModSettingsManager.Instance.IsInitialized) break;
                }
            }

            AddMySettings();
        }

        void AddMySettings()
        {
            var manager = ModSettingsManager.Instance;
            var content = manager.ModContentParent;

            // 添加开关：启用/禁用 MOD
            manager.AddToggle(
                content,
                "启用我的MOD功能",
                true,
                (enabled) =>
                {
                    Debug.Log($"MyAwesomeMod 已{(enabled ? "启用" : "禁用")}");
                }
            );

            // 添加下拉菜单：选择模式
            manager.AddDropdown(
                content,
                "运行模式",
                new System.Collections.Generic.List<string> { "性能优先", "平衡", "质量优先" },
                1,
                (index) =>
                {
                    string[] modes = { "Performance", "Balanced", "Quality" };
                    SetMode(modes[index]);
                }
            );
        }

        void SetMode(string mode)
        {
            Log.LogInfo($"切换到模式: {mode}");
        }
    }
}
```

### 示例 2: 复杂的多层级设置

```csharp
void AddMySettings()
{
    var manager = ModSettingsManager.Instance;
    var content = manager.ModContentParent;

    // 第一组：基础设置
    manager.AddToggle(content, "启用音效", true, OnSoundToggled);
    manager.AddDropdown(content, "音量级别", 
        new List<string> { "10%", "25%", "50%", "75%", "100%" }, 
        4, OnVolumeChanged);

    // 第二组：图形设置
    manager.AddToggle(content, "启用阴影", true, OnShadowToggled);
    manager.AddDropdown(content, "光源质量", 
        new List<string> { "低", "中", "高" }, 
        1, OnLightQualityChanged);

    // 第三组：实验性功能
    manager.AddToggle(content, "[实验] 新渲染器", false, OnNewRendererToggled);
}

void OnSoundToggled(bool value) => Debug.Log($"音效: {value}");
void OnVolumeChanged(int index) => Debug.Log($"音量: {index}");
void OnShadowToggled(bool value) => Debug.Log($"阴影: {value}");
void OnLightQualityChanged(int index) => Debug.Log($"光源质量: {index}");
void OnNewRendererToggled(bool value) => Debug.Log($"新渲染器: {value}");
```

---

## ✨ 最佳实践

### 1. 正确处理初始化时序
```csharp
// ❌ 不要这样做（太早初始化）
void Awake()
{
    AddSettings(); // ModSettingsManager 可能还没初始化
}

// ✅ 应该这样做（使用 Coroutine 延迟）
void Start()
{
    StartCoroutine(WaitAndInitialize());
}

System.Collections.IEnumerator WaitAndInitialize()
{
    yield return null; // 等待一帧
    if (ModSettingsManager.Instance?.IsInitialized == true)
    {
        AddSettings();
    }
}
```

### 2. 保存和加载设置
```csharp
void AddMySettings()
{
    var manager = ModSettingsManager.Instance;
    var content = manager.ModContentParent;

    // 从配置文件读取保存的值
    bool savedValue = LoadSetting("MyFeature", true);

    manager.AddToggle(
        content,
        "我的功能",
        savedValue,
        (newValue) =>
        {
            SaveSetting("MyFeature", newValue);
            ApplyFeature(newValue);
        }
    );
}

void SaveSetting(string key, object value)
{
    // 使用 BepInEx Config 或你自己的保存系统
    PlayerPrefs.SetString(key, value.ToString());
    PlayerPrefs.Save();
}

object LoadSetting(string key, object defaultValue)
{
    return PlayerPrefs.GetString(key, defaultValue.ToString());
}
```

### 3. 处理设置变化的副作用
```csharp
manager.AddDropdown(
    content,
    "渲染分辨率",
    new List<string> { "1280x720", "1600x900", "1920x1080" },
    2,
    (index) =>
    {
        string[] resolutions = { "1280x720", "1600x900", "1920x1080" };
        ApplyResolution(resolutions[index]);
        
        // 重新初始化系统
        ReinitializeGraphics();
    }
);
```

### 4. 使用清晰的标签名称
```csharp
// ❌ 不清楚
manager.AddToggle(content, "启用 X", true, OnToggle);

// ✅ 清晰且简洁
manager.AddToggle(content, "启用实时阴影计算", true, OnToggle);
manager.AddToggle(content, "启用去焦散景深效果", true, OnToggle);
```

### 5. 分组相关的设置
```csharp
// 创建一个占位符（空的行）作为分隔符
var spacer = new GameObject("Spacer");
spacer.transform.SetParent(content.transform);
spacer.AddComponent<LayoutElement>().preferredHeight = 20;

// 然后添加相关的设置组
manager.AddToggle(content, "音效 - 启用", true, OnAudioToggled);
manager.AddDropdown(content, "音效 - 质量", qualities, 1, OnAudioQualityChanged);
```

---

## ❓ 常见问题

### Q1: 如果 ModSettingsManager 未初始化怎么办？

**A**: iGPU Savior MOD 必须被加载。确保：
1. iGPU Savior MOD 已正确安装在 BepInEx 插件目录中
2. 你的 MOD 的依赖项列表中包含它
3. 你的 MOD 加载顺序在 iGPU Savior 之后

```xml
<!-- 在 BepInEx.cfg 中设置加载优先级 -->
[Chainloader]
DLL Search Pattern = *.dll
```

### Q2: 可以添加其他类型的控件吗（例如滑块、输入框）？

**A**: 当前 API 提供了 `AddToggle()` 和 `AddDropdown()`。如果需要其他控件类型，请：

1. 提交 Issue 或 PR 到主项目
2. 暂时自己创建自定义 UI 元素（高级用法）
3. 使用 `ModContentParent` 直接访问容器并添加自定义控件

```csharp
// 高级：自定义 UI
var customUI = Instantiate(yourPrefab, modManager.ModContentParent.transform);
```

### Q3: 如何确保设置的显示顺序？

**A**: 按照你想要的顺序调用 API 方法即可：

```csharp
// 将按此顺序显示
manager.AddToggle(content, "设置 A", true, callback);
manager.AddToggle(content, "设置 B", false, callback);
manager.AddToggle(content, "设置 C", true, callback);
```

### Q4: 如果多个 MOD 同时尝试添加设置会怎样？

**A**: 完全安全！`ModSettingsManager` 的设计支持并发添加。所有 MOD 的设置都会按添加顺序显示在同一个面板中。

---

## 🔧 故障排查

### 问题 1: "ModSettingsManager 为 null"

**症状**: 
```
NullReferenceException: Object reference not set to an instance of an object
```

**解决方案**:
```csharp
// 使用安全导航操作符
if (ModSettingsManager.Instance?.IsInitialized == true)
{
    // 安全地添加设置
}
```

### 问题 2: 设置没有显示

**检查清单**:
1. ✅ `ModSettingsManager.Instance.IsInitialized` 是否为 `true`？
2. ✅ 你是否传递了正确的 `parent`（应该是 `ModContentParent`）？
3. ✅ 在 Unity Inspector 中，MOD 标签页是否存在？
4. ✅ 日志中是否有错误信息？

**调试代码**:
```csharp
void DebugSettings()
{
    var manager = ModSettingsManager.Instance;
    Debug.Log($"Manager 存在: {manager != null}");
    Debug.Log($"已初始化: {manager?.IsInitialized}");
    Debug.Log($"Content Parent: {manager?.ModContentParent}");
    Debug.Log($"Scroll Rect: {manager?.ModScrollRect}");
}
```

### 问题 3: UI 显示错位或重叠

**原因**: 布局组件配置问题

**解决方案**:
- 确保 `ModContentParent` 有正确的 `LayoutGroup` 组件
- 检查 `LayoutElement` 的 `preferredHeight` 设置
- 强制重新构建布局：
```csharp
LayoutRebuilder.ForceRebuildLayoutImmediate(
    modManager.ModContentParent.GetComponent<RectTransform>()
);
```

### 问题 4: 回调没有被触发

**检查**:
```csharp
// 确保回调函数签名正确
manager.AddToggle(
    content,
    "测试",
    true,
    (value) =>  // ✅ 正确：参数是 bool
    {
        Debug.Log(value);
    }
);

manager.AddDropdown(
    content,
    "测试",
    new List<string> { "A", "B" },
    0,
    (index) =>  // ✅ 正确：参数是 int
    {
        Debug.Log(index);
    }
);
```

---

## 📞 获取帮助

如果遇到问题：

1. **检查日志** - 查看 BepInEx 的控制台输出
2. **查阅本文档** - 使用 Ctrl+F 搜索关键词
3. **参考示例** - 查看 `docs/` 目录中的完整示例代码
4. **提交 Issue** - 在项目中报告 Bug 或请求功能
5. **社区讨论** - 在论坛或 Discord 寻求帮助

---

## 🎯 总结

- ✅ 导入 `ModShared` 命名空间
- ✅ 等待 `ModSettingsManager.Instance.IsInitialized == true`
- ✅ 调用 `AddToggle()` / `AddDropdown()` 添加设置
- ✅ 在回调中处理设置变化
- ✅ 使用 PlayerPrefs 或你自己的系统保存设置

祝你的 MOD 开发顺利！🚀
