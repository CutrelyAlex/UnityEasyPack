using System;
using System.Collections.Generic;
using EasyPack.GamePropertySystem;

namespace EasyPack.BuffSystem
{
    /// <summary>
    /// Buff 模块实现，用于将修饰符应用到 GameProperty 上
    /// 在 Buff 生命周期的不同事件（创建、移除、堆叠变化）中添加或移除修饰符
    /// </summary>
    public class CastModifierToProperty : BuffModule
    {
        /// <summary>
        /// 获取或设置组合属性管理器，用于查找目标属性
        /// </summary>
        public GamePropertyManager CombineGamePropertyManager { get; set; }

        /// <summary>
        /// 获取或设置要应用的修饰符模板
        /// </summary>
        public IModifier Modifier { get; set; }

        /// <summary>
        /// 组合属性的 ID，用于从管理器中获取对应的组合属性
        /// </summary>
        public string CombinePropertyID { get; set; }

        /// <summary>
        /// 具体属性的 ID，用于在组合属性中查找相应的 GameProperty
        /// 为空时使用组合属性的结果属性
        /// </summary>
        public string PropertyID { get; set; }

        /// <summary>
        /// 存储已应用的所有修饰符实例
        /// </summary>
        private readonly List<IModifier> _appliedModifiers = new List<IModifier>();

        /// <summary>
        /// 缓存的属性实例，避免重复查找
        /// </summary>
        private GameProperty _cachedProperty;

        /// <summary>
        /// 获取目标属性，如果未缓存则查找并缓存
        /// </summary>
        /// <returns>目标 GameProperty 实例，未找到返回 null</returns>
        private GameProperty GetProperty()
        {
            if (_cachedProperty == null && CombineGamePropertyManager != null)
            {
                _cachedProperty = CombineGamePropertyManager.GetGamePropertyFromCombine(CombinePropertyID, PropertyID);
            }
            return _cachedProperty;
        }

        /// <summary>
        /// 创建一个新的 CastModifierToProperty 实例并注册生命周期回调
        /// </summary>
        /// <param name="modifier">要应用的修饰符模板</param>
        /// <param name="combinePropertyID">组合属性 ID</param>
        /// <param name="combineGamePropertyManager">组合属性管理器</param>
        /// <param name="propertyID">具体属性 ID（可选，为空时使用组合属性的结果属性）</param>
        /// <exception cref="ArgumentNullException">当必需参数为 null 时抛出</exception>
        public CastModifierToProperty(IModifier modifier, string combinePropertyID, GamePropertyManager combineGamePropertyManager, string propertyID = "")
        {
            Modifier = modifier ?? throw new ArgumentNullException(nameof(modifier));
            CombinePropertyID = combinePropertyID ?? throw new ArgumentNullException(nameof(combinePropertyID));
            PropertyID = propertyID;
            CombineGamePropertyManager = combineGamePropertyManager ?? throw new ArgumentNullException(nameof(combineGamePropertyManager));

            // 注册 Buff 生命周期回调
            RegisterCallback(BuffCallBackType.OnCreate, OnCreate);
            RegisterCallback(BuffCallBackType.OnAddStack, OnAddStack);
            RegisterCallback(BuffCallBackType.OnRemove, OnRemove);
            RegisterCallback(BuffCallBackType.OnReduceStack, OnReduceStack);
        }

        /// <summary>
        /// 处理 Buff 创建时的回调，添加修饰符到目标属性
        /// </summary>
        /// <param name="buff">Buff 实例</param>
        /// <param name="parameters">回调参数</param>
        private void OnCreate(Buff buff, object[] parameters)
        {
            AddModifier();
        }

        /// <summary>
        /// 处理 Buff 堆叠层数增加时的回调，添加修饰符到目标属性
        /// </summary>
        /// <param name="buff">Buff 实例</param>
        /// <param name="parameters">回调参数</param>
        private void OnAddStack(Buff buff, object[] parameters)
        {
            AddModifier();
        }

        /// <summary>
        /// 处理 Buff 移除时的回调，移除所有已应用的修饰符
        /// </summary>
        /// <param name="buff">Buff 实例</param>
        /// <param name="parameters">回调参数</param>
        private void OnRemove(Buff buff, object[] parameters)
        {
            RemoveAllModifiers();
        }

        /// <summary>
        /// 处理 Buff 堆叠层数减少时的回调，移除一个修饰符
        /// </summary>
        /// <param name="buff">Buff 实例</param>
        /// <param name="parameters">回调参数</param>
        private void OnReduceStack(Buff buff, object[] parameters)
        {
            RemoveSingleModifier();
        }

        /// <summary>
        /// 添加修饰符到目标属性（克隆模板以支持多次应用）
        /// Modifier 字段已在构造函数验证非 null，此处无需重复验证
        /// </summary>
        private void AddModifier()
        {
            var property = GetProperty();
            if (property == null)
                return;

            var modifierClone = Modifier.Clone();
            property.AddModifier(modifierClone);

            // 记录已应用的修饰符以便后续移除
            _appliedModifiers.Add(modifierClone);
        }

        /// <summary>
        /// 移除最后添加的修饰符（对应堆叠层数减少）
        /// </summary>
        private void RemoveSingleModifier()
        {
            var property = GetProperty();
            if (property == null || _appliedModifiers.Count == 0)
                return;

            // 移除最后添加的修饰符
            var lastModifier = _appliedModifiers[^1];
            property.RemoveModifier(lastModifier);
            _appliedModifiers.RemoveAt(_appliedModifiers.Count - 1);
        }

        /// <summary>
        /// 移除所有已应用的修饰符（对应 Buff 完全移除）
        /// </summary>
        private void RemoveAllModifiers()
        {
            var property = GetProperty();
            if (property == null || _appliedModifiers.Count == 0)
                return;

            // 逆序移除所有已应用的修饰符
            for (int i = _appliedModifiers.Count - 1; i >= 0; i--)
            {
                property.RemoveModifier(_appliedModifiers[i]);
            }
            _appliedModifiers.Clear();
        }
    }
}