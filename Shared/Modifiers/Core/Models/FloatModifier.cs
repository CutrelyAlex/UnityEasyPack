namespace EasyPack.Modifiers
{
    public class FloatModifier : IModifier<float>
    {
        public ModifierType Type { get; }
        public int Priority { get; }
        public float Value { get; }

        public FloatModifier(ModifierType type, int priority, float value)
        {
            Type = type;
            Priority = priority;
            Value = value;
        }
    }
}