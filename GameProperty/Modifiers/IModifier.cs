namespace RPGPack
{
    public interface IModifier
    {
        ModifierType Type { get; }
        int Priority { get; set; }
        public float Value { get; set; }
    }
}