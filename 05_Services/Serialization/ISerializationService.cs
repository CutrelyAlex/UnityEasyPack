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
    /// 提供类型序列化器的注册管理和序列化/反序列化功能
    /// 继承自 IService 以支持架构生命周期管理
    /// </summary>
    public interface ISerializationService : IService
    {
        /// <summary>
        /// 注册类型序列化器（泛型版本）
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
