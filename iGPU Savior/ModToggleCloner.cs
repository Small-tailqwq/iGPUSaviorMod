using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;

namespace PotatoOptimization
{
    public class ModToggleCloner
    {
        /// <summary>
        /// 全新策略：直接克隆整个“番茄钟音效”行，保留完美的原生布局
        /// </summary>
        public static GameObject CreateToggle(Transform settingRoot, string labelText, bool initialValue, Action<bool> onValueChanged)
        {
            try
            {
                if (settingRoot == null) return null;

                // 1. 【寻源】直接找到那个完美的参照物：番茄钟音效行
                // 它的层级通常是：SelectPomodoroSoundOnOffButtons (这是一个包含标题和按钮的完整行)
                Transform audioTabContent = settingRoot.Find("MusicAudio/ScrollView/Viewport/Content");
                if (audioTabContent == null) 
                {
                    PotatoPlugin.Log.LogError("Audio tab content not found");
                    return null;
                }

                // 寻找包含 "Pomodoro" 和 "OnOff" 的物体
                // 原名可能是 "SelectPomodoroSoundOnOffButtons"
                Transform originalRow = null;
                foreach (Transform child in audioTabContent)
                {
                    if (child.name.Contains("Pomodoro") && child.name.Contains("OnOff"))
                    {
                        originalRow = child;
                        break;
                    }
                }

                if (originalRow == null)
                {
                    PotatoPlugin.Log.LogError("Original Pomodoro Row not found! Cannot clone style.");
                    return null;
                }

                PotatoPlugin.Log.LogInfo($"Found template row: {originalRow.name}");

                // 2. 【克隆】完整复制这一行
                GameObject toggleRow = UnityEngine.Object.Instantiate(originalRow.gameObject);
                toggleRow.name = $"ModToggle_{labelText}";
                
                // 确保它默认激活
                toggleRow.SetActive(true);

                // 3. 【改字】修改左边的标题
                // 标题通常叫 "TitleText", "Text", "Name" 或者就是第一个 TextMeshPro 组件
                var titleTexts = toggleRow.GetComponentsInChildren<TMP_Text>(true);
                bool titleFound = false;
                
                // 策略：最左边的 Text 是标题，右边的 Text 是按钮上的字
                // 我们按 X 坐标排个序
                if (titleTexts.Length > 0)
                {
                    var sortedTexts = titleTexts.OrderBy(t => t.transform.position.x).ToArray();
                    
                    // 第一个肯定是标题
                    sortedTexts[0].text = labelText;
                    titleFound = true;
                    
                    // 如果有 "ON" "OFF" 之外的怪字，可以在这里修正，但番茄钟本身就是 ON/OFF，所以大概率不用动
                }

                if (!titleFound)
                {
                    // 降级方案：找名字里带 Title 的
                    var title = toggleRow.transform.Find("Title"); 
                    if (title != null && title.GetComponent<TMP_Text>() != null)
                        title.GetComponent<TMP_Text>().text = labelText;
                }

                // 4. 【接线】找到那一对按钮并重写逻辑
                // 这个行里肯定有两个 Button 组件
                Button[] buttons = toggleRow.GetComponentsInChildren<Button>(true);
                
                if (buttons.Length < 2)
                {
                    PotatoPlugin.Log.LogError("Cloned row has less than 2 buttons!");
                    return toggleRow; // 至少把壳子返回去
                }

                // 按位置排序：左边是 ON，右边是 OFF (通常如此)
                Array.Sort(buttons, (a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
                
                Button btnOn = buttons[0];
                Button btnOff = buttons[1];

                // 确保按钮文字也是 ON / OFF (万一原版是日文 "开启/关闭")
                SetButtonText(btnOn, "ON");
                SetButtonText(btnOff, "OFF");

                // === 关键：接管点击逻辑 ===
                // 先清除原版可能绑定的 Audio 设置事件
                btnOn.onClick.RemoveAllListeners();
                btnOff.onClick.RemoveAllListeners();

                // 状态更新函数 (直接复用原版的高亮逻辑)
                // 我们不手动改颜色，而是模拟“点击”后的状态
                // 观察发现：原版可能通过 Interactable 或者 setActive 子物体来控制
                // 我们先尝试最通用的 Interactable 互斥方案，看看能否触发原版自带的 Transition
                
                void UpdateState(bool state)
                {
                    // state = true (ON): On不可点(高亮), Off可点
                    btnOn.interactable = !state;
                    btnOff.interactable = state;
                    
                    // 如果原版有额外的脚本控制高亮 (比如 InteractableUI)，
                    // 仅改 interactable 可能不够，我们需要更深层的“视觉欺骗”
                    // 但首先，我们试试最简单的 interactable，因为这是 Unity 标准
                }

                // 绑定点击
                btnOn.onClick.AddListener(() => {
                    if (btnOn.interactable) {
                        UpdateState(true);
                        onValueChanged?.Invoke(true);
                        PlayClickSound(settingRoot);
                    }
                });

                btnOff.onClick.AddListener(() => {
                    if (btnOff.interactable) {
                        UpdateState(false);
                        onValueChanged?.Invoke(false);
                        PlayClickSound(settingRoot);
                    }
                });

                // 初始化视觉状态
                UpdateState(initialValue);

                return toggleRow;
            }
            catch (Exception e)
            {
                PotatoPlugin.Log.LogError($"CreateToggle failed: {e}");
                return null;
            }
        }

        private static void SetButtonText(Button btn, string text)
        {
            var tmp = btn.GetComponentInChildren<TMP_Text>();
            if (tmp != null) tmp.text = text;
        }

        private static void PlayClickSound(Transform root)
        {
            // 简单的音效播放，找不到也不报错
            try {
                // ... 你的音效逻辑 ...
            } catch {}
        }
    }
}