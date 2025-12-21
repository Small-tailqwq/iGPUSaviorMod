using UnityEngine;
using PotatoOptimization.Core;

namespace PotatoOptimization.Features
{
    /// <summary>
    /// 竖屏模式管理器 - 负责检测竖屏并自动调整相机参数
    /// </summary>
    public class PortraitModeManager
    {
        private bool _isEnabled;
        private bool _isPortraitMode = false;
        private bool _hasOriginalParams = false;

        // 保存的原始相机参数
        private Vector3 _originalPosition;
        private Quaternion _originalRotation;
        private float _originalFOV;
        private float _originalOrthoSize;

        public bool IsEnabled => _isEnabled;
        public bool IsPortraitMode => _isPortraitMode;

        public PortraitModeManager(bool enabledByDefault)
        {
            _isEnabled = enabledByDefault;
            
            // Save original camera parameters immediately at initialization (in landscape state)
            // This prevents saving already-adjusted values if game starts in portrait mode
            if (_isEnabled)
            {
                TrySaveOriginalParamsInLandscape();
            }
        }

        /// <summary>
        /// 切换竖屏优化开关
        /// </summary>
        public void Toggle()
        {
            _isEnabled = !_isEnabled;
            PotatoPlugin.Log.LogWarning($">>> 竖屏优化: {(_isEnabled ? "已启用" : "已禁用")} <<<");

            // 如果禁用且当前处于竖屏模式，恢复相机参数
            if (!_isEnabled && _isPortraitMode)
            {
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    RestoreOriginalParams(mainCam);
                    _isPortraitMode = false;
                }
            }
            // 如果启用且之前没有保存原始参数，尝试保存（仅在横屏时）
            else if (_isEnabled && !_hasOriginalParams)
            {
                TrySaveOriginalParamsInLandscape();
            }
        }

        /// <summary>
        /// 设置启用状态
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            if (_isEnabled == enabled) return;
            
            _isEnabled = enabled;
            if (!enabled && _isPortraitMode)
            {
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    RestoreOriginalParams(mainCam);
                    _isPortraitMode = false;
                }
            }
        }

        /// <summary>
        /// 检测并处理竖屏模式 (在 Update 中调用)
        /// </summary>
        public void Update()
        {
            if (!_isEnabled) return;

            Camera mainCam = Camera.main;
            if (mainCam == null) return;

            // 判断当前是否为竖屏 (高度 > 宽度)
            bool currentIsPortrait = Screen.height > Screen.width;

            // 状态变化时进行处理
            if (currentIsPortrait != _isPortraitMode)
            {
                _isPortraitMode = currentIsPortrait;

                if (_isPortraitMode)
                {
                    PotatoPlugin.Log.LogInfo($"检测到竖屏模式: {Screen.width}x{Screen.height}");
                    
                    // 确保在进入竖屏前已保存原始参数（横屏状态下的参数）
                    if (!_hasOriginalParams)
                    {
                        PotatoPlugin.Log.LogWarning("警告: 未能在横屏状态保存原始参数，将使用当前相机参数作为基准");
                        SaveOriginalParams(mainCam);
                    }
                    
                    ApplyPortraitAdjustment(mainCam);
                }
                else
                {
                    PotatoPlugin.Log.LogInfo($"恢复横屏模式: {Screen.width}x{Screen.height}");
                    
                    // 切换回横屏时，如果之前没有保存原始参数，现在保存
                    if (!_hasOriginalParams)
                    {
                        SaveOriginalParams(mainCam);
                    }
                    else
                    {
                        RestoreOriginalParams(mainCam);
                    }
                }
            }
        }

        private void SaveOriginalParams(Camera cam)
        {
            if (!_hasOriginalParams)
            {
                _originalPosition = cam.transform.position;
                _originalRotation = cam.transform.rotation;
                _originalFOV = cam.fieldOfView;
                _originalOrthoSize = cam.orthographicSize;
                _hasOriginalParams = true;
                
                // 检测异常的相机位置值（可能的多显示器问题）
                float posMagnitude = _originalPosition.magnitude;
                if (posMagnitude > Constants.AbnormalCameraPositionThreshold)
                {
                    PotatoPlugin.Log.LogWarning($"[竖屏优化] 警告: 保存的原始相机位置异常大 (magnitude={posMagnitude:F2})");
                    PotatoPlugin.Log.LogWarning($"[竖屏优化] 这可能是多显示器环境导致的，建议在横屏模式下重新保存参数");
                }
                
                PotatoPlugin.Log.LogInfo($"已保存原始相机参数: Pos={_originalPosition}, Rot={_originalRotation.eulerAngles}, FOV={_originalFOV}");
            }
        }

        private void ApplyPortraitAdjustment(Camera cam)
        {
            Vector3 originalPos = _originalPosition;
            Vector3 originalRot = _originalRotation.eulerAngles;
            float originalFov = _originalFOV;

            // 位置调整 - 基于原始值的相对偏移
            Vector3 newPosition = cam.transform.position;
            newPosition.x = originalPos.x * Constants.PortraitPositionXMultiplier;
            newPosition.y = originalPos.y * Constants.PortraitPositionYMultiplier;
            newPosition.z = originalPos.z * Constants.PortraitPositionZMultiplier;
            
            // 安全检查：如果计算出的位置异常大（可能是多显示器问题），记录警告
            float positionMagnitude = newPosition.magnitude;
            if (positionMagnitude > Constants.AbnormalCameraPositionThreshold)
            {
                PotatoPlugin.Log.LogWarning($"[竖屏优化] 警告: 计算出的相机位置异常大 (magnitude={positionMagnitude:F2})，可能是多显示器环境导致");
                PotatoPlugin.Log.LogWarning($"[竖屏优化] 原始位置: {originalPos}, 计算位置: {newPosition}");
            }
            
            cam.transform.position = newPosition;

            // 旋转调整 - 基于原始欧拉角的相对变化
            Vector3 newRotation = originalRot;
            newRotation.x = originalRot.x * Constants.PortraitRotationXMultiplier;
            newRotation.y = originalRot.y * Constants.PortraitRotationYMultiplier;
            newRotation.z = originalRot.z;
            cam.transform.rotation = Quaternion.Euler(newRotation);

            // FOV 调整
            if (cam.orthographic)
            {
                cam.orthographicSize = _originalOrthoSize * Constants.PortraitFOVMultiplier;
            }
            else
            {
                cam.fieldOfView = originalFov * Constants.PortraitFOVMultiplier;
            }

            PotatoPlugin.Log.LogInfo($"[竖屏优化] 已应用相对调整:\n" +
                $"  原始 Pos={originalPos:F4} Rot={originalRot:F4} FOV={originalFov:F2}\n" +
                $"  调整 Pos={cam.transform.position:F4} Rot={cam.transform.rotation.eulerAngles:F4} FOV={cam.fieldOfView:F2}");
        }

        private void TrySaveOriginalParamsInLandscape()
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                // Only save if we're in landscape mode
                if (Screen.width >= Screen.height)
                {
                    SaveOriginalParams(mainCam);
                    PotatoPlugin.Log.LogInfo("横屏状态下保存原始相机参数");
                }
                else
                {
                    PotatoPlugin.Log.LogWarning("检测到竖屏状态，将在首次切换到横屏时保存原始参数");
                }
            }
        }

        private void RestoreOriginalParams(Camera cam)
        {
            if (_hasOriginalParams)
            {
                cam.transform.position = _originalPosition;
                cam.transform.rotation = _originalRotation;
                cam.fieldOfView = _originalFOV;
                cam.orthographicSize = _originalOrthoSize;
                PotatoPlugin.Log.LogInfo($"已恢复原始相机参数: Pos={_originalPosition}, Rot={_originalRotation.eulerAngles}, FOV={_originalFOV}");
            }
        }
    }
}
