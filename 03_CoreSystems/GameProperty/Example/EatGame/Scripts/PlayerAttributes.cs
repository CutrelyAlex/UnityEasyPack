using EasyPack.GamePropertySystem;
using UnityEngine;
using System.Collections.Generic;

namespace EasyPack.GamePropertySystem.Example.EatGame
{
    /// <summary>
    /// 玩家属性管理器
    /// </summary>
    public class PlayerAttributes
    {
        // 一级属性
        public GameProperty Satiety { get; private set; }      // 饱食度
        public GameProperty Health { get; private set; }       // 生命值
        public GameProperty Sanity { get; private set; }       // SAN值

        // 二级属性
        public GameProperty SatietyChangePerDay { get; private set; }    // 每日饱食度变化
        public GameProperty HealthChangePerDay { get; private set; }     // 每日生命值变化
        public GameProperty SanityChangePerDay { get; private set; }     // 每日SAN变化
        public GameProperty SatietyCapacity { get; private set; }        // 饱食度承受能力
        public GameProperty HealthCapacity { get; private set; }         // 生命值承受能力
        public GameProperty SanityCapacity { get; private set; }         // SAN承受能力

        // 修饰符管理器
        public ModifierManager ModifierManager { get; private set; }

        public PlayerAttributes()
        {
            ModifierManager = new ModifierManager();
            InitializeProperties();
            SetupDependencies();
        }

        private void InitializeProperties()
        {
            // 初始化一级属性
            Satiety = new GameProperty("Satiety", 50f);
            Health = new GameProperty("Health", 100f);
            Sanity = new GameProperty("Sanity", 100f);

            // 初始化二级属性
            SatietyChangePerDay = new GameProperty("SatietyChangePerDay", -10f);  // 每天减少10饱食度
            HealthChangePerDay = new GameProperty("HealthChangePerDay", 0f);
            SanityChangePerDay = new GameProperty("SanityChangePerDay", 0f);
            SatietyCapacity = new GameProperty("SatietyCapacity", 100f);
            HealthCapacity = new GameProperty("HealthCapacity", 100f);
            SanityCapacity = new GameProperty("SanityCapacity", 100f);
        }

        private void SetupDependencies()
        {
            // 每日生命值变化依赖于饱食度和SAN值状态
            HealthChangePerDay.AddDependency(Satiety, (dep, newVal) =>
            {
                float baseChange = HealthChangePerDay.GetBaseValue();
                if (newVal < 20) baseChange -= 5;  // 饱食度过低惩罚
                if (newVal > 80) baseChange -= 1;  // 饱食度过高惩罚
                return baseChange;
            });

            HealthChangePerDay.AddDependency(Sanity, (dep, newVal) =>
            {
                float baseChange = HealthChangePerDay.GetValue();
                if (newVal < 20) baseChange -= 2;  // SAN值过低惩罚
                if (newVal > 90) baseChange -= 1;  // SAN值过高惩罚
                return baseChange;
            });

            // 每日SAN变化依赖于饱食度和SAN值状态
            SanityChangePerDay.AddDependency(Satiety, (dep, newVal) =>
            {
                float baseChange = SanityChangePerDay.GetBaseValue();
                if (newVal > 80) baseChange -= 3;  // 饱食度过高惩罚
                return baseChange;
            });

            SanityChangePerDay.AddDependency(Sanity, (dep, newVal) =>
            {
                float baseChange = SanityChangePerDay.GetValue();
                if (newVal < 20) baseChange -= 2;  // SAN值过低恶性循环
                return baseChange;
            });
        }

        /// <summary>
        /// 执行每日属性结算
        /// </summary>
        public void ProcessDailyChanges()
        {
            Debug.Log("处理修饰符持续时间...");
            ModifierManager.ProcessDailyModifiers();

            Debug.Log($"应用每日变化 - 饱食度: {Satiety.GetValue():F1} + ({SatietyChangePerDay.GetValue():+0.0;-0.0;0}) = {Mathf.Clamp(Satiety.GetValue() + SatietyChangePerDay.GetValue(), 0, SatietyCapacity.GetValue()):F1}");
            Debug.Log($"应用每日变化 - 生命值: {Health.GetValue():F1} + ({HealthChangePerDay.GetValue():+0.0;-0.0;0}) = {Mathf.Clamp(Health.GetValue() + HealthChangePerDay.GetValue(), 0, HealthCapacity.GetValue()):F1}");
            Debug.Log($"应用每日变化 - SAN值: {Sanity.GetValue():F1} + ({SanityChangePerDay.GetValue():+0.0;-0.0;0}) = {Mathf.Clamp(Sanity.GetValue() + SanityChangePerDay.GetValue(), 0, SanityCapacity.GetValue()):F1}");

            // 应用每日变化
            Satiety.SetBaseValue(Mathf.Clamp(Satiety.GetValue() + SatietyChangePerDay.GetValue(), 0, SatietyCapacity.GetValue()));
            Health.SetBaseValue(Mathf.Clamp(Health.GetValue() + HealthChangePerDay.GetValue(), 0, HealthCapacity.GetValue()));
            Sanity.SetBaseValue(Mathf.Clamp(Sanity.GetValue() + SanityChangePerDay.GetValue(), 0, SanityCapacity.GetValue()));
        }

        /// <summary>
        /// 检查游戏结束条件
        /// </summary>
        public bool IsGameOver()
        {
            return Health.GetValue() <= 0 || Satiety.GetValue() <= 0 || Sanity.GetValue() <= 0;
        }

        /// <summary>
        /// 获取属性状态描述
        /// </summary>
        public string GetStatusDescription()
        {
            string status = $"饱食度: {Satiety.GetValue():F1}/{SatietyCapacity.GetValue()} ({SatietyChangePerDay.GetValue():+0.0;-0.0;0}/天)\n" +
                           $"生命值: {Health.GetValue():F1}/{HealthCapacity.GetValue()} ({HealthChangePerDay.GetValue():+0.0;-0.0;0}/天)\n" +
                           $"SAN值: {Sanity.GetValue():F1}/{SanityCapacity.GetValue()} ({SanityChangePerDay.GetValue():+0.0;-0.0;0}/天)";

            // 添加状态提示
            string statusEffects = GetStatusEffectsDescription();
            if (!string.IsNullOrEmpty(statusEffects))
            {
                status += "\n\n" + statusEffects;
            }

            return status;
        }

        /// <summary>
        /// 获取当前状态效果描述
        /// </summary>
        private string GetStatusEffectsDescription()
        {
            List<string> effects = new List<string>();

            // 检查饱食度相关状态
            float satiety = Satiety.GetValue();
            if (satiety < 20)
            {
                effects.Add("⚠️ 饱食度过低：生命值每日额外减少5点");
            }
            else if (satiety > 80)
            {
                effects.Add("⚠️ 饱食度过高：生命值每日额外减少1点");
            }

            // 检查SAN值相关状态
            float sanity = Sanity.GetValue();
            if (sanity < 20)
            {
                effects.Add("⚠️ SAN值过低：生命值每日额外减少2点");
                effects.Add("⚠️ SAN值过低：SAN值每日额外减少2点（恶性循环）");
            }
            else if (sanity > 90)
            {
                effects.Add("⚠️ SAN值过高：生命值每日额外减少1点");
            }

            if (satiety > 80)
            {
                effects.Add("⚠️ 饱食度过高：SAN值每日额外减少3点");
            }

            return effects.Count > 0 ? string.Join("\n", effects) : "";
        }
    }
}