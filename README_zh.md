# DescriptIon — 跨平台 `descript.ion` 文件读写库

[![.NET](https://img.shields.io/badge/.NET-Standard%202.0-blue?logo=.net)](https://dotnet.microsoft.com/) [![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

**DescriptIon** 是一个轻量级、跨平台的 C# 库，用于读取、写入和管理 [Total Commander](https://www.ghisler.com/) 和 [Double Commander](https://doublecmd.sourceforge.io/) 使用的 `descript.ion` 文件。它支持多行注释、带空格/引号的文件名、自动格式检测，并提供高级维护功能（如清理无效条目、自然排序等）。

> [!NOTE]
> Mapaler：我是 C# 新手，没能力写健壮的库，所以是用 AI 辅助写的。

---

## ✨ 特性

- ✅ **兼容 Total Commander 与 Double Commander**
  - 自动识别并处理两种不同的多行注释格式
- ✅ **内存中统一使用 `\n` 表示换行**
  - 保存时自动转换为目标格式
- ✅ **保留原始编码与注释风格**
  - 自动检测文件编码（UTF-8 with BOM / ANSI 等）
- ✅ **高级维护功能**
  - 清理无效注释（无对应文件/目录的条目）
  - 按操作系统习惯排序（默认 `CurrentCulture`，可自定义）
- ✅ **零依赖**：仅使用 .NET 标准库

---

## 📦 安装

通过 NuGet 安装包:

```bash
dotnet add package DescriptIon.Core
```

或通过包管理控制台安装:

```powershell
Install-Package DescriptIon.Core
```

> [!TIP]
> **为什么叫 `DescriptIon.Core`？**  
> [`DescriptIon`](https://www.nuget.org/packages/DescriptIon) 包 ID 已被注册。所以我采用 `DescriptIon.Core` 作为包名。

---

## 🚀 快速开始

### 1. 基本读写

```csharp
// 加载 descript.ion
var store = new DescriptionStore(@"C:\MyFolder");
store.Load();

// 获取注释
string? comment = store.GetComment("report.pdf");

// 设置多行注释（使用 \n）
store.SetComment("notes.txt", "第一行\n第二行");

// 保存回文件
store.Save();
```

### 2. 高级维护

```csharp
var store = new DescriptionStore(@"C:\MyFolder");
store.Load();

// 清理已删除文件的注释
store.RemoveOrphanedEntries();

// 按文件名排序（默认使用 CurrentCulture）
store.Sort();

// 或指定排序方式
store.Sort(StringComparer.Ordinal);

store.Save(); // 写入整洁版 descript.ion
```

### 3. 快速辅助方法
这是对完整读写的静态封装，只适用于少量读写。如果使用量较大，建议使用前面所述的完整方法，可以减少磁盘IO。

```csharp
// 设置文件的注释
DescriptionHelper.SetComment(@"C:\Data\image.jpg", "我的照片");
// 读取文件的注释
string? cmt = DescriptionHelper.GetComment(@"C:\Data\Projects\");

// 支持 FileInfo / DirectoryInfo
var file = new FileInfo("log.txt");
DescriptionHelper.SetComment(file, "Application log");
```

---

## 🧩 核心 API

### `DescriptionStore` 类

| 方法 | 说明 |
|------|------|
| `Load()` | 从 `descript.ion` 加载注释（自动检测格式与编码） |
| `Save()` | 保存到 `descript.ion`（保留原始编码，按 `Format` 输出） |
| `GetComment(string fileName)` | 获取注释（不区分大小写） |
| `SetComment(string fileName, string comment)` | 设置注释（`\n` 表示换行） |
| `RemoveComment(string fileName)` | 移除注释 |
| `RemoveOrphanedEntries()` | 删除无对应文件/目录的条目 |
| `Sort(IComparer<string>? comparer = null)` | 排序条目（默认 `StringComparer.CurrentCulture`） |

### `DescriptionHelper` 静态类

| 方法 | 说明 |
|------|------|
| `GetComment(string fullPath)` | 通过完整路径获取注释 |
| `SetComment(string fullPath, string? comment)` | 通过完整路径设置注释 |
| `GetComment(FileSystemInfo item)` | 支持 `FileInfo` / `DirectoryInfo` |
| `SetComment(FileSystemInfo item, string? comment)` | 同上 |

---

## 🔍 格式说明

### Total Commander 格式
- 多行注释用 `\n` 表示（存储为字面 `"\\n"`）
- 行尾附加标记：`EOT (U+0004) + Â (U+00C2)`
- 示例：
  ```text
  "my file.txt" Line 1\\nLine 2\x04Â
  ```

### Double Commander 格式
- 多行注释用 **NO-BREAK SPACE (U+00A0)** 连接
- 无额外标记
- 示例：
  ```text
  folder My folder with two lines
  ```

> 💡 库内部统一使用 `\n`，自动处理格式转换。

## 🧩 第三方软件兼容性

除了 Total Commander 和 Double Commander，其他软件对 `descript.ion` 的支持程度各不相同：

- **📦 7-Zip**  
  - ✅ 支持读取 `descript.ion` 文件以显示文件注释  
  - ⚠️ **仅支持 UTF-8 编码**（带 BOM）  
  - ❌ **不支持多行注释** —— 换行符会被忽略或截断  
  - 💡 建议：若需与 7-Zip 兼容，请使用单行注释并保存为 UTF-8

- **🖼️ XnView / XnViewMP**  
  - ✅ **完全支持 Total Commander 格式**  
  - ✅ 正确解析多行注释（含 `\\n` 和 EOT+Â 标记）  
  - ✅ 支持带空格、引号的文件名  
  - ✅ 自动识别 UTF-8 / ANSI 编码  
  - 💡 是除 Total Commander 外兼容性最好的图像浏览器之一

---

## 🧪 测试覆盖

- 文件名解析（含空格、引号、转义）
- 多行注释（TC / DC 格式）
- 编码检测（UTF-8 BOM / ANSI）
- 无效条目清理
- 排序行为
- 路径末尾斜杠处理

---

## 📜 许可证

MIT License — 免费用于个人和商业项目。

---

## 🙌 致谢

- 受 [Total Commander](https://www.ghisler.com/) 和 [Double Commander](https://doublecmd.sourceforge.io/) 启发

---

> **让 `descript.ion` 在 .NET 世界焕发新生！**  
> 适用于文件管理器插件、备份工具、文档整理脚本等场景。

## 更新日志 (Changelog)

- **v1.0.1** (2025-12-13)
  - 修复：在 Windows 上更新已存在的 `descript.ion` 文件时可能出现 `UnauthorizedAccessException` 的问题。
  - 移除了 `DescriptIonEntry` 类，内部改用 `Dictionary<string, string>` 存储，简化设计并提升性能。
