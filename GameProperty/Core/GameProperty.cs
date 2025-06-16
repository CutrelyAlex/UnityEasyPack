/// <summary>
/// 表示RPGPack系统中可被修饰的float属性。
/// 支持修饰器、依赖关系和脏数据追踪。
/// 可用于管理角色属性或其他可被buff、debuff等游戏逻辑影响的动态数值。
/// 
/// 一般情况下，优先使用CombineProperty而非GameProperty
/// </summary>
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RPGPack
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
            ID = id;
            Modifiers = new List<IModifier>();
            MakeDirty();
        }
        public GameProperty(float initValue, string id)
        {
            _baseValue = initValue;
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
            // 刷新依赖
            foreach (var dep in _dependencies)
            {
                dep.GetValue();
            }

            // 如果有随机数值或依赖中有随机数，那应当每次获取都计算
            bool hasRandomModifiers = HasNonClampRangeModifiers() || _hasRandomDependency; 

            // 如果有随机修饰器或者属性已脏，则重新计算
            if (hasRandomModifiers || _isDirty)
            {
                var oldValue = _cacheValue;
                var ret = _baseValue;

                ApplyModify(ref ret);
                _cacheValue = ret;

                // 只有在非随机情况下才清除脏标记
                if (!hasRandomModifiers)
                    _isDirty = false;

                if (!oldValue.Equals(_cacheValue))
                {
                    OnValueChanged?.Invoke(oldValue, _cacheValue);
                }
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
            }
            return this;
        }
        #endregion 

        #region 依赖       

        private readonly HashSet<GameProperty> _dependencies = new();
        private readonly Dictionary<GameProperty, Action<float, float>> _dependencyHandlers = new();

        /// <summary>
        /// 当属性值发生变化时触发。
        /// </summary>
        public event Action<float, float> OnValueChanged;
        private bool _isDirty = false;
        private Action _onDirty;
        private readonly HashSet<Action> _onDirtyHandlers = new();

        /// <summary>
        /// 添加对另一个 <see cref="GameProperty"/> 的依赖。
        /// 当依赖属性变化时，本属性会被标记为脏并重新计算。
        /// </summary>
        /// <param name="dependency">要依赖的属性。</param>
        /// <summary>
        /// 添加对另一个 <see cref="GameProperty"/> 的依赖。
        /// 当依赖属性变化时，本属性会被标记为脏并重新计算。
        /// </summary>
        /// <param name="dependency">要依赖的属性。</param>
        public IProperty<float> AddDependency(GameProperty dependency)
        {
            // 检查是否为null或已经存在依赖
            if (dependency == null || !_dependencies.Add(dependency)) return this;

            // 检测循环依赖
            if (WouldCreateCyclicDependency(dependency))
            {
                _dependencies.Remove(dependency);
                Debug.LogWarning($"无法添加依赖：检测到循环依赖。{ID} -> {dependency.ID}");
                return this;
            }

            // 创建并注册值变化处理器
            void handler(float oldVal, float newVal) => MakeDirty();
            _dependencyHandlers[dependency] = handler;
            dependency.OnValueChanged += handler;

            // 如果依赖有随机修饰器，确保本属性也会随之更新
            UpdateRandomDependencyState();

            return this;
        }

        /// <summary>
        /// 移除对另一个 <see cref="GameProperty"/> 的依赖。
        /// </summary>
        /// <param name="dependency">要移除依赖的属性。</param>
        public IProperty<float> RemoveDependency(GameProperty dependency)
        {
            if (!_dependencies.Remove(dependency)) return this;

            // 移除事件处理器
            if (_dependencyHandlers.TryGetValue(dependency, out var handler))
            {
                dependency.OnValueChanged -= handler;
                _dependencyHandlers.Remove(dependency);
            }

            // 更新随机依赖状态
            UpdateRandomDependencyState();

            return this;
        }

        /// <summary>
        /// 检测是否有任何依赖项包含随机修饰器
        /// </summary>
        private bool _hasRandomDependency = false;

        /// <summary>
        /// 更新随机依赖状态标记
        /// </summary>
        private void UpdateRandomDependencyState()
        {
            _hasRandomDependency = _dependencies.Any(dep => dep.HasNonClampRangeModifiers() || dep._hasRandomDependency);
        }

        /// <summary>
        /// 检测添加依赖是否会导致循环依赖
        /// </summary>
        private bool WouldCreateCyclicDependency(GameProperty dependency)
        {
            // 如果依赖项就是自身，直接返回true
            if (dependency == this) return true;

            // 检查依赖项的依赖是否包含自身（递归检查）
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

        /// <summary>
        /// 向此属性添加一个修饰器，并标记为脏。
        /// </summary>
        /// <param name="modifier">要添加的修饰器。</param>

        public IProperty<float> AddModifier(IModifier modifier)
        {
            Modifiers.Add(modifier);
            MakeDirty();
            return this;
        }

        /// <summary>
        /// 移除此属性上的所有修饰器，并标记为脏。
        /// </summary>
        public IProperty<float> ClearModifiers()
        {
            Modifiers.Clear();
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
                Modifiers.Add(modifier);
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
                Modifiers.Remove(modifier);
            }
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
            if (_modifier != null) Modifiers.Remove(_modifier);
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

        private void ApplyModify(ref float ret)
        {
            if (Modifiers.Count == 0)
                return;
            var groupedModifiers = Modifiers.GroupBy(m => m.Type).ToDictionary(g => g.Key, g => g.ToList());

            ApplyModifierByType(ref ret, ModifierType.Add, groupedModifiers);
            ApplyModifierByType(ref ret, ModifierType.PriorityAdd, groupedModifiers);
            ApplyModifierByType(ref ret, ModifierType.Mul, groupedModifiers);
            ApplyModifierByType(ref ret, ModifierType.PriorityMul, groupedModifiers);
            ApplyModifierByType(ref ret, ModifierType.AfterAdd, groupedModifiers);
            ApplyModifierByType(ref ret, ModifierType.Clamp, groupedModifiers);
            ApplyModifierByType(ref ret, ModifierType.Override, groupedModifiers);
        }

        private void ApplyModifierByType(ref float value, ModifierType type, Dictionary<ModifierType, List<IModifier>> groupedModifiers)
        {
            if (!groupedModifiers.TryGetValue(type, out var modifiers))
                return;

            var strategy = ModifierStrategyManager.GetStrategy(type);
            strategy.Apply(ref value, modifiers);
        }

        #endregion
    }
}