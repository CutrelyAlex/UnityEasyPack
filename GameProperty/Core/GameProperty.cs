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

            // ����������ֵ�������������������Ӧ��ÿ�λ�ȡ������
            bool hasRandomModifiers = HasNonClampRangeModifiers() || _hasRandomDependency; 

            // �������������������������࣬�����¼���
            if (hasRandomModifiers || _isDirty)
            {
                var oldValue = _cacheValue;
                var ret = _baseValue;

                ApplyModify(ref ret);
                _cacheValue = ret;

                // ֻ���ڷ��������²��������
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

        #region ����       

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
        /// <summary>
        /// ��Ӷ���һ�� <see cref="GameProperty"/> ��������
        /// ���������Ա仯ʱ�������Իᱻ���Ϊ�ಢ���¼��㡣
        /// </summary>
        /// <param name="dependency">Ҫ���������ԡ�</param>
        public IProperty<float> AddDependency(GameProperty dependency)
        {
            // ����Ƿ�Ϊnull���Ѿ���������
            if (dependency == null || !_dependencies.Add(dependency)) return this;

            // ���ѭ������
            if (WouldCreateCyclicDependency(dependency))
            {
                _dependencies.Remove(dependency);
                Debug.LogWarning($"�޷������������⵽ѭ��������{ID} -> {dependency.ID}");
                return this;
            }

            // ������ע��ֵ�仯������
            void handler(float oldVal, float newVal) => MakeDirty();
            _dependencyHandlers[dependency] = handler;
            dependency.OnValueChanged += handler;

            // ��������������������ȷ��������Ҳ����֮����
            UpdateRandomDependencyState();

            return this;
        }

        /// <summary>
        /// �Ƴ�����һ�� <see cref="GameProperty"/> ��������
        /// </summary>
        /// <param name="dependency">Ҫ�Ƴ����������ԡ�</param>
        public IProperty<float> RemoveDependency(GameProperty dependency)
        {
            if (!_dependencies.Remove(dependency)) return this;

            // �Ƴ��¼�������
            if (_dependencyHandlers.TryGetValue(dependency, out var handler))
            {
                dependency.OnValueChanged -= handler;
                _dependencyHandlers.Remove(dependency);
            }

            // �����������״̬
            UpdateRandomDependencyState();

            return this;
        }

        /// <summary>
        /// ����Ƿ����κ�������������������
        /// </summary>
        private bool _hasRandomDependency = false;

        /// <summary>
        /// �����������״̬���
        /// </summary>
        private void UpdateRandomDependencyState()
        {
            _hasRandomDependency = _dependencies.Any(dep => dep.HasNonClampRangeModifiers() || dep._hasRandomDependency);
        }

        /// <summary>
        /// �����������Ƿ�ᵼ��ѭ������
        /// </summary>
        private bool WouldCreateCyclicDependency(GameProperty dependency)
        {
            // ����������������ֱ�ӷ���true
            if (dependency == this) return true;

            // ���������������Ƿ���������ݹ��飩
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

        #region ������׷��

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


        /// <summary>
        /// ����Ƿ���ڷ�Clamp���͵�RangeModifier
        /// </summary>
        /// <returns>��������򷵻�true�����򷵻�false</returns>
        private bool HasNonClampRangeModifiers()
        {
            return Modifiers.OfType<RangeModifier>().Any(m => m.Type != ModifierType.Clamp);
        }

        private void ApplyModify(ref float ret)
        {
            if (Modifiers.Count == 0)
                return;
            var groupedModifiers = Modifiers.GroupBy(m => m.Type).ToDictionary(g => g.Key, g => g.ToList());

            ProcessModifier(ref ret, ModifierType.Add, groupedModifiers);
            ProcessModifier(ref ret, ModifierType.PriorityAdd, groupedModifiers);
            ProcessModifier(ref ret, ModifierType.Mul, groupedModifiers);
            ProcessModifier(ref ret, ModifierType.PriorityMul, groupedModifiers);
            ProcessModifier(ref ret, ModifierType.AfterAdd, groupedModifiers);
            ProcessModifier(ref ret, ModifierType.Clamp, groupedModifiers);
            ProcessModifier(ref ret, ModifierType.Override, groupedModifiers);
        }

        private void ProcessModifier(ref float value, ModifierType modifierType, Dictionary<ModifierType, List<IModifier>> groupedModifiers)
        {
            if (!groupedModifiers.TryGetValue(modifierType, out var modifiers))
                return;

            var floatMods = modifiers.OfType<FloatModifier>().ToList();
            var rangeMods = modifiers.OfType<RangeModifier>().ToList();

            switch (modifierType)
            {
                case ModifierType.Add:
                {
                    var floatAdd = floatMods.Sum(m => m.Value);
                    var rangeAdd = rangeMods.Sum(m => UnityEngine.Random.Range(m.Value.x, m.Value.y));
                    value += floatAdd + rangeAdd;
                    break;
                }
                case ModifierType.PriorityAdd:
                {
                    var priorityFloatAdd = floatMods.OrderByDescending(m => m.Priority).FirstOrDefault()?.Value ?? 0f;
                    var priorityRangeMod = rangeMods.OrderByDescending(m => m.Priority).FirstOrDefault();
                    float priorityRangeAdd = priorityRangeMod != null ? UnityEngine.Random.Range(priorityRangeMod.Value.x, priorityRangeMod.Value.y) : 0f;

                    if (floatMods.Any() && rangeMods.Any())
                    {
                        var floatPriority = floatMods.Max(m => m.Priority);
                        var rangePriority = rangeMods.Max(m => m.Priority);
                        value += floatPriority >= rangePriority ? priorityFloatAdd : priorityRangeAdd;
                    }
                    else
                    {
                        value += priorityFloatAdd + priorityRangeAdd;
                    }
                    break;
                }
                case ModifierType.Mul:
                {
                    var floatMul = floatMods.Aggregate(1f, (acc, m) => acc * m.Value);
                    var rangeMul = rangeMods.Aggregate(1f, (acc, m) => acc * UnityEngine.Random.Range(m.Value.x, m.Value.y));
                    value *= floatMul * rangeMul;
                    break;
                }
                case ModifierType.PriorityMul:
                {
                    var priorityFloatMul = floatMods.OrderByDescending(m => m.Priority).FirstOrDefault()?.Value ?? 1f;
                    var priorityRangeMod = rangeMods.OrderByDescending(m => m.Priority).FirstOrDefault();
                    float priorityRangeMul = priorityRangeMod != null ? UnityEngine.Random.Range(priorityRangeMod.Value.x, priorityRangeMod.Value.y) : 1f;

                    if (floatMods.Any() && rangeMods.Any())
                    {
                        var floatPriority = floatMods.Max(m => m.Priority);
                        var rangePriority = rangeMods.Max(m => m.Priority);
                        value *= floatPriority >= rangePriority ? priorityFloatMul : priorityRangeMul;
                    }
                    else
                    {
                        value *= priorityFloatMul * priorityRangeMul;
                    }
                    break;
                }
                case ModifierType.AfterAdd:
                {
                    var floatAfterAdd = floatMods.Sum(m => m.Value);
                    var rangeAfterAdd = rangeMods.Sum(m => UnityEngine.Random.Range(m.Value.x, m.Value.y));
                    value += floatAfterAdd + rangeAfterAdd;
                    break;
                }
                case ModifierType.Override:
                {
                    var floatOverrideMod = floatMods.OrderByDescending(m => m.Priority).FirstOrDefault();
                    var rangeOverrideMod = rangeMods.OrderByDescending(m => m.Priority).FirstOrDefault();

                    if (floatOverrideMod != null && rangeOverrideMod != null)
                    {
                        value = floatOverrideMod.Priority >= rangeOverrideMod.Priority ?
                                floatOverrideMod.Value :
                                UnityEngine.Random.Range(rangeOverrideMod.Value.x, rangeOverrideMod.Value.y);
                    }
                    else if (floatOverrideMod != null)
                    {
                        value = floatOverrideMod.Value;
                    }
                    else if (rangeOverrideMod != null)
                    {
                        value = UnityEngine.Random.Range(rangeOverrideMod.Value.x, rangeOverrideMod.Value.y);
                    }
                    break;
                }
                case ModifierType.Clamp:
                {
                    var clampMod = rangeMods.OrderByDescending(m => m.Priority).FirstOrDefault();
                    if (clampMod != null)
                    {
                        value = Mathf.Clamp(value, clampMod.Value.x, clampMod.Value.y);
                    }
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(modifierType), modifierType, null);
            }
        }
        #endregion
    }
}