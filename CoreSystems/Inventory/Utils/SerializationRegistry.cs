using EasyPack;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyPack
{
    /// <summary>
    /// 序列化注册器 - 管理容器和条件的序列化/反序列化
    /// </summary>
    public static class SerializationRegistry
    {
        #region 容器工厂注册

        private static readonly Dictionary<string, Func<SerializedContainer, Container>> _containerFactories 
            = new Dictionary<string, Func<SerializedContainer, Container>>();

        /// <summary>
        /// 注册容器类型工厂
        /// </summary>
        public static void RegisterContainerFactory<T>(string kind, Func<SerializedContainer, T> factory) 
            where T : Container
        {
            if (string.IsNullOrEmpty(kind))
            {
                Debug.LogWarning("容器类型名称不能为空");
                return;
            }

            if (factory == null)
            {
                Debug.LogWarning($"容器工厂不能为null: {kind}");
                return;
            }

            _containerFactories[kind] = dto => factory(dto);
            Debug.Log($"注册容器类型: {kind}");
        }

        /// <summary>
        /// 创建容器实例
        /// </summary>
        public static Container CreateContainer(SerializedContainer dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.ContainerKind))
            {
                Debug.LogWarning("容器DTO或类型名称为空");
                return null;
            }

            if (_containerFactories.TryGetValue(dto.ContainerKind, out var factory))
            {
                try
                {
                    return factory(dto);
                }
                catch (Exception e)
                {
                    Debug.LogError($"创建容器失败 [{dto.ContainerKind}]: {e.Message}");
                    return null;
                }
            }

            Debug.LogWarning($"未注册的容器类型: {dto.ContainerKind}，尝试使用默认容器");
            
            // 默认回退到LinerContainer
            if (_containerFactories.TryGetValue("LinerContainer", out var defaultFactory))
            {
                return defaultFactory(dto);
            }

            return null;
        }

        /// <summary>
        /// 检查容器类型是否已注册
        /// </summary>
        public static bool IsContainerTypeRegistered(string kind)
        {
            return !string.IsNullOrEmpty(kind) && _containerFactories.ContainsKey(kind);
        }

        /// <summary>
        /// 获取所有已注册的容器类型
        /// </summary>
        public static IEnumerable<string> GetRegisteredContainerTypes()
        {
            return _containerFactories.Keys;
        }

        #endregion

        #region 条件序列化器注册

        private static readonly Dictionary<string, IConditionSerializer> _conditionSerializers 
            = new Dictionary<string, IConditionSerializer>();

        /// <summary>
        /// 注册条件序列化器
        /// </summary>
        public static void RegisterConditionSerializer(IConditionSerializer serializer)
        {
            if (serializer == null)
            {
                Debug.LogWarning("条件序列化器不能为null");
                return;
            }

            if (string.IsNullOrEmpty(serializer.Kind))
            {
                Debug.LogWarning("条件序列化器Kind不能为空");
                return;
            }

            _conditionSerializers[serializer.Kind] = serializer;
            Debug.Log($"注册条件序列化器: {serializer.Kind}");
        }

        /// <summary>
        /// 序列化条件
        /// </summary>
        public static SerializedCondition SerializeCondition(IItemCondition condition)
        {
            if (condition == null) return null;

            // 优先使用自序列化接口
            if (condition is ISerializableCondition serializableCondition)
            {
                return serializableCondition.ToDto();
            }

            // 尝试查找注册的序列化器
            foreach (var serializer in _conditionSerializers.Values)
            {
                if (serializer.CanHandle(condition))
                {
                    try
                    {
                        return serializer.Serialize(condition);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"条件序列化失败 [{condition.GetType().Name}]: {e.Message}");
                        return null;
                    }
                }
            }

            Debug.LogWarning($"未找到适合的序列化器: {condition.GetType().Name}");
            return null;
        }

        /// <summary>
        /// 反序列化条件
        /// </summary>
        public static IItemCondition DeserializeCondition(SerializedCondition dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.Kind))
            {
                return null;
            }

            if (_conditionSerializers.TryGetValue(dto.Kind, out var serializer))
            {
                try
                {
                    return serializer.Deserialize(dto);
                }
                catch (Exception e)
                {
                    Debug.LogError($"条件反序列化失败 [{dto.Kind}]: {e.Message}");
                    return null;
                }
            }

            Debug.LogWarning($"未注册的条件类型: {dto.Kind}");
            return null;
        }

        /// <summary>
        /// 检查条件类型是否已注册
        /// </summary>
        public static bool IsConditionTypeRegistered(string kind)
        {
            return !string.IsNullOrEmpty(kind) && _conditionSerializers.ContainsKey(kind);
        }

        /// <summary>
        /// 获取所有已注册的条件类型
        /// </summary>
        public static IEnumerable<string> GetRegisteredConditionTypes()
        {
            return _conditionSerializers.Keys;
        }

        #endregion

        #region 初始化

        private static bool _initialized = false;

        /// <summary>
        /// 初始化注册器（注册所有内置类型）
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            Debug.Log("=== 初始化序列化注册器 ===");

            // 注册容器类型
            RegisterContainerFactory("LinerContainer", dto => 
                new LinerContainer(dto.ID, dto.Name, dto.Type, dto.Capacity));

            // 注册条件序列化器
            RegisterConditionSerializer(new ItemTypeConditionSerializer());
            RegisterConditionSerializer(new AttributeConditionSerializer());
            RegisterConditionSerializer(new AllConditionSerializer());
            RegisterConditionSerializer(new AnyConditionSerializer());
            RegisterConditionSerializer(new NotConditionSerializer());

            _initialized = true;
            Debug.Log("=== 序列化注册器初始化完成 ===");
        }

        /// <summary>
        /// 确保已初始化
        /// </summary>
        internal static void EnsureInitialized()
        {
            if (!_initialized)
            {
                Initialize();
            }
        }

        /// <summary>
        /// 清空所有注册（仅用于测试）
        /// </summary>
        public static void Clear()
        {
            _containerFactories.Clear();
            _conditionSerializers.Clear();
            _initialized = false;
            Debug.Log("序列化注册器已清空");
        }

        #endregion
    }
}
