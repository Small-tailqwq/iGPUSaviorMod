# 📚 MOD 设置集成文档中心

欢迎！这是 iGPU Savior MOD 为其他开发者提供的完整文档中心。

---

## 🎯 快速导航

### 我是第一次接触这个系统
👉 **[MOD 设置集成指南](MOD_SETTINGS_INTEGRATION_GUIDE.md)** - 从零开始

包含:
- 快速开始 (5 分钟)
- 核心概念
- 完整示例代码
- 常见问题解答

### 我只需要 API 参考
👉 **[API 速查表](MOD_SETTINGS_API_QUICK_REFERENCE.md)** - 快速查询

包含:
- 所有公开方法
- 参数说明
- 代码片段
- 初始化模式

### 我的代码有问题
👉 **[常见陷阱 & 最佳实践](MOD_SETTINGS_PITFALLS_AND_BEST_PRACTICES.md)** - 故障排查

包含:
- 7 个最常见的错误
- 解决方案对比
- 调试检查清单
- 安全模式代码

### 我想了解底层原理
👉 **[系统架构 & 流程图](MOD_SETTINGS_ARCHITECTURE.md)** - 深入理解

包含:
- 完整的架构图
- 时序图
- 生命周期说明
- 性能分析

---

## 📖 按使用场景选择文档

### 场景 1️⃣: 我想快速添加一个简单的开关

**时间**: ~10 分钟  
**难度**: ⭐ 简单

```csharp
// 第 1 步: 导入命名空间
using ModShared;

// 第 2 步: 在 Start() 中添加
void Start() => StartCoroutine(Setup());

IEnumerator Setup()
{
    yield return null;
    if (ModSettingsManager.Instance?.IsInitialized == true)
    {
        var mgr = ModSettingsManager.Instance;
        mgr.AddToggle(mgr.ModContentParent, "我的功能", true, 
            (v) => Debug.Log(v));
    }
}
```

👉 详细步骤: [MOD 设置集成指南 - 快速开始](MOD_SETTINGS_INTEGRATION_GUIDE.md#🚀-快速开始)

---

### 场景 2️⃣: 我需要一个完整的设置系统

**时间**: ~30 分钟  
**难度**: ⭐⭐ 中等

1. 阅读 [完整集成指南](MOD_SETTINGS_INTEGRATION_GUIDE.md)
2. 参考 [示例 2: 复杂的多层级设置](MOD_SETTINGS_INTEGRATION_GUIDE.md#示例-2-复杂的多层级设置)
3. 学习如何 [保存和加载设置](MOD_SETTINGS_INTEGRATION_GUIDE.md#2-保存和加载设置)
4. 检查 [调试检查清单](MOD_SETTINGS_PITFALLS_AND_BEST_PRACTICES.md#-调试检查清单)

---

### 场景 3️⃣: 我的代码报错了

**时间**: 根据情况 5-30 分钟  
**难度**: ⭐⭐ 取决于问题

查看 [常见陷阱 & 最佳实践](MOD_SETTINGS_PITFALLS_AND_BEST_PRACTICES.md):

- ❌ 陷阱 1 - 时序问题
- ❌ 陷阱 2 - 错误的回调签名
- ❌ 陷阱 3 - 使用错误的父容器
- ❌ 陷阱 4 - 忘记检查初始化状态
- ❌ 陷阱 5 - 不保存/恢复用户设置
- ❌ 陷阱 6 - 处理副作用不当
- ❌ 陷阱 7 - 没有验证用户输入

每个陷阱都有:
- ❌ 错误做法
- ✅ 正确做法
- 📝 说明和解释

---

### 场景 4️⃣: 我想深入学习系统

**时间**: ~60 分钟  
**难度**: ⭐⭐⭐ 高级

按照此顺序阅读:

1. [系统架构图](MOD_SETTINGS_ARCHITECTURE.md#-系统架构图)
2. [初始化时序图](MOD_SETTINGS_ARCHITECTURE.md#-初始化时序图)
3. [代码组织结构](MOD_SETTINGS_ARCHITECTURE.md#-代码组织结构)
4. [线程安全性](MOD_SETTINGS_ARCHITECTURE.md#-线程安全性)
5. [性能考虑](MOD_SETTINGS_ARCHITECTURE.md#-性能考虑)
6. [深入理解](MOD_SETTINGS_ARCHITECTURE.md#-深入理解)

---

## 📋 文档对比表

| 文档 | 目标读者 | 长度 | 深度 | 最佳用途 |
|------|---------|------|------|---------|
| 📖 [集成指南](MOD_SETTINGS_INTEGRATION_GUIDE.md) | 所有开发者 | 📄 长 | 入门-中等 | 学习系统、参考示例 |
| ⚡ [API 速查表](MOD_SETTINGS_API_QUICK_REFERENCE.md) | 有经验的开发者 | 📄 短 | 快速查询 | 查询方法、回忆语法 |
| 🚨 [陷阱 & 最佳实践](MOD_SETTINGS_PITFALLS_AND_BEST_PRACTICES.md) | 遇到问题的开发者 | 📄 中 | 实用 | 调试、避免错误、学习最佳实践 |
| 🏗️ [系统架构](MOD_SETTINGS_ARCHITECTURE.md) | 高级开发者 | 📄 长 | 深度 | 理解设计、性能优化 |

---

## 🚀 推荐学习路径

### 路径 A: 快速上手 (15 分钟)

```
1. 阅读此页面 (你在这里) ✓
2. 浏览 [快速开始](MOD_SETTINGS_INTEGRATION_GUIDE.md#🚀-快速开始)
3. 参考 [最小示例](MOD_SETTINGS_API_QUICK_REFERENCE.md#最小示例)
4. 开始编码！
```

### 路径 B: 全面理解 (45 分钟)

```
1. 阅读 [完整集成指南](MOD_SETTINGS_INTEGRATION_GUIDE.md) (20 分钟)
2. 学习 [常见陷阱](MOD_SETTINGS_PITFALLS_AND_BEST_PRACTICES.md) (15 分钟)
3. 参考 [API 速查表](MOD_SETTINGS_API_QUICK_REFERENCE.md) (5 分钟)
4. 开始编码！
```

### 路径 C: 成为专家 (120 分钟)

```
1. 阅读所有文档按此顺序:
   a. [集成指南](MOD_SETTINGS_INTEGRATION_GUIDE.md) (30 分钟)
   b. [陷阱 & 最佳实践](MOD_SETTINGS_PITFALLS_AND_BEST_PRACTICES.md) (30 分钟)
   c. [系统架构](MOD_SETTINGS_ARCHITECTURE.md) (45 分钟)
   d. [API 速查表](MOD_SETTINGS_API_QUICK_REFERENCE.md) (10 分钟)

2. 研究源代码:
   - ModSettingsManager.cs
   - ModSettingsIntegration.cs
   - ModToggleCloner.cs

3. 开始编码并做出贡献！
```

---

## 🎯 常见任务速查

### 任务 1: 添加一个开关
```csharp
mgr.AddToggle(mgr.ModContentParent, "启用功能", true, 
    (enabled) => SaveSetting("feature", enabled));
```
👉 详见: [API 速查表 - AddToggle](MOD_SETTINGS_API_QUICK_REFERENCE.md#addtoggle---添加开关)

### 任务 2: 添加一个下拉菜单
```csharp
mgr.AddDropdown(mgr.ModContentParent, "质量", 
    new List<string> { "低", "中", "高" }, 0,
    (index) => ApplyQuality(index));
```
👉 详见: [API 速查表 - AddDropdown](MOD_SETTINGS_API_QUICK_REFERENCE.md#adddropdown---添加下拉菜单)

### 任务 3: 保存用户设置
```csharp
void SaveSetting(string key, object value)
{
    PlayerPrefs.SetString(key, value.ToString());
    PlayerPrefs.Save();
}
```
👉 详见: [集成指南 - 保存和加载设置](MOD_SETTINGS_INTEGRATION_GUIDE.md#2-保存和加载设置)

### 任务 4: 正确初始化
```csharp
void Start() => StartCoroutine(InitSettings());

IEnumerator InitSettings()
{
    yield return null;
    if (ModSettingsManager.Instance?.IsInitialized == true)
    {
        AddMySettings();
    }
}
```
👉 详见: [陷阱 & 最佳实践 - 陷阱 1](MOD_SETTINGS_PITFALLS_AND_BEST_PRACTICES.md#-陷阱-1-时序问题---过早初始化)

### 任务 5: 处理设置变化副作用
```csharp
(index) =>
{
    StartCoroutine(ApplySettingAsync(index));
}

IEnumerator ApplySettingAsync(int level)
{
    // 异步处理，避免 UI 冻结
    yield return SomeLongOperation();
    SaveSetting("level", level);
}
```
👉 详见: [陷阱 & 最佳实践 - 陷阱 6](MOD_SETTINGS_PITFALLS_AND_BEST_PRACTICES.md#-陷阱-6-处理设置变化的副作用不当)

### 任务 6: 添加多个相关设置
```csharp
var parent = mgr.ModContentParent;

// 图形设置组
mgr.AddToggle(parent, "阴影", true, OnShadowChanged);
mgr.AddDropdown(parent, "阴影质量", qualities, 1, OnQualityChanged);

// 性能设置组
mgr.AddToggle(parent, "LOD", true, OnLODChanged);
```
👉 详见: [最佳实践 - 分组相关的设置](MOD_SETTINGS_PITFALLS_AND_BEST_PRACTICES.md#5-分组相关的设置)

---

## ❓ 快速问题解答

### Q: 我应该从哪个文档开始？
**A**: 如果你是第一次接触，从 [集成指南](MOD_SETTINGS_INTEGRATION_GUIDE.md) 开始。

### Q: 如何快速查找特定方法？
**A**: 使用 [API 速查表](MOD_SETTINGS_API_QUICK_REFERENCE.md)，用 Ctrl+F 搜索。

### Q: 我的代码有问题，怎么办？
**A**: 查看 [陷阱 & 最佳实践](MOD_SETTINGS_PITFALLS_AND_BEST_PRACTICES.md)，里面有 7 个常见错误的解决方案。

### Q: 为什么我的设置没有显示？
**A**: 检查 [陷阱 2](MOD_SETTINGS_PITFALLS_AND_BEST_PRACTICES.md#-陷阱-2-错误的回调签名) 和 [陷阱 3](MOD_SETTINGS_PITFALLS_AND_BEST_PRACTICES.md#-陷阱-3-使用错误的父容器)。

### Q: 我想理解系统如何工作？
**A**: 阅读 [系统架构](MOD_SETTINGS_ARCHITECTURE.md)，里面有详细的流程图和架构说明。

---

## 🔗 相关资源

### 源代码文件
- `iGPU Savior/UI/ModSettingsManager.cs` - 核心实现
- `iGPU Savior/UI/ModSettingsIntegration.cs` - Harmony 集成
- `iGPU Savior/UI/ModToggleCloner.cs` - UI 克隆逻辑

### 外部资源
- [BepInEx 官方文档](https://docs.bepinex.dev/)
- [Harmony 补丁指南](https://harmony.pardeike.net/)
- [Unity 官方文档](https://docs.unity3d.com/)

### 示例 MOD
查看 `docs/souce/` 目录下的源代码示例

---

## 💡 提示

### 💡 提示 1: 使用 Coroutine 处理异步初始化
```csharp
// ✅ 推荐
void Start()
{
    StartCoroutine(InitSettings());
}

IEnumerator InitSettings()
{
    yield return null;  // 等待一帧
    // 现在安全地访问 ModSettingsManager
}
```

### 💡 提示 2: 总是检查 IsInitialized
```csharp
// ✅ 推荐
if (ModSettingsManager.Instance?.IsInitialized == true)
{
    // 安全地使用 API
}
```

### 💡 提示 3: 保存用户设置
```csharp
// ✅ 重要
mgr.AddToggle(parent, "启用", savedValue,
    (newValue) =>
    {
        PlayerPrefs.SetInt("feature", newValue ? 1 : 0);
        PlayerPrefs.Save();
    }
);
```

### 💡 提示 4: 使用清晰的标签
```csharp
// ❌ 不清楚
mgr.AddToggle(parent, "启用 X", true, OnToggle);

// ✅ 清晰
mgr.AddToggle(parent, "启用实时阴影计算", true, OnToggle);
```

---

## 🎓 学习资源总结

| 资源 | 内容 | 级别 |
|------|------|------|
| 本页面 (README) | 导航和快速参考 | ⭐ 基础 |
| 集成指南 | 完整教程和示例 | ⭐⭐ 初级 |
| API 速查表 | 方法参考和代码片段 | ⭐⭐ 初级 |
| 陷阱 & 最佳实践 | 常见错误和解决方案 | ⭐⭐⭐ 中级 |
| 系统架构 | 深入设计和原理 | ⭐⭐⭐⭐ 高级 |

---

## 🚀 准备好了吗？

选择你的起点:

1. **快速上手?** → [MOD 设置集成指南](MOD_SETTINGS_INTEGRATION_GUIDE.md)
2. **只需要 API?** → [API 速查表](MOD_SETTINGS_API_QUICK_REFERENCE.md)
3. **遇到问题?** → [常见陷阱 & 最佳实践](MOD_SETTINGS_PITFALLS_AND_BEST_PRACTICES.md)
4. **想了解原理?** → [系统架构 & 流程图](MOD_SETTINGS_ARCHITECTURE.md)

---

## 📞 获取帮助

- 💬 **查看文档** - 使用 Ctrl+F 搜索关键词
- 📋 **查看检查清单** - 对照调试检查清单
- 🔍 **查看源代码** - 参考实现细节
- 📝 **报告问题** - 提交 Issue 到项目

---

祝你的 MOD 开发顺利！🚀

*最后更新: 2025-12-04*
