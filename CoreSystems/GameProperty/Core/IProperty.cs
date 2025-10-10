using System;
using System.Collections.Generic;

namespace EasyPack
{
    public interface IReadableProperty<out T>
    {
        string ID { get; }
        T GetValue();
    }

    public interface IModifiableProperty<T> : IReadableProperty<T>
    {
        List<IModifier> Modifiers { get; }
        IModifiableProperty<T> AddModifier(IModifier modifier);
        IModifiableProperty<T> RemoveModifier(IModifier modifier);
        IModifiableProperty<T> ClearModifiers();
    }

    public interface IDrityTackable
    {
        void MakeDirty();
        void OnDirty(Action aciton);
    }
}