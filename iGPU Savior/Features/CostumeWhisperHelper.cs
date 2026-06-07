using System;
using System.Collections;
using Bulbul;
using TMPro;
using UnityEngine;
using PotatoOptimization.Core;
using NestopiSystem.DIContainers;

namespace PotatoOptimization.Features
{
    public static class CostumeWhisperHelper
    {
        public static string[] GetCostumeSkinNames()
        {
            return Enum.GetNames(typeof(CostumeChangeService.CostumeSkinType));
        }

        public static bool IsWhisperPending()
        {
            var config = PotatoPlugin.Config;
            return config != null && !string.IsNullOrEmpty(config.CfgSuggestedCostumeSkin.Value);
        }

        public static string GetPendingSkinName()
        {
            var config = PotatoPlugin.Config;
            return config?.CfgSuggestedCostumeSkin.Value ?? "";
        }

        public static string SkinValueToDisplayKey(string skinValue)
        {
            return "SKIN_" + skinValue;
        }

        public static string DisplayKeyToSkinValue(string displayKey)
        {
            if (displayKey == "WHISPER_NONE") return "";
            if (displayKey.StartsWith("SKIN_")) return displayKey.Substring(5);
            return displayKey;
        }

        public static void ShowToast(string message, float duration = 3f)
        {
            try
            {
                var oldToast = GameObject.Find("WhisperToast");
                if (oldToast != null) UnityEngine.Object.Destroy(oldToast);

                var canvasObj = GameObject.Find("Canvas");
                if (canvasObj == null) return;
                var canvas = canvasObj.GetComponent<Canvas>() ?? canvasObj.GetComponentInParent<Canvas>();
                if (canvas == null) return;

                var go = new GameObject("WhisperToast");
                go.transform.SetParent(canvas.transform, false);
                go.layer = canvas.gameObject.layer;

                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -200f);
                rt.sizeDelta = new Vector2(600f, 80f);

                var tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.text = message;
                tmp.fontSize = 28;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
                tmp.outlineColor = new Color(0f, 0f, 0f, 0.8f);
                tmp.outlineWidth = 0.2f;

                try
                {
                    var fontSupplier = ProjectLifetimeScope.Resolve<FontSupplier>();
                    var langSupplier = ProjectLifetimeScope.Resolve<LanguageSupplier>();
                    if (fontSupplier != null && langSupplier != null)
                    {
                        var lang = langSupplier.Get();
                        tmp.font = fontSupplier.GetFontAsset(lang);
                        var mat = fontSupplier.GetFontMaterial(tmp, lang);
                        if (mat != null) tmp.fontMaterial = mat;
                    }
                }
                catch
                {
                    var existingTmp = canvas.GetComponentInChildren<TMP_Text>(true);
                    if (existingTmp != null)
                    {
                        tmp.font = existingTmp.font;
                        if (existingTmp.fontMaterial != null)
                            tmp.fontMaterial = new Material(existingTmp.fontMaterial);
                    }
                }

                var cg = go.AddComponent<CanvasGroup>();
                cg.alpha = 0f;

                var runner = PotatoOptimization.UI.ModUICoroutineRunner.Instance;
                if (runner != null && runner.gameObject.activeInHierarchy)
                    runner.StartCoroutine(AnimateToast(go, cg, duration));
                else
                    UnityEngine.Object.Destroy(go, duration + 1f);
            }
            catch (Exception e)
            {
                PotatoPlugin.Log.LogWarning($"[WhisperToast] Failed: {e.Message}");
            }
        }

        private static IEnumerator AnimateToast(GameObject go, CanvasGroup cg, float displayDuration)
        {
            if (go == null || cg == null) yield break;

            float t = 0f;
            while (t < 0.3f)
            {
                if (go == null) yield break;
                t += Time.deltaTime;
                cg.alpha = Mathf.Lerp(0f, 1f, t / 0.3f);
                yield return null;
            }
            if (go == null) yield break;
            cg.alpha = 1f;

            yield return new WaitForSeconds(displayDuration);

            if (go == null) yield break;
            t = 0f;
            while (t < 0.5f)
            {
                if (go == null) yield break;
                t += Time.deltaTime;
                cg.alpha = Mathf.Lerp(1f, 0f, t / 0.5f);
                yield return null;
            }

            if (go != null) UnityEngine.Object.Destroy(go);
        }
    }
}
