using System.Collections.Generic;
using System.Threading.Tasks;
using EasyPack.ENekoFramework;
using UnityEngine;

namespace EasyPack.BuffSystem
{
    /// <summary>
    ///     Buff生命周期管理器，负责Buff的创建、更新、移除和查询
    ///     实现IBuffService接口以集成到EasyPack架构中
    /// </summary>
    public class BuffService : BaseService, IBuffService
    {
        #region IService 生命周期

        /// <summary>
        ///     服务初始化钩子方法
        ///     派生类应重写此方法以实现自定义初始化逻辑
        /// </summary>
        protected override async Task OnInitializeAsync()
        {
            await base.OnInitializeAsync();
            // 预留异步初始化
        }

        /// <summary>
        ///     服务释放钩子方法
        /// </summary>
        protected override async Task OnDisposeAsync()
        {
            _targetToBuffs?.Clear();
            _allBuffs?.Clear();
            _removedBuffs?.Clear();
            _timedBuffs?.Clear();
            _permanentBuffs?.Clear();
            _triggeredBuffs?.Clear();
            _moduleCache?.Clear();
            _buffsByID?.Clear();
            _buffsByTag?.Clear();
            _buffsByLayer?.Clear();
            _buffPositions?.Clear();
            _timedBuffPositions?.Clear();
            _permanentBuffPositions?.Clear();
            _buffsToRemove?.Clear();
            _removalIndices?.Clear();

            await base.OnDisposeAsync();
        }

        #endregion

        #region 核心数据结构

        // 主要存储结构
        private readonly Dictionary<object, List<Buff>> _targetToBuffs = new();
        private readonly List<Buff> _allBuffs = new();
        private readonly List<Buff> _removedBuffs = new();

        // 按生命周期分类的Buff列表
        private readonly List<Buff> _timedBuffs = new();
        private readonly List<Buff> _permanentBuffs = new();

        // 更新循环缓存
        private readonly List<Buff> _triggeredBuffs = new();
        private readonly List<BuffModule> _moduleCache = new();

        // 快速查找索引
        private readonly Dictionary<string, List<Buff>> _buffsByID = new();
        private readonly Dictionary<string, List<Buff>> _buffsByTag = new();
        private readonly Dictionary<string, List<Buff>> _buffsByLayer = new();

        // 位置索引用于快速移除
        private readonly Dictionary<Buff, int> _buffPositions = new();
        private readonly Dictionary<Buff, int> _timedBuffPositions = new();
        private readonly Dictionary<Buff, int> _permanentBuffPositions = new();

        // 批量移除优化
        private readonly HashSet<Buff> _buffsToRemove = new();
        private readonly List<int> _removalIndices = new();

        #endregion

        #region Buff创建与添加

        /// <summary>
        ///     创建并添加新的 Buff，处理重复 ID 的叠加策略
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

            if (!_targetToBuffs.TryGetValue(target, out var buffs))
            {
                buffs = new();
                _targetToBuffs[target] = buffs;
            }

            // 检查是否存在相同ID的Buff，处理叠加逻辑
            Buff existingBuff = null;
            foreach (Buff b in buffs)
            {
                if (b.BuffData.ID == buffData.ID)
                {
                    existingBuff = b;
                    break;
                }
            }

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

            // 创建全新的Buff实例
            Buff buff = new()
            {
                BuffData = buffData,
                Creator = creator,
                Target = target,
                DurationTimer = buffData.Duration > 0 ? buffData.Duration : BuffData.InfiniteDuration,
                TriggerTimer = buffData.TriggerInterval,
                CurrentStacks = 1,
            };

            // 设置 BuffModules 的父级引用
            foreach (BuffModule module in buffData.BuffModules)
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

        /// <summary>
        ///     将Buff添加到快速查找索引中
        /// </summary>
        private void RegisterBuffInIndexes(Buff buff)
        {
            // 添加到ID索引
            if (!_buffsByID.TryGetValue(buff.BuffData.ID, out var idList))
            {
                idList = new();
                _buffsByID[buff.BuffData.ID] = idList;
            }

            idList.Add(buff);

            // 添加到标签索引
            if (buff.BuffData.Tags != null)
            {
                foreach (string tag in buff.BuffData.Tags)
                {
                    if (!_buffsByTag.TryGetValue(tag, out var tagList))
                    {
                        tagList = new();
                        _buffsByTag[tag] = tagList;
                    }

                    tagList.Add(buff);
                }
            }

            // 添加到层级索引
            if (buff.BuffData.Layers != null)
            {
                foreach (string layer in buff.BuffData.Layers)
                {
                    if (!_buffsByLayer.TryGetValue(layer, out var layerList))
                    {
                        layerList = new();
                        _buffsByLayer[layer] = layerList;
                    }

                    layerList.Add(buff);
                }
            }
        }

        #endregion

        #region 堆叠管理

        /// <summary>
        ///     增加 Buff 堆叠层数，不超过最大值
        /// </summary>
        /// <param name="buff">要增加堆叠的 Buff 实例</param>
        /// <param name="stack">要增加的堆叠层数</param>
        /// <returns>返回管理器自身以支持链式调用</returns>
        public BuffService IncreaseBuffStacks(Buff buff, int stack = 1)
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
        ///     减少 Buff 堆叠层数，为 0 时移除 Buff
        /// </summary>
        /// <param name="buff">要减少堆叠的 Buff 实例</param>
        /// <param name="stack">要减少的堆叠层数（必须为正数）</param>
        /// <returns>返回管理器自身以支持链式调用</returns>
        public BuffService DecreaseBuffStacks(Buff buff, int stack = 1)
        {
            if (buff == null || stack <= 0)
                return this;

            // 先减少堆叠数
            buff.CurrentStacks -= stack;
            buff.CurrentStacks = Mathf.Max(buff.CurrentStacks, 0);

            // 触发事件（无论是否会被移除）
            buff.OnReduceStack?.Invoke(buff);
            InvokeBuffModules(buff, BuffCallBackType.OnReduceStack);

            // 如果堆叠数<=0，加入移除队列
            if (buff.CurrentStacks <= 0) QueueBuffForRemoval(buff);

            return this;
        }

        #endregion

        #region 单个Buff移除

        public void RemoveBuff(Buff buff)
        {
            if (buff == null)
                return;

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
        }

        /// <summary>
        ///     将 Buff 加入移除队列并立即处理
        /// </summary>
        /// <param name="buff">要移除的 Buff 实例</param>
        /// <returns>返回管理器自身以支持链式调用</returns>
        public BuffService QueueBuffForRemoval(Buff buff)
        {
            if (buff == null)
                return this;

            _buffsToRemove.Add(buff);
            ProcessBuffRemovals();

            return this;
        }

        #endregion

        #region 目标相关移除操作

        /// <summary>
        ///     移除目标对象上的所有 Buff
        /// </summary>
        /// <param name="target">目标对象</param>
        public void RemoveAllBuffs(object target)
        {
            if (_targetToBuffs.TryGetValue(target, out var buffs))
            {
                foreach (Buff buff in buffs)
                {
                    _buffsToRemove.Add(buff);
                }

                ProcessBuffRemovals();
            }
        }

        /// <summary>
        ///     根据 ID 移除目标对象上的 Buff
        /// </summary>
        /// <param name="target">目标对象</param>
        /// <param name="buffID">Buff 的 ID</param>
        public void RemoveBuffByID(object target, string buffID)
        {
            if (_targetToBuffs.TryGetValue(target, out var buffs))
            {
                Buff buff = null;
                foreach (Buff t in buffs)
                {
                    if (t.BuffData.ID != buffID) continue;
                    
                    buff = t;
                    break;
                }

                if (buff != null)
                {
                    _buffsToRemove.Add(buff);
                    ProcessBuffRemovals();
                }
            }
        }

        public void RemoveBuffsByTag(object target, string tag)
        {
            if (_targetToBuffs.TryGetValue(target, out var buffs))
            {
                foreach (Buff t in buffs)
                {
                    if (t.BuffData.HasTag(tag))
                        _buffsToRemove.Add(t);
                }

                ProcessBuffRemovals();
            }
        }

        public void RemoveBuffsByLayer(object target, string layer)
        {
            if (_targetToBuffs.TryGetValue(target, out var buffs))
            {
                foreach (Buff t in buffs)
                {
                    if (t.BuffData.InLayer(layer))
                        _buffsToRemove.Add(t);
                }

                ProcessBuffRemovals();
            }
        }

        #endregion

        #region 全局移除操作

        public BuffService RemoveAllBuffsByID(string buffID)
        {
            if (_buffsByID.TryGetValue(buffID, out var buffs))
            {
                foreach (Buff buff in buffs)
                {
                    _buffsToRemove.Add(buff);
                }

                ProcessBuffRemovals();
            }

            return this;
        }

        public BuffService RemoveAllBuffsByTag(string tag)
        {
            if (_buffsByTag.TryGetValue(tag, out var buffs))
            {
                foreach (Buff buff in buffs)
                {
                    _buffsToRemove.Add(buff);
                }

                ProcessBuffRemovals();
            }

            return this;
        }

        public BuffService RemoveAllBuffsByLayer(string layer)
        {
            if (_buffsByLayer.TryGetValue(layer, out var buffs))
            {
                foreach (Buff buff in buffs)
                {
                    _buffsToRemove.Add(buff);
                }

                ProcessBuffRemovals();
            }

            return this;
        }

        public BuffService FlushPendingRemovals()
        {
            ProcessBuffRemovals();
            return this;
        }

        #endregion

        #region 批量移除核心实现

        /// <summary>
        ///     批量移除Buff的核心实现，处理回调和索引更新
        /// </summary>
        private void ProcessBuffRemovals()
        {
            if (_buffsToRemove.Count == 0)
                return;

            // 执行移除回调
            foreach (Buff buff in _buffsToRemove)
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
            var targetGroups = new Dictionary<object, List<Buff>>();
            foreach (Buff buff in _buffsToRemove)
            {
                if (!targetGroups.TryGetValue(buff.Target, out var group))
                {
                    group = new();
                    targetGroups[buff.Target] = group;
                }

                group.Add(buff);
            }

            foreach (var kvp in targetGroups)
            {
                if (_targetToBuffs.TryGetValue(kvp.Key, out var targetBuffs))
                {
                    foreach (Buff buff in kvp.Value)
                    {
                        SwapRemoveFromList(targetBuffs, buff);
                    }

                    if (targetBuffs.Count == 0) _targetToBuffs.Remove(kvp.Key);
                }
            }

            // 从快速查找索引移除
            foreach (Buff buff in _buffsToRemove)
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
                if (idList.Count == 0) _buffsByID.Remove(buff.BuffData.ID);
            }

            // 从标签索引中移除
            if (buff.BuffData.Tags != null)
            {
                foreach (string tag in buff.BuffData.Tags)
                {
                    if (_buffsByTag.TryGetValue(tag, out var tagList))
                    {
                        SwapRemoveFromList(tagList, buff);
                        if (tagList.Count == 0) _buffsByTag.Remove(tag);
                    }
                }
            }

            // 从层级索引中移除
            if (buff.BuffData.Layers != null)
            {
                foreach (string layer in buff.BuffData.Layers)
                {
                    if (_buffsByLayer.TryGetValue(layer, out var layerList))
                    {
                        SwapRemoveFromList(layerList, buff);
                        if (layerList.Count == 0) _buffsByLayer.Remove(layer);
                    }
                }
            }
        }

        /// <summary>
        ///     批量从带位置索引的列表中移除元素，使用O(1)的swap-remove优化
        /// </summary>
        private void BatchRemoveFromList(List<Buff> list, Dictionary<Buff, int> positions, HashSet<Buff> itemsToRemove)
        {
            if (list.Count == 0 || itemsToRemove.Count == 0)
                return;

            _removalIndices.Clear();

            // 收集需要移除的索引
            foreach (Buff item in itemsToRemove)
            {
                if (positions.TryGetValue(item, out int index))
                    _removalIndices.Add(index);
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
        ///     快速从无位置索引的列表中移除元素
        /// </summary>
        private void SwapRemoveFromList(List<Buff> list, Buff item)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == item)
                {
                    // swap-remove优化
                    list[i] = list[^1];
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
            if (_targetToBuffs.TryGetValue(target, out var buffs))
            {
                foreach (Buff t in buffs)
                {
                    if (t.BuffData.ID == buffID)
                        return true;
                }
            }

            return false;
        }

        public Buff GetBuff(object target, string buffID)
        {
            if (target == null || string.IsNullOrEmpty(buffID))
                return null;
            if (_targetToBuffs.TryGetValue(target, out var buffs))
            {
                foreach (Buff t in buffs)
                {
                    if (t.BuffData.ID == buffID)
                        return t;
                }
            }

            return null;
        }

        public List<Buff> GetTargetBuffs(object target)
        {
            if (target == null)
                return new();

            if (_targetToBuffs.TryGetValue(target, out var buffs)) return new(buffs);

            return new();
        }

        public List<Buff> GetBuffsByTag(object target, string tag)
        {
            if (target == null || string.IsNullOrEmpty(tag))
                return new();

            if (_targetToBuffs.TryGetValue(target, out var buffs))
            {
                var result = new List<Buff>();
                foreach (Buff t in buffs)
                {
                    if (t.BuffData.HasTag(tag))
                        result.Add(t);
                }

                return result;
            }

            return new();
        }

        public List<Buff> GetBuffsByLayer(object target, string layer)
        {
            if (target == null || string.IsNullOrEmpty(layer))
                return new();

            if (_targetToBuffs.TryGetValue(target, out var buffs))
            {
                var result = new List<Buff>();
                foreach (Buff t in buffs)
                {
                    if (t.BuffData.InLayer(layer))
                        result.Add(t);
                }

                return result;
            }

            return new();
        }

        #endregion

        #region 全局查询操作

        public List<Buff> GetAllBuffsByID(string buffID)
        {
            if (_buffsByID.TryGetValue(buffID, out var buffs)) return new(buffs);

            return new();
        }

        public List<Buff> GetAllBuffsByTag(string tag)
        {
            if (_buffsByTag.TryGetValue(tag, out var buffs)) return new(buffs);

            return new();
        }

        public List<Buff> GetAllBuffsByLayer(string layer)
        {
            if (_buffsByLayer.TryGetValue(layer, out var buffs)) return new(buffs);

            return new();
        }

        public bool ContainsBuffWithID(string buffID) => _buffsByID.ContainsKey(buffID) && _buffsByID[buffID].Count > 0;

        public bool ContainsBuffWithTag(string tag) => _buffsByTag.ContainsKey(tag) && _buffsByTag[tag].Count > 0;

        public bool ContainsBuffWithLayer(string layer) =>
            _buffsByLayer.ContainsKey(layer) && _buffsByLayer[layer].Count > 0;

        #endregion

        #region 更新循环

        /// <summary>
        ///     主更新循环，处理时间Buff和永久Buff的时间更新与触发
        /// </summary>
        public void Update(float deltaTime)
        {
            // 暂停时跳过更新
            if (State != ServiceLifecycleState.Ready)
                return;

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
            foreach (Buff buff in _removedBuffs)
            {
                QueueBuffForRemoval(buff);
            }
        }

        private void ProcessTimedBuffs(float deltaTime)
        {
            for (int i = _timedBuffs.Count - 1; i >= 0; i--)
            {
                Buff buff = _timedBuffs[i];

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
                    // 这里加上buff.TriggerTimers是为了补偿超出的时间以防止触发不均
                    // 例如当前帧TriggerTimer为0.1，deltaTime为0.3，小于0后超出的时间直接被丢弃
                    // 会导致有0.2s的时间没有被计算进下一个触发周期
                    buff.TriggerTimer = buff.BuffData.TriggerInterval + buff.TriggerTimer;
                    _triggeredBuffs.Add(buff);
                }
            }
        }

        private void ProcessPermanentBuffs(float deltaTime)
        {
            foreach (Buff buff in _permanentBuffs)
            {
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
            foreach (Buff buff in _triggeredBuffs)
            {
                buff.OnTrigger?.Invoke(buff);
                InvokeBuffModules(buff, BuffCallBackType.OnTick);
            }
        }

        private void ExecuteBuffUpdates()
        {
            // 处理有时间限制的Buff
            foreach (Buff buff in _timedBuffs)
            {
                if (!_removedBuffs.Contains(buff))
                {
                    buff.OnUpdate?.Invoke(buff);
                    InvokeBuffModules(buff, BuffCallBackType.OnUpdate);
                }
            }

            // 处理永久Buff
            foreach (Buff buff in _permanentBuffs)
            {
                buff.OnUpdate?.Invoke(buff);
                InvokeBuffModules(buff, BuffCallBackType.OnUpdate);
            }
        }

        #endregion

        #region 模块执行系统

        /// <summary>
        ///     执行Buff模块，支持优先级排序和条件筛选
        /// </summary>
        public void InvokeBuffModules(Buff buff, BuffCallBackType callBackType, string customCallbackName = "",
                                      params object[] parameters)
        {
            if (buff?.BuffData?.BuffModules?.Count == 0)
                return;

            // 筛选需要执行的模块
            _moduleCache.Clear();
            var modules = buff.BuffData.BuffModules;
            foreach (BuffModule t in modules)
            {
                if (t.ShouldExecute(callBackType, customCallbackName))
                    _moduleCache.Add(t);
            }

            // 按优先级插入排序
            for (int i = 1; i < _moduleCache.Count; i++)
            {
                BuffModule key = _moduleCache[i];
                int j = i - 1;

                while (j >= 0 && _moduleCache[j].Priority < key.Priority)
                {
                    _moduleCache[j + 1] = _moduleCache[j];
                    j--;
                }

                _moduleCache[j + 1] = key;
            }

            // 执行模块
            foreach (BuffModule t in _moduleCache)
            {
                t.Execute(buff, callBackType, customCallbackName, parameters);
            }
        }

        #endregion
    }
}