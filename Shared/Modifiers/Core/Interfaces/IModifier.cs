namespace EasyPack.Modifiers
{
    public interface IModifier
    {
        ModifierType Type { get; }
        int Priority { get; }
    }

    public interface IModifier<out T> : IModifier
    {
        T Value { get; }
    }
}