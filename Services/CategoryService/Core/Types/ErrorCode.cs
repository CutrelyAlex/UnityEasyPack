namespace EasyPack.CategoryService
{
    /// <summary>
    /// 错误代码枚举
    /// 定义操作可能返回的错误类型
    /// </summary>
    public enum ErrorCode
    {
        /// <summary>
        /// 无错误
        /// </summary>
        None = 0,

        /// <summary>
        /// 重复的实体 ID
        /// </summary>
        DuplicateId = 1,

        /// <summary>
        /// 未找到实体或分类
        /// </summary>
        NotFound = 2,

        /// <summary>
        /// 无效的分类名称
        /// </summary>
        InvalidCategory = 3,

        /// <summary>
        /// 无效的模式表达式
        /// </summary>
        InvalidPattern = 4,

        /// <summary>
        /// 并发冲突
        /// </summary>
        ConcurrencyConflict = 5,

        /// <summary>
        /// 锁超时
        /// </summary>
        LockTimeout = 6
    }
}
