namespace EasyPack
{
    public interface IModifier
    {
        ModifierType Type { get; }
        int Priority { get; set; }
        IModifier Clone();
    }

    public interface IModifier<T> : IModifier
    {
        T Value { get; set; }
    }
}