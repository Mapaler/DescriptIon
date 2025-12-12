namespace DescriptIon
{
    /// <summary>
    /// 指定 <c>descript.ion</c> 文件注释的多行格式。
    /// </summary>
    public enum CommentFormat
    {
        /// <summary>
        /// Total Commander 格式：注释中的换行用 \n 表示，
        /// 并在行尾附加 EOT + Â (\u0004\u00C2) 标记。
        /// </summary>
        TotalCommander,

        /// <summary>
        /// Double Commander 格式：多行注释用 NO-BREAK SPACE (\u00A0) 连接，
        /// 无额外标记。
        /// </summary>
        DoubleCommander,

        /// <summary>
        /// 自动检测格式。加载时分析内容决定使用哪种格式；
        /// 若未加载或无法判断，默认使用 <see cref="TotalCommander"/>。
        /// </summary>
        AutoDetect
    }
}