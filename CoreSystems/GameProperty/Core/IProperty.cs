using System;
using System.Collections.Generic;

namespace EasyPack
{
    public interface IProperty<out T>
    {
        string ID { get; set; }
        List<IModifier> Modifiers { get; }
        IProperty<T> AddModifier(IModifier modifier);
        IProperty<T> RemoveModifier(IModifier modifier);
        IProperty<T> ClearModifiers();
        T GetValue();
        void MakeDirty();
        void OnDirty(Action aciton);
    }
}