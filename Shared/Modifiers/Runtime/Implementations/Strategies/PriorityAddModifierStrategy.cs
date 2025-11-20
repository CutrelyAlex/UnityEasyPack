using System.Collections.Generic;
using UnityEngine;

namespace EasyPack.Modifiers
{
    public class PriorityAddModifierStrategy : IModifierStrategy
    {
        public ModifierType Type => ModifierType.PriorityAdd;

        public void Apply(ref float value, IEnumerable<IModifier> modifiers)
        {
            var floatMods = new List<FloatModifier>();
            var rangeMods = new List<RangeModifier>();
            foreach (var mod in modifiers)
            {
                if (mod is FloatModifier fm) floatMods.Add(fm);
                else if (mod is RangeModifier rm) rangeMods.Add(rm);
            }

            floatMods.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            rangeMods.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            var priorityFloatAdd = floatMods.Count > 0 ? floatMods[0].Value : 0f;
            var priorityRangeMod = rangeMods.Count > 0 ? rangeMods[0] : null;
            float priorityRangeAdd = priorityRangeMod != null ? Random.Range(priorityRangeMod.Value.x, priorityRangeMod.Value.y) : 0f;

            if (floatMods.Count > 0 && rangeMods.Count > 0)
            {
                var floatPriority = floatMods[0].Priority;
                var rangePriority = rangeMods[0].Priority;
                value += floatPriority >= rangePriority ? priorityFloatAdd : priorityRangeAdd;
            }
            else
            {
                value += priorityFloatAdd + priorityRangeAdd;
            }
        }
    }
}