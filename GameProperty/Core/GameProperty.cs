using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
/// <summary>
/// 可被修饰的float属性。
/// 支持修饰器、依赖关系和脏数据追踪。
/// 可用于管理角色属性或其他可被buff、debuff等游戏逻辑影响的动态数值。
/// 
/// 一般情况下，优先使用CombineProperty而非GameProperty
/// </summary>

namespace EasyPack
{
    public class GameProperty : IProperty<float>
    {

        #region 基本属性

        /// <summary>
        /// 此属性的唯一标识符。
        /// </summary>
        public string ID { get; set; }

        private float _baseValue;
        private float _cacheValue;

        /// <summary>
        /// 初始化 <see cref="GameProperty"/> 类的新实例。
        /// </summary>
        /// <param name="initValue">属性的初始基础值。</param>
        /// <param name="id">属性的唯一标识符。</param>
        public GameProperty(string id, float initValue)
        {
            _baseValue = initValue;
            _cacheValue = initValue;
            ID = id;
            Modifiers = new List<IModifier>();
            MakeDirty();
        }

        /// <summary>
        /// 获取属性的基础（未修饰）值。
        /// </summary>
        /// <returns>基础值，float类型。</returns>
        public float GetBaseValue() => _baseValue;

        /// <summary>
        /// 获取应用所有修饰器和依赖后的当前属性值。
        /// </summary>
        /// <returns>计算后的float值。</returns>
        public float GetValue()
        {
            bool needsRecalculation = _hasNonClampRangeModifier || _hasRandomDependency || _isDirty;

            if (!needsRecalculation)
                return _cacheValue;

            // 仅在需要时刷新依赖
            if (_isDirty)
            {
                foreach (var dep in _dependencies)
                {
                    dep.GetValue();
                }
            }

            var oldValue = _cacheValue;
            var ret = _baseValue;

            // 修饰器应用
            ApplyModifiers(ref ret);
            _cacheValue = ret;

            // 只有在非随机情况下才清除脏标记
            if (!(_hasNonClampRangeModifier || _hasRandomDependency))
                _isDirty = false;

            if (!oldValue.Equals(_cacheValue))
            {
                OnValueChanged?.Invoke(oldValue, _cacheValue);

                TriggerDependentUpdates();
            }

            return _cacheValue;
        }

        /// <summary>
        /// 设置属性的基础（未修饰）值。
        /// 如果值发生变化会触发重新计算。
        /// </summary>
        /// <param name="value">新的基础值。</param>
        public IProperty<float> SetBaseValue(float value)
        {
            if (!Mathf.Approximately(_baseValue, value))
            {
                _baseValue = value;
                MakeDirty();
                GetValue();
            }
            return this;
        }
        #endregion

        #region 依赖       

        private readonly HashSet<GameProperty> _dependencies = new();
        private readonly HashSet<GameProperty> _dependents = new(); // 反向依赖追踪
        private readonly Dictionary<GameProperty, Func<GameProperty, float, float>> _dependencyCalculators = new();

        /// <summary>
        /// 当属性值发生变化时触发
        /// </summary>
        public event Action<float, float> OnValueChanged;

        private readonly HashSet<Action> _onDirtyHandlers = new();

        /// <summary>
        /// 添加依赖，当dependency变化时，自动使用calculator计算新值
        /// </summary>
        /// <param name="dependency">依赖的属性</param>
        /// <param name="calculator">计算函数：(dependency, newDependencyValue) => newThisValue</param>
        public IProperty<float> AddDependency(GameProperty dependency, Func<GameProperty, float, float> calculator = null)
        {
            if (dependency == null || !_dependencies.Add(dependency)) return this;

            if (WouldCreateCyclicDependency(dependency))
            {
                _dependencies.Remove(dependency);
                Debug.LogWarning($"无法添加依赖：检测到循环依赖。{ID} -> {dependency.ID}");
                return this;
            }

            // 注册反向依赖
            dependency._dependents.Add(this);

            // 存储计算器（如果提供）
            if (calculator != null)
            {
                _dependencyCalculators[dependency] = calculator;
            }

            UpdateRandomDependencyState();
            return this;
        }

        /// <summary>
        /// 添加简单依赖（仅标记为脏，不自动计算）
        /// </summary>
        public IProperty<float> AddDependency(GameProperty dependency)
        {
            return AddDependency(dependency, null);
        }

        /// <summary>
        /// 移除依赖
        /// </summary>
        public IProperty<float> RemoveDependency(GameProperty dependency)
        {
            if (!_dependencies.Remove(dependency)) return this;

            // 移除反向依赖
            dependency._dependents.Remove(this);

            // 移除计算器
            _dependencyCalculators.Remove(dependency);

            UpdateRandomDependencyState();
            return this;
        }

        /// <summary>
        /// 触发所有依赖此属性的属性更新
        /// </summary>
        private void TriggerDependentUpdates()
        {
            foreach (var dependent in _dependents)
            {
                // 如果有自定义计算器，使用它
                if (dependent._dependencyCalculators.TryGetValue(this, out var calculator))
                {
                    var newValue = calculator(this, _cacheValue);
                    dependent.SetBaseValue(newValue);
                }
                else
                {
                    // 对于简单依赖，强制触发值变化事件
                    // 即使基础值没有改变，也要让依赖属性知道需要重新计算
                    dependent.MakeDirty();

                    // 强制触发依赖属性的重新计算和事件
                    var oldValue = dependent._cacheValue;
                    var newValue = dependent.GetValue(); // 这会重新计算值

                    if (oldValue.Equals(newValue))
                    {
                        dependent.OnValueChanged?.Invoke(oldValue, newValue);
                    }
                }
            }
        }

        private bool _hasRandomDependency = false;

        private void UpdateRandomDependencyState()
        {
            _hasRandomDependency = _dependencies.Any(dep => dep.HasNonClampRangeModifiers() || dep._hasRandomDependency);
        }

        private bool WouldCreateCyclicDependency(GameProperty dependency)
        {
            if (dependency == this) return true;

            var visited = new HashSet<GameProperty>();
            var toCheck = new Queue<GameProperty>();
            toCheck.Enqueue(dependency);

            while (toCheck.Count > 0)
            {
                var current = toCheck.Dequeue();
                if (!visited.Add(current)) continue;

                if (current == this) return true;

                foreach (var dep in current._dependencies)
                {
                    toCheck.Enqueue(dep);
                }
            }

            return false;
        }
        #endregion

        #region 脏数据追踪

        private bool _isDirty = false;
        private Action _onDirty;

        /// <summary>
        /// 将属性标记为脏，下次访问时会重新计算其值。
        /// </summary>
        public void MakeDirty()
        {
            if (_isDirty) return;
            _isDirty = true;
            _onDirty?.Invoke();
        }

        /// <summary>
        /// 注册一个回调，当属性被标记为脏时调用。
        /// </summary>
        /// <param name="action">回调方法。</param>
        public void OnDirty(Action action)
        {
            if (_onDirtyHandlers.Add(action))
            {
                _onDirty += action;
            }
        }

        /// <summary>
        /// 移除已注册的脏数据回调。
        /// </summary>
        /// <param name="action">要移除的回调方法。</param>
        public void RemoveOnDirty(Action action)
        {
            if (_onDirtyHandlers.Remove(action))
            {
                _onDirty -= action;
            }
        }
        #endregion

        #region 修饰器   

        /// <summary>
        /// 获取应用于此属性的修饰器列表。
        /// </summary>
        public List<IModifier> Modifiers { get; }
        private readonly Dictionary<ModifierType, List<IModifier>> _groupedModifiers = new();
        private bool _hasNonClampRangeModifier = false;

        /// <summary>
        /// 向此属性添加一个修饰器，并标记为脏。
        /// </summary>
        /// <param name="modifier">要添加的修饰器。</param>
        public IProperty<float> AddModifier(IModifier modifier)
        {
            // 添加到总列表
            Modifiers.Add(modifier);

            // 预分组
            if (!_groupedModifiers.TryGetValue(modifier.Type, out var list))
            {
                list = new List<IModifier>();
                _groupedModifiers[modifier.Type] = list;
            }
            list.Add(modifier);

            // 预检查随机修饰器
            if (modifier is RangeModifier rm && rm.Type != ModifierType.Clamp)
            {
                _hasNonClampRangeModifier = true;
            }

            MakeDirty();
            return this;
        }

        /// <summary>
        /// 移除此属性上的所有修饰器，并标记为脏。
        /// </summary>
        public IProperty<float> ClearModifiers()
        {
            Modifiers.Clear();
            _groupedModifiers.Clear(); // 同时清除分组字典
            _hasNonClampRangeModifier = false; // 重置RangeModifier标记
            MakeDirty();
            return this;
        }

        /// <summary>
        /// 向此属性添加多个修饰器，并标记为脏。
        /// </summary>
        /// <param name="modifiers">要添加的修饰器集合。</param>
        public IProperty<float> AddModifiers(IEnumerable<IModifier> modifiers)
        {
            foreach (var modifier in modifiers)
            {
                AddModifier(modifier);
            }
            MakeDirty();
            return this;
        }

        /// <summary>
        /// 移除此属性上的多个修饰器，并标记为脏。
        /// </summary>
        /// <param name="modifiers">要移除的修饰器集合。</param>
        public IProperty<float> RemoveModifiers(IEnumerable<IModifier> modifiers)
        {
            foreach (var modifier in modifiers)
            {
                RemoveModifier(modifier);
            }

            _hasNonClampRangeModifier = HasNonClampRangeModifiers();
            MakeDirty();
            return this;
        }

        /// <summary>
        /// 移除此属性上的一个修饰器，并标记为脏。
        /// </summary>
        /// <param name="modifier">要移除的修饰器。</param>
        public IProperty<float> RemoveModifier(IModifier modifier)
        {
            var _modifier = Modifiers.Find(m => m.Equals(modifier));
            if (_modifier != null)
            {
                Modifiers.Remove(_modifier);

                // 同时从分组中移除
                if (_groupedModifiers.TryGetValue(_modifier.Type, out var list))
                {
                    list.Remove(_modifier);

                    // 如果列表为空，可以考虑移除该类型的键
                    if (list.Count == 0)
                    {
                        _groupedModifiers.Remove(_modifier.Type);
                    }
                }
            }

            // 更新非Clamp的RangeModifier标记
            _hasNonClampRangeModifier = HasNonClampRangeModifiers();

            MakeDirty();
            return this;
        }

        /// <summary>
        /// 检查是否存在非Clamp类型的RangeModifier
        /// </summary>
        /// <returns>如果存在则返回true，否则返回false</returns>
        private bool HasNonClampRangeModifiers()
        {
            return Modifiers.OfType<RangeModifier>().Any(m => m.Type != ModifierType.Clamp);
        }

        // 缓存策略
        private static readonly Dictionary<ModifierType, IModifierStrategy> _cachedStrategies = new();

        // 获取或缓存策略
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

            // 按优先级应用修饰器类型
            foreach (ModifierType type in Enum.GetValues(typeof(ModifierType)))
            {
                if (type == ModifierType.None) continue;

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
        /// 检查属性是否有任何修饰器
        /// </summary>
        public bool HasModifiers => Modifiers.Count > 0;

        /// <summary>
        /// 获取修饰器数量
        /// </summary>
        public int ModifierCount => Modifiers.Count;

        /// <summary>
        /// 检查是否有指定类型的修饰器
        /// </summary>
        public bool HasModifierOfType(ModifierType type)
        {
            return _groupedModifiers.ContainsKey(type) && _groupedModifiers[type].Count > 0;
        }

        /// <summary>
        /// 获取指定类型的修饰器数量
        /// </summary>
        public int GetModifierCountOfType(ModifierType type)
        {
            return _groupedModifiers.TryGetValue(type, out var list) ? list.Count : 0;
        }
        #endregion
    }
}