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
        private CardRuleEngine _engine;
        private CardFactory _factory;
        private bool _isNight;

        // 简易卡类型
        private sealed class SimpleCard : Card
        {
            public SimpleCard(CardData data, GameProperty property = null, params string[] extraTags)
            {
                Data = data;
                Property = property;
                if (extraTags != null)
                {
                    foreach (var t in extraTags)
                    {
                        if (!string.IsNullOrEmpty(t)) AddTag(t);
                    }
                }
            }
        }

        private void Start()
        {
            RunBestPracticeDemo();
        }

        private void RunBestPracticeDemo()
        {
            Debug.Log("=== EmeCard Best Practice 示例开始 ===");

            // 1) 工厂与引擎
            _factory = new CardFactory();
            _engine = new CardRuleEngine(_factory);
            _engine.Policy.FirstMatchOnly = true;

            // 注册产物

            _factory.Register("灰烬", () => new SimpleCard(new CardData("灰烬", "灰烬", "燃烧后产生的灰烬", CardCategory.Object), null, "灰烬"));
            _factory.Register("木棍", () => new SimpleCard(new CardData("木棍", "木棍", "基础材料", CardCategory.Object), null, "木棍"));
            _factory.Register("火把", () => new SimpleCard(new CardData("火把", "火把", "可点燃", CardCategory.Object), new GameProperty("Ticks", 0f), "火把"));


            // 2) 世界布置
            var world = new SimpleCard(new CardData("世界", "世界", "", CardCategory.Object), null, "世界");
            var tileGrass = new SimpleCard(new CardData("草地格", "草地格", "", CardCategory.Object), null, "草地");
            var tileDirt = new SimpleCard(new CardData("泥地格", "泥地格", "", CardCategory.Object), null, "泥土");
            world.AddChild(tileGrass);
            world.AddChild(tileDirt);

            var player = new SimpleCard(new CardData("玩家", "玩家", "", CardCategory.Object), new GameProperty("XP", 0f), "玩家");
            tileGrass.AddChild(player);

            var tree = new SimpleCard(new CardData("树木", "树木", "", CardCategory.Object), null, "树木", "可燃烧");
            var fire = new SimpleCard(new CardData("火", "火", "", CardCategory.Object), null, "火");
            var make = new SimpleCard(new CardData("制作", "制作", "", CardCategory.Action), null, "制作");
            var chop = new SimpleCard(new CardData("砍", "砍", "", CardCategory.Action), null, "砍");
            tileGrass.AddChild(tree);
            tileGrass.AddChild(fire);
            tileGrass.AddChild(make);
            tileGrass.AddChild(chop);

            // 去重测试对象：同一卡带有两个标签 "A" 与 "B"，并有计数属性 Counter=0
            var dedupObj = new SimpleCard(new CardData("去重对象", "去重对象", "", CardCategory.Object), 
                new GameProperty("Counter", 0f), "A", "B");
            tileGrass.AddChild(dedupObj);

            // 3) 接入事件
            _engine.Attach(tileGrass);
            _engine.Attach(tileDirt);
            _engine.Attach(player);
            _engine.Attach(make);
            _engine.Attach(chop);
            _engine.Attach(fire);

            // 4) 规则注册

            // R1: Use(制作) + 同容器有 玩家 + 树木 -> 产出 木棍（移除1个树木）
            _engine.RegisterRule(new CardRule
            {
                Trigger = CardEventType.Use,
                Requirements = new List<IRuleRequirement>
                {
                    new ConditionRequirement(ctx => ctx.Source.HasTag("制作")),
                    new CardRequirement { Root = RequirementRoot.Container, TargetKind = TargetKind.ByTag, Filter = "玩家", MinCount = 1 },
                    new CardRequirement { Root = RequirementRoot.Container, TargetKind = TargetKind.ByTag, Filter = "木棍", MinCount = 1 },
                    new CardRequirement { Root = RequirementRoot.Container, TargetKind = TargetKind.ByTag, Filter = "火",   MinCount = 1 },
                },
                Effects = new List<IRuleEffect>
                {
                    new RemoveCardsEffect { TargetKind = TargetKind.ByTag, TargetValueFilter = "木棍", Take = 1 },
                    new RemoveCardsEffect { TargetKind = TargetKind.ByTag, TargetValueFilter = "火",   Take = 1 },
                    new CreateCardsEffect { CardIds = new List<string> { "火把" } }
                },
                Policy = new RulePolicy
                {
                    DistinctMatched = true
                }
            });

            // R2: Use(制作) + 同容器有 玩家 + 木棍 + 火 -> 产出 火把（移除1个木棍和1个火）
            _engine.RegisterRule(new CardRule
            {
                Trigger = CardEventType.Use,
                Requirements = new List<IRuleRequirement>
                {
                    new ConditionRequirement(ctx => ctx.Source.HasTag("制作")),
                    new CardRequirement { Root = RequirementRoot.Container, TargetKind = TargetKind.ByTag, Filter = "玩家", MinCount = 1 },
                    new CardRequirement { Root = RequirementRoot.Container, TargetKind = TargetKind.ById,  Filter = "树木", MinCount = 1 },
                },
                Effects = new List<IRuleEffect>
                {
                    new RemoveCardsEffect { TargetKind = TargetKind.ById, TargetValueFilter = "树木", Take = 1 },
                    new CreateCardsEffect { CardIds = new List<string> { "木棍" } }
                },
                Policy = new RulePolicy
                { 
                    DistinctMatched = true
                }
            });

            // R3: Tick(Self) 同容器有“火把” -> 给所有火把 Ticks += 1
            _engine.RegisterRule(new CardRule
            {
                Trigger = CardEventType.Tick,
                OwnerHops = 0, // Self 容器：对触发 Tick 的容器自身处理
                Requirements = new List<IRuleRequirement>
                {
                    new CardRequirement { Root = RequirementRoot.Container, TargetKind = TargetKind.ByTag, Filter = "火把", MinCount = 1 },
                },
                Effects = new List<IRuleEffect>
                {
                    new ModifyPropertyEffect
                    {
                        TargetKind = TargetKind.ByTag,
                        TargetValueFilter = "火把",
                        ApplyMode = ModifyPropertyEffect.Mode.AddToBase,
                        Value = 1f
                    },
                    new InvokeEffect((ctx, _) =>
                    {
                        var torches = TargetSelector.Select(TargetKind.ByTag, ctx, "火把");
                        var ticks = torches.Select(t => t.Property?.GetBaseValue() ?? 0f).ToList();
                        Debug.Log($"[Tick] 本容器火把 Ticks: {(ticks.Count==0?"(无)":string.Join(", ", ticks))}");
                    })
                }
            });

            // R4: Tick(Self) 有 Ticks >= 5 的火把 -> 移除并产出等量的“灰烬”
            _engine.RegisterRule(new CardRule
            {
                Trigger = CardEventType.Tick,
                OwnerHops = 0,
                Requirements = new List<IRuleRequirement>
                {
                    new CardRequirement { Root = RequirementRoot.Container, TargetKind = TargetKind.ByTag, Filter = "火把", MinCount = 1 },
                },
                Effects = new List<IRuleEffect>
                {
                    new InvokeEffect((ctx, _) =>
                    {
                        var torches = TargetSelector.Select(TargetKind.ByTag, ctx, "火把").ToList();
                        int toRemove = 0;
                        foreach (var t in torches)
                        {
                            var gp = t.Property;
                            if (gp != null && gp.GetBaseValue() >= 5f)
                            {
                                t.Owner?.RemoveChild(t, force: false);
                                toRemove++;
                            }
                        }
                        for (int i = 0; i < toRemove; i++)
                        {
                            var ash = _factory.Create("灰烬");
                            if (ash != null) ctx.Container.AddChild(ash);
                        }
                        if (toRemove > 0)
                            Debug.Log($"[燃尽] 有 {toRemove} 个火把燃尽，生成同量灰烬");
                    })
                }
            });

            // RD: 去重测试规则：同一卡被两条条件命中（Tag=A 与 Tag=B）
            _engine.RegisterRule(new CardRule
            {
                Trigger = CardEventType.Custom,
                OwnerHops = 0,
                Requirements = new List<IRuleRequirement>
                {
                    new ConditionRequirement(ctx => ctx.EventId == "DedupTest"),
                    new CardRequirement { Root = RequirementRoot.Container, TargetKind = TargetKind.ByTag, Filter = "A", MinCount = 1 },
                    new CardRequirement { Root = RequirementRoot.Container, TargetKind = TargetKind.ByTag, Filter = "B", MinCount = 1 },
                },
                Effects = new List<IRuleEffect>
                {
                    // 使用 Matched：如果引擎做了去重，这里应只+1；否则会+2
                    new ModifyPropertyEffect
                    {
                        TargetKind = TargetKind.Matched,
                        ApplyMode = ModifyPropertyEffect.Mode.AddToBase,
                        Value = 1f
                    },
                    new InvokeEffect((ctx, matched) =>
                    {
                        var obj = ctx.Container.Children.FirstOrDefault(c => c.Id == "去重对象");
                        float v = obj?.Property?.GetBaseValue() ?? -1f;
                        if (Mathf.Approximately(v, 1f))
                            Debug.Log("matched 去重生效，Counter=1");
                        else if (Mathf.Approximately(v, 2f))
                            Debug.LogWarning("未去重：Counter=2");
                        else
                            Debug.LogWarning($"非预期 Counter={v}");
                    })
                }
            });

            // 5) 演示流程（简单驱动）
            PrintChildren(tileGrass, "初始 草地");

            // Use(制作) -> 产出木棍
            make.Use();
            PrintChildren(tileGrass, "砍树后");

            // Use(制作) -> 产出火把
            make.Use();
            PrintChildren(tileGrass, "制作火把后");

            // 连续 Tick 5 次 -> 火把燃尽为灰烬
            for (int i = 1; i <= 5; i++)
            {
                tileGrass.Tick(1f);
            }
            PrintChildren(tileGrass, "5次Tick后");

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