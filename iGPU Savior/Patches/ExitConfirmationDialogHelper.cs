using System;
using System.Reflection;
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

    private static readonly Type ExitConfirmUIType;
    private static readonly MethodInfo MI_Setup;
    private static readonly MethodInfo MI_Activate;
    private static readonly MethodInfo MI_Deactivate;
    private static readonly FieldInfo FI_OkButton;
    private static readonly FieldInfo FI_CancelButton;
    private static readonly bool IsReady;

    static ExitConfirmationDialogHelper()
    {
      try
      {
        var assembly = Assembly.Load("Assembly-CSharp");
        ExitConfirmUIType = assembly.GetType("Bulbul.ExitConfirmationUI");

        if (ExitConfirmUIType != null)
        {
          MI_Setup = ExitConfirmUIType.GetMethod("Setup", BindingFlags.Instance | BindingFlags.Public);
          MI_Activate = ExitConfirmUIType.GetMethod("Activate", BindingFlags.Instance | BindingFlags.Public);
          MI_Deactivate = ExitConfirmUIType.GetMethod("Deactivate", BindingFlags.Instance | BindingFlags.Public);
          FI_OkButton = ExitConfirmUIType.GetField("_okButton", BindingFlags.Instance | BindingFlags.NonPublic);
          FI_CancelButton = ExitConfirmUIType.GetField("_cancelButton", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        IsReady = ExitConfirmUIType != null &&
                  MI_Activate != null &&
                  MI_Deactivate != null &&
                  FI_OkButton != null &&
                  FI_CancelButton != null;
      }
      catch (Exception e)
      {
        PotatoPlugin.Log.LogError("[ConfirmDialog] Failed to initialize helper: " + e);
      }
    }

    public static bool Show(string dialogNamePrefix, string promptTranslationKey, Action onConfirm, Action onCancel = null)
    {
      if (!IsReady || onConfirm == null)
      {
        return false;
      }

      try
      {
        var confirmComp = CreateDialogInstance(dialogNamePrefix);
        var confirmGO = confirmComp != null ? confirmComp.gameObject : null;
        if (confirmComp == null || confirmGO == null)
        {
          return false;
        }

        EnsureDialogCanvas(confirmGO);
        SafeInvoke(MI_Setup, confirmComp);
        SetupConfirmText(confirmGO, promptTranslationKey);

        TrySetActive(confirmGO, true);
        SafeInvoke(MI_Activate, confirmComp);
        TrySetActive(confirmGO, true);

        var okButton = FI_OkButton.GetValue(confirmComp) as Button;
        var cancelButton = FI_CancelButton.GetValue(confirmComp) as Button;
        if (okButton == null || cancelButton == null)
        {
          SafeDeactivate(confirmComp, confirmGO);
          return false;
        }

        okButton.onClick.RemoveAllListeners();
        cancelButton.onClick.RemoveAllListeners();
        okButton.interactable = true;
        cancelButton.interactable = true;

        okButton.onClick.AddListener(() =>
        {
          try
          {
            onConfirm();
          }
          finally
          {
            SafeDeactivate(confirmComp, confirmGO);
          }
        });

        cancelButton.onClick.AddListener(() =>
        {
          try
          {
            if (onCancel != null)
            {
              onCancel();
            }
          }
          finally
          {
            SafeDeactivate(confirmComp, confirmGO);
          }
        });

        return true;
      }
      catch (Exception e)
      {
        PotatoPlugin.Log.LogError("[ConfirmDialog] Failed to show confirmation dialog: " + e);
        return false;
      }
    }

    private static Component CreateDialogInstance(string dialogNamePrefix)
    {
      try
      {
        var all = Resources.FindObjectsOfTypeAll(ExitConfirmUIType);
        if (all == null || all.Length == 0)
        {
          PotatoPlugin.Log.LogWarning("[ConfirmDialog] ExitConfirmationUI template not found.");
          return null;
        }

        var template = all[0] as Component;
        if (template == null)
        {
          return null;
        }

        var clonedGO = UnityEngine.Object.Instantiate(template.gameObject);
        if (clonedGO == null)
        {
          return null;
        }

        clonedGO.name = string.Format("{0}_{1}", dialogNamePrefix, DateTime.Now.Ticks);
        return clonedGO.GetComponent(ExitConfirmUIType) as Component;
      }
      catch (Exception e)
      {
        PotatoPlugin.Log.LogError("[ConfirmDialog] Failed to create dialog instance: " + e);
        return null;
      }
    }

    private static void EnsureDialogCanvas(GameObject confirmGO)
    {
      var dialogCanvas = confirmGO.GetComponent<Canvas>();
      if (dialogCanvas == null)
      {
        dialogCanvas = confirmGO.AddComponent<Canvas>();
      }

      dialogCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
      dialogCanvas.overrideSorting = true;
      dialogCanvas.sortingOrder = 10000;

      var raycaster = confirmGO.GetComponent<GraphicRaycaster>();
      if (raycaster == null)
      {
        confirmGO.AddComponent<GraphicRaycaster>();
      }

      var rectTransform = confirmGO.GetComponent<RectTransform>();
      if (rectTransform != null)
      {
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
      }

      confirmGO.transform.SetAsLastSibling();
    }

    private static void SetupConfirmText(GameObject confirmGO, string promptTranslationKey)
    {
      try
      {
        DisableAllTextLocalizers(confirmGO);

        var language = ResolveCurrentLanguage();
        var promptText = ModTranslationManager.Get(promptTranslationKey, language);
        var okText = ModTranslationManager.Get(OkTranslationKey, language);
        var cancelText = ModTranslationManager.Get(CancelTranslationKey, language);

        var allTexts = confirmGO.GetComponentsInChildren<TMP_Text>(true);
        foreach (var text in allTexts)
        {
          if (string.IsNullOrEmpty(text.text))
          {
            continue;
          }

          var originalText = text.text;
          var path = GetComponentPath(text.transform);
          var pathLower = path.ToLowerInvariant();
          var textLower = originalText.ToLowerInvariant();

          if (pathLower.Contains("title") || originalText == "ui_exit_title")
          {
            text.text = promptText;
          }
          else if (pathLower.Contains("ok") || textLower == "ui_exit_ok")
          {
            text.text = okText;
          }
          else if (pathLower.Contains("cancel") || textLower == "ui_exit_cancel")
          {
            text.text = cancelText;
          }
        }
      }
      catch (Exception e)
      {
        PotatoPlugin.Log.LogError("[ConfirmDialog] Failed to set dialog text: " + e);
      }
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
        PotatoPlugin.Log.LogWarning("[ConfirmDialog] Failed to resolve current language, using fallback: " + e.Message);
      }

      return GameLanguageType.ChineseSimplified;
    }

    private static void DisableAllTextLocalizers(GameObject obj)
    {
      try
      {
        var localizerType = Type.GetType("Bulbul.TextLocalizationBehaviour, Assembly-CSharp");
        if (localizerType == null)
        {
          return;
        }

        var localizers = obj.GetComponentsInChildren(localizerType, true);
        foreach (var localizer in localizers)
        {
          var component = localizer as Component;
          if (component == null)
          {
            continue;
          }

          var enabledProperty = component.GetType().GetProperty("enabled", BindingFlags.Instance | BindingFlags.Public);
          if (enabledProperty != null && enabledProperty.CanWrite)
          {
            enabledProperty.SetValue(component, false, null);
          }
        }
      }
      catch (Exception e)
      {
        PotatoPlugin.Log.LogWarning("[ConfirmDialog] Failed to disable text localizers: " + e.Message);
      }
    }

    private static string GetComponentPath(Transform transform)
    {
      if (transform == null)
      {
        return string.Empty;
      }

      var path = transform.name;
      var parent = transform.parent;
      var depth = 0;
      while (parent != null && depth < 6)
      {
        path = parent.name + "/" + path;
        parent = parent.parent;
        depth++;
      }

      return path;
    }

    private static void SafeDeactivate(Component confirmComp, GameObject confirmGO)
    {
      try
      {
        SafeInvoke(MI_Deactivate, confirmComp);
        if (confirmGO != null)
        {
          confirmGO.SetActive(false);
          UnityEngine.Object.Destroy(confirmGO, 0.5f);
        }
      }
      catch (Exception e)
      {
        PotatoPlugin.Log.LogError("[ConfirmDialog] Failed to close dialog: " + e);
      }
    }

    private static void TrySetActive(GameObject gameObject, bool isActive)
    {
      try
      {
        if (gameObject != null)
        {
          gameObject.SetActive(isActive);
        }
      }
      catch
      {
      }
    }

    private static void SafeInvoke(MethodInfo method, object instance)
    {
      try
      {
        if (method != null && instance != null)
        {
          method.Invoke(instance, null);
        }
      }
      catch (Exception e)
      {
        PotatoPlugin.Log.LogWarning("[ConfirmDialog] SafeInvoke failed: " + e.Message);
      }
    }
  }
}
