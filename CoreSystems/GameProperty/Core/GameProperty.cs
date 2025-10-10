using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
/// <summary>
/// 基于float数值的游戏属性类
/// 支持修饰符系统，依赖系统，脏标记系统
/// 可用于实现角色属性，装备属性，buff/debuff等各种游戏数值计算
/// 
/// 通常与CombineProperty配合使用，也可以单独使用GameProperty
/// </summary>

namespace EasyPack
{
    public class GameProperty : IProperty<float>
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
            bool needsRecalculation = _hasNonClampRangeModifier || _hasRandomDependency || _isDirty;

            if (!needsRecalculation)
                return _cacheValue;

            // 避免空循环
            if (_isDirty && _dependencies.Count > 0)
            {
                foreach (var dep in _dependencies)
                {
                    dep.GetValue();
                }
            }

            var oldValue = _cacheValue;
            var ret = _baseValue;

            // 应用修饰符
            ApplyModifiers(ref ret);
            _cacheValue = ret;

            // 如果没有随机性依赖项则清理脏标记
            if (!(_hasNonClampRangeModifier || _hasRandomDependency))
                _isDirty = false;

            if (System.Math.Abs(oldValue - _cacheValue) > EPSILON)
            {
                OnValueChanged?.Invoke(oldValue, _cacheValue);

                // 检查是否有依赖者再触发更新
                if (_dependents.Count > 0)
                {
                    TriggerDependentUpdates();
                }
            }

            return _cacheValue;
        }

        /// <summary>
        /// 设置属性的基础值并触发重新计算
        /// 这会影响最终的计算结果
        /// </summary>
        /// <param name="value">新的基础值</param>
        public IProperty<float> SetBaseValue(float value)
        {
            if (System.Math.Abs(_baseValue - value) > EPSILON)
            {
                _baseValue = value;
                MakeDirty();
                // 立即计算以确保事件和依赖更新正确触发
                GetValue();
            }
            return this;
        }
        #endregion

        #region 依赖系统       

        private readonly HashSet<GameProperty> _dependencies = new();
        private readonly HashSet<GameProperty> _dependents = new(); // 依赖于此属性的其他属性
        private readonly Dictionary<GameProperty, Func<GameProperty, float, float>> _dependencyCalculators = new();
        
        // 依赖深度
        private int _dependencyDepth = 0;
        private const int MAX_DEPENDENCY_DEPTH = 100;

        /// <summary>
        /// 属性值改变时的事件
        /// </summary>
        public event Action<float, float> OnValueChanged;

        private readonly HashSet<Action> _onDirtyHandlers = new();

        /// <summary>
        /// 添加一个依赖项，当dependency的值改变时，会调用calculator来计算新值
        /// </summary>
        /// <param name="dependency">依赖的属性</param>
        /// <param name="calculator">计算函数(dependency, newDependencyValue) => newThisValue</param>
        public IProperty<float> AddDependency(GameProperty dependency, Func<GameProperty, float, float> calculator = null)
        {
            if (dependency == null)
                throw new ArgumentNullException(nameof(dependency));

            if (!_dependencies.Add(dependency)) return this;

            if (WouldCreateCyclicDependency(dependency))
            {
                _dependencies.Remove(dependency);
                Debug.LogWarning($"检测到循环依赖，取消添加依赖关系: {ID} -> {dependency.ID}");
                return this;
            }

            // 添加反向引用
            dependency._dependents.Add(this);

            // 更新依赖深度
            UpdateDependencyDepth();

            // 如果有计算函数则立即计算
            if (calculator != null)
            {
                _dependencyCalculators[dependency] = calculator;
                // 立即应用计算结果
                var dependencyValue = dependency.GetValue();
                var newValue = calculator(dependency, dependencyValue);
                SetBaseValue(newValue);
            }

            UpdateRandomDependencyState();
            return this;
        }

        /// <summary>
        /// 添加简单依赖
        /// </summary>
        public IProperty<float> AddDependency(GameProperty dependency)
        {
            return AddDependency(dependency, null);
        }

        /// <summary>
        /// 移除依赖关系
        /// </summary>
        public IProperty<float> RemoveDependency(GameProperty dependency)
        {
            if (!_dependencies.Remove(dependency)) return this;

            // 移除反向引用
            dependency._dependents.Remove(this);

            // 移除计算函数
            _dependencyCalculators.Remove(dependency);

            // 更新依赖深度
            UpdateDependencyDepth();

            UpdateRandomDependencyState();
            return this;
        }

        /// <summary>
        /// 触发所有依赖此属性的其他属性更新
        /// </summary>
        private void TriggerDependentUpdates()
        {
            // 早返
            if (_dependents.Count == 0)
                return;

            foreach (var dependent in _dependents)
            {

                if (dependent._dependencyCalculators.TryGetValue(this, out var calculator))
                {
                    var newValue = calculator(this, _cacheValue);
                    if (System.Math.Abs(dependent._baseValue - newValue) > EPSILON)
                    {
                        dependent._baseValue = newValue;
                        dependent.MakeDirty();
                        dependent.GetValue();
                    }
                }
                else
                {
                    dependent.MakeDirty();
                    dependent.GetValue();
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
            // 自引用
            if (dependency == this) return true;

            // 深度限制检查
            if (dependency._dependencyDepth >= MAX_DEPENDENCY_DEPTH)
            {
                Debug.LogWarning($"依赖深度超过限制 {MAX_DEPENDENCY_DEPTH}");
                return true;
            }

            var visited = new HashSet<GameProperty>();
            var stack = new Stack<GameProperty>();
            stack.Push(dependency);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                
                // 跳过已访问的节点
                if (!visited.Add(current)) continue;

                // 检测到循环
                if (current == this) return true;

                // 将所有依赖项压入栈
                foreach (var dep in current._dependencies)
                {
                    if (!visited.Contains(dep))
                        stack.Push(dep);
                }
            }

            return false;
        }

        /// <summary>
        /// 更新依赖深度（在添加/移除依赖时调用）
        /// </summary>
        private void UpdateDependencyDepth()
        {
            if (_dependencies.Count == 0)
            {
                _dependencyDepth = 0;
                return;
            }

            int maxDepth = 0;
            foreach (var dep in _dependencies)
            {
                if (dep._dependencyDepth > maxDepth)
                    maxDepth = dep._dependencyDepth;
            }
            _dependencyDepth = maxDepth + 1;

            // 传播深度更新到所有依赖此属性的对象
            foreach (var dependent in _dependents)
            {
                dependent.UpdateDependencyDepth();
            }
        }
        #endregion

        #region 脏标记系统

        private bool _isDirty = false;
        private Action _onDirty;

        /// <summary>
        /// 将属性标记为脏状态，表示需要重新计算值
        /// </summary>
        public void MakeDirty()
        {
            if (_isDirty) return;
            
            _isDirty = true;
            _onDirty?.Invoke();
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

        /// <summary>
        /// 向属性添加一个修饰符，修饰符会影响最终值
        /// </summary>
        /// <param name="modifier">要添加的修饰符</param>
        public IProperty<float> AddModifier(IModifier modifier)
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

            // 检查是否有随机性修饰符
            if (modifier is RangeModifier rm && rm.Type != ModifierType.Clamp)
            {
                _hasNonClampRangeModifier = true;
            }

            MakeDirty();
            return this;
        }

        /// <summary>
        /// 清除所有修饰符，属性值将回到基础值
        /// </summary>
        public IProperty<float> ClearModifiers()
        {
            Modifiers.Clear();
            _groupedModifiers.Clear(); // 清理分组缓存
            _modifierIndexMap.Clear(); // 清理索引映射
            _hasNonClampRangeModifier = false; // 重置RangeModifier标记
            MakeDirty();
            return this;
        }

        /// <summary>
        /// 批量添加多个修饰符到属性
        /// </summary>
        /// <param name="modifiers">要添加的修饰符集合</param>
        public IProperty<float> AddModifiers(IEnumerable<IModifier> modifiers)
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
        public IProperty<float> RemoveModifiers(IEnumerable<IModifier> modifiers)
        {
            foreach (var modifier in modifiers)
            {
                if (modifier == null) continue;

                // 使用索引映射快速查找
                if (_modifierIndexMap.TryGetValue(modifier, out var index))
                {
                    // 从总列表移除
                    Modifiers.Remove(modifier);
                    _modifierIndexMap.Remove(modifier);

                    // 从分组中移除
                    if (_groupedModifiers.TryGetValue(modifier.Type, out var list))
                    {
                        list.Remove(modifier);

                        // 如果分组为空则移除整个分组
                        if (list.Count == 0)
                        {
                            _groupedModifiers.Remove(modifier.Type);
                        }
                    }
                }
            }

            // 批量操作后统一检查一次
            _hasNonClampRangeModifier = HasNonClampRangeModifiers();
            MakeDirty();
            return this;
        }

        /// <summary>
        /// 从属性中移除一个特定的修饰符
        /// </summary>
        /// <param name="modifier">要移除的修饰符</param>
        public IProperty<float> RemoveModifier(IModifier modifier)
        {
            // 使用索引映射查找
            if (_modifierIndexMap.TryGetValue(modifier, out var _))
            {
                Modifiers.Remove(modifier);
                _modifierIndexMap.Remove(modifier);

                // 从分组中移除
                if (_groupedModifiers.TryGetValue(modifier.Type, out var list))
                {
                    list.Remove(modifier);

                    // 如果分组为空则移除整个分组
                    if (list.Count == 0)
                    {
                        _groupedModifiers.Remove(modifier.Type);
                    }
                }

                // 仅在必要时检查
                if (modifier is RangeModifier rm && rm.Type != ModifierType.Clamp)
                {
                    _hasNonClampRangeModifier = HasNonClampRangeModifiers();
                }
            }

            MakeDirty();
            return this;
        }

        /// <summary>
        /// 检查是否有非Clamp类型的RangeModifier
        /// </summary>
        /// <returns>如果存在返回true，否则返回false</returns>
        private bool HasNonClampRangeModifiers()
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