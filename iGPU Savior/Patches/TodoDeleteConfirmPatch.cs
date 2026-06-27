using System;
using System.Reflection;
using HarmonyLib;
using PotatoOptimization.Core;
using Bulbul;

namespace PotatoOptimization.Patches
{
  [HarmonyPatch]
  public static class TodoDeleteConfirmPatch
  {
    static MethodBase TargetMethod()
    {
      var method = AccessTools.Method(typeof(TodoUI), "OnClickButtonRemoveTodo");
      if (method == null)
        PotatoPlugin.Log?.LogError("[TodoConfirm] Failed to resolve TodoUI.OnClickButtonRemoveTodo; patch will not be applied.");
      return method;
    }

    static bool Prefix(TodoUI __instance)
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

        var deleteAction = __instance._onDeleteTodoAction;
        var todoData = __instance._todoData;

        if (deleteAction == null || todoData == null)
        {
          PotatoPlugin.Log.LogWarning("[TodoConfirm] Missing delete action or todo data, falling back.");
          return true;
        }

        var shown = ExitConfirmationDialogHelper.Show(
          "TodoDeleteConfirm",
          "TODO_DELETE_CONFIRM_PROMPT",
          () =>
          {
            try { deleteAction.DynamicInvoke(todoData); }
            catch (Exception e) { PotatoPlugin.Log.LogError("[TodoConfirm] Invoke failed: " + e); }
          },
          () => PotatoPlugin.Log.LogInfo("[TodoConfirm] Cancelled delete."));

        return !shown;
      }
      catch (Exception e)
      {
        PotatoPlugin.Log.LogError("[TodoConfirm] Prefix failed: " + e);
        return true;
      }
    }
  }
}
