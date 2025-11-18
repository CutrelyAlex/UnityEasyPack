using System.Collections.Generic;

namespace EasyPack.Modifiers
{
    public interface IModifierStrategy
    {
        ModifierType Type { get; }
        void Apply(ref float value, IEnumerable<IModifier> modifiers);
    }
}