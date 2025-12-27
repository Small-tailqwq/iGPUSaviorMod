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
          // ✅ 创建 Header 后立即调整位置
          if (mod.Name == "iGPU Savior")
          {
            AdjustHeaderPosition(mod.Name);
          }
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
      // ✅ 最后调整 ScrollView
      AdjustScrollViewPosition();

      LayoutRebuilder.ForceRebuildLayoutImmediate(_contentParent as RectTransform);
      _isBuildingUI = false;

      LayoutRebuilder.ForceRebuildLayoutImmediate(_contentParent as RectTransform);
      _isBuildingUI = false;
    }
    // 🆕 === 新增方法：调整 UI 位置 ===
    // 拆分成两个方法
    private void AdjustScrollViewPosition()
    {
      // Transform scrollView = _contentParent?.parent?.parent;
      // if (scrollView != null)
      // {
      //     RectTransform rect = scrollView.GetComponent<RectTransform>();
      //     if (rect != null)
      //     {
      //         // ❌❌❌ 罪魁祸首在这里！删除下面这一行！ ❌❌❌
      //         // rect.anchoredPosition = new Vector2(542.89f, -290.8f); 

      //         // ✅ 改为：什么都不做，或者仅仅打印一下当前的日志供我们确认
      //         PotatoOptimization.Core.PotatoPlugin.Log.LogInfo($"[UI Fix] ScrollView natural position: {rect.anchoredPosition}");

      //         // 既然不移动了，我们只要确保它的 Anchor 是填充父物体的即可 (防御性代码)
      //         // 通常 ScrollView 应该填满整个 Setting 页面
      //         rect.anchorMin = Vector2.zero;
      //         rect.anchorMax = Vector2.one;
      //         rect.sizeDelta = Vector2.zero; 
      //         rect.anchoredPosition = Vector2.zero;
      //     }
      // }
      PotatoOptimization.Core.PotatoPlugin.Log.LogInfo("[UI] AdjustScrollViewPosition called - doing nothing (Legacy code disabled)");
    }

    private void AdjustHeaderPosition(string modName)
    {
      // string headerName = $"Header_{modName}";
      // Transform header = _contentParent?.Find(headerName);
      // if (header != null)
      // {
      //   RectTransform headerRect = header.GetComponent<RectTransform>();
      //   if (headerRect != null)
      //   {
      //     Vector3 pos = headerRect.anchoredPosition;
      //     headerRect.anchoredPosition = new Vector2(200f, pos.y);
      //     PotatoOptimization.Core.PotatoPlugin.Log.LogInfo($"[UI] ✅ Header '{headerName}' adjusted to {headerRect.anchoredPosition}");
      //   }
      // }
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

    // === 核心方法：强制修正布局（解决文字挤压问题） ===
    // 在 ModSettingsManager.cs 中找到 EnforceLayout 方法并替换为以下内容

    // === 核心方法：强制修正布局（解决文字挤压及飞出屏幕问题） ===
    private void EnforceLayout(GameObject obj)
    {
      // 1. 【关键修复】强制重置 RectTransform 以适应 VerticalLayoutGroup
      // 原版控件可能使用了 (0.5, 0.5) 居中或 (1, 1) 右上角锚点，这会导致在 LayoutGroup 中计算出错误的偏移
      var rt = obj.GetComponent<RectTransform>();
      if (rt != null)
      {
        // 强制设为左上角对齐，这是 VerticalLayoutGroup 最喜欢的格式
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0.5f, 1f); // X轴中心，Y轴顶部

        // 修正位置和旋转
        rt.anchoredPosition = Vector2.zero; // 让 LayoutGroup 去计算具体的 Y 轴位置
        obj.transform.localPosition = Vector3.zero; // 双重保险
        obj.transform.localScale = Vector3.one;
        obj.transform.localRotation = Quaternion.identity;
      }

      // 3. 寻找 Label 并强制设置宽度 (原有逻辑保留)
      var texts = obj.GetComponentsInChildren<TMP_Text>(true);
      foreach (var t in texts)
      {
        // 只处理左侧的标题文字 (排除掉按钮内部的文字)
        // 增加判定：通常标题是在最左边的，或者名字里包含 Title
        // 原判定 logic: if (t.transform.position.x < obj.transform.position.x + 100 || t.name.Contains("Title"))
        // 在 obj 位置归零前，position 对比可能不准，建议主要依赖名称或层级

        if (t.name.Contains("Title") || t.name.Contains("Label") || t.name == "Text")
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

      // 4. 确保根物体也有 LayoutElement，否则 LayoutGroup 可能把它压扁
      var rootLE = obj.GetComponent<LayoutElement>();
      if (rootLE == null) rootLE = obj.AddComponent<LayoutElement>();

      // 给一个默认高度，防止被压成 0
      if (rootLE.minHeight < 10) rootLE.minHeight = 60f;
      if (rootLE.preferredHeight < 10) rootLE.preferredHeight = 60f;
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