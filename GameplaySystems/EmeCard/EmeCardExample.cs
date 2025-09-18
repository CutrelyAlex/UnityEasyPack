using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasyPack
{
    /// <summary>
    /// EmeCard 系统示例：
    /// 展示卡牌容器、标签、固有子卡、事件、规则匹配、效果管线与产卡工厂。
    /// </summary>
    public partial class EmeCardExample : MonoBehaviour
    {
        private CardRuleEngine _engine;
        private CardFactory _factory;

        // 示例中使用的简易卡牌类型
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

        // 示例用：添加标签效果（用于演示目标卡添加某标签）
        private sealed class AddTagEffect : IRuleEffect
        {
            public TargetKind TargetKind { get; set; } = TargetKind.Matched;
            public string TargetValueFilter { get; set; }
            public string Tag { get; set; }

            public void Execute(CardRuleContext ctx, IReadOnlyList<Card> matched)
            {
                IReadOnlyList<Card> targets =
                    TargetKind == TargetKind.Matched
                        ? matched
                        : TargetSelector.Select(TargetKind, ctx, TargetValueFilter);

                foreach (var t in targets)
                {
                    if (!string.IsNullOrEmpty(Tag))
                        t.AddTag(Tag);
                }
            }
        }

        void Start()
        {
            RunBaseExample();
            Debug.Log("\n=== 分隔线：进入复杂条件示例 ===\n");
            RunConditionComboExample();
        }

        // 基础展示方法：便于后续拓展新的示例
        private void RunBaseExample()
        {
            Debug.Log("=== EmeCard 系统示例开始 ===\n");

            // 1) 初始化工厂与规则引擎
            _factory = new CardFactory();
            _engine = new CardRuleEngine(_factory);

            // 注册可产出的卡牌（例如“灰烬”）
            _factory.Register("灰烬", () => new SimpleCard(
                new CardData("灰烬", "灰烬", "燃烧后产生的灰烬", CardCategory.Item),
                null, "灰烬"));

            // 2) 构建基础场景：草地容器，树木（可燃烧），火，制作，砍
            var grass = new SimpleCard(new CardData("草地", "草地", "", CardCategory.Item), null, "草地");
            var tree = new SimpleCard(new CardData("树木", "树木", "", CardCategory.Item),
                                       new GameProperty("Temperature", 20f), "树木", "可燃烧");
            var fire = new SimpleCard(new CardData("火", "火", "", CardCategory.Item), null, "火");
            var make = new SimpleCard(new CardData("制作", "制作", "", CardCategory.Action), null, "制作");
            var chop = new SimpleCard(new CardData("砍", "砍", "", CardCategory.Action), null, "砍");

            // 展示“固有子卡牌”：将“可燃烧”作为树木的固有子卡（演示不可移除/不可消耗）
            var flammable = new SimpleCard(new CardData("可燃烧", "可燃烧", "", CardCategory.Attribute), null, "可燃烧");
            tree.AddChild(flammable, intrinsic: true);
            // 注意：规则匹配只扫描容器的 Children，不会深入树木的子层级；
            // 因此同时在树木本体上打上“可燃烧”标签用于匹配（上面构造已添加 Tag="可燃烧"）。

            // 放入草地容器
            grass.AddChild(tree);
            grass.AddChild(fire);
            grass.AddChild(make);
            grass.AddChild(chop);

            // 3) 将会触发事件的卡牌接入引擎
            _engine.Attach(fire);
            _engine.Attach(make);
            _engine.Attach(chop);

            // 4) 规则A：仅效果（不产卡）——当容器中存在“火”时，每次 Tick 让“可燃烧”卡的温度 +1
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

            Debug.Log($"初始温度: {tree.Property?.GetBaseValue()}");
            fire.Tick(1f); // 触发一次按时事件（由 fire 作为触发源，Scope=Owner => 在草地容器匹配）
            Debug.Log($"加热一次后温度: {tree.Property?.GetBaseValue()}");
            PrintChildren(grass, "A. 仅效果后（无产卡）");

            // 5) 规则B：燃烧配方（按时）——“可燃烧” + “火” => 产出“灰烬”，并消耗输入
            _engine.RegisterRule(new CardRule
            {
                Trigger = CardEventType.Tick,
                Scope = RuleScope.Owner,
                Requirements = new List<IRuleRequirement>
                {
                    new CardRequirement { Kind = MatchKind.Tag, Value = "可燃烧", MinCount = 1 },
                    new CardRequirement { Kind = MatchKind.Tag, Value = "火",     MinCount = 1 }
                },
                Effects = new List<IRuleEffect>
                {
                    // 先移除匹配到的输入（含“可燃烧”“火”）
                    new RemoveCardsEffect { TargetKind = TargetKind.Matched },
                    // 再产出“灰烬”
                    new CreateCardsEffect { CardIds = new List<string> { "灰烬" } }
                }
            });

            fire.Tick(1f); // 本次 Tick 将匹配并执行燃烧配方
            PrintChildren(grass, "B. 燃烧后（生成灰烬并消耗材料）");

            // 为后续示例重置：重新添加树木与火
            var tree2 = new SimpleCard(new CardData("树木", "树木", "", CardCategory.Item),
                                       new GameProperty("Temperature", 10f), "树木", "可燃烧");
            tree2.AddChild(new SimpleCard(new CardData("可燃烧", "可燃烧", "", CardCategory.Attribute), null, "可燃烧"), intrinsic: true);
            var fire2 = new SimpleCard(new CardData("火", "火", "", CardCategory.Item), null, "火");
            grass.AddChild(tree2);
            grass.AddChild(fire2);
            _engine.Attach(fire2);

            // 6) 规则C：主动制作（Use）——“制作”（自包含）+ “树木” + “火” => “灰烬”，消耗材料
            _engine.RegisterRule(new CardRule
            {
                Trigger = CardEventType.Use,
                Scope = RuleScope.Owner,
                Requirements = new List<IRuleRequirement>
                {
                    new CardRequirement { Kind = MatchKind.Tag, Value = "制作", MinCount = 1, IncludeSelf = true },
                    new CardRequirement { Kind = MatchKind.Id,  Value = "树木", MinCount = 1 },
                    new CardRequirement { Kind = MatchKind.Tag, Value = "火",   MinCount = 1 },
                },
                Effects = new List<IRuleEffect>
                {
                    // 精确移除“树木”“火”，不移除“制作”本体
                    new RemoveCardsEffect { TargetKind = TargetKind.ById,  TargetValueFilter = "树木" },
                    new RemoveCardsEffect { TargetKind = TargetKind.ByTag, TargetValueFilter = "火"   },
                    new CreateCardsEffect { CardIds = new List<string> { "灰烬" } }
                }
            });

            make.Use(); // 主动触发制作
            PrintChildren(grass, "C. 主动制作后");

            // 7) 规则D：砍树（Use）——“砍”（自包含） + “树木” => 移除“树木”（仅效果，无产卡）
            // 为演示先补一棵树
            var tree3 = new SimpleCard(new CardData("树木", "树木", "", CardCategory.Item), null, "树木", "可燃烧");
            grass.AddChild(tree3);

            _engine.RegisterRule(new CardRule
            {
                Trigger = CardEventType.Use,
                Scope = RuleScope.Owner,
                Requirements = new List<IRuleRequirement>
                {
                    new CardRequirement { Kind = MatchKind.Tag, Value = "砍",  MinCount = 1, IncludeSelf = true },
                    new CardRequirement { Kind = MatchKind.Id,  Value = "树木", MinCount = 1 },
                },
                Effects = new List<IRuleEffect>
                {
                    new RemoveCardsEffect { TargetKind = TargetKind.ById, TargetValueFilter = "树木" }
                }
            });

            chop.Use(); // 砍
            PrintChildren(grass, "D. 砍树后");

            Debug.Log("\n=== EmeCard 系统示例结束 ===");
        }

        // 新增：复杂条件/组合/事件流示例
        private void RunConditionComboExample()
        {
            Debug.Log("=== EmeCard 条件/组合/事件流 示例开始 ===\n");

            _factory = new CardFactory();
            _engine = new CardRuleEngine(_factory);

            _factory.Register("灰烬", () => new SimpleCard(new CardData("灰烬", "灰烬", "燃烧后产生的灰烬", CardCategory.Item), null, "灰烬"));

            // 世界与地块（容器）
            var world = new SimpleCard(new CardData("世界", "世界", "", CardCategory.Item), null, "世界");
            var tileGrass = new SimpleCard(new CardData("草地格", "草地格", "", CardCategory.Item), null, "草地");
            var tileDirt = new SimpleCard(new CardData("泥地格", "泥地格", "", CardCategory.Item), null, "泥土");
            world.AddChild(tileGrass);
            world.AddChild(tileDirt);

            // 玩家与道具
            var player = new SimpleCard(new CardData("玩家", "玩家", "", CardCategory.Item), new GameProperty("XP", 0f), "玩家");
            tileGrass.AddChild(player); // 初始在草地格

            // 草地上的素材：树木 + 火
            var tree = new SimpleCard(new CardData("树木", "树木", "", CardCategory.Item), null, "树木", "可燃烧");
            var fire = new SimpleCard(new CardData("火", "火", "", CardCategory.Item), null, "火");
            tileGrass.AddChild(tree);
            tileGrass.AddChild(fire);

            // 将会触发事件的对象接入引擎（条件事件会从 tile/玩家 发出）
            _engine.Attach(tileGrass);
            _engine.Attach(tileDirt);
            _engine.Attach(player);

            // 规则1：玩家进入草地格 -> 燃烧树木+火 -> 产出灰烬 -> 链式给玩家加经验
            _engine.RegisterRule(new CardRule
            {
                Trigger = CardEventType.Custom,
                Scope = RuleScope.Self, // 在地块自身容器范围内匹配
                Requirements = new List<IRuleRequirement>
                {
                    // 用 Requirement 而非 CustomId 表达事件过滤
                    new ConditionRequirement(ctx => ctx.Event.ID == "PlayerEnter"),
                    new CardRequirement { Kind = MatchKind.Tag, Value = "草地", MinCount = 1, IncludeSelf = true }, // 匹配容器自身
                    new CardRequirement { Kind = MatchKind.Tag, Value = "树木", MinCount = 1 },
                    new CardRequirement { Kind = MatchKind.Tag, Value = "火",   MinCount = 1 },
                },
                Effects = new List<IRuleEffect>
                {
                    new RemoveCardsEffect { TargetKind = TargetKind.ByTag, TargetValueFilter = "树木" },
                    new RemoveCardsEffect { TargetKind = TargetKind.ByTag, TargetValueFilter = "火"   },
                    new CreateCardsEffect { CardIds = new List<string> { "灰烬" } },
                    // 链式事件：让进入者（事件Data携带的玩家）获得经验
                    new InvokeEffect((ctx, matched) =>
                    {
                        var p = ctx.Event.Data as Card;
                        if (p != null)
                        {
                            p.Custom("GainXP", 10f);
                            Debug.Log($"[进入草地] 触发链式事件：玩家获得经验 +10");
                        }
                    })
                }
            });

            // 规则2：监听玩家的“获得经验”事件（Custom），修改玩家属性
            _engine.RegisterRule(new CardRule
            {
                Trigger = CardEventType.Custom,
                Scope = RuleScope.Self, // 事件源就是玩家自身
                Requirements = new List<IRuleRequirement>
                {
                    new ConditionRequirement(ctx => ctx.Event.ID == "GainXP"),
                },
                Effects = new List<IRuleEffect>
                {
                    new ModifyPropertyEffect
                    {
                        TargetKind = TargetKind.Source,
                        ApplyMode = ModifyPropertyEffect.Mode.AddToBase,
                        Value = 10f
                    },
                    new InvokeEffect((ctx, matched) =>
                    {
                        var xp = ctx.Source.Property?.GetBaseValue() ?? 0f;
                        Debug.Log($"[玩家经验] 当前XP: {xp}");
                    })
                }
            });

            // 用于模拟“是否为夜晚”的条件变量
            bool isNight = false;

            // 规则3：时间切换演示（改为 Tick + ConditionRequirement）——夜晚点燃火把（添加“火”标签）
            var torch = new SimpleCard(new CardData("火把", "火把", "", CardCategory.Item), null, "火把");
            tileGrass.AddChild(torch);

            _engine.RegisterRule(new CardRule
            {
                Trigger = CardEventType.Tick, // 改为 Tick
                Scope = RuleScope.Self, // 由地块触发
                Requirements = new List<IRuleRequirement>
                {
                    new ConditionRequirement(ctx => isNight), // 仅在夜晚为真时命中
                    new CardRequirement { Kind = MatchKind.Tag, Value = "草地", MinCount = 1, IncludeSelf = true },
                    new CardRequirement { Kind = MatchKind.Tag, Value = "火把", MinCount = 1 }
                },
                Effects = new List<IRuleEffect>
                {
                    new AddTagEffect { TargetKind = TargetKind.ByTag, TargetValueFilter = "火把", Tag = "火" },
                    new InvokeEffect((ctx, matched) => Debug.Log("[夜晚] 草地格：火把被点燃（添加标签：火）"))
                }
            });

            // ———— 演示流程 ————

            // 玩家移动到泥地格：从草地移除 -> 加入泥地
            if (player.Owner != null) player.Owner.RemoveChild(player);
            tileDirt.AddChild(player);
            // 触发“进入泥地”（这里无规则响应，只作演示）
            tileDirt.Custom("PlayerEnter", player);
            Debug.Log("玩家移动到泥地格");

            // 玩家再移动回草地格：将触发“进入草地”的条件 -> 执行规则1（燃烧树木+火 -> 灰烬；链式触发玩家GainXP -> 规则2修改XP）
            if (player.Owner != null) player.Owner.RemoveChild(player);
            tileGrass.AddChild(player);
            tileGrass.Custom("PlayerEnter", player);

            PrintChildren(tileGrass, "进入草地后（执行燃烧与经验链式）");

            // 夜晚到来：设置 isNight 为 true -> Tick 一次触发规则3
            tileGrass.Tick(1f);
            Debug.Log($"火把是否带有'火'标签: {torch.HasTag("火")}");
            isNight = true;
            tileGrass.Tick(1f);
            Debug.Log($"火把是否带有'火'标签: {torch.HasTag("火")}");
            // 恢复为白天
            isNight = false;

            Debug.Log("\n=== EmeCard 条件/组合/事件流 示例结束 ===");
        }

        private static void PrintChildren(Card container, string title)
        {
            var names = container.Children.Select(c => c.Id).ToList();
            Debug.Log($"{title} => 容器[{container.Id}] 子项: {(names.Count == 0 ? "(空)" : string.Join(", ", names))}");
        }
    }
}