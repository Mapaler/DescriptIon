using System;
using System.IO;
using System.Reflection.Metadata;
using System.Text;
using Xunit;

namespace DescriptIon.Tests;

public class DescriptIonParsingTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _descriptPath;

    public DescriptIonParsingTests()
    {
        // 创建临时目录用于测试
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _descriptPath = Path.Combine(_tempDir, "descript.ion");
    }

    public void Dispose()
    {
        // 清理临时文件
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void Parse_SimpleFile_CommentLoadedCorrectly()
    {
        File.WriteAllText(_descriptPath, "file.txt This is a comment");

        var store = new DescriptionStore(_tempDir);
        store.Load();

        var comment = store.GetComment("file.txt");
        Assert.Equal("This is a comment", comment);
    }

    [Fact]
    public void Parse_CaseInsensitiveFileName_MatchesRegardlessOfCase()
    {
        File.WriteAllText(_descriptPath, "Report.PDF Annual report 2025");

        var store = new DescriptionStore(_tempDir);
        store.Load();

        Assert.Equal("Annual report 2025", store.GetComment("report.pdf"));
        Assert.Equal("Annual report 2025", store.GetComment("REPORT.PDF"));
    }

    [Fact]
    public void Parse_TotalCommanderFormat_MultiLineDecodedCorrectly()
    {
        // 用真实字节“\n”分隔，并在最后附加 \x04Â
        var content = "file.txt First line\\nSecond line" + "\u0004\u00C2";
        File.WriteAllText(_descriptPath, content, Encoding.UTF8);

        var store = new DescriptionStore(_tempDir, CommentFormat.TotalCommander);
        store.Load();

        var comment = store.GetComment("file.txt");
        Assert.Equal("First line\nSecond line", comment);
    }
    [Fact]
    public void Parse_DoubleCommanderFormat_MultiLineDecodedCorrectly()
    {
        // 使用 NO-BREAK SPACE (\u00A0) 连接
        var content = "folder My folder\u00A0with two lines";
        File.WriteAllText(_descriptPath, content, Encoding.UTF8);

        var store = new DescriptionStore(_tempDir, CommentFormat.DoubleCommander);
        store.Load();

        var comment = store.GetComment("folder");
        Assert.Equal("My folder\nwith two lines", comment);
    }
    [Fact]
    public void Load_AutoDetect_RecognizesTotalCommanderByEOTMarker()
    {
        var content = "test.txt Hello\u0004\u00C2";
        File.WriteAllText(_descriptPath, content, Encoding.UTF8);

        var store = new DescriptionStore(_tempDir, CommentFormat.AutoDetect);
        store.Load();

        Assert.Equal(CommentFormat.TotalCommander, store.Format);
        Assert.Equal("Hello", store.GetComment("test.txt"));
    }

    [Fact]
    public void Helper_GetComment_FilePath_WorksForFile()
    {
        File.WriteAllText(_descriptPath, "image.jpg A photo");

        var fullPath = Path.Combine(_tempDir, "image.jpg");
        var comment = DescriptionHelper.GetComment(fullPath);

        Assert.Equal("A photo", comment);
    }

    [Fact]
    public void Helper_GetComment_FolderPathWithoutSlash_Works()
    {
        File.WriteAllText(_descriptPath, "Projects My project folder");

        var folderPath = Path.Combine(_tempDir, "Projects");
        var comment = DescriptionHelper.GetComment(folderPath);

        Assert.Equal("My project folder", comment);
    }

    [Fact]
    public void Helper_GetComment_FolderPathWithTrailingSlash_Works()
    {
        File.WriteAllText(_descriptPath, "Data Data directory");

        var folderPath = Path.Combine(_tempDir, "Data") + Path.DirectorySeparatorChar;
        var comment = DescriptionHelper.GetComment(folderPath);

        Assert.Equal("Data directory", comment);
    }

    [Fact]
    public void Helper_GetComment_UsingFileSystemInfo_WorksForFileAndFolder()
    {
        File.WriteAllText(_descriptPath, "log.txt Application log\nfolder Subdirectory");

        var fileInfo = new FileInfo(Path.Combine(_tempDir, "log.txt"));
        var dirInfo = new DirectoryInfo(Path.Combine(_tempDir, "folder"));

        Assert.Equal("Application log", DescriptionHelper.GetComment(fileInfo));
        Assert.Equal("Subdirectory", DescriptionHelper.GetComment(dirInfo));
    }

    [Fact]
    public void Helper_SetComment_ForFile_CreatesDescriptIonIfNotExists()
    {
        var filePath = Path.Combine(_tempDir, "test.txt");
        DescriptionHelper.SetComment(filePath, "Test file comment");

        Assert.True(File.Exists(_descriptPath));
        var content = File.ReadAllText(_descriptPath);
        Assert.Contains("test.txt Test file comment", content);
    }

    [Fact]
    public void Helper_SetComment_ForFolder_UpdatesDescriptIonCorrectly()
    {
        var folderPath = Path.Combine(_tempDir, "Backup");
        DescriptionHelper.SetComment(folderPath, "Backup folder\nCreated today");

        var content = File.ReadAllText(_descriptPath);
        Assert.Contains("Backup Backup folder", content);
        Assert.Contains("Created today", content);
    }

    [Fact]
    public void Parse_EmptyOrWhitespaceLines_Ignored()
    {
        var content = @"
file1.txt Comment 1

file2.txt Comment 2
";
        File.WriteAllText(_descriptPath, content.Trim());

        var store = new DescriptionStore(_tempDir);
        store.Load();

        Assert.Equal("Comment 1", store.GetComment("file1.txt"));
        Assert.Equal("Comment 2", store.GetComment("file2.txt"));
    }
    [Fact]
    public void GetComment_ReturnsNull_WhenFileNotInDescriptIon()
    {
        File.WriteAllText(_descriptPath, "README Some comment");

        var store = new DescriptionStore(_tempDir);
        store.Load();

        Assert.Null(store.GetComment("NONEXISTENT.TXT"));
    }
    [Fact]
    public void Parse_LineWithoutSpace_TreatedAsFileNameWithEmptyComment()
    {
        File.WriteAllText(_descriptPath, "README");

        var store = new DescriptionStore(_tempDir);
        store.Load();

        string? comment = store.GetComment("README");
        Assert.NotNull(comment);      // 确保有条目
        Assert.Empty(comment);        // 然后检查是否为空字符串
    }

    [Fact]
    public void RemoveComment_RemovesEntryFromStore()
    {
        File.WriteAllText(_descriptPath, "old.txt To be removed");

        var store = new DescriptionStore(_tempDir);
        store.Load();
        store.RemoveComment("old.txt");
        store.Save();

        var content = File.ReadAllText(_descriptPath);
        Assert.DoesNotContain("old.txt", content);
    }

    [Fact]
    public void Save_Should_Update_Hidden_DescriptIon_File()
    {
        var dir = new DirectoryInfo("TestHidden");
        dir.Create();
        var filePath = Path.Combine(dir.FullName, "descript.ion");

        // 创建一个隐藏的 descript.ion（模拟 TC 行为）
        File.WriteAllText(filePath, "test.txt Hello");
        File.SetAttributes(filePath, File.GetAttributes(filePath) | FileAttributes.Hidden);

        // 加载并修改
        var store = new DescriptionStore(dir.FullName);
        store.Load();
        store.SetComment("test.txt", "Updated!");
        store.Save(); // ← 这里应该不再抛异常

        Assert.True(File.Exists(filePath));
        var content = File.ReadAllText(filePath);
        Assert.Contains("Updated!", content);

        dir.Delete(true);
    }
}