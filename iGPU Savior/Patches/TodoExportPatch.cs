using System;
using Bulbul;
using HarmonyLib;
using NestopiSystem.DIContainers;
using PotatoOptimization.Core;
using PotatoOptimization.Features;
using PotatoOptimization.UI;
using PotatoOptimization.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PotatoOptimization.Patches
{
  [HarmonyPatch(typeof(TodoListUI), nameof(TodoListUI.Setup))]
  internal static class TodoExportPatch
  {
    private const string ExportContainerName = "PotatoTodoExportUI";
    private const string ExportBaseSpriteName = "t_ui_letterbutton";
    private const string ExportActiveSpriteName = "t_ui_letterbutton_active";
    private const float ExportButtonWidth = 96f;
    private const float ExportTextFontSize = 17f;
    private const float HorizontalControlGap = 4f;

    private static void Postfix(TodoListUI __instance)
    {
      try
      {
        if (__instance == null || __instance.addTodoButton == null)
        {
          return;
        }

        var context = __instance.GetComponent<TodoExportUiContext>();
        if (context == null)
        {
          context = __instance.gameObject.AddComponent<TodoExportUiContext>();
        }

        var addButton = __instance.addTodoButton;
        var addContainerRect = addButton.transform.parent as RectTransform;
        if (addContainerRect == null)
        {
          return;
        }

        var exportContainerRect = __instance.transform.Find(ExportContainerName) as RectTransform;
        Button exportButton;
        if (exportContainerRect == null)
        {
          exportButton = CreateExportButton(
            addButton,
            addContainerRect,
            __instance.transform as RectTransform,
            out exportContainerRect);
        }
        else
        {
          exportButton = exportContainerRect.GetComponentInChildren<Button>(true);
        }

        if (exportButton == null || exportContainerRect == null)
        {
          return;
        }

        LayoutExportButton(addContainerRect, exportContainerRect);
        context.Bind(__instance, exportButton);
      }
      catch (Exception e)
      {
        PotatoPlugin.Log?.LogError("[TodoExport] Failed to inject todo export UI: " + e);
      }
    }

    private static Button CreateExportButton(
      Button templateButton,
      RectTransform templateContainerRect,
      RectTransform parent,
      out RectTransform exportContainerRect)
    {
      exportContainerRect = null;
      if (parent == null)
      {
        return null;
      }

      var exportContainer = new GameObject(
        ExportContainerName,
        typeof(RectTransform),
        typeof(CanvasRenderer),
        typeof(Image));
      exportContainer.transform.SetParent(parent, false);
      exportContainerRect = exportContainer.transform as RectTransform;
      CopyRectTransform(templateContainerRect, exportContainerRect);

      var templateImage = templateContainerRect.GetComponent<Image>();
      var exportImage = exportContainer.GetComponent<Image>();
      CopyImage(templateImage, exportImage);

      var buttonObject = UnityEngine.Object.Instantiate(
        templateButton.gameObject,
        exportContainerRect);
      buttonObject.name = "PotatoTodoExportButton";

      foreach (var localizer in buttonObject.GetComponentsInChildren<TextLocalizationBehaviour>(true))
      {
        localizer.enabled = false;
      }

      foreach (var text in buttonObject.GetComponentsInChildren<TMP_Text>(true))
      {
        text.text = GetText("TODO_EXPORT_BUTTON");
        var localizer = text.GetComponent<ModLocalizer>();
        if (localizer == null)
        {
          localizer = text.gameObject.AddComponent<ModLocalizer>();
        }

        localizer.Key = "TODO_EXPORT_BUTTON";
      }

      var exportButton = buttonObject.GetComponent<Button>();
      if (exportButton != null)
      {
        exportButton.onClick.RemoveAllListeners();
      }

      return exportButton;
    }

    private static void LayoutExportButton(
      RectTransform addContainerRect,
      RectTransform exportContainerRect)
    {
      var parent = exportContainerRect.parent as RectTransform;
      if (parent == null)
      {
        return;
      }

      ApplyTextOnlyButtonStyle(exportContainerRect);
      SetHorizontalSize(exportContainerRect, ExportButtonWidth);
      var exportButton = exportContainerRect.GetComponentInChildren<Button>(true);
      if (exportButton != null)
      {
        SetHorizontalSize(exportButton.transform as RectTransform, ExportButtonWidth);
      }

      foreach (var image in exportContainerRect.GetComponentsInChildren<Image>(true))
      {
        if (image.transform != exportContainerRect)
        {
          SetHorizontalSize(image.transform as RectTransform, ExportButtonWidth);
        }
      }

      foreach (var text in exportContainerRect.GetComponentsInChildren<TMP_Text>(true))
      {
        SetHorizontalSize(text.transform as RectTransform, ExportButtonWidth);
        text.enableAutoSizing = false;
        text.fontSize = ExportTextFontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.margin = Vector4.zero;
      }

      var addCenterInParent = parent.InverseTransformPoint(
        addContainerRect.TransformPoint(addContainerRect.rect.center));
      float anchorX = Mathf.Lerp(
        parent.rect.xMin,
        parent.rect.xMax,
        exportContainerRect.anchorMin.x);
      float anchorY = Mathf.Lerp(
        parent.rect.yMin,
        parent.rect.yMax,
        exportContainerRect.anchorMin.y);
      float exportCenterX = NoteExportHeaderLayout.GetFollowingControlCenterX(
        addCenterInParent.x,
        addContainerRect.rect.width,
        exportContainerRect.rect.width,
        HorizontalControlGap);

      exportContainerRect.anchoredPosition = new Vector2(
        exportCenterX - anchorX,
        addCenterInParent.y - anchorY);
    }

    private static void ApplyTextOnlyButtonStyle(RectTransform exportContainerRect)
    {
      Sprite baseSprite = FindLoadedSprite(ExportBaseSpriteName);
      Sprite activeSprite = FindLoadedSprite(ExportActiveSpriteName);
      if (baseSprite == null || activeSprite == null)
      {
        PotatoPlugin.Log?.LogWarning(
          "[TodoExport] Text-only button sprites were unavailable.");
        return;
      }

      var baseImage = exportContainerRect.GetComponent<Image>();
      if (baseImage != null)
      {
        baseImage.sprite = baseSprite;
        baseImage.overrideSprite = null;
        baseImage.type = Image.Type.Sliced;
        baseImage.color = Color.white;
      }

      foreach (var image in exportContainerRect.GetComponentsInChildren<Image>(true))
      {
        if (image.transform == exportContainerRect)
        {
          continue;
        }

        image.sprite = activeSprite;
        image.overrideSprite = null;
        image.type = Image.Type.Sliced;
      }
    }

    private static Sprite FindLoadedSprite(string spriteName)
    {
      foreach (var sprite in Resources.FindObjectsOfTypeAll<Sprite>())
      {
        if (sprite != null &&
            string.Equals(sprite.name, spriteName, StringComparison.Ordinal))
        {
          return sprite;
        }
      }

      return null;
    }

    private static void SetHorizontalSize(RectTransform rectTransform, float width)
    {
      if (rectTransform != null)
      {
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
      }
    }

    private static void CopyRectTransform(RectTransform source, RectTransform destination)
    {
      destination.anchorMin = source.anchorMin;
      destination.anchorMax = source.anchorMax;
      destination.pivot = source.pivot;
      destination.sizeDelta = source.sizeDelta;
      destination.anchoredPosition = source.anchoredPosition;
      destination.localScale = Vector3.one;
      destination.localRotation = Quaternion.identity;
    }

    private static void CopyImage(Image source, Image destination)
    {
      if (source == null || destination == null)
      {
        return;
      }

      destination.sprite = source.sprite;
      destination.overrideSprite = source.overrideSprite;
      destination.type = source.type;
      destination.preserveAspect = source.preserveAspect;
      destination.fillCenter = source.fillCenter;
      destination.fillMethod = source.fillMethod;
      destination.fillAmount = source.fillAmount;
      destination.fillClockwise = source.fillClockwise;
      destination.fillOrigin = source.fillOrigin;
      destination.color = source.color;
      destination.material = source.material;
      destination.raycastTarget = source.raycastTarget;
    }

    internal static TodoListData GetCurrentList(TodoListUI todoListUI)
    {
      if (todoListUI == null ||
          SaveDataManager.Instance == null ||
          SaveDataManager.Instance.TodoAllData == null ||
          SaveDataManager.Instance.TodoAllData.TodoListDic == null)
      {
        return null;
      }

      SaveDataManager.Instance.TodoAllData.TodoListDic.TryGetValue(
        todoListUI.CurrentTodoListID,
        out TodoListData listData);
      return listData;
    }

    private static string GetText(string key)
    {
      try
      {
        var languageSupplier = ProjectLifetimeScope.Resolve<LanguageSupplier>();
        if (languageSupplier?.Language != null)
        {
          return ModTranslationManager.Get(key, languageSupplier.Language.CurrentValue);
        }
      }
      catch (Exception e)
      {
        PotatoPlugin.Log?.LogWarning(
          "[TodoExport] Failed to resolve language: " + e.Message);
      }

      return ModTranslationManager.Get(key, GameLanguageType.ChineseSimplified);
    }

    internal static ExportLanguage ResolveExportLanguage()
    {
      try
      {
        var languageSupplier = ProjectLifetimeScope.Resolve<LanguageSupplier>();
        if (languageSupplier?.Language != null)
        {
          return ModTranslationManager.ToExportLanguage(
            languageSupplier.Language.CurrentValue);
        }
      }
      catch (Exception e)
      {
        PotatoPlugin.Log?.LogWarning(
          "[TodoExport] Failed to resolve export language: " + e.Message);
      }

      return ExportLanguage.Chinese;
    }
  }

  internal sealed class TodoExportUiContext : MonoBehaviour
  {
    private TodoListUI _todoListUI;
    private Button _exportButton;
    private TodoExportManager _manager;

    public void Bind(TodoListUI todoListUI, Button exportButton)
    {
      if (!ReferenceEquals(_todoListUI, todoListUI))
      {
        _todoListUI = todoListUI;
        _manager = new TodoExportManager(
          () => TodoExportPatch.GetCurrentList(_todoListUI),
          new NoteExportFolderPicker(),
          TodoExportPatch.ResolveExportLanguage);
        _manager.OnExportSuccess += message =>
          PotatoPlugin.Log?.LogInfo("[TodoExport] " + message);
        _manager.OnExportFailure += message =>
          PotatoPlugin.Log?.LogWarning("[TodoExport] " + message);
      }

      if (!ReferenceEquals(_exportButton, exportButton))
      {
        _exportButton = exportButton;
        _exportButton.onClick.RemoveAllListeners();
        _exportButton.onClick.AddListener(HandleExport);
      }
    }

    private void HandleExport()
    {
      _manager?.ExportCurrentList();
    }
  }
}
