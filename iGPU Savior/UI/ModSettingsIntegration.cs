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
using PotatoOptimization.Features;

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
    private static GameObject _whisperDropdown;

    static void Postfix(SettingUI __instance)
    {
      try
      {
        PotatoPlugin.Log.LogInfo("[ModSettings] Setup Postfix called");
        cachedSettingUI = __instance;
        _rootCanvas = __instance.GetComponentInParent<Canvas>() ?? Object.FindObjectOfType<Canvas>();
        PotatoPlugin.Log.LogInfo($"[ModSettings] Canvas found: {_rootCanvas != null}");

        CreateModSettingsTab(__instance);
        PotatoPlugin.Log.LogInfo($"[ModSettings] Tab created, modContentParent: {modContentParent != null}");
        
        HookIntoTabButtons(__instance);
        PotatoPlugin.Log.LogInfo("[ModSettings] Tab buttons hooked");

        modContentParent?.SetActive(false);
        PotatoPlugin.Log.LogInfo("[ModSettings] Setup completed successfully");
      }
      catch (System.Exception e)
      {
        PotatoPlugin.Log.LogError($"[ModSettings] Integration failed: {e.Message}\n{e.StackTrace}");
      }
    }


    static void CreateModSettingsTab(SettingUI settingUI)
    {
      try
      {
        var creditsButton = AccessTools.Field(typeof(SettingUI), "_creditsInteractableUI").GetValue(settingUI) as InteractableUI;
        var generalParent = AccessTools.Field(typeof(SettingUI), "_generalParent").GetValue(settingUI) as GameObject;
        if (creditsButton == null || generalParent == null) return;

        GameObject modTabButton = Object.Instantiate(creditsButton.gameObject);
        modTabButton.name = "ModSettingsTabButton";
        modTabButton.transform.SetParent(creditsButton.transform.parent, false);
        modTabButton.transform.SetSiblingIndex(creditsButton.transform.GetSiblingIndex() + 1);

        // The General page is structurally compatible with setting rows. Credits is not:
        // its text-oriented ScrollView forced the old implementation into fixed offsets.
        modContentParent = Object.Instantiate(generalParent);
        modContentParent.name = "ModSettingsContent";
        modContentParent.transform.SetParent(generalParent.transform.parent, false);
        modContentParent.SetActive(false);
        ModSettingsStyle.ConfigurePanel(modContentParent);

        var nativeScrollRect = generalParent.GetComponentInChildren<ScrollRect>(true);
        var scrollRect = modContentParent.GetComponentInChildren<ScrollRect>(true);
        if (scrollRect == null) return;

        var content = scrollRect.content;
        foreach (Transform child in content) Object.Destroy(child.gameObject);

        ConfigureContentLayout(content.gameObject, nativeScrollRect?.content);

        ModSettingsManager manager = ModSettingsManager.Instance;
        if (manager == null)
        {
          GameObject managerObj = new GameObject("ModSettingsManager");
          Object.DontDestroyOnLoad(managerObj);
          manager = managerObj.AddComponent<ModSettingsManager>();
        }

        modInteractableUI = modTabButton.GetComponent<InteractableUI>();
        modInteractableUI?.Setup();
        modTabButton.GetComponent<Button>()?.onClick.AddListener(() => SwitchToModTab(settingUI));

        var btnText = modTabButton.GetComponentInChildren<TMP_Text>(true);
        ModSettingsStyle.BindModText(btnText, "MOD_TAB_TITLE");
        ModSettingsStyle.ConfigureTabBar(modTabButton.transform.parent);

        RegisterCurrentMod(manager);
      }
      catch (System.Exception e)
      {
        PotatoPlugin.Log.LogError($"CreateModSettingsTab failed: {e.Message}");
      }
    }

    static void ConfigureContentLayout(GameObject content, Transform nativeContent)
    {
      ModSettingsStyle.ConfigureContent(content.transform, nativeContent);
    }

    static void RegisterCurrentMod(ModSettingsManager manager)
    {
      ModUICoroutineRunner.Instance.RunDelayed(0.5f, () =>
      {
        if (modContentParent == null || cachedSettingUI == null) return;

        manager.RegisterMod("iGPU Savior", PotatoOptimization.Core.Constants.PluginVersion);

        manager.AddToggle("SETTING_MIRROR_AUTO", PotatoPlugin.Config.CfgEnableMirror.Value, val =>
        {
          PotatoPlugin.Config.CfgEnableMirror.Value = val;
          Object.FindObjectOfType<PotatoController>()?.SetMirrorState(val);
        });

        manager.AddToggle("SETTING_PORTRAIT_AUTO", PotatoPlugin.Config.CfgEnablePortraitMode.Value, val =>
        {
          PotatoPlugin.Config.CfgEnablePortraitMode.Value = val;
          PotatoPlugin.Log.LogInfo($"竖屏优化自启动已设置为: {val}");
        });
        
        manager.AddToggle("SETTING_DELETE_CONFIRM", PotatoPlugin.Config.CfgEnableDeleteConfirm.Value, val =>
        {
          PotatoPlugin.Config.CfgEnableDeleteConfirm.Value = val;
          PotatoPlugin.Log.LogInfo($"删除二次确认已设置为: {val}");
        });

        manager.AddToggle("SETTING_BG_OPTIMIZATION", PotatoPlugin.Config.CfgEnableBackgroundOptimization.Value, val =>
        {
          PotatoPlugin.Config.CfgEnableBackgroundOptimization.Value = val;
          PotatoPlugin.Config.Save();
          PotatoPlugin.Log.LogInfo($"后台省电优化已设置为: {val}");
        });

        manager.AddToggle("SETTING_PIP_AUTO_HIDE_GUI", PotatoPlugin.Config.CfgAutoHideGuiInPiP.Value, val =>
        {
          PotatoPlugin.Config.CfgAutoHideGuiInPiP.Value = val;
          PotatoPlugin.Config.Save();
          PotatoPlugin.Log.LogInfo($"小窗自动隐藏GUI已设置为: {val}");
        });

        manager.AddDropdown("SETTING_MINI_SCALE", new List<string> { "1/3", "1/4", "1/5" },
                  (int)PotatoPlugin.Config.CfgWindowScale.Value - 3,
                  index => PotatoPlugin.Config.CfgWindowScale.Value = (WindowScaleRatio)(index + 3));

        manager.AddDropdown("SETTING_DRAG_MODE", new List<string> { "DRAG_MODE_CTRL", "DRAG_MODE_ALT", "DRAG_MODE_RIGHT" },
                  (int)PotatoPlugin.Config.CfgDragMode.Value,
                  index => PotatoPlugin.Config.CfgDragMode.Value = (DragMode)index);

        var keyOptions = new List<string> { "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12" };
        int GetKeyIndex(KeyCode key) { int i = key - KeyCode.F1; return (i >= 0 && i < 12) ? i : 0; }
        KeyCode GetKey(int i) { return KeyCode.F1 + i; }

        manager.AddDropdown("SETTING_KEY_POTATO", keyOptions, GetKeyIndex(PotatoPlugin.Config.KeyPotatoMode.Value),
                  i => PotatoPlugin.Config.KeyPotatoMode.Value = GetKey(i));
        manager.AddDropdown("SETTING_KEY_PIP", keyOptions, GetKeyIndex(PotatoPlugin.Config.KeyPiPMode.Value),
                  i => PotatoPlugin.Config.KeyPiPMode.Value = GetKey(i));
        manager.AddDropdown("SETTING_KEY_MIRROR", keyOptions, GetKeyIndex(PotatoPlugin.Config.KeyCameraMirror.Value),
                  i => PotatoPlugin.Config.KeyCameraMirror.Value = GetKey(i));

        manager.AddDropdown("SETTING_KEY_PORTRAIT", keyOptions, GetKeyIndex(PotatoPlugin.Config.KeyPortraitMode.Value),
                  i => PotatoPlugin.Config.KeyPortraitMode.Value = GetKey(i));

        // 悄悄话-服装建议下拉框
        var whisperSkinKeys = new List<string> { "WHISPER_NONE" };
        var whisperSkinMap = new Dictionary<string, string> { { "WHISPER_NONE", "" } };
        foreach (var skinName in CostumeWhisperHelper.GetCostumeSkinNames())
        {
            var key = "SKIN_" + skinName;
            whisperSkinKeys.Add(key);
            whisperSkinMap[key] = skinName;
        }
        string currentWhisperValue = PotatoPlugin.Config.CfgSuggestedCostumeSkin.Value;
        string currentWhisperKey = string.IsNullOrEmpty(currentWhisperValue) ? "WHISPER_NONE"
            : "SKIN_" + currentWhisperValue;
        int whisperDefaultIdx = whisperSkinKeys.IndexOf(currentWhisperKey);
        if (whisperDefaultIdx < 0) whisperDefaultIdx = 0;

        manager.AddDropdown("SETTING_WHISPER", whisperSkinKeys, whisperDefaultIdx, index =>
        {
            string selectedKey = whisperSkinKeys[index];
            string skinValue = whisperSkinMap[selectedKey];
            PotatoPlugin.Config.CfgSuggestedCostumeSkin.Value = skinValue;
            PotatoPlugin.Config.Save();

            if (index == 0)
            {
                PotatoPlugin.Log.LogInfo("[Whisper] Cleared suggestion");
            }
            else
            {
                var langSupplier = NestopiSystem.DIContainers.ProjectLifetimeScope
                    .Resolve<LanguageSupplier>();
                var currentLang = langSupplier?.Get() ?? GameLanguageType.English;
                string translatedName = ModTranslationManager.Get(selectedKey, currentLang);
                string msg = ModTranslationManager.Get("WHISPER_SET_SUCCESS", currentLang);
                CostumeWhisperHelper.ShowToast($"{msg} [{translatedName}]");
                PotatoPlugin.Log.LogInfo($"[Whisper] Set suggestion: {skinValue}");

                ModUICoroutineRunner.Instance.RunDelayed(0.2f, () =>
                {
                    SetWhisperDropdownPending();
                });
            }
        });

        var scrollRect = modContentParent.GetComponentInChildren<ScrollRect>();
        if (scrollRect != null)
        {
          manager.RebuildUI(scrollRect.content, cachedSettingUI.transform);
          ModUICoroutineRunner.Instance.RunDelayed(1.0f, () =>
          {
            if (modContentParent != null)
            {
              FindWhisperDropdown();
              if (CostumeWhisperHelper.IsWhisperPending())
              {
                SetWhisperDropdownPending();
              }
            }
          });
        }
      });
    }
    // 在 ModSettingsIntegration 类中添加
    private static void FindWhisperDropdown()
    {
      _whisperDropdown = null;
      if (modContentParent == null) return;
      var allT = modContentParent.GetComponentsInChildren<Transform>(true);
      foreach (var child in allT)
      {
        if (child.name == "SelectButton_WHISPER_NONE")
        {
          var p = child.parent;
          while (p != null)
          {
            if (p.name == "ModPulldownList") { _whisperDropdown = p.gameObject; return; }
            p = p.parent;
          }
          break;
        }
      }
    }

    private static void SetWhisperDropdownPending()
    {
      if (_whisperDropdown == null) FindWhisperDropdown();
      if (_whisperDropdown == null) return;

      var pulldownBtn = _whisperDropdown.transform.Find("PulldownList/PulldownButton");
      if (pulldownBtn != null)
      {
        var btn = pulldownBtn.GetComponent<Button>();
        if (btn != null) btn.interactable = false;
        var cg = pulldownBtn.GetComponent<CanvasGroup>() ?? pulldownBtn.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0.4f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
      }

      var curText = _whisperDropdown.transform.Find("PulldownList/Pulldown/CurrentSelectText (TMP)");
      if (curText != null)
      {
        var tmp = curText.GetComponent<TMP_Text>();
        if (tmp != null)
        {
          var langSupplier = NestopiSystem.DIContainers.ProjectLifetimeScope.Resolve<LanguageSupplier>();
          var lang = langSupplier?.Get() ?? GameLanguageType.English;
          tmp.text = ModTranslationManager.Get("WHISPER_PENDING", lang);
          tmp.color = new Color(1f, 1f, 1f, 0.5f);
        }
      }
    }

    public static void DebugUIHierarchy(Transform root)
    {
      PotatoPlugin.Log.LogWarning($"[UI DEBUG] Inspecting Hierarchy for: {root.name}");
      InspectRecursive(root, 0);
    }
    private static void InspectRecursive(Transform t, int depth)
    {
      foreach (Transform child in t)
      {
        InspectRecursive(child, depth + 1);
      }
    }

    private static void HookIntoTabButtons(SettingUI settingUI)
    {
      var allFields = AccessTools.GetDeclaredFields(typeof(SettingUI));
      var interactableFields = SettingUITabFieldSelector.GetTabInteractableFields(
        allFields,
        typeof(InteractableUI),
        typeof(GameObject));

      foreach (var fBtn in interactableFields)
      {
        var prefix = fBtn.Name.Replace("InteractableUI", "");
        var fParent = allFields.FirstOrDefault(f => f.Name == prefix + "Parent" && f.FieldType == typeof(GameObject));

        if (fParent != null)
        {
          var capturedBtn = fBtn.GetValue(settingUI) as InteractableUI;
          var capturedParent = fParent.GetValue(settingUI) as GameObject;

          if (capturedBtn != null && capturedParent != null)
          {
            capturedBtn.GetComponent<Button>()?.onClick.AddListener(() =>
            {
              modContentParent?.SetActive(false);
              modInteractableUI?.DeactivateUseUI(false);
              capturedParent.SetActive(true);
              capturedBtn.ActivateUseUI(false);
            });
          }
        }
      }
    }

    private static void SwitchToModTab(SettingUI settingUI)
    {
      var allFields = AccessTools.GetDeclaredFields(typeof(SettingUI));
      var tabInteractableFields = SettingUITabFieldSelector.GetTabInteractableFields(
        allFields,
        typeof(InteractableUI),
        typeof(GameObject));
      var tabParentFields = tabInteractableFields
        .Select(tabField =>
        {
          var prefix = tabField.Name.Replace("InteractableUI", "");
          return allFields.FirstOrDefault(f => f.Name == prefix + "Parent" && f.FieldType == typeof(GameObject));
        })
        .Where(parentField => parentField != null)
        .Distinct();

      // 关闭原版游戏目前存在的所有标签内容页
      foreach (var fParent in tabParentFields)
        (fParent.GetValue(settingUI) as GameObject)?.SetActive(false);

      // 取消原版游戏目前存在的所有标签按钮的激活高亮状态
      foreach (var fBtn in tabInteractableFields)
        (fBtn.GetValue(settingUI) as InteractableUI)?.DeactivateUseUI(false);

      OnOpenModTab();
      modInteractableUI?.ActivateUseUI(false);
      modContentParent?.SetActive(true);

      var scrollRect = modContentParent?.GetComponentInChildren<ScrollRect>();
      if (scrollRect != null)
      {
        ModSettingsStyle.ConfigureScrollViewport(scrollRect);
        LayoutRebuilder.ForceRebuildLayoutImmediate(modContentParent.GetComponent<RectTransform>());
        ModSettingsStyle.ConfigureScrollViewport(scrollRect);
        scrollRect.verticalNormalizedPosition = 1f;

        ModUICoroutineRunner.Instance.RunDelayed(0.05f, () =>
        {
          if (modContentParent == null) return;
          var delayedScrollRect = modContentParent.GetComponentInChildren<ScrollRect>();
          if (delayedScrollRect == null) return;

          ModSettingsStyle.ConfigureScrollViewport(delayedScrollRect);
          delayedScrollRect.StopMovement();
          delayedScrollRect.verticalNormalizedPosition = 1f;
        });
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

        var allFields = AccessTools.GetDeclaredFields(typeof(SettingUI));
        var tabInteractableFields = SettingUITabFieldSelector.GetTabInteractableFields(
          allFields,
          typeof(InteractableUI),
          typeof(GameObject));
        var tabParentFields = tabInteractableFields
          .Select(tabField =>
          {
            var prefix = tabField.Name.Replace("InteractableUI", "");
            return allFields.FirstOrDefault(f => f.Name == prefix + "Parent" && f.FieldType == typeof(GameObject));
          })
          .Where(parentField => parentField != null)
          .Distinct();
        
        // 当打开菜单时，动态关闭所有除非 General 以外的其他原版标签页
        foreach (var fParent in tabParentFields)
        {
            var p = fParent.GetValue(__instance) as GameObject;
            if (fParent.Name != "_generalParent") p?.SetActive(false);
        }

        foreach (var fBtn in tabInteractableFields)
        {
            var b = fBtn.GetValue(__instance) as InteractableUI;
            if (fBtn.Name != "_generalInteractableUI") b?.DeactivateUseUI(false);
        }
      }
      catch (System.Exception e)
      {
        PotatoPlugin.Log.LogWarning($"[ModSettingsActivate] Postfix failed: {e.Message}");
      }
    }
  }
}
