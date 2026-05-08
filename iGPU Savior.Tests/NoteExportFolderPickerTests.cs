using System;
using System.Collections.Generic;
using System.Threading;
using PotatoOptimization.Features;
using PotatoOptimization.Core;
using Xunit;

namespace IGPUSavior.Tests
{
    public class NoteExportFolderPickerTests
    {
        [Fact]
        public void RunOnStaThread_RunsWorkOnStaThread()
        {
            int executingThreadId = -1;
            ApartmentState apartmentState = ApartmentState.Unknown;

            int result = NoteExportFolderPicker.RunOnStaThread(() =>
            {
                executingThreadId = Thread.CurrentThread.ManagedThreadId;
                apartmentState = Thread.CurrentThread.GetApartmentState();
                return 42;
            });

            Assert.Equal(42, result);
            Assert.Equal(ApartmentState.STA, apartmentState);
            Assert.NotEqual(Thread.CurrentThread.ManagedThreadId, executingThreadId);
        }

        [Fact]
        public void RunOnStaThread_WhenAlreadySta_RunsInlineOnCurrentThread()
        {
            int callerThreadId = -1;
            int executingThreadId = -1;

            RunOnStaTestThread(() =>
            {
                callerThreadId = Thread.CurrentThread.ManagedThreadId;

                int result = NoteExportFolderPicker.RunOnStaThread(() =>
                {
                    executingThreadId = Thread.CurrentThread.ManagedThreadId;
                    return 42;
                });

                Assert.Equal(42, result);
            });

            Assert.Equal(callerThreadId, executingThreadId);
        }

        [Fact]
        public void TryPickFolder_WhenCancelled_ReturnsFalse_AndDoesNotLogError()
        {
            var logger = new TestLogger();
            PotatoPlugin.Log = logger;

            var picker = new NoteExportFolderPicker(
                () => new IntPtr(123),
                work => work(),
                _ => NoteExportFolderPicker.FolderPickerResult.Cancelled(),
                logger.LogWarning,
                logger.LogError,
                (work, onCompleted) => onCompleted(work()));

            bool picked = picker.TryPickFolder(out string folderPath);

            Assert.False(picked);
            Assert.Null(folderPath);
            Assert.Empty(logger.Errors);
            Assert.Equal(NoteExportFolderPicker.FolderPickerOutcome.Cancelled, picker.LastOutcome);
        }

        [Fact]
        public void TryPickFolder_WhenCurrentThreadIsNotSta_ForwardsOwnerHandleToWorkerPath()
        {
            IntPtr observedOwnerHandle = new IntPtr(-1);

            var picker = new NoteExportFolderPicker(
                () => new IntPtr(123),
                work => work(),
                ownerHandle =>
                {
                    observedOwnerHandle = ownerHandle;
                    return NoteExportFolderPicker.FolderPickerResult.Cancelled();
                },
                _ => { },
                _ => { },
                (work, onCompleted) => onCompleted(work()));

            bool picked = picker.TryPickFolder(out string folderPath);

            Assert.False(picked);
            Assert.Null(folderPath);
            Assert.Equal(new IntPtr(123), observedOwnerHandle);
        }

        [Fact]
        public void TryPickFolder_WhenUnexpectedFailure_ReturnsFalse_AndLogsError()
        {
            var logger = new TestLogger();
            PotatoPlugin.Log = logger;

            var picker = new NoteExportFolderPicker(
                () => IntPtr.Zero,
                work => work(),
                _ => NoteExportFolderPicker.FolderPickerResult.Failed(new InvalidOperationException("boom")),
                logger.LogWarning,
                logger.LogError,
                (work, onCompleted) => onCompleted(work()));

            bool picked = picker.TryPickFolder(out string folderPath);

            Assert.False(picked);
            Assert.Null(folderPath);
            Assert.Single(logger.Errors);
            Assert.Contains("boom", logger.Errors[0]);
            Assert.Equal(NoteExportFolderPicker.FolderPickerOutcome.Failed, picker.LastOutcome);
        }

        [Fact]
        public void TryPickFolder_WhenOwnerHandleProviderThrows_ReturnsFalse_AndLogsError()
        {
            var logger = new TestLogger();
            PotatoPlugin.Log = logger;

            var picker = new NoteExportFolderPicker(
                () => throw new InvalidOperationException("owner failed"),
                work => work(),
                _ => NoteExportFolderPicker.FolderPickerResult.Success(@"C:\Exports"),
                logger.LogWarning,
                logger.LogError,
                (work, onCompleted) => onCompleted(work()));

            bool picked = picker.TryPickFolder(out string folderPath);

            Assert.False(picked);
            Assert.Null(folderPath);
            Assert.Single(logger.Errors);
            Assert.Contains("owner failed", logger.Errors[0]);
        }

        [Fact]
        public void TryPickFolder_WhenStaRunnerThrows_ReturnsFalse_AndLogsError()
        {
            var logger = new TestLogger();
            PotatoPlugin.Log = logger;

            var picker = new NoteExportFolderPicker(
                () => IntPtr.Zero,
                _ => throw new InvalidOperationException("sta failed"),
                _ => NoteExportFolderPicker.FolderPickerResult.Success(@"C:\Exports"),
                logger.LogWarning,
                logger.LogError,
                (work, onCompleted) => onCompleted(work()));

            bool picked = picker.TryPickFolder(out string folderPath);

            Assert.False(picked);
            Assert.Null(folderPath);
            Assert.Single(logger.Errors);
            Assert.Contains("sta failed", logger.Errors[0]);
        }

        [Fact]
        public void TryPickFolder_WhenSuccessful_ReturnsChosenFolderPath()
        {
            var logger = new TestLogger();
            PotatoPlugin.Log = logger;

            var picker = new NoteExportFolderPicker(
                () => IntPtr.Zero,
                work => work(),
                _ => NoteExportFolderPicker.FolderPickerResult.Success(@"C:\Exports"),
                logger.LogWarning,
                logger.LogError,
                (work, onCompleted) => onCompleted(work()));

            bool picked = picker.TryPickFolder(out string folderPath);

            Assert.True(picked);
            Assert.Equal(@"C:\Exports", folderPath);
            Assert.Equal(NoteExportFolderPicker.FolderPickerOutcome.Succeeded, picker.LastOutcome);
            Assert.Contains(logger.Warnings, message => message.Contains("Opening folder picker"));
        }

        [Fact]
        public void TryPickFolder_WhenOwnerHandleIsZero_StillLogsOpeningAttempt()
        {
            var logger = new TestLogger();
            PotatoPlugin.Log = logger;

            var picker = new NoteExportFolderPicker(
                () => IntPtr.Zero,
                work => work(),
                _ => NoteExportFolderPicker.FolderPickerResult.Cancelled(),
                logger.LogWarning,
                logger.LogError,
                (work, onCompleted) => onCompleted(work()));

            bool picked = picker.TryPickFolder(out string folderPath);

            Assert.False(picked);
            Assert.Null(folderPath);
            Assert.Contains(logger.Warnings, message => message.Contains("owner=0x0"));
        }

        [Fact]
        public void TryPickFolder_WhenResultPathIsEmpty_ReturnsFalse_AndLeavesFolderPathNull()
        {
            var picker = new NoteExportFolderPicker(
                () => IntPtr.Zero,
                work => work(),
                _ => NoteExportFolderPicker.FolderPickerResult.Success(string.Empty),
                _ => { },
                _ => { },
                (work, onCompleted) => onCompleted(work()));

            bool picked = picker.TryPickFolder(out string folderPath);

            Assert.False(picked);
            Assert.Null(folderPath);
        }

        [Fact]
        public void BeginPickFolder_UsesAsyncRunner_AndInvokesCallbackLater()
        {
            var logger = new TestLogger();
            PotatoPlugin.Log = logger;
            Func<NoteExportFolderPicker.FolderPickerResult> pendingWork = null;
            Action<NoteExportFolderPicker.FolderPickerResult> pendingCallback = null;
            NoteExportFolderPicker.FolderPickerResult observedResult = null;

            var picker = new NoteExportFolderPicker(
                () => IntPtr.Zero,
                work => work(),
                _ => NoteExportFolderPicker.FolderPickerResult.Success(@"C:\Exports"),
                logger.LogWarning,
                logger.LogError,
                (work, onCompleted) =>
                {
                    pendingWork = work;
                    pendingCallback = onCompleted;
                });

            picker.BeginPickFolder(result => observedResult = result);

            Assert.NotNull(pendingWork);
            Assert.NotNull(pendingCallback);
            Assert.Null(observedResult);

            pendingCallback(pendingWork());

            Assert.NotNull(observedResult);
            Assert.True(observedResult.Succeeded);
            Assert.Equal(@"C:\Exports", observedResult.FolderPath);
            Assert.Contains(logger.Warnings, message => message.Contains("Opening folder picker"));
        }

        private static void RunOnStaTestThread(Action action)
        {
            Exception error = null;
            Thread thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    error = ex;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (error != null)
            {
                throw error;
            }
        }
    }
}
