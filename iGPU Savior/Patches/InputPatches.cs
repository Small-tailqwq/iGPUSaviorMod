using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Bulbul;
using PotatoOptimization.Core;

namespace PotatoOptimization.Patches
{
  public static class InputMousePositionPatch
  {
    public static bool IsInputMirrored { get; set; } = false;

    public static Vector3 GetMirroredMousePosition()
    {
      var pos = Input.mousePosition;
      return new Vector3(Screen.width - pos.x, pos.y, pos.z);
    }
  }

  [HarmonyPatch]
  public class FacilityClickHeroineMirrorPatch
  {
    static MethodBase TargetMethod()
    {
      var method = AccessTools.Method(typeof(FacilityClickHeroine), "UpdateHeroineClickFlag");
      if (method == null)
        PotatoPlugin.Log?.LogError("[FacilityClickHeroineMirrorPatch] Failed to resolve UpdateHeroineClickFlag; patch will not be applied.");
      return method;
    }

    static bool Prefix(FacilityClickHeroine __instance)
    {
      if (!InputMousePositionPatch.IsInputMirrored)
        return true;

      __instance.IsClickedHeroineCurrentFrame = false;

      if (Input.GetMouseButtonDown(0) && Camera.main != null)
      {
        var ray = Camera.main.ScreenPointToRay(InputMousePositionPatch.GetMirroredMousePosition());
        bool hit = Physics.Raycast(ray, out _, float.PositiveInfinity, __instance._heroineLayerMask);
        __instance.IsClickedHeroineCurrentFrame = hit;
      }

      return false;
    }
  }

  [HarmonyPatch]
  public class CursorServiceMirrorPatch
  {
    static MethodBase TargetMethod()
    {
      var method = AccessTools.Method(typeof(CursorService), "UpdateCursor");
      if (method == null)
        PotatoPlugin.Log?.LogError("[CursorServiceMirrorPatch] Failed to resolve CursorService.UpdateCursor; patch will not be applied.");
      return method;
    }

    static bool Prefix(CursorService __instance)
    {
      if (!InputMousePositionPatch.IsInputMirrored)
        return true;

      if (DevicePlatformExtension.IsMobile(DevicePlatform.Steam))
        return false;

      var heroineService = __instance._heroineService;
      var scenarioReader = __instance._scenarioReader;
      var roomGameManager = __instance._roomGameManager;
      int heroineLayerMask = __instance._heroineLayerMask;

      if (heroineService == null || scenarioReader == null || roomGameManager == null)
        return true;

      if (InputController.Instance.CurrentFrameEventSystemRaycastResult.Count > 0 || Camera.main == null)
      {
        __instance.ChangeCursorDefault();
      }
      else if (Physics.Raycast(Camera.main.ScreenPointToRay(InputMousePositionPatch.GetMirroredMousePosition()),
          out _, float.PositiveInfinity, heroineLayerMask))
      {
        if (heroineService.IsPossibleClickHeroineReaction())
        {
          if (scenarioReader.IsPlayingScenario() ||
              roomGameManager.CurrentMainState == RoomGameManager.MainState.TalkingGameStartDirection ||
              roomGameManager.CurrentMainState == RoomGameManager.MainState.EndGameStartDirection ||
              roomGameManager.CurrentMainState == RoomGameManager.MainState.Tutorial0 ||
              roomGameManager.CurrentMainState == RoomGameManager.MainState.Tutorial1)
          {
            if (roomGameManager.CurrentMainState == RoomGameManager.MainState.Tutorial4)
              __instance.ChangeCursorTalk();
            else
              __instance.ChangeCursorDefault();
          }
          else
          {
            __instance.ChangeCursorTalk();
          }
        }
        else
        {
          __instance.ChangeCursorTalkBlock();
        }
      }
      else
      {
        __instance.ChangeCursorDefault();
      }

      return false;
    }
  }
}
