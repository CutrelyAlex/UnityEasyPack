using System;
using System.Collections.Generic;
using UnityEngine;
using EasyPack.CategoryService;
using EasyPack.CustomData;

namespace EasyPack.CategoryService.Examples
{
    /// <summary>
    /// 游戏属性示例
    /// 展示 CategoryService 如何管理游戏属性的分类和查询
    /// </summary>
    public class GamePropertyCategoryExample : MonoBehaviour
    {
        /// <summary>
        /// 游戏属性定义
        /// </summary>
        [System.Serializable]
        public class GameProperty
        {
            public string Id;
            public string Name;
            public string Description;
            public float BaseValue;

            public GameProperty(string id, string name, string description, float baseValue)
            {
                Id = id;
                Name = name;
                Description = description;
                BaseValue = baseValue;
            }
        }

        private CategoryService<GameProperty> _categoryService;

        private void Start()
        {
            // 初始化分类服务
            InitializeCategoryService();

            // 运行示例
            RunBasicRegistrationExample();
            RunHierarchicalCategoryExample();
            RunWildcardQueryExample();
            RunTagSystemExample();
            RunMetadataExample();
            RunBatchRegistrationExample();
        }

        /// <summary>
        /// 初始化分类服务
        /// </summary>
        private void InitializeCategoryService()
        {
            // 创建服务，指定 ID 提取器和缓存策略
            _categoryService = new CategoryService<GameProperty>(
                property => property.Id,
                StringComparison.OrdinalIgnoreCase,
                CacheStrategy.Balanced
            );

            Debug.Log("[CategoryService] 分类服务已初始化");
        }

        /// <summary>
        /// 示例 1：基础注册和查询（US1）
        /// </summary>
        private void RunBasicRegistrationExample()
        {
            Debug.Log("\n========== 示例 1: 基础注册和查询 ==========");

            // 创建游戏属性
            var healthProp = new GameProperty("prop_health", "生命值", "角色生命值属性", 100f);
            var manaProperty = new GameProperty("prop_mana", "魔法值", "角色魔法值属性", 50f);
            var attackProperty = new GameProperty("prop_attack", "攻击力", "角色攻击力属性", 10f);

            // 注册到分类
            var result1 = _categoryService.RegisterEntity(healthProp, "Character.Vitality")
                .WithMetadata(new List<CustomDataEntry>
                {
                    new CustomDataEntry { Id = "type", Type = CustomDataType.String, StringValue = "vital" }
                })
                .Complete();

            var result2 = _categoryService.RegisterEntity(manaProperty, "Character.Vitality")
                .Complete();

            var result3 = _categoryService.RegisterEntity(attackProperty, "Character.Combat")
                .Complete();

            Debug.Log($"[US1] 注册生命值: {(result1.IsSuccess ? "成功" : "失败")}");
            Debug.Log($"[US1] 注册魔法值: {(result2.IsSuccess ? "成功" : "失败")}");
            Debug.Log($"[US1] 注册攻击力: {(result3.IsSuccess ? "成功" : "失败")}");

            // 查询分类
            var vitalityProps = _categoryService.GetByCategory("Character.Vitality");
            Debug.Log($"[US1] Character.Vitality 分类中有 {vitalityProps.Count} 个属性");
            foreach (var prop in vitalityProps)
            {
                Debug.Log($"      - {prop.Name} (ID: {prop.Id})");
            }

            // 按 ID 查询
            var getResult = _categoryService.GetById("prop_health");
            if (getResult.IsSuccess)
            {
                Debug.Log($"[US1] 获取 prop_health: {getResult.Value.Name}");
            }

            // 获取所有分类
            var allCategories = _categoryService.GetAllCategories();
            Debug.Log($"[US1] 总共有 {allCategories.Count} 个分类");
        }

        /// <summary>
        /// 示例 2：层级分类和递归查询（US2）
        /// </summary>
        private void RunHierarchicalCategoryExample()
        {
            Debug.Log("\n========== 示例 2: 层级分类和递归查询 ==========");

            // 创建多层级属性
            var physicalDmg = new GameProperty("dmg_physical", "物理伤害", "物理伤害属性", 15f);
            var magicDmg = new GameProperty("dmg_magic", "魔法伤害", "魔法伤害属性", 20f);
            var fireDmg = new GameProperty("dmg_fire", "火焰伤害", "火焰伤害属性", 18f);

            // 注册到多层级分类
            _categoryService.RegisterEntity(physicalDmg, "Damage.Physical").Complete();
            _categoryService.RegisterEntity(magicDmg, "Damage.Magical").Complete();
            _categoryService.RegisterEntity(fireDmg, "Damage.Magical.Fire").Complete();

            // 精确查询
            var physicalDmgs = _categoryService.GetByCategory("Damage.Physical");
            Debug.Log($"[US2] Damage.Physical 分类有 {physicalDmgs.Count} 个属性");

            // 递归查询（包含子分类）
            var allDamages = _categoryService.GetByCategory("Damage", includeChildren: true);
            Debug.Log($"[US2] Damage 及其子分类共有 {allDamages.Count} 个属性");
            foreach (var prop in allDamages)
            {
                Debug.Log($"      - {prop.Name}");
            }
        }

        /// <summary>
        /// 示例 3：通配符查询（US2）
        /// </summary>
        private void RunWildcardQueryExample()
        {
            Debug.Log("\n========== 示例 3: 通配符查询 ==========");

            // 创建更多属性来展示通配符
            var armorPhysical = new GameProperty("armor_physical", "物理护甲", "减少物理伤害", 5f);
            var armorMagic = new GameProperty("armor_magic", "魔法护甲", "减少魔法伤害", 3f);

            _categoryService.RegisterEntity(armorPhysical, "Defense.Armor.Physical").Complete();
            _categoryService.RegisterEntity(armorMagic, "Defense.Armor.Magical").Complete();

            // 使用通配符查询（Defense.Armor.* 匹配所有护甲）
            var allArmors = _categoryService.GetByCategory("Defense.Armor.*");
            Debug.Log($"[US2] 使用通配符 'Defense.Armor.*' 查询: {allArmors.Count} 个属性");
            foreach (var prop in allArmors)
            {
                Debug.Log($"      - {prop.Name}");
            }

            // 使用通配符查询所有 Defense 属性
            var allDefenses = _categoryService.GetByCategory("Defense.*", includeChildren: true);
            Debug.Log($"[US2] 使用通配符 'Defense.*' 查询: {allDefenses.Count} 个属性");
        }

        /// <summary>
        /// 示例 4：标签系统（US3）
        /// </summary>
        private void RunTagSystemExample()
        {
            Debug.Log("\n========== 示例 4: 标签系统 ==========");

            // 创建带标签的属性
            var hpProp = new GameProperty("status_hp", "血量", "生命值", 100f);
            var mpProp = new GameProperty("status_mp", "法力", "魔法值", 50f);
            var speedProp = new GameProperty("status_speed", "速度", "移动速度", 5f);

            // 注册并添加标签
            _categoryService.RegisterEntity(hpProp, "Status")
                .WithTags("important", "vital", "increasable")
                .Complete();

            _categoryService.RegisterEntity(mpProp, "Status")
                .WithTags("important", "magic", "increasable")
                .Complete();

            _categoryService.RegisterEntity(speedProp, "Status")
                .WithTags("movement", "increasable")
                .Complete();

            // 按标签查询
            var importantProps = _categoryService.GetByTag("important");
            Debug.Log($"[US3] 标签 'important' 有 {importantProps.Count} 个属性");
            foreach (var prop in importantProps)
            {
                Debug.Log($"      - {prop.Name}");
            }

            var increasableProps = _categoryService.GetByTag("increasable");
            Debug.Log($"[US3] 标签 'increasable' 有 {increasableProps.Count} 个属性");

            // 组合查询：分类 + 标签
            var importantStatusProps = _categoryService.GetByCategoryAndTag("Status", "important");
            Debug.Log($"[US3] Status 分类中标签为 'important' 的有 {importantStatusProps.Count} 个");
            foreach (var prop in importantStatusProps)
            {
                Debug.Log($"      - {prop.Name}");
            }
        }

        /// <summary>
        /// 示例 5：元数据管理（US4）
        /// </summary>
        private void RunMetadataExample()
        {
            Debug.Log("\n========== 示例 5: 元数据管理 ==========");

            // 创建属性并添加丰富的元数据
            var specialProp = new GameProperty("special_crit", "暴击率", "造成暴击伤害的概率", 20f);

            var metadata = new List<CustomDataEntry>
            {
                new CustomDataEntry { Id = "author", Type = CustomDataType.String, StringValue = "GameDesigner01" },
                new CustomDataEntry { Id = "version", Type = CustomDataType.String, StringValue = "1.0" },
                new CustomDataEntry { Id = "lastModified", Type = CustomDataType.String, StringValue = "2025-11-20" },
                new CustomDataEntry { Id = "category", Type = CustomDataType.String, StringValue = "Combat" }
            };

            _categoryService.RegisterEntity(specialProp, "Ability.Special")
                .WithMetadata(metadata)
                .Complete();

            // 获取元数据
            var metadataResult = _categoryService.GetMetadata("special_crit");
            if (metadataResult.IsSuccess)
            {
                Debug.Log($"[US4] special_crit 的元数据:");
                foreach (var entry in metadataResult.Value)
                {
                    Debug.Log($"      {entry.Id}: {entry.GetValue()}");
                }
            }

            // 更新元数据
            metadata.Add(new CustomDataEntry { Id = "deprecated", Type = CustomDataType.String, StringValue = "false" });
            var updateResult = _categoryService.UpdateMetadata("special_crit", metadata);
            Debug.Log($"[US4] 更新元数据: {(updateResult.IsSuccess ? "成功" : "失败")}");
        }

        /// <summary>
        /// 示例 6：批量注册（US1 扩展）
        /// </summary>
        private void RunBatchRegistrationExample()
        {
            Debug.Log("\n========== 示例 6: 批量注册 ==========");

            // 创建一批属性
            var batchProps = new List<GameProperty>
            {
                new GameProperty("buff_str", "力量加成", "增加力量", 5f),
                new GameProperty("buff_dex", "敏捷加成", "增加敏捷", 3f),
                new GameProperty("buff_int", "智力加成", "增加智力", 4f),
                new GameProperty("buff_vit", "体质加成", "增加体质", 2f),
                new GameProperty("buff_luk", "幸运加成", "增加幸运", 1f)
            };

            // 批量注册
            var batchResult = _categoryService.RegisterBatch(batchProps, "Buff.Enhancement");

            Debug.Log($"[US1] 批量注册结果:");
            Debug.Log($"      总数: {batchResult.TotalCount}");
            Debug.Log($"      成功: {batchResult.SuccessCount}");
            Debug.Log($"      失败: {batchResult.FailureCount}");
            Debug.Log($"      全部成功: {batchResult.IsFullSuccess}");

            // 验证批量注册结果
            var buffProps = _categoryService.GetByCategory("Buff.Enhancement");
            Debug.Log($"[US1] Buff.Enhancement 分类现有 {buffProps.Count} 个属性");
        }

        private void OnDestroy()
        {
            // 清理资源
            _categoryService?.Dispose();
            Debug.Log("[CategoryService] 分类服务已释放");
        }
    }
}
