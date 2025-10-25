using System;
using System.Collections.Generic;

namespace EasyPack
{
    public static class ModifierStrategyManager
    {
        private static readonly Dictionary<ModifierType, IModifierStrategy> _strategies = new();
        public static readonly ModifierType[] MODIFIER_TYPE_ORDER = new ModifierType[]
        {
            ModifierType.Add,
            ModifierType.PriorityAdd,
            ModifierType.Mul,
            ModifierType.PriorityMul,
            ModifierType.AfterAdd,
            ModifierType.Override,
            ModifierType.Clamp
        };
        static ModifierStrategyManager()
        {
            RegisterStrategy(new AddModifierStrategy());
            RegisterStrategy(new PriorityAddModifierStrategy());
            RegisterStrategy(new MulModifierStrategy());
            RegisterStrategy(new PriorityMulModifierStrategy());
            RegisterStrategy(new AfterAddModifierStrategy());
            RegisterStrategy(new ClampModifierStrategy());
            RegisterStrategy(new OverrideModifierStrategy());
        }

        public static void RegisterStrategy(IModifierStrategy strategy)
        {
            _strategies[strategy.Type] = strategy;
        }

        public static IModifierStrategy GetStrategy(ModifierType type)
        {
            if (_strategies.TryGetValue(type, out var strategy))
                return strategy;

            throw new ArgumentException($"找不到类型为 {type} 的修改器策略");
        }
    }
}