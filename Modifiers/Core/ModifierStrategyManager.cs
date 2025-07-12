using System;
using System.Collections.Generic;

namespace EasyPack
{
    /// <summary>
    /// 修饰器策略管理器
    /// </summary>
    public static class ModifierStrategyManager
    {
        private static readonly Dictionary<ModifierType, IModifierStrategy> _strategies = new Dictionary<ModifierType, IModifierStrategy>();

        // 静态构造函数初始化所有策略
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

        /// <summary>
        /// 注册一个修饰器策略
        /// </summary>
        public static void RegisterStrategy(IModifierStrategy strategy)
        {
            _strategies[strategy.Type] = strategy;
        }

        /// <summary>
        /// 获取指定类型的修饰器策略
        /// </summary>
        public static IModifierStrategy GetStrategy(ModifierType type)
        {
            if (_strategies.TryGetValue(type, out var strategy))
                return strategy;

            throw new ArgumentException($"找不到类型为 {type} 的修饰器策略");
        }
    }
}