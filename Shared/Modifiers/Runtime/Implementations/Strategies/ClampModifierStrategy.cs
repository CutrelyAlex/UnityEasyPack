using System.Collections.Generic;
using UnityEngine;

namespace EasyPack.Modifiers
{
    public class ClampModifierStrategy : IModifierStrategy
    {
        public ModifierType Type => ModifierType.Clamp;

        public void Apply(ref float value, IEnumerable<IModifier> modifiers)
        {
            // Clamp修改器只对RangeModifier生效
            RangeModifier clampMod = null;
            int maxPriority = int.MinValue;
            foreach (IModifier mod in modifiers)
                if (mod is RangeModifier rm && rm.Priority > maxPriority)
                {
                    maxPriority = rm.Priority;
                    clampMod = rm;
                }

            if (clampMod != null) value = Mathf.Clamp(value, clampMod.Value.x, clampMod.Value.y);
        }
    }
}