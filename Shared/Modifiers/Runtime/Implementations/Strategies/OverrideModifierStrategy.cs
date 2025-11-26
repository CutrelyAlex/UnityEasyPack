using System.Collections.Generic;
using UnityEngine;

namespace EasyPack.Modifiers
{
    public class OverrideModifierStrategy : IModifierStrategy
    {
        public ModifierType Type => ModifierType.Override;

        public void Apply(ref float value, IEnumerable<IModifier> modifiers)
        {
            FloatModifier floatOverrideMod = null;
            RangeModifier rangeOverrideMod = null;
            int maxFloatPriority = int.MinValue;
            int maxRangePriority = int.MinValue;

            foreach (IModifier mod in modifiers)
            {
                if (mod is FloatModifier fm && fm.Priority > maxFloatPriority)
                {
                    maxFloatPriority = fm.Priority;
                    floatOverrideMod = fm;
                }
                else if (mod is RangeModifier rm && rm.Priority > maxRangePriority)
                {
                    maxRangePriority = rm.Priority;
                    rangeOverrideMod = rm;
                }
            }

            if (floatOverrideMod != null && rangeOverrideMod != null)
                value = floatOverrideMod.Priority >= rangeOverrideMod.Priority
                    ? floatOverrideMod.Value
                    : Random.Range(rangeOverrideMod.Value.x, rangeOverrideMod.Value.y);
            else if (floatOverrideMod != null)
                value = floatOverrideMod.Value;
            else if (rangeOverrideMod != null) value = Random.Range(rangeOverrideMod.Value.x, rangeOverrideMod.Value.y);
        }
    }
}