using System.Collections.Generic;
using UnityEngine;

namespace EasyPack.Modifiers
{
    /// <summary>
    ///     乘法修改器策略
    /// </summary>
    public class MulModifierStrategy : IModifierStrategy
    {
        public ModifierType Type => ModifierType.Mul;

        public void Apply(ref float value, IEnumerable<IModifier> modifiers)
        {
            float floatMul = 1f;
            float rangeMul = 1f;
            foreach (IModifier mod in modifiers)
            {
                if (mod is FloatModifier fm)
                    floatMul *= fm.Value;
                else if (mod is RangeModifier rm) rangeMul *= Random.Range(rm.Value.x, rm.Value.y);
            }

            value *= floatMul * rangeMul;
        }
    }
}