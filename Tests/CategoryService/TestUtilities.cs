using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using EasyPack.Category;
using EasyPack.CustomData;

namespace EasyPack.CategoryTests
{
    /// <summary>
    /// 测试辅助工具类
    /// 提供常用的断言、验证和数据生成方法
    /// </summary>
    public static class TestAssertions
    {
        /// <summary>
        /// 断言操作成功
        /// </summary>
        public static void AssertSuccess(OperationResult result, string message = "Operation should succeed")
        {
            Assert.IsTrue(result.IsSuccess, $"{message}. ErrorCode: {result.ErrorCode}, Message: {result.ErrorMessage}");
        }

        /// <summary>
        /// 断言操作失败
        /// </summary>
        public static void AssertFailure(OperationResult result, ErrorCode expectedErrorCode = ErrorCode.None, string message = "Operation should fail")
        {
            Assert.IsFalse(result.IsSuccess, message);
            if (expectedErrorCode != ErrorCode.None)
            {
                Assert.AreEqual(expectedErrorCode, result.ErrorCode, $"Expected error code {expectedErrorCode}, got {result.ErrorCode}");
            }
        }

        /// <summary>
        /// 断言泛型操作失败
        /// </summary>
        public static void AssertFailure<T>(OperationResult<T> result, ErrorCode expectedErrorCode = ErrorCode.None, string message = "Operation should fail")
        {
            Assert.IsFalse(result.IsSuccess, message);
            if (expectedErrorCode != ErrorCode.None)
            {
                Assert.AreEqual(expectedErrorCode, result.ErrorCode, $"Expected error code {expectedErrorCode}, got {result.ErrorCode}");
            }
        }

        /// <summary>
        /// 断言操作成功并返回预期的值
        /// </summary>
        public static T AssertSuccessWithValue<T>(OperationResult<T> result, T expectedValue = default, string message = "Operation should succeed with expected value")
        {
            Assert.IsTrue(result.IsSuccess, $"{message}. ErrorCode: {result.ErrorCode}, Message: {result.ErrorMessage}");
            if (expectedValue != null)
            {
                Assert.AreEqual(expectedValue, result.Value);
            }
            return result.Value;
        }

        /// <summary>
        /// 断言列表包含指定数量的元素
        /// </summary>
        public static void AssertListCount<T>(IReadOnlyList<T> list, int expectedCount, string message = "")
        {
            Assert.IsNotNull(list, "List should not be null");
            Assert.AreEqual(expectedCount, list.Count, $"{message} Expected {expectedCount} items, got {list.Count}");
        }

        /// <summary>
        /// 断言列表为空
        /// </summary>
        public static void AssertListEmpty<T>(IReadOnlyList<T> list, string message = "")
        {
            Assert.IsNotNull(list, "List should not be null");
            Assert.AreEqual(0, list.Count, $"{message} List should be empty but has {list.Count} items");
        }

        /// <summary>
        /// 断言列表包含指定元素
        /// </summary>
        public static void AssertListContains<T>(IReadOnlyList<T> list, T item, string message = "")
        {
            Assert.IsNotNull(list, "List should not be null");
            Assert.IsTrue(list.Contains(item), $"{message} List should contain {item}");
        }

        /// <summary>
        /// 断言列表不包含指定元素
        /// </summary>
        public static void AssertListDoesNotContain<T>(IReadOnlyList<T> list, T item, string message = "")
        {
            Assert.IsNotNull(list, "List should not be null");
            Assert.IsFalse(list.Contains(item), $"{message} List should not contain {item}");
        }

        /// <summary>
        /// 断言批量操作结果全部成功
        /// </summary>
        public static void AssertBatchFullSuccess(BatchOperationResult result, int expectedCount, string message = "")
        {
            Assert.IsNotNull(result, "Batch result should not be null");
            Assert.IsTrue(result.IsFullSuccess, $"{message} Expected full success, but got {result.SuccessCount}/{result.TotalCount}");
            Assert.AreEqual(expectedCount, result.SuccessCount, $"{message} Expected {expectedCount} successes");
            Assert.AreEqual(expectedCount, result.TotalCount, $"{message} Expected {expectedCount} total items");
        }

        /// <summary>
        /// 断言批量操作结果包含指定数量的成功和失败
        /// </summary>
        public static void AssertBatchPartialSuccess(BatchOperationResult result, int expectedSuccessCount, int expectedFailureCount, string message = "")
        {
            Assert.IsNotNull(result, "Batch result should not be null");
            Assert.IsTrue(result.IsPartialSuccess, $"{message} Expected partial success");
            Assert.AreEqual(expectedSuccessCount, result.SuccessCount, $"{message} Expected {expectedSuccessCount} successes");
            Assert.AreEqual(expectedFailureCount, result.FailureCount, $"{message} Expected {expectedFailureCount} failures");
        }
    }

    /// <summary>
    /// 测试数据生成器
    /// 提供常用的测试数据
    /// </summary>
    public static class TestDataGenerator
    {
        /// <summary>
        /// 生成指定数量的测试实体
        /// </summary>
        public static List<TestEntity> GenerateEntities(int count, string idPrefix = "entity_")
        {
            var entities = new List<TestEntity>();
            for (int i = 0; i < count; i++)
            {
                entities.Add(new TestEntity(idPrefix + i, $"Entity {i}", $"Description for entity {i}"));
            }
            return entities;
        }

        /// <summary>
        /// 生成指定数量的标签
        /// </summary>
        public static string[] GenerateTags(int count, string tagPrefix = "tag_")
        {
            var tags = new string[count];
            for (int i = 0; i < count; i++)
            {
                tags[i] = tagPrefix + i;
            }
            return tags;
        }

        /// <summary>
        /// 生成分层分类路径
        /// </summary>
        public static string GenerateCategory(int depth, string baseName = "Category")
        {
            var parts = new List<string>();
            for (int i = 0; i < depth; i++)
            {
                parts.Add($"{baseName}{i}");
            }
            return string.Join(".", parts);
        }

        /// <summary>
        /// 生成多个分层分类
        /// </summary>
        public static List<string> GenerateCategories(int hierarchyLevels, int itemsPerLevel)
        {
            var categories = new List<string>();

            void GenerateHierarchy(string prefix, int level)
            {
                if (level >= hierarchyLevels)
                    return;

                for (int i = 0; i < itemsPerLevel; i++)
                {
                    var category = string.IsNullOrEmpty(prefix) 
                        ? $"Cat{level}_{i}" 
                        : $"{prefix}.Cat{level}_{i}";
                    categories.Add(category);
                    GenerateHierarchy(category, level + 1);
                }
            }

            GenerateHierarchy("", 0);
            return categories;
        }

        /// <summary>
        /// 创建测试元数据集合
        /// </summary>
        public static CustomDataCollection CreateTestMetadata(int itemCount = 3)
        {
            var metadata = new CustomDataCollection();
            for (int i = 0; i < itemCount; i++)
            {
                metadata.Add(CustomDataEntry.CreateString($"key_{i}", $"value_{i}"));
            }
            return metadata;
        }

        /// <summary>
        /// 创建包含 CustomDataCollection 的元数据集合
        /// </summary>
        public static CustomDataCollection CreateMetadataWithCustomDataCollection(int listCount = 2, int itemsPerList = 3)
        {
            var metadata = new CustomDataCollection();
            
            for (int i = 0; i < listCount; i++)
            {
                var list = new CustomDataCollection();
                for (int j = 0; j < itemsPerList; j++)
                {
                    list.Add(CustomDataEntry.CreateString($"item_{j}", $"Value_{i}_{j}"));
                }
                metadata.Add(CustomDataEntry.CreateEntryList($"list_{i}", list));
            }
            
            return metadata;
        }

        /// <summary>
        /// 创建混合类型的元数据集合（包含普通字段和 CustomDataCollection）
        /// </summary>
        public static CustomDataCollection CreateMixedMetadata(int basicCount = 2, int listCount = 2)
        {
            var metadata = new CustomDataCollection();
            
            // 添加普通字段
            for (int i = 0; i < basicCount; i++)
            {
                metadata.Add(CustomDataEntry.CreateString($"basic_{i}", $"value_{i}"));
            }
            
            // 添加 CustomDataCollection 字段
            for (int i = 0; i < listCount; i++)
            {
                var list = new CustomDataCollection();
                list.Add(CustomDataEntry.CreateInt($"attr_count", 10 + i));
                list.Add(CustomDataEntry.CreateString($"attr_name", $"Attribute_{i}"));
                
                metadata.Add(CustomDataEntry.CreateEntryList($"list_{i}", list));
            }
            
            return metadata;
        }

        /// <summary>
        /// 创建嵌套的 CustomDataCollection 元数据
        /// </summary>
        public static CustomDataCollection CreateNestedCustomDataCollectionMetadata()
        {
            var metadata = new CustomDataCollection();
            
            // 创建第2层的列表
            var innerList1 = new CustomDataCollection();
            innerList1.Add(CustomDataEntry.CreateString("nested_item_1", "Nested Value 1"));
            innerList1.Add(CustomDataEntry.CreateInt("nested_count", 5));
            
            var innerList2 = new CustomDataCollection();
            innerList2.Add(CustomDataEntry.CreateString("nested_item_2", "Nested Value 2"));
            innerList2.Add(CustomDataEntry.CreateBool("nested_flag", true));
            
            // 创建第1层的列表，包含嵌套列表
            var outerList = new CustomDataCollection();
            outerList.Add(CustomDataEntry.CreateEntryList("inner_1", innerList1));
            outerList.Add(CustomDataEntry.CreateEntryList("inner_2", innerList2));
            
            // 添加到元数据
            metadata.Add(CustomDataEntry.CreateEntryList("nested_structure", outerList));
            
            return metadata;
        }
    }

    /// <summary>
    /// 测试固件基类
    /// 提供通用的测试生命周期方法
    /// </summary>
    public abstract class CategoryTestBase
    {
        protected CategoryManager<TestEntity, string> Manager { get; set; }

        [SetUp]
        public virtual void SetUp()
        {
            // 创建新的管理器实例，使用 ID 提取函数
            Manager = new CategoryManager<TestEntity, string>(entity => entity.Id.Trim());
        }

        [TearDown]
        public virtual void TearDown()
        {
            // 清理资源
            Manager?.Dispose();
            Manager = null;
        }

        /// <summary>
        /// 注册实体到分类
        /// </summary>
        protected OperationResult RegisterEntity(TestEntity entity, string category, string[] tags = null, CustomDataCollection metadata = null)
        {
            var registration = Manager.RegisterEntity(entity, category);
            
            if (tags != null && tags.Length > 0)
            {
                registration.WithTags(tags);
            }

            if (metadata != null)
            {
                registration.WithMetadata(metadata);
            }

            return registration.Complete();
        }

        /// <summary>
        /// 验证实体是否存在于指定分类中
        /// </summary>
        protected bool EntityInCategory(TestEntity entity, string category, bool includeChildren = false)
        {
            var result = Manager.GetByCategory(category, includeChildren);
            return result.Any(e => e.Id == entity.Id);
        }

        /// <summary>
        /// 验证实体是否拥有指定标签
        /// </summary>
        protected bool EntityHasTag(TestEntity entity, string tag)
        {
            var result = Manager.GetByTag(tag);
            return result.Any(e => e.Id == entity.Id);
        }
    }
}
