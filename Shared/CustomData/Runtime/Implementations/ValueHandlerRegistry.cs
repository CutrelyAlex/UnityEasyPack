using System.Collections.Generic;
using UnityEngine;

namespace EasyPack.CustomData
{
    /// <summary>
    /// 值处理器注册表
    /// 静态工厂类，负责管理和提供所有数据类型的值处理器实例
    /// </summary>
    public static class ValueHandlerRegistry
    {
        /// <summary>
        /// 处理器字典
        /// 存储所有数据类型对应的处理器实例
        /// </summary>
        private static readonly Dictionary<CustomDataType, IValueHandler> Handlers = new();

        /// <summary>
        /// 静态构造函数
        /// 初始化所有支持的数据类型处理器
        /// </summary>
        static ValueHandlerRegistry()
        {
            Handlers[CustomDataType.Int] = new IntValueHandler();
            Handlers[CustomDataType.Float] = new FloatValueHandler();
            Handlers[CustomDataType.Bool] = new BoolValueHandler();
            Handlers[CustomDataType.String] = new StringValueHandler();
            Handlers[CustomDataType.Vector2] = new Vector2ValueHandler();
            Handlers[CustomDataType.Vector3] = new Vector3ValueHandler();
            Handlers[CustomDataType.Color] = new ColorValueHandler();
            Handlers[CustomDataType.Json] = new JsonValueHandler();
            Handlers[CustomDataType.Custom] = new CustomValueHandler();
            Handlers[CustomDataType.None] = new NoneValueHandler();
        }

        /// <summary>
        /// 根据数据类型获取对应的处理器
        /// </summary>
        /// <param name="type">数据类型枚举值</param>
        /// <returns>对应的值处理器实例，如果未找到则返回None处理器</returns>
        public static IValueHandler GetHandler(CustomDataType type)
        {
            if (Handlers.TryGetValue(type, out var handler))
                return handler;

            return Handlers[CustomDataType.None];
        }

        /// <summary>
        /// 根据值的运行时类型获取对应的处理器
        /// </summary>
        /// <param name="value">要处理的值对象</param>
        /// <returns>对应值类型的处理器实例，复杂对象使用Json处理器</returns>
        public static IValueHandler GetHandlerForValue(object value)
        {
            if (value == null)
                return Handlers[CustomDataType.None];

            var valueType = value.GetType();

            if (valueType == typeof(int))
                return Handlers[CustomDataType.Int];
            if (valueType == typeof(float))
                return Handlers[CustomDataType.Float];
            if (valueType == typeof(bool))
                return Handlers[CustomDataType.Bool];
            if (valueType == typeof(string))
                return Handlers[CustomDataType.String];
            if (valueType == typeof(Vector2))
                return Handlers[CustomDataType.Vector2];
            if (valueType == typeof(Vector3))
                return Handlers[CustomDataType.Vector3];
            if (valueType == typeof(Color))
                return Handlers[CustomDataType.Color];

            return Handlers[CustomDataType.Json];
        }
    }
}
