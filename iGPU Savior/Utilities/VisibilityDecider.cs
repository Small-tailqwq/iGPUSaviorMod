using System.Collections.Generic;

namespace PotatoOptimization.Utilities
{
    public struct VisRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public VisRect(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }
    }

    public struct MonitorBounds
    {
        public VisRect Bounds;
    }

    public struct FullscreenWindowInfo
    {
        public VisRect Rect;
        public bool IsOnGameMonitor;
    }

    public struct VisibilitySignals
    {
        public bool IsIconic;
        public bool IsCloaked;
        public VisRect WindowRect;
        public VisRect VirtualScreen;
        public MonitorBounds? GameMonitor;
        public IReadOnlyList<FullscreenWindowInfo> FullscreenWindows;
    }

    /// <summary>
    /// 纯逻辑判定：游戏窗口是否对用户可见。无 P/Invoke，可单测。
    /// 不可见（任一命中）：最小化 / 虚拟桌面隐藏 / 完全拖出虚拟屏幕 /
    /// 游戏所在显示器有其他全屏窗口覆盖。
    /// </summary>
    public static class VisibilityDecider
    {
        public static bool IsVisible(VisibilitySignals s)
        {
            if (s.IsIconic) return false;
            if (s.IsCloaked) return false;
            if (!RectsIntersect(s.WindowRect, s.VirtualScreen)) return false;
            if (!s.GameMonitor.HasValue) return true;

            var gameBounds = s.GameMonitor.Value.Bounds;
            var list = s.FullscreenWindows;
            if (list == null) return true;

            foreach (var fw in list)
            {
                if (!fw.IsOnGameMonitor) continue;
                if (Covers(fw.Rect, gameBounds)) return false;
            }
            return true;
        }

        // 相切（边缘重合）视为有交集 → 可见，故用严格不等号判定"完全在外"
        private static bool RectsIntersect(VisRect a, VisRect b)
        {
            return !(a.Right < b.Left || a.Left > b.Right ||
                     a.Bottom < b.Top || a.Top > b.Bottom);
        }

        private static bool Covers(VisRect cover, VisRect target)
        {
            return cover.Left <= target.Left && cover.Right >= target.Right &&
                   cover.Top <= target.Top && cover.Bottom >= target.Bottom;
        }
    }
}
