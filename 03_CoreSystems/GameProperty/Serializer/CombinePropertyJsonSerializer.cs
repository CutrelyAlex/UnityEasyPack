using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyPack
{
    /// <summary>
    /// CombinePropertySingle 的可序列化数据结构
    /// </summary>
    [Serializable]
    public class SerializableCombinePropertySingle
    {
        /// <summary>
        /// 组合属性的唯一标识符
        /// </summary>
        public string ID;

        /// <summary>
        /// 基础值
        /// </summary>
        public float BaseValue;

        /// <summary>
        /// 结果持有者的序列化数据
        /// </summary>
        public SerializableGameProperty ResultHolder;
    }

    /// <summary>
    /// CombinePropertyCustom 的可序列化数据结构
    /// </summary>
    [Serializable]
    public class SerializableCombinePropertyCustom
    {
        /// <summary>
        /// 组合属性的唯一标识符
        /// </summary>
        public string ID;

        /// <summary>
        /// 基础值
        /// </summary>
        public float BaseValue;

        /// <summary>
        /// 结果持有者的序列化数据
        /// </summary>
        public SerializableGameProperty ResultHolder;

        /// <summary>
        /// 已注册的属性ID列表
        /// </summary>
        public List<string> RegisteredPropertyIDs = new List<string>();

        /// <summary>
        /// 已注册的属性序列化数据列表
        /// </summary>
        public List<SerializableGameProperty> RegisteredProperties = new List<SerializableGameProperty>();
    }

    /// <summary>
    /// CombinePropertySingle 的 JSON 序列化器
    /// </summary>
    public class CombinePropertySingleJsonSerializer : JsonSerializerBase<CombinePropertySingle>
    {
        private readonly ModifierSerializer _modifierSerializer = new ModifierSerializer();

        /// <summary>
        /// 将 CombinePropertySingle 对象序列化为 JSON 字符串
        /// </summary>
        /// <param name="obj">要序列化的对象</param>
        /// <returns>JSON 字符串，如果对象为 null 则返回 null</returns>
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

        /// <summary>
        /// 从 JSON 字符串反序列化为 CombinePropertySingle 对象
        /// </summary>
        /// <param name="json">JSON 字符串</param>
        /// <returns>反序列化的对象，如果 JSON 无效则返回 null</returns>
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

        /// <summary>
        /// 将 GameProperty 序列化为可序列化数据结构
        /// </summary>
        /// <param name="property">要序列化的 GameProperty</param>
        /// <returns>可序列化数据结构</returns>
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
    /// 序列化属性 ID、基础值和修饰符，但不序列化计算器函数和已注册属性的引用关系
    /// 反序列化后需要重新注册属性和设置计算器
    /// </summary>
    public class CombinePropertyCustomJsonSerializer : JsonSerializerBase<CombinePropertyCustom>
    {
        private readonly ModifierSerializer _modifierSerializer = new ModifierSerializer();

        /// <summary>
        /// 将 CombinePropertyCustom 对象序列化为 JSON 字符串
        /// </summary>
        /// <param name="obj">要序列化的对象</param>
        /// <returns>JSON 字符串，如果对象为 null 则返回 null</returns>
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

        /// <summary>
        /// 从 JSON 字符串反序列化为 CombinePropertyCustom 对象
        /// 注意：反序列化后需要重新注册属性引用和设置计算器函数
        /// </summary>
        /// <param name="json">JSON 字符串</param>
        /// <returns>反序列化的对象，如果 JSON 无效则返回 null</returns>
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

        /// <summary>
        /// 将 GameProperty 序列化为可序列化数据结构
        /// </summary>
        /// <param name="property">要序列化的 GameProperty</param>
        /// <returns>可序列化数据结构</returns>
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

        /// <summary>
        /// 从可序列化数据结构反序列化为 GameProperty
        /// </summary>
        /// <param name="data">可序列化数据结构</param>
        /// <returns>反序列化的 GameProperty</returns>
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
