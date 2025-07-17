using System;
using System.Collections.Generic;

namespace EasyPack
{
    public class CombinePropertyCustom : ICombineGameProperty
    {
        private Dictionary<string, GameProperty> GameProperties { get; set; } = new Dictionary<string, GameProperty>();
        public string ID { get; }

        private readonly float _baseCombineValue;
        public float GetBaseValue() => _baseCombineValue;
        public Func<ICombineGameProperty, float> Calculater { get; set; }

        private readonly GameProperty _resultHolder;
        public GameProperty ResultHolder => _resultHolder;

        private readonly Dictionary<GameProperty, Action<float, float>> _eventHandlers = new();
        private bool _isDisposed = false;

        public bool IsValid() => !_isDisposed && _resultHolder != null;

        public float GetValue()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(CombinePropertyCustom));

            _resultHolder.SetBaseValue(Calculater?.Invoke(this) ?? _baseCombineValue);
            return _resultHolder.GetValue();
        }

        public CombinePropertyCustom(string id, float baseValue = 0)
        {
            ID = id;
            _baseCombineValue = baseValue;
            _resultHolder = new GameProperty(id + "@ResultHolder", baseValue);
            Calculater = e => _baseCombineValue;
        }

        public GameProperty RegisterProperty(GameProperty gameProperty)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(CombinePropertyCustom));

            if (gameProperty == null)
                throw new ArgumentNullException(nameof(gameProperty));

            GameProperties[gameProperty.ID] = gameProperty;

            var handler = new Action<float, float>((oldVal, newVal) =>
            {
            });

            _eventHandlers[gameProperty] = handler;
            gameProperty.OnValueChanged += handler;

            return gameProperty;
        }

        public void UnRegisterProperty(GameProperty gameProperty)
        {
            if (gameProperty == null || _isDisposed) return;

            if (GameProperties.Remove(gameProperty.ID) && _eventHandlers.TryGetValue(gameProperty, out var handler))
            {
                gameProperty.OnValueChanged -= handler;
                _eventHandlers.Remove(gameProperty);
            }
        }

        public GameProperty GetProperty(string id) =>
            _isDisposed ? null : GameProperties.TryGetValue(id, out var property) ? property : null;

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