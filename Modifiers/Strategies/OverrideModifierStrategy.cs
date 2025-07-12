using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasyPack
{
    /// <summary>
    /// ����ֵ����������
    /// </summary>
    public class OverrideModifierStrategy : IModifierStrategy
    {
        public ModifierType Type => ModifierType.Override;

        public void Apply(ref float value, IEnumerable<IModifier> modifiers)
        {
            var floatMods = modifiers.OfType<FloatModifier>().ToList();
            var rangeMods = modifiers.OfType<RangeModifier>().ToList();

            // ��ȡ������ȼ���������
            var floatOverrideMod = floatMods.OrderByDescending(m => m.Priority).FirstOrDefault();
            var rangeOverrideMod = rangeMods.OrderByDescending(m => m.Priority).FirstOrDefault();

            // �������������ͺ����ȼ�����ֵ
            if (floatOverrideMod != null && rangeOverrideMod != null)
            {
                // �������Ͷ�����ʱ���Ƚ����ȼ�
                value = floatOverrideMod.Priority >= rangeOverrideMod.Priority ?
                        floatOverrideMod.Value :
                        Random.Range(rangeOverrideMod.Value.x, rangeOverrideMod.Value.y);
            }
            else if (floatOverrideMod != null)
            {
                // ֻ�и���������
                value = floatOverrideMod.Value;
            }
            else if (rangeOverrideMod != null)
            {
                // ֻ�з�Χ������
                value = Random.Range(rangeOverrideMod.Value.x, rangeOverrideMod.Value.y);
            }
        }
    }
}