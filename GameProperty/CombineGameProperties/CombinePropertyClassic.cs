/// <summary>
/// CombinePropertyClassic 实现了 ICombineGameProperty，
/// 用于组合多个 GameProperty（如基础值、Buff、Debuff 等），
/// 并通过经典的计算方式计算最终属性值：
/// 最终属性 = (属性 + 加法加成) × (1 + 乘法加成百分比) - 减益 × (1 + 乘法减益百分比)
/// 注意这些属性值可以是负的，所以-50%(最大生命值加成)之类的属性也可以正常计算
///     
/// </summary>

using System;
using System.Collections.Generic;
namespace RPGPack
{
    public class CombinePropertyClassic : ICombineGameProperty
    {
        private Dictionary<string, GameProperty> GameProperties { get; set; } = new Dictionary<string, GameProperty>();
        public string ID { get; }
        public Func<ICombineGameProperty, float> Calculater { get; }

        private GameProperty _resultHolder;
        public GameProperty ResultHolder => _resultHolder;

        private float _cacheValue;


        private float _baseCombineValue;
        public float GetBaseValue() => _baseCombineValue;

        public float GetValue() 
        {
            _resultHolder.SetBaseValue(Calculater(this));

            return _resultHolder.GetValue();
        }
  

        public CombinePropertyClassic(string id, float BaseValue, string ConfigProperty, string IncreaseValue, string IncreaseMul, string DecreaseValue, string DecreaseMul)
        {
            _resultHolder = new GameProperty(0, id + "@ResultHolder");
            ID = id;
            RegisterProperty(new GameProperty(BaseValue, ConfigProperty));
            RegisterProperty(new GameProperty(0, IncreaseValue));
            RegisterProperty(new GameProperty(0, IncreaseMul));
            RegisterProperty(new GameProperty(0, DecreaseValue));
            RegisterProperty(new GameProperty(0, DecreaseMul));
            GameProperty BaseProperty = GetProperty(ConfigProperty);
            GameProperty BaseBuffValue = GetProperty(IncreaseValue);
            GameProperty BaseBuffMul = GetProperty(IncreaseMul);
            GameProperty DeBuffValue = GetProperty(DecreaseValue);
            GameProperty DeBuffMul = GetProperty(DecreaseMul);
            Calculater = e =>
            {
                return (BaseProperty.GetValue() + BaseBuffValue.GetValue()) * (BaseBuffMul.GetValue() + 1) - DeBuffValue.GetValue() * (1 + DeBuffMul.GetValue());
            };

            foreach (var prop in GameProperties.Values)
            {
                prop.OnValueChanged += (oldVal, newVal) =>
                {
                    var oldCombine = _cacheValue;
                    var newCombine = Calculater(this);
                    if (!oldCombine.Equals(newCombine))
                    {
                        _cacheValue = newCombine;
                    }
                };
            }
            // 初始化缓存值
            _cacheValue = Calculater(this);
        }

        public GameProperty RegisterProperty(GameProperty gameProperty)
        {
            GameProperties[gameProperty.ID] = gameProperty;
            return gameProperty;
        }

        public void UnRegisterProperty(GameProperty gameProperty) => GameProperties.Remove(gameProperty.ID);

        public GameProperty GetProperty(string id) => GameProperties.TryGetValue(id, out var property) ? property : null;
    }
}