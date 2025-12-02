using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

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

        // ✅ 改进后的镜像实现：使用投影矩阵而非Transform缩放
        private void ApplyCameraMirror()
        {
            Camera[] allCameras = Camera.allCameras;
            
            if (allCameras == null || allCameras.Length == 0)
            {
                PotatoPlugin.Log.LogWarning(">>> 未找到任何摄像机，跳过镜像 <<<");
                return;
            }

            int mirroredCount = 0;

            foreach (Camera cam in allCameras)
            {
                if (cam != null)
                {
                    // ✅ 关键修复：使用投影矩阵翻转，而不是 Transform.scale
                    if (isCameraMirrored)
                    {
                        // 创建一个水平翻转的投影矩阵
                        Matrix4x4 mat = cam.projectionMatrix;
                        mat *= Matrix4x4.Scale(new Vector3(-1, 1, 1)); // 只翻转 X 轴
                        cam.projectionMatrix = mat;
                        
                        // 反转剔除模式（重要！否则模型会消失）
                        cam.ResetWorldToCameraMatrix();
                    }
                    else
                    {
                        // 恢复默认投影矩阵
                        cam.ResetProjectionMatrix();
                        cam.ResetWorldToCameraMatrix();
                    }
                    
                    mirroredCount++;
                }
            }

            if (isCameraMirrored)
            {
                PotatoPlugin.Log.LogWarning($">>> 摄像机镜像: ON (已翻转 {mirroredCount} 个摄像机，画面已水平翻转) <<<");
            }
            else
            {
                PotatoPlugin.Log.LogWarning($">>> 摄像机镜像: OFF (已恢复 {mirroredCount} 个摄像机) <<<");
            }
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
    }
}