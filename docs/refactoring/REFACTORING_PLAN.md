# 代码重构计划文档

## 重构目标
将 iGPU Savior MOD 从单一大文件架构重构为模块化、易维护的结构。

## 新架构设计

### 文件夹结构
```
iGPU Savior/
├── Core/                      # 核心功能
│   ├── Enums.cs              # 枚举定义 ✅
│   ├── Constants.cs          # 常量定义 ✅
│   ├── PotatoPlugin.cs       # 插件入口
│   └── PotatoController.cs   # 主控制器
│
├── Configuration/             # 配置管理
│   └── ConfigurationManager.cs
│
├── Features/                  # 功能模块
│   ├── CameraMirrorManager.cs    # 相机镜像功能
│   ├── PortraitModeManager.cs    # 竖屏优化功能
│   ├── RenderQualityManager.cs   # 渲染质量管理
│   └── AudioManager.cs           # 音频管理
│
├── Utilities/                 # 工具类
│   ├── WindowManager.cs      # 窗口管理工具 ✅
│   └── TypeHelper.cs         # 类型辅助工具
│
├── UI/                        # UI 相关
│   ├── ModSettingsUI.cs      # MOD设置界面
│   ├── PulldownHelper.cs     # 下拉菜单辅助
│   └── ToggleHelper.cs       # 开关辅助
│
├── Patches/                   # Harmony 补丁
│   └── InputPatches.cs       # 输入系统补丁
│
└── Legacy/                    # 待重构的原始文件
    ├── Class1.cs (→ 将被拆分)
    ├── ModPulldownCloner.cs
    ├── ModSettingsIntegration.cs
    ├── ModSettingsManager.cs
    └── ModToggleCloner.cs
```

## 模块职责划分

### 1. Core (核心模块)
- **PotatoPlugin**: BepInEx 插件入口,负责初始化和协调各模块
- **PotatoController**: 主控制器,协调各功能模块的交互
- **Enums**: 所有枚举类型定义
- **Constants**: 全局常量定义

### 2. Configuration (配置模块)
- **ConfigurationManager**: 统一管理所有 BepInEx 配置项,提供类型安全的访问接口

### 3. Features (功能模块)
- **CameraMirrorManager**: 相机镜像功能的完整实现
- **PortraitModeManager**: 竖屏模式优化
- **RenderQualityManager**: 渲染质量和性能控制
- **AudioManager**: 音频处理(声道交换等)

### 4. Utilities (工具模块)
- **WindowManager**: 封装所有 Win32 API 调用
- **TypeHelper**: 反射和类型查找辅助方法

### 5. UI (界面模块)
- **ModSettingsUI**: MOD 设置界面的集成和管理
- **PulldownHelper**: 下拉菜单创建和管理
- **ToggleHelper**: 开关控件创建和管理

### 6. Patches (补丁模块)
- **InputPatches**: Harmony 补丁,用于输入系统的修改

## 重构原则

### 单一职责原则 (SRP)
每个类应该只有一个变更的理由。例如:
- WindowManager 只负责窗口操作
- CameraMirrorManager 只负责相机镜像
- ConfigurationManager 只负责配置管理

### 依赖倒置原则 (DIP)
高层模块不应依赖低层模块,两者都应依赖抽象。使用接口和抽象类来解耦。

### 开闭原则 (OCP)
对扩展开放,对修改封闭。新功能通过添加新类实现,而非修改现有代码。

## 命名空间组织

```csharp
PotatoOptimization                  // 根命名空间
├── Core                            // 核心功能
├── Configuration                    // 配置管理
├── Features                         // 功能模块
│   ├── Camera
│   ├── Rendering
│   └── Audio
├── Utilities                        // 工具类
├── UI                              // UI 组件
│   ├── Components
│   └── Helpers
└── Patches                          // Harmony 补丁
```

## 重构步骤

### 阶段 1: 基础设施 ✅
1. 创建文件夹结构
2. 创建 Enums.cs
3. 创建 Constants.cs
4. 创建 WindowManager.cs

### 阶段 2: 功能提取 (进行中)
5. 从 Class1.cs 提取 PotatoPlugin
6. 从 Class1.cs 提取 PotatoController
7. 创建 ConfigurationManager
8. 提取 CameraMirrorManager
9. 提取 PortraitModeManager
10. 提取 RenderQualityManager
11. 提取 AudioManager
12. 提取 InputPatches

### 阶段 3: UI 重构
13. 重构 ModSettingsIntegration → ModSettingsUI
14. 重构 ModPulldownCloner → PulldownHelper
15. 重构 ModToggleCloner → ToggleHelper
16. 优化 ModSettingsManager

### 阶段 4: 集成测试
17. 更新所有引用
18. 编译测试
19. 功能验证
20. 性能测试

## API 设计原则

### 1. 清晰的接口
```csharp
// ❌ 不好的设计
public void DoSomething(bool flag1, bool flag2, int mode);

// ✅ 好的设计
public void EnableMirrorMode(MirrorOptions options);
```

### 2. 使用属性而非公共字段
```csharp
// ❌ 不好
public bool isEnabled;

// ✅ 好
public bool IsEnabled { get; private set; }
```

### 3. 异常处理
所有公共 API 应该妥善处理异常,避免崩溃。

### 4. 日志记录
关键操作应记录日志,便于调试。

## 待解决问题

1. **循环依赖**: 确保各模块间无循环依赖
2. **状态同步**: 多个管理器间的状态同步机制
3. **事件系统**: 考虑引入事件系统解耦模块通信
4. **单例模式**: 是否使用单例,如何管理生命周期

## 后续优化建议

1. 引入依赖注入容器 (如 Zenject)
2. 实现完整的事件系统
3. 添加单元测试
4. 性能分析和优化
5. 文档生成 (XML 注释 + DocFX)

---
**创建日期**: 2025-12-04  
**当前状态**: 进行中  
**完成度**: 20%
