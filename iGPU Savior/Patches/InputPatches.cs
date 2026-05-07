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
        private static FieldInfo FI_heroineLayerMask;
        private static PropertyInfo PI_IsClickedHeroineCurrentFrame;

        static MethodBase TargetMethod()
        {
            return typeof(Bulbul.FacilityClickHeroine).GetMethod("UpdateHeroineClickFlag",
                BindingFlags.Instance | BindingFlags.NonPublic);
        }

        static void Prepare()
        {
            var t = typeof(Bulbul.FacilityClickHeroine);
            FI_heroineLayerMask = t.GetField("_heroineLayerMask", BindingFlags.Instance | BindingFlags.NonPublic);
            PI_IsClickedHeroineCurrentFrame = t.GetProperty("IsClickedHeroineCurrentFrame", BindingFlags.Instance | BindingFlags.Public);
        }

        static bool Prefix(object __instance)
        {
            if (!InputMousePositionPatch.IsInputMirrored)
                return true;

            if (FI_heroineLayerMask == null || PI_IsClickedHeroineCurrentFrame == null)
                return true;

            int heroineLayerMask = (int)FI_heroineLayerMask.GetValue(__instance);
            PI_IsClickedHeroineCurrentFrame.SetValue(__instance, false);

            if (Input.GetMouseButtonDown(0))
            {
                Vector3 mirroredPos = InputMousePositionPatch.GetMirroredMousePosition();
                Ray ray = Camera.main.ScreenPointToRay(mirroredPos);
                bool hit = Physics.Raycast(ray, out _, float.PositiveInfinity, heroineLayerMask);
                PI_IsClickedHeroineCurrentFrame.SetValue(__instance, hit);
            }

            return false;
        }
    }

    /// <summary>
    /// 补丁 CursorService.UpdateCursor
    /// 在镜像模式下使用镜像坐标做光标样式射线检测
    /// 状态机分支复制自 Assembly-CSharp 反编译结果（v1.7.4）
    /// </summary>
    [HarmonyPatch]
    public class CursorServiceMirrorPatch
    {
        private static FieldInfo FI_heroineService;
        private static FieldInfo FI_scenarioReader;
        private static FieldInfo FI_roomGameManager;
        private static FieldInfo FI_heroineLayerMask;
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
            FI_heroineService = t.GetField("_heroineService", BindingFlags.Instance | BindingFlags.NonPublic);
            FI_scenarioReader = t.GetField("_scenarioReader", BindingFlags.Instance | BindingFlags.NonPublic);
            FI_roomGameManager = t.GetField("_roomGameManager", BindingFlags.Instance | BindingFlags.NonPublic);
            FI_heroineLayerMask = t.GetField("_heroineLayerMask", BindingFlags.Instance | BindingFlags.NonPublic);
            MI_ChangeCursorDefault = t.GetMethod("ChangeCursorDefault", BindingFlags.Instance | BindingFlags.NonPublic);
            MI_ChangeCursorTalk = t.GetMethod("ChangeCursorTalk", BindingFlags.Instance | BindingFlags.NonPublic);
            MI_ChangeCursorTalkBlock = t.GetMethod("ChangeCursorTalkBlock", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        static bool Prefix(CursorService __instance)
        {
            if (!InputMousePositionPatch.IsInputMirrored)
                return true;

            if (FI_heroineService == null || FI_scenarioReader == null ||
                FI_roomGameManager == null || FI_heroineLayerMask == null)
                return true;

            if (Bulbul.DevicePlatformExtension.IsMobile(Bulbul.DevicePlatform.Steam))
                return false;

            var heroineService = FI_heroineService.GetValue(__instance) as Bulbul.HeroineService;
            var scenarioReader = FI_scenarioReader.GetValue(__instance) as Bulbul.ScenarioReader;
            var roomGameManager = FI_roomGameManager.GetValue(__instance) as Bulbul.RoomGameManager;
            int heroineLayerMask = (int)FI_heroineLayerMask.GetValue(__instance);

            if (heroineService == null || scenarioReader == null || roomGameManager == null)
                return true;

            if (Bulbul.InputController.Instance.CurrentFrameEventSystemRaycastResult.Count > 0 || Camera.main == null)
            {
                Invoke(MI_ChangeCursorDefault, __instance);
            }
            else if (Physics.Raycast(Camera.main.ScreenPointToRay(InputMousePositionPatch.GetMirroredMousePosition()),
                out _, float.PositiveInfinity, heroineLayerMask))
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

        private static void Invoke(MethodInfo mi, object instance)
        {
            mi?.Invoke(instance, null);
        }
    }
}
