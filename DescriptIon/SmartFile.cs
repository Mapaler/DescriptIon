using System;
using System.IO;
using System.Text;

namespace DescriptIon
{
    public static class SmartFile
    {
        #region DetectEncoding
        /// <summary>
        /// 检测指定文件的文本编码（基于字节顺序标记 BOM）。
        /// </summary>
        /// <param name="filePath">要检测的文件的完整路径。</param>
        /// <returns>
        /// 检测到的编码：
        /// <list type="bullet">
        ///   <item><description>UTF-8（带 BOM）→ 返回 <see cref="Encoding.UTF8"/></description></item>
        ///   <item><description>UTF-16 LE（带 BOM）→ 返回 <see cref="Encoding.Unicode"/></description></item>
        ///   <item><description>UTF-16 BE（带 BOM）→ 返回 <see cref="Encoding.BigEndianUnicode"/></description></item>
        ///   <item><description>无 BOM 或未知 → 返回 <see cref="Encoding.Default"/>（通常是系统 ANSI 编码，如 GBK）</description></item>
        /// </list>
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="filePath"/> 为 <see langword="null"/>。</exception>
        /// <exception cref="FileNotFoundException">指定的文件不存在。</exception>
        /// <remarks>
        /// 此方法仅检查文件开头的 2~3 字节。若文件小于 3 字节，则返回 <see cref="Encoding.Default"/>。
        /// </remarks>
        public static Encoding DetectEncoding(string filePath)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException($"文件未找到: {filePath}");

            using FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            if (fs.Length < 2) return Encoding.Default;

            byte[] bom = new byte[3];
            int read = fs.Read(bom, 0, (int)Math.Min(3, fs.Length));
            if (read < 2) return Encoding.Default;

            if (bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
                return Encoding.UTF8;
            if (bom[0] == 0xFF && bom[1] == 0xFE)
                return Encoding.Unicode;
            if (bom[0] == 0xFE && bom[1] == 0xFF)
                return Encoding.BigEndianUnicode;

            return Encoding.Default;
        }
        /// <summary>
        /// 检测指定文件的文本编码（基于字节顺序标记 BOM）。
        /// </summary>
        /// <param name="file">表示目标文件的 <see cref="FileInfo"/> 对象。</param>
        /// <returns>检测到的编码（规则同 <see cref="DetectEncoding(string)"/>）。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="file"/> 为 <see langword="null"/>。</exception>
        /// <exception cref="FileNotFoundException">文件不存在。</exception>
        public static Encoding DetectEncoding(FileInfo file)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            return DetectEncoding(file.FullName);
        }
        #endregion

        #region ReadAllText
        /// <summary>
        /// 从指定文件读取所有文本内容，若未提供编码则使用检测到的编码。
        /// </summary>
        /// <param name="filePath">要读取的文件的完整路径。</param>
        /// <param name="encoding">
        /// 要使用的文本编码。若为 <see langword="null"/>，则使用使用 <see cref="DetectEncoding(string)"/> 检测到的编码）。
        /// </param>
        /// <returns>文件的全部文本内容。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="filePath"/> 为 <see langword="null"/>。</exception>
        /// <exception cref="FileNotFoundException">指定的文件不存在。</exception>
        public static string ReadAllText(string filePath, Encoding encoding = null)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException($"文件未找到: {filePath}");
            encoding ??= DetectEncoding(filePath);
            using FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using StreamReader reader = new StreamReader(fs, encoding);
            return reader.ReadToEnd();
        }
        /// <summary>
        /// 从指定文件读取所有文本内容，若未提供编码则使用检测到的编码。
        /// </summary>
        /// <param name="file">表示目标文件的 <see cref="FileInfo"/> 对象。</param>
        /// <param name="encoding">
        /// 要使用的文本编码。若为 <see langword="null"/>，则使用使用 <see cref="DetectEncoding(string)"/> 检测到的编码）。
        /// </param>
        /// <returns>文件的全部文本内容。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="file"/> 为 <see langword="null"/>。</exception>
        /// <exception cref="FileNotFoundException">文件不存在。</exception>
        public static string ReadAllText(FileInfo file, Encoding encoding = null)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            return ReadAllText(file.FullName, encoding);
        }
        #endregion

        #region WriteAllText
        /// <summary>
        /// 将指定字符串写入文件，使用给定的编码（默认为 UTF-8 带 BOM）。
        /// </summary>
        /// <param name="filePath">要写入的文件的完整路径。</param>
        /// <param name="content">要写入的文本内容。</param>
        /// <param name="encoding">
        /// 要使用的文本编码。若为 <see langword="null"/>，则使用带 BOM 的 UTF-8（<c>new UTF8Encoding(true)</c>）。
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="filePath"/> 为 <see langword="null"/>。</exception>
        /// <remarks>
        /// 如果文件已存在，将被覆盖。父目录会自动创建。
        /// </remarks>
        public static void WriteAllText(string filePath, string content, Encoding encoding = null)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));
            encoding ??= new UTF8Encoding(true); // 默认：UTF-8 with BOM
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            using FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            using StreamWriter writer = new StreamWriter(fs, encoding);
            writer.Write(content);
        }
        /// <summary>
        /// 将指定字符串写入文件，使用给定的编码（默认为 UTF-8 带 BOM）。
        /// </summary>
        /// <param name="file">表示目标文件的 <see cref="FileInfo"/> 对象。</param>
        /// <param name="content">要写入的文本内容。</param>
        /// <param name="encoding">
        /// 要使用的文本编码。若为 <see langword="null"/>，则使用带 BOM 的 UTF-8。
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="file"/> 为 <see langword="null"/>。</exception>
        public static void WriteAllText(FileInfo file, string content, Encoding encoding = null)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            WriteAllText(file.FullName, content, encoding);
        }
        #endregion
    }
}