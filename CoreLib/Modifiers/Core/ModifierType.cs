using System;

namespace EasyPack
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