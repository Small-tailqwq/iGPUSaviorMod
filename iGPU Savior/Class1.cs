using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PotatoOptimization
{
    // ==========================================
    // 1. 定义枚举
    // ==========================================
    public enum WindowScaleRatio
    {
        OneThird = 3,   // 1/3
        OneFourth = 4,  // 1/4
        OneFifth = 5    // 1/5
    }

    public enum DragMode
    {
        Ctrl_LeftClick, // Ctrl + 左键 (最推荐，系统级丝滑)
        Alt_LeftClick,  // Alt + 左键
        RightClick_Hold // 右键按住 (手动计算，已修复抽搐)
    }

    public enum UIStyle
    {
        Modern,      // 现代化Chrome风格 (CreateModernDropdown)
        GameNative   // 游戏原生风格 (ModPulldownCloner)
    }

    // ==========================================
    // 2. 插件入口
    // ==========================================
    [BepInPlugin("chillwithyou.potatomode", "Potato Mode Optimization", "1.6.0")]
    public class PotatoPlugin : BaseUnityPlugin
    {
        public static PotatoPlugin Instance;
        public static ManualLogSource Log;

        public static ConfigEntry<KeyCode> KeyPotatoMode;
        public static ConfigEntry<KeyCode> KeyPiPMode;
        public static ConfigEntry<KeyCode> KeyCameraMirror; // 镜像摄像机快捷键
        public static ConfigEntry<bool> CfgEnableMirror; // 启动时是否启用镜像
        public static ConfigEntry<WindowScaleRatio> CfgWindowScale;
        public static ConfigEntry<DragMode> CfgDragMode;
        public static ConfigEntry<UIStyle> CfgUIStyle; // UI风格选择

        private GameObject runnerObject;

        void Awake()
        {
            Instance = this;
            Log = Logger;

            InitConfig();
            
            // 应用Harmony补丁
            try
            {
                var harmony = new HarmonyLib.Harmony("chillwithyou.potatomode");
                harmony.PatchAll();
                Log.LogWarning(">>> Harmony patches applied successfully! <<<");
            }
            catch (Exception e)
            {
                Log.LogError($"Failed to apply Harmony patches: {e}");
            }
            
            Log.LogWarning(">>> [V1.6] 插件启动：修复右键抽搐 & 窗口样式还原 & MOD设置UI <<<");

            runnerObject = new GameObject("PotatoRunner");
            DontDestroyOnLoad(runnerObject);
            runnerObject.hideFlags = HideFlags.HideAndDontSave;
            runnerObject.AddComponent<PotatoController>();
        }

        private void InitConfig()
        {
            KeyPotatoMode = Config.Bind("Hotkeys", "PotatoModeKey", KeyCode.F2, "切换土豆模式的按键");
            KeyPiPMode = Config.Bind("Hotkeys", "PiPModeKey", KeyCode.F3, "切换画中画小窗的按键");
            KeyCameraMirror = Config.Bind("Hotkeys", "CameraMirrorKey", KeyCode.F4, "切换摄像机镜像的按键(左右翻转画面)");
            CfgEnableMirror = Config.Bind("Camera", "EnableMirrorOnStart", false, "启动时是否自动启用摄像机镜像(默认关闭,建议先用UE Explorer测试)");
            CfgWindowScale = Config.Bind("Window", "ScaleRatio", WindowScaleRatio.OneThird, "小窗缩放比例");
            CfgDragMode = Config.Bind("Window", "DragMethod", DragMode.Ctrl_LeftClick, "拖动方式");
            CfgUIStyle = Config.Bind("UI", "Style", UIStyle.Modern, "MOD设置界面风格 (Modern=现代化Chrome风格, GameNative=游戏原生风格)");
        }
    }

    // ==========================================
    // 3. 逻辑控制器
    // ==========================================
    public class PotatoController : MonoBehaviour
    {
        private bool isPotatoMode = false; // ✨ Default to false
        private bool isSmallWindow = false;
        private bool isCameraMirrored = false; // 新增：镜像状态

        // === 镜像模式相关 ===
        private RenderTexture mirrorRenderTexture;
        private GameObject mirrorCanvas;
        private RawImage mirrorRawImage;
        private Material mirrorFlipMaterial;
        private int lastScreenWidth;
        private int lastScreenHeight;
        private AudioChannelSwapper audioSwapper; // 音频声道交换组件

        private float targetRenderScale = 0.4f;
        private int currentTargetWidth;
        private int currentTargetHeight;

        private float lastRunTime = 0f;
        private float runInterval = 3.0f;

        // === 记忆变量 ===
        private int origWidth;
        private int origHeight;
        private FullScreenMode origMode;
        private IntPtr origStyle; // ✨ Changed to IntPtr for 64-bit compatibility

        // ==========================================
        // ✨ Windows API 
        // ==========================================
        [DllImport("user32.dll")] private static extern IntPtr GetActiveWindow();
        
        // 32-bit and 64-bit compatible P/Invoke
        [DllImport("user32.dll", EntryPoint = "SetWindowLong")] private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")] private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
        
        [DllImport("user32.dll", EntryPoint = "GetWindowLong")] private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")] private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        private static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8) return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            else return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
        }

        private static IntPtr GetWindowLong(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 8) return GetWindowLongPtr64(hWnd, nIndex);
            else return new IntPtr(GetWindowLong32(hWnd, nIndex));
        }

        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        
        // ✨ 新增：获取屏幕绝对坐标 (解决抽搐的关键)
        [DllImport("user32.dll")] 
        [return: MarshalAs(UnmanagedType.Bool)] 
        static extern bool GetCursorPos(out POINT lpPoint);
        
        [StructLayout(LayoutKind.Sequential)] 
        public struct POINT 
        { 
            public int X; 
            public int Y; 
        }

        [DllImport("user32.dll")] 
        [return: MarshalAs(UnmanagedType.Bool)] 
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        
        [StructLayout(LayoutKind.Sequential)] 
        public struct RECT 
        { 
            public int Left; 
            public int Top; 
            public int Right; 
            public int Bottom; 
        }

        // 常量
        private const int GWL_STYLE = -16;
        private const uint WS_CAPTION = 0x00C00000;
        private const uint WS_THICKFRAME = 0x00040000;
        private const uint WS_SYSMENU = 0x00080000;
        
        private const uint SWP_FRAMECHANGED = 0x0020;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint SWP_NOZORDER = 0x0004;

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        // 右键拖动变量
        private POINT dragStartScreenPos; // ✨ 改用 POINT 记录屏幕绝对坐标
        private Vector2 dragStartWindowPos;
        private bool isRightDragging = false;

        // === 镜像模式输入标志 ===
        public static bool isInputMirrored = false;

        void Start()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            if (isPotatoMode) ApplyPotatoMode(false);

            // 初始记录一下，以防万一
            origWidth = Screen.width;
            origHeight = Screen.height;
            origMode = Screen.fullScreenMode;
            // ✨ 获取当前窗口样式 (带边框/不带边框)
            IntPtr hWnd = GetActiveWindow();
            origStyle = GetWindowLong(hWnd, GWL_STYLE);

            // 初始化分辨率跟踪
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;

            // 根据配置决定是否启动时启用镜像
            if (PotatoPlugin.CfgEnableMirror.Value)
            {
                StartCoroutine(ApplyMirrorOnStart());
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(PotatoPlugin.KeyPotatoMode.Value))
            {
                isPotatoMode = !isPotatoMode;
                if (isPotatoMode) 
                { 
                    ApplyPotatoMode(true); 
                    PotatoPlugin.Log.LogWarning(">>> 土豆模式: ON <<<"); 
                }
                else 
                { 
                    RestoreQuality(); 
                    PotatoPlugin.Log.LogWarning(">>> 土豆模式: OFF <<<"); 
                }
            }

            if (Input.GetKeyDown(PotatoPlugin.KeyPiPMode.Value))
            {
                ToggleWindowMode();
            }

            if (Input.GetKeyDown(PotatoPlugin.KeyCameraMirror.Value))
            {
                ToggleCameraMirror();
            }

            // ✨ 检测分辨率变化（镜像模式下）
            if (isCameraMirrored)
            {
                CheckAndHandleResolutionChange();
            }

            if (isSmallWindow)
            {
                HandleDragLogic();
            }

            if (isPotatoMode)
            {
                if (Time.realtimeSinceStartup - lastRunTime > runInterval)
                {
                    ApplyPotatoMode(false);
                    lastRunTime = Time.realtimeSinceStartup;
                }
            }
        }

        // === ✨ 分辨率变化检测 ===
        private void CheckAndHandleResolutionChange()
        {
            if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
            {
                PotatoPlugin.Log.LogInfo($"检测到分辨率变化: {lastScreenWidth}x{lastScreenHeight} -> {Screen.width}x{Screen.height}");
                
                lastScreenWidth = Screen.width;
                lastScreenHeight = Screen.height;

                // 重建 RenderTexture
                RecreateRenderTexture();
            }
        }

        private void RecreateRenderTexture()
        {
            if (!isCameraMirrored || mirrorRenderTexture == null)
                return;

            try
            {
                Camera mainCam = Camera.main;
                if (mainCam == null)
                    return;

                // 1. 暂时断开摄像机
                mainCam.targetTexture = null;

                // 2. 释放旧 RT
                mirrorRenderTexture.Release();

                // 3. 创建新 RT
                mirrorRenderTexture = CreateMirrorRenderTexture();

                // 4. 重新连接
                mainCam.targetTexture = mirrorRenderTexture;
                if (mirrorRawImage != null)
                {
                    mirrorRawImage.texture = mirrorRenderTexture;
                }

                PotatoPlugin.Log.LogInfo("已重建 RenderTexture，适应新分辨率");
            }
            catch (Exception e)
            {
                PotatoPlugin.Log.LogError($"重建 RenderTexture 失败: {e.Message}");
            }
        }

        // === ✨ 修复后的 F3 切换逻辑 ===
        private void ToggleWindowMode()
        {
            IntPtr hWnd = GetActiveWindow();

            if (!isSmallWindow)
            {
                // [进小窗]
                // 1. 存档
                origWidth = Screen.width;
                origHeight = Screen.height;
                origMode = Screen.fullScreenMode;
                origStyle = GetWindowLong(hWnd, GWL_STYLE); // ✨ 关键：记下当前有没有边框

                // 2. 变身
                isSmallWindow = true;
                CalculateTargetResolution();
                Screen.SetResolution(currentTargetWidth, currentTargetHeight, FullScreenMode.Windowed);
                
                // 3. 去边框+置顶
                StartCoroutine(SetPiPMode(true));
                
                PotatoPlugin.Log.LogWarning($">>> 开启画中画 (原始样式已备份) <<<");
            }
            else
            {
                // [回全屏]
                // 1. 状态复位
                isSmallWindow = false;

                // 2. 还原分辨率
                Screen.SetResolution(origWidth, origHeight, origMode);

                // 3. 还原样式+取消置顶
                StartCoroutine(SetPiPMode(false));
                
                PotatoPlugin.Log.LogWarning(">>> 恢复原始状态 <<<");
            }
        }

        // === ✨ 启动时应用镜像 ===
        private IEnumerator ApplyMirrorOnStart()
        {
            // 等待几帧，确保场景和摄像机已加载
            yield return new WaitForSeconds(0.5f);
            
            if (PotatoPlugin.CfgEnableMirror.Value)
            {
                isCameraMirrored = true;
                ApplyCameraMirror();
                PotatoPlugin.Log.LogWarning(">>> 启动时已自动启用摄像机镜像 <<<");
            }
        }

        // === ✨ 新增：摄像机镜像功能 ===
        private void ToggleCameraMirror()
        {
            isCameraMirrored = !isCameraMirrored;
            ApplyCameraMirror();
        }

        // ✅ 修复后的镜像实现：使用 RenderTexture + UV 翻转，而非投影矩阵
        // ✅ 改为 public，允许从 ModSettingsIntegration 调用
        public void ApplyCameraMirror()
        {
            if (isCameraMirrored)
            {
                EnableMirrorMode();
            }
            else
            {
                DisableMirrorMode();
            }
        }

        private void EnableMirrorMode()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                PotatoPlugin.Log.LogWarning(">>> 未找到主摄像机，跳过镜像 <<<");
                return;
            }

            try
            {
                // 1. 创建 RenderTexture
                mirrorRenderTexture = CreateMirrorRenderTexture();
                mainCam.targetTexture = mirrorRenderTexture;

                // 2. 创建翻转材质
                mirrorFlipMaterial = CreateFlipMaterial();

                // 3. 创建全屏 Canvas + RawImage
                CreateMirrorCanvas();

                // 4. 启用输入镜像
                isInputMirrored = true;

                // 5. 启用音频声道交换
                EnableAudioSwap();

                PotatoPlugin.Log.LogWarning(">>> 镜像模式: ON (RenderTexture 方案，不破坏法线) <<<");
            }
            catch (Exception e)
            {
                PotatoPlugin.Log.LogError($"启用镜像模式失败: {e.Message}");
                DisableMirrorMode(); // 清理失败的资源
            }
        }

        private void DisableMirrorMode()
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.targetTexture = null;
            }

            // 清理资源
            if (mirrorCanvas != null) 
            {
                Destroy(mirrorCanvas);
                mirrorCanvas = null;
            }
            
            if (mirrorRenderTexture != null) 
            {
                mirrorRenderTexture.Release();
                mirrorRenderTexture = null;
            }
            
            if (mirrorFlipMaterial != null) 
            {
                Destroy(mirrorFlipMaterial);
                mirrorFlipMaterial = null;
            }

            mirrorRawImage = null;

            // 禁用输入镜像
            isInputMirrored = false;

            // 禁用音频声道交换
            DisableAudioSwap();

            PotatoPlugin.Log.LogWarning(">>> 镜像模式: OFF <<<");
        }

        private RenderTexture CreateMirrorRenderTexture()
        {
            // 考虑土豆模式的 renderScale（如果可用）
            float renderScale = 1.0f;
            
            // 尝试获取 renderScale（某些Unity版本可能不支持）
            try
            {
                var pipeline = GraphicsSettings.currentRenderPipeline;
                if (pipeline != null)
                {
                    PropertyInfo scaleProp = pipeline.GetType().GetProperty("renderScale");
                    if (scaleProp != null && scaleProp.CanRead)
                    {
                        object scaleValue = scaleProp.GetValue(pipeline, null);
                        if (scaleValue != null)
                        {
                            renderScale = (float)scaleValue;
                        }
                    }
                }
            }
            catch
            {
                // 忽略错误，使用默认值 1.0
            }

            int width = Mathf.Max(256, (int)(Screen.width * renderScale));
            int height = Mathf.Max(256, (int)(Screen.height * renderScale));

            RenderTexture rt = new RenderTexture(width, height, 24);
            rt.name = "MirrorRT";
            rt.antiAliasing = QualitySettings.antiAliasing > 0 ? QualitySettings.antiAliasing : 1;
            rt.filterMode = FilterMode.Bilinear;
            rt.wrapMode = TextureWrapMode.Clamp;
            rt.Create();

            PotatoPlugin.Log.LogInfo($"创建 RenderTexture: {width}x{height} (renderScale={renderScale:F2})");
            return rt;
        }

        private void CreateMirrorCanvas()
        {
            // 创建 Canvas GameObject
            mirrorCanvas = new GameObject("MirrorCanvas");
            DontDestroyOnLoad(mirrorCanvas);

            Canvas canvas = mirrorCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -100; // 最底层，不遮挡游戏 UI

            CanvasScaler scaler = mirrorCanvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // 不添加 GraphicRaycaster - 避免拦截鼠标事件

            // 创建 RawImage
            GameObject rawImageObj = new GameObject("MirrorDisplay");
            rawImageObj.transform.SetParent(mirrorCanvas.transform, false);

            mirrorRawImage = rawImageObj.AddComponent<RawImage>();
            mirrorRawImage.texture = mirrorRenderTexture;
            mirrorRawImage.material = mirrorFlipMaterial;
            mirrorRawImage.raycastTarget = false; // 关键：允许点击穿透到3D场景

            // 拉伸到全屏
            RectTransform rt = rawImageObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private Material CreateFlipMaterial()
        {
            // 使用 Unity 内置 UI Shader
            Shader shader = Shader.Find("UI/Default");
            if (shader == null)
            {
                PotatoPlugin.Log.LogWarning("未找到 UI/Default Shader，尝试使用 Unlit/Texture");
                shader = Shader.Find("Unlit/Texture");
            }

            Material mat = new Material(shader);
            
            // 通过材质属性实现 UV 水平翻转
            mat.mainTextureScale = new Vector2(-1, 1);
            mat.mainTextureOffset = new Vector2(1, 0);

            return mat;
        }

        private void EnableAudioSwap()
        {
            try
            {
                // 查找 AudioListener
                AudioListener listener = FindObjectOfType<AudioListener>();
                if (listener == null)
                {
                    PotatoPlugin.Log.LogWarning("未找到 AudioListener，跳过音频镜像");
                    return;
                }

                // 添加声道交换组件
                audioSwapper = listener.gameObject.GetComponent<AudioChannelSwapper>();
                if (audioSwapper == null)
                {
                    audioSwapper = listener.gameObject.AddComponent<AudioChannelSwapper>();
                }

                audioSwapper.enabled = true;
                PotatoPlugin.Log.LogInfo("已启用音频声道交换 (左右互换)");
            }
            catch (Exception e)
            {
                PotatoPlugin.Log.LogError($"启用音频镜像失败: {e.Message}");
            }
        }

        private void DisableAudioSwap()
        {
            if (audioSwapper != null)
            {
                audioSwapper.enabled = false;
                PotatoPlugin.Log.LogInfo("已禁用音频声道交换");
            }
        }

        // ✅ 新增：设置镜像状态（供UI调用）
        public void SetMirrorState(bool enabled)
        {
            isCameraMirrored = enabled;
            ApplyCameraMirror();
        }

        private IEnumerator SetPiPMode(bool enable)
        {
            yield return null; 
            yield return null;
            
            try
            {
                IntPtr hWnd = GetActiveWindow();

                if (enable)
                {
                    // === 开启 PiP ===
                    IntPtr stylePtr = GetWindowLong(hWnd, GWL_STYLE);
                    long style = stylePtr.ToInt64();
                    style &= ~((long)WS_CAPTION | WS_THICKFRAME | WS_SYSMENU);
                    
                    SetWindowLong(hWnd, GWL_STYLE, new IntPtr(style));
                    SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_FRAMECHANGED | SWP_SHOWWINDOW);
                }
                else
                {
                    // === ✨ 恢复原始 ===
                    // 关键修复：强制写回之前保存的 origStyle
                    SetWindowLong(hWnd, GWL_STYLE, origStyle);

                    // 取消置顶 + 强制刷新 Frame
                    SetWindowPos(hWnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_FRAMECHANGED | SWP_SHOWWINDOW);
                }
            }
            catch (Exception e) 
            { 
                PotatoPlugin.Log.LogError($"窗口样式操作失败: {e.Message}"); 
            }
        }

        private void HandleDragLogic()
        {
            DragMode mode = PotatoPlugin.CfgDragMode.Value;

            if (mode == DragMode.Ctrl_LeftClick || mode == DragMode.Alt_LeftClick)
            {
                bool modifierPressed = (mode == DragMode.Ctrl_LeftClick) ? 
                    (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) : 
                    (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt));

                if (Input.GetMouseButtonDown(0) && modifierPressed) 
                {
                    DoApiDrag();
                }
            }
            else if (mode == DragMode.RightClick_Hold)
            {
                if (Input.GetMouseButtonDown(1))
                {
                    isRightDragging = true;
                    
                    // ✨ 关键修复：使用 GetCursorPos 获取屏幕绝对坐标
                    GetCursorPos(out dragStartScreenPos);
                    
                    // 获取窗口当前位置
                    IntPtr hWnd = GetActiveWindow();
                    GetWindowRect(hWnd, out RECT rect);
                    dragStartWindowPos = new Vector2(rect.Left, rect.Top);
                }
                
                if (Input.GetMouseButtonUp(1)) 
                {
                    isRightDragging = false;
                }

                if (isRightDragging)
                {
                    // ✨ 获取当前屏幕绝对坐标
                    GetCursorPos(out POINT currentScreenPos);
                    
                    // 计算绝对位移 (屏幕像素级，1:1)
                    int deltaX = currentScreenPos.X - dragStartScreenPos.X;
                    int deltaY = currentScreenPos.Y - dragStartScreenPos.Y;

                    int newX = (int)(dragStartWindowPos.x + deltaX);
                    int newY = (int)(dragStartWindowPos.y + deltaY);

                    // 移动窗口
                    SetWindowPos(GetActiveWindow(), IntPtr.Zero, newX, newY, 0, 0, SWP_NOSIZE | SWP_NOZORDER);
                }
            }
        }

        private void CalculateTargetResolution()
        {
            Resolution screenRes = Screen.currentResolution;
            int divisor = (int)PotatoPlugin.CfgWindowScale.Value;
            currentTargetWidth = screenRes.width / divisor;
            currentTargetHeight = screenRes.height / divisor;
        }

        private void DoApiDrag()
        {
            try 
            { 
                ReleaseCapture(); 
                SendMessage(GetActiveWindow(), WM_NCLBUTTONDOWN, HT_CAPTION, 0); 
            } 
            catch (Exception e)
            {
                PotatoPlugin.Log.LogError($"拖动窗口失败: {e.Message}");
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) 
        { 
            if (isPotatoMode) ApplyPotatoMode(false); 
        }

        private void ApplyPotatoMode(bool showLog)
        {
            try
            {
                Application.targetFrameRate = 15;
                QualitySettings.vSyncCount = 0;
                var pipeline = GraphicsSettings.currentRenderPipeline;
                if (pipeline != null)
                {
                    Type type = pipeline.GetType();
                    SetProp(type, pipeline, "renderScale", targetRenderScale);
                    SetProp(type, pipeline, "shadowDistance", 0f);
                    SetProp(type, pipeline, "msaaSampleCount", 1);
                }
                var allComponents = FindObjectsOfType<MonoBehaviour>();
                if (allComponents != null)
                {
                    foreach (var comp in allComponents)
                    {
                        if (comp != null && comp.enabled && comp.GetType().Name.Contains("Volume"))
                            comp.enabled = false;
                    }
                }
            }
            catch (Exception e)
            {
                if (showLog) PotatoPlugin.Log.LogError($"应用土豆模式失败: {e.Message}");
            }
        }

        private void RestoreQuality()
        {
            try
            {
                Application.targetFrameRate = 60;
                var pipeline = GraphicsSettings.currentRenderPipeline;
                if (pipeline != null)
                {
                    SetProp(pipeline.GetType(), pipeline, "renderScale", 1.0f);
                    SetProp(pipeline.GetType(), pipeline, "shadowDistance", 50f);
                }
            }
            catch (Exception e)
            {
                PotatoPlugin.Log.LogError($"恢复画质失败: {e.Message}");
            }
        }

        private void SetProp(Type type, object obj, string propName, object value)
        {
            try
            {
                PropertyInfo prop = type.GetProperty(propName);
                if (prop != null && prop.CanWrite) prop.SetValue(obj, value, null);
            }
            catch (Exception e)
            {
                PotatoPlugin.Log.LogWarning($"设置属性失败 [{propName}]: {e.Message}");
            }
        }

        void OnDestroy()
        {
            // 清理镜像资源
            if (isCameraMirrored)
            {
                DisableMirrorMode();
            }

            // 取消场景加载回调
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    // ==========================================
    // 4. Harmony Patch: 拦截鼠标输入并镜像翻转
    // ==========================================
    [HarmonyPatch(typeof(Input), "mousePosition", MethodType.Getter)]
    public class InputMousePositionPatch
    {
        static void Postfix(ref Vector3 __result)
        {
            // 关键修复：仅在点击 3D 场景时翻转，点击 UI 时保持原始坐标
            if (PotatoController.isInputMirrored)
            {
                // 检测鼠标下是否有 UI 元素
                if (IsPointerOverUI(__result))
                {
                    // 鼠标在 UI 上，不翻转（UI 本身没有镜像）
                    return;
                }

                // 鼠标在 3D 场景上，翻转坐标
                __result = new Vector3(
                    Screen.width - __result.x,  // 水平翻转
                    __result.y,                   // Y 保持不变
                    __result.z                    // Z 保持不变
                );
            }
        }

        /// <summary>
        /// 检测鼠标位置是否在 UI 元素上
        /// </summary>
        private static bool IsPointerOverUI(Vector3 mousePosition)
        {
            // 方法1: 使用 EventSystem (推荐)
            if (EventSystem.current != null)
            {
                PointerEventData eventData = new PointerEventData(EventSystem.current)
                {
                    position = new Vector2(mousePosition.x, mousePosition.y)
                };

                List<RaycastResult> results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(eventData, results);

                // 过滤掉镜像 Canvas（MirrorCanvas）
                foreach (var result in results)
                {
                    if (result.gameObject != null && 
                        result.gameObject.name != "MirrorDisplay" && 
                        result.gameObject.GetComponentInParent<Canvas>() != null &&
                        result.gameObject.GetComponentInParent<Canvas>().name != "MirrorCanvas")
                    {
                        return true; // 检测到游戏 UI
                    }
                }
            }

            return false; // 没有 UI，是 3D 场景
        }
    }

    // ==========================================
    // 5. 音频声道交换组件
    // ==========================================
    /// <summary>
    /// 音频滤镜：交换左右声道，用于镜像模式
    /// </summary>
    public class AudioChannelSwapper : MonoBehaviour
    {
        void OnAudioFilterRead(float[] data, int channels)
        {
            // 仅处理立体声 (2 声道)
            if (channels != 2)
                return;

            // 交换左右声道
            // data 格式: [L0, R0, L1, R1, L2, R2, ...]
            for (int i = 0; i < data.Length; i += 2)
            {
                float temp = data[i];       // 保存左声道
                data[i] = data[i + 1];      // 左 = 右
                data[i + 1] = temp;         // 右 = 左
            }
        }
    }
}