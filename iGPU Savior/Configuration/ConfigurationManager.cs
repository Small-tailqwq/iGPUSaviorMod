using BepInEx.Configuration;
using UnityEngine;
using PotatoOptimization.Core;

namespace PotatoOptimization.Configuration
{
    /// <summary>
    /// 统一管理所有 BepInEx 配置项,提供类型安全的访问接口
    /// </summary>
    public class ConfigurationManager
    {
        private readonly ConfigFile _config;

        // ==================== 快捷键配置 ====================
        public ConfigEntry<KeyCode> KeyPotatoMode { get; private set; }
        public ConfigEntry<KeyCode> KeyPiPMode { get; private set; }
        public ConfigEntry<KeyCode> KeyCameraMirror { get; private set; }
        public ConfigEntry<KeyCode> KeyPortraitMode { get; private set; }

        // ==================== 相机配置 ====================
        public ConfigEntry<bool> CfgEnableMirror { get; private set; }
        public ConfigEntry<bool> CfgEnablePortraitMode { get; private set; }

        // ==================== 窗口配置 ====================
        public ConfigEntry<WindowScaleRatio> CfgWindowScale { get; private set; }
        public ConfigEntry<DragMode> CfgDragMode { get; private set; }

        public ConfigurationManager(ConfigFile config)
        {
            _config = config;
            InitializeConfigurations();
        }

        private void InitializeConfigurations()
        {
            // 快捷键配置
            KeyPotatoMode = _config.Bind("Hotkeys", "PotatoModeKey", KeyCode.F2, 
                "切换土豆模式的按键");
            
            KeyPiPMode = _config.Bind("Hotkeys", "PiPModeKey", KeyCode.F3, 
                "切换画中画小窗的按键");
            
            KeyCameraMirror = _config.Bind("Hotkeys", "CameraMirrorKey", KeyCode.F4, 
                "切换摄像机镜像的按键(左右翻转画面)");
            
            KeyPortraitMode = _config.Bind("Hotkeys", "PortraitModeKey", KeyCode.F5, 
                "切换竖屏优化的按键(方便调试参数)");

            // 相机配置
            CfgEnableMirror = _config.Bind("Camera", "EnableMirrorOnStart", false, 
                "启动时是否自动启用摄像机镜像(默认关闭,建议先用UE Explorer测试)");
            
            CfgEnablePortraitMode = _config.Bind("Camera", "EnablePortraitMode", false, 
                "启动时是否自动启用竖屏优化(默认关闭,如启用会在游戏初始化后自动激活)");

            // 窗口配置
            CfgWindowScale = _config.Bind("Window", "ScaleRatio", WindowScaleRatio.OneThird, 
                "小窗缩放比例");
            
            CfgDragMode = _config.Bind("Window", "DragMethod", DragMode.Ctrl_LeftClick, 
                "拖动方式");

        }

        /// <summary>
        /// 保存所有配置
        /// </summary>
        public void Save()
        {
            _config.Save();
        }

        /// <summary>
        /// 重新加载配置
        /// </summary>
        public void Reload()
        {
            _config.Reload();
        }
    }
}
