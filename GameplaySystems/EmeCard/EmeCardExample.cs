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
    /// - 展示递归匹配（CardRule.Recursive/MaxDepth）与递归选择（TargetKind.*Recursive/ContainerDescendants）。
    /// </summary>
    public sealed class EmeCardExample : MonoBehaviour
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

            // 注册产物
            _factory.Register("灰烬", () => new SimpleCard(new CardData("灰烬", "灰烬", "燃烧后产生的灰烬", CardCategory.Item), null, "灰烬"));
            _factory.Register("木棍", () => new SimpleCard(new CardData("木棍", "木棍", "基础材料", CardCategory.Item), null, "木棍"));
            _factory.Register("火把", () => new SimpleCard(new CardData("火把", "火把", "可点燃", CardCategory.Item), null, "火把"));

            // 2) 世界布置
            var world = new SimpleCard(new CardData("世界", "世界", "", CardCategory.Item), null, "世界");
            var tileGrass = new SimpleCard(new CardData("草地格", "草地格", "", CardCategory.Item), null, "草地");
            var tileDirt = new SimpleCard(new CardData("泥地格", "泥地格", "", CardCategory.Item), null, "泥土");
            world.AddChild(tileGrass);
            world.AddChild(tileDirt);

            var player = new SimpleCard(new CardData("玩家", "玩家", "", CardCategory.Item), new GameProperty("XP", 0f), "玩家");
            tileGrass.AddChild(player);

            var tree = new SimpleCard(new CardData("树木", "树木", "", CardCategory.Item), new GameProperty("Temperature", 20f), "树木", "可燃烧");
            var fire = new SimpleCard(new CardData("火", "火", "", CardCategory.Item), null, "火");
            var make = new SimpleCard(new CardData("制作", "制作", "", CardCategory.Action), null, "制作");
            var chop = new SimpleCard(new CardData("砍", "砍", "", CardCategory.Action), null, "砍");
            tileGrass.AddChild(tree);
            tileGrass.AddChild(fire);
            tileGrass.AddChild(make);
            tileGrass.AddChild(chop);

            // 3) 接入事件
            _engine.Attach(tileGrass);
            _engine.Attach(tileDirt);
            _engine.Attach(player);
            _engine.Attach(make);
            _engine.Attach(chop);
            _engine.Attach(fire);

            // 4) 规则注册

            // R1: Tick加热（容器内存在“火” -> 所有“可燃烧”升温 +1）
            _engine.RegisterRule(new CardRule
            {
                Trigger = CardEventType.Tick,
                // OwnerHops = 1 默认即为容器=Owner
                Requirements = new List<IRuleRequirement>
                {
                    new CardRequirement { Kind = MatchKind.Tag, Value = "火", MinCount = 1 }
                },
                Effects = new List<IRuleEffect>
                {
                    new ModifyPropertyEffect
                    {
                        TargetKind = TargetKind.ByTag,
                        TargetValueFilter = "可燃烧",
                        ApplyMode = ModifyPropertyEffect.Mode.AddToBase,
                        Value = 1f
                    }
                }
            });

            // R2: Tick燃烧配方（可燃烧 + 火 -> 灰烬；消耗材料；且温度>23才燃烧）
            _engine.RegisterRule(new CardRule
            {
                Trigger = CardEventType.Tick,
                Requirements = new List<IRuleRequirement>
                {
                    new CardRequirement { Kind = MatchKind.Tag, Value = "可燃烧", MinCount = 1 },
                    new CardRequirement { Kind = MatchKind.Tag, Value = "火", MinCount = 1 },
                    new ConditionRequirement(ctx =>
                        ctx.Container.Children.Any(c => c.HasTag("可燃烧") && (c.Property?.GetBaseValue() ?? 0f) > 23f))
                },
                Effects = new List<IRuleEffect>
                {
                    new RemoveCardsEffect { TargetKind = TargetKind.ByTag, TargetValueFilter = "树木" },
                    new RemoveCardsEffect { TargetKind = TargetKind.ByTag, TargetValueFilter = "火"   },
                    new CreateCardsEffect { CardIds = new List<string> { "灰烬" } }
                }
            });

            // R3: Use制作（制作 + 树木 -> 木棍；不消耗“制作”）
            _engine.RegisterRule(new CardRule
            {
                Trigger = CardEventType.Use,
                Requirements = new List<IRuleRequirement>
                {
                    // 以 Owner 为容器（默认），但把 Source 本体也纳入 -> 用 Anchor=Source + ContainerOnly 来替代旧 IncludeSelf
                    new CardRequirement { Anchor = RequirementAnchor.Source, Search = RequirementSearchScope.ContainerOnly, Kind = MatchKind.Tag, Value = "制作", MinCount = 1 },
                    new CardRequirement { Kind = MatchKind.Id,  Value = "树木", MinCount = 1 }
                },
                Effects = new List<IRuleEffect>
                {
                    new RemoveCardsEffect { TargetKind = TargetKind.ById, TargetValueFilter = "树木" },
                    new CreateCardsEffect { CardIds = new List<string> { "木棍" } }
                }
            });

            // R4: Use制作（制作 + 木棍 + 火 -> 火把）
            _engine.RegisterRule(new CardRule
            {
                Trigger = CardEventType.Use,
                Requirements = new List<IRuleRequirement>
                {
                    new CardRequirement { Anchor = RequirementAnchor.Source, Search = RequirementSearchScope.ContainerOnly, Kind = MatchKind.Tag, Value = "制作", MinCount = 1 },
                    new CardRequirement { Kind = MatchKind.Tag, Value = "木棍", MinCount = 1 },
                    new CardRequirement { Kind = MatchKind.Tag, Value = "火",   MinCount = 1 }
                },
                Effects = new List<IRuleEffect>
                {
                    new RemoveCardsEffect { TargetKind = TargetKind.ByTag, TargetValueFilter = "木棍" },
                    new RemoveCardsEffect { TargetKind = TargetKind.ByTag, TargetValueFilter = "火"   },
                    new CreateCardsEffect { CardIds = new List<string> { "火把" } }
                }
            });

            // R5: Use砍树（砍 + 树木 -> 移除树）
            _engine.RegisterRule(new CardRule
            {
                Trigger = CardEventType.Use,
                Requirements = new List<IRuleRequirement>
                {
                    new CardRequirement { Anchor = RequirementAnchor.Source, Search = RequirementSearchScope.ContainerOnly, Kind = MatchKind.Tag, Value = "砍", MinCount = 1 },
                    new CardRequirement { Kind = MatchKind.Id,  Value = "树木", MinCount = 1 }
                },
                Effects = new List<IRuleEffect>
                {
                    new RemoveCardsEffect { TargetKind = TargetKind.ById, TargetValueFilter = "树木" }
                }
            });

            // R6: Custom-进入草地（以 Self 为容器 -> OwnerHops=0）
            _engine.RegisterRule(new CardRule
            {
                Trigger = CardEventType.Custom,
                OwnerHops = 0,
                Requirements = new List<IRuleRequirement>
                {
                    new ConditionRequirement(ctx => ctx.EventId == "PlayerEnter"),
                    new CardRequirement { Anchor = RequirementAnchor.Container, Search = RequirementSearchScope.ContainerOnly, Kind = MatchKind.Tag, Value = "草地", MinCount = 1 }
                },
                Effects = new List<IRuleEffect>
                {
                    new InvokeEffect((ctx, _) =>
                    {
                        var p = ctx.DataCard;
                        if (p != null) p.Custom("GainXP", 5f);
                        Debug.Log("[进入草地] 玩家获得经验 +5");
                    })
                }
            });

            // R7: Custom-GainXP（Self 容器）
            _engine.RegisterRule(new CardRule
            {
                Trigger = CardEventType.Custom,
                OwnerHops = 0,
                Requirements = new List<IRuleRequirement>
                {
                    new ConditionRequirement(ctx => ctx.EventId == "GainXP")
                },
                Effects = new List<IRuleEffect>
                {
                    new InvokeEffect((ctx, _) =>
                    {
                        float inc = 0f;
                        if (ctx.Event.Data is float f) inc = f;
                        var gp = ctx.Source.Property;
                        if (gp != null)
                        {
                            gp.SetBaseValue(gp.GetBaseValue() + inc);
                            Debug.Log($"[玩家经验] 增加 {inc} -> 当前XP: {gp.GetBaseValue()}");
                        }
                    })
                }
            });

            // R8: Tick-夜晚点燃火把（Self 容器）
            _engine.RegisterRule(new CardRule
            {
                Trigger = CardEventType.Tick,
                OwnerHops = 0,
                Requirements = new List<IRuleRequirement>
                {
                    new ConditionRequirement(ctx => _isNight),
                    new CardRequirement { Anchor = RequirementAnchor.Container, Search = RequirementSearchScope.ContainerOnly, Kind = MatchKind.Tag, Value = "草地", MinCount = 1 },
                    new CardRequirement { Kind = MatchKind.Tag, Value = "火把", MinCount = 1 }
                },
                Effects = new List<IRuleEffect>
                {
                    new AddTagEffect { TargetKind = TargetKind.ByTag, TargetValueFilter = "火把", Tag = "火" },
                    new InvokeEffect((ctx, _) => Debug.Log("[夜晚] 草地格：火把被点燃（添加标签：火）"))
                }
            });

            // 其余示例规则若有 Scope=Self，请改为 OwnerHops=0；Scope=Owner 则可省略（默认 =1）

            // 5) 演示流程
            PrintChildren(tileGrass, "初始 草地");
            PrintChildren(tileDirt, "初始 泥地");

            if (player.Owner != null) player.Owner.RemoveChild(player);
            tileDirt.AddChild(player);
            tileDirt.Custom("PlayerEnter", player);
            if (player.Owner != null) player.Owner.RemoveChild(player);
            tileGrass.AddChild(player);
            tileGrass.Custom("PlayerEnter", player);

            make.Use();
            PrintChildren(tileGrass, "制作木棍后");

            make.Use();
            PrintChildren(tileGrass, "制作火把后");

            tileGrass.Tick(1f);
            Debug.Log($"火把是否带有'火'标签: {tileGrass.Children.Any(c => c.HasTag("火把") && c.HasTag("火"))}");
            tileGrass.AddChild(fire);
            tileGrass.AddChild(tree);

            fire.Tick(1f);
            PrintChildren(tileGrass, "一次加热/燃烧后");
            fire.Tick(1f);
            fire.Tick(1f);
            fire.Tick(1f);
            PrintChildren(tileGrass, "3次加热/燃烧后");

            Debug.Log("=== EmeCard Best Practice 示例结束 ===");
        }

        private static void PrintChildren(Card container, string title)
        {
            var names = container.Children.Select(c => c.Id).ToList();
            Debug.Log($"{title} => 容器[{container.Id}] 子项: {(names.Count == 0 ? "(空)" : string.Join(", ", names))}");
        }
    }
}