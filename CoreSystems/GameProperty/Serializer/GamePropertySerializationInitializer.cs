using UnityEngine;
using EasyPack;

namespace EasyPack
{
    /// <summary>
    /// GameProperty序列化系统初始化器
    /// 在Unity运行时自动注册所有GameProperty相关的序列化器
    /// </summary>
    public static class GamePropertySerializationInitializer
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

                RegisterModifierSerializers();
                RegisterGamePropertySerializers();
                RegisterCombinePropertySerializers();
                
                _isInitialized = true;
                Debug.Log("[GameProperty] 序列化系统初始化完成");
            }
        }

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

        private static void RegisterGamePropertySerializers()
        {
            // 注册GameProperty JSON序列化器
            SerializationServiceManager.RegisterSerializer(new GamePropertyJsonSerializer());
        }

        private static void RegisterCombinePropertySerializers()
        {
            // 注册CombinePropertySingle JSON序列化器
            SerializationServiceManager.RegisterSerializer(new CombinePropertySingleJsonSerializer());
            
            // 注册CombinePropertyCustom JSON序列化器
            SerializationServiceManager.RegisterSerializer(new CombinePropertyCustomJsonSerializer());
        }


        public static void ManualInitialize()
        {
            Initialize();
        }
    }
}
