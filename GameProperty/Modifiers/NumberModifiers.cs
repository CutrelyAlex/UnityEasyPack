using System;
using UnityEngine;

namespace RPGPack
{
    [Serializable]
    public enum ModifierType
    {
        None,
        Add,
        AfterAdd,
        OverrideAdd,
        Mul,
        OverrideMul,
        Override,
        Clamp,
    }

    public class FloatModifier : IModifier
    {
        public ModifierType Type { get; }
        public int Priority { get; set; }
        public float Value { get; set; }

        public FloatModifier(ModifierType type, int priority, float value)
        {
            Type = type;
            Priority = priority;
            Value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj is FloatModifier other)
            {
                return Type == other.Type && Priority == other.Priority && Value.Equals(other.Value);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, Priority, Value);
        }

    }
    public class Vector2Modifier : IModifier
    {
        public ModifierType Type { get; }
        public int Priority { get; set; }
        public Vector2 Range { get; set; }

        public float Value { get; set; }
        public string ID { get; set; }

        public Vector2Modifier(ModifierType type, int priority, Vector2 range)
        {
            Type = type;
            Priority = priority;
            Range = range;
        }

        public override bool Equals(object obj)
        {
            if (obj is Vector2Modifier other)
            {
                return Type == other.Type && Priority == other.Priority && Range.Equals(other.Range);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, Priority, Range);
        }
    }

    [Serializable]
    public class SerializableModifier
    {
        public ModifierType Type;
        public int Priority;
        public float Value;
        public Vector2 Range;
        public string ModifierClass;
    }
}