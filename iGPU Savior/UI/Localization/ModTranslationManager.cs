using System.Collections.Generic;
using Bulbul;

namespace PotatoOptimization.UI
{
  public static class ModTranslationManager
  {
    private static Dictionary<string, Dictionary<GameLanguageType, string>> _translations = new Dictionary<string, Dictionary<GameLanguageType, string>>();

    static ModTranslationManager()
    {
      InitializeTranslations();
    }

    public static void Add(string key, string en, string ja, string zh)
    {
      var dict = new Dictionary<GameLanguageType, string>
              {
                  { GameLanguageType.English, en },
                  { GameLanguageType.Japanese, ja },
                  { GameLanguageType.ChineseSimplified, zh },
                  { GameLanguageType.ChineseTraditional, zh }
              };
      _translations[key] = dict;
    }

    private static void InitializeTranslations()
    {
      // General
      Add("MOD_SETTINGS_TITLE", "MOD Settings", "MOD設定", "MOD 设置");

      // Toggles
      Add("SETTING_MIRROR_AUTO", "Mirror Auto-Start", "ミラー自動起動", "镜像自启动");
      Add("SETTING_PORTRAIT_AUTO", "Portrait Auto-Start", "縦画面自動起動", "竖屏优化自启动");

      // Dropdowns
      Add("SETTING_MINI_SCALE", "Mini Window Scale", "小窓スケール", "小窗缩放");
      Add("SETTING_DRAG_MODE", "Drag Mode", "ドラッグモード", "小窗拖动模式");

      // Keys
      Add("SETTING_KEY_POTATO", "Potato Mode Key", "ポテトモードキー", "土豆模式快捷键");
      Add("SETTING_KEY_PIP", "PiP Mode Key", "PiPモードキー", "小窗模式快捷键");
      Add("SETTING_KEY_MIRROR", "Mirror Key", "ミラーキー", "镜像模式快捷键");

      // Input
      Add("SETTING_KEY_PORTRAIT", "Portrait Key", "縦画面キー", "竖屏优化快捷键");

      // Drag Mode Options
      Add("DRAG_MODE_CTRL", "Ctrl + Left Click", "Ctrl + 左クリック", "Ctrl + 左键");
      Add("DRAG_MODE_ALT", "Alt + Left Click", "Alt + 左クリック", "Alt + 左键");
      Add("DRAG_MODE_RIGHT", "Right Key Hold", "右クリックホールド", "右键按住");

      // Todo Delete Confirmation
      Add("TODO_DELETE_CONFIRM_PROMPT", "Are you sure you want to delete this task?", "このタスクを削除してもよろしいですか？", "确定要删除此待办事项吗？");
      Add("TODO_DELETE_CONFIRM_OK", "Confirm", "確定", "确定");
      Add("TODO_DELETE_CONFIRM_CANCEL", "Cancel", "キャンセル", "取消");
    }

    public static string Get(string key, GameLanguageType lang)
    {
      if (string.IsNullOrEmpty(key)) return "";

      if (_translations.TryGetValue(key, out var langDict))
      {
        if (langDict.TryGetValue(lang, out var text))
          return text;

        // Fallback to English
        if (langDict.TryGetValue(GameLanguageType.English, out var enText))
          return enText;

        return $"[{key}]"; // Totally missing
      }

      // Fallback: if key is not found, maybe it IS the test (legacy support)
      // But ideally we return the key so we spot missing translations
      return key;
    }
  }
}
