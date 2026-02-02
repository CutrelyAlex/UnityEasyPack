using NUnit.Framework;
using System;
using System.Collections.Generic;
using EasyPack.Category;
using EasyPack.CustomData;

namespace EasyPack.CategoryTests
{
    /// <summary>
    /// CategoryManager Metadata CustomDataCollection 特性测试
    /// 测试 metadata 对新增 CustomDataCollection 类型的支持
    /// 验证元数据可以存储嵌套的自定义数据列表
    /// </summary>
    [TestFixture]
    public class MetadataCustomDataCollectionTests : CategoryTestBase
    {
        #region 基础 CustomDataCollection 元数据测试

        /// <summary>
        /// 测试在 metadata 中存储简单的 CustomDataCollection
        /// </summary>
        [Test]
        public void SetMetadata_WithSimpleCustomDataCollection_Success()
        {
            // Arrange
            var entity = new TestEntity("equipment_001", "Iron Sword");
            var metadata = new CustomDataCollection();
            
            // 创建一个包含物品属性的列表
            var attributesList = new CustomDataCollection
            {
                CustomDataEntry.CreateInt("damage", 25),
                CustomDataEntry.CreateInt("durability", 100),
                CustomDataEntry.CreateString("material", "Iron")
            };
            
            metadata.Add(CustomDataEntry.CreateEntryList("attributes", attributesList));
            
            // Act
            Manager.RegisterEntity(entity, "Equipment.Weapon")
                .WithMetadata(metadata)
                .Complete();
            
            var retrieved = Manager.GetMetadata(entity.Id);
            
            // Assert
            Assert.AreEqual(1, retrieved.Count);
            var entry = retrieved[0];
            Assert.AreEqual(CustomDataType.EntryList, entry.Type);
            Assert.AreEqual("attributes", entry.Key);
            Assert.AreEqual(3, entry.EntryListValue.Count);
        }

        [Test]
        public void SetMetadata_WithCustomDataList_Success()
        {
            // Arrange
            var entity = new TestEntity("equipment_001", "Iron Sword");
            var metadata = new CustomDataCollection();
            
            var dataList = CustomDataList.FromValues(10, 20, 30);

            metadata.Add(CustomDataEntry.CreateEntryList("damage_values", dataList));

            // Act
            Manager.RegisterEntity(entity, "Equipment.Weapon")
                .WithMetadata(metadata)
                .Complete();
            
            var retrieved = Manager.GetMetadata(entity.Id);

            // Assert
            Assert.AreEqual(1, retrieved.Count);
            var entry = retrieved["damage_values"];
            Assert.AreEqual(CustomDataType.EntryList, entry.Type);
            Assert.AreEqual(3, entry.EntryListValue.Count);
            Assert.AreEqual(10, entry.EntryListValue[0].GetValue());
            Assert.AreEqual(20, entry.EntryListValue[1].GetValue());
            Assert.AreEqual(30, entry.EntryListValue[2].GetValue());

            var getValue0 = entry.As<CustomDataList>().Get<int>(0);
        }

        /// <summary>
        /// 测试在 metadata 中存储多个 CustomDataCollection
        /// </summary>
        [Test]
        public void SetMetadata_WithMultipleCustomDataCollections_Success()
        {
            // Arrange
            var entity = new TestEntity("character_001", "Hero");
            var metadata = new CustomDataCollection();
            
            // 创建技能列表
            var skillsList = new CustomDataCollection
            {
                CustomDataEntry.CreateString("skill_1", "Fire Bolt"),
                CustomDataEntry.CreateString("skill_2", "Ice Storm"),
                CustomDataEntry.CreateString("skill_3", "Lightning Strike")
            };
            
            // 创建装备列表
            var equipmentList = new CustomDataCollection
            {
                CustomDataEntry.CreateString("head", "Iron Helmet"),
                CustomDataEntry.CreateString("body", "Steel Armor"),
                CustomDataEntry.CreateString("feet", "Iron Boots")
            };
            
            metadata.Add(CustomDataEntry.CreateEntryList("skills", skillsList));
            metadata.Add(CustomDataEntry.CreateEntryList("equipment", equipmentList));
            
            // Act
            Manager.RegisterEntity(entity, "Character.Hero")
                .WithMetadata(metadata)
                .Complete();
            
            var retrieved = Manager.GetMetadata(entity.Id);
            
            // Assert
            Assert.AreEqual(2, retrieved.Count);
            
            var skillsEntry = retrieved["skills"];
            Assert.AreEqual(CustomDataType.EntryList, skillsEntry.Type);
            Assert.AreEqual(3, skillsEntry.EntryListValue.Count);
            
            var equipmentEntry = retrieved["equipment"];
            Assert.AreEqual(CustomDataType.EntryList, equipmentEntry.Type);
            Assert.AreEqual(3, equipmentEntry.EntryListValue.Count);
        }

        /// <summary>
        /// 测试在 metadata 中存储混合类型的数据（CustomDataCollection + 普通类型）
        /// </summary>
        [Test]
        public void SetMetadata_WithMixedTypes_Success()
        {
            // Arrange
            var entity = new TestEntity("item_001", "Legendary Sword");
            var metadata = new CustomDataCollection
            {
                // 添加普通数据
                CustomDataEntry.CreateString("name", "Excalibur"),
                CustomDataEntry.CreateInt("rarity", 5),
                CustomDataEntry.CreateFloat("weight", 2.5f)
            };
            
            // 添加列表数据
            var modifiersList = new CustomDataCollection
            {
                CustomDataEntry.CreateString("mod_1", "+10% Damage"),
                CustomDataEntry.CreateString("mod_2", "+5 Crit Chance")
            };
            
            metadata.Add(CustomDataEntry.CreateEntryList("modifiers", modifiersList));
            
            // Act
            Manager.RegisterEntity(entity, "Items.Weapon")
                .WithMetadata(metadata)
                .Complete();
            
            var retrieved = Manager.GetMetadata(entity.Id);
            
            // Assert
            Assert.AreEqual(4, retrieved.Count);
            
            // 验证普通类型
            Assert.AreEqual("Excalibur", retrieved["name"].GetValue());
            Assert.AreEqual(5, retrieved["rarity"].GetValue());
            Assert.AreEqual(2.5f, retrieved["weight"].GetValue());
            
            // 验证列表类型
            var modifiersEntry = retrieved["modifiers"];
            Assert.AreEqual(CustomDataType.EntryList, modifiersEntry.Type);
            Assert.AreEqual(2, modifiersEntry.EntryListValue.Count);
        }

        #endregion

        #region 嵌套 CustomDataCollection 元数据测试

        /// <summary>
        /// 测试在 metadata 中存储嵌套的 CustomDataCollection（列表中包含列表）
        /// </summary>
        [Test]
        public void SetMetadata_WithNestedCustomDataCollection_Success()
        {
            // Arrange
            var entity = new TestEntity("player_001", "Player One");
            var metadata = new CustomDataCollection();
            
            // 创建嵌套结构：InventorySlots -> Items
            var inventorySlots = new CustomDataCollection();
            
            // 第一个物品的属性列表
            var item1Attrs = new CustomDataCollection
            {
                CustomDataEntry.CreateString("type", "Sword"),
                CustomDataEntry.CreateInt("quantity", 1)
            };
            
            // 第二个物品的属性列表
            var item2Attrs = new CustomDataCollection
            {
                CustomDataEntry.CreateString("type", "Potion"),
                CustomDataEntry.CreateInt("quantity", 5)
            };
            
            inventorySlots.Add(CustomDataEntry.CreateEntryList("item_1", item1Attrs));
            inventorySlots.Add(CustomDataEntry.CreateEntryList("item_2", item2Attrs));
            
            metadata.Add(CustomDataEntry.CreateEntryList("inventory", inventorySlots));
            
            // Act
            Manager.RegisterEntity(entity, "Player")
                .WithMetadata(metadata)
                .Complete();
            
            var retrieved = Manager.GetMetadata(entity.Id);
            
            // Assert
            Assert.AreEqual(1, retrieved.Count);
            var inventoryEntry = retrieved["inventory"];
            Assert.AreEqual(CustomDataType.EntryList, inventoryEntry.Type);
            Assert.AreEqual(2, inventoryEntry.EntryListValue.Count);
            
            // 验证嵌套结构
            var item1 = ((CustomDataCollection)inventoryEntry.EntryListValue)["item_1"];
            Assert.AreEqual(CustomDataType.EntryList, item1.Type);
            Assert.AreEqual("Sword", ((CustomDataCollection)item1.EntryListValue)["type"].GetValue());
            Assert.AreEqual(1, ((CustomDataCollection)item1.EntryListValue)["quantity"].GetValue());
            
            var item2 = ((CustomDataCollection)inventoryEntry.EntryListValue)["item_2"];
            Assert.AreEqual(CustomDataType.EntryList, item2.Type);
            Assert.AreEqual("Potion", ((CustomDataCollection)item2.EntryListValue)["type"].GetValue());
            Assert.AreEqual(5, ((CustomDataCollection)item2.EntryListValue)["quantity"].GetValue());
        }

        /// <summary>
        /// 测试深层嵌套（3层）的 CustomDataCollection
        /// </summary>
        [Test]
        public void SetMetadata_WithDeepNestedCustomDataCollection_Success()
        {
            // Arrange
            var entity = new TestEntity("dungeon_001", "Dungeon");
            var metadata = new CustomDataCollection();
            
            // 第3层：房间内的敌人
            var enemy1Props = new CustomDataCollection
            {
                CustomDataEntry.CreateString("name", "Goblin"),
                CustomDataEntry.CreateInt("health", 20)
            };
            
            // 第2层：房间列表
            var room1Enemies = new CustomDataCollection
            {
                CustomDataEntry.CreateEntryList("enemy_1", enemy1Props)
            };
            
            var room1 = new CustomDataCollection
            {
                CustomDataEntry.CreateString("name", "Entrance"),
                CustomDataEntry.CreateEntryList("enemies", room1Enemies)
            };
            
            // 第1层：建筑物信息
            var rooms = new CustomDataCollection
            {
                CustomDataEntry.CreateEntryList("room_1", room1)
            };
            
            metadata.Add(CustomDataEntry.CreateEntryList("structure", rooms));
            
            // Act
            Manager.RegisterEntity(entity, "Dungeon")
                .WithMetadata(metadata)
                .Complete();
            
            var retrieved = Manager.GetMetadata(entity.Id);
            
            // Assert
            Assert.AreEqual(1, retrieved.Count);
            
            var structure = (CustomDataCollection)retrieved["structure"].EntryListValue;
            var room = (CustomDataCollection)structure["room_1"].EntryListValue;
            var enemies = (CustomDataCollection)room["enemies"].EntryListValue;
            var enemy = (CustomDataCollection)enemies["enemy_1"].EntryListValue;
            
            Assert.AreEqual("Goblin", enemy["name"].GetValue());
            Assert.AreEqual(20, enemy["health"].GetValue());
        }

        #endregion

        #region 元数据更新测试

        /// <summary>
        /// 测试更新包含 CustomDataCollection 的元数据
        /// </summary>
        [Test]
        public void UpdateMetadata_WithCustomDataCollection_Success()
        {
            // Arrange
            var entity = new TestEntity("quest_001", "Quest");
            var initialMetadata = new CustomDataCollection
            {
                CustomDataEntry.CreateString("status", "Active")
            };
            
            Manager.RegisterEntity(entity, "Quest")
                .WithMetadata(initialMetadata)
                .Complete();
            
            // 创建新的元数据，包含列表
            var rewards = new CustomDataCollection
            {
                CustomDataEntry.CreateString("reward_1", "Gold x100"),
                CustomDataEntry.CreateString("reward_2", "Experience x50")
            };
            
            var newMetadata = new CustomDataCollection
            {
                CustomDataEntry.CreateString("status", "Completed"),
                CustomDataEntry.CreateEntryList("rewards", rewards)
            };
            
            // Act
            var result = Manager.UpdateMetadata(entity.Id, newMetadata);
            var updated = Manager.GetMetadata(entity.Id);
            
            // Assert
            TestAssertions.AssertSuccess(result);
            Assert.AreEqual(2, updated.Count);
            
            var rewardsEntry = updated["rewards"];
            Assert.AreEqual(CustomDataType.EntryList, rewardsEntry.Type);
            Assert.AreEqual(2, rewardsEntry.EntryListValue.Count);
        }

        /// <summary>
        /// 测试部分更新元数据中的 CustomDataCollection
        /// </summary>
        [Test]
        public void UpdateMetadata_PartialUpdateList_Success()
        {
            // Arrange
            var entity = new TestEntity("squad_001", "Squad");
            
            var members = new CustomDataCollection
            {
                CustomDataEntry.CreateString("member_1", "Alice"),
                CustomDataEntry.CreateString("member_2", "Bob"),
                CustomDataEntry.CreateString("member_3", "Charlie")
            };
            
            var metadata = new CustomDataCollection
            {
                CustomDataEntry.CreateEntryList("members", members)
            };
            
            Manager.RegisterEntity(entity, "Squad")
                .WithMetadata(metadata)
                .Complete();
            
            // 获取现有元数据，更新列表
            var current = Manager.GetMetadata(entity.Id);
            var membersList = current["members"].EntryListValue;
            
            // 添加新成员
            membersList.Add(CustomDataEntry.CreateString("member_4", "Diana"));
            
            // Act
            var result = Manager.UpdateMetadata(entity.Id, current);
            var updated = Manager.GetMetadata(entity.Id);
            
            // Assert
            TestAssertions.AssertSuccess(result);
            Assert.AreEqual(4, updated["members"].EntryListValue.Count);
        }

        #endregion

        #region 元数据序列化测试

        /// <summary>
        /// 测试包含 CustomDataCollection 的元数据的序列化
        /// </summary>
        [Test]
        public void Metadata_WithCustomDataCollection_SerializesCorrectly()
        {
            // Arrange
            var entity = new TestEntity("quest_reward_001", "Quest Reward");
            
            var itemsList = new CustomDataCollection
            {
                CustomDataEntry.CreateString("item_1", "Iron Sword"),
                CustomDataEntry.CreateInt("quantity", 1),
                CustomDataEntry.CreateFloat("weight", 3.5f)
            };
            
            var metadata = new CustomDataCollection
            {
                CustomDataEntry.CreateEntryList("items", itemsList)
            };
            
            Manager.RegisterEntity(entity, "Reward")
                .WithMetadata(metadata)
                .Complete();
            
            // Act
            var retrieved = Manager.GetMetadata(entity.Id);
            var entry = retrieved["items"];
            var serialized = entry.SerializeValue();
            
            // 反序列化验证
            var newEntry = new CustomDataEntry { Key = "items" };
            var deserializeResult = newEntry.TryDeserializeValue(serialized, CustomDataType.EntryList);
            
            // Assert
            Assert.IsTrue(deserializeResult);
            Assert.AreEqual(3, newEntry.EntryListValue.Count);
            Assert.AreEqual("Iron Sword", newEntry.EntryListValue[0].GetValue());
        }

        #endregion

        #region 元数据性能和边界情况测试

        /// <summary>
        /// 测试包含大量项目的 CustomDataCollection
        /// </summary>
        [Test]
        public void SetMetadata_WithLargeCustomDataCollection_Success()
        {
            // Arrange
            var entity = new TestEntity("achievement_001", "Achievement");
            var metadata = new CustomDataCollection();
            
            // 创建大量项目的列表
            var records = new CustomDataCollection();
            const int itemCount = 100;
            for (int i = 0; i < itemCount; i++)
            {
                records.Add(CustomDataEntry.CreateString($"record_{i:D3}", $"Value_{i}"));
            }
            
            metadata.Add(CustomDataEntry.CreateEntryList("records", records));
            
            // Act
            Manager.RegisterEntity(entity, "Achievement")
                .WithMetadata(metadata)
                .Complete();
            
            var retrieved = Manager.GetMetadata(entity.Id);
            var listEntry = retrieved["records"];
            
            // Assert
            Assert.AreEqual(itemCount, listEntry.EntryListValue.Count);
            
            // 验证随机项目
            Assert.AreEqual("Value_50", ((CustomDataCollection)listEntry.EntryListValue)["record_050"].GetValue());
            Assert.AreEqual("Value_99", ((CustomDataCollection)listEntry.EntryListValue)["record_099"].GetValue());
        }

        /// <summary>
        /// 测试空的 CustomDataCollection
        /// </summary>
        [Test]
        public void SetMetadata_WithEmptyCustomDataCollection_Success()
        {
            // Arrange
            var entity = new TestEntity("empty_001", "Empty");
            var metadata = new CustomDataCollection();
            
            // 添加空列表
            var emptyList = new CustomDataCollection();
            metadata.Add(CustomDataEntry.CreateEntryList("empty", emptyList));
            
            // Act
            Manager.RegisterEntity(entity, "Empty")
                .WithMetadata(metadata)
                .Complete();
            
            var retrieved = Manager.GetMetadata(entity.Id);
            
            // Assert
            Assert.AreEqual(1, retrieved.Count);
            Assert.AreEqual(0, retrieved["empty"].EntryListValue.Count);
        }

        /// <summary>
        /// 测试只包含 CustomDataCollection 的元数据
        /// </summary>
        [Test]
        public void SetMetadata_OnlyCustomDataCollections_Success()
        {
            // Arrange
            var entity = new TestEntity("multi_list_001", "Multi List");
            var metadata = new CustomDataCollection();
            
            // 添加多个只包含列表的元数据
            for (int i = 0; i < 3; i++)
            {
                var list = new CustomDataCollection
                {
                    CustomDataEntry.CreateString($"item_{i}_1", $"Data_{i}_1"),
                    CustomDataEntry.CreateString($"item_{i}_2", $"Data_{i}_2")
                };
                
                metadata.Add(CustomDataEntry.CreateEntryList($"list_{i}", list));
            }
            
            // Act
            Manager.RegisterEntity(entity, "MultiList")
                .WithMetadata(metadata)
                .Complete();
            
            var retrieved = Manager.GetMetadata(entity.Id);
            
            // Assert
            Assert.AreEqual(3, retrieved.Count);
            for (int i = 0; i < 3; i++)
            {
                var entry = retrieved[$"list_{i}"];
                Assert.AreEqual(CustomDataType.EntryList, entry.Type);
                Assert.AreEqual(2, entry.EntryListValue.Count);
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 注册实体到分类
        /// </summary>
        protected OperationResult RegisterEntity(TestEntity entity, string category)
        {
            return Manager.RegisterEntity(entity, category).Complete();
        }

        #endregion
    }
}
