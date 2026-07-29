using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PotatoOptimization.Utilities
{
    public sealed class TodoExportItem
    {
        public TodoExportItem(string text, bool isCompleted, DateTime? expire)
        {
            Text = text ?? string.Empty;
            IsCompleted = isCompleted;
            Expire = expire;
        }

        public string Text { get; }

        public bool IsCompleted { get; }

        public DateTime? Expire { get; }
    }

    public static class TodoExportFormatter
    {
        public static string Format(
            IReadOnlyList<TodoExportItem> items,
            ExportLanguage language)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            string dueDatePrefix = ExportLocalization.GetDueDatePrefix(language);
            return string.Join(
                "\r\n\r\n",
                items.Select(item => FormatItem(item, dueDatePrefix)));
        }

        private static string FormatItem(TodoExportItem item, string dueDatePrefix)
        {
            string marker = item.IsCompleted ? "- [x] " : "- [ ] ";
            string normalizedText = NormalizeLineEndings(item.Text);
            string[] textLines = normalizedText.Split(new[] { '\n' });
            string content = marker + textLines[0];

            if (textLines.Length > 1)
            {
                content += "\r\n" + string.Join(
                    "\r\n",
                    textLines.Skip(1).Select(line => "  " + line));
            }

            if (item.Expire.HasValue)
            {
                content += "\r\n  - " + dueDatePrefix +
                    item.Expire.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            return content;
        }

        private static string NormalizeLineEndings(string value)
        {
            return (value ?? string.Empty)
                .Replace("\r\n", "\n")
                .Replace("\r", "\n");
        }
    }
}
