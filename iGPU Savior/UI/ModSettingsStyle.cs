using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PotatoOptimization.UI
{
  internal static class ModSettingsStyle
  {
    internal const float NativeRowWidth = 1260f;
    internal const float NativeRowHeight = 60.9f;
    internal const float NativeTitleAnchoredX = 324f;
    private const float MinimumTitleWidth = 360f;
    private const float ViewportTitleInset = 64f;
    private static bool hasNativeContentHorizontalLayout;
    private static ModSettingsRectLayoutSnapshot nativeContentLayout;
    private static int nativeContentPaddingLeft;
    private static int nativeContentPaddingRight;
    private static bool nativeContentChildControlWidth;
    private static bool nativeContentChildForceExpandWidth;
    private static ContentSizeFitter.FitMode nativeContentHorizontalFit;

    public static void ConfigurePanel(GameObject panel)
    {
      if (panel == null) return;

      var title = panel.transform.Find("Title")?.GetComponent<TMP_Text>();
      BindModText(title, "MOD_SETTINGS_TITLE");

      foreach (var button in panel.GetComponentsInChildren<Button>(true))
      {
        if (button.name.EndsWith("InitButton", StringComparison.OrdinalIgnoreCase))
          button.gameObject.SetActive(false);
      }
    }

    public static void ConfigureContent(Transform content, Transform nativeContent)
    {
      if (content == null) return;

      CaptureNativeContentHorizontalLayout(nativeContent);

      var contentRect = content.GetComponent<RectTransform>();
      if (contentRect != null)
      {
        // Keep the General page clone's native horizontal geometry. Only ensure
        // the content stretches vertically through the viewport for correct scroll bounds.
        contentRect.anchorMin = new Vector2(contentRect.anchorMin.x, 1f);
        contentRect.anchorMax = new Vector2(contentRect.anchorMax.x, 0f);
        contentRect.localScale = Vector3.one;
      }

      var layout = content.GetComponent<VerticalLayoutGroup>() ?? content.gameObject.AddComponent<VerticalLayoutGroup>();
      layout.spacing = 0f;
      layout.padding = new RectOffset(nativeContentPaddingLeft, nativeContentPaddingRight, 0, 24);
      layout.childAlignment = TextAnchor.UpperLeft;
      layout.childControlHeight = true;
      layout.childControlWidth = hasNativeContentHorizontalLayout ? nativeContentChildControlWidth : true;
      layout.reverseArrangement = false;
      layout.childForceExpandHeight = false;
      layout.childForceExpandWidth = hasNativeContentHorizontalLayout ? nativeContentChildForceExpandWidth : false;

      var fitter = content.GetComponent<ContentSizeFitter>() ?? content.gameObject.AddComponent<ContentSizeFitter>();
      fitter.horizontalFit = hasNativeContentHorizontalLayout ? nativeContentHorizontalFit : ContentSizeFitter.FitMode.Unconstrained;
      fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

      var scrollRect = content.GetComponentInParent<ScrollRect>();
      ConfigureScrollViewport(scrollRect);
      RestoreNativeContentHorizontalLayout(contentRect);
    }

    public static void ConfigureScrollViewport(ScrollRect scrollRect)
    {
      if (scrollRect?.viewport != null)
      {
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        scrollRect.verticalScrollbarSpacing = 0f;
        scrollRect.horizontalScrollbarSpacing = 0f;

        var viewport = scrollRect.viewport;
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = Vector2.zero;
        viewport.offsetMax = new Vector2(0f, -ViewportTitleInset);

        if (viewport.GetComponent<RectMask2D>() == null)
          viewport.gameObject.AddComponent<RectMask2D>();

        RestoreNativeContentHorizontalLayout(scrollRect);
      }
    }

    public static void RestoreNativeContentHorizontalLayout(ScrollRect scrollRect)
    {
      if (scrollRect == null) return;
      RestoreNativeContentHorizontalLayout(scrollRect.content);
    }

    private static void CaptureNativeContentHorizontalLayout(Transform nativeContent)
    {
      if (nativeContent == null) return;

      var rect = nativeContent.GetComponent<RectTransform>();
      if (rect == null) return;

      nativeContentLayout = CaptureLayout(rect);

      var layout = nativeContent.GetComponent<VerticalLayoutGroup>();
      if (layout != null)
      {
        nativeContentPaddingLeft = layout.padding.left;
        nativeContentPaddingRight = layout.padding.right;
        nativeContentChildControlWidth = layout.childControlWidth;
        nativeContentChildForceExpandWidth = layout.childForceExpandWidth;
      }
      else
      {
        nativeContentPaddingLeft = 0;
        nativeContentPaddingRight = 0;
        nativeContentChildControlWidth = true;
        nativeContentChildForceExpandWidth = false;
      }

      var fitter = nativeContent.GetComponent<ContentSizeFitter>();
      nativeContentHorizontalFit = fitter != null
        ? fitter.horizontalFit
        : ContentSizeFitter.FitMode.Unconstrained;

      hasNativeContentHorizontalLayout = true;
    }

    private static void RestoreNativeContentHorizontalLayout(RectTransform contentRect)
    {
      if (!hasNativeContentHorizontalLayout || contentRect == null) return;

      ApplyLayout(contentRect, CaptureLayout(contentRect).WithHorizontalFrom(nativeContentLayout));
    }

    private static ModSettingsRectLayoutSnapshot CaptureLayout(RectTransform rect)
    {
      return new ModSettingsRectLayoutSnapshot(
        rect.anchorMin.x,
        rect.anchorMin.y,
        rect.anchorMax.x,
        rect.anchorMax.y,
        rect.pivot.x,
        rect.pivot.y,
        rect.offsetMin.x,
        rect.offsetMin.y,
        rect.offsetMax.x,
        rect.offsetMax.y);
    }

    private static void ApplyLayout(RectTransform rect, ModSettingsRectLayoutSnapshot layout)
    {
      rect.anchorMin = new Vector2(layout.AnchorMinX, layout.AnchorMinY);
      rect.anchorMax = new Vector2(layout.AnchorMaxX, layout.AnchorMaxY);
      rect.pivot = new Vector2(layout.PivotX, layout.PivotY);
      rect.offsetMin = new Vector2(layout.OffsetMinX, layout.OffsetMinY);
      rect.offsetMax = new Vector2(layout.OffsetMaxX, layout.OffsetMaxY);
    }

    public static void ConfigureTabBar(Transform tabBar)
    {
      if (tabBar == null) return;

      var layout = tabBar.GetComponent<HorizontalLayoutGroup>();
      if (layout == null) return;

      layout.childControlWidth = true;
      layout.childForceExpandWidth = true;
      layout.spacing = 2f;
      layout.padding.left = 0;
      layout.padding.right = 0;

      foreach (Transform child in tabBar)
      {
        if (child.GetComponent<Button>() == null) continue;

        var element = child.GetComponent<LayoutElement>() ?? child.gameObject.AddComponent<LayoutElement>();
        element.ignoreLayout = false;
        element.minWidth = 100f;
        element.preferredWidth = 0f;
        element.flexibleWidth = 1f;
      }
    }

    public static void PrepareRow(GameObject row)
    {
      if (row == null) return;

      var rect = row.GetComponent<RectTransform>();
      var element = row.GetComponent<LayoutElement>();
      float preferredHeight = element != null && element.preferredHeight > 10f
        ? element.preferredHeight
        : rect != null && rect.rect.height > 10f ? rect.rect.height : NativeRowHeight;

      if (rect != null)
      {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(NativeRowWidth, preferredHeight);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
      }

      element = element ?? row.AddComponent<LayoutElement>();
      element.ignoreLayout = false;
      element.minWidth = NativeRowWidth;
      element.preferredWidth = NativeRowWidth;
      element.minHeight = preferredHeight;
      element.preferredHeight = preferredHeight;
      element.flexibleHeight = 0f;
      element.flexibleWidth = 0f;

      var title = FindRowTitle(row);
      if (title == null) return;

      title.alignment = TextAlignmentOptions.MidlineLeft;
      title.enableWordWrapping = false;
      title.overflowMode = TextOverflowModes.Ellipsis;

      var titleRect = title.GetComponent<RectTransform>();
      if (titleRect != null && titleRect.rect.width < MinimumTitleWidth)
        titleRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, MinimumTitleWidth);
    }

    public static TMP_Text FindRowTitle(GameObject row)
    {
      if (row == null) return null;

      var exact = row.transform.Find("TitleText")?.GetComponent<TMP_Text>();
      if (exact != null) return exact;

      foreach (var text in row.GetComponentsInChildren<TMP_Text>(true))
      {
        if (text.GetComponentInParent<Button>() == null)
          return text;
      }

      return null;
    }

    public static void BindModText(TMP_Text text, string key)
    {
      if (text == null) return;

      ModUIHelper.RemoveLocalizers(text.gameObject);
      text.text = key;
      var localizer = text.GetComponent<ModLocalizer>() ?? text.gameObject.AddComponent<ModLocalizer>();
      localizer.Key = key;
    }

    public static void ApplySectionHeaderStyle(TextMeshProUGUI text, Transform settingRoot)
    {
      if (text == null || settingRoot == null) return;

      var source = settingRoot.Find("General/Title")?.GetComponent<TMP_Text>() ??
                   settingRoot.GetComponentInChildren<TMP_Text>(true);
      if (source == null) return;

      text.font = source.font;
      text.fontSharedMaterial = source.fontSharedMaterial;
      text.color = source.color;
    }
  }
}
