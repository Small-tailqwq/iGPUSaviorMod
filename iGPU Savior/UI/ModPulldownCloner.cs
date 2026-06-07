using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Reflection;
using PotatoOptimization.Core;
using PotatoOptimization.Utilities;
using Bulbul;

namespace PotatoOptimization.UI
{
  /// <summary>
  /// Clone game's native dropdown component and customize it for MOD settings
  /// </summary>
  public class ModPulldownCloner
  {
    // 使用 TypeHelper 获取类型
    private static Type GetPulldownUIType()
    {
      return TypeHelper.GetPulldownUIType();
    }

    /// <summary>
    /// Clone the game's GraphicQualityPulldownList and clear its options
    /// Returns a ready-to-use empty pulldown GameObject
    /// </summary>
    public static GameObject CloneAndClearPulldown(Transform settingUITransform)
    {
      try
      {
        if (settingUITransform == null)
        {
          PotatoPlugin.Log.LogError("settingUITransform is null");
          return null;
        }

        // Find the original pulldown in Graphics settings
        Transform originalPath = settingUITransform.Find("Graphics/ScrollView/Viewport/Content/GraphicQualityPulldownList");
        if (originalPath == null)
        {
          PotatoPlugin.Log.LogError("GraphicQualityPulldownList not found");
          return null;
        }

        // Clone it
        GameObject clone = UnityEngine.Object.Instantiate(originalPath.gameObject);
        clone.name = "ModPulldownList";
        clone.SetActive(false);

        // 🔥 Fix Localization
        ModUIHelper.RemoveLocalizers(clone);

        // 尝试找到标题文本并挂载 ModLocalizer
        // 原版下拉框的标题通常在 "TitleText" 或 "Title/Text"
        var paths = new[] { "TitleText", "Title/Text", "Text" };
        foreach (var p in paths)
        {
          var t = clone.transform.Find(p);
          if (t != null)
          {
            var loc = t.gameObject.AddComponent<ModLocalizer>();
            // Key will be set by ModSettingsManager.CreateDropdownSequence
            break;
          }
        }

        // Also attach ModLocalizer to the Selected Text header (CurrentSelectText) so it can be updated dynamically
        var headerPaths = new[] { "PulldownList/Pulldown/CurrentSelectText (TMP)", "CurrentSelectText (TMP)" };
        foreach (var p in headerPaths)
        {
          var t = clone.transform.Find(p);
          if (t != null)
          {
            var loc = t.gameObject.AddComponent<ModLocalizer>();
            // Key is initially null/empty, will be set by UpdatePulldownSelectedText or Default selection logic
            break;
          }
        }

        // Find the Content container (where option buttons are stored)
        Transform content = clone.transform.Find("PulldownList/Pulldown/CurrentSelectText (TMP)/Content");
        if (content == null)
        {
          PotatoPlugin.Log.LogError("Cloned pulldown's Content container not found");
          UnityEngine.Object.Destroy(clone);
          return null;
        }

        // Clear all existing option buttons
        int childCount = content.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
          Transform child = content.GetChild(i);
          UnityEngine.Object.Destroy(child.gameObject);
        }

        // Keep Content always active, but ensure it's initially not visible (will be clipped by RectMask2D)
        content.gameObject.SetActive(true);
        PotatoPlugin.Log.LogDebug("Content initialized (always active, clipped by parent)");

        // Verify PulldownButton exists
        Transform pulldownButtonTransform = clone.transform.Find("PulldownList/PulldownButton");
        if (pulldownButtonTransform != null)
        {
          Button pulldownButton = pulldownButtonTransform.GetComponent<Button>();
          if (pulldownButton == null)
          {
            PotatoPlugin.Log.LogError("PulldownButton has no Button component");
          }
        }
        else
        {
          PotatoPlugin.Log.LogError("PulldownButton not found");
        }

        PotatoPlugin.Log.LogDebug($"Successfully cloned pulldown: {clone.name}");
        // Note: EnsurePulldownListUI will be called after parenting in CreateNativeDropdown
        return clone;
      }
      catch (Exception e)
      {
        PotatoPlugin.Log.LogError($"Failed to clone pulldown: {e}");
        return null;
      }
    }

    /// <summary>
    /// Get a template button from the original pulldown (to clone for new options)
    /// </summary>
    public static GameObject GetSelectButtonTemplate(Transform settingUITransform)
    {
      try
      {
        if (settingUITransform == null)
        {
          PotatoPlugin.Log.LogError("settingUITransform is null");
          return null;
        }

        // Get the first option button from GraphicQualityPulldownList as template
        Transform firstButton = settingUITransform.Find(
            "Graphics/ScrollView/Viewport/Content/GraphicQualityPulldownList/PulldownList/Pulldown/CurrentSelectText (TMP)/Content"
        );

        if (firstButton != null && firstButton.childCount > 0)
        {
          firstButton = firstButton.GetChild(0);
        }
        else
        {
          firstButton = null;
        }

        if (firstButton == null)
        {
          PotatoPlugin.Log.LogError("Original SelectButton template not found");
          return null;
        }

        // Clone it as template
        GameObject template = UnityEngine.Object.Instantiate(firstButton.gameObject);
        template.name = "SelectButtonTemplate";
        template.SetActive(false);

        // 🔥 Fix Localization
        ModUIHelper.RemoveLocalizers(template);

        return template;
      }
      catch (Exception e)
      {
        PotatoPlugin.Log.LogError($"Failed to get SelectButton template: {e}");
        return null;
      }
    }

    /// <summary>
    /// Add an option to the pulldown
    /// </summary>
    public static void AddOption(GameObject pulldownClone, GameObject buttonTemplate, string optionText, Action onClick)
    {
      try
      {
        // Find Content container
        Transform content = pulldownClone.transform.Find("PulldownList/Pulldown/CurrentSelectText (TMP)/Content");
        if (content == null)
        {
          // Check if we already moved content to Viewport (scrolling enabled)
          // If content was moved, the path above won't work, so we search recursively
          content = pulldownClone.transform.Find("PulldownList/Pulldown/CurrentSelectText (TMP)/ScrollView/Viewport/Content");

          // Fallback search
          if (content == null)
          {
            var allContent = pulldownClone.GetComponentsInChildren<RectTransform>(true);
            foreach (var rt in allContent)
            {
              if (rt.name == "Content") { content = rt; break; }
            }
          }

          if (content == null)
          {
            PotatoPlugin.Log.LogError("Content container not found");
            return;
          }
        }

        // Create new button from template
        GameObject newButton = UnityEngine.Object.Instantiate(buttonTemplate, content);
        newButton.name = $"SelectButton_{optionText}";
        newButton.SetActive(true);

        // Set button text
        TMP_Text buttonText = newButton.GetComponentInChildren<TMP_Text>();
        if (buttonText != null)
        {
          buttonText.text = optionText;
          // Localize option button
          var loc = buttonText.gameObject.AddComponent<ModLocalizer>();
          loc.Key = optionText;
        }

        // Ensure all Image components have raycastTarget enabled
        var images = newButton.GetComponentsInChildren<UnityEngine.UI.Image>(true);
        foreach (var img in images)
        {
          img.raycastTarget = true;
        }

        // Setup button click event
        Button button = newButton.GetComponent<Button>();
        if (button != null)
        {
          button.onClick.RemoveAllListeners();
          button.onClick.AddListener(() =>
          {
            PotatoPlugin.Log.LogInfo($"Option clicked: {optionText}");

            // === 修复点：使用通用方法获取类型，不再写死字符串 ===
            try
            {
              Type pulldownType = GetPulldownUIType(); // <--- 使用新方法
              if (pulldownType != null)
              {
                // 尝试在自身或子物体查找组件
                var pulldownUI = pulldownClone.GetComponent(pulldownType);
                if (pulldownUI == null)
                  pulldownUI = pulldownClone.GetComponentInChildren(pulldownType);

                if (pulldownUI != null)
                {
                  // Update selected text logic:
                  // 1. Get Translated text for immediate visual update
                  var langSupplier = NestopiSystem.DIContainers.ProjectLifetimeScope.Resolve<Bulbul.LanguageSupplier>();
                  var lang = langSupplier != null ? langSupplier.Language.CurrentValue : Bulbul.GameLanguageType.English; // Use R3 CurrentValue or Subscription?
                                                                                                                          // NOTE: LanguageSupplier.Language is ReactiveProperty<GameLanguageType> usually.
                                                                                                                          // Assuming NestopiSystem R3 usage, .CurrentValue is correct.
                                                                                                                          // If failing, safely fallback.
                                                                                                                          // Wait, I can use ModLocalizer's cached logic or just ModTranslationManager with English if accessing static supplier is hard?
                                                                                                                          // Using user's provided ModLocalizer usage style:
                                                                                                                          // Actually ModTranslationManager.Get(key, ...) needs type.
                                                                                                                          // Let's rely on ModLocalizer on the header to update itself!

                  // Update the ModLocalizer on the header FIRST
                  var paths = new[] { "PulldownList/Pulldown/CurrentSelectText (TMP)", "CurrentSelectText (TMP)" };
                  foreach (var p in paths)
                  {
                    var t = pulldownClone.transform.Find(p);
                    if (t != null)
                    {
                      var headerLoc = t.GetComponent<ModLocalizer>();
                      if (headerLoc != null)
                      {
                        headerLoc.Key = optionText; // This triggers Refresh() immediately due to Property setter!
                                                    // Since Refresh() sets text, we might not strictly need the game method update,
                                                    // BUT the game method might do other internal state logic.
                      }
                      break;
                    }
                  }

                  // Also call game method with translated text (for consistency/internal state)
                  string finalVisualText = ModTranslationManager.Get(optionText, lang);
                  if (string.IsNullOrEmpty(finalVisualText)) finalVisualText = optionText; // Fallback

                  var changeTextMethod = pulldownType.GetMethod("ChangeSelectContentText", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                  if (changeTextMethod != null)
                  {
                    changeTextMethod.Invoke(pulldownUI, new object[] { finalVisualText });
                    PotatoPlugin.Log.LogInfo($"Updated selected text to: {finalVisualText}");
                  }

                  // Close the pulldown
                  var closePullDownMethod = pulldownType.GetMethod("ClosePullDown", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                  if (closePullDownMethod != null)
                  {
                    closePullDownMethod.Invoke(pulldownUI, new object[] { false });
                    PotatoPlugin.Log.LogInfo("Dropdown closed via ClosePullDown()");
                  }
                }
              }
            }
            catch (Exception ex)
            {
              PotatoPlugin.Log.LogWarning($"Failed to update dropdown: {ex.Message}");
            }

            // Trigger user callback AFTER updating UI
            onClick?.Invoke();
          });

          if (!button.interactable) button.interactable = true;

          if (button.targetGraphic == null)
          {
            var graphic = newButton.GetComponent<UnityEngine.UI.Image>();
            if (graphic != null) button.targetGraphic = graphic;
          }
        }
      }
      catch (Exception e)
      {
        PotatoPlugin.Log.LogError($"Failed to add option: {e}");
      }
    }

    public static void MountPulldown(GameObject pulldownClone, string parentPath)
    {
      try
      {
        GameObject settingRoot = GameObject.Find("UI_FacilitySetting");
        if (settingRoot == null) return;

        Transform parent = settingRoot.transform.Find(parentPath);
        if (parent == null) return;

        pulldownClone.transform.SetParent(parent, false);
        pulldownClone.SetActive(true);
      }
      catch (Exception e)
      {
        PotatoPlugin.Log.LogError($"Failed to mount pulldown: {e}");
      }
    }

    /// <summary>
    /// Ensure the PulldownListUI component is properly configured on the cloned pulldown
    /// </summary>
    public static void EnsurePulldownListUI(GameObject clone, Transform originalPath, Transform content, float manualContentHeight = -1f)
    {
      try
      {
        // 1. 获取类型 (使用通用方法)
        Type pulldownUIType = GetPulldownUIType();
        if (pulldownUIType == null)
        {
          PotatoPlugin.Log.LogError("PulldownListUI type not found");
          return;
        }

        // 2. 找到关键节点
        Transform pulldownList = clone.transform.Find("PulldownList");
        Transform pulldown = clone.transform.Find("PulldownList/Pulldown");
        Transform pulldownButton = clone.transform.Find("PulldownList/PulldownButton");
        Transform currentSelectText = clone.transform.Find("PulldownList/Pulldown/CurrentSelectText (TMP)");

        // 3. 挂载 PulldownListUI 脚本
        GameObject uiHost = (pulldownList != null) ? pulldownList.gameObject : clone;
        Component pulldownUI = uiHost.GetComponent(pulldownUIType);
        if (pulldownUI == null) pulldownUI = uiHost.AddComponent(pulldownUIType);

        // 4. 获取必要的组件引用
        Button pulldownButtonComp = pulldownButton?.GetComponent<Button>();
        TMP_Text currentSelectTextComp = currentSelectText?.GetComponent<TMP_Text>();
        RectTransform pulldownParentRect = pulldown?.GetComponent<RectTransform>();
        RectTransform pulldownButtonRect = pulldownButton?.GetComponent<RectTransform>();
        RectTransform contentRect = content?.GetComponent<RectTransform>();

        if (pulldownButtonComp == null || currentSelectTextComp == null || pulldownParentRect == null) return;

        // 5. 反射辅助方法
        void SetField(string fieldName, object value)
        {
          if (value == null) return;
          pulldownUIType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(pulldownUI, value);
        }

        // =========================================================================
        // 🔥 核心修复：高度计算与滚动条构建 🔥
        // =========================================================================

        // A. 精确计算所需高度
        int childCount = content.childCount;
        float itemHeight = 40f;

        if (childCount > 0)
        {
          var firstChild = content.GetChild(0).GetComponent<RectTransform>();
          if (firstChild != null && firstChild.rect.height > 10) itemHeight = firstChild.rect.height;
        }

        float realContentHeight = childCount * itemHeight;

        // B. 滚动逻辑判断
        float maxVisibleItems = 6f;
        float maxViewHeight = maxVisibleItems * itemHeight;
        bool needsScroll = realContentHeight > maxViewHeight;
        float finalViewHeight = needsScroll ? maxViewHeight : realContentHeight;

        // C. 计算展开动画目标高度
        float headerHeight = pulldownParentRect.rect.height;
        float openSize = headerHeight + finalViewHeight + 10f;

        // D. 动态构建 ScrollView 结构
        if (needsScroll)
        {
          // === 修复12选项样式异常 ===
          // 确保 VerticalLayoutGroup 强制扩展宽度，防止按钮变窄
          var vlg = content.GetComponent<VerticalLayoutGroup>();
          if (vlg != null)
          {
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
          }

          // 检查是否已经在 Viewport 里了
          if (content.parent.name != "Viewport")
          {
            // 创建 ScrollView 结构...
            GameObject scrollView = new GameObject("ScrollView", typeof(RectTransform));
            scrollView.transform.SetParent(content.parent, false);

            var scrollRectRT = scrollView.GetComponent<RectTransform>();
            scrollRectRT.anchorMin = Vector2.zero;
            scrollRectRT.anchorMax = new Vector2(1f, 0f);
            scrollRectRT.pivot = new Vector2(0.5f, 1f);
            scrollRectRT.sizeDelta = new Vector2(0, finalViewHeight);
            scrollRectRT.anchoredPosition = Vector2.zero;

            var scrollRect = scrollView.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 20f;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(scrollView.transform, false);
            var viewRect = viewport.GetComponent<RectTransform>();
            viewRect.anchorMin = Vector2.zero;
            viewRect.anchorMax = Vector2.one;
            viewRect.sizeDelta = Vector2.zero;

            content.SetParent(viewport.transform, true);

            scrollRect.viewport = viewRect;
            scrollRect.content = contentRect;

            // === 关键点：滚动模式下的 Content 定位 ===
            // 必须是【顶部对齐】，否则 12 个选项会“倒挂”或看不见
            contentRect.anchorMin = new Vector2(0, 1); // Top Left
            contentRect.anchorMax = new Vector2(1, 1); // Top Right
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, realContentHeight);

            var fitter = content.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
          }
        }
        else
        {
          // === 修复3选项位置偏右 ===
          if (contentRect != null)
          {
            // 1. 设置为标准下拉框的【底部对齐】锚点
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = new Vector2(1f, 0f);
            contentRect.pivot = new Vector2(0.5f, 1f);

            // 2. 【关键修复】强制 X 轴 SizeDelta 为 0
            // 之前的代码保留了原有的 sizeDelta.x，如果它不为 0，就会导致偏移
            contentRect.sizeDelta = new Vector2(0, realContentHeight);
            contentRect.anchoredPosition = Vector2.zero;
          }
        }

        // =========================================================================
        // 🔥 关键修复：把 Canvas 加在【clone 根节点】上 🔥 
        // =========================================================================

        Canvas rootCanvas = clone.GetComponent<Canvas>();
        if (rootCanvas == null)
        {
          rootCanvas = clone.AddComponent<Canvas>();
          rootCanvas.overrideSorting = false;
          rootCanvas.sortingOrder = 0;

          if (clone.GetComponent<GraphicRaycaster>() == null)
            clone.AddComponent<GraphicRaycaster>();

          PotatoPlugin.Log.LogDebug("✅ Canvas added to ROOT object (ModPulldownList)");
        }

        // 🧹 清理子物体 Canvas
        if (pulldown != null)
        {
          var childCanvas = pulldown.GetComponent<Canvas>();
          if (childCanvas != null) UnityEngine.Object.Destroy(childCanvas);
        }
        if (pulldownList != null)
        {
          var childCanvas = pulldownList.GetComponent<Canvas>();
          if (childCanvas != null) UnityEngine.Object.Destroy(childCanvas);
        }

        // 7. 初始化层级控制器
        var layerController = clone.GetComponent<PulldownLayerController>();
        if (layerController == null) layerController = clone.AddComponent<PulldownLayerController>();

        layerController.Initialize(pulldownUI, rootCanvas);

        // 8. 继续反射赋值
        SetField("_currentSelectContentText", currentSelectTextComp);
        SetField("_pullDownParentRect", pulldownParentRect);
        SetField("_openPullDownSizeDeltaY", openSize);
        SetField("_pullDownOpenCloseSeconds", 0.3f);
        SetField("_pullDownOpenButton", pulldownButtonComp);
        SetField("_pullDownButtonRect", pulldownButtonRect);
        SetField("_isOpen", false);

        // 9. 调用原版 Setup 方法
        pulldownUIType.GetMethod("Setup")?.Invoke(pulldownUI, null);
      }
      catch (Exception e)
      {
        PotatoPlugin.Log.LogError($"Failed to configure PulldownListUI: {e}");
      }
    }
  }

  /// <summary>
  /// Helper component to control Canvas sorting order based on PulldownListUI._isOpen state
  /// Attached to root GameObject to ensure Update() always runs
  /// </summary>
  public class PulldownLayerController : MonoBehaviour
  {
    private Component pulldownUI;
    private Canvas targetCanvas;
    private FieldInfo isOpenField;
    private FieldInfo closeSecondsField;
    private bool lastIsOpen = false;
    private bool isInitialized = false;
    private Coroutine releaseSortingCoroutine;

    public void Initialize(Component pulldownUIComponent, Canvas canvas)
    {
      pulldownUI = pulldownUIComponent;
      targetCanvas = canvas;

      if (pulldownUI != null)
      {
        isOpenField = pulldownUI.GetType().GetField("_isOpen", BindingFlags.NonPublic | BindingFlags.Instance);
        closeSecondsField = pulldownUI.GetType().GetField("_pullDownOpenCloseSeconds", BindingFlags.NonPublic | BindingFlags.Instance);
        isInitialized = true;

        // 强制刷新一次状态
        UpdateSortingOrder(false);
        PotatoPlugin.Log.LogDebug("PulldownLayerController initialized successfully");
      }
    }

    private void Update()
    {
      if (!isInitialized || pulldownUI == null || targetCanvas == null || isOpenField == null) return;

      try
      {
        bool isOpen = (bool)isOpenField.GetValue(pulldownUI);

        // Only update when state changes to reduce overhead
        if (isOpen != lastIsOpen)
        {
          ApplySortingAction(PulldownLayerTransition.GetSortingAction(lastIsOpen, isOpen));
          lastIsOpen = isOpen;
        }
      }
      catch (System.Exception e)
      {
        PotatoPlugin.Log.LogWarning($"[PulldownLayer] Update failed: {e.Message}");
      }
    }

    private void ApplySortingAction(PulldownLayerSortingAction action)
    {
      switch (action)
      {
        case PulldownLayerSortingAction.RaiseNow:
          StopReleaseSortingCoroutine();
          UpdateSortingOrder(true);
          break;
        case PulldownLayerSortingAction.DelayLower:
          DelayLowerSortingOrder();
          break;
        case PulldownLayerSortingAction.LowerNow:
          StopReleaseSortingCoroutine();
          UpdateSortingOrder(false);
          break;
      }
    }

    private void DelayLowerSortingOrder()
    {
      UpdateSortingOrder(true);
      StopReleaseSortingCoroutine();
      releaseSortingCoroutine = StartCoroutine(ReleaseSortingAfterCloseAnimation());
    }

    private IEnumerator ReleaseSortingAfterCloseAnimation()
    {
      float delay = GetCloseAnimationSeconds();
      if (delay > 0f)
        yield return new WaitForSeconds(delay);

      releaseSortingCoroutine = null;
      if (!isInitialized || pulldownUI == null || targetCanvas == null || isOpenField == null) yield break;

      bool isOpen = false;
      try
      {
        isOpen = (bool)isOpenField.GetValue(pulldownUI);
      }
      catch (Exception e)
      {
        PotatoPlugin.Log.LogWarning($"[PulldownLayer] Delayed release failed: {e.Message}");
        yield break;
      }

      if (!isOpen)
        UpdateSortingOrder(false);
    }

    private float GetCloseAnimationSeconds()
    {
      if (closeSecondsField == null || pulldownUI == null) return 0f;

      object value = closeSecondsField.GetValue(pulldownUI);
      return value is float seconds ? seconds : 0f;
    }

    private void StopReleaseSortingCoroutine()
    {
      if (releaseSortingCoroutine == null) return;

      StopCoroutine(releaseSortingCoroutine);
      releaseSortingCoroutine = null;
    }

    private void OnDestroy()
    {
      StopReleaseSortingCoroutine();
    }

    private void UpdateSortingOrder(bool isOpen)
    {
      if (targetCanvas == null) return;

      // ========== 优化修复：开关 overrideSorting ==========
      if (isOpen)
      {
        targetCanvas.overrideSorting = true;
        targetCanvas.sortingOrder = 30000;
      }
      else
      {
        targetCanvas.overrideSorting = false;
        targetCanvas.sortingOrder = 0;
      }
    }
  }

  public static class ModUIHelper
  {
    public static void RemoveLocalizers(GameObject obj)
    {
      if (obj == null) return;
      // Search on the object itself
      var localizer = obj.GetComponent<TextLocalizationBehaviour>();
      if (localizer != null)
      {
        UnityEngine.Object.Destroy(localizer);
        PotatoPlugin.Log.LogInfo($"[UI Fix] Removed localization 'spy' from {obj.name}");
      }

      // check children
      var childLocalizers = obj.GetComponentsInChildren<TextLocalizationBehaviour>(true);
      foreach (var childLoc in childLocalizers)
      {
        UnityEngine.Object.Destroy(childLoc);
      }
    }
  }
}
