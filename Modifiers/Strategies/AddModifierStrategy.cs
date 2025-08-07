using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasyPack
{
    public class AddModifierStrategy : IModifierStrategy
    {
        public ModifierType Type => ModifierType.Add;

        public void Apply(ref float value, IEnumerable<IModifier> modifiers)
        {
            var floatMods = modifiers.OfType<FloatModifier>().ToList();
            var rangeMods = modifiers.OfType<RangeModifier>().ToList();

            var floatAdd = floatMods.Sum(m => m.Value);
            var rangeAdd = rangeMods.Sum(m => Random.Range(m.Value.x, m.Value.y));
            value += floatAdd + rangeAdd;
        }
    }
}