namespace EasyPack.Category
{
    /// <summary>
    ///     分类管理器常量定义
    /// </summary>
    public static class CategoryConstants
    {

        /// <summary>
        ///     分类路径分隔符
        /// </summary>
        public const string CATEGORY_SEPARATOR = ".";

        /// <summary>
        ///     ID 分配的目标上限
        /// </summary>
        public const int ID_OVERFLOW_THRESHOLD = int.MaxValue - 100;

        /// <summary>
        ///     默认初始容量（映射表）
        /// </summary>
        public const int DEFAULT_MAPPER_CAPACITY = 256;
    }
}