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
        private readonly Dictionary<string, List<IRPGBuff>> _buffsByID = new();
        private readonly Dictionary<string, Dictionary<string, List<IRPGBuff>>> _buffsByLayerAndID = new();

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
        /// ��ȡ�����Ѵ����� Buff��ȫ�־�̬���ϣ�
        /// </summary>
        /// <returns>Buffö�ټ���</returns>
        public static IEnumerable<IRPGBuff> GetAllBuffs() => _allBuffs;

        #region ��ɾ���Buff

        /// <summary>
        /// ���Buff��������
        /// </summary>
        /// <param name="buff">Ҫ��ӵ�Buffʵ��</param>
        /// <returns>�����Ƿ�ɹ����</returns>
        public bool AddBuff(IRPGBuff buff)
        {
            if (buff == null) return false;


            // ʹ�û����ֵ����
            IRPGBuff existing;
            if (!string.IsNullOrEmpty(buff.Layer))
            {
                // ��Layer��BuffID����
                if (_buffsByLayerAndID.TryGetValue(buff.Layer, out var idMap) &&
                    idMap.TryGetValue(buff.BuffID, out var buffsInLayer) &&
                    buffsInLayer.Count > 0)
                {
                    existing = buffsInLayer[0];
                }
                else
                {
                    existing = null;
                }
            }
            else
            {
                // ����BuffID����
                if (_buffsByID.TryGetValue(buff.BuffID, out var buffs) && buffs.Count > 0)
                {
                    existing = buffs.Find(b => string.IsNullOrEmpty(b.Layer));
                }
                else
                {
                    existing = null;
                }
            }

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

                // ��������
                AddToIndices(newBuff);

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
        public BuffManager RemoveBuff(string buffId, string layer = null, object source = null)
        {
            return RemoveBuffStack(buffId, int.MaxValue, layer, source);
        }

        /// <summary>
        /// �Ƴ�ָ��Buff��ָ������
        /// </summary>
        /// <param name="buffId">Buff��ΨһID</param>
        /// <param name="stack">Ҫ�Ƴ��Ĳ�����Ĭ��Ϊ1</param>
        /// <param name="source">Buff��Դ���󣬿�ѡ</param>
        /// <returns>���������Ա���ʽ����</returns>
        public BuffManager RemoveBuffStack(string buffId, int stack = 1, string layer = null, object source = null)
        {
            if (string.IsNullOrEmpty(buffId)) return this;

            IRPGBuff buff;
            if (layer != null)
            {
                // ��Layer��BuffID����
                if (_buffsByLayerAndID.TryGetValue(layer, out var idMap) &&
                    idMap.TryGetValue(buffId, out var buffsInLayer))
                {
                    buff = buffsInLayer.Find(b => source == null || b.Source == source);
                }
                else
                {
                    buff = null;
                }
            }
            else
            {
                // ��BuffID����
                if (_buffsByID.TryGetValue(buffId, out var buffs))
                {
                    buff = buffs.Find(b => (source == null || b.Source == source) &&
                                           (layer == null || b.Layer == layer));
                }
                else
                {
                    buff = null;
                }
            }

            if (buff != null)
            {
                // ������Buffֻ���ٲ������������Ƴ�����Buff
                if (buff.CanStack && buff.StackCount > 1)
                {
                    if (stack >= buff.StackCount)
                    {
                        BuffRemoved?.Invoke(buff);
                        _buffs.Remove(buff);
                        _allBuffs.Remove(buff);
                        RemoveFromIndices(buff);
                    }
                    else
                    {
                        buff.StackCount -= stack;
                        BuffStackRemoved?.Invoke(buff);
                    }
                }
                else
                {
                    BuffRemoved?.Invoke(buff);
                    _buffs.Remove(buff);
                    _allBuffs.Remove(buff);
                    RemoveFromIndices(buff);
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

            // ʹ�û����ֵ���ٲ���
            if (!string.IsNullOrEmpty(layer))
            {
                // ��Layer��BuffID����
                if (_buffsByLayerAndID.TryGetValue(layer, out var idMap) &&
                    idMap.TryGetValue(buffId, out var buffsInLayer))
                {
                    return buffsInLayer.Find(b => source == null || b.Source == source);
                }
            }
            else
            {
                // ����BuffID����
                if (_buffsByID.TryGetValue(buffId, out var buffs))
                {
                    return buffs.Find(b => (source == null || b.Source == source));
                }
            }
            return null;
        }



        /// <summary>
        /// �������Buff
        /// </summary>
        /// <returns>��������</returns>
        public BuffManager Clear()
        {
            if (BuffRemoved != null)
            {
                var tempBuffs = new List<IRPGBuff>(_buffs);
                foreach (var buff in tempBuffs)
                {
                    BuffRemoved.Invoke(buff);
                }
            }

            _buffs.Clear();
            _allBuffs.Clear();
            _buffsByID.Clear();
            _buffsByLayerAndID.Clear();

            return this;
        }
#endregion
        public BuffManager Update(float deltaTime)
        {
            var expiredBuffs = new List<IRPGBuff>(4);

            // ����������Ƴ�ʱ����Ӱ������
            for (int i = _buffs.Count - 1; i >= 0; i--)
            {
                var buff = _buffs[i];

                // ������Buff���Դ���
                if (buff is TriggerableBuff triggerableBuff)
                {
                    triggerableBuff.TryTrigger();
                }

                // ����������Buff
                if (buff is PeriodicBuff periodicBuff)
                {
                    periodicBuff.UpdateTick(deltaTime);
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
                            RemoveBuffStack(buff.BuffID, 1, buff.Layer, buff.Source);
                            buff.Elapsed = 0;
                            continue;
                        }
                        BuffRemoved?.Invoke(buff);
                        expiredBuffs.Add(buff);
                        _buffs.RemoveAt(i);
                        _allBuffs.Remove(buff);
                        RemoveFromIndices(buff);
                    }
                }
            }

            // ͳһ���������¼�
            if (BuffExpired != null)
            {
                foreach (var buff in expiredBuffs)
                {
                    BuffExpired.Invoke(buff);
                }
            }
            return this;
        }



        #region ����
        // <summary>
        /// ��Buff��ӵ�������
        /// </summary>
        private void AddToIndices(IRPGBuff buff)
        {
            // ��ӵ�ID����
            if (!_buffsByID.TryGetValue(buff.BuffID, out var buffs))
            {
                buffs = new List<IRPGBuff>();
                _buffsByID[buff.BuffID] = buffs;
            }
            buffs.Add(buff);

            // �����Layer����ӵ�Layer����
            if (!string.IsNullOrEmpty(buff.Layer))
            {
                if (!_buffsByLayerAndID.TryGetValue(buff.Layer, out var idMap))
                {
                    idMap = new Dictionary<string, List<IRPGBuff>>();
                    _buffsByLayerAndID[buff.Layer] = idMap;
                }

                if (!idMap.TryGetValue(buff.BuffID, out var layerBuffs))
                {
                    layerBuffs = new List<IRPGBuff>();
                    idMap[buff.BuffID] = layerBuffs;
                }

                layerBuffs.Add(buff);
            }
        }

        /// <summary>
        /// ���������Ƴ�Buff
        /// </summary>
        private void RemoveFromIndices(IRPGBuff buff)
        {
            // ��ID�������Ƴ�
            if (_buffsByID.TryGetValue(buff.BuffID, out var buffs))
            {
                buffs.Remove(buff);
                if (buffs.Count == 0)
                {
                    _buffsByID.Remove(buff.BuffID);
                }
            }

            // �����Layer����Layer�������Ƴ�
            if (!string.IsNullOrEmpty(buff.Layer) &&
                _buffsByLayerAndID.TryGetValue(buff.Layer, out var idMap) &&
                idMap.TryGetValue(buff.BuffID, out var layerBuffs))
            {
                layerBuffs.Remove(buff);
                if (layerBuffs.Count == 0)
                {
                    idMap.Remove(buff.BuffID);
                    if (idMap.Count == 0)
                    {
                        _buffsByLayerAndID.Remove(buff.Layer);
                    }
                }
            }
        }
        #endregion
    }
}