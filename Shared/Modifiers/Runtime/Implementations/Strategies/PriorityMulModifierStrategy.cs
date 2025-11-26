using System.Collections.Generic;
using UnityEngine;

namespace EasyPack.Modifiers
{
    public class PriorityMulModifierStrategy : IModifierStrategy
    {
        public ModifierType Type => ModifierType.PriorityMul;

        public void Apply(ref float value, IEnumerable<IModifier> modifiers)
        {
            var floatMods = new List<FloatModifier>();
            var rangeMods = new List<RangeModifier>();
            foreach (IModifier mod in modifiers)
                if (mod is FloatModifier fm) floatMods.Add(fm);
                else if (mod is RangeModifier rm) rangeMods.Add(rm);

            floatMods.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            rangeMods.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            float priorityFloatMul = floatMods.Count > 0 ? floatMods[0].Value : 1f;
            RangeModifier priorityRangeMod = rangeMods.Count > 0 ? rangeMods[0] : null;
            float priorityRangeMul = priorityRangeMod != null
                ? Random.Range(priorityRangeMod.Value.x, priorityRangeMod.Value.y)
                : 1f;

            if (floatMods.Count > 0 && rangeMods.Count > 0)
            {
                int floatPriority = floatMods[0].Priority;
                int rangePriority = rangeMods[0].Priority;
                value *= floatPriority >= rangePriority ? priorityFloatMul : priorityRangeMul;
            }
            else
            {
                value *= priorityFloatMul * priorityRangeMul;
            }
        }
    }
}