using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyPack
{
    /// <summary>
    /// 可序列化的 GameProperty 数据结构，用于 JSON 序列化
    /// </summary>
    [Serializable]
    public class SerializableGameProperty
    {
        /// <summary>
        /// 属性的唯一标识符
        /// </summary>
        public string ID;

        /// <summary>
        /// 属性的基础值
        /// </summary>
        public float BaseValue;

        /// <summary>
        /// 属性的修饰符列表
        /// </summary>
        public SerializableModifierList ModifierList;
    }

    /// <summary>
    /// 可序列化的修饰符列表包装器
    /// </summary>
    [Serializable]
    public class SerializableModifierList
    {
        /// <summary>
        /// 修饰符集合
        /// </summary>
        public List<SerializableModifier> Modifiers = new List<SerializableModifier>();
    }

    /// <summary>
    /// GameProperty 的 JSON 序列化器
    /// 序列化属性的 ID、基础值和修饰符列表，不包括依赖关系
    /// </summary>
    public class GamePropertyJsonSerializer : JsonSerializerBase<GameProperty>
    {
        private readonly ModifierSerializer _modifierSerializer = new ModifierSerializer();

        /// <summary>
        /// 将 GameProperty 对象序列化为 JSON 字符串
        /// </summary>
        /// <param name="obj">要序列化的 GameProperty 对象</param>
        /// <returns>JSON 字符串，如果对象为 null 则返回 null</returns>
        public override string SerializeToJson(GameProperty obj)
        {
            if (obj == null) return null;

            var data = new SerializableGameProperty
            {
                ID = obj.ID,
                BaseValue = obj.GetBaseValue(),
                ModifierList = new SerializableModifierList()
            };

            // 使用 ModifierSerializer 序列化所有修饰器
            foreach (var modifier in obj.Modifiers)
            {
                string modifierJson = _modifierSerializer.SerializeToJson(modifier);
                if (!string.IsNullOrEmpty(modifierJson))
                {
                    var serMod = JsonUtility.FromJson<SerializableModifier>(modifierJson);
                    data.ModifierList.Modifiers.Add(serMod);
                }
            }

            return JsonUtility.ToJson(data);
        }

        /// <summary>
        /// 从 JSON 字符串反序列化为 GameProperty 对象
        /// </summary>
        /// <param name="json">JSON 字符串</param>
        /// <returns>反序列化的 GameProperty 对象，如果 JSON 无效则返回 null</returns>
        public override GameProperty DeserializeFromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;

            var data = JsonUtility.FromJson<SerializableGameProperty>(json);
            if (data == null) return null;

            var property = new GameProperty(data.ID, data.BaseValue);

            // 使用 ModifierSerializer 还原所有修饰器
            if (data.ModifierList != null && data.ModifierList.Modifiers != null)
            {
                foreach (var serMod in data.ModifierList.Modifiers)
                {
                    string modifierJson = JsonUtility.ToJson(serMod);
                    IModifier modifier = _modifierSerializer.DeserializeFromJson(modifierJson);
                    if (modifier != null)
                    {
                        property.AddModifier(modifier);
                    }
                }
            }

            return property;
        }
    }
}
