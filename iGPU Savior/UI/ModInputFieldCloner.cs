using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;
using PotatoOptimization.Core;

namespace PotatoOptimization.UI
{
    public class ModInputFieldCloner
    {
        public static GameObject CreateInputField(
            Transform modContent,
            string labelText,
            string initialValue,
            Action<string> onValueChanged)
        {
            try
            {
                if (modContent == null) return null;

                // 1. 寻找模板
                Transform templateObj = modContent.Find("FrameRate");
                if (templateObj == null)
                {
                    PotatoPlugin.Log.LogWarning("[Input] Template 'FrameRate' not found!");
                    return null;
                }

                // 2. 克隆
                GameObject clone = UnityEngine.Object.Instantiate(templateObj.gameObject);
                clone.name = labelText.Replace(" ", "").Replace("(", "").Replace(")", "");
                clone.SetActive(false);

                // 3. 清理垃圾子物体
                Transform deactiveInput = clone.transform.Find("DeactiveFrameRate");
                if (deactiveInput != null) UnityEngine.Object.DestroyImmediate(deactiveInput.gameObject);
                Transform parentTitle = clone.transform.Find("TitleText");
                if (parentTitle != null) UnityEngine.Object.DestroyImmediate(parentTitle.gameObject);

                // 4. 清理脚本
                var allComponents = clone.GetComponentsInChildren<MonoBehaviour>(true);
                foreach (var comp in allComponents)
                {
                    if (comp == null) continue;
                    Type type = comp.GetType();
                    string ns = type.Namespace ?? "";
                    // 保留基础UI组件
                    bool isSafe = ns.StartsWith("UnityEngine.UI") || ns.Contains("TMPro") ||
                                  type == typeof(LayoutElement) || type == typeof(CanvasGroup) || type == typeof(CanvasRenderer);
                    if (!isSafe) UnityEngine.Object.DestroyImmediate(comp);
                }

                // 5. 核心布局修正
                Transform activeFrame = clone.transform.Find("ActiveFrameRate");
                if (activeFrame != null)
                {
                    activeFrame.name = "InputField";
                    GameObject activeFrameObj = activeFrame.gameObject;

                    // 🔥🔥🔥 第一步：处决所有布局组件 (内鬼) 🔥🔥🔥
                    // 必须先杀掉它们，才能手动控制坐标！
                    var hlg = activeFrameObj.GetComponent<HorizontalLayoutGroup>();
                    if (hlg != null) UnityEngine.Object.DestroyImmediate(hlg);
                    
                    var vlg = activeFrameObj.GetComponent<VerticalLayoutGroup>();
                    if (vlg != null) UnityEngine.Object.DestroyImmediate(vlg);
                    
                    var csf = activeFrameObj.GetComponent<ContentSizeFitter>();
                    if (csf != null) UnityEngine.Object.DestroyImmediate(csf);

                    // =========================================================
                    // 手动坐标控制 (Manual Coordinate Control)
                    // =========================================================

                    // A. 父容器归位 (0,0) - 绝对居中
                    RectTransform frameRect = activeFrame.GetComponent<RectTransform>();
                    if (frameRect != null)
                    {
                        frameRect.anchorMin = new Vector2(0.5f, 0.5f);
                        frameRect.anchorMax = new Vector2(0.5f, 0.5f);
                        frameRect.pivot = new Vector2(0.5f, 0.5f);
                        
                        frameRect.anchoredPosition = Vector2.zero; // 居中
                        frameRect.sizeDelta = new Vector2(1260f, 50f); // 高度限制为50
                    }

                    // B. 文本对齐 (-306)
                    var titleText = activeFrame.Find("TitleText")?.GetComponent<TMP_Text>();
                    if (titleText == null) titleText = activeFrame.GetComponentInChildren<TMP_Text>();

                    if (titleText != null)
                    {
                        titleText.text = labelText;
                        titleText.alignment = TextAlignmentOptions.MidlineLeft;

                        RectTransform titleRect = titleText.GetComponent<RectTransform>();
                        if (titleRect != null)
                        {
                            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
                            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
                            
                            // 🔥 文本使用左轴心，确保起始点精准 🔥
                            titleRect.pivot = new Vector2(0f, 0.5f); 
                            
                            // ✅ 应用 -306 (从中心向左偏移)
                            // 🔥🔥🔥 向上修正 40，让文字往上飘 🔥🔥🔥
                            titleRect.anchoredPosition = new Vector2(-306f, 40f);
                            titleRect.sizeDelta = new Vector2(400f, 50f);
                        }
                    }

                    // C. 输入框对齐 (295.75)
                    Transform inputFieldObj = activeFrame.Find("WorkTimeInputField (TMP)");
                    if (inputFieldObj == null) 
                    {
                        var inputComp = activeFrame.GetComponentInChildren<TMP_InputField>();
                        if (inputComp != null) inputFieldObj = inputComp.transform;
                    }

                    if (inputFieldObj != null)
                    {
                        inputFieldObj.name = "TMP_InputField";
                        RectTransform inputRect = inputFieldObj.GetComponent<RectTransform>();
                        if (inputRect != null)
                        {
                            inputRect.anchorMin = new Vector2(0.5f, 0.5f);
                            inputRect.anchorMax = new Vector2(0.5f, 0.5f);
                            inputRect.pivot = new Vector2(0f, 0.5f); // 左轴心

                            // 🔥🔥🔥 这里的 Y 必须也是 40！🔥🔥🔥
                            // 之前可能是 new Vector2(40f, 0f); 
                            // 现在改为:
                            inputRect.anchoredPosition = new Vector2(40f, 40f); 
                            
                            // 宽度保持你调好的 390
                            inputRect.sizeDelta = new Vector2(405f, 40f); 
                        }
                    }

                    // D. 逻辑绑定
                    var inputField = activeFrame.GetComponentInChildren<TMP_InputField>();
                    if (inputField != null)
                    {
                        inputField.contentType = TMP_InputField.ContentType.Standard;
                        inputField.lineType = TMP_InputField.LineType.SingleLine;
                        inputField.characterValidation = TMP_InputField.CharacterValidation.None;
                        inputField.characterLimit = 0;
                        inputField.text = initialValue;

                        inputField.onValueChanged.RemoveAllListeners();
                        inputField.onEndEdit.RemoveAllListeners();
                        inputField.onSubmit.RemoveAllListeners();
                        inputField.onSelect.RemoveAllListeners();
                        inputField.onDeselect.RemoveAllListeners();

                        inputField.onEndEdit.AddListener((val) => onValueChanged?.Invoke(val));
                    }
                }

                // E. 行高控制
                var le = clone.GetComponent<LayoutElement>();
                if (le == null) le = clone.AddComponent<LayoutElement>();
                // 强制高度 50，消除上下多余空隙
                le.minHeight = 50f;
                le.preferredHeight = 50f;
                le.flexibleHeight = 0;

                return clone;
            }
            catch (Exception e)
            {
                PotatoPlugin.Log.LogError($"[Input] CreateInputField failed: {e}");
                return null;
            }
        }
    }
}