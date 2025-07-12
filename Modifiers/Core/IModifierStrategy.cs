using System.Collections.Generic;

namespace EasyPack
{
    /// <summary>
    /// ÐÞÊÎÆ÷²ßÂÔ½Ó¿Ú
    /// </summary>
    public interface IModifierStrategy
    {
        ModifierType Type { get; }
        void Apply(ref float value, IEnumerable<IModifier> modifiers);
    }
}