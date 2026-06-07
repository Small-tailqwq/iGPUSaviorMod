using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;
using PotatoOptimization.Core;
using Bulbul;

namespace PotatoOptimization.UI
{
  public class ModToggleCloner
  {
    /// <summary>
    /// Clone a native two-button setting row while preserving the game's ON/OFF localization.
    /// </summary>
    public static GameObject CreateToggle(Transform settingRoot, string labelText, bool initialValue, Action<bool> onValueChanged)
    {
      GameObject toggleRow = null;
      try
      {
        if (settingRoot == null) return null;

        Transform originalRow = FindToggleTemplate(settingRoot);
        if (originalRow == null)
        {
          PotatoPlugin.Log.LogError("[ModToggle] Native two-button template not found");
          return null;
        }

        toggleRow = UnityEngine.Object.Instantiate(originalRow.gameObject);
        toggleRow.name = $"ModToggle_{labelText}";
        toggleRow.SetActive(true);

        // Only replace the row title. The cloned button localizers must remain attached
        // so the game's own ON/OFF strings and fonts continue following language changes.
        ModSettingsStyle.BindModText(ModSettingsStyle.FindRowTitle(toggleRow), labelText);

        if (!TryFindToggleButtons(toggleRow, out Button btnOn, out Button btnOff))
        {
          PotatoPlugin.Log.LogError($"[ModToggle] Template {originalRow.name} has no ON/OFF button pair");
          UnityEngine.Object.Destroy(toggleRow);
          return null;
        }

        btnOn.onClick.RemoveAllListeners();
        btnOff.onClick.RemoveAllListeners();

        void UpdateState(bool state)
        {
          btnOn.interactable = !state;
          btnOff.interactable = state;

          var btnOnInteractableUI = btnOn.GetComponent<InteractableUI>();
          var btnOffInteractableUI = btnOff.GetComponent<InteractableUI>();

          if (state)
          {
            btnOnInteractableUI?.ActivateUseUI(false);
            btnOffInteractableUI?.DeactivateUseUI(false);
          }
          else
          {
            btnOnInteractableUI?.DeactivateUseUI(false);
            btnOffInteractableUI?.ActivateUseUI(false);
          }
        }

        btnOn.onClick.AddListener(() =>
        {
          if (btnOn.interactable)
          {
            UpdateState(true);
            onValueChanged?.Invoke(true);
            PlayClickSound(settingRoot);
          }
        });

        btnOff.onClick.AddListener(() =>
        {
          if (btnOff.interactable)
          {
            UpdateState(false);
            onValueChanged?.Invoke(false);
            PlayClickSound(settingRoot);
          }
        });

        UpdateState(initialValue);
        return toggleRow;
      }
      catch (Exception e)
      {
        PotatoPlugin.Log.LogError($"CreateToggle failed: {e}");
        if (toggleRow != null)
          UnityEngine.Object.Destroy(toggleRow);
        return null;
      }
    }

    private static Transform FindToggleTemplate(Transform settingRoot)
    {
      string[] contentPaths =
      {
        "General/ScrollView/Viewport/Content",
        "Graphics/ScrollView/Viewport/Content",
        "MusicAudio/ScrollView/Viewport/Content"
      };

      foreach (string path in contentPaths)
      {
        var content = settingRoot.Find(path);
        if (content == null) continue;

        foreach (Transform child in content)
        {
          if (ModSettingsStyle.FindRowTitle(child.gameObject) != null &&
              TryFindToggleButtons(child.gameObject, out _, out _))
            return child;
        }
      }

      return null;
    }

    private static bool TryFindToggleButtons(GameObject row, out Button onButton, out Button offButton)
    {
      var buttons = row.GetComponentsInChildren<Button>(true);
      onButton = buttons.FirstOrDefault(button =>
        button.name.IndexOf("OnButton", StringComparison.OrdinalIgnoreCase) >= 0 &&
        button.name.IndexOf("OffButton", StringComparison.OrdinalIgnoreCase) < 0);
      offButton = buttons.FirstOrDefault(button =>
        button.name.IndexOf("OffButton", StringComparison.OrdinalIgnoreCase) >= 0);

      return onButton != null && offButton != null;
    }

    private static void PlayClickSound(Transform root)
    {
      // Placeholder for sound effect logic
      // Currently no sound implementation needed
    }
  }
}
