using System.Collections.Generic;
using UnityEngine;

namespace EasyPack.Modifiers
{
    public class AddModifierStrategy : IModifierStrategy
    {
        public ModifierType Type => ModifierType.Add;

        public void Apply(ref float value, IEnumerable<IModifier> modifiers)
        {
            float floatAdd = 0;
            float rangeAdd = 0;
            foreach (IModifier mod in modifiers)
            {
                if (mod is FloatModifier fm)
                    floatAdd += fm.Value;
                else if (mod is RangeModifier rm) rangeAdd += Random.Range(rm.Value.x, rm.Value.y);
            }

            value += floatAdd + rangeAdd;
        }
    }
}