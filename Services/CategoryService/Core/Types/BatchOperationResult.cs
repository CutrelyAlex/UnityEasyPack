using System.Collections.Generic;

namespace EasyPack.Category
{
    /// <summary>
    /// 批量操作结果
    /// </summary>
    public class BatchOperationResult
    {
        /// <summary>
        /// 总数
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 成功数量
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// 失败数量
        /// </summary>
        public int FailureCount { get; set; }

        /// <summary>
        /// 详细结果
        /// </summary>
        public List<(string EntityId, bool Success, ErrorCode ErrorCode, string ErrorMessage)> Details { get; set; }

        /// <summary>
        /// 是否全部成功
        /// </summary>
        public bool IsFullSuccess => FailureCount == 0;

        /// <summary>
        /// 是否部分成功
        /// </summary>
        public bool IsPartialSuccess => SuccessCount > 0 && FailureCount > 0;
    }
}
