using System;
using System.Collections.Generic;

namespace EasyPack.GamePropertySystem
{
    /// <summary>
    ///     批量操作结果
    ///     表示操作的成功/失败状态和详细信息
    /// </summary>
    /// <typeparam name="T">成功数据类型（如List&lt;string&gt;表示成功的属性ID列表）</typeparam>
    [Serializable]
    public class OperationResult<T>
    {
        /// <summary>是否全部成功（无失败项）</summary>
        public bool IsFullSuccess;

        /// <summary>成功项数量</summary>
        public int SuccessCount;

        /// <summary>成功数据（泛型）</summary>
        public T SuccessData;

        /// <summary>失败项详情列表</summary>
        public List<FailureRecord> Failures;

        /// <summary>
        ///     默认构造函数
        /// </summary>
        public OperationResult() => Failures = new();

        /// <summary>
        ///     创建全部成功的结果
        /// </summary>
        public static OperationResult<T> Success(T data, int count) =>
            new()
            {
                IsFullSuccess = true,
                SuccessCount = count,
                SuccessData = data,
                Failures = new(),
            };

        /// <summary>
        ///     创建部分成功的结果
        /// </summary>
        public static OperationResult<T> PartialSuccess(T data, int successCount, List<FailureRecord> failures) =>
            new()
            {
                IsFullSuccess = false,
                SuccessCount = successCount,
                SuccessData = data,
                Failures = failures,
            };
    }

    /// <summary>
    ///     失败记录
    ///     描述单个失败项的详细信息
    /// </summary>
    [Serializable]
    public struct FailureRecord
    {
        /// <summary>失败项ID（属性ID或分类名）</summary>
        public string ItemId;

        /// <summary>错误消息</summary>
        public string ErrorMessage;

        /// <summary>错误类型</summary>
        public FailureType Type;

        /// <summary>
        ///     构造函数
        /// </summary>
        public FailureRecord(string itemId, string errorMessage, FailureType type)
        {
            ItemId = itemId;
            ErrorMessage = errorMessage;
            Type = type;
        }
    }

    /// <summary>
    ///     失败类型枚举
    /// </summary>
    public enum FailureType
    {
        /// <summary>属性不存在</summary>
        PropertyNotFound,

        /// <summary>分类不存在</summary>
        CategoryNotFound,

        /// <summary>修饰符无效</summary>
        InvalidModifier,

        /// <summary>属性已释放</summary>
        PropertyDisposed,

        /// <summary>状态不允许操作（如服务已暂停）</summary>
        InvalidState,

        /// <summary>未知错误</summary>
        UnknownError,
    }
}