using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using PotatoOptimization.Features;

namespace PotatoOptimization.Patches
{
    /// <summary>
    /// Harmony 补丁：拦截鼠标输入并在镜像模式下翻转坐标
    /// </summary>
    [HarmonyPatch(typeof(Input), "mousePosition", MethodType.Getter)]
    public class InputMousePositionPatch
    {
        /// <summary>
        /// 镜像模式是否启用 (由 CameraMirrorManager 控制)
        /// </summary>
        public static bool IsInputMirrored { get; set; } = false;

        // 拖拽场景下的判定缓存：按下时是否位于 UI
        private static bool _isPointerDown;
        private static bool _pressedOverUI;

        static void Postfix(ref Vector3 __result)
        {
            // 捕获按下/抬起状态，锁定拖拽周期内的判定来源
            UpdatePointerState(__result);

            // 仅在镜像模式启用时处理
            if (!IsInputMirrored)
                return;

            bool allowMirror = ShouldMirror(__result);
            if (!allowMirror)
                return;

            // 鼠标在 3D 场景上，翻转坐标
            __result = new Vector3(
                Screen.width - __result.x,  // 水平翻转
                __result.y,                   // Y 保持不变
                __result.z                    // Z 保持不变
            );
        }

        /// <summary>
        /// 根据按下瞬间的命中结果，锁定整个拖拽流程的镜像规则
        /// </summary>
        private static bool ShouldMirror(Vector3 mousePosition)
        {
            if (!_isPointerDown)
            {
                // 未拖拽时保持原逻辑：UI 不镜像，场景镜像
                return !IsPointerOverUI(mousePosition);
            }

            // 拖拽中：沿用按下时的命中结果
            return !_pressedOverUI;
        }

        /// <summary>
        /// 记录按下/抬起事件，避免拖拽过程中因 UI 状态切换导致镜像跳变
        /// </summary>
        private static void UpdatePointerState(Vector3 mousePosition)
        {
            bool pressedThisFrame = Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2);
            if (pressedThisFrame)
            {
                _pressedOverUI = IsPointerOverUI(mousePosition);
                _isPointerDown = true;
            }

            bool releasedThisFrame = Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2);
            if (releasedThisFrame)
            {
                // 只有当所有鼠标按钮都释放时才退出拖拽状态
                bool anyButtonStillPressed = Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2);
                if (!anyButtonStillPressed)
                {
                    _isPointerDown = false;
                }
            }
        }

        /// <summary>
        /// 检测鼠标位置是否在 UI 元素上
        /// </summary>
        private static bool IsPointerOverUI(Vector3 mousePosition)
        {
            // 使用 EventSystem 检测 UI
            if (EventSystem.current != null)
            {
                PointerEventData eventData = new PointerEventData(EventSystem.current)
                {
                    position = new Vector2(mousePosition.x, mousePosition.y)
                };

                List<RaycastResult> results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(eventData, results);

                // 过滤掉镜像 Canvas（MirrorCanvas）
                foreach (var result in results)
                {
                    if (result.gameObject != null && 
                        result.gameObject.name != "MirrorDisplay" && 
                        result.gameObject.GetComponentInParent<Canvas>() != null &&
                        result.gameObject.GetComponentInParent<Canvas>().name != "MirrorCanvas")
                    {
                        return true; // 检测到游戏 UI
                    }
                }
            }

            return false; // 没有 UI，是 3D 场景
        }
    }
}
