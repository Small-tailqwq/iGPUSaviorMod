using System;
using System.Reflection;
using HarmonyLib;
using PotatoOptimization.Core;
using Bulbul;

namespace PotatoOptimization.Patches
{
  /// <summary>
  /// 待办删除二次确认补丁
  /// 复用 ExitConfirmationDialogHelper 统一弹窗逻辑
  /// </summary>
  [HarmonyPatch]
  public static class TodoDeleteConfirmPatch
  {
    private static readonly Type TodoUIType;
    private static readonly FieldInfo FI_OnDeleteAction;
    private static readonly FieldInfo FI_TodoData;
    private static readonly bool IsReady;

    static TodoDeleteConfirmPatch()
    {
      try
      {
        var assembly = Assembly.Load("Assembly-CSharp");
        TodoUIType = assembly.GetType("TodoUI");

        if (TodoUIType != null)
        {
          FI_OnDeleteAction = TodoUIType.GetField("_onDeleteTodoAction", BindingFlags.Instance | BindingFlags.NonPublic);
          FI_TodoData = TodoUIType.GetField("_todoData", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        IsReady = TodoUIType != null && FI_OnDeleteAction != null && FI_TodoData != null;
        PotatoPlugin.Log.LogInfo($"[TodoConfirm] Initialized. Ready={IsReady}");
      }
      catch (Exception e)
      {
        PotatoPlugin.Log.LogError("[TodoConfirm] Failed to initialize: " + e);
      }
    }

    static MethodBase TargetMethod()
    {
      return TodoUIType?.GetMethod("OnClickButtonRemoveTodo", BindingFlags.Instance | BindingFlags.Public);
    }

    static bool Prefix(object __instance)
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

        // 提取待办数据，供确认回调使用
        var deleteAction = FI_OnDeleteAction.GetValue(__instance) as Delegate;
        var todoData = FI_TodoData.GetValue(__instance);

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
            try
            {
              deleteAction.DynamicInvoke(todoData);
            }
            catch (Exception e)
            {
              PotatoPlugin.Log.LogError("[TodoConfirm] Invoke delete action failed: " + e);
            }
          },
          () =>
          {
            PotatoPlugin.Log.LogInfo("[TodoConfirm] Cancelled delete.");
          });

        return !shown; // shown=true 表示弹窗已接管，阻止原方法
      }
      catch (Exception e)
      {
        PotatoPlugin.Log.LogError("[TodoConfirm] Prefix failed: " + e);
        return true;
      }
    }
  }
}
