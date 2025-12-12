using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DescriptIon
{
    /// <summary>
    /// 表示一个 Total Commander 或 Double Commander 的 <c>descript.ion</c> 文件的可读写视图。
    /// 支持带空格的文件名、引号转义、多行注释，并能自动识别和保留原始文本编码及注释格式。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 此类内部统一使用标准换行符 <c>\n</c> 表示多行注释。
    /// 保存时会根据目标格式（<see cref="CommentFormat"/>）转换为：
    /// <list type="bullet">
    ///   <item><description>Total Commander：附加 <c>\u0004\u00C2</c> 标记</description></item>
    ///   <item><description>Double Commander：用 <c>\u00A0</c>（NO-BREAK SPACE）连接各行</description></item>
    /// </list>
    /// </para>
    /// <para>推荐使用 <see cref="CommentFormat.AutoDetect"/> 以兼容现有文件。</para>
    /// </remarks>
    public class DescriptionStore
    {
        private readonly DirectoryInfo _directory;
        private Encoding? _encoding;
        private CommentFormat _format;
        private readonly Dictionary<string, string> _entries;

        private const string FileName = "descript.ion";

        /// <summary>
        /// 初始化一个新的 <see cref="DescriptionStore"/> 实例，绑定到指定目录路径，
        /// 并指定注释格式。
        /// </summary>
        /// <param name="directoryPath">包含或即将包含 <c>descript.ion</c> 的目录路径。</param>
        /// <param name="format">注释的多行格式。默认为 <see cref="CommentFormat.AutoDetect"/>。</param>
        /// <exception cref="ArgumentNullException"><paramref name="directoryPath"/> 为 <see langword="null"/>。</exception>
        public DescriptionStore(string directoryPath, CommentFormat format = CommentFormat.AutoDetect)
            : this(new DirectoryInfo(directoryPath ?? throw new ArgumentNullException(nameof(directoryPath))), format)
        {
        }

        /// <summary>
        /// 初始化一个新的 <see cref="DescriptionStore"/> 实例，绑定到指定目录，
        /// 并指定注释格式。
        /// </summary>
        /// <param name="directory">表示目标目录的 <see cref="DirectoryInfo"/> 对象。</param>
        /// <param name="format">注释的多行格式。默认为 <see cref="CommentFormat.AutoDetect"/>。</param>
        /// <exception cref="ArgumentNullException"><paramref name="directory"/> 为 <see langword="null"/>。</exception>
        public DescriptionStore(DirectoryInfo directory, CommentFormat format = CommentFormat.AutoDetect)
        {
            _directory = directory ?? throw new ArgumentNullException(nameof(directory));
            _entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _format = format;
            _encoding = null;
        }

        /// <summary>
        /// 获取当前绑定的目录。
        /// </summary>
        public DirectoryInfo Directory => _directory;

        /// <summary>
        /// 获取当前所有注释条目的只读副本。
        /// </summary>
        /// <value>
        /// 字典的键为文件名（不区分大小写），值为对应的 <see cref="DescriptIonEntry"/> 对象。
        /// 注释内容始终使用标准 <c>\n</c> 表示换行，与底层格式无关。
        /// </value>
        public IReadOnlyDictionary<string, string> Entries =>
            new Dictionary<string, string>(_entries, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 获取或设置当前使用的注释格式。
        /// </summary>
        /// <remarks>
        /// 如果已调用 <see cref="Load"/> 且使用了 <see cref="CommentFormat.AutoDetect"/>，
        /// 此属性将反映实际检测到的格式。
        /// 修改此属性会影响后续 <see cref="Save"/> 的输出格式。
        /// </remarks>
        public CommentFormat Format
        {
            get => _format;
            set => _format = value;
        }

        /// <summary>
        /// 获取或设置用于读写 <c>descript.ion</c> 文件的编码。
        /// 默认为 UTF-8（带 BOM）。
        /// </summary>
        /// <remarks>
        /// 如果调用 <see cref="Load"/> 且文件存在时，
        /// 此属性将反映实际检测到的格式。
        /// 修改此属性会影响后续 <see cref="Save"/> 的输出格式。
        /// </remarks>
        public Encoding Encoding
        {
            get => _encoding ??= new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
            set => _encoding = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// 从 <c>descript.ion</c> 文件加载注释（如果存在）。
        /// 自动检测并记录文件的原始文本编码和注释格式（若为 <see cref="CommentFormat.AutoDetect"/>）。
        /// </summary>
        /// <returns>
        /// 如果文件存在并成功加载，则返回 <see langword="true"/>；否则返回 <see langword="false"/>。
        /// </returns>
        /// <exception cref="IOException">读取文件时发生 I/O 错误。</exception>
        /// <exception cref="UnauthorizedAccessException">无权访问文件。</exception>
        public bool Load()
        {
            string filePath = Path.Combine(_directory.FullName, FileName);
            if (!File.Exists(filePath))
            {
                _entries.Clear();
                _encoding = null;
                if (_format == CommentFormat.AutoDetect)
                    _format = CommentFormat.TotalCommander; // 默认回退
                return false;
            }

            _encoding = SmartFile.DetectEncoding(filePath);
            string content = SmartFile.ReadAllText(filePath, _encoding);

            string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            Dictionary<string, string> entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            CommentFormat detectedFormat = CommentFormat.TotalCommander; // 默认假设 TC

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                // 检查是否为 Total Commander 多行格式（行尾有 \u0004\u00C2）
                if (line.EndsWith("\u0004\u00c2"))
                {
                    detectedFormat = CommentFormat.TotalCommander;
                    string rawLine = line.Substring(0, line.Length - 2); // 移除标记
                    var (fileName, comment) = ParseLineInternal(rawLine, isTotalCommander: true);
                    if (!string.IsNullOrEmpty(fileName))
                        entries[fileName] = comment;
                }
                // 否则检查是否含 \u00A0（Double Commander 特征）
                else if (line.Contains("\u00a0") && !line.Contains("\\n"))
                {
                    detectedFormat = CommentFormat.DoubleCommander;
                    var (fileName, comment) = ParseLineInternal(line, isTotalCommander: false);
                    if (!string.IsNullOrEmpty(fileName))
                        entries[fileName] = comment;
                }
                else
                {
                    // 普通单行：按 TC 方式解析（兼容性更好）
                    var (fileName, comment) = ParseLineInternal(line, isTotalCommander: true);
                    if (!string.IsNullOrEmpty(fileName))
                        entries[fileName] = comment;
                }
            }

            _entries.Clear();
            foreach (var kvp in entries)
                _entries[kvp.Key] = kvp.Value;

            // 若为 AutoDetect，锁定检测到的格式
            if (_format == CommentFormat.AutoDetect)
            {
                _format = detectedFormat;
            }

            return true;
        }

        /// <summary>
        /// 将当前注释数据保存到 <c>descript.ion</c> 文件。
        /// 使用加载时检测到的原始编码；若从未加载，则使用 UTF-8 带 BOM。
        /// 输出格式由 <see cref="Format"/> 属性决定。
        /// </summary>
        /// <exception cref="IOException">写入文件时发生 I/O 错误。</exception>
        /// <exception cref="UnauthorizedAccessException">无权写入目录或文件。</exception>
        public void Save()
        {
            string filePath = Path.Combine(_directory.FullName, FileName);
            List<string> lines = new List<string>();

            foreach (var kvp in _entries)
            {
                string fileName = kvp.Key;
                string comment = kvp.Value;
                //需要等待 Double Commander 提供文件名内存在 " 字符的支持
                string fileNamePart = fileName.Contains(' ') || fileName.Contains('"')
                    ? $"\"{fileName.Replace("\"", "\"\"")}\""
                    : fileName;

                string outputComment;
                if (_format == CommentFormat.DoubleCommander)
                {
                    // Double Commander: \n → \u00A0
                    outputComment = comment.Replace("\n", "\u00A0");
                }
                else
                {
                    // Total Commander: \n → \\n
                    outputComment = comment.Replace("\n", "\\n");
                    if (comment.Contains('\n'))
                    {
                        outputComment += "\u0004\u00C2";
                    }
                }

                lines.Add($"{fileNamePart} {outputComment}");
            }

            _directory.Create(); // 确保目录存在

            Encoding encodingToUse = _encoding ?? new UTF8Encoding(true); // 默认 UTF-8 with BOM
            SmartFile.WriteAllText(filePath, string.Join(Environment.NewLine, lines), encodingToUse);
        }

        /// <summary>
        /// 设置指定文件的注释。
        /// </summary>
        /// <param name="fileName">文件名（不区分大小写）。</param>
        /// <param name="comment">
        /// 注释内容。使用标准 <c>\n</c> 表示换行（例如 <c>"第一行\n第二行"</c>）。
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="fileName"/> 或 <paramref name="comment"/> 为 <see langword="null"/>。
        /// </exception>
        /// <remarks>
        /// 此方法仅修改内存中的数据，需调用 <see cref="Save"/> 才会写入磁盘。
        /// </remarks>
        public void SetComment(string fileName, string comment)
        {
            if (fileName == null) throw new ArgumentNullException(nameof(fileName));
            if (comment == null) throw new ArgumentNullException(nameof(comment));

            _entries[fileName] = comment;
        }

        /// <summary>
        /// 获取指定文件的注释。
        /// </summary>
        /// <param name="fileName">文件名（不区分大小写）。</param>
        /// <returns>注释字符串（使用标准 <c>\n</c> 表示换行），如果不存在则返回 <see langword="null"/>。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="fileName"/> 为 null。</exception>
        public string? GetComment(string fileName)
        {
            if (fileName == null) throw new ArgumentNullException(nameof(fileName));
            return _entries.TryGetValue(fileName, out string? comment) ? comment : null;
        }

        /// <summary>
        /// 移除指定文件的注释。
        /// </summary>
        /// <param name="fileName">文件名（不区分大小写）。</param>
        /// <returns>如果注释存在并被移除，则为 <see langword="true"/>；否则为 <see langword="false"/>。</returns>
        public bool RemoveComment(string fileName)
        {
            return _entries.Remove(fileName);
        }

        /// <summary>
        /// 移除所有没有对应物理文件或子目录的注释条目。
        /// </summary>
        /// <remarks>
        /// 此方法仅修改内存中的数据，需调用 <see cref="Save"/> 才会写入磁盘。
        /// </remarks>
        public void RemoveOrphanedEntries()
        {
            List<string> keysToRemove = _entries.Keys
                .Where(fileName =>
                {
                    string fullPath = Path.Combine(_directory.FullName, fileName);
                    return !File.Exists(fullPath) && !new DirectoryInfo(fullPath).Exists;
                })
                .ToList();

            foreach (string key in keysToRemove)
            {
                _entries.Remove(key);
            }
        }
        /// <summary>
        /// 对注释条目按键名（文件/文件夹名）重新排序。
        /// </summary>
        /// <param name="comparer">
        /// 用于比较键名的比较器。若为 <see langword="null"/>，
        /// 则使用 <see cref="StringComparer.CurrentCulture"/>（默认行为）。
        /// </param>
        /// <remarks>
        /// 此方法仅影响内存中的顺序，需调用 <see cref="Save"/> 才会写入磁盘。
        /// </remarks>
        public void Sort(IComparer<string>? comparer = null)
        {
            comparer ??= StringComparer.CurrentCulture;

            List<string> sortedKeys = _entries.Keys
                .OrderBy(k => k, comparer)
                .ToList();

            // 重建字典以保留插入顺序（.NET Core 2.1+ / .NET Standard 2.1+ 保证 Dictionary 插入顺序）
            Dictionary<string, string> newEntries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string key in sortedKeys)
            {
                newEntries[key] = _entries[key];
            }

            _entries.Clear();
            foreach (var kvp in newEntries)
            {
                _entries[kvp.Key] = kvp.Value;
            }
        }

        /// <summary>
        /// 从一行原始文本中解析出文件名和注释。
        /// </summary>
        /// <param name="fullLine">去除多行标记后的完整行（如 "file.txt comment"）。</param>
        /// <param name="isTotalCommander">
        /// 是否按 Total Commander 规则解析（影响 \n 的处理）。
        /// 对于 DC，\u00A0 已在调用前替换为 \n。
        /// </param>
        /// <returns>解析后的条目，或 <see langword="null"/>（若无效）。</returns>
        private static (string fileName, string comment) ParseLineInternal(string fullLine, bool isTotalCommander)
        {
            if (string.IsNullOrWhiteSpace(fullLine))
                return (string.Empty, string.Empty);

            int i = 0;
            bool inQuotes = false;
            StringBuilder fileNameBuilder = new StringBuilder();

            // Step 1: 解析文件名（支持引号和 "" 转义）
            while (i < fullLine.Length)
            {
                char c = fullLine[i];

                if (c == '"')
                {
                    // 检查是否是 "" 转义
                    if (i + 1 < fullLine.Length && fullLine[i + 1] == '"')
                    {
                        // 转义：添加一个 "
                        fileNameBuilder.Append('"');
                        i += 2; // 跳过两个 "
                    }
                    else
                    {
                        // 切换引号状态
                        inQuotes = !inQuotes;
                        i++;
                    }
                }
                else if (c == ' ' && !inQuotes)
                {
                    // 找到分隔空格
                    i++; // 跳过空格
                    break;
                }
                else
                {
                    fileNameBuilder.Append(c);
                    i++;
                }
            }

            string fileName = fileNameBuilder.ToString();
            string comment = i < fullLine.Length ? fullLine.Substring(i) : "";

            // Step 2: 处理注释内容
            if (isTotalCommander)
            {
                // TC: 字面 "\\n" → 内部 "\n"
                comment = comment.Replace("\\n", "\n");
            }
            else
            {
                // DC: \u00A0 → 内部 "\n"
                comment = comment.Replace("\u00A0", "\n");
            }

            return (fileName, comment);
        }
    }
}