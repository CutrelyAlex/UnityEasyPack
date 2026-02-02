using System.Collections.Generic;
using EasyPack.CustomData;
using EasyPack.EmeCardSystem;
using EasyPack.GamePropertySystem;
using EasyPack.Modifiers;
using NUnit.Framework;
using UnityEngine;

namespace EasyPack.EmeCardTests
{
    /// <summary>
    ///     EmeCard 系统测试
    ///     模仿 InventoryTest.cs 的测试模式，全面测试 EmeCard 系统的各项功能
    /// </summary>
    [TestFixture]
    public class EmeCardTest
    {
        [SetUp]
        public void Setup()
        {
            // 初始化设置
        }

        [TearDown]
        public void TearDown()
        {
            // 清理工作
        }

        [Test]
        public void Test_CardData_Creation()
        {
            // 测试基本创建
            var data1 = new CardData("warrior", "战士", "强者", "Card.Object");
            Assert.AreEqual("warrior", data1.ID, "ID 应为 warrior");
            Assert.AreEqual("战士", data1.Name, "名称应为战士");
            Assert.AreEqual("强者", data1.Description, "描述应正确");
            Assert.AreEqual("Card.Object", data1.Category, "类别应为 Object");

            // 测试默认值
            var data2 = new CardData("test");
            Assert.AreEqual("test", data2.ID, "ID 应为 test");
            Assert.AreEqual("Default", data2.Name, "默认名称应为 Default");
            Assert.AreEqual("", data2.Description, "默认描述应为空");
            Assert.AreEqual("Default", data2.Category, "默认类别应为 Default");

            // 测试带标签的创建
            string[] tags = { "武器", "近战", "铁制" };
            var data3 = new CardData("iron_sword", "铁剑", "铁制的剑", "Card.Object", tags);
            Assert.IsNotNull(data3.DefaultTags, "默认标签不应为 null");
            Assert.AreEqual(3, data3.DefaultTags.Length, $"标签数量应为 3，实际: {data3.DefaultTags.Length}");
        }

        [Test]
        public void Test_Card_BasicProperties()
        {
            var data = new CardData("potion", "药水", "恢复生命的药水", "Card.Object");
            var card = new Card(data);

            // 测试基本属性
            Assert.AreEqual("potion", card.Id, $"卡牌 ID 应为 potion，实际: {card.Id}");
            Assert.AreEqual("药水", card.Name, $"卡牌名称应为药水，实际: {card.Name}");
            Assert.AreEqual("恢复生命的药水", card.Description, "卡牌描述应正确");
            Assert.AreEqual("Card.Object", card.Category, "卡牌类别应为 Object");
            Assert.AreEqual(data, card.Data, "卡牌的 Data 应与创建时的 data 相同");

            // 测试索引
            Assert.AreEqual(-1, card.Index, $"默认索引应为 0，实际: {card.Index}");

            // 测试属性列表
            Assert.IsNotNull(card.Properties, "属性列表不应为 null");
            Assert.AreEqual(0, card.Properties.Count, $"初始属性列表应为空，实际数量: {card.Properties.Count}");

            // 测试带属性的创建
            var prop = new GameProperty("Health", 100f);
            var card2 = new Card(data, prop);
            Assert.AreEqual(1, card2.Properties.Count, $"属性数量应为 1，实际: {card2.Properties.Count}");
            Assert.IsNotNull(card2.GetProperty("Health"), "应能获取到 Health 属性");
            Assert.AreEqual(100f, card2.GetProperty("Health").GetValue(), "Health 属性值应为 100");

            // 测试多属性创建
            var props = new List<GameProperty> { new("Attack", 50f), new("Defense", 30f) };
            var card3 = new Card(data, props);
            Assert.AreEqual(2, card3.Properties.Count, $"属性数量应为 2，实际: {card3.Properties.Count}");
            Assert.IsNotNull(card3.GetProperty("Attack"), "应能获取到 Attack 属性");
            Assert.IsNotNull(card3.GetProperty("Defense"), "应能获取到 Defense 属性");
        }

        [Test]
        public void Test_Card_DefaultMetadata()
        {
            // 创建工厂和引擎
            var factory = new CardFactory();

            // 创建CardData并添加DefaultMetaData
            var cardData = new CardData("wizard", "法师", "使用魔法的角色", "Card.Object");
            cardData.DefaultMetaData.Set("Mana", 100);
            cardData.DefaultMetaData.Set("MagicResistance", 0.5f);
            cardData.DefaultMetaData.Set("SpellBook", "Basic Spells");
            cardData.DefaultMetaData.Add(CustomDataEntry.CreateInt("TestInt", 42));

            factory.Register("wizard", () => new(cardData));
            var engine = new CardEngine(factory);

            // 创建卡牌前检查DefaultMetaData
            Assert.IsNotNull(cardData.DefaultMetaData, "CardData的DefaultMetaData不应为null");
            Assert.AreEqual(4, cardData.DefaultMetaData.Count,
                $"DefaultMetaData应有4个条目，实际: {cardData.DefaultMetaData.Count}");

            // 创建并添加卡牌到引擎
            Card card = engine.CreateCard("wizard");
            Assert.IsNotNull(card, "应能创建法师卡牌");
            Assert.AreEqual("wizard", card.Id, "卡牌ID应为wizard");

            // 验证Card通过CategoryManager获取到metadata
            // 注：metadata是在RegisterToCategoryManager时通过CategoryManager应用的
            CustomDataCollection metaDataByEngine = engine.CategoryManager.GetMetadata(card.UID);

            // 检查metadata中的数据
            Assert.IsTrue(cardData.DefaultMetaData.HasValue("Mana"), "DefaultMetaData应包含Mana");
            Assert.IsTrue(cardData.DefaultMetaData.HasValue("MagicResistance"), "DefaultMetaData应包含MagicResistance");
            Assert.IsTrue(cardData.DefaultMetaData.HasValue("SpellBook"), "DefaultMetaData应包含SpellBook");
            Assert.IsTrue(cardData.DefaultMetaData.HasValue("TestInt"), "DefaultMetaData应包含TestInt");

            // 验证具体的值
            int manaValue = cardData.DefaultMetaData.Get("Mana", -1);
            int metaDataByEngineManaValue = metaDataByEngine.Get("Mana", -1);
            Assert.AreEqual(100, manaValue, $"Mana值应为100，实际: {manaValue}");
            Assert.AreEqual(100, metaDataByEngineManaValue, $"通过引擎获取的Mana值应为100，实际: {metaDataByEngineManaValue}");

            float resistanceValue = cardData.DefaultMetaData.Get("MagicResistance", -1f);
            float metaDataByEngineResistanceValue = metaDataByEngine.Get("MagicResistance", -1f);
            Assert.AreEqual(0.5f, resistanceValue, $"MagicResistance值应为0.5，实际: {resistanceValue}");
            Assert.AreEqual(0.5f, metaDataByEngineResistanceValue,
                $"通过引擎获取的MagicResistance值应为0.5，实际: {metaDataByEngineResistanceValue}");

            string spellBook = cardData.DefaultMetaData.Get("SpellBook", "");
            string metaDataByEngineSpellBook = metaDataByEngine.Get("SpellBook", "");
            Assert.AreEqual("Basic Spells", spellBook, $"SpellBook值应为'Basic Spells'，实际: '{spellBook}'");
            Assert.AreEqual("Basic Spells", metaDataByEngineSpellBook,
                $"通过引擎获取的SpellBook值应为'Basic Spells'，实际: '{metaDataByEngineSpellBook}'");

            int testIntValue = cardData.DefaultMetaData.Get("TestInt", -1);
            int metaDataByEngineTestIntValue = metaDataByEngine.Get("TestInt", -1);
            Assert.AreEqual(42, testIntValue, $"TestInt值应为42，实际: {testIntValue}");
            Assert.AreEqual(42, metaDataByEngineTestIntValue,
                $"通过引擎获取的TestInt值应为42，实际: {metaDataByEngineTestIntValue}");

            // 测试多个卡牌实例共享同一个DefaultMetaData
            var cardData2 = new CardData("mage", "法术师", "另一个法师", "Card.Object");
            cardData2.DefaultMetaData.Set("Level", 5);
            factory.Register("mage", () => new(cardData2));

            Card card2 = engine.CreateCard("mage");
            Assert.IsNotNull(card2, "应能创建法术师卡牌");
            Assert.IsTrue(cardData2.DefaultMetaData.HasValue("Level"), "cardData2的DefaultMetaData应包含Level");
            Assert.AreEqual(5, cardData2.DefaultMetaData.Get("Level", -1), "Level值应为5");
        }

        [Test]
        public void Test_Card_Tags()
        {
            // 创建工厂和引擎
            var factory = new CardFactory();
            factory.Register("sword", () => new(
                new("sword", "剑", "", "Card.Object", new[] { "武器", "近战" })));
            factory.Register("sword_extra", () => new(
                new("sword_extra", "剑", "", "Card.Object", new[] { "武器", "近战" }),
                "额外标签1", "额外标签2"));

            var engine = new CardEngine(factory);

            // 测试默认标签
            Card card = engine.CreateCard("sword");

            Assert.AreEqual(2, card.Tags.Count, $"默认标签数量应为 2，实际: {card.Tags.Count}");
            Assert.IsTrue(card.HasTag("武器"), "应有'武器'标签");
            Assert.IsTrue(card.HasTag("近战"), "应有'近战'标签");

            // 测试添加标签
            bool added1 = card.AddTag("锋利");
            Assert.IsTrue(added1, "添加新标签应返回 true");
            Assert.IsTrue(card.HasTag("锋利"), "应有'锋利'标签");
            Assert.AreEqual(3, card.Tags.Count, $"标签数量应为 3，实际: {card.Tags.Count}");

            // 测试重复添加
            bool added2 = card.AddTag("武器");
            Assert.IsFalse(added2, "重复添加标签应返回 false");
            Assert.AreEqual(3, card.Tags.Count, "标签数量应保持不变");

            // 测试移除标签
            bool removed1 = card.RemoveTag("近战");
            Assert.IsTrue(removed1, "移除存在的标签应返回 true");
            Assert.IsFalse(card.HasTag("近战"), "不应再有'近战'标签");
            Assert.AreEqual(2, card.Tags.Count, $"标签数量应为 2，实际: {card.Tags.Count}");

            // 测试移除不存在的标签
            bool removed2 = card.RemoveTag("不存在");
            Assert.IsFalse(removed2, "移除不存在的标签应返回 false");

            // 测试额外标签（构造函数）
            Card card2 = engine.CreateCard("sword_extra");
            Assert.AreEqual(4, card2.Tags.Count, $"应有 4 个标签（2默认+2额外），实际: {card2.Tags.Count}");
            Assert.IsTrue(card2.HasTag("额外标签1"), "应有额外标签1");
            Assert.IsTrue(card2.HasTag("额外标签2"), "应有额外标签2");
        }

        [Test]
        public void Test_Card_ParentChildRelationship()
        {
            // 创建工厂和引擎
            var factory = new CardFactory();
            factory.Register("player", () => new(new("player", "玩家", "", "Card.Object")));
            factory.Register("sword", () => new(new("sword", "剑", "", "Card.Object")));
            factory.Register("shield", () => new(new("shield", "盾", "", "Card.Object")));
            factory.Register("potion", () => new(new("potion", "药水", "", "Card.Object")));
            factory.Register("other", () => new(new("other", "其他", "", "Card.Object")));
            factory.Register("intrinsic", () => new(new("intrinsic", "固有", "", "Card.Object")));

            var engine = new CardEngine(factory);

            Card parent = engine.CreateCard("player");
            Card child = engine.CreateCard("sword");

            // 初始状态
            Assert.IsNull(parent.Owner, "父卡初始应无持有者");
            Assert.IsNull(child.Owner, "子卡初始应无持有者");
            Assert.AreEqual(0, parent.Children.Count, "父卡初始应无子卡");

            // 添加子卡
            parent.AddChild(child);
            Assert.AreEqual(parent, child.Owner, "子卡的持有者应为父卡");
            Assert.AreEqual(1, parent.Children.Count, $"父卡应有 1 个子卡，实际: {parent.Children.Count}");
            Assert.AreEqual(child, parent.Children[0], "父卡的第一个子卡应为 child");

            // 添加更多子卡
            Card child2 = engine.CreateCard("shield");
            Card child3 = engine.CreateCard("potion");
            parent.AddChild(child2);
            parent.AddChild(child3);
            Assert.AreEqual(3, parent.Children.Count, $"父卡应有 3 个子卡，实际: {parent.Children.Count}");

            // 移除子卡
            bool removed = parent.RemoveChild(child2);
            Assert.IsTrue(removed, "移除子卡应成功");
            Assert.IsNull(child2.Owner, "被移除的子卡应无持有者");
            Assert.AreEqual(2, parent.Children.Count, $"父卡应剩余 2 个子卡，实际: {parent.Children.Count}");

            // 测试移除不存在的子卡
            Card notChild = engine.CreateCard("other");
            bool removed2 = parent.RemoveChild(notChild);
            Assert.IsFalse(removed2, "移除不存在的子卡应失败");

            // 测试固有子卡
            Card intrinsicChild = engine.CreateCard("intrinsic");
            parent.AddChild(intrinsicChild, true);
            Assert.AreEqual(3, parent.Children.Count, "添加固有子卡后应有 3 个子卡");

            // 尝试移除固有子卡（不强制）
            bool removed3 = parent.RemoveChild(intrinsicChild);
            Assert.IsFalse(removed3, "不强制移除固有子卡应失败");
            Assert.AreEqual(3, parent.Children.Count, "固有子卡应仍存在");

            // 强制移除固有子卡
            bool removed4 = parent.RemoveChild(intrinsicChild, true);
            Assert.IsTrue(removed4, "强制移除固有子卡应成功");
            Assert.AreEqual(2, parent.Children.Count, "固有子卡应被移除");
        }

        [Test]
        public void Test_Card_Events()
        {
            var factory = new CardFactory();
            factory.Register("test_card", () => new(new("test_card", "测试卡", "", "Card.Object")));
            var engine = new CardEngine(factory);

            Card card = engine.CreateCard("test_card");

            // 测试事件订阅
            int eventCount = 0;
            string lastEventType = null;
            Card lastEventSource = null;

            card.OnEvent += (source, evt) =>
            {
                eventCount++;
                lastEventType = evt.EventType;
                lastEventSource = source;
            };

            // 测试 Tick 事件
            card.Tick(0.016f);
            Assert.AreEqual(1, eventCount, $"应触发 1 次事件，实际: {eventCount}");
            Assert.AreEqual(CardEventTypes.TICK, lastEventType, "事件类型应为 Tick");
            Assert.AreEqual(card, lastEventSource, "事件源应为卡牌自身");

            // 测试 Use 事件
            card.Use();
            Assert.AreEqual(2, eventCount, $"应触发 2 次事件，实际: {eventCount}");
            Assert.AreEqual(CardEventTypes.USE, lastEventType, "事件类型应为 Use");

            // 测试 Custom 事件
            card.RaiseEvent("test_custom", (object)null);
            Assert.AreEqual(3, eventCount, $"应触发 3 次事件，实际: {eventCount}");
            Assert.AreEqual("test_custom", lastEventType, "事件类型应为 test_custom");

            // 测试 AddedToOwner 和 RemovedFromOwner 事件
            var parent = new Card(new("parent", "父卡", "", "Card.Object"));
            parent.AddChild(card); // 应触发 AddedToOwner
            Assert.AreEqual(4, eventCount, $"添加到父卡应触发事件，实际事件数: {eventCount}");
            Assert.AreEqual(CardEventTypes.ADDED_TO_OWNER, lastEventType, "事件类型应为 AddedToOwner");

            parent.RemoveChild(card); // 应触发 RemovedFromOwner
            Assert.AreEqual(5, eventCount, $"从父卡移除应触发事件，实际事件数: {eventCount}");
            Assert.AreEqual(CardEventTypes.REMOVED_FROM_OWNER, lastEventType, "事件类型应为 RemovedFromOwner");
        }

        [Test]
        public void Test_CardFactory_Registration()
        {
            var factory = new CardFactory();

            // 注册卡牌模板
            factory.Register("sword", () => new(new("sword", "剑", "", "Card.Object"), "武器"));
            factory.Register("potion", () => new(new("potion", "药水", "", "Card.Object"), "消耗品"));

            // 使用 Engine 创建卡牌以确保标签正确管理
            var engine = new CardEngine(factory);

            // 测试创建
            Card sword = engine.CreateCard("sword");
            Assert.IsNotNull(sword, "应能创建剑");
            Assert.AreEqual("sword", sword.Id, "创建的卡牌 ID 应为 sword");
            Assert.IsTrue(sword.HasTag("武器"), "剑应有'武器'标签");

            Card potion = engine.CreateCard("potion");
            Assert.IsNotNull(potion, "应能创建药水");
            Assert.AreEqual("potion", potion.Id, "创建的卡牌 ID 应为 potion");

            // 测试创建不存在的模板
            Card unknown = engine.CreateCard("unknown");
            Assert.IsNull(unknown, "创建不存在的模板应返回 null");
        }

        [Test]
        public void Test_CardFactory_Creation()
        {
            var factory = new CardFactory();

            // 注册带属性的卡牌
            factory.Register("health_potion", () =>
            {
                var data = new CardData("health_potion", "生命药水", "恢复100生命", "Card.Object");
                var prop = new GameProperty("HealAmount", 100f);
                return new(data, prop, "药水", "消耗品");
            });

            // 使用 Engine 创建卡牌以确保标签正确管理
            var engine = new CardEngine(factory);

            Card potion = engine.CreateCard("health_potion");
            Assert.IsNotNull(potion, "应能创建生命药水");
            Assert.AreEqual(1, potion.Properties.Count, "生命药水应有 1 个属性");
            Assert.IsNotNull(potion.GetProperty("HealAmount"), "应有 HealAmount 属性");
            Assert.AreEqual(100f, potion.GetProperty("HealAmount").GetValue(), "HealAmount 应为 100");
            Assert.IsTrue(potion.HasTag("药水") && potion.HasTag("消耗品"), "应有正确的标签");

            // 测试多次创建（每次都是新实例）
            Card potion2 = engine.CreateCard("health_potion");
            Assert.IsNotNull(potion2, "应能再次创建");
            Assert.AreNotEqual(potion, potion2, "每次创建应返回新实例");
        }

        [Test]
        public void Test_CardEngine_CardManagement()
        {
            var factory = new CardFactory();
            factory.Register("test", () => new(new("test", "测试", "", "Card.Object")));

            var engine = new CardEngine(factory);

            // 测试创建并添加卡牌
            Card card1 = engine.CreateCard("test");
            Assert.IsNotNull(card1, "应能通过引擎创建卡牌");
            Assert.AreEqual("test", card1.Id, "卡牌 ID 应为 test");

            // 测试索引分配
            Card card2 = engine.CreateCard("test");
            Assert.IsNotNull(card2, "应能创建第二张同 ID 卡牌");
            Assert.AreNotEqual(card1.Index, card2.Index, "不同实例应有不同索引");

            // 测试手动添加卡牌
            var manualCard = new Card(new("manual", "手动", "", "Card.Object"));
            engine.AddCard(manualCard);
            Assert.GreaterOrEqual(manualCard.Index, 0, "手动添加的卡牌应被分配索引");

            // 测试移除卡牌
            engine.RemoveCard(card1);
            // 移除后再查询应查不到（后续测试会验证）
        }

        [Test]
        public void Test_CardEngine_CardQuery()
        {
            var factory = new CardFactory();
            factory.Register("sword", () => new(new("sword", "剑", "", "Card.Object")));
            factory.Register("shield", () => new(new("shield", "盾", "", "Card.Object")));

            var engine = new CardEngine(factory);

            Card sword1 = engine.CreateCard("sword");
            Card sword2 = engine.CreateCard("sword");
            Card shield1 = engine.CreateCard("shield");

            // 按 ID 和 Index 查询
            Card foundSword1 = engine.GetCardByKey(sword1.Id, sword1.Index);
            Assert.AreEqual(sword1, foundSword1, "应能通过 Key 找到 sword1");

            Card foundSword2 = engine.GetCardByKey(sword2.Id, sword2.Index);
            Assert.AreEqual(sword2, foundSword2, "应能通过 Key 找到 sword2");

            // 按 ID 查询所有
            var allSwords = new List<Card>(engine.GetCardsById("sword"));
            Assert.AreEqual(2, allSwords.Count, $"应找到 2 把剑，实际: {allSwords.Count}");

            // 按 ID 查询第一个
            Card firstSword = engine.GetCardById("sword");
            Assert.IsNotNull(firstSword, "应能找到第一把剑");
            Assert.AreEqual("sword", firstSword.Id, "找到的卡牌 ID 应为 sword");

            // 查询不存在的 ID
            Card notFound = engine.GetCardById("notexist");
            Assert.IsNull(notFound, "查询不存在的 ID 应返回 null");

            // 测试移除后查询
            engine.RemoveCard(sword1);
            Card shouldBeNull = engine.GetCardByKey(sword1.Id, sword1.Index);
            Assert.IsNull(shouldBeNull, "移除后应查询不到");

            var remainingSwords = new List<Card>(engine.GetCardsById("sword"));
            Assert.AreEqual(1, remainingSwords.Count, $"移除一个后应剩余 1 把剑，实际: {remainingSwords.Count}");
        }

        [Test]
        public void Test_CardRule_BasicStructure()
        {
            // 测试基本规则结构
            var rule = new CardRule();
            Assert.AreEqual(CardEventTypes.ADDED_TO_OWNER, rule.EventType, "默认触发器应为 AddedToOwner");
            Assert.AreEqual(1, rule.MatchRootHops, "默认 OwnerHops 应为 1");
            Assert.AreEqual(int.MaxValue, rule.MaxDepth, "默认 MaxDepth 应为 int.MaxValue");
            Assert.AreEqual(0, rule.Priority, "默认优先级应为 0");
            Assert.IsNotNull(rule.Requirements, "Requirements 不应为 null");
            Assert.IsNotNull(rule.Effects, "Effects 不应为 null");
            Assert.IsNotNull(rule.Policy, "Policy 不应为 null");

            // 测试规则配置
            rule.EventType = CardEventTypes.TICK;
            rule.MatchRootHops = 0;
            rule.Priority = 10;
            Assert.AreEqual(CardEventTypes.TICK, rule.EventType, "应能设置触发器");

            Assert.AreEqual(0, rule.MatchRootHops, "应能设置 OwnerHops");
            Assert.AreEqual(10, rule.Priority, "应能设置优先级");
        }

        [Test]
        public void Test_CardRule_SimpleRequirement()
        {
            var factory = new CardFactory();
            factory.Register("player", () => new(new("player", "玩家", "", "Card.Object")));
            factory.Register("sword", () => new(new("sword", "剑", "", "Card.Object"), "武器"));

            var engine = new CardEngine(factory);

            Card player = engine.CreateCard("player");
            Card sword = engine.CreateCard("sword");
            player.AddChild(sword);

            // 创建简单规则：需要容器有"武器"标签的子卡
            CardRule rule = new CardRuleBuilder()
                .OnTick()
                .MatchRootAtSelf()
                .NeedMatchRootTag("武器")
                .Build();

            Assert.AreEqual(CardEventTypes.TICK, rule.EventType, "触发器应为 Tick");
            Assert.AreEqual(0, rule.MatchRootHops, "OwnerHops 应为 0（Self）");
            Assert.AreEqual(1, rule.Requirements.Count, $"应有 1 个条件，实际: {rule.Requirements.Count}");
        }

        [Test]
        public void Test_CardRule_SimpleEffect()
        {
            var factory = new CardFactory();
            factory.Register("card", () => new(new("card", "卡牌", "", "Card.Object")));

            var engine = new CardEngine(factory);

            // 测试添加标签效果
            CardRule rule1 = new CardRuleBuilder()
                .OnUse()
                .MatchRootAtSelf()
                .DoAddTagToMatched("已使用")
                .Build();

            Assert.AreEqual(1, rule1.Effects.Count, $"应有 1 个效果，实际: {rule1.Effects.Count}");
            Assert.IsInstanceOf<AddTagEffect>(rule1.Effects[0], "效果应为 AddTagEffect");

            // 测试移除卡牌效果
            CardRule rule2 = new CardRuleBuilder()
                .OnTick()
                .MatchRootAtParent()
                .NeedMatchRootTag("消耗品")
                .DoRemoveByTag("消耗品", 1)
                .Build();

            Assert.AreEqual(1, rule2.Effects.Count, "应有 1 个移除效果");
            Assert.IsInstanceOf<RemoveCardsEffect>(rule2.Effects[0], "效果应为 RemoveCardsEffect");
        }

        [Test]
        public void Test_CardRule_TickEvent()
        {
            var factory = new CardFactory();
            factory.Register("timer", () =>
            {
                var data = new CardData("timer", "计时器", "", "Card.Object");
                var prop = new GameProperty("Time", 0f);
                return new(data, prop, "计时器");
            });

            var engine = new CardEngine(factory);

            // 创建规则：每次 Tick 时增加 Time 属性
            CardRule rule = new CardRuleBuilder()
                .OnTick()
                .MatchRootAtSelf()
                .DoInvoke((ctx, matched) =>
                {
                    GameProperty timeProp = ctx.Source.GetProperty("Time");
                    if (timeProp != null)
                    {
                        // 使用 Modifier 增加时间
                        var timeModifier = new FloatModifier(ModifierType.Add, 0, ctx.DeltaTime);
                        timeProp.AddModifier(timeModifier);
                    }
                })
                .Build();

            engine.RegisterRule(rule);

            Card timer = engine.CreateCard("timer");
            GameProperty timeProp = timer.GetProperty("Time");
            Assert.IsNotNull(timeProp, "计时器应有 Time 属性");

            float initialTime = timeProp.GetValue();
            Assert.AreEqual(0f, initialTime, $"初始时间应为 0，实际: {initialTime}");

            // 触发 Tick 事件
            timer.Tick(0.016f);
            engine.Pump(); // 处理事件队列

            float afterTick = timeProp.GetValue();
            Assert.Greater(afterTick, 0f, $"Tick 后时间应大于 0，实际: {afterTick}");
        }

        [Test]
        public void Test_CardRule_UseEvent()
        {
            var factory = new CardFactory();
            factory.Register("consumable", () => new(new("consumable", "消耗品", "", "Card.Object"), "可使用"));

            var engine = new CardEngine(factory);

            // 创建规则：使用时给源卡牌自身添加"已使用"标签
            CardRule rule = new CardRuleBuilder()
                .OnUse()
                .MatchRootAtSelf()
                .DoAddTagToSource("已使用")
                .Build();

            engine.RegisterRule(rule);

            Card item = engine.CreateCard("consumable");
            Assert.IsFalse(item.HasTag("已使用"), "初始不应有'已使用'标签");

            // 触发 Use 事件
            item.Use();
            engine.Pump();

            Assert.IsTrue(item.HasTag("已使用"), "使用后应有'已使用'标签");
        }

        [Test]
        public void Test_CardRule_MultipleRequirements()
        {
            var factory = new CardFactory();
            factory.Register("player", () => new(new("player", "玩家", "", "Card.Object")));
            factory.Register("sword", () => new(new("sword", "剑", "", "Card.Object"), "武器", "近战"));
            factory.Register("potion", () => new(new("potion", "药水", "", "Card.Object"), "消耗品"));

            var engine = new CardEngine(factory);

            // 创建规则：需要同时有"武器"和"消耗品"标签的子卡
            CardRule rule = new CardRuleBuilder()
                .OnUse()
                .MatchRootAtSelf()
                .NeedMatchRootTag("武器")
                .NeedMatchRootTag("消耗品")
                .DoAddTagToSource("战备充足")
                .Build();

            engine.RegisterRule(rule);

            Card player = engine.CreateCard("player");
            Card sword = engine.CreateCard("sword");
            Card potion = engine.CreateCard("potion");

            player.AddChild(sword);
            Assert.IsFalse(player.HasTag("战备充足"), "只有武器时不应触发");

            player.Use();
            engine.Pump();
            Assert.IsFalse(player.HasTag("战备充足"), "只有武器时规则不应生效");

            // 添加药水后再测试
            player.AddChild(potion);
            player.Use();
            engine.Pump();
            Assert.IsTrue(player.HasTag("战备充足"), "同时有武器和消耗品时应触发");
        }

        [Test]
        public void Test_CardRule_MultipleEffects()
        {
            var factory = new CardFactory();
            factory.Register("card", () =>
            {
                var data = new CardData("card", "卡牌", "", "Card.Object");
                var prop = new GameProperty("Value", 10f);
                return new(data, prop, "可激活");
            });

            // 注册一个子卡作为"触发器"
            factory.Register("trigger", () => new(new("trigger", "触发器", "", "Card.Object"), "触发器"));

            var engine = new CardEngine(factory);

            // 创建规则：使用 Need 方法直接选择源卡牌的子卡（触发器），这样 matched 会包含触发器
            // 但我们要修改的是容器（父卡），所以需要改变策略
            // 更好的方法：使用条件判断，然后用 DoInvoke 直接操作
            CardRule rule = new CardRuleBuilder()
                .OnUse()
                .MatchRootAtSelf()
                .When(ctx => ctx.Source.HasTag("可激活")) // 条件判断
                .DoInvoke((ctx, matched) =>
                {
                    // 直接操作源卡牌
                    ctx.Source.AddTag("已激活");

                    // 使用 Modifier 修改属性
                    GameProperty valueProp = ctx.Source.GetProperty("Value");
                    if (valueProp != null)
                    {
                        // 创建一个加法修饰符（优先级0）
                        var modifier = new FloatModifier(ModifierType.Add, 0, 5f);
                        valueProp.AddModifier(modifier);
                    }
                })
                .Build();

            engine.RegisterRule(rule);

            Card card = engine.CreateCard("card");
            GameProperty valueProp = card.GetProperty("Value");

            Assert.IsFalse(card.HasTag("已激活"), "初始无标签");
            Assert.AreEqual(10f, valueProp.GetValue(), "初始值为 10");

            card.Use();
            engine.Pump();

            Assert.IsTrue(card.HasTag("已激活"), "应添加标签");
            Assert.AreEqual(15f, valueProp.GetValue(), $"Value 应为 15（使用 Modifier），实际: {valueProp.GetValue()}");
        }

        [Test]
        public void Test_CardRule_RecursiveSelection()
        {
            var factory = new CardFactory();
            factory.Register("container", () => new(new("container", "容器", "", "Card.Object")));
            factory.Register("item", () => new(new("item", "物品", "", "Card.Object"), "物品"));

            var engine = new CardEngine(factory);

            // 创建规则：递归查找所有后代中的"物品"标签
            // 使用 maxMatched=0 表示返回所有找到的卡牌
            CardRule rule = new CardRuleBuilder()
                .OnUse()
                .MatchRootAtSelf()
                .NeedMatchRootTagRecursive("物品", 3, 0) // minCount=3 表示至少3个才触发，maxMatched=0 表示返回所有
                .DoInvoke((ctx, matched) =>
                {
                    // matched 包含递归找到的所有物品
                    ctx.Source.AddTag($"找到{matched.Count}个物品");
                })
                .Build();

            engine.RegisterRule(rule);

            // 构建嵌套结构
            Card root = engine.CreateCard("container");
            Card level1 = engine.CreateCard("container");
            Card level2 = engine.CreateCard("container");
            Card item1 = engine.CreateCard("item");
            Card item2 = engine.CreateCard("item");
            Card item3 = engine.CreateCard("item");

            root.AddChild(level1);
            level1.AddChild(level2);
            level1.AddChild(item1);
            level2.AddChild(item2);
            level2.AddChild(item3);

            root.Use();
            engine.Pump();

            Assert.IsTrue(root.HasTag("找到3个物品"), $"应找到3个物品，实际标签: {string.Join(", ", root.Tags)}");
        }

        [Test]
        public void Test_CardRule_MaxMatchedParameter()
        {
            var factory = new CardFactory();
            factory.Register("player", () => new(new("player", "玩家", "", "Card.Object")));
            factory.Register("item", () => new(new("item", "物品", "", "Card.Object", new string[] { "物品" })));

            var engine = new CardEngine(factory);

            Card player = engine.CreateCard("player");
            Card item1 = engine.CreateCard("item");
            Card item2 = engine.CreateCard("item");
            Card item3 = engine.CreateCard("item");
            Card item4 = engine.CreateCard("item");
            Card item5 = engine.CreateCard("item");

            player.AddChild(item1);
            player.AddChild(item2);
            player.AddChild(item3);
            player.AddChild(item4);
            player.AddChild(item5);

            // 测试1：MinCount=3, MaxMatched=-1（默认，返回 MinCount 个）
            CardRule rule1 = new CardRuleBuilder()
                .OnUse()
                .MatchRootAtSelf()
                .NeedMatchRootTag("物品", 3)
                .DoInvoke((ctx, matched) => { ctx.Source.AddTag($"默认返回{matched.Count}个"); })
                .Build();

            engine.RegisterRule(rule1);
            player.Use();
            engine.Pump();

            Assert.IsTrue(player.HasTag("默认返回3个"),
                $"MaxMatched=-1 应返回 MinCount(3) 个，实际标签: {string.Join(", ", player.Tags)}");

            // 清理标签
            player.RemoveTag("默认返回3个");
            engine.RemoveCard(player); // 移除以清除规则

            // 测试2：MinCount=3, MaxMatched=2（只返回2个）
            var engine2 = new CardEngine(factory);
            CardRule rule2 = new CardRuleBuilder()
                .OnUse()
                .MatchRootAtSelf()
                .NeedMatchRootTag("物品", 3, 2)
                .DoInvoke((ctx, matched) => { ctx.Source.AddTag($"限制返回{matched.Count}个"); })
                .Build();
            engine2.RegisterRule(rule2);

            Card player2 = engine2.CreateCard("player");
            player2.AddChild(engine2.CreateCard("item"));
            player2.AddChild(engine2.CreateCard("item"));
            player2.AddChild(engine2.CreateCard("item"));
            player2.AddChild(engine2.CreateCard("item"));
            player2.AddChild(engine2.CreateCard("item"));

            player2.Use();
            engine2.Pump();

            Assert.IsTrue(player2.HasTag("限制返回2个"), $"MaxMatched=2 应只返回2个，实际标签: {string.Join(", ", player2.Tags)}");

            // 测试3：MinCount=3, MaxMatched=0（返回所有）
            var engine3 = new CardEngine(factory);
            CardRule rule3 = new CardRuleBuilder()
                .OnUse()
                .MatchRootAtSelf()
                .NeedMatchRootTag("物品", 3, 0)
                .DoInvoke((ctx, matched) => { ctx.Source.AddTag($"返回全部{matched.Count}个"); })
                .Build();
            engine3.RegisterRule(rule3);

            Card player3 = engine3.CreateCard("player");
            player3.AddChild(engine3.CreateCard("item"));
            player3.AddChild(engine3.CreateCard("item"));
            player3.AddChild(engine3.CreateCard("item"));
            player3.AddChild(engine3.CreateCard("item"));
            player3.AddChild(engine3.CreateCard("item"));

            player3.Use();
            engine3.Pump();

            Assert.IsTrue(player3.HasTag("返回全部5个"), $"MaxMatched=0 应返回所有5个，实际标签: {string.Join(", ", player3.Tags)}");
        }

        [Test]
        public void Test_CardRule_CreateAndRemoveCards()
        {
            var factory = new CardFactory();
            factory.Register("crafter", () => new(new("crafter", "工匠", "", "Card.Object")));
            factory.Register("wood", () => new(new("wood", "木头", "", "Card.Object", new string[] { "材料" })));
            factory.Register("sword", () => new(new("sword", "木剑", "", "Card.Object", new string[] { "武器" })));

            var engine = new CardEngine(factory);

            // 创建规则：消耗2个材料，创建1个武器
            CardRule rule = new CardRuleBuilder()
                .OnUse()
                .MatchRootAtSelf()
                .NeedMatchRootTag("材料", 2)
                .DoRemoveByTag("材料", 2)
                .DoCreate("sword")
                .Build();

            engine.RegisterRule(rule);

            Card crafter = engine.CreateCard("crafter");
            Card wood1 = engine.CreateCard("wood");
            Card wood2 = engine.CreateCard("wood");

            crafter.AddChild(wood1);
            crafter.AddChild(wood2);

            Assert.AreEqual(2, crafter.Children.Count, $"初始应有2个子卡，实际: {crafter.Children.Count}");

            crafter.Use();
            engine.Pump();

            Assert.AreEqual(1, crafter.Children.Count, $"消耗2个材料后应剩余1个子卡，实际: {crafter.Children.Count}");

            bool hasSword = false;
            foreach (Card child in crafter.Children)
            {
                if (child.HasTag("武器"))
                {
                    hasSword = true;
                    break;
                }
            }

            Assert.IsTrue(hasSword, "应创建出武器");
        }

        [Test]
        public void Test_CardRule_PropertyModification()
        {
            var factory = new CardFactory();
            factory.Register("player", () =>
            {
                var data = new CardData("player", "玩家", "", "Card.Object");
                var hp = new GameProperty("HP", 100f);
                return new(data, hp);
            });
            factory.Register("buff", () => new(new("buff", "增益", "", "Card.Attribute"), "增益"));

            var engine = new CardEngine(factory);

            // 创建规则：每次Tick时，如果有增益子卡，通过 Modifier 恢复1点生命
            CardRule rule = new CardRuleBuilder()
                .OnTick()
                .MatchRootAtSelf()
                .NeedMatchRootTag("增益")
                .DoInvoke((ctx, matched) =>
                {
                    GameProperty hp = ctx.Source.GetProperty("HP");
                    if (hp != null && hp.GetBaseValue() < 100f)
                    {
                        // 使用 Modifier 来增加生命值
                        var healModifier = new FloatModifier(ModifierType.Add, 0, 1f);
                        hp.AddModifier(healModifier);
                    }
                })
                .Build();

            engine.RegisterRule(rule);

            Card player = engine.CreateCard("player");
            GameProperty hp = player.GetProperty("HP");
            hp.SetBaseValue(80f);

            Assert.AreEqual(80f, hp.GetValue(), "初始HP为80");

            // 没有增益时Tick
            player.Tick(1f);
            engine.Pump();
            Assert.AreEqual(80f, hp.GetValue(), "没有增益不应恢复");

            // 添加增益后Tick
            Card buff = engine.CreateCard("buff");
            player.AddChild(buff);

            player.Tick(1f);
            engine.Pump();
            Assert.AreEqual(81f, hp.GetValue(), $"有增益应恢复1点，实际: {hp.GetValue()}");

            player.Tick(1f);
            engine.Pump();
            Assert.AreEqual(82f, hp.GetValue(), $"再次恢复，实际: {hp.GetValue()}");
        }

        [Test]
        public void Test_CardRule_CustomEvents()
        {
            var factory = new CardFactory();
            factory.Register("skill", () => new(new("skill", "技能", "", "Card.Action")));
            factory.Register("target", () => new(new("target", "目标", "", "Card.Object")));

            var engine = new CardEngine(factory);

            // 创建规则：监听自定义事件"OnDamage"
            CardRule rule = new CardRuleBuilder()
                .On("OnDamage")
                .MatchRootAtSelf()
                .DoInvoke((ctx, matched) =>
                {
                    if (ctx.Event.DataObject is int damage) ctx.Source.AddTag($"受到{damage}点伤害");
                })
                .Build();

            engine.RegisterRule(rule);

            Card target = engine.CreateCard("target");

            Assert.AreEqual(0, target.Tags.Count, "初始无标签");

            // 触发自定义事件
            target.RaiseEvent("OnDamage", 50);
            engine.Pump();

            Assert.IsTrue(target.HasTag("受到50点伤害"), $"应有伤害标签，实际标签: {string.Join(", ", target.Tags)}");

            // 触发不同的自定义事件（不应响应）
            target.RaiseEvent("OnHeal", 20);
            engine.Pump();

            Assert.IsFalse(target.HasTag("受到20点伤害"), "不应响应OnHeal事件");
        }

        [Test]
        public void Test_CardRule_ConditionalRequirement()
        {
            var factory = new CardFactory();
            factory.Register("player", () =>
            {
                var data = new CardData("player", "玩家", "", "Card.Object");
                var hp = new GameProperty("HP", 100f);
                return new(data, hp);
            });

            var engine = new CardEngine(factory);

            // 创建规则：HP低于50时触发
            CardRule rule = new CardRuleBuilder()
                .OnTick()
                .MatchRootAtSelf()
                .When(ctx =>
                {
                    GameProperty hp = ctx.Source.GetProperty("HP");
                    return hp != null && hp.GetValue() < 50f;
                })
                .DoAddTagToSource("危险状态")
                .Build();

            engine.RegisterRule(rule);

            Card player = engine.CreateCard("player");
            GameProperty hp = player.GetProperty("HP");

            // HP为100时
            player.Tick(1f);
            engine.Pump();
            Assert.IsFalse(player.HasTag("危险状态"), "HP充足不应触发");

            // HP降到30
            hp.SetBaseValue(30f);
            player.Tick(1f);
            engine.Pump();
            Assert.IsTrue(player.HasTag("危险状态"), "HP低于50应触发");
        }

        [Test]
        public void Test_CardRule_RulePriority()
        {
            var factory = new CardFactory();
            factory.Register("card", () => new(new("card", "卡牌", "", "Card.Object")));

            var engine = new CardEngine(factory);
            engine.Policy.RuleSelection = RuleSelectionMode.Priority; // 启用优先级模式

            int executionOrder = 0;

            // 低优先级规则（后执行）
            CardRule rule1 = new CardRuleBuilder()
                .OnUse()
                .MatchRootAtSelf()
                .Priority(10)
                .DoInvoke((ctx, matched) =>
                {
                    executionOrder = executionOrder * 10 + 1;
                    ctx.Source.AddTag("规则1");
                })
                .Build();

            // 高优先级规则（先执行）
            CardRule rule2 = new CardRuleBuilder()
                .OnUse()
                .MatchRootAtSelf()
                .Priority(1)
                .DoInvoke((ctx, matched) =>
                {
                    executionOrder = executionOrder * 10 + 2;
                    ctx.Source.AddTag("规则2");
                })
                .Build();

            engine.RegisterRule(rule1);
            engine.RegisterRule(rule2);

            Card card = engine.CreateCard("card");
            card.Use();
            engine.Pump();

            Assert.AreEqual(21, executionOrder, $"执行顺序应为21（规则2先，规则1后），实际: {executionOrder}");
            Assert.IsTrue(card.HasTag("规则1") && card.HasTag("规则2"), "两条规则都应执行");
        }

        [Test]
        public void Test_CardRule_CompositeRequirements()
        {
            var factory = new CardFactory();
            factory.Register("player", () => new(new("player", "玩家", "", "Card.Object")));
            factory.Register("weapon", () => new(new("weapon", "武器", "", "Card.Object", new string[] { "武器", "装备" })));
            factory.Register("armor", () => new(new("armor", "护甲", "", "Card.Object", new string[] { "护甲", "装备" })));

            var engine = new CardEngine(factory);

            // 使用复合需求：需要有装备标签的子卡（武器或护甲任一即可）
            var anyReq = new AnyRequirement();
            anyReq.Children.Add(new CardsRequirement
            {
                Root = SelectionRoot.MatchRoot,
                Scope = TargetScope.Children,
                FilterMode = CardFilterMode.ByTag,
                FilterValue = "武器",
                MinCount = 1,
            });
            anyReq.Children.Add(new CardsRequirement
            {
                Root = SelectionRoot.MatchRoot,
                Scope = TargetScope.Children,
                FilterMode = CardFilterMode.ByTag,
                FilterValue = "护甲",
                MinCount = 1,
            });

            CardRule rule = new CardRuleBuilder()
                .OnUse()
                .MatchRootAtSelf()
                .AddRequirement(anyReq)
                .DoAddTagToSource("有装备")
                .Build();

            engine.RegisterRule(rule);

            Card player = engine.CreateCard("player");

            // 无装备
            player.Use();
            engine.Pump();
            Assert.IsFalse(player.HasTag("有装备"), "无装备不应触发");

            // 有武器
            Card weapon = engine.CreateCard("weapon");
            player.AddChild(weapon);
            player.Use();
            engine.Pump();
            Assert.IsTrue(player.HasTag("有装备"), "有武器应触发");

            // 清除标签测试护甲
            player.RemoveTag("有装备");
            player.RemoveChild(weapon);
            Card armor = engine.CreateCard("armor");
            player.AddChild(armor);
            player.Use();
            engine.Pump();
            Assert.IsTrue(player.HasTag("有装备"), "有护甲也应触发");
        }

        [Test]
        public void Test_CardRule_ComplexGameplay()
        {
            var factory = new CardFactory();

            // 注册各种卡牌
            factory.Register("player", () =>
            {
                var data = new CardData("player", "玩家", "", "Card.Object");
                var hp = new GameProperty("HP", 100f);
                var mana = new GameProperty("Mana", 50f);
                return new(data, new List<GameProperty> { hp, mana });
            });

            string[] defaultTags = new string[] { "法术", "火系" };
            factory.Register("fire_spell", () => new(new("fire_spell", "火球术", "", "Card.Action", defaultTags)));
            factory.Register("mana_potion",
                () => new(new("mana_potion", "魔法药水", "", "Card.Object", new string[] { "药水" })));
            factory.Register("burn", () => new(new("burn", "灼烧", "", "Card.Attribute", new string[] { "负面状态" })));

            var engine = new CardEngine(factory);

            // 规则1：使用法术消耗魔法值并添加灼烧状态
            CardRule rule1 = new CardRuleBuilder()
                .OnUse()
                .MatchRootAtParent() // 法术在玩家下
                .WhenSourceHasTag("法术")
                .DoInvoke((ctx, matched) =>
                {
                    GameProperty mana = ctx.MatchRoot.GetProperty("Mana");
                    if (mana != null && mana.GetValue() >= 10f)
                    {
                        // 使用 Modifier 减少魔法值
                        var costModifier = new FloatModifier(ModifierType.Add, 0, -10f);
                        mana.AddModifier(costModifier);
                        ctx.Source.AddTag("已施放");

                        // 如果是火系，创建灼烧状态
                        if (ctx.Source.HasTag("火系"))
                        {
                            Card burn = ctx.Engine.CreateCard("burn");
                            if (burn != null) ctx.MatchRoot.AddChild(burn);
                        }
                    }
                })
                .Build();

            // 规则2：使用药水恢复魔法
            CardRule rule2 = new CardRuleBuilder()
                .OnUse()
                .MatchRootAtParent()
                .WhenSourceHasTag("药水")
                .DoInvoke((ctx, matched) =>
                {
                    GameProperty mana = ctx.MatchRoot.GetProperty("Mana");
                    if (mana != null)
                    {
                        // 使用 Modifier 增加魔法值
                        var restoreModifier = new FloatModifier(ModifierType.Add, 0, 20f);
                        mana.AddModifier(restoreModifier);
                    }

                    // 延迟移除，确保规则执行完毕
                    ctx.MatchRoot.RemoveChild(ctx.Source);
                })
                .Build();

            // 规则3：每次Tick，如果有灼烧状态子卡，造成伤害
            CardRule rule3 = new CardRuleBuilder()
                .OnTick()
                .MatchRootAtSelf()
                .NeedMatchRootTag("负面状态")
                .DoInvoke((ctx, matched) =>
                {
                    GameProperty hp = ctx.Source.GetProperty("HP");
                    if (hp != null)
                    {
                        // 使用 Modifier 减少生命值
                        var damageModifier = new FloatModifier(ModifierType.Add, 0, -2f);
                        hp.AddModifier(damageModifier);
                    }
                })
                .Build();

            engine.RegisterRule(rule1);
            engine.RegisterRule(rule2);
            engine.RegisterRule(rule3);

            // 开始游戏
            Card player = engine.CreateCard("player");
            Card fireSpell = engine.CreateCard("fire_spell");
            Card manaPotion = engine.CreateCard("mana_potion");

            player.AddChild(fireSpell);
            player.AddChild(manaPotion);

            GameProperty hp = player.GetProperty("HP");
            GameProperty mana = player.GetProperty("Mana");

            Assert.AreEqual(50f, mana.GetValue(), "初始魔法为50");
            Assert.AreEqual(2, player.Children.Count, "初始有2个子卡");

            // 施放火球术
            fireSpell.Use();
            engine.Pump();

            Assert.AreEqual(40f, mana.GetValue(), $"施放法术后魔法应为40，实际: {mana.GetValue()}");
            Assert.IsTrue(fireSpell.HasTag("已施放"), "法术应标记为已施放");
            Assert.AreEqual(3, player.Children.Count, $"应创建灼烧状态，实际子卡数: {player.Children.Count}");

            // 使用魔法药水
            manaPotion.Use();
            engine.Pump();

            Assert.AreEqual(60f, mana.GetValue(), $"使用药水后魔法应为60，实际: {mana.GetValue()}");
            Assert.AreEqual(2, player.Children.Count, $"药水应被移除，实际子卡数: {player.Children.Count}");

            // Tick测试灼烧伤害
            player.Tick(1f);
            engine.Pump();

            Assert.AreEqual(98f, hp.GetValue(), $"灼烧应造成2点伤害，实际HP: {hp.GetValue()}");
        }

        [Test]
        public void Test_CardEngine_UnregisterRule()
        {
            var factory = new CardFactory();
            factory.Register("test_card", () => new(new("test_card", "测试卡牌", "", "Card.Object"), "测试"));

            var engine = new CardEngine(factory);

            // 创建一个简单的规则：使用时添加标签
            CardRule rule = new CardRuleBuilder()
                .OnUse()
                .MatchRootAtSelf()
                .DoAddTagToSource("已使用")
                .Build();

            // 注册规则
            engine.RegisterRule(rule);

            // 创建测试卡牌
            Card card = engine.CreateCard("test_card");
            Assert.IsFalse(card.HasTag("已使用"), "初始状态不应有已使用标签");

            // 触发规则
            card.Use();
            engine.Pump();
            Assert.IsTrue(card.HasTag("已使用"), "规则应被触发，添加已使用标签");

            // 移除标签，准备下一次测试
            card.RemoveTag("已使用");

            // 注销规则
            bool unregisterResult = engine.UnregisterRule(rule);
            Assert.IsTrue(unregisterResult, "注销规则应成功");

            // 再次触发，应该不再生效
            card.Use();
            engine.Pump();
            Assert.IsFalse(card.HasTag("已使用"), "注销后规则不应再触发");

            // 测试注销不存在的规则
            bool unregisterNonExistent = engine.UnregisterRule(rule);
            Assert.IsFalse(unregisterNonExistent, "注销不存在的规则应返回false");

            // 测试注销null规则
            bool unregisterNull = engine.UnregisterRule(null);
            Assert.IsFalse(unregisterNull, "注销null规则应返回false");
        }
    }
}