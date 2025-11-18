using EasyPack.Modifiers;
using System.Collections.Generic;
using UnityEngine;

namespace EasyPack.GamePropertySystem.Example.EatGame
{
    /// <summary>
    /// 食物随机生成器
    /// </summary>
    public class FoodGenerator
    {
        private readonly List<Food> _foodTemplates = new List<Food>();

        public FoodGenerator()
        {
            InitializeFoodTemplates();
        }

        private void InitializeFoodTemplates()
        {
            // 新鲜苹果 (加法)
            var apple = new Food
            {
                Name = "新鲜苹果",
                Description = "新鲜多汁的苹果，富含维生素"
            };
            apple.ImmediateEffects["Satiety"] = 20f;
            apple.ImmediateEffects["Sanity"] = 5f;
            apple.SustainedEffects["SatietyChangePerDay"] = (ModifierType.Add, 2f, 2);
            _foodTemplates.Add(apple);

            // 过期牛奶 (加法)
            var milk = new Food
            {
                Name = "过期牛奶",
                Description = "闻起来有点酸，但还能喝"
            };
            milk.ImmediateEffects["Satiety"] = 15f;
            milk.SustainedEffects["HealthChangePerDay"] = (ModifierType.Add, -3f, 3);
            milk.SustainedEffects["SanityChangePerDay"] = (ModifierType.Add, -10f, 3);
            _foodTemplates.Add(milk);

            // 生肉 (加法)
            var rawMeat = new Food
            {
                Name = "生肉",
                Description = "新鲜的生肉，营养丰富但有风险"
            };
            rawMeat.ImmediateEffects["Satiety"] = 30f;
            rawMeat.SustainedEffects["HealthChangePerDay"] = (ModifierType.Add, -5f, 2);
            rawMeat.SustainedEffects["SatietyChangePerDay"] = (ModifierType.Add, 5f, 1);
            _foodTemplates.Add(rawMeat);

            // 变质面包 (加法)
            var staleBread = new Food
            {
                Name = "变质面包",
                Description = "硬邦邦的面包，有点发霉"
            };
            staleBread.ImmediateEffects["Satiety"] = 10f;
            staleBread.ImmediateEffects["Sanity"] = -5f;
            staleBread.SustainedEffects["HealthChangePerDay"] = (ModifierType.Add, -2f, 4);
            _foodTemplates.Add(staleBread);

            // 能量饮料 (加法)
            var energyDrink = new Food
            {
                Name = "能量饮料",
                Description = "提神醒脑，但会影响睡眠"
            };
            energyDrink.ImmediateEffects["Sanity"] = 15f;
            energyDrink.SustainedEffects["SanityChangePerDay"] = (ModifierType.Add, -8f, 2);
            energyDrink.SustainedEffects["HealthChangePerDay"] = (ModifierType.Add, -1f, 1);
            _foodTemplates.Add(energyDrink);

            // 巧克力 (加法)
            var chocolate = new Food
            {
                Name = "巧克力",
                Description = "甜蜜的安慰，但会让人上瘾"
            };
            chocolate.ImmediateEffects["Sanity"] = 20f;
            chocolate.ImmediateEffects["Satiety"] = 5f;
            chocolate.SustainedEffects["SanityChangePerDay"] = (ModifierType.Add, -3f, 3);
            _foodTemplates.Add(chocolate);

            // 水 (加法)
            var water = new Food
            {
                Name = "清水",
                Description = "纯净的水，基本生存需求"
            };
            water.ImmediateEffects["Satiety"] = 5f;
            water.SustainedEffects["HealthChangePerDay"] = (ModifierType.Add, 1f, 1);
            _foodTemplates.Add(water);

            // 野果 (加法)
            var wildBerry = new Food
            {
                Name = "野果",
                Description = "从树上摘的野果，可能有惊喜"
            };
            wildBerry.ImmediateEffects["Satiety"] = 15f;
            wildBerry.ImmediateEffects["Sanity"] = Random.Range(-10f, 10f);  // 随机效果
            wildBerry.SustainedEffects["HealthChangePerDay"] = (ModifierType.Add, Random.Range(-3f, 3f), 2);
            _foodTemplates.Add(wildBerry);

            // 维生素片 (乘法 - 增强生命变化)
            var vitamin = new Food
            {
                Name = "维生素片",
                Description = "能够增强身体恢复能力的维生素"
            };
            vitamin.ImmediateEffects["Satiety"] = 8f;
            vitamin.ImmediateEffects["Health"] = 5f;
            vitamin.SustainedEffects["HealthChangePerDay"] = (ModifierType.Mul, 1.2f, 2);  // 生命恢复×1.2倍
            _foodTemplates.Add(vitamin);

            // 强心针 (乘法 - 加强生命恢复)
            var heartTonic = new Food
            {
                Name = "强心针",
                Description = "能够强化心脏功能，加强生命力的针剂"
            };
            heartTonic.ImmediateEffects["Health"] = 10f;
            heartTonic.ImmediateEffects["Sanity"] = -8f;
            heartTonic.SustainedEffects["HealthChangePerDay"] = (ModifierType.Mul, 1.5f, 3);  // 生命恢复×1.5倍
            _foodTemplates.Add(heartTonic);

            // 麻痹毒药 (除法 - 减弱饱食度消耗)
            var paralysisToxin = new Food
            {
                Name = "麻痹毒药",
                Description = "麻痹神经，减缓新陈代谢"
            };
            paralysisToxin.ImmediateEffects["Satiety"] = 12f;
            paralysisToxin.ImmediateEffects["Sanity"] = -15f;
            paralysisToxin.SustainedEffects["SatietyChangePerDay"] = (ModifierType.Mul, 0.5f, 2);  // 饱食度消耗÷2
            _foodTemplates.Add(paralysisToxin);

            // 麻醉药 (除法 - 减弱所有属性变化)
            var anesthetic = new Food
            {
                Name = "麻醉药",
                Description = "全身麻醉，暂停所有生理变化"
            };
            anesthetic.ImmediateEffects["Health"] = -10f;
            anesthetic.ImmediateEffects["Sanity"] = -20f;
            anesthetic.SustainedEffects["SatietyChangePerDay"] = (ModifierType.Mul, 0.3f, 1);  // 饱食度消耗÷3
            anesthetic.SustainedEffects["HealthChangePerDay"] = (ModifierType.Mul, 0.2f, 1);  // 生命变化÷5
            _foodTemplates.Add(anesthetic);

            // 稳定药物 (覆盖 - 固定饱食度变化)
            var stableMedicine = new Food
            {
                Name = "稳定药物",
                Description = "能够稳定身体状态，保持饱食度平衡"
            };
            stableMedicine.ImmediateEffects["Satiety"] = 25f;
            stableMedicine.ImmediateEffects["Health"] = 5f;
            stableMedicine.SustainedEffects["SatietyChangePerDay"] = (ModifierType.Override, 0f, 2);  // 覆盖为0，不消耗饱食度
            _foodTemplates.Add(stableMedicine);

            // 精神焦点 (覆盖 - 固定SAN值变化)
            var mentalFocus = new Food
            {
                Name = "精神焦点",
                Description = "能够集中精神，精神状态保持稳定"
            };
            mentalFocus.ImmediateEffects["Sanity"] = 30f;
            mentalFocus.ImmediateEffects["Satiety"] = -5f;
            mentalFocus.SustainedEffects["SanityChangePerDay"] = (ModifierType.Override, 0f, 2);  // 覆盖为0，SAN值不变化
            _foodTemplates.Add(mentalFocus);
        }

        /// <summary>
        /// 生成4种随机食物
        /// </summary>
        public List<Food> GenerateDailyFoods()
        {
            var selectedFoods = new List<Food>();
            var availableFoods = new List<Food>(_foodTemplates);

            for (int i = 0; i < 4 && availableFoods.Count > 0; i++)
            {
                int randomIndex = Random.Range(0, availableFoods.Count);
                selectedFoods.Add(availableFoods[randomIndex]);
                availableFoods.RemoveAt(randomIndex);
            }

            return selectedFoods;
        }

        /// <summary>
        /// 获取食物效果描述
        /// </summary>
        public string GetFoodEffectDescription(Food food)
        {
            var desc = $"{food.Name}\n{food.Description}\n\n即时效果:\n";

            foreach (var effect in food.ImmediateEffects)
            {
                string attrName = GetAttributeDisplayName(effect.Key);
                string sign = effect.Value >= 0 ? "+" : "";
                desc += $"{attrName}: {sign}{effect.Value}\n";
            }

            if (food.SustainedEffects.Count > 0)
            {
                desc += "\n持续效果:\n";
                foreach (var effect in food.SustainedEffects)
                {
                    string attrName = GetAttributeDisplayName(effect.Key);
                    string effectStr = GetModifierEffectDescription(effect.Value.type, effect.Value.value);
                    desc += $"{attrName}: {effectStr} (持续{effect.Value.duration}天)\n";
                }
            }

            return desc;
        }

        private string GetModifierEffectDescription(ModifierType type, float value)
        {
            switch (type)
            {
                case ModifierType.Add:
                    string sign = value >= 0 ? "+" : "";
                    return $"{sign}{value}";
                case ModifierType.Mul:
                    return $"×{value:F1}倍";
                case ModifierType.Override:
                    return $"覆盖为{value:F1}";
                case ModifierType.PriorityAdd:
                    return $"优先+{value}";
                case ModifierType.PriorityMul:
                    return $"优先×{value:F1}倍";
                case ModifierType.AfterAdd:
                    return $"后加+{value}";
                default:
                    return value.ToString("F1");
            }
        }

        private string GetAttributeDisplayName(string attrKey)
        {
            switch (attrKey)
            {
                case "Satiety": return "饱食度";
                case "Health": return "生命值";
                case "Sanity": return "SAN值";
                case "SatietyChangePerDay": return "每日饱食度变化";
                case "HealthChangePerDay": return "每日生命值变化";
                case "SanityChangePerDay": return "每日SAN变化";
                default: return attrKey;
            }
        }
    }
}