# 📚 iGPU Savior MOD 文档目录

## 🎯 MOD 设置集成文档

为了帮助其他 MOD 开发者轻松地将设置集成到游戏自带的设置界面，我们创建了一套完整的文档。

### 📖 核心文档 (5 份)

| 文档 | 用途 | 时间 | 难度 |
|------|------|------|------|
| **[MOD 设置 README](MOD_SETTINGS_README.md)** | 📋 导航中心，快速定位需要的文档 | 2-5 分钟 | ⭐ 简单 |
| **[MOD 设置集成指南](MOD_SETTINGS_INTEGRATION_GUIDE.md)** | 📖 完整教程，涵盖所有内容 | 20-30 分钟 | ⭐⭐ 初级 |
| **[API 速查表](MOD_SETTINGS_API_QUICK_REFERENCE.md)** | ⚡ 快速查询，方法参考 | 1-2 分钟 | ⭐⭐ 初级 |
| **[常见陷阱 & 最佳实践](MOD_SETTINGS_PITFALLS_AND_BEST_PRACTICES.md)** | 🚨 故障排查，避免错误 | 5-30 分钟 | ⭐⭐⭐ 中级 |
| **[系统架构 & 流程图](MOD_SETTINGS_ARCHITECTURE.md)** | 🏗️ 深度理解，系统设计 | 45-60 分钟 | ⭐⭐⭐⭐ 高级 |

### 📋 文档清单
- **[MOD 设置文档清单](MOD_SETTINGS_DOCUMENTATION_CHECKLIST.md)** - 所有文档的统计和总结

---

## 🚀 快速开始

### 我是新手 → 阅读这个顺序
1. [MOD 设置 README](MOD_SETTINGS_README.md) (2 分钟)
2. [集成指南 - 快速开始部分](MOD_SETTINGS_INTEGRATION_GUIDE.md#🚀-快速开始) (5 分钟)
3. [API 速查表 - 最小示例](MOD_SETTINGS_API_QUICK_REFERENCE.md#最小示例) (1 分钟)
4. **开始编码！**

### 我需要快速查询 → 使用这个
- [API 速查表](MOD_SETTINGS_API_QUICK_REFERENCE.md) - Ctrl+F 搜索

### 我遇到了问题 → 查看这个
- [常见陷阱 & 最佳实践](MOD_SETTINGS_PITFALLS_AND_BEST_PRACTICES.md) - 7 个常见错误的解决方案

### 我想深入学习 → 阅读所有文档
1. [集成指南](MOD_SETTINGS_INTEGRATION_GUIDE.md) (30 分钟)
2. [常见陷阱](MOD_SETTINGS_PITFALLS_AND_BEST_PRACTICES.md) (20 分钟)
3. [系统架构](MOD_SETTINGS_ARCHITECTURE.md) (60 分钟)

---

## 📂 文档组织结构

```
docs/
├── MOD_SETTINGS_README.md ⭐ 
│   └─ 导航中心，从这里开始
│
├── MOD_SETTINGS_INTEGRATION_GUIDE.md ⭐
│   ├─ 快速开始
│   ├─ 核心概念
│   ├─ API 文档
│   ├─ 完整示例 (2 个)
│   ├─ 最佳实践 (5 个)
│   ├─ 常见问题 (4 个)
│   └─ 故障排查
│
├── MOD_SETTINGS_API_QUICK_REFERENCE.md ⭐
│   ├─ 导入和单例
│   ├─ 方法参考表
│   ├─ 初始化模式
│   ├─ 常用代码片段
│   ├─ 安全检查清单
│   └─ 最小示例
│
├── MOD_SETTINGS_PITFALLS_AND_BEST_PRACTICES.md ⭐
│   ├─ 7 个常见陷阱 (每个都有 ❌ 和 ✅)
│   ├─ 5 个最佳实践汇总
│   ├─ 标准化初始化模式
│   ├─ 错误记录最佳实践
│   └─ 调试检查清单 (16 项)
│
├── MOD_SETTINGS_ARCHITECTURE.md ⭐
│   ├─ 系统架构图
│   ├─ 初始化时序图
│   ├─ 用户交互流程图
│   ├─ 代码组织结构
│   ├─ 依赖关系图
│   ├─ 线程安全性分析
│   ├─ 性能分析
│   ├─ 生命周期说明
│   └─ 深入理解
│
└── MOD_SETTINGS_DOCUMENTATION_CHECKLIST.md
    ├─ 文档统计
    ├─ 推荐阅读顺序
    ├─ 文档亮点总结
    └─ 更新日志
```

---

## 🎓 学习路径

### 路径 1: 快速上手 (15 分钟)
```
README (2 min) 
  → 快速开始 (5 min) 
    → 最小示例 (1 min) 
      → 开始编码 ✅
```

### 路径 2: 全面学习 (45 分钟)
```
集成指南 (30 min)
  → 常见陷阱 (15 min)
    → 开始编码 ✅
```

### 路径 3: 成为专家 (120 分钟)
```
集成指南 (30 min)
  → 常见陷阱 (30 min)
    → 系统架构 (45 min)
      → 研究源代码 (15 min)
        → 成为贡献者 ✅
```

---

## 💡 文档特色

### ✨ 集成指南的特色
- 🎯 3 个实际示例 (从简单到复杂)
- 📝 详细的概念说明
- 🔧 5 个最佳实践
- 🚨 4 个故障排查方案

### ✨ API 速查表的特色
- ⚡ 快速查询 (Ctrl+F)
- 📋 清晰的参考表
- 💻 可直接运行的代码
- ✅ 安全检查清单

### ✨ 陷阱 & 最佳实践的特色
- 🚨 7 个常见错误详解
- ✅ 每个都有 3+ 解决方案
- 📋 16 项调试清单
- 📝 标准化代码模板

### ✨ 系统架构的特色
- 🏗️ 5 个详细的流程图
- ⏱️ 毫秒级时序分析
- 📊 实际性能数据
- 🔍 深入设计原理

### ✨ README 的特色
- 🧭 快速导航
- 📋 场景导向
- 🎓 推荐学习路径
- 💻 任务速查

---

## 📊 文档覆盖范围

### API 功能
- ✅ `AddToggle()` - 添加开关
- ✅ `AddDropdown()` - 添加下拉菜单
- ✅ `ModSettingsManager.Instance` - 单例访问
- ✅ `ModContentParent` - 容器访问
- ✅ 属性和初始化

### 开发场景
- ✅ 简单场景 (单个设置)
- ✅ 中等场景 (多个相关设置)
- ✅ 复杂场景 (多层级系统)
- ✅ 问题排查和优化

### 知识层级
- ✅ 入门 (快速开始)
- ✅ 初级 (基础使用)
- ✅ 中级 (最佳实践)
- ✅ 高级 (系统设计)

### 常见问题
- ✅ 15+ 个常见问题
- ✅ 7 个常见错误
- ✅ 5+ 个故障排查方案
- ✅ 20+ 个最佳实践

---

## 🔗 相关资源

### 核心源代码
- `iGPU Savior/UI/ModSettingsManager.cs` - 核心实现 (324 行)
- `iGPU Savior/UI/ModSettingsIntegration.cs` - Harmony 集成 (940 行)
- `iGPU Savior/UI/ModToggleCloner.cs` - UI 克隆

### 外部资源
- [BepInEx 官方文档](https://docs.bepinex.dev/)
- [Harmony 补丁指南](https://harmony.pardeike.net/)
- [Unity 官方文档](https://docs.unity3d.com/)

---

## ⭐ 特别推荐

### 对于快速上手
👉 **先读这个**: [MOD 设置 README](MOD_SETTINGS_README.md)

### 对于快速查询
👉 **用这个**: [API 速查表](MOD_SETTINGS_API_QUICK_REFERENCE.md)

### 对于遇到问题
👉 **查这个**: [常见陷阱 & 最佳实践](MOD_SETTINGS_PITFALLS_AND_BEST_PRACTICES.md)

### 对于深入理解
👉 **学这个**: [系统架构 & 流程图](MOD_SETTINGS_ARCHITECTURE.md)

### 对于完整教程
👉 **看这个**: [MOD 设置集成指南](MOD_SETTINGS_INTEGRATION_GUIDE.md)

---

## 🎯 按职能选择文档

### 我是 MOD 开发者
→ [集成指南](MOD_SETTINGS_INTEGRATION_GUIDE.md) + [API 速查表](MOD_SETTINGS_API_QUICK_REFERENCE.md)

### 我遇到了问题
→ [常见陷阱 & 最佳实践](MOD_SETTINGS_PITFALLS_AND_BEST_PRACTICES.md)

### 我是系统设计者
→ [系统架构](MOD_SETTINGS_ARCHITECTURE.md)

### 我管理 MOD 生态
→ [文档清单](MOD_SETTINGS_DOCUMENTATION_CHECKLIST.md)

---

## 📈 文档质量

| 方面 | 评分 |
|------|------|
| **完整性** | ⭐⭐⭐⭐⭐ 覆盖所有功能 |
| **易用性** | ⭐⭐⭐⭐⭐ 多个入口点 |
| **代码质量** | ⭐⭐⭐⭐⭐ 50+ 个示例 |
| **深度** | ⭐⭐⭐⭐⭐ 入门到高级 |
| **可维护性** | ⭐⭐⭐⭐⭐ 清晰结构 |

---

## 🚀 准备好开始了吗？

选择你的起点：

| 情况 | 推荐文档 | 时间 |
|------|--------|------|
| 第一次使用 | [README](MOD_SETTINGS_README.md) | 2 min |
| 需要快速查询 | [API 速查表](MOD_SETTINGS_API_QUICK_REFERENCE.md) | 1 min |
| 代码出错了 | [陷阱 & 最佳实践](MOD_SETTINGS_PITFALLS_AND_BEST_PRACTICES.md) | 5-30 min |
| 想完全理解 | [集成指南](MOD_SETTINGS_INTEGRATION_GUIDE.md) | 30 min |
| 想学系统设计 | [系统架构](MOD_SETTINGS_ARCHITECTURE.md) | 60 min |

---

## 📞 需要帮助？

- 📋 **不知道从哪里开始?** → 阅读 [MOD 设置 README](MOD_SETTINGS_README.md)
- 🔍 **需要快速查询?** → 使用 [API 速查表](MOD_SETTINGS_API_QUICK_REFERENCE.md)
- 🚨 **代码有问题?** → 查看 [常见陷阱](MOD_SETTINGS_PITFALLS_AND_BEST_PRACTICES.md)
- 🏗️ **想深入理解?** → 学习 [系统架构](MOD_SETTINGS_ARCHITECTURE.md)
- 📚 **想要全部?** → 按顺序阅读所有文档

---

**版本**: v1.0  
**最后更新**: 2025-12-04  
**维护**: iGPU Savior MOD 团队

祝你的 MOD 开发顺利！🚀
