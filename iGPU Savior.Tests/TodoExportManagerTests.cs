using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Bulbul;
using PotatoOptimization.Features;
using PotatoOptimization.Utilities;
using Xunit;

namespace IGPUSavior.Tests
{
    public class TodoExportManagerTests
    {
        [Fact]
        public void CreateSnapshot_UsesTodoOrderAndIncludesBothStates()
        {
            var list = new TodoListData
            {
                TitleText = "Tasks",
                TodoDic = new Dictionary<ulong, TodoData>
                {
                    [1] = new TodoData
                    {
                        UniqueID = 1,
                        TodoText = "Completed",
                        CurrentState = TodoState.Complete,
                    },
                    [2] = new TodoData
                    {
                        UniqueID = 2,
                        TodoText = "Pending",
                        CurrentState = TodoState.Working,
                        Expire = new DateTime(2026, 8, 4),
                    },
                },
                TodoOrderList = new List<ulong> { 2, 1 },
            };

            TodoExportSnapshot snapshot = TodoExportManager.CreateSnapshot(list);

            Assert.Equal("Tasks", snapshot.Title);
            Assert.Equal(new[] { "Pending", "Completed" }, snapshot.Items.Select(item => item.Text));
            Assert.False(snapshot.Items[0].IsCompleted);
            Assert.True(snapshot.Items[1].IsCompleted);
            Assert.Equal(new DateTime(2026, 8, 4), snapshot.Items[0].Expire);
        }

        [Fact]
        public void ExportCurrentList_WritesUtf8BomTxtFile()
        {
            string exportFolder = Path.Combine(
                Path.GetTempPath(),
                "iGPU-Savior-TodoExport-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(exportFolder);

            try
            {
                var list = new TodoListData
                {
                    TitleText = "My Tasks",
                    TodoDic = new Dictionary<ulong, TodoData>
                    {
                        [1] = new TodoData
                        {
                            UniqueID = 1,
                            TodoText = "Task",
                            CurrentState = TodoState.Working,
                        },
                    },
                    TodoOrderList = new List<ulong> { 1 },
                };
                var manager = new TodoExportManager(
                    () => list,
                    CreateFolderPicker(exportFolder));

                manager.ExportCurrentList();

                string filePath = Assert.Single(Directory.GetFiles(exportFolder, "*.txt"));
                byte[] bytes = File.ReadAllBytes(filePath);
                Assert.True(bytes.Take(3).SequenceEqual(Encoding.UTF8.GetPreamble()));
                Assert.Equal("- [ ] Task", File.ReadAllText(filePath, Encoding.UTF8));
            }
            finally
            {
                Directory.Delete(exportFolder, true);
            }
        }

        [Fact]
        public void ExportCurrentList_UsesJapaneseContentAndFallbackFileName()
        {
            string exportFolder = Path.Combine(
                Path.GetTempPath(),
                "iGPU-Savior-TodoExport-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(exportFolder);

            try
            {
                var list = new TodoListData
                {
                    TitleText = string.Empty,
                    TodoDic = new Dictionary<ulong, TodoData>
                    {
                        [1] = new TodoData
                        {
                            UniqueID = 1,
                            TodoText = "Task",
                            CurrentState = TodoState.Working,
                            Expire = new DateTime(2026, 8, 4),
                        },
                    },
                    TodoOrderList = new List<ulong> { 1 },
                };
                var manager = new TodoExportManager(
                    () => list,
                    CreateFolderPicker(exportFolder),
                    () => ExportLanguage.Japanese);

                manager.ExportCurrentList();

                string filePath = Assert.Single(Directory.GetFiles(exportFolder, "*.txt"));
                Assert.StartsWith("タスク_", Path.GetFileName(filePath));
                Assert.Equal(
                    "- [ ] Task\r\n  - 期限：2026-08-04",
                    File.ReadAllText(filePath, Encoding.UTF8));
            }
            finally
            {
                Directory.Delete(exportFolder, true);
            }
        }

        [Fact]
        public void ExportCurrentList_WhenPickerCancels_DoesNotWriteOrReportFailure()
        {
            var list = new TodoListData { TitleText = "Tasks" };
            var manager = new TodoExportManager(
                () => list,
                CreateCancelledFolderPicker());
            string failure = null;
            manager.OnExportFailure += message => failure = message;

            manager.ExportCurrentList();

            Assert.Null(failure);
        }

        private static NoteExportFolderPicker CreateFolderPicker(string folderPath)
        {
            return new NoteExportFolderPicker(
                () => IntPtr.Zero,
                work => work(),
                _ => NoteExportFolderPicker.FolderPickerResult.Success(folderPath),
                _ => { },
                _ => { },
                (work, callback) => callback(work()));
        }

        private static NoteExportFolderPicker CreateCancelledFolderPicker()
        {
            return new NoteExportFolderPicker(
                () => IntPtr.Zero,
                work => work(),
                _ => NoteExportFolderPicker.FolderPickerResult.Cancelled(),
                _ => { },
                _ => { },
                (work, callback) => callback(work()));
        }
    }
}
