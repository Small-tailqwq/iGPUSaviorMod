using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PotatoOptimization.UI;

namespace ModShared
{
  public class ModSettingsManager : MonoBehaviour
  {
    public static ModSettingsManager Instance { get; private set; }

    // === 兼容性修复：加回 IsInitialized，让 EnvSync 能检测到 ===
    public bool IsInitialized { get; private set; } = false;

    private abstract class SettingItemDef { public string Label; }
    private class ToggleDef : SettingItemDef { public bool DefaultValue; public Action<bool> OnValueChanged; }
    private class DropdownDef : SettingItemDef { public List<string> Options; public int DefaultIndex; public Action<int> OnValueChanged; }

    private class InputFieldDef : SettingItemDef
    {
      public string DefaultValue;
      public Action<string> OnValueChanged;
    }

    private class ModData
    {
      public string Name;
      public string Version;
      public List<SettingItemDef> Items = new List<SettingItemDef>();
    }

    private List<ModData> _registeredMods = new List<ModData>();
    private ModData _currentRegisteringMod;

    private Transform _contentParent;
    private Transform _settingUIRoot;
    private bool _isBuildingUI = false;

    void Awake()
    {
      if (Instance == null)
      {
        Instance = this;
        IsInitialized = true; // 标记初始化完成
        DontDestroyOnLoad(gameObject);
      }
      else if (Instance != this)
      {
        Destroy(gameObject);
      }
    }

    public void RegisterMod(string modName, string modVersion)
    {
      var existing = _registeredMods.Find(m => m.Name == modName);
      if (existing != null)
      {
        _currentRegisteringMod = existing;
        return;
      }
      ModData newMod = new ModData { Name = modName, Version = modVersion };
      _registeredMods.Add(newMod);
      _currentRegisteringMod = newMod;
      // 注意：这里我去掉了"成功"二字，方便我们在日志里确认代码是否更新
      PotatoOptimization.Core.PotatoPlugin.Log.LogInfo($"[ModManager] Mod 注册: {modName}");
    }

    /// <summary>
    /// Register a translation key for 3rd party mods.
    /// Call this BEFORE AddToggle/AddDropdown if you want to use keys.
    /// </summary>
    public void RegisterTranslation(string key, string en, string ja, string zh)
    {
      PotatoOptimization.UI.ModTranslationManager.Add(key, en, ja, zh);
    }

    // === 兼容性修复：处理 EnvSync 这种未调用 RegisterMod 直接 Add 的情况 ===
    private void EnsureCurrentMod()
    {
      if (_currentRegisteringMod == null)
      {
        RegisterMod("General Settings", ""); // 自动归入通用设置
      }
    }

    public void AddToggle(string labelOrKey, bool defaultValue, Action<bool> onValueChanged)
    {
      EnsureCurrentMod();
      _currentRegisteringMod.Items.Add(new ToggleDef
      { Label = labelOrKey, DefaultValue = defaultValue, OnValueChanged = onValueChanged });
    }

    public void AddDropdown(string labelOrKey, List<string> options, int defaultIndex, Action<int> onValueChanged)
    {
      EnsureCurrentMod();
      _currentRegisteringMod.Items.Add(new DropdownDef
      { Label = labelOrKey, Options = options, DefaultIndex = defaultIndex, OnValueChanged = onValueChanged });
    }

    public void AddInputField(string labelText, string defaultValue, Action<string> onValueChanged)
    {
      EnsureCurrentMod();  // ← 先确保有当前 Mod

      _currentRegisteringMod.Items.Add(new InputFieldDef
      {
        Label = labelText,
        DefaultValue = defaultValue,
        OnValueChanged = onValueChanged
      });
    }

    public void RebuildUI(Transform contentParent, Transform settingUIRoot)
    {
      if (_isBuildingUI) return;
      _contentParent = contentParent;
      _settingUIRoot = settingUIRoot;
      StartCoroutine(BuildSequence());
    }

    private IEnumerator BuildSequence()
    {
      _isBuildingUI = true;
      foreach (Transform child in _contentParent) Destroy(child.gameObject);
      yield return null;

      foreach (var mod in _registeredMods)
      {
        if (mod.Name != "General Settings" || !string.IsNullOrEmpty(mod.Version))
        {
          CreateSectionHeader(mod.Name, mod.Version);
        }

        foreach (var item in mod.Items)
        {
          if (item is ToggleDef toggle)
          {
            GameObject obj = ModToggleCloner.CreateToggle(_settingUIRoot, toggle.Label, toggle.DefaultValue, toggle.OnValueChanged);
            if (obj != null)
            {
              obj.transform.SetParent(_contentParent, false);
              EnforceLayout(obj);
              obj.SetActive(true);
            }
          }
          else if (item is DropdownDef dropdown)
          {
            yield return CreateDropdownSequence(dropdown);
          }
          else if (item is InputFieldDef inputDef)
          {
            // 🆕 关键修改：从 _settingUIRoot 查找原版游戏的模板位置
            Transform graphicsContent = _settingUIRoot.Find("Graphics/ScrollView/Viewport/Content");

            if (graphicsContent == null)
            {
              PotatoOptimization.Core.PotatoPlugin.Log.LogError("[Manager] Graphics Content not found!");
              continue;
            }

            GameObject obj = ModInputFieldCloner.CreateInputField(
                graphicsContent,  // ← 传入 Graphics 的 Content，里面有模板
                inputDef.Label,
                inputDef.DefaultValue,
                inputDef.OnValueChanged
            );

            if (obj != null)
            {
              obj.transform.SetParent(_contentParent, false);
              EnforceLayout(obj);
              obj.SetActive(true);
            }
            else
            {
              PotatoOptimization.Core.PotatoPlugin.Log.LogWarning($"[Manager] Failed to create input field: {inputDef.Label}");
            }

          }
        }
        CreateDivider();
      }
      Canvas.ForceUpdateCanvases();
      LayoutRebuilder.ForceRebuildLayoutImmediate(_contentParent as RectTransform);
      Canvas.ForceUpdateCanvases();

      var scrollRect = _contentParent.GetComponentInParent<ScrollRect>();
      if (scrollRect != null)
      {
        ModSettingsStyle.ConfigureScrollViewport(scrollRect);
        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.GetComponent<RectTransform>());
        scrollRect.StopMovement();
        scrollRect.verticalNormalizedPosition = 1f;
      }

      _isBuildingUI = false;
    }

    private IEnumerator CreateDropdownSequence(DropdownDef def)
    {
      GameObject pulldownClone = ModPulldownCloner.CloneAndClearPulldown(_settingUIRoot);
      if (pulldownClone == null) yield break;

      // 设置标题
      var paths = new[] { "TitleText", "Title/Text", "Text" };
      foreach (var p in paths)
      {
        var t = pulldownClone.transform.Find(p);
        if (t != null)
        {
          var tmp = t.GetComponent<TMP_Text>();
          if (tmp)
          {
            tmp.text = def.Label;

            // ✅ 设置 Localization Key
            var loc = t.GetComponent<ModLocalizer>();
            if (loc != null) loc.Key = def.Label;

            break;
          }
        }
      }

      GameObject buttonTemplate = ModPulldownCloner.GetSelectButtonTemplate(_settingUIRoot);
      for (int i = 0; i < def.Options.Count; i++)
      {
        int idx = i;
        ModPulldownCloner.AddOption(pulldownClone, buttonTemplate, def.Options[i], () => def.OnValueChanged?.Invoke(idx));
      }

      if (def.DefaultIndex >= 0 && def.DefaultIndex < def.Options.Count)
        UpdatePulldownSelectedText(pulldownClone, def.Options[def.DefaultIndex]);

      Destroy(buttonTemplate);
      pulldownClone.transform.SetParent(_contentParent, false);

      EnforceLayout(pulldownClone); // === 强制对齐 ===
      pulldownClone.SetActive(true);

      Transform content = pulldownClone.transform.Find("PulldownList/Pulldown/CurrentSelectText (TMP)/Content");
      LayoutRebuilder.ForceRebuildLayoutImmediate(content as RectTransform);
      Canvas.ForceUpdateCanvases();
      yield return null;
      LayoutRebuilder.ForceRebuildLayoutImmediate(content as RectTransform);
      yield return null;

      float contentHeight = (content as RectTransform).sizeDelta.y;
      if (contentHeight < 40f) contentHeight = def.Options.Count * 40f;

      Transform originalPulldown = _settingUIRoot.Find("Graphics/ScrollView/Viewport/Content/GraphicQualityPulldownList");
      ModPulldownCloner.EnsurePulldownListUI(pulldownClone, originalPulldown, content, contentHeight);

      yield return new WaitForSeconds(0.05f);
    }

    private void EnforceLayout(GameObject obj)
    {
      ModSettingsStyle.PrepareRow(obj);
    }

    private void CreateSectionHeader(string name, string version)
    {
      GameObject obj = new GameObject($"Header_{name}", typeof(RectTransform));
      obj.transform.SetParent(_contentParent, false);

      var rect = obj.GetComponent<RectTransform>();
      rect.sizeDelta = new Vector2(ModSettingsStyle.NativeRowWidth, 64);

      var le = obj.AddComponent<LayoutElement>();
      le.ignoreLayout = false;
      le.minWidth = ModSettingsStyle.NativeRowWidth;
      le.preferredWidth = ModSettingsStyle.NativeRowWidth;
      le.minHeight = 64f;
      le.preferredHeight = 64f;
      le.flexibleHeight = 0f;
      le.flexibleWidth = 0f;

      var titleObj = new GameObject("TitleText", typeof(RectTransform));
      titleObj.transform.SetParent(obj.transform, false);
      var titleRect = titleObj.GetComponent<RectTransform>();
      titleRect.anchorMin = new Vector2(0f, 0.5f);
      titleRect.anchorMax = new Vector2(0f, 0.5f);
      titleRect.pivot = new Vector2(0f, 0.5f);
      titleRect.anchoredPosition = new Vector2(ModSettingsStyle.NativeTitleAnchoredX, 0f);
      titleRect.sizeDelta = new Vector2(900f, 64f);

      var tmp = titleObj.AddComponent<TextMeshProUGUI>();
      string verStr = string.IsNullOrEmpty(version) ? "" : $" <size=18><color=#888888>v{version}</color></size>";
      tmp.text = $"<size=24><b>{name}</b></size>{verStr}";
      tmp.alignment = TextAlignmentOptions.BottomLeft;
      tmp.color = Color.white;
      tmp.margin = new Vector4(0f, 0f, 0f, 8f);
      ModSettingsStyle.ApplySectionHeaderStyle(tmp, _settingUIRoot);

      var lineObj = new GameObject("Line", typeof(RectTransform));
      lineObj.transform.SetParent(obj.transform, false);
      var lineRect = lineObj.GetComponent<RectTransform>();
      lineRect.anchorMin = new Vector2(0f, 0f);
      lineRect.anchorMax = new Vector2(1f, 0f);
      lineRect.pivot = new Vector2(0.5f, 0f);
      lineRect.anchoredPosition = Vector2.zero;
      lineRect.sizeDelta = new Vector2(0f, 2f);
      var line = lineObj.AddComponent<Image>();
      line.color = new Color(tmp.color.r, tmp.color.g, tmp.color.b, 0.18f);
      line.raycastTarget = false;
    }

    private void CreateDivider()
    {
      GameObject obj = new GameObject("Divider", typeof(RectTransform));
      obj.transform.SetParent(_contentParent, false);
      var rect = obj.GetComponent<RectTransform>();
      rect.sizeDelta = new Vector2(ModSettingsStyle.NativeRowWidth, 12f);
      var le = obj.AddComponent<LayoutElement>();
      le.ignoreLayout = false;
      le.minWidth = ModSettingsStyle.NativeRowWidth;
      le.preferredWidth = ModSettingsStyle.NativeRowWidth;
      le.minHeight = 12f;
      le.preferredHeight = 12f;
      le.flexibleHeight = 0f;
    }

    private void UpdatePulldownSelectedText(GameObject clone, string text)
    {
      var paths = new[] { "PulldownList/Pulldown/CurrentSelectText (TMP)", "CurrentSelectText (TMP)" };
      foreach (var p in paths)
      {
        var t = clone.transform.Find(p);
        if (t != null)
        {
          var tmp = t.GetComponent<TMP_Text>();
          if (tmp)
          {
            tmp.text = text; // Set initial text (might be key or translated if passed translated)
                             // Actually UpdatePulldownSelectedText is called with options[defaultIndex].
                             // options are now KEYS. So text is KEY.

            // Attach ModLocalizer
            var loc = t.gameObject.GetComponent<ModLocalizer>();
            if (loc == null) loc = t.gameObject.AddComponent<ModLocalizer>();
            loc.Key = text; // Setting Key triggers Refresh() -> sets text to translation.

            return;
          }
        }
      }
    }
  }
}
