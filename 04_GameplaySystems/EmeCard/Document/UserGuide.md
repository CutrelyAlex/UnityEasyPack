# EmeCard 系统 - 用户使用指南

**适用EasyPack版本：** EasyPack v1.5.30  
**最后更新：** 2025-10-26

---

## 目录

1. [概述](#概述)
2. [快速开始](#快速开始)
3. [常见场景](#常见场景)
4. [进阶用法](#进阶用法)
5. [故障排查](#故障排查)
6. [术语表](#术语表)

---

## 概述

### 系统简介

**EmeCard** 是一个基于数据驱动的规则引擎系统，专为 Unity 游戏开发设计。它通过**卡牌**（Card）作为通用容器，将游戏中的实体、属性、行为统一建模，并通过**事件-规则-效果**的机制实现灵活的游戏逻辑。

### 核心特性

- **🎯 数据驱动**：通过 `CardData` 定义静态配置，通过 `Card` 实例管理运行时状态
- **🔄 事件驱动**：基于事件触发规则，支持 `Tick`、`Use`、`Custom` 等多种事件类型
- **🧩 规则引擎**：通过 `CardRule` 定义"条件-效果"逻辑，支持复杂的匹配与执行策略
- **🌲 层次结构**：卡牌可持有子卡牌，形成树状结构，支持递归查询与操作
- **🏷️ 标签系统**：灵活的标签匹配机制，用于规则筛选与分类
- **⚡ 性能优化**：延迟事件队列、批量处理、引用计数，避免死循环
- **🔌 可扩展**：通过 `IRuleRequirement`、`IRuleEffect` 接口自定义规则组件

### 适用场景

| 场景 | 说明 | 示例 |
|------|------|------|
| **卡牌游戏** | 卡牌效果、组合技、Buff/Debuff | 卡牌对战 |
| **合成系统** | 配方匹配、材料消耗 | 需要"木棍+火"才能合成火把 |
| **Roguelike** | 遗物、事件、随机效果 | 每次进房间触发遗物效果 |
| **策略游戏** | 单位技能、地形效果 | 草地上的单位每回合恢复生命 |
| **模拟经营** | 资源管理、建筑升级 | 仓库满时自动触发出售 |

---

## 快速开始

### 前置条件

- **Unity 版本**：Unity 2021.3 或更高版本
- **依赖系统**：需要 `EasyPack.GamePropertySystem`（用于数值属性管理）

### 安装步骤

1. **导入 EmeCard 系统**  
   将 `Assets/EasyPack/04_GameplaySystems/EmeCard/` 文件夹复制到项目中

2. **添加命名空间引用**  
   在脚本文件顶部添加：
   ```csharp
   using EasyPack.EmeCardSystem;
   using EasyPack.GamePropertySystem; // 如果需要使用属性
   ```

3. **检查依赖**  
   确保 `GamePropertySystem` 已正确导入

### 第一示例

以下示例演示如何创建一个简单的合成系统：使用"制作"工具消耗"树木"生成"木棍"。

```csharp
using UnityEngine;
using EasyPack.EmeCardSystem;
using EasyPack.GamePropertySystem;
using System.Collections.Generic;

public class QuickStartExample : MonoBehaviour
{
    void Start()
    {
        // 1. 创建工厂和引擎
        var factory = new CardFactory();
        var engine = new CardEngine(factory);

        // 2. 注册卡牌模板
        factory.Register("世界", () => 
            new Card(new CardData("世界", "世界", "", CardCategory.Object), "世界"));
        factory.Register("玩家", () => 
            new Card(new CardData("玩家", "玩家", "", CardCategory.Object), "玩家"));
        factory.Register("树木", () => 
            new Card(new CardData("树木", "树木", "", CardCategory.Object), "树木"));
        factory.Register("木棍", () => 
            new Card(new CardData("木棍", "木棍", "", CardCategory.Object), "木棍"));
        factory.Register("制作", () => 
            new Card(new CardData("制作", "制作", "", CardCategory.Action), "制作"));

        // 3. 搭建游戏世界
        var world = engine.CreateCard("世界");
        var player = engine.CreateCard("玩家");
        var tree = engine.CreateCard("树木");
        var make = engine.CreateCard("制作");
        
        world.AddChild(player);
        world.AddChild(tree);
        world.AddChild(make);

        // 4. 注册规则：使用制作工具时，如果有玩家和树木，消耗树木创建木棍
        engine.RegisterRule(b => b
            .On(CardEventType.Use)               // 监听 Use 事件
            .When(ctx => ctx.Source.HasTag("制作"))  // 要求触发源是"制作"工具
            .NeedTag("玩家")                      // 要求容器中有"玩家"
            .NeedId("树木")                       // 要求容器中有"树木"
            .DoRemoveById("树木", take: 1)       // 移除 1 个"树木"
            .DoCreate("木棍")                     // 创建 1 个"木棍"
        );

        // 5. 触发事件
        Debug.Log("初始状态：");
        PrintChildren(world);
        // 输出: 玩家, 树木, 制作

        make.Use();       // 触发制作工具的 Use 事件
        engine.Pump();    // 处理事件队列

        Debug.Log("制作后：");
        PrintChildren(world);
        // 输出: 玩家, 木棍, 制作 (树木被消耗)
    }

    void PrintChildren(Card parent)
    {
        Debug.Log($"{parent.Id}: {string.Join(", ", System.Linq.Enumerable.Select(parent.Children, c => c.Id))}");
    }
}
```

**预期输出**：
```
初始状态：
世界: 玩家, 树木, 制作

制作后：
世界: 玩家, 木棍, 制作
```

---

## 常见场景

### 场景 1：基础卡牌创建与标签管理

**使用场景**：创建带有标签的卡牌，用于后续规则匹配。

```csharp
using UnityEngine;
using EasyPack.EmeCardSystem;

public class BasicCardExample : MonoBehaviour
{
    void Start()
    {
        // 创建卡牌数据
        var weaponData = new CardData(
            id: "sword",
            name: "铁剑",
            desc: "一把普通的铁剑",
            category: CardCategory.Object,
            defaultTags: new[] { "武器", "近战" }
        );

        // 创建卡牌实例（不带属性）
        var sword = new Card(weaponData, "锋利"); // 额外添加"锋利"标签

        // 查询标签
        Debug.Log($"是否是武器：{sword.HasTag("武器")}");       // True
        Debug.Log($"是否锋利：{sword.HasTag("锋利")}");         // True
        Debug.Log($"所有标签：{string.Join(", ", sword.Tags)}"); // 武器, 近战, 锋利

        // 动态添加/移除标签
        sword.AddTag("附魔");
        sword.RemoveTag("锋利");
        Debug.Log($"更新后标签：{string.Join(", ", sword.Tags)}"); // 武器, 近战, 附魔
    }
}
```

---

### 场景 2：层次结构与持有关系

**使用场景**：构建游戏世界树，玩家持有物品，房间包含敌人。

```csharp
using UnityEngine;
using EasyPack.EmeCardSystem;

public class HierarchyExample : MonoBehaviour
{
    void Start()
    {
        var factory = new CardFactory();
        var engine = new CardEngine(factory);

        // 注册卡牌
        factory.Register("玩家", () => new Card(new CardData("玩家", "玩家"), "玩家"));
        factory.Register("背包", () => new Card(new CardData("背包", "背包"), "背包"));
        factory.Register("金币", () => new Card(new CardData("金币", "金币"), "金币"));
        factory.Register("药水", () => new Card(new CardData("药水", "药水"), "药水"));

        // 创建层次结构
        var player = engine.CreateCard("玩家");
        var bag = engine.CreateCard("背包");
        player.AddChild(bag, intrinsic: true); // 背包是"固有子卡"，无法被规则移除

        var coin1 = engine.CreateCard("金币");
        var coin2 = engine.CreateCard("金币");
        var potion = engine.CreateCard("药水");
        bag.AddChild(coin1);
        bag.AddChild(coin2);
        bag.AddChild(potion);

        // 查询层次结构
        Debug.Log($"玩家的子卡数量：{player.ChildrenCount}");          // 1
        Debug.Log($"背包的子卡数量：{bag.ChildrenCount}");             // 3
        Debug.Log($"金币的持有者：{coin1.Owner.Id}");                  // 背包
        Debug.Log($"背包是否为固有：{player.IsIntrinsic(bag)}");       // True

        // 尝试移除固有子卡（失败）
        bool removed = player.RemoveChild(bag, force: false);
        Debug.Log($"移除背包（非强制）：{removed}");                    // False

        // 强制移除固有子卡（成功）
        removed = player.RemoveChild(bag, force: true);
        Debug.Log($"移除背包（强制）：{removed}");                      // True
    }
}
```

---

### 场景 3：规则匹配与效果执行

**使用场景**：实现"燃烧"效果：每次 Tick 时，所有带"可燃烧"标签的卡牌转换为"灰烬"。

```csharp
using UnityEngine;
using EasyPack.EmeCardSystem;

public class BurnRuleExample : MonoBehaviour
{
    void Start()
    {
        var factory = new CardFactory();
        var engine = new CardEngine(factory);

        factory.Register("森林", () => new Card(new CardData("森林", "森林"), "森林"));
        factory.Register("树木", () => new Card(new CardData("树木", "树木"), "树木", "可燃烧"));
        factory.Register("灰烬", () => new Card(new CardData("灰烬", "灰烬"), "灰烬"));

        var forest = engine.CreateCard("森林");
        var tree1 = engine.CreateCard("树木");
        var tree2 = engine.CreateCard("树木");
        forest.AddChild(tree1);
        forest.AddChild(tree2);

        // 注册燃烧规则
        engine.RegisterRule(b => b
            .On(CardEventType.Tick)                   // 监听 Tick 事件
            .NeedTag("可燃烧", minCount: 1, maxMatched: 0) // 需要至少 1 个可燃烧物，返回所有匹配
            .DoRemoveByTag("可燃烧")                  // 移除所有可燃烧物
            .DoInvoke((ctx, matched) =>               // 为每个匹配创建灰烬
            {
                foreach (var _ in matched)
                    ctx.Container.AddChild(ctx.Factory.Owner.CreateCard("灰烬"));
                Debug.Log($"燃烧了 {matched.Count} 个物体");
            })
        );

        Debug.Log($"燃烧前：{string.Join(", ", System.Linq.Enumerable.Select(forest.Children, c => c.Id))}");
        // 输出: 树木, 树木

        forest.Tick(1f);
        engine.Pump();

        Debug.Log($"燃烧后：{string.Join(", ", System.Linq.Enumerable.Select(forest.Children, c => c.Id))}");
        // 输出: 灰烬, 灰烬
    }
}
```

---

### 场景 4：递归选择与深度限制

**使用场景**：在整个世界树中查找所有"玩家"卡牌，无论层级深度。

```csharp
using UnityEngine;
using EasyPack.EmeCardSystem;
using System.Linq;

public class RecursiveSelectionExample : MonoBehaviour
{
    void Start()
    {
        var factory = new CardFactory();
        var engine = new CardEngine(factory);

        factory.Register("世界", () => new Card(new CardData("世界", "世界"), "世界"));
        factory.Register("区域", () => new Card(new CardData("区域", "区域"), "区域"));
        factory.Register("玩家", () => new Card(new CardData("玩家", "玩家"), "玩家"));

        var world = engine.CreateCard("世界");
        var area1 = engine.CreateCard("区域");
        var area2 = engine.CreateCard("区域");
        world.AddChild(area1);
        world.AddChild(area2);

        var player1 = engine.CreateCard("玩家");
        var player2 = engine.CreateCard("玩家");
        var player3 = engine.CreateCard("玩家");
        area1.AddChild(player1);
        area2.AddChild(player2);
        area2.AddChild(player3);

        // 注册规则：递归查找所有玩家
        engine.RegisterRule(b => b
            .On(CardEventType.Custom, "统计玩家")
            .AtRoot()                               // 以根容器为锚点
            .NeedTagRecursive("玩家", minCount: 1, maxMatched: 0) // 递归查找所有玩家
            .DoInvoke((ctx, matched) =>
            {
                Debug.Log($"在整个世界树中发现 {matched.Count} 个玩家");
                foreach (var p in matched)
                    Debug.Log($"  - {p.Id} (Owner: {p.Owner.Id})");
            })
        );

        world.Custom("统计玩家");
        engine.Pump();
        // 输出: 在整个世界树中发现 3 个玩家
        //       - 玩家 (Owner: 区域)
        //       - 玩家 (Owner: 区域)
        //       - 玩家 (Owner: 区域)
    }
}
```

---

### 场景 5：属性修改与 GameProperty 集成

**使用场景**：实现 Buff 系统，玩家获得"力量药水"标签时，攻击力 +10。

```csharp
using UnityEngine;
using EasyPack.EmeCardSystem;
using EasyPack.GamePropertySystem;
using System.Collections.Generic;

public class PropertyModificationExample : MonoBehaviour
{
    void Start()
    {
        var factory = new CardFactory();
        var engine = new CardEngine(factory);

        // 注册带属性的玩家
        factory.Register("玩家", () => new Card(
            new CardData("玩家", "玩家"),
            new List<GameProperty> { new GameProperty("攻击力", 50f) },
            "玩家"
        ));

        var player = engine.CreateCard("玩家");
        Debug.Log($"初始攻击力：{player.GetProperty("攻击力").GetValue()}"); // 50

        // 注册规则：添加"力量药水"标签时，攻击力 +10
        engine.RegisterRule(b => b
            .On(CardEventType.Custom, "使用药水")
            .WhenSourceHasTag("玩家")
            .DoModifyMatched("攻击力", 10f, ModifyPropertyEffect.Mode.AddToBase)
            .DoInvoke((ctx, matched) =>
            {
                var atk = ctx.Source.GetProperty("攻击力").GetValue();
                Debug.Log($"使用力量药水后，攻击力：{atk}");
            })
        );

        player.Custom("使用药水");
        engine.Pump();
        // 输出: 使用力量药水后，攻击力：60
    }
}
```

---

## 进阶用法

### 1. 自定义规则组件

实现自定义的 `IRuleRequirement` 和 `IRuleEffect`：

```csharp
using System.Collections.Generic;
using EasyPack.EmeCardSystem;

// 自定义要求：检查容器子卡总数是否超过阈值
public class ChildCountRequirement : IRuleRequirement
{
    public int MinCount { get; set; } = 1;
    public int MaxCount { get; set; } = int.MaxValue;

    public bool TryMatch(CardRuleContext ctx, out List<Card> matched)
    {
        matched = new List<Card>();
        int count = ctx.Container?.ChildrenCount ?? 0;
        return count >= MinCount && count <= MaxCount;
    }
}

// 自定义效果：播放音效
public class PlaySoundEffect : IRuleEffect
{
    public string SoundName { get; set; }

    public void Execute(CardRuleContext ctx, IReadOnlyList<Card> matched)
    {
        // 伪代码：播放音效
        UnityEngine.Debug.Log($"播放音效：{SoundName}");
        // AudioManager.Play(SoundName);
    }
}

// 使用示例
public class CustomComponentExample : MonoBehaviour
{
    void Start()
    {
        var engine = new CardEngine(new CardFactory());
        engine.RegisterRule(b => b
            .On(CardEventType.Use)
            .AddRequirement(new ChildCountRequirement { MinCount = 5, MaxCount = 10 })
            .Do(new PlaySoundEffect { SoundName = "success.wav" })
        );
    }
}
```

---

### 2. 规则优先级与执行顺序

控制规则的执行顺序：

```csharp
var engine = new CardEngine(new CardFactory());

// 设置引擎为优先级模式
engine.Policy.RuleSelection = RuleSelectionMode.Priority;

// 高优先级规则（Priority 值越小越优先）
engine.RegisterRule(b => b
    .On(CardEventType.Tick)
    .Priority(1)  // 最高优先级
    .DoInvoke((ctx, m) => Debug.Log("规则 1：最先执行"))
);

engine.RegisterRule(b => b
    .On(CardEventType.Tick)
    .Priority(5)  // 中等优先级
    .DoInvoke((ctx, m) => Debug.Log("规则 2：第二执行"))
);

engine.RegisterRule(b => b
    .On(CardEventType.Tick)
    .Priority(10) // 低优先级
    .DoInvoke((ctx, m) => Debug.Log("规则 3：最后执行"))
);

// 输出顺序: 规则 1 → 规则 2 → 规则 3
```

---

### 3. 延迟事件与批量处理

理解事件处理的深度机制：

```csharp
var engine = new CardEngine(new CardFactory());
var root = engine.CreateCard("根");
var child = engine.CreateCard("子");
root.AddChild(child);

// 注册规则：Use 事件触发新的 Custom 事件
engine.RegisterRule(b => b
    .On(CardEventType.Use)
    .DoInvoke((ctx, m) =>
    {
        Debug.Log("收到 Use 事件，触发 Custom 事件");
        ctx.Source.Custom("递归事件");
    })
);

engine.RegisterRule(b => b
    .On(CardEventType.Custom, "递归事件")
    .DoInvoke((ctx, m) => Debug.Log("收到 Custom 事件"))
);

root.Use();
engine.Pump();

// 输出:
// 收到 Use 事件，触发 Custom 事件
// 收到 Custom 事件
```

**关键机制**：
- 在规则执行过程中（`_processingDepth > 0`）触发的新事件会进入**延迟队列**
- 主队列处理完毕后，延迟队列的事件会被批量移入主队列
- 避免了事件处理过程中队列被修改导致的迭代器失效

---

### 4. 性能优化建议

#### 4.1 限制递归深度

```csharp
// ❌ 不推荐：无限递归深度
engine.RegisterRule(b => b
    .On(CardEventType.Tick)
    .NeedTagRecursive("目标", maxDepth: null) // 可能遍历整个树
);

// ✅ 推荐：限制深度
engine.RegisterRule(b => b
    .On(CardEventType.Tick)
    .MaxDepth(3)                              // 仅递归 3 层
    .NeedTagRecursive("目标", maxDepth: 3)
);
```

#### 4.2 使用 Take 限制匹配数量

```csharp
// ❌ 不推荐：处理所有匹配
engine.RegisterRule(b => b
    .On(CardEventType.Use)
    .NeedTag("敌人", maxMatched: 0)           // 返回所有匹配
    .DoRemoveByTag("敌人")                    // 移除所有敌人
);

// ✅ 推荐：限制数量
engine.RegisterRule(b => b
    .On(CardEventType.Use)
    .NeedTag("敌人", maxMatched: 5)           // 最多返回 5 个
    .DoRemoveByTag("敌人", take: 5)          // 最多移除 5 个
);
```

#### 4.3 避免死循环

```csharp
// ❌ 危险：可能导致死循环
engine.RegisterRule(b => b
    .On(CardEventType.Custom, "事件A")
    .DoInvoke((ctx, m) => ctx.Source.Custom("事件B"))
);

engine.RegisterRule(b => b
    .On(CardEventType.Custom, "事件B")
    .DoInvoke((ctx, m) => ctx.Source.Custom("事件A")) // 会触发事件A
);

// ✅ 安全：使用 StopPropagation 中断传播
engine.RegisterRule(b => b
    .On(CardEventType.Custom, "事件A")
    .DoInvoke((ctx, m) => ctx.Source.Custom("事件B"))
    .StopPropagation() // 执行后中止后续规则
);
```

---

## 故障排查

### 常见问题

#### 问题 1：编译错误 - 找不到类型 `CardEngine`

**症状**：  
```
The type or namespace name 'CardEngine' could not be found
```

**原因**：缺少命名空间引用

**解决方法**：  
在文件头部添加：
```csharp
using EasyPack.EmeCardSystem;
```

---

#### 问题 2：规则没有触发

**症状**：触发事件后，规则效果没有执行

**排查步骤**：
1. **检查事件类型是否匹配**
   ```csharp
   // 规则监听 Use 事件
   engine.RegisterRule(b => b.On(CardEventType.Use) ...);
   
   // 但触发的是 Tick 事件
   card.Tick(1f); // ❌ 不匹配
   card.Use();    // ✅ 匹配
   ```

2. **检查 CustomId 是否匹配**
   ```csharp
   // 规则监听特定 Custom 事件
   engine.RegisterRule(b => b.On(CardEventType.Custom, "攻击") ...);
   
   // 但触发的是其他 ID
   card.Custom("防御"); // ❌ 不匹配
   card.Custom("攻击"); // ✅ 匹配
   ```

3. **检查条件是否满足**
   ```csharp
   // 规则要求容器中有"玩家"标签
   engine.RegisterRule(b => b.NeedTag("玩家") ...);
   
   // 但容器中没有"玩家"
   container.AddChild(new Card(...)); // 确保添加了带"玩家"标签的卡
   ```

4. **确认已调用 `engine.Pump()`**
   ```csharp
   card.Use();
   // ❌ 缺少 Pump，事件在队列中未处理
   
   card.Use();
   engine.Pump(); // ✅ 正确
   ```

---

#### 问题 3：无法移除固有子卡

**症状**：
```csharp
bool removed = parent.RemoveChild(child); // 返回 false
```

**原因**：子卡被标记为固有（`intrinsic`），普通 `RemoveChild` 无法移除

**解决方法**：
```csharp
// 方法 1：强制移除
parent.RemoveChild(child, force: true);

// 方法 2：不使用 intrinsic 标记
parent.AddChild(child, intrinsic: false);
```

---

#### 问题 4：事件处理超过最大限制

**原因**：规则之间形成循环触发

**解决方法**：
1. **检查规则逻辑**，避免 A 触发 B，B 触发 A
2. **使用 `StopPropagation()`** 中断传播
3. **添加条件判断**，防止重复触发
   ```csharp
   engine.RegisterRule(b => b
       .On(CardEventType.Custom, "循环")
       .When(ctx => !ctx.Source.HasTag("已处理")) // 防止重复
       .DoAddTagToSource("已处理")
       .DoInvoke((ctx, m) => ctx.Source.Custom("循环"))
   );
   ```

---

#### 问题 5：属性修改不生效

**症状**：调用 `DoModifyMatched` 后属性值没有变化

**排查步骤**：
1. **确认卡牌有该属性**
   ```csharp
   var prop = card.GetProperty("攻击力");
   if (prop == null)
       Debug.LogError("卡牌没有'攻击力'属性");
   ```

2. **检查属性名是否正确**（区分大小写）
   ```csharp
   DoModifyMatched("攻击力", 10f) // ✅ 正确
   DoModifyMatched("攻击", 10f)   // ❌ 属性名不匹配
   ```

3. **验证 Scope 和匹配结果**
   ```csharp
   .DoModify("攻击力", 10f, scope: TargetScope.Matched) // 作用于匹配结果
   .DoInvoke((ctx, matched) => Debug.Log($"匹配数量：{matched.Count}"))
   ```

---

### FAQ 更新记录

*本节持续更新，记录用户反馈的新问题。*

#### 问题 X：（待补充）
*如遇到未列出的问题，请提交 GitHub Issue 或联系维护团队。*

---

## 术语表

### 核心术语（中英文对照）

| 中文 | 英文 | 说明 |
|------|------|------|
| **卡牌** | Card | 系统的基本单元，可表示实体、属性、行为等 |
| **卡牌数据** | CardData | 卡牌的静态配置（ID、名称、描述、默认标签等） |
| **卡牌引擎** | CardEngine | 管理卡牌实例、规则注册、事件分发的核心引擎 |
| **卡牌工厂** | CardFactory | 根据 ID 创建卡牌实例的工厂 |
| **规则** | CardRule | 定义"触发条件-效果"的逻辑单元 |
| **要求项** | IRuleRequirement | 规则的匹配条件（如"需要有玩家"） |
| **效果** | IRuleEffect | 规则执行的结果（如"移除卡牌"、"创建卡牌"） |
| **事件** | CardEvent | 触发规则的载体，包含类型、ID、数据 |
| **标签** | Tag | 字符串标识，用于分类和匹配（大小写敏感） |
| **持有者** | Owner | 当前卡牌的父卡（卡牌树中的父节点） |
| **子卡牌** | Children | 当前卡牌持有的子卡列表 |
| **固有子卡** | Intrinsic Child | 不可被规则消耗或移除的特殊子卡 |
| **容器** | Container | 规则执行的上下文容器（由 `OwnerHops` 决定） |
| **触发源** | Source | 触发事件的卡牌 |
| **匹配集** | Matched | 规则匹配阶段返回的卡牌集合 |
| **选择范围** | TargetScope | 目标选择的作用域（Children/Descendants/Matched） |
| **过滤模式** | CardFilterMode | 筛选卡牌的方式（ByTag/ById/ByCategory/None） |
| **优先级** | Priority | 规则执行的优先级（数值越小越优先） |
| **递归深度** | MaxDepth | 递归查询时的最大层数限制 |

### 事件类型

| 事件类型 | 说明 | 触发方式 |
|---------|------|---------|
| **AddedToOwner** | 卡牌被添加到持有者时触发 | `owner.AddChild(card)` |
| **RemovedFromOwner** | 卡牌从持有者移除时触发 | `owner.RemoveChild(card)` |
| **Tick** | 定时/帧更新事件 | `card.Tick(deltaTime)` |
| **Use** | 主动使用事件 | `card.Use()` |
| **Custom** | 自定义事件 | `card.Custom(id, data)` |

### 卡牌类别

| 类别 | 说明 | 示例 |
|------|------|------|
| **Object** | 物品/实体类 | 玩家、敌人、道具 |
| **Attribute** | 属性/状态类 | Buff、Debuff、标记 |
| **Action** | 行为/动作类 | 技能、制作工具 |
| **Environment** | 环境类 | 地形、天气 |

---

**维护者**：NEKOPACK 团队  
**联系方式**：提交 GitHub Issue 或 Pull Request  
**许可证**：遵循项目主许可证
