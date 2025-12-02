using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Bulbul;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using ModShared;
using BepInEx.Configuration;

namespace PotatoOptimization
{
    /// <summary>
    /// 现代化UI设计系统
    /// </summary>
    public static class ModernUIStyle
    {
        public static class Colors
        {
            // Chrome风格配色
            public static readonly Color Background = new Color(0.11f, 0.11f, 0.13f, 1f);
            public static readonly Color Surface = new Color(0.15f, 0.15f, 0.17f, 1f);
            public static readonly Color SurfaceHover = new Color(0.18f, 0.18f, 0.20f, 1f);
            public static readonly Color Primary = new Color(0.26f, 0.52f, 0.96f, 1f);
            public static readonly Color PrimaryHover = new Color(0.20f, 0.46f, 0.90f, 1f);
            public static readonly Color TextPrimary = new Color(0.95f, 0.95f, 0.95f, 1f);
            public static readonly Color TextSecondary = new Color(0.70f, 0.70f, 0.72f, 1f);
            public static readonly Color Divider = new Color(0.25f, 0.25f, 0.27f, 1f);
            public static readonly Color Success = new Color(0.20f, 0.73f, 0.45f, 1f);
        }

        public static class Sizes
        {
            public const float RowHeight = 72f;
            public const float HeaderHeight = 48f;
            public const float DropdownHeight = 48f;
            public const float ToggleSize = 44f;
            public const float Spacing = 16f;
        }
    }

    [HarmonyPatch(typeof(SettingUI), "Setup")]
    public class ModSettingsIntegration
    {
        private static GameObject modContentParent;
        private static InteractableUI modInteractableUI;
        private static SettingUI cachedSettingUI;
        private static bool useGraphicsContent = false;
        
        // UI资源缓存
        private static TMP_FontAsset _cachedFont;
        private static Sprite _cachedRoundedSprite;
        private static Canvas _rootCanvas;

        static void Postfix(SettingUI __instance)
        {
            try
            {
                cachedSettingUI = __instance;
                
                // 获取根Canvas
                _rootCanvas = __instance.GetComponentInParent<Canvas>();
                if (_rootCanvas == null)
                {
                    // 尝试在场景中查找
                    _rootCanvas = Object.FindObjectOfType<Canvas>();
                }
                
                if (ModSettingsManager.Instance != null && ModSettingsManager.Instance.IsInitialized)
                {
                    RegisterModToExistingTab();
                }
                else
                {
                    CreateModSettingsTab(__instance);
                }
                
                HookIntoTabButtons(__instance);
            }
            catch (System.Exception e)
            {
                PotatoPlugin.Log.LogError($"MOD设置集成失败: {e.Message}\n{e.StackTrace}");
            }
        }

        static void CreateModSettingsTab(SettingUI settingUI)
        {
            try
            {
                var creditsButton = AccessTools.Field(typeof(SettingUI), "_creditsInteractableUI")
                    .GetValue(settingUI) as InteractableUI;
                var creditsParent = AccessTools.Field(typeof(SettingUI), "_creditsParent")
                    .GetValue(settingUI) as GameObject;

                if (creditsButton == null || creditsParent == null)
                {
                    PotatoPlugin.Log.LogError("无法找到Credits按钮或面板");
                    return;
                }

                // 克隆按钮
                GameObject modTabButton = Object.Instantiate(creditsButton.gameObject);
                modTabButton.name = "ModSettingsTabButton";
                modTabButton.transform.SetParent(creditsButton.transform.parent, false);
                modTabButton.transform.SetSiblingIndex(creditsButton.transform.GetSiblingIndex() + 1);

                // 克隆内容面板
                modContentParent = Object.Instantiate(creditsParent);
                modContentParent.name = "ModSettingsContent";
                modContentParent.transform.SetParent(creditsParent.transform.parent, false);
                modContentParent.SetActive(false);

                var scrollRect = modContentParent.GetComponentInChildren<ScrollRect>();
                if (scrollRect == null)
                {
                    PotatoPlugin.Log.LogError("无法找到ScrollRect组件");
                    return;
                }

                                // �� Graphics ҳ Content ���������ȫһ�£����Է��?
                Transform GraphicsContent = GetGraphicsContentTransform();
                useGraphicsContent = GraphicsContent != null;
                var content = useGraphicsContent ? GraphicsContent : scrollRect.content;

                // ���浱ǰͼ��ҳ������(ֻ�ռ���ǰֱ���Ӷ���)
                if (useGraphicsContent && GraphicsOriginalChildren.Count == 0)
                {
                    for (int i = 0; i < content.childCount; i++)
                    {
                        GraphicsOriginalChildren.Add(content.GetChild(i));
                    }
                }

                // ֻ�� Mod ҳ�б���ʱ�Ŵ���ԭ���
                if (!useGraphicsContent)
                {
                    foreach (Transform child in content)
                    {
                        Object.Destroy(child.gameObject);
                    }
                }
                ConfigureContentLayout(content.gameObject);

                // 创建管理器
                GameObject managerObj = new GameObject("ModSettingsManager");
                Object.DontDestroyOnLoad(managerObj);
                var manager = managerObj.AddComponent<ModSettingsManager>();
                manager.Initialize(modTabButton, content.gameObject, scrollRect);

                // ✅ 使用 ModUICoroutineRunner 延迟更新UI
                ModUICoroutineRunner.Instance.RunDelayed(0.3f, () => {
                    UpdateModButtonText(modTabButton);
                    UpdateModContentText(modContentParent);
                    AdjustTabBarLayout(creditsButton.transform.parent);
                });

                // 设置按钮事件
                modInteractableUI = modTabButton.GetComponent<InteractableUI>();
                if (modInteractableUI != null)
                {
                    modInteractableUI.Setup();
                    var btn = modInteractableUI.GetComponent<Button>();
                    btn?.onClick.AddListener(() => SwitchToModTab(settingUI));
                }

                RegisterCurrentMod(manager);
                
                PotatoPlugin.Log.LogInfo(">>> MOD设置标签创建成功 <<<");
            }
            catch (System.Exception e)
            {
                PotatoPlugin.Log.LogError($"创建MOD标签失败: {e.Message}\n{e.StackTrace}");
            }
        }

        static void ConfigureContentLayout(GameObject content)
        {
            var vGroup = content.GetComponent<VerticalLayoutGroup>();
            if (vGroup == null) vGroup = content.AddComponent<VerticalLayoutGroup>();
            
            vGroup.spacing = ModernUIStyle.Sizes.Spacing;
            vGroup.padding = new RectOffset(32, 32, 24, 24);
            vGroup.childControlHeight = false;
            vGroup.childControlWidth = true;
            vGroup.childForceExpandHeight = false;
            vGroup.childForceExpandWidth = true;

            var fitter = content.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        static void PrepareUIResources()
        {
            if (_cachedFont != null) return;

            try
            {
                // 获取字体
                var anyText = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>()
                    .FirstOrDefault(t => t.font != null);
                if (anyText != null) _cachedFont = anyText.font;

                // 获取圆角Sprite
                _cachedRoundedSprite = Resources.FindObjectsOfTypeAll<Sprite>()
                    .FirstOrDefault(s => s != null && (s.name.Contains("UISprite") || s.name.Contains("Background")));
                
                if (_cachedRoundedSprite == null)
                {
                    // 备用：从任何Image组件获取
                    var anyImage = Resources.FindObjectsOfTypeAll<Image>()
                        .FirstOrDefault(i => i.sprite != null);
                    if (anyImage != null) _cachedRoundedSprite = anyImage.sprite;
                }
            }
            catch (System.Exception e)
            {
                PotatoPlugin.Log.LogWarning($"准备UI资源时出错: {e.Message}");
            }
        }

        /// <summary>
        /// 注册到已存在的MOD标签页
        /// </summary>
        static void RegisterModToExistingTab()
        {
            if (ModSettingsManager.Instance == null)
            {
                PotatoPlugin.Log.LogWarning("ModSettingsManager.Instance is null");
                return;
            }

            RegisterCurrentMod(ModSettingsManager.Instance);
        }

        static void RegisterCurrentMod(ModSettingsManager manager)
        {
            // ✅ 使用 ModUICoroutineRunner
            ModUICoroutineRunner.Instance.RunDelayed(0.5f, () => {
                if (modContentParent == null)
                {
                    PotatoPlugin.Log.LogWarning("modContentParent is null");
                    return;
                }

                var scrollRect = modContentParent.GetComponentInChildren<ScrollRect>();
                if (scrollRect == null)
                {
                    PotatoPlugin.Log.LogWarning("ScrollRect not found");
                    return;
                }

                                // �� Graphics ҳ Content ���������ȫһ�£����Է��?
                Transform GraphicsContent = GetGraphicsContentTransform();
                useGraphicsContent = false;
                var content = scrollRect.content;

                if (GraphicsContent != null)
                {
                    CopyLayoutFromGraphics(GraphicsContent, content);
                }

                foreach (Transform child in content)
                {
                    Object.Destroy(child.gameObject);
                }
                PrepareUIResources();

                // === ✅ 使用游戏原生风格构建 UI ===
                
                CreateSectionHeader(content, "基础设置");
                
                // ✅ 1. 使用游戏原生的开关（克隆 VerticalSync）
                GameObject mirrorToggle = ModToggleCloner.CreateToggleGroup(
                    cachedSettingUI.transform, 
                    "画面镜像", 
                    PotatoPlugin.CfgEnableMirror.Value,
                    val => {
                        PotatoPlugin.CfgEnableMirror.Value = val;
                        // ✅ 立即应用镜像（不需要等到下次按 F4）
                        PotatoController controller = Object.FindObjectOfType<PotatoController>();
                        if (controller != null)
                        {
                            controller.SetMirrorState(val);
                            PotatoPlugin.Log.LogInfo($"通过UI设置镜像状态: {(val ? "ON" : "OFF")}");
                        }
                        else
                        {
                            PotatoPlugin.Log.LogWarning("未找到 PotatoController 实例");
                        }
                    }
                );
                
                if (mirrorToggle != null)
                {
                    mirrorToggle.transform.SetParent(content, false);
                    mirrorToggle.SetActive(true);
                    PotatoPlugin.Log.LogInfo("✅ 成功添加画面镜像开关（游戏原生风格）");
                }
                else
                {
                    PotatoPlugin.Log.LogError("❌ 创建画面镜像开关失败");
                }

                // 2. 小窗缩放比例下拉框（游戏原生风格）
                CreateNativeDropdown(content, "小窗缩放比例",
                    new List<string> { "1/3 大小", "1/4 大小", "1/5 大小" },
                    (int)PotatoPlugin.CfgWindowScale.Value - 3,
                    index => {
                        PotatoPlugin.CfgWindowScale.Value = (WindowScaleRatio)(index + 3);
                        PotatoPlugin.Log.LogInfo($"小窗缩放比例: {PotatoPlugin.CfgWindowScale.Value}");
                    });

                // 3. 拖动方式下拉框（游戏原生风格）
                CreateNativeDropdown(content, "小窗拖动方式",
                    new List<string> { "Ctrl + 左键", "Alt + 左键", "右键按住" },
                    (int)PotatoPlugin.CfgDragMode.Value,
                    index => {
                        PotatoPlugin.CfgDragMode.Value = (DragMode)index;
                        PotatoPlugin.Log.LogInfo($"拖动方式: {PotatoPlugin.CfgDragMode.Value}");
                    });

                CreateSectionHeader(content, "快捷键设置");
                
                var keyOptions = GetFunctionKeyOptions();
                
                CreateNativeDropdown(content, "土豆模式快捷键",
                    keyOptions, GetKeyCodeIndex(PotatoPlugin.KeyPotatoMode.Value),
                    index => {
                        PotatoPlugin.KeyPotatoMode.Value = GetKeyCodeFromIndex(index);
                        PotatoPlugin.Log.LogInfo($"土豆模式快捷键: {PotatoPlugin.KeyPotatoMode.Value}");
                    });

                CreateNativeDropdown(content, "小窗模式快捷键",
                    keyOptions, GetKeyCodeIndex(PotatoPlugin.KeyPiPMode.Value),
                    index => {
                        PotatoPlugin.KeyPiPMode.Value = GetKeyCodeFromIndex(index);
                        PotatoPlugin.Log.LogInfo($"小窗模式快捷键: {PotatoPlugin.KeyPiPMode.Value}");
                    });

                CreateNativeDropdown(content, "镜像快捷键",
                    keyOptions, GetKeyCodeIndex(PotatoPlugin.KeyCameraMirror.Value),
                    index => {
                        PotatoPlugin.KeyCameraMirror.Value = GetKeyCodeFromIndex(index);
                        PotatoPlugin.Log.LogInfo($"镜像快捷键: {PotatoPlugin.KeyCameraMirror.Value}");
                    });

                // �򿪺����±��㣬������Ŀ����Ǹ���Transform
                var contentRect = content as RectTransform ?? content.GetComponent<RectTransform>();
                if (contentRect != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
                }
            });
        }

        private static Transform GetGraphicsContentTransform()
        {
            if (cachedSettingUI == null) return null;
            var t = cachedSettingUI.transform.Find("Graphics/ScrollView/Viewport/Content");
            if (t == null)
            {
                PotatoPlugin.Log.LogWarning("δ�ҵ� Graphics/ScrollView/Viewport/Content, ����Modҳ�Զ��� layout");
            }
            return t;
        }
        // ==================== UI组件构建方法 ====================

        static void CreateSectionHeader(Transform parent, string text)
        {
            GameObject obj = new GameObject("SectionHeader");
            obj.transform.SetParent(parent, false);

            var le = obj.AddComponent<LayoutElement>();
            le.minHeight = ModernUIStyle.Sizes.HeaderHeight;
            le.preferredHeight = ModernUIStyle.Sizes.HeaderHeight;

            var tmp = obj.AddComponent<TextMeshProUGUI>();
            if (_cachedFont != null) tmp.font = _cachedFont;
            tmp.text = text;
            tmp.fontSize = 22;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = ModernUIStyle.Colors.Primary;
            tmp.alignment = TextAlignmentOptions.BottomLeft;
            tmp.margin = new Vector4(0, 0, 0, 8);
        }

        static void CreateModernToggle(Transform parent, string label, string description, 
            bool isOn, System.Action<bool> onValueChanged)
        {
            GameObject row = new GameObject($"Toggle_{label}");
            row.transform.SetParent(parent, false);

            var bg = row.AddComponent<Image>();
            bg.color = ModernUIStyle.Colors.Surface;
            if (_cachedRoundedSprite != null)
            {
                bg.sprite = _cachedRoundedSprite;
                bg.type = Image.Type.Sliced;
            }

            var hGroup = row.AddComponent<HorizontalLayoutGroup>();
            hGroup.padding = new RectOffset(24, 20, 16, 16);
            hGroup.spacing = 16;
            hGroup.childAlignment = TextAnchor.MiddleLeft;
            hGroup.childControlWidth = false;
            hGroup.childControlHeight = false;

            var le = row.AddComponent<LayoutElement>();
            le.minHeight = ModernUIStyle.Sizes.RowHeight;
            le.preferredHeight = ModernUIStyle.Sizes.RowHeight;

            // 文本区域
            GameObject textArea = new GameObject("TextArea");
            textArea.transform.SetParent(row.transform, false);

            var textLe = textArea.AddComponent<LayoutElement>();
            textLe.flexibleWidth = 1;

            var vGroup = textArea.AddComponent<VerticalLayoutGroup>();
            vGroup.spacing = 4;
            vGroup.childControlHeight = false;
            vGroup.childControlWidth = true;

            CreateLabel(textArea.transform, label, 28, ModernUIStyle.Colors.TextPrimary);
            
            if (!string.IsNullOrEmpty(description))
            {
                CreateLabel(textArea.transform, description, 22, ModernUIStyle.Colors.TextSecondary);
            }

            CreateChromeStyleToggle(row.transform, isOn, onValueChanged);
        }

        static void CreateChromeStyleToggle(Transform parent, bool isOn, System.Action<bool> onValueChanged)
        {
            GameObject toggleRoot = new GameObject("ChromeToggle");
            toggleRoot.transform.SetParent(parent, false);

            var le = toggleRoot.AddComponent<LayoutElement>();
            le.minWidth = 48;
            le.minHeight = 28;
            le.preferredWidth = 48;
            le.preferredHeight = 28;

            var trackBg = toggleRoot.AddComponent<Image>();
            trackBg.color = isOn ? ModernUIStyle.Colors.Primary : new Color(0.3f, 0.3f, 0.32f, 1f);
            if (_cachedRoundedSprite != null)
            {
                trackBg.sprite = _cachedRoundedSprite;
                trackBg.type = Image.Type.Sliced;
            }

            // 滑块
            GameObject knob = new GameObject("Knob");
            knob.transform.SetParent(toggleRoot.transform, false);

            var knobRect = knob.AddComponent<RectTransform>();
            knobRect.sizeDelta = new Vector2(20, 20);
            knobRect.anchorMin = new Vector2(isOn ? 1f : 0f, 0.5f);
            knobRect.anchorMax = new Vector2(isOn ? 1f : 0f, 0.5f);
            knobRect.anchoredPosition = new Vector2(isOn ? -4f : 4f, 0);

            var knobImg = knob.AddComponent<Image>();
            knobImg.color = Color.white;
            if (_cachedRoundedSprite != null)
            {
                knobImg.sprite = _cachedRoundedSprite;
                knobImg.type = Image.Type.Sliced;
            }

            var shadow = knob.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.3f);
            shadow.effectDistance = new Vector2(0, -2);

            var btn = toggleRoot.AddComponent<Button>();
            btn.targetGraphic = trackBg;
            
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
            btn.colors = colors;

            btn.onClick.AddListener(() => {
                bool newState = !isOn;
                isOn = newState;

                trackBg.color = newState ? ModernUIStyle.Colors.Primary : new Color(0.3f, 0.3f, 0.32f, 1f);
                knobRect.anchorMin = new Vector2(newState ? 1f : 0f, 0.5f);
                knobRect.anchorMax = new Vector2(newState ? 1f : 0f, 0.5f);
                knobRect.anchoredPosition = new Vector2(newState ? -4f : 4f, 0);

                onValueChanged?.Invoke(newState);
            });
        }

        static void CreateModernDropdown(Transform parent, string label, string description, 
            List<string> options, int currentIndex, System.Action<int> onValueChanged)
        {
            GameObject row = new GameObject($"Dropdown_{label}");
            row.transform.SetParent(parent, false);

            var bg = row.AddComponent<Image>();
            bg.color = ModernUIStyle.Colors.Surface;
            if (_cachedRoundedSprite != null)
            {
                bg.sprite = _cachedRoundedSprite;
                bg.type = Image.Type.Sliced;
            }

            var hGroup = row.AddComponent<HorizontalLayoutGroup>();
            hGroup.padding = new RectOffset(24, 20, 16, 16);
            hGroup.spacing = 16;
            hGroup.childAlignment = TextAnchor.MiddleLeft;
            hGroup.childControlWidth = false;
            hGroup.childControlHeight = false;

            var le = row.AddComponent<LayoutElement>();
            le.minHeight = ModernUIStyle.Sizes.RowHeight;
            le.preferredHeight = ModernUIStyle.Sizes.RowHeight;

            // 文本区域
            GameObject textArea = new GameObject("TextArea");
            textArea.transform.SetParent(row.transform, false);

            var textLe = textArea.AddComponent<LayoutElement>();
            textLe.flexibleWidth = 1;

            var vGroup = textArea.AddComponent<VerticalLayoutGroup>();
            vGroup.spacing = 4;
            vGroup.childControlHeight = false;
            vGroup.childControlWidth = true;

            CreateLabel(textArea.transform, label, 28, ModernUIStyle.Colors.TextPrimary);
            
            if (!string.IsNullOrEmpty(description))
            {
                CreateLabel(textArea.transform, description, 22, ModernUIStyle.Colors.TextSecondary);
            }

            // 下拉框按钮
            GameObject dropdownRoot = new GameObject("DropdownButton");
            dropdownRoot.transform.SetParent(row.transform, false);

            var dropLe = dropdownRoot.AddComponent<LayoutElement>();
            dropLe.minWidth = 200;
            dropLe.preferredWidth = 200;
            dropLe.minHeight = ModernUIStyle.Sizes.DropdownHeight;
            dropLe.preferredHeight = ModernUIStyle.Sizes.DropdownHeight;

            var dropBg = dropdownRoot.AddComponent<Image>();
            dropBg.color = ModernUIStyle.Colors.Background;
            if (_cachedRoundedSprite != null)
            {
                dropBg.sprite = _cachedRoundedSprite;
                dropBg.type = Image.Type.Sliced;
            }

            var dropBtn = dropdownRoot.AddComponent<Button>();
            var btnColors = dropBtn.colors;
            btnColors.normalColor = Color.white;
            btnColors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            dropBtn.colors = btnColors;

            // 当前显示文本
            GameObject currentTextObj = new GameObject("CurrentText");
            currentTextObj.transform.SetParent(dropdownRoot.transform, false);

            var currentRect = currentTextObj.AddComponent<RectTransform>();
            currentRect.anchorMin = Vector2.zero;
            currentRect.anchorMax = Vector2.one;
            currentRect.offsetMin = new Vector2(16, 0);
            currentRect.offsetMax = new Vector2(-40, 0);

            var currentText = currentTextObj.AddComponent<TextMeshProUGUI>();
            if (_cachedFont != null) currentText.font = _cachedFont;
            currentText.fontSize = 24;
            currentText.alignment = TextAlignmentOptions.MidlineLeft;
            currentText.color = ModernUIStyle.Colors.TextPrimary;
            currentText.text = (options.Count > currentIndex && currentIndex >= 0) ? options[currentIndex] : "";

            // 箭头图标
            GameObject arrow = CreateArrowIcon(dropdownRoot.transform);

            // === 关键修复：找到最顶层的 Canvas ===
            Canvas highestCanvas = Object.FindObjectsOfType<Canvas>()
                .Where(c => c.isActiveAndEnabled)
                .OrderByDescending(c => c.sortingOrder)
                .FirstOrDefault();

            if (highestCanvas == null)
            {
                PotatoPlugin.Log.LogError("[Dropdown] 无法找到任何激活的 Canvas，下拉列表可能无法显示！");
                return;
            }

            // === 创建弹出列表（挂到高层级 Canvas 下）===
            GameObject listRoot = new GameObject($"DropdownList_{label}");
            listRoot.transform.SetParent(highestCanvas.transform, false);
            listRoot.SetActive(false);

            // 让列表总是在最前面渲染
            var listCanvas = listRoot.AddComponent<Canvas>();
            listCanvas.overrideSorting = true;
            listCanvas.sortingOrder = highestCanvas.sortingOrder + 100; // 比父 Canvas 高
            listRoot.AddComponent<GraphicRaycaster>(); // 允许接收点击

            var listRect = listRoot.GetComponent<RectTransform>(); // Canvas 会自动添加 RectTransform
            listRect.anchorMin = Vector2.zero;
            listRect.anchorMax = Vector2.zero;
            listRect.pivot = new Vector2(0, 1); // 左上角为锚点

            var listBg = listRoot.AddComponent<Image>();
            listBg.color = ModernUIStyle.Colors.Surface;
            if (_cachedRoundedSprite != null)
            {
                listBg.sprite = _cachedRoundedSprite;
                listBg.type = Image.Type.Sliced;
            }

            var listShadow = listRoot.AddComponent<Shadow>();
            listShadow.effectColor = new Color(0, 0, 0, 0.5f);
            listShadow.effectDistance = new Vector2(0, -4);

            var listVGroup = listRoot.AddComponent<VerticalLayoutGroup>();
            listVGroup.padding = new RectOffset(8, 8, 8, 8);
            listVGroup.spacing = 4;
            listVGroup.childControlHeight = true;
            listVGroup.childControlWidth = true;

            var listFitter = listRoot.AddComponent<ContentSizeFitter>();
            listFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            listFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained; // 宽度手动设置

            // 创建选项
            int selectedIndex = currentIndex; // 闭包变量
            for (int i = 0; i < options.Count; i++)
            {
                int index = i;
                CreateDropdownOption(listRoot.transform, options[i], index == selectedIndex, () => {
                    selectedIndex = index;
                    currentText.text = options[index];
                    listRoot.SetActive(false);
                    arrow.transform.localRotation = Quaternion.identity; // 恢复箭头
                    onValueChanged?.Invoke(index);
                });
            }

            // 按钮点击：显示 / 隐藏列表
            dropBtn.onClick.AddListener(() => {
                bool isActive = !listRoot.activeSelf;
                listRoot.SetActive(isActive);

                if (isActive)
                {
                    // 更新位置（每次打开时重新计算，以防窗口缩放）
                    PositionDropdownList(dropdownRoot, listRoot, highestCanvas);
                    arrow.transform.localRotation = Quaternion.Euler(0, 0, 180);
                }
                else
                {
                    arrow.transform.localRotation = Quaternion.identity;
                }
            });

            // 点击外部时关闭
            ModUICoroutineRunner.Instance.StartCoroutine(CloseOnClickOutside(listRoot, dropdownRoot, arrow, highestCanvas));
        }

        static void PositionDropdownList(GameObject button, GameObject list, Canvas targetCanvas)
        {
            var buttonRect = button.GetComponent<RectTransform>();
            var listRect = list.GetComponent<RectTransform>();

            // 获取 button 的世界坐标四个角
            Vector3[] corners = new Vector3[4];
            buttonRect.GetWorldCorners(corners);
            Vector3 bottomLeft = corners[0]; // 左下角

            // 转换到目标 Canvas 的局部坐标
            RectTransform canvasRect = targetCanvas.GetComponent<RectTransform>();
            Camera uiCamera = (targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : targetCanvas.worldCamera;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                RectTransformUtility.WorldToScreenPoint(uiCamera, bottomLeft),
                uiCamera,
                out Vector2 localPoint
            );

            // 设置列表的位置（在 button 下方，稍微偏移）
            listRect.anchoredPosition = new Vector2(localPoint.x, localPoint.y - 8);
            
            // 宽度和 button 一致
            listRect.sizeDelta = new Vector2(buttonRect.rect.width, listRect.sizeDelta.y);
        }

        static System.Collections.IEnumerator CloseOnClickOutside(GameObject list, GameObject button, GameObject arrow, Canvas targetCanvas)
        {
            Camera uiCamera = (targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : targetCanvas.worldCamera;

            while (list != null)
            {
                if (list.activeSelf && Input.GetMouseButtonDown(0))
                {
                    Vector2 mousePos = Input.mousePosition;
                    
                    bool clickedOnList = RectTransformUtility.RectangleContainsScreenPoint(
                        list.GetComponent<RectTransform>(), mousePos, uiCamera);
                    
                    bool clickedOnButton = RectTransformUtility.RectangleContainsScreenPoint(
                        button.GetComponent<RectTransform>(), mousePos, uiCamera);

                    if (!clickedOnList && !clickedOnButton)
                    {
                        list.SetActive(false);
                        if (arrow != null)
                        {
                            arrow.transform.localRotation = Quaternion.identity;
                        }
                    }
                }
                yield return null;
            }
        }

        static void CreateDropdownOption(Transform parent, string text, bool isSelected, System.Action onClick)
        {
            GameObject optObj = new GameObject($"Option_{text}");
            optObj.transform.SetParent(parent, false);

            var le = optObj.AddComponent<LayoutElement>();
            le.minHeight = 44;
            le.preferredHeight = 44;

            var btn = optObj.AddComponent<Button>();
            var img = optObj.AddComponent<Image>();
            img.color = isSelected ? ModernUIStyle.Colors.SurfaceHover : Color.clear;

            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            btn.colors = colors;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(optObj.transform, false);

            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16, 0);
            textRect.offsetMax = new Vector2(-16, 0);

            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            if (_cachedFont != null) tmp.font = _cachedFont;
            tmp.fontSize = 24;
            tmp.text = text;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = ModernUIStyle.Colors.TextPrimary;

            btn.onClick.AddListener(() => onClick?.Invoke());
        }

        static GameObject CreateArrowIcon(Transform parent)
        {
            GameObject arrow = new GameObject("Arrow");
            arrow.transform.SetParent(parent, false);

            var rect = arrow.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 0.5f);
            rect.anchorMax = new Vector2(1, 0.5f);
            rect.sizeDelta = new Vector2(20, 20);
            rect.anchoredPosition = new Vector2(-16, 0);

            var text = arrow.AddComponent<TextMeshProUGUI>();
            if (_cachedFont != null) text.font = _cachedFont;
            text.text = "▼";
            text.fontSize = 16;
            text.alignment = TextAlignmentOptions.Center;
            text.color = ModernUIStyle.Colors.TextSecondary;

            return arrow;
        }

        static GameObject CreateLabel(Transform parent, string text, float fontSize, Color color)
        {
            GameObject obj = new GameObject("Label");
            obj.transform.SetParent(parent, false);

            var tmp = obj.AddComponent<TextMeshProUGUI>();
            if (_cachedFont != null) tmp.font = _cachedFont;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.enableWordWrapping = true;

            return obj;
        }

        // ==================== 辅助方法 ====================
        
        static void HookIntoTabButtons(SettingUI settingUI)
        {
            try
            {
                var generalButton = AccessTools.Field(typeof(SettingUI), "_generalInteractableUI").GetValue(settingUI) as InteractableUI;
                var graphicButton = AccessTools.Field(typeof(SettingUI), "_graphicInteractableUI").GetValue(settingUI) as InteractableUI;
                var audioButton = AccessTools.Field(typeof(SettingUI), "_audioInteractableUI").GetValue(settingUI) as InteractableUI;
                var creditsButton = AccessTools.Field(typeof(SettingUI), "_creditsInteractableUI").GetValue(settingUI) as InteractableUI;
                
                var generalParent = AccessTools.Field(typeof(SettingUI), "_generalParent").GetValue(settingUI) as GameObject;
                var graphicParent = AccessTools.Field(typeof(SettingUI), "_graphicParent").GetValue(settingUI) as GameObject;
                var audioParent = AccessTools.Field(typeof(SettingUI), "_audioParent").GetValue(settingUI) as GameObject;
                var creditsParent = AccessTools.Field(typeof(SettingUI), "_creditsParent").GetValue(settingUI) as GameObject;
                
                AddHideModTabListener(generalButton, generalParent);
                AddHideModTabListener(graphicButton, graphicParent);
                AddHideModTabListener(audioButton, audioParent);
                AddHideModTabListener(creditsButton, creditsParent);
            }
            catch (System.Exception e)
            {
                PotatoPlugin.Log.LogError($"挂钩标签按钮失败: {e}");
            }
        }
        
        static void AddHideModTabListener(InteractableUI button, GameObject targetParent)
        {
            if (button == null) return;
            
            var uiButton = button.GetComponent<Button>();
            if (uiButton != null)
            {
                uiButton.onClick.AddListener(() => {
                    if (modContentParent != null && modContentParent.activeSelf)
                    {
                        // ✅ 使用 ModUICoroutineRunner
                        ModUICoroutineRunner.Instance.StartCoroutine(HideModTabAndShowTarget(button, targetParent));
                    }
                });
            }
        }
        
        static System.Collections.IEnumerator HideModTabAndShowTarget(InteractableUI targetButton, GameObject targetParent)
        {
            yield return null;
            
            if (modContentParent != null && !useGraphicsContent)
            {
                modContentParent.SetActive(false);
                modInteractableUI?.DeactivateUseUI(false);
            }
            
            if (targetParent != null && !targetParent.activeSelf)
            {
                targetParent.SetActive(true);
                targetButton?.ActivateUseUI(false);
            }
        }
        
        static void SwitchToModTab(SettingUI settingUI)
        {
            var generalParent = AccessTools.Field(typeof(SettingUI), "_generalParent").GetValue(settingUI) as GameObject;
            var graphicParent = AccessTools.Field(typeof(SettingUI), "_graphicParent").GetValue(settingUI) as GameObject;
            var audioParent = AccessTools.Field(typeof(SettingUI), "_audioParent").GetValue(settingUI) as GameObject;
            var creditsParent = AccessTools.Field(typeof(SettingUI), "_creditsParent").GetValue(settingUI) as GameObject;
            
            generalParent?.SetActive(false);
            // �����ڵ���ͼ��ҳ�Ĳ���(������ͼ��ҳ�� Content)
            graphicParent?.SetActive(useGraphicsContent);
            audioParent?.SetActive(false);
            creditsParent?.SetActive(false);
            
            var generalButton = AccessTools.Field(typeof(SettingUI), "_generalInteractableUI").GetValue(settingUI) as InteractableUI;
            var graphicButton = AccessTools.Field(typeof(SettingUI), "_graphicInteractableUI").GetValue(settingUI) as InteractableUI;
            var audioButton = AccessTools.Field(typeof(SettingUI), "_audioInteractableUI").GetValue(settingUI) as InteractableUI;
            var creditsButton = AccessTools.Field(typeof(SettingUI), "_creditsInteractableUI").GetValue(settingUI) as InteractableUI;
            
            generalButton?.DeactivateUseUI(false);
            graphicButton?.DeactivateUseUI(false);
            audioButton?.DeactivateUseUI(false);
            creditsButton?.DeactivateUseUI(false);
            
            modInteractableUI?.ActivateUseUI(false);
            modContentParent?.SetActive(!useGraphicsContent);
            
            // ѡ�ѷ����ĸ���Ӧ�ø��´�����
            ScrollRect scrollRect = null;
            if (useGraphicsContent && graphicParent != null)
            {
                scrollRect = graphicParent.GetComponentInChildren<ScrollRect>();
            }
            else
            {
                scrollRect = modContentParent?.GetComponentInChildren<ScrollRect>();
            }
            if (scrollRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.GetComponent<RectTransform>());
                scrollRect.verticalNormalizedPosition = 1f;
            }
        }
        
        static void UpdateModButtonText(GameObject modTabButton)
        {
            try
            {
                var allTexts = modTabButton.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var text in allTexts)
                {
                    text.text = "MOD";
                }
            }
            catch (System.Exception e)
            {
                PotatoPlugin.Log.LogError($"更新按钮文本失败: {e}");
            }
        }
        
        static void UpdateModContentText(GameObject modContentParent)
        {
            try
            {
                var titleText = modContentParent.GetComponent<TextMeshProUGUI>();
                if (titleText != null) titleText.text = "MOD设置";
                
                var scrollRect = modContentParent.GetComponentInChildren<ScrollRect>();
                Transform contentTransform = null;
                if (scrollRect?.content != null)
                {
                    contentTransform = scrollRect.content.transform;
                }
                
                var allTexts = modContentParent.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var text in allTexts)
                {
                    if (contentTransform != null && text.transform.IsChildOf(contentTransform))
                    {
                        continue;
                    }
                    
                    if (text.text.Contains("制作人员") || text.text.Contains("Credits"))
                    {
                        text.text = "MOD设置";
                    }
                }
            }
            catch (System.Exception e)
            {
                PotatoPlugin.Log.LogError($"更新内容文本失败: {e}");
            }
        }
        
        static void AdjustTabBarLayout(Transform tabBarParent)
        {
            try
            {
                var rectTransform = tabBarParent.GetComponent<RectTransform>();
                var horizontalLayout = tabBarParent.GetComponent<HorizontalLayoutGroup>();
                
                if (horizontalLayout == null || rectTransform == null) return;

                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
                
                if (tabBarParent.childCount == 0) return;
                
                var firstChild = tabBarParent.GetChild(0).GetComponent<RectTransform>();
                if (firstChild == null) return;
                
                float tabWidth = firstChild.rect.width;
                float spacing = horizontalLayout.spacing;
                int tabCount = tabBarParent.childCount;
                
                float original4TabsWidth = (tabWidth * 4) + (spacing * 3);
                float current5TabsWidth = (tabWidth * 5) + (spacing * 4);
                float widthIncrease = current5TabsWidth - original4TabsWidth;
                float positionOffset = widthIncrease / 2f;
                
                Vector2 currentPos = rectTransform.anchoredPosition;
                rectTransform.anchoredPosition = new Vector2(currentPos.x - positionOffset, currentPos.y);
                
                horizontalLayout.padding.left = 0;
                horizontalLayout.padding.right = 0;
                horizontalLayout.childAlignment = TextAnchor.UpperLeft;
                
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            }
            catch (System.Exception e)
            {
                PotatoPlugin.Log.LogError($"调整标签栏布局失败: {e}");
            }
        }
        
        static List<string> GetFunctionKeyOptions() => new List<string> 
            { "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12" };
        
        static int GetKeyCodeIndex(KeyCode key)
        {
            int index = key - KeyCode.F1;
            return index >= 0 && index < 12 ? index : 1;
        }
        
        static KeyCode GetKeyCodeFromIndex(int index)
        {
            return index >= 0 && index < 12 ? KeyCode.F1 + index : KeyCode.F2;
        }

        /// <summary>
        /// 创建游戏原生风格的下拉框(使用ModPulldownCloner)
        /// </summary>
        static void CreateNativeDropdown(Transform parent, string label, List<string> options, int currentIndex, System.Action<int> onValueChanged)
        {
            try
            {
                // 检查是否有缓存的SettingUI实例
                if (cachedSettingUI == null)
                {
                    PotatoPlugin.Log.LogError($"cachedSettingUI 为 null,无法创建原生下拉框: {label}");
                    return;
                }

                // 1. 克隆游戏原生下拉框(传入SettingUI的Transform)
                GameObject pulldownClone = ModPulldownCloner.CloneAndClearPulldown(cachedSettingUI.transform);
                if (pulldownClone == null)
                {
                    PotatoPlugin.Log.LogError($"克隆下拉框失败: {label}");
                    return;
                }

                // ✅ 2. 先设置父对象（在 SetActive(false) 状态下）
                pulldownClone.transform.SetParent(parent, false); // ← worldPositionStays = false 很重要！
                
                // ✅ 3. 再次重置局部位置（防止父对象的布局影响）
                var rectTransform = pulldownClone.GetComponent<RectTransform>();
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.anchoredPosition3D = Vector3.zero;
                rectTransform.localPosition = Vector3.zero;

                // 4. 修改标题
                Transform titleTransform = pulldownClone.transform.Find("TitleText");
                if (titleTransform != null)
                {
                    TMP_Text titleText = titleTransform.GetComponent<TMP_Text>();
                    if (titleText != null)
                    {
                        titleText.text = label;
                        PotatoPlugin.Log.LogInfo($"✅ 设置下拉框标题: '{label}'");
                    }
                }
                else
                {
                    PotatoPlugin.Log.LogWarning($"⚠️ 未找到 TitleText 组件");
                }

                // 5. 获取按钮模板
                GameObject buttonTemplate = ModPulldownCloner.GetSelectButtonTemplate(cachedSettingUI.transform);
                if (buttonTemplate == null)
                {
                    PotatoPlugin.Log.LogError($"获取按钮模板失败: {label}");
                    Object.Destroy(pulldownClone);
                    return;
                }

                // 6. 添加选项
                for (int i = 0; i < options.Count; i++)
                {
                    int index = i;
                    ModPulldownCloner.AddOption(pulldownClone, buttonTemplate, options[i], () =>
                    {
                        PotatoPlugin.Log.LogInfo($"选择了 {label}: {options[index]}");
                        onValueChanged?.Invoke(index);
                        
                        // 更新下拉框顶部的显示文本
                        SetPulldownSelectedText(pulldownClone, options[index]);
                    });
                }

                // 7. 设置默认选中项
                if (currentIndex >= 0 && currentIndex < options.Count)
                {
                    SetPulldownSelectedText(pulldownClone, options[currentIndex]);
                }

                // ✅ 8. 添加 LayoutElement 组件（适配 VerticalLayoutGroup）
                var layoutElement = pulldownClone.GetComponent<LayoutElement>();
                if (layoutElement == null)
                {
                    layoutElement = pulldownClone.AddComponent<LayoutElement>();
                }
                layoutElement.preferredHeight = 72f; // 告诉 VerticalLayoutGroup 这个元素的首选高度
                PotatoPlugin.Log.LogInfo($"✅ 添加 LayoutElement (preferredHeight = 72f)");

                // ✅ 9. 最后激活（确保所有设置都完成后）
                pulldownClone.SetActive(true);

                // ✅ 10. 强制更新画布并重建布局
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(parent.GetComponent<RectTransform>());

                // 11. 清理模板
                Object.Destroy(buttonTemplate);

                PotatoPlugin.Log.LogInfo($"成功创建原生风格下拉框: {label}");
            }
            catch (System.Exception e)
            {
                PotatoPlugin.Log.LogError($"创建原生下拉框失败 [{label}]: {e}");
            }
        }

        /// <summary>
        /// 设置下拉框的当前选中项显示文本
        /// 根据UnityExplorer截图: CurrentSelectText (TMP) 组件本身就是 TMP_Text
        /// </summary>
        static void SetPulldownSelectedText(GameObject pulldownClone, string text)
        {
            try
            {
                // ✅ 关键发现: CurrentSelectText (TMP) 本身就是一个 TMP_Text 组件!
                Transform currentSelectTransform = pulldownClone.transform.Find("PulldownList/Pulldown/CurrentSelectText (TMP)");
                if (currentSelectTransform != null)
                {
                    TMP_Text currentText = currentSelectTransform.GetComponent<TMP_Text>();
                    if (currentText != null)
                    {
                        currentText.text = text;
                        PotatoPlugin.Log.LogInfo($"✅ 设置选中项文本为: '{text}'");
                        return;
                    }
                }

                // 备用路径(以防万一)
                string[] possiblePaths = new[]
                {
                    "SelectContent/Text (TMP)",
                    "SelectContent/Text",
                    "CurrentSelectText (TMP)",
                    "CurrentSelectText",
                    "Title/Text (TMP)",
                    "Title/Text",
                    "Text (TMP)"
                };

                TMP_Text selectText = null;
                foreach (var path in possiblePaths)
                {
                    Transform target = pulldownClone.transform.Find(path);
                    if (target != null)
                    {
                        selectText = target.GetComponent<TMP_Text>();
                        if (selectText != null)
                        {
                            selectText.text = text;
                            PotatoPlugin.Log.LogInfo($"✅ 设置选中项文本为: '{text}' (路径: {path})");
                            return;
                        }
                    }
                }

                // 如果上面的路径都找不到,递归搜索所有TMP_Text组件
                PotatoPlugin.Log.LogWarning($"未找到预期路径,尝试递归查找...");
                var allTexts = pulldownClone.GetComponentsInChildren<TMP_Text>(true);
                PotatoPlugin.Log.LogInfo($"找到 {allTexts.Length} 个TMP_Text组件:");
                foreach (var tmpText in allTexts)
                {
                    string fullPath = GetFullPath(tmpText.transform, pulldownClone.transform);
                    PotatoPlugin.Log.LogInfo($"  - {fullPath}");
                    
                    // 尝试通过名称判断是否是显示当前选中项的文本
                    if (tmpText.name.Contains("CurrentSelectText") || 
                        tmpText.name.Contains("SelectContent") ||
                        tmpText.transform.parent?.name.Contains("CurrentSelectText") == true)
                    {
                        tmpText.text = text;
                        PotatoPlugin.Log.LogInfo($"✅ 通过递归设置文本为: '{text}' (路径: {fullPath})");
                        return;
                    }
                }

                // 最后尝试使用反射调用PulldownListUI的方法
                try
                {
                    var pulldownUI = pulldownClone.GetComponent(System.Type.GetType("Bulbul.PulldownListUI, Assembly-CSharp"));
                    if (pulldownUI != null)
                    {
                        var method = pulldownUI.GetType().GetMethod("ChangeSelectContentText");
                        if (method != null)
                        {
                            method.Invoke(pulldownUI, new object[] { text });
                            PotatoPlugin.Log.LogInfo($"✅ 通过反射设置选中项文本为: '{text}'");
                            return;
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    PotatoPlugin.Log.LogWarning($"反射调用失败: {ex.Message}");
                }

                PotatoPlugin.Log.LogError($"❌ 所有方法都失败,无法设置选中项文本: '{text}'");
            }
            catch (System.Exception e)
            {
                PotatoPlugin.Log.LogError($"设置选中项文本时发生异常: {e}");
            }
        }

        /// <summary>
        /// 获取Transform的完整路径(用于调试)
        /// </summary>
        static string GetFullPath(Transform transform, Transform root)
        {
            if (transform == root) return transform.name;
            
            string path = transform.name;
            Transform current = transform.parent;
            
            while (current != null && current != root)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            
            return path;
        }
    }

    /// <summary>
    /// 协程运行器 - 用于延迟操作和异步UI更新
    /// </summary>
    public class ModUICoroutineRunner : MonoBehaviour
    {
        private static ModUICoroutineRunner _instance;
        
        public static ModUICoroutineRunner Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("ModUI_CoroutineRunner");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<ModUICoroutineRunner>();
                }
                return _instance;
            }
        }

        /// <summary>
        /// 延迟执行
        /// </summary>
        public void RunDelayed(float seconds, System.Action action)
        {
            StartCoroutine(DelayedAction(seconds, action));
        }

        private System.Collections.IEnumerator DelayedAction(float seconds, System.Action action)
        {
            yield return new WaitForSeconds(seconds);
            action?.Invoke();
        }
    }
}




