using EasyPack.CustomData;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyPack.CategoryService.Examples
{
    /// <summary>
    /// Buff 系统示例
    /// 展示如何使用 CategoryService 管理游戏中的 Buff/Debuff 系统
    /// 演示标签系统和元数据的实际应用场景
    /// </summary>
    public class BuffCategoryExample : MonoBehaviour
    {
        /// <summary>
        /// Buff 定义
        /// </summary>
        [System.Serializable]
        public class BuffData
        {
            public string Id;
            public string Name;
            public string Description;
            public float Duration;
            public float Power;

            public BuffData(string id, string name, string description, float duration, float power)
            {
                Id = id;
                Name = name;
                Description = description;
                Duration = duration;
                Power = power;
            }
        }

        private CategoryService<BuffData> _buffService;

        private void Start()
        {
            InitializeBuffService();
            RunBuffSystemExample();
            RunDebuffSystemExample();
            RunBuffQueryExample();
            RunRemovalExample();
        }

        /// <summary>
        /// 初始化 Buff 服务
        /// </summary>
        private void InitializeBuffService()
        {
            _buffService = new CategoryService<BuffData>(
                buff => buff.Id,
                StringComparison.OrdinalIgnoreCase,
                CacheStrategy.Balanced
            );

            Debug.Log("[BuffService] Buff 系统已初始化");
        }

        /// <summary>
        /// 示例 1：Buff 系统（增益效果）
        /// </summary>
        private void RunBuffSystemExample()
        {
            Debug.Log("\n========== Buff 系统示例 ==========");

            // 创建不同类型的 Buff
            var strengthBuff = new BuffData("buff_str_up", "力量提升", "增加攻击力 20%", 30f, 0.2f);
            var speedBuff = new BuffData("buff_speed_up", "加速", "移动速度 +50%", 20f, 0.5f);
            var regenerationBuff = new BuffData("buff_regen", "生命再生", "每秒恢复 10 血量", 60f, 10f);
            var invulnBuff = new BuffData("buff_invuln", "无敌", "无敌状态", 5f, 1f);

            // 注册到分类并添加标签
            _buffService.RegisterEntity(strengthBuff, "Buff.Stat")
                .WithTags("beneficial", "attack", "temporary", "stackable")
                .WithMetadata(new List<CustomDataEntry>
                {
                    new CustomDataEntry { Id = "priority", Type = CustomDataType.String, StringValue = "1" },
                    new CustomDataEntry { Id = "stackLimit", Type = CustomDataType.String, StringValue = "3" },
                    new CustomDataEntry { Id = "source", Type = CustomDataType.String, StringValue = "equipment" }
                })
                .Complete();

            _buffService.RegisterEntity(speedBuff, "Buff.Movement")
                .WithTags("beneficial", "movement", "temporary")
                .WithMetadata(new List<CustomDataEntry>
                {
                    new CustomDataEntry { Id = "priority", Type = CustomDataType.String, StringValue = "2" },
                    new CustomDataEntry { Id = "stackLimit", Type = CustomDataType.String, StringValue = "1" },
                    new CustomDataEntry { Id = "source", Type = CustomDataType.String, StringValue = "skill" }
                })
                .Complete();

            _buffService.RegisterEntity(regenerationBuff, "Buff.Recovery")
                .WithTags("beneficial", "healing", "persistent")
                .WithMetadata(new List<CustomDataEntry>
                {
                    new CustomDataEntry { Id = "tickRate", Type = CustomDataType.String, StringValue = "1" },
                    new CustomDataEntry { Id = "source", Type = CustomDataType.String, StringValue = "passive" }
                })
                .Complete();

            _buffService.RegisterEntity(invulnBuff, "Buff.Defense")
                .WithTags("beneficial", "defense", "temporary", "exclusive")
                .WithMetadata(new List<CustomDataEntry>
                {
                    new CustomDataEntry { Id = "priority", Type = CustomDataType.String, StringValue = "10" },
                    new CustomDataEntry { Id = "overridable", Type = CustomDataType.String, StringValue = "false" }
                })
                .Complete();

            // 查询所有 Buff
            var allBuffs = _buffService.GetByCategory("Buff", includeChildren: true);
            Debug.Log($"[Buff] 总共注册了 {allBuffs.Count} 个 Buff:");
            foreach (var buff in allBuffs)
            {
                Debug.Log($"      ✓ {buff.Name} (持续时间: {buff.Duration}s)");
            }

            // 查询特定类别的 Buff
            var statBuffs = _buffService.GetByCategory("Buff.Stat");
            Debug.Log($"[Buff.Stat] 有 {statBuffs.Count} 个属性相关的 Buff");

            var defenseBuffs = _buffService.GetByCategory("Buff.Defense");
            Debug.Log($"[Buff.Defense] 有 {defenseBuffs.Count} 个防御相关的 Buff");
        }

        /// <summary>
        /// 示例 2：Debuff 系统（负面效果）
        /// </summary>
        private void RunDebuffSystemExample()
        {
            Debug.Log("\n========== Debuff 系统示例 ==========");

            // 创建 Debuff
            var poisonDebuff = new BuffData("debuff_poison", "中毒", "每秒损失 5 血量", 15f, 5f);
            var weakDebuff = new BuffData("debuff_weak", "虚弱", "攻击力 -30%", 20f, 0.3f);
            var stunDebuff = new BuffData("debuff_stun", "眩晕", "无法行动", 3f, 1f);
            var bleedDebuff = new BuffData("debuff_bleed", "流血", "每次受击增加伤害", 10f, 1.2f);

            // 注册 Debuff
            _buffService.RegisterEntity(poisonDebuff, "Debuff.Damage.Poison")
                .WithTags("harmful", "damage_over_time", "removable", "stackable")
                .WithMetadata(new List<CustomDataEntry>
                {
                    new CustomDataEntry { Id = "resisType", Type = CustomDataType.String, StringValue = "poison" },
                    new CustomDataEntry { Id = "source", Type = CustomDataType.String, StringValue = "enemy" }
                })
                .Complete();

            _buffService.RegisterEntity(weakDebuff, "Debuff.Stat")
                .WithTags("harmful", "stat_reduction", "temporary")
                .WithMetadata(new List<CustomDataEntry>
                {
                    new CustomDataEntry { Id = "affectedStat", Type = CustomDataType.String, StringValue = "attack" }
                })
                .Complete();

            _buffService.RegisterEntity(stunDebuff, "Debuff.Control")
                .WithTags("harmful", "crowd_control", "temporary", "priority")
                .WithMetadata(new List<CustomDataEntry>
                {
                    new CustomDataEntry { Id = "priority", Type = CustomDataType.String, StringValue = "5" },
                    new CustomDataEntry { Id = "overridable", Type = CustomDataType.String, StringValue = "true" }
                })
                .Complete();

            _buffService.RegisterEntity(bleedDebuff, "Debuff.Damage.Physical")
                .WithTags("harmful", "damage_stacking", "temporary")
                .WithMetadata(new List<CustomDataEntry>
                {
                    new CustomDataEntry { Id = "stackType", Type = CustomDataType.String, StringValue = "damage" }
                })
                .Complete();

            // 查询所有 Debuff
            var allDebuffs = _buffService.GetByCategory("Debuff", includeChildren: true);
            Debug.Log($"[Debuff] 总共注册了 {allDebuffs.Count} 个 Debuff:");
            foreach (var debuff in allDebuffs)
            {
                Debug.Log($"      ✗ {debuff.Name} (持续时间: {debuff.Duration}s)");
            }

            // 查询控制类 Debuff
            var ccDebuffs = _buffService.GetByTag("crowd_control");
            Debug.Log($"[Debuff] 控制类 Debuff 有 {ccDebuffs.Count} 个");

            // 查询持续伤害 Debuff
            var dotDebuffs = _buffService.GetByTag("damage_over_time");
            Debug.Log($"[Debuff] 持续伤害 Debuff 有 {dotDebuffs.Count} 个");
        }

        /// <summary>
        /// 示例 3：高级查询示例
        /// </summary>
        private void RunBuffQueryExample()
        {
            Debug.Log("\n========== 高级查询示例 ==========");

            // 查询所有可移除的效果
            var removableEffects = _buffService.GetByTag("removable");
            Debug.Log($"[查询] 可移除的效果: {removableEffects.Count} 个");

            // 查询所有临时效果
            var temporaryEffects = _buffService.GetByTag("temporary");
            Debug.Log($"[查询] 临时效果: {temporaryEffects.Count} 个");

            // 查询伤害类 Buff 中的毒素伤害
            var poisonDamages = _buffService.GetByCategory("Debuff.Damage.Poison", includeChildren: true);
            Debug.Log($"[查询] 毒素伤害类效果: {poisonDamages.Count} 个");

            // 查询所有物理伤害相关的效果
            var physicalDamages = _buffService.GetByCategory("Debuff.Damage.Physical*");
            Debug.Log($"[查询] 物理伤害相关: {physicalDamages.Count} 个");

            // 组合查询：有害 + 可堆叠
            var harmfulStackable = _buffService.GetByCategoryAndTag("Debuff", "stackable");
            Debug.Log($"[查询] 有害且可堆叠的效果: {harmfulStackable.Count} 个");
            foreach (var effect in harmfulStackable)
            {
                Debug.Log($"      - {effect.Name}");
            }
        }

        /// <summary>
        /// 示例 4：移除和删除示例
        /// </summary>
        private void RunRemovalExample()
        {
            Debug.Log("\n========== 删除和移除示例 ==========");

            // 获取一个 Buff 的元数据来检查属性
            var poisonResult = _buffService.GetById("debuff_poison");
            if (poisonResult.IsSuccess)
            {
                var poison = poisonResult.Value;
                var metadataResult = _buffService.GetMetadata("debuff_poison");

                if (metadataResult.IsSuccess)
                {
                    Debug.Log($"[移除前] {poison.Name} 的元数据:");
                    foreach (var entry in metadataResult.Value)
                    {
                        Debug.Log($"        {entry.Id}: {entry.GetValue()}");
                    }
                }
            }

            // 删除一个具体的 Buff
            var deleteResult = _buffService.DeleteEntity("debuff_poison");
            Debug.Log($"[删除] 删除 debuff_poison: {(deleteResult.IsSuccess ? "成功" : "失败")}");

            // 验证删除
            var remaining = _buffService.GetByCategory("Debuff", includeChildren: true);
            Debug.Log($"[删除后] Debuff 总数: {remaining.Count} 个");

            // 删除整个分类（包含子分类）
            var deleteCatResult = _buffService.DeleteCategoryRecursive("Debuff.Damage");
            Debug.Log($"[删除] 删除 Debuff.Damage 分类树: {(deleteCatResult.IsSuccess ? "成功" : "失败")}");

            // 最终统计
            var finalCount = _buffService.GetByCategory("Debuff", includeChildren: true);
            Debug.Log($"[最终] 剩余 Debuff 数量: {finalCount.Count} 个");

            // 获取所有分类
            var allCategories = _buffService.GetAllCategories();
            Debug.Log($"[最终] 剩余分类数量: {allCategories.Count} 个");
        }

        private void OnDestroy()
        {
            _buffService?.Dispose();
            Debug.Log("[BuffService] Buff 系统已释放");
        }
    }
}
