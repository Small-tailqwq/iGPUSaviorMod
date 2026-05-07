using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using PotatoOptimization.Core;

namespace PotatoOptimization.Features
{
    /// <summary>
    /// 渲染质量管理器 - 负责土豆模式和渲染质量控制
    /// </summary>
    public class RenderQualityManager
    {
        private bool _isPotatoMode = false;
        private float _lastRunTime = 0f;

        public bool IsPotatoMode => _isPotatoMode;

        /// <summary>
        /// 切换土豆模式
        /// </summary>
        public void TogglePotatoMode()
        {
            _isPotatoMode = !_isPotatoMode;
            
            if (_isPotatoMode)
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

        /// <summary>
        /// 定期刷新土豆模式设置 (某些设置可能被游戏覆盖)
        /// </summary>
        public void UpdatePotatoMode()
        {
            if (!_isPotatoMode) return;

            if (Time.realtimeSinceStartup - _lastRunTime > Constants.DefaultRunInterval)
            {
                ApplyPotatoMode(false);
                _lastRunTime = Time.realtimeSinceStartup;
            }
        }

        /// <summary>
        /// 应用土豆模式设置
        /// </summary>
        private void ApplyPotatoMode(bool showLog)
        {
            try
            {
                // 设置目标帧率
                Application.targetFrameRate = Constants.PotatoModeTargetFPS;
                QualitySettings.vSyncCount = 0;

                // 直接强转 URP 管线，无需反射
                var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                if (urp != null)
                {
                    urp.renderScale = Constants.PotatoModeRenderScale;
                    urp.shadowDistance = Constants.PotatoModeShadowDistance;
                    urp.msaaSampleCount = 1;
                }

                // 禁用所有体积效果（直接查找 Volume 组件）
                var volumes = UnityEngine.Object.FindObjectsOfType<Volume>();
                if (volumes != null)
                {
                    foreach (var vol in volumes)
                    {
                        if (vol != null && vol.enabled)
                        {
                            vol.weight = 0f;
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                if (showLog)
                    PotatoPlugin.Log.LogError($"应用土豆模式失败: {e.Message}");
            }
        }

        /// <summary>
        /// 恢复正常画质
        /// </summary>
        private void RestoreQuality()
        {
            try
            {
                Application.targetFrameRate = Constants.NormalModeTargetFPS;

                var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                if (urp != null)
                {
                    urp.renderScale = Constants.NormalRenderScale;
                    urp.shadowDistance = Constants.NormalShadowDistance;
                }
            }
            catch (System.Exception e)
            {
                PotatoPlugin.Log.LogError($"恢复画质失败: {e.Message}");
            }
        }
    }
}
