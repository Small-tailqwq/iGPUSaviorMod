using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using R3;

namespace ModShared
{
    /// <summary>
    /// 公共的MOD设置管理器 - 所有MOD共享
    /// </summary>
    public class ModSettingsManager : MonoBehaviour
    {
        public static ModSettingsManager Instance { get; private set; }
        
        public GameObject ModTabButton { get; private set; }
        public GameObject ModContentParent { get; private set; }
        public ScrollRect ModScrollRect { get; private set; }
        
        private List<string> registeredMods = new List<string>();
        public bool IsInitialized { get; private set; }
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        public void Initialize(GameObject tabButton, GameObject contentParent, ScrollRect scrollRect)
        {
            if (IsInitialized)
            {
                Debug.LogWarning("[ModSettings] Already initialized!");
                return;
            }
            
            ModTabButton = tabButton;
            ModContentParent = contentParent;
            ModScrollRect = scrollRect;
            IsInitialized = true;
            
            Debug.Log("[ModSettings] Mod Settings tab initialized!");
        }
        
        public GameObject RegisterMod(string modName, string modVersion)
        {
            if (!IsInitialized)
            {
                Debug.LogError($"[ModSettings] Not initialized! {modName} cannot register.");
                return null;
            }
            
            if (registeredMods.Contains(modName))
            {
                Debug.LogWarning($"[ModSettings] {modName} already registered!");
                return null;
            }
            
            registeredMods.Add(modName);
            GameObject modSection = CreateModSection(modName, modVersion);
            
            Debug.Log($"[ModSettings] {modName} v{modVersion} registered successfully!");
            return modSection;
        }
        
        private GameObject CreateModSection(string modName, string modVersion)
        {
            GameObject section = new GameObject($"ModSection_{modName}");
            section.transform.SetParent(ModContentParent.transform, false);
            
            var layout = section.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 15;
            layout.padding = new RectOffset(20, 20, 15, 15);
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            
            var fitter = section.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            CreateSectionTitle(section.transform, modName, modVersion);
            
            if (registeredMods.Count > 1)
            {
                CreateDivider(section.transform);
            }
            
            return section;
        }
        
        private void CreateSectionTitle(Transform parent, string modName, string version)
        {
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(parent, false);
            
            var titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = $"<b>{modName}</b> <size=16><color=#888888>v{version}</color></size>";
            titleText.fontSize = 24;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = Color.white;
            
            var rectTransform = titleObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(0, 40);
        }
        
        private void CreateDivider(Transform parent)
        {
            GameObject divider = new GameObject("Divider");
            divider.transform.SetParent(parent, false);
            
            var image = divider.AddComponent<Image>();
            image.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            
            var rectTransform = divider.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(0, 2);
            
            var layout = divider.AddComponent<LayoutElement>();
            layout.minHeight = 2;
            layout.preferredHeight = 2;
        }
        
        /// <summary>
        /// 添加开关（Toggle）- 使用默认父容器
        /// </summary>
        public GameObject AddToggle(string label, bool defaultValue, Action<bool> onValueChanged)
        {
            if (ModContentParent == null)
            {
                throw new InvalidOperationException("ModContentParent 未初始化，请先调用 Initialize()");
            }
            return AddToggle(ModContentParent, label, defaultValue, onValueChanged);
        }
        
        /// <summary>
        /// 添加开关（Toggle）- 指定父容器
        /// </summary>
        public GameObject AddToggle(GameObject parent, string label, bool defaultValue, Action<bool> onValueChanged)
        {
            GameObject row = CreateSettingRow(parent.transform, label);
            
            // 创建Toggle容器
            GameObject toggleContainer = new GameObject("ToggleContainer");
            toggleContainer.transform.SetParent(row.transform, false);
            
            var containerRect = toggleContainer.AddComponent<RectTransform>();
            containerRect.sizeDelta = new Vector2(44, 44);  // ✅ 改为 44x44（与 iGPU Savior 一致）
            
            // 创建背景
            GameObject toggleBg = new GameObject("Background");
            toggleBg.transform.SetParent(toggleContainer.transform, false);
            
            var bgRect = toggleBg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            
            var bgImage = toggleBg.AddComponent<Image>();
            bgImage.color = new Color(0.29f, 0.29f, 0.32f, 0.5f);  // ✅ 改为深灰蓝（与 iGPU Savior 一致）
            
            // 创建Checkmark
            GameObject checkmark = new GameObject("Checkmark");
            checkmark.transform.SetParent(toggleContainer.transform, false);
            
            var checkRect = checkmark.AddComponent<RectTransform>();
            checkRect.sizeDelta = new Vector2(32, 32);  // ✅ 改为 32x32（与 iGPU Savior 一致）
            checkRect.anchoredPosition = Vector2.zero;
            
            var checkImage = checkmark.AddComponent<Image>();
            checkImage.color = Color.white;
            checkImage.sprite = CreateCheckmarkSprite();
            
            // 创建Toggle组件
            var toggle = toggleContainer.AddComponent<Toggle>();
            toggle.targetGraphic = bgImage;
            toggle.graphic = checkImage;
            toggle.isOn = defaultValue;
            
            if (onValueChanged != null)
            {
                toggle.onValueChanged.AddListener(onValueChanged.Invoke);
            }
            
            return row;
        }
        
        /// <summary>
        /// 添加下拉菜单（Dropdown）- 使用默认父容器
        /// </summary>
        public GameObject AddDropdown(string label, List<string> options, int defaultIndex, Action<int> onValueChanged)
        {
            if (ModContentParent == null)
            {
                throw new InvalidOperationException("ModContentParent 未初始化，请先调用 Initialize()");
            }
            return AddDropdown(ModContentParent, label, options, defaultIndex, onValueChanged);
        }
        
        /// <summary>
        /// 添加下拉菜单（Dropdown）- 指定父容器
        /// </summary>
        public GameObject AddDropdown(GameObject parent, string label, List<string> options, int defaultIndex, Action<int> onValueChanged)
        {
            GameObject row = CreateSettingRow(parent.transform, label);
            
            // 创建Dropdown容器
            GameObject dropdownObj = new GameObject("Dropdown");
            dropdownObj.transform.SetParent(row.transform, false);
            
            var dropdownRect = dropdownObj.AddComponent<RectTransform>();
            dropdownRect.sizeDelta = new Vector2(200, 44);  // ✅ 改为 44（与 iGPU Savior 一致）
            
            // 添加背景Image
            var bgImage = dropdownObj.AddComponent<Image>();
            bgImage.color = new Color(0.29f, 0.29f, 0.32f, 0.8f);  // ✅ 改为深灰蓝（与 iGPU Savior 一致）
            
            // 创建Label (显示当前选中项)
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(dropdownObj.transform, false);
            
            var labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.offsetMin = new Vector2(10, 0);
            labelRect.offsetMax = new Vector2(-30, 0);
            
            var labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.fontSize = 24;  // ✅ 改为 24（与 iGPU Savior 一致）
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            labelText.color = Color.white;
            
            // 创建箭头
            GameObject arrowObj = new GameObject("Arrow");
            arrowObj.transform.SetParent(dropdownObj.transform, false);
            
            var arrowRect = arrowObj.AddComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1, 0.5f);
            arrowRect.anchorMax = new Vector2(1, 0.5f);
            arrowRect.pivot = new Vector2(0.5f, 0.5f);
            arrowRect.sizeDelta = new Vector2(20, 20);
            arrowRect.anchoredPosition = new Vector2(-15, 0);
            
            var arrowText = arrowObj.AddComponent<TextMeshProUGUI>();
            arrowText.text = "▼";
            arrowText.fontSize = 14;
            arrowText.alignment = TextAlignmentOptions.Center;
            arrowText.color = Color.white;
            
            // 创建Dropdown组件
            var dropdown = dropdownObj.AddComponent<TMP_Dropdown>();
            dropdown.targetGraphic = bgImage;
            dropdown.captionText = labelText;
            
            // 添加选项
            dropdown.ClearOptions();
            dropdown.AddOptions(options);
            dropdown.value = defaultIndex;
            dropdown.RefreshShownValue();
            
            if (onValueChanged != null)
            {
                dropdown.onValueChanged.AddListener(onValueChanged.Invoke);
            }
            
            return row;
        }
        
        /// <summary>
        /// 创建设置行（标签 + 控件）
        /// </summary>
        private GameObject CreateSettingRow(Transform parent, string label)
        {
            GameObject row = new GameObject($"Row_{label}");
            row.transform.SetParent(parent, false);
            
            // ✅ 配置 RectTransform - 与原生 UI 元素一致
            var rowRect = row.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0, 1);  // 左上角锚点
            rowRect.anchorMax = new Vector2(1, 1);  // 右上角锚点
            rowRect.pivot = new Vector2(0, 1);      // ❌ 改为左上角为轴心（不是中心）
            rowRect.anchoredPosition = Vector2.zero; // 位置 (0, 0)
            rowRect.sizeDelta = new Vector2(0, 72); // 宽度自适应，高度 72
            
            var hLayout = row.AddComponent<HorizontalLayoutGroup>();
            hLayout.childControlHeight = false;
            hLayout.childControlWidth = false;
            hLayout.childForceExpandHeight = false;
            hLayout.childForceExpandWidth = false;
            hLayout.spacing = 20;
            hLayout.childAlignment = TextAnchor.MiddleLeft;
            hLayout.padding = new RectOffset(20, 20, 12, 12);  // ✅ 添加内边距
            
            var layoutElement = row.AddComponent<LayoutElement>();
            layoutElement.minHeight = 72;  // ✅ 改为 72
            layoutElement.preferredHeight = 72;  // ✅ 改为 72
            
            // 创建标签
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(row.transform, false);
            
            var labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 32;  // ✅ 改为 32（与 iGPU Savior 一致）
            labelText.color = new Color(0.8f, 0.8f, 0.8f, 1f);  // ✅ 改为浅灰
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            
            var labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(300, 72);  // ✅ 改为 72
            
            var labelLayout = labelObj.AddComponent<LayoutElement>();
            labelLayout.minWidth = 300;
            labelLayout.preferredWidth = 300;
            
            return row;
        }
        
        /// <summary>
        /// 创建简单的Checkmark Sprite
        /// </summary>
        private Sprite CreateCheckmarkSprite()
        {
            // 创建一个简单的勾号纹理
            Texture2D texture = new Texture2D(32, 32);
            Color[] pixels = new Color[32 * 32];
            
            // 填充透明
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }
            
            // 画勾号（简化版）
            for (int i = 10; i < 20; i++)
            {
                pixels[i * 32 + 15] = Color.white;
                pixels[i * 32 + 16] = Color.white;
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
        }
    }
}
