using System;
using EasyPack.CustomData;

namespace EasyPack.Serialization
{
    /// <summary>
    ///     双泛型序列化器适配器
    ///     将 ITypeSerializer&lt;TOriginal, TSerializable&gt; 适配为 ITypeSerializer&lt;TOriginal&gt;
    /// </summary>
    internal class TypeSerializerAdapter<TOriginal, TSerializable> : ITypeSerializer<TOriginal>
        where TSerializable : ISerializable
    {
        private readonly ITypeSerializer<TOriginal, TSerializable> _serializer;

        public TypeSerializerAdapter(ITypeSerializer<TOriginal, TSerializable> serializer) => _serializer = serializer;

        public Type TargetType => typeof(TOriginal);
        public SerializationStrategy SupportedStrategy => SerializationStrategy.Json;

        public string SerializeToJson(TOriginal obj) => _serializer.SerializeToJson(obj);

        public TOriginal DeserializeFromJson(string json) => _serializer.DeserializeFromJson(json);

        public string SerializeToJson(object obj) => SerializeToJson((TOriginal)obj);

        public object DeserializeFromJson(string json, Type targetType) => DeserializeFromJson(json);

        public CustomDataCollection SerializeToCustomData(TOriginal obj) =>
            throw new NotSupportedException($"类型 {typeof(TOriginal).Name} 的双泛型序列化器不支持 CustomDataEntry 序列化");

        public TOriginal DeserializeFromCustomData(CustomDataCollection entries) =>
            throw new NotSupportedException($"类型 {typeof(TOriginal).Name} 的双泛型序列化器不支持 CustomDataEntry 反序列化");

        public CustomDataCollection SerializeToCustomData(object obj) => SerializeToCustomData((TOriginal)obj);

        public object DeserializeFromCustomData(CustomDataCollection entries, Type targetType) =>
            DeserializeFromCustomData(entries);

        public byte[] SerializeToBinary(TOriginal obj) =>
            throw new NotSupportedException($"类型 {typeof(TOriginal).Name} 的双泛型序列化器不支持二进制序列化");

        public TOriginal DeserializeFromBinary(byte[] data) =>
            throw new NotSupportedException($"类型 {typeof(TOriginal).Name} 的双泛型序列化器不支持二进制反序列化");

        public byte[] SerializeToBinary(object obj) => SerializeToBinary((TOriginal)obj);

        public object DeserializeFromBinary(byte[] data, Type targetType) => DeserializeFromBinary(data);
    }
}