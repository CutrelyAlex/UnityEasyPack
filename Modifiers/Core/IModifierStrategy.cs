using System.Collections.Generic;

namespace RPGPack
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