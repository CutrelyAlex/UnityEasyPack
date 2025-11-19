using System;
using UnityEngine;

namespace EasyPack.CustomData
{
    /// <summary>
    /// 值处理器接口
    /// 定义了处理不同数据类型值的统一接口
    /// </summary>
    public interface IValueHandler
    {
        /// <summary>
        /// 获取此处理器支持的数据类型
        /// </summary>
        CustomDataType SupportedType { get; }

        /// <summary>
        /// 从CustomDataEntry中获取当前类型的值
        /// </summary>
        /// <param name="entry">数据条目实例</param>
        /// <returns>当前类型的值对象</returns>
        object GetValue(CustomDataEntry entry);

        /// <summary>
        /// 设置CustomDataEntry中当前类型的值
        /// </summary>
        /// <param name="entry">数据条目实例</param>
        /// <param name="value">要设置的值</param>
        void SetValue(CustomDataEntry entry, object value);

        /// <summary>
        /// 尝试从字符串数据反序列化并设置到CustomDataEntry中
        /// </summary>
        /// <param name="entry">数据条目实例</param>
        /// <param name="data">要反序列化的字符串数据</param>
        /// <param name="jsonClrType">JSON对象的CLR类型（可选，用于复杂对象）</param>
        /// <returns>如果反序列化成功返回true，否则返回false</returns>
        bool TryDeserialize(CustomDataEntry entry, string data, Type jsonClrType = null);

        /// <summary>
        /// 将CustomDataEntry中当前类型的值序列化为字符串
        /// </summary>
        /// <param name="entry">数据条目实例</param>
        /// <returns>序列化后的字符串</returns>
        string Serialize(CustomDataEntry entry);

        /// <summary>
        /// 清除CustomDataEntry中当前类型的值
        /// </summary>
        /// <param name="entry">数据条目实例</param>
        void Clear(CustomDataEntry entry);
    }
}
