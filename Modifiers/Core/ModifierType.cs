using System;

namespace RPGPack
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