using System;
using System.IO;

namespace DescriptIon
{
    /// <summary>
    /// 提供对 <see cref="DescriptionStore"/> 的静态便捷访问方法。
    /// 这些方法适用于简单场景，每次调用都会完整执行 Load → Modify → Save 流程。
    /// 对于高频或批量操作，请直接使用 <see cref="DescriptionStore"/> 实例以提升性能。
    /// </summary>
    public static class DescriptionHelper
    {
        /// <summary>
        /// 获取指定目录下某文件的注释内容。
        /// 自动加载 <c>descript.ion</c> 文件（若存在），并返回注释（使用标准 <c>\n</c> 换行）。
        /// </summary>
        /// <param name="directoryPath">包含 <c>descript.ion</c> 的目录路径。</param>
        /// <param name="fileName">目标文件名（不区分大小写）。</param>
        /// <returns>注释字符串，或 <see langword="null"/>（如果无注释或文件不存在）。</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="directoryPath"/> 或 <paramref name="fileName"/> 为 <see langword="null"/>。
        /// </exception>
        /// <exception cref="IOException">读取文件时发生 I/O 错误。</exception>
        /// <exception cref="UnauthorizedAccessException">无权访问目录或文件。</exception>
        public static string? GetComment(string directoryPath, string fileName)
        {
            if (directoryPath == null) throw new ArgumentNullException(nameof(directoryPath));
            if (fileName == null) throw new ArgumentNullException(nameof(fileName));

            DescriptionStore store = new DescriptionStore(directoryPath, CommentFormat.AutoDetect);
            store.Load();
            return store.GetComment(fileName);
        }

        /// <summary>
        /// 获取指定文件的注释内容。
        /// 该方法从文件所在目录自动加载 <c>descript.ion</c> 文件。
        /// </summary>
        /// <param name="fullPath">目标文件的完整路径。</param>
        /// <returns>注释字符串，或 <see langword="null"/>（如果无注释或文件不存在）。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="fullPath"/> 为 <see langword="null"/>。</exception>
        /// <exception cref="ArgumentException"><paramref name="fullPath"/> 不是有效路径。</exception>
        /// <exception cref="IOException">读取文件时发生 I/O 错误。</exception>
        /// <exception cref="UnauthorizedAccessException">无权访问目录或文件。</exception>
        public static string? GetComment(string fullPath)
        {
            var (dir, name) = ParsePath(fullPath);
            return GetComment(dir, name);
        }

        /// <summary>
        /// 获取指定文件的注释内容。
        /// 该方法从 <see cref="FileInfo"/> 对象所在目录自动加载 <c>descript.ion</c> 文件。
        /// </summary>
        /// <param name="item">表示目标文件的 <see cref="FileSystemInfo"/> 对象。</param>
        /// <returns>注释字符串，或 <see langword="null"/>（如果无注释或文件不存在）。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="item"/> 为 <see langword="null"/>。</exception>
        /// <exception cref="IOException">读取文件时发生 I/O 错误。</exception>
        /// <exception cref="UnauthorizedAccessException">无权访问目录或文件。</exception>
        public static string? GetComment(FileSystemInfo item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            return GetComment(item.FullName);
        }

        /// <summary>
        /// 设置指定目录下某文件的注释内容。
        /// 自动加载现有 <c>descript.ion</c>（若存在），更新注释，并保存回磁盘。
        /// 注释中的换行符应使用标准 <c>\n</c>。
        /// </summary>
        /// <param name="directoryPath">包含 <c>descript.ion</c> 的目录路径。</param>
        /// <param name="fileName">目标文件名（不区分大小写）。</param>
        /// <param name="comment">注释内容（可为 <see langword="null"/> 表示删除）。</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="directoryPath"/> 或 <paramref name="fileName"/> 为 <see langword="null"/>。
        /// </exception>
        /// <exception cref="IOException">读取或写入文件时发生 I/O 错误。</exception>
        /// <exception cref="UnauthorizedAccessException">无权访问目录或文件。</exception>
        public static void SetComment(string directoryPath, string fileName, string? comment)
        {
            if (directoryPath == null) throw new ArgumentNullException(nameof(directoryPath));
            if (fileName == null) throw new ArgumentNullException(nameof(fileName));

            DescriptionStore store = new DescriptionStore(directoryPath, CommentFormat.AutoDetect);
            store.Load();

            if (comment == null)
                store.RemoveComment(fileName);
            else
                store.SetComment(fileName, comment);

            store.Save();
        }

        /// <summary>
        /// 设置指定文件的注释内容。
        /// 该方法会自动在其所在目录的 <c>descript.ion</c> 文件中更新注释。
        /// </summary>
        /// <param name="fullPath">目标文件的完整路径。</param>
        /// <param name="comment">注释内容（可为 <see langword="null"/> 表示删除）。</param>
        /// <exception cref="ArgumentNullException"><paramref name="fullPath"/> 为 <see langword="null"/>。</exception>
        /// <exception cref="ArgumentException"><paramref name="fullPath"/> 不是有效路径。</exception>
        /// <exception cref="IOException">读取或写入文件时发生 I/O 错误。</exception>
        /// <exception cref="UnauthorizedAccessException">无权访问目录或文件。</exception>
        public static void SetComment(string fullPath, string? comment)
        {
            var (dir, name) = ParsePath(fullPath);
            SetComment(dir, name, comment);
        }

        /// <summary>
        /// 设置指定文件的注释内容。
        /// 该方法会自动在其所在目录的 <c>descript.ion</c> 文件中更新注释。
        /// </summary>
        /// <param name="item">表示目标文件的 <see cref="FileSystemInfo"/> 对象。</param>
        /// <param name="comment">注释内容（可为 <see langword="null"/> 表示删除）。</param>
        /// <exception cref="ArgumentNullException"><paramref name="item"/> 为 <see langword="null"/>。</exception>
        /// <exception cref="IOException">读取或写入文件时发生 I/O 错误。</exception>
        /// <exception cref="UnauthorizedAccessException">无权访问目录或文件。</exception>
        public static void SetComment(FileSystemInfo item, string? comment)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            SetComment(item.FullName, comment);
        }

        /// <summary>
        /// 从完整路径中提取其所在目录和名称（支持文件或文件夹，允许末尾分隔符）。
        /// </summary>
        /// <param name="fullPath">目标文件或文件夹的完整路径。</param>
        /// <returns>元组 (directory, name)。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="fullPath"/> 为 null。</exception>
        /// <exception cref="ArgumentException">路径无效或无法解析。</exception>
        private static (string directory, string name) ParsePath(string fullPath)
        {
            if (fullPath == null) throw new ArgumentNullException(nameof(fullPath));
            if (!Path.IsPathRooted(fullPath))
                throw new ArgumentException("路径必须是绝对路径。", nameof(fullPath));

            // 移除末尾的目录分隔符（兼容 "C:\Folder\"）
            string normalized = fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            string? directory = Path.GetDirectoryName(normalized);
            if (directory == null)
                throw new ArgumentException("无法确定路径的父目录（例如根目录）。", nameof(fullPath));

            string name = Path.GetFileName(normalized);
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("路径无效：无法提取目标名称。", nameof(fullPath));

            return (directory, name);
        }
    }
}
