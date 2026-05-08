using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using PotatoOptimization.Configuration;
using PotatoOptimization.Patches;

namespace PotatoOptimization.Core
{
  /// <summary>
  /// BepInEx 插件入口
  /// </summary>
  [BepInPlugin(Constants.PluginGUID, Constants.PluginName, Constants.PluginVersion)]
  public class PotatoPlugin : BaseUnityPlugin
  {
    // 单例访问
    public static PotatoPlugin Instance { get; private set; }
    public static ManualLogSource Log { get; private set; }
    public static new ConfigurationManager Config { get; private set; }

    private GameObject _runnerObject;

    void Awake()
    {
      // 初始化单例
      Instance = this;
      Log = Logger;

      // 初始化配置管理器
      Config = new ConfigurationManager(base.Config);

      // 应用 Harmony 补丁
      ApplyHarmonyPatches();

      // 创建主控制器
      CreateController();

      Log.LogWarning($">>> {Constants.PluginName} v{Constants.PluginVersion} 启动成功 <<<");
    }

    private void ApplyHarmonyPatches()
    {
      try
      {
        // 强制加载补丁类到内存中，确保它们的静态构造函数被执行
        // 这很重要，因为 Harmony.PatchAll() 只能扫描已加载的类
        var todoDeleteConfirmType = typeof(TodoDeleteConfirmPatch);
        Log.LogWarning($"[Patch Init] Loaded TodoDeleteConfirmPatch: {todoDeleteConfirmType.FullName}");

        var noteDeleteConfirmType = typeof(NoteDeleteConfirmPatch);
        Log.LogWarning($"[Patch Init] Loaded NoteDeleteConfirmPatch: {noteDeleteConfirmType.FullName}");

        var noteExportPatchType = typeof(NoteExportPatch);
        Log.LogWarning($"[Patch Init] Loaded NoteExportPatch: {noteExportPatchType.FullName}");

        var noteExportSelectPagePatchType = typeof(NoteExportSelectPagePatch);
        Log.LogWarning($"[Patch Init] Loaded NoteExportSelectPagePatch: {noteExportSelectPagePatchType.FullName}");

        var noteExportSelectPageGuardPatchType = typeof(NoteExportSelectPageGuardPatch);
        Log.LogWarning($"[Patch Init] Loaded NoteExportSelectPageGuardPatch: {noteExportSelectPageGuardPatchType.FullName}");

        var noteExportEditTitleGuardPatchType = typeof(NoteExportEditTitleGuardPatch);
        Log.LogWarning($"[Patch Init] Loaded NoteExportEditTitleGuardPatch: {noteExportEditTitleGuardPatchType.FullName}");

        var noteExportStartReorderGuardPatchType = typeof(NoteExportStartReorderGuardPatch);
        Log.LogWarning($"[Patch Init] Loaded NoteExportStartReorderGuardPatch: {noteExportStartReorderGuardPatchType.FullName}");

        var noteExportDragReorderGuardPatchType = typeof(NoteExportDragReorderGuardPatch);
        Log.LogWarning($"[Patch Init] Loaded NoteExportDragReorderGuardPatch: {noteExportDragReorderGuardPatchType.FullName}");

        var noteExportEndReorderGuardPatchType = typeof(NoteExportEndReorderGuardPatch);
        Log.LogWarning($"[Patch Init] Loaded NoteExportEndReorderGuardPatch: {noteExportEndReorderGuardPatchType.FullName}");

        var facilityClickMirrorType = typeof(FacilityClickHeroineMirrorPatch);
        Log.LogWarning($"[Patch Init] Loaded FacilityClickHeroineMirrorPatch: {facilityClickMirrorType.FullName}");

        var cursorServiceMirrorType = typeof(CursorServiceMirrorPatch);
        Log.LogWarning($"[Patch Init] Loaded CursorServiceMirrorPatch: {cursorServiceMirrorType.FullName}");
        
        var harmony = new Harmony(Constants.PluginGUID);
        harmony.PatchAll();
        Log.LogWarning(">>> Harmony patches applied successfully! <<<");
      }
      catch (Exception e)
      {
        Log.LogError($"Failed to apply Harmony patches: {e}");
      }
    }

    private void CreateController()
    {
      _runnerObject = new GameObject("PotatoRunner");
      DontDestroyOnLoad(_runnerObject);
      _runnerObject.hideFlags = HideFlags.HideAndDontSave;
      _runnerObject.AddComponent<PotatoController>();
    }
  }
}
