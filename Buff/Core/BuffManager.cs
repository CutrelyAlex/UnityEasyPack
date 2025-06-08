using System;
using System.Collections.Generic;

namespace RPGPack
{
    /// <summary>
    /// ֻ����Buff���������ڹ�����¼�����ֱ�Ӳ���IProperty��
    /// Buff��Ӧ�����Ƴ���BuffHandle����
    /// </summary>
    public class BuffManager
    {
        private readonly List<IRPGBuff> _buffs = new();
        private static readonly HashSet<IRPGBuff> _allBuffs = new();

        /// <summary>
        /// Buff����¼�
        /// </summary>
        public event Action<IRPGBuff> BuffAddedOrChanged;
        /// <summary>
        /// Buff�Ƴ��¼�
        /// </summary>
        public event Action<IRPGBuff> BuffRemoved;
        /// <summary>
        /// Buff�����¼�
        /// </summary>
        public event Action<IRPGBuff> BuffExpired;
        /// <summary>
        /// Buff�Ƴ�һ���¼�
        /// </summary>
        public event Action<IRPGBuff> BuffStackRemoved;

        /// <summary>
        /// ���Buff��������
        /// </summary>
        /// <param name="buff">Ҫ��ӵ�Buffʵ��</param>
        /// <returns>���������Ա���ʽ����</returns>
        public bool AddBuff(IRPGBuff buff)
        {
            if (buff == null) return false;


            // ���ȸ���Layer��������BuffID���ң�����ֱ����BuffID����
            IRPGBuff existing = string.IsNullOrEmpty(buff.Layer)
                    ? _buffs.Find(b => b.BuffID == buff.BuffID)
                    : _buffs.Find(b => b.Layer == buff.Layer && b.BuffID == buff.BuffID);

            if (existing != null)
            {
                if (existing.CanStack)
                {
                    int maxStack = existing.MaxStackCount > 0 ? existing.MaxStackCount : int.MaxValue;
                    int newStack = Math.Min(existing.StackCount + buff.StackCount, maxStack);
                    if (newStack > existing.StackCount)
                    {
                        existing.StackCount = newStack;
                    }
                    // ����ʱˢ�³���ʱ��
                    existing.Elapsed = 0;
                    BuffAddedOrChanged?.Invoke(existing);
                    return true;
                }
                return false;
            }
            else
            {
                // ��¡Buff����
                var newBuff = (buff as Buff)?.Clone() ?? buff;
                _buffs.Add(newBuff);
                _allBuffs.Add(newBuff);
                BuffAddedOrChanged?.Invoke(newBuff);
                return true;
            }
        }

        /// <summary>
        /// �Ƴ�ָ��Buff���Ƴ����в㣩
        /// </summary>
        /// <param name="buffId">Buff��ΨһID</param>
        /// <param name="source">Buff��Դ���󣬿�ѡ</param>
        /// <returns>���������Ա���ʽ����</returns>
        public BuffManager RemoveBuff(string buffId, object source = null)
        {
            return RemoveBuffStack(buffId, int.MaxValue, source);
        }

        /// <summary>
        /// �Ƴ�ָ��Buff��ָ������
        /// </summary>
        /// <param name="buffId">Buff��ΨһID</param>
        /// <param name="layer">Ҫ�Ƴ��Ĳ�����Ĭ��Ϊ1</param>
        /// <param name="source">Buff��Դ���󣬿�ѡ</param>
        /// <returns>���������Ա���ʽ����</returns>
        public BuffManager RemoveBuffStack(string buffId, int layer = 1, object source = null)
        {
            if (string.IsNullOrEmpty(buffId)) return this;

            var buff = _buffs.Find(b => b.BuffID == buffId && (source == null || b.Source == source));
            if (buff != null)
            {
                // ������Buffֻ���ٲ������������Ƴ�����Buff
                if (buff.CanStack && buff.StackCount > 1)
                {
                    if (layer >= buff.StackCount)
                    {
                        BuffRemoved?.Invoke(buff);
                        _buffs.Remove(buff);
                        _allBuffs.Remove(buff);
                    }
                    else
                    {
                        buff.StackCount -= layer;
                        BuffStackRemoved?.Invoke(buff);
                    }
                }
                else
                {
                    BuffRemoved?.Invoke(buff);
                    _buffs.Remove(buff);
                    _allBuffs.Remove(buff);
                }
            }
            return this;
        }

        /// <summary>
        /// ��ȡָ��Buff���ɰ�Layer��ѯ
        /// </summary>
        /// <param name="buffId">Buff��ΨһID</param>
        /// <param name="layer">Buff���飬��ѡ��Ϊ���򲻰�Layer����</param>
        /// <param name="source">Buff��Դ���󣬿�ѡ</param>
        /// <returns>�����ҵ���Buffʵ����δ�ҵ�����null</returns>
        public IRPGBuff GetBuff(string buffId, string layer = null, object source = null)
        {
            if (string.IsNullOrEmpty(buffId)) return null;
            if (string.IsNullOrEmpty(layer))
            {
                // ����Layer����
                return _buffs.Find(b => b.BuffID == buffId && (source == null || b.Source == source));
            }
            else
            {
                // ��Layer����
                return _buffs.Find(b => b.BuffID == buffId && b.Layer == layer && (source == null || b.Source == source));
            }
        }


        /// <summary>
        /// ��ȡ�����Ѵ����� Buff��ȫ�־�̬���ϣ�
        /// </summary>
        /// <returns>Buffö�ټ���</returns>
        public static IEnumerable<IRPGBuff> GetAllBuffs() => _allBuffs;

        /// <summary>
        /// ����Buff���������ڣ���ÿ֡����,����Mono��Update()�е��ã�
        /// </summary>
        /// <param name="deltaTime">�����ϴθ��µ�ʱ�䣨�룩</param>
        /// <returns>��������</returns>
        public BuffManager Update(float deltaTime)
        {
            var expiredBuffs = new List<IRPGBuff>();
            // ����������Ƴ�ʱ����Ӱ������
            for (int i = _buffs.Count - 1; i >= 0; i--)
            {
                var buff = _buffs[i];

                // ������Buff���Դ���
                if (buff is TriggerableBuff triggerableBuff)
                {
                    triggerableBuff.TryTrigger();
                }

                // ֻ�����г���ʱ���Buff
                if (buff.Duration > 0)
                {
                    buff.Elapsed += deltaTime;
                    if (buff.IsExpired)
                    {
                        // ֧��ÿ�ι���ֻ�Ƴ�һ��
                        if (buff.RemoveOneStackEachDuration && buff.CanStack && buff.StackCount > 1)
                        {
                            RemoveBuffStack(buff.BuffID, 1, buff.Source);
                            buff.Elapsed = 0;
                            continue;
                        }
                        BuffRemoved?.Invoke(buff);
                        expiredBuffs.Add(buff);
                        _buffs.RemoveAt(i);
                        _allBuffs.Remove(buff);
                    }
                }
            }
            // ͳһ���������¼�
            foreach (var buff in expiredBuffs)
            {
                BuffExpired?.Invoke(buff);
            }
            return this;
        }

        /// <summary>
        /// �������Buff
        /// </summary>
        /// <returns>��������</returns>
        public BuffManager Clear()
        {
            var tempBuffs = new List<IRPGBuff>(_buffs);
            foreach (var buff in tempBuffs)
            {
                BuffRemoved?.Invoke(buff);
            }
            _buffs.Clear();
            _allBuffs.Clear();
            return this;
        }
    }
}