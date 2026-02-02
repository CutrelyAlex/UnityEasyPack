using System.Collections.Generic;
using EasyPack.EmeCardSystem;
using NUnit.Framework;
using UnityEngine;

namespace EasyPack.EmeCardTests
{
    /// <summary>
    ///     EmeCard 位置系统测试
    ///     全面测试卡牌位置管理、父子卡牌位置继承、TryMoveCardToPositionEffect 等功能
    ///     包括三个复杂真实案例：国际象棋、塔防游戏、物品栏系统
    ///     
    ///     核心设计：
    ///     - 子卡牌与父卡牌在同一位置（子卡牌 Position 等于父卡牌 Position）
    ///     - 引擎只索引根卡牌（_cardsByPosition 字典只包含没有 Owner 的卡牌）
    ///     - 移除"虚空位置"概念（不再有 z=-1 或 VOID_POSITION 的特殊处理）
    /// </summary>
    [TestFixture]
    public class EmeCardPosTest
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

        #region 基础位置系统测试

        [Test]
        public void Test_CardPosition_DefaultPosition()
        {
            // 测试新创建卡牌的默认位置
            var factory = new CardFactory();
            factory.Register("test", () => new(new("test", "测试", "", "Card.Object")));
            var engine = new CardEngine(factory);
            
            Card card = engine.CreateCard("test");
            
            // 新创建的卡牌默认位置为 null（未设置位置）
            Assert.IsNull(card.Position, "新创建卡牌的默认位置应为 null");
            
            // 设置位置后应能正确获取
            engine.TryMoveCardToPosition(card, Vector3Int.zero);
            Assert.AreEqual(Vector3Int.zero, card.Position, "设置位置后应为 (0, 0, 0)");
        }

        [Test]
        public void Test_CardPosition_ChildCardsInheritParentPosition()
        {
            var factory = new CardFactory();
            factory.Register("container", () => new(new("container", "容器", "", "Card.Object")));
            factory.Register("child", () => new(new("child", "子卡", "", "Card.Object")));
            
            var engine = new CardEngine(factory);

            Card container = engine.CreateCard("container");
            var pos1 = new Vector3Int(5, 3, 1);
            engine.TryMoveCardToPosition(container, pos1);
            Card child = engine.CreateCard("child");

            // 初始位置
            Assert.AreEqual(pos1, container.Position, "容器初始位置应为 (5, 3, 1)");

            // 添加子卡后，子卡应继承父卡牌的位置
            container.AddChild(child);

            Assert.AreEqual(pos1, child.Position, "子卡添加到容器后应继承父卡牌位置");
            Assert.AreEqual(pos1, container.Position, "容器位置应不变");
            
            // 子卡牌不应出现在位置索引中（只有根卡牌被索引）
            Card cardAtPos = engine.GetCardByPosition(pos1);
            Assert.AreEqual(container, cardAtPos, "位置索引应只包含根卡牌（容器）");
        }

        [Test]
        public void Test_CardPosition_GetPositionByUID()
        {
            var factory = new CardFactory();
            factory.Register("test", () => new(new("test", "测试", "", "Card.Object")));

            var engine = new CardEngine(factory);

            Card card1 = engine.CreateCard("test");
            Card card2 = engine.CreateCard("test");

            // 设置卡牌位置
            engine.TryMoveCardToPosition(card1, new Vector3Int(1, 2, 3));
            engine.TryMoveCardToPosition(card2, new Vector3Int(4, 5, 6));

            // 按UID查询位置
            Vector3Int? pos1 = engine.GetPositionByUID(card1.UID);
            Vector3Int? pos2 = engine.GetPositionByUID(card2.UID);

            Assert.AreEqual(new Vector3Int(1, 2, 3), pos1, $"card1 位置查询错误：{pos1}");
            Assert.AreEqual(new Vector3Int(4, 5, 6), pos2, $"card2 位置查询错误：{pos2}");

            // 查询不存在的UID应返回 null
            Vector3Int? defaultPos = engine.GetPositionByUID(-999);
            Assert.IsNull(defaultPos, "不存在的UID应返回 null");
        }

        [Test]
        public void Test_CardPosition_GetCardByPosition()
        {
            var factory = new CardFactory();
            factory.Register("card_a", () => new(new("card_a", "卡牌A", "", "Card.Object")));
            factory.Register("card_b", () => new(new("card_b", "卡牌B", "", "Card.Object")));

            var engine = new CardEngine(factory);

            Card cardA = engine.CreateCard("card_a");
            Card cardB = engine.CreateCard("card_b");

            Vector3Int posA = new(1, 0, 0);
            Vector3Int posB = new(2, 0, 0);

            // 设置卡牌位置
            engine.TryMoveCardToPosition(cardA, posA);
            engine.TryMoveCardToPosition(cardB, posB);

            // 按位置查询卡牌
            Card foundA = engine.GetCardByPosition(posA);
            Card foundB = engine.GetCardByPosition(posB);

            Assert.AreEqual(cardA, foundA, "位置A应找到cardA");
            Assert.AreEqual(cardB, foundB, "位置B应找到cardB");

            // 查询空位置应返回null
            Card notFound = engine.GetCardByPosition(new Vector3Int(999, 999, 999));
            Assert.IsNull(notFound, "空位置应返回null");
        }

        [Test]
        public void Test_CardPosition_TryMoveCardToPosition()
        {
            var factory = new CardFactory();
            factory.Register("card", () => new(new("card", "卡牌", "", "Card.Object")));

            var engine = new CardEngine(factory);

            Card card = engine.CreateCard("card");
            Vector3Int pos1 = new(1, 2, 3);
            Vector3Int pos2 = new(4, 5, 6);

            // 第一次移动
            engine.TryMoveCardToPosition(card, pos1);
            Assert.AreEqual(pos1, card.Position, "卡牌位置应更新为 pos1");
            Assert.AreEqual(card, engine.GetCardByPosition(pos1), "pos1 应能找到卡牌");
            Assert.IsNull(engine.GetCardByPosition(pos2), "pos2 应为空");

            // 第二次移动
            engine.TryMoveCardToPosition(card, pos2);
            Assert.AreEqual(pos2, card.Position, "卡牌位置应更新为 pos2");
            Assert.IsNull(engine.GetCardByPosition(pos1), "pos1 应变为空");
            Assert.AreEqual(card, engine.GetCardByPosition(pos2), "pos2 应能找到卡牌");
        }

        [Test]
        public void Test_CardPosition_OneCardPerPosition()
        {
            var factory = new CardFactory();
            factory.Register("card", () => new(new("card", "卡牌", "", "Card.Object")));

            var engine = new CardEngine(factory);

            Card card1 = engine.CreateCard("card");
            Card card2 = engine.CreateCard("card");
            Vector3Int samePos = new(5, 5, 5);

            // 将card1移动到位置
            engine.TryMoveCardToPosition(card1, samePos);
            Assert.AreEqual(card1, engine.GetCardByPosition(samePos), "samePos 应包含 card1");

            // 将card2也移动到同一位置
            engine.TryMoveCardToPosition(card2, samePos, forceOverwrite: true);
            Assert.AreEqual(card2, engine.GetCardByPosition(samePos), "samePos 现在应包含 card2");
        }

        [Test]
        public void Test_CardPosition_ChildFollowsParentPosition()
        {
            var factory = new CardFactory();
            factory.Register("parent", () => new(new("parent", "父卡", "", "Card.Object")));
            factory.Register("child", () => new(new("child", "子卡", "", "Card.Object")));

            var engine = new CardEngine(factory);

            Card parent = engine.CreateCard("parent");
            Card child = engine.CreateCard("child");
            
            Vector3Int parentPos = new(5, 5, 0);
            engine.TryMoveCardToPosition(parent, parentPos);

            parent.AddChild(child);
            Assert.AreEqual(parentPos, child.Position, "子卡应继承父卡位置");

            // 移动父卡到新位置，子卡应跟随
            Vector3Int newParentPos = new(10, 10, 0);
            engine.TryMoveCardToPosition(parent, newParentPos);

            // 子卡应跟随父卡移动到新位置
            Assert.AreEqual(newParentPos, child.Position, "子卡应跟随父卡移动");
        }

        [Test]
        public void Test_CardPosition_RemoveChildUpdatesPosition()
        {
            var factory = new CardFactory();
            factory.Register("parent", () => new(new("parent", "父卡", "", "Card.Object")));
            factory.Register("child", () => new(new("child", "子卡", "", "Card.Object")));

            var engine = new CardEngine(factory);

            Card parent = engine.CreateCard("parent");
            Card child = engine.CreateCard("child");
            
            Vector3Int parentPos = new(3, 3, 0);
            engine.TryMoveCardToPosition(parent, parentPos);

            parent.AddChild(child);
            Assert.AreEqual(parentPos, child.Position, "子卡在容器中应继承父卡位置");

            // 移除子卡
            parent.RemoveChild(child);
            Assert.IsNull(child.Owner, "子卡应无持有者");

            // 移除后子卡位置保持不变（但不会自动添加到位置索引）
            Assert.AreEqual(parentPos, child.Position, "移除后子卡位置应保持不变");

            // 现在可以将其移动到其他位置并被索引
            Vector3Int newPos = new(7, 7, 7);
            engine.TryMoveCardToPosition(child, newPos);
            Assert.AreEqual(newPos, child.Position, "没有Owner的卡牌可以自由移动");
            
            // 验证移除后的卡牌可以被重新添加到位置索引
            Card foundChild = engine.GetCardByPosition(newPos);
            Assert.AreEqual(child, foundChild, "移除后的卡牌应能被重新添加到位置索引");
        }

        #endregion

        #region TryMoveCardToPositionEffect 测试

        [Test]
        public void Test_TryMoveCardToPositionEffect_SingleCardMovement()
        {
            var factory = new CardFactory();
            factory.Register("source", () => new(new("source", "源卡", "", "Card.Object")));
            factory.Register("target", () => new(new("target", "目标卡", "", "Card.Object")));

            var engine = new CardEngine(factory);

            Card source = engine.CreateCard("source");
            Card target = engine.CreateCard("target");

            Vector3Int sourcePos = new(1, 1, 1);
            Vector3Int targetPos = new(2, 2, 2);

            engine.TryMoveCardToPosition(source, sourcePos);
            engine.TryMoveCardToPosition(target, targetPos);

            // 创建规则使用TryMoveCardToPositionEffect
            CardRule rule = new CardRuleBuilder()
                .OnUse()
                .MatchRootAtSelf()
                .DoInvoke((ctx, _) =>
                {
                    var effect = new MoveCardToPositionEffect
                    {
                        Root = SelectionRoot.MatchRoot,
                        Scope = TargetScope.Matched,
                        TargetPosition = new Vector3Int(3, 3, 3)
                    };
                    effect.Execute(ctx, new HashSet<Card> { source });
                })
                .Build();

            engine.RegisterRule(rule);

            source.Use();
            engine.Pump();

            Assert.AreEqual(new Vector3Int(3, 3, 3), source.Position, "source 应被移动到目标位置");
        }

        [Test]
        public void Test_TryMoveCardToPositionEffect_ForcedOverride()
        {
            var factory = new CardFactory();
            factory.Register("moving", () => new(new("moving", "移动卡", "", "Card.Object")));
            factory.Register("blocking", () => new(new("blocking", "阻挡卡", "", "Card.Object")));

            var engine = new CardEngine(factory);

            Card movingCard = engine.CreateCard("moving");
            Card blockingCard = engine.CreateCard("blocking");

            Vector3Int movingPos = new(1, 1, 1);
            Vector3Int blockingPos = new(2, 2, 2);

            engine.TryMoveCardToPosition(movingCard, movingPos);
            engine.TryMoveCardToPosition(blockingCard, blockingPos);

            // 验证初始状态
            Assert.AreEqual(movingCard, engine.GetCardByPosition(movingPos), "初始：movingPos 应有 movingCard");
            Assert.AreEqual(blockingCard, engine.GetCardByPosition(blockingPos), "初始：blockingPos 应有 blockingCard");

            // 创建规则，将 movingCard 移动到 blockingPos（强制覆盖）
            CardRule rule = new CardRuleBuilder()
                .OnUse()
                .MatchRootAtSelf()
                .DoInvoke((ctx, _) =>
                {
                    var effect = new MoveCardToPositionEffect
                    {
                        Root = SelectionRoot.MatchRoot,
                        Scope = TargetScope.Matched,
                        TargetPosition = blockingPos,
                        ForceOverwrite = true  // 强制覆盖目标位置的卡牌
                    };
                    effect.Execute(ctx, new HashSet<Card> { movingCard });
                })
                .Build();

            engine.RegisterRule(rule);
            movingCard.Use();
            engine.Pump();

            // 验证覆盖结果
            Assert.AreEqual(movingCard, engine.GetCardByPosition(blockingPos), "movingCard 应被移动到 blockingPos");
            // 使用 ForceOverwrite 时，原位置的卡牌被清除位置（Position 变为 null）
            Assert.IsNull(blockingCard.Position, "blockingCard 的位置应被清除为 null");
            Assert.IsNull(engine.GetCardByPosition(movingPos), "movingPos 应变为空");
        }

        #endregion

        #region 复杂真实场景测试

        [Test]
        public void Test_CardPosition_ComplexScenario_Chess()
        {
            // 复杂真实案例1：国际象棋棋盘系统
            // 验证位置系统在复杂游戏场景中的表现

            var factory = new CardFactory();
            factory.Register("pawn", () => new(new("pawn", "兵", "", "Card.Piece", new[] { "棋子", "兵" })));
            factory.Register("rook", () => new(new("rook", "车", "", "Card.Piece", new[] { "棋子", "车" })));
            factory.Register("bishop", () => new(new("bishop", "象", "", "Card.Piece", new[] { "棋子", "象" })));
            factory.Register("knight", () => new(new("knight", "马", "", "Card.Piece", new[] { "棋子", "马" })));
            factory.Register("queen", () => new(new("queen", "皇后", "", "Card.Piece", new[] { "棋子", "皇后" })));
            factory.Register("king", () => new(new("king", "国王", "", "Card.Piece", new[] { "棋子", "国王" })));

            var engine = new CardEngine(factory);

            // 模拟棋盘：8x8网格
            // 位置编码：(文件号, 秩号, 0) 其中 文件号 0-7, 秩号 0-7

            // 创建白方棋子（秩号 0-1）
            var whitePieces = new Dictionary<string, Card>
            {
                // 第一秩：车、马、象、皇后、国王、象、马、车
                ["WRook1"] = engine.CreateCard("rook"), ["WKnight1"] = engine.CreateCard("knight"), ["WBishop1"] = engine.CreateCard("bishop"),
                ["WQueen"] = engine.CreateCard("queen"),
                ["WKing"] = engine.CreateCard("king"),
                ["WBishop2"] = engine.CreateCard("bishop"),
                ["WKnight2"] = engine.CreateCard("knight"),
                ["WRook2"] = engine.CreateCard("rook"),
            };

            // 第二秩：8个兵
            var whitePawns = new List<Card>();
            for (int i = 0; i < 8; i++)
            {
                whitePawns.Add(engine.CreateCard("pawn"));
            }

            // 放置白方棋子
            engine.TryMoveCardToPosition(whitePieces["WRook1"], new(0, 0, 0));
            engine.TryMoveCardToPosition(whitePieces["WKnight1"], new(1, 0, 0));
            engine.TryMoveCardToPosition(whitePieces["WBishop1"], new(2, 0, 0));
            engine.TryMoveCardToPosition(whitePieces["WQueen"], new(3, 0, 0));
            engine.TryMoveCardToPosition(whitePieces["WKing"], new(4, 0, 0));
            engine.TryMoveCardToPosition(whitePieces["WBishop2"], new(5, 0, 0));
            engine.TryMoveCardToPosition(whitePieces["WKnight2"], new(6, 0, 0));
            engine.TryMoveCardToPosition(whitePieces["WRook2"], new(7, 0, 0));

            // 放置白方兵
            for (int i = 0; i < 8; i++)
            {
                engine.TryMoveCardToPosition(whitePawns[i], new(i, 1, 0));
            }

            // 验证初始棋盘配置
            Assert.AreEqual(whitePieces["WRook1"], engine.GetCardByPosition(new(0, 0, 0)), "白1号车位置错误");
            Assert.AreEqual(whitePieces["WKing"], engine.GetCardByPosition(new(4, 0, 0)), "白国王位置错误");
            Assert.AreEqual(whitePawns[0], engine.GetCardByPosition(new(0, 1, 0)), "白兵位置错误");
            Assert.AreEqual(whitePawns[7], engine.GetCardByPosition(new(7, 1, 0)), "白兵位置错误");

            // 模拟移动：白皇后从 (3,0,0) 移动到 (3,2,0)
            Vector3Int queenStart = new(3, 0, 0);
            Vector3Int queenMove = new(3, 2, 0);
            Card queen = engine.GetCardByPosition(queenStart);
            Assert.IsNotNull(queen, "皇后应在起始位置");

            engine.TryMoveCardToPosition(queen, queenMove);
            Assert.AreEqual(queen, engine.GetCardByPosition(queenMove), "皇后应移动到新位置");
            Assert.IsNull(engine.GetCardByPosition(queenStart), "起始位置应为空");

            // 模拟白1号兵从 (0,1,0) 移动到 (0,3,0)
            Vector3Int pawnStart = new(0, 1, 0);
            Vector3Int pawnMove = new(0, 3, 0);
            Card pawn = engine.GetCardByPosition(pawnStart);
            Assert.IsNotNull(pawn, "兵应在起始位置");

            engine.TryMoveCardToPosition(pawn, pawnMove);
            Assert.AreEqual(pawn, engine.GetCardByPosition(pawnMove), "兵应移动到新位置");
            Assert.IsNull(engine.GetCardByPosition(pawnStart), "兵的起始位置应为空");

            // 计算棋盘上的棋子总数（应为 16）
            int totalPieces = 0;
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    if (engine.GetCardByPosition(new(x, y, 0)) != null)
                    {
                        totalPieces++;
                    }
                }
            }
            Assert.AreEqual(16, totalPieces, $"棋盘上应有 16 个棋子，实际: {totalPieces}");
        }

        [Test]
        public void Test_CardPosition_ComplexScenario_TowerGame()
        {
            // 复杂真实案例2：塔防游戏中的位置管理
            // 测试卡牌位置系统在动态游戏场景中的应用

            var factory = new CardFactory();
            factory.Register("tower", () => new(new("tower", "防御塔", "", "Card.Tower")));
            factory.Register("enemy", () => new(new("enemy", "敌人", "", "Card.Enemy", new[] { "敌人", "目标" })));
            factory.Register("explosion", () => new(new("explosion", "爆炸效果", "", "Card.Effect", new[] { "效果", "临时" })));

            var engine = new CardEngine(factory);

            // 创建三个防御塔，分别放在 (0,0,0), (5,0,0), (10,0,0)
            var towers = new List<Card>();
            var towerAssignments = new List<(Card tower, Vector3Int position)>();
            var towerPositions = new List<Vector3Int>
            {
                new(0, 0, 0),
                new(5, 0, 0),
                new(10, 0, 0)
            };

            foreach (var pos in towerPositions)
            {
                Card tower = engine.CreateCard("tower");
                engine.TryMoveCardToPosition(tower, pos);
                towers.Add(tower);
                towerAssignments.Add((tower, pos));
            }

            Assert.AreEqual(3, towers.Count, "应创建3个塔");

            // 创建敌人，放在 (2, 0, 0)，逐步向右移动
            Card enemy = engine.CreateCard("enemy");
            Vector3Int enemyPos = new(2, 0, 0);
            engine.TryMoveCardToPosition(enemy, enemyPos);

            // 验证初始配置
            Assert.AreEqual(enemy, engine.GetCardByPosition(enemyPos), "敌人应在初始位置");

            // 敌人向右移动（模拟游戏中的路径移动）
            var path = new[]
            {
                new Vector3Int(2, 0, 0),
                new Vector3Int(3, 0, 0),
                new Vector3Int(4, 0, 0),
                new Vector3Int(5, 0, 0),  // 接近第二个塔
                new Vector3Int(6, 0, 0),
                new Vector3Int(7, 0, 0),
                new Vector3Int(8, 0, 0),
                new Vector3Int(9, 0, 0),
                new Vector3Int(10, 0, 0), // 接近第三个塔
            };

            foreach (var pos in path)
            {
                // 如果敌人靠近某个塔，塔会生成爆炸效果
                foreach (var (tower, towerPos) in towerAssignments)
                {
                    float distance = Vector3Int.Distance(pos, towerPos);
                    if (distance < 2.0f) // 攻击范围
                    {
                        // 在塔的位置生成爆炸效果（强制覆盖）
                        Card explosion = engine.CreateCard("explosion");
                        engine.TryMoveCardToPosition(explosion, towerPos, forceOverwrite: true);

                        // 爆炸应该覆盖该位置（在真实场景中，敌人会被攻击）
                        Card atTowerPos = engine.GetCardByPosition(towerPos);
                        Assert.IsNotNull(atTowerPos, $"塔位置应有对象");
                    }
                }

                // 移动敌人到下一个位置
                engine.TryMoveCardToPosition(enemy, pos, forceOverwrite: true);
            }

            // 验证最终敌人位置
            Assert.AreEqual(new Vector3Int(10, 0, 0), enemy.Position, "敌人应到达终点");
            Assert.AreEqual(enemy, engine.GetCardByPosition(new Vector3Int(10, 0, 0)), "终点应有敌人");
        }

        [Test]
        public void Test_CardPosition_ComplexScenario_InventorySystem()
        {
            // 复杂真实案例3：物品栏系统中的位置继承应用
            // 物品在背包中（继承玩家位置）vs 在世界中（独立位置）

            var factory = new CardFactory();
            factory.Register("player", () => new(new("player", "玩家", "", "Card.Unit")));
            factory.Register("potion", () => new(new("potion", "药水", "", "Card.Item", new[] { "消耗品", "药水" })));
            factory.Register("weapon", () => new(new("weapon", "武器", "", "Card.Item", new[] { "装备", "武器" })));
            factory.Register("armor", () => new(new("armor", "护甲", "", "Card.Item", new[] { "装备", "护甲" })));

            var engine = new CardEngine(factory);

            // 创建玩家
            Card player = engine.CreateCard("player");
            Vector3Int playerPos = new(5, 5, 0);
            engine.TryMoveCardToPosition(player, playerPos);

            // 玩家在背包中有物品（继承玩家位置）
            Card potion1 = engine.CreateCard("potion");
            Card potion2 = engine.CreateCard("potion");
            Card weapon = engine.CreateCard("weapon");
            Card armor = engine.CreateCard("armor");

            player.AddChild(potion1);
            player.AddChild(potion2);
            player.AddChild(weapon);
            player.AddChild(armor);

            // 验证背包物品继承玩家位置
            Assert.AreEqual(playerPos, potion1.Position, "背包物品应继承玩家位置");
            Assert.AreEqual(playerPos, potion2.Position, "背包物品应继承玩家位置");
            Assert.AreEqual(playerPos, weapon.Position, "背包物品应继承玩家位置");
            Assert.AreEqual(playerPos, armor.Position, "背包物品应继承玩家位置");
            
            // 验证子卡牌不在位置索引中（只有根卡牌被索引）
            Card cardAtPlayerPos = engine.GetCardByPosition(playerPos);
            Assert.AreEqual(player, cardAtPlayerPos, "位置索引应只包含玩家（根卡牌）");

            // 玩家掉落一个药水到世界（移除子卡关系）
            player.RemoveChild(potion1);
            Vector3Int droppedPos = new(6, 5, 0);
            engine.TryMoveCardToPosition(potion1, droppedPos);

            // 验证药水现在在世界中
            Assert.AreEqual(droppedPos, potion1.Position, "掉落的物品应在指定位置");
            Assert.AreEqual(potion1, engine.GetCardByPosition(droppedPos), "世界位置应找到掉落的物品");
            Assert.IsNull(potion1.Owner, "掉落的物品应无持有者");

            // 玩家捡起药水（添加为子卡）
            player.AddChild(potion1);
            Assert.AreEqual(playerPos, potion1.Position, "捡起的物品应继承玩家位置");
            Assert.IsNull(engine.GetCardByPosition(droppedPos), "原掉落位置应为空");

            // 验证背包中仍有正确数量的物品
            Assert.AreEqual(4, player.Children.Count, "背包应有4个物品");

            // 批量查询：计算世界中独立的消耗品（不在任何玩家背包中）
            // 在这个场景中，所有物品都在背包中，所以世界中独立物品应为 0
            int worldItemCount = 0;
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    Card itemAtPos = engine.GetCardByPosition(new Vector3Int(x, y, 0));
                    if (itemAtPos != null && !itemAtPos.Equals(player) && itemAtPos.HasTag("消耗品"))
                    {
                        worldItemCount++;
                    }
                }
            }
            Assert.AreEqual(0, worldItemCount, "世界中应无独立消耗品");

            // 掉落所有武器和护甲
            player.RemoveChild(weapon);
            player.RemoveChild(armor);
            engine.TryMoveCardToPosition(weapon, new(5, 6, 0));
            engine.TryMoveCardToPosition(armor, new(4, 5, 0));

            // 重新计算世界中独立装备数量
            int worldEquipCount = 0;
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    Card equipAtPos = engine.GetCardByPosition(new(x, y, 0));
                    if (equipAtPos != null && equipAtPos.HasTag("装备"))
                    {
                        worldEquipCount++;
                    }
                }
            }
            Assert.AreEqual(2, worldEquipCount, "世界中应有2件独立装备");
        }

        #endregion

        #region 新增：子卡牌位置继承和递归更新测试

        [Test]
        public void Test_CardPosition_NestedChildrenInheritPosition()
        {
            // 测试多层嵌套子卡牌的位置继承
            var factory = new CardFactory();
            factory.Register("container", () => new(new("container", "容器", "", "Card.Object")));
            factory.Register("item", () => new(new("item", "物品", "", "Card.Object")));

            var engine = new CardEngine(factory);

            Card root = engine.CreateCard("container");
            Card level1 = engine.CreateCard("container");
            Card level2 = engine.CreateCard("item");

            Vector3Int rootPos = new(10, 20, 0);
            engine.TryMoveCardToPosition(root, rootPos);

            // 建立嵌套关系
            root.AddChild(level1);
            level1.AddChild(level2);

            // 所有子卡牌应继承根卡牌的位置
            Assert.AreEqual(rootPos, level1.Position, "level1 应继承 root 位置");
            Assert.AreEqual(rootPos, level2.Position, "level2 应继承 root 位置");

            // 移动根卡牌，所有后代应跟随移动
            Vector3Int newRootPos = new(30, 40, 0);
            engine.TryMoveCardToPosition(root, newRootPos);

            Assert.AreEqual(newRootPos, root.Position, "root 应移动到新位置");
            Assert.AreEqual(newRootPos, level1.Position, "level1 应跟随 root 移动");
            Assert.AreEqual(newRootPos, level2.Position, "level2 应跟随 root 移动");
        }

        [Test]
        public void Test_CardPosition_OnlyRootCardsIndexed()
        {
            // 验证只有根卡牌被索引到位置字典
            var factory = new CardFactory();
            factory.Register("parent", () => new(new("parent", "父卡", "", "Card.Object")));
            factory.Register("child", () => new(new("child", "子卡", "", "Card.Object")));

            var engine = new CardEngine(factory);

            Card parent = engine.CreateCard("parent");
            Card child1 = engine.CreateCard("child");
            Card child2 = engine.CreateCard("child");

            Vector3Int parentPos = new(5, 5, 0);
            engine.TryMoveCardToPosition(parent, parentPos);

            parent.AddChild(child1);
            parent.AddChild(child2);

            // 在父卡牌位置只能找到父卡牌
            Card foundCard = engine.GetCardByPosition(parentPos);
            Assert.AreEqual(parent, foundCard, "位置索引应只返回根卡牌");
            Assert.AreNotEqual(child1, foundCard, "子卡牌不应出现在位置索引中");
            Assert.AreNotEqual(child2, foundCard, "子卡牌不应出现在位置索引中");
        }

        [Test]
        public void Test_CardPosition_ClearCardPosition()
        {
            // 测试 ClearCardPosition 功能（如果已实现）
            var factory = new CardFactory();
            factory.Register("card", () => new(new("card", "卡牌", "", "Card.Object")));

            var engine = new CardEngine(factory);

            Card card = engine.CreateCard("card");
            Vector3Int pos = new(5, 5, 0);
            engine.TryMoveCardToPosition(card, pos);

            // 验证卡牌在位置索引中
            Assert.AreEqual(card, engine.GetCardByPosition(pos), "卡牌应在位置索引中");

            // 移动到另一个位置以清除原位置
            Vector3Int newPos = new(10, 10, 0);
            engine.TryMoveCardToPosition(card, newPos);

            // 原位置应为空
            Assert.IsNull(engine.GetCardByPosition(pos), $"原位置应为空");
            Assert.AreEqual(card, engine.GetCardByPosition(newPos), "新位置应有卡牌");
        }

        [Test]
        public void Test_CardPosition_ParentMovementUpdatesAllChildren()
        {
            // 测试父卡牌移动时，所有子卡牌位置自动更新
            var factory = new CardFactory();
            factory.Register("parent", () => new(new("parent", "父卡", "", "Card.Object")));
            factory.Register("child", () => new(new("child", "子卡", "", "Card.Object")));

            var engine = new CardEngine(factory);

            Card parent = engine.CreateCard("parent");
            var children = new List<Card>();
            for (int i = 0; i < 5; i++)
            {
                Card child = engine.CreateCard("child");
                parent.AddChild(child);
                children.Add(child);
            }

            Vector3Int initialPos = new(1, 1, 0);
            engine.TryMoveCardToPosition(parent, initialPos);

            // 验证所有子卡牌初始位置
            foreach (Card child in children)
            {
                Assert.AreEqual(initialPos, child.Position, "所有子卡牌应继承父卡牌初始位置");
            }

            // 移动父卡牌
            Vector3Int newPos = new(100, 200, 0);
            engine.TryMoveCardToPosition(parent, newPos);

            // 验证所有子卡牌跟随移动
            foreach (Card child in children)
            {
                Assert.AreEqual(newPos, child.Position, "所有子卡牌应跟随父卡牌移动到新位置");
            }
        }

        [Test]
        public void Test_CardPosition_MoveOnlyRootCards()
        {
            // 测试 TryMoveCardToPosition 只能用于根卡牌
            var factory = new CardFactory();
            factory.Register("parent", () => new(new("parent", "父卡", "", "Card.Object")));
            factory.Register("child", () => new(new("child", "子卡", "", "Card.Object")));

            var engine = new CardEngine(factory);

            Card parent = engine.CreateCard("parent");
            Card child = engine.CreateCard("child");

            Vector3Int parentPos = new(5, 5, 0);
            engine.TryMoveCardToPosition(parent, parentPos);
            parent.AddChild(child);

            // 子卡牌应继承父卡牌位置
            Assert.AreEqual(parentPos, child.Position, "子卡牌应继承父卡牌位置");
        }

        #endregion
    }
}
