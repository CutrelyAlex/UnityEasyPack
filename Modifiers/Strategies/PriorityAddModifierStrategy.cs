using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace EasyPack
{
    public class PriorityAddModifierStrategy : IModifierStrategy
    {
        public ModifierType Type => ModifierType.PriorityAdd;

        public void Apply(ref float value, IEnumerable<IModifier> modifiers)
        {
            var floatMods = modifiers.OfType<FloatModifier>().ToList();
            var rangeMods = modifiers.OfType<RangeModifier>().ToList();

            var priorityFloatAdd = floatMods.OrderByDescending(m => m.Priority).FirstOrDefault()?.Value ?? 0f;
            var priorityRangeMod = rangeMods.OrderByDescending(m => m.Priority).FirstOrDefault();
            float priorityRangeAdd = priorityRangeMod != null ? Random.Range(priorityRangeMod.Value.x, priorityRangeMod.Value.y) : 0f;

            if (floatMods.Any() && rangeMods.Any())
            {
                var floatPriority = floatMods.Max(m => m.Priority);
                var rangePriority = rangeMods.Max(m => m.Priority);
                value += floatPriority >= rangePriority ? priorityFloatAdd : priorityRangeAdd;
            }
            else
            {
                value += priorityFloatAdd + priorityRangeAdd;
            }
        }
    }
}