using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Bulbul;
using PotatoOptimization.Core;
using PotatoOptimization.Utilities;

namespace PotatoOptimization.Features
{
    public sealed class TodoExportManager
    {
        private readonly Func<TodoListData> _currentListProvider;
        private readonly NoteExportFolderPicker _folderPicker;
        private readonly Func<ExportLanguage> _languageProvider;

        public TodoExportManager(
            Func<TodoListData> currentListProvider,
            NoteExportFolderPicker folderPicker,
            Func<ExportLanguage> languageProvider = null)
        {
            _currentListProvider = currentListProvider ??
                throw new ArgumentNullException(nameof(currentListProvider));
            _folderPicker = folderPicker ??
                throw new ArgumentNullException(nameof(folderPicker));
            _languageProvider = languageProvider ?? (() => ExportLanguage.English);
        }

        public event Action<string> OnExportSuccess;

        public event Action<string> OnExportFailure;

        internal Action<Action> DispatchToMainThread { get; set; } = action => action?.Invoke();

        public void ExportCurrentList()
        {
            TodoExportSnapshot snapshot;
            try
            {
                snapshot = CreateSnapshot(_currentListProvider());
            }
            catch (Exception ex)
            {
                LogError("[TodoExport] Failed to capture todo data: " + ex);
                OnExportFailure?.Invoke("Todo data is unavailable.");
                return;
            }

            if (snapshot == null)
            {
                OnExportFailure?.Invoke("Todo data is unavailable.");
                return;
            }

            ExportLanguage language = _languageProvider();
            _folderPicker.BeginPickFolder(result =>
            {
                DispatchToMainThread(() => ContinueExport(result, snapshot, language));
            });
        }

        internal static TodoExportSnapshot CreateSnapshot(TodoListData listData)
        {
            if (listData == null)
            {
                return null;
            }

            var items = new List<TodoExportItem>();
            if (listData.TodoOrderList != null && listData.TodoDic != null)
            {
                foreach (ulong todoId in listData.TodoOrderList)
                {
                    if (!listData.TodoDic.TryGetValue(todoId, out TodoData todoData) ||
                        todoData == null)
                    {
                        continue;
                    }

                    items.Add(new TodoExportItem(
                        todoData.TodoText,
                        todoData.CurrentState == TodoState.Complete,
                        todoData.Expire));
                }
            }

            return new TodoExportSnapshot(listData.TitleText, items);
        }

        private void ContinueExport(
            NoteExportFolderPicker.FolderPickerResult result,
            TodoExportSnapshot snapshot,
            ExportLanguage language)
        {
            if (result == null)
            {
                OnExportFailure?.Invoke("Folder selection failed.");
                return;
            }

            if (result.WasCancelled)
            {
                return;
            }

            if (!result.Succeeded || string.IsNullOrWhiteSpace(result.FolderPath))
            {
                OnExportFailure?.Invoke("Folder selection failed.");
                return;
            }

            var reservedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string filePath = NoteExportNaming.GetUniqueExportFilePath(
                result.FolderPath,
                snapshot.Title,
                ExportLocalization.GetTodoFallbackTitle(language),
                reservedPaths);
            string content = TodoExportFormatter.Format(snapshot.Items, language);

            try
            {
                File.WriteAllText(filePath, content, new UTF8Encoding(true));
                OnExportSuccess?.Invoke($"Exported {snapshot.Items.Count} todo(s) to {filePath}");
            }
            catch (Exception ex)
            {
                LogError($"[TodoExport] Failed to export todos to '{filePath}': {ex}");
                OnExportFailure?.Invoke("Export failed.");
            }
        }

        private static void LogError(string message)
        {
            PotatoPlugin.Log?.LogError(message);
        }
    }

    public sealed class TodoExportSnapshot
    {
        public TodoExportSnapshot(string title, IReadOnlyList<TodoExportItem> items)
        {
            Title = title ?? string.Empty;
            Items = items ?? throw new ArgumentNullException(nameof(items));
        }

        public string Title { get; }

        public IReadOnlyList<TodoExportItem> Items { get; }
    }
}
