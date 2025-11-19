using EasyPack.CustomData;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyPack.CategoryService.Examples
{
    /// <summary>
    /// CategoryService MVP 快速测试脚本
    /// 用于验证所有核心功能的可用性
    /// </summary>
    public class CategoryServiceMVPTest : MonoBehaviour
    {
        [System.Serializable]
        public class TestEntity
        {
            public string Id;
            public string Name;

            public TestEntity(string id, string name)
            {
                Id = id;
                Name = name;
            }
        }

        private int _passedTests = 0;
        private int _failedTests = 0;

        private void Start()
        {
            Debug.Log("╔════════════════════════════════════════════════════╗");
            Debug.Log("║     CategoryService MVP 功能测试开始                 ║");
            Debug.Log("╚════════════════════════════════════════════════════╝");

            var service = new CategoryService<TestEntity>(
                entity => entity.Id,
                System.StringComparison.OrdinalIgnoreCase,
                CacheStrategy.Balanced
            );

            // 运行所有测试
            TestBasicRegistration(service);
            TestBatchRegistration(service);
            TestCategoryQuery(service);
            TestHierarchicalCategories(service);
            TestWildcardQuery(service);
            TestTagSystem(service);
            TestMetadata(service);
            TestDeletion(service);

            // 输出测试结果
            PrintTestSummary();

            service.Dispose();
        }

        #region 测试方法

        private void TestBasicRegistration(CategoryService<TestEntity> service)
        {
            Debug.Log("\n[测试 1] 基础注册");

            var entity1 = new TestEntity("id1", "Entity 1");
            var entity2 = new TestEntity("id2", "Entity 2");

            var result1 = service.RegisterEntity(entity1, "Category.A").Complete();
            var result2 = service.RegisterEntity(entity2, "Category.A").Complete();

            AssertTrue(result1.IsSuccess, "注册实体1应该成功");
            AssertTrue(result2.IsSuccess, "注册实体2应该成功");

            var retrieved = service.GetById("id1");
            AssertTrue(retrieved.IsSuccess, "应该能获取注册的实体");
            AssertEquals(retrieved.Value.Name, "Entity 1", "实体名称应该匹配");
        }

        private void TestBatchRegistration(CategoryService<TestEntity> service)
        {
            Debug.Log("\n[测试 2] 批量注册");

            var entities = new List<TestEntity>
            {
                new TestEntity("batch1", "Batch Entity 1"),
                new TestEntity("batch2", "Batch Entity 2"),
                new TestEntity("batch3", "Batch Entity 3")
            };

            var result = service.RegisterBatch(entities, "Batch.Category");

            AssertTrue(result.IsFullSuccess, "批量注册应该全部成功");
            AssertEquals(result.SuccessCount, 3, "成功数应该为3");
            AssertEquals(result.FailureCount, 0, "失败数应该为0");
        }

        private void TestCategoryQuery(CategoryService<TestEntity> service)
        {
            Debug.Log("\n[测试 3] 分类查询");

            var entities = service.GetByCategory("Category.A");
            AssertTrue(entities.Count > 0, "应该查询到分类A中的实体");

            var batchEntities = service.GetByCategory("Batch.Category");
            AssertEquals(batchEntities.Count, 3, "应该查询到3个批量注册的实体");

            var allCategories = service.GetAllCategories();
            AssertTrue(allCategories.Count >= 2, "应该至少有2个分类");
        }

        private void TestHierarchicalCategories(CategoryService<TestEntity> service)
        {
            Debug.Log("\n[测试 4] 层级分类");

            var entity = new TestEntity("hier1", "Hierarchical Entity");
            service.RegisterEntity(entity, "Level1.Level2.Level3").Complete();

            var level1 = service.GetByCategory("Level1", includeChildren: true);
            AssertTrue(level1.Count > 0, "应该能获取Level1的所有后代");

            var level2 = service.GetByCategory("Level1.Level2");
            AssertTrue(level2.Count > 0, "应该能获取Level2的实体");
        }

        private void TestWildcardQuery(CategoryService<TestEntity> service)
        {
            Debug.Log("\n[测试 5] 通配符查询");

            var entities = service.RegisterEntity(new TestEntity("wild1", "Wild 1"), "Wildcard.Sub.A").Complete();
            service.RegisterEntity(new TestEntity("wild2", "Wild 2"), "Wildcard.Sub.B").Complete();
            service.RegisterEntity(new TestEntity("wild3", "Wild 3"), "Wildcard.Sub.C").Complete();

            var wildResults = service.GetByCategory("Wildcard.Sub.*");
            AssertTrue(wildResults.Count >= 3, "通配符查询应该返回至少3个结果");
        }

        private void TestTagSystem(CategoryService<TestEntity> service)
        {
            Debug.Log("\n[测试 6] 标签系统");

            var entity1 = new TestEntity("tag1", "Tag Entity 1");
            var entity2 = new TestEntity("tag2", "Tag Entity 2");

            service.RegisterEntity(entity1, "Tag.Category")
                .WithTags("important", "test")
                .Complete();

            service.RegisterEntity(entity2, "Tag.Category")
                .WithTags("important")
                .Complete();

            var important = service.GetByTag("important");
            AssertEquals(important.Count, 2, "应该有2个带important标签的实体");

            var test = service.GetByTag("test");
            AssertEquals(test.Count, 1, "应该有1个带test标签的实体");

            var combo = service.GetByCategoryAndTag("Tag.Category", "important");
            AssertEquals(combo.Count, 2, "组合查询应该返回2个结果");
        }

        private void TestMetadata(CategoryService<TestEntity> service)
        {
            Debug.Log("\n[测试 7] 元数据");

            var entity = new TestEntity("meta1", "Meta Entity");
            var metadata = new List<CustomDataEntry>
            {
                new CustomDataEntry { Id = "key1", Type = CustomDataType.String, StringValue = "value1" },
                new CustomDataEntry { Id = "key2", Type = CustomDataType.String, StringValue = "value2" }
            };

            service.RegisterEntity(entity, "Meta.Category")
                .WithMetadata(metadata)
                .Complete();

            var getResult = service.GetMetadata("meta1");
            AssertTrue(getResult.IsSuccess, "应该能获取元数据");
            AssertEquals(getResult.Value.Count, 2, "元数据应该有2个条目");

            var newMetadata = new List<CustomDataEntry>
            {
                new CustomDataEntry { Id = "key3", Type = CustomDataType.String, StringValue = "value3" }
            };

            var updateResult = service.UpdateMetadata("meta1", newMetadata);
            AssertTrue(updateResult.IsSuccess, "更新元数据应该成功");

            var getAgain = service.GetMetadata("meta1");
            AssertEquals(getAgain.Value.Count, 1, "更新后元数据应该有1个条目");
        }

        private void TestDeletion(CategoryService<TestEntity> service)
        {
            Debug.Log("\n[测试 8] 删除");

            var entity = new TestEntity("del1", "Delete Entity");
            service.RegisterEntity(entity, "Delete.Category").Complete();

            var before = service.GetByCategory("Delete.Category");
            var beforeCount = before.Count;

            var delResult = service.DeleteEntity("del1");
            AssertTrue(delResult.IsSuccess, "删除实体应该成功");

            var after = service.GetByCategory("Delete.Category");
            AssertTrue(after.Count < beforeCount, "删除后实体数应该减少");

            // 测试分类删除
            service.RegisterEntity(new TestEntity("del2", "Delete Entity 2"), "Delete.Sub.A").Complete();
            service.RegisterEntity(new TestEntity("del3", "Delete Entity 3"), "Delete.Sub.B").Complete();

            var delCatResult = service.DeleteCategoryRecursive("Delete.Sub");
            AssertTrue(delCatResult.IsSuccess, "删除分类应该成功");
        }

        #endregion

        #region 辅助方法

        private void AssertTrue(bool condition, string message)
        {
            if (condition)
            {
                Debug.Log($"  ✓ {message}");
                _passedTests++;
            }
            else
            {
                Debug.LogError($"  ✗ {message}");
                _failedTests++;
            }
        }

        private void AssertEquals(int actual, int expected, string message)
        {
            if (actual == expected)
            {
                Debug.Log($"  ✓ {message} (实际: {actual})");
                _passedTests++;
            }
            else
            {
                Debug.LogError($"  ✗ {message} (期望: {expected}, 实际: {actual})");
                _failedTests++;
            }
        }

        private void AssertEquals(string actual, string expected, string message)
        {
            if (actual == expected)
            {
                Debug.Log($"  ✓ {message} ('{actual}')");
                _passedTests++;
            }
            else
            {
                Debug.LogError($"  ✗ {message} (期望: '{expected}', 实际: '{actual}')");
                _failedTests++;
            }
        }

        private void PrintTestSummary()
        {
            var total = _passedTests + _failedTests;
            var passRate = total > 0 ? (_passedTests * 100f) / total : 0f;

            Debug.Log("\n╔════════════════════════════════════════════════════╗");
            Debug.Log("║              测试结果统计                            ║");
            Debug.Log("╠════════════════════════════════════════════════════╣");
            Debug.Log($"║ 通过: {_passedTests,4} / {total,4}  失败: {_failedTests,4}");
            Debug.Log($"║ 通过率: {passRate:F1}%");
            Debug.Log("╠════════════════════════════════════════════════════╣");

            if (_failedTests == 0)
            {
                Debug.Log("║  ✓ 所有测试通过！MVP 功能可用！                     ║");
            }
            else
            {
                Debug.Log($"║  ✗ 有 {_failedTests} 个测试失败，请检查错误                   ║");
            }

            Debug.Log("╚════════════════════════════════════════════════════╝");
        }

        #endregion
    }
}
