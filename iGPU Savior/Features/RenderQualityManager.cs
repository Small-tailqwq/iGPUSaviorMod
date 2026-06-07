using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using PotatoOptimization.Core;

namespace PotatoOptimization.Features
{
    /// <summary>
    /// 渲染质量管理器 — 三级模式：正常 / 土豆(F2) / 后台(自动)
    /// </summary>
    public class RenderQualityManager
    {
        private enum RenderMode { Normal, Potato, Background }

        private RenderMode _currentMode = RenderMode.Normal;
        private float _lastRunTime = 0f;
        private readonly Dictionary<Volume, float> _originalVolumeWeights = new Dictionary<Volume, float>();

        // 土豆模式修改前的原始值
        private bool _savedPotatoOriginals = false;
        private bool _savedPotatoUrpOriginals = false;
        private int _origTargetFrameRate;
        private int _origVSyncCount;
        private float _origRenderScale;
        private float _origShadowDistance;
        private int _origMSAASampleCount;
        private bool _origHDR;
        private bool _origSoftShadows;
        private bool _origAdditionalLightShadows;
        private bool _origMainLightShadows;
        private bool _origDepthTexture;
        private bool _origOpaqueTexture;
        private int _origMaxAdditionalLights;

        // 后台模式保存的设置
        private bool _savedBackgroundFrameSettings = false;
        private int _foregroundTargetFrameRate;
        private int _foregroundVSyncCount;
        private bool _savedBackgroundUrpOriginals = false;
        private float _origBgRenderScale;
        private float _origBgShadowDistance;
        private bool _isPiPModeActive;

        // 反射缓存（URP 部分属性在编辑器中是只读的，需要通过反射写入）
        private static readonly PropertyInfo _propHDR = typeof(UniversalRenderPipelineAsset).GetProperty("supportsHDR");
        private static readonly PropertyInfo _propSoftShadows = typeof(UniversalRenderPipelineAsset).GetProperty("supportsSoftShadows");
        private static readonly PropertyInfo _propMainLightShadows = typeof(UniversalRenderPipelineAsset).GetProperty("supportsMainLightShadows");
        private static readonly PropertyInfo _propAdditionalLightShadows = typeof(UniversalRenderPipelineAsset).GetProperty("supportsAdditionalLightShadows");
        private static readonly PropertyInfo _propDepthTexture = typeof(UniversalRenderPipelineAsset).GetProperty("supportsCameraDepthTexture");
        private static readonly PropertyInfo _propOpaqueTexture = typeof(UniversalRenderPipelineAsset).GetProperty("supportsCameraOpaqueTexture");
        private static readonly PropertyInfo _propMaxAdditionalLights = typeof(UniversalRenderPipelineAsset).GetProperty("maxAdditionalLightsCount");

        public bool IsPotatoMode => _currentMode == RenderMode.Potato;
        public bool IsBackgroundMode => _currentMode == RenderMode.Background;

        /// <summary>
        /// 小窗已有较低渲染分辨率，期间完全豁免自动后台优化。
        /// </summary>
        public void SetPiPModeActive(bool active)
        {
            if (_isPiPModeActive == active) return;

            _isPiPModeActive = active;
            if (active && _currentMode == RenderMode.Background)
            {
                SetMode(RenderMode.Normal);
            }
            else if (!active &&
                     _currentMode == RenderMode.Normal &&
                     !Application.isFocused &&
                     PotatoPlugin.Config.CfgEnableBackgroundOptimization.Value)
            {
                SetMode(RenderMode.Background);
            }
        }

        /// <summary>
        /// 切换土豆模式（F2 快捷键调用）
        /// </summary>
        public void TogglePotatoMode()
        {
            if (_currentMode == RenderMode.Potato)
            {
                // 关闭土豆模式 → 如果窗口不在前台则切后台模式，否则切正常
                if (_isPiPModeActive ||
                    Application.isFocused ||
                    !PotatoPlugin.Config.CfgEnableBackgroundOptimization.Value)
                    SetMode(RenderMode.Normal);
                else
                    SetMode(RenderMode.Background);
                PotatoPlugin.Log.LogWarning(">>> 土豆模式: OFF <<<");
            }
            else
            {
                SetMode(RenderMode.Potato);
                PotatoPlugin.Log.LogWarning(">>> 土豆模式: ON <<<");
            }
        }

        /// <summary>
        /// 焦点变化时调用（由 PotatoController 驱动）
        /// </summary>
        public void OnFocusChanged(bool focused)
        {
            if (_currentMode == RenderMode.Potato)
                return; // 土豆模式优先级最高，不受焦点影响

            if (_isPiPModeActive)
            {
                if (_currentMode == RenderMode.Background)
                    SetMode(RenderMode.Normal);
                return;
            }

            // 后台优化开关关闭时，失焦不做任何处理
            if (!focused && !PotatoPlugin.Config.CfgEnableBackgroundOptimization.Value)
                return;

            SetMode(focused ? RenderMode.Normal : RenderMode.Background);
        }

        /// <summary>
        /// 定期刷新（防止游戏覆盖设置）
        /// </summary>
        public void Update()
        {
            if (_currentMode == RenderMode.Background &&
                (!PotatoPlugin.Config.CfgEnableBackgroundOptimization.Value || _isPiPModeActive))
            {
                SetMode(RenderMode.Normal);
                return;
            }

            if (_currentMode == RenderMode.Normal)
                return;

            float interval = _currentMode == RenderMode.Background
                ? Constants.BackgroundRunInterval
                : Constants.DefaultRunInterval;

            if (Time.realtimeSinceStartup - _lastRunTime > interval)
            {
                ApplyCurrentMode();
                _lastRunTime = Time.realtimeSinceStartup;
            }
        }

        // ==================== 核心切换逻辑 ====================

        private void SetMode(RenderMode mode)
        {
            if (_currentMode == mode) return;

            if (_currentMode == RenderMode.Background)
                RestoreBackgroundMode();
            else if (_currentMode == RenderMode.Potato)
                RestorePotatoMode();

            _currentMode = mode;
            ApplyCurrentMode();
            _lastRunTime = Time.realtimeSinceStartup;
        }

        private void ApplyCurrentMode()
        {
            switch (_currentMode)
            {
                case RenderMode.Background:
                    ApplyBackgroundMode();
                    break;
                case RenderMode.Potato:
                    ApplyPotatoMode();
                    break;
                case RenderMode.Normal:
                    break;
            }
        }

        // ==================== 后台模式（温和限帧） ====================

        private void ApplyBackgroundMode()
        {
            try
            {
                if (!_savedBackgroundFrameSettings)
                {
                    _foregroundTargetFrameRate = Application.targetFrameRate;
                    _foregroundVSyncCount = QualitySettings.vSyncCount;
                    _savedBackgroundFrameSettings = true;
                }

                Application.targetFrameRate = Constants.BackgroundTargetFPS;
                QualitySettings.vSyncCount = 0;

                var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                if (urp != null)
                {
                    if (!_savedBackgroundUrpOriginals)
                    {
                        _origBgRenderScale = urp.renderScale;
                        _origBgShadowDistance = urp.shadowDistance;
                        _savedBackgroundUrpOriginals = true;
                    }
                    urp.renderScale = Constants.BackgroundRenderScale;
                    urp.shadowDistance = 0f;
                }
            }
            catch (System.Exception e)
            {
                PotatoPlugin.Log.LogError($"后台模式应用失败: {e.Message}");
            }
        }

        private void RestoreBackgroundMode()
        {
            if (!_savedBackgroundFrameSettings && !_savedBackgroundUrpOriginals) return;

            Application.targetFrameRate = _foregroundTargetFrameRate;
            QualitySettings.vSyncCount = _foregroundVSyncCount;
            _savedBackgroundFrameSettings = false;

            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp != null && _savedBackgroundUrpOriginals)
            {
                urp.renderScale = _origBgRenderScale;
                urp.shadowDistance = _origBgShadowDistance;
                _savedBackgroundUrpOriginals = false;
            }
        }

        // ==================== 土豆模式（手动切换） ====================

        private void ApplyPotatoMode()
        {
            try
            {
                var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                SavePotatoOriginals(urp);

                Application.targetFrameRate = Constants.PotatoModeTargetFPS;
                QualitySettings.vSyncCount = 0;

                if (urp != null)
                {
                    urp.renderScale = Constants.PotatoModeRenderScale;
                    urp.shadowDistance = Constants.PotatoModeShadowDistance;
                    urp.msaaSampleCount = 1;
                    SetURPProperty(_propHDR, urp, false);
                    SetURPProperty(_propSoftShadows, urp, false);
                    SetURPProperty(_propMainLightShadows, urp, false);
                    SetURPProperty(_propAdditionalLightShadows, urp, false);
                    SetURPProperty(_propMaxAdditionalLights, urp, 2);
                    // 保留深度/不透明纹理（土豆模式下某些 shader 可能仍需要）
                }

                DisableAllVolumes();
            }
            catch (System.Exception e)
            {
                PotatoPlugin.Log.LogError($"土豆模式应用失败: {e.Message}");
            }
        }

        // ==================== 恢复土豆模式前的画质 ====================

        private void RestorePotatoMode()
        {
            try
            {
                if (!_savedPotatoOriginals) return;

                Application.targetFrameRate = _origTargetFrameRate;
                QualitySettings.vSyncCount = _origVSyncCount;

                var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                if (urp != null && _savedPotatoUrpOriginals)
                {
                    urp.renderScale = _origRenderScale;
                    urp.shadowDistance = _origShadowDistance;
                    urp.msaaSampleCount = _origMSAASampleCount;
                    SetURPProperty(_propHDR, urp, _origHDR);
                    SetURPProperty(_propSoftShadows, urp, _origSoftShadows);
                    SetURPProperty(_propMainLightShadows, urp, _origMainLightShadows);
                    SetURPProperty(_propAdditionalLightShadows, urp, _origAdditionalLightShadows);
                    SetURPProperty(_propDepthTexture, urp, _origDepthTexture);
                    SetURPProperty(_propOpaqueTexture, urp, _origOpaqueTexture);
                    SetURPProperty(_propMaxAdditionalLights, urp, _origMaxAdditionalLights);
                }

                RestoreAllVolumes();
                _savedPotatoOriginals = false;
                _savedPotatoUrpOriginals = false;
            }
            catch (System.Exception e)
            {
                PotatoPlugin.Log.LogError($"恢复土豆模式前画质失败: {e.Message}");
            }
        }

        // ==================== Volume 管理 ====================

        private void DisableAllVolumes()
        {
            var deadKeys = _originalVolumeWeights.Where(kvp => kvp.Key == null).Select(kvp => kvp.Key).ToList();
            foreach (var key in deadKeys)
                _originalVolumeWeights.Remove(key);

            var volumes = Object.FindObjectsOfType<Volume>();
            if (volumes == null) return;

            foreach (var vol in volumes)
            {
                if (vol != null && vol.enabled)
                {
                    if (!_originalVolumeWeights.ContainsKey(vol))
                        _originalVolumeWeights[vol] = vol.weight;
                    vol.weight = 0f;
                }
            }
        }

        private void RestoreAllVolumes()
        {
            foreach (var kvp in _originalVolumeWeights)
            {
                if (kvp.Key != null)
                    kvp.Key.weight = kvp.Value;
            }
            _originalVolumeWeights.Clear();
        }

        // ==================== 反射辅助 ====================

        private static void SetURPProperty(PropertyInfo prop, UniversalRenderPipelineAsset urp, object value)
        {
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(urp, value);
            }
            else if (prop != null)
            {
                // 只读属性 → 尝试通过 backing field 写入
                var field = typeof(UniversalRenderPipelineAsset).GetField(
                    $"<{prop.Name}>k__BackingField",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(urp, value);
                }
            }
        }

        private static object ReadURPProperty(PropertyInfo prop, UniversalRenderPipelineAsset urp, object fallback)
        {
            if (prop != null)
                return prop.GetValue(urp) ?? fallback;
            return fallback;
        }

        // ==================== 原始值保存 ====================

        private void SavePotatoOriginals(UniversalRenderPipelineAsset urp)
        {
            if (_savedPotatoOriginals) return;

            _origTargetFrameRate = Application.targetFrameRate;
            _origVSyncCount = QualitySettings.vSyncCount;

            if (urp != null)
            {
                _origRenderScale = urp.renderScale;
                _origShadowDistance = urp.shadowDistance;
                _origMSAASampleCount = urp.msaaSampleCount;
                _origHDR = (bool)ReadURPProperty(_propHDR, urp, true);
                _origSoftShadows = (bool)ReadURPProperty(_propSoftShadows, urp, false);
                _origMainLightShadows = (bool)ReadURPProperty(_propMainLightShadows, urp, false);
                _origAdditionalLightShadows = (bool)ReadURPProperty(_propAdditionalLightShadows, urp, false);
                _origDepthTexture = (bool)ReadURPProperty(_propDepthTexture, urp, false);
                _origOpaqueTexture = (bool)ReadURPProperty(_propOpaqueTexture, urp, false);
                _origMaxAdditionalLights = (int)ReadURPProperty(_propMaxAdditionalLights, urp, 4);
                _savedPotatoUrpOriginals = true;
            }

            _savedPotatoOriginals = true;
        }
    }
}
