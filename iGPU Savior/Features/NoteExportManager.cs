using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PotatoOptimization.Core;
using PotatoOptimization.Utilities;

namespace PotatoOptimization.Features
{
    public class NoteExportManager
    {
        private readonly NoteExportSelectionState _selectionState = new NoteExportSelectionState();
        private readonly NoteService _noteService;
        private readonly NoteExportFolderPicker _folderPicker;

        public event Action<bool> OnSelectionModeChanged;
        public event Action<IReadOnlyCollection<ulong>> OnSelectionChanged;
        public event Action<string> OnExportSuccess;
        public event Action<string> OnExportFailure;

        internal Action<Action> DispatchToMainThread { get; set; } = action => action?.Invoke();

        public NoteExportManager(NoteService noteService, NoteExportFolderPicker folderPicker)
        {
            _noteService = noteService;
            _folderPicker = folderPicker;
        }

        public bool IsSelectionMode => _selectionState.IsSelectionMode;

        public IReadOnlyCollection<ulong> SelectedPageIds => _selectionState.SelectedPageIds;

        public int SelectedCount => _selectionState.SelectedPageIds.Count;

        internal static bool ShouldReuseManagerForService(NoteExportManager manager, NoteService noteService)
        {
            return manager != null && ReferenceEquals(manager._noteService, noteService);
        }

        internal static NoteExportManager RebindManagerSubscriptions(
            NoteExportManager currentManager,
            NoteExportManager replacementManager,
            Action<bool> selectionModeChangedHandler,
            Action<IReadOnlyCollection<ulong>> selectionChangedHandler)
        {
            if (ReferenceEquals(currentManager, replacementManager))
            {
                return currentManager;
            }

            if (currentManager != null)
            {
                if (selectionModeChangedHandler != null)
                {
                    currentManager.OnSelectionModeChanged -= selectionModeChangedHandler;
                }

                if (selectionChangedHandler != null)
                {
                    currentManager.OnSelectionChanged -= selectionChangedHandler;
                }
            }

            if (replacementManager != null)
            {
                if (selectionModeChangedHandler != null)
                {
                    replacementManager.OnSelectionModeChanged += selectionModeChangedHandler;
                }

                if (selectionChangedHandler != null)
                {
                    replacementManager.OnSelectionChanged += selectionChangedHandler;
                }
            }

            return replacementManager;
        }

        public void EnterSelectionMode()
        {
            _selectionState.EnterSelectionMode();
            OnSelectionModeChanged?.Invoke(_selectionState.IsSelectionMode);
            OnSelectionChanged?.Invoke(_selectionState.SelectedPageIds);
        }

        public void ToggleSelection(ulong pageId)
        {
            if (!_selectionState.IsSelectionMode)
            {
                return;
            }

            _selectionState.ToggleSelection(pageId);
            OnSelectionChanged?.Invoke(_selectionState.SelectedPageIds);
        }

        public void ExitSelectionMode()
        {
            _selectionState.ExitSelectionMode();
            OnSelectionModeChanged?.Invoke(_selectionState.IsSelectionMode);
            OnSelectionChanged?.Invoke(_selectionState.SelectedPageIds);
        }

        public bool IsSelected(ulong pageId)
        {
            return _selectionState.IsSelected(pageId);
        }

        public void ExportSelected()
        {
            if (SelectedCount == 0)
            {
                OnExportFailure?.Invoke("No notes selected.");
                return;
            }

            if (_noteService == null || _folderPicker == null)
            {
                OnExportFailure?.Invoke("Export dependencies are unavailable.");
                return;
            }

            IReadOnlyList<ulong> orderedPageIds = GetStableExportOrder(SelectedPageIds);
            _folderPicker.BeginPickFolder(result =>
            {
                DispatchToMainThread(() => ContinueExport(result, orderedPageIds));
            });
        }

        private void ContinueExport(NoteExportFolderPicker.FolderPickerResult result, IReadOnlyList<ulong> orderedPageIds)
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

            string folderPath = result.FolderPath;

            int exportedCount = 0;
            int failedCount = 0;
            HashSet<string> reservedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (ulong pageId in orderedPageIds)
            {
                Bulbul.PageDataV2 pageData = _noteService.GetPageSaveData(pageId);
                if (pageData == null)
                {
                    LogWarning($"[NoteExport] Skipping page {pageId}: page data was unavailable.");
                    failedCount++;
                    continue;
                }

                string title = _noteService.GetPageTitle(pageId) ?? string.Empty;
                string filePath = GetUniqueExportFilePath(folderPath, title, reservedPaths);
                string content = NoteExportNaming.FormatExportContent(title, pageData.MainText);

                try
                {
                    File.WriteAllText(filePath, content, new UTF8Encoding(true));
                    exportedCount++;
                }
                catch (Exception ex)
                {
                    LogError($"[NoteExport] Failed to export page {pageId} to '{filePath}': {ex}");
                    failedCount++;
                }
            }

            if (exportedCount > 0 && failedCount == 0)
            {
                ExitSelectionMode();
                OnExportSuccess?.Invoke($"Exported {exportedCount} note(s) to {folderPath}");
                return;
            }

            if (exportedCount > 0)
            {
                ExitSelectionMode();
                OnExportFailure?.Invoke($"Export partially failed: exported {exportedCount} of {orderedPageIds.Count} note(s) to {folderPath}.");
                return;
            }

            OnExportFailure?.Invoke("Export failed.");
        }

        internal static IReadOnlyList<ulong> GetStableExportOrder(IReadOnlyCollection<ulong> selectedPageIds)
        {
            return selectedPageIds
                .OrderBy(pageId => pageId)
                .ToArray();
        }

        private static string GetUniqueExportFilePath(string folderPath, string title, HashSet<string> reservedPaths)
        {
            string originalFileName = NoteExportNaming.GenerateExportFileName(title);
            string candidatePath = Path.Combine(folderPath, originalFileName);
            if (!File.Exists(candidatePath) && reservedPaths.Add(candidatePath))
            {
                return candidatePath;
            }

            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
            string extension = Path.GetExtension(originalFileName);

            int suffix = 2;
            while (true)
            {
                string suffixedFileName = $"{fileNameWithoutExtension}_{suffix}{extension}";
                candidatePath = Path.Combine(folderPath, suffixedFileName);
                if (!File.Exists(candidatePath) && reservedPaths.Add(candidatePath))
                {
                    return candidatePath;
                }

                suffix++;
            }
        }

        private static void LogWarning(string message)
        {
            if (PotatoPlugin.Log != null)
            {
                PotatoPlugin.Log.LogWarning(message);
            }
        }

        private static void LogError(string message)
        {
            if (PotatoPlugin.Log != null)
            {
                PotatoPlugin.Log.LogError(message);
            }
        }
    }
}
