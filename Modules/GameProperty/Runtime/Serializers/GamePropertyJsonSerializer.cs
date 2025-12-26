using System;
using System.Collections.Generic;
using EasyPack.Modifiers;
using EasyPack.Serialization;
using UnityEngine;

namespace EasyPack.GamePropertySystem
{
    /// <summary>
    ///     可序列化的 GameProperty 数据结构，用于 JSON 序列化
    /// </summary>
    [Serializable]
    public class SerializableGameProperty : ISerializable
    {
        /// <summary>
        ///     属性的唯一标识符
        /// </summary>
        public string ID;

        /// <summary>
        ///     属性的唯一 UID。
        ///     -1 表示未分配。
        /// </summary>
        public long UID;

        /// <summary>
        ///     属性的基础值
        /// </summary>
        public float BaseValue;

        /// <summary>
        ///     属性的修饰符列表
        /// </summary>
        public SerializableModifier[] Modifiers = Array.Empty<SerializableModifier>();
    }

    /// <summary>
    ///     GameProperty 的 JSON 序列化器
    ///     实现双泛型接口，将 GameProperty 与 SerializableGameProperty DTO 互转
    ///     序列化属性的 ID、基础值和修饰符列表，不包括依赖关系
    /// </summary>
    public class GamePropertyJsonSerializer : ITypeSerializer<GameProperty, SerializableGameProperty>
    {
        private readonly ModifierSerializer _modifierSerializer = new();

        /// <summary>
        ///     将 GameProperty 对象转换为可序列化的 DTO
        /// </summary>
        /// <param name="obj">GameProperty 对象</param>
        /// <returns>SerializableGameProperty DTO</returns>
        public SerializableGameProperty ToSerializable(GameProperty obj)
        {
            if (obj == null) return null;

            var modifiersList = new List<SerializableModifier>();

            foreach (IModifier modifier in obj.Modifiers)
            {
                SerializableModifier serMod = _modifierSerializer.ToSerializable(modifier);
                if (serMod != null) modifiersList.Add(serMod);
            }

            return new()
            {
                ID = obj.ID, UID = obj.UID, BaseValue = obj.GetBaseValue(), Modifiers = modifiersList.ToArray(),
            };
        }

        /// <summary>
        ///     从可序列化 DTO 转换回 GameProperty 对象
        /// </summary>
        /// <param name="dto">SerializableGameProperty DTO</param>
        /// <returns>GameProperty 对象</returns>
        public GameProperty FromSerializable(SerializableGameProperty dto)
        {
            if (dto == null) return null;

            var property = new GameProperty(dto.ID, dto.BaseValue)
            {
                // UID 由 Service 统一分配与校验；
                // 这里仅还原序列化值。
                UID = dto.UID == 0 ? -1 : dto.UID,
            };

            // 使用 ModifierSerializer 还原所有修饰器
            if (dto.Modifiers != null)
            {
                foreach (SerializableModifier serMod in dto.Modifiers)
                {
                    string modifierJson = JsonUtility.ToJson(serMod);
                    IModifier modifier = _modifierSerializer.DeserializeFromJson(modifierJson);
                    if (modifier != null) property.AddModifier(modifier);
                }
            }

            return property;
        }

        /// <summary>
        ///     将 DTO 序列化为 JSON 字符串
        /// </summary>
        public string ToJson(SerializableGameProperty dto) => dto == null ? null : JsonUtility.ToJson(dto);

        /// <summary>
        ///     从 JSON 字符串反序列化为 DTO
        /// </summary>
        public SerializableGameProperty FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            return JsonUtility.FromJson<SerializableGameProperty>(json);
        }

        /// <summary>
        ///     将 GameProperty 对象序列化为 JSON 字符串
        /// </summary>
        /// <param name="gameProperty">要序列化的 GameProperty 对象</param>
        /// <returns>JSON 字符串，如果对象为 null 则返回 null</returns>
        public string SerializeToJson(GameProperty gameProperty)
        {
            SerializableGameProperty dto = ToSerializable(gameProperty);
            return ToJson(dto);
        }

        /// <summary>
        ///     从 JSON 字符串反序列化为 GameProperty 对象
        /// </summary>
        /// <param name="json">JSON 字符串</param>
        /// <returns>反序列化的 GameProperty 对象，如果 JSON 无效则返回 null</returns>
        public GameProperty DeserializeFromJson(string json)
        {
            SerializableGameProperty dto = FromJson(json);
            return FromSerializable(dto);
        }
    }
}