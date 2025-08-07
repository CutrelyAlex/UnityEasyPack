using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
/// <summary>
/// ????????float?????
/// ??????????????????????????????
/// ????????????????????????buff??debuff???????????????????
/// 
/// ????????????????CombineProperty????GameProperty
/// </summary>

namespace EasyPack
{
    public class GameProperty : IProperty<float>
    {

        #region ????????

        /// <summary>
        /// ????????????????
        /// </summary>
        public string ID { get; set; }

        private float _baseValue;
        private float _cacheValue;

        /// <summary>
        /// ????? <see cref="GameProperty"/> ??????????
        /// </summary>
        /// <param name="initValue">??????????????</param>
        /// <param name="id">??????????????</param>
        public GameProperty(string id, float initValue)
        {
            _baseValue = initValue;
            _cacheValue = initValue;
            ID = id;
            Modifiers = new List<IModifier>();
            MakeDirty();
        }

        /// <summary>
        /// ??????????????????????
        /// </summary>
        /// <returns>???????float?????</returns>
        public float GetBaseValue() => _baseValue;

        /// <summary>
        /// ??????????????????????????????????
        /// </summary>
        /// <returns>??????float???</returns>
        public float GetValue()
        {
            bool needsRecalculation = _hasNonClampRangeModifier || _hasRandomDependency || _isDirty;

            if (!needsRecalculation)
                return _cacheValue;

            // ???????????????
            if (_isDirty)
            {
                foreach (var dep in _dependencies)
                {
                    dep.GetValue();
                }
            }

            var oldValue = _cacheValue;
            var ret = _baseValue;

            // ?????????
            ApplyModifiers(ref ret);
            _cacheValue = ret;

            // ??????????????????????
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
        /// ???????????????????????
        /// ?????????????????????
        /// </summary>
        /// <param name="value">?????????</param>
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

        #region ????       

        private readonly HashSet<GameProperty> _dependencies = new();
        private readonly HashSet<GameProperty> _dependents = new(); // ???????????
        private readonly Dictionary<GameProperty, Func<GameProperty, float, float>> _dependencyCalculators = new();

        /// <summary>
        /// ??????????????????
        /// </summary>
        public event Action<float, float> OnValueChanged;

        private readonly HashSet<Action> _onDirtyHandlers = new();

        /// <summary>
        /// ????????????dependency???????????calculator???????
        /// </summary>
        /// <param name="dependency">??????????</param>
        /// <param name="calculator">????????(dependency, newDependencyValue) => newThisValue</param>
        public IProperty<float> AddDependency(GameProperty dependency, Func<GameProperty, float, float> calculator = null)
        {
            if (dependency == null)
                throw new ArgumentNullException(nameof(dependency));

            if (!_dependencies.Add(dependency)) return this;

            if (WouldCreateCyclicDependency(dependency))
            {
                _dependencies.Remove(dependency);
                Debug.LogWarning($"?????????????????????????{ID} -> {dependency.ID}");
                return this;
            }

            // ?????????
            dependency._dependents.Add(this);

            // ?????????????????
            if (calculator != null)
            {
                _dependencyCalculators[dependency] = calculator;
                // ???????????
                var dependencyValue = dependency.GetValue();
                var newValue = calculator(dependency, dependencyValue);
                SetBaseValue(newValue);
            }

            UpdateRandomDependencyState();
            return this;
        }

        /// <summary>
        /// ?????????
        /// </summary>
        public IProperty<float> AddDependency(GameProperty dependency)
        {
            return AddDependency(dependency, null);
        }

        /// <summary>
        /// ???????
        /// </summary>
        public IProperty<float> RemoveDependency(GameProperty dependency)
        {
            if (!_dependencies.Remove(dependency)) return this;

            // ???????????
            dependency._dependents.Remove(this);

            // ?????????
            _dependencyCalculators.Remove(dependency);

            UpdateRandomDependencyState();
            return this;
        }

        /// <summary>
        /// ??????????????????????????
        /// </summary>
        private void TriggerDependentUpdates()
        {
            foreach (var dependent in _dependents)
            {
                // ??????????????????????
                if (dependent._dependencyCalculators.TryGetValue(this, out var calculator))
                {
                    var newValue = calculator(this, _cacheValue);
                    dependent.SetBaseValue(newValue);
                }
                else
                {
                    // ???????????????????????
                    // ??????????????????????????????????????
                    dependent.MakeDirty();

                    var oldValue = dependent._cacheValue;
                    var newValue = dependent.GetValue(); // ???????????

                    //if (oldValue.Equals(newValue))
                    //{
                    //    dependent.OnValueChanged?.Invoke(oldValue, newValue);
                    //}
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

        #region ?????????

        private bool _isDirty = false;
        private Action _onDirty;

        /// <summary>
        /// ????????????´???????????????????
        /// </summary>
        public void MakeDirty()
        {
            if (_isDirty) return;
            _isDirty = true;
            _onDirty?.Invoke();
        }

        /// <summary>
        /// ????????????????????????????á?
        /// </summary>
        /// <param name="action">?????????</param>
        public void OnDirty(Action action)
        {
            if (_onDirtyHandlers.Add(action))
            {
                _onDirty += action;
            }
        }

        /// <summary>
        /// ???????????????????
        /// </summary>
        /// <param name="action">??????????????</param>
        public void RemoveOnDirty(Action action)
        {
            if (_onDirtyHandlers.Remove(action))
            {
                _onDirty -= action;
            }
        }
        #endregion

        #region ??????   

        /// <summary>
        /// ?????????????????????????
        /// </summary>
        public List<IModifier> Modifiers { get; }
        private readonly Dictionary<ModifierType, List<IModifier>> _groupedModifiers = new();
        private bool _hasNonClampRangeModifier = false;

        /// <summary>
        /// ??????????????????????????????
        /// </summary>
        /// <param name="modifier">??????????????</param>
        public IProperty<float> AddModifier(IModifier modifier)
        {

            if (modifier == null)
                throw new ArgumentNullException(nameof(modifier));

            // ??????????
            Modifiers.Add(modifier);

            // ?????
            if (!_groupedModifiers.TryGetValue(modifier.Type, out var list))
            {
                list = new List<IModifier>();
                _groupedModifiers[modifier.Type] = list;
            }
            list.Add(modifier);

            // ?????????????
            if (modifier is RangeModifier rm && rm.Type != ModifierType.Clamp)
            {
                _hasNonClampRangeModifier = true;
            }

            MakeDirty();
            return this;
        }

        /// <summary>
        /// ????????????????????????????????
        /// </summary>
        public IProperty<float> ClearModifiers()
        {
            Modifiers.Clear();
            _groupedModifiers.Clear(); // ????????????
            _hasNonClampRangeModifier = false; // ????RangeModifier???
            MakeDirty();
            return this;
        }

        /// <summary>
        /// ?????????????????????????????
        /// </summary>
        /// <param name="modifiers">?????????????????</param>
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
        /// ??????????????????????????????
        /// </summary>
        /// <param name="modifiers">?????????????????</param>
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
        /// ???????????????????????????????
        /// </summary>
        /// <param name="modifier">??????????????</param>
        public IProperty<float> RemoveModifier(IModifier modifier)
        {
            var _modifier = Modifiers.Find(m => m.Equals(modifier));
            if (_modifier != null)
            {
                Modifiers.Remove(_modifier);

                // ????????????
                if (_groupedModifiers.TryGetValue(_modifier.Type, out var list))
                {
                    list.Remove(_modifier);

                    // ????????????????????????????
                    if (list.Count == 0)
                    {
                        _groupedModifiers.Remove(_modifier.Type);
                    }
                }
            }

            // ?????Clamp??RangeModifier???
            _hasNonClampRangeModifier = HasNonClampRangeModifiers();

            MakeDirty();
            return this;
        }

        /// <summary>
        /// ??????????Clamp?????RangeModifier
        /// </summary>
        /// <returns>???????????true????????false</returns>
        private bool HasNonClampRangeModifiers()
        {
            return Modifiers.OfType<RangeModifier>().Any(m => m.Type != ModifierType.Clamp);
        }

        // ???????
        private static readonly Dictionary<ModifierType, IModifierStrategy> _cachedStrategies = new();

        // ??????????
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

            // ????????????????????
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

        #region ???
        /// <summary>
        /// ?????????????????????
        /// </summary>
        public bool HasModifiers => Modifiers.Count > 0;

        /// <summary>
        /// ?????????????
        /// </summary>
        public int ModifierCount => Modifiers.Count;

        /// <summary>
        /// ??????????????????????
        /// </summary>
        public bool ContainModifierOfType(ModifierType type)
        {
            return _groupedModifiers.ContainsKey(type) && _groupedModifiers[type].Count > 0;
        }

        /// <summary>
        /// ?????????????????????
        /// </summary>
        public int GetModifierCountOfType(ModifierType type)
        {
            return _groupedModifiers.TryGetValue(type, out var list) ? list.Count : 0;
        }
        #endregion
    }
}