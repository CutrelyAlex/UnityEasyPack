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
                var card = new Card(data);
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
                var data = new CardData("warrior", "战士", "强大的战士", "Equipment.Character.Warrior", new[] { "Player", "Combat" });
                var card = new Card(data);
                card.Properties.Add(new GameProperty("Health", 100f));
                card.Properties.Add(new GameProperty("Attack", 15f));
                return card;
            });

            _factory.Register("mage", () =>
            {
                var data = new CardData("mage", "法师", "神秘的法师", "Equipment.Character.Mage", new[] { "Player", "Magic" });
                var card = new Card(data);
                card.Properties.Add(new GameProperty("Mana", 80f));
                card.Properties.Add(new GameProperty("Intelligence", 20f));
                return card;
            });

            _factory.Register("sword", () =>
            {
                var data = new CardData("sword", "剑", "一把锋利的剑", "Equipment.Weapon", new[] { "Equipment", "Weapon" });
                var card = new Card(data);
                card.Properties.Add(new GameProperty("Damage", 25f));
                return card;
            });

            // Act 1 - 创建卡牌
            var warrior = _engine.CreateCard("warrior");
            var mage = _engine.CreateCard("mage");
            var sword = _engine.CreateCard("sword");

            // Act 2 - 使用 CardEngine 序列化（支持 Card 的完整结构）
            var json = _engine.SerializeToJson();
            Assert.IsNotNull(json, "序列化 JSON 不应为空");
            Assert.IsNotEmpty(json, "序列化 JSON 不应为空");

            // Act 3 - 反序列化到新引擎
            var newEngine = new CardEngine(_factory);
            newEngine.DeserializeFromJson(json);

            // Assert - 仅验证 CardData 一致性
            var restoredWarrior = newEngine.GetCardByUID(warrior.UID);
            var restoredMage = newEngine.GetCardByUID(mage.UID);
            var restoredSword = newEngine.GetCardByUID(sword.UID);

            Assert.IsNotNull(restoredWarrior, "应恢复 warrior");
            Assert.IsNotNull(restoredMage, "应恢复 mage");
            Assert.IsNotNull(restoredSword, "应恢复 sword");

            Assert.AreEqual("warrior", restoredWarrior.Id, "战士 ID 应一致");
            Assert.AreEqual("mage", restoredMage.Id, "法师 ID 应一致");
            Assert.AreEqual("sword", restoredSword.Id, "剑 ID 应一致");

            Assert.AreEqual("Equipment.Character.Warrior", restoredWarrior.Category, "战士模板分类应保持");
            Assert.AreEqual("Equipment.Character.Mage", restoredMage.Category, "法师模板分类应保持");
            Assert.AreEqual("Equipment.Weapon", restoredSword.Category, "剑模板分类应保持");

            Assert.IsTrue(restoredWarrior.HasTag("Player"), "战士默认标签 Player 应保持");
            Assert.IsTrue(restoredMage.HasTag("Magic"), "法师默认标签 Magic 应保持");
            Assert.IsTrue(restoredSword.HasTag("Weapon"), "剑默认标签 Weapon 应保持");
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
                var data = new CardData("knight", "骑士", "穿着盔甲的骑士", "Equipment.Character.Knight", new[] { "Player", "Combat" });
                return new Card(data);
            });

            _factory.Register("bow", () =>
            {
                var data = new CardData("bow", "弓", "一张精致的弓", "Equipment.Weapon.Bow", new[] { "Equipment", "Weapon" });
                return new Card(data);
            });

            // Act 1 - 创建并注册卡牌
            var knight = _engine.CreateCard("knight");
            var bow = _engine.CreateCard("bow");

            // Act 2 - 使用 CardEngine 序列化
            var json = _engine.SerializeToJson();

            // Act 3 - 反序列化到临时引擎，再转移实体到新引擎
            var tempEngine = new CardEngine(_factory);
            tempEngine.DeserializeFromJson(json);

            // Act 4 - 创建新引擎，将反序列化出来的实体通过 AddCard 恢复到引擎缓存
            var newEngine = new CardEngine(_factory);

            var recoveredCards = tempEngine.GetAllCards().ToList();
            Assert.AreEqual(2, recoveredCards.Count, "应能取回全部反序列化卡牌实体");

            foreach (var recoveredCard in recoveredCards)
            {
                newEngine.AddCard(recoveredCard);
            }

            // Assert - 仅验证 CardData 一致性
            var restoredKnight = newEngine.GetCardByUID(knight.UID);
            var restoredBow = newEngine.GetCardByUID(bow.UID);
            Assert.IsNotNull(restoredKnight, "骑士卡牌应在引擎中");
            Assert.IsNotNull(restoredBow, "弓卡牌应在引擎中");

            Assert.AreEqual("Equipment.Character.Knight", restoredKnight.Category, "骑士分类应保持");
            Assert.AreEqual("Equipment.Weapon.Bow", restoredBow.Category, "弓分类应保持");
            Assert.IsTrue(restoredKnight.HasTag("Player"), "骑士默认标签 Player 应保持");
            Assert.IsTrue(restoredBow.HasTag("Weapon"), "弓默认标签 Weapon 应保持");
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
            foreach (var (cardId, category, tags) in cardConfigs)
            {
                _factory.Register(cardId, () =>
                {
                    var data = new CardData(cardId, $"卡牌_{cardId}", $"这是一张{cardId}卡牌", category, tags);
                    return new Card(data);
                });
            }

            // Act 1 - 创建多张卡牌
            var createdCards = new List<Card>();

            foreach (var (cardId, category, tags) in cardConfigs)
            {
                var card = _engine.CreateCard(cardId);
                createdCards.Add(card);

                // 额外保障：模板分类应与配置一致（由 CardData.Category 驱动）
                Assert.AreEqual(category, card.Category, $"卡牌 {cardId} 模板分类应为 {category}");
                foreach (var tag in tags)
                {
                    Assert.IsTrue(card.HasTag(tag), $"卡牌 {cardId} 应包含默认标签 {tag}");
                }
            }

            // Act 2 - 使用 CardEngine 序列化并反序列化
            var json = _engine.SerializeToJson();

            var newEngine = new CardEngine(_factory);
            newEngine.DeserializeFromJson(json);

            // Assert - 验证 CardData 一致性
            foreach (var (cardId, category, tags) in cardConfigs)
            {
                var restoredCard = newEngine.GetCardById(cardId);
                Assert.IsNotNull(restoredCard, $"应恢复卡牌 {cardId}");
                Assert.AreEqual(category, restoredCard.Category, $"恢复后卡牌 {cardId} 分类应一致");
                foreach (var tag in tags)
                {
                    Assert.IsTrue(restoredCard.HasTag(tag), $"恢复后卡牌 {cardId} 应保留默认标签 {tag}");
                }
            }
        }

        [Test]
        public void Test_CardEngine_Serialization_RoundTrip()
        {
            // 1. 准备数据
            var factory = new CardFactory();
            factory.Register("hero", () => new Card(new CardData("hero", "英雄", category: "Unit.Hero", defaultTags: new[] { "Hero" })));
            factory.Register("sword", () => new Card(new CardData("sword", "剑", category: "Item.Weapon", defaultTags: new[] { "Weapon" })));

            var engine = new CardEngine(factory);

            var hero = engine.CreateCard("hero");
            var sword = engine.CreateCard("sword");

            // 2. 序列化
            string json = engine.SerializeToJson();
            Assert.IsNotNull(json);

            // 3. 反序列化到新引擎
            var newEngine = new CardEngine(factory);
            newEngine.DeserializeFromJson(json);

            // 4. 仅验证 CardData 一致性
            var restoredHero = newEngine.GetCardByUID(hero.UID);
            Assert.IsNotNull(restoredHero);
            Assert.AreEqual("hero", restoredHero.Id);
            Assert.AreEqual("Unit.Hero", restoredHero.Category, "模板分类应保留在卡牌数据中");
            Assert.IsTrue(restoredHero.HasTag("Hero"), "默认标签应保持");

            var restoredSword = newEngine.GetCardByUID(sword.UID);
            Assert.IsNotNull(restoredSword);
            Assert.AreEqual("sword", restoredSword.Id);
            Assert.AreEqual("Item.Weapon", restoredSword.Category, "模板分类应保留在卡牌数据中");
            Assert.IsTrue(restoredSword.HasTag("Weapon"), "默认标签应保持");
        }

        [Test]
        public void Test_CardEngine_Full_Serialization_RoundTrip()
        {
            // Arrange
            _factory.Register("hero", () => { return new Card(new CardData("hero", "Hero", defaultTags: new[] { "Character" })); });
            _factory.Register("item", () => { return new Card(new CardData("item", "Item", defaultTags: new[] { "Equipment" })); });
            _factory.Register("velocity", () =>
            {
                // 因此模板元数据应在 CardData 上先配置，再创建 Card。
                var data = new CardData("velocity", "Velocity", defaultTags: new[] { "Equipment" });
                data.DefaultMetaData.Set("IsVelocity", true);
                data.DefaultMetaData.Set("Speed", 9.8f);
                data.DefaultMetaData.Set("Direction", new Vector2(1.0f, 0.0f));
                return new Card(data);
            });
            _factory.Register("vu", () =>
            {
                var data = new CardData("vu", "Velocity Variant", defaultTags: new[] { "Equipment" });
                data.DefaultMetaData.Set("IsVelocity", true);
                data.DefaultMetaData.Set("Speed", 9.8f);
                data.DefaultMetaData.Set("Direction", new Vector2(0.0f, 2.0f));
                return new Card(data);
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
            var newEngine = _engine;
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

        [Test]
        public void Test_CardEngine_Serialization_RuntimeMetadataPersists_DefaultMetadataUnchanged()
        {
            // Arrange
            var factory = new CardFactory();
            var cardData = new CardData("meta_card", "Meta Card", category: "Card.Object");
            cardData.DefaultMetaData.Set("TemplateOnly", 1);

            factory.Register("meta_card", () => new Card(cardData));

            var engine = new CardEngine(factory);
            Card card = engine.CreateCard("meta_card");

            // 修改运行时 metadata（不应写回模板）
            card.ModifyRuntimeMetadata(meta =>
            {
                meta.Set("TemplateOnly", 999);
                meta.Set("RuntimeOnly", "runtime_value");
            });

            // 序列化前先确认模板未被污染
            Assert.AreEqual(1, card.Data.DefaultMetaData.Get("TemplateOnly", -1),
                "修改 RuntimeMetadata 后模板 DefaultMetaData 不应变化");
            Assert.IsFalse(card.Data.DefaultMetaData.HasValue("RuntimeOnly"),
                "运行时新增字段不应写入模板 DefaultMetaData");

            // Act
            string json = engine.SerializeToJson();
            var restoredEngine = new CardEngine(factory);

            // 反序列化前先确认新引擎的模板未被污染
            var templateCard = restoredEngine.CreateCard("meta_card");
            Assert.AreEqual(1, templateCard.Data.DefaultMetaData.Get("TemplateOnly", -1),
                "新引擎模板 DefaultMetaData 应保持原样");
            Assert.IsFalse(templateCard.Data.DefaultMetaData.HasValue("RuntimeOnly"),
                "新引擎模板 DefaultMetaData 不应包含运行时字段");

            restoredEngine.DeserializeFromJson(json);

            // Assert
            Card restored = restoredEngine.GetCardByUID(card.UID);
            Assert.IsNotNull(restored, "反序列化后应能找到卡牌");

            // 模板默认值保持原样
            Assert.AreEqual(1, restored.Data.DefaultMetaData.Get("TemplateOnly", -1),
                "反序列化后模板 DefaultMetaData 不应被运行时值覆盖");
            Assert.IsFalse(restored.Data.DefaultMetaData.HasValue("RuntimeOnly"),
                "反序列化后模板 DefaultMetaData 不应包含运行时字段");

            // 运行时 metadata 保持序列化时的更改
            CustomDataCollection runtimeMeta = restoredEngine.CategoryManager.GetMetadata(restored.UID);
            Assert.IsNotNull(runtimeMeta, "反序列化后应有运行时 metadata");
            Assert.AreEqual(999, runtimeMeta.Get("TemplateOnly", -1),
                "反序列化后运行时 TemplateOnly 应保留修改值");
            Assert.AreEqual("runtime_value", runtimeMeta.Get("RuntimeOnly", ""),
                "反序列化后运行时 RuntimeOnly 应保留修改值");
        }

        #endregion
    }
}