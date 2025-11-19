namespace EasyPack.CategoryService
{
    /// <summary>
    /// 操作结果包装器（无返回值）
    /// 用于表示操作成功或失败及其原因
    /// </summary>
    public class OperationResult
    {
        /// <summary>
        /// 操作是否成功
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// 错误代码（成功时为 None）
        /// </summary>
        public ErrorCode ErrorCode { get; }

        /// <summary>
        /// 错误消息（可选）
        /// </summary>
        public string ErrorMessage { get; }

        private OperationResult(bool isSuccess, ErrorCode errorCode, string errorMessage)
        {
            IsSuccess = isSuccess;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
        }

        /// <summary>
        /// 创建成功结果
        /// </summary>
        public static OperationResult Success()
        {
            return new OperationResult(true, ErrorCode.None, null);
        }

        /// <summary>
        /// 创建失败结果
        /// </summary>
        public static OperationResult Failure(ErrorCode errorCode, string errorMessage = null)
        {
            return new OperationResult(false, errorCode, errorMessage);
        }

        /// <summary>
        /// 隐式转换为布尔值
        /// </summary>
        public static implicit operator bool(OperationResult result)
        {
            return result.IsSuccess;
        }
    }

    /// <summary>
    /// 操作结果包装器（带返回值）
    /// </summary>
    /// <typeparam name="T">返回值类型</typeparam>
    public class OperationResult<T>
    {
        /// <summary>
        /// 操作是否成功
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// 错误代码（成功时为 None）
        /// </summary>
        public ErrorCode ErrorCode { get; }

        /// <summary>
        /// 错误消息（可选）
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// 返回值（成功时有效）
        /// </summary>
        public T Value { get; }

        private OperationResult(bool isSuccess, ErrorCode errorCode, string errorMessage, T value)
        {
            IsSuccess = isSuccess;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
            Value = value;
        }

        /// <summary>
        /// 创建成功结果
        /// </summary>
        public static OperationResult<T> Success(T value)
        {
            return new OperationResult<T>(true, ErrorCode.None, null, value);
        }

        /// <summary>
        /// 创建失败结果
        /// </summary>
        public static OperationResult<T> Failure(ErrorCode errorCode, string errorMessage = null)
        {
            return new OperationResult<T>(false, errorCode, errorMessage, default(T));
        }

        /// <summary>
        /// 隐式转换为布尔值
        /// </summary>
        public static implicit operator bool(OperationResult<T> result)
        {
            return result.IsSuccess;
        }
    }
}
