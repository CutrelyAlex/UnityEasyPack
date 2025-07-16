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
            _resultHolder = new GameProperty(id + "@ResultHolder", 0);
            ID = id;
            _baseCombineValue = BaseValue;

            RegisterProperty(new GameProperty(ConfigProperty, BaseValue));
            RegisterProperty(new GameProperty(IncreaseValue, 0));
            RegisterProperty(new GameProperty(IncreaseMul, 0));
            RegisterProperty(new GameProperty(DecreaseValue, 0));
            RegisterProperty(new GameProperty(DecreaseMul, 0));

            GameProperty BaseProperty = GetProperty(ConfigProperty);
            GameProperty BaseBuffValue = GetProperty(IncreaseValue);
            GameProperty BaseBuffMul = GetProperty(IncreaseMul);
            GameProperty DeBuffValue = GetProperty(DecreaseValue);
            GameProperty DeBuffMul = GetProperty(DecreaseMul);

            Calculater = e =>
            {
                return (BaseProperty.GetValue() + BaseBuffValue.GetValue()) * (BaseBuffMul.GetValue() + 1) - DeBuffValue.GetValue() * (1 + DeBuffMul.GetValue());
            };

            // 改进事件处理，避免内存泄漏
            SubscribeToPropertyChanges();

            // 初始化缓存值
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

            foreach (var kvp in _eventHandlers)
            {
                kvp.Key.OnValueChanged -= kvp.Value;
            }
            _eventHandlers.Clear();
            GameProperties.Clear();

            _isDisposed = true;
        }
    }
}