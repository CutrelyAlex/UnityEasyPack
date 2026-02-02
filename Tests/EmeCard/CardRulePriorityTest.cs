using System.Collections.Generic;
using EasyPack.EmeCardSystem;
using NUnit.Framework;
using UnityEngine;

namespace EasyPack.EmeCardTests
{
    /// <summary>
    ///     EmeCard 规则执行优先级测试
    ///     
    ///     核心测试范围：
    ///     1. 规则注册顺序 vs Priority 优先级的影响
    ///     2. 效果池模式（EffectPoolFlushMode.AfterPump）vs 流式模式（EnableEffectPool=false）的执行顺序
    ///     3. StopEventOnSuccess 对规则链的影响
    ///     4. 多个事件触发时的优先级排序
    ///     5. 同优先级规则的执行顺序保证（注册顺序）
    ///     6. 嵌套事件中的优先级表现
    /// </summary>
    [TestFixture]
    public class CardRulePriorityTest
    {
        private CardFactory _factory;
        private CardEngine _engine;
        private List<string> _executionLog;

        [SetUp]
        public void Setup()
        {
            _factory = new CardFactory();
            _factory.Register("card", () => new(new("card", "卡牌", "", "Card.Object")));
            _engine = new CardEngine(_factory);
            _executionLog = new List<string>();
        }

        [TearDown]
        public void TearDown()
        {
            _engine.ClearAllCards();
        }

        #region 规则优先级基础测试

        [Test]
        public void Test_RulePriority_HigherPriorityFirst()
        {
            // 测试：Priority 值越小优先级越高
            Card card = _engine.CreateCard("card");

            // 注册三个规则，优先级分别为 2, 0, 1
            // 期望执行顺序：0, 1, 2
            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.TICK)
                .MatchRootAtSelf()
                .Priority(2)
                .DoInvoke((ctx, _) => _executionLog.Add("Rule_Priority_2"))
                .Build());

            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.TICK)
                .MatchRootAtSelf()
                .Priority(0)
                .DoInvoke((ctx, _) => _executionLog.Add("Rule_Priority_0"))
                .Build());

            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.TICK)
                .MatchRootAtSelf()
                .Priority(1)
                .DoInvoke((ctx, _) => _executionLog.Add("Rule_Priority_1"))
                .Build());

            card.Tick(0.016f);
            _engine.Pump();

            // 验证执行顺序
            Assert.AreEqual(3, _executionLog.Count, "应该执行3条规则");
            Assert.AreEqual("Rule_Priority_0", _executionLog[0], "优先级 0 应该首先执行");
            Assert.AreEqual("Rule_Priority_1", _executionLog[1], "优先级 1 应该次之执行");
            Assert.AreEqual("Rule_Priority_2", _executionLog[2], "优先级 2 应该最后执行");
        }

        [Test]
        public void Test_RulePriority_SamePriorityFollowsRegistrationOrder()
        {
            // 测试：相同优先级的规则按注册顺序执行
            Card card = _engine.CreateCard("card");

            // 注册三个优先级相同的规则
            for (int i = 0; i < 3; i++)
            {
                int index = i; // 捕获正确的值
                _engine.RegisterRule(new CardRuleBuilder()
                    .On(CardEventTypes.TICK)
                    .MatchRootAtSelf()
                    .Priority(0) // 所有规则优先级相同
                    .DoInvoke((ctx, _) => _executionLog.Add($"Rule_{index}"))
                    .Build());
            }

            card.Tick(0.016f);
            _engine.Pump();

            // 验证注册顺序执行
            Assert.AreEqual(3, _executionLog.Count);
            Assert.AreEqual("Rule_0", _executionLog[0], "第一个注册的规则应该首先执行");
            Assert.AreEqual("Rule_1", _executionLog[1], "第二个注册的规则应该次之执行");
            Assert.AreEqual("Rule_2", _executionLog[2], "第三个注册的规则应该最后执行");
        }

        #endregion

        #region 效果池模式 vs 流式模式优先级对比

        [Test]
        public void Test_EffectPoolMode_GlobalPrioritySort()
        {
            // 测试：效果池模式下，所有规则的效果按全局优先级排序
            // 启用效果池，AfterPump 模式
            _engine.Policy.EnableEffectPool = true;
            _engine.Policy.EffectPoolFlushMode = EffectPoolFlushMode.AfterPump;

            Card card = _engine.CreateCard("card");

            // 注册两个规则，第一个注册的优先级高
            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.TICK)
                .MatchRootAtSelf()
                .Priority(0) // 高优先级
                .DoInvoke((ctx, _) => _executionLog.Add("Effect1"))
                .DoInvoke((ctx, _) => _executionLog.Add("Effect2"))
                .Build());

            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.TICK)
                .MatchRootAtSelf()
                .Priority(1) // 低优先级
                .DoInvoke((ctx, _) => _executionLog.Add("Effect3"))
                .Build());

            card.Tick(0.016f);
            _engine.Pump();

            // 在效果池模式下，效果按全局优先级排序
            // 预期顺序：Effect1, Effect2（高优先级规则的效果）, Effect3（低优先级规则的效果）
            Assert.AreEqual(3, _executionLog.Count);
            Assert.AreEqual("Effect1", _executionLog[0], "高优先级规则的第一个效果应首先执行");
            Assert.AreEqual("Effect2", _executionLog[1], "高优先级规则的第二个效果应次之执行");
            Assert.AreEqual("Effect3", _executionLog[2], "低优先级规则的效果应最后执行");
        }

        [Test]
        public void Test_StreamMode_EffectsExecuteImmediately()
        {
            // 测试：禁用效果池时，规则效果立即执行，不进行全局排序
            // 禁用效果池：逐个规则执行其所有效果
            _engine.Policy.EnableEffectPool = false;

            Card card = _engine.CreateCard("card");

            // 注册两个规则
            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.TICK)
                .MatchRootAtSelf()
                .Priority(0)
                .DoInvoke((ctx, _) => _executionLog.Add("Effect1"))
                .DoInvoke((ctx, _) => _executionLog.Add("Effect2"))
                .Build());

            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.TICK)
                .MatchRootAtSelf()
                .Priority(1)
                .DoInvoke((ctx, _) => _executionLog.Add("Effect3"))
                .Build());

            card.Tick(0.016f);
            _engine.Pump();

            // 禁用效果池时，规则按注册/优先级顺序执行，每个规则的效果立即执行
            // 预期顺序：Effect1, Effect2（第一个规则的所有效果）, Effect3（第二个规则的效果）
            Assert.AreEqual(3, _executionLog.Count);
            Assert.AreEqual("Effect1", _executionLog[0]);
            Assert.AreEqual("Effect2", _executionLog[1]);
            Assert.AreEqual("Effect3", _executionLog[2]);
        }

        [Test]
        public void Test_EffectPoolMode_GlobalPrioritySortVsStream()
        {
            // 效果池模式：按全局优先级排序
            _engine.Policy.EnableEffectPool = true;
            _engine.Policy.EffectPoolFlushMode = EffectPoolFlushMode.AfterPump;

            Card card = _engine.CreateCard("card");

            // 注册规则：优先级 1 先注册，优先级 0 后注册
            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.USE)
                .MatchRootAtSelf()
                .Priority(1)
                .DoInvoke((ctx, _) => _executionLog.Add("LowPriority_Effect"))
                .Build());

            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.USE)
                .MatchRootAtSelf()
                .Priority(0)
                .DoInvoke((ctx, _) => _executionLog.Add("HighPriority_Effect"))
                .Build());

            card.Use();
            _engine.Pump();

            // 效果池模式：按优先级排序
            Assert.AreEqual("HighPriority_Effect", _executionLog[0], "效果池模式应先执行高优先级");
            Assert.AreEqual("LowPriority_Effect", _executionLog[1], "效果池模式应后执行低优先级");
        }

        [Test]
        public void Test_StreamMode_AlsoPrioritySorted()
        {
            // 流式模式也按优先级执行（默认 RuleSelection = Priority）
            _engine.Policy.EnableEffectPool = false;

            Card card = _engine.CreateCard("card");

            // 注册相同的规则
            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.USE)
                .MatchRootAtSelf()
                .Priority(1)
                .DoInvoke((ctx, _) => _executionLog.Add("LowPriority_Effect"))
                .Build());

            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.USE)
                .MatchRootAtSelf()
                .Priority(0)
                .DoInvoke((ctx, _) => _executionLog.Add("HighPriority_Effect"))
                .Build());

            card.Use();
            _engine.Pump();

            // 流式模式：同样按优先级执行
            Assert.AreEqual("HighPriority_Effect", _executionLog[0], "流式模式也应先执行高优先级");
            Assert.AreEqual("LowPriority_Effect", _executionLog[1], "流式模式也应后执行低优先级");
        }

        [Test]
        public void Test_RegistrationOrderMode_IgnoresPriority()
        {
            // 测试：当 RuleSelection = RegistrationOrder 时，忽略优先级，按注册顺序执行
            _engine.Policy.RuleSelection = RuleSelectionMode.RegistrationOrder;
            _engine.Policy.EnableEffectPool = false;

            Card card = _engine.CreateCard("card");

            // 优先级 1 先注册，优先级 0 后注册
            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.USE)
                .MatchRootAtSelf()
                .Priority(1)
                .DoInvoke((ctx, _) => _executionLog.Add("LowPriority_Effect"))
                .Build());

            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.USE)
                .MatchRootAtSelf()
                .Priority(0)
                .DoInvoke((ctx, _) => _executionLog.Add("HighPriority_Effect"))
                .Build());

            card.Use();
            _engine.Pump();

            // RegistrationOrder 模式：按注册顺序执行，忽略优先级
            Assert.AreEqual("LowPriority_Effect", _executionLog[0], "RegistrationOrder 模式应按注册顺序执行");
            Assert.AreEqual("HighPriority_Effect", _executionLog[1], "RegistrationOrder 模式应按注册顺序执行");
        }

        #endregion

        #region StopEventOnSuccess 对优先级的影响

        [Test]
        public void Test_StopEventOnSuccess_SkipsLowerPriorityRules()
        {
            // 测试：StopEventOnSuccess 在效果池模式下的行为
            _engine.Policy.EnableEffectPool = true;
            _engine.Policy.EffectPoolFlushMode = EffectPoolFlushMode.AfterPump;

            Card card = _engine.CreateCard("card");

            // 注册高优先级规则，设置 StopEventOnSuccess
            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.TICK)
                .MatchRootAtSelf()
                .Priority(0)
                .StopPropagation()
                .DoInvoke((ctx, _) => _executionLog.Add("HighPriority_Effect"))
                .Build());

            // 注册低优先级规则
            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.TICK)
                .MatchRootAtSelf()
                .Priority(1)
                .DoInvoke((ctx, _) => _executionLog.Add("LowPriority_Effect"))
                .Build());

            card.Tick(0.016f);
            _engine.Pump();

            // 即使低优先级规则命中，也应该被跳过
            Assert.AreEqual(1, _executionLog.Count, "只有高优先级规则的效果应该执行");
            Assert.AreEqual("HighPriority_Effect", _executionLog[0]);
        }

        [Test]
        public void Test_StopEventOnSuccess_DoesNotAffectOtherEvents()
        {
            // 测试：StopEventOnSuccess 只影响同一事件，不影响其他事件的规则
            _engine.Policy.EnableEffectPool = true;

            Card card = _engine.CreateCard("card");

            // 注册 TICK 事件的规则，设置 StopEventOnSuccess
            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.TICK)
                .MatchRootAtSelf()
                .Priority(0)
                .StopPropagation()
                .DoInvoke((ctx, _) => _executionLog.Add("Tick_Effect"))
                .Build());

            // 注册另一个 TICK 事件的规则（应该被跳过）
            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.TICK)
                .MatchRootAtSelf()
                .Priority(1)
                .DoInvoke((ctx, _) => _executionLog.Add("Tick_Effect2"))
                .Build());

            // 注册 USE 事件的规则（应该执行）
            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.USE)
                .MatchRootAtSelf()
                .Priority(1)
                .DoInvoke((ctx, _) => _executionLog.Add("Use_Effect"))
                .Build());

            card.Tick(0.016f);
            card.Use();
            _engine.Pump();

            // 验证：Tick 事件被中止，但 Use 事件正常执行
            Assert.AreEqual(2, _executionLog.Count, "应该执行 Tick_Effect 和 Use_Effect");
            Assert.IsTrue(_executionLog.Contains("Tick_Effect"));
            Assert.IsFalse(_executionLog.Contains("Tick_Effect2"), "同一事件的低优先级规则应被跳过");
            Assert.IsTrue(_executionLog.Contains("Use_Effect"));
        }

        #endregion

        #region 多事件场景中的优先级

        [Test]
        public void Test_MultipleEventsWithEffectPool_GlobalPrioritySort()
        {
            // 测试：多个事件触发时，效果池保证全局优先级排序
            // 即使事件的顺序不同，优先级高的规则效果总是先执行
            _engine.Policy.EnableEffectPool = true;
            _engine.Policy.EffectPoolFlushMode = EffectPoolFlushMode.AfterPump;

            Card card = _engine.CreateCard("card");

            // 注册 TICK 事件的规则，低优先级
            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.TICK)
                .MatchRootAtSelf()
                .Priority(1)
                .DoInvoke((ctx, _) => _executionLog.Add("Tick_LowPriority"))
                .Build());

            // 注册 USE 事件的规则，高优先级
            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.USE)
                .MatchRootAtSelf()
                .Priority(0)
                .DoInvoke((ctx, _) => _executionLog.Add("Use_HighPriority"))
                .Build());

            // 触发 TICK 然后 USE，但 USE 优先级更高
            card.Tick(0.016f);
            card.Use();
            _engine.Pump();

            // 效果池模式：USE 的高优先级效果应先执行，尽管 TICK 先触发
            Assert.AreEqual(2, _executionLog.Count);
            Assert.AreEqual("Use_HighPriority", _executionLog[0], "高优先级的 USE 效果应先执行");
            Assert.AreEqual("Tick_LowPriority", _executionLog[1], "低优先级的 TICK 效果应后执行");
        }

        [Test]
        public void Test_MultipleEventsStreamMode_FollowsEventThenPriorityOrder()
        {
            // 测试：流式模式下，先处理完一个事件的所有规则，再处理下一个事件
            _engine.Policy.EnableEffectPool = false;

            Card card = _engine.CreateCard("card");

            // TICK 事件的规则
            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.TICK)
                .MatchRootAtSelf()
                .Priority(1)
                .DoInvoke((ctx, _) => _executionLog.Add("Tick_1"))
                .Build());

            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.TICK)
                .MatchRootAtSelf()
                .Priority(0)
                .DoInvoke((ctx, _) => _executionLog.Add("Tick_0"))
                .Build());

            // USE 事件的规则
            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.USE)
                .MatchRootAtSelf()
                .Priority(0)
                .DoInvoke((ctx, _) => _executionLog.Add("Use_0"))
                .Build());

            card.Tick(0.016f);
            card.Use();
            _engine.Pump();

            // 流式模式：TICK 事件的规则先执行（按优先级），然后 USE 事件的规则
            Assert.AreEqual(3, _executionLog.Count);
            Assert.AreEqual("Tick_0", _executionLog[0], "TICK 高优先级规则先执行");
            Assert.AreEqual("Tick_1", _executionLog[1], "TICK 低优先级规则次之");
            Assert.AreEqual("Use_0", _executionLog[2], "USE 事件规则最后执行");
        }

        #endregion

        #region 嵌套事件与优先级交互

        [Test]
        public void Test_NestedEvents_EffectPoolMode()
        {
            // 测试：效果执行过程中触发新事件，效果池如何处理优先级
            _engine.Policy.EnableEffectPool = true;
            _engine.Policy.EffectPoolFlushMode = EffectPoolFlushMode.AfterPump;

            Card card = _engine.CreateCard("card");

            // TICK 事件的规则：执行时触发 USE 事件
            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.TICK)
                .MatchRootAtSelf()
                .Priority(1)
                .DoInvoke((ctx, _) =>
                {
                    _executionLog.Add("Tick_Effect");
                    ctx.Source.Use(); // 在规则执行中触发 USE 事件
                })
                .Build());

            // USE 事件的规则，高优先级
            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.USE)
                .MatchRootAtSelf()
                .Priority(0)
                .DoInvoke((ctx, _) => _executionLog.Add("Use_HighPriority"))
                .Build());

            card.Tick(0.016f);
            _engine.Pump();

            // 效果池模式：嵌套事件产生的效果也会加入全局池
            // 由于 USE 优先级更高，其效果可能被排到前面
            // 但具体顺序取决于何时加入池以及是否立即刷新
            Assert.AreEqual(2, _executionLog.Count);
            Assert.IsTrue(_executionLog.Contains("Tick_Effect"));
            Assert.IsTrue(_executionLog.Contains("Use_HighPriority"));
        }

        [Test]
        public void Test_NestedEvents_StreamMode()
        {
            // 测试：流式模式下嵌套事件的处理
            _engine.Policy.EnableEffectPool = false;

            Card card = _engine.CreateCard("card");

            // TICK 事件的规则：执行时触发 USE 事件
            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.TICK)
                .MatchRootAtSelf()
                .Priority(0)
                .DoInvoke((ctx, _) =>
                {
                    _executionLog.Add("Tick_Effect");
                    ctx.Source.Use();
                })
                .Build());

            // USE 事件的规则
            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.USE)
                .MatchRootAtSelf()
                .Priority(0)
                .DoInvoke((ctx, _) => _executionLog.Add("Use_Effect"))
                .Build());

            card.Tick(0.016f);
            _engine.Pump();

            // 流式模式：TICK 规则先执行，在执行中触发 USE 事件
            // 新事件被加到队列，在当前事件处理完后处理
            Assert.AreEqual(2, _executionLog.Count);
            Assert.AreEqual("Tick_Effect", _executionLog[0], "TICK 效果先执行");
            Assert.AreEqual("Use_Effect", _executionLog[1], "嵌套的 USE 效果后执行");
        }

        #endregion

        #region 效果索引顺序保证

        [Test]
        public void Test_EffectIndexOrder_WithinSamePriorityAndOrder()
        {
            // 测试：同优先级、同注册顺序的规则，其多个效果按索引顺序执行
            _engine.Policy.EnableEffectPool = true;

            Card card = _engine.CreateCard("card");

            // 单个规则，多个效果
            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.TICK)
                .MatchRootAtSelf()
                .Priority(0)
                .DoInvoke((ctx, _) => _executionLog.Add("Effect_0"))
                .DoInvoke((ctx, _) => _executionLog.Add("Effect_1"))
                .DoInvoke((ctx, _) => _executionLog.Add("Effect_2"))
                .Build());

            card.Tick(0.016f);
            _engine.Pump();

            // 同一规则的效果应按定义顺序执行
            Assert.AreEqual(3, _executionLog.Count);
            Assert.AreEqual("Effect_0", _executionLog[0]);
            Assert.AreEqual("Effect_1", _executionLog[1]);
            Assert.AreEqual("Effect_2", _executionLog[2]);
        }

        [Test]
        public void Test_ComplexPriorityScenario()
        {
            // 综合测试：复杂的优先级场景
            // 规则A：优先级 0，2个效果
            // 规则B：优先级 1，3个效果
            // 规则C：优先级 0，1个效果
            // 期望顺序：A的效果（优先级0，先注册）→ C的效果（优先级0，后注册）→ B的效果（优先级1）

            _engine.Policy.EnableEffectPool = true;

            Card card = _engine.CreateCard("card");

            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.TICK)
                .MatchRootAtSelf()
                .Priority(0)
                .DoInvoke((ctx, _) => _executionLog.Add("A1"))
                .DoInvoke((ctx, _) => _executionLog.Add("A2"))
                .Build());

            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.TICK)
                .MatchRootAtSelf()
                .Priority(1)
                .DoInvoke((ctx, _) => _executionLog.Add("B1"))
                .DoInvoke((ctx, _) => _executionLog.Add("B2"))
                .DoInvoke((ctx, _) => _executionLog.Add("B3"))
                .Build());

            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.TICK)
                .MatchRootAtSelf()
                .Priority(0)
                .DoInvoke((ctx, _) => _executionLog.Add("C1"))
                .Build());

            card.Tick(0.016f);
            _engine.Pump();

            Assert.AreEqual(6, _executionLog.Count);
            Assert.AreEqual("A1", _executionLog[0], "A的效果先执行（优先级0，最早注册）");
            Assert.AreEqual("A2", _executionLog[1]);
            Assert.AreEqual("C1", _executionLog[2], "C的效果次之（优先级0，后注册）");
            Assert.AreEqual("B1", _executionLog[3], "B的效果最后（优先级1）");
            Assert.AreEqual("B2", _executionLog[4]);
            Assert.AreEqual("B3", _executionLog[5]);
        }

        #endregion

        #region 复杂场景：多事件交替 + 嵌套 + StopEventOnSuccess

        [Test]
        public void Test_MultipleAlternatingEvents_WithStopPropagation()
        {
            // 复杂测试：多个事件交替触发，其中某些规则设置了 StopEventOnSuccess
            // 确保 StopEventOnSuccess 不会影响其他事件的规则执行
            //
            // 场景：
            // - TICK 事件有 3 个规则（优先级 0,1,2），其中优先级0的规则设置 StopEventOnSuccess
            // - USE 事件有 2 个规则（优先级 0,1）
            // - 在 TICK 规则执行中触发 USE 事件
            //
            // 期望执行顺序：
            // 1. TICK 优先级0规则执行 → StopEventOnSuccess 触发 → 跳过 TICK 优先级1,2规则
            // 2. USE 优先级0,1规则都执行（不应被 TICK 的 StopEventOnSuccess 影响）

            _engine.Policy.EnableEffectPool = true;
            _engine.Policy.EffectPoolFlushMode = EffectPoolFlushMode.AfterPump;

            Card card = _engine.CreateCard("card");

            // TICK 事件的规则：优先级 0，设置 StopEventOnSuccess
            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.TICK)
                .MatchRootAtSelf()
                .Priority(0)
                .StopPropagation()
                .DoInvoke((ctx, _) =>
                {
                    _executionLog.Add("Tick_P0_StopEvent");
                    // 在执行中触发 USE 事件
                    ctx.Source.Use();
                })
                .Build());

            // TICK 事件的规则：优先级 1（应被跳过）
            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.TICK)
                .MatchRootAtSelf()
                .Priority(1)
                .DoInvoke((ctx, _) => _executionLog.Add("Tick_P1_ShouldSkip"))
                .Build());

            // TICK 事件的规则：优先级 2（应被跳过）
            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.TICK)
                .MatchRootAtSelf()
                .Priority(2)
                .DoInvoke((ctx, _) => _executionLog.Add("Tick_P2_ShouldSkip"))
                .Build());

            // USE 事件的规则：优先级 0
            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.USE)
                .MatchRootAtSelf()
                .Priority(0)
                .DoInvoke((ctx, _) => _executionLog.Add("Use_P0_ShouldExecute"))
                .Build());

            // USE 事件的规则：优先级 1
            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.USE)
                .MatchRootAtSelf()
                .Priority(1)
                .DoInvoke((ctx, _) => _executionLog.Add("Use_P1_ShouldExecute"))
                .Build());

            card.Tick(0.016f);
            _engine.Pump();

            // 验证结果
            Assert.AreEqual(3, _executionLog.Count, "应该有 3 个效果执行");
            Assert.AreEqual("Tick_P0_StopEvent", _executionLog[0], "TICK 优先级 0 规则先执行");
            
            // USE 事件的两个规则都应该执行（按优先级）
            Assert.IsTrue(_executionLog.Contains("Use_P0_ShouldExecute"), 
                "USE 优先级 0 规则应执行");
            Assert.IsTrue(_executionLog.Contains("Use_P1_ShouldExecute"), 
                "USE 优先级 1 规则应执行");
            
            // TICK 的低优先级规则不应被执行
            Assert.IsFalse(_executionLog.Contains("Tick_P1_ShouldSkip"), 
                "TICK 优先级 1 规则应被 StopEventOnSuccess 跳过");
            Assert.IsFalse(_executionLog.Contains("Tick_P2_ShouldSkip"), 
                "TICK 优先级 2 规则应被 StopEventOnSuccess 跳过");
        }

        [Test]
        public void Test_NestedEvents_WithMultipleStopPropagation()
        {
            // 复杂测试：嵌套事件中多个规则都设置了 StopEventOnSuccess
            // 验证每个事件的 StopEventOnSuccess 只影响该事件，不影响嵌套事件
            //
            // 场景：
            // - TICK 事件：规则 A（优先级0，StopEventOnSuccess）触发 USE 事件
            //            规则 B（优先级1）
            // - USE 事件：规则 C（优先级0，StopEventOnSuccess）
            //            规则 D（优先级1）
            //
            // 期望：
            // - TICK: A 执行 → StopEventOnSuccess → B 跳过
            // - USE: C 执行 → StopEventOnSuccess → D 跳过

            _engine.Policy.EnableEffectPool = true;
            _engine.Policy.EffectPoolFlushMode = EffectPoolFlushMode.AfterPump;

            Card card = _engine.CreateCard("card");

            // TICK 事件规则 A：优先级 0，StopEventOnSuccess，触发 USE
            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.TICK)
                .MatchRootAtSelf()
                .Priority(0)
                .StopPropagation()
                .DoInvoke((ctx, _) =>
                {
                    _executionLog.Add("A_Tick_P0_Stop");
                    ctx.Source.Use(); // 触发 USE 事件
                })
                .Build());

            // TICK 事件规则 B：优先级 1（应被跳过）
            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.TICK)
                .MatchRootAtSelf()
                .Priority(1)
                .DoInvoke((ctx, _) => _executionLog.Add("B_Tick_P1_Skip"))
                .Build());

            // USE 事件规则 C：优先级 0，StopEventOnSuccess
            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.USE)
                .MatchRootAtSelf()
                .Priority(0)
                .StopPropagation()
                .DoInvoke((ctx, _) => _executionLog.Add("C_Use_P0_Stop"))
                .Build());

            // USE 事件规则 D：优先级 1（应被跳过）
            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.USE)
                .MatchRootAtSelf()
                .Priority(1)
                .DoInvoke((ctx, _) => _executionLog.Add("D_Use_P1_Skip"))
                .Build());

            card.Tick(0.016f);
            _engine.Pump();

            // 验证结果
            Assert.AreEqual(2, _executionLog.Count, "应该有 2 个效果执行（A 和 C）");
            Assert.AreEqual("A_Tick_P0_Stop", _executionLog[0], "A 规则先执行（TICK 优先级0）");
            Assert.AreEqual("C_Use_P0_Stop", _executionLog[1], "C 规则次之执行（USE 优先级0）");
            
            // 验证被跳过的规则
            Assert.IsFalse(_executionLog.Contains("B_Tick_P1_Skip"), 
                "B 规则应被跳过（TICK 事件的 StopEventOnSuccess）");
            Assert.IsFalse(_executionLog.Contains("D_Use_P1_Skip"), 
                "D 规则应被跳过（USE 事件的 StopEventOnSuccess）");
        }

        [Test]
        public void Test_ComplexInterleaving_MultipleEventsWithStopPropagation()
        {
            // 复杂场景：多个事件交替触发，验证 StopEventOnSuccess 的隔离性
            //
            // 触发顺序：
            // 1. Tick(0.016f) - 入队
            // 2. Use() - 在 Tick 规则 A 中触发，入队
            // 3. Pump() 开始处理
            //    - 处理 Tick 事件：A（优先级0，StopEventOnSuccess）→ B 被跳过
            //    - 处理 Use 事件：C（优先级0）→ D（优先级1，StopEventOnSuccess）→ E 被跳过
            //
            // 期望：A, C, D 执行；B, E 被跳过

            _engine.Policy.EnableEffectPool = true;
            _engine.Policy.EffectPoolFlushMode = EffectPoolFlushMode.AfterPump;

            Card card = _engine.CreateCard("card");

            // TICK 规则 A：优先级 0，StopEventOnSuccess，触发 USE
            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.TICK)
                .MatchRootAtSelf()
                .Priority(0)
                .StopPropagation()
                .DoInvoke((ctx, _) =>
                {
                    _executionLog.Add("A");
                    ctx.Source.Use();
                })
                .Build());

            // TICK 规则 B：优先级 1（被 A 的 StopEventOnSuccess 跳过）
            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.TICK)
                .MatchRootAtSelf()
                .Priority(1)
                .DoInvoke((ctx, _) => _executionLog.Add("B"))
                .Build());

            // USE 规则 C：优先级 0（无 StopEventOnSuccess）
            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.USE)
                .MatchRootAtSelf()
                .Priority(0)
                .DoInvoke((ctx, _) => _executionLog.Add("C"))
                .Build());

            // USE 规则 D：优先级 1，StopEventOnSuccess
            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.USE)
                .MatchRootAtSelf()
                .Priority(1)
                .StopPropagation()
                .DoInvoke((ctx, _) => _executionLog.Add("D"))
                .Build());

            // USE 规则 E：优先级 2（被 D 的 StopEventOnSuccess 跳过）
            _engine.RegisterRule(new CardRuleBuilder()
                .On(CardEventTypes.USE)
                .MatchRootAtSelf()
                .Priority(2)
                .DoInvoke((ctx, _) => _executionLog.Add("E"))
                .Build());

            card.Tick(0.016f);
            _engine.Pump();

            // 验证结果：应该有 A, C, D 三个效果
            Assert.AreEqual(3, _executionLog.Count, "应该有 3 个效果（A, C, D）");
            Assert.AreEqual("A", _executionLog[0], "A 规则先执行（TICK 优先级0）");
            Assert.AreEqual("C", _executionLog[1], "C 规则次之执行（USE 优先级0）");
            Assert.AreEqual("D", _executionLog[2], "D 规则最后执行（USE 优先级1）");
            
            // 验证被跳过的规则
            Assert.IsFalse(_executionLog.Contains("B"), 
                "B 规则应被跳过（TICK 事件的 StopEventOnSuccess）");
            Assert.IsFalse(_executionLog.Contains("E"), 
                "E 规则应被跳过（USE 事件的 StopEventOnSuccess）");
        }

        #endregion
    }
}

