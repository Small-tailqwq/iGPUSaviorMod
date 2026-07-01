using System.Collections.Generic;
using PotatoOptimization.Utilities;
using Xunit;

namespace IGPUSavior.Tests
{
    public class VisibilityDeciderTests
    {
        private static readonly VisRect VScreen = new VisRect(0, 0, 1920, 1080);
        private static readonly VisRect InScreen = new VisRect(100, 100, 800, 600);
        private static readonly MonitorBounds GameMon = new MonitorBounds { Bounds = VScreen };

        [Fact]
        public void IsVisible_Minimized_ReturnsFalse()
        {
            var s = NewSignals();
            s.IsIconic = true;
            Assert.False(VisibilityDecider.IsVisible(s));
        }

        [Fact]
        public void IsVisible_Cloaked_ReturnsFalse()
        {
            var s = NewSignals();
            s.IsCloaked = true;
            Assert.False(VisibilityDecider.IsVisible(s));
        }

        [Fact]
        public void IsVisible_DraggedOffscreen_ReturnsFalse()
        {
            var s = NewSignals();
            s.WindowRect = new VisRect(-5000, -5000, -4000, -4000);
            Assert.False(VisibilityDecider.IsVisible(s));
        }

        [Fact]
        public void IsVisible_TouchingVirtualScreenEdge_ReturnsTrue()
        {
            // 窗口右边恰好贴到虚拟屏幕左边（相切），按"有交集"判为可见
            var s = NewSignals();
            s.WindowRect = new VisRect(-100, 0, 0, 100);
            Assert.True(VisibilityDecider.IsVisible(s));
        }

        [Fact]
        public void IsVisible_GameMonitorNull_NoFullscreen_ReturnsTrue()
        {
            var s = NewSignals();
            s.GameMonitor = null;
            s.FullscreenWindows = new List<FullscreenWindowInfo>();
            Assert.True(VisibilityDecider.IsVisible(s));
        }

        [Fact]
        public void IsVisible_FullscreenOnGameMonitorCovering_ReturnsFalse()
        {
            var s = NewSignals();
            s.FullscreenWindows = new List<FullscreenWindowInfo>
            {
                new FullscreenWindowInfo { Rect = VScreen, IsOnGameMonitor = true }
            };
            Assert.False(VisibilityDecider.IsVisible(s));
        }

        [Fact]
        public void IsVisible_FullscreenOnOtherMonitor_ReturnsTrue()
        {
            var s = NewSignals();
            s.FullscreenWindows = new List<FullscreenWindowInfo>
            {
                new FullscreenWindowInfo { Rect = VScreen, IsOnGameMonitor = false }
            };
            Assert.True(VisibilityDecider.IsVisible(s));
        }

        [Fact]
        public void IsVisible_FullscreenOnGameMonitorNotCovering_ReturnsTrue()
        {
            var s = NewSignals();
            s.FullscreenWindows = new List<FullscreenWindowInfo>
            {
                new FullscreenWindowInfo { Rect = new VisRect(0, 0, 100, 100), IsOnGameMonitor = true }
            };
            Assert.True(VisibilityDecider.IsVisible(s));
        }

        [Fact]
        public void IsVisible_FullscreenListNull_ReturnsTrue()
        {
            var s = NewSignals();
            s.FullscreenWindows = null;
            Assert.True(VisibilityDecider.IsVisible(s));
        }

        private static VisibilitySignals NewSignals() =>
            new VisibilitySignals
            {
                IsIconic = false,
                IsCloaked = false,
                WindowRect = InScreen,
                VirtualScreen = VScreen,
                GameMonitor = GameMon,
                FullscreenWindows = new List<FullscreenWindowInfo>()
            };
    }
}
