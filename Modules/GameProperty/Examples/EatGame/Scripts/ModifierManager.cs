using System.Collections.Generic;
using EasyPack.Modifiers;
using UnityEngine;

namespace EasyPack.GamePropertySystem.Example.EatGame
{
    /// <summary>
    ///     修饰符管理器，处理持续时间
    /// </summary>
    public class ModifierManager
    {
        private class TimedModifier
        {
            public FloatModifier Modifier;
            public int RemainingDays;
            public GameProperty TargetProperty;
        }

        private readonly List<TimedModifier> _activeModifiers = new();

        /// <summary>
        ///     添加带持续时间的修饰符
        /// </summary>
        public void AddTimedModifier(GameProperty targetProperty, FloatModifier modifier, int duration)
        {
            Debug.Log(
                $"[ModifierManager] 添加修饰符: 属性={targetProperty.ID}, 类型={modifier.Type}, 值={modifier.Value}, 优先级={modifier.Priority}, 持续时间={duration}天");

            var timedModifier = new TimedModifier
            {
                Modifier = modifier,
                RemainingDays = duration + 1, // 加1以确保持续正确的天数
                TargetProperty = targetProperty,
            };

            _activeModifiers.Add(timedModifier);
            targetProperty.AddModifier(modifier);

            // 显示添加后的属性值变化
            float oldValue = targetProperty.GetValue();
            Debug.Log($"[ModifierManager] 添加前属性值: {oldValue}, 添加后属性值: {targetProperty.GetValue()}");
            Debug.Log($"[ModifierManager] 当前活跃修饰符数量: {_activeModifiers.Count}");
        }

        /// <summary>
        ///     处理每日修饰符更新（减少持续时间，移除过期修饰符）
        /// </summary>
        public void ProcessDailyModifiers()
        {
            Debug.Log($"[ModifierManager] 开始处理每日修饰符更新, 当前活跃修饰符数量: {_activeModifiers.Count}");

            for (int i = _activeModifiers.Count - 1; i >= 0; i--)
            {
                TimedModifier timedModifier = _activeModifiers[i];
                Debug.Log(
                    $"[ModifierManager] 处理修饰符 {i}: 属性={timedModifier.TargetProperty.ID}, 类型={timedModifier.Modifier.Type}, 值={timedModifier.Modifier.Value}, 剩余时间={timedModifier.RemainingDays}天");

                timedModifier.RemainingDays--;

                if (timedModifier.RemainingDays <= 0)
                {
                    // 移除修饰符
                    Debug.Log(
                        $"[ModifierManager] 修饰符到期，移除: 属性={timedModifier.TargetProperty.ID}, 类型={timedModifier.Modifier.Type}, 值={timedModifier.Modifier.Value}");
                    float valueBeforeRemove = timedModifier.TargetProperty.GetValue();
                    timedModifier.TargetProperty.RemoveModifier(timedModifier.Modifier);
                    float valueAfterRemove = timedModifier.TargetProperty.GetValue();
                    Debug.Log($"[ModifierManager] 移除修饰符前后属性值变化: {valueBeforeRemove} -> {valueAfterRemove}");
                    _activeModifiers.RemoveAt(i);
                }
                else
                {
                    Debug.Log($"[ModifierManager] 修饰符继续生效: 剩余{timedModifier.RemainingDays}天");
                }
            }

            Debug.Log($"[ModifierManager] 每日修饰符更新完成, 剩余活跃修饰符数量: {_activeModifiers.Count}");
        }

        /// <summary>
        ///     清除所有修饰符
        /// </summary>
        public void ClearAllModifiers()
        {
            foreach (TimedModifier timedModifier in _activeModifiers)
            {
                timedModifier.TargetProperty.RemoveModifier(timedModifier.Modifier);
            }

            _activeModifiers.Clear();
        }

        /// <summary>
        ///     获取活跃修饰符数量
        /// </summary>
        public int GetActiveModifierCount() => _activeModifiers.Count;
    }
}