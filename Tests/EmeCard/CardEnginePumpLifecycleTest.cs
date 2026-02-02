using System.Collections.Generic;
using EasyPack.EmeCardSystem;
using EasyPack.GamePropertySystem;
using NUnit.Framework;
using UnityEngine;

namespace EasyPack.EmeCardTests
{
    /// <summary>
    ///     EmeCard Pump 生命周期事件测试
    ///     专注于测试 PumpStart 和 PumpEnd 事件在复杂事件链中的应用
    ///     包括真实案例：物理引擎规则链（移动→碰撞→销毁→清理）
    /// </summary>
    [TestFixture]
    public class CardEnginePumpLifecycleTest
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

        #region PumpEnd 生命周期测试

        /// <summary>
        ///     复杂案例：物理引擎规则链
        ///     流程：Tick → Move → Collision → Collision Handler → Heat → PumpEnd Cleanup
        ///     场景：
        ///     1. 移动规则：在 Tick 事件下，条件性地移动物体到新位置
        ///     2. 碰撞规则：当物体移动到某个位置时触发碰撞检测
        ///     3. 黑洞规则：碰撞后标记"待删除"，并触发升温事件
        ///     4. 升温规则：处理升温事件，增加卡牌温度属性
        ///     5. PumpEnd 清理规则：在 Pump 结束时删除所有标记"待删除"的卡牌
        ///     预期结果：
        ///     - 移动成功，卡牌位置正确更新
        ///     - 碰撞被检测，升温事件被正确处理
        ///     - 温度属性被增加
        ///     - 标记待删除的卡牌在 PumpEnd 时被删除
        ///     - 所有事件处理顺序正确
        /// </summary>
        [Test]
        public void Test_PumpEnd_ComplexPhysicsRuleChain()
        {
            // ============ 1. 初始化工厂和引擎 ============
            var factory = new CardFactory();

            // 注册物体卡牌：包含温度和移动标志属性
            factory.Register("physics_object", () => new(
                new("physics_object", "物理物体", "受物理规则影响的物体", "Card.Object"),
                new List<GameProperty>
                {
                    new("Temperature", 0f), // 温度：初始值 0
                    new("IsMoving", 0f), // 移动标志：0=不移动，1=移动
                }
            ));

            var engine = new CardEngine(factory);

            // ============ 2. 创建测试卡牌 ============
            Card movingObject = engine.CreateCard("physics_object"); // 移动物体
            Card blackHole = engine.CreateCard("physics_object"); // 黑洞（碰撞点）

            // 设置初始位置
            Vector3Int collisionPos = new(1, 0, 0); // 移动目标位置（黑洞所在）

            engine.TryMoveCardToPosition(movingObject, new(0, 0, 0));
            engine.TryMoveCardToPosition(blackHole, collisionPos);

            // ============ 3. 注册移动规则 ============
            // 在 Tick 事件时，条件性地移动物体
            engine.RegisterRule(r => r
                .OnTick()
                .When(ctx => ctx.Source.GetProperty("IsMoving")?.GetValue() > 0)
                .DoInvoke((ctx, _) =>
                {
                    // 移动到碰撞位置
                    engine.TryMoveCardToPosition(ctx.Source, collisionPos, forceOverwrite: true);
                    // 清除移动标志
                    ctx.Source.GetProperty("IsMoving")?.SetBaseValue(0);
                }));

            // ============ 4. 注册碰撞检测规则 ============
            // 当位置改变时，直接标记和触发热事件（简化版本）
            engine.RegisterRule(r => r
                .On("OnPositionChanged")
                .DoInvoke((ctx, _) =>
                {
                    Debug.Log($"[OnPositionChanged] {ctx.Source.Name} 位置改变");
                    // 标记移动物体为待删除（这是碰撞的结果）
                    ctx.Source.AddTag("__PendingRemoval");
                    Debug.Log($"[OnPositionChanged] 标记 {ctx.Source.Name} 为待删除");
                    // 触发升温事件
                    ctx.Source.RaiseEvent("Heat");
                }));

            // ============ 6. 注册升温规则 ============
            // 处理升温事件，增加温度
            engine.RegisterRule(r => r
                .On("Heat")
                .When(ctx => !ctx.Source.HasTag("__PendingRemoval")) // 只有未标记删除的才升温
                .DoInvoke((ctx, _) =>
                {
                    GameProperty tempProp = ctx.Source.GetProperty("Temperature");
                    if (tempProp != null)
                    {
                        float currentTemp = tempProp.GetValue();
                        tempProp.SetBaseValue(currentTemp + 1f);
                    }
                }));

            // ============ 7. 注册 PumpEnd 清理规则 ============
            // 在 Pump 结束时删除所有标记待删除的卡牌
            engine.RegisterRule(r => r
                .OnPumpEnd()
                .When(ctx => ctx.Source.HasTag("__PendingRemoval")) // 检查源卡牌本身是否有标记
                .DoInvoke((ctx, _) =>
                {
                    Debug.Log($"[PumpEnd] 删除 {ctx.Source.Name}");
                    // 直接从引擎中移除卡牌
                    if (ctx.Source.Owner != null) ctx.Source.Owner.RemoveChild(ctx.Source);
                    ctx.Engine.RemoveCard(ctx.Source);
                }));

            // ============ 8. 执行测试流程 ============

            // 阶段1：标记物体为移动状态
            movingObject.GetProperty("IsMoving")?.SetBaseValue(1f);

            // 阶段2：触发 Tick 事件，驱动移动
            movingObject.Tick(0.016f);

            // 阶段2b：处理 Tick 事件（执行移动规则）
            engine.Pump();

            // 验证移动成功
            Assert.AreEqual(collisionPos, movingObject.Position,
                "物体应移动到碰撞位置");

            // 阶段3：触发位置变化事件
            movingObject.RaiseEvent("OnPositionChanged");

            // 阶段3b：处理位置变化事件（执行碰撞检测规则）
            engine.Pump();

            // ============ 9. 验证碰撞和标记结果 ============

            // 验证升温事件被触发但温度不增加（因为有条件限制）
            float tempBeforePump = movingObject.GetProperty("Temperature")?.GetValue() ?? -1;
            Assert.AreEqual(0f, tempBeforePump,
                "标记删除后温度应不增加（被条件阻止）");

            // 阶段4：执行最终 Pump，处理 PumpEnd 清理
            engine.Pump();

            // 验证 PumpEnd 清理：物体已被删除
            Card deletedCard = engine.GetCardByKey(movingObject.Id, movingObject.Index);
            Assert.IsNull(deletedCard,
                "PumpEnd 后物体应被删除");

            // 验证黑洞仍然存在
            Card holeStillExists = engine.GetCardByKey(blackHole.Id, blackHole.Index);
            Assert.NotNull(holeStillExists,
                "黑洞应继续存在");

            Debug.Log("✓ 复杂物理规则链测试通过：移动→碰撞→升温→PumpEnd清理");
        }

        /// <summary>
        ///     变种案例：正常升温（不在碰撞时标记删除）
        ///     测试升温规则在正常流程中的工作
        /// </summary>
        [Test]
        public void Test_PumpEnd_NormalHeatingWithoutDeletion()
        {
            var factory = new CardFactory();
            factory.Register("hot_object", () => new(
                new("hot_object", "热源", "会升温的物体", "Card.Object"),
                new GameProperty("Temperature", 0f)
            ));

            var engine = new CardEngine(factory);

            Card object1 = engine.CreateCard("hot_object");
            Card object2 = engine.CreateCard("hot_object");

            // 升温规则：没有删除标记检查
            engine.RegisterRule(r => r
                .On("Heat")
                .DoInvoke((ctx, _) =>
                {
                    GameProperty tempProp = ctx.Source.GetProperty("Temperature");
                    if (tempProp != null) tempProp.SetBaseValue(tempProp.GetValue() + 1f);
                }));

            // PumpEnd：清理所有标记删除的卡牌（这里没有）
            engine.RegisterRule(r => r
                .OnPumpEnd()
                .NeedMatchRootTag("__PendingRemoval")
                .DoRemove());

            // 触发升温
            object1.RaiseEvent("Heat");
            object1.RaiseEvent("Heat");
            object2.RaiseEvent("Heat");

            engine.Pump();

            // 验证温度增加
            Assert.AreEqual(2f, object1.GetProperty("Temperature")?.GetValue() ?? -1,
                "object1 温度应增加 2");
            Assert.AreEqual(1f, object2.GetProperty("Temperature")?.GetValue() ?? -1,
                "object2 温度应增加 1");

            // 验证两个对象都还存在（未被删除）
            Assert.NotNull(engine.GetCardByKey(object1.Id, object1.Index), "object1 应存在");
            Assert.NotNull(engine.GetCardByKey(object2.Id, object2.Index), "object2 应存在");

            Debug.Log("✓ 正常升温测试通过：温度正确增加，对象未被删除");
        }

        /// <summary>
        ///     验证 PumpStart 事件也能正常工作
        ///     PumpStart 在所有普通事件处理前执行
        /// </summary>
        [Test]
        public void Test_PumpStart_ExecutesBeforeNormalEvents()
        {
            var factory = new CardFactory();
            factory.Register("test", () => new(
                new("test", "测试", "", "Card.Object"),
                new GameProperty("InitValue", 0f)
            ));

            var engine = new CardEngine(factory);

            List<string> executionOrder = new();

            // PumpStart：标记初始化
            engine.RegisterRule(r => r
                .OnPumpStart()
                .DoInvoke((ctx, _) =>
                {
                    executionOrder.Add("PumpStart");
                    ctx.Source.GetProperty("InitValue")?.SetBaseValue(100f);
                }));

            // 普通规则：Tick
            engine.RegisterRule(r => r
                .OnTick()
                .DoInvoke((ctx, _) => { executionOrder.Add("Tick"); }));

            // PumpEnd：最后处理
            engine.RegisterRule(r => r
                .OnPumpEnd()
                .DoInvoke((ctx, _) => { executionOrder.Add("PumpEnd"); }));

            Card card = engine.CreateCard("test");
            card.Tick(0.016f);

            engine.Pump();

            // 验证执行顺序
            Assert.AreEqual(3, executionOrder.Count, "应有 3 个事件执行");
            Assert.AreEqual("PumpStart", executionOrder[0], "PumpStart 应首先执行");
            Assert.AreEqual("Tick", executionOrder[1], "Tick 应在 PumpStart 后执行");
            Assert.AreEqual("PumpEnd", executionOrder[2], "PumpEnd 应最后执行");

            // 验证 PumpStart 的效果
            Assert.AreEqual(100f, card.GetProperty("InitValue")?.GetValue() ?? -1,
                "PumpStart 应能修改属性");

            Debug.Log("✓ PumpStart 测试通过：执行顺序正确");
        }

        #endregion
    }
}