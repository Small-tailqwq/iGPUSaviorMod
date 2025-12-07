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

    // === 布局常量：左侧标签的强制宽度，确保对齐 ===
    private const float LABEL_WIDTH = 380f;

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

    // === 兼容性修复：处理 EnvSync 这种未调用 RegisterMod 直接 Add 的情况 ===
    private void EnsureCurrentMod()
    {
      if (_currentRegisteringMod == null)
      {
        RegisterMod("General Settings", ""); // 自动归入通用设置
      }
    }

    public void AddToggle(string label, bool defaultValue, Action<bool> onValueChanged)
    {
      EnsureCurrentMod();
      _currentRegisteringMod.Items.Add(new ToggleDef
      { Label = label, DefaultValue = defaultValue, OnValueChanged = onValueChanged });
    }

    public void AddDropdown(string label, List<string> options, int defaultIndex, Action<int> onValueChanged)
    {
      EnsureCurrentMod();
      _currentRegisteringMod.Items.Add(new DropdownDef
      { Label = label, Options = options, DefaultIndex = defaultIndex, OnValueChanged = onValueChanged });
    }

    public void AddInputField(string label, string defaultValue, Action<string> onValueChanged)
{
    EnsureCurrentMod();
    _currentRegisteringMod.Items.Add(new InputFieldDef
    { 
        Label = label, 
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
        // 如果是 General Settings 且没有版本号，就不显示 Header
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
              EnforceLayout(obj); // === 强制对齐 ===
              obj.SetActive(true);
            }
          }
          else if (item is DropdownDef dropdown)
          {
            yield return CreateDropdownSequence(dropdown);
          }
          else if (item is InputFieldDef inputDef)
          {
              GameObject obj = ModInputFieldCloner.CreateInputField(_settingUIRoot, inputDef.Label, inputDef.DefaultValue, inputDef.OnValueChanged);
              if (obj != null)
              {
                  obj.transform.SetParent(_contentParent, false);
                  EnforceLayout(obj); // === 强制对齐 ===
                  obj.SetActive(true);
              }
          }
        }
        CreateDivider();
      }

      LayoutRebuilder.ForceRebuildLayoutImmediate(_contentParent as RectTransform);
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
          if (tmp) { tmp.text = def.Label; break; }
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

    // === 核心方法：强制修正布局（解决文字挤压问题） ===
    private void EnforceLayout(GameObject obj)
    {
      // 1. 归位 (解决 -145 偏移)
      obj.transform.localPosition = Vector3.zero;
      obj.transform.localScale = Vector3.one;
      obj.transform.localRotation = Quaternion.identity;

      // 2. 寻找 Label 并强制设置宽度
      var texts = obj.GetComponentsInChildren<TMP_Text>(true);
      foreach (var t in texts)
      {
        // 只处理左侧的标题文字
        if (t.transform.position.x < obj.transform.position.x + 100 || t.name.Contains("Title"))
        {
          var le = t.GetComponent<LayoutElement>();
          if (le == null) le = t.gameObject.AddComponent<LayoutElement>();

          // 强制宽度 380，让右边的按钮对齐
          le.minWidth = LABEL_WIDTH;
          le.preferredWidth = LABEL_WIDTH;
          le.flexibleWidth = 0;

          t.alignment = TextAlignmentOptions.MidlineLeft;
          break;
        }
      }
    }

    private void CreateSectionHeader(string name, string version)
    {
      GameObject obj = new GameObject($"Header_{name}");
      obj.transform.SetParent(_contentParent, false);

      var rect = obj.AddComponent<RectTransform>();
      rect.sizeDelta = new Vector2(0, 55);

      var le = obj.AddComponent<LayoutElement>();
      le.minHeight = 55f;
      le.preferredHeight = 55f;
      le.flexibleWidth = 1f;

      var tmp = obj.AddComponent<TextMeshProUGUI>();
      string verStr = string.IsNullOrEmpty(version) ? "" : $" <size=18><color=#888888>v{version}</color></size>";
      tmp.text = $"<size=24><b>{name}</b></size>{verStr}";
      tmp.alignment = TextAlignmentOptions.BottomLeft;
      tmp.color = Color.white;
    }

    private void CreateDivider()
    {
      GameObject obj = new GameObject("Divider");
      obj.transform.SetParent(_contentParent, false);
      var le = obj.AddComponent<LayoutElement>();
      le.minHeight = 20f;
      le.preferredHeight = 20f;
    }

    private void UpdatePulldownSelectedText(GameObject clone, string text)
    {
      var paths = new[] { "PulldownList/Pulldown/CurrentSelectText (TMP)", "CurrentSelectText (TMP)" };
      foreach (var p in paths)
      {
        var t = clone.transform.Find(p);
        if (t != null) { var tmp = t.GetComponent<TMP_Text>(); if (tmp) { tmp.text = text; return; } }
      }
    }
  }
}