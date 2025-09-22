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
    /// - 展示 Scope 与 OwnerHops 以及 TargetKind 的使用差异；
    /// - 展示递归匹配（CardRule.Recursive/MaxDepth）与递归选择（TargetKind.*Recursive/ContainerDescendants）。
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

            // 追加一组“深层子项”，用于演示递归匹配/选择
            var nest = new SimpleCard(new CardData("巢", "巢", "", CardCategory.Item), new GameProperty("Temperature", 10f), "可燃烧");
            tree.AddChild(nest);
            var ember = new SimpleCard(new CardData("余烬", "余烬", "", CardCategory.Item), null, "火");
            nest.AddChild(ember);

            // 3) 接入事件（谁会发事件就接谁）
            _engine.Attach(tileGrass);
            _engine.Attach(tileDirt);
            _engine.Attach(player);
            _engine.Attach(make);
            _engine.Attach(chop);
            _engine.Attach(fire);
            _engine.Attach(ember);

            // 4) 规则注册

            // R1: Tick加热（容器内存在“火” -> 所有“可燃烧”升温 +1）[一层匹配/一层选择]
            _engine.RegisterRule(new CardRule
            {
                Trigger = CardEventType.Tick,
                Scope = RuleScope.Owner,
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

            // R2: Tick燃烧配方（可燃烧 + 火 -> 灰烬；消耗材料；且温度>23才燃烧）[一层匹配/一层选择]
            _engine.RegisterRule(new CardRule
            {
                Trigger = CardEventType.Tick,
                Scope = RuleScope.Owner,
                Requirements = new List<IRuleRequirement>
                {
                    new CardRequirement { Kind = MatchKind.Tag, Value = "可燃烧", MinCount = 1 },
                    new CardRequirement { Kind = MatchKind.Tag, Value = "火", MinCount = 1 },
                    // 温度阈值：容器内任意“可燃烧”卡的 Temperature > 23 才命中
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
                Scope = RuleScope.Owner,
                Requirements = new List<IRuleRequirement>
                {
                    new CardRequirement { Kind = MatchKind.Tag, Value = "制作", MinCount = 1, IncludeSelf = true },
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
                Scope = RuleScope.Owner,
                Requirements = new List<IRuleRequirement>
                {
                    new CardRequirement { Kind = MatchKind.Tag, Value = "制作", MinCount = 1, IncludeSelf = true },
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
                Scope = RuleScope.Owner,
                Requirements = new List<IRuleRequirement>
                {
                    new CardRequirement { Kind = MatchKind.Tag, Value = "砍", MinCount = 1, IncludeSelf = true },
                    new CardRequirement { Kind = MatchKind.Id,  Value = "树木", MinCount = 1 }
                },
                Effects = new List<IRuleEffect>
                {
                    new RemoveCardsEffect { TargetKind = TargetKind.ById, TargetValueFilter = "树木" }
                }
            });

            // R6: Custom-进入草地 -> 链式给玩家加经验（Scope.Self + IncludeSelf）
            _engine.RegisterRule(new CardRule
            {
                Trigger = CardEventType.Custom,
                Scope = RuleScope.Self,
                Requirements = new List<IRuleRequirement>
                {
                    new ConditionRequirement(ctx => ctx.EventId == "PlayerEnter"),
                    new CardRequirement { Kind = MatchKind.Tag, Value = "草地", MinCount = 1, IncludeSelf = true }
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

            // R7: Custom-GainXP -> 修改玩家XP（Scope.Self）
            _engine.RegisterRule(new CardRule
            {
                Trigger = CardEventType.Custom,
                Scope = RuleScope.Self,
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

            // R8: Tick-夜晚点燃火把（通过外部布尔条件控制）
            _engine.RegisterRule(new CardRule
            {
                Trigger = CardEventType.Tick,
                Scope = RuleScope.Self,
                Requirements = new List<IRuleRequirement>
                {
                    new ConditionRequirement(ctx => _isNight),
                    new CardRequirement { Kind = MatchKind.Tag, Value = "草地", MinCount = 1, IncludeSelf = true },
                    new CardRequirement { Kind = MatchKind.Tag, Value = "火把", MinCount = 1 }
                },
                Effects = new List<IRuleEffect>
                {
                    new AddTagEffect { TargetKind = TargetKind.ByTag, TargetValueFilter = "火把", Tag = "火" },
                    new InvokeEffect((ctx, _) => Debug.Log("[夜晚] 草地格：火把被点燃（添加标签：火）"))
                }
            });

            // R9: AddedToOwner - 玩家被加入到“草地”持有者时加经验（Scope.Self，检查事件载荷中的 Owner）
            _engine.RegisterRule(new CardRule
            {
                Trigger = CardEventType.AddedToOwner,
                Scope = RuleScope.Self,
                Requirements = new List<IRuleRequirement>
                {
                    new ConditionRequirement(ctx => ctx.Source.HasTag("玩家")),
                    new ConditionRequirement(ctx => (ctx.Event.Data as Card)?.HasTag("草地") == true),
                },
                Effects = new List<IRuleEffect>
                {
                    new InvokeEffect((ctx, _) =>
                    {
                        ctx.Source.Custom("GainXP", 2f);
                        Debug.Log("[事件] 玩家加入草地（AddedToOwner）：额外经验 +2");
                    })
                }
            });

            // R10: RemovedFromOwner - 玩家离开“草地”时日志（Scope.Self）
            _engine.RegisterRule(new CardRule
            {
                Trigger = CardEventType.RemovedFromOwner,
                Scope = RuleScope.Self,
                Requirements = new List<IRuleRequirement>
                {
                    new ConditionRequirement(ctx => ctx.Source.HasTag("玩家")),
                    new ConditionRequirement(ctx => (ctx.Event.Data as Card)?.HasTag("草地") == true),
                },
                Effects = new List<IRuleEffect>
                {
                    new InvokeEffect((ctx, _) => Debug.Log("[事件] 玩家离开草地（RemovedFromOwner）"))
                }
            });

            // R11: Custom-CheckActions - 使用 Category 匹配（容器内至少2个 Action）
            _engine.RegisterRule(new CardRule
            {
                Trigger = CardEventType.Custom,
                Scope = RuleScope.Owner,
                Requirements = new List<IRuleRequirement>
                {
                    new ConditionRequirement(ctx => ctx.EventId == "CheckActions"),
                    new CardRequirement { Kind = MatchKind.Category, Category = CardCategory.Action, MinCount = 2 }
                },
                Effects = new List<IRuleEffect>
                {
                    new InvokeEffect((ctx, _) => Debug.Log("[类别匹配] 容器内存在至少 2 个 Action 卡"))
                }
            });

            // R12: Custom-MarkTile - 使用 TargetKind.Container / ContainerChildren 给容器与子项打标签（仅一层选择）
            _engine.RegisterRule(new CardRule
            {
                Trigger = CardEventType.Custom,
                Scope = RuleScope.Self,
                Requirements = new List<IRuleRequirement>
                {
                    new ConditionRequirement(ctx => ctx.EventId == "MarkTile"),
                    new CardRequirement { Kind = MatchKind.Tag, Value = "草地", MinCount = 1, IncludeSelf = true }
                },
                Effects = new List<IRuleEffect>
                {
                    new AddTagEffect { TargetKind = TargetKind.Container, Tag = "高亮" },
                    new AddTagEffect { TargetKind = TargetKind.ContainerChildren, Tag = "检查中" },
                    new InvokeEffect((ctx, _) => Debug.Log("[标记] 草地及子项添加标签：高亮/检查中（仅一层）"))
                }
            });

            // R13: Tick-递归加热演示（Owner 作用域 + 递归匹配 + 递归选择）
            // 要点：
            // - Recursive=true/MaxDepth=int.MaxValue：在容器子树中寻找“火”（匹配）；
            // - TargetKind.ByTagRecursive：对整个子树内“可燃烧”目标加温（选择）。
            _engine.RegisterRule(new CardRule
            {
                Trigger = CardEventType.Tick,
                Scope = RuleScope.Owner,
                Recursive = true,
                MaxDepth = int.MaxValue,
                Requirements = new List<IRuleRequirement>
                {
                    new CardRequirement { Kind = MatchKind.Tag, Value = "火", MinCount = 1 }
                },
                Effects = new List<IRuleEffect>
                {
                    new ModifyPropertyEffect
                    {
                        TargetKind = TargetKind.ByTagRecursive,
                        TargetValueFilter = "可燃烧",
                        ApplyMode = ModifyPropertyEffect.Mode.AddToBase,
                        Value = 0.5f
                    }
                }
            });

            // R14: Use制作（OwnerHops=-1 到 Root“世界”作为容器）+ 递归选择 ContainerDescendants
            // 要点：
            // - Scope=Owner + OwnerHops=-1：以“世界”为容器；
            // - Container / ContainerDescendants：分别给世界和其所有后代打标签。
            _engine.RegisterRule(new CardRule
            {
                Trigger = CardEventType.Use,
                Scope = RuleScope.Owner,
                OwnerHops = -1, // Root
                Requirements = new List<IRuleRequirement>
                {
                    new CardRequirement { Kind = MatchKind.Tag, Value = "制作", MinCount = 1, IncludeSelf = true }
                },
                Effects = new List<IRuleEffect>
                {
                    new AddTagEffect { TargetKind = TargetKind.Container, Tag = "世界标记" },
                    new AddTagEffect { TargetKind = TargetKind.ContainerDescendants, Tag = "扫描完成" },
                    new InvokeEffect((ctx, _) => Debug.Log("[OwnerHops] 以 Root(世界) 为容器，对全局后代进行递归打标：扫描完成"))
                }
            });

            // 5) 演示流程
            PrintChildren(tileGrass, "初始 草地");
            PrintChildren(tileDirt, "初始 泥地");

            // 玩家往返移动，触发进入草地加经验（R6/R7 以及 R9/R10）
            if (player.Owner != null) player.Owner.RemoveChild(player);
            tileDirt.AddChild(player);
            tileDirt.Custom("PlayerEnter", player);
            if (player.Owner != null) player.Owner.RemoveChild(player);
            tileGrass.AddChild(player);
            tileGrass.Custom("PlayerEnter", player);

            // 使用 Category 匹配（检查容器内是否有至少2个Action: 制作/砍）
            make.Custom("CheckActions");

            // 制作：树木 -> 木棍
            make.Use();
            PrintChildren(tileGrass, "制作木棍后");

            // 再制作：木棍 + 火 -> 火把
            make.Use();
            PrintChildren(tileGrass, "制作火把后");

            // 标记草地及其子项（Container / ContainerChildren）
            tileGrass.Custom("MarkTile");

            // 夜晚到来：点燃火把（Tick）
            _isNight = true;
            tileGrass.Tick(1f);
            Debug.Log($"火把是否带有'火'标签: {tileGrass.Children.Any(c => c.HasTag("火把") && c.HasTag("火"))}");
            tileGrass.AddChild(fire);
            tileGrass.AddChild(tree);

            // 加热/燃烧演示（若仍有“火”和“可燃烧”），包含递归加热（R13）
            fire.Tick(1f);
            ember.Tick(1f); // 深层“余烬”也发 Tick，驱动递归规则链
            PrintChildren(tileGrass, "一次加热/燃烧后");
            fire.Tick(1f);
            fire.Tick(1f);
            fire.Tick(1f);
            PrintChildren(tileGrass, "3次加热/燃烧后");

            // 触发 OwnerHops 到 Root 的全局打标（R14）
            make.Use();

            Debug.Log("=== EmeCard Best Practice 示例结束 ===");
        }

        private static void PrintChildren(Card container, string title)
        {
            var names = container.Children.Select(c => c.Id).ToList();
            Debug.Log($"{title} => 容器[{container.Id}] 子项: {(names.Count == 0 ? "(空)" : string.Join(", ", names))}");
        }
    }
}