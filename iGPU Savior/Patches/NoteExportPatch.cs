using System;
using HarmonyLib;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Bulbul;
using NestopiSystem.DIContainers;
using PotatoOptimization.Core;
using PotatoOptimization.Features;
using PotatoOptimization.UI;

namespace PotatoOptimization.Patches
{
  [HarmonyPatch(typeof(NoteUI), nameof(NoteUI.Setup))]
  internal static class NoteExportPatch
  {
    private const string ExportButtonName = "PotatoNoteExportButton";
    private const string CancelButtonName = "PotatoNoteExportCancelButton";
    private const string ConfirmButtonName = "PotatoNoteExportConfirmButton";
    private const float StackedControlGap = 4f;

    private static NoteExportManager _manager;

    private static void Postfix(NoteUI __instance)
    {
      try
      {
        var manager = EnsureManager();
        if (manager == null || __instance == null)
        {
          return;
        }

        var context = __instance.GetComponent<NoteExportUiContext>();
        if (context == null)
        {
          context = __instance.gameObject.AddComponent<NoteExportUiContext>();
        }

        var selectPageListUI = __instance._selectPageListUI;
        var currentPageUI = __instance._currentPageUI;
        var addButton = selectPageListUI?._addSelectPageUIButton;
        var titleInput = currentPageUI?._titleInputField;
        var mainInput = currentPageUI?._mainInputField;

        if (addButton == null)
        {
          return;
        }

        var closeButton = __instance._noteCloseButton;
        var closeButtonRect = closeButton != null ? closeButton.transform as RectTransform : null;
        var buttonParent = closeButtonRect != null ? closeButtonRect.parent as RectTransform : null;
        var addButtonRect = addButton.transform as RectTransform;
        var noteListScrollRect = __instance.transform.Find("SelectNotes/Scroll View") as RectTransform;
        if (closeButtonRect == null || buttonParent == null || addButtonRect == null)
        {
          return;
        }

        context.Bind(manager, addButton, titleInput, mainInput);
        EnsureButtons(
          context,
          addButton,
          buttonParent,
          closeButtonRect,
          addButtonRect,
          noteListScrollRect);
        context.Refresh();
      }
      catch (Exception e)
      {
        PotatoPlugin.Log?.LogError("[NoteExport] Failed to inject note export UI: " + e);
      }
    }

    internal static NoteExportManager GetManager()
    {
      return EnsureManager();
    }

    private static NoteExportManager EnsureManager()
    {
      try
      {
        var noteService = RoomLifetimeScope.Resolve<NoteService>();
        if (noteService == null)
        {
          return null;
        }

        if (NoteExportManager.ShouldReuseManagerForService(_manager, noteService))
        {
          return _manager;
        }

        if (_manager != null && _manager.IsSelectionMode)
        {
          _manager.ExitSelectionMode();
        }

        _manager = new NoteExportManager(noteService, new NoteExportFolderPicker());
        _manager.OnExportSuccess += message => PotatoPlugin.Log?.LogInfo("[NoteExport] " + message);
        _manager.OnExportFailure += message => PotatoPlugin.Log?.LogWarning("[NoteExport] " + message);
      }
      catch (Exception e)
      {
        PotatoPlugin.Log?.LogError("[NoteExport] Failed to initialize manager: " + e);
      }

      return _manager;
    }

    private static void EnsureButtons(
      NoteExportUiContext context,
      Button addButton,
      RectTransform buttonParent,
      RectTransform closeButtonRect,
      RectTransform addButtonRect,
      RectTransform noteListScrollRect)
    {
      if (context.ExportButton == null)
      {
        context.ExportButton = MakeHeaderButton(addButton, buttonParent, closeButtonRect, ExportButtonName, () =>
        {
          var manager = EnsureManager();
          if (manager != null) manager.EnterSelectionMode();
        });
      }

      if (context.CancelButton == null)
      {
        context.CancelButton = MakeHeaderButton(addButton, buttonParent, closeButtonRect, CancelButtonName, () =>
        {
          var manager = EnsureManager();
          if (manager != null) manager.ExitSelectionMode();
        });
      }

      if (context.ConfirmButton == null)
      {
        context.ConfirmButton = MakeHeaderButton(addButton, buttonParent, closeButtonRect, ConfirmButtonName, () =>
        {
          var manager = EnsureManager();
          if (manager != null) manager.ExportSelected();
        });
      }

      LayoutHeaderButtons(
        context,
        addButtonRect,
        noteListScrollRect);
    }

    private static Button MakeHeaderButton(Button template, RectTransform parent, RectTransform closeButtonRect, string name, UnityAction onClick)
    {
      var buttonObject = UnityEngine.Object.Instantiate(template.gameObject, parent);
      buttonObject.name = name;

      var rect = buttonObject.transform as RectTransform;
      var templateRect = template.transform as RectTransform;
      if (rect != null && templateRect != null && closeButtonRect != null)
      {
        rect.anchorMin = closeButtonRect.anchorMin;
        rect.anchorMax = closeButtonRect.anchorMax;
        rect.pivot = closeButtonRect.pivot;
        rect.sizeDelta = templateRect.sizeDelta;
        rect.localScale = Vector3.one;
      }

      DisableLocalization(buttonObject);

      var button = buttonObject.GetComponent<Button>();
      if (button == null) return null;

      button.onClick.RemoveAllListeners();
      if (onClick != null) button.onClick.AddListener(onClick);

      return button;
    }

    private static void LayoutHeaderButtons(
      NoteExportUiContext context,
      RectTransform addButtonRect,
      RectTransform noteListScrollRect)
    {
      var exportRect = context.ExportButton.transform as RectTransform;
      var cancelRect = context.CancelButton.transform as RectTransform;
      var confirmRect = context.ConfirmButton.transform as RectTransform;
      if (exportRect == null || cancelRect == null || confirmRect == null || addButtonRect == null)
        return;

      PlaceButtonAtAddButton(cancelRect, addButtonRect);
      PlaceExportButtonBelowAddButton(exportRect, addButtonRect, StackedControlGap);
      PlaceExportButtonBelowAddButton(confirmRect, addButtonRect, StackedControlGap);
      ReserveListSpaceBelowExportButton(exportRect, addButtonRect, noteListScrollRect, StackedControlGap);
    }

    private static void PlaceButtonAtAddButton(
      RectTransform buttonRect,
      RectTransform addButtonRect)
    {
      var parent = buttonRect.parent as RectTransform;
      if (parent == null) return;

      var addCenterInParent = parent.InverseTransformPoint(addButtonRect.TransformPoint(addButtonRect.rect.center));
      float anchorX = Mathf.Lerp(parent.rect.xMin, parent.rect.xMax, buttonRect.anchorMin.x);
      float anchorY = Mathf.Lerp(parent.rect.yMin, parent.rect.yMax, buttonRect.anchorMin.y);
      buttonRect.anchoredPosition = new Vector2(
        addCenterInParent.x - anchorX,
        addCenterInParent.y - anchorY);
    }

    private static void PlaceExportButtonBelowAddButton(
      RectTransform exportRect,
      RectTransform addButtonRect,
      float gap)
    {
      var parent = exportRect.parent as RectTransform;
      if (parent == null) return;

      var addCenterInParent = parent.InverseTransformPoint(addButtonRect.TransformPoint(addButtonRect.rect.center));
      float anchorX = Mathf.Lerp(parent.rect.xMin, parent.rect.xMax, exportRect.anchorMin.x);
      float anchorY = Mathf.Lerp(parent.rect.yMin, parent.rect.yMax, exportRect.anchorMin.y);
      float exportCenterY = NoteExportHeaderLayout.GetStackedControlCenterY(
        addCenterInParent.y,
        addButtonRect.rect.height,
        exportRect.rect.height,
        gap);

      exportRect.anchoredPosition = new Vector2(
        addCenterInParent.x - anchorX,
        exportCenterY - anchorY);
    }

    private static void ReserveListSpaceBelowExportButton(
      RectTransform exportRect,
      RectTransform addButtonRect,
      RectTransform noteListScrollRect,
      float gap)
    {
      var scrollParent = noteListScrollRect != null
        ? noteListScrollRect.parent as RectTransform
        : null;
      if (scrollParent == null) return;

      float exportCenterY = NoteExportHeaderLayout.GetStackedControlCenterY(
        addButtonRect.rect.center.y,
        addButtonRect.rect.height,
        exportRect.rect.height,
        gap);
      float contentTopY = NoteExportHeaderLayout.GetFollowingContentTopY(
        exportCenterY,
        exportRect.rect.height,
        gap);
      var desiredTopWorld = addButtonRect.TransformPoint(new Vector3(
        addButtonRect.rect.center.x,
        contentTopY,
        0f));
      float desiredTopInParent = scrollParent.InverseTransformPoint(desiredTopWorld).y;

      var offsetMax = noteListScrollRect.offsetMax;
      offsetMax.y = desiredTopInParent - scrollParent.rect.yMax;
      noteListScrollRect.offsetMax = offsetMax;
    }

    private static Button CloneActionButton(Button template, string name, UnityAction onClick)
    {
      var buttonObject = UnityEngine.Object.Instantiate(template.gameObject, template.transform.parent);
      buttonObject.name = name;
      DisableLocalization(buttonObject);

      var button = buttonObject.GetComponent<Button>();
      if (button == null)
      {
        return null;
      }

      button.onClick.RemoveAllListeners();
      if (onClick != null)
      {
        button.onClick.AddListener(onClick);
      }

      return button;
    }

    internal static void SetButtonText(Button button, string text)
    {
      if (button == null)
      {
        return;
      }

      var texts = button.GetComponentsInChildren<TMP_Text>(true);
      foreach (var item in texts)
      {
        item.text = text;
      }
    }

    internal static string GetText(string key)
    {
      return ModTranslationManager.Get(key, ResolveCurrentLanguage());
    }

    internal static string FormatConfirmText(int count)
    {
      var template = GetText("NOTE_EXPORT_CONFIRM");
      if (template.IndexOf("{0}", StringComparison.Ordinal) >= 0)
      {
        return string.Format(template, count);
      }

      return template + "(" + count + ")";
    }

    private static GameLanguageType ResolveCurrentLanguage()
    {
      try
      {
        var languageSupplier = ProjectLifetimeScope.Resolve<LanguageSupplier>();
        if (languageSupplier != null && languageSupplier.Language != null)
        {
          return languageSupplier.Language.CurrentValue;
        }
      }
      catch (Exception e)
      {
        PotatoPlugin.Log?.LogWarning("[NoteExport] Failed to resolve language: " + e.Message);
      }

      return GameLanguageType.ChineseSimplified;
    }

    private static void DisableLocalization(GameObject gameObject)
    {
      try
      {
        var localizers = gameObject.GetComponentsInChildren<TextLocalizationBehaviour>(true);
        foreach (var localizer in localizers)
        {
          localizer.enabled = false;
        }
      }
      catch (Exception e)
      {
        PotatoPlugin.Log?.LogWarning("[NoteExport] Failed to disable button localization: " + e.Message);
      }
    }
  }

  [HarmonyPatch(typeof(SelectPageUI), nameof(SelectPageUI.Setup))]
  internal static class NoteExportSelectPagePatch
  {
    private static void Postfix(SelectPageUI __instance)
    {
      try
      {
        var manager = NoteExportPatch.GetManager();
        if (__instance == null || manager == null)
        {
          return;
        }

        var card = __instance.GetComponent<NoteExportCardState>();
        if (card == null)
        {
          card = __instance.gameObject.AddComponent<NoteExportCardState>();
        }

        var titleInput = __instance._titleInputField;
        var selectPageButton = __instance._selectPageButton;
        var reorderTrigger = __instance.reorderTrigger;
        var dragUICanvasGroup = __instance._dragUICanvasGroup;
        var removePageButton = __instance._removePageButton;

        card.Bind(manager, __instance, titleInput, selectPageButton, reorderTrigger, dragUICanvasGroup, removePageButton);
        card.Refresh();
      }
      catch (Exception e)
      {
        PotatoPlugin.Log?.LogError("[NoteExport] Failed to inject card export UI: " + e);
      }
    }
  }

  [HarmonyPatch(typeof(SelectPageUI), nameof(SelectPageUI.SelectPage))]
  internal static class NoteExportSelectPageGuardPatch
  {
    private static bool Prefix(SelectPageUI __instance)
    {
      var manager = NoteExportPatch.GetManager();
      if (manager == null || !manager.IsSelectionMode || __instance == null)
      {
        return true;
      }

      manager.ToggleSelection(__instance.PageID);
      return false;
    }
  }

  [HarmonyPatch(typeof(SelectPageUI), nameof(SelectPageUI.EditTitle))]
  internal static class NoteExportEditTitleGuardPatch
  {
    private static bool Prefix()
    {
      var manager = NoteExportPatch.GetManager();
      return manager == null || !manager.IsSelectionMode;
    }
  }

  [HarmonyPatch(typeof(SelectPageListUI), "OnStartReorder")]
  internal static class NoteExportStartReorderGuardPatch
  {
    private static bool Prefix()
    {
      var manager = NoteExportPatch.GetManager();
      return manager == null || !manager.IsSelectionMode;
    }
  }

  [HarmonyPatch(typeof(SelectPageListUI), "OnDragReorder")]
  internal static class NoteExportDragReorderGuardPatch
  {
    private static bool Prefix()
    {
      var manager = NoteExportPatch.GetManager();
      return manager == null || !manager.IsSelectionMode;
    }
  }

  [HarmonyPatch(typeof(SelectPageListUI), "OnEndReorder")]
  internal static class NoteExportEndReorderGuardPatch
  {
    private static bool Prefix()
    {
      var manager = NoteExportPatch.GetManager();
      return manager == null || !manager.IsSelectionMode;
    }
  }

  internal sealed class NoteExportUiContext : MonoBehaviour
  {
    private NoteExportManager _manager;
    private TMP_InputField _titleInputField;
    private TMP_InputField _mainInputField;
    private bool _isBound;
    private bool _hasOriginalAddButtonState;
    private bool _originalAddButtonActive;
    private bool _hasOriginalTitleInputState;
    private bool _originalTitleInputInteractable;
    private bool _hasOriginalMainInputState;
    private bool _originalMainInputInteractable;

    public Button ExportButton { get; set; }
    public Button CancelButton { get; set; }
    public Button ConfirmButton { get; set; }
    public Button AddButton { get; private set; }

    private IDisposable _languageSubscription;

    public void Bind(NoteExportManager manager, Button addButton, TMP_InputField titleInputField, TMP_InputField mainInputField)
    {
      var captureAddButtonState = NoteExportStateCapturePolicy.ShouldCaptureOriginalState(
        manager != null && manager.IsSelectionMode,
        _hasOriginalAddButtonState,
        ReferenceEquals(AddButton, addButton));
      var captureTitleInputState = NoteExportStateCapturePolicy.ShouldCaptureOriginalState(
        manager != null && manager.IsSelectionMode,
        _hasOriginalTitleInputState,
        ReferenceEquals(_titleInputField, titleInputField));
      var captureMainInputState = NoteExportStateCapturePolicy.ShouldCaptureOriginalState(
        manager != null && manager.IsSelectionMode,
        _hasOriginalMainInputState,
        ReferenceEquals(_mainInputField, mainInputField));

      _manager = NoteExportManager.RebindManagerSubscriptions(
        _manager,
        manager,
        HandleSelectionModeChanged,
        HandleSelectionChanged);
      _isBound = _manager != null;

      AddButton = addButton;
      _titleInputField = titleInputField;
      _mainInputField = mainInputField;
      CaptureOriginalStates(captureAddButtonState, captureTitleInputState, captureMainInputState);
      TrySubscribeLanguage();
    }

    public void Refresh()
    {
      UpdateButtons();
      UpdateInputs();
    }

    private void HandleSelectionModeChanged(bool _)
    {
      Refresh();
    }

    private void HandleSelectionChanged(System.Collections.Generic.IReadOnlyCollection<ulong> _)
    {
      UpdateButtons();
    }

    private void UpdateButtons()
    {
      if (_manager == null || AddButton == null || ExportButton == null || CancelButton == null || ConfirmButton == null)
      {
        return;
      }

      var isSelectionMode = _manager.IsSelectionMode;
      AddButton.gameObject.SetActive(isSelectionMode ? false : GetOriginalAddButtonActive());
      ExportButton.gameObject.SetActive(!isSelectionMode);
      CancelButton.gameObject.SetActive(isSelectionMode);
      ConfirmButton.gameObject.SetActive(isSelectionMode);

      NoteExportPatch.SetButtonText(ExportButton, NoteExportPatch.GetText("NOTE_EXPORT_BUTTON"));
      NoteExportPatch.SetButtonText(CancelButton, NoteExportPatch.GetText("NOTE_EXPORT_CANCEL"));
      NoteExportPatch.SetButtonText(ConfirmButton, NoteExportPatch.FormatConfirmText(_manager.SelectedCount));
      ConfirmButton.interactable = _manager.SelectedCount > 0;
    }

    private void UpdateInputs()
    {
      if (_manager == null)
      {
        return;
      }

      if (_titleInputField != null)
      {
        _titleInputField.interactable = _manager.IsSelectionMode ? false : GetOriginalTitleInputInteractable();
      }

      if (_mainInputField != null)
      {
        _mainInputField.interactable = _manager.IsSelectionMode ? false : GetOriginalMainInputInteractable();
      }
    }

    private void OnDestroy()
    {
      if (_manager != null && _manager.IsSelectionMode)
      {
        _manager.ExitSelectionMode();
      }

      _manager = NoteExportManager.RebindManagerSubscriptions(
        _manager,
        null,
        HandleSelectionModeChanged,
        HandleSelectionChanged);
      _isBound = false;

      _languageSubscription?.Dispose();
    }

    private void TrySubscribeLanguage()
    {
      if (_languageSubscription != null) return;
      try
      {
        var languageSupplier = ProjectLifetimeScope.Resolve<LanguageSupplier>();
        if (languageSupplier?.Language != null)
        {
          _languageSubscription = languageSupplier.Language.Subscribe(_ => UpdateButtons());
        }
      }
      catch (Exception e)
      {
        PotatoPlugin.Log?.LogWarning("[NoteExport] Language subscription failed: " + e.Message);
      }
    }

    private void CaptureOriginalStates(bool captureAddButtonState, bool captureTitleInputState, bool captureMainInputState)
    {
      if (captureAddButtonState && AddButton != null)
      {
        _originalAddButtonActive = AddButton.gameObject.activeSelf;
        _hasOriginalAddButtonState = true;
      }

      if (captureTitleInputState && _titleInputField != null)
      {
        _originalTitleInputInteractable = _titleInputField.interactable;
        _hasOriginalTitleInputState = true;
      }

      if (captureMainInputState && _mainInputField != null)
      {
        _originalMainInputInteractable = _mainInputField.interactable;
        _hasOriginalMainInputState = true;
      }
    }

    private bool GetOriginalAddButtonActive()
    {
      return !_hasOriginalAddButtonState || _originalAddButtonActive;
    }

    private bool GetOriginalTitleInputInteractable()
    {
      return !_hasOriginalTitleInputState || _originalTitleInputInteractable;
    }

    private bool GetOriginalMainInputInteractable()
    {
      return !_hasOriginalMainInputState || _originalMainInputInteractable;
    }
  }

  internal sealed class NoteExportCardState : MonoBehaviour
  {
    private const string OverlayName = "PotatoNoteExportOverlay";
    private const string CheckboxName = "PotatoNoteExportCheckbox";
    private const string CheckboxBaseName = "BaseCircle";
    private const string CheckboxCheckName = "Check";
    private const string CheckboxBaseSpriteName = "t_ui_todo_circle_anim_8";
    private const string CheckboxCheckSpriteName = "t_ui_todo_circle_check";
    private const float CheckboxSize = 32f;

    private static Sprite _checkboxBaseSprite;
    private static Sprite _checkboxCheckSprite;

    private NoteExportManager _manager;
    private SelectPageUI _pageUI;
    private TMP_InputField _titleInputField;
    private Button _selectPageButton;
    private EventTrigger _reorderTrigger;
    private CanvasGroup _dragUICanvasGroup;
    private Button _removePageButton;
    private Button _overlayButton;
    private Image _checkboxBaseImage;
    private Image _checkboxCheckImage;
    private bool _isBound;
    private bool _hasOriginalTitleInputState;
    private bool _originalTitleInputInteractable;
    private bool _hasOriginalSelectPageButtonState;
    private bool _originalSelectPageButtonInteractable;
    private bool _hasOriginalReorderTriggerState;
    private bool _originalReorderTriggerEnabled;
    private bool _hasOriginalDragUiActiveState;
    private bool _originalDragUiActive;
    private bool _hasOriginalRemoveButtonState;
    private bool _originalRemoveButtonActive;

    public void Bind(
      NoteExportManager manager,
      SelectPageUI pageUI,
      TMP_InputField titleInputField,
      Button selectPageButton,
      EventTrigger reorderTrigger,
      CanvasGroup dragUICanvasGroup,
      Button removePageButton)
    {
      var captureTitleInputState = NoteExportStateCapturePolicy.ShouldCaptureOriginalState(
        manager != null && manager.IsSelectionMode,
        _hasOriginalTitleInputState,
        ReferenceEquals(_titleInputField, titleInputField));
      var captureSelectPageButtonState = NoteExportStateCapturePolicy.ShouldCaptureOriginalState(
        manager != null && manager.IsSelectionMode,
        _hasOriginalSelectPageButtonState,
        ReferenceEquals(_selectPageButton, selectPageButton));
      var captureReorderTriggerState = NoteExportStateCapturePolicy.ShouldCaptureOriginalState(
        manager != null && manager.IsSelectionMode,
        _hasOriginalReorderTriggerState,
        ReferenceEquals(_reorderTrigger, reorderTrigger));
      var captureDragUiState = NoteExportStateCapturePolicy.ShouldCaptureOriginalState(
        manager != null && manager.IsSelectionMode,
        _hasOriginalDragUiActiveState,
        ReferenceEquals(_dragUICanvasGroup, dragUICanvasGroup));
      var captureRemoveButtonState = NoteExportStateCapturePolicy.ShouldCaptureOriginalState(
        manager != null && manager.IsSelectionMode,
        _hasOriginalRemoveButtonState,
        ReferenceEquals(_removePageButton, removePageButton));

      _manager = NoteExportManager.RebindManagerSubscriptions(
        _manager,
        manager,
        HandleManagerChanged,
        HandleSelectionChanged);
      _isBound = _manager != null;

      _pageUI = pageUI;
      _titleInputField = titleInputField;
      _selectPageButton = selectPageButton;
      _reorderTrigger = reorderTrigger;
      _dragUICanvasGroup = dragUICanvasGroup;
      _removePageButton = removePageButton;

      CaptureOriginalStates(
        captureTitleInputState,
        captureSelectPageButtonState,
        captureReorderTriggerState,
        captureDragUiState,
        captureRemoveButtonState);
      EnsureOverlay();
    }

    public void Refresh()
    {
      if (_manager == null || _pageUI == null)
      {
        return;
      }

      var isSelectionMode = _manager.IsSelectionMode;
      var isSelected = _manager.IsSelected(_pageUI.PageID);

      if (_overlayButton != null)
      {
        _overlayButton.gameObject.SetActive(isSelectionMode);
      }

      if (_checkboxCheckImage != null)
      {
        _checkboxCheckImage.gameObject.SetActive(isSelected);
      }

      if (_removePageButton != null)
      {
        _removePageButton.gameObject.SetActive(isSelectionMode ? false : GetOriginalRemoveButtonActive());
      }

      if (_reorderTrigger != null)
      {
        _reorderTrigger.enabled = isSelectionMode ? false : GetOriginalReorderTriggerEnabled();
      }

      if (_dragUICanvasGroup != null)
      {
        _dragUICanvasGroup.gameObject.SetActive(isSelectionMode ? false : GetOriginalDragUiActive());
      }

      if (_selectPageButton != null)
      {
        _selectPageButton.interactable = isSelectionMode ? false : GetOriginalSelectPageButtonInteractable();
      }

      if (_titleInputField != null)
      {
        _titleInputField.interactable = isSelectionMode ? false : GetOriginalTitleInputInteractable();
      }
    }

    private void HandleManagerChanged(bool _)
    {
      Refresh();
    }

    private void HandleSelectionChanged(System.Collections.Generic.IReadOnlyCollection<ulong> _)
    {
      Refresh();
    }

    private void EnsureOverlay()
    {
      if (_overlayButton != null && _checkboxBaseImage != null && _checkboxCheckImage != null)
      {
        var existingOverlayRect = _overlayButton.transform as RectTransform;
        var existingCheckboxRect = _checkboxBaseImage.transform.parent as RectTransform;
        LayoutCheckboxRect(existingOverlayRect, existingCheckboxRect);
        return;
      }

      var overlayTransform = transform.Find(OverlayName);
      GameObject overlayObject;
      if (overlayTransform != null)
      {
        overlayObject = overlayTransform.gameObject;
      }
      else
      {
        overlayObject = new GameObject(OverlayName, typeof(RectTransform), typeof(Image), typeof(Button));
        overlayObject.transform.SetParent(transform, false);
      }

      var overlayRect = overlayObject.GetComponent<RectTransform>();
      overlayRect.anchorMin = Vector2.zero;
      overlayRect.anchorMax = Vector2.one;
      overlayRect.offsetMin = Vector2.zero;
      overlayRect.offsetMax = Vector2.zero;

      var overlayImage = overlayObject.GetComponent<Image>();
      overlayImage.color = new Color(1f, 1f, 1f, 0.001f);
      overlayImage.raycastTarget = true;

      _overlayButton = overlayObject.GetComponent<Button>();
      _overlayButton.transition = Selectable.Transition.None;
      _overlayButton.onClick.RemoveAllListeners();
      _overlayButton.onClick.AddListener(() =>
      {
        if (_manager != null && _pageUI != null)
        {
          _manager.ToggleSelection(_pageUI.PageID);
        }
      });

      var checkboxTransform = overlayObject.transform.Find(CheckboxName);
      GameObject checkboxObject;
      if (checkboxTransform != null)
      {
        checkboxObject = checkboxTransform.gameObject;
      }
      else
      {
        checkboxObject = new GameObject(CheckboxName, typeof(RectTransform));
        checkboxObject.transform.SetParent(overlayObject.transform, false);
      }

      var checkboxRect = checkboxObject.GetComponent<RectTransform>();
      LayoutCheckboxRect(overlayRect, checkboxRect);

      ResolveCheckboxSprites();
      _checkboxBaseImage = EnsureCheckboxImage(
        checkboxObject.transform,
        CheckboxBaseName,
        _checkboxBaseSprite);
      _checkboxCheckImage = EnsureCheckboxImage(
        checkboxObject.transform,
        CheckboxCheckName,
        _checkboxCheckSprite);
      _checkboxCheckImage.gameObject.SetActive(false);

      var oldLabel = checkboxObject.transform.Find("Label");
      if (oldLabel != null)
      {
        oldLabel.gameObject.SetActive(false);
      }

      overlayObject.transform.SetAsLastSibling();
    }

    private static Image EnsureCheckboxImage(Transform parent, string name, Sprite sprite)
    {
      var imageTransform = parent.Find(name);
      GameObject imageObject;
      if (imageTransform != null)
      {
        imageObject = imageTransform.gameObject;
      }
      else
      {
        imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
      }

      var rect = imageObject.GetComponent<RectTransform>();
      rect.anchorMin = Vector2.zero;
      rect.anchorMax = Vector2.one;
      rect.offsetMin = Vector2.zero;
      rect.offsetMax = Vector2.zero;

      var image = imageObject.GetComponent<Image>();
      image.sprite = sprite;
      image.color = Color.white;
      image.preserveAspect = true;
      image.raycastTarget = false;
      return image;
    }

    private static void ResolveCheckboxSprites()
    {
      if (_checkboxBaseSprite != null && _checkboxCheckSprite != null) return;

      var sprites = Resources.FindObjectsOfTypeAll<Sprite>();
      foreach (var sprite in sprites)
      {
        if (sprite == null) continue;
        if (_checkboxBaseSprite == null && sprite.name == CheckboxBaseSpriteName)
        {
          _checkboxBaseSprite = sprite;
        }
        else if (_checkboxCheckSprite == null && sprite.name == CheckboxCheckSpriteName)
        {
          _checkboxCheckSprite = sprite;
        }

        if (_checkboxBaseSprite != null && _checkboxCheckSprite != null) break;
      }
    }

    private void CaptureOriginalStates(
      bool captureTitleInputState,
      bool captureSelectPageButtonState,
      bool captureReorderTriggerState,
      bool captureDragUiState,
      bool captureRemoveButtonState)
    {
      if (captureTitleInputState && _titleInputField != null)
      {
        _originalTitleInputInteractable = _titleInputField.interactable;
        _hasOriginalTitleInputState = true;
      }

      if (captureSelectPageButtonState && _selectPageButton != null)
      {
        _originalSelectPageButtonInteractable = _selectPageButton.interactable;
        _hasOriginalSelectPageButtonState = true;
      }

      if (captureReorderTriggerState && _reorderTrigger != null)
      {
        _originalReorderTriggerEnabled = _reorderTrigger.enabled;
        _hasOriginalReorderTriggerState = true;
      }

      if (captureDragUiState && _dragUICanvasGroup != null)
      {
        _originalDragUiActive = _dragUICanvasGroup.gameObject.activeSelf;
        _hasOriginalDragUiActiveState = true;
      }

      if (captureRemoveButtonState && _removePageButton != null)
      {
        _originalRemoveButtonActive = _removePageButton.gameObject.activeSelf;
        _hasOriginalRemoveButtonState = true;
      }
    }

    private void LayoutCheckboxRect(RectTransform overlayRect, RectTransform checkboxRect)
    {
      var dragRect = _dragUICanvasGroup != null ? _dragUICanvasGroup.transform as RectTransform : null;
      if (overlayRect == null || checkboxRect == null || dragRect == null)
      {
        if (checkboxRect != null)
        {
          checkboxRect.anchorMin = new Vector2(0f, 1f);
          checkboxRect.anchorMax = new Vector2(0f, 1f);
          checkboxRect.pivot = new Vector2(0.5f, 0.5f);
          checkboxRect.sizeDelta = new Vector2(CheckboxSize, CheckboxSize);
          checkboxRect.anchoredPosition = new Vector2(20f, -20f);
        }
        return;
      }

      var localCenter = overlayRect.InverseTransformPoint(dragRect.TransformPoint(dragRect.rect.center));

      checkboxRect.anchorMin = new Vector2(0.5f, 0.5f);
      checkboxRect.anchorMax = new Vector2(0.5f, 0.5f);
      checkboxRect.pivot = new Vector2(0.5f, 0.5f);
      checkboxRect.sizeDelta = new Vector2(CheckboxSize, CheckboxSize);
      checkboxRect.anchoredPosition = new Vector2(localCenter.x, localCenter.y);
    }

    private bool GetOriginalTitleInputInteractable()
    {
      return !_hasOriginalTitleInputState || _originalTitleInputInteractable;
    }

    private bool GetOriginalSelectPageButtonInteractable()
    {
      return !_hasOriginalSelectPageButtonState || _originalSelectPageButtonInteractable;
    }

    private bool GetOriginalReorderTriggerEnabled()
    {
      return !_hasOriginalReorderTriggerState || _originalReorderTriggerEnabled;
    }

    private bool GetOriginalDragUiActive()
    {
      return !_hasOriginalDragUiActiveState || _originalDragUiActive;
    }

    private bool GetOriginalRemoveButtonActive()
    {
      return !_hasOriginalRemoveButtonState || _originalRemoveButtonActive;
    }

    private void OnDestroy()
    {
      _manager = NoteExportManager.RebindManagerSubscriptions(
        _manager,
        null,
        HandleManagerChanged,
        HandleSelectionChanged);
      _isBound = false;
    }
  }
}
