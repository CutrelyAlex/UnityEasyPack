using System;
using System.Collections.Generic;
using Unity.VisualScripting;

namespace RPGPack
{
    /// <summary>
    /// BuffHandle �����Զ����� BuffManager �����ã�����̬��Ӧ Buff ����ɾ��ġ�
    /// �û�ֻ���ʼ��ʱ�� Manager��֮��ͨ�� ApplyToProperty ���ɽ� Buff Ӧ�õ����ԡ�
    /// </summary>
    public class BuffHandle
    {
        private BuffManager _manager;
        // ��¼������BuffID��ӳ��
        private readonly Dictionary<GameProperty, HashSet<string>> _propertyBuffMap = new();

        /// <summary>
        /// ���캯������ѡ��BuffManager
        /// </summary>
        public BuffHandle(BuffManager manager = null)
        {
            if (manager != null)
            {
                Initialize(manager);
            }
        }

        /// <summary>
        /// ��ʼ��BuffHandle����BuffManager
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
        /// ��BuffӦ�õ�������
        /// </summary>
        public BuffHandle ApplyToProperty(IRPGBuff buff, GameProperty property)
        {
            if (_manager == null || buff == null || property == null)
                throw new InvalidOperationException("BuffHandleδ��ʼ���������Ч");

            bool added = _manager.AddBuff(buff);

            // ��ȡ��ǰBuff��Manager�е�ʵ�������Ӻ�������Ѵ��ڵ�Buff��
            var managedBuff = _manager.GetBuff(buff.BuffID, buff.Layer, buff.Source);

            // ��¼ӳ���ϵ
            if (!_propertyBuffMap.TryGetValue(property, out var buffIDSet))
            {
                buffIDSet = new HashSet<string>();
                _propertyBuffMap[property] = buffIDSet;
            }
            buffIDSet.Add(buff.BuffID);

            // ֻ��Buff���������ʱ���Ƴ�/���Modifier
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
        /// Buff���Ƴ������ʱ���Զ�ͬ���Ƴ������ϵ�Modifier
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
        /// Buff���Ӳ�������ʱ���Զ�ͬ�����������ϵ�Modifier
        /// </summary>
        private void OnBuffStackRemoved(IRPGBuff buff)
        {
            foreach (var kv in _propertyBuffMap)
            {
                if (kv.Value.Contains(buff.BuffID))
                {
                    // ���Ƴ����У������°���ǰStackCount���
                    RemoveAllModifiersByBuffID(kv.Key, buff.BuffID, buff.Layer, buff.Source);
                    for (int i = 0; i < buff.StackCount; i++)
                    {
                        kv.Key.AddModifier(buff.Modifier);
                    }
                }
            }
        }

        /// <summary>
        /// �Ƴ�������������ָ��BuffID��ص�Modifier
        /// </summary>
        private void RemoveAllModifiersByBuffID(GameProperty property, string buffId, string layer = null, object source = null)
        {
            // ��ȡ��ǰBuff��Ӧ��Modifierʵ��
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