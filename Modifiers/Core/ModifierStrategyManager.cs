using System;
using System.Collections.Generic;

namespace EasyPack
{
    /// <summary>
    /// ���������Թ�����
    /// </summary>
    public static class ModifierStrategyManager
    {
        private static readonly Dictionary<ModifierType, IModifierStrategy> _strategies = new Dictionary<ModifierType, IModifierStrategy>();

        // ��̬���캯����ʼ�����в���
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
        /// ע��һ������������
        /// </summary>
        public static void RegisterStrategy(IModifierStrategy strategy)
        {
            _strategies[strategy.Type] = strategy;
        }

        /// <summary>
        /// ��ȡָ�����͵�����������
        /// </summary>
        public static IModifierStrategy GetStrategy(ModifierType type)
        {
            if (_strategies.TryGetValue(type, out var strategy))
                return strategy;

            throw new ArgumentException($"�Ҳ�������Ϊ {type} ������������");
        }
    }
}