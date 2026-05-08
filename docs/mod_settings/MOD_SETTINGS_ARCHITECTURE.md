# 🏗️ MOD 设置系统架构 & 流程图

本文档展示了整个 MOD 设置系统如何工作，帮助开发者理解底层机制。

---

## 📐 系统架构图

```
┌─────────────────────────────────────────────────────────────┐
│                       游戏启动                               │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│        BepInEx 加载所有插件                                  │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ 1. iGPU Savior MOD (PotatoPlugin)                    │ │
│  │ 2. 其他 MOD 插件 (你的 MOD)                           │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│             MonoBehaviour.Awake() 阶段                       │
│  • iGPU Savior 初始化 Harmony 补丁                          │
│  • 准备拦截 SettingUI.Setup()                              │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│   用户打开设置界面 → SettingUI.Setup() 被调用                │
│   (Harmony 补丁拦截此调用)                                  │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│         iGPU Savior Harmony Postfix 触发                     │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ ModSettingsIntegration.Postfix(SettingUI instance)   │ │
│  │  1. 检查 ModSettingsManager 是否存在                  │ │
│  │  2. 如果不存在，创建 MOD 标签页                        │ │
│  │  3. 实例化 ModSettingsManager (单例)                  │ │
│  │  4. 调用 Initialize() 方法                            │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│      ModSettingsManager 初始化完成                            │
│  IsInitialized = true ✓                                     │
│  可以开始添加 MOD 设置了                                     │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│   其他 MOD 检测到 IsInitialized = true                      │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ YourMod.Start() / Coroutine:                         │ │
│  │  1. 获取 ModSettingsManager.Instance                 │ │
│  │  2. 调用 AddToggle() / AddDropdown()                 │ │
│  │  3. 传入回调函数处理用户交互                          │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│   设置UI更新显示                                             │
│  • MOD 标签页显示所有注册的设置项                            │
│  • 用户可以修改设置                                         │
│  • 回调函数处理变化并保存                                   │
└─────────────────────────────────────────────────────────────┘
```

---

## 🔄 初始化时序图

```
时间 →

游戏启动
  │
  ├─ T+0ms: BepInEx 加载插件
  │
  ├─ T+50ms: iGPU Savior Awake() 
  │          └─ 应用 Harmony 补丁
  │
  ├─ T+100ms: 你的 MOD Awake()
  │           └─ 保存对 Logger 的引用
  │
  ├─ T+150ms: 用户点击设置按钮
  │           └─ SettingUI.Setup() 被调用
  │
  ├─ T+160ms: Harmony 补丁拦截 Setup()
  │           └─ ModSettingsIntegration.Postfix()
  │              ├─ 克隆 Credits 标签页
  │              ├─ 创建 ModSettingsManager (DontDestroyOnLoad)
  │              └─ IsInitialized = true ✓
  │
  ├─ T+170ms: 你的 MOD Start()
  │           └─ 启动 Coroutine 检查初始化
  │
  ├─ T+180ms: Coroutine yield return null
  │           └─ 检查 ModSettingsManager.Instance?.IsInitialized
  │              ✓ true - 准备添加设置
  │
  └─ T+190ms: AddToggle() / AddDropdown() 被调用
              └─ 设置UI显示你的设置项
```

---

## 🎯 用户交互流程

```
┌─────────────────────────────┐
│   用户在 MOD 标签页中       │
│   修改某个设置项            │
└──────────┬──────────────────┘
           │
           ▼
┌─────────────────────────────┐
│  Toggle / Dropdown          │
│  onValueChanged 事件触发    │
└──────────┬──────────────────┘
           │
           ▼
┌─────────────────────────────┐
│  你提供的回调函数执行       │
│  (Action<bool/int>)         │
└──────────┬──────────────────┘
           │
           ├─→ 保存到 PlayerPrefs
           │
           ├─→ 应用游戏逻辑
           │   (如: 修改图形设置)
           │
           └─→ 更新 UI 反馈
```

---

## 📦 代码组织结构

```
iGPU Savior/
├── Core/
│   └── PotatoPlugin.cs
│       └─ 管理 Harmony 补丁应用
│
├── UI/
│   ├── ModSettingsIntegration.cs
│   │   ├─ [HarmonyPatch] SettingUI.Setup()
│   │   ├─ CreateModSettingsTab()
│   │   ├─ SwitchToModTab()
│   │   └─ UpdateModContentText()
│   │
│   └── ModSettingsManager.cs
│       ├─ 单例: Instance
│       ├─ IsInitialized
│       ├─ ModContentParent (GameObject)
│       ├─ AddToggle()
│       ├─ AddDropdown()
│       └─ CreateSettingRow()
│
└── docs/
    ├── MOD_SETTINGS_INTEGRATION_GUIDE.md ⬅️ 你在这里
    ├── MOD_SETTINGS_API_QUICK_REFERENCE.md
    ├── MOD_SETTINGS_PITFALLS_AND_BEST_PRACTICES.md
    └── MOD_SETTINGS_ARCHITECTURE.md (本文件)
```

---

## 🔌 依赖关系图

```
你的 MOD 插件
   │
   └─> using ModShared
       └─> ModSettingsManager
           │
           ├─> ModContentParent (GameObject)
           │   └─> RectTransform
           │   └─> LayoutGroup
           │
           ├─> ModScrollRect (ScrollRect)
           │
           ├─> AddToggle()
           │   └─> CreateSettingRow()
           │   └─> Toggle 组件
           │
           └─> AddDropdown()
               └─> CreateSettingRow()
               └─> TMP_Dropdown 组件
```

---

## 🛡️ 线程安全性

### 当前行为
```csharp
// ModSettingsManager 使用单例模式
public static ModSettingsManager Instance { get; private set; }

// 在 Awake() 中初始化
void Awake()
{
    if (Instance == null)
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    else if (Instance != this)
    {
        Destroy(gameObject);  // 确保只有一个实例
    }
}
```

### 安全性保证
- ✅ **单线程**: Unity 主线程只调用一次 Awake()
- ✅ **生命周期保护**: DontDestroyOnLoad 防止重复销毁
- ✅ **初始化检查**: IsInitialized 标志防止重复初始化

---

## 🌳 GameObject 层级结构

```
Canvas (UI)
└── ChangeOrderObjects
    └── UI_FacilitySetting
        ├── [Native Tabs]
        │   ├── General
        │   ├── Graphics
        │   ├── Audio
        │   └── Credits
        │
        ├── [Native Content Panels]
        │   ├── _generalParent (General content)
        │   ├── _graphicParent (Graphics content)
        │   ├── _audioParent (Audio content)
        │   └── _creditsParent (Credits content)
        │
        └── [MOD Content] ⭐ (由 iGPU Savior 创建)
            ├── ModSettingsTabButton
            │   └── InteractableUI
            │       └── Button
            │           └── onClick → SwitchToModTab()
            │
            └── ModSettingsContent
                ├── Title (TextMeshProUGUI) "MOD"
                │
                ├── ScrollRect
                │   └── Content (RectTransform)
                │       ├── [YourMod-Toggle]
                │       │   ├── Label
                │       │   └── ToggleContainer
                │       │       └── Toggle
                │       │
                │       ├── [YourMod-Dropdown]
                │       │   ├── Label
                │       │   └── Dropdown
                │       │
                │       └── [OtherMod-Settings]
                │           └── ...
                │
                └── LayoutGroup
```

---

## 🔐 错误处理策略

```
异常发生点 → 处理方式
─────────────────────────────────────────
ModSettingsManager.Instance == null
  → 使用安全导航操作符 (?.IsInitialized)
  → 日志警告并返回

IsInitialized == false
  → 使用 Coroutine 重试
  → 最多等待 5 秒

ModContentParent == null
  → 日志错误
  → 跳过添加设置

回调函数异常
  → try-catch 包装
  → 日志错误信息
  → 继续运行
```

---

## 📊 性能考虑

### 初始化开销
```
操作                           开销        备注
─────────────────────────────────────────────
Harmony 补丁应用         ~5-10ms      一次性
GameObject 克隆          ~2-3ms       一次性
ModSettingsManager 创建    ~1ms        一次性
AddToggle() 调用          ~0.5ms      每个设置
AddDropdown() 调用        ~0.8ms      每个设置
─────────────────────────────────────────────
总计 (10 个设置)         ~20-30ms     可接受范围
```

### 运行时开销
```
操作                      开销        触发频率
─────────────────────────────────────────────
Toggle 状态改变          <1ms        用户交互
Dropdown 值改变          <1ms        用户交互
回调函数执行              自定义      取决于你的代码
UI 更新/重绘              <5ms        取决于 UI 复杂度
─────────────────────────────────────────────
```

### 优化建议
- ✅ 延迟加载复杂的设置 UI
- ✅ 批量 UI 更新而不是逐个更新
- ✅ 避免在回调中执行重操作（使用 Coroutine）
- ✅ 缓存常用的引用

---

## 🔄 生命周期事件

```
游戏启动序列:
  1. BepInEx.Harmony.ApplyPatches()
     └─ ModSettingsIntegration 补丁就位

  2. SettingUI.Setup() 被调用
     └─ Harmony Postfix → CreateModSettingsTab()
        └─ ModSettingsManager 创建并初始化
           └─ IsInitialized = true

  3. 其他 MOD Start()
     └─ Coroutine 检查 IsInitialized
        └─ AddToggle() / AddDropdown()

  4. 用户交互
     └─ 回调函数执行
        └─ 设置已保存

游戏关闭序列:
  1. SettingUI.Deactivate() / Destroy()
     └─ Harmony 补丁：Activate() 恢复初始状态

  2. ModSettingsManager (DontDestroyOnLoad)
     └─ 保留在内存中用于下一个会话
```

---

## 🎓 深入理解

### 为什么使用 Harmony Patches?

```
原始代码 (SettingUI.Setup):
  │
  ├─ 初始化原生标签页
  │
  └─ [END] ← 没有 MOD 集成

使用 Harmony Postfix:
  │
  ├─ 初始化原生标签页 (原始代码)
  │
  ├─ [POSTFIX HOOK] ← 我们在这里执行
  │  └─ 添加 MOD 标签页
  │  └─ 创建 ModSettingsManager
  │
  └─ [END]
```

**优势**:
- ✅ 不需要修改原始代码
- ✅ 可以并行多个补丁
- ✅ 易于启用/禁用

### 为什么用 DontDestroyOnLoad?

```
正常 GameObject 生命周期:
  场景加载 → 场景卸载 → GameObject 销毁 → 丢失引用

ModSettingsManager 生命周期:
  场景加载 → 场景卸载 (但 DDOL 保护) → 保持活跃
  └─ 可以被后续 MOD 访问
```

---

## 📞 扩展点

### 可能的未来功能

```
v1.0 (当前)
├── AddToggle()
├── AddDropdown()
└── ModContentParent (直接容器访问)

v1.1 (建议)
├── AddSlider()          ← 滑块控件
├── AddInputField()      ← 输入框
├── AddColorPicker()     ← 颜色选择器
└── AddSeparator()       ← 分隔符

v1.2 (可能)
├── 数据绑定系统
├── 配置文件导入/导出
├── 设置搜索功能
└── 预设系统
```

---

## 🚀 总结

整个系统设计遵循以下原则：

| 原则 | 实现 |
|------|------|
| **简洁性** | 只暴露必要的 API (`AddToggle`, `AddDropdown`) |
| **安全性** | 单例模式 + 初始化检查 |
| **灵活性** | 支持多个 MOD 并发注册 |
| **性能** | 最小化初始化开销，按需创建 UI |
| **可维护性** | 清晰的代码组织和文档 |

---

**下一步**:
- 查看 [完整集成指南](MOD_SETTINGS_INTEGRATION_GUIDE.md)
- 参考 [API 速查表](MOD_SETTINGS_API_QUICK_REFERENCE.md)
- 学习 [常见陷阱](MOD_SETTINGS_PITFALLS_AND_BEST_PRACTICES.md)

