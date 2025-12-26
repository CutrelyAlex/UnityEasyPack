using System.Collections.Generic;
using UnityEngine;

namespace EasyPack.Modifiers
{
    public class AfterAddModifierStrategy : IModifierStrategy
    {
        public ModifierType Type => ModifierType.AfterAdd;

        public void Apply(ref float value, IEnumerable<IModifier> modifiers)
        {
            float floatAfterAdd = 0;
            float rangeAfterAdd = 0;
            foreach (IModifier mod in modifiers)
            {
                if (mod is FloatModifier fm)
                {
                    floatAfterAdd += fm.Value;
                }
                else if (mod is RangeModifier rm) rangeAfterAdd += Random.Range(rm.Value.x, rm.Value.y);
            }

            value += floatAfterAdd + rangeAfterAdd;
        }
    }
}