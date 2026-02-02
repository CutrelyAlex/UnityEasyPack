using NUnit.Framework;
using System.Collections.Generic;
using EasyPack.EmeCardSystem;

namespace EasyPack.EmeCardTests
{
    /// <summary>
    /// 测试卡牌子卡事件触发机制。
    /// 
    /// 主要测试：
    /// - AddedToOwner 事件在卡牌被添加为子卡时的触发
    /// - RemovedFromOwner 事件在卡牌从父卡移除时的触发
    /// - 规则如何对这些事件做出反应
    /// </summary>
    [TestFixture]
    public class CardChildEventTest
    {
        private CardFactory _factory;
        private CardEngine _engine;

        [SetUp]
        public void SetUp()
        {
            _factory = new CardFactory();
            _engine = new CardEngine(_factory);

            // 注册测试用卡牌
            _factory.Register("parent", () => new(new("parent", "父卡", "", "Card.Object")));
            _factory.Register("child", () => new(new("child", "子卡", "", "Card.Object")));
            _factory.Register("tagged_child", () => new(new("tagged_child", "带标签的子卡", "", "Card.Object"), "特殊"));
        }

        [TearDown]
        public void TearDown()
        {
            _factory = null;
            _engine = null;
        }

        /// <summary>
        /// 测试：AddedToOwner 事件触发时，规则能够正确响应。
        /// </summary>
        [Test]
        public void Test_AddedToOwner_Event_TriggerRule()
        {
            var invokeCount = 0;

            // 创建规则：当卡牌被添加到父卡时，为子卡添加"已收纳"标签
            CardRule rule = new CardRuleBuilder()
                .On(CardEventTypes.ADDED_TO_OWNER)
                .MatchRootAtSelf()
                .DoInvoke((ctx, matched) =>
                {
                    invokeCount++;
                    // 标记子卡已被父卡收纳
                    ctx.Source.AddTag("已收纳");
                })
                .Build();

            _engine.RegisterRule(rule);

            // 创建卡牌
            Card parent = _engine.CreateCard("parent");
            Card child = _engine.CreateCard("child");

            Assert.IsFalse(child.HasTag("已收纳"), "初始子卡不应有'已收纳'标签");
            Assert.AreEqual(0, invokeCount, "初始规则不应被触发");

            // 添加子卡，应触发事件
            parent.AddChild(child);
            _engine.Pump(); // 处理事件队列

            Assert.AreEqual(1, invokeCount, "AddedToOwner 事件应触发规则 1 次");
            Assert.IsTrue(child.HasTag("已收纳"), "子卡应被标记为'已收纳'");
        }

        /// <summary>
        /// 测试：RemovedFromOwner 事件触发时，规则能够正确响应。
        /// </summary>
        [Test]
        public void Test_RemovedFromOwner_Event_TriggerRule()
        {
            var invokeCount = 0;
            var removedChildId = "";

            // 创建规则：当卡牌从父卡移除时，记录被移除的子卡
            CardRule rule = new CardRuleBuilder()
                .On(CardEventTypes.REMOVED_FROM_OWNER)
                .MatchRootAtSelf()
                .DoInvoke((ctx, matched) =>
                {
                    invokeCount++;
                    removedChildId = ctx.Source.Id;
                    ctx.Source.AddTag("已移除");
                })
                .Build();

            _engine.RegisterRule(rule);

            // 创建卡牌
            Card parent = _engine.CreateCard("parent");
            Card child = _engine.CreateCard("child");

            parent.AddChild(child);
            _engine.Pump();

            Assert.AreEqual(0, invokeCount, "移除前规则不应被触发");
            Assert.IsFalse(child.HasTag("已移除"), "移除前子卡不应有'已移除'标签");

            // 移除子卡，应触发事件
            parent.RemoveChild(child);
            _engine.Pump();

            Assert.AreEqual(1, invokeCount, "RemovedFromOwner 事件应触发规则 1 次");
            Assert.AreEqual("child", removedChildId, "应记录被移除的子卡 ID");
            Assert.IsTrue(child.HasTag("已移除"), "子卡应被标记为'已移除'");
        }

        /// <summary>
        /// 测试：多个子卡添加时，每个都触发各自的 AddedToOwner 事件。
        /// </summary>
        [Test]
        public void Test_AddedToOwner_MultipleChildren()
        {
            var addedChildren = new List<string>();

            // 创建规则：记录所有被添加的子卡
            CardRule rule = new CardRuleBuilder()
                .On(CardEventTypes.ADDED_TO_OWNER)
                .MatchRootAtSelf()
                .DoInvoke((ctx, matched) =>
                {
                    addedChildren.Add(ctx.Source.Id);
                })
                .Build();

            _engine.RegisterRule(rule);

            Card parent = _engine.CreateCard("parent");
            Card child1 = _engine.CreateCard("child");
            Card child2 = _engine.CreateCard("child");
            Card child3 = _engine.CreateCard("child");

            // 添加多个子卡
            parent.AddChild(child1);
            parent.AddChild(child2);
            parent.AddChild(child3);
            _engine.Pump();

            Assert.AreEqual(3, addedChildren.Count, "应记录 3 个子卡的 AddedToOwner 事件");
            Assert.AreEqual(3, parent.Children.Count, "父卡应有 3 个子卡");
        }

        /// <summary>
        /// 测试：规则可以匹配带特定标签的被添加子卡，并执行相应操作。
        /// </summary>
        [Test]
        public void Test_AddedToOwner_WithTagRequirement()
        {
            var specialChildCount = 0;

            // 创建规则：只对带有"特殊"标签的子卡做出反应
            // 注：Source 是被添加的子卡，MatchRoot 是父卡容器
            CardRule rule = new CardRuleBuilder()
                .On(CardEventTypes.ADDED_TO_OWNER)
                .MatchRootAtParent()
                .WhenSourceHasTag("特殊")  // 检查子卡本身是否有"特殊"标签
                .DoInvoke((ctx, matched) =>
                {
                    specialChildCount++;
                    // 为父卡添加标记，表示有特殊子卡被添加
                    ctx.MatchRoot.AddTag("已激活特殊子卡");
                })
                .Build();

            _engine.RegisterRule(rule);

            Card parent = _engine.CreateCard("parent");
            Card normalChild = _engine.CreateCard("child");
            Card specialChild = _engine.CreateCard("tagged_child"); // 带有"特殊"标签

            // 添加普通子卡，不应触发
            parent.AddChild(normalChild);
            _engine.Pump();
            Assert.AreEqual(0, specialChildCount, "普通子卡不应触发规则");

            // 添加特殊子卡，应触发
            parent.AddChild(specialChild);
            _engine.Pump();
            Assert.AreEqual(1, specialChildCount, "特殊子卡应触发规则");
            Assert.IsTrue(parent.HasTag("已激活特殊子卡"), "父卡应被标记");
        }

        /// <summary>
        /// 测试：通过 CardEngine.AddChildToCard() 方法添加子卡时，也能正确触发事件。
        /// </summary>
        [Test]
        public void Test_AddedToOwner_ViaEngineAddChildToCard()
        {
            var invokeCount = 0;

            CardRule rule = new CardRuleBuilder()
                .On(CardEventTypes.ADDED_TO_OWNER)
                .MatchRootAtSelf()
                .DoInvoke((ctx, matched) => invokeCount++)
                .Build();

            _engine.RegisterRule(rule);

            Card parent = _engine.CreateCard("parent");
            Card child = _engine.CreateCard("child");

            // 通过引擎的 AddChildToCard 方法添加（parent 已通过 CreateCard 注册）
            _engine.AddChildToCard(parent, child);
            _engine.Pump();

            Assert.AreEqual(1, invokeCount, "通过引擎添加子卡也应触发 AddedToOwner 事件");
            Assert.AreEqual(parent, child.Owner, "子卡的所有者应为父卡");
        }

        /// <summary>
        /// 测试：连续添加和移除子卡时，事件序列正确无误。
        /// </summary>
        [Test]
        public void Test_ChildEvent_Sequence()
        {
            var eventSequence = new List<string>();

            // 添加事件
            CardRule addRule = new CardRuleBuilder()
                .On(CardEventTypes.ADDED_TO_OWNER)
                .MatchRootAtSelf()
                .DoInvoke((ctx, matched) => eventSequence.Add("added"))
                .Build();

            // 移除事件
            CardRule removeRule = new CardRuleBuilder()
                .On(CardEventTypes.REMOVED_FROM_OWNER)
                .MatchRootAtSelf()
                .DoInvoke((ctx, matched) => eventSequence.Add("removed"))
                .Build();

            _engine.RegisterRule(addRule);
            _engine.RegisterRule(removeRule);

            Card parent = _engine.CreateCard("parent");
            Card child = _engine.CreateCard("child");

            // 添加子卡
            parent.AddChild(child);
            _engine.Pump();
            Assert.AreEqual(1, eventSequence.Count, "应有 1 个事件");
            Assert.AreEqual("added", eventSequence[0], "第一个事件应为 added");

            // 移除子卡
            parent.RemoveChild(child);
            _engine.Pump();
            Assert.AreEqual(2, eventSequence.Count, "应有 2 个事件");
            Assert.AreEqual("removed", eventSequence[1], "第二个事件应为 removed");

            // 再次添加子卡
            parent.AddChild(child);
            _engine.Pump();
            Assert.AreEqual(3, eventSequence.Count, "应有 3 个事件");
            Assert.AreEqual("added", eventSequence[2], "第三个事件应为 added");
        }

        /// <summary>
        /// 测试：固有子卡无法通过普通 RemoveChild 移除，但可以通过 force=true 移除。
        /// 移除固有子卡也会触发 RemovedFromOwner 事件。
        /// </summary>
        [Test]
        public void Test_IntrinsicChild_RemovalEvent()
        {
            var removalCount = 0;

            CardRule rule = new CardRuleBuilder()
                .On(CardEventTypes.REMOVED_FROM_OWNER)
                .MatchRootAtSelf()
                .DoInvoke((ctx, matched) => removalCount++)
                .Build();

            _engine.RegisterRule(rule);

            Card parent = _engine.CreateCard("parent");
            Card intrinsicChild = _engine.CreateCard("child");

            // 添加为固有子卡
            parent.AddChild(intrinsicChild, intrinsic: true);
            _engine.Pump();

            // 尝试普通移除，应失败
            bool removed = parent.RemoveChild(intrinsicChild);
            _engine.Pump();
            Assert.IsFalse(removed, "不能通过普通方式移除固有子卡");
            Assert.AreEqual(0, removalCount, "普通移除失败不应触发事件");

            // 强制移除，应成功
            removed = parent.RemoveChild(intrinsicChild, force: true);
            _engine.Pump();
            Assert.IsTrue(removed, "强制移除应成功");
            Assert.AreEqual(1, removalCount, "强制移除应触发 RemovedFromOwner 事件");
        }

        /// <summary>
        /// 测试：多个规则可以响应同一个 AddedToOwner 事件。
        /// </summary>
        [Test]
        public void Test_AddedToOwner_MultipleRules()
        {
            var rule1Triggered = false;
            var rule2Triggered = false;

            // 规则 1：添加"规则1"标签
            CardRule rule1 = new CardRuleBuilder()
                .On(CardEventTypes.ADDED_TO_OWNER)
                .MatchRootAtSelf()
                .DoInvoke((ctx, matched) =>
                {
                    rule1Triggered = true;
                    ctx.Source.AddTag("规则1");
                })
                .Build();

            // 规则 2：添加"规则2"标签
            CardRule rule2 = new CardRuleBuilder()
                .On(CardEventTypes.ADDED_TO_OWNER)
                .MatchRootAtSelf()
                .DoInvoke((ctx, matched) =>
                {
                    rule2Triggered = true;
                    ctx.Source.AddTag("规则2");
                })
                .Build();

            _engine.RegisterRule(rule1);
            _engine.RegisterRule(rule2);

            Card parent = _engine.CreateCard("parent");
            Card child = _engine.CreateCard("child");

            parent.AddChild(child);
            _engine.Pump();

            Assert.IsTrue(rule1Triggered, "规则 1 应被触发");
            Assert.IsTrue(rule2Triggered, "规则 2 应被触发");
            Assert.IsTrue(child.HasTag("规则1"), "子卡应有规则 1 的标签");
            Assert.IsTrue(child.HasTag("规则2"), "子卡应有规则 2 的标签");
        }

        /// <summary>
        /// 测试：GetEventData 能正确获取事件中的原所有者信息。
        /// </summary>
        [Test]
        public void Test_RemovedFromOwner_EventData()
        {
            Card eventDataOwner = null;

            CardRule rule = new CardRuleBuilder()
                .On(CardEventTypes.REMOVED_FROM_OWNER)
                .MatchRootAtSelf()
                .DoInvoke((ctx, matched) =>
                {
                    // 获取事件数据中的原所有者
                    if (ctx.Event is ICardEvent<Card> cardEvent)
                    {
                        eventDataOwner = cardEvent.Data;
                    }
                })
                .Build();

            _engine.RegisterRule(rule);

            Card parent = _engine.CreateCard("parent");
            Card child = _engine.CreateCard("child");

            parent.AddChild(child);
            _engine.Pump();

            parent.RemoveChild(child);
            _engine.Pump();

            Assert.IsNotNull(eventDataOwner, "事件数据不应为 null");
            Assert.AreEqual(parent, eventDataOwner, "事件数据应为原所有者卡牌");
        }
    }
}
