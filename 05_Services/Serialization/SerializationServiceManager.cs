using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyPack
{
    /// <summary>
    /// 序列化服务全局访问管理器
    /// 提供单例访问和便捷的静态方法
    /// </summary>
    public static class SerializationServiceManager
    {
        private static ISerializationService _instance;
        private static readonly object _lock = new();

        /// <summary>
        /// 全局序列化服务实例
        /// </summary>
        public static ISerializationService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new SerializationService();
                            Debug.Log("[SerializationServiceManager] Created new SerializationService instance");
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 设置自定义序列化服务实例（用于测试或扩展）
        /// </summary>
        public static void SetInstance(ISerializationService customService)
        {
            lock (_lock)
            {
                if (_instance != null)
                {
                    Debug.LogWarning("[SerializationServiceManager] Replacing existing SerializationService instance");
                }
                _instance = customService;
            }
        }

        /// <summary>
        /// 重置为默认实例（用于测试）
        /// </summary>
        public static void ResetToDefault()
        {
            lock (_lock)
            {
                _instance = new SerializationService();
                Debug.Log("[SerializationServiceManager] Reset to default SerializationService instance");
            }
        }

        #region 注册静态方法
        /// <summary>
        /// 注册类型序列化器
        /// </summary>
        public static void RegisterSerializer<T>(ITypeSerializer<T> serializer)
        {
            Instance.RegisterSerializer(serializer);
        }

        /// <summary>
        /// 注册类型序列化器（非泛型版本）
        /// </summary>
        public static void RegisterSerializer(ITypeSerializer serializer)
        {
            Instance.RegisterSerializer(serializer);
        }

        /// <summary>
        /// 序列化对象到 JSON 字符串
        /// </summary>
        public static string SerializeToJson<T>(T obj)
        {
            return Instance.SerializeToJson(obj);
        }

        /// <summary>
        /// 序列化对象到 JSON 字符串（非泛型版本）
        /// </summary>
        public static string SerializeToJson(object obj, Type type)
        {
            return Instance.SerializeToJson(obj, type);
        }

        /// <summary>
        /// 从 JSON 字符串反序列化对象
        /// </summary>
        public static T DeserializeFromJson<T>(string json)
        {
            return Instance.DeserializeFromJson<T>(json);
        }

        /// <summary>
        /// 从 JSON 字符串反序列化对象（非泛型版本）
        /// </summary>
        public static object DeserializeFromJson(string json, Type type)
        {
            return Instance.DeserializeFromJson(json, type);
        }

        /// <summary>
        /// 序列化对象到 CustomDataEntry 列表
        /// </summary>
        public static List<CustomDataEntry> SerializeToCustomData<T>(T obj)
        {
            return Instance.SerializeToCustomData(obj);
        }

        /// <summary>
        /// 序列化对象到 CustomDataEntry 列表（非泛型版本）
        /// </summary>
        public static List<CustomDataEntry> SerializeToCustomData(object obj, Type type)
        {
            return Instance.SerializeToCustomData(obj, type);
        }

        /// <summary>
        /// 从 CustomDataEntry 列表反序列化对象
        /// </summary>
        public static T DeserializeFromCustomData<T>(List<CustomDataEntry> entries)
        {
            return Instance.DeserializeFromCustomData<T>(entries);
        }

        /// <summary>
        /// 从 CustomDataEntry 列表反序列化对象（非泛型版本）
        /// </summary>
        public static object DeserializeFromCustomData(List<CustomDataEntry> entries, Type type)
        {
            return Instance.DeserializeFromCustomData(entries, type);
        }

        /// <summary>
        /// 检查类型是否已注册序列化器
        /// </summary>
        public static bool HasSerializer(Type type)
        {
            return Instance.HasSerializer(type);
        }

        /// <summary>
        /// 检查类型是否已注册序列化器（泛型版本）
        /// </summary>
        public static bool HasSerializer<T>()
        {
            return Instance.HasSerializer<T>();
        }

        /// <summary>
        /// 获取类型支持的序列化策略
        /// </summary>
        public static SerializationStrategy GetSupportedStrategy(Type type)
        {
            return Instance.GetSupportedStrategy(type);
        }

        /// <summary>
        /// 获取类型支持的序列化策略（泛型版本）
        /// </summary>
        public static SerializationStrategy GetSupportedStrategy<T>()
        {
            return Instance.GetSupportedStrategy<T>();
        }

        #endregion
    }
}
