using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyPack
{

    [Serializable]
    public class SerializableGameProperty
    {
        public string ID;
        public float BaseValue;
        public SerializableModifierList ModifierList;
    }


    [Serializable]
    public class SerializableModifierList
    {
        public List<SerializableModifier> Modifiers = new List<SerializableModifier>();
    }

    /// <summary>
    /// GameProperty 的 JSON 序列化器
    /// 只序列化属性本身，不包括依赖关系
    /// </summary>
    public class GamePropertyJsonSerializer : JsonSerializerBase<GameProperty>
    {
        private readonly ModifierSerializer _modifierSerializer = new ModifierSerializer();

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
