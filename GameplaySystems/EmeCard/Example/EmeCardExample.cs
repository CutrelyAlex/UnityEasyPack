using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasyPack
{
    /// <summary>
    /// EmeCard 系统完整示例：万物卡游戏
    /// 
    /// 演示内容：
    /// 1. 事件驱动系统（Tick/Use/Custom）
    /// 2. 新架构的便捷方法（AtSelf/NeedTag/DoRemoveTag等）
    /// 3. 容器锚点选择（AtSelf/AtParent/AtRoot）
    /// 4. 目标选择的三元组（SelectionRoot + TargetScope + FilterMode）
    /// 5. 卡牌工厂和规则引擎的使用
    /// 6. 递归选择和深度控制
    /// </summary>
    public sealed class EmeCardExample : MonoBehaviour
    {
        private CardEngine _engine;
        private CardFactory _factory;

        private void Start()
        {
            RunDemo();
        }

        private void RunDemo()
        {
            Debug.Log("=== EmeCard 新架构完整示例开始 ===\n");

            // 1. 初始化
            InitializeFactoryAndEngine();

            // 2. 搭建世界
            SetupWorld(out var world, out var tileGrass, out var player);

            // 3. 注册规则
            RegisterRules();

            // 4. 运行游戏流程
            RunGameplay(tileGrass, player);

            Debug.Log("\n=== EmeCard 新架构完整示例结束 ===");
        }

        /// <summary>
        /// 初始化工厂和引擎
        /// </summary>
        private void InitializeFactoryAndEngine()
        {
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
                    new List<GameProperty> { new GameProperty("Ticks", 0f) }, "火把"));
            
            _factory.Register("灰烬", () => 
                new Card(new CardData("灰烬", "灰烬", "", CardCategory.Object), "灰烬"));
            
            _factory.Register("制作", () => 
                new Card(new CardData("制作", "制作", "", CardCategory.Action), "制作"));
        }

        /// <summary>
        /// 搭建游戏世界
        /// </summary>
        private void SetupWorld(out Card world, out Card tileGrass, out Card player)
        {
            world = _engine.CreateCard("世界");
            tileGrass = _engine.CreateCard("草地格");
            world.AddChild(tileGrass);

            player = _engine.CreateCard("玩家");
            var tree = _engine.CreateCard("树木");
            var fire = _engine.CreateCard("火");
            var make = _engine.CreateCard("制作");

            tileGrass.AddChild(player);
            tileGrass.AddChild(tree);
            tileGrass.AddChild(fire);
            tileGrass.AddChild(make);

            PrintChildren(tileGrass, "初始状态");
        }

        /// <summary>
        /// 注册游戏规则
        /// </summary>
        private void RegisterRules()
        {
            // ==================== 规则1 ====================
            // Use(制作) + 同容器有玩家和树木 -> 产出木棍（消耗1个树木）
            // 演示：便捷方法 NeedTag, NeedId, DoRemoveId, DoCreate
            _engine.RegisterRule(b => b
                .On(CardEventType.Use)
                .When(ctx => ctx.Source.HasTag("制作"))
                .NeedTag("玩家")
                .NeedId("树木")
                .DoRemoveId("树木", take: 1)
                .DoCreate("木棍")
                .StopPropagation()
            );

            // ==================== 规则2 ====================
            // Use(制作) + 同容器有玩家、木棍、火 -> 产出火把（消耗1个木棍和1个火）
            // 演示：便捷方法 DoRemoveTag
            _engine.RegisterRule(b => b
                .On(CardEventType.Use)
                .When(ctx => ctx.Source.HasTag("制作"))
                .NeedTag("玩家")
                .NeedTag("木棍")
                .NeedTag("火")
                .DoRemoveTag("木棍", take: 1)
                .DoRemoveTag("火", take: 1)
                .DoCreate("火把")
                .StopPropagation()
            );

            // ==================== 规则3 ====================
            // Tick(Self) -> 所有火把的Ticks属性+1
            // 演示：AtSelf(), DoModifyTag()
            _engine.RegisterRule(b => b
                .On(CardEventType.Tick)
                .AtSelf()
                .NeedTag("火把")
                .DoModifyTag("火把", "Ticks", 1f)
                .DoInvoke((ctx, matched) =>
                {
                    var torches = ctx.Container.Children.Where(c => c.HasTag("火把")).ToList();
                    var ticks = torches.Select(t => t.Properties?.FirstOrDefault()?.GetBaseValue() ?? 0f);
                    Debug.Log($"[Tick] 火把燃烧进度: {string.Join(", ", ticks)}");
                })
            );

            // ==================== 规则4 ====================
            // Tick(Self) -> 燃尽Ticks>=5的火把，产出灰烬
            // 演示：AtSelf() 和自定义逻辑
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

            // ==================== 规则5 ====================
            // Use(玩家) -> 显示当前状态
            // 演示：简单的自定义逻辑
            _engine.RegisterRule(b => b
                .On(CardEventType.Use)
                .When(ctx => ctx.Source.HasTag("玩家"))
                .DoInvoke((ctx, _) =>
                {
                    var loc = ctx.Source.Owner;
                    var items = loc?.Children.Where(c => c != ctx.Source).Select(c => c.Id) ?? new string[0];
                    Debug.Log($"[玩家行动] 当前位置: {loc?.Id ?? "无"}, 周围物品: {string.Join(", ", items)}");
                })
            );

            // ==================== 规则6====================
            // 演示递归选择：AtRoot() + NeedTagRecursive()
            // 假设世界中任何地方有"夜晚"标签，就触发某个效果
            _engine.RegisterRule(b => b
                .On(CardEventType.Custom, "检查夜晚")
                .AtRoot()
                .NeedTagRecursive("夜晚", minCount: 1)
                .DoInvoke((ctx, matched) =>
                {
                    Debug.Log($"[夜晚检测] 在整个世界树中发现了 {matched.Count} 个夜晚标记");
                })
            );
        }

        /// <summary>
        /// 运行游戏流程
        /// </summary>
        private void RunGameplay(Card tileGrass, Card player)
        {
            // 1. 制作木棍
            Debug.Log("\n--- 制作木棍 ---");
            var make = tileGrass.Children.First(c => c.HasTag("制作"));
            make.Use();
            PrintChildren(tileGrass, "制作木棍后");

            // 2. 制作火把
            Debug.Log("\n--- 制作火把 ---");
            make.Use();
            PrintChildren(tileGrass, "制作火把后");

            // 3. 玩家查看状态
            Debug.Log("\n--- 玩家查看状态 ---");
            player.Use();

            // 4. Tick 6次，火把燃尽
            Debug.Log("\n--- 火把燃烧过程 ---");
            for (int i = 1; i <= 6; i++)
            {
                Debug.Log($"\n[第 {i} 次 Tick]");
                tileGrass.Tick(1f);
            }
            PrintChildren(tileGrass, "燃烧结束后");

            // 5. 演示递归选择（可选）
            Debug.Log("\n--- 测试递归选择 ---");
            var nightCard = _engine.CreateCard("草地格"); // 复用模板创建一个带"夜晚"标签的卡
            nightCard.AddTag("夜晚");
            tileGrass.AddChild(nightCard);
            tileGrass.Custom("检查夜晚");
        }

        /// <summary>
        /// 打印容器内容
        /// </summary>
        private void PrintChildren(Card container, string title)
        {
            var names = container.Children.Select(c => c.Id).ToArray();
            Debug.Log($"[{title}] 容器 [{container.Id}] 包含: {(names.Length == 0 ? "(空)" : string.Join(", ", names))}");
        }
    }
}