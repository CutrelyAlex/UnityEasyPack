using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyPack.InventorySystem
{
    /// <summary>
    /// 条件类型注册表
    /// 用于映射 SerializedCondition.Kind 到具体的条件类型
    /// </summary>
    public static class ConditionTypeRegistry
    {
        private static readonly Dictionary<string, Type> _kindToType = new();

        /// <summary>
        /// 注册条件类型
        /// </summary>
        /// <param name="kind">条件的 Kind 标识</param>
        /// <param name="conditionType">条件的具体类型</param>
        public static void RegisterConditionType(string kind, Type conditionType)
        {
            if (!typeof(IItemCondition).IsAssignableFrom(conditionType))
            {
                Debug.LogError($"[ConditionTypeRegistry] 类型 {conditionType.Name} 必须实现 IItemCondition 接口");
                return;
            }

            _kindToType[kind] = conditionType;
            Debug.Log($"[ConditionTypeRegistry] 注册条件类型: {kind} -> {conditionType.Name}");
        }

        /// <summary>
        /// 根据 Kind 获取条件类型
        /// </summary>
        /// <param name="kind">条件的 Kind 标识</param>
        /// <returns>对应的条件类型，如果未注册则返回 null</returns>
        public static Type GetConditionType(string kind)
        {
            if (_kindToType.TryGetValue(kind, out var type))
                return type;

            Debug.LogWarning($"[ConditionTypeRegistry] 未注册的条件类型: {kind}");
            return null;
        }

        /// <summary>
        /// 检查 Kind 是否已注册
        /// </summary>
        public static bool IsRegistered(string kind)
        {
            return !string.IsNullOrEmpty(kind) && _kindToType.ContainsKey(kind);
        }

        /// <summary>
        /// 获取所有已注册的 Kind
        /// </summary>
        public static IEnumerable<string> GetRegisteredKinds()
        {
            return _kindToType.Keys;
        }

        /// <summary>
        /// 清除所有注册
        /// </summary>
        public static void Clear()
        {
            _kindToType.Clear();
        }
    }
}

