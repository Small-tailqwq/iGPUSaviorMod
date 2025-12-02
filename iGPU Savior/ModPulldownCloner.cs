using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace PotatoOptimization
{
    /// <summary>
    /// 克隆游戏的图形质量下拉框,并改造为 Mod 的自定义下拉框
    /// </summary>
    public class ModPulldownCloner
    {
        /// <summary>
        /// 克隆图形质量下拉框,并清空原有选项
        /// </summary>
        /// <param name="settingUITransform">SettingUI组件的Transform</param>
        /// <returns>克隆后的 GameObject(已禁用)</returns>
        public static GameObject CloneAndClearPulldown(Transform settingUITransform)
        {
            try
            {
                if (settingUITransform == null)
                {
                    PotatoPlugin.Log.LogError("settingUITransform 为 null");
                    return null;
                }

                // 1. 从SettingUI.transform开始查找图形质量下拉框
                // 路径: Graphics/ScrollView/Viewport/Content/GraphicQualityPulldownList
                Transform originalPath = settingUITransform.Find(
                    "Graphics/ScrollView/Viewport/Content/GraphicQualityPulldownList"
                );

                if (originalPath == null)
                {
                    PotatoPlugin.Log.LogError("未找到 GraphicQualityPulldownList");
                    return null;
                }

                // 3. 克隆整个下拉框物体
                GameObject clone = UnityEngine.Object.Instantiate(originalPath.gameObject);
                clone.name = "ModPulldownList";

                // 4. 暂时禁用克隆体(防止和原版冲突)
                clone.SetActive(false);

                PotatoPlugin.Log.LogInfo($"成功克隆 {originalPath.name} -> {clone.name}");

                // 5. 清空 Content 下的原有选项按钮
                // ✅ 根据UnityExplorer截图: PulldownList/Pulldown/CurrentSelectText (TMP)/Content
                Transform content = clone.transform.Find("PulldownList/Pulldown/CurrentSelectText (TMP)/Content");
                if (content == null)
                {
                    PotatoPlugin.Log.LogError("未找到克隆体的 Content 容器 (路径: PulldownList/Pulldown/CurrentSelectText (TMP)/Content)");
                    UnityEngine.Object.Destroy(clone);
                    return null;
                }

                // 销毁所有原有的 SelectButton
                int childCount = content.childCount;
                for (int i = childCount - 1; i >= 0; i--)
                {
                    Transform child = content.GetChild(i);
                    PotatoPlugin.Log.LogInfo($"销毁原有选项: {child.name} (索引 {i})");
                    UnityEngine.Object.Destroy(child.gameObject);
                }

                PotatoPlugin.Log.LogInfo($"已清空 {childCount} 个原有选项");
                return clone;
            }
            catch (Exception e)
            {
                PotatoPlugin.Log.LogError($"克隆下拉框失败: {e}");
                return null;
            }
        }

        /// <summary>
        /// 克隆一个 SelectButton 作为模板(用于后续添加自己的选项)
        /// </summary>
        /// <param name="settingUITransform">SettingUI组件的Transform</param>
        /// <returns>SelectButton 的模板对象(已禁用)</returns>
        public static GameObject GetSelectButtonTemplate(Transform settingUITransform)
        {
            try
            {
                if (settingUITransform == null)
                {
                    PotatoPlugin.Log.LogError("settingUITransform 为 null");
                    return null;
                }

                // 找到原版的第一个 SelectButton
                // ✅ 根据UnityExplorer截图的真实路径
                Transform firstButton = settingUITransform.Find(
                    "Graphics/ScrollView/Viewport/Content/GraphicQualityPulldownList/PulldownList/Pulldown/CurrentSelectText (TMP)/Content"
                );
                
                // 获取容器下的第一个子对象作为模板
                if (firstButton != null && firstButton.childCount > 0)
                {
                    firstButton = firstButton.GetChild(0);
                }
                else
                {
                    firstButton = null;
                }

                if (firstButton == null)
                {
                    PotatoPlugin.Log.LogError("未找到原版的 SelectButton 模板");
                    return null;
                }

                // 克隆它
                GameObject template = UnityEngine.Object.Instantiate(firstButton.gameObject);
                template.name = "SelectButtonTemplate";
                template.SetActive(false); // 禁用模板

                PotatoPlugin.Log.LogInfo("成功克隆 SelectButton 模板");
                return template;
            }
            catch (Exception e)
            {
                PotatoPlugin.Log.LogError($"获取 SelectButton 模板失败: {e}");
                return null;
            }
        }

        /// <summary>
        /// 在克隆的下拉框中添加一个新选项
        /// </summary>
        /// <param name="pulldownClone">克隆的下拉框 GameObject</param>
        /// <param name="buttonTemplate">SelectButton 模板</param>
        /// <param name="optionText">选项的显示文本</param>
        /// <param name="onClick">点击时的回调</param>
        public static void AddOption(GameObject pulldownClone, GameObject buttonTemplate, string optionText, Action onClick)
        {
            try
            {
                // 1. 找到 Content 容器
                // ✅ 根据UnityExplorer截图: PulldownList/Pulldown/CurrentSelectText (TMP)/Content
                Transform content = pulldownClone.transform.Find("PulldownList/Pulldown/CurrentSelectText (TMP)/Content");
                if (content == null)
                {
                    PotatoPlugin.Log.LogError("未找到 Content 容器 (路径: PulldownList/Pulldown/CurrentSelectText (TMP)/Content)");
                    return;
                }

                // 2. 克隆 SelectButton 模板
                GameObject newButton = UnityEngine.Object.Instantiate(buttonTemplate, content);
                newButton.name = $"SelectButton_{optionText}";
                newButton.SetActive(true); // 启用新按钮

                // 3. 修改按钮的文本
                TMP_Text buttonText = newButton.GetComponentInChildren<TMP_Text>();
                if (buttonText != null)
                {
                    buttonText.text = optionText;
                    PotatoPlugin.Log.LogInfo($"设置按钮文本: {optionText}");
                }

                // 4. 绑定点击事件
                Button button = newButton.GetComponent<Button>();
                if (button != null)
                {
                    // 先清空原有的点击事件(重要!避免调用游戏原逻辑)
                    button.onClick.RemoveAllListeners();

                    // 添加我们自己的点击事件
                    button.onClick.AddListener(() =>
                    {
                        PotatoPlugin.Log.LogInfo($"点击了选项: {optionText}");
                        onClick?.Invoke();

                        // 更新下拉框顶部的显示文本(尝试调用,失败也不影响功能)
                        try
                        {
                            var pulldownUI = pulldownClone.GetComponent(System.Type.GetType("Bulbul.PulldownListUI, Assembly-CSharp"));
                            if (pulldownUI != null)
                            {
                                var method = pulldownUI.GetType().GetMethod("ChangeSelectContentText");
                                method?.Invoke(pulldownUI, new object[] { optionText });
                            }
                        }
                        catch
                        {
                            // 忽略错误,不影响主要功能
                        }
                    });

                    PotatoPlugin.Log.LogInfo($"成功添加选项: {optionText}");
                }
                else
                {
                    PotatoPlugin.Log.LogError($"SelectButton 上没有 Button 组件: {newButton.name}");
                }
            }
            catch (Exception e)
            {
                PotatoPlugin.Log.LogError($"添加选项失败: {e}");
            }
        }

        /// <summary>
        /// 将克隆的下拉框挂载到设置界面的指定位置
        /// </summary>
        /// <param name="pulldownClone">克隆的下拉框</param>
        /// <param name="parentPath">要挂载到的父物体路径(例如 "Graphics/ScrollView/Viewport/Content")</param>
        public static void MountPulldown(GameObject pulldownClone, string parentPath)
        {
            try
            {
                GameObject settingRoot = GameObject.Find("UI_FacilitySetting");
                if (settingRoot == null)
                {
                    PotatoPlugin.Log.LogError("未找到 UI_FacilitySetting");
                    return;
                }

                Transform parent = settingRoot.transform.Find(parentPath);
                if (parent == null)
                {
                    PotatoPlugin.Log.LogError($"未找到父物体: {parentPath}");
                    return;
                }

                // 设置父物体
                pulldownClone.transform.SetParent(parent, false);

                // 启用克隆体
                pulldownClone.SetActive(true);

                PotatoPlugin.Log.LogInfo($"成功将下拉框挂载到: {parentPath}");
            }
            catch (Exception e)
            {
                PotatoPlugin.Log.LogError($"挂载下拉框失败: {e}");
            }
        }
    }
}
