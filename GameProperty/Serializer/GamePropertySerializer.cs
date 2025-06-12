using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPGPack
{
    /// <summary>
    /// ��Ϸ���ԵĿ����л���ʾ��ʽ�����ڴ洢�ʹ���
    /// </summary>
    [Serializable]
    public class SerializableGameProperty
    {
        public string ID;
        public float BaseValue;
        public SerializableModifierList ModifierList;
    }

    /// <summary>
    /// �������б�Ŀ����л���ʾ��ʽ
    /// </summary>
    [Serializable]
    public class SerializableModifierList
    {
        public List<SerializableModifier> Modifiers = new List<SerializableModifier>();
    }

    /// <summary>
    /// �������Ŀ����л���ʾ��ʽ
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
    /// �ṩ��Ϸ���Ե����л��ͷ����л�����
    /// </summary>
    public static class GamePropertySerializer
    {
        /// <summary>
        /// ��GameProperty���л�Ϊ�ɴ洢����ʽ
        /// </summary>
        /// <param name="property">Ҫ���л���GameProperty</param>
        /// <returns>�����л��ı�ʾ��ʽ</returns>
        public static SerializableGameProperty Serialize(GameProperty property)
        {
            var result = new SerializableGameProperty
            {
                ID = property.ID,
                BaseValue = property.GetBaseValue(),
                ModifierList = new SerializableModifierList()
            };

            // ���л�����������
            foreach (var modifier in property.Modifiers)
            {
                var serMod = new SerializableModifier
                {
                    Type = modifier.Type,
                    Priority = modifier.Priority
                };

                // �����������������ò�ͬ��ֵ
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
        /// �����л���ʾ��ʽ��ԭGameProperty
        /// </summary>
        /// <param name="serialized">���л��ı�ʾ��ʽ</param>
        /// <returns>��ԭ��GameProperty</returns>
        public static GameProperty FromSerializable(SerializableGameProperty serialized)
        {
            var property = new GameProperty(serialized.ID, serialized.BaseValue);

            // ������������б���ԭ����������
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