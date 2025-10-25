using System;
using System.Collections.Generic;

namespace EasyPack
{
    /// <summary>
    /// 序列化策略
    /// </summary>
    public enum SerializationStrategy
    {
        /// <summary>
        /// JSON 序列化
        /// </summary>
        Json,

        /// <summary>
        /// CustomDataEntry 列表序列化
        /// </summary>
        CustomDataEntry,

        /// <summary>
        /// 二进制序列化
        /// </summary>
        Binary
    }

    /// <summary>
    /// 类型序列化器接口
    /// </summary>
    public interface ITypeSerializer
    {
        /// <summary>
        /// 目标类型
        /// </summary>
        Type TargetType { get; }

        /// <summary>
        /// 支持的序列化策略
        /// </summary>
        SerializationStrategy SupportedStrategy { get; }

        /// <summary>
        /// 序列化对象到 JSON 字符串
        /// </summary>
        string SerializeToJson(object obj);

        /// <summary>
        /// 从 JSON 字符串反序列化对象
        /// </summary>
        object DeserializeFromJson(string json, Type targetType);

        /// <summary>
        /// 序列化对象到 CustomDataEntry 列表
        /// </summary>
        List<CustomDataEntry> SerializeToCustomData(object obj);

        /// <summary>
        /// 从 CustomDataEntry 列表反序列化对象
        /// </summary>
        object DeserializeFromCustomData(List<CustomDataEntry> entries, Type targetType);

        /// <summary>
        /// 序列化对象到二进制数据
        /// </summary>
        byte[] SerializeToBinary(object obj);

        /// <summary>
        /// 从二进制数据反序列化对象
        /// </summary>
        object DeserializeFromBinary(byte[] data, Type targetType);
    }

    /// <summary>
    /// 泛型类型序列化器接口
    /// </summary>
    public interface ITypeSerializer<T> : ITypeSerializer
    {
        string SerializeToJson(T obj);
        T DeserializeFromJson(string json);

        List<CustomDataEntry> SerializeToCustomData(T obj);
        T DeserializeFromCustomData(List<CustomDataEntry> entries);

        byte[] SerializeToBinary(T obj);
        T DeserializeFromBinary(byte[] data);
    }

    /// <summary>
    /// 统一序列化服务接口
    /// </summary>
    public interface ISerializationService
    {
        void RegisterSerializer<T>(ITypeSerializer<T> serializer);
        void RegisterSerializer(ITypeSerializer serializer);

        string SerializeToJson<T>(T obj);
        string SerializeToJson(object obj, Type type);
        T DeserializeFromJson<T>(string json);
        object DeserializeFromJson(string json, Type type);

        List<CustomDataEntry> SerializeToCustomData<T>(T obj);
        List<CustomDataEntry> SerializeToCustomData(object obj, Type type);
        T DeserializeFromCustomData<T>(List<CustomDataEntry> entries);
        object DeserializeFromCustomData(List<CustomDataEntry> entries, Type type);

        byte[] SerializeToBinary<T>(T obj);
        byte[] SerializeToBinary(object obj, Type type);
        T DeserializeFromBinary<T>(byte[] data);
        object DeserializeFromBinary(byte[] data, Type type);

        bool HasSerializer(Type type);
        bool HasSerializer<T>();

        SerializationStrategy GetSupportedStrategy(Type type);
        SerializationStrategy GetSupportedStrategy<T>();
    }
}
