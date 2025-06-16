using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RPGPack
{
    /// <summary>
    /// ³Ë·¨ÐÞÊÎÆ÷²ßÂÔ
    /// </summary>
    public class MulModifierStrategy : IModifierStrategy
    {
        public ModifierType Type => ModifierType.Mul;

        public void Apply(ref float value, IEnumerable<IModifier> modifiers)
        {
            var floatMods = modifiers.OfType<FloatModifier>().ToList();
            var rangeMods = modifiers.OfType<RangeModifier>().ToList();

            var floatMul = floatMods.Aggregate(1f, (acc, m) => acc * m.Value);
            var rangeMul = rangeMods.Aggregate(1f, (acc, m) => acc * Random.Range(m.Value.x, m.Value.y));
            value *= floatMul * rangeMul;
        }
    }
}