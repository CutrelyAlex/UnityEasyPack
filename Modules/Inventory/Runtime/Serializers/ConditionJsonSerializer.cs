using System;
using System.Collections.Generic;
using EasyPack.Serialization;
using UnityEngine;

namespace EasyPack.InventorySystem
{
    /// <summary>
    ///     条件的 JSON 序列化器
    /// </summary>
    public class ConditionJsonSerializer : ITypeSerializer<IItemCondition, SerializedCondition>
    {
        /// <summary>
        ///     Kind 到条件类型的映射表
        /// </summary>
        private static readonly Dictionary<string, Type> _kindToType = new()
        {
            ["ItemType"] = typeof(ItemTypeCondition),
            ["Attr"] = typeof(AttributeCondition),
            ["All"] = typeof(AllCondition),
            ["Any"] = typeof(AnyCondition),
            ["Not"] = typeof(NotCondition),
        };

        /// <summary>
        ///     将条件对象转换为可序列化的 DTO
        /// </summary>
        public SerializedCondition ToSerializable(IItemCondition condition)
        {
            if (condition == null)
                return null;

            if (condition is not ISerializableCondition serializableCondition)
            {
                Debug.LogWarning(
                    $"[ConditionJsonSerializer] 条件类型 {condition.GetType().Name} 未实现 ISerializableCondition 接口");
                return null;
            }

            return serializableCondition.ToDto();
        }

        /// <summary>
        ///     从 DTO 转换回条件对象
        /// </summary>
        public IItemCondition FromSerializable(SerializedCondition dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.Kind))
            {
                Debug.LogWarning("[ConditionJsonSerializer] 无效的条件 DTO");
                return null;
            }

            // 根据 Kind 查找对应的条件类型
            if (!_kindToType.TryGetValue(dto.Kind, out Type conditionType))
            {
                Debug.LogWarning($"[ConditionJsonSerializer] 未注册的条件类型: {dto.Kind}");
                return null;
            }

            // 创建条件实例并反序列化
            try
            {
                if (Activator.CreateInstance(conditionType) is not ISerializableCondition instance)
                {
                    Debug.LogError($"[ConditionJsonSerializer] 无法创建条件实例: {conditionType.Name}");
                    return null;
                }

                return instance.FromDto(dto);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConditionJsonSerializer] 反序列化条件失败 ({dto.Kind}): {ex.Message}");
                return null;
            }
        }

        /// <summary>
        ///     将 DTO 序列化为 JSON 字符串
        /// </summary>
        public string ToJson(SerializedCondition dto) => dto == null ? null : JsonUtility.ToJson(dto);

        /// <summary>
        ///     从 JSON 字符串反序列化为 DTO
        /// </summary>
        public SerializedCondition FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return null;

            try
            {
                return JsonUtility.FromJson<SerializedCondition>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConditionJsonSerializer] JSON 解析失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        ///     将条件对象序列化为 JSON 字符串
        /// </summary>
        public string SerializeToJson(IItemCondition condition)
        {
            SerializedCondition dto = ToSerializable(condition);
            return ToJson(dto);
        }

        /// <summary>
        ///     从 JSON 字符串反序列化为条件对象
        /// </summary>
        public IItemCondition DeserializeFromJson(string json)
        {
            SerializedCondition dto = FromJson(json);
            return FromSerializable(dto);
        }

        /// <summary>
        ///     注册自定义条件类型
        /// </summary>
        /// <param name="kind">条件的 Kind 标识</param>
        /// <param name="conditionType">条件的具体类型</param>
        public static void RegisterConditionType(string kind, Type conditionType)
        {
            if (string.IsNullOrEmpty(kind))
            {
                Debug.LogError("[ConditionJsonSerializer] Kind 不能为空");
                return;
            }

            if (!typeof(ISerializableCondition).IsAssignableFrom(conditionType))
            {
                Debug.LogError($"[ConditionJsonSerializer] 类型 {conditionType.Name} 必须实现 ISerializableCondition 接口");
                return;
            }

            _kindToType[kind] = conditionType;
            Debug.Log($"[ConditionJsonSerializer] 已注册条件类型: {kind} -> {conditionType.Name}");
        }

        /// <summary>
        ///     检查 Kind 是否已注册
        /// </summary>
        public static bool IsRegistered(string kind) => !string.IsNullOrEmpty(kind) && _kindToType.ContainsKey(kind);

        /// <summary>
        ///     获取所有已注册的 Kind
        /// </summary>
        public static IEnumerable<string> GetRegisteredKinds() => _kindToType.Keys;
    }
}