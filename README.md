# DescriptIon — Cross-platform `descript.ion` File Reader/Writer for .NET

[![.NET Standard 2.0](https://img.shields.io/badge/.NET-Standard%202.0-blue?logo=.net)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

**DescriptIon** is a lightweight, cross-platform C# library for reading, writing, and managing `descript.ion` files used by [Total Commander](https://www.ghisler.com/) and [Double Commander](https://doublecmd.sourceforge.io/). It supports multi-line comments, filenames with spaces/quotes, automatic format detection, and advanced maintenance features (e.g., cleaning orphaned entries, natural sorting).

> [!NOTE]  
> Mapaler: I'm new to C#, so this library was developed with AI assistance.
> 🇨🇳 Chinese users: See [README_zh.md](README_zh.md) for the Chinese version.

---

## ✨ Features

- ✅ **Compatible with Total Commander & Double Commander**
  - Automatically detects and handles two different multi-line comment formats
- ✅ **Unified newline handling**
  - Internally uses `\n` for line breaks
  - Automatically converts to target format on save
- ✅ **Preserves original encoding and style**
  - Auto-detects file encoding (UTF-8 with BOM, ANSI, etc.)
- ✅ **Advanced maintenance tools**
  - Remove orphaned entries (comments without corresponding files/directories)
  - Sort entries using OS-aware rules (`CurrentCulture` by default, customizable)
- ✅ **Zero dependencies**: Uses only .NET Standard libraries

---

## 📦 Installation

Install the package via NuGet:

```bash
dotnet add package DescriptIon.Core
```

Or via Package Manager Console:

```powershell
Install-Package DescriptIon.Core
```

> [!TIP]
> **Why `DescriptIon.Core`?**  
> The package ID [`DescriptIon`](https://www.nuget.org/packages/DescriptIon) was used. So I uses the name `DescriptIon.Core`.

---

## 🚀 Quick Start

### 1. Basic Read/Write

```csharp
// Load descript.ion
var store = new DescriptionStore(@"C:\MyFolder");
store.Load();

// Get comment
string? comment = store.GetComment("report.pdf");

// Set multi-line comment (use \n)
store.SetComment("notes.txt", "Line 1\nLine 2");

// Save back to file
store.Save();
```

### 2. Advanced Maintenance

```csharp
var store = new DescriptionStore(@"C:\MyFolder");
store.Load();

// Clean up comments for deleted files
store.RemoveOrphanedEntries();

// Sort by filename (default: CurrentCulture)
store.Sort();

// Or specify comparer
store.Sort(StringComparer.Ordinal);

store.Save(); // Writes a clean descript.ion
```

### 3. Helper Methods (for occasional use)

> For heavy usage, prefer the full `DescriptionStore` approach to reduce disk I/O.

```csharp
// Set comment via full path
DescriptionHelper.SetComment(@"C:\Data\image.jpg", "My photo");

// Get comment
string? cmt = DescriptionHelper.GetComment(@"C:\Data\Projects\");

// Supports FileInfo / DirectoryInfo
var file = new FileInfo("log.txt");
DescriptionHelper.SetComment(file, "Application log");
```

---

## 🧩 Core API

### `DescriptionStore` Class

| Method | Description |
|--------|-------------|
| `Load()` | Loads comments from `descript.ion` (auto-detects format & encoding) |
| `Save()` | Saves to `descript.ion` (preserves original encoding, outputs in detected format) |
| `GetComment(string fileName)` | Gets comment (case-insensitive) |
| `SetComment(string fileName, string comment)` | Sets comment (`\n` for line breaks) |
| `RemoveComment(string fileName)` | Removes comment |
| `RemoveOrphanedEntries()` | Deletes entries with no matching file/directory |
| `Sort(IComparer<string>? comparer = null)` | Sorts entries (default: `StringComparer.CurrentCulture`) |

### `DescriptionHelper` Static Class

| Method | Description |
|--------|-------------|
| `GetComment(string fullPath)` | Gets comment by full path |
| `SetComment(string fullPath, string? comment)` | Sets comment by full path |
| `GetComment(FileSystemInfo item)` | Supports `FileInfo` / `DirectoryInfo` |
| `SetComment(FileSystemInfo item, string? comment)` | Same as above |

---

## 🔍 Format Details

### Total Commander Format
- Multi-line comments use `\n` (stored as literal `"\\n"`)
- Line ends with marker: `EOT (U+0004) + Â (U+00C2)`
- Example:
  ```text
  "my file.txt" Line 1\\nLine 2\x04Â
  ```

### Double Commander Format
- Multi-line comments joined by **NO-BREAK SPACE (U+00A0)**
- No extra markers
- Example:
  ```text
  folder My folder with two lines
  ```

> 💡 The library internally uses `\n` and handles format conversion automatically.

---

## 🧩 Third-Party Compatibility

Beyond Total Commander and Double Commander, support varies:

- **📦 7-Zip**  
  - ✅ Reads `descript.ion` to show file comments  
  - ⚠️ **Only supports UTF-8 encoding** (BOM optional)  
  - ❌ **Does not support multi-line comments** — line breaks are ignored or truncated  
  - 💡 Recommendation: Use single-line comments and UTF-8 for 7-Zip compatibility

- **🖼️ XnView / XnViewMP**  
  - ✅ **Fully supports Total Commander format**  
  - ✅ Correctly parses multi-line comments (including `\\n` and EOT+Â markers)  
  - ✅ Handles filenames with spaces and quotes  
  - ✅ Auto-detects UTF-8 / ANSI encoding  
  - 💡 One of the best-compatible image viewers outside Total Commander

> 📌 Tip: For maximum compatibility across tools, use **Total Commander format + UTF-8 with BOM + single-line comments**.

---

## 🧪 Test Coverage

- Filename parsing (spaces, quotes, escaping)
- Multi-line comments (TC / DC formats)
- Encoding detection (UTF-8 BOM / ANSI)
- Orphaned entry cleanup
- Sorting behavior
- Trailing slash handling in paths

---

## 📜 License

MIT License — free for personal and commercial use.

---

## 🙌 Acknowledgements

Inspired by [Total Commander](https://www.ghisler.com/) and [Double Commander](https://doublecmd.sourceforge.io/).  
Bringing `descript.ion` to life in the .NET world!

> Perfect for file manager plugins, backup tools, document organization scripts, and more.

---

## Changelog

- **v1.0.1** (2025-12-13)
  - Fixed: `UnauthorizedAccessException` when updating an existing `descript.ion` file on Windows.
  - Removed the `DescriptIonEntry` class; now uses `Dictionary<string, string>` internally for simpler design and better performance.
