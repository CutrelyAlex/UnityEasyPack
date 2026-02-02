using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using EasyPack.Category;

namespace EasyPack.CategoryTests
{
    /// <summary>
    ///     CategoryManager 分类操作测试
    ///     测试分类的CRUD、通配符模式、层级关系和重命名
    /// </summary>
    [TestFixture]
    public class CategoryOperationTests : CategoryTestBase
    {
        #region 分类创建和结构测试

        [Test]
        public void RegisterEntity_CreatesNewCategory_Success()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");

            // Act
            OperationResult result = RegisterEntity(entity, "Equipment");

            // Assert
            TestAssertions.AssertSuccess(result);
            var categories = Manager.GetCategoriesNodes();
            TestAssertions.AssertListContains(categories, "Equipment");
        }

        [Test]
        public void RegisterEntity_CreatesCategoryHierarchy_Success()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");

            // Act
            OperationResult result = RegisterEntity(entity, "Equipment.Weapon.Sword");

            // Assert
            TestAssertions.AssertSuccess(result);
            var categories = Manager.GetCategoriesNodes();
            TestAssertions.AssertListContains(categories, "Equipment");
            TestAssertions.AssertListContains(categories, "Equipment.Weapon");
            TestAssertions.AssertListContains(categories, "Equipment.Weapon.Sword");
        }

        [Test]
        public void RegisterEntity_DeepHierarchy_MaxDepthExceeded_Failure()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            string deepCategory = "A.B.C.D.E.F"; // 6 levels

            // Act
            OperationResult result = Manager.RegisterEntitySafe(entity, deepCategory).Complete();

            // Assert
            TestAssertions.AssertFailure(result, ErrorCode.InvalidCategory);
        }

        [Test]
        public void RegisterEntity_MaxValidDepth_Success()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            string maxCategory = "A.B.C.D.E"; // 5 levels (max)

            // Act
            OperationResult result = Manager.RegisterEntitySafe(entity, maxCategory).Complete();

            // Assert
            TestAssertions.AssertSuccess(result);
        }

        #endregion

        #region 分类查询测试

        [Test]
        public void GetByCategory_ReturnsAllCategoryNodes()
        {
            // Arrange
            var entity1 = new TestEntity("entity_1", "Entity 1");
            var entity2 = new TestEntity("entity_2", "Entity 2");
            var entity3 = new TestEntity("entity_3", "Entity 3");

            RegisterEntity(entity1, "Equipment");
            RegisterEntity(entity2, "Equipment.Weapon");
            RegisterEntity(entity3, "Armor");

            // Act
            var categories = Manager.GetCategoriesNodes();

            // Assert
            TestAssertions.AssertListCount(categories, 3);
            TestAssertions.AssertListContains(categories, "Equipment");
            TestAssertions.AssertListContains(categories, "Equipment.Weapon");
            TestAssertions.AssertListContains(categories, "Armor");
        }

        [Test]
        public void GetLeafCategories_ReturnsOnlyLeaves()
        {
            // Arrange
            var entity1 = new TestEntity("entity_1", "Entity 1");
            var entity2 = new TestEntity("entity_2", "Entity 2");
            var entity3 = new TestEntity("entity_3", "Entity 3");

            RegisterEntity(entity1, "Equipment");
            RegisterEntity(entity2, "Equipment.Weapon");
            RegisterEntity(entity3, "Equipment.Weapon.Sword");

            // Act
            var leafCategories = Manager.GetLeafCategories();

            // Assert
            TestAssertions.AssertListCount(leafCategories, 1);
            TestAssertions.AssertListContains(leafCategories, "Equipment.Weapon.Sword");
        }

        [Test]
        public void GetLeafCategories_MultipleLeaves()
        {
            // Arrange
            var entity1 = new TestEntity("entity_1", "Entity 1");
            var entity2 = new TestEntity("entity_2", "Entity 2");
            var entity3 = new TestEntity("entity_3", "Entity 3");

            RegisterEntity(entity1, "Equipment.Weapon.Sword");
            RegisterEntity(entity2, "Equipment.Armor.Helmet");
            RegisterEntity(entity3, "Consumable");

            // Act
            var leafCategories = Manager.GetLeafCategories();

            // Assert
            TestAssertions.AssertListCount(leafCategories, 3);
            TestAssertions.AssertListContains(leafCategories, "Equipment.Weapon.Sword");
            TestAssertions.AssertListContains(leafCategories, "Equipment.Armor.Helmet");
            TestAssertions.AssertListContains(leafCategories, "Consumable");
        }

        #endregion

        #region 通配符模式查询测试

        [Test]
        public void GetByCategory_Wildcard_MatchesPattern()
        {
            // Arrange
            var entity1 = new TestEntity("entity_1", "Entity 1");
            var entity2 = new TestEntity("entity_2", "Entity 2");
            var entity3 = new TestEntity("entity_3", "Entity 3");

            RegisterEntity(entity1, "Equipment.Weapon.Sword");
            RegisterEntity(entity2, "Equipment.Weapon.Bow");
            RegisterEntity(entity3, "Equipment.Armor.Helmet");

            // Act
            var result = Manager.GetByCategory("Equipment.Weapon.*");

            // Assert
            TestAssertions.AssertListCount(result, 2);
            TestAssertions.AssertListContains(result, entity1);
            TestAssertions.AssertListContains(result, entity2);
        }

        [Test]
        public void GetByCategory_Wildcard_PrefixPattern()
        {
            // Arrange
            var entity1 = new TestEntity("entity_1", "Entity 1");
            var entity2 = new TestEntity("entity_2", "Entity 2");
            var entity3 = new TestEntity("entity_3", "Entity 3");

            RegisterEntity(entity1, "Equipment.Weapon.Sword");
            RegisterEntity(entity2, "Equipment.Armor.Helmet");
            RegisterEntity(entity3, "Consumable.Potion");

            // Act
            var result = Manager.GetByCategory("Equipment.*");

            // Assert
            TestAssertions.AssertListCount(result, 2);
            TestAssertions.AssertListContains(result, entity1);
            TestAssertions.AssertListContains(result, entity2);
        }

        [Test]
        public void GetByCategory_Wildcard_SingleChar()
        {
            // Arrange
            var entity1 = new TestEntity("entity_1", "Entity 1");
            var entity2 = new TestEntity("entity_2", "Entity 2");
            var entity3 = new TestEntity("entity_3", "Entity 3");

            RegisterEntity(entity1, "Equipment.Weapon.Sword");
            RegisterEntity(entity2, "Equipment.Weapon.Bow");
            RegisterEntity(entity3, "Equipment.Armor");

            // Act
            var result = Manager.GetByCategory("Equipment.Weapon.*");

            // Assert
            TestAssertions.AssertListCount(result, 2);
        }

        [Test]
        public void GetByCategory_Wildcard_NoMatch_EmptyResult()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            RegisterEntity(entity, "Equipment.Weapon");

            // Act
            var result = Manager.GetByCategory("Armor.*");

            // Assert
            TestAssertions.AssertListEmpty(result);
        }

        [Test]
        public void GetByCategory_Wildcard_CaseSensitive()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            RegisterEntity(entity, "Equipment.Weapon");

            // Act
            var result1 = Manager.GetByCategory("Equipment.*");
            var result2 = Manager.GetByCategory("equipment.*");

            // Assert
            TestAssertions.AssertListCount(result1, 1);
            TestAssertions.AssertListEmpty(result2);
        }

        #endregion

        #region 分类删除测试

        [Test]
        public void DeleteCategory_SingleLevel_Success()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            RegisterEntity(entity, "Equipment");
            Assert.IsTrue(EntityInCategory(entity, "Equipment"));

            // Act
            OperationResult result = Manager.DeleteCategory("Equipment");

            // Assert
            TestAssertions.AssertSuccess(result);
            var retrievedEntities = Manager.GetByCategory("Equipment");
            TestAssertions.AssertListEmpty(retrievedEntities);
        }

        [Test]
        public void DeleteCategory_DoesNotDeleteChildren()
        {
            // Arrange
            var entity1 = new TestEntity("entity_1", "Entity 1");
            var entity2 = new TestEntity("entity_2", "Entity 2");
            RegisterEntity(entity1, "Equipment");
            RegisterEntity(entity2, "Equipment.Weapon");

            // Act
            OperationResult result = Manager.DeleteCategory("Equipment");

            // Assert
            TestAssertions.AssertSuccess(result);
            var retrievedEntities = Manager.GetByCategory("Equipment.Weapon");
            TestAssertions.AssertListCount(retrievedEntities, 1);
        }

        [Test]
        public void DeleteCategoryRecursive_DeletesAllDescendants()
        {
            // Arrange
            var entity1 = new TestEntity("entity_1", "Entity 1");
            var entity2 = new TestEntity("entity_2", "Entity 2");
            var entity3 = new TestEntity("entity_3", "Entity 3");
            RegisterEntity(entity1, "Equipment");
            RegisterEntity(entity2, "Equipment.Weapon");
            RegisterEntity(entity3, "Equipment.Weapon.Sword");

            // Act
            OperationResult result = Manager.DeleteCategoryRecursive("Equipment");

            // Assert
            TestAssertions.AssertSuccess(result);
            var categories = Manager.GetCategoriesNodes();
            TestAssertions.AssertListDoesNotContain(categories, "Equipment");
            TestAssertions.AssertListDoesNotContain(categories, "Equipment.Weapon");
            TestAssertions.AssertListDoesNotContain(categories, "Equipment.Weapon.Sword");
        }

        [Test]
        public void DeleteCategoryRecursive_RemovesAllEntities()
        {
            // Arrange
            var entities = TestDataGenerator.GenerateEntities(3, "entity_");
            foreach (TestEntity entity in entities)
            {
                RegisterEntity(entity, "Equipment");
            }

            // Act
            Manager.DeleteCategoryRecursive("Equipment");

            // Assert
            Statistics stats = Manager.GetStatistics();
            Assert.AreEqual(0, stats.TotalEntities);
        }

        [Test]
        public void DeleteCategory_NonExistent_Failure()
        {
            // Act
            OperationResult result = Manager.DeleteCategory("NonExistent");

            // Assert
            TestAssertions.AssertFailure(result, ErrorCode.NotFound);
        }

        #endregion

        #region 分类移动测试

        [Test]
        public void MoveEntityToCategory_ExistingEntity_Success()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            RegisterEntity(entity, "Equipment");
            Assert.IsTrue(EntityInCategory(entity, "Equipment"));

            // Act
            OperationResult result = Manager.MoveEntityToCategory("entity_1", "Armor");

            // Assert
            TestAssertions.AssertSuccess(result);
            Assert.IsFalse(EntityInCategory(entity, "Equipment"));
            Assert.IsTrue(EntityInCategory(entity, "Armor"));
        }

        [Test]
        public void MoveEntityToCategory_DeepHierarchy_Success()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            RegisterEntity(entity, "Equipment");

            // Act
            OperationResult result = Manager.MoveEntityToCategory("entity_1", "Equipment.Weapon.Sword");

            // Assert
            TestAssertions.AssertSuccess(result);
            Assert.IsTrue(EntityInCategory(entity, "Equipment.Weapon.Sword"));
        }

        [Test]
        public void MoveEntityToCategory_InvalidCategory_Failure()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            RegisterEntity(entity, "Equipment");

            // Act
            OperationResult result = Manager.MoveEntityToCategorySafe("entity_1", "A.B.C.D.E.F");

            // Assert
            TestAssertions.AssertFailure(result, ErrorCode.InvalidCategory);
        }

        [Test]
        public void MoveEntityToCategory_NonExistentEntity_Failure()
        {
            // Act
            OperationResult result = Manager.MoveEntityToCategory("nonexistent_id", "Equipment");

            // Assert
            TestAssertions.AssertFailure(result, ErrorCode.NotFound);
        }

        #endregion

        #region 分类重命名测试

        [Test]
        public void RenameCategory_SingleLevel_Success()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            RegisterEntity(entity, "Equipment");
            var oldCategories = Manager.GetCategoriesNodes();
            TestAssertions.AssertListContains(oldCategories, "Equipment");

            // Act
            OperationResult result = Manager.RenameCategory("Equipment", "Gear");

            // Assert
            TestAssertions.AssertSuccess(result);
            var newCategories = Manager.GetCategoriesNodes();
            TestAssertions.AssertListDoesNotContain(newCategories, "Equipment");
            TestAssertions.AssertListContains(newCategories, "Gear");
            Assert.IsTrue(EntityInCategory(entity, "Gear"));
        }

        [Test]
        public void RenameCategory_RecursiveRenaming()
        {
            // Arrange
            var entity1 = new TestEntity("entity_1", "Entity 1");
            var entity2 = new TestEntity("entity_2", "Entity 2");
            var entity3 = new TestEntity("entity_3", "Entity 3");
            RegisterEntity(entity1, "Equipment");
            RegisterEntity(entity2, "Equipment.Weapon");
            RegisterEntity(entity3, "Equipment.Weapon.Sword");

            // Act
            OperationResult result = Manager.RenameCategory("Equipment", "Gear");

            // Assert
            TestAssertions.AssertSuccess(result);
            var categories = Manager.GetCategoriesNodes();
            TestAssertions.AssertListContains(categories, "Gear");
            TestAssertions.AssertListContains(categories, "Gear.Weapon");
            TestAssertions.AssertListContains(categories, "Gear.Weapon.Sword");
        }

        [Test]
        public void RenameCategory_DuplicateName_Failure()
        {
            // Arrange
            var entity1 = new TestEntity("entity_1", "Entity 1");
            var entity2 = new TestEntity("entity_2", "Entity 2");
            RegisterEntity(entity1, "Equipment");
            RegisterEntity(entity2, "Armor");

            // Act
            OperationResult result = Manager.RenameCategory("Equipment", "Armor");

            // Assert
            TestAssertions.AssertFailure(result, ErrorCode.DuplicateId);
        }

        [Test]
        public void RenameCategory_NonExistent_Failure()
        {
            // Act
            OperationResult result = Manager.RenameCategory("NonExistent", "NewName");

            // Assert
            TestAssertions.AssertFailure(result, ErrorCode.NotFound);
        }

        [Test]
        public void RenameCategorySafe_InvalidNewName_Failure()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            RegisterEntity(entity, "Equipment");

            // Act
            OperationResult result = Manager.RenameCategorySafe("Equipment", "A.B.C.D.E.F");

            // Assert
            TestAssertions.AssertFailure(result, ErrorCode.InvalidCategory);
        }

        #endregion

        #region 分类正规化测试

        [Test]
        public void CategoryNormalizer_TrimsWhitespace()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");

            // Act
            OperationResult result = Manager.RegisterEntitySafe(entity, "  Equipment  ").Complete();

            // Assert
            TestAssertions.AssertSuccess(result);
            var categories = Manager.GetCategoriesNodes();
            TestAssertions.AssertListContains(categories, "Equipment");
        }

        [Test]
        public void CategoryNormalizer_EmptyAfterTrim_Fails()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");

            // Act
            OperationResult result = Manager.RegisterEntitySafe(entity, "   ").Complete();

            // Assert
            TestAssertions.AssertFailure(result, ErrorCode.InvalidCategory);
        }

        [Test]
        public void CategoryNormalizer_EmptyParts_Fails()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");

            // Act
            OperationResult result = Manager.RegisterEntitySafe(entity, "Equipment..Weapon").Complete();

            // Assert
            TestAssertions.AssertFailure(result, ErrorCode.InvalidCategory);
        }

        #endregion

        #region 分类层级关系测试

        [Test]
        public void CategoryHierarchy_CreateParentBeforeChild_AutoCreatesParent()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");

            // Act
            RegisterEntity(entity, "Equipment.Weapon.Sword");

            // Assert
            var categories = Manager.GetCategoriesNodes();
            TestAssertions.AssertListCount(categories, 3);
            TestAssertions.AssertListContains(categories, "Equipment");
            TestAssertions.AssertListContains(categories, "Equipment.Weapon");
            TestAssertions.AssertListContains(categories, "Equipment.Weapon.Sword");
        }

        [Test]
        public void GetByCategory_IncludeChildren_CorrectDepth()
        {
            // Arrange
            var entity1 = new TestEntity("entity_1", "Entity 1");
            var entity2 = new TestEntity("entity_2", "Entity 2");
            var entity3 = new TestEntity("entity_3", "Entity 3");
            var entity4 = new TestEntity("entity_4", "Entity 4");

            RegisterEntity(entity1, "Equipment");
            RegisterEntity(entity2, "Equipment.Weapon");
            RegisterEntity(entity3, "Equipment.Weapon.Sword");
            RegisterEntity(entity4, "Equipment.Armor");

            // Act
            var result = Manager.GetByCategory("Equipment.Weapon", true);

            // Assert
            TestAssertions.AssertListCount(result, 2);
            TestAssertions.AssertListContains(result, entity2);
            TestAssertions.AssertListContains(result, entity3);
        }

        #endregion

        #region 分类边界情况测试

        [Test]
        public void Category_CaseSensitive()
        {
            // Arrange
            var entity1 = new TestEntity("entity_1", "Entity 1");
            RegisterEntity(entity1, "Equipment");

            // Act
            var result1 = Manager.GetByCategory("Equipment");
            var result2 = Manager.GetByCategory("equipment");
            var result3 = Manager.GetByCategory("EQUIPMENT");

            // Assert
            TestAssertions.AssertListCount(result1, 1);
            TestAssertions.AssertListEmpty(result2);
            TestAssertions.AssertListEmpty(result3);
        }

        [Test]
        public void Category_WithSpecialCharacters_Success()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");

            // Act
            OperationResult result = RegisterEntity(entity, "Equipment-2.Weapon_v2");

            // Assert
            TestAssertions.AssertSuccess(result);
            var categories = Manager.GetCategoriesNodes();
            TestAssertions.AssertListContains(categories, "Equipment-2.Weapon_v2");
        }

        #endregion

        #region 统计测试

        [Test]
        public void Statistics_TotalCategories_Accurate()
        {
            // Arrange
            var entities = TestDataGenerator.GenerateEntities(3, "entity_");
            RegisterEntity(entities[0], "Equipment");
            RegisterEntity(entities[1], "Equipment.Weapon");
            RegisterEntity(entities[2], "Equipment.Weapon.Sword");

            // Act
            Statistics stats = Manager.GetStatistics();

            // Assert
            Assert.AreEqual(3, stats.TotalCategories);
        }

        [Test]
        public void Statistics_MaxCategoryDepth_Accurate()
        {
            // Arrange
            var entity = new TestEntity("entity_1", "Entity 1");
            RegisterEntity(entity, "A.B.C.D.E");

            // Act
            Statistics stats = Manager.GetStatistics();

            // Assert
            Assert.AreEqual(5, stats.MaxCategoryDepth);
        }

        #endregion
    }
}