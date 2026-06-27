using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Bulbul;
using NestopiSystem.DIContainers;
using PotatoOptimization.Core;
using PotatoOptimization.UI;

namespace PotatoOptimization.Patches
{
  internal static class ExitConfirmationDialogHelper
  {
    private const string OkTranslationKey = "TODO_DELETE_CONFIRM_OK";
    private const string CancelTranslationKey = "TODO_DELETE_CONFIRM_CANCEL";

    public static bool Show(string dialogNamePrefix, string promptTranslationKey, Action onConfirm, Action onCancel = null)
    {
      if (onConfirm == null)
        return false;

      GameObject clonedGO = null;
      try
      {
        var templates = Resources.FindObjectsOfTypeAll<ExitConfirmationUI>();
        if (templates == null || templates.Length == 0)
        {
          PotatoPlugin.Log.LogWarning("[ConfirmDialog] ExitConfirmationUI template not found.");
          return false;
        }

        clonedGO = UnityEngine.Object.Instantiate(templates[0].gameObject);
        if (clonedGO == null)
          return false;

        clonedGO.name = $"{dialogNamePrefix}_{DateTime.Now.Ticks}";
        var confirmComp = clonedGO.GetComponent<ExitConfirmationUI>();
        if (confirmComp == null)
        {
          UnityEngine.Object.Destroy(clonedGO);
          return false;
        }

        SetupCanvas(clonedGO);
        confirmComp.Setup();
        SetupConfirmText(clonedGO, promptTranslationKey);

        clonedGO.SetActive(true);
        confirmComp.Activate();

        var okButton = confirmComp._okButton;
        var cancelButton = confirmComp._cancelButton;
        if (okButton == null || cancelButton == null)
        {
          SafeDeactivate(confirmComp, clonedGO);
          return false;
        }

        okButton.onClick.RemoveAllListeners();
        cancelButton.onClick.RemoveAllListeners();
        okButton.interactable = true;
        cancelButton.interactable = true;

        okButton.onClick.AddListener(() =>
        {
          try { onConfirm(); }
          finally { SafeDeactivate(confirmComp, clonedGO); }
        });

        cancelButton.onClick.AddListener(() =>
        {
          try { onCancel?.Invoke(); }
          finally { SafeDeactivate(confirmComp, clonedGO); }
        });

        return true;
      }
      catch (Exception e)
      {
        if (clonedGO != null)
          UnityEngine.Object.Destroy(clonedGO);
        PotatoPlugin.Log.LogError("[ConfirmDialog] Failed to show confirmation dialog: " + e);
        return false;
      }
    }

    private static void SetupCanvas(GameObject go)
    {
      var canvas = go.GetComponent<Canvas>();
      if (canvas == null)
        canvas = go.AddComponent<Canvas>();

      canvas.renderMode = RenderMode.ScreenSpaceOverlay;
      canvas.overrideSorting = true;
      canvas.sortingOrder = 10000;

      if (go.GetComponent<GraphicRaycaster>() == null)
        go.AddComponent<GraphicRaycaster>();

      var rect = go.GetComponent<RectTransform>();
      if (rect != null)
      {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
      }

      go.transform.SetAsLastSibling();
    }

    private static void SetupConfirmText(GameObject go, string promptKey)
    {
      try
      {
        foreach (var loc in go.GetComponentsInChildren<TextLocalizationBehaviour>(true))
          loc.enabled = false;

        var lang = ResolveCurrentLanguage();
        var prompt = ModTranslationManager.Get(promptKey, lang);
        var ok = ModTranslationManager.Get(OkTranslationKey, lang);
        var cancel = ModTranslationManager.Get(CancelTranslationKey, lang);

        foreach (var text in go.GetComponentsInChildren<TMP_Text>(true))
        {
          if (string.IsNullOrEmpty(text.text)) continue;

          var path = GetComponentPath(text.transform).ToLowerInvariant();
          var lower = text.text.ToLowerInvariant();

          if (path.Contains("title") || lower == "ui_exit_title")
            text.text = prompt;
          else if (path.Contains("ok") || lower == "ui_exit_ok")
            text.text = ok;
          else if (path.Contains("cancel") || lower == "ui_exit_cancel")
            text.text = cancel;
        }
      }
      catch (Exception e)
      {
        PotatoPlugin.Log.LogError("[ConfirmDialog] Failed to set text: " + e);
      }
    }

    private static GameLanguageType ResolveCurrentLanguage()
    {
      try
      {
        var supplier = ProjectLifetimeScope.Resolve<LanguageSupplier>();
        if (supplier?.Language != null)
          return supplier.Language.CurrentValue;
      }
      catch (Exception e)
      {
        PotatoPlugin.Log.LogWarning("[ConfirmDialog] Lang fallback: " + e.Message);
      }
      return GameLanguageType.ChineseSimplified;
    }

    private static string GetComponentPath(Transform t)
    {
      if (t == null) return "";
      var path = t.name;
      var p = t.parent;
      var depth = 0;
      while (p != null && depth < 6)
      {
        path = p.name + "/" + path;
        p = p.parent;
        depth++;
      }
      return path;
    }

    private static void SafeDeactivate(ExitConfirmationUI comp, GameObject go)
    {
      try
      {
        comp?.Deactivate();
        if (go != null)
        {
          go.SetActive(false);
          UnityEngine.Object.Destroy(go, 0.5f);
        }
      }
      catch (Exception e)
      {
        PotatoPlugin.Log.LogError("[ConfirmDialog] Failed to close dialog: " + e);
      }
    }
  }
}
