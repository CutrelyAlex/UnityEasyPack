using System;

namespace EasyPack.Modifiers
{
    [Serializable]
    public enum ModifierType
    {
        None,
        Add,
        PriorityAdd,
        Mul,
        PriorityMul,
        AfterAdd,
        Override,
        Clamp,
    }
}