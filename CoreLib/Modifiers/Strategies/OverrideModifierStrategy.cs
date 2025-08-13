using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasyPack
{
    public class OverrideModifierStrategy : IModifierStrategy
    {
        public ModifierType Type => ModifierType.Override;

        public void Apply(ref float value, IEnumerable<IModifier> modifiers)
        {
            var floatMods = modifiers.OfType<FloatModifier>().ToList();
            var rangeMods = modifiers.OfType<RangeModifier>().ToList();

            var floatOverrideMod = floatMods.OrderByDescending(m => m.Priority).FirstOrDefault();
            var rangeOverrideMod = rangeMods.OrderByDescending(m => m.Priority).FirstOrDefault();

            if (floatOverrideMod != null && rangeOverrideMod != null)
            {
                value = floatOverrideMod.Priority >= rangeOverrideMod.Priority ?
                        floatOverrideMod.Value :
                        Random.Range(rangeOverrideMod.Value.x, rangeOverrideMod.Value.y);
            }
            else if (floatOverrideMod != null)
            {
                value = floatOverrideMod.Value;
            }
            else if (rangeOverrideMod != null)
            {
                value = Random.Range(rangeOverrideMod.Value.x, rangeOverrideMod.Value.y);
            }
        }
    }
}