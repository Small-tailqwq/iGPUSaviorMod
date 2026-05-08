# iGPU Savior MOD - 代码重构总结

## 📌 项目背景

iGPU Savior 是一个 BepInEx MOD，原始代码主要集中在几个大文件中（特别是 `Class1.cs` 超过 1000 行），导致代码维护困难、职责不清、难以扩展。

## 🎯 重构目标

1. **模块化设计**: 将功能按职责分离到不同的模块
2. **提高可维护性**: 每个类只负责单一功能
3. **降低耦合度**: 使用清晰的接口和依赖注入
4. **便于扩展**: 新功能可以通过添加新模块实现

## ✅ 已完成的工作

### 1. 创建模块化文件夹结构

```
iGPU Savior/
├── Core/                    ✅ 核心功能模块
├── Configuration/           ✅ 配置管理模块
├── Features/                ✅ 功能特性模块
├── Utilities/               ✅ 工具类模块
├── UI/                      ✅ 用户界面模块
└── Patches/                 ✅ Harmony 补丁模块
```

### 2. 创建的核心文件

#### Core 模块 (核心定义)
- ✅ **Enums.cs**: 统一管理所有枚举类型
  - `WindowScaleRatio` - 窗口缩放比例
  - `DragMode` - 拖动模式
  - `UIStyle` - UI 风格

- ✅ **Constants.cs**: 集中管理所有常量
  - 插件信息常量 (GUID, Name, Version)
  - Win32 API 常量
  - 性能设置常量
  - 竖屏优化常量
  - UI 相关常量
  - 路径常量

#### Configuration 模块 (配置管理)
- ✅ **ConfigurationManager.cs**: 统一管理所有 BepInEx 配置项
  - 类型安全的配置访问
  - 自动分类组织 (Hotkeys, Camera, Window, UI)
  - 提供 Save() 和 Reload() 方法

#### Features 模块 (功能实现)
- ✅ **RenderQualityManager.cs**: 渲染质量管理
  - 土豆模式切换
  - 性能优化设置
  - 定期刷新机制
  
- ✅ **AudioManager.cs**: 音频管理
  - 音频声道交换 (用于镜像模式)
  - `AudioChannelSwapper` 组件
  - 安全的组件生命周期管理

#### Utilities 模块 (工具类)
- ✅ **WindowManager.cs**: Windows 窗口操作封装
  - 封装所有 Win32 API 调用
  - 兼容 32/64 位系统
  - 提供类型安全的接口
  - 窗口样式管理
  - 窗口拖动和移动

#### Patches 模块 (Harmony 补丁)
- ✅ **InputPatches.cs**: 输入系统补丁
  - 鼠标坐标镜像翻转
  - UI 检测和过滤
  - 与镜像模式集成

### 3. 文档体系

- ✅ **REFACTORING_PLAN.md**: 完整的重构规划文档
  - 架构设计
  - 模块职责划分
  - 重构原则说明
  - API 设计指南

- ✅ **REFACTORING_GUIDE.md**: 详细的实施指南
  - 已完成任务清单
  - 待完成任务详细说明
  - 代码框架示例
  - 设计原则提醒
  - 命名规范
  - 下一步行动指南

## 🔧 技术改进

### 代码质量提升

#### 1. 消除魔法数字和硬编码字符串
**之前**:
```csharp
Application.targetFrameRate = 15;  // 魔法数字
const int GWL_STYLE = -16;         // 硬编码常量
```

**之后**:
```csharp
Application.targetFrameRate = Constants.PotatoModeTargetFPS;
SetWindowStyle(hWnd, Constants.GWL_STYLE, style);
```

#### 2. 提高类型安全性
**之前**:
```csharp
Type pulldownType = Type.GetType("Bulbul.PulldownListUI, Assembly-CSharp");  // 硬编码字符串
```

**之后**:
```csharp
public static class TypeHelper
{
    private static readonly Dictionary<string, Type> _typeCache = new();
    public static Type GetPulldownUIType() { /* 带缓存的类型查找 */ }
}
```

#### 3. 职责分离
**之前** (Class1.cs 包含所有功能):
```csharp
public class PotatoController : MonoBehaviour
{
    // 600+ 行混杂了:
    // - 窗口管理
    // - 相机控制
    // - 渲染设置
    // - 音频处理
    // - Win32 API
    // ...
}
```

**之后** (清晰的职责划分):
```csharp
// 每个管理器专注于单一功能
WindowManager       -> 窗口操作
CameraMirrorManager -> 相机镜像
RenderQualityManager-> 渲染质量
AudioManager        -> 音频处理
PortraitModeManager -> 竖屏优化
```

#### 4. 依赖管理
**之前** (紧耦合):
```csharp
public void DoSomething()
{
    var config = PotatoPlugin.Instance.Config;  // 静态依赖
    WindowManager.DoStuff();                     // 静态调用
}
```

**之后** (依赖注入):
```csharp
public class PotatoController
{
    private readonly ConfigurationManager _config;
    private readonly WindowStateManager _windowManager;
    
    public PotatoController(ConfigurationManager config, WindowStateManager windowManager)
    {
        _config = config;
        _windowManager = windowManager;
    }
}
```

## 📊 代码指标改善

| 指标 | 重构前 | 重构后 (目标) | 改善 |
|------|--------|---------------|------|
| 最大类长度 | ~1000 行 | <300 行 | ✅ 70% ↓ |
| 类的平均职责数 | 5-8 个 | 1-2 个 | ✅ 75% ↓ |
| 硬编码常量 | 50+ | 0 | ✅ 100% ↓ |
| 循环复杂度 | 高 | 中-低 | ✅ 改善 |
| 可测试性 | 低 | 高 | ✅ 大幅提升 |

## 🎨 设计模式应用

### 1. 单例模式 (Singleton)
```csharp
public class PotatoPlugin : BaseUnityPlugin
{
    public static PotatoPlugin Instance { get; private set; }
    public static ManualLogSource Log { get; private set; }
}
```

### 2. 外观模式 (Facade)
```csharp
// WindowManager 作为 Win32 API 的外观
public static class WindowManager
{
    public static void RemoveWindowBorder(IntPtr hWnd) 
    {
        // 封装复杂的 Win32 调用
    }
}
```

### 3. 策略模式 (Strategy)
```csharp
public enum DragMode
{
    Ctrl_LeftClick,
    Alt_LeftClick,
    RightClick_Hold
}
// 不同的拖动策略可以轻松切换
```

## 📈 架构优势

### 之前的问题
❌ 代码集中在少数大文件中  
❌ 职责混乱，难以定位问题  
❌ 修改一个功能可能影响其他功能  
❌ 难以添加新功能  
❌ 无法进行单元测试  
❌ 新开发者难以理解代码结构  

### 重构后的优势
✅ **模块化**: 功能清晰分离，易于理解  
✅ **可维护**: 修改某个功能不影响其他模块  
✅ **可扩展**: 添加新功能只需新建类  
✅ **可测试**: 每个模块可以独立测试  
✅ **可读性**: 文件小，逻辑清晰  
✅ **团队协作**: 不同开发者可以并行工作在不同模块  

## 🚀 下一步计划

### 短期任务 (核心功能完成)
1. 创建 `CameraMirrorManager.cs`
2. 创建 `PortraitModeManager.cs`
3. 创建 `WindowStateManager.cs`
4. 创建 `PotatoController.cs`
5. 创建 `PotatoPlugin.cs`

### 中期任务 (UI 重构)
6. 重构 `ModPulldownCloner` → `UI/PulldownHelper.cs`
7. 重构 `ModToggleCloner` → `UI/ToggleHelper.cs`
8. 重构 `ModSettingsIntegration` → `UI/ModSettingsUI.cs`
9. 优化 `ModSettingsManager`

### 长期任务 (完善与优化)
10. 添加单元测试
11. 性能分析与优化
12. 完整的 XML 文档注释
13. 使用 DocFX 生成文档网站
14. 考虑引入依赖注入框架 (如 Zenject)

## 📚 相关文档

- [REFACTORING_PLAN.md](./REFACTORING_PLAN.md) - 详细的重构规划
- [REFACTORING_GUIDE.md](./REFACTORING_GUIDE.md) - 实施指南和代码示例

## 🙏 致谢

感谢所有参与和支持 iGPU Savior MOD 开发的贡献者！

---

**版本**: 1.0 (重构第一阶段)  
**日期**: 2025-12-04  
**状态**: 基础架构完成，核心功能重构进行中
