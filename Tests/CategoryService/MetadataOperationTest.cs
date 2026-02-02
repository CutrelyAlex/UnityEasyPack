using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using EasyPack.Category;
using EasyPack.CustomData;

namespace EasyPack.CategoryTests
{
    /// <summary>
    /// CategoryManager 元数据操作测试
    /// 测试元数据的增删查改、持久性和CustomDataCollection集成
    /// </summary>
    [TestFixture]
    public class MetadataOperationTests : CategoryTestBase
    {
        #region 元数据查询测试 (HasMetadata)

        [Test]
        public void HasMetadata_WhenNoMetadata_ReturnsFalse()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            RegisterEntity(entity, "Equipment");

            // Act
            bool result = Manager.HasMetadata("entity_1");

            // Assert
            Assert.IsFalse(result, "未设置元数据时应返回 false");
        }

        [Test]
        public void HasMetadata_WithMetadata_ReturnsTrue()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            var metadata = TestDataGenerator.CreateTestMetadata(1);
            Manager.RegisterEntity(entity, "Equipment")
                .WithMetadata(metadata)
                .Complete();

            // Act
            bool result = Manager.HasMetadata("entity_1");

            // Assert
            Assert.IsTrue(result, "已设置元数据时应返回 true");
        }

        [Test]
        public void HasMetadata_NonExistentEntity_ReturnsFalse()
        {
            // Act
            bool result = Manager.HasMetadata("invalid_id");

            // Assert
            Assert.IsFalse(result, "不存在的实体应返回 false");
        }

        [Test]
        public void HasMetadata_AfterDeletion_ReturnsFalse()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            var metadata = TestDataGenerator.CreateTestMetadata(1);
            Manager.RegisterEntity(entity, "Equipment")
                .WithMetadata(metadata)
                .Complete();
            
            Assert.IsTrue(Manager.HasMetadata("entity_1"));

            // Act
            Manager.DeleteEntity("entity_1");

            // Assert
            Assert.IsFalse(Manager.HasMetadata("entity_1"), "实体删除后元数据也应被清理，HasMetadata 应返回 false");
        }

        #endregion

        #region 元数据设置测试

        [Test]
        public void GetMetadata_NoMetadata_ReturnsEmptyCollection()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            RegisterEntity(entity, "Equipment");

            // Act
            var metadata = Manager.GetMetadata("entity_1");

            // Assert
            Assert.IsNotNull(metadata);
            Assert.AreEqual(0, metadata.Count);
        }

        [Test]
        public void GetMetadata_WithMetadata_ReturnsData()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            var metadata = TestDataGenerator.CreateTestMetadata(3);
            Manager.RegisterEntity(entity, "Equipment")
                .WithMetadata(metadata)
                .Complete();

            // Act
            var retrieved = Manager.GetMetadata("entity_1");

            // Assert
            Assert.AreEqual(3, retrieved.Count);
        }

        [Test]
        public void GetMetadata_NonExistentEntity_ReturnsEmptyCollection()
        {
            // Act
            var metadata = Manager.GetMetadata("nonexistent_id");

            // Assert
            Assert.IsNotNull(metadata);
            Assert.AreEqual(0, metadata.Count);
        }

        [Test]
        public void GetMetadataResult_Success_ReturnsValue()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            var metadata = TestDataGenerator.CreateTestMetadata(2);
            Manager.RegisterEntity(entity, "Equipment")
                .WithMetadata(metadata)
                .Complete();

            // Act
            var result = Manager.GetMetadataResult("entity_1");

            // Assert
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(2, result.Value.Count);
        }

        [Test]
        public void GetMetadataResult_NonExistent_Failure()
        {
            // Act
            var result = Manager.GetMetadataResult("nonexistent_id");

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(ErrorCode.NotFound, result.ErrorCode);
        }

        #endregion

        #region 元数据更新测试

        [Test]
        public void UpdateMetadata_NewMetadata_Success()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            RegisterEntity(entity, "Equipment");
            var newMetadata = TestDataGenerator.CreateTestMetadata(2);

            // Act
            var result = Manager.UpdateMetadata("entity_1", newMetadata);

            // Assert
            TestAssertions.AssertSuccess(result);
            var retrieved = Manager.GetMetadata("entity_1");
            Assert.AreEqual(2, retrieved.Count);
        }

        [Test]
        public void UpdateMetadata_ReplaceExisting_Success()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            var oldMetadata = TestDataGenerator.CreateTestMetadata(3);
            Manager.RegisterEntity(entity, "Equipment")
                .WithMetadata(oldMetadata)
                .Complete();
            
            var newMetadata = TestDataGenerator.CreateTestMetadata(2);

            // Act
            var result = Manager.UpdateMetadata("entity_1", newMetadata);

            // Assert
            TestAssertions.AssertSuccess(result);
            var retrieved = Manager.GetMetadata("entity_1");
            Assert.AreEqual(2, retrieved.Count);
        }

        [Test]
        public void UpdateMetadata_NonExistentEntity_Failure()
        {
            // Arrange
            var metadata = TestDataGenerator.CreateTestMetadata(1);

            // Act
            var result = Manager.UpdateMetadata("nonexistent_id", metadata);

            // Assert
            TestAssertions.AssertFailure(result, ErrorCode.NotFound);
        }

        [Test]
        public void UpdateMetadata_NullMetadata_Success()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            RegisterEntity(entity, "Equipment");

            // Act
            var result = Manager.UpdateMetadata("entity_1", null);

            // Assert
            TestAssertions.AssertSuccess(result);
        }

        #endregion

        #region CustomDataCollection集成测试

        [Test]
        public void Metadata_AddEntry_Success()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            var metadata = new CustomDataCollection();
            metadata.Add(CustomDataEntry.CreateString("rarity", "rare"));
            
            Manager.RegisterEntity(entity, "Equipment")
                .WithMetadata(metadata)
                .Complete();

            // Act
            var retrieved = Manager.GetMetadata("entity_1");

            // Assert
            Assert.AreEqual(1, retrieved.Count);
            Assert.IsTrue(retrieved.Any(e => e.Key == "rarity" && e.StringValue == "rare"));
        }

        [Test]
        public void Metadata_MultipleEntries_Success()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            var metadata = new CustomDataCollection();
            
            var properties = new[]
            {
                ("rarity", "rare"),
                ("damage", "50"),
                ("element", "fire"),
                ("level", "10")
            };
            
            foreach (var (key, value) in properties)
            {
                metadata.Add(CustomDataEntry.CreateString(key, value));
            }
            
            Manager.RegisterEntity(entity, "Equipment")
                .WithMetadata(metadata)
                .Complete();

            // Act
            var retrieved = Manager.GetMetadata("entity_1");

            // Assert
            Assert.AreEqual(4, retrieved.Count);
            foreach (var (key, value) in properties)
            {
                Assert.IsTrue(retrieved.Any(e => e.Key == key && e.StringValue == value));
            }
        }

        [Test]
        public void Metadata_UpdateEntry_Success()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            var metadata = new CustomDataCollection();
            metadata.Add(CustomDataEntry.CreateString("damage", "50"));
            
            Manager.RegisterEntity(entity, "Equipment")
                .WithMetadata(metadata)
                .Complete();

            // Act
            var retrieved = Manager.GetMetadata("entity_1");
            retrieved.Clear();
            retrieved.Add(CustomDataEntry.CreateString("damage", "100"));
            Manager.UpdateMetadata("entity_1", retrieved);

            // Assert
            var updated = Manager.GetMetadata("entity_1");
            Assert.AreEqual(1, updated.Count);
            Assert.IsTrue(updated.Any(e => e.Key == "damage" && e.StringValue == "100"));
        }

        [Test]
        public void Metadata_RemoveEntry_Success()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            var metadata = new CustomDataCollection();
            metadata.Add(CustomDataEntry.CreateString("key1", "value1"));
            metadata.Add(CustomDataEntry.CreateString("key2", "value2"));
            
            Manager.RegisterEntity(entity, "Equipment")
                .WithMetadata(metadata)
                .Complete();

            // Act
            var retrieved = Manager.GetMetadata("entity_1");
            var entryToRemove = retrieved.FirstOrDefault(e => e.Key == "key1");
            if (entryToRemove != null)
            {
                retrieved.Remove(entryToRemove);
            }
            Manager.UpdateMetadata("entity_1", retrieved);

            // Assert
            var updated = Manager.GetMetadata("entity_1");
            Assert.AreEqual(1, updated.Count);
            Assert.IsTrue(updated.Any(e => e.Key == "key2"));
        }

        #endregion

        #region 元数据持久性测试

        [Test]
        public void Metadata_PersistAfterEntityMove_Success()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            var metadata = TestDataGenerator.CreateTestMetadata(2);
            Manager.RegisterEntity(entity, "Equipment")
                .WithMetadata(metadata)
                .Complete();

            var metadataBefore = Manager.GetMetadata("entity_1");
            Assert.AreEqual(2, metadataBefore.Count);

            // Act
            Manager.MoveEntityToCategory("entity_1", "Armor");

            // Assert
            var metadataAfter = Manager.GetMetadata("entity_1");
            Assert.AreEqual(2, metadataAfter.Count);
            Assert.AreEqual(metadataBefore.Count, metadataAfter.Count);
        }

        [Test]
        public void Metadata_RemovedWithEntity_Success()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            var metadata = TestDataGenerator.CreateTestMetadata(2);
            Manager.RegisterEntity(entity, "Equipment")
                .WithMetadata(metadata)
                .Complete();

            var metadataBefore = Manager.GetMetadata("entity_1");
            Assert.AreEqual(2, metadataBefore.Count);

            // Act
            Manager.DeleteEntity("entity_1");

            // Assert
            var metadataAfter = Manager.GetMetadata("entity_1");
            Assert.AreEqual(0, metadataAfter.Count);
        }

        [Test]
        public void Metadata_PersistAfterTagAddition_Success()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            var metadata = TestDataGenerator.CreateTestMetadata(2);
            Manager.RegisterEntity(entity, "Equipment")
                .WithMetadata(metadata)
                .Complete();

            var metadataBefore = Manager.GetMetadata("entity_1");

            // Act
            Manager.AddTag("entity_1", "rare");

            // Assert
            var metadataAfter = Manager.GetMetadata("entity_1");
            Assert.AreEqual(metadataBefore.Count, metadataAfter.Count);
        }

        #endregion

        #region 元数据查询与检索测试

        [Test]
        public void GetMetadata_MultipleEntities_Independent()
        {
            // Arrange
            var entity1 = new TestEntity("entity_1", "Entity 1");
            var entity2 = new TestEntity("entity_2", "Entity 2");
            var metadata1 = new CustomDataCollection();
            var metadata2 = new CustomDataCollection();
            
            metadata1.Add(CustomDataEntry.CreateString("type", "sword"));
            metadata2.Add(CustomDataEntry.CreateString("type", "armor"));
            
            Manager.RegisterEntity(entity1, "Equipment")
                .WithMetadata(metadata1)
                .Complete();
            Manager.RegisterEntity(entity2, "Equipment")
                .WithMetadata(metadata2)
                .Complete();

            // Act
            var retrieved1 = Manager.GetMetadata("entity_1");
            var retrieved2 = Manager.GetMetadata("entity_2");

            // Assert
            Assert.AreEqual(1, retrieved1.Count);
            Assert.AreEqual(1, retrieved2.Count);
            Assert.IsTrue(retrieved1.Any(e => e.StringValue == "sword"));
            Assert.IsTrue(retrieved2.Any(e => e.StringValue == "armor"));
        }

        [Test]
        public void Metadata_SharedAcrossOperations()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            var metadata = TestDataGenerator.CreateTestMetadata(3);
            Manager.RegisterEntity(entity, "Equipment")
                .WithMetadata(metadata)
                .Complete();

            // Act - Perform multiple operations
            Manager.AddTag("entity_1", "rare");
            Manager.MoveEntityToCategory("entity_1", "Equipment.Weapon");
            Manager.AddTag("entity_1", "epic");

            // Assert - Metadata should be preserved
            var retrieved = Manager.GetMetadata("entity_1");
            Assert.AreEqual(3, retrieved.Count);
        }

        #endregion

        #region 元数据清空测试

        [Test]
        public void ClearMetadata_RemovesAll()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            var metadata = TestDataGenerator.CreateTestMetadata(3);
            Manager.RegisterEntity(entity, "Equipment")
                .WithMetadata(metadata)
                .Complete();

            var emptyMetadata = new CustomDataCollection();

            // Act
            Manager.UpdateMetadata("entity_1", emptyMetadata);

            // Assert
            var retrieved = Manager.GetMetadata("entity_1");
            Assert.AreEqual(0, retrieved.Count);
        }

        #endregion

        #region 元数据边界情况测试

        [Test]
        public void Metadata_LargeValue_Success()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            var largeValue = new string('x', 10000);
            var metadata = new CustomDataCollection();
            metadata.Add(CustomDataEntry.CreateString("description", largeValue));
            
            // Act
            Manager.RegisterEntity(entity, "Equipment")
                .WithMetadata(metadata)
                .Complete();

            // Assert
            var retrieved = Manager.GetMetadata("entity_1");
            Assert.AreEqual(1, retrieved.Count);
            Assert.AreEqual(largeValue, retrieved[0].StringValue);
        }

        [Test]
        public void Metadata_ManyEntries_Success()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            var metadata = new CustomDataCollection();
            
            for (int i = 0; i < 100; i++)
            {
                metadata.Add(CustomDataEntry.CreateString($"key_{i}", $"value_{i}"));
            }
            
            // Act
            Manager.RegisterEntity(entity, "Equipment")
                .WithMetadata(metadata)
                .Complete();

            // Assert
            var retrieved = Manager.GetMetadata("entity_1");
            Assert.AreEqual(100, retrieved.Count);
        }

        [Test]
        public void Metadata_SpecialCharacters_Success()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            var metadata = new CustomDataCollection();
            metadata.Add(CustomDataEntry.CreateString("description", "Special chars: @#$%^&*()_+-=[]{}|;:',.<>?/~`"));
            
            // Act
            Manager.RegisterEntity(entity, "Equipment")
                .WithMetadata(metadata)
                .Complete();

            // Assert
            var retrieved = Manager.GetMetadata("entity_1");
            Assert.AreEqual(1, retrieved.Count);
            Assert.IsTrue(retrieved.Any(e => e.StringValue.Contains("@#$%")));
        }

        [Test]
        public void Metadata_EmptyKey_Success()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            var metadata = new CustomDataCollection();
            metadata.Add(CustomDataEntry.CreateString("", "empty_key_value"));
            
            // Act
            Manager.RegisterEntity(entity, "Equipment")
                .WithMetadata(metadata)
                .Complete();

            // Assert
            var retrieved = Manager.GetMetadata("entity_1");
            Assert.AreEqual(1, retrieved.Count);
        }

        [Test]
        public void Metadata_UnicodeCharacters_Success()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            var metadata = new CustomDataCollection();
            metadata.Add(CustomDataEntry.CreateString("name_cn", "魔法剑"));
            metadata.Add(CustomDataEntry.CreateString("name_jp", "魔法の剣"));
            metadata.Add(CustomDataEntry.CreateString("name_emoji", "⚔️🛡️"));
            
            // Act
            Manager.RegisterEntity(entity, "Equipment")
                .WithMetadata(metadata)
                .Complete();

            // Assert
            var retrieved = Manager.GetMetadata("entity_1");
            Assert.AreEqual(3, retrieved.Count);
            Assert.IsTrue(retrieved.Any(e => e.StringValue == "魔法剑"));
        }

        #endregion

        #region 统计测试

        [Test]
        public void Statistics_IncludesMetadataInMemoryUsage()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            var metadata = TestDataGenerator.CreateTestMetadata(5);
            Manager.RegisterEntity(entity, "Equipment")
                .WithMetadata(metadata)
                .Complete();

            // Act
            var stats = Manager.GetStatistics();

            // Assert
            Assert.Greater(stats.MemoryUsageBytes, 0);
        }

        #endregion
    }
}
