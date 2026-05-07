using HarmonyLib;
using UnityEngine;
using System.Reflection;

namespace PotatoOptimization.Patches
{
    /// <summary>
    /// 镜像模式输入控制
    /// 不再全局镜像 Input.mousePosition（会污染 EventSystem 的 UI hover 判定）
    /// 改为只在 3D 射线检测时镜像坐标
    /// </summary>
    public static class InputMousePositionPatch
    {
        /// <summary>
        /// 镜像模式是否启用 (由 CameraMirrorManager 控制)
        /// </summary>
        public static bool IsInputMirrored { get; set; } = false;

        /// <summary>
        /// 获取镜像后的鼠标坐标（水平翻转）
        /// </summary>
        public static Vector3 GetMirroredMousePosition()
        {
            Vector3 pos = Input.mousePosition;
            return new Vector3(Screen.width - pos.x, pos.y, pos.z);
        }
    }

    /// <summary>
    /// 补丁 FacilityClickHeroine.UpdateHeroineClickFlag
    /// 在镜像模式下使用镜像坐标进行 3D 射线检测，避免污染 UI hover
    /// </summary>
    [HarmonyPatch]
    public class FacilityClickHeroineMirrorPatch
    {
        static MethodBase TargetMethod()
        {
            return typeof(Bulbul.FacilityClickHeroine).GetMethod("UpdateHeroineClickFlag",
                BindingFlags.Instance | BindingFlags.NonPublic);
        }

        static bool Prefix(object __instance)
        {
            if (!InputMousePositionPatch.IsInputMirrored)
                return true;

            var type = __instance.GetType();
            var heroineLayerMaskField = type.GetField("_heroineLayerMask",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var isClickedProp = type.GetProperty("IsClickedHeroineCurrentFrame",
                BindingFlags.Instance | BindingFlags.Public);

            if (heroineLayerMaskField == null || isClickedProp == null)
                return true;

            int heroineLayerMask = (int)heroineLayerMaskField.GetValue(__instance);
            isClickedProp.SetValue(__instance, false);

            if (Input.GetMouseButtonDown(0))
            {
                Vector3 mirroredPos = InputMousePositionPatch.GetMirroredMousePosition();
                Ray ray = Camera.main.ScreenPointToRay(mirroredPos);
                bool hit = Physics.Raycast(ray, out _, float.PositiveInfinity, heroineLayerMask);
                isClickedProp.SetValue(__instance, hit);
            }

            return false;
        }
    }

    /// <summary>
    /// 补丁 CursorService.UpdateCursor
    /// 在镜像模式下使用镜像坐标做光标样式射线检测
    /// </summary>
    [HarmonyPatch]
    public class CursorServiceMirrorPatch
    {
        private static MethodInfo MI_ChangeCursorDefault;
        private static MethodInfo MI_ChangeCursorTalk;
        private static MethodInfo MI_ChangeCursorTalkBlock;

        static MethodBase TargetMethod()
        {
            return typeof(CursorService).GetMethod("UpdateCursor",
                BindingFlags.Instance | BindingFlags.Public);
        }

        static void Prepare()
        {
            var t = typeof(CursorService);
            MI_ChangeCursorDefault = t.GetMethod("ChangeCursorDefault", BindingFlags.Instance | BindingFlags.NonPublic);
            MI_ChangeCursorTalk = t.GetMethod("ChangeCursorTalk", BindingFlags.Instance | BindingFlags.NonPublic);
            MI_ChangeCursorTalkBlock = t.GetMethod("ChangeCursorTalkBlock", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        static bool Prefix(CursorService __instance)
        {
            if (!InputMousePositionPatch.IsInputMirrored)
                return true;

            if (Bulbul.DevicePlatformExtension.IsMobile(Bulbul.DevicePlatform.Steam))
                return false;

            var heroineService = GetField<Bulbul.HeroineService>(__instance, "_heroineService");
            var scenarioReader = GetField<Bulbul.ScenarioReader>(__instance, "_scenarioReader");
            var roomGameManager = GetField<Bulbul.RoomGameManager>(__instance, "_roomGameManager");
            int heroineLayerMask = GetFieldValue<int>(__instance, "_heroineLayerMask");

            // 光标样式：用镜像坐标做 3D 射线
            if (Bulbul.InputController.Instance.CurrentFrameEventSystemRaycastResult.Count > 0 || Camera.main == null)
            {
                Invoke(MI_ChangeCursorDefault, __instance);
            }
            else if (Physics.Raycast(Camera.main.ScreenPointToRay(InputMousePositionPatch.GetMirroredMousePosition()),
                out RaycastHit hitInfo, float.PositiveInfinity, heroineLayerMask))
            {
                if (heroineService.IsPossibleClickHeroineReaction())
                {
                    if (scenarioReader.IsPlayingScenario() ||
                        roomGameManager.CurrentMainState == Bulbul.RoomGameManager.MainState.TalkingGameStartDirection ||
                        roomGameManager.CurrentMainState == Bulbul.RoomGameManager.MainState.EndGameStartDirection ||
                        roomGameManager.CurrentMainState == Bulbul.RoomGameManager.MainState.Tutorial0 ||
                        roomGameManager.CurrentMainState == Bulbul.RoomGameManager.MainState.Tutorial1)
                    {
                        if (roomGameManager.CurrentMainState == Bulbul.RoomGameManager.MainState.Tutorial4)
                            Invoke(MI_ChangeCursorTalk, __instance);
                        else
                            Invoke(MI_ChangeCursorDefault, __instance);
                    }
                    else
                    {
                        Invoke(MI_ChangeCursorTalk, __instance);
                    }
                }
                else
                {
                    Invoke(MI_ChangeCursorTalkBlock, __instance);
                }
            }
            else
            {
                Invoke(MI_ChangeCursorDefault, __instance);
            }

            return false;
        }

        private static T GetField<T>(object obj, string name)
        {
            var fi = obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            return fi != null ? (T)fi.GetValue(obj) : default;
        }

        private static T GetFieldValue<T>(object obj, string name)
        {
            var fi = obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            return fi != null ? (T)fi.GetValue(obj) : default;
        }

        private static void Invoke(MethodInfo mi, object instance)
        {
            mi?.Invoke(instance, null);
        }
    }
}
