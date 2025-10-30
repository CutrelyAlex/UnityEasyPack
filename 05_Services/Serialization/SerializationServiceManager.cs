using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyPack
{
    /// <summary>
    /// 序列化服务全局访问管理器（已过时）
    /// 提供向后兼容的代理访问，优先使用架构服务
    /// 建议使用 EasyPackArchitecture.Instance.Container.ResolveAsync&lt;ISerializationService&gt;() 代替
    /// </summary>
    public static class SerializationServiceManager
    {
        private static ISerializationService _localInstance;
        private static readonly object _lock = new();

        /// <summary>
        /// 全局序列化服务实例（已过时）
        /// 请使用 EasyPackArchitecture.Instance.GetServiceAsync&lt;ISerializationService&gt;() 代替
        /// </summary>
        [Obsolete("Use await EasyPackArchitecture.Instance.GetServiceAsync<ISerializationService>() instead. This static manager will be removed in a future version.")]
        public static ISerializationService Instance
        {
            get
            {
                // 优先尝试从架构获取服务
                try
                {
                    var architecture = EasyPackArchitecture.Instance;
                    if (architecture != null)
                    {
                        var service = architecture.GetServiceAsync<ISerializationService>().GetAwaiter().GetResult();
                        if (service != null)
                        {
                            return service;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SerializationServiceManager] 无法从架构获取服务：{ex.Message}。回退到本地实例。");
                }

                // 降级到本地单例
                if (_localInstance == null)
                {
                    lock (_lock)
                    {
                        if (_localInstance == null)
                        {
                            _localInstance = new SerializationService();
                            // 初始化服务
                            _localInstance.InitializeAsync().GetAwaiter().GetResult();
                            Debug.LogWarning("[SerializationServiceManager] 已创建备用序列化服务实例。建议使用 EasyPackArchitecture 替代。");
                        }
                    }
                }
                return _localInstance;
            }
        }

        /// <summary>
        /// 设置自定义序列化服务实例（用于测试或扩展）（已过时）
        /// </summary>
        [Obsolete("Use EasyPackArchitecture service container for dependency injection instead.")]
        public static void SetInstance(ISerializationService customService)
        {
            lock (_lock)
            {
                if (_localInstance != null)
                {
                    Debug.LogWarning("[SerializationServiceManager] 正在替换现有的序列化服务实例");
                }
                _localInstance = customService;
            }
        }

        /// <summary>
        /// 重置为默认实例（用于测试）（已过时）
        /// </summary>
        [Obsolete("Use EasyPackArchitecture service container lifecycle management instead.")]
        public static void ResetToDefault()
        {
            lock (_lock)
            {
                _localInstance = new SerializationService();
                Debug.Log("[SerializationServiceManager] 已重置为默认序列化服务实例");
            }
        }

        #region 注册静态方法（已过时）
        
        /// <summary>
        /// 注册类型序列化器（已过时）
        /// </summary>
        [Obsolete("Use ISerializationService.RegisterSerializer via EasyPackArchitecture instead.")]
        public static void RegisterSerializer<T>(ITypeSerializer<T> serializer)
        {
            Instance.RegisterSerializer(serializer);
        }

        /// <summary>
        /// 注册类型序列化器（非泛型版本）（已过时）
        /// </summary>
        [Obsolete("Use ISerializationService.RegisterSerializer via EasyPackArchitecture instead.")]
        public static void RegisterSerializer(ITypeSerializer serializer)
        {
            Instance.RegisterSerializer(serializer);
        }

        /// <summary>
        /// 序列化对象到 JSON 字符串（已过时）
        /// </summary>
        [Obsolete("Use ISerializationService.SerializeToJson via EasyPackArchitecture instead.")]
        public static string SerializeToJson<T>(T obj)
        {
            return Instance.SerializeToJson(obj);
        }

        /// <summary>
        /// 序列化对象到 JSON 字符串（非泛型版本）（已过时）
        /// </summary>
        [Obsolete("Use ISerializationService.SerializeToJson via EasyPackArchitecture instead.")]
        public static string SerializeToJson(object obj, Type type)
        {
            return Instance.SerializeToJson(obj, type);
        }

        /// <summary>
        /// 从 JSON 字符串反序列化对象（已过时）
        /// </summary>
        [Obsolete("Use ISerializationService.DeserializeFromJson via EasyPackArchitecture instead.")]
        public static T DeserializeFromJson<T>(string json)
        {
            return Instance.DeserializeFromJson<T>(json);
        }

        /// <summary>
        /// 从 JSON 字符串反序列化对象（非泛型版本）（已过时）
        /// </summary>
        [Obsolete("Use ISerializationService.DeserializeFromJson via EasyPackArchitecture instead.")]
        public static object DeserializeFromJson(string json, Type type)
        {
            return Instance.DeserializeFromJson(json, type);
        }

        /// <summary>
        /// 序列化对象到 CustomDataEntry 列表（已过时）
        /// </summary>
        [Obsolete("Use ISerializationService.SerializeToCustomData via EasyPackArchitecture instead.")]
        public static List<CustomDataEntry> SerializeToCustomData<T>(T obj)
        {
            return Instance.SerializeToCustomData(obj);
        }

        /// <summary>
        /// 序列化对象到 CustomDataEntry 列表（非泛型版本）（已过时）
        /// </summary>
        [Obsolete("Use ISerializationService.SerializeToCustomData via EasyPackArchitecture instead.")]
        public static List<CustomDataEntry> SerializeToCustomData(object obj, Type type)
        {
            return Instance.SerializeToCustomData(obj, type);
        }

        /// <summary>
        /// 从 CustomDataEntry 列表反序列化对象（已过时）
        /// </summary>
        [Obsolete("Use ISerializationService.DeserializeFromCustomData via EasyPackArchitecture instead.")]
        public static T DeserializeFromCustomData<T>(List<CustomDataEntry> entries)
        {
            return Instance.DeserializeFromCustomData<T>(entries);
        }

        /// <summary>
        /// 从 CustomDataEntry 列表反序列化对象（非泛型版本）（已过时）
        /// </summary>
        [Obsolete("Use ISerializationService.DeserializeFromCustomData via EasyPackArchitecture instead.")]
        public static object DeserializeFromCustomData(List<CustomDataEntry> entries, Type type)
        {
            return Instance.DeserializeFromCustomData(entries, type);
        }

        /// <summary>
        /// 检查类型是否已注册序列化器（已过时）
        /// </summary>
        [Obsolete("Use ISerializationService.HasSerializer via EasyPackArchitecture instead.")]
        public static bool HasSerializer(Type type)
        {
            return Instance.HasSerializer(type);
        }

        /// <summary>
        /// 检查类型是否已注册序列化器（泛型版本）（已过时）
        /// </summary>
        [Obsolete("Use ISerializationService.HasSerializer via EasyPackArchitecture instead.")]
        public static bool HasSerializer<T>()
        {
            return Instance.HasSerializer<T>();
        }

        /// <summary>
        /// 获取类型支持的序列化策略（已过时）
        /// </summary>
        [Obsolete("Use ISerializationService.GetSupportedStrategy via EasyPackArchitecture instead.")]
        public static SerializationStrategy GetSupportedStrategy(Type type)
        {
            return Instance.GetSupportedStrategy(type);
        }

        /// <summary>
        /// 获取类型支持的序列化策略（泛型版本）（已过时）
        /// </summary>
        [Obsolete("Use ISerializationService.GetSupportedStrategy via EasyPackArchitecture instead.")]
        public static SerializationStrategy GetSupportedStrategy<T>()
        {
            return Instance.GetSupportedStrategy<T>();
        }

        #endregion
    }
}
