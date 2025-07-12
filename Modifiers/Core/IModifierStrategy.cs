using System.Collections.Generic;

namespace EasyPack
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