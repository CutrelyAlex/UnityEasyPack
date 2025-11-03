using System;
using UnityEngine;

namespace EasyPack.InventorySystem
{
    /// <summary>
    /// Inventory 序列化系统初始化器
    /// 负责向 SerializationService 注册所有 Inventory 相关的序列化器
    /// </summary>
    public static class InventorySerializationInitializer
    {
        /// <summary>
        /// 向序列化服务注册所有 Inventory 相关的序列化器
        /// </summary>
        /// <param name="service">序列化服务实例</param>
        public static void RegisterSerializers(ISerializationService service)
        {
            try
            {
                RegisterItemSerializers(service);
                RegisterContainerSerializers(service);
                RegisterConditionSerializers(service);
                
                Debug.Log("[Inventory] 序列化器注册完成");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Inventory] 序列化器注册失败: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 注册物品序列化器
        /// </summary>
        private static void RegisterItemSerializers(ISerializationService service)
        {
            service.RegisterSerializer(new ItemJsonSerializer());
            service.RegisterSerializer(new GridItemJsonSerializer());
        }

        /// <summary>
        /// 注册容器序列化器
        /// </summary>
        private static void RegisterContainerSerializers(ISerializationService service)
        {
            // 注册基类容器序列化器
            service.RegisterSerializer(new ContainerJsonSerializer());
            // 注册网格容器专用序列化器
            service.RegisterSerializer(new GridContainerJsonSerializer());
        }

        /// <summary>
        /// 注册条件序列化器
        /// </summary>
        private static void RegisterConditionSerializers(ISerializationService service)
        {
            RegisterConditionSerializer<ItemTypeCondition>(service, "ItemType");
            RegisterConditionSerializer<AttributeCondition>(service, "Attr");
            RegisterConditionSerializer<AllCondition>(service, "All");
            RegisterConditionSerializer<AnyCondition>(service, "Any");
            RegisterConditionSerializer<NotCondition>(service, "Not");
        }

        /// <summary>
        /// 注册单个条件序列化器的辅助方法
        /// </summary>
        /// <typeparam name="T">条件类型</typeparam>
        /// <param name="service">序列化服务实例</param>
        /// <param name="kind">条件的 Kind 标识</param>
        private static void RegisterConditionSerializer<T>(ISerializationService service, string kind)
            where T : ISerializableCondition, new()
        {
            service.RegisterSerializer(new SerializableConditionJsonSerializer<T>());
            ConditionTypeRegistry.RegisterConditionType(kind, typeof(T));
        }

        /// <summary>
        /// 手动初始化序列化系统（已过时，请使用 EasyPackArchitecture 的序列化服务）
        /// </summary>
        [Obsolete("Use SerializationService via EasyPackArchitecture.Instance.Container.ResolveAsync<ISerializationService>()")]
        public static void ManualInitialize()
        {
            SerializationServiceManager.RegisterSerializer(new ItemJsonSerializer());
            SerializationServiceManager.RegisterSerializer(new GridItemJsonSerializer());
            SerializationServiceManager.RegisterSerializer(new ContainerJsonSerializer());
            SerializationServiceManager.RegisterSerializer(new GridContainerJsonSerializer());
            
            RegisterConditionSerializerOld<ItemTypeCondition>("ItemType");
            RegisterConditionSerializerOld<AttributeCondition>("Attr");
            RegisterConditionSerializerOld<AllCondition>("All");
            RegisterConditionSerializerOld<AnyCondition>("Any");
            RegisterConditionSerializerOld<NotCondition>("Not");
            
            Debug.Log("[Inventory] 序列化系统通过 ManualInitialize 初始化（已过时）");
        }

        private static void RegisterConditionSerializerOld<T>(string kind)
            where T : ISerializableCondition, new()
        {
            SerializationServiceManager.RegisterSerializer(new SerializableConditionJsonSerializer<T>());
            ConditionTypeRegistry.RegisterConditionType(kind, typeof(T));
        }
    }
}
