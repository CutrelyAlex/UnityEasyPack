using System.Collections.Generic;
using EasyPack.GamePropertySystem;
using UnityEngine;

namespace EasyPack.GamePropertySystem.Example.EatGame
{
    /// <summary>
    /// 食物数据类
    /// </summary>
    [System.Serializable]
    public class Food
    {
        /// <summary>
        /// 食物名字
        /// </summary>
        public string Name;

        /// <summary>
        /// 食物描述
        /// </summary>
        public string Description;

        /// <summary>
        /// 即时效果：属性名 -> 变化值
        /// </summary>
        public Dictionary<string, float> ImmediateEffects = new Dictionary<string, float>();

        /// <summary>
        /// 持续效果：属性名 -> (修饰符类型, 修饰符值, 持续天数)
        /// </summary>
        public Dictionary<string, (ModifierType type, float value, int duration)> SustainedEffects = 
            new Dictionary<string, (ModifierType, float, int)>();

        /// <summary>
        /// 应用即时效果到玩家属性
        /// </summary>
        public void ApplyImmediateEffects(PlayerAttributes player)
        {
            foreach (var effect in ImmediateEffects)
            {
                switch (effect.Key)
                {
                    case "Satiety":
                        player.Satiety.SetBaseValue(Mathf.Clamp(player.Satiety.GetValue() + effect.Value, 0, player.SatietyCapacity.GetValue()));
                        break;
                    case "Health":
                        player.Health.SetBaseValue(Mathf.Clamp(player.Health.GetValue() + effect.Value, 0, player.HealthCapacity.GetValue()));
                        break;
                    case "Sanity":
                        player.Sanity.SetBaseValue(Mathf.Clamp(player.Sanity.GetValue() + effect.Value, 0, player.SanityCapacity.GetValue()));
                        break;
                }
            }
        }

        /// <summary>
        /// 应用持续效果到玩家属性
        /// </summary>
        public void ApplySustainedEffects(PlayerAttributes player)
        {
            foreach (var effect in SustainedEffects)
            {
                Debug.Log($"[Food] 应用持续效果: {effect.Key}, 类型={effect.Value.type}, 值={effect.Value.value}, 持续时间={effect.Value.duration}");

                var modifier = new FloatModifier(effect.Value.type, 0, effect.Value.value);
                var duration = effect.Value.duration;

                switch (effect.Key)
                {
                    case "SatietyChangePerDay":
                        Debug.Log($"[Food] 添加到SatietyChangePerDay: 基础值={player.SatietyChangePerDay.GetBaseValue()}, 添加modifier后值={player.SatietyChangePerDay.GetValue() + (effect.Value.type == ModifierType.Add ? effect.Value.value : 0)}");
                        player.ModifierManager.AddTimedModifier(player.SatietyChangePerDay, modifier, duration);
                        break;
                    case "HealthChangePerDay":
                        Debug.Log($"[Food] 添加到HealthChangePerDay: 基础值={player.HealthChangePerDay.GetBaseValue()}, 添加modifier后值={player.HealthChangePerDay.GetValue() + (effect.Value.type == ModifierType.Add ? effect.Value.value : 0)}");
                        player.ModifierManager.AddTimedModifier(player.HealthChangePerDay, modifier, duration);
                        break;
                    case "SanityChangePerDay":
                        Debug.Log($"[Food] 添加到SanityChangePerDay: 基础值={player.SanityChangePerDay.GetBaseValue()}, 添加modifier后值={player.SanityChangePerDay.GetValue() + (effect.Value.type == ModifierType.Add ? effect.Value.value : 0)}");
                        player.ModifierManager.AddTimedModifier(player.SanityChangePerDay, modifier, duration);
                        break;
                }
            }
        }
    }
}