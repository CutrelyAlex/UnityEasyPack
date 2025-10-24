namespace EasyPack
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
    }
}