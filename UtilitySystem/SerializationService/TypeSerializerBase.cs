using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace EasyPack
{
    /// <summary>
    /// 通用序列化器基类
    /// </summary>
    public abstract class TypeSerializerBase<T> : ITypeSerializer<T>
    {
        public Type TargetType => typeof(T);
        public abstract SerializationStrategy SupportedStrategy { get; }

        public virtual string SerializeToJson(T obj)
        {
            throw new NotSupportedException($"Type {typeof(T).Name} does not support JSON serialization");
        }

        public virtual T DeserializeFromJson(string json)
        {
            throw new NotSupportedException($"Type {typeof(T).Name} does not support JSON deserialization");
        }

        public string SerializeToJson(object obj)
        {
            if (obj == null) return null;
            if (obj is T typedObj)
            {
                return SerializeToJson(typedObj);
            }
            throw new ArgumentException($"Object is not of type {typeof(T).Name}");
        }

        public object DeserializeFromJson(string json, Type targetType)
        {
            return DeserializeFromJson(json);
        }

        public virtual List<CustomDataEntry> SerializeToCustomData(T obj)
        {
            throw new NotSupportedException($"Type {typeof(T).Name} does not support CustomDataEntry serialization");
        }

        public virtual T DeserializeFromCustomData(List<CustomDataEntry> entries)
        {
            throw new NotSupportedException($"Type {typeof(T).Name} does not support CustomDataEntry deserialization");
        }

        public List<CustomDataEntry> SerializeToCustomData(object obj)
        {
            if (obj == null) return new List<CustomDataEntry>();
            if (obj is T typedObj)
            {
                return SerializeToCustomData(typedObj);
            }
            throw new ArgumentException($"Object is not of type {typeof(T).Name}");
        }

        public object DeserializeFromCustomData(List<CustomDataEntry> entries, Type targetType)
        {
            return DeserializeFromCustomData(entries);
        }

        public virtual byte[] SerializeToBinary(T obj)
        {
            throw new NotSupportedException($"Type {typeof(T).Name} does not support Binary serialization");
        }

        public virtual T DeserializeFromBinary(byte[] data)
        {
            throw new NotSupportedException($"Type {typeof(T).Name} does not support Binary deserialization");
        }

        public byte[] SerializeToBinary(object obj)
        {
            if (obj == null) return null;
            if (obj is T typedObj)
            {
                return SerializeToBinary(typedObj);
            }
            throw new ArgumentException($"Object is not of type {typeof(T).Name}");
        }

        public object DeserializeFromBinary(byte[] data, Type targetType)
        {
            return DeserializeFromBinary(data);
        }
    }

    /// <summary>
    /// JSON 序列化器基类
    /// </summary>
    public abstract class JsonSerializerBase<T> : TypeSerializerBase<T>
    {
        public override SerializationStrategy SupportedStrategy => SerializationStrategy.Json;

        public abstract override string SerializeToJson(T obj);
        public abstract override T DeserializeFromJson(string json);
    }

    /// <summary>
    /// CustomData 序列化器基类
    /// </summary>
    public abstract class CustomDataSerializerBase<T> : TypeSerializerBase<T>
    {
        public override SerializationStrategy SupportedStrategy => SerializationStrategy.CustomDataEntry;

        public abstract override List<CustomDataEntry> SerializeToCustomData(T obj);
        public abstract override T DeserializeFromCustomData(List<CustomDataEntry> entries);
    }

    /// <summary>
    /// Binary 序列化器基类
    /// </summary>
    public abstract class BinarySerializerBase<T> : TypeSerializerBase<T>
    {
        public override SerializationStrategy SupportedStrategy => SerializationStrategy.Binary;

        public abstract override byte[] SerializeToBinary(T obj);
        public abstract override T DeserializeFromBinary(byte[] data);
    }

    /// <summary>
    /// Unity JsonUtility 序列化器
    /// </summary>
    public class UnityJsonSerializer<T> : JsonSerializerBase<T>
    {
        public override string SerializeToJson(T obj)
        {
            if (obj == null) return null;
            return JsonUtility.ToJson(obj);
        }

        public override T DeserializeFromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return default(T);
            return JsonUtility.FromJson<T>(json);
        }
    }

    /// <summary>
    /// 标准 BinaryFormatter 序列化器
    /// </summary>
    public class StandardBinarySerializer<T> : BinarySerializerBase<T>
    {
        public override byte[] SerializeToBinary(T obj)
        {
            if (obj == null) return null;

            using (var stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, obj);
                return stream.ToArray();
            }
        }

        public override T DeserializeFromBinary(byte[] data)
        {
            if (data == null || data.Length == 0) return default(T);

            using (var stream = new MemoryStream(data))
            {
                var formatter = new BinaryFormatter();
                return (T)formatter.Deserialize(stream);
            }
        }
    }
}
