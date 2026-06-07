using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using PotatoOptimization.Core;

namespace PotatoOptimization.Utilities
{
    /// <summary>
    /// Windows 窗口管理工具类 - 封装所有 Win32 API 调用
    /// </summary>
    public static class WindowManager
    {
        // ==================== Win32 API 声明 ====================
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AdjustWindowRectEx(ref RECT lpRect, uint dwStyle, bool bMenu, uint dwExStyle);

        [DllImport("user32.dll")]
        private static extern IntPtr CallWindowProc(
            IntPtr lpPrevWndFunc,
            IntPtr hWnd,
            uint msg,
            IntPtr wParam,
            IntPtr lParam);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private delegate IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        // ==================== 结构体定义 ====================
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        // ==================== 常量 ====================
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        // 扩展样式
        private const long WS_EX_DLGMODALFRAME = 0x00000001L;
        private const long WS_EX_WINDOWEDGE = 0x00000100L;
        private const long WS_EX_CLIENTEDGE = 0x00000200L;
        private const long WS_EX_STATICEDGE = 0x00020000L;

        // DWM 窗口外观属性 (Windows 11+)
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWA_BORDER_COLOR = 34;
        private const int DWMWCP_ROUND = 2;
        private const int DWMWA_COLOR_DEFAULT = unchecked((int)0xFFFFFFFF);
        private const int DWMWA_COLOR_NONE = unchecked((int)0xFFFFFFFE);

        // 无边框可缩放窗口
        private const int GWL_WNDPROC = -4;
        private const uint WM_NCCALCSIZE = 0x0083;
        private const uint WM_NCHITTEST = 0x0084;
        private const int HTCLIENT = 1;
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;
        private const int SM_CXSIZEFRAME = 32;
        private const int SM_CYSIZEFRAME = 33;
        private const int SM_CXPADDEDBORDER = 92;

        private static IntPtr _cachedWindowHandle;
        private static IntPtr _originalWindowProc;
        private static WindowProc _pipWindowProc;
        private static bool _pipWindowProcInstalled;

        // ==================== 公共方法 ====================

        public static IntPtr GetCurrentWindowHandle()
        {
            if (_cachedWindowHandle != IntPtr.Zero && IsWindow(_cachedWindowHandle))
                return _cachedWindowHandle;

            // 按 Unity 窗口类名精准查找，避免同进程的控制台窗口干扰
            _cachedWindowHandle = FindWindow("UnityWndClass", null);

            // 降级：使用 Process.MainWindowHandle
            if (_cachedWindowHandle == IntPtr.Zero || !IsWindow(_cachedWindowHandle))
            {
                using (Process process = Process.GetCurrentProcess())
                {
                    process.Refresh();
                    _cachedWindowHandle = process.MainWindowHandle;
                }
            }

            // 最终降级：取当前活动窗口
            if (_cachedWindowHandle == IntPtr.Zero || !IsWindow(_cachedWindowHandle))
                _cachedWindowHandle = GetActiveWindow();

            return _cachedWindowHandle;
        }

        public static IntPtr SetWindowStyle(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8)
                return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            else
                return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
        }

        public static IntPtr GetWindowStyle(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 8)
                return GetWindowLongPtr64(hWnd, nIndex);
            else
                return new IntPtr(GetWindowLong32(hWnd, nIndex));
        }

        /// <summary>
        /// 移除窗口边框和标题栏（原始模式，不可缩放）
        /// </summary>
        public static void RemoveWindowBorder(IntPtr hWnd)
        {
            long style = GetWindowStyle(hWnd, Constants.GWL_STYLE).ToInt64();
            style &= ~((long)Constants.WS_CAPTION | Constants.WS_THICKFRAME | Constants.WS_SYSMENU);
            SetWindowStyle(hWnd, Constants.GWL_STYLE, new IntPtr(style));

            // 清除扩展边框样式
            long exStyle = GetWindowStyle(hWnd, Constants.GWL_EXSTYLE).ToInt64();
            exStyle &= ~(WS_EX_DLGMODALFRAME | WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE);
            SetWindowStyle(hWnd, Constants.GWL_EXSTYLE, new IntPtr(exStyle));

            SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0,
                Constants.SWP_NOMOVE | Constants.SWP_NOSIZE | Constants.SWP_FRAMECHANGED | Constants.SWP_SHOWWINDOW);
        }

        /// <summary>
        /// 浏览器风格小窗：无标题栏 + 可拖拽缩放 + 圆角
        /// </summary>
        public static void SetPiPWindowStyle(IntPtr hWnd)
        {
            InstallPiPWindowProc(hWnd);

            // 基本样式：移除标题栏和系统菜单，保留 WS_THICKFRAME（缩放边框）
            long style = GetWindowStyle(hWnd, Constants.GWL_STYLE).ToInt64();
            style &= ~((long)Constants.WS_CAPTION | Constants.WS_SYSMENU);
            style |= (long)Constants.WS_THICKFRAME;
            SetWindowStyle(hWnd, Constants.GWL_STYLE, new IntPtr(style));

            // 扩展样式：清除所有边框相关的视觉效果
            long exStyle = GetWindowStyle(hWnd, Constants.GWL_EXSTYLE).ToInt64();
            exStyle &= ~(WS_EX_DLGMODALFRAME | WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE | WS_EX_STATICEDGE);
            SetWindowStyle(hWnd, Constants.GWL_EXSTYLE, new IntPtr(exStyle));

            // 圆角 (Windows 11+，Windows 10 静默失败)
            int cornerPreference = DWMWCP_ROUND;
            DwmSetWindowAttribute(hWnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPreference, sizeof(int));

            // 隐藏 WS_THICKFRAME 的可见边框，但保留原生边缘缩放命中区。
            int borderColor = DWMWA_COLOR_NONE;
            DwmSetWindowAttribute(hWnd, DWMWA_BORDER_COLOR, ref borderColor, sizeof(int));

            // 置顶 + 刷新框架
            SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0,
                Constants.SWP_NOMOVE | Constants.SWP_NOSIZE | Constants.SWP_FRAMECHANGED | Constants.SWP_SHOWWINDOW);
        }

        /// <summary>
        /// 恢复窗口样式
        /// </summary>
        public static void RestoreWindowStyle(IntPtr hWnd, IntPtr originalStyle, IntPtr originalExStyle)
        {
            RestoreWindowProc(hWnd);

            int borderColor = DWMWA_COLOR_DEFAULT;
            DwmSetWindowAttribute(hWnd, DWMWA_BORDER_COLOR, ref borderColor, sizeof(int));

            SetWindowStyle(hWnd, Constants.GWL_STYLE, originalStyle);
            SetWindowStyle(hWnd, Constants.GWL_EXSTYLE, originalExStyle);
            SetWindowPos(hWnd, HWND_NOTOPMOST, 0, 0, 0, 0,
                Constants.SWP_NOMOVE | Constants.SWP_NOSIZE | Constants.SWP_FRAMECHANGED | Constants.SWP_SHOWWINDOW);
        }

        /// <summary>
        /// 移动窗口
        /// </summary>
        public static bool MoveWindow(IntPtr hWnd, int x, int y)
        {
            return SetWindowPos(hWnd, IntPtr.Zero, x, y, 0, 0, Constants.SWP_NOSIZE | Constants.SWP_NOZORDER);
        }

        /// <summary>
        /// 调整窗口大小
        /// </summary>
        public static bool ResizeWindow(IntPtr hWnd, int width, int height)
        {
            return SetWindowPos(hWnd, IntPtr.Zero, 0, 0, width, height, Constants.SWP_NOMOVE | Constants.SWP_NOZORDER);
        }

        /// <summary>
        /// 按客户区大小调整窗口，避免可缩放边框占用目标分辨率。
        /// </summary>
        public static bool ResizeClientArea(IntPtr hWnd, int width, int height)
        {
            RECT rect = new RECT { Right = width, Bottom = height };
            uint style = unchecked((uint)GetWindowStyle(hWnd, Constants.GWL_STYLE).ToInt64());
            uint exStyle = unchecked((uint)GetWindowStyle(hWnd, Constants.GWL_EXSTYLE).ToInt64());

            if (!AdjustWindowRectEx(ref rect, style, false, exStyle))
                return false;

            return ResizeWindow(hWnd, rect.Right - rect.Left, rect.Bottom - rect.Top);
        }

        /// <summary>
        /// 开始拖动窗口 (系统级)
        /// </summary>
        public static void StartSystemDrag()
        {
            try
            {
                ReleaseCapture();
                SendMessage(GetActiveWindow(), Constants.WM_NCLBUTTONDOWN, Constants.HT_CAPTION, 0);
            }
            catch (Exception e)
            {
                PotatoPlugin.Log.LogError($"拖动窗口失败: {e.Message}");
            }
        }

        public static bool GetCursorScreenPosition(out POINT point)
        {
            return GetCursorPos(out point);
        }

        public static bool GetWindowBounds(IntPtr hWnd, out RECT rect)
        {
            return GetWindowRect(hWnd, out rect);
        }

        public static bool GetClientBounds(IntPtr hWnd, out RECT rect)
        {
            return GetClientRect(hWnd, out rect);
        }

        private static void InstallPiPWindowProc(IntPtr hWnd)
        {
            if (_pipWindowProcInstalled) return;

            _pipWindowProc = PiPWindowProc;
            IntPtr newWindowProc = Marshal.GetFunctionPointerForDelegate(_pipWindowProc);
            _originalWindowProc = SetWindowStyle(hWnd, GWL_WNDPROC, newWindowProc);
            _pipWindowProcInstalled = _originalWindowProc != IntPtr.Zero;
        }

        private static void RestoreWindowProc(IntPtr hWnd)
        {
            if (!_pipWindowProcInstalled) return;

            SetWindowStyle(hWnd, GWL_WNDPROC, _originalWindowProc);
            _pipWindowProcInstalled = false;
            _originalWindowProc = IntPtr.Zero;
            _pipWindowProc = null;
        }

        private static IntPtr PiPWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_NCCALCSIZE)
            {
                // 让 Unity 客户区覆盖整个窗口，彻底移除 WS_THICKFRAME 的顶部白条。
                return IntPtr.Zero;
            }

            if (msg == WM_NCHITTEST)
            {
                int hitTest = GetResizeHitTest(hWnd, lParam);
                if (hitTest != HTCLIENT)
                    return new IntPtr(hitTest);
            }

            return CallWindowProc(_originalWindowProc, hWnd, msg, wParam, lParam);
        }

        private static int GetResizeHitTest(IntPtr hWnd, IntPtr lParam)
        {
            if (!GetWindowRect(hWnd, out RECT rect))
                return HTCLIENT;

            int packedPosition = lParam.ToInt32();
            int cursorX = (short)(packedPosition & 0xFFFF);
            int cursorY = (short)((packedPosition >> 16) & 0xFFFF);

            int horizontalBorder = Math.Max(6, GetSystemMetrics(SM_CXSIZEFRAME) + GetSystemMetrics(SM_CXPADDEDBORDER));
            int verticalBorder = Math.Max(6, GetSystemMetrics(SM_CYSIZEFRAME) + GetSystemMetrics(SM_CXPADDEDBORDER));

            bool left = cursorX >= rect.Left && cursorX < rect.Left + horizontalBorder;
            bool right = cursorX <= rect.Right && cursorX > rect.Right - horizontalBorder;
            bool top = cursorY >= rect.Top && cursorY < rect.Top + verticalBorder;
            bool bottom = cursorY <= rect.Bottom && cursorY > rect.Bottom - verticalBorder;

            if (top && left) return HTTOPLEFT;
            if (top && right) return HTTOPRIGHT;
            if (bottom && left) return HTBOTTOMLEFT;
            if (bottom && right) return HTBOTTOMRIGHT;
            if (left) return HTLEFT;
            if (right) return HTRIGHT;
            if (top) return HTTOP;
            if (bottom) return HTBOTTOM;
            return HTCLIENT;
        }
    }
}
