using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using EasyPack.Category;

namespace EasyPack.CategoryTests
{
    /// <summary>
    /// CategoryManager 标签操作测试
    /// 测试标签的增删查改、缓存行为和多标签查询
    /// </summary>
    [TestFixture]
    public class TagOperationTests : CategoryTestBase
    {
        #region 添加标签测试

        [Test]
        public void AddTag_SingleTag_Success()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            RegisterEntity(entity, "Equipment");

            // Act
            var result = Manager.AddTag("entity_1", "rare");

            // Assert
            TestAssertions.AssertSuccess(result);
            Assert.IsTrue(EntityHasTag(entity, "rare"));
        }

        [Test]
        public void AddTag_MultipleTags_Success()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            RegisterEntity(entity, "Equipment");
            var tags = new[] { "rare", "epic", "sword" };

            // Act
            foreach (var tag in tags)
            {
                Manager.AddTag("entity_1", tag);
            }

            // Assert
            foreach (var tag in tags)
            {
                Assert.IsTrue(EntityHasTag(entity, tag));
            }
        }

        [Test]
        public void AddTag_DuplicateTag_AllowsDuplicate()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            RegisterEntity(entity, "Equipment");

            // Act
            var result1 = Manager.AddTag("entity_1", "rare");
            var result2 = Manager.AddTag("entity_1", "rare");

            // Assert
            TestAssertions.AssertSuccess(result1);
            TestAssertions.AssertSuccess(result2);
            
            var taggedEntities = Manager.GetByTag("rare");
            TestAssertions.AssertListCount(taggedEntities, 1, "Should only appear once in tag results");
        }

        [Test]
        public void AddTag_NonExistingEntity_Failure()
        {
            // Act
            var result = Manager.AddTag("nonexistent_id", "rare");

            // Assert
            TestAssertions.AssertFailure(result, ErrorCode.NotFound);
        }

        [Test]
        public void AddTag_EmptyTag_Failure()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            RegisterEntity(entity, "Equipment");

            // Act
            var result = Manager.AddTag("entity_1", "");

            // Assert
            TestAssertions.AssertFailure(result, ErrorCode.InvalidCategory);
        }

        [Test]
        public void AddTag_WhitespaceTag_Failure()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            RegisterEntity(entity, "Equipment");

            // Act
            var result = Manager.AddTag("entity_1", "   ");

            // Assert
            TestAssertions.AssertFailure(result, ErrorCode.InvalidCategory);
        }

        [Test]
        public void AddTag_NullTag_Failure()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            RegisterEntity(entity, "Equipment");

            // Act
            var result = Manager.AddTag("entity_1", null);

            // Assert
            TestAssertions.AssertFailure(result, ErrorCode.InvalidCategory);
        }

        #endregion

        #region 移除标签测试

        [Test]
        public void RemoveTag_ExistingTag_Success()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            Manager.RegisterEntity(entity, "Equipment")
                .WithTags("rare")
                .Complete();
            Assert.IsTrue(EntityHasTag(entity, "rare"));

            // Act
            var result = Manager.RemoveTag("entity_1", "rare");

            // Assert
            TestAssertions.AssertSuccess(result);
            Assert.IsFalse(EntityHasTag(entity, "rare"));
        }

        [Test]
        public void RemoveTag_NonExistingTag_Failure()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            RegisterEntity(entity, "Equipment");

            // Act
            var result = Manager.RemoveTag("entity_1", "nonexistent");

            // Assert
            TestAssertions.AssertFailure(result, ErrorCode.NotFound);
        }

        [Test]
        public void RemoveTag_NonExistingEntity_Failure()
        {
            // Act
            var result = Manager.RemoveTag("nonexistent_id", "rare");

            // Assert
            TestAssertions.AssertFailure(result, ErrorCode.NotFound);
        }

        [Test]
        public void RemoveTag_RemovesFromTagToEntityMapping()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            Manager.RegisterEntity(entity, "Equipment")
                .WithTags("rare")
                .Complete();

            // Act
            Manager.RemoveTag("entity_1", "rare");

            // Assert
            var result = Manager.GetByTag("rare");
            TestAssertions.AssertListEmpty(result, "Tag should have no entities after removal");
        }

        [Test]
        public void RemoveTag_EmptyTag_Failure()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            RegisterEntity(entity, "Equipment");

            // Act
            var result = Manager.RemoveTag("entity_1", "");

            // Assert
            TestAssertions.AssertFailure(result, ErrorCode.InvalidCategory);
        }

        #endregion

        #region 标签查询测试

        [Test]
        public void GetByTag_SingleEntity_Success()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            Manager.RegisterEntity(entity, "Equipment")
                .WithTags("rare")
                .Complete();

            // Act
            var result = Manager.GetByTag("rare");

            // Assert
            TestAssertions.AssertListCount(result, 1);
            TestAssertions.AssertListContains(result, entity);
        }

        [Test]
        public void GetByTag_MultipleEntities_Success()
        {
            // Arrange
            var entities = TestDataGenerator.GenerateEntities(3, "entity_");
            foreach (var entity in entities)
            {
                Manager.RegisterEntity(entity, "Equipment")
                    .WithTags("rare")
                    .Complete();
            }

            // Act
            var result = Manager.GetByTag("rare");

            // Assert
            TestAssertions.AssertListCount(result, 3);
            foreach (var entity in entities)
            {
                TestAssertions.AssertListContains(result, entity);
            }
        }

        [Test]
        public void GetByTag_NonExistingTag_EmptyList()
        {
            // Act
            var result = Manager.GetByTag("nonexistent");

            // Assert
            TestAssertions.AssertListEmpty(result);
        }

        [Test]
        public void GetByTag_EmptyTag_EmptyList()
        {
            // Act
            var result = Manager.GetByTag("");

            // Assert
            TestAssertions.AssertListEmpty(result);
        }

        [Test]
        public void GetByTag_NullTag_EmptyList()
        {
            // Act
            var result = Manager.GetByTag(null);

            // Assert
            TestAssertions.AssertListEmpty(result);
        }

        #endregion

        #region 多标签查询测试

        [Test]
        public void GetByTags_AND_AllTagsRequired_Success()
        {
            // Arrange
            var entity1 = new TestEntity("entity_1", "Entity 1");
            var entity2 = new TestEntity("entity_2", "Entity 2");
            var entity3 = new TestEntity("entity_3", "Entity 3");
            
            Manager.RegisterEntity(entity1, "Equipment")
                .WithTags("rare", "epic")
                .Complete();
            Manager.RegisterEntity(entity2, "Equipment")
                .WithTags("rare")
                .Complete();
            Manager.RegisterEntity(entity3, "Equipment")
                .WithTags("common")
                .Complete();

            // Act
            var result = Manager.GetByTags(new[] { "rare", "epic" }, matchAll: true);

            // Assert
            TestAssertions.AssertListCount(result, 1);
            TestAssertions.AssertListContains(result, entity1);
        }

        [Test]
        public void GetByTags_OR_AnyTagAccepted_Success()
        {
            // Arrange
            var entity1 = new TestEntity("entity_1", "Entity 1");
            var entity2 = new TestEntity("entity_2", "Entity 2");
            var entity3 = new TestEntity("entity_3", "Entity 3");
            
            Manager.RegisterEntity(entity1, "Equipment")
                .WithTags("rare")
                .Complete();
            Manager.RegisterEntity(entity2, "Equipment")
                .WithTags("epic")
                .Complete();
            Manager.RegisterEntity(entity3, "Equipment")
                .WithTags("common")
                .Complete();

            // Act
            var result = Manager.GetByTags(new[] { "rare", "epic" }, matchAll: false);

            // Assert
            TestAssertions.AssertListCount(result, 2);
            TestAssertions.AssertListContains(result, entity1);
            TestAssertions.AssertListContains(result, entity2);
        }

        [Test]
        public void GetByTags_AND_NoCommonTags_EmptyResult()
        {
            // Arrange
            var entity1 = new TestEntity("entity_1", "Entity 1");
            var entity2 = new TestEntity("entity_2", "Entity 2");
            
            Manager.RegisterEntity(entity1, "Equipment")
                .WithTags("rare")
                .Complete();
            Manager.RegisterEntity(entity2, "Equipment")
                .WithTags("epic")
                .Complete();

            // Act
            var result = Manager.GetByTags(new[] { "rare", "epic" }, matchAll: true);

            // Assert
            TestAssertions.AssertListEmpty(result);
        }

        [Test]
        public void GetByTags_EmptyArray_EmptyResult()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            Manager.RegisterEntity(entity, "Equipment")
                .WithTags("rare")
                .Complete();

            // Act
            var result = Manager.GetByTags(new string[0], matchAll: true);

            // Assert
            TestAssertions.AssertListEmpty(result);
        }

        [Test]
        public void GetByTags_NullArray_EmptyResult()
        {
            // Act
            var result = Manager.GetByTags(null, matchAll: true);

            // Assert
            TestAssertions.AssertListEmpty(result);
        }

        [Test]
        public void GetByTags_WhitespaceTag_Ignored()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            Manager.RegisterEntity(entity, "Equipment")
                .WithTags("rare")
                .Complete();

            // Act
            var result = Manager.GetByTags(new[] { "rare", "   " }, matchAll: false);

            // Assert
            TestAssertions.AssertListCount(result, 1);
        }

        #endregion

        #region 分类和标签交叉查询测试

        [Test]
        public void GetByCategoryAndTag_BothMatch_Success()
        {
            // Arrange
            var entity1 = new TestEntity("entity_1", "Entity 1");
            var entity2 = new TestEntity("entity_2", "Entity 2");
            var entity3 = new TestEntity("entity_3", "Entity 3");
            
            Manager.RegisterEntity(entity1, "Equipment")
                .WithTags("rare")
                .Complete();
            Manager.RegisterEntity(entity2, "Equipment")
                .WithTags("common")
                .Complete();
            Manager.RegisterEntity(entity3, "Armor")
                .WithTags("rare")
                .Complete();

            // Act
            var result = Manager.GetByCategoryAndTag("Equipment", "rare");

            // Assert
            TestAssertions.AssertListCount(result, 1);
            TestAssertions.AssertListContains(result, entity1);
        }

        [Test]
        public void GetByCategoryAndTag_NoIntersection_EmptyResult()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            Manager.RegisterEntity(entity, "Equipment")
                .WithTags("rare")
                .Complete();

            // Act
            var result = Manager.GetByCategoryAndTag("Equipment", "common");

            // Assert
            TestAssertions.AssertListEmpty(result);
        }

        #endregion

        #region 标签缓存测试

        [Test]
        public void GetByTag_CacheHit_Performance()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            Manager.RegisterEntity(entity, "Equipment")
                .WithTags("rare")
                .Complete();

            // Act - First query (cache miss)
            var result1 = Manager.GetByTag("rare");
            var cacheSize1 = Manager.GetCacheSize();

            // Second query (cache hit)
            var result2 = Manager.GetByTag("rare");
            var cacheSize2 = Manager.GetCacheSize();

            // Assert
            TestAssertions.AssertListCount(result1, 1);
            TestAssertions.AssertListCount(result2, 1);
            Assert.AreEqual(cacheSize1, cacheSize2);
            Assert.Greater(cacheSize2, 0, "Cache should be populated");
        }

        [Test]
        public void AddTag_InvalidatesCacheForTag()
        {
            // Arrange
            var entity1 = new TestEntity("entity_1", "Entity 1");
            var entity2 = new TestEntity("entity_2", "Entity 2");
            Manager.RegisterEntity(entity1, "Equipment")
                .WithTags("rare")
                .Complete();

            var result1 = Manager.GetByTag("rare");
            TestAssertions.AssertListCount(result1, 1);

            // Act
            Manager.RegisterEntity(entity2, "Equipment").Complete();
            Manager.AddTag("entity_2", "rare");

            // Assert
            var result2 = Manager.GetByTag("rare");
            TestAssertions.AssertListCount(result2, 2);
        }

        [Test]
        public void RemoveTag_InvalidatesCacheForTag()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            Manager.RegisterEntity(entity, "Equipment")
                .WithTags("rare")
                .Complete();

            var result1 = Manager.GetByTag("rare");
            TestAssertions.AssertListCount(result1, 1);

            // Act
            Manager.RemoveTag("entity_1", "rare");

            // Assert
            var result2 = Manager.GetByTag("rare");
            TestAssertions.AssertListEmpty(result2);
        }

        [Test]
        public void InvalidateTagCache_ClearsSpecificTagCache()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            Manager.RegisterEntity(entity, "Equipment")
                .WithTags("rare", "epic")
                .Complete();

            Manager.GetByTag("rare");
            Manager.GetByTag("epic");
            var cacheSize1 = Manager.GetCacheSize();

            // Act
            Manager.InvalidateTagCache("rare");
            var cacheSize2 = Manager.GetCacheSize();

            // Assert
            Assert.Greater(cacheSize1, cacheSize2, "Cache should be reduced after invalidation");
        }

        [Test]
        public void ClearCache_EmptiesAllTagCache()
        {
            // Arrange
            var entities = TestDataGenerator.GenerateEntities(3, "entity_");
            var tags = new[] { "tag1", "tag2", "tag3" };
            
            foreach (var entity in entities)
            {
                Manager.RegisterEntity(entity, "Equipment")
                    .WithTags(tags)
                    .Complete();
            }

            Manager.GetByTag("tag1");
            Manager.GetByTag("tag2");
            Manager.GetByTag("tag3");
            
            Assert.Greater(Manager.GetCacheSize(), 0);

            // Act
            Manager.ClearCache();

            // Assert
            Assert.AreEqual(0, Manager.GetCacheSize());
        }

        #endregion

        #region 标签边界情况测试

        [Test]
        public void Tag_WithSpecialCharacters_Success()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            RegisterEntity(entity, "Equipment");

            // Act
            var result = Manager.AddTag("entity_1", "rare-epic_v2.0");

            // Assert
            TestAssertions.AssertSuccess(result);
            Assert.IsTrue(EntityHasTag(entity, "rare-epic_v2.0"));
        }

        [Test]
        public void Tag_CaseSensitive()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            Manager.RegisterEntity(entity, "Equipment")
                .WithTags("Rare")
                .Complete();

            // Act
            var result1 = Manager.GetByTag("Rare");
            var result2 = Manager.GetByTag("rare");
            var result3 = Manager.GetByTag("RARE");

            // Assert
            TestAssertions.AssertListCount(result1, 1);
            TestAssertions.AssertListEmpty(result2);
            TestAssertions.AssertListEmpty(result3);
        }
        #endregion

        #region 统计测试

        [Test]
        public void Statistics_IncludesTotalTags()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            Manager.RegisterEntity(entity, "Equipment")
                .WithTags("rare", "epic", "sword")
                .Complete();

            // Act
            var stats = Manager.GetStatistics();

            // Assert
            Assert.AreEqual(3, stats.TotalTags);
        }

        [Test]
        public void Statistics_MultipleEntitiesWithSharedTags()
        {
            // Arrange
            var entities = TestDataGenerator.GenerateEntities(3, "entity_");
            foreach (var entity in entities)
            {
                Manager.RegisterEntity(entity, "Equipment")
                    .WithTags("rare")
                    .Complete();
            }

            // Act
            var stats = Manager.GetStatistics();

            // Assert
            Assert.AreEqual(3, stats.TotalEntities);
            Assert.AreEqual(1, stats.TotalTags);
        }

        #endregion
    }
}
