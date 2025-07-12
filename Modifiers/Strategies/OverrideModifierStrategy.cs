using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasyPack
{
    /// <summary>
    /// 覆盖值修饰器策略
    /// </summary>
    public class OverrideModifierStrategy : IModifierStrategy
    {
        public ModifierType Type => ModifierType.Override;

        public void Apply(ref float value, IEnumerable<IModifier> modifiers)
        {
            var floatMods = modifiers.OfType<FloatModifier>().ToList();
            var rangeMods = modifiers.OfType<RangeModifier>().ToList();

            // 获取最高优先级的修饰器
            var floatOverrideMod = floatMods.OrderByDescending(m => m.Priority).FirstOrDefault();
            var rangeOverrideMod = rangeMods.OrderByDescending(m => m.Priority).FirstOrDefault();

            // 根据修饰器类型和优先级覆盖值
            if (floatOverrideMod != null && rangeOverrideMod != null)
            {
                // 两种类型都存在时，比较优先级
                value = floatOverrideMod.Priority >= rangeOverrideMod.Priority ?
                        floatOverrideMod.Value :
                        Random.Range(rangeOverrideMod.Value.x, rangeOverrideMod.Value.y);
            }
            else if (floatOverrideMod != null)
            {
                // 只有浮点修饰器
                value = floatOverrideMod.Value;
            }
            else if (rangeOverrideMod != null)
            {
                // 只有范围修饰器
                value = Random.Range(rangeOverrideMod.Value.x, rangeOverrideMod.Value.y);
            }
        }
    }
}