using System;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime.ExceptionServices;
using PotatoOptimization.Core;
using PotatoOptimization.Utilities;

namespace PotatoOptimization.Features
{
    public class NoteExportFolderPicker
    {
        internal enum FolderPickerOutcome
        {
            None,
            Succeeded,
            Cancelled,
            Failed,
        }

        private const uint FOS_PICKFOLDERS = 0x00000020;
        private const uint FOS_FORCEFILESYSTEM = 0x00000040;
        private const uint FOS_PATHMUSTEXIST = 0x00000800;
        private const uint SIGDN_FILESYSPATH = 0x80058000;
        private const int HRESULT_CANCELLED = unchecked((int)0x800704C7);
        private readonly Func<IntPtr> _ownerHandleProvider;
        private readonly Func<Func<FolderPickerResult>, FolderPickerResult> _staRunner;
        private readonly Func<IntPtr, FolderPickerResult> _pickFolderOnCurrentThread;
        private readonly Action<string> _logWarning;
        private readonly Action<string> _logError;
        private readonly Action<Func<FolderPickerResult>, Action<FolderPickerResult>> _asyncRunner;

        public NoteExportFolderPicker()
            : this(
                WindowManager.GetCurrentWindowHandle,
                RunOnStaThread,
                TryPickFolderOnCurrentThread,
                LogWarning,
                LogError,
                RunAsyncOnStaThread)
        {
        }

        internal NoteExportFolderPicker(
            Func<IntPtr> ownerHandleProvider,
            Func<Func<FolderPickerResult>, FolderPickerResult> staRunner,
            Func<IntPtr, FolderPickerResult> pickFolderOnCurrentThread,
            Action<string> logWarning,
            Action<string> logError,
            Action<Func<FolderPickerResult>, Action<FolderPickerResult>> asyncRunner)
        {
            _ownerHandleProvider = ownerHandleProvider ?? throw new ArgumentNullException(nameof(ownerHandleProvider));
            _staRunner = staRunner ?? throw new ArgumentNullException(nameof(staRunner));
            _pickFolderOnCurrentThread = pickFolderOnCurrentThread ?? throw new ArgumentNullException(nameof(pickFolderOnCurrentThread));
            _logWarning = logWarning ?? throw new ArgumentNullException(nameof(logWarning));
            _logError = logError ?? throw new ArgumentNullException(nameof(logError));
            _asyncRunner = asyncRunner ?? throw new ArgumentNullException(nameof(asyncRunner));
        }

        internal FolderPickerOutcome LastOutcome { get; private set; }

        internal void BeginPickFolder(Action<FolderPickerResult> onCompleted)
        {
            if (onCompleted == null)
            {
                throw new ArgumentNullException(nameof(onCompleted));
            }

            LastOutcome = FolderPickerOutcome.None;

            IntPtr ownerHandle;
            try
            {
                ownerHandle = _ownerHandleProvider();
                _logWarning($"[NoteExport] Opening folder picker (owner=0x{ownerHandle.ToInt64():X}).");
            }
            catch (Exception ex)
            {
                _logError($"[NoteExport] Folder picker failed: {ex}");
                LastOutcome = FolderPickerOutcome.Failed;
                onCompleted(FolderPickerResult.Failed(ex));
                return;
            }

            _asyncRunner(
                () => _pickFolderOnCurrentThread(ownerHandle),
                result =>
                {
                    if (result == null)
                    {
                        LastOutcome = FolderPickerOutcome.Failed;
                        onCompleted(FolderPickerResult.Failed(new InvalidOperationException("Folder picker returned no result.")));
                        return;
                    }

                    if (result.WasCancelled)
                    {
                        _logWarning("[NoteExport] Folder picker was cancelled.");
                        LastOutcome = FolderPickerOutcome.Cancelled;
                    }
                    else if (!result.Succeeded || string.IsNullOrEmpty(result.FolderPath))
                    {
                        if (result.Error != null)
                        {
                            _logError($"[NoteExport] Folder picker failed: {result.Error}");
                        }

                        LastOutcome = FolderPickerOutcome.Failed;
                    }
                    else
                    {
                        _logWarning($"[NoteExport] Folder picker selected: {result.FolderPath}");
                        LastOutcome = FolderPickerOutcome.Succeeded;
                    }

                    onCompleted(result);
                });
        }

        public bool TryPickFolder(out string folderPath)
        {
            folderPath = null;
            LastOutcome = FolderPickerOutcome.None;

            FolderPickerResult result;
            try
            {
                IntPtr ownerHandle = _ownerHandleProvider();
                _logWarning($"[NoteExport] Opening folder picker (owner=0x{ownerHandle.ToInt64():X}).");
                result = _staRunner(() => _pickFolderOnCurrentThread(ownerHandle));
            }
            catch (Exception ex)
            {
                _logError($"[NoteExport] Folder picker failed: {ex}");
                LastOutcome = FolderPickerOutcome.Failed;
                return false;
            }

            if (result.WasCancelled)
            {
                _logWarning("[NoteExport] Folder picker was cancelled.");
                LastOutcome = FolderPickerOutcome.Cancelled;
                return false;
            }

            if (!result.Succeeded)
            {
                if (result.Error != null)
                {
                    _logError($"[NoteExport] Folder picker failed: {result.Error}");
                }

                LastOutcome = FolderPickerOutcome.Failed;
                return false;
            }

            if (string.IsNullOrEmpty(result.FolderPath))
            {
                LastOutcome = FolderPickerOutcome.Failed;
                return false;
            }

            folderPath = result.FolderPath;
            _logWarning($"[NoteExport] Folder picker selected: {folderPath}");
            LastOutcome = FolderPickerOutcome.Succeeded;
            return true;
        }

        internal static void RunAsyncOnStaThread(Func<FolderPickerResult> work, Action<FolderPickerResult> onCompleted)
        {
            if (work == null)
            {
                throw new ArgumentNullException(nameof(work));
            }

            if (onCompleted == null)
            {
                throw new ArgumentNullException(nameof(onCompleted));
            }

            Thread thread = new Thread(() =>
            {
                FolderPickerResult result;
                try
                {
                    result = work();
                }
                catch (Exception ex)
                {
                    result = FolderPickerResult.Failed(ex);
                }

                onCompleted(result);
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        internal static T RunOnStaThread<T>(Func<T> work)
        {
            if (work == null)
            {
                throw new ArgumentNullException(nameof(work));
            }

            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            {
                return work();
            }

            T result = default(T);
            Exception error = null;

            Thread thread = new Thread(() =>
            {
                try
                {
                    result = work();
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
                ExceptionDispatchInfo.Capture(error).Throw();
            }

            return result;
        }

        private static FolderPickerResult TryPickFolderOnCurrentThread(IntPtr ownerHandle)
        {
            IFileOpenDialog dialog = null;
            IShellItem shellItem = null;
            IntPtr folderPathPointer = IntPtr.Zero;

            try
            {
                Type dialogType = Type.GetTypeFromCLSID(new Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7"));
                if (dialogType == null)
                {
                    return FolderPickerResult.Failed(new InvalidOperationException("IFileOpenDialog CLSID is unavailable."));
                }

                dialog = (IFileOpenDialog)Activator.CreateInstance(dialogType);
                uint options;
                dialog.GetOptions(out options);
                dialog.SetOptions(options | FOS_PICKFOLDERS | FOS_FORCEFILESYSTEM | FOS_PATHMUSTEXIST);

                int result = dialog.Show(ownerHandle);
                if (result == HRESULT_CANCELLED)
                {
                    return FolderPickerResult.Cancelled();
                }

                if (result != 0)
                {
                    return FolderPickerResult.Failed(new COMException("IFileOpenDialog.Show failed.", result));
                }

                dialog.GetResult(out shellItem);
                if (shellItem == null)
                {
                    return FolderPickerResult.Failed(new InvalidOperationException("IFileOpenDialog returned no shell item."));
                }

                shellItem.GetDisplayName(SIGDN_FILESYSPATH, out folderPathPointer);
                if (folderPathPointer == IntPtr.Zero)
                {
                    return FolderPickerResult.Failed(new InvalidOperationException("Selected folder path was unavailable."));
                }

                string folderPath = Marshal.PtrToStringUni(folderPathPointer);
                return string.IsNullOrEmpty(folderPath)
                    ? FolderPickerResult.Failed(new InvalidOperationException("Selected folder path was empty."))
                    : FolderPickerResult.Success(folderPath);
            }
            catch (COMException ex) when (ex.ErrorCode == HRESULT_CANCELLED)
            {
                return FolderPickerResult.Cancelled();
            }
            catch (Exception ex)
            {
                return FolderPickerResult.Failed(ex);
            }
            finally
            {
                if (folderPathPointer != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(folderPathPointer);
                }

                ReleaseComObject(shellItem);
                ReleaseComObject(dialog);
            }
        }

        private static void LogError(string message)
        {
            if (PotatoPlugin.Log != null)
            {
                PotatoPlugin.Log.LogError(message);
            }
        }

        private static void LogWarning(string message)
        {
            if (PotatoPlugin.Log != null)
            {
                PotatoPlugin.Log.LogWarning(message);
            }
        }

        private static void ReleaseComObject(object comObject)
        {
            if (comObject != null && Marshal.IsComObject(comObject))
            {
                Marshal.FinalReleaseComObject(comObject);
            }
        }

        internal sealed class FolderPickerResult
        {
            public bool Succeeded { get; private set; }
            public bool WasCancelled { get; private set; }
            public string FolderPath { get; private set; }
            public Exception Error { get; private set; }

            public static FolderPickerResult Success(string folderPath)
            {
                return new FolderPickerResult { Succeeded = true, FolderPath = folderPath };
            }

            public static FolderPickerResult Cancelled()
            {
                return new FolderPickerResult { WasCancelled = true };
            }

            public static FolderPickerResult Failed(Exception error)
            {
                return new FolderPickerResult { Error = error ?? new InvalidOperationException("Unknown folder picker failure.") };
            }
        }
    }

    [ComImport]
    [Guid("42f85136-db7e-439c-85f1-e4075d135fc8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IFileDialog
    {
        [PreserveSig]
        int Show(IntPtr parent);

        void SetFileTypes(uint cFileTypes, IntPtr rgFilterSpec);
        void SetFileTypeIndex(uint iFileType);
        void GetFileTypeIndex(out uint piFileType);
        void Advise(IntPtr pfde, out uint pdwCookie);
        void Unadvise(uint dwCookie);
        void SetOptions(uint fos);
        void GetOptions(out uint pfos);
        void SetDefaultFolder(IShellItem psi);
        void SetFolder(IShellItem psi);
        void GetFolder(out IShellItem ppsi);
        void GetCurrentSelection(out IShellItem ppsi);
        void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
        void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
        void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
        void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
        void GetResult(out IShellItem ppsi);
    }

    [ComImport]
    [Guid("D57C7288-D4AD-4768-BE02-9D969532D960")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IFileOpenDialog : IFileDialog
    {
    }

    [ComImport]
    [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IShellItem
    {
        void BindToHandler(IntPtr pbc, [MarshalAs(UnmanagedType.LPStruct)] Guid bhid, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IntPtr ppv);
        void GetParent(out IShellItem ppsi);
        void GetDisplayName(uint sigdnName, out IntPtr ppszName);
        void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
        void Compare(IShellItem psi, uint hint, out int piOrder);
    }
}
