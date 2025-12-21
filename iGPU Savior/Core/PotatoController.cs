using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using PotatoOptimization.Configuration;
using PotatoOptimization.Features;

namespace PotatoOptimization.Core
{
    /// <summary>
    /// 主控制器 - 协调所有功能模块的核心组件
    /// </summary>
    public class PotatoController : MonoBehaviour
    {
        // 配置管理器
        private ConfigurationManager _config;

        // 功能管理器
        private RenderQualityManager _renderManager;
        private CameraMirrorManager _mirrorManager;
        private PortraitModeManager _portraitManager;
        private WindowStateManager _windowManager;
        private AudioManager _audioManager;

        void Start()
        {
            // 获取配置管理器
            _config = PotatoPlugin.Config;

            // 初始化功能模块
            InitializeManagers();

            // 注册场景加载回调
            SceneManager.sceneLoaded += OnSceneLoaded;

            // 如果配置要求启动时启用镜像
            if (_config.CfgEnableMirror.Value)
            {
                StartCoroutine(ApplyMirrorOnStart());
            }

            // 竖屏优化不在Start中启用，只在场景加载后启用（确保游戏完全初始化）
            // 这样可以避免保存游戏初始化时的默认相机位置(0,1,-10)

            PotatoPlugin.Log.LogInfo("PotatoController 已启动");
        }

        void Update()
        {
            HandleHotkeyInput();
            UpdateManagers();
        }

        void OnDestroy()
        {
            // 清理资源
            _mirrorManager?.Cleanup();
            
            // 取消场景加载回调
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void InitializeManagers()
        {
            // 创建功能管理器实例
            _audioManager = new AudioManager();
            _renderManager = new RenderQualityManager();
            _mirrorManager = new CameraMirrorManager(_audioManager);
            _portraitManager = new PortraitModeManager();  // 默认禁用，由协程或用户手动启用
            _windowManager = new WindowStateManager(_config);
        }

        private void HandleHotkeyInput()
        {
            // F2 - 土豆模式
            if (Input.GetKeyDown(_config.KeyPotatoMode.Value))
            {
                _renderManager.TogglePotatoMode();
            }

            // F3 - PiP 模式
            if (Input.GetKeyDown(_config.KeyPiPMode.Value))
            {
                StartCoroutine(_windowManager.TogglePiPMode());
            }

            // F4 - 相机镜像
            if (Input.GetKeyDown(_config.KeyCameraMirror.Value))
            {
                _mirrorManager.Toggle();
            }

            // F5 - 竖屏优化
            if (Input.GetKeyDown(_config.KeyPortraitMode.Value))
            {
                _portraitManager.Toggle();
            }
        }

        private void UpdateManagers()
        {
            // 更新土豆模式 (定期刷新设置)
            _renderManager.UpdatePotatoMode();

            // 检测镜像模式的分辨率变化
            _mirrorManager.CheckResolutionChange();

            // 更新竖屏优化
            _portraitManager.Update();

            // 处理窗口拖动
            _windowManager.HandleDragLogic();
        }

        private IEnumerator ApplyMirrorOnStart()
        {
            // 等待场景加载完成
            yield return new WaitForSeconds(0.5f);

            if (_config.CfgEnableMirror.Value)
            {
                _mirrorManager.SetMirrorState(true);
                PotatoPlugin.Log.LogWarning(">>> 启动时已自动启用摄像机镜像 <<<");
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 场景加载后刷新土豆模式设置
            if (_renderManager.IsPotatoMode)
            {
                _renderManager.UpdatePotatoMode();
            }

            // 延迟15秒后自动镜像（如果开启自动镜像）
            if (_config != null && _config.CfgEnableMirror.Value)
            {
                StartCoroutine(DelayedMirror());
            }

            // 延迟15秒后自动启用竖屏优化（如果开启自动启用）
            if (_config != null && _config.CfgEnablePortraitMode.Value)
            {
                StartCoroutine(DelayedPortraitMode());
            }
        }

        private IEnumerator DelayedMirror()
        {
            yield return new WaitForSeconds(15f);
            _mirrorManager?.SetMirrorState(true);
            PotatoPlugin.Log.LogWarning($">>> 场景切换后已自动启用摄像机镜像 <<<");
        }

        private IEnumerator DelayedPortraitMode()
        {
            yield return new WaitForSeconds(15f);
            _portraitManager?.SetEnabled(true);
            PotatoPlugin.Log.LogWarning($">>> 场景切换后已自动启用竖屏优化 <<<");
        }

        /// <summary>
        /// 公共方法：供外部（如 UI）调用设置镜像状态
        /// </summary>
        public void SetMirrorState(bool enabled)
        {
            _mirrorManager?.SetMirrorState(enabled);
        }
    }
}
