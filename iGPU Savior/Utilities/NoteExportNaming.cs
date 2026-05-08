using System;

namespace PotatoOptimization.Utilities
{
    public static class NoteExportNaming
    {
        private const string WindowsInvalidFileNameChars = "<>:\"/\\|?*";

        public static string SanitizeTitle(string title)
        {
            string sanitizedTitle = title ?? string.Empty;

            foreach (char currentChar in sanitizedTitle)
            {
                if (currentChar < 32 || WindowsInvalidFileNameChars.IndexOf(currentChar) >= 0)
                {
                    sanitizedTitle = sanitizedTitle.Replace(currentChar.ToString(), string.Empty);
                }
            }

            return sanitizedTitle.Trim();
        }

        public static string GenerateExportFileName(string title)
        {
            string sanitizedTitle = SanitizeTitle(title);

            if (string.IsNullOrWhiteSpace(sanitizedTitle))
            {
                sanitizedTitle = "note";
            }

            return $"{sanitizedTitle}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
        }

        public static string FormatExportContent(string originalTitle, string body)
        {
            string safeTitle = (originalTitle ?? string.Empty).Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");
            string safeBody = (body ?? string.Empty).Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");

            return safeTitle + "\r\n\r\n" + safeBody;
        }
    }
}
