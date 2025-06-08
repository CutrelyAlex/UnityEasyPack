/// <summary>
/// ��ʾRPGPackϵͳ�пɱ����ε�float���ԡ�
/// ֧����������������ϵ��������׷�١�
/// �����ڹ����ɫ���Ի������ɱ�buff��debuff����Ϸ�߼�Ӱ��Ķ�̬��ֵ��
/// 
/// һ������£�����ʹ��CombineProperty����GameProperty
/// </summary>
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RPGPack
{
    public class GameProperty : IProperty<float>
    {
        #region ��������

        /// <summary>
        /// �����Ե�Ψһ��ʶ����
        /// </summary>
        public string ID { get; set; }

        private float _baseValue;
        private float _cacheValue;

        /// <summary>
        /// ��ʼ�� <see cref="GameProperty"/> �����ʵ����
        /// </summary>
        /// <param name="initValue">���Եĳ�ʼ����ֵ��</param>
        /// <param name="id">���Ե�Ψһ��ʶ����</param>
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
        /// ��ȡ���ԵĻ�����δ���Σ�ֵ��
        /// </summary>
        /// <returns>����ֵ��float���͡�</returns>
        public float GetBaseValue() => _baseValue;

        /// <summary>
        /// ��ȡӦ��������������������ĵ�ǰ����ֵ��
        /// </summary>
        /// <returns>������floatֵ��</returns>
        public float GetValue()
        {
            // ˢ������
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
        /// �������ԵĻ�����δ���Σ�ֵ��
        /// ���ֵ�����仯�ᴥ�����¼��㡣
        /// </summary>
        /// <param name="value">�µĻ���ֵ��</param>
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

        #region ������������        

        private readonly HashSet<GameProperty> _dependencies = new();
        private readonly Dictionary<GameProperty, Action<float, float>> _dependencyHandlers = new();

        /// <summary>
        /// ������ֵ�����仯ʱ������
        /// </summary>
        public event Action<float, float> OnValueChanged;
        private bool _isDirty = false;
        private Action _onDirty;
        private readonly HashSet<Action> _onDirtyHandlers = new();

        /// <summary>
        /// ��Ӷ���һ�� <see cref="GameProperty"/> ��������
        /// ���������Ա仯ʱ�������Իᱻ���Ϊ�ಢ���¼��㡣
        /// </summary>
        /// <param name="dependency">Ҫ���������ԡ�</param>
        public IProperty<float> AddDependency(GameProperty dependency)
        {
            if (dependency == null || !_dependencies.Add(dependency)) return this;
            void handler(float oldVal, float newVal) => MakeDirty();
            _dependencyHandlers[dependency] = handler;
            dependency.OnValueChanged += handler;
            return this;
        }

        /// <summary>
        /// �Ƴ�����һ�� <see cref="GameProperty"/> ��������
        /// </summary>
        /// <param name="dependency">Ҫ�Ƴ����������ԡ�</param>
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
        /// �����Ա��Ϊ�࣬�´η���ʱ�����¼�����ֵ��
        /// </summary>
        public void MakeDirty()
        {
            if (_isDirty) return;
            _isDirty = true;
            _onDirty?.Invoke();
        }

        /// <summary>
        /// ע��һ���ص��������Ա����Ϊ��ʱ���á�
        /// </summary>
        /// <param name="action">�ص�������</param>
        public void OnDirty(Action action)
        {
            if (_onDirtyHandlers.Add(action))
            {
                _onDirty += action;
            }
        }

        /// <summary>
        /// �Ƴ���ע��������ݻص���
        /// </summary>
        /// <param name="action">Ҫ�Ƴ��Ļص�������</param>
        public void RemoveOnDirty(Action action)
        {
            if (_onDirtyHandlers.Remove(action))
            {
                _onDirty -= action;
            }
        }
        #endregion

        #region ������   

        /// <summary>
        /// ��ȡӦ���ڴ����Ե��������б�
        /// </summary>
        public List<IModifier> Modifiers { get; }

        /// <summary>
        /// ����������һ���������������Ϊ�ࡣ
        /// </summary>
        /// <param name="modifier">Ҫ��ӵ���������</param>
        public IProperty<float> AddModifier(IModifier modifier)
        {
            Modifiers.Add(modifier);
            MakeDirty();
            return this;
        }

        /// <summary>
        /// �Ƴ��������ϵ������������������Ϊ�ࡣ
        /// </summary>
        public IProperty<float> ClearModifiers()
        {
            Modifiers.Clear();
            MakeDirty();
            return this;
        }

        /// <summary>
        /// ���������Ӷ���������������Ϊ�ࡣ
        /// </summary>
        /// <param name="modifiers">Ҫ��ӵ����������ϡ�</param>
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
        /// �Ƴ��������ϵĶ���������������Ϊ�ࡣ
        /// </summary>
        /// <param name="modifiers">Ҫ�Ƴ������������ϡ�</param>
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
        /// �Ƴ��������ϵ�һ���������������Ϊ�ࡣ
        /// </summary>
        /// <param name="modifier">Ҫ�Ƴ�����������</param>
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

    #region �洢
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
        /// ���л�һ�� <see cref="GameProperty"/> ����Ϊ�ɴ洢�ĸ�ʽ��
        /// </summary>
        /// <param name="property">Ҫ���л������Զ���</param>
        /// <returns>���л���� <see cref="SerializableGameProperty"/> ����</returns>
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
        /// �����л��� <see cref="SerializableGameProperty"/> ����ԭΪ <see cref="GameProperty"/> ����
        /// </summary>
        /// <param name="serializedProperty">���л������Զ���</param>
        /// <returns>��ԭ��� <see cref="GameProperty"/> ����</returns>
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