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

        // === 修复 Localization：移除原版翻译组件，防止语言切换时变回 Credits ===
        ModUIHelper.RemoveLocalizers(modTabButton);

        // 挂载我们自己的 Localizer
        // 找到 Button 下的 Text (通常是 Text 或 TextMeshPro)
        var btnText = modTabButton.GetComponentInChildren<TMP_Text>(true);
        if (btnText != null)
        {
          var loc = btnText.gameObject.AddComponent<ModLocalizer>();
          loc.Key = "MOD_SETTINGS_TITLE";
        }
        else
        {
          // Fallback if no TMP, look for Text
          var legacyText = modTabButton.GetComponentInChildren<Text>(true);
          if (legacyText != null)
          {
            // Replace Legacy Text with TMP ? Or just set text.
            // Ideally we want ModLocalizer which works with TMP.
            // If the game uses Legacy Text here, we might need a ModLocalizerLegacy or just manual update.
            // Assuming game uses TMP given previous code searches.
          }
        }



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
      // =========================================================
      // 第一步：配置内部 Content (列表容器)
      // =========================================================
      var contentRect = content.GetComponent<RectTransform>();
      if (contentRect != null)
      {
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0, 0);
        contentRect.localScale = Vector3.one;
      }

      // 添加列表布局控制
      var vGroup = content.GetComponent<VerticalLayoutGroup>() ?? content.AddComponent<VerticalLayoutGroup>();
      vGroup.spacing = 16f;
      vGroup.padding = new RectOffset(10, 40, 20, 20); // 左, 右, 上, 下
      vGroup.childAlignment = TextAnchor.UpperLeft;
      vGroup.childControlHeight = false;
      vGroup.childControlWidth = true;
      vGroup.childForceExpandHeight = false;
      vGroup.childForceExpandWidth = true;

      var fitter = content.GetComponent<ContentSizeFitter>() ?? content.AddComponent<ContentSizeFitter>();
      fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
      fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
      var scrollRect = content.GetComponentInParent<ScrollRect>();
      if (scrollRect != null)
      {
        // ==============================================================
        // 👇👇👇 你的 Diff 里缺少了这一段核心代码 👇👇👇
        // ==============================================================
        var rootObj = scrollRect.transform.parent.gameObject;

        // 必须立刻销毁根物体上的布局组件，否则它会无视你的设置，强制把 ScrollView 拉伸到全屏
        var rootLayout = rootObj.GetComponent<VerticalLayoutGroup>();
        if (rootLayout != null) UnityEngine.Object.DestroyImmediate(rootLayout);

        var rootHLayout = rootObj.GetComponent<HorizontalLayoutGroup>();
        if (rootHLayout != null) UnityEngine.Object.DestroyImmediate(rootHLayout);
        // ==============================================================
        // 👆👆👆 必须加上上面这一段 👆👆👆
        // ==============================================================

        var scrollRectTransform = scrollRect.GetComponent<RectTransform>();

        // 确保它是全屏拉伸的锚点
        scrollRectTransform.anchorMin = Vector2.zero;
        scrollRectTransform.anchorMax = Vector2.one;
        scrollRectTransform.pivot = new Vector2(0.5f, 0.5f);

        // 设置边距 (Left, Bottom, Right, Top)
        scrollRectTransform.offsetMin = new Vector2(50f, 50f);

        // 这里的数值必须是负数，Top=-150 才能把顶部空出来
        scrollRectTransform.offsetMax = new Vector2(-50f, -150f);

        PotatoPlugin.Log.LogInfo($"[UI Fix] Constrained ScrollView window: Top=-150, Bottom=50");

        // 顺便修复 Viewport
        if (scrollRect.viewport != null)
        {
          scrollRect.viewport.anchorMin = Vector2.zero;
          scrollRect.viewport.anchorMax = Vector2.one;
          scrollRect.viewport.sizeDelta = Vector2.zero;
          scrollRect.viewport.anchoredPosition = Vector2.zero;

          // 确保有遮罩 (Credits 界面默认可能没有 RectMask2D)
          if (scrollRect.viewport.GetComponent<RectMask2D>() == null)
          {
            scrollRect.viewport.gameObject.AddComponent<RectMask2D>();
          }
        }
      }
    }

    // === 新增的强力修复方法 (请添加到类中) ===
    static void FixScrollViewLayout(ScrollRect scrollRect)
    {
      try
      {
        if (scrollRect == null) return;
        GameObject scrollViewObj = scrollRect.gameObject;
        GameObject rootObj = scrollViewObj.transform.parent.gameObject;

        PotatoPlugin.Log.LogInfo($"[UI Nuclear Fix] Applying fix to {scrollViewObj.name} inside {rootObj.name}");

        // 1. 【关键一步：拆除父级控制】
        // 如果不删除这个组件，你设置的任何 offset 都会在下一帧被它重置！
        var rootVLG = rootObj.GetComponent<VerticalLayoutGroup>();
        if (rootVLG != null)
        {
          PotatoPlugin.Log.LogInfo("  - Destroying VerticalLayoutGroup on Root");
          UnityEngine.Object.DestroyImmediate(rootVLG);
        }
        var rootHLG = rootObj.GetComponent<HorizontalLayoutGroup>();
        if (rootHLG != null) UnityEngine.Object.DestroyImmediate(rootHLG);

        // 2. 【强制设置 ScrollView 坐标】
        var rt = scrollRect.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);

        // 强制边距：上留 130，下留 50，左右留 40
        rt.offsetMin = new Vector2(40f, 50f);   // Left, Bottom
        rt.offsetMax = new Vector2(-40f, -130f); // Right, Top (负数)

        PotatoPlugin.Log.LogInfo($"  - ScrollView Margins Applied: Min={rt.offsetMin}, Max={rt.offsetMax}");

        // 3. 【替换遮罩系统】
        // 移除可能失效的旧 Mask，换上 RectMask2D
        if (scrollRect.viewport != null)
        {
          var oldMask = scrollRect.viewport.GetComponent<Mask>();
          var oldImage = scrollRect.viewport.GetComponent<Image>();

          if (oldMask != null) UnityEngine.Object.DestroyImmediate(oldMask);
          if (oldImage != null) UnityEngine.Object.DestroyImmediate(oldImage);

          var rectMask = scrollRect.viewport.GetComponent<RectMask2D>();
          if (rectMask == null) rectMask = scrollRect.viewport.gameObject.AddComponent<RectMask2D>();

          // 确保 Viewport 填满 ScrollView
          var vpRect = scrollRect.viewport.GetComponent<RectTransform>();
          vpRect.anchorMin = Vector2.zero;
          vpRect.anchorMax = Vector2.one;
          vpRect.sizeDelta = Vector2.zero;
          vpRect.anchoredPosition = Vector2.zero;
        }
      }
      catch (System.Exception e)
      {
        PotatoPlugin.Log.LogError($"[UI Nuclear Fix] Failed: {e.Message}");
      }
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

        var scrollRect = modContentParent.GetComponentInChildren<ScrollRect>();
        if (scrollRect != null)
        {
          manager.RebuildUI(scrollRect.content, cachedSettingUI.transform);
          ModUICoroutineRunner.Instance.RunDelayed(1.0f, () =>
          {
            if (modContentParent != null) DebugUIHierarchy(modContentParent.transform);
          });
        }
      });
    }
    // 在 ModSettingsIntegration 类中添加
    public static void DebugUIHierarchy(Transform root)
    {
      PotatoPlugin.Log.LogWarning($"[UI DEBUG] Inspecting Hierarchy for: {root.name}");
      InspectRecursive(root, 0);
    }
    private static void InspectRecursive(Transform t, int depth)
    {
      // 性能优化：禁用所有 GetComponent 查询和字符串拼接
      // string indent = new string('-', depth * 2);
      // var rect = t.GetComponent<RectTransform>();

      // 检查是否有遮罩组件
      // string maskInfo = "";
      // if (t.GetComponent<UnityEngine.UI.Mask>() != null) maskInfo += " [Mask]";
      // if (t.GetComponent<UnityEngine.UI.RectMask2D>() != null) maskInfo += " [RectMask2D]";
      // if (t.GetComponent<UnityEngine.UI.ScrollRect>() != null) maskInfo += " [ScrollRect]";
      // if (t.GetComponent<UnityEngine.UI.Image>() != null) maskInfo += " [Image]";

      // 打印关键布局信息
      // string layoutInfo = rect != null
      //     ? $"Pos={rect.anchoredPosition}, Size={rect.sizeDelta}, AnchorMin={rect.anchorMin}, AnchorMax={rect.anchorMax}, Pivot={rect.pivot}"
      //     : "Not RectTransform";

      // PotatoPlugin.Log.LogInfo($"{indent}{t.name} {maskInfo} | {layoutInfo}");

      foreach (Transform child in t)
      {
        InspectRecursive(child, depth + 1);
      }
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
      // === 修复 UI 溢出问题：原版加上 MOD 按钮后超出了屏幕宽度 ===
      // 我们在此让顶部的所有分类标签自适应变窄（通过强制约束 HorizontalLayoutGroup 和总宽度）
      
      var rectTransform = tabBarParent.GetComponent<RectTransform>();
      if (rectTransform != null)
      {
        // 1. 移除可能存在的尺寸自适应组件，防止父容器随着按钮增多无限制横向膨胀
        var fitter = tabBarParent.GetComponent<ContentSizeFitter>();
        if (fitter != null)
        {
          UnityEngine.Object.DestroyImmediate(fitter);
        }

        // 2. 将此栏框的总宽度锁死在安全尺寸内（1140 刚好能在滚动视图内完美容纳，原为 1189 导致越界）
        rectTransform.sizeDelta = new Vector2(1140f, rectTransform.sizeDelta.y);

        // 3. 将之前错误的偏移动画修正回居中偏左一点的位置。不再沿用 `-90f` 导致第一项（常规）被遮挡切角的旧版逻辑
        rectTransform.anchoredPosition = new Vector2(15f, rectTransform.anchoredPosition.y);
      }

      var hlg = tabBarParent.GetComponent<HorizontalLayoutGroup>();
      if (hlg != null)
      {
        PotatoPlugin.Log.LogInfo($"[UI Fix] Applying adaptive HorizontalLayoutGroup for tabs.");
        // 第四步极其重要：告诉 Unity LayoutGroup：你现在有支配底下 6 个按钮缩放和拉伸的终极权限。
        // 它会根据总长自动将原先的按钮等比率（只缩小了不到 4%）压缩，以此适应我们刚刚指定的 1140 宽度。
        hlg.childControlWidth = true; 
        hlg.childForceExpandWidth = true;
        hlg.spacing = 2f; 
        
        if (hlg.padding != null) 
        { 
          hlg.padding.left = 0; 
          hlg.padding.right = 0; 
        }
      }
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
      var allFields = AccessTools.GetDeclaredFields(typeof(SettingUI));
      var interactableFields = allFields.Where(f => f.FieldType == typeof(InteractableUI) && f.Name.EndsWith("InteractableUI"));

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

      // 关闭原版游戏目前存在的所有标签内容页
      foreach (var fParent in allFields.Where(f => f.FieldType == typeof(GameObject) && f.Name.EndsWith("Parent")))
        (fParent.GetValue(settingUI) as GameObject)?.SetActive(false);

      // 取消原版游戏目前存在的所有标签按钮的激活高亮状态
      foreach (var fBtn in allFields.Where(f => f.FieldType == typeof(InteractableUI) && f.Name.EndsWith("InteractableUI")))
        (fBtn.GetValue(settingUI) as InteractableUI)?.DeactivateUseUI(false);

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

        var allFields = AccessTools.GetDeclaredFields(typeof(SettingUI));
        
        // 当打开菜单时，动态关闭所有除非 General 以外的其他原版标签页
        foreach (var fParent in allFields.Where(f => f.FieldType == typeof(GameObject) && f.Name.EndsWith("Parent")))
        {
            var p = fParent.GetValue(__instance) as GameObject;
            if (fParent.Name != "_generalParent") p?.SetActive(false);
        }

        foreach (var fBtn in allFields.Where(f => f.FieldType == typeof(InteractableUI) && f.Name.EndsWith("InteractableUI")))
        {
            var b = fBtn.GetValue(__instance) as InteractableUI;
            if (fBtn.Name != "_generalInteractableUI") b?.DeactivateUseUI(false);
        }
      }
      catch { }
    }
  }
}
