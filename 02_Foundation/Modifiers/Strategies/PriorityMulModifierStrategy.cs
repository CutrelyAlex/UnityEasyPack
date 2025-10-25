using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasyPack
{
    public class PriorityMulModifierStrategy : IModifierStrategy
    {
        public ModifierType Type => ModifierType.PriorityMul;

        public void Apply(ref float value, IEnumerable<IModifier> modifiers)
        {
            var floatMods = modifiers.OfType<FloatModifier>().ToList();
            var rangeMods = modifiers.OfType<RangeModifier>().ToList();

            var priorityFloatMul = floatMods.OrderByDescending(m => m.Priority).FirstOrDefault()?.Value ?? 1f;
            var priorityRangeMod = rangeMods.OrderByDescending(m => m.Priority).FirstOrDefault();
            float priorityRangeMul = priorityRangeMod != null ? Random.Range(priorityRangeMod.Value.x, priorityRangeMod.Value.y) : 1f;

            if (floatMods.Any() && rangeMods.Any())
            {
                var floatPriority = floatMods.Max(m => m.Priority);
                var rangePriority = rangeMods.Max(m => m.Priority);
                value *= floatPriority >= rangePriority ? priorityFloatMul : priorityRangeMul;
            }
            else
            {
                value *= priorityFloatMul * priorityRangeMul;
            }
        }
    }
}