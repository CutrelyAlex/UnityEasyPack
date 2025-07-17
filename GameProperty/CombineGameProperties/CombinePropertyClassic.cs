using System;
using System.Collections.Generic;

namespace EasyPack
{
    public class CombinePropertyClassic : ICombineGameProperty, IDisposable
    {
        private Dictionary<string, GameProperty> GameProperties { get; set; } = new Dictionary<string, GameProperty>();
        public string ID { get; }
        public Func<ICombineGameProperty, float> Calculater { get; }

        private readonly GameProperty _resultHolder;
        public GameProperty ResultHolder => _resultHolder;

        private float _cacheValue;
        private readonly float _baseCombineValue;

        private readonly Dictionary<GameProperty, Action<float, float>> _eventHandlers = new();
        private readonly List<string> _managedPropertyIds = new List<string>(); // 跟踪管理的属性ID

        public bool IsValid() => !_isDisposed && _resultHolder != null;
        public float GetBaseValue() => _baseCombineValue;

        private bool _isDisposed = false;

        public float GetValue()
        {
            _resultHolder.SetBaseValue(Calculater(this));
            return _resultHolder.GetValue();
        }

        public CombinePropertyClassic(string id, float BaseValue, string ConfigProperty, string IncreaseValue, string IncreaseMul, string DecreaseValue, string DecreaseMul)
        {
            _resultHolder = CombineGamePropertyManager.GetOrCreateGameProperty(id + "@ResultHolder", 0);
            ID = id;
            _baseCombineValue = BaseValue;

            RegisterProperty(CombineGamePropertyManager.GetOrCreateGameProperty(ConfigProperty, BaseValue));
            RegisterProperty(CombineGamePropertyManager.GetOrCreateGameProperty(IncreaseValue, 0));
            RegisterProperty(CombineGamePropertyManager.GetOrCreateGameProperty(IncreaseMul, 0));
            RegisterProperty(CombineGamePropertyManager.GetOrCreateGameProperty(DecreaseValue, 0));
            RegisterProperty(CombineGamePropertyManager.GetOrCreateGameProperty(DecreaseMul, 0));

            _managedPropertyIds.Add(id + "@ResultHolder");
            _managedPropertyIds.Add(ConfigProperty);
            _managedPropertyIds.Add(IncreaseValue);
            _managedPropertyIds.Add(IncreaseMul);
            _managedPropertyIds.Add(DecreaseValue);
            _managedPropertyIds.Add(DecreaseMul);

            GameProperty BaseProperty = GetProperty(ConfigProperty);
            GameProperty BaseBuffValue = GetProperty(IncreaseValue);
            GameProperty BaseBuffMul = GetProperty(IncreaseMul);
            GameProperty DeBuffValue = GetProperty(DecreaseValue);
            GameProperty DeBuffMul = GetProperty(DecreaseMul);

            Calculater = e =>
            {
                return (BaseProperty.GetValue() + BaseBuffValue.GetValue()) * (BaseBuffMul.GetValue() + 1) - DeBuffValue.GetValue() * (1 + DeBuffMul.GetValue());
            };

            SubscribeToPropertyChanges();

            _cacheValue = Calculater(this);
        }

        private void SubscribeToPropertyChanges()
        {
            foreach (var prop in GameProperties.Values)
            {
                var handler = new Action<float, float>((oldVal, newVal) =>
                {
                    var oldCombine = _cacheValue;
                    var newCombine = Calculater(this);
                    if (!oldCombine.Equals(newCombine))
                    {
                        _cacheValue = newCombine;
                    }
                });

                _eventHandlers[prop] = handler;
                prop.OnValueChanged += handler;
            }
        }

        public GameProperty RegisterProperty(GameProperty gameProperty)
        {
            GameProperties[gameProperty.ID] = gameProperty;
            return gameProperty;
        }

        public void UnRegisterProperty(GameProperty gameProperty)
        {
            if (GameProperties.Remove(gameProperty.ID) && _eventHandlers.TryGetValue(gameProperty, out var handler))
            {
                gameProperty.OnValueChanged -= handler;
                _eventHandlers.Remove(gameProperty);
            }
        }

        public GameProperty GetProperty(string id) => GameProperties.TryGetValue(id, out var property) ? property : null;

        public void Dispose()
        {
            if (_isDisposed) return;

            // 取消订阅事件
            foreach (var kvp in _eventHandlers)
            {
                kvp.Key.OnValueChanged -= kvp.Value;
            }
            _eventHandlers.Clear();
            GameProperties.Clear();

            // 减少所有管理的GameProperty的引用计数
            foreach (var propertyId in _managedPropertyIds)
            {
                CombineGamePropertyManager.ReleaseGameProperty(propertyId);
            }
            _managedPropertyIds.Clear();

            _isDisposed = true;
        }
    }
}