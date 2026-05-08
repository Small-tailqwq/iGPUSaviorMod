using System;
using System.Reflection;
using HarmonyLib;
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
    private const string NoteTitleTextName = "NoteTitleText";
    private const float HeaderButtonGap = 6f;

    private static readonly FieldInfo FI_SelectPageListUI = AccessTools.Field(typeof(NoteUI), "_selectPageListUI");
    private static readonly FieldInfo FI_CurrentPageUI = AccessTools.Field(typeof(NoteUI), "_currentPageUI");
    private static readonly FieldInfo FI_AddSelectPageUIButton = AccessTools.Field(typeof(SelectPageListUI), "_addSelectPageUIButton");
    private static readonly FieldInfo FI_TitleInputField = AccessTools.Field(typeof(CurrentPageUI), "_titleInputField");
    private static readonly FieldInfo FI_MainInputField = AccessTools.Field(typeof(CurrentPageUI), "_mainInputField");

    private static NoteExportManager _manager;

    private static readonly FieldInfo FI_NoteCloseButton = AccessTools.Field(typeof(NoteUI), "_noteCloseButton");

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

        var selectPageListUI = FI_SelectPageListUI.GetValue(__instance) as SelectPageListUI;
        var currentPageUI = FI_CurrentPageUI.GetValue(__instance) as CurrentPageUI;
        var addButton = selectPageListUI != null ? FI_AddSelectPageUIButton.GetValue(selectPageListUI) as Button : null;
        var titleInput = currentPageUI != null ? FI_TitleInputField.GetValue(currentPageUI) as TMP_InputField : null;
        var mainInput = currentPageUI != null ? FI_MainInputField.GetValue(currentPageUI) as TMP_InputField : null;

        if (addButton == null)
        {
          return;
        }

        var headerAnchor = FindNoteTitleText(__instance.transform);
        var closeButton = FI_NoteCloseButton.GetValue(__instance) as Button;
        var closeButtonRect = closeButton != null ? closeButton.transform as RectTransform : null;
        var buttonParent = closeButtonRect != null ? closeButtonRect.parent as RectTransform : null;
        if (headerAnchor == null || closeButtonRect == null || buttonParent == null)
        {
          return;
        }

        context.Bind(manager, addButton, titleInput, mainInput);
        EnsureButtons(context, addButton, buttonParent, headerAnchor, closeButtonRect);
        context.Refresh();
      }
      catch (Exception e)
      {
        PotatoPlugin.Log?.LogError("[NoteExport] Failed to inject note export UI: " + e);
      }
    }

    private static RectTransform FindNoteTitleText(Transform noteUITransform)
    {
      var t = noteUITransform.Find(NoteTitleTextName);
      if (t != null) return t as RectTransform;

      if (noteUITransform.parent != null)
      {
        t = noteUITransform.parent.Find(NoteTitleTextName);
        if (t != null) return t as RectTransform;
      }

      return null;
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

    private static void EnsureButtons(NoteExportUiContext context, Button addButton, RectTransform buttonParent, RectTransform headerAnchor, RectTransform closeButtonRect)
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

      LayoutHeaderButtons(context, headerAnchor, closeButtonRect);
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

    private static void LayoutHeaderButtons(NoteExportUiContext context, RectTransform headerAnchor, RectTransform closeButtonRect)
    {
      var exportRect = context.ExportButton.transform as RectTransform;
      var cancelRect = context.CancelButton.transform as RectTransform;
      var confirmRect = context.ConfirmButton.transform as RectTransform;
      if (exportRect == null || cancelRect == null || confirmRect == null || headerAnchor == null || closeButtonRect == null) return;

      float buttonY = NoteExportHeaderLayout.GetButtonY(headerAnchor.anchoredPosition.y);
      float closeButtonWidth = closeButtonRect.rect.width > 0f ? closeButtonRect.rect.width : closeButtonRect.sizeDelta.x;
      float buttonWidth = exportRect.rect.width > 0f ? exportRect.rect.width : exportRect.sizeDelta.x;
      float exportButtonX = NoteExportHeaderLayout.GetPrimaryButtonX(closeButtonRect.anchoredPosition.x, closeButtonWidth, buttonWidth, HeaderButtonGap);

      exportRect.anchoredPosition = new Vector2(exportButtonX, buttonY);
      cancelRect.anchoredPosition = new Vector2(exportButtonX, buttonY);
      confirmRect.anchoredPosition = new Vector2(NoteExportHeaderLayout.GetSecondaryButtonX(exportButtonX, buttonWidth, HeaderButtonGap), buttonY);
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
        var localizerType = Type.GetType("Bulbul.TextLocalizationBehaviour, Assembly-CSharp");
        if (localizerType == null)
        {
          return;
        }

        var localizers = gameObject.GetComponentsInChildren(localizerType, true);
        foreach (var localizer in localizers)
        {
          var component = localizer as Behaviour;
          if (component != null)
          {
            component.enabled = false;
          }
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
    private static readonly FieldInfo FI_TitleInputField = AccessTools.Field(typeof(SelectPageUI), "_titleInputField");
    private static readonly FieldInfo FI_SelectPageButton = AccessTools.Field(typeof(SelectPageUI), "_selectPageButton");
    private static readonly FieldInfo FI_ReorderTrigger = AccessTools.Field(typeof(SelectPageUI), "reorderTrigger");
    private static readonly FieldInfo FI_DragUICanvasGroup = AccessTools.Field(typeof(SelectPageUI), "_dragUICanvasGroup");
    private static readonly FieldInfo FI_RemovePageButton = AccessTools.Field(typeof(SelectPageUI), "_removePageButton");

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

        var titleInput = FI_TitleInputField.GetValue(__instance) as TMP_InputField;
        var selectPageButton = FI_SelectPageButton.GetValue(__instance) as Button;
        var reorderTrigger = FI_ReorderTrigger.GetValue(__instance) as EventTrigger;
        var dragUICanvasGroup = FI_DragUICanvasGroup.GetValue(__instance) as CanvasGroup;
        var removePageButton = FI_RemovePageButton.GetValue(__instance) as Button;

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

    private NoteExportManager _manager;
    private SelectPageUI _pageUI;
    private TMP_InputField _titleInputField;
    private Button _selectPageButton;
    private EventTrigger _reorderTrigger;
    private CanvasGroup _dragUICanvasGroup;
    private Button _removePageButton;
    private Button _overlayButton;
    private TMP_Text _checkboxText;
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

      if (_checkboxText != null)
      {
        _checkboxText.text = isSelected ? "[x]" : "[ ]";
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
      if (_overlayButton != null && _checkboxText != null)
      {
        var existingOverlayRect = _overlayButton.transform as RectTransform;
        var existingCheckboxRect = _checkboxText.transform.parent as RectTransform;
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

      _checkboxText = checkboxObject.GetComponent<TMP_Text>();
      if (_checkboxText == null)
      {
        var templateText = _titleInputField != null ? _titleInputField.textComponent : null;
        if (templateText != null)
        {
          _checkboxText = UnityEngine.Object.Instantiate(templateText, checkboxObject.transform);
          _checkboxText.rectTransform.anchorMin = Vector2.zero;
          _checkboxText.rectTransform.anchorMax = Vector2.one;
          _checkboxText.rectTransform.offsetMin = Vector2.zero;
          _checkboxText.rectTransform.offsetMax = Vector2.zero;
        }
        else
        {
          _checkboxText = checkboxObject.AddComponent<TextMeshProUGUI>();
        }
      }

      _checkboxText.name = "Label";
      _checkboxText.alignment = TextAlignmentOptions.Center;
      _checkboxText.raycastTarget = false;
      _checkboxText.text = "[ ]";

      overlayObject.transform.SetAsLastSibling();
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
          checkboxRect.pivot = new Vector2(0f, 1f);
          checkboxRect.sizeDelta = new Vector2(40f, 24f);
          checkboxRect.anchoredPosition = new Vector2(20f, -12f);
        }
        return;
      }

      float dragWidth = dragRect.rect.width > 0f ? dragRect.rect.width : dragRect.sizeDelta.x;
      float dragHeight = dragRect.rect.height > 0f ? dragRect.rect.height : dragRect.sizeDelta.y;
      var localCenter = overlayRect.InverseTransformPoint(dragRect.TransformPoint(dragRect.rect.center));

      checkboxRect.anchorMin = new Vector2(0.5f, 0.5f);
      checkboxRect.anchorMax = new Vector2(0.5f, 0.5f);
      checkboxRect.pivot = new Vector2(0.5f, 0.5f);
      checkboxRect.sizeDelta = new Vector2(Mathf.Max(36f, dragWidth), Mathf.Max(24f, dragHeight));
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
