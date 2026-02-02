using System;
using System.Collections.Generic;
using System.Linq;
using EasyPack.Architecture;
using EasyPack.Category;
using EasyPack.CustomData;
using EasyPack.EmeCardSystem;
using EasyPack.GamePropertySystem;
using EasyPack.Serialization;
using NUnit.Framework;
using UnityEngine;

namespace EasyPack.EmeCardTests
{
    /// <summary>
    ///     EmeCard 序列化系统单元测试
    ///     测试覆盖范围：
    ///     =============
    ///     1. 基本序列化/反序列化 - 验证 JSON 转换功能
    ///     2. 往返序列化 - 确保数据完整性
    ///     3. 循环引用检测 - 防止无限递归
    ///     4. 固有子卡保持 - 验证特殊关系
    ///     5. 可选字段默认值 - 容错性测试
    /// </summary>
    [TestFixture]
    public class CardSerializationTest
    {
        private ISerializationService _serializationService;
        private CardEngine _engine;
        private CardFactory _factory;

        [SetUp]
        public void Setup()
        {
            // 通过 EasyPack 架构初始化序列化服务
            // GamePropertyManager 会在初始化时自动注册所有序列化器
            _serializationService = EasyPackArchitecture.Instance.ResolveAsync<ISerializationService>().GetAwaiter()
                .GetResult();
            Assert.IsNotNull(_serializationService, "序列化服务应该成功解析");

            // 初始化 CardEngine 和 CardFactory
            _factory = new CardFactory();
            _engine = new CardEngine(_factory);
        }

        [TearDown]
        public void TearDown()
        {
            // 清理工作
            _engine = null;
            _factory = null;
        }

        [Test]
        public void Test_Basic_Serialize_Deserialize()
        {
            // 使用 Engine 创建卡牌以确保标签正确管理
            var factory = new CardFactory();
            factory.Register("warrior", () => 
            {
                var data = new CardData("warrior", "战士", "强者", "Card.Object", new[] { "Combat" });
                var card = new Card(data, "Player");
                card.Properties.Add(new GameProperty("Health", 100f));
                return card;
            });
            var engine = new CardEngine(factory);
            var card = engine.CreateCard("warrior");

            string json = _serializationService.SerializeToJson(card);
            Assert.IsNotNull(json, "JSON 不应为空");
            Assert.IsNotEmpty(json, "JSON 不应为空");

            // 反序列化后的卡牌需要重新注册到引擎才能使用标签
            var clone = _serializationService.DeserializeFromJson<Card>(json);
            Assert.IsNotNull(clone, "反序列化应成功");
            Assert.AreEqual("warrior", clone.Id, "ID 应匹配");
            
            // 将 clone 添加到引擎以恢复标签功能
            engine.AddCard(clone);
            // 运行时标签（extraTags，例如 Player）不再由 CardJsonSerializer 序列化；
            // 标签恢复应由 CardEngine/CategoryManager 的状态负责。
            // 这里仅验证 CardData.DefaultTags 会在 AddCard 注册时被应用。
            Assert.IsTrue(clone.HasTag("Combat"), "应包含默认标签 Combat");
            Assert.IsFalse(clone.HasTag("Player"), "不应恢复运行时标签 Player");
            Assert.IsNotNull(clone.GetProperty("Health"), "应包含 Health 属性");
        }

        [Test]
        public void Test_RoundTrip_PreservesHierarchyAndData()
        {
            var player = new Card(new("player", "玩家"));
            var sword = new Card(new("sword", "剑"));
            var shield = new Card(new("shield", "盾"));
            
            // 将所有卡牌添加到 Engine 以分配 UID 并建立管理关系
            _engine.AddCard(player);
            _engine.AddCard(sword);
            _engine.AddCard(shield);
            
            // 建立父子关系
            player.AddChild(sword);
            player.AddChild(shield, true);

            // 使用 Engine 序列化整个状态（包括子卡关系）
            string json = _engine.SerializeToJson();
            
            // 创建新的 Engine 并加载状态
            var newFactory = new CardFactory();
            newFactory.Register("player", () => new Card(new("player", "玩家")));
            newFactory.Register("sword", () => new Card(new("sword", "剑")));
            newFactory.Register("shield", () => new Card(new("shield", "盾")));
            
            CardJsonSerializer.Factory = newFactory;
            var newEngine = new CardEngine(newFactory);
            newEngine.DeserializeFromJson(json);
            
            // 通过 UID 获取恢复后的卡牌
            Card restored = newEngine.GetCardByUID(player.UID);

            Assert.IsNotNull(restored, "应该能找到恢复的卡牌");
            Assert.AreEqual(2, restored.Children.Count, "应恢复两个子卡");
            Assert.AreEqual("sword", restored.Children[0].Id, "第一子卡应为 sword");
            Assert.AreEqual("shield", restored.Children[1].Id, "第二子卡应为 shield");
            Assert.IsFalse(restored.IsIntrinsic(restored.Children[0]), "第一子卡不应为固有");
            Assert.IsTrue(restored.IsIntrinsic(restored.Children[1]), "第二子卡应为固有");
        }

        [Test]
        public void Test_CircularReference_Detected()
        {
            var a = new Card(new("A", "A"));
            var b = new Card(new("B", "B"));
            a.AddChild(b);

            // 循环引用应在 AddChild 时就被检测到并抛出异常
            Assert.Throws<InvalidOperationException>(() =>
            {
                b.AddChild(a); // 这会形成循环：A -> B -> A
            }, "添加卡牌形成循环依赖应抛出异常");

            // 验证序列化也能检测到循环引用（如果 AddChild 检测失败）
            // 这里我们先移除 B，让 A 没有循环引用
            a.RemoveChild(b);
            // 重新创建循环
            a.AddChild(b);
            Assert.Throws<InvalidOperationException>(() => { b.AddChild(a); });
        }

        [Test]
        public void Test_IntrinsicChildren_Preserved()
        {
            var p = new Card(new("p", "p"));
            var c = new Card(new("c", "c"));
            
            // 将所有卡牌添加到 Engine 以分配 UID
            _engine.AddCard(p);
            _engine.AddCard(c);
            
            // 建立父子关系
            p.AddChild(c, true);

            // 使用 Engine 序列化整个状态（包括子卡关系）
            string json = _engine.SerializeToJson();
            
            // 创建新的 Engine 并加载状态
            var newFactory = new CardFactory();
            newFactory.Register("p", () => new Card(new("p", "p")));
            newFactory.Register("c", () => new Card(new("c", "c")));
            
            CardJsonSerializer.Factory = newFactory;
            var newEngine = new CardEngine(newFactory);
            newEngine.DeserializeFromJson(json);
            
            // 通过 UID 获取恢复后的卡牌
            Card restored = newEngine.GetCardByUID(p.UID);
            
            Assert.IsNotNull(restored, "应该能找到恢复的卡牌");
            Assert.AreEqual(1, restored.Children.Count, "应该有1个子卡");
            Assert.IsTrue(restored.IsIntrinsic(restored.Children[0]), "固有子卡应保持");
        }

        [Test]
        public void Test_MissingOptionalFields_Defaults()
        {
            string json = "{\"ID\":\"x\",\"Name\":\"X\",\"Description\":\"\",\"Category\":0}";
            var card = _serializationService.DeserializeFromJson<Card>(json);
            Assert.IsNotNull(card, "应能容忍可选字段缺失");
            Assert.AreEqual(0, card.Index, "Index 默认 0");
            Assert.AreEqual(0, card.Children.Count, "Children 默认空");
            Assert.AreEqual(0, card.Tags.Count, "Tags 默认空");
        }

        #region CategoryManager 往返序列化测试

        /// <summary>
        ///     测试 CategoryManager-EmeCard 集成的序列化往返
        ///     1. 创建若干卡牌
        ///     2. 设置 Metadata、Tag 和 Category
        ///     3. 添加到 CategoryManager
        ///     4. 序列化 CategoryManager
        ///     5. 反序列化并通过 CardEngine 恢复
        /// </summary>
        [Test]
        public void Test_CategoryManager_EmeCard_RoundTrip_Serialization()
        {
            // Arrange - 注册卡牌模板
            _factory.Register("warrior", () =>
            {
                var data = new CardData("warrior", "战士", "强大的战士", "Equipment.Character.Warrior");
                var card = new Card(data, "Player", "Combat");
                card.Properties.Add(new GameProperty("Health", 100f));
                card.Properties.Add(new GameProperty("Attack", 15f));
                return card;
            });

            _factory.Register("mage", () =>
            {
                var data = new CardData("mage", "法师", "神秘的法师", "Equipment.Character.Mage");
                var card = new Card(data, "Player", "Magic");
                card.Properties.Add(new GameProperty("Mana", 80f));
                card.Properties.Add(new GameProperty("Intelligence", 20f));
                return card;
            });

            _factory.Register("sword", () =>
            {
                var data = new CardData("sword", "剑", "一把锋利的剑", "Equipment.Weapon");
                var card = new Card(data, "Equipment", "Weapon");
                card.Properties.Add(new GameProperty("Damage", 25f));
                return card;
            });

            // Act 1 - 创建卡牌
            var warrior = _engine.CreateCard("warrior");
            var mage = _engine.CreateCard("mage");
            var sword = _engine.CreateCard("sword");

            // Act 2 - 获取 CategoryManager 并注册卡牌
            var categoryManager = _engine.CategoryManager;

            // 注意：CardEngine.CreateCard 会自动把卡牌注册到 CategoryManager。
            // 这里不要再次 RegisterEntity（否则会 DuplicateId，导致分类/标签/元数据并未真正写入）。

            // 战士卡牌：写入标签与元数据
            var warriorMetadata = new CustomDataCollection
            {
                CustomDataEntry.CreateString("rarity", "common"),
                CustomDataEntry.CreateString("role", "tank")
            };

            Assert.IsTrue((bool?)categoryManager.UpdateMetadata(warrior.UID, warriorMetadata).IsSuccess, "战士元数据应写入成功");
            foreach (var tag in new[] { "player", "melee", "tank", "humanoid" })
            {
                Assert.IsTrue((bool?)categoryManager.AddTag(warrior.UID, tag).IsSuccess, $"战士标签 '{tag}' 应写入成功");
            }

            // 法师卡牌：写入标签与元数据
            var mageMetadata = new CustomDataCollection
            {
                CustomDataEntry.CreateString("rarity", "rare"),
                CustomDataEntry.CreateString("role", "ranged"),
                CustomDataEntry.CreateString("speciality", "magic")
            };

            Assert.IsTrue((bool?)categoryManager.UpdateMetadata(mage.UID, mageMetadata).IsSuccess, "法师元数据应写入成功");
            foreach (var tag in new[] { "player", "ranged", "mage", "humanoid", "magical" })
            {
                Assert.IsTrue((bool?)categoryManager.AddTag(mage.UID, tag).IsSuccess, $"法师标签 '{tag}' 应写入成功");
            }

            // 剑卡牌：写入标签与元数据
            var swordMetadata = new CustomDataCollection
            {
                CustomDataEntry.CreateString("type", "sword"),
                CustomDataEntry.CreateString("material", "steel"),
                CustomDataEntry.CreateInt("level", 5)
            };

            Assert.IsTrue((bool?)categoryManager.UpdateMetadata(sword.UID, swordMetadata).IsSuccess, "剑元数据应写入成功");
            foreach (var tag in new[] { "equipment", "weapon", "common" })
            {
                Assert.IsTrue((bool?)categoryManager.AddTag(sword.UID, tag).IsSuccess, $"剑标签 '{tag}' 应写入成功");
            }

            // 记录序列化前的状态
            var statsBefore = categoryManager.GetStatistics();
            var warriorTagsBefore = categoryManager.GetEntityTags(warrior.UID).OrderBy(t => t).ToList();
            var mageTagsBefore = categoryManager.GetEntityTags(mage.UID).OrderBy(t => t).ToList();
            var swordTagsBefore = categoryManager.GetEntityTags(sword.UID).OrderBy(t => t).ToList();

            var warriorMetadataBefore = categoryManager.GetMetadata(warrior.UID);
            var mageMetadataBefore = categoryManager.GetMetadata(mage.UID);
            var swordMetadataBefore = categoryManager.GetMetadata(sword.UID);

            Debug.Log($"[BEFORE SERIALIZATION] Entities: {statsBefore.TotalEntities}, Categories: {statsBefore.TotalCategories}, Tags: {statsBefore.TotalTags}");

            // Act 3 - 使用 CardEngine 序列化（支持 Card 的完整结构）
            var json = _engine.SerializeToJson();
            Assert.IsNotNull(json, "序列化 JSON 不应为空");
            Assert.IsNotEmpty(json, "序列化 JSON 不应为空");
            Debug.Log($"[SERIALIZED JSON LENGTH] {json.Length}");

            // Act 4 - 清空并反序列化到新引擎
            var newEngine = new CardEngine(_factory);
            newEngine.DeserializeFromJson(json);

            var categoryManagerAfter = newEngine.CategoryManager;
            var statsClear = newEngine.CategoryManager.GetStatistics();
            Assert.AreEqual(statsBefore.TotalEntities, statsClear.TotalEntities, "反序列化后实体数量应相同");

            // Act 5 - 验证反序列化的数据
            var statsAfter = categoryManagerAfter.GetStatistics();
            Assert.AreEqual(statsBefore.TotalEntities, statsAfter.TotalEntities, "实体数量应相同");
            Assert.AreEqual(statsBefore.TotalCategories, statsAfter.TotalCategories, "分类数量应相同");
            Assert.AreEqual(statsBefore.TotalTags, statsAfter.TotalTags, "标签数量应相同");

            // Assert - 验证标签
            var warriorTagsAfter = categoryManagerAfter.GetEntityTags(warrior.UID).OrderBy(t => t).ToList();
            var mageTagsAfter = categoryManagerAfter.GetEntityTags(mage.UID).OrderBy(t => t).ToList();
            var swordTagsAfter = categoryManagerAfter.GetEntityTags(sword.UID).OrderBy(t => t).ToList();

            Assert.AreEqual(warriorTagsBefore.Count, warriorTagsAfter.Count, "战士标签数量应相同");
            Assert.AreEqual(mageTagsBefore.Count, mageTagsAfter.Count, "法师标签数量应相同");
            Assert.AreEqual(swordTagsBefore.Count, swordTagsAfter.Count, "剑标签数量应相同");

            for (int i = 0; i < warriorTagsBefore.Count; i++)
            {
                Assert.AreEqual(warriorTagsBefore[i], warriorTagsAfter[i], $"战士标签 {i} 应匹配");
            }

            // Assert - 验证元数据
            var warriorMetadataAfter = categoryManagerAfter.GetMetadata(warrior.UID);
            var mageMetadataAfter = categoryManagerAfter.GetMetadata(mage.UID);
            var swordMetadataAfter = categoryManagerAfter.GetMetadata(sword.UID);

            Assert.AreEqual(warriorMetadataBefore.Count, warriorMetadataAfter.Count, "战士元数据数量应相同");
            Assert.AreEqual(mageMetadataBefore.Count, mageMetadataAfter.Count, "法师元数据数量应相同");
            Assert.AreEqual(swordMetadataBefore.Count, swordMetadataAfter.Count, "剑元数据数量应相同");

            // Assert - 验证分类查询
            var characters = categoryManagerAfter.GetByCategory("Equipment.Character", includeChildren: true);
            Assert.AreEqual(2, characters.Count, "Equipment.Character 应包含 2 个卡牌");

            var weapons = categoryManagerAfter.GetByCategory("Equipment.Weapon");
            Assert.AreEqual(1, weapons.Count, "Equipment.Weapon 应包含 1 个卡牌");

            // Assert - 验证标签查询
            var players = categoryManagerAfter.GetByTag("player");
            Assert.AreEqual(2, players.Count, "player 标签应有 2 个卡牌");

            var equipment = categoryManagerAfter.GetByTag("equipment");
            Assert.AreEqual(1, equipment.Count, "equipment 标签应有 1 个卡牌");

            Debug.Log("[TEST PASSED] CategoryManager-EmeCard 序列化往返测试通过");
        }

        /// <summary>
        ///     测试通过 CardEngine.AddCard 恢复卡牌到引擎的完整流程
        /// </summary>
        [Test]
        public void Test_CategoryManager_EmeCard_Recovery_Via_Engine()
        {
            // Arrange - 注册卡牌模板
            _factory.Register("knight", () =>
            {
                var data = new CardData("knight", "骑士", "穿着盔甲的骑士", "Equipment.Character.Knight");
                return new Card(data, "Player", "Combat");
            });

            _factory.Register("bow", () =>
            {
                var data = new CardData("bow", "弓", "一张精致的弓", "Equipment.Weapon.Bow");
                return new Card(data, "Equipment", "Weapon");
            });

            // Act 1 - 创建并注册卡牌
            var knight = _engine.CreateCard("knight");
            var bow = _engine.CreateCard("bow");

            var categoryManager = _engine.CategoryManager as CategoryManager<Card, long>;
            Assert.IsNotNull(categoryManager, "CategoryManager 应为 CategoryManager<Card, long> 类型");

            var knightMetadata = new CustomDataCollection
            {
                CustomDataEntry.CreateString("armor", "plate")
            };

            Assert.IsTrue(categoryManager.UpdateMetadata(knight.UID, knightMetadata).IsSuccess, "骑士元数据应写入成功");
            foreach (var tag in new[] { "player", "warrior", "noble" })
            {
                Assert.IsTrue(categoryManager.AddTag(knight.UID, tag).IsSuccess, $"骑士标签 '{tag}' 应写入成功");
            }

            foreach (var tag in new[] { "equipment", "weapon", "ranged" })
            {
                Assert.IsTrue(categoryManager.AddTag(bow.UID, tag).IsSuccess, $"弓标签 '{tag}' 应写入成功");
            }

            var statsBefore = categoryManager.GetStatistics();
            var knightTagsBefore = categoryManager.GetEntityTags(knight.UID).OrderBy(t => t).ToList();

            // Act 2 - 使用 CardEngine 序列化
            var json = _engine.SerializeToJson();

            // Act 3 - 反序列化到新引擎获取 CategoryManager（作为恢复数据源）
            var tempEngine = new CardEngine(_factory);
            tempEngine.DeserializeFromJson(json);
            var recoveredManager = tempEngine.CategoryManager as CategoryManager<Card, long>;
            Assert.IsNotNull(recoveredManager, "反序列化得到的 CategoryManager 不应为空");

            var statsAfter = recoveredManager.GetStatistics();
            Assert.AreEqual(statsBefore.TotalEntities, statsAfter.TotalEntities, "实体数量应相同");

            // Act 4 - 创建新引擎，将反序列化出来的实体通过 AddCard 恢复到引擎缓存
            // 重要：不要先把 JSON Load 到 newEngine.CategoryManager，否则 AddCard 会二次 RegisterEntity 导致 DuplicateId 警告。
            var newEngine = new CardEngine(_factory);
            var newEngineMgr = newEngine.CategoryManager;

            var recoveredCards = recoveredManager.GetAllEntities();
            Assert.AreEqual(statsBefore.TotalEntities, recoveredCards.Count, "应能取回全部反序列化卡牌实体");

            foreach (var recoveredCard in recoveredCards)
            {
                newEngine.AddCard(recoveredCard);

                // CategoryManager 序列化里有元数据，需要手动同步到新引擎的 CategoryManager
                var meta = recoveredManager.GetMetadata(recoveredCard.UID);
                Assert.IsTrue(newEngineMgr.UpdateMetadata(recoveredCard.UID, meta).IsSuccess,
                    $"恢复卡牌 UID={recoveredCard.UID} 的元数据应成功");

                // CategoryManager 的运行时标签同样需要同步到新引擎的 CategoryManager。
                // 注意：CardJsonSerializer 不再保存运行时 Tags；此处是“通过 Manager 状态恢复”。
                foreach (var tag in recoveredManager.GetEntityTags(recoveredCard.UID))
                {
                    Assert.IsTrue(newEngineMgr.AddTag(recoveredCard.UID, tag).IsSuccess,
                        $"恢复卡牌 UID={recoveredCard.UID} 的标签 '{tag}' 应成功");
                }
            }

            // Assert - 验证卡牌已添加到引擎
            Assert.IsTrue(newEngine.HasCard(newEngine.GetCardByUID(knight.UID)), "骑士卡牌应在引擎中");
            Assert.IsTrue(newEngine.HasCard(newEngine.GetCardByUID(bow.UID)), "弓卡牌应在引擎中");

            // Assert - 验证标签恢复
            var expectedKnightTags = recoveredManager.GetEntityTags(knight.UID).OrderBy(t => t).ToList();
            var knightTagsAfter = newEngineMgr.GetEntityTags(knight.UID).OrderBy(t => t).ToList();
            Assert.AreEqual(knightTagsBefore.Count, knightTagsAfter.Count, "标签数量应相同");

            for (int i = 0; i < knightTagsBefore.Count; i++)
            {
                Assert.AreEqual(knightTagsBefore[i], knightTagsAfter[i], $"标签 {i} 应匹配");
            }

            // 额外校验：反序列化出的标签与新引擎内一致
            Assert.AreEqual(expectedKnightTags.Count, knightTagsAfter.Count, "恢复后标签数量应与反序列化一致");

            Debug.Log("[TEST PASSED] CategoryManager-EmeCard 通过 Engine 恢复测试通过");
        }

        /// <summary>
        ///     测试复杂场景：多卡牌、多分类层级、各种标签和元数据组合
        /// </summary>
        [Test]
        public void Test_CategoryManager_EmeCard_ComplexScenario()
        {
            // Arrange - 定义复杂场景配置（分类深度 ≤ 5）
            var cardConfigs = new[]
            {
                ("hero_1", "Character.Warrior.Knight", new[] { "hero", "warrior", "rare", "male" }),
                ("hero_2", "Character.Mage.Wizard", new[] { "hero", "mage", "epic", "female" }),
                ("hero_3", "Character.Rogue.Assassin", new[] { "hero", "rogue", "legendary", "male" }),
                ("item_1", "Equipment.Weapon.Sword", new[] { "item", "weapon", "common" }),
                ("item_2", "Equipment.Armor.Plate", new[] { "item", "armor", "rare" }),
                ("item_3", "Equipment.Accessory.Ring", new[] { "item", "accessory", "epic" }),
            };

            // Arrange - 注册多个卡牌模板（确保 CardData.Category 与预期分类一致，避免后续重复 RegisterEntity）
            foreach (var (cardId, category, _) in cardConfigs)
            {
                _factory.Register(cardId, () =>
                {
                    var data = new CardData(cardId, $"卡牌_{cardId}", $"这是一张{cardId}卡牌", category);
                    return new Card(data);
                });
            }

            var categoryManager = _engine.CategoryManager as CategoryManager<Card, long>;
            Assert.IsNotNull(categoryManager, "CategoryManager 应为 CategoryManager<Card, long> 类型");

            // Act 1 - 创建多张卡牌，并写入标签与元数据
            var createdCards = new List<Card>();

            foreach (var (cardId, category, tags) in cardConfigs)
            {
                var card = _engine.CreateCard(cardId);
                createdCards.Add(card);

                // 为每张卡牌创建不同的元数据
                var metadata = new CustomDataCollection();
                metadata.Add(CustomDataEntry.CreateString("description", $"This is {cardId}"));
                metadata.Add(CustomDataEntry.CreateString("creator", "System"));
                if (cardId.StartsWith("hero"))
                {
                    metadata.Add(CustomDataEntry.CreateInt("level", int.Parse(cardId.Last().ToString()) * 10));
                }

                // 写入元数据
                Assert.IsTrue(categoryManager.UpdateMetadata(card.UID, metadata).IsSuccess,
                    $"写入元数据应成功: {cardId}");

                // 写入标签
                foreach (var tag in tags)
                {
                    Assert.IsTrue(categoryManager.AddTag(card.UID, tag).IsSuccess,
                        $"写入标签应成功: {cardId} tag={tag}");
                }

                // 额外保障：分类应与配置一致（由 CardData.Category 驱动）
                Assert.IsTrue(categoryManager.IsInCategory(card.UID, category, includeChildren: false),
                    $"卡牌 {cardId} 应位于分类 {category}");
            }

            // Act 2 - 记录序列化前的状态
            var statsBefore = categoryManager.GetStatistics();
            var allCategoriesBefore = categoryManager.GetCategoriesNodes().OrderBy(c => c).ToList();
            var tagStatsBefore = new Dictionary<string, int>();
            var allTags = new HashSet<string>();
            foreach (var card in createdCards)
            {
                foreach (var tag in categoryManager.GetEntityTags(card.UID))
                {
                    allTags.Add(tag);
                }
            }
            foreach (var tag in allTags)
            {
                tagStatsBefore[tag] = categoryManager.GetByTag(tag).Count;
            }

            Debug.Log($"[COMPLEX] Before: {statsBefore.TotalEntities} entities, {statsBefore.TotalCategories} categories, {statsBefore.TotalTags} tags");

            // Act 3 - 使用 CardEngine 序列化并反序列化
            var json = _engine.SerializeToJson();
            
            var newEngine = new CardEngine(_factory);
            newEngine.DeserializeFromJson(json);
            
            categoryManager = newEngine.CategoryManager as CategoryManager<Card, long>;
            Assert.IsNotNull(categoryManager, "反序列化后的 CategoryManager 不应为空");

            // Assert - 验证数据完整性
            var statsAfter = categoryManager.GetStatistics();
            Assert.AreEqual(statsBefore.TotalEntities, statsAfter.TotalEntities, "实体数量应相同");
            Assert.AreEqual(statsBefore.TotalCategories, statsAfter.TotalCategories, "分类数量应相同");
            Assert.AreEqual(statsBefore.TotalTags, statsAfter.TotalTags, "标签数量应相同");

            // Assert - 验证所有分类
            var allCategoriesAfter = categoryManager.GetCategoriesNodes().OrderBy(c => c).ToList();
            Assert.AreEqual(allCategoriesBefore.Count, allCategoriesAfter.Count, "分类数量应相同");

            // Assert - 验证所有标签统计
            foreach (var tag in tagStatsBefore.Keys)
            {
                var countBefore = tagStatsBefore[tag];
                var countAfter = categoryManager.GetByTag(tag).Count;
                Assert.AreEqual(countBefore, countAfter, $"标签 '{tag}' 的卡牌数应相同");
            }

            // Assert - 验证层级分类查询
            var characters = categoryManager.GetByCategory("Character", includeChildren: true);
            Assert.AreEqual(3, characters.Count, "Character 应包含 3 个卡牌");

            var equipment = categoryManager.GetByCategory("Equipment", includeChildren: true);
            Assert.AreEqual(3, equipment.Count, "Equipment 应包含 3 个卡牌");

            // 注意：这里的卡牌通常注册在更细分的子分类（例如 Equipment.Weapon.Sword），
            // 因此查询父分类时需要 includeChildren=true。
            var weapons = categoryManager.GetByCategory("Equipment.Weapon", includeChildren: true);
            Assert.AreEqual(1, weapons.Count, "Equipment.Weapon（含子分类）应包含 1 个卡牌");

            // Assert - 验证多标签查询
            var heroItems = categoryManager.GetByTags(new[] { "hero" }, matchAll: false);
            Assert.AreEqual(3, heroItems.Count, "hero 标签应有 3 个卡牌");

            var rareItems = categoryManager.GetByTags(new[] { "rare" }, matchAll: false);
            Assert.AreEqual(2, rareItems.Count, "rare 标签应有 2 个卡牌");

            Debug.Log("[TEST PASSED] 复杂场景序列化往返测试通过");
        }

        [Test]
        public void Test_CardEngine_Serialization_RoundTrip()
        {
            // 1. 准备数据
            var factory = new CardFactory();
            // 注意：CardEngine.AddCard 会按 CardData.Category 自动注册到 CategoryManager。
            // 因此这里必须在模板层就设置好 category，避免后续重复注册导致 DuplicateId。
            factory.Register("hero", () => new Card(new CardData("hero", "英雄", category: "Unit.Hero")));
            factory.Register("sword", () => new Card(new CardData("sword", "剑", category: "Item.Weapon")));
            
            var engine = new CardEngine(factory);
            
            var hero = engine.CreateCard("hero");
            var sword = engine.CreateCard("sword");
            
            // 设置标签（分类已在 AddCard 时完成注册）
            engine.CategoryManager.AddTag(hero.UID, "Legendary");
            engine.CategoryManager.AddTag(sword.UID, "Sharp");
            
            // 设置元数据
            var metadata = new CustomDataCollection();
            metadata.Set("Level", 10);
            engine.CategoryManager.UpdateMetadata(hero.UID, metadata);

            // 2. 序列化
            string json = engine.SerializeToJson();
            Assert.IsNotNull(json);

            // 3. 反序列化到新引擎
            var newEngine = new CardEngine(factory);
            newEngine.DeserializeFromJson(json);

            // 4. 验证
            Assert.AreEqual(2, newEngine.CategoryManager.GetStatistics().TotalEntities);
            
            var restoredHero = newEngine.CategoryManager.GetById(hero.UID).Value;
            Assert.IsNotNull(restoredHero);
            Assert.AreEqual("hero", restoredHero.Id);
            Assert.IsTrue(newEngine.CategoryManager.HasTag(hero.UID, "Legendary"));
            Assert.AreEqual("Unit.Hero", newEngine.CategoryManager.GetReadableCategoryPath(hero.UID));
            Assert.AreEqual(10, newEngine.CategoryManager.GetMetadata(hero.UID).Get<int>("Level"));

            var restoredSword = newEngine.CategoryManager.GetById(sword.UID).Value;
            Assert.IsNotNull(restoredSword);
            Assert.AreEqual("sword", restoredSword.Id);
            Assert.IsTrue(newEngine.CategoryManager.HasTag(sword.UID, "Sharp"));
            Assert.AreEqual("Item.Weapon", newEngine.CategoryManager.GetReadableCategoryPath(sword.UID));
        }

        [Test]
        public void Test_CardEngine_Full_Serialization_RoundTrip()
        {
            // Arrange
            _factory.Register("hero", () => {return new Card(new CardData("hero", "Hero"), "Character");});
            _factory.Register("item", () => {return new Card(new CardData("item", "Item"), "Equipment");});
            _factory.Register("velocity", () => {
                // 注意：Card 的逻辑 ID 来自 CardData.ID。
                // 这里必须保持 CardData.ID 与工厂注册 key 一致，否则序列化/反序列化会按错误 ID 走工厂，导致静态默认数据（DefaultMetaData）无法恢复。
                var card = new Card(new CardData("velocity", "Velocity"), "Equipment");
                card.Data.DefaultMetaData.Set("IsVelocity", true);
                card.Data.DefaultMetaData.Set("Speed", 9.8f);
                card.Data.DefaultMetaData.Set("Direction", new Vector2(1.0f, 0.0f));
                return card;
            });
            _factory.RegisterVariant("velocity", "vu", card => {
                card.Data.DefaultMetaData.Set("Direction", new Vector2(0.0f, 2.0f));
            });
            var hero = _engine.CreateCard("hero");
            hero.Position = new Vector3Int(1, 0, 1);
            hero.Properties.Add(new GameProperty("HP", 100));
            
            var item = _engine.CreateCard("item");
            item.Position = new Vector3Int(2, 0, 2);
            
            var vu = _engine.CreateCard("vu");
            var vel = _engine.CreateCard("velocity");

            // Add metadata via CategoryManager
            var metadata = new CustomDataCollection();
            metadata.Set("Level", 5);
            _engine.CategoryManager.UpdateMetadata(hero.UID, metadata);

            // Act
            string json = _engine.SerializeToJson();
            Assert.IsNotEmpty(json);
            
            _engine.ClearAllCards();
            var newEngine =_engine;
            newEngine.DeserializeFromJson(json);

            // Assert
            // 1. Verify Cards exist
            var restoredHero = newEngine.GetCardByUID(hero.UID);
            var restoredItem = newEngine.GetCardByUID(item.UID);
            var restoredVu = newEngine.GetCardByUID(vu.UID);
            var restoredVel = newEngine.GetCardByUID(vel.UID);

            Assert.IsNotNull(restoredHero, "Hero should be restored");
            Assert.IsNotNull(restoredItem, "Item should be restored");
            Assert.IsNotNull(restoredVu, "VU should be restored");
            Assert.IsNotNull(restoredVel, "Velocity should be restored");

            // 2. Verify Properties
            Assert.AreEqual(100, restoredHero.GetProperty("HP").GetValue(), "HP property should be restored");

            // 3. Verify Position
            Assert.AreEqual(new Vector3Int(1, 0, 1), restoredHero.Position, "Hero position should be restored");
            Assert.AreEqual(new Vector3Int(2, 0, 2), restoredItem.Position, "Item position should be restored");
            
            // 4. Verify Engine Caches
            Assert.AreEqual(restoredHero, newEngine.GetCardByPosition(new Vector3Int(1, 0, 1)), "Engine position cache should work");
            
            // 5. Verify Tags
            Assert.IsTrue(restoredHero.HasTag("Character"), "Hero tag should be restored");
            
            // 6. Verify Metadata
            var restoredMetadata = newEngine.CategoryManager.GetMetadata(restoredHero.UID);
            Assert.IsNotNull(restoredMetadata, "Metadata should be restored");
            Assert.AreEqual(5, restoredMetadata.Get<int>("Level"), "Metadata value should be correct");

            // 7. Verify MetaDataDirection
            Assert.AreEqual(new Vector2(0.0f, 2.0f), restoredVu.Data.DefaultMetaData.Get<Vector2>("Direction"), "VU Direction should be restored");
            Assert.AreEqual(new Vector2(1.0f, 0.0f), restoredVel.Data.DefaultMetaData.Get<Vector2>("Direction"), "Velocity Direction should be restored");

            // 8. Verify MetaData in CategoryManger
            var velMetadata = newEngine.CategoryManager.GetMetadata(restoredVel.UID);
            Assert.IsNotNull(velMetadata, "Velocity Metadata should be restored");
            Assert.IsTrue(velMetadata.Get<bool>("IsVelocity"), "Velocity IsVelocity metadata should be true");
            Assert.AreEqual(9.8f, velMetadata.Get<float>("Speed"), "Velocity Speed metadata should be correct");
            var vuMetadata = newEngine.CategoryManager.GetMetadata(restoredVu.UID);
            Assert.IsNotNull(vuMetadata, "VU Metadata should be restored");
            Assert.IsTrue(vuMetadata.Get<bool>("IsVelocity"), "VU IsVelocity metadata should be true");
            Assert.AreEqual(9.8f, vuMetadata.Get<float>("Speed"), "VU Speed metadata should be correct");
            Assert.AreEqual(new Vector2(0.0f, 2.0f), vuMetadata.Get<Vector2>("Direction"), "VU Direction metadata should be correct");
        }

        #endregion
    }
}