using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PotatoOptimization.Core;
using PotatoOptimization.UI;
using Bulbul;
using NestopiSystem.DIContainers;

namespace PotatoOptimization.Patches
{
  /// <summary>
  /// 待办删除二次确认补丁
  /// 克隆 ExitConfirmationUI 的 UI 结构，但完全接管按钮回调逻辑
  /// 参考 ModToggleCloner 的设计模式
  /// </summary>
  [HarmonyPatch]
  public static class TodoDeleteConfirmPatch
  {
    // 更可靠的类型查找方式：直接从程序集加载
    private static readonly Type TodoUIType;
    private static readonly Type ExitConfirmUIType;
    private static readonly Type TodoDataType;

    private static readonly FieldInfo FI_OnDeleteAction;
    private static readonly FieldInfo FI_TodoData;
    private static readonly MethodInfo MI_Activate;
    private static readonly MethodInfo MI_Deactivate;

    // 标志：类型加载是否成功
    private static bool _typesLoaded = false;

    // 静态构造函数：初始化所有反射成员
    static TodoDeleteConfirmPatch()
    {
      try
      {
        PotatoPlugin.Log.LogWarning("[TodoConfirm] 🔧 Static constructor starting...");
        
        // 从 Assembly-CSharp 程序集加载类型
        var assembly = Assembly.Load("Assembly-CSharp");
        PotatoPlugin.Log.LogWarning("[TodoConfirm] Assembly-CSharp loaded successfully");
        
        // TodoUI 在全局命名空间，不是 Bulbul.TodoUI！
        TodoUIType = assembly.GetType("TodoUI");
        ExitConfirmUIType = assembly.GetType("Bulbul.ExitConfirmationUI");
        TodoDataType = assembly.GetType("Bulbul.TodoData");

        if (TodoUIType == null)
        {
          PotatoPlugin.Log.LogError("[TodoConfirm] ❌ Failed to load TodoUI from global namespace");
        }
        else
        {
          PotatoPlugin.Log.LogWarning("[TodoConfirm] ✅ Successfully loaded TodoUI from global namespace");
        }
        
        if (ExitConfirmUIType == null)
        {
          PotatoPlugin.Log.LogError("[TodoConfirm] ❌ Failed to load Bulbul.ExitConfirmationUI");
        }
        else
        {
          PotatoPlugin.Log.LogWarning("[TodoConfirm] ✅ Successfully loaded Bulbul.ExitConfirmationUI");
        }

        // 初始化反射成员
        if (TodoUIType != null)
        {
          FI_OnDeleteAction = TodoUIType.GetField("_onDeleteTodoAction", BindingFlags.Instance | BindingFlags.NonPublic);
          FI_TodoData = TodoUIType.GetField("_todoData", BindingFlags.Instance | BindingFlags.NonPublic);
          
          if (FI_OnDeleteAction == null)
            PotatoPlugin.Log.LogWarning("[TodoConfirm] ⚠️ _onDeleteTodoAction field not found");
          else
            PotatoPlugin.Log.LogWarning("[TodoConfirm] ✅ _onDeleteTodoAction field found");
            
          if (FI_TodoData == null)
            PotatoPlugin.Log.LogWarning("[TodoConfirm] ⚠️ _todoData field not found");
          else
            PotatoPlugin.Log.LogWarning("[TodoConfirm] ✅ _todoData field found");
        }

        if (ExitConfirmUIType != null)
        {
          MI_Activate = ExitConfirmUIType.GetMethod("Activate", BindingFlags.Instance | BindingFlags.Public);
          MI_Deactivate = ExitConfirmUIType.GetMethod("Deactivate", BindingFlags.Instance | BindingFlags.Public);
          
          if (MI_Activate == null)
            PotatoPlugin.Log.LogWarning("[TodoConfirm] ⚠️ Activate method not found");
          else
            PotatoPlugin.Log.LogWarning("[TodoConfirm] ✅ Activate method found");
            
          if (MI_Deactivate == null)
            PotatoPlugin.Log.LogWarning("[TodoConfirm] ⚠️ Deactivate method not found");
          else
            PotatoPlugin.Log.LogWarning("[TodoConfirm] ✅ Deactivate method found");
        }

        _typesLoaded = (TodoUIType != null && ExitConfirmUIType != null && 
                        FI_OnDeleteAction != null && FI_TodoData != null);
        
        PotatoPlugin.Log.LogWarning($"[TodoConfirm] 🔧 Static constructor completed. Types loaded: {_typesLoaded}");
      }
      catch (Exception e)
      {
        PotatoPlugin.Log.LogError($"[TodoConfirm] ❌ Static constructor failed: {e}");
      }
    }

    // 动态定位目标方法 TodoUI.OnClickButtonRemoveTodo (Public 方法)
    static MethodBase TargetMethod()
    {
      PotatoPlugin.Log.LogWarning("[TodoConfirm] 🎯 TargetMethod() called");
      
      var t = TodoUIType;
      if (t == null)
      {
        PotatoPlugin.Log.LogError("[TodoConfirm] ❌ TodoUI type is null in TargetMethod()");
        return null;
      }
      
      // OnClickButtonRemoveTodo 是 Public 方法
      var method = t.GetMethod("OnClickButtonRemoveTodo", BindingFlags.Instance | BindingFlags.Public);
      if (method == null)
      {
        PotatoPlugin.Log.LogError($"[TodoConfirm] ❌ Method 'OnClickButtonRemoveTodo' not found in {t.FullName}");
        
        // 诊断：列出所有 Public 方法
        var allMethods = t.GetMethods(BindingFlags.Instance | BindingFlags.Public);
        PotatoPlugin.Log.LogWarning($"[TodoConfirm] 📋 Available public methods in TodoUI ({allMethods.Length}):");
        foreach (var m in allMethods.Take(20))
        {
          PotatoPlugin.Log.LogWarning($"  - {m.Name}");
        }
        
        return null;
      }
      
      PotatoPlugin.Log.LogWarning($"[TodoConfirm] ✅ Successfully found target method: {t.FullName}.OnClickButtonRemoveTodo");
      return method;
    }

    // Prefix: 拦截删除操作，显示二次确认弹窗
    static bool Prefix(object __instance)
    {
      PotatoPlugin.Log.LogWarning("[TodoConfirm] 🚀 Prefix invoked! Intercepting delete...");
      
      try
      {
        if (!_typesLoaded || TodoUIType == null || ExitConfirmUIType == null || __instance == null)
        {
          PotatoPlugin.Log.LogWarning($"[TodoConfirm] ⚠️ Types not loaded (_typesLoaded={_typesLoaded}), falling back to original delete.");
          return true; // 放行原逻辑
        }

        PotatoPlugin.Log.LogWarning("[TodoConfirm] ✅ Types loaded, proceeding with dialog creation");

        var confirmComp = CreateExitConfirmInstance();
        if (confirmComp == null)
        {
          PotatoPlugin.Log.LogWarning("[TodoConfirm] Failed to create confirmation dialog; falling back.");
          return true;
        }

        var confirmGO = (confirmComp as Component)?.gameObject;
        if (confirmGO == null)
        {
          PotatoPlugin.Log.LogWarning("[TodoConfirm] Confirmation dialog has no GameObject; falling back.");
          return true;
        }

        // 🔥 关键：为对话框添加独立的 Canvas，确保置顶显示
        var dialogCanvas = confirmGO.GetComponent<Canvas>();
        if (dialogCanvas == null)
        {
          dialogCanvas = confirmGO.AddComponent<Canvas>();
        }
        
        // 设置为 ScreenSpaceOverlay 模式，确保在最顶层
        dialogCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        dialogCanvas.overrideSorting = true;
        dialogCanvas.sortingOrder = 10000; // 确保在所有 UI 之上
        
        // 添加 GraphicRaycaster 以支持按钮点击
        var raycaster = confirmGO.GetComponent<UnityEngine.UI.GraphicRaycaster>();
        if (raycaster == null)
        {
          raycaster = confirmGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }
        
        PotatoPlugin.Log.LogWarning($"[TodoConfirm] Canvas setup: renderMode={dialogCanvas.renderMode}, sortingOrder={dialogCanvas.sortingOrder}");

        // 🔥 调用 Setup() 方法以正确初始化对话框
        try
        {
          var setupMethod = ExitConfirmUIType.GetMethod("Setup", BindingFlags.Instance | BindingFlags.Public);
          if (setupMethod != null)
          {
            setupMethod.Invoke(confirmComp, null);
            PotatoPlugin.Log.LogWarning("[TodoConfirm] Setup() called successfully");
          }
        }
        catch (Exception e)
        {
          PotatoPlugin.Log.LogWarning($"[TodoConfirm] Setup() error: {e.Message}");
        }

        // 🔥 Setup() 之后再设置文本，防止被 Setup() 重置
        SetupConfirmText(confirmGO, confirmComp);

        // 🔥 设置对话框位置（参考原版 ExitConfirmationUI 的位置）
        try
        {
          var rectTransform = confirmGO.GetComponent<RectTransform>();
          if (rectTransform != null)
          {
            // 设置锚点为屏幕中心
            rectTransform.anchorMin = new UnityEngine.Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new UnityEngine.Vector2(0.5f, 0.5f);
            rectTransform.pivot = new UnityEngine.Vector2(0.5f, 0.5f);
            
            // 设置位置为屏幕中心
            rectTransform.anchoredPosition = UnityEngine.Vector2.zero;
            
            // 确保在最前层显示
            confirmGO.transform.SetAsLastSibling();
            
            PotatoPlugin.Log.LogWarning($"[TodoConfirm] Position set: anchoredPosition={rectTransform.anchoredPosition}, localPosition={rectTransform.localPosition}");
          }
        }
        catch (Exception e)
        {
          PotatoPlugin.Log.LogWarning($"[TodoConfirm] Position setup error: {e.Message}");
        }

        // 重要：激活弹窗前先确保对象可见
        // （ExitConfirmationUI 会在 Setup 中调用 SetActive(false)，我们需要恢复）
        try { confirmGO.SetActive(true); } catch { }
        
        // 调用 Activate 触发动画
        MI_Activate?.Invoke(confirmComp, null);
        
        // 再次确保对象可见
        try { confirmGO.SetActive(true); } catch { }
        
        PotatoPlugin.Log.LogInfo($"[TodoConfirm] Dialog activated, gameObject active: {confirmGO.activeSelf}");

        // 直接从 ExitConfirmationUI 获取按钮，订阅点击事件
        Button okButton = null;
        Button cancelButton = null;
        
        try
        {
          var okField = ExitConfirmUIType.GetField("_okButton", BindingFlags.Instance | BindingFlags.NonPublic);
          var cancelField = ExitConfirmUIType.GetField("_cancelButton", BindingFlags.Instance | BindingFlags.NonPublic);
          
          okButton = okField?.GetValue(confirmComp) as Button;
          cancelButton = cancelField?.GetValue(confirmComp) as Button;
          
          PotatoPlugin.Log.LogInfo($"[TodoConfirm] OK button found: {okButton != null}, Cancel button found: {cancelButton != null}");
        }
        catch (Exception e)
        {
          PotatoPlugin.Log.LogWarning($"[TodoConfirm] Failed to get buttons: {e.Message}");
        }

        if (okButton == null || cancelButton == null)
        {
          PotatoPlugin.Log.LogWarning("[TodoConfirm] OK or Cancel button not found; deactivating and falling back.");
          SafeDeactivate(confirmComp, confirmGO);
          return true;
        }

        // 关键：清除按钮上所有旧的回调（防止触发游戏退出等原逻辑）
        okButton.onClick.RemoveAllListeners();
        cancelButton.onClick.RemoveAllListeners();
        
        // 🔥 确保按钮可交互
        okButton.interactable = true;
        cancelButton.interactable = true;
        
        PotatoPlugin.Log.LogInfo($"[TodoConfirm] Cleared all button listeners, OK interactable: {okButton.interactable}, Cancel interactable: {cancelButton.interactable}");
        PotatoPlugin.Log.LogInfo($"[TodoConfirm] OK button gameObject active: {okButton.gameObject.activeSelf}, Cancel button gameObject active: {cancelButton.gameObject.activeSelf}");

        // 订阅确认按钮
        okButton.onClick.AddListener(() =>
        {
          PotatoPlugin.Log.LogWarning("[TodoConfirm] ⭐ OK button clicked! User confirmed deletion.");
          try
          {
            PotatoPlugin.Log.LogInfo("[TodoConfirm] User confirmed delete, invoking _onDeleteTodoAction");
            
            // 通过反射获取私有字段 _onDeleteTodoAction 和 _todoData
            var deleteActionObj = FI_OnDeleteAction?.GetValue(__instance) as Delegate;
            var todoData = FI_TodoData?.GetValue(__instance);
            
            if (deleteActionObj != null && todoData != null)
            {
              try 
              { 
                deleteActionObj.DynamicInvoke(todoData);
                PotatoPlugin.Log.LogInfo("[TodoConfirm] Delete action invoked successfully");
              }
              catch (Exception e) 
              { 
                PotatoPlugin.Log.LogError($"[TodoConfirm] Invoke delete action failed: {e}"); 
              }
            }
            else
            {
              PotatoPlugin.Log.LogWarning("[TodoConfirm] Missing _onDeleteTodoAction or _todoData field");
            }
          }
          finally
          {
            SafeDeactivate(confirmComp, confirmGO);
          }
        });

        // 订阅取消按钮
        cancelButton.onClick.AddListener(() =>
        {
          PotatoPlugin.Log.LogWarning("[TodoConfirm] ⭐ Cancel button clicked! User cancelled deletion.");
          SafeDeactivate(confirmComp, confirmGO);
        });

        // 拦截原方法，阻止直接删除
        return false;
      }
      catch (Exception e)
      {
        PotatoPlugin.Log.LogError($"[TodoConfirm] Prefix exception: {e}");
        return true; // 出错则放行原逻辑
      }
    }

    private static Component CreateExitConfirmInstance()
    {
      try
      {
        // 必须正确克隆 ExitConfirmationUI 对象
        // 因为原对象的按钮回调被硬编码为触发游戏退出
        // 我们需要一个独立的克隆对象，然后完全覆盖按钮回调
        
        var all = Resources.FindObjectsOfTypeAll(ExitConfirmUIType);
        if (all != null && all.Length > 0)
        {
          // 第一个实例作为模板
          var template = all[0] as Component;
          if (template == null)
          {
            PotatoPlugin.Log.LogError("[TodoConfirm] Template cast to Component failed");
            return null;
          }

          // 克隆模板对象
          var clonedGO = UnityEngine.Object.Instantiate(template.gameObject);
          if (clonedGO == null)
          {
            PotatoPlugin.Log.LogError("[TodoConfirm] Instantiate failed");
            return null;
          }

          // 为克隆对象起个唯一的名字（基于时间戳），避免重名冲突
          clonedGO.name = $"TodoDeleteConfirm_{System.DateTime.Now.Ticks}";
          
          // 从克隆对象获取 ExitConfirmationUI 组件
          var clonedComponent = clonedGO.GetComponent(ExitConfirmUIType) as Component;
          if (clonedComponent != null)
          {
            PotatoPlugin.Log.LogInfo($"[TodoConfirm] Successfully cloned ExitConfirmationUI: {clonedGO.name}");
            return clonedComponent;
          }
          else
          {
            PotatoPlugin.Log.LogWarning($"[TodoConfirm] Cloned object has no ExitConfirmationUI component, destroying it");
            UnityEngine.Object.Destroy(clonedGO);
            return null;
          }
        }
        
        PotatoPlugin.Log.LogError("[TodoConfirm] No ExitConfirmationUI template found in Resources");
        return null;
      }
      catch (Exception e)
      {
        PotatoPlugin.Log.LogError($"[TodoConfirm] Create instance failed: {e}");
        return null;
      }
    }

    private static Component FindExitConfirmTemplate()
    {
      try
      {
        // 优先：通过 Resources.FindObjectsOfTypeAll 在内存中查找已加载的预制体模板
        var all = Resources.FindObjectsOfTypeAll(ExitConfirmUIType);
        if (all != null && all.Length > 0)
        {
          // 选第一个作为克隆模板
          PotatoPlugin.Log.LogInfo($"[TodoConfirm] Found {all.Length} ExitConfirmationUI instance(s) in memory");
          var template = all[0] as Component;
          if (template != null)
          {
            PotatoPlugin.Log.LogInfo($"[TodoConfirm] Template found: {template.gameObject.name}");
          }
          else
          {
            PotatoPlugin.Log.LogWarning("[TodoConfirm] Template cast to Component failed");
          }
          return template;
        }
        else
        {
          PotatoPlugin.Log.LogWarning("[TodoConfirm] No ExitConfirmationUI found in Resources");
        }
      }
      catch (Exception e)
      {
        PotatoPlugin.Log.LogError($"[TodoConfirm] Find template failed: {e.Message}");
      }

      return null;
    }

    private static void SetupConfirmText(GameObject confirmGO, object confirmComp)
    {
      try
      {
        // 获取游戏当前语言 - 使用 LanguageSupplier (DI)
        GameLanguageType currentLang = GameLanguageType.ChineseSimplified; // 默认简体中文
        try
        {
          var languageSupplier = ProjectLifetimeScope.Resolve<LanguageSupplier>();
          if (languageSupplier != null && languageSupplier.Language != null)
          {
            currentLang = languageSupplier.Language.CurrentValue;
            PotatoPlugin.Log.LogWarning($"[TodoConfirm] ✅ Got language from LanguageSupplier: {currentLang}");
          }
          else
          {
            PotatoPlugin.Log.LogWarning("[TodoConfirm] ⚠️ LanguageSupplier or Language property is null");
          }
        }
        catch (Exception e)
        {
          PotatoPlugin.Log.LogWarning($"[TodoConfirm] ⚠️ Failed to resolve LanguageSupplier: {e.Message}, using default");
        }

        // 关键：TextLocalizationBehaviour 会不断重置文本，必须彻底禁用或移除它
        DisableAllTextLocalizers(confirmGO);

        // 获取本地化文本，并输出诊断信息
        var promptText_Localized = ModTranslationManager.Get("TODO_DELETE_CONFIRM_PROMPT", currentLang);
        var okText_Localized = ModTranslationManager.Get("TODO_DELETE_CONFIRM_OK", currentLang);
        var cancelText_Localized = ModTranslationManager.Get("TODO_DELETE_CONFIRM_CANCEL", currentLang);

        PotatoPlugin.Log.LogWarning($"[TodoConfirm] Language: {currentLang}");
        PotatoPlugin.Log.LogWarning($"[TodoConfirm] Prompt: '{promptText_Localized}'");
        PotatoPlugin.Log.LogWarning($"[TodoConfirm] OK: '{okText_Localized}'");
        PotatoPlugin.Log.LogWarning($"[TodoConfirm] Cancel: '{cancelText_Localized}'");

        // 诊断：列出所有 TMP_Text 及其内容
        var allTexts = confirmGO.GetComponentsInChildren<TMP_Text>(true);
        PotatoPlugin.Log.LogWarning($"[TodoConfirm] Found {allTexts.Length} TMP_Text components");

        // 修改所有文本
        if (allTexts != null && allTexts.Length > 0)
        {
          foreach (var t in allTexts)
          {
            if (string.IsNullOrEmpty(t.text))
              continue;

            var originalText = t.text;
            var path = GetComponentPath(t.transform);

            // 策略：根据路径和内容识别文本
            // Title（包含提示文本）
            if (path.Contains("Title") || originalText.Contains("title") || originalText == "ui_exit_title")
            {
              t.text = promptText_Localized;
              PotatoPlugin.Log.LogWarning($"[TodoConfirm] [TITLE] '{originalText}' -> '{promptText_Localized}'");
              
              var localizer = t.GetComponent("TextLocalizationBehaviour");
              if (localizer != null)
                DisableComponent(localizer);
            }
            // OK 按钮
            else if (path.Contains("OK") || originalText.Contains("ok") || originalText == "ui_exit_ok")
            {
              t.text = okText_Localized;
              PotatoPlugin.Log.LogWarning($"[TodoConfirm] [OK_BTN] '{originalText}' -> '{okText_Localized}'");
              
              var localizer = t.GetComponent("TextLocalizationBehaviour");
              if (localizer != null)
                DisableComponent(localizer);
            }
            // Cancel 按钮
            else if (path.Contains("Cancel") || originalText.Contains("cancel") || originalText == "ui_exit_cancel")
            {
              t.text = cancelText_Localized;
              PotatoPlugin.Log.LogWarning($"[TodoConfirm] [CANCEL_BTN] '{originalText}' -> '{cancelText_Localized}'");
              
              var localizer = t.GetComponent("TextLocalizationBehaviour");
              if (localizer != null)
                DisableComponent(localizer);
            }
          }
        }
        
        PotatoPlugin.Log.LogWarning("[TodoConfirm] Text setup completed");
      }
      catch (Exception e)
      {
        PotatoPlugin.Log.LogError($"[TodoConfirm] Setup text failed: {e.Message}");
      }
    }
    
    private static void DisableComponent(object component)
    {
      try
      {
        var comp = component as Component;
        if (comp == null) return;
        
        var enabledProp = comp.GetType().GetProperty("enabled", BindingFlags.Instance | BindingFlags.Public);
        if (enabledProp != null && enabledProp.CanWrite)
        {
          enabledProp.SetValue(comp, false);
        }
      }
      catch (Exception e)
      {
        PotatoPlugin.Log.LogWarning($"[TodoConfirm] Failed to disable component: {e.Message}");
      }
    }
    
    private static void DisableAllTextLocalizers(GameObject obj)
    {
      try
      {
        // 禁用对象及其所有子对象上的 TextLocalizationBehaviour 组件
        var localizerType = System.Type.GetType("Bulbul.TextLocalizationBehaviour, Assembly-CSharp");
        if (localizerType == null)
        {
          PotatoPlugin.Log.LogWarning("[TodoConfirm] TextLocalizationBehaviour type not found");
          return;
        }
        
        var allLocalizers = obj.GetComponentsInChildren(localizerType, true);
        foreach (var localizer in allLocalizers)
        {
          DisableComponent(localizer);
        }
        
        PotatoPlugin.Log.LogWarning($"[TodoConfirm] Disabled {allLocalizers.Length} TextLocalizationBehaviour components");
      }
      catch (Exception e)
      {
        PotatoPlugin.Log.LogWarning($"[TodoConfirm] DisableAllTextLocalizers error: {e.Message}");
      }
    }
    
    private static string GetComponentPath(Transform trans)
    {
      if (trans == null) return "null";
      var path = trans.name;
      var parent = trans.parent;
      int depth = 0;
      while (parent != null && depth < 4)
      {
        path = parent.name + "/" + path;
        parent = parent.parent;
        depth++;
      }
      return path;
    }

    private static void RemoveLocalizers(GameObject obj)
    {
      try
      {
        // Search on the object itself
        var localizer = obj.GetComponent("TextLocalizationBehaviour");
        if (localizer != null)
        {
          UnityEngine.Object.Destroy(localizer);
          PotatoPlugin.Log.LogInfo($"[TodoConfirm] Removed localization from {obj.name}");
        }

        // Also check children
        var allChildren = obj.GetComponentsInChildren(System.Type.GetType("TextLocalizationBehaviour, Assembly-CSharp"), true);
        foreach (var child in allChildren)
        {
          if (child != null)
            UnityEngine.Object.Destroy(child);
        }
      }
      catch (Exception e)
      {
        PotatoPlugin.Log.LogWarning($"[TodoConfirm] RemoveLocalizers error: {e.Message}");
      }
    }

    private static void SafeDeactivate(object confirmComp, GameObject confirmGO)
    {
      // 克隆对象，用完后销毁，防止内存泄漏和 UI 冻结
      try 
      { 
        // 先停用
        MI_Deactivate?.Invoke(confirmComp, null);
        if (confirmGO != null) 
        { 
          confirmGO.SetActive(false);
          PotatoPlugin.Log.LogInfo($"[TodoConfirm] Dialog deactivated: {confirmGO.name}");
          
          // 延后销毁，给动画时间完成
          UnityEngine.Object.Destroy(confirmGO, 0.5f);
          PotatoPlugin.Log.LogInfo($"[TodoConfirm] Cloned dialog scheduled for destruction: {confirmGO.name}");
        }
      }
      catch (Exception e)
      {
        PotatoPlugin.Log.LogError($"[TodoConfirm] SafeDeactivate error: {e.Message}");
      }
    }
  }
}
