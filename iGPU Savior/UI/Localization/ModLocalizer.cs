using UnityEngine;
using TMPro;
using Bulbul;
using R3;
using PotatoOptimization.Core;
using System;
using NestopiSystem;
using NestopiSystem.DIContainers;
using VContainer;

namespace PotatoOptimization.UI
{
  public class ModLocalizer : MonoBehaviour
  {
    private string _key;
    public string Key
    {
      get => _key;
      set
      {
        if (_key != value)
        {
          _key = value;
          Refresh();
        }
      }
    }

    private TMP_Text _textComp;
    private IDisposable _subscription;

    private static GameLanguageType _cachedLang = GameLanguageType.English; // Default fallback
    private static FontSupplier _cachedFontSupplier;
    private static LanguageSupplier _cachedLanguageSupplier;

    // Use a simplified approach: just re-resolve or cache statically?
    // Since we're in a Monobehaviour, keeping instance ref is fine, but resolving in Start is standard.
    // However, for runtime updates (Refresh) we need access to Lang/Font.

    private GameLanguageType _currentLang = GameLanguageType.English;
    private FontSupplier _fontSupplier;

    void Start()
    {
      _textComp = GetComponent<TMP_Text>();
      if (_textComp == null)
      {
        _textComp = GetComponentInChildren<TMP_Text>();
      }

      if (_textComp == null)
      {
        // PotatoPlugin.Log.LogError($"[ModLocalizer] No TMP_Text found on {gameObject.name}");
        return;
      }

      try
      {
        // Resolve services
        var languageSupplier = ProjectLifetimeScope.Resolve<LanguageSupplier>();
        _fontSupplier = ProjectLifetimeScope.Resolve<FontSupplier>();

        if (languageSupplier != null)
        {
          _subscription = languageSupplier.Language.Subscribe(lang =>
          {
            _currentLang = lang;
            Refresh();
          });
        }
      }
      catch (System.Exception e)
      {
        PotatoPlugin.Log.LogError($"[ModLocalizer] Error in Start: {e}");
      }
    }

    public void Refresh()
    {
      if (_textComp == null) return;

      // 1. Update Text
      string translated = ModTranslationManager.Get(Key, _currentLang);
      if (!string.IsNullOrEmpty(translated))
      {
        _textComp.text = translated;
      }
      // If translated is empty (key not found or empty key), maybe keep original text?
      // For now, if key provided, we expect translation.

      // 2. Update Font
      if (_fontSupplier != null)
      {
        var fontAsset = _fontSupplier.GetFontAsset(_currentLang);
        if (fontAsset != null)
        {
          _textComp.font = fontAsset;
        }
      }
    }

    void OnDestroy()
    {
      _subscription?.Dispose();
    }
  }
}
