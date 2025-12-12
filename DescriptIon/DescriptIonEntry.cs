namespace DescriptIon
{
    /// <summary>
    /// 表示 <c>descript.ion</c> 文件中的一个注释条目。
    /// 注释内容使用标准 \n 表示换行，与具体文件管理器格式无关。
    /// </summary>
    public class DescriptIonEntry
    {
        /// <summary>
        /// 获取或设置被注释的文件名。
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 获取或设置注释文本。
        /// 使用标准 \n 表示换行（例如 "line1\nline2"）。
        /// </summary>
        public string Comment { get; set; }
    }
}