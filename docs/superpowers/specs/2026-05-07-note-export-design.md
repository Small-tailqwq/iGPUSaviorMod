# 笔记导出功能设计文档

**日期**: 2026-05-07
**状态**: 已批准（修订版）
**作者**: AI Assistant

## 概述

为 iGPU Savior MOD 添加笔记导出功能，允许用户选择笔记并批量导出为 txt 文件到指定文件夹。

## 目标

- 在笔记 UI 中添加"导出"按钮
- 支持多选笔记
- 选择目标文件夹，批量导出为 txt 文件
- 文件名格式：`{清理后标题}_{时间戳}.txt`（天然防冲突）
- 仅支持 Windows 10+ 平台

## 架构

### 新增文件

```
iGPU Savior/
├── Features/NoteExportManager.cs    # 导出业务逻辑 + 文件夹对话框
└── Patches/NoteExportPatch.cs       # Harmony 补丁（UI 注入）
```

### 修改文件

```
iGPU Savior/
└── Core/PotatoPlugin.cs             # 显式加载补丁
```

## 组件设计

### NoteExportManager

负责导出业务逻辑，包括：
- 管理选择状态（哪些笔记被选中）
- 处理文件夹对话框（IFileOpenDialog COM API）
- 执行批量导出操作

```csharp
public class NoteExportManager
{
    // 状态
    private bool _isSelectionMode = false;
    private HashSet<ulong> _selectedPageIds = new HashSet<ulong>();
    
    // 事件
    public event Action<bool> OnSelectionModeChanged;
    public event Action<ulong, bool> OnSelectionChanged;
    
    // 方法
    public void EnterSelectionMode();
    public void ExitSelectionMode();
    public void ToggleSelection(ulong pageId);
    public bool IsSelected(ulong pageId);
    public void ExportSelected();  // 弹出文件夹选择器，批量导出
}
```

### NoteExportPatch

Harmony 补丁，负责 UI 注入：
- 补丁 `NoteUI.Setup()` 注入导出按钮
- 补丁 `SelectPageUI.Setup()` 添加复选框
- 监听 `NoteExportManager` 事件更新 UI

### 文件夹对话框

使用 Vista+ COM API `IFileOpenDialog` with `FOS_PICKFOLDERS`：

```csharp
[ComImport, Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
private interface IFileOpenDialog
{
    // IModalWindow
    void Show(IntPtr hwndOwner);
    
    // IFileDialog
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
    
    // IFileOpenDialog
    void GetResults(out IShellItemArray ppenum);
    void GetSelectedItems(out IShellItemArray ppsai);
}

[Flags]
private enum FOS
{
    FOS_PICKFOLDERS = 0x00000020,
    FOS_FORCEFILESYSTEM = 0x00000040,
    FOS_NOVALIDATE = 0x00000100,
    FOS_NOTESTFILECREATE = 0x00010000,
    FOS_DONTADDTORECENT = 0x02000000,
}

[ComImport, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
private interface IShellItem
{
    void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);
    void GetParent(out IShellItem ppsi);
    void GetDisplayName(uint sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
    // ...
}

[ComImport, Guid("b63ea76d-1f85-456f-a19c-48159efa858b")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
private interface IShellItemArray
{
    void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);
    void GetPropertyStore(int flags, ref Guid riid, out IntPtr ppv);
    void GetPropertyDescriptionList(ref KeyType keyType, ref Guid riid, out IntPtr ppv);
    void GetAttributes(int AttribFlags, int sfgaoMask, out int psfgao);
    void GetCount(out uint pdwNumItems);
    void GetItemAt(uint dwIndex, out IShellItem ppsi);
    void EnumItems(out IntPtr ppenumShellItems);
}

private static class Shell32
{
    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    public static extern void SHCreateItemFromParsingName(
        [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
        IntPtr pbc, ref Guid riid, out IShellItem ppv);
    
    public static readonly Guid SID_IFileOpenDialog = new Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7");
    public static readonly Guid CLSID_FileOpenDialog = new Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7");
}
```

**获取窗口句柄**：
```csharp
IntPtr hwnd = WindowManager.GetCurrentWindowHandle();  // 复用现有 Win32 工具类
```

### 文件名处理

```csharp
string SanitizeFileName(string title)
{
    // 删除 Windows 文件名禁止字符
    char[] invalid = Path.GetInvalidFileNameChars();
    string sanitized = new string(title.Where(c => !invalid.Contains(c)).ToArray());
    
    // 截断过长文件名（保留 50 字符 + 时间戳）
    if (sanitized.Length > 50)
        sanitized = sanitized.Substring(0, 50);
    
    return sanitized.Trim();
}

string GenerateExportFileName(string title)
{
    string sanitized = SanitizeFileName(title);
    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
    
    // 兜底：清理后为空，用 note_ 前缀
    if (string.IsNullOrWhiteSpace(sanitized))
        sanitized = "note";
    
    return $"{sanitized}_{timestamp}.txt";
}
```

### 导出格式

```
笔记标题原文

正文内容...
```

- 第一行：标题原文（不经过清理，保留原始 Unicode）
- 第二行：空行
- 第三行起：正文内容
- 编码：UTF-8 with BOM（`new UTF8Encoding(true)`）

```csharp
void ExportPage(string folderPath, ulong pageId, NoteService noteService)
{
    string title = noteService.GetPageTitle(pageId);
    var pageData = noteService.GetPageSaveData(pageId);
    string body = pageData?.MainText ?? "";
    
    string fileName = GenerateExportFileName(title);
    string filePath = Path.Combine(folderPath, fileName);
    
    string content = $"{title}\r\n\r\n{body}";
    File.WriteAllText(filePath, content, new UTF8Encoding(true));
}
```

## UI 交互流程

### 正常模式

```
┌─────────────────────────────────────┐
│ [新页面] [导出]                [×] │
├─────────────────────────────────────┤
│ 标题编辑区                          │
├─────────────────────────────────────┤
│ 正文编辑区                          │
├─────────────────────────────────────┤
│ ⋮ 笔记标题1                    [🗑] │
│ ⋮ 笔记标题2                    [🗑] │
│ ⋮ 笔记标题3                    [🗑] │
└─────────────────────────────────────┘
```

### 选择模式（点击导出后）

```
┌─────────────────────────────────────┐
│ [取消] [确认导出(2)]          [×] │
├─────────────────────────────────────┤
│ 标题编辑区（禁用）                  │
├─────────────────────────────────────┤
│ 正文编辑区（禁用）                  │
├─────────────────────────────────────┤
│ ☐ 笔记标题1                         │
│ ☑ 笔记标题2                         │
│ ☑ 笔记标题3                         │
└─────────────────────────────────────┘
```

### 导出完成反馈

导出完成后，在 MOD 设置区域显示简短提示（复用游戏的 TooltipService 或临时文本）：
- 成功：`导出完成：3 条笔记已保存到 [文件夹路径]`
- 失败：`导出失败：[错误信息]`

## 状态机

```
Normal → (点击导出) → Selecting → (点击取消) → Normal
                ↓
        (点击确认导出)
                ↓
        (弹出文件夹选择器)
                ↓
        (用户选择文件夹)
                ↓
        (批量写入 txt 文件)
                ↓
        (显示完成提示)
                ↓
            Normal
```

## 错误处理

| 场景 | 处理 |
|------|------|
| 文件夹选择器取消 | 静默返回选择模式，不报错 |
| 文件夹选择器 COM 初始化失败 | 回退到 Environment.GetFolderPath(Desktop) |
| 单个文件写入失败 | 记录错误日志，继续导出其他文件 |
| 标题为空 | 使用 "note" 前缀 + 时间戳 |
| 所有字符都是非法字符 | 使用 "note_{timestamp}.txt" |
| 文件名冲突 | 时间戳精确到秒，天然防冲突；如仍冲突，追加序号 |
| NoteService 返回 null | 跳过该笔记，记录警告 |

## 多语言支持

UI 字符串通过 `ModTranslationManager` 注册：

```csharp
Add("NOTE_EXPORT_BUTTON", "Export", "エクスポート", "导出");
Add("NOTE_EXPORT_CONFIRM", "Confirm Export ({0})", "エクスポート確認 ({0})", "确认导出({0})");
Add("NOTE_EXPORT_CANCEL", "Cancel", "キャンセル", "取消");
Add("NOTE_EXPORT_SUCCESS", "Exported: {0} notes to {1}", "エクスポート完了: {0}件 → {1}", "导出完成：{0} 条笔记已保存到 {1}");
Add("NOTE_EXPORT_FAIL", "Export failed: {0}", "エクスポート失敗: {0}", "导出失败：{0}");
```

## 测试要点

- [ ] 导出按钮正常显示（正常模式下）
- [ ] 点击导出进入选择模式，按钮变为"取消"和"确认导出(N)"
- [ ] 复选框正常切换，计数更新
- [ ] 点击取消返回正常模式
- [ ] 确认导出弹出文件夹选择器
- [ ] 选择文件夹后批量导出成功
- [ ] 导出的 txt 文件名格式正确：`{标题}_{时间戳}.txt`
- [ ] 导出的 txt 内容正确（标题原文 + 空行 + 正文）
- [ ] 导出的 txt 编码为 UTF-8 with BOM
- [ ] 取消文件夹选择器不报错
- [ ] 特殊字符标题正确清理
- [ ] 空标题使用 "note_{timestamp}.txt" 兜底
- [ ] 导出完成显示成功提示
- [ ] 多语言环境下 UI 文本正确显示
