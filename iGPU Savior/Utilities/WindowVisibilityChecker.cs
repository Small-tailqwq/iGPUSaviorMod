using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using PotatoOptimization.Core;

namespace PotatoOptimization.Utilities
{
    /// <summary>
    /// 通过 Win32 判定游戏窗口是否在屏幕上对用户可见。
    /// 采集 <see cref="VisibilitySignals"/> 后委托 <see cref="VisibilityDecider"/> 判定。
    /// </summary>
    public sealed class WindowVisibilityChecker
    {
        private const int DWMWA_CLOAKED = 14;
        private const int SM_XVIRTUALSCREEN = 76;
        private const int SM_YVIRTUALSCREEN = 77;
        private const int SM_CXVIRTUALSCREEN = 78;
        private const int SM_CYVIRTUALSCREEN = 79;
        private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out WindowManager.RECT lpRect);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("dwmapi.dll")]
        private static extern int DwmGetWindowAttribute(IntPtr hwnd, int attribute, ref int pvAttribute, int cbAttribute);

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;
            public WindowManager.RECT rcMonitor;
            public WindowManager.RECT rcWork;
            public uint dwFlags;
        }

        private readonly Func<IntPtr> _getGameWindowHandle;

        public WindowVisibilityChecker(Func<IntPtr> getGameWindowHandle)
        {
            _getGameWindowHandle = getGameWindowHandle;
        }

        public bool IsGameWindowVisible()
        {
            IntPtr hwnd = _getGameWindowHandle();
            if (hwnd == IntPtr.Zero || !IsWindow(hwnd))
            {
                PotatoPlugin.Log?.LogWarning("WindowVisibilityChecker: 游戏窗口句柄无效，按可见处理");
                return true;
            }

            IntPtr gameMonHandle = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            MonitorBounds? gameMonitor = GetMonitorBounds(gameMonHandle);

            // 前台窗口必在最前、不可能被其他窗口遮挡 → 仅跳过全屏遮挡判定(判定4)，
            // 最小化/虚拟桌面隐藏/拖出屏幕(判定1/2/3)仍照常执行。
            bool isForeground = GetForegroundWindow() == hwnd;

            var signals = new VisibilitySignals
            {
                IsIconic = SafeIsIconic(hwnd),
                IsCloaked = GetCloaked(hwnd),
                WindowRect = ToVisRect(SafeGetWindowRect(hwnd)),
                VirtualScreen = GetVirtualScreen(),
                GameMonitor = gameMonitor,
                FullscreenWindows = isForeground ? null : EnumerateFullscreenWindows(hwnd, gameMonHandle, gameMonitor)
            };

            return VisibilityDecider.IsVisible(signals);
        }

        private static bool SafeIsIconic(IntPtr hwnd)
        {
            try { return IsIconic(hwnd); }
            catch { return false; }
        }

        private static bool GetCloaked(IntPtr hwnd)
        {
            try
            {
                int cloaked = 0;
                int hr = DwmGetWindowAttribute(hwnd, DWMWA_CLOAKED, ref cloaked, sizeof(int));
                return hr >= 0 && cloaked != 0;
            }
            catch { return false; }
        }

        private static WindowManager.RECT SafeGetWindowRect(IntPtr hwnd)
        {
            if (GetWindowRect(hwnd, out WindowManager.RECT r)) return r;
            return new WindowManager.RECT();
        }

        private static VisRect GetVirtualScreen()
        {
            int x = GetSystemMetrics(SM_XVIRTUALSCREEN);
            int y = GetSystemMetrics(SM_YVIRTUALSCREEN);
            int w = GetSystemMetrics(SM_CXVIRTUALSCREEN);
            int h = GetSystemMetrics(SM_CYVIRTUALSCREEN);
            return new VisRect(x, y, x + w, y + h);
        }

        private static MonitorBounds? GetMonitorBounds(IntPtr hmon)
        {
            if (hmon == IntPtr.Zero) return null;
            var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            if (!GetMonitorInfo(hmon, ref info)) return null;
            return new MonitorBounds { Bounds = ToVisRect(info.rcMonitor) };
        }

        private static List<FullscreenWindowInfo> EnumerateFullscreenWindows(
            IntPtr gameHwnd, IntPtr gameMonHandle, MonitorBounds? gameMonitor)
        {
            var list = new List<FullscreenWindowInfo>();
            if (!gameMonitor.HasValue || gameMonHandle == IntPtr.Zero) return list;

            VisRect gameBounds = gameMonitor.Value.Bounds;
            EnumWindowsProc callback = (IntPtr h, IntPtr lp) =>
            {
                try
                {
                    if (h == gameHwnd) return true;
                    if (!IsWindowVisible(h)) return true;
                    if (!GetWindowRect(h, out WindowManager.RECT r)) return true;

                    VisRect vr = ToVisRect(r);
                    if (!Covers(vr, gameBounds)) return true;

                    IntPtr hmon = MonitorFromWindow(h, MONITOR_DEFAULTTONEAREST);
                    list.Add(new FullscreenWindowInfo
                    {
                        Rect = vr,
                        IsOnGameMonitor = hmon == gameMonHandle
                    });
                }
                catch { }
                return true;
            };

            EnumWindows(callback, IntPtr.Zero);
            return list;
        }

        private static VisRect ToVisRect(WindowManager.RECT r) =>
            new VisRect(r.Left, r.Top, r.Right, r.Bottom);

        private static bool Covers(VisRect cover, VisRect target) =>
            cover.Left <= target.Left && cover.Right >= target.Right &&
            cover.Top <= target.Top && cover.Bottom >= target.Bottom;
    }
}
