using System;
using System.Collections.Generic;

namespace RPGPack
{
    /// <summary>
    /// 只负责Buff的生命周期管理和事件，不直接操作IProperty。
    /// Buff的应用与移除由BuffHandle负责。
    /// </summary>
    public class BuffManager
    {
        private readonly List<IRPGBuff> _buffs = new();
        private static readonly HashSet<IRPGBuff> _allBuffs = new();
        private readonly Dictionary<string, List<IRPGBuff>> _buffsByID = new();
        private readonly Dictionary<string, Dictionary<string, List<IRPGBuff>>> _buffsByLayerAndID = new();

        /// <summary>
        /// Buff添加事件
        /// </summary>
        public event Action<IRPGBuff> BuffAddedOrChanged;
        /// <summary>
        /// Buff移除事件
        /// </summary>
        public event Action<IRPGBuff> BuffRemoved;
        /// <summary>
        /// Buff过期事件
        /// </summary>
        public event Action<IRPGBuff> BuffExpired;
        /// <summary>
        /// Buff移除一层事件
        /// </summary>
        public event Action<IRPGBuff> BuffStackRemoved;


        /// <summary>
        /// 获取所有已创建的 Buff（全局静态集合）
        /// </summary>
        /// <returns>Buff枚举集合</returns>
        public static IEnumerable<IRPGBuff> GetAllBuffs() => _allBuffs;

        #region 增删查改Buff

        /// <summary>
        /// 添加Buff到管理器
        /// </summary>
        /// <param name="buff">要添加的Buff实例</param>
        /// <returns>返回是否成功添加</returns>
        public bool AddBuff(IRPGBuff buff)
        {
            if (buff == null) return false;


            // 使用缓存字典查找
            IRPGBuff existing;
            if (!string.IsNullOrEmpty(buff.Layer))
            {
                // 按Layer和BuffID查找
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
                // 仅按BuffID查找
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
                    // 叠加时刷新持续时间
                    existing.Elapsed = 0;
                    BuffAddedOrChanged?.Invoke(existing);
                    return true;
                }
                return false;
            }
            else
            {
                // 克隆Buff对象
                var newBuff = (buff as Buff)?.Clone() ?? buff;
                _buffs.Add(newBuff);
                _allBuffs.Add(newBuff);

                // 更新索引
                AddToIndices(newBuff);

                BuffAddedOrChanged?.Invoke(newBuff);
                return true;
            }
        }

        /// <summary>
        /// 移除指定Buff（移除所有层）
        /// </summary>
        /// <param name="buffId">Buff的唯一ID</param>
        /// <param name="source">Buff来源对象，可选</param>
        /// <returns>返回自身以便链式调用</returns>
        public BuffManager RemoveBuff(string buffId, string layer = null, object source = null)
        {
            return RemoveBuffStack(buffId, int.MaxValue, layer, source);
        }

        /// <summary>
        /// 移除指定Buff的指定层数
        /// </summary>
        /// <param name="buffId">Buff的唯一ID</param>
        /// <param name="stack">要移除的层数，默认为1</param>
        /// <param name="source">Buff来源对象，可选</param>
        /// <returns>返回自身以便链式调用</returns>
        public BuffManager RemoveBuffStack(string buffId, int stack = 1, string layer = null, object source = null)
        {
            if (string.IsNullOrEmpty(buffId)) return this;

            IRPGBuff buff;
            if (layer != null)
            {
                // 按Layer和BuffID查找
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
                // 按BuffID查找
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
                // 叠加型Buff只减少层数，不足则移除整个Buff
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
        /// 获取指定Buff，可按Layer查询
        /// </summary>
        /// <param name="buffId">Buff的唯一ID</param>
        /// <param name="layer">Buff分组，可选。为空则不按Layer过滤</param>
        /// <param name="source">Buff来源对象，可选</param>
        /// <returns>返回找到的Buff实例，未找到返回null</returns>
        public IRPGBuff GetBuff(string buffId, string layer = null, object source = null)
        {
            if (string.IsNullOrEmpty(buffId)) return null;

            // 使用缓存字典快速查找
            if (!string.IsNullOrEmpty(layer))
            {
                // 按Layer和BuffID查找
                if (_buffsByLayerAndID.TryGetValue(layer, out var idMap) &&
                    idMap.TryGetValue(buffId, out var buffsInLayer))
                {
                    return buffsInLayer.Find(b => source == null || b.Source == source);
                }
            }
            else
            {
                // 仅按BuffID查找
                if (_buffsByID.TryGetValue(buffId, out var buffs))
                {
                    return buffs.Find(b => (source == null || b.Source == source));
                }
            }
            return null;
        }



        /// <summary>
        /// 清空所有Buff
        /// </summary>
        /// <returns>返回自身</returns>
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

            // 倒序遍历，移除时不会影响索引
            for (int i = _buffs.Count - 1; i >= 0; i--)
            {
                var buff = _buffs[i];

                // 触发型Buff尝试触发
                if (buff is TriggerableBuff triggerableBuff)
                {
                    triggerableBuff.TryTrigger();
                }

                // 处理周期性Buff
                if (buff is PeriodicBuff periodicBuff)
                {
                    periodicBuff.UpdateTick(deltaTime);
                }

                // 只处理有持续时间的Buff
                if (buff.Duration > 0)
                {
                    buff.Elapsed += deltaTime;
                    if (buff.IsExpired)
                    {
                        // 支持每次过期只移除一层
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

            // 统一触发过期事件
            if (BuffExpired != null)
            {
                foreach (var buff in expiredBuffs)
                {
                    BuffExpired.Invoke(buff);
                }
            }
            return this;
        }



        #region 索引
        // <summary>
        /// 将Buff添加到索引中
        /// </summary>
        private void AddToIndices(IRPGBuff buff)
        {
            // 添加到ID索引
            if (!_buffsByID.TryGetValue(buff.BuffID, out var buffs))
            {
                buffs = new List<IRPGBuff>();
                _buffsByID[buff.BuffID] = buffs;
            }
            buffs.Add(buff);

            // 如果有Layer，添加到Layer索引
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
        /// 从索引中移除Buff
        /// </summary>
        private void RemoveFromIndices(IRPGBuff buff)
        {
            // 从ID索引中移除
            if (_buffsByID.TryGetValue(buff.BuffID, out var buffs))
            {
                buffs.Remove(buff);
                if (buffs.Count == 0)
                {
                    _buffsByID.Remove(buff.BuffID);
                }
            }

            // 如果有Layer，从Layer索引中移除
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