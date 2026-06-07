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
      Add("MOD_TAB_TITLE", "MOD", "MOD", "MOD");
      Add("MOD_SETTINGS_TITLE", "MOD Settings", "MOD設定", "MOD 设置");

      // Toggles
      Add("SETTING_MIRROR_AUTO", "Mirror Auto-Start", "ミラー自動起動", "镜像自启动");
      Add("SETTING_PORTRAIT_AUTO", "Portrait Auto-Start", "縦画面自動起動", "竖屏优化自启动");
      Add("SETTING_DELETE_CONFIRM", "Delete Confirmation", "削除確認", "删除二次确认");
      Add("SETTING_PIP_AUTO_HIDE_GUI", "Auto-Hide GUI in PiP", "小窓でGUIを自動非表示", "小窗自动隐藏GUI");

      // Performance
      Add("SETTING_BG_OPTIMIZATION", "Background Optimization", "バックグラウンド最適化", "后台省电优化");

      // Dropdowns
      Add("SETTING_MINI_SCALE", "Initial PiP Scale", "小窓の初期スケール", "小窗初始缩放");
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

      // Note Delete Confirmation
      Add("NOTE_DELETE_CONFIRM_PROMPT", "Are you sure you want to delete this note?", "このノートを削除してもよろしいですか？", "确定要删除这页笔记吗？");

      // Costume Suggestion
      Add("SETTING_WHISPER", "Costume Suggestion", "服装提案", "服装建议");
      Add("WHISPER_NONE", "No Suggestion (Normal Lottery)", "提案なし（通常抽選）", "无建议（使用正常轮换）");
      Add("WHISPER_SET_SUCCESS", "Set! Will take effect soon", "設定完了！適用予定", "设置完成，敬请期待");
      Add("WHISPER_PENDING", "Pending...", "適用待ち", "敬请期待");
      // 服装名翻译（供下拉框显示）
      Add("SKIN_Default_1", "Default Outfit", "デフォルト服", "默认服装");
      Add("SKIN_Polo_1", "Polo Style 1", "ポロスタイル1", "马球衫款式1");
      Add("SKIN_Polo_2", "Polo Style 2", "ポロスタイル2", "马球衫款式2");
      Add("SKIN_Tee_1", "T-Shirt Style 1", "Tシャツスタイル1", "T恤款式1");
      Add("SKIN_Tee_2", "T-Shirt Style 2", "Tシャツスタイル2", "T恤款式2");

      // Note Export
      Add("NOTE_EXPORT_BUTTON", "Export", "エクスポート", "导出");
      Add("NOTE_EXPORT_CANCEL", "Cancel", "キャンセル", "取消");
      Add("NOTE_EXPORT_CONFIRM", "Confirm Export", "エクスポート確認", "确认导出");
      Add("NOTE_EXPORT_SUCCESS", "Export complete", "エクスポート完了", "导出完成");
      Add("NOTE_EXPORT_FAIL", "Export failed", "エクスポート失敗", "导出失败");
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
