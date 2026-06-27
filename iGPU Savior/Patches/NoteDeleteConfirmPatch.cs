using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using PotatoOptimization.Core;

namespace PotatoOptimization.Patches
{
  [HarmonyPatch]
  public static class NoteDeleteConfirmPatch
  {
    private static readonly HashSet<ulong> AllowedDeleteIds = new HashSet<ulong>();
    private static readonly object Gate = new object();

    static MethodBase TargetMethod()
    {
      var method = AccessTools.Method(typeof(NoteService), "RemovePage", new[] { typeof(ulong) });
      if (method == null)
        PotatoPlugin.Log?.LogError("[NoteConfirm] Failed to resolve NoteService.RemovePage; patch will not be applied.");
      return method;
    }

    static bool Prefix(NoteService __instance, ulong __0)
    {
      try
      {
        if (PotatoPlugin.Config != null &&
            PotatoPlugin.Config.CfgEnableDeleteConfirm != null &&
            !PotatoPlugin.Config.CfgEnableDeleteConfirm.Value)
        {
          return true;
        }

        if (__instance == null)
          return true;

        var pageUniqueID = __0;

        lock (Gate)
        {
          if (AllowedDeleteIds.Remove(pageUniqueID))
            return true;
        }

        var shown = ExitConfirmationDialogHelper.Show(
          "NoteDeleteConfirm",
          "NOTE_DELETE_CONFIRM_PROMPT",
          () =>
          {
            try
            {
              lock (Gate)
              {
                AllowedDeleteIds.Add(pageUniqueID);
              }

              __instance.RemovePage(pageUniqueID);
            }
            catch (Exception e)
            {
              PotatoPlugin.Log.LogError("[NoteConfirm] Confirm failed: " + e);
            }
          });

        if (!shown)
        {
          PotatoPlugin.Log.LogWarning("[NoteConfirm] Dialog create failed, fallback to original remove pageId=" + pageUniqueID);
          return true;
        }

        return false;
      }
      catch (Exception e)
      {
        PotatoPlugin.Log.LogError("[NoteConfirm] Prefix failed: " + e);
        return true;
      }
    }
  }
}
