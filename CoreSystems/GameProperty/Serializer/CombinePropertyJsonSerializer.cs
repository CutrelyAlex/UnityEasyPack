using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyPack
{
    /// <summary>
    /// CombinePropertySingle 的序列化数据
    /// </summary>
    [Serializable]
    public class SerializableCombinePropertySingle
    {
        public string ID;
        public float BaseValue;
        public SerializableGameProperty ResultHolder;
    }

    /// <summary>
    /// CombinePropertyCustom 的序列化数据
    /// </summary>
    [Serializable]
    public class SerializableCombinePropertyCustom
    {
        public string ID;
        public float BaseValue;
        public SerializableGameProperty ResultHolder;
        public List<string> RegisteredPropertyIDs = new List<string>();
        public List<SerializableGameProperty> RegisteredProperties = new List<SerializableGameProperty>();
    }

    /// <summary>
    /// CombinePropertySingle 的 JSON 序列化器
    /// </summary>
    public class CombinePropertySingleJsonSerializer : JsonSerializerBase<CombinePropertySingle>
    {
        private readonly ModifierSerializer _modifierSerializer = new ModifierSerializer();

        public override string SerializeToJson(CombinePropertySingle obj)
        {
            if (obj == null) return null;

            var data = new SerializableCombinePropertySingle
            {
                ID = obj.ID,
                BaseValue = obj.GetBaseValue(),
                ResultHolder = SerializeGameProperty(obj.ResultHolder)
            };

            return JsonUtility.ToJson(data);
        }

        public override CombinePropertySingle DeserializeFromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;

            var data = JsonUtility.FromJson<SerializableCombinePropertySingle>(json);
            if (data == null) return null;

            // 创建 CombinePropertySingle
            var single = new CombinePropertySingle(data.ID, data.BaseValue);

            // 反序列化 ResultHolder 的修饰器并应用到 single 的 ResultHolder
            if (data.ResultHolder != null && data.ResultHolder.ModifierList != null)
            {
                var modifierListData = data.ResultHolder.ModifierList;
                if (modifierListData.Modifiers != null)
                {
                    foreach (var serMod in modifierListData.Modifiers)
                    {
                        IModifier modifier;
                        if (serMod.IsRangeModifier)
                        {
                            modifier = new RangeModifier(serMod.Type, serMod.Priority, serMod.RangeValue);
                        }
                        else
                        {
                            modifier = new FloatModifier(serMod.Type, serMod.Priority, serMod.FloatValue);
                        }
                        single.AddModifier(modifier);
                    }
                }
            }

            return single;
        }

        private SerializableGameProperty SerializeGameProperty(GameProperty property)
        {
            if (property == null) return null;

            var data = new SerializableGameProperty
            {
                ID = property.ID,
                BaseValue = property.GetBaseValue(),
                ModifierList = new SerializableModifierList()
            };

            // 使用 ModifierSerializer 序列化所有修饰器
            foreach (var modifier in property.Modifiers)
            {
                string modifierJson = _modifierSerializer.SerializeToJson(modifier);
                if (!string.IsNullOrEmpty(modifierJson))
                {
                    var serMod = JsonUtility.FromJson<SerializableModifier>(modifierJson);
                    data.ModifierList.Modifiers.Add(serMod);
                }
            }

            return data;
        }

        private GameProperty DeserializeGameProperty(SerializableGameProperty data)
        {
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

    /// <summary>
    /// CombinePropertyCustom 的 JSON 序列化器
    /// 只序列化注册的属性，不序列化计算器函数
    /// </summary>
    public class CombinePropertyCustomJsonSerializer : JsonSerializerBase<CombinePropertyCustom>
    {
        private readonly ModifierSerializer _modifierSerializer = new ModifierSerializer();

        public override string SerializeToJson(CombinePropertyCustom obj)
        {
            if (obj == null) return null;

            var data = new SerializableCombinePropertyCustom
            {
                ID = obj.ID,
                BaseValue = obj.GetBaseValue(),
                ResultHolder = SerializeGameProperty(obj.ResultHolder)
            };

            // 这里只能序列化属性快照，无法恢复原始引用关系
            // 需要在反序列化后重新注册属性和设置计算器

            return JsonUtility.ToJson(data);
        }

        public override CombinePropertyCustom DeserializeFromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;

            var data = JsonUtility.FromJson<SerializableCombinePropertyCustom>(json);
            if (data == null) return null;

            // 创建 CombinePropertyCustom
            var custom = new CombinePropertyCustom(data.ID, data.BaseValue);

            // 反序列化 ResultHolder 的修饰器并应用到 custom 的 ResultHolder
            if (data.ResultHolder != null && data.ResultHolder.ModifierList != null)
            {
                var modifierListData = data.ResultHolder.ModifierList;
                if (modifierListData.Modifiers != null)
                {
                    foreach (var serMod in modifierListData.Modifiers)
                    {
                        IModifier modifier;
                        if (serMod.IsRangeModifier)
                        {
                            modifier = new RangeModifier(serMod.Type, serMod.Priority, serMod.RangeValue);
                        }
                        else
                        {
                            modifier = new FloatModifier(serMod.Type, serMod.Priority, serMod.FloatValue);
                        }
                        custom.ResultHolder.AddModifier(modifier);
                    }
                }
            }

            return custom;
        }

        private SerializableGameProperty SerializeGameProperty(GameProperty property)
        {
            if (property == null) return null;

            var data = new SerializableGameProperty
            {
                ID = property.ID,
                BaseValue = property.GetBaseValue(),
                ModifierList = new SerializableModifierList()
            };

            // 使用 ModifierSerializer 序列化所有修饰器
            foreach (var modifier in property.Modifiers)
            {
                string modifierJson = _modifierSerializer.SerializeToJson(modifier);
                if (!string.IsNullOrEmpty(modifierJson))
                {
                    var serMod = JsonUtility.FromJson<SerializableModifier>(modifierJson);
                    data.ModifierList.Modifiers.Add(serMod);
                }
            }

            return data;
        }

        private GameProperty DeserializeGameProperty(SerializableGameProperty data)
        {
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
