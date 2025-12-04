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

        private static TMP_FontAsset _cachedFont;
        private static Sprite _cachedRoundedSprite;
        private static Canvas _rootCanvas;
        
        // Store all MOD dropdowns for closing when switching tabs
        private static List<GameObject> modDropdowns = new List<GameObject>();

        static void Postfix(SettingUI __instance)
        {
            try
            {
                cachedSettingUI = __instance;
                _rootCanvas = __instance.GetComponentInParent<Canvas>() ?? Object.FindObjectOfType<Canvas>();

                if (ModSettingsManager.Instance != null && ModSettingsManager.Instance.IsInitialized)
                {
                    RegisterModToExistingTab();
                }
                else
                {
                    CreateModSettingsTab(__instance);
                }

                HookIntoTabButtons(__instance);

                // Ensure MOD content hidden by default
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
                if (creditsButton == null || creditsParent == null)
                {
                    PotatoPlugin.Log.LogError("Cannot find Credits button or panel");
                    return;
                }

                GameObject modTabButton = Object.Instantiate(creditsButton.gameObject);
                modTabButton.name = "ModSettingsTabButton";
                modTabButton.transform.SetParent(creditsButton.transform.parent, false);
                modTabButton.transform.SetSiblingIndex(creditsButton.transform.GetSiblingIndex() + 1);

                modContentParent = Object.Instantiate(creditsParent);
                modContentParent.name = "ModSettingsContent";
                modContentParent.transform.SetParent(creditsParent.transform.parent, false);
                modContentParent.SetActive(false);

                var scrollRect = modContentParent.GetComponentInChildren<ScrollRect>();
                if (scrollRect == null)
                {
                    PotatoPlugin.Log.LogError("ScrollRect not found in mod content");
                    return;
                }

                // Copy layout from Graphics content to MOD content for consistent left/right alignment
                Transform graphicsContent = GetGraphicsContentTransform();
                var content = scrollRect.content;
                if (graphicsContent != null)
                {
                    CopyLayoutFromGraphics(graphicsContent, content);
                }

                foreach (Transform child in content) Object.Destroy(child.gameObject);
                ConfigureContentLayout(content.gameObject);

                GameObject managerObj = new GameObject("ModSettingsManager");
                Object.DontDestroyOnLoad(managerObj);
                var manager = managerObj.AddComponent<ModSettingsManager>();
                manager.Initialize(modTabButton, content.gameObject, scrollRect);

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
                PotatoPlugin.Log.LogError($"CreateModSettingsTab failed: {e.Message}\n{e.StackTrace}");
            }
        }

        static void ConfigureContentLayout(GameObject content)
        {
            var vGroup = content.GetComponent<VerticalLayoutGroup>() ?? content.AddComponent<VerticalLayoutGroup>();
            vGroup.spacing = 16f;
            vGroup.padding = new RectOffset(32, 32, 24, 24);
            vGroup.childControlHeight = false;
            vGroup.childControlWidth = true;
            vGroup.childForceExpandHeight = false;
            vGroup.childForceExpandWidth = true;

            var fitter = content.GetComponent<ContentSizeFitter>() ?? content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        static void PrepareUIResources()
        {
            if (_cachedFont != null && _cachedRoundedSprite != null) return;
            try
            {
                var anyText = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>().FirstOrDefault(t => t.font != null);
                if (anyText != null) _cachedFont = anyText.font;

                _cachedRoundedSprite = Resources.FindObjectsOfTypeAll<Sprite>()
                    .FirstOrDefault(s => s != null && (s.name.Contains("UISprite") || s.name.Contains("Background")));

                if (_cachedRoundedSprite == null)
                {
                    var anyImage = Resources.FindObjectsOfTypeAll<Image>().FirstOrDefault(i => i.sprite != null);
                    if (anyImage != null) _cachedRoundedSprite = anyImage.sprite;
                }
            }
            catch (System.Exception e)
            {
                PotatoPlugin.Log.LogWarning($"PrepareUIResources failed: {e.Message}");
            }
        }

        static void RegisterModToExistingTab()
        {
            if (ModSettingsManager.Instance == null)
            {
                PotatoPlugin.Log.LogWarning("ModSettingsManager.Instance is null");
                return;
            }
            RegisterCurrentMod(ModSettingsManager.Instance);
        }

        static void RegisterCurrentMod(ModSettingsManager manager)
        {
            ModUICoroutineRunner.Instance.RunDelayed(0.5f, () =>
            {
                if (modContentParent == null)
                {
                    PotatoPlugin.Log.LogWarning("modContentParent is null");
                    return;
                }

                var scrollRect = modContentParent.GetComponentInChildren<ScrollRect>();
                if (scrollRect == null)
                {
                    PotatoPlugin.Log.LogWarning("ScrollRect not found");
                    return;
                }

                Transform graphicsContent = GetGraphicsContentTransform();
                var content = scrollRect.content;
                if (graphicsContent != null)
                {
                    CopyLayoutFromGraphics(graphicsContent, content);
                    
                    // ✅ 确保 Content 的 RectTransform 配置正确
                    var srcRect = graphicsContent as RectTransform ?? graphicsContent.GetComponent<RectTransform>();
                    var tgtRect = content as RectTransform ?? content.GetComponent<RectTransform>();
                    
                    if (srcRect != null && tgtRect != null)
                    {
                        // 只复制 anchor，不复制 position！
                        tgtRect.anchorMin = srcRect.anchorMin;
                        tgtRect.anchorMax = srcRect.anchorMax;
                        tgtRect.pivot = srcRect.pivot;
                        // ❌ 不要复制 anchoredPosition，它会把内容推到错误位置
                        // tgtRect.anchoredPosition = srcRect.anchoredPosition;
                        // ✅ 强制重置为 (0, 0)
                        tgtRect.anchoredPosition = Vector2.zero;
                        tgtRect.sizeDelta = new Vector2(0, 0); // 让 ContentSizeFitter 自动计算高度
                        
                        PotatoPlugin.Log.LogInfo($"✅ Content RectTransform 已配置: anchorMin={tgtRect.anchorMin}, anchoredPosition={tgtRect.anchoredPosition}");
                    }
                }

                // ✅ 检查是否已经有外部 MOD 的设置（通过查找特定的 Row_ 前缀）
                // 如果有，就不清空；如果没有，说明是第一次加载 iGPU Savior 的设置
                bool hasExternalModSettings = false;
                foreach (Transform child in content)
                {
                    if (child.name.StartsWith("Row_"))
                    {
                        hasExternalModSettings = true;
                        break;
                    }
                }

                // 只清空不是外部 MOD 的设置（即 iGPU Savior 的下拉菜单）
                if (!hasExternalModSettings)
                {
                    // 第一次加载，清空所有内容
                    foreach (Transform child in content)
                        Object.Destroy(child.gameObject);
                    PotatoPlugin.Log.LogInfo("First load: cleared all content");
                }
                else
                {
                    // 不是第一次加载，不要清空外部 MOD 的设置
                    PotatoPlugin.Log.LogInfo("External MOD settings detected, preserving them");
                }

                PrepareUIResources();

                // CreateSectionHeader(content, "Basic Settings"); // Removed - causes text clipping outside viewport

                // ========== 使用新的 ModToggleCloner 创建开关 ==========
                GameObject mirrorToggle = ModToggleCloner.CreateToggle(
                    cachedSettingUI.transform,
                    "Enable Mirror", // 标题
                    PotatoPlugin.Config.CfgEnableMirror.Value, // 初始值
                    val =>
                    {
                        PotatoPlugin.Config.CfgEnableMirror.Value = val;
                        // 实时应用设置
                        var controller = Object.FindObjectOfType<PotatoController>();
                        controller?.SetMirrorState(val);
                        PotatoPlugin.Log.LogInfo($"Mirror toggled: {val}");
                    }
                );
                
                if (mirrorToggle != null)
                {
                    mirrorToggle.transform.SetParent(content, false);
                    mirrorToggle.SetActive(true);
                    PotatoPlugin.Log.LogInfo($"✅ Mirror Toggle added to content. Parent: {mirrorToggle.transform.parent.name}");
                }
                else
                {
                    PotatoPlugin.Log.LogError("❌ Failed to create Mirror Toggle");
                }
                // ===================================================

                // ✅ 直接添加到 content 容器，不再使用 iGPUSaviorSection
                CreateNativeDropdown(content, "Window Scale",
                    new List<string> { "1/3 Size", "1/4 Size", "1/5 Size" },
                    (int)PotatoPlugin.Config.CfgWindowScale.Value - 3,
                    index =>
                    {
                        PotatoPlugin.Config.CfgWindowScale.Value = (WindowScaleRatio)(index + 3);
                    });

                CreateNativeDropdown(content, "Window Drag Mode",
                    new List<string> { "Ctrl + Left Click", "Alt + Left Click", "Right Click Hold" },
                    (int)PotatoPlugin.Config.CfgDragMode.Value,
                    index =>
                    {
                        PotatoPlugin.Config.CfgDragMode.Value = (DragMode)index;
                    });

                // CreateSectionHeader(content, "Hotkeys"); // Removed - causes text clipping outside viewport
                var keyOptions = GetFunctionKeyOptions();

                CreateNativeDropdown(content, "Potato Mode Hotkey",
                    keyOptions, GetKeyCodeIndex(PotatoPlugin.Config.KeyPotatoMode.Value),
                    index =>
                    {
                        PotatoPlugin.Config.KeyPotatoMode.Value = GetKeyCodeFromIndex(index);
                    });

                CreateNativeDropdown(content, "PiP Mode Hotkey",
                    keyOptions, GetKeyCodeIndex(PotatoPlugin.Config.KeyPiPMode.Value),
                    index =>
                    {
                        PotatoPlugin.Config.KeyPiPMode.Value = GetKeyCodeFromIndex(index);
                    });

                CreateNativeDropdown(content, "Camera Mirror Hotkey",
                    keyOptions, GetKeyCodeIndex(PotatoPlugin.Config.KeyCameraMirror.Value),
                    index =>
                    {
                        PotatoPlugin.Config.KeyCameraMirror.Value = GetKeyCodeFromIndex(index);
                    });

                var contentRect = content as RectTransform ?? content.GetComponent<RectTransform>();
                if (contentRect != null) LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
                
                // 🔍 调试：输出 Content 的子对象数量
                int childCount = content.childCount;
                PotatoPlugin.Log.LogInfo($"🔍 Content has {childCount} children after adding all settings:");
                for (int i = 0; i < childCount; i++)
                {
                    var child = content.GetChild(i);
                    PotatoPlugin.Log.LogInfo($"   [{i}] {child.name} (active: {child.gameObject.activeSelf})");
                }
                
                PotatoPlugin.Log.LogInfo("✅ iGPU Savior 设置已注册，外部 MOD 设置得以保留");
            });
        }

        private static Transform GetGraphicsContentTransform()
        {
            if (cachedSettingUI == null) return null;
            var t = cachedSettingUI.transform.Find("Graphics/ScrollView/Viewport/Content");
            return t;
        }

        private static void CopyLayoutFromGraphics(Transform source, Transform target)
        {
            try
            {
                if (source == null || target == null) return;

                var srcRect = source as RectTransform ?? source.GetComponent<RectTransform>();
                var tgtRect = target as RectTransform ?? target.GetComponent<RectTransform>();
                if (srcRect != null && tgtRect != null)
                {
                    tgtRect.anchorMin = srcRect.anchorMin;
                    tgtRect.anchorMax = srcRect.anchorMax;
                    tgtRect.pivot = srcRect.pivot;
                    // ❌ 不要复制 offsetMin/offsetMax 和 anchoredPosition
                    // 这些会导致内容被推到错误位置
                    // tgtRect.offsetMin = srcRect.offsetMin;
                    // tgtRect.offsetMax = srcRect.offsetMax;
                    // tgtRect.anchoredPosition = srcRect.anchoredPosition;
                    // tgtRect.sizeDelta = srcRect.sizeDelta;
                    
                    // ✅ 强制重置位置为 (0, 0)，让 LayoutGroup 和 ContentSizeFitter 处理
                    tgtRect.anchoredPosition = Vector2.zero;
                    tgtRect.sizeDelta = Vector2.zero;
                }

                var srcVlg = source.GetComponent<VerticalLayoutGroup>();
                var tgtVlg = target.GetComponent<VerticalLayoutGroup>() ?? target.gameObject.AddComponent<VerticalLayoutGroup>();
                if (srcVlg != null && tgtVlg != null)
                {
                    tgtVlg.padding = srcVlg.padding;
                    tgtVlg.spacing = srcVlg.spacing;
                    tgtVlg.childAlignment = srcVlg.childAlignment;
                    tgtVlg.childControlHeight = srcVlg.childControlHeight;
                    tgtVlg.childControlWidth = srcVlg.childControlWidth;
                    tgtVlg.childForceExpandHeight = srcVlg.childForceExpandHeight;
                    tgtVlg.childForceExpandWidth = srcVlg.childForceExpandWidth;
                }

                var srcFitter = source.GetComponent<ContentSizeFitter>();
                var tgtFitter = target.GetComponent<ContentSizeFitter>() ?? target.gameObject.AddComponent<ContentSizeFitter>();
                if (srcFitter != null && tgtFitter != null)
                {
                    tgtFitter.horizontalFit = srcFitter.horizontalFit;
                    tgtFitter.verticalFit = srcFitter.verticalFit;
                }

                var srcLe = source.GetComponent<LayoutElement>();
                var tgtLe = target.GetComponent<LayoutElement>() ?? target.gameObject.AddComponent<LayoutElement>();
                if (srcLe != null && tgtLe != null)
                {
                    tgtLe.minHeight = srcLe.minHeight;
                    tgtLe.preferredHeight = srcLe.preferredHeight;
                    tgtLe.minWidth = srcLe.minWidth;
                    tgtLe.preferredWidth = srcLe.preferredWidth;
                    tgtLe.flexibleHeight = srcLe.flexibleHeight;
                    tgtLe.flexibleWidth = srcLe.flexibleWidth;
                }
            }
            catch (System.Exception e)
            {
                PotatoPlugin.Log.LogWarning($"CopyLayoutFromGraphics failed: {e.Message}");
            }
        }

        // Tab/button helpers
        private static void HookIntoTabButtons(SettingUI settingUI)
        {
            try
            {
                var generalButton = AccessTools.Field(typeof(SettingUI), "_generalInteractableUI").GetValue(settingUI) as InteractableUI;
                var graphicButton = AccessTools.Field(typeof(SettingUI), "_graphicInteractableUI").GetValue(settingUI) as InteractableUI;
                var audioButton = AccessTools.Field(typeof(SettingUI), "_audioInteractableUI").GetValue(settingUI) as InteractableUI;
                var creditsButton = AccessTools.Field(typeof(SettingUI), "_creditsInteractableUI").GetValue(settingUI) as InteractableUI;

                var generalParent = AccessTools.Field(typeof(SettingUI), "_generalParent").GetValue(settingUI) as GameObject;
                var graphicParent = AccessTools.Field(typeof(SettingUI), "_graphicParent").GetValue(settingUI) as GameObject;
                var audioParent = AccessTools.Field(typeof(SettingUI), "_audioParent").GetValue(settingUI) as GameObject;
                var creditsParent = AccessTools.Field(typeof(SettingUI), "_creditsParent").GetValue(settingUI) as GameObject;

                AddHideModTabListener(generalButton, generalParent);
                AddHideModTabListener(graphicButton, graphicParent);
                AddHideModTabListener(audioButton, audioParent);
                AddHideModTabListener(creditsButton, creditsParent);
            }
            catch (System.Exception e)
            {
                PotatoPlugin.Log.LogWarning($"HookIntoTabButtons failed: {e.Message}");
            }
        }

        private static void AddHideModTabListener(InteractableUI button, GameObject targetParent)
        {
            if (button == null) return;
            var uiButton = button.GetComponent<Button>();
            if (uiButton != null)
            {
                uiButton.onClick.AddListener(() =>
                {
                    if (modContentParent != null)
                    {
                        modContentParent.SetActive(false);
                        modInteractableUI?.DeactivateUseUI(false);
                    }
                    if (targetParent != null)
                    {
                        targetParent.SetActive(true);
                        button.ActivateUseUI(false);
                    }
                });
            }
        }

        private static void UpdateModButtonText(GameObject modTabButton)
        {
            try
            {
                var allTexts = modTabButton.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var text in allTexts) text.text = "MOD";
            }
            catch (System.Exception e)
            {
                PotatoPlugin.Log.LogWarning($"UpdateModButtonText failed: {e.Message}");
            }
        }

        private static void UpdateModContentText(GameObject modContentParent)
        {
            try
            {
                var titleText = modContentParent.GetComponent<TextMeshProUGUI>();
                if (titleText != null) titleText.text = "MOD Settings";

                // ✅ 关键修复：找到 Title 子物体并修改其文本
                Transform titleTransform = modContentParent.transform.Find("Title");
                if (titleTransform != null)
                {
                    var titleTextComponent = titleTransform.GetComponent<TextMeshProUGUI>();
                    if (titleTextComponent != null)
                    {
                        titleTextComponent.text = "MOD";
                    }
                }

                var allTexts = modContentParent.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var text in allTexts)
                {
                    if (text.text.Contains("Credits"))
                    {
                        text.text = "MOD Settings";
                    }
                }
            }
            catch (System.Exception e)
            {
                PotatoPlugin.Log.LogWarning($"UpdateModContentText failed: {e.Message}");
            }
        }

        private static void SwitchToModTab(SettingUI settingUI)
        {
            var generalParent = AccessTools.Field(typeof(SettingUI), "_generalParent").GetValue(settingUI) as GameObject;
            var graphicParent = AccessTools.Field(typeof(SettingUI), "_graphicParent").GetValue(settingUI) as GameObject;
            var audioParent = AccessTools.Field(typeof(SettingUI), "_audioParent").GetValue(settingUI) as GameObject;
            var creditsParent = AccessTools.Field(typeof(SettingUI), "_creditsParent").GetValue(settingUI) as GameObject;

            generalParent?.SetActive(false);
            graphicParent?.SetActive(false);
            audioParent?.SetActive(false);
            creditsParent?.SetActive(false);

            // ✅ 新增：反激活所有原生标签按钮的UI状态
            DeactivateAllNativeTabButtons(settingUI);

            // Close all MOD dropdowns before showing MOD tab (mimicking native behavior)
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
            // Play click sound
            PlayClickSound();
            
            // Update tab activation states
            UpdateTabStates();
            
            // Close all dropdowns when opening MOD tab (matching native SettingUI behavior)
            foreach (var dropdown in modDropdowns)
            {
                if (dropdown == null) continue;
                
                try
                {
                    // Get PulldownListUI component directly (it's already added by EnsurePulldownListUI)
                    var pulldownComponents = dropdown.GetComponents<Component>();
                    Component pulldownListUI = null;
                    
                    foreach (var comp in pulldownComponents)
                    {
                        if (comp != null && comp.GetType().Name == "PulldownListUI")
                        {
                            pulldownListUI = comp;
                            break;
                        }
                    }
                    
                    if (pulldownListUI != null)
                    {
                        var closeMethod = pulldownListUI.GetType().GetMethod("ClosePullDown");
                        if (closeMethod != null)
                        {
                            closeMethod.Invoke(pulldownListUI, new object[] { true }); // true = immediate close
                        }
                    }
                }
                catch (System.Exception e)
                {
                    PotatoPlugin.Log.LogWarning($"Failed to close dropdown: {e.Message}");
                }
            }
        }

        /// <summary>
        /// Play the game's native click sound
        /// </summary>
        private static void PlayClickSound()
        {
            try
            {
                if (cachedSettingUI != null)
                {
                    var systemSeServiceField = cachedSettingUI.GetType().GetField("_systemSeService",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (systemSeServiceField != null)
                    {
                        var systemSeService = systemSeServiceField.GetValue(cachedSettingUI);
                        if (systemSeService != null)
                        {
                            var playClickMethod = systemSeService.GetType().GetMethod("PlayClick");
                            playClickMethod?.Invoke(systemSeService, null);
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                PotatoPlugin.Log.LogWarning($"Failed to play click sound: {e.Message}");
            }
        }
        
        /// <summary>
        /// Update tab activation states - ensure only MOD tab is active
        /// </summary>
        private static void UpdateTabStates()
        {
            try
            {
                if (cachedSettingUI == null) return;
                
                var settingUITransform = cachedSettingUI.transform;
                
                // Find all tab buttons
                string[] tabPaths = new string[]
                {
                    "SettingTab/GeneralTabButton",
                    "SettingTab/GraphicsTabButton",
                    "SettingTab/AudioTabButton",
                    "SettingTab/CreditsTabButton"
                };
                
                // Deactivate all native tabs
                foreach (var path in tabPaths)
                {
                    var tabTransform = settingUITransform.Find(path);
                    if (tabTransform != null)
                    {
                        var interactableUI = tabTransform.GetComponent<InteractableUI>();
                        if (interactableUI != null)
                        {
                            interactableUI.DeactivateUseUI(false);
                        }
                    }
                }
                
                // Activate MOD tab
                if (modInteractableUI != null)
                {
                    modInteractableUI.ActivateUseUI(false);
                }
            }
            catch (System.Exception e)
            {
                PotatoPlugin.Log.LogWarning($"Failed to update tab states: {e.Message}");
            }
        }

        /// <summary>
        /// ✅ 新增方法：反激活所有原生标签按钮（General, Graphics, Audio, Credits）
        /// </summary>
        private static void DeactivateAllNativeTabButtons(SettingUI settingUI)
        {
            try
            {
                var generalButton = AccessTools.Field(typeof(SettingUI), "_generalInteractableUI").GetValue(settingUI) as InteractableUI;
                var graphicButton = AccessTools.Field(typeof(SettingUI), "_graphicInteractableUI").GetValue(settingUI) as InteractableUI;
                var audioButton = AccessTools.Field(typeof(SettingUI), "_audioInteractableUI").GetValue(settingUI) as InteractableUI;
                var creditsButton = AccessTools.Field(typeof(SettingUI), "_creditsInteractableUI").GetValue(settingUI) as InteractableUI;

                generalButton?.DeactivateUseUI(false);
                graphicButton?.DeactivateUseUI(false);
                audioButton?.DeactivateUseUI(false);
                creditsButton?.DeactivateUseUI(false);
            }
            catch (System.Exception e)
            {
                PotatoPlugin.Log.LogWarning($"Failed to deactivate native tab buttons: {e.Message}");
            }
        }

        private static void AdjustTabBarLayout(Transform tabBarParent)
        {
            try
            {
                var rectTransform = tabBarParent.GetComponent<RectTransform>();
                var horizontalLayout = tabBarParent.GetComponent<HorizontalLayoutGroup>();
                if (rectTransform == null || horizontalLayout == null) return;

                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
                if (tabBarParent.childCount == 0) return;

                var firstChild = tabBarParent.GetChild(0).GetComponent<RectTransform>();
                if (firstChild == null) return;

                float tabWidth = firstChild.rect.width;
                float spacing = horizontalLayout.spacing;
                int tabCount = tabBarParent.childCount;

                float originalWidth = (tabWidth * 4f) + (spacing * 3f);
                float currentWidth = (tabWidth * tabCount) + (spacing * (tabCount - 1));
                float offset = (currentWidth - originalWidth) / 2f;

                rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x - offset, rectTransform.anchoredPosition.y);
                horizontalLayout.childAlignment = TextAnchor.UpperLeft;
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            }
            catch (System.Exception e)
            {
                PotatoPlugin.Log.LogWarning($"AdjustTabBarLayout failed: {e.Message}");
            }
        }

        // ==================== UI helpers ====================

        static void CreateSectionHeader(Transform parent, string text)
        {
            GameObject obj = new GameObject("SectionHeader");
            obj.transform.SetParent(parent, false);

            var le = obj.AddComponent<LayoutElement>();
            le.minHeight = 48f;
            le.preferredHeight = 48f;

            var tmp = obj.AddComponent<TextMeshProUGUI>();
            if (_cachedFont != null) tmp.font = _cachedFont;
            tmp.text = text;
            tmp.fontSize = 22;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(0.26f, 0.52f, 0.96f, 1f);
            tmp.alignment = TextAlignmentOptions.BottomLeft;
            tmp.margin = new Vector4(0, 0, 0, 8);
        }

        static void CreateNativeDropdown(Transform parent, string label, List<string> options, int currentIndex, System.Action<int> onValueChanged)
        {
            try
            {
                if (cachedSettingUI == null)
                {
                    PotatoPlugin.Log.LogError($"cachedSettingUI is null, cannot create dropdown: {label}");
                    return;
                }

                GameObject pulldownClone = ModPulldownCloner.CloneAndClearPulldown(cachedSettingUI.transform);
                if (pulldownClone == null)
                {
                    PotatoPlugin.Log.LogError($"clone pulldown failed: {label}");
                    return;
                }

                // ❌ 删除位置重置代码 - 让 LayoutGroup 自动处理
                // var rectTransform = pulldownClone.GetComponent<RectTransform>();
                // if (rectTransform != null)
                // {
                //     rectTransform.anchoredPosition = Vector2.zero;
                //     rectTransform.anchoredPosition3D = Vector3.zero;
                //     rectTransform.localPosition = Vector3.zero;
                // }

                Transform titleTransform = pulldownClone.transform.Find("TitleText");
                if (titleTransform != null)
                {
                    TMP_Text titleText = titleTransform.GetComponent<TMP_Text>();
                    if (titleText != null) titleText.text = label;
                }

                GameObject buttonTemplate = ModPulldownCloner.GetSelectButtonTemplate(cachedSettingUI.transform);
                if (buttonTemplate == null)
                {
                    PotatoPlugin.Log.LogError($"button template null: {label}");
                    Object.Destroy(pulldownClone);
                    return;
                }

                // 添加选项
                for (int i = 0; i < options.Count; i++)
                {
                    int index = i;
                    ModPulldownCloner.AddOption(pulldownClone, buttonTemplate, options[i], () =>
                    {
                        onValueChanged?.Invoke(index);
                        SetPulldownSelectedText(pulldownClone, options[index]);
                    });
                }

                // 设置默认选中项
                if (currentIndex >= 0 && currentIndex < options.Count)
                {
                    SetPulldownSelectedText(pulldownClone, options[currentIndex]);
                }

                // 设置 LayoutElement
                var layoutElement = pulldownClone.GetComponent<LayoutElement>() ?? pulldownClone.AddComponent<LayoutElement>();
                layoutElement.preferredHeight = 72f;

                // 挂载并激活
                pulldownClone.transform.SetParent(parent, false);
                pulldownClone.SetActive(true);

                Transform content = pulldownClone.transform.Find("PulldownList/Pulldown/CurrentSelectText (TMP)/Content");
                
                Object.Destroy(buttonTemplate);
                modDropdowns.Add(pulldownClone);

                // ========== 新方案：使用协程等待布局完成 ==========
                ModUICoroutineRunner.Instance.StartCoroutine(WaitForLayoutAndConfigure(
                    pulldownClone, 
                    content, 
                    label, 
                    options, 
                    currentIndex, 
                    onValueChanged
                ));
            }
            catch (System.Exception e)
            {
                PotatoPlugin.Log.LogError($"CreateNativeDropdown failed [{label}]: {e}");
            }
        }

        /// <summary>
        /// 等待布局完成后再配置下拉菜单
        /// </summary>
        private static IEnumerator WaitForLayoutAndConfigure(
            GameObject pulldownClone, 
            Transform content, 
            string label,
            List<string> options,
            int currentIndex,
            System.Action<int> onValueChanged)
        {
            if (pulldownClone == null || content == null) yield break;
            
            var contentRect = content.GetComponent<RectTransform>();
            if (contentRect == null) yield break;
            
            // 第一步：强制重建布局
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            Canvas.ForceUpdateCanvases();
            
            // 第二步：等待 Unity 完成一帧渲染
            yield return new WaitForEndOfFrame();
            
            // 第三步：再次强制重建（确保所有组件都更新）
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            
            // 第四步：等待下一帧（此时 sizeDelta 应该是正确的）
            yield return new WaitForEndOfFrame();
            
            // 第五步：读取 Content 的实际高度
            float contentHeight = contentRect.sizeDelta.y;
            PotatoPlugin.Log.LogInfo($"[{label}] Content height after layout: {contentHeight}");
            
            // 如果高度仍然不正确（< 40），手动计算
            if (contentHeight < 40f)
            {
                int buttonCount = content.childCount;
                contentHeight = buttonCount * 40f; // 每个按钮 40px
                PotatoPlugin.Log.LogWarning($"[{label}] Layout failed, using calculated height: {contentHeight} ({buttonCount} buttons)");
            }
            
            // 配置下拉菜单
            Transform originalPulldown = cachedSettingUI.transform.Find("Graphics/ScrollView/Viewport/Content/GraphicQualityPulldownList");
            ModPulldownCloner.EnsurePulldownListUI(
                pulldownClone, 
                originalPulldown, 
                content,
                contentHeight // 传递正确的高度
            );
            
            // ========== 修复：不再调用 ConfigureDropdownCanvas，避免销毁 Canvas A ==========
            // ConfigureDropdownCanvas(pulldownClone, label); // 删除！这会销毁 Controller 持有的 Canvas 引用
        }

        /* ========== 已删除：此方法会销毁 PulldownLayerController 持有的 Canvas 引用 ==========
        /// <summary>
        /// 配置下拉菜单的 Canvas（解决层级冲突）
        /// </summary>
        private static void ConfigureDropdownCanvas(GameObject pulldownClone, string label)
        {
            Transform pulldownList = pulldownClone.transform.Find("PulldownList");
            if (pulldownList == null)
            {
                PotatoPlugin.Log.LogError($"[{label}] PulldownList not found!");
                return;
            }
            
            // 移除旧的 Canvas
            Canvas oldCanvas = pulldownList.GetComponent<Canvas>();
            if (oldCanvas != null)
            {
                Object.Destroy(oldCanvas);
            }
            
            // 添加新的 Canvas
            Canvas dropdownCanvas = pulldownList.gameObject.AddComponent<Canvas>();
            dropdownCanvas.overrideSorting = true;
            dropdownCanvas.sortingOrder = 1000; // 提高到 1000，确保在最上层
            
            // 添加 GraphicRaycaster
            if (pulldownList.GetComponent<GraphicRaycaster>() == null)
            {
                pulldownList.gameObject.AddComponent<GraphicRaycaster>();
            }
            
            PotatoPlugin.Log.LogInfo($"[{label}] Canvas configured with sortingOrder=1000");
        }
        */

        static void SetPulldownSelectedText(GameObject pulldownClone, string text)
        {
            try
            {
                Transform currentSelectTransform = pulldownClone.transform.Find("PulldownList/Pulldown/CurrentSelectText (TMP)");
                if (currentSelectTransform != null)
                {
                    TMP_Text currentText = currentSelectTransform.GetComponent<TMP_Text>();
                    if (currentText != null)
                    {
                        currentText.text = text;
                        return;
                    }
                }

                string[] possiblePaths = new[]
                {
                    "SelectContent/Text (TMP)",
                    "SelectContent/Text",
                    "CurrentSelectText (TMP)",
                    "CurrentSelectText",
                    "Title/Text (TMP)",
                    "Title/Text",
                    "Text (TMP)"
                };

                TMP_Text selectText = null;
                foreach (var path in possiblePaths)
                {
                    Transform target = pulldownClone.transform.Find(path);
                    if (target != null)
                    {
                        selectText = target.GetComponent<TMP_Text>();
                        if (selectText != null)
                        {
                            selectText.text = text;
                            return;
                        }
                    }
                }

                var pulldownUI = pulldownClone.GetComponent(System.Type.GetType("Bulbul.PulldownListUI, Assembly-CSharp"));
                if (pulldownUI != null)
                {
                    var method = pulldownUI.GetType().GetMethod("ChangeSelectContentText");
                    method?.Invoke(pulldownUI, new object[] { text });
                }
            }
            catch (System.Exception e)
            {
                PotatoPlugin.Log.LogError($"SetPulldownSelectedText error: {e}");
            }
        }

        static List<string> GetFunctionKeyOptions() => new List<string>
            { "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12" };

        static int GetKeyCodeIndex(KeyCode key)
        {
            int index = key - KeyCode.F1;
            return index >= 0 && index < 12 ? index : 1;
        }

        static KeyCode GetKeyCodeFromIndex(int index)
        {
            return index >= 0 && index < 12 ? KeyCode.F1 + index : KeyCode.F2;
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

        private System.Collections.IEnumerator DelayedAction(float seconds, System.Action action)
        {
            yield return new WaitForSeconds(seconds);
            action?.Invoke();
        }
    }

    /// <summary>
    /// ✅ 关键修复：重新激活设置界面时，恢复"常规"标签和其内容面板
    /// 这很重要，因为如果用户在"常规"标签时直接点击MOD标签，
    /// SwitchToModTab会关闭所有原生标签的InteractableUI状态和内容面板
    /// </summary>
    [HarmonyPatch(typeof(SettingUI), "Activate")]
    public class ModSettingsActivateHandler
    {
        static void Postfix(SettingUI __instance)
        {
            try
            {
                // 通过反射获取静态字段和UI引用
                var modContentParent = AccessTools.Field(typeof(ModSettingsIntegration), "modContentParent").GetValue(null) as GameObject;
                var modInteractableUI = AccessTools.Field(typeof(ModSettingsIntegration), "modInteractableUI").GetValue(null) as InteractableUI;

                // 关闭MOD内容面板
                if (modContentParent != null && modContentParent.activeSelf)
                {
                    modContentParent.SetActive(false);
                }
                
                // 关闭MOD按钮的激活状态
                if (modInteractableUI != null)
                {
                    modInteractableUI.DeactivateUseUI(false);
                }

                // ✅ 关键：恢复"常规"标签的激活状态和内容面板
                var generalButton = AccessTools.Field(typeof(SettingUI), "_generalInteractableUI").GetValue(__instance) as InteractableUI;
                var generalParent = AccessTools.Field(typeof(SettingUI), "_generalParent").GetValue(__instance) as GameObject;
                
                if (generalButton != null)
                {
                    generalButton.ActivateUseUI(false);
                }
                
                if (generalParent != null && !generalParent.activeSelf)
                {
                    generalParent.SetActive(true);
                }

                // 关闭其他标签的内容面板
                var graphicParent = AccessTools.Field(typeof(SettingUI), "_graphicParent").GetValue(__instance) as GameObject;
                var audioParent = AccessTools.Field(typeof(SettingUI), "_audioParent").GetValue(__instance) as GameObject;
                var creditsParent = AccessTools.Field(typeof(SettingUI), "_creditsParent").GetValue(__instance) as GameObject;

                if (graphicParent != null && graphicParent.activeSelf)
                    graphicParent.SetActive(false);
                if (audioParent != null && audioParent.activeSelf)
                    audioParent.SetActive(false);
                if (creditsParent != null && creditsParent.activeSelf)
                    creditsParent.SetActive(false);
            }
            catch { }
        }
    }
}
