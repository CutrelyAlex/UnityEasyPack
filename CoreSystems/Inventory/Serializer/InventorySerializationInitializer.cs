using System;
using UnityEngine;
using EasyPack;

namespace EasyPack
{
    /// <summary>
    /// Inventory序列化系统初始化器
    /// </summary>
    public static class InventorySerializationInitializer
    {
        private static bool _isInitialized = false;
        private static readonly object _lock = new();

        /// <summary>
        /// 在场景加载前自动注册所有序列化器
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            lock (_lock)
            {
                if (_isInitialized)
                {
                    return;
                }

                RegisterItemSerializers();
                RegisterContainerSerializers();
                RegisterConditionSerializers();

                _isInitialized = true;
                Debug.Log("[Inventory] 序列化系统初始化完成");
            }
        }

        /// <summary>
        /// 注册物品序列化器
        /// </summary>
        private static void RegisterItemSerializers()
        {
            SerializationServiceManager.RegisterSerializer(new ItemJsonSerializer());
            SerializationServiceManager.RegisterSerializer(new GridItemJsonSerializer());
        }

        /// <summary>
        /// 注册容器序列化器
        /// </summary>
        private static void RegisterContainerSerializers()
        {
            // 注册基类容器序列化器
            SerializationServiceManager.RegisterSerializer(new ContainerJsonSerializer());
            // 注册网格容器专用序列化器
            SerializationServiceManager.RegisterSerializer(new GridContainerJsonSerializer());
        }

        /// <summary>
        /// 注册条件序列化器
        /// </summary>
        private static void RegisterConditionSerializers()
        {
            RegisterConditionSerializer<ItemTypeCondition>("ItemType");
            RegisterConditionSerializer<AttributeCondition>("Attr");
            RegisterConditionSerializer<AllCondition>("All");
            RegisterConditionSerializer<AnyCondition>("Any");
            RegisterConditionSerializer<NotCondition>("Not");
        }

        /// <summary>
        /// 注册单个条件序列化器的辅助方法
        /// </summary>
        /// <typeparam name="T">条件类型</typeparam>
        /// <param name="kind">条件的 Kind 标识</param>
        private static void RegisterConditionSerializer<T>(string kind)
            where T : ISerializableCondition, new()
        {
            SerializationServiceManager.RegisterSerializer(new SerializableConditionJsonSerializer<T>());
            ConditionTypeRegistry.RegisterConditionType(kind, typeof(T));
        }

        /// <summary>
        /// 手动初始化
        /// </summary>
        public static void ManualInitialize()
        {
            Initialize();
        }

        /// <summary>
        /// 检查是否已初始化
        /// </summary>
        public static bool IsInitialized => _isInitialized;
    }
}
