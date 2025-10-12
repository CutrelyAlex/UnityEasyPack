using System;
using UnityEngine;

namespace EasyPack
{
    /// <summary>
    /// IItemCondition 条件的 JSON 序列化器
    /// </summary>
    public class ConditionJsonSerializer : JsonSerializerBase<IItemCondition>
    {
        /// <summary>
        /// 序列化条件对象为 JSON
        /// </summary>
        public override string SerializeToJson(IItemCondition obj)
        {
            if (obj == null) return null;

            // 使用 ISerializableCondition 接口
            if (obj is ISerializableCondition serializableCondition)
            {
                var dto = serializableCondition.ToDto();
                if (dto != null)
                {
                    return JsonUtility.ToJson(dto);
                }
            }

            Debug.LogWarning($"[ConditionJsonSerializer] 条件类型 {obj.GetType().Name} 未实现 ISerializableCondition 接口");
            return null;
        }

        /// <summary>
        /// 从 JSON 反序列化条件对象
        /// </summary>
        public override IItemCondition DeserializeFromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;

            SerializedCondition dto;
            try
            {
                dto = JsonUtility.FromJson<SerializedCondition>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ConditionJsonSerializer] 反序列化失败: {e.Message}");
                return null;
            }

            if (dto == null || string.IsNullOrEmpty(dto.Kind))
                return null;

            try
            {
                // 根据 Kind 创建对应的条件实例
                ISerializableCondition condition = CreateConditionByKind(dto.Kind);

                if (condition != null)
                {
                    return condition.FromDto(dto) as IItemCondition;
                }

                Debug.LogWarning($"[ConditionJsonSerializer] 未知条件类型: {dto.Kind}");
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ConditionJsonSerializer] 条件反序列化失败 [{dto.Kind}]: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 根据 Kind 创建条件实例
        /// 此处未来需要评估是否有更灵活的注册机制，比如使用工厂模式
        /// </summary>
        private static ISerializableCondition CreateConditionByKind(string kind)
        {
            switch (kind)
            {
                case "ItemType":
                    return new ItemTypeCondition("");

                case "Attr":
                    return new AttributeCondition("", null);

                case "All":
                    return new AllCondition();

                case "Any":
                    return new AnyCondition();

                case "Not":
                    return new NotCondition();

                default:
                    Debug.LogWarning($"[ConditionJsonSerializer] 未支持的条件类型: {kind}");
                    return null;
            }
        }
    }
}
