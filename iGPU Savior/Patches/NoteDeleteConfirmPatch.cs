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
    private static readonly Type NoteServiceType;
    private static readonly MethodInfo MI_RemovePage;
    private static readonly bool IsReady;
    private static readonly HashSet<ulong> AllowedDeleteIds = new HashSet<ulong>();
    private static readonly object Gate = new object();

    static NoteDeleteConfirmPatch()
    {
      try
      {
        PotatoPlugin.Log.LogWarning("[NoteConfirm] Static constructor starting...");
        var assembly = Assembly.Load("Assembly-CSharp");
        NoteServiceType = assembly.GetType("NoteService");
        if (NoteServiceType != null)
        {
          MI_RemovePage = NoteServiceType.GetMethod(
            "RemovePage",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(ulong) },
            null);
        }

        IsReady = NoteServiceType != null && MI_RemovePage != null;
        PotatoPlugin.Log.LogWarning("[NoteConfirm] Initialized. Ready=" + IsReady);
        PotatoPlugin.Log.LogWarning("[NoteConfirm] Type=" + (NoteServiceType != null ? NoteServiceType.FullName : "null"));
        PotatoPlugin.Log.LogWarning("[NoteConfirm] RemovePage=" + (MI_RemovePage != null));
      }
      catch (Exception e)
      {
        PotatoPlugin.Log.LogError("[NoteConfirm] Failed to initialize: " + e);
      }
    }

    static MethodBase TargetMethod()
    {
      PotatoPlugin.Log.LogWarning("[NoteConfirm] TargetMethod called. Found=" + (MI_RemovePage != null));
      return MI_RemovePage;
    }

    static bool Prefix(object __instance, ulong __0)
    {
      try
      {
        if (PotatoPlugin.Config != null &&
            PotatoPlugin.Config.CfgEnableDeleteConfirm != null &&
            !PotatoPlugin.Config.CfgEnableDeleteConfirm.Value)
        {
          return true;
        }

        if (!IsReady || __instance == null)
        {
          return true;
        }

        var pageUniqueID = __0;

        lock (Gate)
        {
          if (AllowedDeleteIds.Remove(pageUniqueID))
          {
            PotatoPlugin.Log.LogWarning("[NoteConfirm] Allow original remove for pageId=" + pageUniqueID);
            return true;
          }
        }

        PotatoPlugin.Log.LogWarning("[NoteConfirm] Intercept remove pageId=" + pageUniqueID);
        var shown = ExitConfirmationDialogHelper.Show(
          "NoteDeleteConfirm",
          "NOTE_DELETE_CONFIRM_PROMPT",
          () =>
          {
            try
            {
              PotatoPlugin.Log.LogWarning("[NoteConfirm] Confirmed delete pageId=" + pageUniqueID);
              lock (Gate)
              {
                AllowedDeleteIds.Add(pageUniqueID);
              }

              MI_RemovePage.Invoke(__instance, new object[] { pageUniqueID });
            }
            catch (Exception e)
            {
              PotatoPlugin.Log.LogError("[NoteConfirm] Confirm invoke failed: " + e);
            }
          },
          () =>
          {
            PotatoPlugin.Log.LogWarning("[NoteConfirm] Cancelled delete pageId=" + pageUniqueID);
          });

        if (!shown)
        {
          PotatoPlugin.Log.LogWarning("[NoteConfirm] Dialog create failed, fallback original remove pageId=" + pageUniqueID);
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
