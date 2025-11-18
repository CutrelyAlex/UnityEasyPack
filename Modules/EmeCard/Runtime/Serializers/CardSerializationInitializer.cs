using EasyPack.Serialization;
using System;
using UnityEngine;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    /// EmeCard 序列化系统初始化器
    /// 负责向 SerializationService 注册 Card 序列化器
    /// </summary>
    public static class CardSerializationInitializer
    {
        /// <summary>
        /// 向序列化服务注册所有 Card 相关的序列化器
        /// </summary>
        /// <param name="service">序列化服务实例</param>
        public static void RegisterSerializers(ISerializationService service)
        {
            try
            {
                service.RegisterSerializer(new CardJsonSerializer());
                Debug.Log("[EmeCard] Card 序列化器注册完成");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EmeCard] Card 序列化器注册失败: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
