using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasyPack
{
    /// <summary>
    /// 后置加法修饰器策略
    /// </summary>
    public class AfterAddModifierStrategy : IModifierStrategy
    {
        public ModifierType Type => ModifierType.AfterAdd;

        public void Apply(ref float value, IEnumerable<IModifier> modifiers)
        {
            var floatMods = modifiers.OfType<FloatModifier>().ToList();
            var rangeMods = modifiers.OfType<RangeModifier>().ToList();

            var floatAfterAdd = floatMods.Sum(m => m.Value);
            var rangeAfterAdd = rangeMods.Sum(m => Random.Range(m.Value.x, m.Value.y));
            value += floatAfterAdd + rangeAfterAdd;
        }
    }
}