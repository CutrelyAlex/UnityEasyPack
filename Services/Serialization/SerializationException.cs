using System;

namespace EasyPack
{
    /// <summary>
    /// 序列化错误码
    /// </summary>
    public enum SerializationErrorCode
    {
        /// <summary>
        /// 未找到序列化器
        /// </summary>
        NoSerializerFound,

        /// <summary>
        /// 序列化失败
        /// </summary>
        SerializationFailed,

        /// <summary>
        /// 反序列化失败
        /// </summary>
        DeserializationFailed,

        /// <summary>
        /// 版本不匹配
        /// </summary>
        VersionMismatch,

        /// <summary>
        /// 不支持的策略
        /// </summary>
        UnsupportedStrategy,

        /// <summary>
        /// 无效的数据格式
        /// </summary>
        InvalidDataFormat
    }

    /// <summary>
    /// 序列化异常类
    /// </summary>
    public class SerializationException : Exception
    {
        /// <summary>
        /// 目标类型
        /// </summary>
        public Type TargetType { get; }

        /// <summary>
        /// 错误码
        /// </summary>
        public SerializationErrorCode ErrorCode { get; }

        public SerializationException(string message, Type targetType, SerializationErrorCode errorCode)
            : base(message)
        {
            TargetType = targetType;
            ErrorCode = errorCode;
        }

        public SerializationException(string message, Type targetType, SerializationErrorCode errorCode, Exception innerException)
            : base(message, innerException)
        {
            TargetType = targetType;
            ErrorCode = errorCode;
        }

        public override string ToString()
        {
            return $"[{ErrorCode}] {Message} (Type: {TargetType?.Name ?? "Unknown"})";
        }
    }
}
