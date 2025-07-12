using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasyPack
{
    /// <summary>
    /// 限制范围修饰器策略
    /// </summary>
    public class ClampModifierStrategy : IModifierStrategy
    {
        public ModifierType Type => ModifierType.Clamp;

        public void Apply(ref float value, IEnumerable<IModifier> modifiers)
        {
            // Clamp修饰器只处理RangeModifier类型
            var rangeMods = modifiers.OfType<RangeModifier>().ToList();

            // 根据优先级获取最高优先级的Clamp修饰器
            var clampMod = rangeMods.OrderByDescending(m => m.Priority).FirstOrDefault();
            if (clampMod != null)
            {
                value = Mathf.Clamp(value, clampMod.Value.x, clampMod.Value.y);
            }
        }
    }
}