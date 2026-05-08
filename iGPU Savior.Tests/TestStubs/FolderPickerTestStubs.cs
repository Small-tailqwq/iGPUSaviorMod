using System;
using System.Collections.Generic;

namespace Bulbul
{
    public class PageDataV2
    {
        public string MainText;
    }
}

public class NoteService
{
    private readonly Dictionary<ulong, string> _titles = new Dictionary<ulong, string>();
    private readonly Dictionary<ulong, Bulbul.PageDataV2> _pages = new Dictionary<ulong, Bulbul.PageDataV2>();

    public void SetPage(ulong pageId, string title, string mainText)
    {
        _titles[pageId] = title;
        _pages[pageId] = new Bulbul.PageDataV2 { MainText = mainText };
    }

    public void SetPageData(ulong pageId, Bulbul.PageDataV2 pageData)
    {
        _pages[pageId] = pageData;
    }

    public string GetPageTitle(ulong pageId)
    {
        return _titles.TryGetValue(pageId, out string title) ? title : null;
    }

    public Bulbul.PageDataV2 GetPageSaveData(ulong pageId)
    {
        return _pages.TryGetValue(pageId, out Bulbul.PageDataV2 pageData) ? pageData : null;
    }
}

namespace PotatoOptimization.Core
{
    public static class PotatoPlugin
    {
        public static TestLogger Log { get; set; }
    }

    public class TestLogger
    {
        public List<string> Errors { get; } = new List<string>();
        public List<string> Warnings { get; } = new List<string>();

        public void LogError(string message)
        {
            Errors.Add(message);
        }

        public void LogWarning(string message)
        {
            Warnings.Add(message);
        }
    }
}

namespace PotatoOptimization.Utilities
{
    public static class WindowManager
    {
        public static IntPtr GetCurrentWindowHandle()
        {
            return IntPtr.Zero;
        }
    }
}
