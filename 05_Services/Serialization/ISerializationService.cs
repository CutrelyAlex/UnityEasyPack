using System;
using System.Collections.Generic;
using EasyPack.ENekoFramework;

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
    /// 双泛型类型序列化器接口
    /// 定义将原始对象 TOriginal 转换为可序列化 DTO TSerializable 的完整流程
    /// 分离对象转换和序列化逻辑，提供更清晰的序列化架构
    /// </summary>
    /// <typeparam name="TOriginal">原始对象类型（如 Card、GameProperty）</typeparam>
    /// <typeparam name="TSerializable">可序列化 DTO 类型（如 SerializableCard），必须实现 ISerializable</typeparam>
    public interface ITypeSerializer<TOriginal, TSerializable> where TSerializable : ISerializable
    {
        /// <summary>
        /// 将原始对象转换为可序列化 DTO
        /// 此方法负责将复杂对象转换为适合序列化的简单数据结构
        /// </summary>
        /// <param name="obj">原始对象</param>
        /// <returns>可序列化 DTO 对象</returns>
        TSerializable ToSerializable(TOriginal obj);

        /// <summary>
        /// 从可序列化 DTO 转换回原始对象
        /// 此方法负责从 DTO 重建完整的原始对象
        /// </summary>
        /// <param name="dto">可序列化 DTO 对象</param>
        /// <returns>原始对象</returns>
        /// <exception cref="SerializationException">当 DTO 数据不完整或无效时抛出</exception>
        TOriginal FromSerializable(TSerializable dto);

        /// <summary>
        /// 将 DTO 序列化为 JSON 字符串
        /// 通常使用 Unity 的 JsonUtility.ToJson
        /// </summary>
        /// <param name="dto">可序列化 DTO 对象</param>
        /// <returns>JSON 字符串</returns>
        string ToJson(TSerializable dto);

        /// <summary>
        /// 从 JSON 字符串反序列化为 DTO
        /// 通常使用 Unity 的 JsonUtility.FromJson
        /// </summary>
        /// <param name="json">JSON 字符串</param>
        /// <returns>可序列化 DTO 对象</returns>
        TSerializable FromJson(string json);

        /// <summary>
        /// 将原始对象直接序列化为 JSON（语法糖方法）
        /// 组合 ToSerializable 和 ToJson 两步操作
        /// </summary>
        /// <param name="obj">原始对象</param>
        /// <returns>JSON 字符串</returns>
        string SerializeToJson(TOriginal obj);

        /// <summary>
        /// 从 JSON 直接反序列化为原始对象（语法糖方法）
        /// 组合 FromJson 和 FromSerializable 两步操作
        /// </summary>
        /// <param name="json">JSON 字符串</param>
        /// <returns>原始对象</returns>
        TOriginal DeserializeFromJson(string json);
    }

    /// <summary>
    /// 类型序列化器接口（单泛型，用于向后兼容）
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
    /// 提供类型序列化器的注册管理和序列化/反序列化功能
    /// 继承自 IService 以支持架构生命周期管理
    /// </summary>
    public interface ISerializationService : IService
    {
        /// <summary>
        /// 注册双泛型类型序列化器
        /// </summary>
        /// <typeparam name="TOriginal">原始对象类型</typeparam>
        /// <typeparam name="TSerializable">可序列化 DTO 类型</typeparam>
        /// <param name="serializer">序列化器实例</param>
        void RegisterSerializer<TOriginal, TSerializable>(ITypeSerializer<TOriginal, TSerializable> serializer) 
            where TSerializable : ISerializable;

        /// <summary>
        /// 注册类型序列化器（泛型版本，用于向后兼容）
        /// </summary>
        /// <typeparam name="T">要序列化的类型</typeparam>
        /// <param name="serializer">序列化器实例</param>
        void RegisterSerializer<T>(ITypeSerializer<T> serializer);
        
        /// <summary>
        /// 注册类型序列化器（非泛型版本）
        /// </summary>
        /// <param name="serializer">序列化器实例</param>
        void RegisterSerializer(ITypeSerializer serializer);

        /// <summary>
        /// 序列化对象到 JSON 字符串（泛型版本）
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="obj">要序列化的对象</param>
        /// <returns>JSON 字符串</returns>
        string SerializeToJson<T>(T obj);
        
        /// <summary>
        /// 序列化对象到 JSON 字符串（非泛型版本）
        /// </summary>
        /// <param name="obj">要序列化的对象</param>
        /// <param name="type">对象类型</param>
        /// <returns>JSON 字符串</returns>
        string SerializeToJson(object obj, Type type);
        
        /// <summary>
        /// 从 JSON 字符串反序列化对象（泛型版本）
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="json">JSON 字符串</param>
        /// <returns>反序列化后的对象</returns>
        T DeserializeFromJson<T>(string json);
        
        /// <summary>
        /// 从 JSON 字符串反序列化对象（非泛型版本）
        /// </summary>
        /// <param name="json">JSON 字符串</param>
        /// <param name="type">目标类型</param>
        /// <returns>反序列化后的对象</returns>
        object DeserializeFromJson(string json, Type type);

        /// <summary>
        /// 序列化对象到 CustomDataEntry 列表（泛型版本）
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="obj">要序列化的对象</param>
        /// <returns>CustomDataEntry 列表</returns>
        List<CustomDataEntry> SerializeToCustomData<T>(T obj);
        
        /// <summary>
        /// 序列化对象到 CustomDataEntry 列表（非泛型版本）
        /// </summary>
        /// <param name="obj">要序列化的对象</param>
        /// <param name="type">对象类型</param>
        /// <returns>CustomDataEntry 列表</returns>
        List<CustomDataEntry> SerializeToCustomData(object obj, Type type);
        
        /// <summary>
        /// 从 CustomDataEntry 列表反序列化对象（泛型版本）
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="entries">CustomDataEntry 列表</param>
        /// <returns>反序列化后的对象</returns>
        T DeserializeFromCustomData<T>(List<CustomDataEntry> entries);
        
        /// <summary>
        /// 从 CustomDataEntry 列表反序列化对象（非泛型版本）
        /// </summary>
        /// <param name="entries">CustomDataEntry 列表</param>
        /// <param name="type">目标类型</param>
        /// <returns>反序列化后的对象</returns>
        object DeserializeFromCustomData(List<CustomDataEntry> entries, Type type);

        /// <summary>
        /// 序列化对象到二进制数据（泛型版本）
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="obj">要序列化的对象</param>
        /// <returns>二进制数据</returns>
        byte[] SerializeToBinary<T>(T obj);
        
        /// <summary>
        /// 序列化对象到二进制数据（非泛型版本）
        /// </summary>
        /// <param name="obj">要序列化的对象</param>
        /// <param name="type">对象类型</param>
        /// <returns>二进制数据</returns>
        byte[] SerializeToBinary(object obj, Type type);
        
        /// <summary>
        /// 从二进制数据反序列化对象（泛型版本）
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="data">二进制数据</param>
        /// <returns>反序列化后的对象</returns>
        T DeserializeFromBinary<T>(byte[] data);
        
        /// <summary>
        /// 从二进制数据反序列化对象（非泛型版本）
        /// </summary>
        /// <param name="data">二进制数据</param>
        /// <param name="type">目标类型</param>
        /// <returns>反序列化后的对象</returns>
        object DeserializeFromBinary(byte[] data, Type type);

        /// <summary>
        /// 检查是否已注册指定类型的序列化器
        /// </summary>
        /// <param name="type">要检查的类型</param>
        /// <returns>如果已注册返回 true，否则返回 false</returns>
        bool HasSerializer(Type type);
        
        /// <summary>
        /// 检查是否已注册指定类型的序列化器（泛型版本）
        /// </summary>
        /// <typeparam name="T">要检查的类型</typeparam>
        /// <returns>如果已注册返回 true，否则返回 false</returns>
        bool HasSerializer<T>();

        /// <summary>
        /// 获取指定类型支持的序列化策略
        /// </summary>
        /// <param name="type">要查询的类型</param>
        /// <returns>序列化策略</returns>
        SerializationStrategy GetSupportedStrategy(Type type);
        
        /// <summary>
        /// 获取指定类型支持的序列化策略（泛型版本）
        /// </summary>
        /// <typeparam name="T">要查询的类型</typeparam>
        /// <returns>序列化策略</returns>
        SerializationStrategy GetSupportedStrategy<T>();
        
        /// <summary>
        /// 获取所有已注册的序列化器（用于编辑器显示）
        /// </summary>
        /// <returns>类型到序列化器的只读字典</returns>
        IReadOnlyDictionary<Type, ITypeSerializer> GetRegisteredSerializers();
    }
}
