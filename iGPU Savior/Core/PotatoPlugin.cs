using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using PotatoOptimization.Configuration;

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
            Log.LogWarning(">>> [V1.7.2] 插件启动：竖屏优化优化版<<<");
        }

        private void ApplyHarmonyPatches()
        {
            try
            {
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
