namespace PotatoOptimization.Core
{
    /// <summary>
    /// 全局常量定义
    /// </summary>
    public static class Constants
    {
        // ==================== 插件信息 ====================
        public const string PluginGUID = "chillwithyou.potatomode";
        public const string PluginName = "Potato Mode Optimization";
        public const string PluginVersion = "1.7.1";

        // ==================== Win32 API 常量 ====================
        public const int GWL_STYLE = -16;
        public const uint WS_CAPTION = 0x00C00000;
        public const uint WS_THICKFRAME = 0x00040000;
        public const uint WS_SYSMENU = 0x00080000;
        
        public const uint SWP_FRAMECHANGED = 0x0020;
        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_SHOWWINDOW = 0x0040;
        public const uint SWP_NOZORDER = 0x0004;

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        // ==================== 性能设置 ====================
        public const int PotatoModeTargetFPS = 15;
        public const int NormalModeTargetFPS = 60;
        public const float PotatoModeRenderScale = 0.4f;
        public const float NormalRenderScale = 1.0f;
        public const float PotatoModeShadowDistance = 0f;
        public const float NormalShadowDistance = 50f;

        // ==================== 竖屏优化常量 ====================
        public const float PortraitPositionXMultiplier = 1.053f;
        public const float PortraitPositionYMultiplier = 0.939f;
        public const float PortraitPositionZMultiplier = 0.962f;
        public const float PortraitRotationXMultiplier = 0.83f;
        public const float PortraitRotationYMultiplier = 0.96f;
        public const float PortraitFOVMultiplier = 2.0f;
        public const float AbnormalCameraPositionThreshold = 1000f;

        // ==================== UI 常量 ====================
        public const float MaxVisibleDropdownItems = 6f;
        public const float DefaultDropdownItemHeight = 40f;
        public const float DefaultDropdownHeaderHeight = 10f;
        
        public const int RenderTextureMinSize = 256;
        public const float DefaultRunInterval = 3.0f;

        // ==================== 路径常量 ====================
        public const string GraphicsContentPath = "Graphics/ScrollView/Viewport/Content";
        public const string GraphicQualityPulldownPath = "Graphics/ScrollView/Viewport/Content/GraphicQualityPulldownList";
        public const string AudioTabContentPath = "MusicAudio/ScrollView/Viewport/Content";
    }
}
