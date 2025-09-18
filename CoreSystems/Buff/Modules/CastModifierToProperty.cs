using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyPack
{
    /// <summary>
    /// 一个BuffModule，用于将Modifier应用到GameProperty上。
    /// 在不同的回调事件(如创建、移除等)中，可以对目标的GameProperty添加或移除修饰器。
    /// </summary>
    public class CastModifierToProperty : BuffModule
    {
        public CombineGamePropertyManager CombineGamePropertyManager { get; set; }
        /// <summary>
        /// 要应用的修饰器
        /// </summary>
        public IModifier Modifier { get; set; }

        /// <summary>
        /// 组合属性的ID，用于从CombineGamePropertyManager中获取对应的组合属性
        /// </summary>
        public string CombinePropertyID { get; set; }

        /// <summary>
        /// 具体属性的ID，用于在组合属性中查找相应的GameProperty
        /// 可为空，如果为空则使用组合属性的结果属性
        /// </summary>
        public string PropertyID { get; set; }

        /// <summary>
        /// 存储已应用的所有修饰器，便于移除
        /// </summary>
        private readonly List<IModifier> _appliedModifiers = new List<IModifier>();

        /// <summary>
        /// 创建一个新的CastModifierToProperty实例，并注册多个生命周期回调
        /// </summary>
        /// <param name="modifier">要应用的修饰器</param>
        /// <param name="combinePropertyID">组合属性ID</param>
        /// <param name="propertyID">具体属性ID(可选)</param>
        public CastModifierToProperty(IModifier modifier, string combinePropertyID, CombineGamePropertyManager combineGamePropertyManager, string propertyID = "")
        {
            Modifier = modifier;
            CombinePropertyID = combinePropertyID;
            PropertyID = propertyID;
            CombineGamePropertyManager = combineGamePropertyManager;

            // 注册各种生命周期回调
            RegisterCallback(BuffCallBackType.OnCreate, OnCreate);
            RegisterCallback(BuffCallBackType.OnAddStack, OnAddStack);
            RegisterCallback(BuffCallBackType.OnRemove, OnRemove);
            RegisterCallback(BuffCallBackType.OnReduceStack, OnReduceStack);
        }

        /// <summary>
        /// 处理Buff创建时的回调
        /// </summary>
        private void OnCreate(Buff buff, object[] parameters)
        {
            AddModifier();
        }

        /// <summary>
        /// 处理Buff层数增加时的回调
        /// </summary>
        private void OnAddStack(Buff buff, object[] parameters)
        {
            AddModifier();
        }

        /// <summary>
        /// 处理Buff移除时的回调
        /// </summary>
        private void OnRemove(Buff buff, object[] parameters)
        {
            RemoveAllModifiers();
        }

        /// <summary>
        /// 处理Buff层数减少时的回调
        /// </summary>
        private void OnReduceStack(Buff buff, object[] parameters)
        {
            RemoveSingleModifier();
        }

        /// <summary>
        /// 添加修饰器到目标属性
        /// </summary>
        private void AddModifier()
        {
            var property = CombineGamePropertyManager.GetGamePropertyFromCombine(CombinePropertyID, PropertyID);
            if (property == null || Modifier == null)
                return;

            var modifierClone = Modifier.Clone();
            property.AddModifier(modifierClone);

            // 记录已应用的修饰器以便后续移除
            _appliedModifiers.Add(modifierClone);
        }

        /// <summary>
        /// 移除单个修饰器
        /// </summary>
        private void RemoveSingleModifier()
        {
            var property = CombineGamePropertyManager.GetGamePropertyFromCombine(CombinePropertyID, PropertyID);
            if (property == null || _appliedModifiers.Count == 0)
                return;

            // 移除最后添加的修饰器
            var lastModifier = _appliedModifiers[^1];
            property.RemoveModifier(lastModifier);
            _appliedModifiers.RemoveAt(_appliedModifiers.Count - 1);
        }

        /// <summary>
        /// 移除所有已应用的修饰器
        /// </summary>
        private void RemoveAllModifiers()
        {
            var property = CombineGamePropertyManager.GetGamePropertyFromCombine(CombinePropertyID, PropertyID);
            if (property == null)
                return;

            // 移除所有已应用的修饰器
            property.RemoveModifiers(_appliedModifiers);
            _appliedModifiers.Clear();
        }
    }
}