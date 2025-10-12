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
        private static readonly object _lock = new object();

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
        }

        /// <summary>
        /// 注册容器序列化器
        /// </summary>
        private static void RegisterContainerSerializers()
        {
            // 只需要为基类 Container 注册序列化器
            // SerializationService 会自动查找继承类型（如 LinerContainer, GridContainer）的基类序列化器
            SerializationServiceManager.RegisterSerializer(new ContainerJsonSerializer());
        }

        /// <summary>
        /// 注册条件序列化器
        /// </summary>
        private static void RegisterConditionSerializers()
        {
            // 注册独立的条件序列化器
            SerializationServiceManager.RegisterSerializer(new ConditionJsonSerializer());
        }

        /// <summary>
        /// 手动初始化（用于测试或特殊场景）
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
