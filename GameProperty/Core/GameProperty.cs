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

            if (!_isDirty) return _cacheValue;
            var oldValue = _cacheValue;
            var ret = _baseValue;

            ApplyModify(ref ret);
            _cacheValue = ret;
            _isDirty = false;

            if (!oldValue.Equals(_cacheValue))
            {
                OnValueChanged?.Invoke(oldValue, _cacheValue);
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

        #region 依赖与脏数据        

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
        public IProperty<float> AddDependency(GameProperty dependency)
        {
            if (dependency == null || !_dependencies.Add(dependency)) return this;
            void handler(float oldVal, float newVal) => MakeDirty();
            _dependencyHandlers[dependency] = handler;
            dependency.OnValueChanged += handler;
            return this;
        }

        /// <summary>
        /// 移除对另一个 <see cref="GameProperty"/> 的依赖。
        /// </summary>
        /// <param name="dependency">要移除依赖的属性。</param>
        public IProperty<float> RemoveDependency(GameProperty dependency)
        {
            if (!_dependencies.Remove(dependency)) return this;
            if (_dependencyHandlers.TryGetValue(dependency, out var handler))
            {
                dependency.OnValueChanged -= handler;
                _dependencyHandlers.Remove(dependency);
            }
            return this;
        }

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

        private void ApplyModify(ref float ret)
        {
            var groupedModifiers = Modifiers.GroupBy(m => m.Type).ToDictionary(g => g.Key, g => g.ToList());

            ProcessModifier(ref ret, ModifierType.Add, groupedModifiers);
            ProcessModifier(ref ret, ModifierType.OverrideAdd, groupedModifiers);
            ProcessModifier(ref ret, ModifierType.Mul, groupedModifiers);
            ProcessModifier(ref ret, ModifierType.OverrideMul, groupedModifiers);
            ProcessModifier(ref ret, ModifierType.AfterAdd, groupedModifiers);
            ProcessModifier(ref ret, ModifierType.Clamp, groupedModifiers);
            ProcessModifier(ref ret, ModifierType.Override, groupedModifiers);
        }

        private void ProcessModifier(ref float value, ModifierType modifierType, Dictionary<ModifierType, List<IModifier>> groupedModifiers)
        {
            if (!groupedModifiers.TryGetValue(modifierType, out var modifiers)) return;

            switch (modifierType)
            {
                case ModifierType.Add:
                    value += modifiers.Sum(m => m.Value);
                    break;
                case ModifierType.OverrideAdd:
                    IModifier lastAdd = modifiers.OrderBy(m => m.Priority)
                                                 .LastOrDefault();
                    value += lastAdd == null ? 0 : lastAdd.Value;
                    break;
                case ModifierType.Mul:
                    value *= modifiers.OfType<FloatModifier>()
                                      .Aggregate(1f, (acc, m) => acc * m.Value);
                    break;
                case ModifierType.OverrideMul:
                    IModifier lastMul = modifiers.OrderBy(m => m.Priority)
                                                 .LastOrDefault();
                    value *= lastMul == null ? 1 : lastMul.Value;
                    break;
                case ModifierType.AfterAdd:
                    value += modifiers.OfType<FloatModifier>()
                                      .Sum(m => m.Value);
                    break;
                case ModifierType.Override:
                    IModifier lastOverride = modifiers.OrderBy(m => m.Priority)
                                                      .LastOrDefault();
                    value = lastOverride == null ? value : lastOverride.Value;
                    break;
                case ModifierType.Clamp:
                    Vector2Modifier lastClamp = modifiers.OfType<Vector2Modifier>()
                                                         .OrderBy(m => m.Priority)
                                                         .LastOrDefault();
                    if (lastClamp != null)
                    {
                        var min = lastClamp.Range == null ? float.MinValue : lastClamp.Range.x;
                        var max = lastClamp.Range == null ? float.MaxValue : lastClamp.Range.y;
                        value = Math.Clamp(value, min, max);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(modifierType), modifierType, null);
            }
        }
        #endregion
    }

    #region 存储
    [Serializable]
    public class SerializableGameProperty
    {
        public string ID;
        public float BaseValue;
        public SerializableModifierList ModifierList;
    }

    [Serializable]
    public class SerializableModifierList
    {
        public List<SerializableModifier> Modifiers;
    }

    public static class GamePropertySerializer
    {
        /// <summary>
        /// 序列化一个 <see cref="GameProperty"/> 对象为可存储的格式。
        /// </summary>
        /// <param name="property">要序列化的属性对象。</param>
        /// <returns>序列化后的 <see cref="SerializableGameProperty"/> 对象。</returns>
        public static SerializableGameProperty Serialize(GameProperty property)
        {
            var serializableModifiers = 
                property.Modifiers.Select(m =>
            {
                if (m is FloatModifier fm)
                {
                    return new SerializableModifier
                    {
                        Type = fm.Type,
                        Priority = fm.Priority,
                        Value = fm.Value,
                        ModifierClass = "FloatModifier"
                    };
                }
                else if (m is Vector2Modifier vm)
                {
                    return new SerializableModifier
                    {
                        Type = vm.Type,
                        Priority = vm.Priority,
                        Value = vm.Value,
                        Range = vm.Range,
                        ModifierClass = "Vector2Modifier"
                    };
                }
                return null;
            }).ToList();

            return new SerializableGameProperty
            {
                ID = property.ID,
                BaseValue = property.GetBaseValue(),
                ModifierList = new SerializableModifierList { Modifiers = serializableModifiers }
            };
        }

        /// <summary>
        /// 从序列化的 <see cref="SerializableGameProperty"/> 对象还原为 <see cref="GameProperty"/> 对象。
        /// </summary>
        /// <param name="serializedProperty">序列化的属性对象。</param>
        /// <returns>还原后的 <see cref="GameProperty"/> 对象。</returns>
        public static GameProperty FromSerializable(SerializableGameProperty serializedProperty)
        {
            var property = new GameProperty(serializedProperty.BaseValue, serializedProperty.ID);
            if (serializedProperty.ModifierList != null && serializedProperty.ModifierList.Modifiers != null)
            {
                foreach (var sm in serializedProperty.ModifierList.Modifiers)
                {
                    IModifier modifier = sm.ModifierClass switch
                    {
                        "FloatModifier" => new FloatModifier(sm.Type, sm.Priority, sm.Value),
                        "Vector2Modifier" => new Vector2Modifier(sm.Type, sm.Priority, sm.Range),
                        _ => null
                    };
                    if (modifier != null)
                        property.AddModifier(modifier);
                }
            }
            return property;
        }
    }
    #endregion
}