using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasyPack
{
    /// <summary>
    /// Buff生命周期管理器，负责Buff的创建、更新、移除和查询
    /// 不直接操作IProperty，Buff的应用与移除由BuffHandle负责
    /// </summary>
    public class BuffManager
    {
        #region 核心数据结构

        // 主要存储结构
        private readonly Dictionary<object, List<Buff>> _targetToBuffs = new Dictionary<object, List<Buff>>();
        private readonly List<Buff> _allBuffs = new List<Buff>();
        private readonly List<Buff> _removedBuffs = new List<Buff>();

        // 按生命周期分类的Buff列表
        private readonly List<Buff> _timedBuffs = new List<Buff>();
        private readonly List<Buff> _permanentBuffs = new List<Buff>();

        // 更新循环缓存
        private readonly List<Buff> _triggeredBuffs = new List<Buff>();
        private readonly List<BuffModule> _moduleCache = new List<BuffModule>();

        // 快速查找索引
        private readonly Dictionary<string, List<Buff>> _buffsByID = new Dictionary<string, List<Buff>>();
        private readonly Dictionary<string, List<Buff>> _buffsByTag = new Dictionary<string, List<Buff>>();
        private readonly Dictionary<string, List<Buff>> _buffsByLayer = new Dictionary<string, List<Buff>>();

        // 位置索引用于快速移除
        private readonly Dictionary<Buff, int> _buffPositions = new Dictionary<Buff, int>();
        private readonly Dictionary<Buff, int> _timedBuffPositions = new Dictionary<Buff, int>();
        private readonly Dictionary<Buff, int> _permanentBuffPositions = new Dictionary<Buff, int>();

        // 批量移除优化
        private readonly HashSet<Buff> _buffsToRemove = new HashSet<Buff>();
        private readonly List<int> _removalIndices = new List<int>();

        #endregion

        #region Buff创建与添加

        /// <summary>
        /// 创建并添加新的 Buff，处理重复 ID 的叠加策略
        /// </summary>
        /// <param name="buffData">Buff 配置数据</param>
        /// <param name="creator">创建 Buff 的游戏对象</param>
        /// <param name="target">Buff 应用的目标对象</param>
        /// <returns>创建或更新的 Buff 实例，失败返回 null</returns>
        public Buff CreateBuff(BuffData buffData, GameObject creator, GameObject target)
        {
            if (buffData == null)
            {
                Debug.LogError("BuffData不能为null");
                return null;
            }
            if (target == null)
            {
                Debug.LogError("Target不能为null");
                return null;
            }

            if (!_targetToBuffs.TryGetValue(target, out List<Buff> buffs))
            {
                buffs = new List<Buff>();
                _targetToBuffs[target] = buffs;
            }

            // 检查是否存在相同ID的Buff，处理叠加逻辑
            Buff existingBuff = buffs.FirstOrDefault(b => b.BuffData.ID == buffData.ID);
            if (existingBuff != null)
            {
                // 处理持续时间叠加策略
                switch (buffData.BuffSuperpositionStrategy)
                {
                    case BuffSuperpositionDurationType.Add:
                        existingBuff.DurationTimer += buffData.Duration;
                        break;
                    case BuffSuperpositionDurationType.ResetThenAdd:
                        existingBuff.DurationTimer = 2 * buffData.Duration;
                        break;
                    case BuffSuperpositionDurationType.Reset:
                        existingBuff.DurationTimer = buffData.Duration;
                        break;
                    case BuffSuperpositionDurationType.Keep:
                        break;
                }

                // 处理堆叠数叠加策略
                switch (buffData.BuffSuperpositionStacksStrategy)
                {
                    case BuffSuperpositionStacksType.Add:
                        IncreaseBuffStacks(existingBuff);
                        break;
                    case BuffSuperpositionStacksType.ResetThenAdd:
                        existingBuff.CurrentStacks = 1;
                        IncreaseBuffStacks(existingBuff);
                        break;
                    case BuffSuperpositionStacksType.Reset:
                        existingBuff.CurrentStacks = 1;
                        break;
                    case BuffSuperpositionStacksType.Keep:
                        break;
                }

                return existingBuff;
            }
            else
            {
                // 创建全新的Buff实例
                Buff buff = new()
                {
                    BuffData = buffData,
                    Creator = creator,
                    Target = target,
                    DurationTimer = buffData.Duration > 0 ? buffData.Duration : -1f,
                    TriggerTimer = buffData.TriggerInterval,
                    CurrentStacks = 1
                };

                // 设置 BuffModules 的父级引用
                foreach (var module in buffData.BuffModules)
                {
                    module.SetParentBuff(buff);
                }

                // 添加到各种管理列表和索引
                buffs.Add(buff);
                _buffPositions[buff] = _allBuffs.Count;
                _allBuffs.Add(buff);

                // 根据持续时间分类存储
                if (buff.DurationTimer > 0)
                {
                    _timedBuffPositions[buff] = _timedBuffs.Count;
                    _timedBuffs.Add(buff);
                }
                else
                {
                    _permanentBuffPositions[buff] = _permanentBuffs.Count;
                    _permanentBuffs.Add(buff);
                }

                RegisterBuffInIndexes(buff);

                // 执行创建回调
                buff.OnCreate?.Invoke(buff);
                InvokeBuffModules(buff, BuffCallBackType.OnCreate);

                // 处理创建时立即触发
                if (buffData.TriggerOnCreate)
                {
                    buff.OnTrigger?.Invoke(buff);
                    InvokeBuffModules(buff, BuffCallBackType.OnTick);
                }

                return buff;
            }
        }

        /// <summary>
        /// 将Buff添加到快速查找索引中
        /// </summary>
        private void RegisterBuffInIndexes(Buff buff)
        {
            // 添加到ID索引
            if (!_buffsByID.TryGetValue(buff.BuffData.ID, out var idList))
            {
                idList = new List<Buff>();
                _buffsByID[buff.BuffData.ID] = idList;
            }
            idList.Add(buff);

            // 添加到标签索引
            if (buff.BuffData.Tags != null)
            {
                foreach (var tag in buff.BuffData.Tags)
                {
                    if (!_buffsByTag.TryGetValue(tag, out var tagList))
                    {
                        tagList = new List<Buff>();
                        _buffsByTag[tag] = tagList;
                    }
                    tagList.Add(buff);
                }
            }

            // 添加到层级索引
            if (buff.BuffData.Layers != null)
            {
                foreach (var layer in buff.BuffData.Layers)
                {
                    if (!_buffsByLayer.TryGetValue(layer, out var layerList))
                    {
                        layerList = new List<Buff>();
                        _buffsByLayer[layer] = layerList;
                    }
                    layerList.Add(buff);
                }
            }
        }

        #endregion

        #region 堆叠管理

        /// <summary>
        /// 增加 Buff 堆叠层数，不超过最大值
        /// </summary>
        /// <param name="buff">要增加堆叠的 Buff 实例（已验证非 null）</param>
        /// <param name="stack">要增加的堆叠层数</param>
        /// <returns>返回管理器自身以支持链式调用</returns>
        private BuffManager IncreaseBuffStacks(Buff buff, int stack = 1)
        {
            if (buff.CurrentStacks >= buff.BuffData.MaxStacks)
                return this;

            buff.CurrentStacks += stack;
            buff.CurrentStacks = Mathf.Min(buff.CurrentStacks, buff.BuffData.MaxStacks);

            buff.OnAddStack?.Invoke(buff);
            InvokeBuffModules(buff, BuffCallBackType.OnAddStack);

            return this;
        }

        /// <summary>
        /// 减少 Buff 堆叠层数，为 0 时移除 Buff
        /// </summary>
        /// <param name="buff">要减少堆叠的 Buff 实例（已验证非 null）</param>
        /// <param name="stack">要减少的堆叠层数</param>
        /// <returns>返回管理器自身以支持链式调用</returns>
        private BuffManager DecreaseBuffStacks(Buff buff, int stack = 1)
        {
            if (buff == null || buff.CurrentStacks <= 1)
            {
                QueueBuffForRemoval(buff);
                return this;
            }

            buff.CurrentStacks -= stack;
            if (buff.CurrentStacks <= 0)
            {
                QueueBuffForRemoval(buff);
                return this;
            }

            buff.OnReduceStack?.Invoke(buff);
            InvokeBuffModules(buff, BuffCallBackType.OnReduceStack);
            return this;
        }

        #endregion

        #region 单个Buff移除

        public BuffManager RemoveBuff(Buff buff)
        {
            if (buff == null)
                return this;

            switch (buff.BuffData.BuffRemoveStrategy)
            {
                case BuffRemoveType.All:
                    QueueBuffForRemoval(buff);
                    break;
                case BuffRemoveType.OneStack:
                    DecreaseBuffStacks(buff);
                    break;
                case BuffRemoveType.Manual:
                    break;
            }

            return this;
        }

        /// <summary>
        /// 将 Buff 加入移除队列并立即处理
        /// </summary>
        /// <param name="buff">要移除的 Buff 实例（已验证非 null）</param>
        /// <returns>返回管理器自身以支持链式调用</returns>
        private BuffManager QueueBuffForRemoval(Buff buff)
        {
            _buffsToRemove.Add(buff);
            ProcessBuffRemovals();

            return this;
        }

        #endregion

        #region 目标相关移除操作

        /// <summary>
        /// 移除目标对象上的所有 Buff
        /// </summary>
        /// <param name="target">目标对象</param>
        /// <returns>返回管理器自身以支持链式调用</returns>
        public BuffManager RemoveAllBuffs(object target)
        {
            if (_targetToBuffs.TryGetValue(target, out List<Buff> buffs))
            {
                foreach (var buff in buffs)
                {
                    _buffsToRemove.Add(buff);
                }
                ProcessBuffRemovals();
            }
            return this;
        }

        /// <summary>
        /// 根据 ID 移除目标对象上的 Buff
        /// </summary>
        /// <param name="target">目标对象</param>
        /// <param name="buffID">Buff 的 ID</param>
        /// <returns>返回管理器自身以支持链式调用</returns>
        public BuffManager RemoveBuffByID(object target, string buffID)
        {
            if (_targetToBuffs.TryGetValue(target, out List<Buff> buffs))
            {
                Buff buff = buffs.FirstOrDefault(b => b.BuffData.ID == buffID);
                if (buff != null)
                {
                    _buffsToRemove.Add(buff);
                    ProcessBuffRemovals();
                }
            }
            return this;
        }

        public BuffManager RemoveBuffsByTag(object target, string tag)
        {
            if (_targetToBuffs.TryGetValue(target, out List<Buff> buffs))
            {
                foreach (var buff in buffs.Where(b => b.BuffData.HasTag(tag)))
                {
                    _buffsToRemove.Add(buff);
                }
                ProcessBuffRemovals();
            }
            return this;
        }

        public BuffManager RemoveBuffsByLayer(object target, string layer)
        {
            if (_targetToBuffs.TryGetValue(target, out List<Buff> buffs))
            {
                foreach (var buff in buffs.Where(b => b.BuffData.InLayer(layer)))
                {
                    _buffsToRemove.Add(buff);
                }
                ProcessBuffRemovals();
            }
            return this;
        }

        #endregion

        #region 全局移除操作

        public BuffManager RemoveAllBuffsByID(string buffID)
        {
            if (_buffsByID.TryGetValue(buffID, out var buffs))
            {
                foreach (var buff in buffs)
                {
                    _buffsToRemove.Add(buff);
                }
                ProcessBuffRemovals();
            }
            return this;
        }

        public BuffManager RemoveAllBuffsByTag(string tag)
        {
            if (_buffsByTag.TryGetValue(tag, out var buffs))
            {
                foreach (var buff in buffs)
                {
                    _buffsToRemove.Add(buff);
                }
                ProcessBuffRemovals();
            }
            return this;
        }

        public BuffManager RemoveAllBuffsByLayer(string layer)
        {
            if (_buffsByLayer.TryGetValue(layer, out var buffs))
            {
                foreach (var buff in buffs)
                {
                    _buffsToRemove.Add(buff);
                }
                ProcessBuffRemovals();
            }
            return this;
        }

        public BuffManager FlushPendingRemovals()
        {
            ProcessBuffRemovals();
            return this;
        }

        #endregion

        #region 批量移除核心实现

        /// <summary>
        /// 批量移除Buff的核心实现，处理回调和索引更新
        /// </summary>
        private void ProcessBuffRemovals()
        {
            if (_buffsToRemove.Count == 0)
                return;

            // 执行移除回调
            foreach (var buff in _buffsToRemove)
            {
                buff.OnRemove?.Invoke(buff);
                InvokeBuffModules(buff, BuffCallBackType.OnRemove);

                buff.OnCreate = null;
                buff.OnRemove = null;
                buff.OnAddStack = null;
                buff.OnReduceStack = null;
                buff.OnUpdate = null;
                buff.OnTrigger = null;
            }

            // 批量从各个列表移除
            BatchRemoveFromList(_allBuffs, _buffPositions, _buffsToRemove);
            BatchRemoveFromList(_timedBuffs, _timedBuffPositions, _buffsToRemove);
            BatchRemoveFromList(_permanentBuffs, _permanentBuffPositions, _buffsToRemove);

            // 从目标索引移除
            var targetGroups = _buffsToRemove.GroupBy(b => b.Target);
            foreach (var group in targetGroups)
            {
                if (_targetToBuffs.TryGetValue(group.Key, out List<Buff> targetBuffs))
                {
                    foreach (var buff in group)
                    {
                        SwapRemoveFromList(targetBuffs, buff);
                    }

                    if (targetBuffs.Count == 0)
                    {
                        _targetToBuffs.Remove(group.Key);
                    }
                }
            }

            // 从快速查找索引移除
            foreach (var buff in _buffsToRemove)
            {
                UnregisterBuffFromIndexes(buff);
            }

            _buffsToRemove.Clear();
        }

        private void UnregisterBuffFromIndexes(Buff buff)
        {
            // 从ID索引中移除
            if (_buffsByID.TryGetValue(buff.BuffData.ID, out var idList))
            {
                SwapRemoveFromList(idList, buff);
                if (idList.Count == 0)
                {
                    _buffsByID.Remove(buff.BuffData.ID);
                }
            }

            // 从标签索引中移除
            if (buff.BuffData.Tags != null)
            {
                foreach (var tag in buff.BuffData.Tags)
                {
                    if (_buffsByTag.TryGetValue(tag, out var tagList))
                    {
                        SwapRemoveFromList(tagList, buff);
                        if (tagList.Count == 0)
                        {
                            _buffsByTag.Remove(tag);
                        }
                    }
                }
            }

            // 从层级索引中移除
            if (buff.BuffData.Layers != null)
            {
                foreach (var layer in buff.BuffData.Layers)
                {
                    if (_buffsByLayer.TryGetValue(layer, out var layerList))
                    {
                        SwapRemoveFromList(layerList, buff);
                        if (layerList.Count == 0)
                        {
                            _buffsByLayer.Remove(layer);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 批量从带位置索引的列表中移除元素，使用O(1)的swap-remove优化
        /// </summary>
        private void BatchRemoveFromList(List<Buff> list, Dictionary<Buff, int> positions, HashSet<Buff> itemsToRemove)
        {
            if (list.Count == 0 || itemsToRemove.Count == 0)
                return;

            _removalIndices.Clear();

            // 收集需要移除的索引
            foreach (var item in itemsToRemove)
            {
                if (positions.TryGetValue(item, out int index))
                {
                    _removalIndices.Add(index);
                }
            }

            if (_removalIndices.Count == 0)
                return;

            // 从高到低排序索引，避免移除时索引变化
            _removalIndices.Sort((a, b) => b.CompareTo(a));

            // 批量移除
            foreach (int index in _removalIndices)
            {
                if (index < list.Count)
                {
                    Buff removedBuff = list[index];
                    int lastIndex = list.Count - 1;

                    if (index != lastIndex)
                    {
                        // swap-remove优化：用最后元素替换当前元素
                        Buff lastBuff = list[lastIndex];
                        list[index] = lastBuff;
                        positions[lastBuff] = index;
                    }

                    list.RemoveAt(lastIndex);
                    positions.Remove(removedBuff);
                }
            }
        }

        /// <summary>
        /// 快速从无位置索引的列表中移除元素
        /// </summary>
        private void SwapRemoveFromList(List<Buff> list, Buff item)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == item)
                {
                    // swap-remove优化
                    list[i] = list[list.Count - 1];
                    list.RemoveAt(list.Count - 1);
                    break;
                }
            }
        }

        #endregion

        #region 目标查询操作

        public bool ContainsBuff(object target, string buffID)
        {
            if (target == null || string.IsNullOrEmpty(buffID))
                return false;
            if (_targetToBuffs.TryGetValue(target, out List<Buff> buffs))
            {
                return buffs.Any(b => b.BuffData.ID == buffID);
            }
            return false;
        }

        public Buff GetBuff(object target, string buffID)
        {
            if (target == null || string.IsNullOrEmpty(buffID))
                return null;
            if (_targetToBuffs.TryGetValue(target, out List<Buff> buffs))
            {
                return buffs.FirstOrDefault(b => b.BuffData.ID == buffID);
            }
            return null;
        }

        public List<Buff> GetTargetBuffs(object target)
        {
            if (target == null)
                return new List<Buff>();

            if (_targetToBuffs.TryGetValue(target, out List<Buff> buffs))
            {
                return new List<Buff>(buffs);
            }
            return new List<Buff>();
        }

        public List<Buff> GetBuffsByTag(object target, string tag)
        {
            if (target == null || string.IsNullOrEmpty(tag))
                return new List<Buff>();

            if (_targetToBuffs.TryGetValue(target, out List<Buff> buffs))
            {
                return buffs.Where(b => b.BuffData.HasTag(tag)).ToList();
            }
            return new List<Buff>();
        }

        public List<Buff> GetBuffsByLayer(object target, string layer)
        {
            if (target == null || string.IsNullOrEmpty(layer))
                return new List<Buff>();

            if (_targetToBuffs.TryGetValue(target, out List<Buff> buffs))
            {
                return buffs.Where(b => b.BuffData.InLayer(layer)).ToList();
            }
            return new List<Buff>();
        }

        #endregion

        #region 全局查询操作

        public List<Buff> GetAllBuffsByID(string buffID)
        {
            if (_buffsByID.TryGetValue(buffID, out var buffs))
            {
                return new List<Buff>(buffs);
            }
            return new List<Buff>();
        }

        public List<Buff> GetAllBuffsByTag(string tag)
        {
            if (_buffsByTag.TryGetValue(tag, out var buffs))
            {
                return new List<Buff>(buffs);
            }
            return new List<Buff>();
        }

        public List<Buff> GetAllBuffsByLayer(string layer)
        {
            if (_buffsByLayer.TryGetValue(layer, out var buffs))
            {
                return new List<Buff>(buffs);
            }
            return new List<Buff>();
        }

        public bool ContainsBuffWithID(string buffID)
        {
            return _buffsByID.ContainsKey(buffID) && _buffsByID[buffID].Count > 0;
        }

        public bool ContainsBuffWithTag(string tag)
        {
            return _buffsByTag.ContainsKey(tag) && _buffsByTag[tag].Count > 0;
        }

        public bool ContainsBuffWithLayer(string layer)
        {
            return _buffsByLayer.ContainsKey(layer) && _buffsByLayer[layer].Count > 0;
        }

        #endregion

        #region 更新循环

        /// <summary>
        /// 主更新循环，处理时间Buff和永久Buff的时间更新与触发
        /// </summary>
        public BuffManager Update(float deltaTime)
        {
            _removedBuffs.Clear();
            _triggeredBuffs.Clear();

            // 更新有时间限制的Buff
            ProcessTimedBuffs(deltaTime);

            // 更新永久Buff的触发时间
            ProcessPermanentBuffs(deltaTime);

            // 批量执行触发和更新回调
            ExecuteTriggeredBuffs();
            ExecuteBuffUpdates();

            // 移除过期的Buff
            foreach (var buff in _removedBuffs)
            {
                QueueBuffForRemoval(buff);
            }

            return this;
        }

        private void ProcessTimedBuffs(float deltaTime)
        {
            for (int i = _timedBuffs.Count - 1; i >= 0; i--)
            {
                var buff = _timedBuffs[i];

                // 更新持续时间
                buff.DurationTimer -= deltaTime;
                if (buff.DurationTimer <= 0)
                {
                    _removedBuffs.Add(buff);
                    continue;
                }

                // 检查触发间隔
                buff.TriggerTimer -= deltaTime;
                if (buff.TriggerTimer <= 0)
                {
                    buff.TriggerTimer = buff.BuffData.TriggerInterval;
                    _triggeredBuffs.Add(buff);
                }
            }
        }

        private void ProcessPermanentBuffs(float deltaTime)
        {
            for (int i = 0; i < _permanentBuffs.Count; i++)
            {
                var buff = _permanentBuffs[i];

                // 检查触发间隔
                buff.TriggerTimer -= deltaTime;
                if (buff.TriggerTimer <= 0)
                {
                    buff.TriggerTimer = buff.BuffData.TriggerInterval;
                    _triggeredBuffs.Add(buff);
                }
            }
        }

        private void ExecuteTriggeredBuffs()
        {
            foreach (var buff in _triggeredBuffs)
            {
                buff.OnTrigger?.Invoke(buff);
                InvokeBuffModules(buff, BuffCallBackType.OnTick);
            }
        }

        private void ExecuteBuffUpdates()
        {
            // 处理有时间限制的Buff
            foreach (var buff in _timedBuffs)
            {
                if (!_removedBuffs.Contains(buff))
                {
                    buff.OnUpdate?.Invoke(buff);
                    InvokeBuffModules(buff, BuffCallBackType.OnUpdate);
                }
            }

            // 处理永久Buff
            foreach (var buff in _permanentBuffs)
            {
                buff.OnUpdate?.Invoke(buff);
                InvokeBuffModules(buff, BuffCallBackType.OnUpdate);
            }
        }

        #endregion

        #region 模块执行系统

        /// <summary>
        /// 执行Buff模块，支持优先级排序和条件筛选
        /// </summary>
        public void InvokeBuffModules(Buff buff, BuffCallBackType callBackType, string customCallbackName = "", params object[] parameters)
        {
            if (buff.BuffData.BuffModules == null || buff.BuffData.BuffModules.Count == 0)
                return;

            // 筛选需要执行的模块
            _moduleCache.Clear();
            var modules = buff.BuffData.BuffModules;
            for (int i = 0; i < modules.Count; i++)
            {
                if (modules[i].ShouldExecute(callBackType, customCallbackName))
                {
                    _moduleCache.Add(modules[i]);
                }
            }

            // 按优先级插入排序
            for (int i = 1; i < _moduleCache.Count; i++)
            {
                var key = _moduleCache[i];
                int j = i - 1;

                while (j >= 0 && _moduleCache[j].Priority < key.Priority)
                {
                    _moduleCache[j + 1] = _moduleCache[j];
                    j--;
                }
                _moduleCache[j + 1] = key;
            }

            // 执行模块
            for (int i = 0; i < _moduleCache.Count; i++)
            {
                _moduleCache[i].Execute(buff, callBackType, customCallbackName, parameters);
            }
        }

        #endregion
    }
}