using System;
using System.Collections.Generic;

namespace EasyPack
{
    /// <summary>
    /// 定义只读属性的基础接口，提供属性值读取功能
    /// </summary>
    /// <typeparam name="T">属性值的类型</typeparam>
    public interface IReadableProperty<out T>
    {
        /// <summary>
        /// 获取属性的唯一标识符
        /// </summary>
        string ID { get; }

        /// <summary>
        /// 获取属性的当前值
        /// </summary>
        /// <returns>属性的当前值</returns>
        T GetValue();
    }

    /// <summary>
    /// 定义可修改属性的接口，支持通过修饰符系统动态修改属性值
    /// </summary>
    /// <typeparam name="T">属性值的类型</typeparam>
    public interface IModifiableProperty<T> : IReadableProperty<T>
    {
        /// <summary>
        /// 获取应用于此属性的所有修饰符列表
        /// </summary>
        List<IModifier> Modifiers { get; }

        /// <summary>
        /// 向属性添加一个修饰符，修饰符会影响最终值的计算
        /// </summary>
        /// <param name="modifier">要添加的修饰符</param>
        /// <returns>返回属性自身以支持链式调用</returns>
        IModifiableProperty<T> AddModifier(IModifier modifier);

        /// <summary>
        /// 从属性中移除指定的修饰符
        /// </summary>
        /// <param name="modifier">要移除的修饰符</param>
        /// <returns>返回属性自身以支持链式调用</returns>
        IModifiableProperty<T> RemoveModifier(IModifier modifier);

        /// <summary>
        /// 清除所有修饰符，属性值将回到基础值
        /// </summary>
        /// <returns>返回属性自身以支持链式调用</returns>
        IModifiableProperty<T> ClearModifiers();
    }

    /// <summary>
    /// 定义脏标记系统接口，用于标记属性需要重新计算并响应脏状态变化
    /// </summary>
    public interface IDrityTackable
    {
        /// <summary>
        /// 将对象标记为脏状态，表示需要重新计算
        /// </summary>
        void MakeDirty();

        /// <summary>
        /// 注册一个在对象变为脏状态时执行的回调函数
        /// </summary>
        /// <param name="aciton">脏状态变化时的回调函数</param>
        void OnDirty(Action aciton);
    }
}