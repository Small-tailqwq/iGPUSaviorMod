using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using PotatoOptimization.Features;
using PotatoOptimization.Utilities;
using Xunit;

namespace IGPUSavior.Tests
{
    public class NoteExportManagerStateTests
    {
        [Fact]
        public void EnterSelectionMode_SetsSelectionMode_AndClearsPreviousSelection()
        {
            var state = new NoteExportSelectionState();

            state.EnterSelectionMode();
            state.ToggleSelection(10UL);

            state.EnterSelectionMode();

            Assert.True(state.IsSelectionMode);
            Assert.Empty(state.SelectedPageIds);
        }

        [Fact]
        public void ToggleSelection_AddsAndRemovesIds()
        {
            var state = new NoteExportSelectionState();

            state.EnterSelectionMode();
            state.ToggleSelection(10UL);
            state.ToggleSelection(20UL);
            state.ToggleSelection(10UL);

            Assert.Equal(new ulong[] { 20UL }, state.SelectedPageIds.ToArray());
        }

        [Fact]
        public void ExitSelectionMode_SetsSelectionModeFalse_AndClearsSelection()
        {
            var state = new NoteExportSelectionState();

            state.EnterSelectionMode();
            state.ToggleSelection(10UL);

            state.ExitSelectionMode();

            Assert.False(state.IsSelectionMode);
            Assert.Empty(state.SelectedPageIds);
        }

        [Fact]
        public void IsSelected_ReflectsCurrentSelection()
        {
            var state = new NoteExportSelectionState();

            state.EnterSelectionMode();
            state.ToggleSelection(10UL);

            Assert.True(state.IsSelected(10UL));
            Assert.False(state.IsSelected(20UL));
        }

        [Fact]
        public void SelectedPageIds_ExposesCurrentSetReadOnly()
        {
            var state = new NoteExportSelectionState();

            state.EnterSelectionMode();
            state.ToggleSelection(10UL);
            state.ToggleSelection(20UL);

            Assert.Equal(2, state.SelectedPageIds.Count);
            Assert.Contains(10UL, state.SelectedPageIds);
            Assert.Contains(20UL, state.SelectedPageIds);
        }

        [Fact]
        public void SelectedPageIds_ReturnsSnapshotThatDoesNotChangeAfterLaterToggle()
        {
            var state = new NoteExportSelectionState();

            state.EnterSelectionMode();
            state.ToggleSelection(10UL);

            var snapshot = state.SelectedPageIds;

            state.ToggleSelection(20UL);

            Assert.Single(snapshot);
            Assert.Contains(10UL, snapshot);
            Assert.DoesNotContain(20UL, snapshot);
            Assert.Equal(2, state.SelectedPageIds.Count);
        }

        [Fact]
        public void ShouldReuseManagerForService_WhenManagerTracksSameService_ReturnsTrue()
        {
            var noteService = new NoteService();
            var manager = new NoteExportManager(noteService, CreateCancelledFolderPicker());

            bool shouldReuse = NoteExportManager.ShouldReuseManagerForService(manager, noteService);

            Assert.True(shouldReuse);
        }

        [Fact]
        public void ShouldReuseManagerForService_WhenManagerTracksDifferentService_ReturnsFalse()
        {
            var manager = new NoteExportManager(new NoteService(), CreateCancelledFolderPicker());

            bool shouldReuse = NoteExportManager.ShouldReuseManagerForService(manager, new NoteService());

            Assert.False(shouldReuse);
        }

        [Fact]
        public void RebindManagerSubscriptions_WhenManagerChanges_MovesNotificationsToReplacementManager()
        {
            var firstManager = CreateManager();
            var secondManager = CreateManager();
            int modeChangedCallCount = 0;
            int selectionChangedCallCount = 0;
            Action<bool> modeChangedHandler = _ => modeChangedCallCount++;
            Action<IReadOnlyCollection<ulong>> selectionChangedHandler = _ => selectionChangedCallCount++;

            var boundManager = NoteExportManager.RebindManagerSubscriptions(
                null,
                firstManager,
                modeChangedHandler,
                selectionChangedHandler);

            boundManager = NoteExportManager.RebindManagerSubscriptions(
                boundManager,
                secondManager,
                modeChangedHandler,
                selectionChangedHandler);

            firstManager.EnterSelectionMode();
            secondManager.EnterSelectionMode();

            Assert.Same(secondManager, boundManager);
            Assert.Equal(1, modeChangedCallCount);
            Assert.Equal(1, selectionChangedCallCount);
        }

        [Fact]
        public void RebindManagerSubscriptions_WhenManagerIsUnchanged_DoesNotDuplicateSubscriptions()
        {
            var manager = CreateManager();
            int modeChangedCallCount = 0;
            int selectionChangedCallCount = 0;
            Action<bool> modeChangedHandler = _ => modeChangedCallCount++;
            Action<IReadOnlyCollection<ulong>> selectionChangedHandler = _ => selectionChangedCallCount++;

            var boundManager = NoteExportManager.RebindManagerSubscriptions(
                null,
                manager,
                modeChangedHandler,
                selectionChangedHandler);

            boundManager = NoteExportManager.RebindManagerSubscriptions(
                boundManager,
                manager,
                modeChangedHandler,
                selectionChangedHandler);

            manager.EnterSelectionMode();

            Assert.Same(manager, boundManager);
            Assert.Equal(1, modeChangedCallCount);
            Assert.Equal(1, selectionChangedCallCount);
        }

        [Fact]
        public void ToggleSelection_OutsideSelectionMode_DoesNothing()
        {
            var state = new NoteExportSelectionState();

            state.ToggleSelection(10UL);

            Assert.False(state.IsSelectionMode);
            Assert.Empty(state.SelectedPageIds);
            Assert.False(state.IsSelected(10UL));
        }

        [Fact]
        public void Manager_EnterSelectionMode_RaisesModeAndSelectionNotifications()
        {
            var manager = CreateManager();
            bool? modeChanged = null;
            IReadOnlyCollection<ulong> selectionChanged = null;

            manager.OnSelectionModeChanged += isSelectionMode => modeChanged = isSelectionMode;
            manager.OnSelectionChanged += selectedPageIds => selectionChanged = selectedPageIds;

            manager.EnterSelectionMode();

            Assert.True(modeChanged);
            Assert.NotNull(selectionChanged);
            Assert.Empty(selectionChanged);
        }

        [Fact]
        public void Manager_ToggleSelection_InSelectionMode_RaisesSelectionChangedWithSnapshot()
        {
            var manager = CreateManager();
            int selectionChangedCallCount = 0;
            IReadOnlyCollection<ulong> firstPayload = null;

            manager.OnSelectionChanged += selectedPageIds =>
            {
                selectionChangedCallCount++;
                if (selectionChangedCallCount == 2)
                {
                    firstPayload = selectedPageIds;
                }
            };

            manager.EnterSelectionMode();
            manager.ToggleSelection(10UL);
            manager.ToggleSelection(20UL);

            Assert.Equal(3, selectionChangedCallCount);
            Assert.NotNull(firstPayload);
            Assert.Single(firstPayload);
            Assert.Contains(10UL, firstPayload);
            Assert.DoesNotContain(20UL, firstPayload);
            Assert.Equal(new ulong[] { 10UL, 20UL }, manager.SelectedPageIds.OrderBy(id => id));
        }

        [Fact]
        public void Manager_ToggleSelection_OutsideSelectionMode_DoesNotRaiseSelectionChanged()
        {
            var manager = CreateManager();
            int selectionChangedCallCount = 0;

            manager.OnSelectionChanged += _ => selectionChangedCallCount++;

            manager.ToggleSelection(10UL);

            Assert.Equal(0, selectionChangedCallCount);
            Assert.Empty(manager.SelectedPageIds);
        }

        [Fact]
        public void ExportSelected_WithNoSelection_RaisesFailure()
        {
            var noteService = new NoteService();
            var manager = new NoteExportManager(noteService, CreateCancelledFolderPicker());
            string failureMessage = null;

            manager.OnExportFailure += message => failureMessage = message;

            manager.ExportSelected();

            Assert.NotNull(failureMessage);
        }

        [Fact]
        public void ExportSelected_WhenFolderPickerCancels_KeepsSelectionModeAndSelection()
        {
            var noteService = new NoteService();
            noteService.SetPage(10UL, "Title", "Body");
            var manager = new NoteExportManager(noteService, CreateCancelledFolderPicker());
            string successMessage = null;
            string failureMessage = null;

            manager.OnExportSuccess += message => successMessage = message;
            manager.OnExportFailure += message => failureMessage = message;
            manager.EnterSelectionMode();
            manager.ToggleSelection(10UL);

            manager.ExportSelected();

            Assert.True(manager.IsSelectionMode);
            Assert.Equal(new ulong[] { 10UL }, manager.SelectedPageIds);
            Assert.Null(successMessage);
            Assert.Null(failureMessage);
        }

        [Fact]
        public void ExportSelected_WhenFolderPickerFails_RaisesFailureAndKeepsSelection()
        {
            var noteService = new NoteService();
            noteService.SetPage(10UL, "Title", "Body");
            var manager = new NoteExportManager(noteService, CreateFailedFolderPicker("picker failed"));
            string successMessage = null;
            string failureMessage = null;

            manager.OnExportSuccess += message => successMessage = message;
            manager.OnExportFailure += message => failureMessage = message;
            manager.EnterSelectionMode();
            manager.ToggleSelection(10UL);

            manager.ExportSelected();

            Assert.True(manager.IsSelectionMode);
            Assert.Equal(new ulong[] { 10UL }, manager.SelectedPageIds);
            Assert.Null(successMessage);
            Assert.NotNull(failureMessage);
            Assert.Contains("folder", failureMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ExportSelected_UsesAsyncFolderPickerCallbackBeforeCompletingExport()
        {
            string exportFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(exportFolder);

            try
            {
                var noteService = new NoteService();
                noteService.SetPage(10UL, "Async Title", "Async Body");
                Func<NoteExportFolderPicker.FolderPickerResult> pendingWork = null;
                Action<NoteExportFolderPicker.FolderPickerResult> pendingCallback = null;
                var picker = new NoteExportFolderPicker(
                    () => IntPtr.Zero,
                    work => work(),
                    _ => NoteExportFolderPicker.FolderPickerResult.Success(exportFolder),
                    _ => { },
                    _ => { },
                    (work, onCompleted) =>
                    {
                        pendingWork = work;
                        pendingCallback = onCompleted;
                    });
                var manager = new NoteExportManager(noteService, picker);
                manager.DispatchToMainThread = action => action();

                manager.EnterSelectionMode();
                manager.ToggleSelection(10UL);

                manager.ExportSelected();

                Assert.NotNull(pendingWork);
                Assert.NotNull(pendingCallback);
                Assert.Empty(Directory.GetFiles(exportFolder, "*.txt"));
                Assert.True(manager.IsSelectionMode);

                pendingCallback(pendingWork());

                Assert.Single(Directory.GetFiles(exportFolder, "*.txt"));
                Assert.False(manager.IsSelectionMode);
            }
            finally
            {
                if (Directory.Exists(exportFolder))
                {
                    Directory.Delete(exportFolder, recursive: true);
                }
            }
        }

        [Fact]
        public void ExportSelected_WritesUtf8BomFiles_ClearsSelectionAndRaisesSuccess()
        {
            string exportFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(exportFolder);

                try
                {
                    var noteService = new NoteService();
                    noteService.SetPage(10UL, "My:/\\*?\"<>| Note", "Line 1\nLine 2");
                var manager = new NoteExportManager(noteService, CreateFolderPicker(exportFolder));
                    string successMessage = null;

                manager.OnExportSuccess += message => successMessage = message;
                manager.EnterSelectionMode();
                manager.ToggleSelection(10UL);

                manager.ExportSelected();

                string exportedFilePath = Directory.GetFiles(exportFolder, "*.txt").Single();
                byte[] bytes = File.ReadAllBytes(exportedFilePath);
                string content = File.ReadAllText(exportedFilePath, Encoding.UTF8);
                string expectedContent = NoteExportNaming.FormatExportContent("My:/\\*?\"<>| Note", "Line 1\nLine 2");

                Assert.True(bytes.Length >= 3);
                Assert.Equal(0xEF, bytes[0]);
                Assert.Equal(0xBB, bytes[1]);
                Assert.Equal(0xBF, bytes[2]);
                Assert.Equal(expectedContent, content);
                Assert.False(manager.IsSelectionMode);
                Assert.Empty(manager.SelectedPageIds);
                Assert.Equal(0, manager.SelectedCount);
                Assert.NotNull(successMessage);
                Assert.Contains("1", successMessage);
                Assert.Contains(exportFolder, successMessage);
            }
            finally
            {
                if (Directory.Exists(exportFolder))
                {
                    Directory.Delete(exportFolder, recursive: true);
                }
            }
        }

        [Fact]
        public void ExportSelected_WhenAllExportsFail_RaisesFailureAndKeepsSelection()
        {
            string missingFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing");
            var noteService = new NoteService();
            noteService.SetPage(10UL, "Title", "Body");
            var manager = new NoteExportManager(noteService, CreateFolderPicker(missingFolder));
            string failureMessage = null;

            manager.OnExportFailure += message => failureMessage = message;
            manager.EnterSelectionMode();
            manager.ToggleSelection(10UL);

            manager.ExportSelected();

            Assert.NotNull(failureMessage);
            Assert.True(manager.IsSelectionMode);
            Assert.Equal(new ulong[] { 10UL }, manager.SelectedPageIds);
            Assert.Equal(1, manager.SelectedCount);
        }

        [Fact]
        public void ExportSelected_WhenSomeExportsFail_RaisesFailureInsteadOfSuccess()
        {
            string exportFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(exportFolder);

            try
            {
                var noteService = new NoteService();
                noteService.SetPage(10UL, "First", "First body");
                noteService.SetPage(20UL, "Second", "Second body");
                noteService.SetPageData(20UL, null);
                var manager = new NoteExportManager(noteService, CreateFolderPicker(exportFolder));
                string successMessage = null;
                string failureMessage = null;

                manager.OnExportSuccess += message => successMessage = message;
                manager.OnExportFailure += message => failureMessage = message;
                manager.EnterSelectionMode();
                manager.ToggleSelection(10UL);
                manager.ToggleSelection(20UL);

                manager.ExportSelected();

                string[] exportedFiles = Directory.GetFiles(exportFolder, "*.txt");

                Assert.Single(exportedFiles);
                Assert.Null(successMessage);
                Assert.NotNull(failureMessage);
                Assert.Contains("partial", failureMessage, StringComparison.OrdinalIgnoreCase);
                Assert.False(manager.IsSelectionMode);
                Assert.Empty(manager.SelectedPageIds);
            }
            finally
            {
                if (Directory.Exists(exportFolder))
                {
                    Directory.Delete(exportFolder, recursive: true);
                }
            }
        }

        [Fact]
        public void ExportSelected_WhenBatchHasCollidingFileNames_WritesDistinctFiles()
        {
            string exportFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(exportFolder);

            try
            {
                var noteService = new NoteService();
                noteService.SetPage(10UL, "Same Title", "First body");
                noteService.SetPage(20UL, "Same Title", "Second body");
                var manager = new NoteExportManager(noteService, CreateFolderPicker(exportFolder));

                manager.EnterSelectionMode();
                manager.ToggleSelection(10UL);
                manager.ToggleSelection(20UL);

                manager.ExportSelected();

                string[] exportedFiles = Directory.GetFiles(exportFolder, "*.txt");
                Assert.Equal(2, exportedFiles.Length);
                Assert.Contains(exportedFiles, path => !Path.GetFileNameWithoutExtension(path).EndsWith("_2", StringComparison.Ordinal));
                Assert.Contains(exportedFiles, path => Path.GetFileNameWithoutExtension(path).EndsWith("_2", StringComparison.Ordinal));

                string firstContent = File.ReadAllText(exportedFiles.Single(path => !Path.GetFileNameWithoutExtension(path).EndsWith("_2", StringComparison.Ordinal)), Encoding.UTF8);
                string secondContent = File.ReadAllText(exportedFiles.Single(path => Path.GetFileNameWithoutExtension(path).EndsWith("_2", StringComparison.Ordinal)), Encoding.UTF8);

                Assert.Equal(NoteExportNaming.FormatExportContent("Same Title", "First body"), firstContent);
                Assert.Equal(NoteExportNaming.FormatExportContent("Same Title", "Second body"), secondContent);
            }
            finally
            {
                if (Directory.Exists(exportFolder))
                {
                    Directory.Delete(exportFolder, recursive: true);
                }
            }
        }

        [Fact]
        public void ExportSelected_WhenDuplicateTitlesAreSelectedOutOfOrder_UsesStablePageOrderForFileNames()
        {
            string exportFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(exportFolder);

            try
            {
                var noteService = new NoteService();
                noteService.SetPage(10UL, "Same Title", "Lower page body");
                noteService.SetPage(20UL, "Same Title", "Higher page body");
                var manager = new NoteExportManager(noteService, CreateFolderPicker(exportFolder));

                manager.EnterSelectionMode();
                manager.ToggleSelection(20UL);
                manager.ToggleSelection(10UL);

                manager.ExportSelected();

                string[] exportedFiles = Directory.GetFiles(exportFolder, "*.txt");
                string baseFilePath = exportedFiles.Single(path => !Path.GetFileNameWithoutExtension(path).EndsWith("_2", StringComparison.Ordinal));
                string suffixedFilePath = exportedFiles.Single(path => Path.GetFileNameWithoutExtension(path).EndsWith("_2", StringComparison.Ordinal));

                Assert.Equal(
                    NoteExportNaming.FormatExportContent("Same Title", "Lower page body"),
                    File.ReadAllText(baseFilePath, Encoding.UTF8));
                Assert.Equal(
                    NoteExportNaming.FormatExportContent("Same Title", "Higher page body"),
                    File.ReadAllText(suffixedFilePath, Encoding.UTF8));
            }
            finally
            {
                if (Directory.Exists(exportFolder))
                {
                    Directory.Delete(exportFolder, recursive: true);
                }
            }
        }

        [Fact]
        public void ExportSelected_WhenTargetFolderAlreadyContainsFile_WritesNextAvailableName()
        {
            string exportFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(exportFolder);

            try
            {
                string sanitizedTitle = NoteExportNaming.SanitizeTitle("Existing Title");
                string[] existingFilePaths = Enumerable.Range(-2, 5)
                    .Select(offset => Path.Combine(exportFolder, $"{sanitizedTitle}_{DateTime.Now.AddSeconds(offset):yyyyMMdd_HHmmss}.txt"))
                    .ToArray();

                foreach (string existingFilePath in existingFilePaths)
                {
                    File.WriteAllText(existingFilePath, "existing", Encoding.UTF8);
                }

                var noteService = new NoteService();
                noteService.SetPage(10UL, "Existing Title", "Fresh body");
                var manager = new NoteExportManager(noteService, CreateFolderPicker(exportFolder));

                manager.EnterSelectionMode();
                manager.ToggleSelection(10UL);

                manager.ExportSelected();

                string[] exportedFiles = Directory.GetFiles(exportFolder, "*.txt");
                Assert.Equal(existingFilePaths.Length + 1, exportedFiles.Length);

                foreach (string existingFilePath in existingFilePaths)
                {
                    Assert.Equal("existing", File.ReadAllText(existingFilePath, Encoding.UTF8));
                }

                string newExportPath = exportedFiles.Single(path => !existingFilePaths.Contains(path, StringComparer.OrdinalIgnoreCase));
                Assert.EndsWith("_2.txt", Path.GetFileName(newExportPath), StringComparison.Ordinal);
                Assert.Equal(NoteExportNaming.FormatExportContent("Existing Title", "Fresh body"), File.ReadAllText(newExportPath, Encoding.UTF8));
            }
            finally
            {
                if (Directory.Exists(exportFolder))
                {
                    Directory.Delete(exportFolder, recursive: true);
                }
            }
        }

        [Fact]
        public void NoteExportManager_DoesNotExposePublicParameterlessConstructor()
        {
            ConstructorInfo publicParameterlessConstructor = typeof(NoteExportManager).GetConstructor(Type.EmptyTypes);

            Assert.Null(publicParameterlessConstructor);
        }

        private static NoteExportManager CreateManager()
        {
            return new NoteExportManager(new NoteService(), CreateCancelledFolderPicker());
        }

        private static NoteExportFolderPicker CreateCancelledFolderPicker()
        {
            return new NoteExportFolderPicker(
                () => IntPtr.Zero,
                work => work(),
                _ => NoteExportFolderPicker.FolderPickerResult.Cancelled(),
                _ => { },
                _ => { },
                (work, onCompleted) => onCompleted(work()));
        }

        private static NoteExportFolderPicker CreateFolderPicker(string folderPath)
        {
            return new NoteExportFolderPicker(
                () => IntPtr.Zero,
                work => work(),
                _ => NoteExportFolderPicker.FolderPickerResult.Success(folderPath),
                _ => { },
                _ => { },
                (work, onCompleted) => onCompleted(work()));
        }

        private static NoteExportFolderPicker CreateFailedFolderPicker(string message)
        {
            return new NoteExportFolderPicker(
                () => IntPtr.Zero,
                work => work(),
                _ => NoteExportFolderPicker.FolderPickerResult.Failed(new InvalidOperationException(message)),
                _ => { },
                _ => { },
                (work, onCompleted) => onCompleted(work()));
        }
    }
}
