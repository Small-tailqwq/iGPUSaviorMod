namespace PotatoOptimization.Utilities
{
    public enum ExportLanguage
    {
        English,
        Japanese,
        Chinese,
    }

    public static class ExportLocalization
    {
        public static string GetDueDatePrefix(ExportLanguage language)
        {
            switch (language)
            {
                case ExportLanguage.Japanese:
                    return "期限：";
                case ExportLanguage.Chinese:
                    return "到期日：";
                default:
                    return "Due date: ";
            }
        }

        public static string GetNoteFallbackTitle(ExportLanguage language)
        {
            switch (language)
            {
                case ExportLanguage.Japanese:
                    return "ノート";
                case ExportLanguage.Chinese:
                    return "笔记";
                default:
                    return "note";
            }
        }

        public static string GetTodoFallbackTitle(ExportLanguage language)
        {
            switch (language)
            {
                case ExportLanguage.Japanese:
                    return "タスク";
                case ExportLanguage.Chinese:
                    return "待办";
                default:
                    return "todo";
            }
        }
    }
}
