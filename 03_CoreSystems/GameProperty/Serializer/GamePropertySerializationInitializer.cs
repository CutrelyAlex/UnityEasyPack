using System;
using UnityEngine;

namespace EasyPack.GamePropertySystem
{
    /// <summary>
    /// GameProperty 序列化系统初始化器
    /// 负责向 SerializationService 注册所有 GameProperty 相关的序列化器
    /// </summary>
    public static class GamePropertySerializationInitializer
    {
        /// <summary>
        /// 向序列化服务注册所有 GameProperty 相关的序列化器
        /// </summary>
        /// <param name="service">序列化服务实例</param>
        public static void RegisterSerializers(ISerializationService service)
        {
            try
            {
                RegisterModifierSerializers(service);
                RegisterGamePropertySerializers(service);
                RegisterCombinePropertySerializers(service);
                
                Debug.Log("[GameProperty] 序列化器注册完成");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameProperty] 序列化器注册失败: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 注册所有修饰符（Modifier）相关的序列化器
        /// </summary>
        private static void RegisterModifierSerializers(ISerializationService service)
        {
            // 注册IModifier序列化器
            service.RegisterSerializer(new ModifierSerializer());

            // 注册FloatModifier序列化器
            service.RegisterSerializer(new FloatModifierSerializer());

            // 注册RangeModifier序列化器
            service.RegisterSerializer(new RangeModifierSerializer());

            // 注册Modifier列表序列化器
            service.RegisterSerializer(new ModifierListSerializer());
        }

        /// <summary>
        /// 注册 GameProperty 序列化器
        /// </summary>
        private static void RegisterGamePropertySerializers(ISerializationService service)
        {
            // 注册GameProperty JSON序列化器
            service.RegisterSerializer(new GamePropertyJsonSerializer());
        }

        /// <summary>
        /// 注册 CombineProperty 序列化器（CombinePropertySingle 和 CombinePropertyCustom）
        /// </summary>
        private static void RegisterCombinePropertySerializers(ISerializationService service)
        {
            // 注册CombinePropertySingle JSON序列化器
            service.RegisterSerializer(new CombinePropertySingleJsonSerializer());

            // 注册CombinePropertyCustom JSON序列化器
            service.RegisterSerializer(new CombinePropertyCustomJsonSerializer());
        }

        /// <summary>
        /// 手动初始化序列化系统（已过时，请使用 EasyPackArchitecture 的序列化服务）
        /// </summary>
        [Obsolete("Use SerializationService via EasyPackArchitecture.Instance.Container.ResolveAsync<ISerializationService>()")]
        public static void ManualInitialize()
        {
            SerializationServiceManager.RegisterSerializer(new ModifierSerializer());
            SerializationServiceManager.RegisterSerializer(new FloatModifierSerializer());
            SerializationServiceManager.RegisterSerializer(new RangeModifierSerializer());
            SerializationServiceManager.RegisterSerializer(new ModifierListSerializer());
            SerializationServiceManager.RegisterSerializer(new GamePropertyJsonSerializer());
            SerializationServiceManager.RegisterSerializer(new CombinePropertySingleJsonSerializer());
            SerializationServiceManager.RegisterSerializer(new CombinePropertyCustomJsonSerializer());
            Debug.Log("[GameProperty] 序列化系统通过 ManualInitialize 初始化（已过时）");
        }
    }
}
