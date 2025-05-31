using System;
using System.Collections.Generic;

namespace RPGPack
{
    public interface IProperty<out T>
    {
        string ID { get; set; }
        List<IModifier> Modifiers { get; }
        void AddModifier(IModifier modifier);
        void RemoveModifier(IModifier modifier);
        void ClearModifiers();
        Func<T> GetValueGetter() => GetValue;
        T GetValue();
        void MakeDirty();
        void OnDirty(Action aciton);
    }
}