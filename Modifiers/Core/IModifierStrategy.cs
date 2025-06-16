using System.Collections.Generic;

namespace RPGPack
{
    /// <summary>
    /// ���������Խӿ�
    /// </summary>
    public interface IModifierStrategy
    {
        ModifierType Type { get; }
        void Apply(ref float value, IEnumerable<IModifier> modifiers);
    }
}