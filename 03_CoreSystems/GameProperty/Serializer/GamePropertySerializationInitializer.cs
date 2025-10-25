using UnityEngine;

namespace EasyPack.GamePropertySystem
{
    /// <summary>
    /// GameProperty 序列化系统初始化器
    /// 在 Unity 运行时自动注册所有 GameProperty 相关的序列化器到 SerializationService
    /// 使用 RuntimeInitializeOnLoadMethod 确保在场景加载前完成注册
    /// </summary>
    public static class GamePropertySerializationInitializer
    {
        private static bool _isInitialized = false;
        private static readonly object _lock = new object();

        /// <summary>
        /// 在场景加载前自动注册所有序列化器
        /// Unity 自动调用此方法，无需手动调用
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

                RegisterModifierSerializers();
                RegisterGamePropertySerializers();
                RegisterCombinePropertySerializers();

                _isInitialized = true;
                Debug.Log("[GameProperty] 序列化系统初始化完成");
            }
        }

        /// <summary>
        /// 注册所有修饰符（Modifier）相关的序列化器
        /// </summary>
        private static void RegisterModifierSerializers()
        {
            // 注册IModifier序列化器
            SerializationServiceManager.RegisterSerializer(new ModifierSerializer());

            // 注册FloatModifier序列化器
            SerializationServiceManager.RegisterSerializer(new FloatModifierSerializer());

            // 注册RangeModifier序列化器
            SerializationServiceManager.RegisterSerializer(new RangeModifierSerializer());

            // 注册Modifier列表序列化器
            SerializationServiceManager.RegisterSerializer(new ModifierListSerializer());
        }

        /// <summary>
        /// 注册 GameProperty 序列化器
        /// </summary>
        private static void RegisterGamePropertySerializers()
        {
            // 注册GameProperty JSON序列化器
            SerializationServiceManager.RegisterSerializer(new GamePropertyJsonSerializer());
        }

        /// <summary>
        /// 注册 CombineProperty 序列化器（CombinePropertySingle 和 CombinePropertyCustom）
        /// </summary>
        private static void RegisterCombinePropertySerializers()
        {
            // 注册CombinePropertySingle JSON序列化器
            SerializationServiceManager.RegisterSerializer(new CombinePropertySingleJsonSerializer());

            // 注册CombinePropertyCustom JSON序列化器
            SerializationServiceManager.RegisterSerializer(new CombinePropertyCustomJsonSerializer());
        }

        /// <summary>
        /// 手动初始化序列化系统（用于测试或特殊场景）
        /// 通常无需手动调用，Unity 会自动调用 Initialize()
        /// </summary>
        public static void ManualInitialize()
        {
            Initialize();
        }
    }
}

