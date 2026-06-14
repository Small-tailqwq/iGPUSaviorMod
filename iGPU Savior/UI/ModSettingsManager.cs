using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    private abstract class SettingItemDef
    {
      public string Label;
      public string Key => Label;
      public ModData Owner;
      public VisibleWhenCondition Condition;
    }

    private class ToggleDef : SettingItemDef
    {
      public bool DefaultValue;
      public bool CurrentValue;
      public Action<bool> OnValueChanged;
    }

    private class DropdownDef : SettingItemDef
    {
      public List<string> Options;
      public int DefaultIndex;
      public int CurrentIndex;
      public Action<int> OnValueChanged;
    }

    private class InputFieldDef : SettingItemDef
    {
      public string DefaultValue;
      public string CurrentValue;
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

    private readonly Dictionary<SettingItemDef, GameObject> _itemUIs =
      new Dictionary<SettingItemDef, GameObject>();
    private readonly HashSet<string> _visibilityWarnings = new HashSet<string>();

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
    private ModData EnsureCurrentMod()
    {
      if (_currentRegisteringMod == null)
      {
        RegisterMod("General Settings", ""); // 自动归入通用设置
      }
      return _currentRegisteringMod;
    }

    public void AddToggle(string labelOrKey, bool defaultValue, Action<bool> onValueChanged)
      => AddToggle(labelOrKey, defaultValue, onValueChanged, null);

    public void AddToggle(string labelOrKey, bool defaultValue, Action<bool> onValueChanged,
                          VisibleWhenCondition visibleWhen)
    {
      var owner = EnsureCurrentMod();
      var toggle = new ToggleDef
      {
        Label = labelOrKey,
        Owner = owner,
        DefaultValue = defaultValue,
        CurrentValue = defaultValue,
        OnValueChanged = onValueChanged,
        Condition = visibleWhen
      };
      owner.Items.Add(toggle);
    }

    public void AddDropdown(string labelOrKey, List<string> options, int defaultIndex,
                            Action<int> onValueChanged)
      => AddDropdown(labelOrKey, options, defaultIndex, onValueChanged, null);

    public void AddDropdown(string labelOrKey, List<string> options, int defaultIndex,
                            Action<int> onValueChanged,
                            VisibleWhenCondition visibleWhen)
    {
      var owner = EnsureCurrentMod();
      var dropdown = new DropdownDef
      {
        Label = labelOrKey,
        Owner = owner,
        Options = options ?? new List<string>(),
        DefaultIndex = defaultIndex,
        CurrentIndex = defaultIndex,
        OnValueChanged = onValueChanged,
        Condition = visibleWhen
      };
      owner.Items.Add(dropdown);
    }

    public void AddInputField(string labelText, string defaultValue, Action<string> onValueChanged)
      => AddInputField(labelText, defaultValue, onValueChanged, null);

    public void AddInputField(string labelText, string defaultValue, Action<string> onValueChanged,
                              VisibleWhenCondition visibleWhen)
    {
      var owner = EnsureCurrentMod();
      var input = new InputFieldDef
      {
        Label = labelText,
        Owner = owner,
        DefaultValue = defaultValue,
        CurrentValue = defaultValue,
        OnValueChanged = onValueChanged,
        Condition = visibleWhen
      };
      owner.Items.Add(input);
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
      _itemUIs.Clear();
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
          GameObject row = null;
          if (item is ToggleDef toggle)
          {
            Action<bool> wrapped = WrapToggleCallback(toggle);
            row = ModToggleCloner.CreateToggle(_settingUIRoot, toggle.Label, toggle.DefaultValue, wrapped);
            if (row != null)
            {
              row.transform.SetParent(_contentParent, false);
              EnforceLayout(row);
              row.SetActive(true);
            }
          }
          else if (item is DropdownDef dropdown)
          {
            yield return CreateDropdownSequence(dropdown, (created) =>
            {
              _itemUIs[dropdown] = created;
            });
          }
          else if (item is InputFieldDef inputDef)
          {
            Transform graphicsContent = _settingUIRoot.Find("Graphics/ScrollView/Viewport/Content");
            if (graphicsContent == null)
            {
              PotatoOptimization.Core.PotatoPlugin.Log.LogError("[Manager] Graphics Content not found!");
              continue;
            }

            Action<string> wrapped = WrapInputCallback(inputDef);
            row = ModInputFieldCloner.CreateInputField(
                graphicsContent,
                inputDef.Label,
                inputDef.DefaultValue,
                wrapped);

            if (row != null)
            {
              row.transform.SetParent(_contentParent, false);
              EnforceLayout(row);
              row.SetActive(true);
            }
            else
            {
              PotatoOptimization.Core.PotatoPlugin.Log.LogWarning($"[Manager] Failed to create input field: {inputDef.Label}");
            }
          }

          if (!(item is DropdownDef) && row != null)
          {
            _itemUIs[item] = row;
          }
        }

        yield return null;
        ApplyInitialVisibilityForMod(mod);
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

    private IEnumerator CreateDropdownSequence(DropdownDef def, Action<GameObject> onRootCreated)
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

      Action<int> wrapped = WrapDropdownCallback(def);
      GameObject buttonTemplate = ModPulldownCloner.GetSelectButtonTemplate(_settingUIRoot);
      for (int i = 0; i < def.Options.Count; i++)
      {
        int idx = i;
        ModPulldownCloner.AddOption(pulldownClone, buttonTemplate, def.Options[i], () => wrapped?.Invoke(idx));
      }

      if (def.DefaultIndex >= 0 && def.DefaultIndex < def.Options.Count)
        UpdatePulldownSelectedText(pulldownClone, def.Options[def.DefaultIndex]);

      Destroy(buttonTemplate);
      pulldownClone.transform.SetParent(_contentParent, false);

      EnforceLayout(pulldownClone); // === 强制对齐 ===
      pulldownClone.SetActive(true);

      onRootCreated?.Invoke(pulldownClone);

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

    private Action<bool> WrapToggleCallback(ToggleDef toggle)
    {
      Action<bool> original = toggle.OnValueChanged;
      return (value) =>
      {
        toggle.CurrentValue = value;
        try
        {
          RefreshDependents(toggle.Owner, toggle);
        }
        catch (Exception e)
        {
          PotatoOptimization.Core.PotatoPlugin.Log.LogWarning($"[ModSettings] 刷新依赖失败: {e.Message}");
        }
        original?.Invoke(value);
      };
    }

    private Action<int> WrapDropdownCallback(DropdownDef dropdown)
    {
      Action<int> original = dropdown.OnValueChanged;
      return (index) =>
      {
        dropdown.CurrentIndex = index;
        try
        {
          RefreshDependents(dropdown.Owner, dropdown);
        }
        catch (Exception e)
        {
          PotatoOptimization.Core.PotatoPlugin.Log.LogWarning($"[ModSettings] 刷新依赖失败: {e.Message}");
        }
        original?.Invoke(index);
      };
    }

    private Action<string> WrapInputCallback(InputFieldDef input)
    {
      Action<string> original = input.OnValueChanged;
      return (value) =>
      {
        input.CurrentValue = value;
        try
        {
          RefreshDependents(input.Owner, input);
        }
        catch (Exception e)
        {
          PotatoOptimization.Core.PotatoPlugin.Log.LogWarning($"[ModSettings] 刷新依赖失败: {e.Message}");
        }
        original?.Invoke(value);
      };
    }

    private void ApplyInitialVisibilityForMod(ModData mod)
    {
      bool changed = false;
      foreach (var item in mod.Items)
      {
        if (item.Condition == null) continue;
        bool visible = EvaluateVisibility(mod, item, out _);
        if (ApplyVisibility(item, visible)) changed = true;
      }
      if (changed)
        LayoutRebuilder.ForceRebuildLayoutImmediate(_contentParent as RectTransform);
    }

    private void RefreshDependents(ModData owner, SettingItemDef controller)
    {
      bool changed = false;
      var dependents = owner.Items.Where(i => i.Condition != null && i.Condition.TargetKey == controller.Key);
      foreach (var dependent in dependents)
      {
        bool visible = EvaluateVisibility(owner, dependent, out _);
        if (ApplyVisibility(dependent, visible)) changed = true;
      }
      if (changed)
        LayoutRebuilder.ForceRebuildLayoutImmediate(_contentParent as RectTransform);
    }

    private bool EvaluateVisibility(ModData owner, SettingItemDef dependent, out string failureReason)
    {
      failureReason = null;
      if (dependent.Condition == null)
        return true;

      var targets = owner.Items.Where(i => i.Key == dependent.Condition.TargetKey).ToList();
      if (targets.Count != 1)
      {
        failureReason = targets.Count == 0
            ? $"Condition target '{dependent.Condition.TargetKey}' not found"
            : $"Condition target '{dependent.Condition.TargetKey}' is ambiguous ({targets.Count} matches)";
        LogVisibilityWarningOnce(dependent, failureReason);
        return true;
      }

      var controller = targets[0];
      var snapshot = ToSnapshot(controller);
      bool result = VisibleWhenEvaluator.Evaluate(dependent.Condition, snapshot, out failureReason);
      if (!string.IsNullOrEmpty(failureReason))
      {
        LogVisibilityWarningOnce(dependent, failureReason);
      }
      return result;
    }

    private SettingValueSnapshot ToSnapshot(SettingItemDef item)
    {
      if (item is ToggleDef toggle)
      {
        return new SettingValueSnapshot
        {
          Kind = SettingValueKind.Toggle,
          ToggleValue = toggle.CurrentValue
        };
      }

      if (item is DropdownDef dropdown)
      {
        return new SettingValueSnapshot
        {
          Kind = SettingValueKind.Dropdown,
          DropdownIndex = dropdown.CurrentIndex,
          DropdownOptions = dropdown.Options
        };
      }

      if (item is InputFieldDef input)
      {
        return new SettingValueSnapshot
        {
          Kind = SettingValueKind.InputField,
          InputValue = input.CurrentValue
        };
      }

      return null;
    }

    private bool ApplyVisibility(SettingItemDef item, bool visible)
    {
      if (!_itemUIs.TryGetValue(item, out GameObject row) || row == null)
      {
        PotatoOptimization.Core.PotatoPlugin.Log.LogDebug(
            $"[ModSettings] 找不到 '{item.Key}' 的 UI 行，跳过可见性设置。");
        return false;
      }
      if (row.activeSelf == visible) return false;

      if (!visible)
      {
        ModPulldownCloner.TryClosePulldown(row);
      }

      row.SetActive(visible);
      return true;
    }

    private void LogVisibilityWarningOnce(SettingItemDef item, string reason)
    {
      string key = $"{item.Owner?.Name}:{item.Key}:{reason}";
      if (_visibilityWarnings.Add(key))
      {
        PotatoOptimization.Core.PotatoPlugin.Log.LogWarning($"[ModSettings] 条件可见性错误（{key}）：{reason}，该项将保持可见。");
      }
    }
  }
}
