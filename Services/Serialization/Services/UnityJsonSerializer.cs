using System;
using EasyPack.CustomData;
using UnityEngine;

namespace EasyPack.Serialization
{
    /// <summary>
    ///     Unity JsonUtility 序列化器
    ///     用于支持带有 [Serializable] 特性的类
    /// </summary>
    internal class UnityJsonSerializer : ITypeSerializer
    {
        public UnityJsonSerializer(Type targetType) => TargetType = targetType;

        public Type TargetType { get; }

        public SerializationStrategy SupportedStrategy => SerializationStrategy.Json;

        public string SerializeToJson(object obj) => JsonUtility.ToJson(obj);

        public object DeserializeFromJson(string json, Type targetType) => JsonUtility.FromJson(json, targetType);

        public CustomDataCollection SerializeToCustomData(object obj) =>
            throw new NotSupportedException("Unity JsonUtility 序列化器不支持 CustomDataEntry 序列化");

        public object DeserializeFromCustomData(CustomDataCollection entries, Type targetType) =>
            throw new NotSupportedException("Unity JsonUtility 序列化器不支持 CustomDataEntry 反序列化");

        public byte[] SerializeToBinary(object obj) =>
            throw new NotSupportedException("Unity JsonUtility 序列化器不支持二进制序列化");

        public object DeserializeFromBinary(byte[] data, Type targetType) =>
            throw new NotSupportedException("Unity JsonUtility 序列化器不支持二进制反序列化");
    }
}