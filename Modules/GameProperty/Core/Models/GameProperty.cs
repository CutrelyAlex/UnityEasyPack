using EasyPack.Modifiers;
using System;
using System.Collections.Generic;
using System.Linq;


namespace EasyPack.GamePropertySystem
{
    /// <summary>
    /// 基于float数值的游戏属性类
    /// 支持修饰符系统，依赖系统，脏标记系统
    /// 可用于实现角色属性，装备属性，buff/debuff等各种游戏数值计算
    /// </summary>
    public class GameProperty : IModifiableProperty<float>, IDirtyTrackable
    {
        // 比较大小的静态常量
        private const float EPSILON = 0.0001f;

        #region 基础属性值

        /// <summary>
        /// 属性的唯一标识符
        /// </summary>
        public string ID { get; set; }

        private float _baseValue;
        private float _cacheValue;

        /// <summary>
        /// 依赖管理器
        /// </summary>
        internal PropertyDependencyManager DependencyManager { get; private set; }

        /// <summary>
        /// 创建一个 <see cref="GameProperty"/> 的新实例
        /// </summary>
        /// <param name="initValue">属性的初始基础值</param>
        /// <param name="id">属性的唯一标识</param>
        public GameProperty(string id, float initValue)
        {
            _baseValue = initValue;
            _cacheValue = initValue;
            ID = id;
            Modifiers = new List<IModifier>();
            DependencyManager = new PropertyDependencyManager(this);
            MakeDirty();
        }

        /// <summary>
        /// 获取属性的基础值（未应用修饰符）
        /// </summary>
        /// <returns>基础值的float数值</returns>
        public float GetBaseValue() => _baseValue;

        /// <summary>
        /// 获取属性的最终值（应用所有修饰符后）
        /// </summary>
        /// <returns>最终值的float数值</returns>
        public float GetValue()
        {
            // 检查是否需要重新计算：
            // 1. 有非Clamp的RangeModifier（随机性）
            // 2. 有随机依赖
            // 3. 自身脏了（包含依赖项传播过来的脏标记）
            bool needsRecalculation = _hasNonClampRangeModifier
                || DependencyManager.HasRandomDependency
                || _isDirty;

            if (!needsRecalculation)
                return _cacheValue;

            // 更新依赖项（如果有依赖项）
            if (DependencyManager.DependencyCount > 0)
            {
                DependencyManager.UpdateDependencies();
            }

            var oldValue = _cacheValue;
            var ret = _baseValue;

            // 应用修饰符
            ApplyModifiers(ref ret);
            _cacheValue = ret;

            // 如果没有随机性依赖项则清理脏标记
            if (!(_hasNonClampRangeModifier || DependencyManager.HasRandomDependency))
                _isDirty = false;

            if (Math.Abs(oldValue - _cacheValue) > EPSILON)
            {
                OnValueChanged?.Invoke(oldValue, _cacheValue);

                // 检查是否有依赖者再触发更新
                if (DependencyManager.DependentCount > 0)
                {
                    DependencyManager.TriggerDependentUpdates(_cacheValue);
                }
            }

            return _cacheValue;
        }

        /// <summary>
        /// 设置属性的基础值并触发重新计算
        /// 这会影响最终的计算结果
        /// </summary>
        /// <param name="value">新的基础值</param>
        public IModifiableProperty<float> SetBaseValue(float value)
        {
            if (Math.Abs(_baseValue - value) > EPSILON)
            {
                var oldBaseValue = _baseValue;
                _baseValue = value;
                MakeDirty();

                // 触发基础值变化事件
                OnBaseValueChanged?.Invoke(oldBaseValue, _baseValue);

                // 立即计算以确保事件和依赖更新正确触发
                GetValue();
            }
            return this;
        }
        #endregion

        #region 依赖系统       

        /// <summary>
        /// 属性最终值改变时的事件（应用修饰符后的值变化）
        /// 仅在GetValue()计算后值实际变化时触发
        /// </summary>
        public event Action<float, float> OnValueChanged;

        /// <summary>
        /// 属性基础值改变时的事件（仅SetBaseValue触发）
        /// 用于需要区分基础值变化和修饰符导致的值变化的场景
        /// </summary>
        public event Action<float, float> OnBaseValueChanged;

        /// <summary>
        /// 添加一个依赖项，当dependency的值改变时，会调用calculator来计算新值
        /// </summary>
        /// <param name="dependency">依赖的属性</param>
        /// <param name="calculator">计算函数(dependency, newDependencyValue) => newThisValue</param>
        public IModifiableProperty<float> AddDependency(GameProperty dependency, Func<GameProperty, float, float> calculator = null)
        {
            DependencyManager.AddDependency(dependency, calculator);
            return this;
        }

        /// <summary>
        /// 添加简单依赖
        /// </summary>
        public IModifiableProperty<float> AddDependency(GameProperty dependency)
        {
            return AddDependency(dependency, null);
        }

        /// <summary>
        /// 移除依赖关系
        /// </summary>
        public IModifiableProperty<float> RemoveDependency(GameProperty dependency)
        {
            DependencyManager.RemoveDependency(dependency);
            return this;
        }

        /// <summary>
        /// 注册一个回调，在属性被标记为脏时立即计算并检查值是否变化
        /// 这是OnDirty和OnValueChanged的组合，适用于需要在修饰符变化时立即响应的场景（如UI更新）
        /// </summary>
        /// <param name="callback">回调函数，参数为(旧值, 新值)</param>
        public void OnDirtyAndValueChanged(Action<float, float> callback)
        {
            OnDirty(() =>
            {
                var oldValue = _cacheValue;
                var newValue = GetValue();
                if (Math.Abs(oldValue - newValue) > EPSILON)
                {
                    callback(oldValue, newValue);
                }
            });
        }

        /// <summary>
        /// 手动触发值计算和通知
        /// 用于批量操作修饰符后，手动触发一次计算和通知，避免多次重复计算
        /// </summary>
        /// <returns>返回当前实例以支持链式调用</returns>
        public IModifiableProperty<float> NotifyIfChanged()
        {
            GetValue();
            return this;
        }

        #endregion

        #region 脏标记系统

        private bool _isDirty = false;
        private Action _onDirty;
        private readonly HashSet<Action> _onDirtyHandlers = new();

        /// <summary>
        /// 检查属性是否处于脏状态
        /// </summary>
        public bool IsDirty => _isDirty;

        /// <summary>
        /// 将属性标记为脏状态，表示需要重新计算值
        /// </summary>
        public void MakeDirty()
        {
            bool wasAlreadyDirty = _isDirty;
            _isDirty = true;

            DependencyManager.InvalidateDirtyCache();

            _onDirty?.Invoke();

            // 只有在第一次变脏时才传播到依赖者
            if (!wasAlreadyDirty && DependencyManager.DependentCount > 0)
            {
                DependencyManager.PropagateDirtyTowardsDependents();
            }
        }

        /// <summary>
        /// 注册一个在属性变为脏状态时的回调函数
        /// </summary>
        /// <param name="action">回调函数</param>
        public void OnDirty(Action action)
        {
            if (_onDirtyHandlers.Add(action))
            {
                _onDirty += action;
            }
        }

        /// <summary>
        /// 移除脏状态变化的回调函数
        /// </summary>
        /// <param name="action">要移除的回调函数</param>
        public void RemoveOnDirty(Action action)
        {
            if (_onDirtyHandlers.Remove(action))
            {
                _onDirty -= action;
            }
        }
        #endregion

        #region 修饰符系统   

        /// <summary>
        /// 应用于此属性的所有修饰符列表
        /// </summary>
        public List<IModifier> Modifiers { get; }
        private readonly Dictionary<ModifierType, List<IModifier>> _groupedModifiers = new();
        private readonly Dictionary<IModifier, int> _modifierIndexMap = new(); // 用于快速查找和删除
        private bool _hasNonClampRangeModifier = false;
        private int _nonClampRangeModifierCount = 0;

        /// <summary>
        /// 向属性添加一个修饰符，修饰符会影响最终值
        /// </summary>
        /// <param name="modifier">要添加的修饰符</param>
        public IModifiableProperty<float> AddModifier(IModifier modifier)
        {
            // 添加到总列表并记录索引
            int index = Modifiers.Count;
            Modifiers.Add(modifier);
            _modifierIndexMap[modifier] = index;

            // 按类型分组
            if (!_groupedModifiers.TryGetValue(modifier.Type, out var list))
            {
                list = new List<IModifier>();
                _groupedModifiers[modifier.Type] = list;
            }
            list.Add(modifier);

            // 检查是否有随机性修饰符
            if (modifier is RangeModifier rm && rm.Type != ModifierType.Clamp)
            {
                _nonClampRangeModifierCount++;
                _hasNonClampRangeModifier = true;
            }

            MakeDirty();
            return this;
        }

        /// <summary>
        /// 清除所有修饰符，属性值将回到基础值
        /// </summary>
        public IModifiableProperty<float> ClearModifiers()
        {
            Modifiers.Clear();
            _groupedModifiers.Clear(); // 清理分组缓存
            _modifierIndexMap.Clear(); // 清理索引映射
            _hasNonClampRangeModifier = false; // 重置RangeModifier标记
            _nonClampRangeModifierCount = 0; // 重置计数器
            MakeDirty();
            return this;
        }

        /// <summary>
        /// 批量添加多个修饰符到属性
        /// </summary>
        /// <param name="modifiers">要添加的修饰符集合</param>
        public IModifiableProperty<float> AddModifiers(IEnumerable<IModifier> modifiers)
        {
            bool needsRangeCheck = false;

            foreach (var modifier in modifiers)
            {
                if (modifier == null)
                    throw new ArgumentNullException(nameof(modifier));

                // 添加到总列表并记录索引
                int index = Modifiers.Count;
                Modifiers.Add(modifier);
                _modifierIndexMap[modifier] = index;

                // 按类型分组
                if (!_groupedModifiers.TryGetValue(modifier.Type, out var list))
                {
                    list = new List<IModifier>();
                    _groupedModifiers[modifier.Type] = list;
                }
                list.Add(modifier);

                // 延迟检查随机性修饰符
                if (modifier is RangeModifier rm && rm.Type != ModifierType.Clamp)
                {
                    needsRangeCheck = true;
                    _nonClampRangeModifierCount++;
                }
            }

            if (needsRangeCheck)
                _hasNonClampRangeModifier = true;

            MakeDirty();
            return this;
        }

        /// <summary>
        /// 批量移除多个修饰符从属性
        /// </summary>
        /// <param name="modifiers">要移除的修饰符集合</param>
        public IModifiableProperty<float> RemoveModifiers(IEnumerable<IModifier> modifiers)
        {
            var toRemove = new List<IModifier>(modifiers);

            toRemove.Sort((a, b) =>
            {
                _modifierIndexMap.TryGetValue(b, out int indexB);
                _modifierIndexMap.TryGetValue(a, out int indexA);
                return indexB.CompareTo(indexA);
            });

            foreach (var modifier in toRemove)
            {
                if (_modifierIndexMap.ContainsKey(modifier))
                {
                    Modifiers.Remove(modifier);
                    _modifierIndexMap.Remove(modifier);

                    // 分组处理
                    if (_groupedModifiers.TryGetValue(modifier.Type, out var list))
                    {
                        list.Remove(modifier);
                        if (list.Count == 0)
                            _groupedModifiers.Remove(modifier.Type);
                    }
                }
            }

            // 重建索引映射
            _modifierIndexMap.Clear();
            for (int i = 0; i < Modifiers.Count; i++)
            {
                _modifierIndexMap[Modifiers[i]] = i;
            }

            _hasNonClampRangeModifier = HasNonClampRangeModifiers();
            MakeDirty();
            return this;
        }

        /// <summary>
        /// 从属性中移除一个特定的修饰符
        /// </summary>
        /// <param name="modifier">要移除的修饰符</param>
        public IModifiableProperty<float> RemoveModifier(IModifier modifier)
        {
            if (!_modifierIndexMap.TryGetValue(modifier, out int index))
                return this;

            int lastIndex = Modifiers.Count - 1;
            if (index != lastIndex)
            {
                var lastModifier = Modifiers[lastIndex];
                Modifiers[index] = lastModifier;
                _modifierIndexMap[lastModifier] = index;
            }

            Modifiers.RemoveAt(lastIndex);
            _modifierIndexMap.Remove(modifier);

            // 从分组中移除
            if (_groupedModifiers.TryGetValue(modifier.Type, out var list))
            {
                list.Remove(modifier);
                if (list.Count == 0)
                    _groupedModifiers.Remove(modifier.Type);
            }

            // 检查是否有随机性修饰符
            if (modifier is RangeModifier rm && rm.Type != ModifierType.Clamp)
            {
                _nonClampRangeModifierCount--;
                _hasNonClampRangeModifier = _nonClampRangeModifierCount > 0;
            }

            MakeDirty();
            return this;
        }

        /// <summary>
        /// 检查是否有非Clamp类型的RangeModifier
        /// </summary>
        /// <returns>如果存在返回true，否则返回false</returns>
        internal bool HasNonClampRangeModifiers()
        {
            return Modifiers.OfType<RangeModifier>().Any(m => m.Type != ModifierType.Clamp);
        }

        // 策略缓存
        private static readonly Dictionary<ModifierType, IModifierStrategy> _cachedStrategies = new();

        // 获取缓存的策略
        private static IModifierStrategy GetCachedStrategy(ModifierType type)
        {
            if (!_cachedStrategies.TryGetValue(type, out var strategy))
            {
                strategy = ModifierStrategyManager.GetStrategy(type);
                _cachedStrategies[type] = strategy;
            }
            return strategy;
        }

        private void ApplyModifiers(ref float value)
        {
            if (_groupedModifiers.Count == 0)
                return;

            // 使用预定义的顺序数组
            foreach (ModifierType type in ModifierStrategyManager.MODIFIER_TYPE_ORDER)
            {
                if (_groupedModifiers.TryGetValue(type, out var modifiers) && modifiers.Count > 0)
                {
                    var strategy = GetCachedStrategy(type);
                    strategy.Apply(ref value, modifiers);
                }
            }
        }
        #endregion

        #region 查询
        /// <summary>
        /// 检查是否有任何修饰符
        /// </summary>
        public bool HasModifiers => Modifiers.Count > 0;

        /// <summary>
        /// 获取修饰符总数
        /// </summary>
        public int ModifierCount => Modifiers.Count;

        /// <summary>
        /// 检查是否包含指定类型的修饰符
        /// </summary>
        public bool ContainModifierOfType(ModifierType type)
        {
            return _groupedModifiers.ContainsKey(type) && _groupedModifiers[type].Count > 0;
        }

        /// <summary>
        /// 获取指定类型的修饰符数量
        /// </summary>
        public int GetModifierCountOfType(ModifierType type)
        {
            return _groupedModifiers.TryGetValue(type, out var list) ? list.Count : 0;
        }
        #endregion
    }
}
