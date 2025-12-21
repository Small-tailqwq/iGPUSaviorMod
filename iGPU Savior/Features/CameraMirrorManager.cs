using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using PotatoOptimization.Core;

namespace PotatoOptimization.Features
{
    /// <summary>
    /// 相机镜像管理器 - 负责相机左右翻转功能
    /// </summary>
    public class CameraMirrorManager
    {
        private readonly AudioManager _audioManager;
        
        private bool _isMirrored = false;
        private RenderTexture _mirrorRenderTexture;
        private GameObject _mirrorCanvas;
        private RawImage _mirrorRawImage;
        private Material _mirrorFlipMaterial;
        private int _lastScreenWidth;
        private int _lastScreenHeight;

        public bool IsMirrored => _isMirrored;

        public CameraMirrorManager(AudioManager audioManager)
        {
            _audioManager = audioManager;
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            
            // 记录初始屏幕信息，帮助诊断多显示器问题
            PotatoPlugin.Log.LogInfo($"[镜像管理器] 初始化: 屏幕分辨率 {Screen.width}x{Screen.height}");
        }

        /// <summary>
        /// 切换镜像状态
        /// </summary>
        public void Toggle()
        {
            SetMirrorState(!_isMirrored);
        }

        /// <summary>
        /// 设置镜像状态
        /// </summary>
        public void SetMirrorState(bool enabled)
        {
            _isMirrored = enabled;
            ApplyMirrorMode();
        }

        /// <summary>
        /// 检测并处理分辨率变化
        /// </summary>
        public void CheckResolutionChange()
        {
            if (!_isMirrored) return;

            if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
            {
                PotatoPlugin.Log.LogInfo($"检测到分辨率变化: {_lastScreenWidth}x{_lastScreenHeight} -> {Screen.width}x{Screen.height}");
                
                _lastScreenWidth = Screen.width;
                _lastScreenHeight = Screen.height;

                RecreateRenderTexture();
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup()
        {
            if (_isMirrored)
            {
                DisableMirrorMode();
            }
        }

        private void ApplyMirrorMode()
        {
            if (_isMirrored)
            {
                EnableMirrorMode();
            }
            else
            {
                DisableMirrorMode();
            }
        }

        private void EnableMirrorMode()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                PotatoPlugin.Log.LogWarning(">>> 未找到主摄像机，跳过镜像 <<<");
                return;
            }

            try
            {
                // 1. 创建 RenderTexture
                _mirrorRenderTexture = CreateMirrorRenderTexture();
                mainCam.targetTexture = _mirrorRenderTexture;

                // 2. 创建翻转材质
                _mirrorFlipMaterial = CreateFlipMaterial();

                // 3. 创建全屏 Canvas + RawImage
                CreateMirrorCanvas();

                // 4. 启用输入镜像
                Patches.InputMousePositionPatch.IsInputMirrored = true;

                // 5. 启用音频声道交换
                _audioManager.EnableChannelSwap();

                PotatoPlugin.Log.LogWarning(">>> 镜像模式: ON (RenderTexture 方案，不破坏法线) <<<");
            }
            catch (Exception e)
            {
                PotatoPlugin.Log.LogError($"启用镜像模式失败: {e.Message}");
                DisableMirrorMode();
            }
        }

        private void DisableMirrorMode()
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.targetTexture = null;
            }

            // 清理资源
            if (_mirrorCanvas != null)
            {
                UnityEngine.Object.Destroy(_mirrorCanvas);
                _mirrorCanvas = null;
            }

            if (_mirrorRenderTexture != null)
            {
                _mirrorRenderTexture.Release();
                _mirrorRenderTexture = null;
            }

            if (_mirrorFlipMaterial != null)
            {
                UnityEngine.Object.Destroy(_mirrorFlipMaterial);
                _mirrorFlipMaterial = null;
            }

            _mirrorRawImage = null;

            // 禁用输入镜像
            Patches.InputMousePositionPatch.IsInputMirrored = false;

            // 禁用音频声道交换
            _audioManager.DisableChannelSwap();

            PotatoPlugin.Log.LogWarning(">>> 镜像模式: OFF <<<");
        }

        private void RecreateRenderTexture()
        {
            if (!_isMirrored || _mirrorRenderTexture == null)
                return;

            try
            {
                Camera mainCam = Camera.main;
                if (mainCam == null)
                    return;

                // 1. 暂时断开摄像机
                mainCam.targetTexture = null;

                // 2. 释放旧 RT
                _mirrorRenderTexture.Release();

                // 3. 创建新 RT
                _mirrorRenderTexture = CreateMirrorRenderTexture();

                // 4. 重新连接
                mainCam.targetTexture = _mirrorRenderTexture;
                if (_mirrorRawImage != null)
                {
                    _mirrorRawImage.texture = _mirrorRenderTexture;
                }

                PotatoPlugin.Log.LogInfo("已重建 RenderTexture，适应新分辨率");
            }
            catch (Exception e)
            {
                PotatoPlugin.Log.LogError($"重建 RenderTexture 失败: {e.Message}");
            }
        }

        private RenderTexture CreateMirrorRenderTexture()
        {
            float renderScale = 1.0f;

            // 尝试获取 renderScale
            try
            {
                var pipeline = GraphicsSettings.currentRenderPipeline;
                if (pipeline != null)
                {
                    PropertyInfo scaleProp = pipeline.GetType().GetProperty("renderScale");
                    if (scaleProp != null && scaleProp.CanRead)
                    {
                        object scaleValue = scaleProp.GetValue(pipeline, null);
                        if (scaleValue != null)
                        {
                            renderScale = (float)scaleValue;
                        }
                    }
                }
            }
            catch
            {
                // 忽略错误，使用默认值 1.0
            }

            int width = Mathf.Max(Constants.RenderTextureMinSize, (int)(Screen.width * renderScale));
            int height = Mathf.Max(Constants.RenderTextureMinSize, (int)(Screen.height * renderScale));

            RenderTexture rt = new RenderTexture(width, height, 24);
            rt.name = "MirrorRT";
            rt.antiAliasing = QualitySettings.antiAliasing > 0 ? QualitySettings.antiAliasing : 1;
            rt.filterMode = FilterMode.Bilinear;
            rt.wrapMode = TextureWrapMode.Clamp;
            rt.Create();

            PotatoPlugin.Log.LogInfo($"创建 RenderTexture: {width}x{height} (renderScale={renderScale:F2})");
            return rt;
        }

        private void CreateMirrorCanvas()
        {
            _mirrorCanvas = new GameObject("MirrorCanvas");
            UnityEngine.Object.DontDestroyOnLoad(_mirrorCanvas);

            Canvas canvas = _mirrorCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -100;

            CanvasScaler scaler = _mirrorCanvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            GameObject rawImageObj = new GameObject("MirrorDisplay");
            rawImageObj.transform.SetParent(_mirrorCanvas.transform, false);

            _mirrorRawImage = rawImageObj.AddComponent<RawImage>();
            _mirrorRawImage.texture = _mirrorRenderTexture;
            _mirrorRawImage.material = _mirrorFlipMaterial;
            _mirrorRawImage.raycastTarget = false;

            RectTransform rt = rawImageObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private Material CreateFlipMaterial()
        {
            Shader shader = Shader.Find("UI/Default");
            if (shader == null)
            {
                PotatoPlugin.Log.LogWarning("未找到 UI/Default Shader，尝试使用 Unlit/Texture");
                shader = Shader.Find("Unlit/Texture");
            }

            Material mat = new Material(shader);
            mat.mainTextureScale = new Vector2(-1, 1);
            mat.mainTextureOffset = new Vector2(1, 0);

            return mat;
        }
    }
}
