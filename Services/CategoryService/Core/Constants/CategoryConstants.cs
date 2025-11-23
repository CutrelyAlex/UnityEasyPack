namespace EasyPack.Category
{
    /// <summary>
    /// 分类管理器常量定义
    /// </summary>
    public static class CategoryConstants
    {
        /// <summary>
        /// 无效的词汇 ID（用于表示未定义/无效状态）
        /// </summary>
        public const int INVALID_TERM_ID = -1;

        /// <summary>
        /// 根节点的词汇 ID
        /// </summary>
        public const int ROOT_TERM_ID = -1;

        /// <summary>
        /// 分类路径分隔符
        /// </summary>
        public const string CATEGORY_SEPARATOR = ".";

        /// <summary>
        /// ID 分配的目标上限
        /// </summary>
        public const int ID_OVERFLOW_THRESHOLD = int.MaxValue - 100;

        /// <summary>
        /// 默认初始容量（映射表）
        /// </summary>
        public const int DEFAULT_MAPPER_CAPACITY = 256;

        /// <summary>
        /// 默认缓存容量（查询结果缓存）
        /// </summary>
        public const int DEFAULT_CACHE_CAPACITY = 128;

        /// <summary>
        /// 锁重试超时（毫秒）
        /// </summary>
        public const int LOCK_TIMEOUT_MS = 5000;
    }

    /// <summary>
    /// 分类词汇类型标记（用于 IntegerMapper&lt;CategoryTerm&gt; 泛型参数）
    /// </summary>
    internal struct CategoryTerm
    {
    }

    /// <summary>
    /// 标签类型标记（用于 IntegerMapper&lt;Tag&gt; 泛型参数）
    /// </summary>
    internal struct Tag
    {
    }
}