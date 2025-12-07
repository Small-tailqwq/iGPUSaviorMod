using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.Linq;
using PotatoOptimization.Core;

namespace PotatoOptimization.UI
{
    public class ModInputFieldCloner
    {
        public static GameObject CreateInputField(Transform settingRoot, string labelText, string initialValue, Action<string> onValueChanged)
        {
            try
            {
                if (settingRoot == null) return null;

                // 1. 寻找模板
                Transform graphicsContent = settingRoot.Find("Graphics/ScrollView/Viewport/Content");
                if (graphicsContent == null) return null;

                // 优先找 DeactiveFrameRate，找不到再模糊搜
                Transform templateObj = graphicsContent.Find("DeactiveFrameRate");
                if (templateObj == null)
                {
                    foreach(Transform child in graphicsContent) {
                        if(child.name.Contains("FrameRate")) { templateObj = child; break; }
                    }
                }

                if (templateObj == null)
                {
                    PotatoPlugin.Log.LogError("[Input] Template 'DeactiveFrameRate' not found!");
                    return null;
                }

                PotatoPlugin.Log.LogInfo($"[Input] Found template: {templateObj.name} (Cloning...)");

                // 2. 克隆 (⚠️ 关键：先设为 false，防止脚本在下一帧立刻运行)
                GameObject clone = UnityEngine.Object.Instantiate(templateObj.gameObject);
                clone.name = $"ModInput_{labelText}";
                clone.SetActive(false);

                // === 3. 🔪 核弹级清理：递归扫描所有子物体 ===
                // 必须转为 List，因为我们在遍历过程中会 Destroy 组件
                var allComponents = clone.GetComponentsInChildren<MonoBehaviour>(true).ToList();
                
                int removedCount = 0;
                foreach (var comp in allComponents)
                {
                    if (comp == null) continue;

                    Type type = comp.GetType();
                    string ns = type.Namespace ?? "";
                    
                    // === 白名单：只保留 UI 相关的纯展示组件 ===
                    bool isSafe = 
                        ns.StartsWith("UnityEngine.UI") ||  // 原生 UI (Image, Button...)
                        ns.Contains("TMPro") ||             // TMP 文本
                        type == typeof(LayoutElement) ||    // 布局元素
                        type == typeof(CanvasGroup) ||
                        type == typeof(CanvasRenderer);     // 渲染器

                    if (!isSafe)
                    {
                        // 发现可疑脚本！(比如 FrameRateController, SettingItem...)
                        // 立即销毁，防止它作妖
                        PotatoPlugin.Log.LogWarning($"[Input] 🔪 Killing logic script: {type.Name} on {comp.gameObject.name}");
                        UnityEngine.Object.DestroyImmediate(comp);
                        removedCount++;
                    }
                }
                
                PotatoPlugin.Log.LogInfo($"[Input] Cleanup complete. Removed {removedCount} logic scripts.");

                // 4. 修改标题
                var titleText = clone.transform.Find("TitleText")?.GetComponent<TMP_Text>();
                if (titleText == null) titleText = clone.GetComponentInChildren<TMP_Text>();
                if (titleText != null) titleText.text = labelText;

                // 5. 改造输入框
                var inputField = clone.GetComponentInChildren<TMP_InputField>();
                if (inputField != null)
                {
                    // 解除封印：允许任意输入
                    inputField.contentType = TMP_InputField.ContentType.Standard;
                    inputField.lineType = TMP_InputField.LineType.SingleLine;
                    inputField.characterValidation = TMP_InputField.CharacterValidation.None;
                    inputField.characterLimit = 0;
                    inputField.text = initialValue;

                    // 暴力移除所有监听器 (包括原版可能残留的)
                    inputField.onValueChanged.RemoveAllListeners();
                    inputField.onEndEdit.RemoveAllListeners();
                    inputField.onSubmit.RemoveAllListeners();
                    inputField.onSelect.RemoveAllListeners();
                    inputField.onDeselect.RemoveAllListeners();

                    // 绑定我们的逻辑
                    inputField.onEndEdit.AddListener((val) => 
                    {
                        PotatoPlugin.Log.LogInfo($"[Input] '{labelText}' saved: {val}");
                        onValueChanged?.Invoke(val);
                    });
                }
                else
                {
                    PotatoPlugin.Log.LogError("[Input] TMP_InputField not found in clone!");
                }

                // 此时 clone 还是 inactive 的，ModSettingsManager 会负责把它放到正确位置并激活
                return clone;
            }
            catch (Exception e)
            {
                PotatoPlugin.Log.LogError($"CreateInputField failed: {e}");
                return null;
            }
        }
    }
}