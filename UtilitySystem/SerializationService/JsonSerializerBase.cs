using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace EasyPack
{
    /// <summary>
    /// JSON 序列化器基类
    /// </summary>
    public abstract class JsonSerializerBase<T> : ITypeSerializer<T>
    {
        public Type TargetType => typeof(T);
        public SerializationStrategy SupportedStrategy => SerializationStrategy.Json;

        public abstract string SerializeToJson(T obj);
        public abstract T DeserializeFromJson(string json);

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

        public List<CustomDataEntry> SerializeToCustomData(T obj)
        {
            throw new NotSupportedException($"Type {typeof(T).Name} does not support CustomDataEntry serialization");
        }

        public T DeserializeFromCustomData(List<CustomDataEntry> entries)
        {
            throw new NotSupportedException($"Type {typeof(T).Name} does not support CustomDataEntry deserialization");
        }

        public List<CustomDataEntry> SerializeToCustomData(object obj)
        {
            throw new NotSupportedException($"Type {typeof(T).Name} does not support CustomDataEntry serialization");
        }

        public object DeserializeFromCustomData(List<CustomDataEntry> entries, Type targetType)
        {
            throw new NotSupportedException($"Type {typeof(T).Name} does not support CustomDataEntry deserialization");
        }

        public byte[] SerializeToBinary(T obj)
        {
            throw new NotSupportedException($"Type {typeof(T).Name} does not support Binary serialization");
        }

        public T DeserializeFromBinary(byte[] data)
        {
            throw new NotSupportedException($"Type {typeof(T).Name} does not support Binary deserialization");
        }

        public byte[] SerializeToBinary(object obj)
        {
            throw new NotSupportedException($"Type {typeof(T).Name} does not support Binary serialization");
        }

        public object DeserializeFromBinary(byte[] data, Type targetType)
        {
            throw new NotSupportedException($"Type {typeof(T).Name} does not support Binary deserialization");
        }
    }

    /// <summary>
    /// CustomData 序列化器基类
    /// </summary>
    public abstract class CustomDataSerializerBase<T> : ITypeSerializer<T>
    {
        public Type TargetType => typeof(T);
        public SerializationStrategy SupportedStrategy => SerializationStrategy.CustomDataEntry;

        public abstract List<CustomDataEntry> SerializeToCustomData(T obj);
        public abstract T DeserializeFromCustomData(List<CustomDataEntry> entries);

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

        public string SerializeToJson(T obj)
        {
            throw new NotSupportedException($"Type {typeof(T).Name} does not support JSON serialization");
        }

        public T DeserializeFromJson(string json)
        {
            throw new NotSupportedException($"Type {typeof(T).Name} does not support JSON deserialization");
        }

        public string SerializeToJson(object obj)
        {
            throw new NotSupportedException($"Type {typeof(T).Name} does not support JSON serialization");
        }

        public object DeserializeFromJson(string json, Type targetType)
        {
            throw new NotSupportedException($"Type {typeof(T).Name} does not support JSON deserialization");
        }

        public byte[] SerializeToBinary(T obj)
        {
            throw new NotSupportedException($"Type {typeof(T).Name} does not support Binary serialization");
        }

        public T DeserializeFromBinary(byte[] data)
        {
            throw new NotSupportedException($"Type {typeof(T).Name} does not support Binary deserialization");
        }

        public byte[] SerializeToBinary(object obj)
        {
            throw new NotSupportedException($"Type {typeof(T).Name} does not support Binary serialization");
        }

        public object DeserializeFromBinary(byte[] data, Type targetType)
        {
            throw new NotSupportedException($"Type {typeof(T).Name} does not support Binary deserialization");
        }
    }

    /// <summary>
    /// Binary 序列化器基类
    /// </summary>
    public abstract class BinarySerializerBase<T> : ITypeSerializer<T>
    {
        public Type TargetType => typeof(T);
        public SerializationStrategy SupportedStrategy => SerializationStrategy.Binary;

        public abstract byte[] SerializeToBinary(T obj);
        public abstract T DeserializeFromBinary(byte[] data);

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

        public string SerializeToJson(T obj)
        {
            throw new NotSupportedException($"Type {typeof(T).Name} does not support JSON serialization");
        }

        public T DeserializeFromJson(string json)
        {
            throw new NotSupportedException($"Type {typeof(T).Name} does not support JSON deserialization");
        }

        public string SerializeToJson(object obj)
        {
            throw new NotSupportedException($"Type {typeof(T).Name} does not support JSON serialization");
        }

        public object DeserializeFromJson(string json, Type targetType)
        {
            throw new NotSupportedException($"Type {typeof(T).Name} does not support JSON deserialization");
        }

        public List<CustomDataEntry> SerializeToCustomData(T obj)
        {
            throw new NotSupportedException($"Type {typeof(T).Name} does not support CustomDataEntry serialization");
        }

        public T DeserializeFromCustomData(List<CustomDataEntry> entries)
        {
            throw new NotSupportedException($"Type {typeof(T).Name} does not support CustomDataEntry deserialization");
        }

        public List<CustomDataEntry> SerializeToCustomData(object obj)
        {
            throw new NotSupportedException($"Type {typeof(T).Name} does not support CustomDataEntry serialization");
        }

        public object DeserializeFromCustomData(List<CustomDataEntry> entries, Type targetType)
        {
            throw new NotSupportedException($"Type {typeof(T).Name} does not support CustomDataEntry deserialization");
        }
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
#pragma warning disable SYSLIB0011
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
#pragma warning restore SYSLIB0011
}
