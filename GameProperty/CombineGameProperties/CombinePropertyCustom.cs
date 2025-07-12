﻿/// <summary>
/// CombinePropertyCustom 实现了 ICombineGameProperty，
/// 支持通过自定义委托（Func）灵活定义属性组合和计算逻辑。
/// 适用于需要特殊或复杂属性计算方式的场景。
/// </summary>

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

        public float GetValue()
        {
            _resultHolder.SetBaseValue(Calculater(this));
            
            return _resultHolder.GetValue();
        }

        public CombinePropertyCustom(string id, float initValue = 0)
        {
            ID = id;
            _resultHolder = new GameProperty(initValue, id + "@ResultHolder");
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