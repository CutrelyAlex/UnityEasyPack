using System;

namespace EasyPack
{
    [Serializable]
    public enum ModifierType
    {
        None,
        Add,
        AfterAdd,
        PriorityAdd,
        Mul,
        PriorityMul,
        Override,
        Clamp,
    }
}