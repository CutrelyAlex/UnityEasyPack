using System;
using System.Collections.Generic;
using Unity.VisualScripting;

namespace RPGPack
{
    /// <summary>
    /// BuffHandle 负责自动管理 BuffManager 的引用，并动态响应 Buff 的增删查改。
    /// 用户只需初始化时绑定 Manager，之后通过 ApplyToProperty 即可将 Buff 应用到属性。
    /// </summary>
    public class BuffHandle
    {
        private BuffManager _manager;
        // 记录属性与BuffID的映射
        private readonly Dictionary<GameProperty, HashSet<string>> _propertyBuffMap = new();

        /// <summary>
        /// 构造函数，可选绑定BuffManager
        /// </summary>
        public BuffHandle(BuffManager manager = null)
        {
            if (manager != null)
            {
                Initialize(manager);
            }
        }

        /// <summary>
        /// 初始化BuffHandle，绑定BuffManager
        /// </summary>
        public BuffHandle Initialize(BuffManager manager)
        {
            if (_manager != null)
            {
                _manager.BuffRemoved -= OnBuffRemoved;
                _manager.BuffStackRemoved -= OnBuffStackRemoved;
                _manager.BuffExpired -= OnBuffRemoved;
            }
            _manager = manager;
            if (_manager != null)
            {
                _manager.BuffRemoved += OnBuffRemoved;
                _manager.BuffStackRemoved += OnBuffStackRemoved;
                _manager.BuffExpired += OnBuffRemoved;
            }
            _propertyBuffMap.Clear();
            return this;
        }

        /// <summary>
        /// 将Buff应用到属性上
        /// </summary>
        public BuffHandle ApplyToProperty(IRPGBuff buff, GameProperty property)
        {
            if (_manager == null || buff == null || property == null)
                throw new InvalidOperationException("BuffHandle未初始化或参数无效");

            bool added = _manager.AddBuff(buff);

            // 获取当前Buff在Manager中的实例（叠加后可能是已存在的Buff）
            var managedBuff = _manager.GetBuff(buff.BuffID, buff.Layer, buff.Source);

            // 记录映射关系
            if (!_propertyBuffMap.TryGetValue(property, out var buffIDSet))
            {
                buffIDSet = new HashSet<string>();
                _propertyBuffMap[property] = buffIDSet;
            }
            buffIDSet.Add(buff.BuffID);

            // 只有Buff被真正添加时才移除/添加Modifier
            if (added && managedBuff != null)
            {
                RemoveAllModifiersByBuffID(property, buff.BuffID, buff.Layer, buff.Source);
                for (int i = 0; i < managedBuff.StackCount; i++)
                {
                    property.AddModifier(managedBuff.Modifier);
                }
            }
            return this;
        }

        /// <summary>
        /// Buff被移除或过期时，自动同步移除属性上的Modifier
        /// </summary>
        private void OnBuffRemoved(IRPGBuff buff)
        {
            foreach (var kv in _propertyBuffMap)
            {
                if (kv.Value.Contains(buff.BuffID))
                {
                    RemoveAllModifiersByBuffID(kv.Key, buff.BuffID, buff.Layer, buff.Source);
                }
            }
        }

        /// <summary>
        /// Buff叠加层数减少时，自动同步减少属性上的Modifier
        /// </summary>
        private void OnBuffStackRemoved(IRPGBuff buff)
        {
            foreach (var kv in _propertyBuffMap)
            {
                if (kv.Value.Contains(buff.BuffID))
                {
                    // 先移除所有，再重新按当前StackCount添加
                    RemoveAllModifiersByBuffID(kv.Key, buff.BuffID, buff.Layer, buff.Source);
                    for (int i = 0; i < buff.StackCount; i++)
                    {
                        kv.Key.AddModifier(buff.Modifier);
                    }
                }
            }
        }

        /// <summary>
        /// 移除属性上所有与指定BuffID相关的Modifier
        /// </summary>
        private void RemoveAllModifiersByBuffID(GameProperty property, string buffId, string layer = null, object source = null)
        {
            // 获取当前Buff对应的Modifier实例
            var buff = _manager?.GetBuff(buffId, layer, source);
            if (buff == null) return;
            var modifier = buff.Modifier;

            for (int i = property.Modifiers.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(property.Modifiers[i], modifier))
                {
                    property.RemoveModifier(modifier);
                }
            }
        }
    }
}