using UnityEngine;

namespace EasyPack.Serialization
{
    /// <summary>
    ///     JSON 序列化器基类
    ///     提供 ToJson/FromJson 的默认JsonUtility实现
    /// </summary>
    /// <typeparam name="TOriginal">原始对象类型</typeparam>
    /// <typeparam name="TSerializable">可序列化 DTO 类型</typeparam>
    public abstract class JsonSerializerBase<TOriginal, TSerializable> : ITypeSerializer<TOriginal, TSerializable>
        where TSerializable : ISerializable
    {
        /// <summary>
        ///     将原始对象转换为可序列化 DTO
        /// </summary>
        public abstract TSerializable ToSerializable(TOriginal obj);

        /// <summary>
        ///     从可序列化 DTO 转换回原始对象
        /// </summary>
        public abstract TOriginal FromSerializable(TSerializable dto);

        /// <summary>
        ///     将 DTO 序列化为 JSON 字符串（默认使用 JsonUtility）
        /// </summary>
        public virtual string ToJson(TSerializable dto)
        {
            return dto == null ? null : JsonUtility.ToJson(dto);
        }

        /// <summary>
        ///     从 JSON 字符串反序列化为 DTO（默认使用 JsonUtility）
        /// </summary>
        public virtual TSerializable FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return default;
            return JsonUtility.FromJson<TSerializable>(json);
        }

        /// <summary>
        ///     将原始对象直接序列化为 JSON
        /// </summary>
        public string SerializeToJson(TOriginal obj)
        {
            TSerializable dto = ToSerializable(obj);
            return ToJson(dto);
        }

        /// <summary>
        ///     从 JSON 直接反序列化为原始对象
        /// </summary>
        public TOriginal DeserializeFromJson(string json)
        {
            TSerializable dto = FromJson(json);
            return FromSerializable(dto);
        }
    }

    /// <summary>
    ///     自身即为 DTO 的序列化器基类
    ///     当 TOriginal == TSerializable 时使用此基类
    /// </summary>
    /// <typeparam name="T">同时作为原始类型和 DTO 类型</typeparam>
    public abstract class SelfSerializerBase<T> : ITypeSerializer<T, T>
        where T : ISerializable
    {
        public T ToSerializable(T obj) => obj;

        public T FromSerializable(T dto) => dto;

        public virtual string ToJson(T dto)
        {
            return dto == null ? null : JsonUtility.ToJson(dto);
        }

        public virtual T FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return default;
            return JsonUtility.FromJson<T>(json);
        }

        public string SerializeToJson(T obj) => ToJson(obj);

        public T DeserializeFromJson(string json) => FromJson(json);
    }
}
