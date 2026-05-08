# 代码重构实施指南

## 📋 重构进度概览

### ✅ 已完成 (第一阶段 - 基础架构)

#### 1. 文件夹结构
- ✅ `Core/` - 核心功能模块
- ✅ `Configuration/` - 配置管理
- ✅ `Features/` - 功能特性模块
- ✅ `Utilities/` - 工具类
- ✅ `UI/` - 用户界面
- ✅ `Patches/` - Harmony 补丁

#### 2. 已创建的核心文件

**Core 模块:**
- ✅ `Core/Enums.cs` - 枚举定义 (WindowScaleRatio, DragMode, UIStyle)
- ✅ `Core/Constants.cs` - 全局常量定义

**Configuration 模块:**
- ✅ `Configuration/ConfigurationManager.cs` - 统一配置管理

**Features 模块:**
- ✅ `Features/RenderQualityManager.cs` - 渲染质量管理
- ✅ `Features/AudioManager.cs` - 音频管理 (包含 AudioChannelSwapper)

**Utilities 模块:**
- ✅ `Utilities/WindowManager.cs` - Win32 窗口操作封装

**Patches 模块:**
- ✅ `Patches/InputPatches.cs` - 输入系统 Harmony 补丁

---

## 🔄 待完成任务

### 阶段 2: 继续拆分 Class1.cs

#### 任务 2.1: 创建相机镜像管理器
**文件**: `Features/CameraMirrorManager.cs`

**需要提取的功能**:
- `EnableMirrorMode()` / `DisableMirrorMode()`
- `CreateMirrorRenderTexture()`
- `CreateMirrorCanvas()`
- `CreateFlipMaterial()`
- `RecreateRenderTexture()`
- `CheckAndHandleResolutionChange()`
- 所有 RenderTexture 相关的字段和逻辑

**依赖关系**:
- 使用 `AudioManager` 进行音频交换
- 控制 `InputPatches.IsInputMirrored` 标志

---

#### 任务 2.2: 创建竖屏模式管理器
**文件**: `Features/PortraitModeManager.cs`

**需要提取的功能**:
- `CheckAndHandlePortraitMode()`
- `SaveOriginalCameraParams()`
- `ApplyPortraitCameraAdjustment()`
- `RestoreOriginalCameraParams()`
- `TogglePortraitOptimization()`
- 所有相机参数保存和恢复的字段

**设计建议**:
```csharp
public class PortraitModeManager
{
    private bool isEnabled;
    private bool isPortraitMode;
    private CameraParameters originalParams;
    
    public void Update(Camera mainCamera)
    {
        // 检测并处理竖屏模式切换
    }
    
    public void Toggle() { /* ... */ }
}
```

---

#### 任务 2.3: 创建主控制器
**文件**: `Core/PotatoController.cs`

**职责**:
- 协调各个功能模块
- 处理快捷键输入
- 管理模式切换逻辑
- 场景加载回调

**依赖的管理器**:
- `ConfigurationManager` - 配置访问
- `RenderQualityManager` - 渲染质量
- `CameraMirrorManager` - 相机镜像
- `PortraitModeManager` - 竖屏优化
- `WindowStateManager` (需创建) - 窗口状态管理
- `AudioManager` - 音频处理

**代码框架**:
```csharp
public class PotatoController : MonoBehaviour
{
    private ConfigurationManager config;
    private RenderQualityManager renderManager;
    private CameraMirrorManager mirrorManager;
    private PortraitModeManager portraitManager;
    private WindowStateManager windowManager;
    
    void Start() { /* 初始化各管理器 */ }
    void Update() { /* 处理输入和更新 */ }
    void OnDestroy() { /* 清理资源 */ }
    
    private void HandleHotkeyInput() { /* 快捷键处理 */ }
}
```

---

#### 任务 2.4: 创建窗口状态管理器
**文件**: `Features/WindowStateManager.cs`

**需要提取的功能**:
- `ToggleWindowMode()` - PiP 模式切换
- `SetPiPMode()` - 设置画中画模式
- `HandleDragLogic()` - 拖动逻辑
- `CalculateTargetResolution()` - 计算目标分辨率
- 所有窗口状态相关的字段 (origWidth, origHeight, origStyle 等)

**依赖**:
- `WindowManager` (工具类) - Win32 API 调用
- `ConfigurationManager` - 获取配置

---

#### 任务 2.5: 创建插件入口
**文件**: `Core/PotatoPlugin.cs`

**内容**:
```csharp
[BepInPlugin(Constants.PluginGUID, Constants.PluginName, Constants.PluginVersion)]
public class PotatoPlugin : BaseUnityPlugin
{
    public static PotatoPlugin Instance;
    public static ManualLogSource Log;
    public static ConfigurationManager Config;
    
    private GameObject runnerObject;
    
    void Awake()
    {
        Instance = this;
        Log = Logger;
        Config = new ConfigurationManager(base.Config);
        
        ApplyHarmonyPatches();
        CreateController();
        
        Log.LogWarning($">>> {Constants.PluginName} v{Constants.PluginVersion} 启动成功 <<<");
    }
    
    private void ApplyHarmonyPatches() { /* ... */ }
    private void CreateController() { /* ... */ }
}
```

---

### 阶段 3: 重构 UI 模块

#### 任务 3.1: 重构 ModPulldownCloner
**新文件**: `UI/PulldownHelper.cs`

**改进点**:
1. 将类型查找逻辑提取到 `Utilities/TypeHelper.cs`
2. 将方法按功能分组 (克隆、配置、选项管理)
3. 添加详细的 XML 注释
4. 简化方法签名,使用参数对象

**示例**:
```csharp
public class PulldownHelper
{
    public static GameObject Clone(Transform settingRoot) { /* ... */ }
    public static void AddOption(GameObject pulldown, OptionConfig config) { /* ... */ }
    public static void Configure(GameObject pulldown, PulldownConfig config) { /* ... */ }
}

public class OptionConfig
{
    public string Text;
    public Action OnClick;
}

public class PulldownConfig
{
    public int VisibleItems = 6;
    public float ItemHeight = 40f;
    // ...
}
```

---

#### 任务 3.2: 重构 ModToggleCloner
**新文件**: `UI/ToggleHelper.cs`

**改进点**:
1. 提取按钮状态管理为独立方法
2. 添加音效播放的统一接口
3. 支持更灵活的样式定制

---

#### 任务 3.3: 重构 ModSettingsIntegration
**新文件**: `UI/ModSettingsUI.cs`

**拆分为多个类**:
- `UI/ModSettingsUI.cs` - 主入口和 Harmony Patch
- `UI/SettingsTabManager.cs` - 标签页管理
- `UI/SettingsLayoutManager.cs` - 布局管理
- `UI/SettingsResourceCache.cs` - 资源缓存 (字体、精灵等)

---

#### 任务 3.4: 优化 ModSettingsManager
**当前问题**:
- 职责不清晰
- 缺少设置持久化
- 缺少验证逻辑

**改进方向**:
1. 明确为"MOD设置注册中心"的角色
2. 添加设置值验证
3. 支持设置导入/导出
4. 提供设置变更事件

---

### 阶段 4: 工具类完善

#### 任务 4.1: 创建类型辅助工具
**文件**: `Utilities/TypeHelper.cs`

```csharp
public static class TypeHelper
{
    private static readonly Dictionary<string, Type> _typeCache = new();
    
    public static Type GetPulldownUIType()
    {
        if (_typeCache.TryGetValue("PulldownUI", out var type))
            return type;
            
        type = Type.GetType("Bulbul.PulldownListUI, Assembly-CSharp")
            ?? Type.GetType("PulldownListUI, Assembly-CSharp")
            ?? Type.GetType("PulldownListUI");
            
        if (type != null)
            _typeCache["PulldownUI"] = type;
            
        return type;
    }
    
    // 其他类型查找方法...
}
```

---

### 阶段 5: 集成与测试

#### 5.1 更新 Class1.cs
- 删除已迁移的代码
- 保留向后兼容的桥接代码 (如果需要)
- 添加 `[Obsolete]` 标记废弃的类

#### 5.2 更新项目引用
确保 `.csproj` 包含所有新文件:
```xml
<Compile Include="Core\Enums.cs" />
<Compile Include="Core\Constants.cs" />
<Compile Include="Configuration\ConfigurationManager.cs" />
<!-- ... 所有新文件 ... -->
```

#### 5.3 编译测试
```powershell
dotnet build "c:\Users\email\source\repos\iGPU Savior\iGPU Savior\iGPU Savior.csproj" -c Release
```

#### 5.4 功能验证清单
- [ ] 土豆模式切换 (F2)
- [ ] PiP 模式切换 (F3)
- [ ] 相机镜像切换 (F4)
- [ ] 竖屏优化切换 (F5)
- [ ] MOD 设置界面显示
- [ ] 下拉菜单交互
- [ ] 开关交互
- [ ] 窗口拖动 (三种模式)
- [ ] 音频声道交换
- [ ] 配置持久化

---

## 🎯 设计原则提醒

### 1. 单一职责
每个类只做一件事,例如:
- ❌ `PotatoController` 不应该包含 Win32 API 调用
- ✅ `PotatoController` 委托给 `WindowManager` 处理窗口操作

### 2. 依赖注入
优先通过构造函数传递依赖:
```csharp
// ✅ 好的设计
public class PotatoController
{
    private readonly ConfigurationManager _config;
    
    public PotatoController(ConfigurationManager config)
    {
        _config = config;
    }
}

// ❌ 不好的设计
public class PotatoController
{
    void Start()
    {
        var config = PotatoPlugin.Config; // 紧耦合
    }
}
```

### 3. 错误处理
所有公共方法应处理异常:
```csharp
public bool TryEnableMirror()
{
    try
    {
        EnableMirrorMode();
        return true;
    }
    catch (Exception e)
    {
        PotatoPlugin.Log.LogError($"启用镜像失败: {e}");
        return false;
    }
}
```

### 4. 日志记录
使用统一的日志级别:
- `LogInfo` - 普通信息
- `LogWarning` - 用户可见的状态变化
- `LogError` - 错误信息

---

## 📝 命名规范

### 类名
- 使用 PascalCase
- 管理器类以 `Manager` 结尾
- 辅助类以 `Helper` 结尾
- 工具类以 `Utility` 或直接使用描述性名词

### 方法名
- 使用 PascalCase
- 动词开头 (`Enable`, `Disable`, `Create`, `Update`)
- 布尔返回值以 `Is`, `Has`, `Can` 开头

### 字段和属性
- 私有字段: `_camelCase` (下划线前缀)
- 公共属性: `PascalCase`
- 常量: `UPPER_SNAKE_CASE` 或 `PascalCase`

### 文件名
- 与主类名一致
- 一个文件只包含一个公共类 (辅助类除外)

---

## 🚀 下一步行动

1. **立即执行**: 完成 `Features/CameraMirrorManager.cs` 的创建
2. **然后**: 创建 `Features/PortraitModeManager.cs`
3. **接着**: 创建 `Features/WindowStateManager.cs`
4. **最后**: 整合到 `Core/PotatoController.cs`

---

## 📚 参考资源

- [C# 编码规范](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [Unity 最佳实践](https://unity.com/how-to/programming-unity)
- [SOLID 原则](https://en.wikipedia.org/wiki/SOLID)

---

**文档版本**: 1.0  
**最后更新**: 2025-12-04  
**维护者**: iGPU Savior 开发团队
