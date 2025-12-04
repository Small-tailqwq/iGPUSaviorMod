using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Bulbul;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ModShared;
using BepInEx.Configuration;
using PotatoOptimization.Core;

namespace PotatoOptimization.UI
{
    [HarmonyPatch(typeof(SettingUI), "Setup")]
    public class ModSettingsIntegration
    {
        private static GameObject modContentParent;
        private static InteractableUI modInteractableUI;
        private static SettingUI cachedSettingUI;
        private static Canvas _rootCanvas;
        private static List<GameObject> modDropdowns = new List<GameObject>();

        static void Postfix(SettingUI __instance)
        {
            try
            {
                cachedSettingUI = __instance;
                _rootCanvas = __instance.GetComponentInParent<Canvas>() ?? Object.FindObjectOfType<Canvas>();

                CreateModSettingsTab(__instance);
                HookIntoTabButtons(__instance);
                
                modContentParent?.SetActive(false);
            }
            catch (System.Exception e)
            {
                PotatoPlugin.Log.LogError($"MOD integration failed: {e.Message}\n{e.StackTrace}");
            }
        }

        static void CreateModSettingsTab(SettingUI settingUI)
        {
            try
            {
                var creditsButton = AccessTools.Field(typeof(SettingUI), "_creditsInteractableUI").GetValue(settingUI) as InteractableUI;
                var creditsParent = AccessTools.Field(typeof(SettingUI), "_creditsParent").GetValue(settingUI) as GameObject;
                if (creditsButton == null || creditsParent == null) return;

                GameObject modTabButton = Object.Instantiate(creditsButton.gameObject);
                modTabButton.name = "ModSettingsTabButton";
                modTabButton.transform.SetParent(creditsButton.transform.parent, false);
                modTabButton.transform.SetSiblingIndex(creditsButton.transform.GetSiblingIndex() + 1);

                modContentParent = Object.Instantiate(creditsParent);
                modContentParent.name = "ModSettingsContent";
                modContentParent.transform.SetParent(creditsParent.transform.parent, false);
                modContentParent.SetActive(false);

                var scrollRect = modContentParent.GetComponentInChildren<ScrollRect>();
                if (scrollRect == null) return;

                var content = scrollRect.content;
                foreach (Transform child in content) Object.Destroy(child.gameObject);
                
                ConfigureContentLayout(content.gameObject);

                ModSettingsManager manager = ModSettingsManager.Instance;
                if (manager == null)
                {
                    GameObject managerObj = new GameObject("ModSettingsManager");
                    Object.DontDestroyOnLoad(managerObj);
                    manager = managerObj.AddComponent<ModSettingsManager>();
                }

                ModUICoroutineRunner.Instance.RunDelayed(0.3f, () =>
                {
                    UpdateModButtonText(modTabButton);
                    UpdateModContentText(modContentParent);
                    AdjustTabBarLayout(modTabButton.transform.parent);
                });

                modInteractableUI = modTabButton.GetComponent<InteractableUI>();
                modInteractableUI?.Setup();
                modTabButton.GetComponent<Button>()?.onClick.AddListener(() => SwitchToModTab(settingUI));

                RegisterCurrentMod(manager);
            }
            catch (System.Exception e)
            {
                PotatoPlugin.Log.LogError($"CreateModSettingsTab failed: {e.Message}");
            }
        }

        static void ConfigureContentLayout(GameObject content)
        {
            var rect = content.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(1, 1);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(0, 0);
                rect.localScale = Vector3.one;
            }

            var vGroup = content.GetComponent<VerticalLayoutGroup>() ?? content.AddComponent<VerticalLayoutGroup>();
            vGroup.spacing = 16f;
            vGroup.padding = new RectOffset(60, 40, 20, 20);
            vGroup.childAlignment = TextAnchor.UpperLeft;
            vGroup.childControlHeight = false;
            vGroup.childControlWidth = true;
            vGroup.childForceExpandHeight = false;
            vGroup.childForceExpandWidth = true;

            var fitter = content.GetComponent<ContentSizeFitter>() ?? content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        }

        static void RegisterCurrentMod(ModSettingsManager manager)
        {
            ModUICoroutineRunner.Instance.RunDelayed(0.5f, () =>
            {
                if (modContentParent == null || cachedSettingUI == null) return;
                
                manager.RegisterMod("iGPU Savior", PotatoOptimization.Core.Constants.PluginVersion);

                manager.AddToggle("Enable Mirror", PotatoPlugin.Config.CfgEnableMirror.Value, val => {
                    PotatoPlugin.Config.CfgEnableMirror.Value = val;
                    Object.FindObjectOfType<PotatoController>()?.SetMirrorState(val);
                });

                manager.AddDropdown("Window Scale", new List<string> { "1/3 Size", "1/4 Size", "1/5 Size" },
                    (int)PotatoPlugin.Config.CfgWindowScale.Value - 3,
                    index => PotatoPlugin.Config.CfgWindowScale.Value = (WindowScaleRatio)(index + 3));

                manager.AddDropdown("Window Drag Mode", new List<string> { "Ctrl + Left Click", "Alt + Left Click", "Right Click Hold" },
                    (int)PotatoPlugin.Config.CfgDragMode.Value,
                    index => PotatoPlugin.Config.CfgDragMode.Value = (DragMode)index);

                var keyOptions = new List<string> { "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12" };
                int GetKeyIndex(KeyCode key) { int i = key - KeyCode.F1; return (i >= 0 && i < 12) ? i : 0; }
                KeyCode GetKey(int i) { return KeyCode.F1 + i; }

                manager.AddDropdown("Potato Mode Hotkey", keyOptions, GetKeyIndex(PotatoPlugin.Config.KeyPotatoMode.Value), 
                    i => PotatoPlugin.Config.KeyPotatoMode.Value = GetKey(i));
                manager.AddDropdown("PiP Mode Hotkey", keyOptions, GetKeyIndex(PotatoPlugin.Config.KeyPiPMode.Value), 
                    i => PotatoPlugin.Config.KeyPiPMode.Value = GetKey(i));
                manager.AddDropdown("Camera Mirror Hotkey", keyOptions, GetKeyIndex(PotatoPlugin.Config.KeyCameraMirror.Value), 
                    i => PotatoPlugin.Config.KeyCameraMirror.Value = GetKey(i));

                var scrollRect = modContentParent.GetComponentInChildren<ScrollRect>();
                if (scrollRect != null)
                {
                    manager.RebuildUI(scrollRect.content, cachedSettingUI.transform);
                }
            });
        }

        static Transform GetGraphicsContentTransform()
        {
            return cachedSettingUI != null ? cachedSettingUI.transform.Find("Graphics/ScrollView/Viewport/Content") : null;
        }

        static void UpdateModButtonText(GameObject modTabButton)
        {
            var allTexts = modTabButton.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in allTexts) text.text = "MOD";
        }

        static void UpdateModContentText(GameObject modContentParent)
        {
            var titleTransform = modContentParent.transform.Find("Title");
            if (titleTransform != null)
            {
                var t = titleTransform.GetComponent<TextMeshProUGUI>();
                if (t != null) t.text = "MOD";
            }
            var allTexts = modContentParent.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in allTexts)
            {
                if (text.text.Contains("Credits")) text.text = "MOD Settings";
            }
        }

        static void AdjustTabBarLayout(Transform tabBarParent)
        {
            var rectTransform = tabBarParent.GetComponent<RectTransform>();
            if (rectTransform != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }

        private static void HookIntoTabButtons(SettingUI settingUI)
        {
            var buttons = new[] { "_generalInteractableUI", "_graphicInteractableUI", "_audioInteractableUI", "_creditsInteractableUI" };
            var parents = new[] { "_generalParent", "_graphicParent", "_audioParent", "_creditsParent" };
            for (int i = 0; i < buttons.Length; i++)
            {
                var btn = AccessTools.Field(typeof(SettingUI), buttons[i]).GetValue(settingUI) as InteractableUI;
                var parent = AccessTools.Field(typeof(SettingUI), parents[i]).GetValue(settingUI) as GameObject;
                if (btn != null)
                {
                    var capturedBtn = btn;
                    var capturedParent = parent;
                    btn.GetComponent<Button>()?.onClick.AddListener(() =>
                    {
                        modContentParent?.SetActive(false);
                        modInteractableUI?.DeactivateUseUI(false);
                        if (capturedParent) { capturedParent.SetActive(true); capturedBtn.ActivateUseUI(false); }
                    });
                }
            }
        }

        private static void SwitchToModTab(SettingUI settingUI)
        {
            var parents = new[] { "_generalParent", "_graphicParent", "_audioParent", "_creditsParent" };
            foreach (var p in parents)
                (AccessTools.Field(typeof(SettingUI), p).GetValue(settingUI) as GameObject)?.SetActive(false);

            var buttons = new[] { "_generalInteractableUI", "_graphicInteractableUI", "_audioInteractableUI", "_creditsInteractableUI" };
            foreach (var b in buttons)
                (AccessTools.Field(typeof(SettingUI), b).GetValue(settingUI) as InteractableUI)?.DeactivateUseUI(false);

            OnOpenModTab();
            modInteractableUI?.ActivateUseUI(false);
            modContentParent?.SetActive(true);

            var scrollRect = modContentParent?.GetComponentInChildren<ScrollRect>();
            if (scrollRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(modContentParent.GetComponent<RectTransform>());
                scrollRect.verticalNormalizedPosition = 1f;
            }
        }

        private static void OnOpenModTab()
        {
            PlayClickSound();
            foreach (var dropdown in modDropdowns)
            {
                if (dropdown == null) continue;
                var pulldownListUI = dropdown.GetComponent("PulldownListUI") ?? 
                    dropdown.GetComponentInChildren(System.Type.GetType("Bulbul.PulldownListUI, Assembly-CSharp"));
                pulldownListUI?.GetType().GetMethod("ClosePullDown")?.Invoke(pulldownListUI, new object[] { true });
            }
        }

        private static void PlayClickSound()
        {
            if (cachedSettingUI == null) return;
            var sss = AccessTools.Field(typeof(SettingUI), "_systemSeService").GetValue(cachedSettingUI);
            sss?.GetType().GetMethod("PlayClick")?.Invoke(sss, null);
        }
    }

    public class ModUICoroutineRunner : MonoBehaviour
    {
        private static ModUICoroutineRunner _instance;

        public static ModUICoroutineRunner Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("ModUI_CoroutineRunner");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<ModUICoroutineRunner>();
                }
                return _instance;
            }
        }

        public void RunDelayed(float seconds, System.Action action)
        {
            StartCoroutine(DelayedAction(seconds, action));
        }

        private IEnumerator DelayedAction(float seconds, System.Action action)
        {
            yield return new WaitForSeconds(seconds);
            action?.Invoke();
        }
    }

    [HarmonyPatch(typeof(SettingUI), "Activate")]
    public class ModSettingsActivateHandler
    {
        static void Postfix(SettingUI __instance)
        {
            try
            {
                var modContentParent = AccessTools.Field(typeof(ModSettingsIntegration), "modContentParent").GetValue(null) as GameObject;
                var modInteractableUI = AccessTools.Field(typeof(ModSettingsIntegration), "modInteractableUI").GetValue(null) as InteractableUI;
                modContentParent?.SetActive(false);
                modInteractableUI?.DeactivateUseUI(false);

                var generalButton = AccessTools.Field(typeof(SettingUI), "_generalInteractableUI").GetValue(__instance) as InteractableUI;
                var generalParent = AccessTools.Field(typeof(SettingUI), "_generalParent").GetValue(__instance) as GameObject;
                generalButton?.ActivateUseUI(false);
                generalParent?.SetActive(true);

                var others = new[] { "_graphicParent", "_audioParent", "_creditsParent" };
                foreach (var o in others)
                    (AccessTools.Field(typeof(SettingUI), o).GetValue(__instance) as GameObject)?.SetActive(false);
            }
            catch { }
        }
    }
}
