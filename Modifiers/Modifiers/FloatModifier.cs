using System;

namespace RPGPack
{


    public class FloatModifier : IModifier<float>
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

        IModifier IModifier.Clone()
        {
            return new FloatModifier(Type, Priority, Value);
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
}