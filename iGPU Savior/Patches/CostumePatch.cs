using System;
using System.Reflection;
using HarmonyLib;
using PotatoOptimization.Core;
using Bulbul;

namespace PotatoOptimization.Patches
{
    [HarmonyPatch]
    public static class CostumePatch
    {
        private static Type _costumeChangeServiceType;

        static CostumePatch()
        {
            try
            {
                _costumeChangeServiceType = typeof(CostumeChangeService);
                PotatoPlugin.Log.LogWarning($"[CostumePatch] Initialized. Type={_costumeChangeServiceType?.FullName}");
            }
            catch (Exception e)
            {
                PotatoPlugin.Log.LogError("[CostumePatch] Failed to initialize: " + e);
            }
        }

        static MethodBase TargetMethod()
        {
            return _costumeChangeServiceType?.GetMethod("LotterySkin", new[]
            {
                typeof(DateTime),
                typeof(System.Collections.Generic.ICollection<string>)
            });
        }

        [HarmonyPriority(Priority.First)]
        static bool Prefix(CostumeChangeService __instance, DateTime targetDate, ref CostumeChangeService.CostumeSkinType __result)
        {
            try
            {
                var config = PotatoPlugin.Config;
                if (config == null) return true;

                if (config.CfgDisableCostumeRotation.Value)
                {
                    __result = CostumeChangeService.CostumeSkinType.Default_1;
                    PotatoPlugin.Log.LogInfo("[CostumePatch] Rotation disabled, forcing Default_1");
                    return false;
                }

                var suggestion = config.CfgSuggestedCostumeSkin.Value;
                if (!string.IsNullOrEmpty(suggestion))
                {
                    if (Enum.TryParse<CostumeChangeService.CostumeSkinType>(suggestion, out var suggestedSkin))
                    {
                        __result = suggestedSkin;
                        config.CfgSuggestedCostumeSkin.Value = "";
                        config.Save();
                        PotatoPlugin.Log.LogInfo($"[CostumePatch] Whisper active: forced {suggestion}, auto-cleared");
                        return false;
                    }
                    PotatoPlugin.Log.LogWarning($"[CostumePatch] Invalid suggestion \"{suggestion}\", ignoring");
                }

                return true;
            }
            catch (Exception e)
            {
                PotatoPlugin.Log.LogError("[CostumePatch] Prefix failed: " + e);
                return true;
            }
        }
    }
}
