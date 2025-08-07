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
            // Clamp������ֻ����RangeModifier����
            var rangeMods = modifiers.OfType<RangeModifier>().ToList();

            // �������ȼ���ȡ������ȼ���Clamp������
            var clampMod = rangeMods.OrderByDescending(m => m.Priority).FirstOrDefault();
            if (clampMod != null)
            {
                value = Mathf.Clamp(value, clampMod.Value.x, clampMod.Value.y);
            }
        }
    }
}