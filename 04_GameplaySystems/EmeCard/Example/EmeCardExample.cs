using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using EasyPack.GamePropertySystem;
using EasyPack.EmeCardSystem;

namespace EasyPack
{
    /// <summary>
    /// EmeCard 系统使用案例展示
    /// </summary>
    public sealed class EmeCardExample : MonoBehaviour
    {
        private CardEngine _engine;
        private CardFactory _factory;

        private void Start()
        {
            Debug.Log("===== EmeCard 系统使用案例展示 =====");

            // 案例1: 初始化工厂和引擎
            ShowFactoryAndEngineInitialization();

            // 案例2: 创建卡牌模板
            ShowCardTemplateCreation();

            // 案例3: 搭建游戏世界
            ShowWorldSetup();

            // 案例4: 注册简单规则
            ShowSimpleRuleRegistration();

            // 案例5: 演示事件驱动
            ShowEventDrivenSystem();

            // 案例6: 演示规则效果
            ShowRuleEffects();

            // 案例7: 演示递归选择
            ShowRecursiveSelection();

            // 案例8: 演示复杂规则
            ShowComplexRules();

            // 案例9: 运行完整游戏流程
            ShowCompleteGameplay();

            Debug.Log("===== EmeCard 系统使用案例展示完成 =====");
        }

        /// <summary>
        /// 案例1: 初始化工厂和引擎
        /// </summary>
        private void ShowFactoryAndEngineInitialization()
        {
            Debug.Log("案例1: 初始化工厂和引擎");

            _factory = new CardFactory();
            _engine = new CardEngine(_factory);

            // 注册卡牌模板 - 使用简化构造函数（无属性）
            _factory.Register("世界", () =>
                new Card(new CardData("世界", "世界", "", CardCategory.Object), "世界"));

            _factory.Register("草地格", () =>
                new Card(new CardData("草地格", "草地格", "", CardCategory.Object), "草地"));

            _factory.Register("玩家", () =>
                new Card(new CardData("玩家", "玩家", "", CardCategory.Object), "玩家"));

            _factory.Register("树木", () =>
                new Card(new CardData("树木", "树木", "", CardCategory.Object), "树木", "可燃烧"));

            _factory.Register("木棍", () =>
                new Card(new CardData("木棍", "木棍", "", CardCategory.Object), "木棍"));

            _factory.Register("火", () =>
                new Card(new CardData("火", "火", "", CardCategory.Object), "火"));

            // 火把使用完整构造函数（带属性）
            _factory.Register("火把", () =>
                new Card(new CardData("火把", "火把", "", CardCategory.Object),
                    new List<GameProperty> { new("Ticks", 0f) }, "火把"));

            _factory.Register("灰烬", () =>
                new Card(new CardData("灰烬", "灰烬", "", CardCategory.Object), "灰烬"));

            _factory.Register("制作", () =>
                new Card(new CardData("制作", "制作", "", CardCategory.Action), "制作"));

            Debug.Log("工厂和引擎初始化完成，可用于创建和管理卡牌\n");
        }

        /// <summary>
        /// 案例2: 创建卡牌模板
        /// </summary>
        private void ShowCardTemplateCreation()
        {
            Debug.Log("案例2: 创建卡牌模板");

            // 演示创建不同类型的卡牌
            var simpleCard = new Card(new CardData("simple", "简单卡牌", "一张简单的卡牌", CardCategory.Object));
            Debug.Log($"创建了简单卡牌: {simpleCard.Name} (ID: {simpleCard.Id})");

            var taggedCard = new Card(new CardData("tagged", "带标签卡牌", "", CardCategory.Object), "武器", "近战");
            Debug.Log($"创建了带标签卡牌: {taggedCard.Name}，标签: {string.Join(", ", taggedCard.Tags)}");

            var propertyCard = new Card(
                new CardData("property", "带属性卡牌", "", CardCategory.Object),
                new List<GameProperty> { new("Health", 100f), new("Attack", 50f) }
            );
            Debug.Log($"创建了带属性卡牌: {propertyCard.Name}，属性数量: {propertyCard.Properties.Count}");

            Debug.Log("卡牌模板创建完成，可用于游戏中的各种实体\n");
        }

        /// <summary>
        /// 案例3: 搭建游戏世界
        /// </summary>
        private void ShowWorldSetup()
        {
            Debug.Log("案例3: 搭建游戏世界");

            var world = _engine.CreateCard("世界");
            var tileGrass = _engine.CreateCard("草地格");
            world.AddChild(tileGrass);

            var player = _engine.CreateCard("玩家");
            var tree = _engine.CreateCard("树木");
            var fire = _engine.CreateCard("火");
            var make = _engine.CreateCard("制作");

            tileGrass.AddChild(player);
            tileGrass.AddChild(tree);
            tileGrass.AddChild(fire);
            tileGrass.AddChild(make);

            DisplayCardHierarchy(world, "游戏世界结构");
            Debug.Log("游戏世界搭建完成，包含玩家、树木、火和制作工具\n");
        }

        /// <summary>
        /// 案例4: 注册简单规则
        /// </summary>
        private void ShowSimpleRuleRegistration()
        {
            Debug.Log("案例4: 注册简单规则");

            // 规则1：使用制作工具时，如果有玩家和树木，消耗树木创建木棍
            _engine.RegisterRule(b => b
                .On(CardEventType.Use)
                .When(ctx => ctx.Source.HasTag("制作"))
                .NeedTag("玩家")
                .NeedId("树木")
                .DoRemoveById("树木", take: 1)
                .DoCreate("木棍")
                .StopPropagation()
            );

            Debug.Log("注册了制作木棍的规则：使用制作工具 + 玩家 + 树木 → 消耗树木，创建木棍");

            // 规则2：使用制作工具时，如果有玩家、木棍和火，消耗木棍和火创建火把
            _engine.RegisterRule(b => b
                .On(CardEventType.Use)
                .When(ctx => ctx.Source.HasTag("制作"))
                .NeedTag("玩家")
                .NeedTag("木棍")
                .NeedTag("火")
                .DoRemoveByTag("木棍", take: 1)
                .DoRemoveByTag("火", take: 1)
                .DoCreate("火把")
                .StopPropagation()
            );

            Debug.Log("注册了制作火把的规则：使用制作工具 + 玩家 + 木棍 + 火 → 消耗木棍和火，创建火把");
            Debug.Log("简单规则注册完成\n");
        }

        /// <summary>
        /// 案例5: 演示事件驱动
        /// </summary>
        private void ShowEventDrivenSystem()
        {
            Debug.Log("案例5: 演示事件驱动");

            var testCard = _engine.CreateCard("玩家");
            int eventCount = 0;

            testCard.OnEvent += (source, evt) =>
            {
                eventCount++;
                Debug.Log($"收到事件: {evt.Type}，来源: {source.Id}，总事件数: {eventCount}");
            };

            // 触发不同类型的事件
            testCard.Use();
            testCard.Tick(1f);
            testCard.Custom("test_event");

            Debug.Log($"事件驱动演示完成，共触发 {eventCount} 个事件\n");
        }

        /// <summary>
        /// 案例6: 演示规则效果
        /// </summary>
        private void ShowRuleEffects()
        {
            Debug.Log("案例6: 演示规则效果");

            // 创建一个测试场景
            var testTile = _engine.CreateCard("草地格");
            var testPlayer = _engine.CreateCard("玩家");
            var testTree = _engine.CreateCard("树木");
            var testMake = _engine.CreateCard("制作");

            testTile.AddChild(testPlayer);
            testTile.AddChild(testTree);
            testTile.AddChild(testMake);

            DisplayCardHierarchy(testTile, "测试前");

            // 使用制作工具，应该触发规则创建木棍
            testMake.Use();
            _engine.Pump();

            DisplayCardHierarchy(testTile, "制作木棍后");

            Debug.Log("规则效果演示完成\n");
        }

        /// <summary>
        /// 案例7: 演示递归选择
        /// </summary>
        private void ShowRecursiveSelection()
        {
            Debug.Log("案例7: 演示递归选择");

            // 创建一个复杂的层次结构
            var root = _engine.CreateCard("世界");
            var area1 = _engine.CreateCard("草地格");
            var area2 = _engine.CreateCard("草地格");
            root.AddChild(area1);
            root.AddChild(area2);

            var player1 = _engine.CreateCard("玩家");
            var player2 = _engine.CreateCard("玩家");
            area1.AddChild(player1);
            area2.AddChild(player2);

            // 注册一个递归规则：检查整个世界是否有"夜晚"标签
            _engine.RegisterRule(b => b
                .On(CardEventType.Custom, "检查夜晚")
                .AtRoot()
                .NeedTagRecursive("夜晚", minCount: 1)
                .DoInvoke((ctx, matched) =>
                {
                    Debug.Log($"[递归选择] 在整个世界树中发现了 {matched.Count} 个夜晚标记");
                })
            );

            // 添加夜晚标签到其中一个区域
            area1.AddTag("夜晚");

            // 触发检查
            root.Custom("检查夜晚");
            _engine.Pump();

            Debug.Log("递归选择演示完成\n");
        }

        /// <summary>
        /// 案例8: 演示复杂规则
        /// </summary>
        private void ShowComplexRules()
        {
            Debug.Log("案例8: 演示复杂规则");

            // 注册火把燃烧规则
            _engine.RegisterRule(b => b
                .On(CardEventType.Tick)
                .AtSelf()
                .NeedTag("火把")
                .DoModifyTag("火把", "Ticks", 1f)
                .DoInvoke((ctx, matched) =>
                {
                    var torches = ctx.Container.Children.Where(c => c.HasTag("火把")).ToList();
                    var ticks = torches.Select(t => t.Properties?.FirstOrDefault()?.GetBaseValue() ?? 0f);
                    Debug.Log($"[火把燃烧] 火把Ticks: {string.Join(", ", ticks)}");
                })
            );

            // 注册燃尽规则
            _engine.RegisterRule(b => b
                .On(CardEventType.Tick)
                .AtSelf()
                .DoInvoke((ctx, matched) =>
                {
                    var torches = ctx.Container.Children
                        .Where(c => c.HasTag("火把") &&
                               c.Properties?.FirstOrDefault()?.GetBaseValue() >= 5f)
                        .ToList();

                    if (torches.Count == 0) return;

                    Debug.Log($"[燃尽] {torches.Count} 个火把燃尽");
                    foreach (var torch in torches)
                    {
                        torch.Owner?.RemoveChild(torch, force: false);
                        var ash = _factory.Create("灰烬");
                        ctx.Container.AddChild(ash);
                    }
                })
            );

            Debug.Log("复杂规则注册完成：火把燃烧和燃尽机制\n");
        }

        /// <summary>
        /// 案例9: 运行完整游戏流程
        /// </summary>
        private void ShowCompleteGameplay()
        {
            Debug.Log("案例9: 运行完整游戏流程");

            // 创建游戏世界
            var world = _engine.CreateCard("世界");
            var tileGrass = _engine.CreateCard("草地格");
            world.AddChild(tileGrass);

            var player = _engine.CreateCard("玩家");
            var tree = _engine.CreateCard("树木");
            var fire = _engine.CreateCard("火");
            var make = _engine.CreateCard("制作");

            tileGrass.AddChild(player);
            tileGrass.AddChild(tree);
            tileGrass.AddChild(fire);
            tileGrass.AddChild(make);

            DisplayCardHierarchy(tileGrass, "初始状态");

            // 1. 制作木棍
            Debug.Log("\n--- 制作木棍 ---");
            make.Use();
            _engine.Pump();
            DisplayCardHierarchy(tileGrass, "制作木棍后");

            // 2. 制作火把
            Debug.Log("\n--- 制作火把 ---");
            make.Use();
            _engine.Pump();
            DisplayCardHierarchy(tileGrass, "制作火把后");

            // 3. 火把燃烧过程
            Debug.Log("\n--- 火把燃烧过程 ---");
            for (int i = 1; i <= 6; i++)
            {
                Debug.Log($"\n[第 {i} 次 Tick]");
                tileGrass.Tick(1f);
                _engine.Pump();
                DisplayCardHierarchy(tileGrass, $"Tick {i} 后");
            }

            Debug.Log("完整游戏流程演示完成\n");
        }

        /// <summary>
        /// 显示卡牌层次结构
        /// </summary>
        private void DisplayCardHierarchy(Card root, string title)
        {
            Debug.Log($"[{title}] 卡牌层次结构:");
            DisplayCardRecursive(root, 0);
        }

        private void DisplayCardRecursive(Card card, int depth)
        {
            string indent = new(' ', depth * 2);
            string tags = card.Tags.Count > 0 ? $" [{string.Join(", ", card.Tags)}]" : "";
            string props = card.Properties.Count > 0 ?
                $" (属性: {string.Join(", ", card.Properties.Select(p => $"{p.ID}={p.GetValue()}"))})" : "";
            Debug.Log($"{indent}{card.Id}{tags}{props}");

            foreach (var child in card.Children)
            {
                DisplayCardRecursive(child, depth + 1);
            }
        }
    }
}