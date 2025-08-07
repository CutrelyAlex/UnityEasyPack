using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyPack
{
    /// <summary>
    /// 游戏属性的可序列化表示形式，用于存储和传输
    /// </summary>
    [Serializable]
    public class SerializableGameProperty
    {
        public string ID;
        public float BaseValue;
        public SerializableModifierList ModifierList;
    }

    /// <summary>
    /// 修饰器列表的可序列化表示形式
    /// </summary>
    [Serializable]
    public class SerializableModifierList
    {
        public List<SerializableModifier> Modifiers = new List<SerializableModifier>();
    }

    /// <summary>
    /// 修饰器的可序列化表示形式
    /// </summary>
    [Serializable]
    public class SerializableModifier
    {
        public ModifierType Type;
        public int Priority;
        public float FloatValue;
        public Vector2 RangeValue;
        public bool IsRangeModifier;
    }
    /// <summary>
    /// 提供游戏属性的序列化和反序列化功能
    /// </summary>
    public static class GamePropertySerializer
    {
        /// <summary>
        /// 将GameProperty序列化为可存储的形式
        /// </summary>
        /// <param name="property">要序列化的GameProperty</param>
        /// <returns>可序列化的表示形式</returns>
        public static SerializableGameProperty Serialize(GameProperty property)
        {
            var result = new SerializableGameProperty
            {
                ID = property.ID,
                BaseValue = property.GetBaseValue(),
                ModifierList = new SerializableModifierList()
            };

            // 序列化所有修饰器
            foreach (var modifier in property.Modifiers)
            {
                var serMod = new SerializableModifier
                {
                    Type = modifier.Type,
                    Priority = modifier.Priority
                };

                // 根据修饰器类型设置不同的值
                if (modifier is FloatModifier floatMod)
                {
                    serMod.FloatValue = floatMod.Value;
                    serMod.IsRangeModifier = false;
                }
                else if (modifier is RangeModifier rangeMod)
                {
                    serMod.RangeValue = rangeMod.Value;
                    serMod.IsRangeModifier = true;
                }

                result.ModifierList.Modifiers.Add(serMod);
            }

            return result;
        }

        /// <summary>
        /// 从序列化表示形式还原GameProperty
        /// </summary>
        /// <param name="serialized">序列化的表示形式</param>
        /// <returns>还原的GameProperty</returns>
        public static GameProperty FromSerializable(SerializableGameProperty serialized)
        {
            var property = new GameProperty(serialized.ID, serialized.BaseValue);

            // 如果有修饰器列表，还原所有修饰器
            if (serialized.ModifierList != null && serialized.ModifierList.Modifiers != null)
            {
                foreach (var serMod in serialized.ModifierList.Modifiers)
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
                    property.AddModifier(modifier);
                }
            }

            return property;
        }
    }
}