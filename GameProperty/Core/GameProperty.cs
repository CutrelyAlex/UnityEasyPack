using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
/// <summary>
/// �ɱ����ε�float���ԡ�
/// ֧����������������ϵ��������׷�١�
/// �����ڹ����ɫ���Ի������ɱ�buff��debuff����Ϸ�߼�Ӱ��Ķ�̬��ֵ��
/// 
/// һ������£�����ʹ��CombineProperty����GameProperty
/// </summary>

namespace EasyPack
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
            bool needsRecalculation = _hasNonClampRangeModifier || _hasRandomDependency || _isDirty;

            if (!needsRecalculation)
                return _cacheValue;

            // ������Ҫʱˢ������
            if (_isDirty)
            {
                foreach (var dep in _dependencies)
                {
                    dep.GetValue();
                }
            }

            var oldValue = _cacheValue;
            var ret = _baseValue;

            // ������Ӧ��
            ApplyModifiers(ref ret);
            _cacheValue = ret;

            // ֻ���ڷ��������²��������
            if (!(_hasNonClampRangeModifier || _hasRandomDependency))
                _isDirty = false;

            // ����ֵ�����仯ʱ�����¼�
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
                GetValue();
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
            void handler(float oldVal, float newVal)
            {
                MakeDirty();
            }

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
        private readonly Dictionary<ModifierType, List<IModifier>> _groupedModifiers = new();
        private bool _hasNonClampRangeModifier = false;

        /// <summary>
        /// ����������һ���������������Ϊ�ࡣ
        /// </summary>
        /// <param name="modifier">Ҫ��ӵ���������</param>
        public IProperty<float> AddModifier(IModifier modifier)
        {
            // ��ӵ����б�
            Modifiers.Add(modifier);

            // Ԥ����
            if (!_groupedModifiers.TryGetValue(modifier.Type, out var list))
            {
                list = new List<IModifier>();
                _groupedModifiers[modifier.Type] = list;
            }
            list.Add(modifier);

            // Ԥ������������
            if (modifier is RangeModifier rm && rm.Type != ModifierType.Clamp)
            {
                _hasNonClampRangeModifier = true;
            }

            MakeDirty();
            return this;
        }

        /// <summary>
        /// �Ƴ��������ϵ������������������Ϊ�ࡣ
        /// </summary>
        public IProperty<float> ClearModifiers()
        {
            Modifiers.Clear();
            _groupedModifiers.Clear(); // ͬʱ��������ֵ�
            _hasNonClampRangeModifier = false; // ����RangeModifier���
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
                AddModifier(modifier);
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
                RemoveModifier(modifier);
            }

            _hasNonClampRangeModifier = HasNonClampRangeModifiers();
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
            if (_modifier != null)
            {
                Modifiers.Remove(_modifier);

                // ͬʱ�ӷ������Ƴ�
                if (_groupedModifiers.TryGetValue(_modifier.Type, out var list))
                {
                    list.Remove(_modifier);

                    // ����б�Ϊ�գ����Կ����Ƴ������͵ļ�
                    if (list.Count == 0)
                    {
                        _groupedModifiers.Remove(_modifier.Type);
                    }
                }
            }

            // ���·�Clamp��RangeModifier���
            _hasNonClampRangeModifier = HasNonClampRangeModifiers();

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

        // �������
        private static readonly Dictionary<ModifierType, IModifierStrategy> _cachedStrategies = new();

        // ��ȡ�򻺴����
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

            // �����ȼ�Ӧ������������
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

        #region ��ѯ
        /// <summary>
        /// ��������Ƿ����κ�������
        /// </summary>
        public bool HasModifiers => Modifiers.Count > 0;

        /// <summary>
        /// ��ȡ����������
        /// </summary>
        public int ModifierCount => Modifiers.Count;

        /// <summary>
        /// ����Ƿ���ָ�����͵�������
        /// </summary>
        public bool HasModifierOfType(ModifierType type)
        {
            return _groupedModifiers.ContainsKey(type) && _groupedModifiers[type].Count > 0;
        }

        /// <summary>
        /// ��ȡָ�����͵�����������
        /// </summary>
        public int GetModifierCountOfType(ModifierType type)
        {
            return _groupedModifiers.TryGetValue(type, out var list) ? list.Count : 0;
        }
        #endregion
    }
}