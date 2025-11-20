using UnityEngine;
using EasyPack.CategoryService;
using EasyPack.CustomData;
using System.Collections.Generic;

namespace EasyPack.CategoryService.Examples
{
    /// <summary>
    /// Phase 5-9 功能验证测试
    /// 测试标签系统、元数据管理、语法糖操作、序列化和缓存
    /// </summary>
    public class Phase5To9Test : MonoBehaviour
    {
        [System.Serializable]
        public class TestEntity
        {
            public string Id;
            public string Name;
            public int Value;
        }

        private void Start()
        {
            Debug.Log("=== Phase 5-9 功能测试开始 ===");

            TestPhase5_TagSystem();
            TestPhase6_MetadataManagement();
            TestPhase7_SyntaxSugar();
            TestPhase8_Serialization();
            TestPhase9_CachingStrategies();

            Debug.Log("=== Phase 5-9 功能测试完成 ===");
        }

        /// <summary>
        /// Phase 5: 标签系统测试
        /// </summary>
        private void TestPhase5_TagSystem()
        {
            Debug.Log("\n--- Phase 5: 标签系统测试 ---");

            var manager = new CategoryManager<TestEntity>(e => e.Id);

            // 注册带标签的实体
            var entity1 = new TestEntity { Id = "e1", Name = "Sword", Value = 100 };
            var entity2 = new TestEntity { Id = "e2", Name = "Shield", Value = 80 };
            var entity3 = new TestEntity { Id = "e3", Name = "Potion", Value = 50 };

            manager.RegisterEntity(entity1, "Equipment.Weapon")
                .WithTags("melee", "rare")
                .Complete();

            manager.RegisterEntity(entity2, "Equipment.Armor")
                .WithTags("defense", "rare")
                .Complete();

            manager.RegisterEntity(entity3, "Consumable")
                .WithTags("healing", "common")
                .Complete();

            // 测试标签查询
            var rareItems = manager.GetByTag("rare");
            Debug.Log($"✓ 稀有物品数量: {rareItems.Count} (期望: 2)");
            Debug.Assert(rareItems.Count == 2, "标签查询失败");

            // 测试分类+标签组合查询
            var rareMelee = manager.GetByCategoryAndTag("Equipment.Weapon", "rare");
            Debug.Log($"✓ 稀有近战武器数量: {rareMelee.Count} (期望: 1)");
            Debug.Assert(rareMelee.Count == 1, "组合查询失败");

            Debug.Log("Phase 5 测试通过 ✓");
        }

        /// <summary>
        /// Phase 6: 元数据管理测试
        /// </summary>
        private static void TestPhase6_MetadataManagement()
        {
            Debug.Log("\n--- Phase 6: 元数据管理测试 ---");

            var manager = new CategoryManager<TestEntity>(e => e.Id);

            var entity = new TestEntity { Id = "e1", Name = "Legendary Sword", Value = 1000 };

            // 创建元数据
            var metadata = new CustomDataCollection();
            metadata.SetValue("author", "GameDesigner");
            metadata.SetValue("created", "2025-11-21");
            metadata.SetValue("durability", 100);
            metadata.SetValue("enchanted", true);

            manager.RegisterEntity(entity, "Equipment.Legendary")
                .WithMetadata(metadata)
                .Complete();

            // 获取元数据
            var result = manager.GetMetadata("e1");
            Debug.Assert(result.IsSuccess, "获取元数据失败");

            var retrievedMetadata = result.Value;
            Debug.Log($"✓ 元数据条目数: {retrievedMetadata.Count()} (期望: 4)");
            Debug.Log($"✓ Author: {retrievedMetadata.GetValue<string>("author")}");
            Debug.Log($"✓ Durability: {retrievedMetadata.GetValue<int>("durability")}");

            // 更新元数据
            retrievedMetadata.SetValue("durability", 95);
            var updateResult = manager.UpdateMetadata("e1", retrievedMetadata);
            Debug.Assert(updateResult.IsSuccess, "更新元数据失败");

            Debug.Log("Phase 6 测试通过 ✓");
        }

        /// <summary>
        /// Phase 7: 语法糖操作测试
        /// </summary>
        private void TestPhase7_SyntaxSugar()
        {
            Debug.Log("\n--- Phase 7: 语法糖操作测试 ---");

            var manager = new CategoryManager<TestEntity>(e => e.Id);

            var entity1 = new TestEntity { Id = "e1", Name = "Item1", Value = 10 };
            var entity2 = new TestEntity { Id = "e2", Name = "Item2", Value = 20 };

            manager.RegisterEntity(entity1, "OldCategory").Complete();
            manager.RegisterEntity(entity2, "OldCategory").Complete();

            // 测试移动实体
            var moveResult = manager.MoveEntityToCategory("e1", "NewCategory");
            Debug.Assert(moveResult.IsSuccess, "移动实体失败");

            var newCategoryItems = manager.GetByCategory("NewCategory");
            Debug.Log($"✓ 新分类实体数: {newCategoryItems.Count} (期望: 1)");
            Debug.Assert(newCategoryItems.Count == 1, "实体移动验证失败");

            // 测试重命名分类
            var renameResult = manager.RenameCategory("OldCategory", "RenamedCategory");
            Debug.Assert(renameResult.IsSuccess, "重命名分类失败");

            var renamedItems = manager.GetByCategory("RenamedCategory");
            Debug.Log($"✓ 重命名后分类实体数: {renamedItems.Count} (期望: 1)");
            Debug.Assert(renamedItems.Count == 1, "分类重命名验证失败");

            Debug.Log("Phase 7 测试通过 ✓");
        }

        /// <summary>
        /// Phase 8: 序列化测试
        /// </summary>
        private void TestPhase8_Serialization()
        {
            Debug.Log("\n--- Phase 8: 序列化测试 ---");

            var manager1 = new CategoryManager<TestEntity>(e => e.Id);

            // 注册复杂数据
            var entity1 = new TestEntity { Id = "e1", Name = "Sword", Value = 100 };
            var entity2 = new TestEntity { Id = "e2", Name = "Shield", Value = 80 };

            var metadata1 = new CustomDataCollection();
            metadata1.SetValue("level", 5);
            metadata1.SetValue("equipped", true);

            manager1.RegisterEntity(entity1, "Equipment.Weapon")
                .WithTags("melee", "rare")
                .WithMetadata(metadata1)
                .Complete();

            manager1.RegisterEntity(entity2, "Equipment.Armor")
                .WithTags("defense")
                .Complete();

            // 序列化
            string json = manager1.SerializeToJson();
            Debug.Log($"✓ 序列化成功，JSON 长度: {json.Length}");
            Debug.Assert(!string.IsNullOrEmpty(json), "序列化失败");

            // 从 JSON 创建新实例
            var manager2 = CategoryManager<TestEntity>.CreateFromJson(json, e => e.Id);
            Debug.Assert(manager2 != null, "反序列化失败");

            // 验证数据一致性
            var weapons = manager2.GetByCategory("Equipment.Weapon");
            Debug.Log($"✓ 反序列化后武器数量: {weapons.Count} (期望: 1)");
            Debug.Assert(weapons.Count == 1, "反序列化数据验证失败");

            var rareItems = manager2.GetByTag("rare");
            Debug.Log($"✓ 反序列化后稀有物品数量: {rareItems.Count} (期望: 1)");
            Debug.Assert(rareItems.Count == 1, "标签数据反序列化失败");

            var metadataResult = manager2.GetMetadata("e1");
            Debug.Assert(metadataResult.IsSuccess && metadataResult.Value.Count() > 0, "元数据反序列化失败");

            Debug.Log("Phase 8 测试通过 ✓");
        }

        /// <summary>
        /// Phase 9: 缓存策略测试
        /// </summary>
        private void TestPhase9_CachingStrategies()
        {
            Debug.Log("\n--- Phase 9: 缓存策略测试 ---");

            // 测试所有 4 种缓存策略
            TestCacheStrategy(CacheStrategy.Loose, "松散缓存");
            TestCacheStrategy(CacheStrategy.Balanced, "平衡缓存");
            TestCacheStrategy(CacheStrategy.Efficient, "高效缓存");
            TestCacheStrategy(CacheStrategy.Aggressive, "激进缓存");

            Debug.Log("Phase 9 测试通过 ✓");
        }

        private void TestCacheStrategy(CacheStrategy strategy, string strategyName)
        {
            var manager = new CategoryManager<TestEntity>(e => e.Id, cacheStrategy: strategy);

            // 注册测试数据
            for (int i = 0; i < 10; i++)
            {
                var entity = new TestEntity { Id = $"e{i}", Name = $"Item{i}", Value = i * 10 };
                manager.RegisterEntity(entity, "TestCategory").Complete();
            }

            // 执行查询（应该被缓存）
            var result1 = manager.GetByCategory("TestCategory");
            var result2 = manager.GetByCategory("TestCategory");

            Debug.Log($"✓ {strategyName} 测试完成 - 查询结果数: {result1.Count}");
            Debug.Assert(result1.Count == 10, $"{strategyName} 查询失败");
        }
    }
}
