using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasyPack
{
    /// <summary>
    /// Best Practice 示例（万物卡）：
    /// - 展示有序事件驱动（Tick/Use/Custom/Added/Removed），用 Requirement 做清晰的条件组合；
    /// - 展示规则效果管线（移除/产出/属性修改/调用/打标签）；
    /// - 展示通过工厂注册/创建卡牌；
    /// - 展示锚点选择（OwnerHops）与 TargetKind 的使用差异；
    /// - 展示递归选择（TargetKind.*Recursive/ContainerDescendants）。
    /// </summary>
    public sealed partial class EmeCardExample : MonoBehaviour
    {
        private CardEngine _engine;
        private CardFactory _factory;
        private bool _isNight;

        private void Start()
        {
            RunBestPracticeDemo();
        }

        private void RunBestPracticeDemo()
        {
            Debug.Log("=== EmeCard Best Practice 示例开始 ===");

            // 1) 工厂与引擎
            _factory = new CardFactory();
            _engine = new CardEngine(_factory);
            _engine.Policy.FirstMatchOnly = false;

            // 注册卡牌的模板
            _factory.Register("灰烬", () => new Card(new CardData("灰烬", "灰烬", "燃烧后产生的灰烬", CardCategory.Object), properties: null, "灰烬"));
            _factory.Register("木棍", () => new Card(new CardData("木棍", "木棍", "基础材料", CardCategory.Object), properties: null, "木棍"));
            _factory.Register("火把", () => new Card(new CardData("火把", "火把", "可点燃", CardCategory.Object), new GameProperty("Ticks", 0f), "火把"));
            _factory.Register("世界", () => new Card(new CardData("世界", "世界", "", CardCategory.Object), properties: null, "世界"));
            _factory.Register("草地格", () => new Card(new CardData("草地格", "草地格", "", CardCategory.Object), properties: null, "草地"));
            _factory.Register("泥地格", () => new Card(new CardData("泥地格", "泥地格", "", CardCategory.Object), properties: null, "泥土"));
            _factory.Register("玩家", () => new Card(new CardData("玩家", "玩家", "", CardCategory.Object), new GameProperty("XP", 0f), "玩家"));
            _factory.Register("树木", () => new Card(new CardData("树木", "树木", "", CardCategory.Object), properties: null, "树木", "可燃烧"));
            _factory.Register("火", () => new Card(new CardData("火", "火", "", CardCategory.Object), properties: null, "火"));
            _factory.Register("制作", () => new Card(new CardData("制作", "制作", "", CardCategory.Action), properties: null, "制作"));
            _factory.Register("砍", () => new Card(new CardData("砍", "砍", "", CardCategory.Action),  properties: null, "砍"));
            _factory.Register("去重对象", () => new Card(
                new CardData("去重对象", "去重对象", "", CardCategory.Object),
                new List<GameProperty> { new GameProperty("Counter", 0f) },
                "A", "B"));
            // 2) 世界布置
            var world = _engine.CreateCard("世界");
            var tileGrass = _engine.CreateCard("草地格");
            var tileDirt = _engine.CreateCard("泥地格");
            world.AddChild(tileGrass);
            world.AddChild(tileDirt);

            var player = _engine.CreateCard("玩家");
            tileGrass.AddChild(player);

            var tree = _engine.CreateCard("树木");
            var fire = _engine.CreateCard("火");
            var make = _engine.CreateCard("制作");
            var chop = _engine.CreateCard("砍");
            tileGrass.AddChild(tree);
            tileGrass.AddChild(fire);
            tileGrass.AddChild(make);
            tileGrass.AddChild(chop);

            var gp = new List<GameProperty>
            { new GameProperty("Counter", 0f)};
            // 去重测试对象：同一卡带有两个标签 "A" 与 "B"，并有计数属性 Counter=0
            var dedupObj = _engine.CreateCard("去重对象");
            tileGrass.AddChild(dedupObj);


            // 3) 规则注册（使用 Builder 语法糖）

            // R1: Use(制作) + 同容器有 玩家 + 木棍 + 火 -> 产出 火把（移除1个木棍和1个火）
            _engine.RegisterRule(b => b
                .Trigger(CardEventType.Use)
                .StopEventOnSuccess()
                .WhenSourceTag("制作")
                .NeedContainerTag("玩家")
                .NeedContainerTag("木棍")
                .NeedContainerTag("火")
                .DoRemoveByTag("木棍", take: 1)
                .DoRemoveByTag("火", take: 1)
                .DoCreate("火把")
                .DistinctMatched(true)
            );

            // R2: Use(制作) + 同容器有 玩家 + 树木 -> 产出 木棍（移除1个树木）
            _engine.RegisterRule(b => b
                .Trigger(CardEventType.Use)
                .StopEventOnSuccess()
                .WhenSourceTag("制作")
                .NeedContainerTag("玩家")
                .NeedContainerId("树木")
                .DoRemoveById("树木", take: 1)
                .DoCreate("木棍")
                .DistinctMatched(true)
            );

            // R3: Tick(Self) 同容器有“火把” -> 给所有火把 Ticks += 1
            _engine.RegisterRule(b => b
                .Trigger(CardEventType.Tick)
                .OwnerHops(0)
                .NeedContainerTag("火把")
                .AddEffect(new ModifyPropertyEffect
                {
                    TargetKind = TargetKind.ByTag,
                    TargetValueFilter = "火把",
                    ApplyMode = ModifyPropertyEffect.Mode.AddToBase,
                    Value = 1f
                })
                .DoInvoke((ctx, _) =>
                {
                    var torches = TargetSelector.Select(TargetKind.ByTag, ctx, "火把");
                    var ticks = torches.Select(t => t.Properties[0]?.GetBaseValue() ?? 0f).ToList();
                    Debug.Log($"[Tick] 本容器火把 Ticks: {(ticks.Count == 0 ? "(无)" : string.Join(", ", ticks))}");
                })
            );

            // R4: Tick(Self) 有 Ticks >= 5 的火把 -> 移除并产出等量的“灰烬”
            _engine.RegisterRule(b => b
                .Trigger(CardEventType.Tick)
                .OwnerHops(0)
                .NeedContainerTag("火把")
                // 先累加
                .AddEffect(new ModifyPropertyEffect
                {
                    TargetKind = TargetKind.ByTag,
                    TargetValueFilter = "火把",
                    ApplyMode = ModifyPropertyEffect.Mode.AddToBase,
                    Value = 1f
                })
                // 再检查并燃尽
                .DoInvoke((ctx, _) =>
                {
                    var torches = TargetSelector.Select(TargetKind.ByTag, ctx, "火把").ToList();
                    int toRemove = 0;
                    if(torches == null || torches.Count == 0)
                    {
                        Debug.Log("[燃尽] 本容器无火把");
                        return;
                    }
                    foreach (var t in torches)
                    {
                        var gp = t.Properties[0];
                        if (gp != null && gp.GetBaseValue() >= 5f)
                        {
                            Debug.Log("尝试燃尽火把");
                            t.Owner?.RemoveChild(t, force: false);
                            toRemove++;
                        }
                    }
                    for (int i = 0; i < toRemove; i++)
                    {
                        Debug.Log("生成灰烬");
                        var ash = _factory.Create("灰烬");
                        if (ash != null) ctx.Container.AddChild(ash);
                    }
                    if (toRemove > 0)
                        Debug.Log($"[燃尽] 有 {toRemove} 个火把燃尽，生成同量灰烬");
                })
            );

            // 5) 演示流程（简单驱动）
            PrintChildren(tileGrass, "初始 草地");

            // Use(制作) -> 产出木棍
            make.Use();
            PrintChildren(tileGrass, "制作木棍后");

            // Use(制作) -> 产出火把
            make.Use();
            PrintChildren(tileGrass, "制作火把后");

            // 连续 Tick 6 次 -> 火把应当燃尽为灰烬
            for (int i = 1; i <= 6; i++)
            {
                tileGrass.Tick(1f);
            }
            PrintChildren(tileGrass, "6次Tick后");

            // 触发去重测试
            tileGrass.Custom("DedupTest");

            Debug.Log("=== EmeCard Best Practice 示例结束 ===");
        }

        private static void PrintChildren(Card container, string title)
        {
            var names = container.Children.Select(c => c.Id).ToList();
            Debug.Log($"{title} => 容器[{container.Id}] 子项: {(names.Count == 0 ? "(空)" : string.Join(", ", names))}");
        }
    }
}