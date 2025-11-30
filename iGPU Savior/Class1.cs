using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
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
    // 1. 定义枚举，方便用户选择
    // ==========================================
    public enum WindowScaleRatio
    {
        OneThird = 3,   // 1/3
        OneFourth = 4,  // 1/4
        OneFifth = 5    // 1/5
    }

    public enum DragMode
    {
        Ctrl_LeftClick, // Ctrl + 左键
        Alt_LeftClick,  // Alt + 左键
        RightClick_Hold // 右键按住 (直接拖)
    }

    // ==========================================
    // 2. 插件入口：负责配置定义和 Runner 创建
    // ==========================================
    [BepInPlugin("com.yourname.potatomode", "Potato Mode Optimization", "1.4.0")]
    public class PotatoPlugin : BaseUnityPlugin
    {
        public static PotatoPlugin Instance;
        public static ManualLogSource Log;
        
        // --- ✨ 配置项定义 ✨ ---
        public static ConfigEntry<KeyCode> KeyPotatoMode;
        public static ConfigEntry<KeyCode> KeyPiPMode;
        public static ConfigEntry<WindowScaleRatio> CfgWindowScale;
        public static ConfigEntry<DragMode> CfgDragMode;

        private GameObject runnerObject;

        void Awake()
        {
            Instance = this;
            Log = Logger;

            // 1. 初始化配置 (会自动生成/读取 .cfg 文件)
            InitConfig();

            Log.LogWarning(">>> [V1.4] 插件启动：配置已加载 (自适应分辨率 + 自定义按键) <<<");

            // 2. 创建不死对象
            runnerObject = new GameObject("PotatoRunner");
            DontDestroyOnLoad(runnerObject);
            runnerObject.hideFlags = HideFlags.HideAndDontSave;
            runnerObject.AddComponent<PotatoController>();

            Log.LogWarning(">>> Runner 就绪 <<<");
        }

        private void InitConfig()
        {
            // 分类：按键设置
            KeyPotatoMode = Config.Bind("Hotkeys", "PotatoModeKey", KeyCode.F2, "切换土豆模式的按键");
            KeyPiPMode = Config.Bind("Hotkeys", "PiPModeKey", KeyCode.F3, "切换画中画小窗的按键");

            // 分类：小窗设置
            CfgWindowScale = Config.Bind("Window", "ScaleRatio", WindowScaleRatio.OneThird, 
                "小窗大小相对于屏幕分辨率的比例 (OneThird=1/3, OneFourth=1/4, OneFifth=1/5)");
            
            CfgDragMode = Config.Bind("Window", "DragMethod", DragMode.Ctrl_LeftClick, 
                "拖动窗口的方式 (Ctrl_LeftClick, Alt_LeftClick, RightClick_Hold)");
        }
    }

    // ==========================================
    // 3. 逻辑控制器
    // ==========================================
    public class PotatoController : MonoBehaviour
    {
        private bool isPotatoMode = true;
        private bool isSmallWindow = false;

        private float targetRenderScale = 0.4f;
        // 这些现在是动态计算的，不再写死
        private int currentTargetWidth;
        private int currentTargetHeight;

        private float lastRunTime = 0f;
        private float runInterval = 3.0f;

        // Windows API
        [DllImport("user32.dll")] private static extern IntPtr GetActiveWindow();
        [DllImport("user32.dll", EntryPoint = "SetWindowLong")] private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, uint dwNewLong);
        [DllImport("user32.dll")] private static extern uint GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        // API 常量
        private const int GWL_STYLE = -16;
        private const uint WS_CAPTION = 0x00C00000;
        private const uint WS_THICKFRAME = 0x00040000;
        private const uint WS_SYSMENU = 0x00080000;
        private const uint SWP_FRAMECHANGED = 0x0020;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        // 手动拖动用的变量 (右键模式用)
        private Vector2 dragStartMousePos;
        private Vector2 dragStartWindowPos;
        private bool isRightDragging = false;

        // 获取窗口位置的 API (为了右键手动拖动)
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

        void Start()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            if (isPotatoMode) ApplyPotatoMode(false);
            PotatoPlugin.Log.LogInfo($"控制器启动。拖动模式: {PotatoPlugin.CfgDragMode.Value}");
        }

        void Update()
        {
            // 1. 读取配置的按键 (不再写死 F2)
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

            // 2. 读取配置的按键 (不再写死 F3)
            if (Input.GetKeyDown(PotatoPlugin.KeyPiPMode.Value))
            {
                ToggleWindowMode();
            }

            // 3. 处理拖动逻辑
            if (isSmallWindow)
            {
                HandleDragLogic();
            }

            // 4. 自动巡逻
            if (isPotatoMode)
            {
                if (Time.realtimeSinceStartup - lastRunTime > runInterval)
                {
                    ApplyPotatoMode(false);
                    lastRunTime = Time.realtimeSinceStartup;
                }
            }
        }

        private void HandleDragLogic()
        {
            DragMode mode = PotatoPlugin.CfgDragMode.Value;

            // --- 模式 A & B: 左键 + 修饰键 (使用 API 欺骗法，最丝滑) ---
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
            // --- 模式 C: 右键按住 (手动计算法，因为右键不触发系统标题栏拖动) ---
            else if (mode == DragMode.RightClick_Hold)
            {
                if (Input.GetMouseButtonDown(1))
                {
                    isRightDragging = true;
                    dragStartMousePos = Input.mousePosition;
                    // 获取当前窗口位置
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
                    // 手动计算位移
                    Vector2 currentMouse = Input.mousePosition;
                    Vector2 delta = currentMouse - dragStartMousePos;
                    int newX = (int)(dragStartWindowPos.x + delta.x);
                    int newY = (int)(dragStartWindowPos.y - delta.y);

                    IntPtr hWnd = GetActiveWindow();
                    // 手动移动窗口
                    SetWindowPos(hWnd, IntPtr.Zero, newX, newY, 0, 0, SWP_NOSIZE | SWP_NOZORDER);
                }
            }
        }

        private void DoApiDrag()
        {
            try 
            { 
                ReleaseCapture(); 
                SendMessage(GetActiveWindow(), WM_NCLBUTTONDOWN, HT_CAPTION, 0); 
            } 
            catch { }
        }

        private void ToggleWindowMode()
        {
            isSmallWindow = !isSmallWindow;
            if (isSmallWindow)
            {
                // ✨ 自适应分辨率计算 ✨
                CalculateTargetResolution();

                Screen.SetResolution(currentTargetWidth, currentTargetHeight, FullScreenMode.Windowed);
                StartCoroutine(SetPiPMode(true));
                PotatoPlugin.Log.LogWarning($">>> 开启画中画: {currentTargetWidth}x{currentTargetHeight} ({PotatoPlugin.CfgWindowScale.Value}) <<<");
            }
            else
            {
                Resolution maxRes = Screen.currentResolution;
                Screen.SetResolution(maxRes.width, maxRes.height, FullScreenMode.FullScreenWindow);
                StartCoroutine(SetPiPMode(false));
                PotatoPlugin.Log.LogWarning(">>> 恢复全屏模式 <<<");
            }
        }

        private void CalculateTargetResolution()
        {
            // 获取当前屏幕最大的分辨率 (通常是物理分辨率)
            Resolution screenRes = Screen.currentResolution;
            int divisor = (int)PotatoPlugin.CfgWindowScale.Value; // 枚举值直接就是 3, 4, 5
            
            currentTargetWidth = screenRes.width / divisor;
            currentTargetHeight = screenRes.height / divisor;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) 
        { 
            if (isPotatoMode) ApplyPotatoMode(false); 
        }

        private IEnumerator SetPiPMode(bool enable)
        {
            yield return null; 
            yield return null;
            
            try
            {
                IntPtr hWnd = GetActiveWindow();
                uint style = GetWindowLong(hWnd, GWL_STYLE);
                
                if (enable)
                {
                    style &= ~(WS_CAPTION | WS_THICKFRAME | WS_SYSMENU);
                    SetWindowLong32(hWnd, GWL_STYLE, style);
                    SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_FRAMECHANGED | SWP_SHOWWINDOW);
                }
                else
                {
                    SetWindowPos(hWnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_FRAMECHANGED | SWP_SHOWWINDOW);
                }
            }
            catch (Exception e) 
            { 
                PotatoPlugin.Log.LogError($"窗口样式失败: {e.Message}"); 
            }
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
            catch (Exception) { }
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
            catch { }
        }

        private void SetProp(Type type, object obj, string propName, object value)
        {
            try
            {
                PropertyInfo prop = type.GetProperty(propName);
                if (prop != null && prop.CanWrite) prop.SetValue(obj, value, null);
            }
            catch { }
        }
    }
}