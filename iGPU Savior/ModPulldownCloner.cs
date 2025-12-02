using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Reflection;

namespace PotatoOptimization
{
    /// <summary>
    /// ��¡��Ϸ��ͼ������������,������Ϊ Mod ���Զ���������
    /// </summary>
    public class ModPulldownCloner
    {
        /// <summary>
        /// ��¡ͼ������������,�����ԭ��ѡ��
        /// </summary>
        /// <param name="settingUITransform">SettingUI�����Transform</param>
        /// <returns>��¡��� GameObject(�ѽ���)</returns>
        public static GameObject CloneAndClearPulldown(Transform settingUITransform)
        {
            try
            {
                if (settingUITransform == null)
                {
                    PotatoPlugin.Log.LogError("settingUITransform Ϊ null");
                    return null;
                }

                // 1. ��SettingUI.transform��ʼ����ͼ������������
                // ·��: Graphics/ScrollView/Viewport/Content/GraphicQualityPulldownList
                Transform originalPath = settingUITransform.Find(
                    "Graphics/ScrollView/Viewport/Content/GraphicQualityPulldownList"
                );

                if (originalPath == null)
                {
                    PotatoPlugin.Log.LogError("δ�ҵ� GraphicQualityPulldownList");
                    return null;
                }

                // 3. ��¡��������������(��������� RectTransform ���ò��޸�)
                GameObject clone = UnityEngine.Object.Instantiate(originalPath.gameObject);
                clone.name = "ModPulldownList";

                // 4. ��ʱ���ÿ�¡��(��ֹ��ԭ���ͻ)
                clone.SetActive(false);

                PotatoPlugin.Log.LogInfo($"�ɹ���¡ {originalPath.name} -> {clone.name}");

                // 5. ��� Content �µ�ԭ��ѡ�ť
                // ? ����UnityExplorer��ͼ: PulldownList/Pulldown/CurrentSelectText (TMP)/Content
                Transform content = clone.transform.Find("PulldownList/Pulldown/CurrentSelectText (TMP)/Content");
                if (content == null)
                {
                    PotatoPlugin.Log.LogError("δ�ҵ���¡��� Content ���� (·��: PulldownList/Pulldown/CurrentSelectText (TMP)/Content)");
                    UnityEngine.Object.Destroy(clone);
                    return null;
                }

                // ��������ԭ�е� SelectButton
                int childCount = content.childCount;
                for (int i = childCount - 1; i >= 0; i--)
                {
                    Transform child = content.GetChild(i);
                    PotatoPlugin.Log.LogInfo($"����ԭ��ѡ��: {child.name} (���� {i})");
                    UnityEngine.Object.Destroy(child.gameObject);
                }

                PotatoPlugin.Log.LogInfo($"����� {childCount} ��ԭ��ѡ��");

                // ? 6. �ҵ� PulldownButton (ʵ�ʻ򿪹رչ˵�ĵط�)
                Transform pulldownButtonTransform = clone.transform.Find("PulldownList/PulldownButton");
                if (pulldownButtonTransform != null)
                {
                    Button pulldownButton = pulldownButtonTransform.GetComponent<Button>();
                    if (pulldownButton != null)
                    {
                        // ����Ϸԭ�������߼���Ҳ�Լ�������������Բ��õ��޸ĵ���¼�
                        PotatoPlugin.Log.LogInfo($"? �ҵ� PulldownButton ��������ͨ�� Bulbul.PulldownListUI �����򿪹ر�");
                    }
                    else
                    {
                        PotatoPlugin.Log.LogError($"? PulldownButton û�� Button �����");
                    }
                }
                else
                {
                    PotatoPlugin.Log.LogError($"? δ�ҵ� PulldownButton (·��: PulldownList/PulldownButton)");
                }

                // �ֶ�ȷ�� PulldownListUI �ں�����(ԭģ��û������ʱ���ֶ�����)
                EnsurePulldownListUI(clone, originalPath, content);

                // ? �����Ϸԭ�򿪹رչ˵�ʵ��,�����ֶ��رն�Դ content �����ʾ͸���ڸ� PulldownListUI ���������

                return clone;
            }
            catch (Exception e)
            {
                PotatoPlugin.Log.LogError($"��¡������ʧ��: {e}");
                return null;
            }
        }

        /// <summary>
        /// ��¡һ�� SelectButton ��Ϊģ��(���ں��������Լ���ѡ��)
        /// </summary>
        /// <param name="settingUITransform">SettingUI�����Transform</param>
        /// <returns>SelectButton ��ģ�����(�ѽ���)</returns>
        public static GameObject GetSelectButtonTemplate(Transform settingUITransform)
        {
            try
            {
                if (settingUITransform == null)
                {
                    PotatoPlugin.Log.LogError("settingUITransform Ϊ null");
                    return null;
                }

                // �ҵ�ԭ��ĵ�һ�� SelectButton
                // ? ����UnityExplorer��ͼ����ʵ·��
                Transform firstButton = settingUITransform.Find(
                    "Graphics/ScrollView/Viewport/Content/GraphicQualityPulldownList/PulldownList/Pulldown/CurrentSelectText (TMP)/Content"
                );
                
                // ��ȡ�����µĵ�һ���Ӷ�����Ϊģ��
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
                    PotatoPlugin.Log.LogError("δ�ҵ�ԭ��� SelectButton ģ��");
                    return null;
                }

                // ��¡��
                GameObject template = UnityEngine.Object.Instantiate(firstButton.gameObject);
                template.name = "SelectButtonTemplate";
                template.SetActive(false); // ����ģ��

                PotatoPlugin.Log.LogInfo("�ɹ���¡ SelectButton ģ��");
                return template;
            }
            catch (Exception e)
            {
                PotatoPlugin.Log.LogError($"��ȡ SelectButton ģ��ʧ��: {e}");
                return null;
            }
        }

        /// <summary>
        /// �ڿ�¡��������������һ����ѡ��
        /// </summary>
        /// <param name="pulldownClone">��¡�������� GameObject</param>
        /// <param name="buttonTemplate">SelectButton ģ��</param>
        /// <param name="optionText">ѡ�����ʾ�ı�</param>
        /// <param name="onClick">���ʱ�Ļص�</param>
        public static void AddOption(GameObject pulldownClone, GameObject buttonTemplate, string optionText, Action onClick)
        {
            try
            {
                // 1. �ҵ� Content ����
                // ? ����UnityExplorer��ͼ: PulldownList/Pulldown/CurrentSelectText (TMP)/Content
                Transform content = pulldownClone.transform.Find("PulldownList/Pulldown/CurrentSelectText (TMP)/Content");
                if (content == null)
                {
                    PotatoPlugin.Log.LogError("δ�ҵ� Content ���� (·��: PulldownList/Pulldown/CurrentSelectText (TMP)/Content)");
                    return;
                }

                // 2. ��¡ SelectButton ģ��
                GameObject newButton = UnityEngine.Object.Instantiate(buttonTemplate, content);
                newButton.name = $"SelectButton_{optionText}";
                newButton.SetActive(true); // �����°�ť

                // 3. �޸İ�ť���ı�
                TMP_Text buttonText = newButton.GetComponentInChildren<TMP_Text>();
                if (buttonText != null)
                {
                    buttonText.text = optionText;
                    PotatoPlugin.Log.LogInfo($"���ð�ť�ı�: {optionText}");
                }

                // 4. �󶨵���¼�
                Button button = newButton.GetComponent<Button>();
                if (button != null)
                {
                    // �����ԭ�еĵ���¼�(��Ҫ!���������Ϸԭ�߼�)
                    button.onClick.RemoveAllListeners();

                    // ���������Լ��ĵ���¼�
                    button.onClick.AddListener(() =>
                    {
                        PotatoPlugin.Log.LogInfo($"�����ѡ��: {optionText}");
                        onClick?.Invoke();

                        // ���������򶥲�����ʾ�ı�(���Ե���,ʧ��Ҳ��Ӱ�칦��)
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
                            // ���Դ���,��Ӱ����Ҫ����
                        }
                    });

                    // ? ��֤��ť�Ƿ�ɽ���
                    if (!button.interactable)
                    {
                        button.interactable = true;
                        PotatoPlugin.Log.LogWarning($"?? ��ť '{optionText}' ԭ�����ɽ�������ǿ������");
                    }
                    else
                    {
                        PotatoPlugin.Log.LogInfo($"? �ɹ�Ϊ��ť '{optionText}' ���ӵ���¼�");
                    }

                    PotatoPlugin.Log.LogInfo($"�ɹ�����ѡ��: {optionText}");
                }
                else
                {
                    PotatoPlugin.Log.LogError($"? ��ť '{optionText}' û�� Button �����");
                }
            }
            catch (Exception e)
            {
                PotatoPlugin.Log.LogError($"����ѡ��ʧ��: {e}");
            }
        }

        /// <summary>
        /// ����¡����������ص����ý����ָ��λ��
        /// </summary>
        /// <param name="pulldownClone">��¡��������</param>
        /// <param name="parentPath">Ҫ���ص��ĸ�����·��(���� "Graphics/ScrollView/Viewport/Content")</param>
        public static void MountPulldown(GameObject pulldownClone, string parentPath)
        {
            try
            {
                GameObject settingRoot = GameObject.Find("UI_FacilitySetting");
                if (settingRoot == null)
                {
                    PotatoPlugin.Log.LogError("δ�ҵ� UI_FacilitySetting");
                    return;
                }

                Transform parent = settingRoot.transform.Find(parentPath);
                if (parent == null)
                {
                    PotatoPlugin.Log.LogError($"δ�ҵ�������: {parentPath}");
                    return;
                }

                // ���ø�����
                pulldownClone.transform.SetParent(parent, false);

                // ���ÿ�¡��
                pulldownClone.SetActive(true);

                PotatoPlugin.Log.LogInfo($"�ɹ�����������ص�: {parentPath}");
            }
            catch (Exception e)
            {
                PotatoPlugin.Log.LogError($"����������ʧ��: {e}");
            }
        }

        /// <summary>
        /// ������Ϸ�ĵ����Ч����ѡ��
        /// </summary>
        private static void PlayClickSound()
        {
            try
            {
                // ����Ϸ�� SettingUI ���ҵ� _systemSeService
                var settingUI = UnityEngine.Object.FindObjectOfType(System.Type.GetType("Bulbul.SettingUI, Assembly-CSharp"));
                if (settingUI != null)
                {
                    // ʹ�÷������˽���ֶ� _systemSeService.PlayClick()
                    var systemSeServiceField = settingUI.GetType().GetField("_systemSeService", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    if (systemSeServiceField != null)
                    {
                        var systemSeService = systemSeServiceField.GetValue(settingUI);
                        if (systemSeService != null)
                        {
                            var playClickMethod = systemSeService.GetType().GetMethod("PlayClick");
                            playClickMethod?.Invoke(systemSeService, null);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // ������Чʧ�ܲ�Ӱ�칦�ܣ�ֻ��¼��־
                PotatoPlugin.Log.LogWarning($"���ŵ����Чʧ��: {e.Message}");
            }
        }

        /// <summary>
        /// ȷ�� PulldownListUI ������ ModPulldownList ��, ������������ֶ�����
        /// </summary>
        private static void EnsurePulldownListUI(GameObject clone, Transform originalPath, Transform content)
        {
            try
            {
                Type pulldownUIType = Type.GetType("PulldownListUI, Assembly-CSharp");
                if (pulldownUIType == null)
                {
                    PotatoPlugin.Log.LogError("δ�ҵ� PulldownListUI ���� (Assembly-CSharp)");
                    return;
                }

                Transform pulldownList = clone.transform.Find("PulldownList");
                Transform pulldown = clone.transform.Find("PulldownList/Pulldown");
                Transform pulldownButton = clone.transform.Find("PulldownList/PulldownButton");
                Transform currentSelectText = clone.transform.Find("PulldownList/Pulldown/CurrentSelectText (TMP)");

                Component pulldownUI = (pulldownList != null) ? pulldownList.GetComponent(pulldownUIType) : clone.GetComponent(pulldownUIType);
                if (pulldownUI == null)
                {
                    GameObject attachTarget = (pulldownList != null) ? pulldownList.gameObject : clone;
                    pulldownUI = attachTarget.AddComponent(pulldownUIType);
                    PotatoPlugin.Log.LogInfo("�ֶ����� PulldownListUI �� ModPulldownList");
                }

                // ���ԭģ���� PulldownListUI �ϵĳ�Աֵ��Ϊ�µ� component ���趨
                Component originPulldownUI = null;
                Transform originPulldownList = originalPath.Find("PulldownList");
                if (originPulldownList != null)
                {
                    originPulldownUI = originPulldownList.GetComponent(pulldownUIType);
                }
                if (originPulldownUI == null)
                {
                    originPulldownUI = originalPath.GetComponentInChildren(pulldownUIType, true);
                }

                Button pulldownButtonComp = pulldownButton != null ? pulldownButton.GetComponent<Button>() : null;
                TMP_Text currentSelectTextComp = currentSelectText != null ? currentSelectText.GetComponent<TMP_Text>() : null;
                RectTransform pulldownParentRect = pulldown != null ? pulldown.GetComponent<RectTransform>() : null;
                RectTransform pulldownButtonRect = pulldownButton != null ? pulldownButton.GetComponent<RectTransform>() : null;
                RectTransform contentRect = content != null ? content.GetComponent<RectTransform>() : null;

                void SetField(string fieldName, object value)
                {
                    if (value == null) return;
                    FieldInfo field = pulldownUIType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                    field?.SetValue(pulldownUI, value);
                }

                // �ӱ�ģ����ȡ�� open size �� ease ��ʱ�䣬�õ�������ֵ
                float openSize = 0f;
                float openCloseSeconds = 0.3f;
                object easeValue = null;
                if (originPulldownUI != null)
                {
                    FieldInfo openSizeField = pulldownUIType.GetField("_openPullDownSizeDeltaY", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (openSizeField != null)
                    {
                        object v = openSizeField.GetValue(originPulldownUI);
                        if (v is float f && f > 0f) openSize = f;
                    }
                    FieldInfo secondsField = pulldownUIType.GetField("_pullDownOpenCloseSeconds", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (secondsField != null)
                    {
                        object v = secondsField.GetValue(originPulldownUI);
                        if (v is float f) openCloseSeconds = f;
                    }
                    FieldInfo easeField = pulldownUIType.GetField("_pullDownOpenCloseEase", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (easeField != null)
                    {
                        easeValue = easeField.GetValue(originPulldownUI);
                    }
                }

                // �����Ч open size
                if (openSize <= 0f && pulldownParentRect != null)
                {
                    float closeHeight = pulldownParentRect.rect.height;
                    float contentHeight = contentRect != null ? contentRect.rect.height : 0f;
                    openSize = Math.Max(closeHeight + contentHeight + 20f, closeHeight + 120f);
                }

                SetField("_currentSelectContentText", currentSelectTextComp);
                SetField("_pullDownParentRect", pulldownParentRect);
                SetField("_openPullDownSizeDeltaY", openSize);
                SetField("_pullDownOpenCloseSeconds", openCloseSeconds);
                if (easeValue != null) SetField("_pullDownOpenCloseEase", easeValue);
                SetField("_pullDownOpenButton", pulldownButtonComp);
                SetField("_pullDownButtonRect", pulldownButtonRect);

                // �ֶ�ִ�� Setup()�����¼��󶨣�����Ӳ�����Ա�ֵ
                MethodInfo setupMethod = pulldownUIType.GetMethod("Setup", BindingFlags.Public | BindingFlags.Instance);
                setupMethod?.Invoke(pulldownUI, null);

                PotatoPlugin.Log.LogInfo("PulldownListUI �����÷�ɹ�������Ŀ���Ƿ��㹻");
            }
            catch (Exception e)
            {
                PotatoPlugin.Log.LogError($"���� PulldownListUI ʧ��: {e}");
            }
        }
    }
}
