/// <summary>
/// CombinePropertySingle 实现了 ICombineGameProperty，
/// 仅包含单一 GameProperty，直接返回该属性的值作为最终结果。
/// 适用于无需属性组合，仅需单属性表现的简单场景。
/// </summary>

using System;
namespace EasyPack
{
    public class CombinePropertySingle : ICombineGameProperty
    {
        public string ID { get; }
        public Func<ICombineGameProperty, float> Calculater { get; }

        private readonly GameProperty _resultHolder;
        public GameProperty ResultHolder => _resultHolder;

        public float GetValue()
        {
            return Calculater(this);
        }

        public CombinePropertySingle(string id, float baseValue = 0)
        {
            ID = id;
            _resultHolder = new GameProperty(id + "@ResultHolder", baseValue);
            Calculater = e => ResultHolder.GetValue();
        }

        public GameProperty GetProperty(string id = "")
        {
            return ResultHolder;
        }
    }
}