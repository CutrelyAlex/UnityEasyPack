using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasyPack
{
    public class ClampModifierStrategy : IModifierStrategy
    {
        public ModifierType Type => ModifierType.Clamp;

        public void Apply(ref float value, IEnumerable<IModifier> modifiers)
        {
            // Clamp修改器只对RangeModifier生效
            var rangeMods = modifiers.OfType<RangeModifier>().ToList();

            // 按优先级获取最高优先级的Clamp修改器
            var clampMod = rangeMods.OrderByDescending(m => m.Priority).FirstOrDefault();
            if (clampMod != null)
            {
                value = Mathf.Clamp(value, clampMod.Value.x, clampMod.Value.y);
            }
        }
    }
}