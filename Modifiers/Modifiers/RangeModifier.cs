using System;
using UnityEngine;

namespace EasyPack
{
    public class RangeModifier : IModifier<Vector2>
    {
        public ModifierType Type { get; set; }
        public int Priority { get; set; }
        public Vector2 Value { get; set; }

        public RangeModifier(ModifierType type, int priority, Vector2 range)
        {
            Type = type;
            Priority = priority;
            Value = range;
        }

        public IModifier Clone()
        {
            return new RangeModifier(Type, Priority, Value);
        }

        public override bool Equals(object obj)
        {
            if (obj is RangeModifier other)
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
}