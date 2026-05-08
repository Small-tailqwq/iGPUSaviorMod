# Note Export Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add Windows note export to the in-game note UI so users can multi-select notes, choose a target folder once, and export each note as its own UTF-8 BOM `.txt` file.

**Architecture:** Keep export behavior split into three units: a pure helper for file naming/content formatting, a feature manager for selection/export orchestration, and a Harmony patch for UI injection. Reuse the game's `NoteService` for note data and `WindowManager` for the owner window handle; keep Win32/COM folder picker isolated behind a small wrapper.

**Tech Stack:** C# (.NET Framework 4.7.2), BepInEx, Harmony, Unity UI, Win32 COM interop (`IFileOpenDialog`), xUnit test project for pure logic.

---

## File Map

**Create:**
- `iGPU Savior/Utilities/NoteExportNaming.cs`
  - Pure helper for file name sanitization, file name generation, and export text formatting.
- `iGPU Savior/Features/NoteExportManager.cs`
  - Selection mode state, selected note IDs, export orchestration, success/failure feedback hooks.
- `iGPU Savior/Features/NoteExportFolderPicker.cs`
  - Windows-only `IFileOpenDialog` folder picker wrapper.
- `iGPU Savior/Patches/NoteExportPatch.cs`
  - Harmony UI injection for `NoteUI` and `SelectPageUI`.
- `iGPU Savior.Tests/iGPU Savior.Tests.csproj`
  - Minimal xUnit project for pure helper and selection-state tests.
- `iGPU Savior.Tests/NoteExportNamingTests.cs`
  - Unit tests for invalid character stripping, fallback names, timestamp suffixes, and content formatting.
- `iGPU Savior.Tests/NoteExportManagerStateTests.cs`
  - Unit tests for selection mode state and selected ID toggling.

**Modify:**
- `iGPU Savior/iGPU Savior.csproj`
  - Compile new files and keep shipping assembly references intact.
- `iGPU Savior/Core/PotatoPlugin.cs`
  - Explicitly load `NoteExportPatch` before `PatchAll()`.
- `iGPU Savior/UI/Localization/ModTranslationManager.cs`
  - Add export-related translation keys.

**Manual Verification Targets:**
- Game note UI (`NoteUI`, `SelectPageListUI`, `SelectPageUI`) in the running game.

---

### Task 1: Add testable naming/content helpers

**Files:**
- Create: `iGPU Savior/Utilities/NoteExportNaming.cs`
- Create: `iGPU Savior.Tests/iGPU Savior.Tests.csproj`
- Create: `iGPU Savior.Tests/NoteExportNamingTests.cs`
- Modify: `iGPU Savior/iGPU Savior.csproj`

- [ ] **Step 1: Create the failing test project file**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <Nullable>disable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.10.0" />
    <PackageReference Include="xunit" Version="2.8.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
  </ItemGroup>

  <ItemGroup>
    <Compile Include="..\iGPU Savior\Utilities\NoteExportNaming.cs" Link="NoteExportNaming.cs" />
    <Compile Include="NoteExportNamingTests.cs" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Create failing tests for file name generation and export text formatting**

```csharp
using System.Text.RegularExpressions;
using PotatoOptimization.Utilities;
using Xunit;

public class NoteExportNamingTests
{
    [Fact]
    public void GenerateExportFileName_RemovesInvalidCharacters()
    {
        string fileName = NoteExportNaming.GenerateExportFileName("ab:c/<>d");
        Assert.Matches(@"^abcd_\d{8}_\d{6}\.txt$", fileName);
    }

    [Fact]
    public void GenerateExportFileName_UsesNoteFallbackWhenTitleBecomesEmpty()
    {
        string fileName = NoteExportNaming.GenerateExportFileName("<>:\"/\\|?*");
        Assert.Matches(@"^note_\d{8}_\d{6}\.txt$", fileName);
    }

    [Fact]
    public void FormatExportContent_IncludesTitleBlankLineAndBody()
    {
        string content = NoteExportNaming.FormatExportContent("标题原文", "正文内容");
        Assert.Equal("标题原文\r\n\r\n正文内容", content);
    }

    [Fact]
    public void FormatExportContent_AllowsEmptyBody()
    {
        string content = NoteExportNaming.FormatExportContent("标题原文", "");
        Assert.Equal("标题原文\r\n\r\n", content);
    }
}
```

- [ ] **Step 3: Run the tests to verify they fail**

Run:

```powershell
dotnet test "iGPU Savior.Tests\iGPU Savior.Tests.csproj" --filter "FullyQualifiedName~NoteExportNamingTests"
```

Expected:
- FAIL
- Errors that `PotatoOptimization.Utilities.NoteExportNaming` does not exist yet

- [ ] **Step 4: Add the helper implementation**

```csharp
using System;
using System.IO;
using System.Linq;

namespace PotatoOptimization.Utilities
{
    public static class NoteExportNaming
    {
        public static string SanitizeTitle(string title)
        {
            if (title == null)
                return string.Empty;

            char[] invalid = Path.GetInvalidFileNameChars();
            string sanitized = new string(title.Where(c => !invalid.Contains(c)).ToArray()).Trim();

            if (sanitized.Length > 50)
                sanitized = sanitized.Substring(0, 50).Trim();

            return sanitized;
        }

        public static string GenerateExportFileName(string title)
        {
            string sanitized = SanitizeTitle(title);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            if (string.IsNullOrWhiteSpace(sanitized))
                sanitized = "note";

            return $"{sanitized}_{timestamp}.txt";
        }

        public static string FormatExportContent(string originalTitle, string body)
        {
            string safeTitle = originalTitle ?? string.Empty;
            string safeBody = body ?? string.Empty;
            return safeTitle + "\r\n\r\n" + safeBody;
        }
    }
}
```

- [ ] **Step 5: Include the helper in the shipping project**

Add this line under the Utilities compile section in `iGPU Savior/iGPU Savior.csproj`:

```xml
<Compile Include="Utilities\NoteExportNaming.cs" />
```

- [ ] **Step 6: Run the tests again to verify they pass**

Run:

```powershell
dotnet test "iGPU Savior.Tests\iGPU Savior.Tests.csproj" --filter "FullyQualifiedName~NoteExportNamingTests"
```

Expected:
- PASS
- 4 tests passed

- [ ] **Step 7: Commit**

```bash
git add "iGPU Savior/Utilities/NoteExportNaming.cs" "iGPU Savior/iGPU Savior.csproj" "iGPU Savior.Tests/iGPU Savior.Tests.csproj" "iGPU Savior.Tests/NoteExportNamingTests.cs"
git commit -m "test: add note export naming helpers"
```

### Task 2: Add selection state manager

**Files:**
- Create: `iGPU Savior/Features/NoteExportManager.cs`
- Create: `iGPU Savior.Tests/NoteExportManagerStateTests.cs`
- Modify: `iGPU Savior/iGPU Savior.csproj`
- Modify: `iGPU Savior.Tests/iGPU Savior.Tests.csproj`

- [ ] **Step 1: Create failing tests for selection mode state**

```csharp
using System.Collections.Generic;
using PotatoOptimization.Features;
using Xunit;

public class NoteExportManagerStateTests
{
    [Fact]
    public void EnterSelectionMode_SetsFlagAndClearsSelection()
    {
        var manager = new NoteExportManager(null, null);
        manager.ToggleSelection(10UL);

        manager.EnterSelectionMode();

        Assert.True(manager.IsSelectionMode);
        Assert.Empty(manager.SelectedPageIds);
    }

    [Fact]
    public void ToggleSelection_AddsAndRemovesIds()
    {
        var manager = new NoteExportManager(null, null);
        manager.EnterSelectionMode();

        manager.ToggleSelection(42UL);
        Assert.Contains(42UL, manager.SelectedPageIds);

        manager.ToggleSelection(42UL);
        Assert.DoesNotContain(42UL, manager.SelectedPageIds);
    }

    [Fact]
    public void ExitSelectionMode_ResetsModeAndSelection()
    {
        var manager = new NoteExportManager(null, null);
        manager.EnterSelectionMode();
        manager.ToggleSelection(1UL);

        manager.ExitSelectionMode();

        Assert.False(manager.IsSelectionMode);
        Assert.Empty(manager.SelectedPageIds);
    }
}
```

- [ ] **Step 2: Include the test file in the test project**

Add this line to `iGPU Savior.Tests/iGPU Savior.Tests.csproj`:

```xml
<Compile Include="NoteExportManagerStateTests.cs" />
```

- [ ] **Step 3: Run the state tests to verify they fail**

Run:

```powershell
dotnet test "iGPU Savior.Tests\iGPU Savior.Tests.csproj" --filter "FullyQualifiedName~NoteExportManagerStateTests"
```

Expected:
- FAIL
- `PotatoOptimization.Features.NoteExportManager` does not exist yet

- [ ] **Step 4: Create the minimal manager implementation**

```csharp
using System;
using System.Collections.Generic;

namespace PotatoOptimization.Features
{
    public class NoteExportManager
    {
        private readonly object _noteService;
        private readonly object _folderPicker;
        private readonly HashSet<ulong> _selectedPageIds = new HashSet<ulong>();

        public bool IsSelectionMode { get; private set; }
        public IReadOnlyCollection<ulong> SelectedPageIds => _selectedPageIds;

        public event Action<bool> OnSelectionModeChanged;
        public event Action<ulong, bool> OnSelectionChanged;

        public NoteExportManager(object noteService, object folderPicker)
        {
            _noteService = noteService;
            _folderPicker = folderPicker;
        }

        public void EnterSelectionMode()
        {
            _selectedPageIds.Clear();
            IsSelectionMode = true;
            OnSelectionModeChanged?.Invoke(true);
        }

        public void ExitSelectionMode()
        {
            _selectedPageIds.Clear();
            IsSelectionMode = false;
            OnSelectionModeChanged?.Invoke(false);
        }

        public void ToggleSelection(ulong pageId)
        {
            bool isSelected;
            if (_selectedPageIds.Contains(pageId))
            {
                _selectedPageIds.Remove(pageId);
                isSelected = false;
            }
            else
            {
                _selectedPageIds.Add(pageId);
                isSelected = true;
            }

            OnSelectionChanged?.Invoke(pageId, isSelected);
        }

        public bool IsSelected(ulong pageId)
        {
            return _selectedPageIds.Contains(pageId);
        }
    }
}
```

- [ ] **Step 5: Include the manager in the shipping and test projects**

Add to `iGPU Savior/iGPU Savior.csproj` under Features:

```xml
<Compile Include="Features\NoteExportManager.cs" />
```

Add to `iGPU Savior.Tests/iGPU Savior.Tests.csproj`:

```xml
<Compile Include="..\iGPU Savior\Features\NoteExportManager.cs" Link="NoteExportManager.cs" />
```

- [ ] **Step 6: Run the state tests again to verify they pass**

Run:

```powershell
dotnet test "iGPU Savior.Tests\iGPU Savior.Tests.csproj" --filter "FullyQualifiedName~NoteExportManagerStateTests"
```

Expected:
- PASS
- 3 tests passed

- [ ] **Step 7: Commit**

```bash
git add "iGPU Savior/Features/NoteExportManager.cs" "iGPU Savior/iGPU Savior.csproj" "iGPU Savior.Tests/iGPU Savior.Tests.csproj" "iGPU Savior.Tests/NoteExportManagerStateTests.cs"
git commit -m "feat: add note export selection state"
```

### Task 3: Add Windows folder picker wrapper

**Files:**
- Create: `iGPU Savior/Features/NoteExportFolderPicker.cs`
- Modify: `iGPU Savior/iGPU Savior.csproj`

- [ ] **Step 1: Create the folder picker wrapper**

```csharp
using System;
using System.Runtime.InteropServices;
using PotatoOptimization.Utilities;

namespace PotatoOptimization.Features
{
    public class NoteExportFolderPicker
    {
        public bool TryPickFolder(out string folderPath)
        {
            folderPath = null;

            try
            {
                Type dialogType = Type.GetTypeFromCLSID(new Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7"));
                if (dialogType == null)
                    return false;

                var dialog = (IFileOpenDialog)Activator.CreateInstance(dialogType);
                dialog.SetOptions(FOS.FOS_PICKFOLDERS | FOS.FOS_FORCEFILESYSTEM | FOS.FOS_DONTADDTORECENT);
                dialog.SetTitle("Select export folder");
                dialog.SetOkButtonLabel("Export Here");
                dialog.Show(WindowManager.GetCurrentWindowHandle());

                dialog.GetResult(out IShellItem item);
                item.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out folderPath);
                return !string.IsNullOrWhiteSpace(folderPath);
            }
            catch
            {
                folderPath = null;
                return false;
            }
        }
    }
}
```

- [ ] **Step 2: Add the COM interop declarations to the same file**

```csharp
[Flags]
internal enum FOS : uint
{
    FOS_PICKFOLDERS = 0x00000020,
    FOS_FORCEFILESYSTEM = 0x00000040,
    FOS_DONTADDTORECENT = 0x02000000,
}

internal enum SIGDN : uint
{
    SIGDN_FILESYSPATH = 0x80058000,
}

[ComImport, Guid("d57c7288-d4ad-4768-be02-9d969532d960"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IFileOpenDialog
{
    [PreserveSig] int Show(IntPtr parent);
    void SetFileTypes(uint cFileTypes, IntPtr rgFilterSpec);
    void SetFileTypeIndex(uint iFileType);
    void GetFileTypeIndex(out uint piFileType);
    void Advise(IntPtr pfde, out uint pdwCookie);
    void Unadvise(uint dwCookie);
    void SetOptions(FOS fos);
    void GetOptions(out FOS pfos);
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
    void AddPlace(IShellItem psi, int fdap);
    void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
    void Close(int hr);
    void SetClientGuid(ref Guid guid);
    void ClearClientData();
    void SetFilter(IntPtr pFilter);
    void GetResults(out IntPtr ppenum);
    void GetSelectedItems(out IntPtr ppsai);
}

[ComImport, Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IShellItem
{
    void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);
    void GetParent(out IShellItem ppsi);
    void GetDisplayName(SIGDN sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
    void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
    void Compare(IShellItem psi, uint hint, out int piOrder);
}
```

- [ ] **Step 3: Include the wrapper in the shipping project**

Add to `iGPU Savior/iGPU Savior.csproj` under Features:

```xml
<Compile Include="Features\NoteExportFolderPicker.cs" />
```

- [ ] **Step 4: Build the mod to verify COM interop compiles**

Run:

```powershell
dotnet msbuild "iGPU Savior\iGPU Savior.csproj" /t:Build /p:Configuration=Release /p:GameDir="D:\SteamLibrary\steamapps\common\Chill with You Lo-Fi Story"
```

Expected:
- BUILD SUCCEEDED

- [ ] **Step 5: Commit**

```bash
git add "iGPU Savior/Features/NoteExportFolderPicker.cs" "iGPU Savior/iGPU Savior.csproj"
git commit -m "feat: add note export folder picker"
```

### Task 4: Complete export orchestration and translations

**Files:**
- Modify: `iGPU Savior/Features/NoteExportManager.cs`
- Modify: `iGPU Savior/UI/Localization/ModTranslationManager.cs`

- [ ] **Step 1: Extend the manager with export orchestration**

Replace `NoteExportManager` with:

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PotatoOptimization.Utilities;

namespace PotatoOptimization.Features
{
    public class NoteExportManager
    {
        private readonly NoteService _noteService;
        private readonly NoteExportFolderPicker _folderPicker;
        private readonly HashSet<ulong> _selectedPageIds = new HashSet<ulong>();

        public bool IsSelectionMode { get; private set; }
        public IReadOnlyCollection<ulong> SelectedPageIds => _selectedPageIds;
        public int SelectedCount => _selectedPageIds.Count;

        public event Action<bool> OnSelectionModeChanged;
        public event Action<ulong, bool> OnSelectionChanged;
        public event Action<string> OnExportSuccess;
        public event Action<string> OnExportFailure;

        public NoteExportManager(NoteService noteService, NoteExportFolderPicker folderPicker)
        {
            _noteService = noteService;
            _folderPicker = folderPicker;
        }

        public void EnterSelectionMode()
        {
            _selectedPageIds.Clear();
            IsSelectionMode = true;
            OnSelectionModeChanged?.Invoke(true);
        }

        public void ExitSelectionMode()
        {
            _selectedPageIds.Clear();
            IsSelectionMode = false;
            OnSelectionModeChanged?.Invoke(false);
        }

        public void ToggleSelection(ulong pageId)
        {
            bool isSelected;
            if (_selectedPageIds.Contains(pageId))
            {
                _selectedPageIds.Remove(pageId);
                isSelected = false;
            }
            else
            {
                _selectedPageIds.Add(pageId);
                isSelected = true;
            }

            OnSelectionChanged?.Invoke(pageId, isSelected);
        }

        public bool IsSelected(ulong pageId)
        {
            return _selectedPageIds.Contains(pageId);
        }

        public void ExportSelected()
        {
            if (_selectedPageIds.Count == 0)
            {
                OnExportFailure?.Invoke("No notes selected.");
                return;
            }

            if (!_folderPicker.TryPickFolder(out string folderPath) || string.IsNullOrWhiteSpace(folderPath))
                return;

            int successCount = 0;

            foreach (ulong pageId in _selectedPageIds)
            {
                try
                {
                    string title = _noteService.GetPageTitle(pageId);
                    var pageData = _noteService.GetPageSaveData(pageId);
                    if (pageData == null)
                    {
                        PotatoOptimization.Core.PotatoPlugin.Log.LogWarning($"[NoteExport] Missing page data: {pageId}");
                        continue;
                    }

                    string fileName = NoteExportNaming.GenerateExportFileName(title);
                    string filePath = Path.Combine(folderPath, fileName);
                    string content = NoteExportNaming.FormatExportContent(title, pageData.MainText);
                    File.WriteAllText(filePath, content, new UTF8Encoding(true));
                    successCount++;
                }
                catch (Exception e)
                {
                    PotatoOptimization.Core.PotatoPlugin.Log.LogError($"[NoteExport] Export failed for {pageId}: {e}");
                }
            }

            ExitSelectionMode();
            OnExportSuccess?.Invoke($"Exported {successCount} notes to {folderPath}");
        }
    }
}
```

- [ ] **Step 2: Add export-related translation keys**

Append these lines to `iGPU Savior/UI/Localization/ModTranslationManager.cs` inside `InitializeTranslations()`:

```csharp
Add("NOTE_EXPORT_BUTTON", "Export", "エクスポート", "导出");
Add("NOTE_EXPORT_CANCEL", "Cancel", "キャンセル", "取消");
Add("NOTE_EXPORT_CONFIRM", "Confirm Export", "エクスポート確認", "确认导出");
Add("NOTE_EXPORT_SUCCESS", "Export complete", "エクスポート完了", "导出完成");
Add("NOTE_EXPORT_FAIL", "Export failed", "エクスポート失敗", "导出失败");
```

- [ ] **Step 3: Build the mod to verify manager integration compiles**

Run:

```powershell
dotnet msbuild "iGPU Savior\iGPU Savior.csproj" /t:Build /p:Configuration=Release /p:GameDir="D:\SteamLibrary\steamapps\common\Chill with You Lo-Fi Story"
```

Expected:
- BUILD SUCCEEDED

- [ ] **Step 4: Commit**

```bash
git add "iGPU Savior/Features/NoteExportManager.cs" "iGPU Savior/UI/Localization/ModTranslationManager.cs"
git commit -m "feat: add note export manager"
```

### Task 5: Inject note export UI with Harmony

**Files:**
- Create: `iGPU Savior/Patches/NoteExportPatch.cs`
- Modify: `iGPU Savior/Core/PotatoPlugin.cs`
- Modify: `iGPU Savior/iGPU Savior.csproj`

- [ ] **Step 1: Create the patch shell and explicit load wiring**

Create `iGPU Savior/Patches/NoteExportPatch.cs`:

```csharp
using HarmonyLib;
using UnityEngine;

namespace PotatoOptimization.Patches
{
    [HarmonyPatch(typeof(NoteUI), "Setup")]
    public static class NoteExportPatch
    {
        private static PotatoOptimization.Features.NoteExportManager _manager;

        static void Postfix(NoteUI __instance)
        {
            if (_manager == null)
            {
                var noteService = RoomLifetimeScope.Resolve<NoteService>();
                _manager = new PotatoOptimization.Features.NoteExportManager(
                    noteService,
                    new PotatoOptimization.Features.NoteExportFolderPicker());
            }

            // UI injection happens in the next step.
        }
    }
}
```

Add to `iGPU Savior/iGPU Savior.csproj` under Patches:

```xml
<Compile Include="Patches\NoteExportPatch.cs" />
```

Add to `iGPU Savior/Core/PotatoPlugin.cs` before `PatchAll()`:

```csharp
var noteExportPatchType = typeof(NoteExportPatch);
Log.LogWarning($"[Patch Init] Loaded NoteExportPatch: {noteExportPatchType.FullName}");
```

- [ ] **Step 2: Inject the export buttons and state switching**

Replace the `Postfix` body with:

```csharp
static void Postfix(NoteUI __instance)
{
    if (_manager == null)
    {
        var noteService = RoomLifetimeScope.Resolve<NoteService>();
        _manager = new PotatoOptimization.Features.NoteExportManager(
            noteService,
            new PotatoOptimization.Features.NoteExportFolderPicker());
    }

    var selectPageListField = typeof(NoteUI).GetField("_selectPageListUI", BindingFlags.Instance | BindingFlags.NonPublic);
    var listUi = selectPageListField?.GetValue(__instance) as SelectPageListUI;
    if (listUi == null)
        return;

    var addButtonField = typeof(SelectPageListUI).GetField("_addSelectPageUIButton", BindingFlags.Instance | BindingFlags.NonPublic);
    var addButton = addButtonField?.GetValue(listUi) as UnityEngine.UI.Button;
    if (addButton == null)
        return;

    RectTransform addRect = addButton.transform as RectTransform;
    addRect.sizeDelta = new Vector2(addRect.sizeDelta.x * 0.7f, addRect.sizeDelta.y);

    var exportButton = Object.Instantiate(addButton, addButton.transform.parent);
    exportButton.name = "NoteExportButton";
    exportButton.onClick.RemoveAllListeners();
    exportButton.onClick.AddListener(() =>
    {
        if (!_manager.IsSelectionMode)
            _manager.EnterSelectionMode();
        else
            _manager.ExportSelected();
    });

    var exportLabel = exportButton.GetComponentInChildren<TMPro.TMP_Text>(true);
    if (exportLabel != null)
        exportLabel.text = PotatoOptimization.UI.ModTranslationManager.Get("NOTE_EXPORT_BUTTON", Bulbul.GameLanguageType.ChineseSimplified);

    _manager.OnSelectionModeChanged += isSelectionMode =>
    {
        if (exportLabel == null)
            return;

        exportLabel.text = isSelectionMode
            ? PotatoOptimization.UI.ModTranslationManager.Get("NOTE_EXPORT_CONFIRM", Bulbul.GameLanguageType.ChineseSimplified)
            : PotatoOptimization.UI.ModTranslationManager.Get("NOTE_EXPORT_BUTTON", Bulbul.GameLanguageType.ChineseSimplified);
    };
}
```

- [ ] **Step 3: Add SelectPageUI checkbox mode patch in the same file**

Append this patch to `NoteExportPatch.cs`:

```csharp
[HarmonyPatch(typeof(SelectPageUI), "Setup")]
public static class SelectPageUIExportPatch
{
    static void Postfix(SelectPageUI __instance)
    {
        // Minimal first version: reuse remove button spot to show [ ]/[x] text when selection mode is active.
        // Follow-up polish can replace the drag handle visually if needed.
    }
}
```

Then update it to the actual minimal selectable behavior:

```csharp
static void Postfix(SelectPageUI __instance)
{
    var titleInputFieldField = typeof(SelectPageUI).GetField("_titleInputField", BindingFlags.Instance | BindingFlags.NonPublic);
    var titleInput = titleInputFieldField?.GetValue(__instance) as TMPro.TMP_InputField;
    if (titleInput == null)
        return;

    GameObject checkboxObj = new GameObject("ExportCheckbox", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
    checkboxObj.transform.SetParent(__instance.transform, false);
    var checkboxText = checkboxObj.GetComponent<TMPro.TextMeshProUGUI>();
    checkboxText.text = "☐";
    checkboxText.fontSize = 28;

    var pageIdProp = typeof(SelectPageUI).GetProperty("PageID", BindingFlags.Instance | BindingFlags.Public);
    ulong pageId = (ulong)pageIdProp.GetValue(__instance);

    checkboxObj.AddComponent<UnityEngine.UI.Button>().onClick.AddListener(() =>
    {
        if (_manager == null || !_manager.IsSelectionMode)
            return;

        _manager.ToggleSelection(pageId);
        checkboxText.text = _manager.IsSelected(pageId) ? "☑" : "☐";
    });

    _manager.OnSelectionModeChanged += isSelectionMode =>
    {
        checkboxObj.SetActive(isSelectionMode);
        if (!isSelectionMode)
            checkboxText.text = "☐";
    };
}
```

- [ ] **Step 4: Build the mod**

Run:

```powershell
dotnet msbuild "iGPU Savior\iGPU Savior.csproj" /t:Build /p:Configuration=Release /p:GameDir="D:\SteamLibrary\steamapps\common\Chill with You Lo-Fi Story"
```

Expected:
- BUILD SUCCEEDED

- [ ] **Step 5: Manual verification in game**

Checklist:
- Open the note UI
- Verify a new `Export` button appears beside `New Page`
- Click `Export` and verify the button changes to `Confirm Export`
- Verify checkbox UI appears on page cards
- Select 2-3 notes and click `Confirm Export`
- Pick a folder and verify multiple `.txt` files are written into that folder
- Open one exported file in Notepad and verify it contains `title`, blank line, `body`
- Verify filenames include sanitized title and timestamp
- Verify canceling the folder picker does not break note UI state

- [ ] **Step 6: Commit**

```bash
git add "iGPU Savior/Patches/NoteExportPatch.cs" "iGPU Savior/Core/PotatoPlugin.cs" "iGPU Savior/iGPU Savior.csproj"
git commit -m "feat: add note export UI"
```

### Task 6: Full regression pass

**Files:**
- Modify: none (verification only unless bugs found)

- [ ] **Step 1: Run pure logic tests**

Run:

```powershell
dotnet test "iGPU Savior.Tests\iGPU Savior.Tests.csproj"
```

Expected:
- PASS
- All note export helper and state tests pass

- [ ] **Step 2: Rebuild the release DLL**

Run:

```powershell
dotnet msbuild "iGPU Savior\iGPU Savior.csproj" /t:Build /p:Configuration=Release /p:GameDir="D:\SteamLibrary\steamapps\common\Chill with You Lo-Fi Story" /p:BaseIntermediateOutputPath="obj_ci\" /p:IntermediateOutputPath="obj_ci\Release\" /p:OutDir="artifacts\build\"
```

Expected:
- BUILD SUCCEEDED
- `iGPU Savior\artifacts\build\iGPU Savior.dll` exists

- [ ] **Step 3: Manual smoke regression**

Checklist:
- Mirror mode still works (`F4`)
- Note delete confirm still appears
- Note reorder still works when not in export mode
- Hovering note cards still only shows delete button in normal mode
- Entering and exiting export mode does not break note editing

- [ ] **Step 4: Commit**

```bash
git add .
git commit -m "test: verify note export feature"
```

## Self-Review

**Spec coverage:**
- Folder selection instead of single-file save: covered in Task 3
- Per-note txt files: covered in Task 4 export loop
- Filename sanitization + timestamp fallback: covered in Task 1 helper + Task 4 orchestration
- UTF-8 with BOM: covered in Task 4 `File.WriteAllText(..., new UTF8Encoding(true))`
- Multi-language strings: covered in Task 4 translation keys
- Export feedback: covered in Task 4 manager events + manual verification
- Patch loading: covered in Task 5 `PotatoPlugin`

**Placeholder scan:**
- No TBD/TODO placeholders remain in implementation steps
- Code blocks included for each code-changing step

**Type consistency:**
- `NoteExportNaming.GenerateExportFileName()` and `FormatExportContent()` defined before later tasks use them
- `NoteExportManager` state API defined in Task 2 before Task 5 UI patch uses `_manager.IsSelectionMode` / `_manager.ToggleSelection()`
- `NoteExportFolderPicker.TryPickFolder()` defined before Task 4 manager integration uses it
