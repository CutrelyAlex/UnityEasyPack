namespace EasyPack.Modifiers
{
    public interface IModifier
    {
        ModifierType Type { get; }
        int Priority { get; }

        IModifier Clone();
    }

    public interface IModifier<out T> : IModifier
    {
        T Value { get; }
    }
}