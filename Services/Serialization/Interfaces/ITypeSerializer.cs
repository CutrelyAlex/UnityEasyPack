namespace EasyPack.Serialization
{
    /// <summary>
    ///     双泛型类型序列化器接口
    ///     定义将原始对象 TOriginal 转换为可序列化 DTO TSerializable 的完整流程
    ///     分离对象转换和序列化逻辑，提供更清晰的序列化架构
    /// </summary>
    /// <typeparam name="TOriginal">原始对象类型（如 Card、GameProperty）</typeparam>
    /// <typeparam name="TSerializable">可序列化 DTO 类型（如 SerializableCard），必须实现 ISerializable</typeparam>
    public interface ITypeSerializer<TOriginal, TSerializable> where TSerializable : ISerializable
    {
        /// <summary>
        ///     将原始对象转换为可序列化 DTO
        ///     此方法负责将复杂对象转换为适合序列化的简单数据结构
        /// </summary>
        /// <param name="obj">原始对象</param>
        /// <returns>可序列化 DTO 对象</returns>
        TSerializable ToSerializable(TOriginal obj);

        /// <summary>
        ///     从可序列化 DTO 转换回原始对象
        ///     此方法负责从 DTO 重建完整的原始对象
        /// </summary>
        /// <param name="dto">可序列化 DTO 对象</param>
        /// <returns>原始对象</returns>
        /// <exception cref="SerializationException">当 DTO 数据不完整或无效时抛出</exception>
        TOriginal FromSerializable(TSerializable dto);

        /// <summary>
        ///     将 DTO 序列化为 JSON 字符串
        ///     通常使用 Unity 的 JsonUtility.ToJson
        /// </summary>
        /// <param name="dto">可序列化 DTO 对象</param>
        /// <returns>JSON 字符串</returns>
        string ToJson(TSerializable dto);

        /// <summary>
        ///     从 JSON 字符串反序列化为 DTO
        ///     通常使用 Unity 的 JsonUtility.FromJson
        /// </summary>
        /// <param name="json">JSON 字符串</param>
        /// <returns>可序列化 DTO 对象</returns>
        TSerializable FromJson(string json);

        /// <summary>
        ///     将原始对象直接序列化为 JSON（语法糖方法）
        ///     组合 ToSerializable 和 ToJson 两步操作
        /// </summary>
        /// <param name="obj">原始对象</param>
        /// <returns>JSON 字符串</returns>
        string SerializeToJson(TOriginal obj);

        /// <summary>
        ///     从 JSON 直接反序列化为原始对象（语法糖方法）
        ///     组合 FromJson 和 FromSerializable 两步操作
        /// </summary>
        /// <param name="json">JSON 字符串</param>
        /// <returns>原始对象</returns>
        TOriginal DeserializeFromJson(string json);
    }
}