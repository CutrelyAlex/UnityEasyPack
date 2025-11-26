using EasyPack.CustomData;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace EasyPack.Serialization
{
    /// <summary>
    ///     通用序列化器基类
    /// </summary>
    public abstract class TypeSerializerBase<T> : ITypeSerializer<T>
    {
        public Type TargetType => typeof(T);
        public abstract SerializationStrategy SupportedStrategy { get; }

        public virtual string SerializeToJson(T obj) =>
            throw new NotSupportedException($"类型 {typeof(T).Name} 不支持 JSON 序列化");

        public virtual T DeserializeFromJson(string json) =>
            throw new NotSupportedException($"类型 {typeof(T).Name} 不支持 JSON 反序列化");

        public string SerializeToJson(object obj)
        {
            if (obj is T typedObj) return SerializeToJson(typedObj);

            throw new ArgumentException($"对象不是类型 {typeof(T).Name}");
        }

        public object DeserializeFromJson(string json, Type targetType) => DeserializeFromJson(json);

        public virtual CustomDataCollection SerializeToCustomData(T obj) =>
            throw new NotSupportedException($"类型 {typeof(T).Name} 不支持 CustomDataEntry 序列化");

        public virtual T DeserializeFromCustomData(CustomDataCollection entries) =>
            throw new NotSupportedException($"类型 {typeof(T).Name} 不支持 CustomDataEntry 反序列化");

        public CustomDataCollection SerializeToCustomData(object obj)
        {
            if (obj is T typedObj) return SerializeToCustomData(typedObj);

            throw new ArgumentException($"对象不是类型 {typeof(T).Name}");
        }

        public object DeserializeFromCustomData(CustomDataCollection entries, Type targetType) =>
            DeserializeFromCustomData(entries);

        public virtual byte[] SerializeToBinary(T obj) =>
            throw new NotSupportedException($"类型 {typeof(T).Name} 不支持二进制序列化");

        public virtual T DeserializeFromBinary(byte[] data) =>
            throw new NotSupportedException($"类型 {typeof(T).Name} 不支持二进制反序列化");

        public byte[] SerializeToBinary(object obj)
        {
            if (obj is T typedObj) return SerializeToBinary(typedObj);

            throw new ArgumentException($"对象不是类型 {typeof(T).Name}");
        }

        public object DeserializeFromBinary(byte[] data, Type targetType) => DeserializeFromBinary(data);
    }

    /// <summary>
    ///     JSON 序列化器基类
    /// </summary>
    public abstract class JsonSerializerBase<T> : TypeSerializerBase<T>
    {
        public override SerializationStrategy SupportedStrategy => SerializationStrategy.Json;

        public abstract override string SerializeToJson(T obj);
        public abstract override T DeserializeFromJson(string json);
    }

    /// <summary>
    ///     CustomData 序列化器基类
    /// </summary>
    public abstract class CustomDataSerializerBase<T> : TypeSerializerBase<T>
    {
        public override SerializationStrategy SupportedStrategy => SerializationStrategy.CustomDataEntry;

        public abstract override CustomDataCollection SerializeToCustomData(T obj);
        public abstract override T DeserializeFromCustomData(CustomDataCollection entries);
    }

    /// <summary>
    ///     Binary 序列化器基类
    /// </summary>
    public abstract class BinarySerializerBase<T> : TypeSerializerBase<T>
    {
        public override SerializationStrategy SupportedStrategy => SerializationStrategy.Binary;

        public abstract override byte[] SerializeToBinary(T obj);
        public abstract override T DeserializeFromBinary(byte[] data);
    }

    /// <summary>
    ///     Unity JsonUtility 序列化器
    /// </summary>
    public class UnityJsonSerializer<T> : JsonSerializerBase<T>
    {
        public override string SerializeToJson(T obj) => JsonUtility.ToJson(obj);

        public override T DeserializeFromJson(string json) => JsonUtility.FromJson<T>(json);
    }

    /// <summary>
    ///     标准 BinaryFormatter 序列化器
    /// </summary>
    public class StandardBinarySerializer<T> : BinarySerializerBase<T>
    {
        public override byte[] SerializeToBinary(T obj)
        {
            using var stream = new MemoryStream();
            var formatter = new BinaryFormatter();
            formatter.Serialize(stream, obj);
            return stream.ToArray();
        }

        public override T DeserializeFromBinary(byte[] data)
        {
            using var stream = new MemoryStream(data);
            var formatter = new BinaryFormatter();
            return (T)formatter.Deserialize(stream);
        }
    }
}