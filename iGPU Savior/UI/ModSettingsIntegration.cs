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

        // === 修复 UI 溢出问题：限制按钮宽度 ===
        var le = modTabButton.GetComponent<LayoutElement>();
        if (le == null) le = modTabButton.AddComponent<LayoutElement>();
        // 允许压缩，设置合适的首选宽度 (MOD 字很短，不需要原来那么宽)
        le.flexibleWidth = 0;
        le.minWidth = 80f;
        le.preferredWidth = 120f; // 比 Credits 短一些

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
      vGroup.padding = new RectOffset(10, 40, 20, 20);
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

        manager.AddToggle("Enable Mirror", PotatoPlugin.Config.CfgEnableMirror.Value, val =>
        {
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

        // 测试：把 KeyPortraitMode 作为文本框显示
        // 逻辑：读取当前 Config -> 转 string 显示 -> 用户输入 -> 存入 string (不做校验，用户输错了是用户的事)
        manager.AddInputField("Portrait Key (Text)", PotatoPlugin.Config.KeyPortraitMode.Value.ToString(), val =>
        {
            // 这里我们为了测试"容错不需要"，直接尝试转换，失败拉倒，或者Config本身就是String类型
            // 如果你的 Config 是 KeyCode 类型，依然得 Parse 一下才能存进去，否则类型不匹配
            
            try 
            {
                // 简单粗暴：转大写后强转
                KeyCode newKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), val.ToUpper());
                PotatoPlugin.Config.KeyPortraitMode.Value = newKey;
            }
            catch
            {
                PotatoPlugin.Log.LogWarning($"用户输入的 '{val}' 不是有效的 KeyCode，已忽略");
            }
        });

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
      // === 修复 UI 溢出问题：幽灵模式 (Sidecar) ===

      // 1. 彻底撤销对 HorizontalLayoutGroup 的修改
      // 让原版游戏逻辑去接管那 4 个按钮，这样它们就会恢复正常，不再挤成一团
      var hlg = tabBarParent.GetComponent<HorizontalLayoutGroup>();
      if (hlg != null)
      {
        PotatoPlugin.Log.LogInfo($"[UI Fix] Reverting HorizontalLayoutGroup changes.");
        // 下面这些属性如果被我改坏了，尝试改回默认比较安全的设置
        // 假设原版是 true? 或者 false? 
        // 最安全的方法是：根本不碰它。
        // 但因为之前版本可能已经持久化修改了（虽然是内存中），为防万一，我们重置一下
        // 根据“制\n作”换行，说明宽度太窄。
        // 尝试恢复 childForceExpandWidth = true (通常顶栏按钮需要铺满)
        hlg.childForceExpandWidth = true; // 或者是原版默认值
        hlg.spacing = 0f;
        if (hlg.padding != null) { hlg.padding.left = 0; hlg.padding.right = 0; }
      }

      // 2. 也不需要调整 Parent 的 SizeDelta 了，除非真的太窄
      // 用户之前的图看，其实位置偏移 -90 还是有用的，保留
      var rectTransform = tabBarParent.GetComponent<RectTransform>();
      if (rectTransform != null)
      {
        // 仍然左移，保持视觉居中
        var currentPos = rectTransform.anchoredPosition;
        // 简单的防止无限左移逻辑：假设初值肯定大于 -400 (用户原值 -378)
        // if (currentPos.x > -400) 
        rectTransform.anchoredPosition = new Vector2(currentPos.x - 90f, currentPos.y);
      }

      // 没有任何 ForceRebuild，让 Unity 自己算
    }

    static void ConfigureGhostButton(GameObject modBtn)
    {
      // === 关键逻辑：让 MOD 按钮脱离布局控制 ===
      var le = modBtn.GetComponent<LayoutElement>();
      if (le == null) le = modBtn.AddComponent<LayoutElement>();

      // 1. 【核心】让 HLG 忽略此按钮，这样前面 4 个按钮就会像没加 Mod 按钮一样正常渲染
      le.ignoreLayout = true;

      // 2. 手动定位到父容器的右侧
      var rt = modBtn.GetComponent<RectTransform>();

      // AnchorMin/Max = (1, 0) -> (1, 1) 表示紧贴父容器右边缘
      rt.anchorMin = new Vector2(1f, 0f);
      rt.anchorMax = new Vector2(1f, 1f);
      rt.pivot = new Vector2(0f, 0.5f); // 轴心在左边，方便往右延伸

      // 3. 设置宽高位置
      // X = 0 表示紧贴着父容器的最右边
      // Width = 140
      rt.anchoredPosition = new Vector2(0f, 0f);
      rt.sizeDelta = new Vector2(140f, 0f); // Y=0 配合 anchor (0-1) 表示高度撑满

      // 修正文本对齐
      var text = modBtn.GetComponentInChildren<TextMeshProUGUI>();
      if (text) text.enableWordWrapping = false; // 禁止换行
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
