# EmeCard 系统 - 用户使用指南

**适用 EasyPack 版本:** EasyPack v1.7.0  
**最后更新:** 2025-11-09

---

## 目录

- [EmeCard 系统 - 用户使用指南](#emecard-系统---用户使用指南)
  - [目录](#目录)
  - [概述](#概述)
    - [核心特性](#核心特性)
    - [适用场景](#适用场景)
    - [系统架构](#系统架构)
  - [快速开始](#快速开始)
    - [前置条件](#前置条件)
    - [第一个示例](#第一个示例)
  - [常见场景](#常见场景)
    - [场景1: 初始化工厂和引擎](#场景1-初始化工厂和引擎)
    - [场景2: 创建卡牌模板](#场景2-创建卡牌模板)
    - [场景3: 搭建游戏世界](#场景3-搭建游戏世界)
    - [场景4: 注册简单规则](#场景4-注册简单规则)
    - [场景5: 演示事件驱动](#场景5-演示事件驱动)
  - [进阶用法](#进阶用法)
    - [递归选择](#递归选择)
    - [复杂规则组合](#复杂规则组合)
    - [自定义效果](#自定义效果)
  - [作用域控制: 容器与选择详解](#作用域控制-容器与选择详解)
    - [维度1: 容器位置 — OwnerHops()](#维度1-容器位置--ownerhops)
    - [维度2: 选择范围 — SelectionRoot 与 TargetScope](#维度2-选择范围--selectionroot-与-targetscope)
    - [维度3: 前置条件 — When\*\*\* 方法](#维度3-前置条件--when-方法)
    - [典型应用模式](#典型应用模式)
  - [故障排查](#故障排查)
    - [问题1: 规则不生效](#问题1-规则不生效)
    - [问题2: 卡牌没有被创建](#问题2-卡牌没有被创建)
    - [问题3: 递归选择找不到卡牌](#问题3-递归选择找不到卡牌)
    - [问题4: 固有子卡无法移除](#问题4-固有子卡无法移除)
  - [术语表](#术语表)
    - [核心概念](#核心概念)
    - [事件类型](#事件类型)
    - [规则组件](#规则组件)
    - [选择系统](#选择系统)
    - [卡牌结构](#卡牌结构)
    - [策略配置](#策略配置)

---

## 概述

**EmeCard 系统** 是一个基于卡牌树形结构和事件驱动的游戏逻辑框架。它将游戏世界建模为卡牌的层次结构,通过规则引擎响应事件并执行效果。

### 核心特性

- **树形结构**: 卡牌可以持有子卡牌,构建层次化的游戏世界
- **事件驱动**: 通过 `Use`、`Tick`、`Custom` 等事件触发规则
- **数据驱动**: 规则由 `CardRule` 对象定义,支持序列化
- **流式API**: 使用 `CardRuleBuilder` 快速构建规则
- **属性系统**: 集成 GamePropertySystem,支持修饰符
- **标签系统**: 通过标签灵活过滤和匹配卡牌

### 适用场景

- **卡牌游戏**: 牌库、手牌、场上单位的管理和交互
- **物品系统**: 背包、装备、消耗品的层级管理
- **技能系统**: 技能树、Buff/Debuff、状态效果
- **剧情系统**: 任务、对话、事件的触发与流转
- **模拟经营**: 资源、建筑、单位的生产与消耗

### 系统架构

```
CardEngine (引擎)
    ├─ CardFactory (工厂) - 注册卡牌构造函数
    ├─ CardRule (规则) - 定义触发条件和效果
    └─ Card (卡牌实例)
        ├─ CardData (静态数据)
        ├─ Properties (属性列表)
        ├─ Tags (标签集合)
        └─ Children (子卡牌)
```

---

## 快速开始

### 前置条件

- Unity 2021.3 或更高版本
- 已导入 EasyPack 包
- 了解 C# 基础语法

### 第一个示例

创建一个简单的卡牌系统,实现"使用工具消耗木材创建木棍"的逻辑:

```csharp
using UnityEngine;
using EasyPack.EmeCardSystem;
using EasyPack.GamePropertySystem;

public class FirstExample : MonoBehaviour
{
    private void Start()
    {
        // 1. 创建工厂和引擎
        var factory = new CardFactory();
        var engine = new CardEngine(factory);
        
        // 2. 注册卡牌模板
        factory.Register("工具", () => new Card(
            new CardData("工具", "制作工具", "", CardCategory.Object, new[] { "制作" })
        ));
        
        factory.Register("木材", () => new Card(
            new CardData("木材", "木材", "", CardCategory.Object)
        ));
        
        factory.Register("木棍", () => new Card(
            new CardData("木棍", "木棍", "", CardCategory.Object)
        ));
        
        // 3. 创建世界容器和卡牌
        var world = new Card(new CardData("世界", "游戏世界"));
        engine.AddCard(world);
        
        var player = engine.CreateCard("工具");
        world.AddChild(player);
        
        var wood = engine.CreateCard("木材");
        world.AddChild(wood);
        
        // 4. 注册规则: 使用制作工具消耗木材创建木棍
        engine.RegisterRule(b => b
            .On(CardEventType.Use)
            .WhenSourceHasTag("制作")
            .NeedTag("玩家", 1)
            .NeedId("木材", 1)
            .DoRemoveById("木材", take: 1)
            .DoCreate("木棍", 1)
        );
        
        // 5. 触发事件
        Debug.Log($"使用前: 木材={world.Children.Count(c => c.Id == "木材")}, 木棍={world.Children.Count(c => c.Id == "木棍")}");
        
        player.Use();
        engine.Pump();
        
        Debug.Log($"使用后: 木材={world.Children.Count(c => c.Id == "木材")}, 木棍={world.Children.Count(c => c.Id == "木棍")}");
    }
}
```

**预期输出:**
```
使用前: 木材=1, 木棍=0
使用后: 木材=0, 木棍=1
```

---

## 常见场景

### 场景1: 初始化工厂和引擎

基于 `EmeCardExample.ShowFactoryAndEngineInitialization()`:

```csharp
using UnityEngine;
using EasyPack.EmeCardSystem;

public class FactoryInitExample : MonoBehaviour
{
    private CardEngine _engine;
    private CardFactory _factory;
    
    private void Start()
    {
        // 创建工厂
        _factory = new CardFactory();
        
        // 创建引擎并关联工厂
        _engine = new CardEngine(_factory);
        
        // 验证关联
        Debug.Log($"工厂的引擎引用: {_factory.Owner != null}"); // True
        Debug.Log($"引擎的工厂引用: {_engine.CardFactory != null}"); // True
    }
}
```

**说明:**
- `CardFactory` 负责注册和创建卡牌实例
- `CardEngine` 管理卡牌实例、规则和事件队列
- 两者通过 `Owner` 属性相互引用

---

### 场景2: 创建卡牌模板

基于 `EmeCardExample.ShowCardTemplateCreation()`:

```csharp
using UnityEngine;
using EasyPack.EmeCardSystem;
using EasyPack.GamePropertySystem;

public class CardTemplateExample : MonoBehaviour
{
    private void Start()
    {
        var factory = new CardFactory();
        
        // 1. 简单模板: 仅静态数据
        factory.Register("金币", () => new Card(
            new CardData("金币", "金币", "游戏货币", CardCategory.Object)
        ));
        
        // 2. 带标签的模板
        factory.Register("玩家", () => new Card(
            new CardData("玩家", "玩家", "", CardCategory.Object, 
                        defaultTags: new[] { "玩家", "角色" })
        ));
        
        // 3. 带属性的模板
        factory.Register("英雄", () => 
        {
            var data = new CardData("英雄", "勇者", "", CardCategory.Object);
            var properties = new List<GameProperty>
            {
                new GameProperty("生命值", 100f),
                new GameProperty("攻击力", 20f),
                new GameProperty("防御力", 10f)
            };
            return new Card(data, properties);
        });
        
        // 4. 带固有子卡的模板
        factory.Register("战士", () =>
        {
            var warrior = new Card(new CardData("战士", "战士"));
            var sword = new Card(new CardData("剑", "铁剑", "", CardCategory.Object, new[] { "武器" }));
            warrior.AddChild(sword, intrinsic: true); // 固有装备
            return warrior;
        });
        
        // 测试创建
        var engine = new CardEngine(factory);
        var coin = engine.CreateCard("金币");
        var hero = engine.CreateCard("英雄");
        var warrior = engine.CreateCard("战士");
        
        Debug.Log($"金币名称: {coin.Name}");
        Debug.Log($"英雄属性: {hero.Properties.Count}");
        Debug.Log($"战士装备: {warrior.Intrinsics.Count}");
    }
}
```

**关键点:**
- 使用 `factory.Register()` 注册构造函数
- 模板可以包含属性、标签、固有子卡
- 通过 `engine.CreateCard()` 实例化

---

### 场景3: 搭建游戏世界

基于 `EmeCardExample.ShowWorldSetup()`:

```csharp
using UnityEngine;
using EasyPack.EmeCardSystem;

public class WorldSetupExample : MonoBehaviour
{
    private void Start()
    {
        var factory = new CardFactory();
        var engine = new CardEngine(factory);
        
        // 注册模板
        factory.Register("世界", () => new Card(new CardData("世界", "游戏世界")));
        factory.Register("玩家", () => new Card(new CardData("玩家", "玩家", "", 
            CardCategory.Object, new[] { "玩家" })));
        factory.Register("金币", () => new Card(new CardData("金币", "金币")));
        factory.Register("宝石", () => new Card(new CardData("宝石", "宝石")));
        
        // 创建世界根节点
        var world = engine.CreateCard("世界");
        
        // 创建玩家并添加到世界
        var player = engine.CreateCard("玩家");
        world.AddChild(player);
        
        // 创建资源并添加到世界
        for (int i = 0; i < 5; i++)
        {
            var coin = engine.CreateCard("金币");
            world.AddChild(coin);
        }
        
        for (int i = 0; i < 3; i++)
        {
            var gem = engine.CreateCard("宝石");
            world.AddChild(gem);
        }
        
        // 显示层次结构
        DisplayCardHierarchy(world, "游戏世界结构");
    }
    
    private void DisplayCardHierarchy(Card root, string title)
    {
        Debug.Log($"===== {title} =====");
        DisplayCardRecursive(root, 0);
    }
    
    private void DisplayCardRecursive(Card card, int depth)
    {
        string indent = new string(' ', depth * 2);
        Debug.Log($"{indent}- {card.Name} (ID:{card.Id}, Index:{card.Index})");
        foreach (var child in card.Children)
        {
            DisplayCardRecursive(child, depth + 1);
        }
    }
}
```

**预期输出:**
```
===== 游戏世界结构 =====
- 游戏世界 (ID:世界, Index:0)
  - 玩家 (ID:玩家, Index:1)
  - 金币 (ID:金币, Index:2)
  - 金币 (ID:金币, Index:3)
  - 金币 (ID:金币, Index:4)
  - 金币 (ID:金币, Index:5)
  - 金币 (ID:金币, Index:6)
  - 宝石 (ID:宝石, Index:7)
  - 宝石 (ID:宝石, Index:8)
  - 宝石 (ID:宝石, Index:9)
```

---

### 场景4: 注册简单规则

基于 `EmeCardExample.ShowSimpleRuleRegistration()`:

```csharp
using UnityEngine;
using EasyPack.EmeCardSystem;

public class SimpleRuleExample : MonoBehaviour
{
    private void Start()
    {
        var factory = new CardFactory();
        var engine = new CardEngine(factory);
        
        // 注册模板
        factory.Register("玩家", () => new Card(new CardData("玩家", "玩家", "", 
            CardCategory.Object, new[] { "玩家" })));
        factory.Register("金币", () => new Card(new CardData("金币", "金币")));
        factory.Register("宝箱", () => new Card(new CardData("宝箱", "宝箱", "", 
            CardCategory.Object, new[] { "可用" })));
        
        // 创建世界
        var world = new Card(new CardData("世界", "游戏世界"));
        engine.AddCard(world);
        
        var player = engine.CreateCard("玩家");
        world.AddChild(player);
        
        var chest = engine.CreateCard("宝箱");
        world.AddChild(chest);
        
        // 规则1: 使用宝箱创建5个金币
        engine.RegisterRule(b => b
            .On(CardEventType.Use)
            .WhenSourceHasTag("可用")
            .NeedTag("玩家", 1)
            .DoCreate("金币", 5)
            .PrintContext()
        );
        
        Debug.Log($"使用前金币数量: {world.Children.Count(c => c.Id == "金币")}");
        
        chest.Use();
        engine.Pump();
        
        Debug.Log($"使用后金币数量: {world.Children.Count(c => c.Id == "金币")}");
    }
}
```

**预期输出:**
```
使用前金币数量: 0
使用后金币数量: 5
```

---

### 场景5: 演示事件驱动

基于 `EmeCardExample.ShowEventDrivenSystem()`:

```csharp
using UnityEngine;
using EasyPack.EmeCardSystem;
using EasyPack.GamePropertySystem;

public class EventDrivenExample : MonoBehaviour
{
    private void Start()
    {
        var factory = new CardFactory();
        var engine = new CardEngine(factory);
        
        // 注册模板
        factory.Register("火把", () =>
        {
            var torch = new Card(new CardData("火把", "火把", "", 
                CardCategory.Object, new[] { "火把" }));
            torch.Properties.Add(new GameProperty("燃烧时间", 0f));
            return torch;
        });
        
        // 创建世界和火把
        var world = new Card(new CardData("世界", "游戏世界"));
        engine.AddCard(world);
        
        var torch = engine.CreateCard("火把");
        world.AddChild(torch);
        
        // 规则1: Tick事件增加燃烧时间
        engine.RegisterRule(b => b
            .On(CardEventType.Tick)
            .NeedTag("火把", 1)
            .DoInvoke((ctx, matched) =>
            {
                var deltaTime = ctx.DeltaTime;
                foreach (var card in matched)
                {
                    var burnTime = card.GetProperty("燃烧时间");
                    burnTime.SetBaseValue(burnTime.GetBaseValue() + deltaTime);
                    Debug.Log($"{card.Name} 燃烧时间: {burnTime.GetBaseValue():F2}秒");
                }
            })
        );
        
        // 规则2: 燃烧时间>=5秒时移除火把
        engine.RegisterRule(b => b
            .On(CardEventType.Tick)
            .WhenWithCards(ctx =>
            {
                var burnedTorches = ctx.Container.Children
                    .Where(c => c.HasTag("火把") && 
                               c.GetProperty("燃烧时间").GetBaseValue() >= 5f)
                    .ToList();
                return (burnedTorches.Count > 0, burnedTorches);
            })
            .DoInvoke((ctx, matched) =>
            {
                foreach (var card in matched)
                {
                    Debug.Log($"{card.Name} 已燃尽,将被移除");
                    ctx.Container.RemoveChild(card);
                }
            })
        );
        
        // 模拟6次Tick
        for (int i = 0; i < 6; i++)
        {
            torch.Tick(1f);
            engine.Pump();
        }
        
        Debug.Log($"最终世界子卡数量: {world.Children.Count}");
    }
}
```

**预期输出:**
```
火把 燃烧时间: 1.00秒
火把 燃烧时间: 2.00秒
火把 燃烧时间: 3.00秒
火把 燃烧时间: 4.00秒
火把 燃烧时间: 5.00秒
火把 已燃尽,将被移除
最终世界子卡数量: 0
```

---

## 进阶用法

### 递归选择

基于 `EmeCardExample.ShowRecursiveSelection()`:

```csharp
using UnityEngine;
using EasyPack.EmeCardSystem;

public class RecursiveSelectionExample : MonoBehaviour
{
    private void Start()
    {
        var factory = new CardFactory();
        var engine = new CardEngine(factory);
        
        // 注册模板
        factory.Register("容器", () => new Card(new CardData("容器", "容器")));
        factory.Register("金币", () => new Card(new CardData("金币", "金币")));
        
        // 创建嵌套结构
        var root = engine.CreateCard("容器");
        var level1 = engine.CreateCard("容器");
        var level2 = engine.CreateCard("容器");
        
        root.AddChild(level1);
        level1.AddChild(level2);
        
        // 在各层添加金币
        root.AddChild(engine.CreateCard("金币"));
        level1.AddChild(engine.CreateCard("金币"));
        level2.AddChild(engine.CreateCard("金币"));
        
        // 规则: 递归查找所有金币(深度限制为2)
        engine.RegisterRule(b => b
            .On(CardEventType.Use)
            .AtSelf()
            .NeedIdRecursive("金币", minCount: 1, maxDepth: 2)
            .DoInvoke((ctx, matched) =>
            {
                Debug.Log($"递归找到 {matched.Count} 个金币");
                foreach (var coin in matched)
                {
                    Debug.Log($"- 金币所在层级: {GetDepth(coin, root)}");
                }
            })
        );
        
        root.Use();
        engine.Pump();
    }
    
    private int GetDepth(Card card, Card root)
    {
        int depth = 0;
        var current = card.Owner;
        while (current != null && current != root)
        {
            depth++;
            current = current.Owner;
        }
        return depth;
    }
}
```

---

### 复杂规则组合

基于 `EmeCardExample.ShowComplexRules()`:

```csharp
using UnityEngine;
using EasyPack.EmeCardSystem;

public class ComplexRulesExample : MonoBehaviour
{
    private void Start()
    {
        var factory = new CardFactory();
        var engine = new CardEngine(factory);
        
        // 注册模板
        factory.Register("玩家", () => new Card(new CardData("玩家", "玩家", "", 
            CardCategory.Object, new[] { "玩家" })));
        factory.Register("金币", () => new Card(new CardData("金币", "金币")));
        factory.Register("宝石", () => new Card(new CardData("宝石", "宝石")));
        factory.Register("商人", () => new Card(new CardData("商人", "商人", "", 
            CardCategory.Object, new[] { "可用", "商人" })));
        
        // 创建世界
        var world = new Card(new CardData("世界", "游戏世界"));
        engine.AddCard(world);
        
        var player = engine.CreateCard("玩家");
        world.AddChild(player);
        
        // 添加10个金币到玩家
        for (int i = 0; i < 10; i++)
        {
            var coin = engine.CreateCard("金币");
            player.AddChild(coin);
        }
        
        var merchant = engine.CreateCard("商人");
        world.AddChild(merchant);
        
        // 规则: 使用商人,消耗5个金币换1个宝石
        engine.RegisterRule(b => b
            .On(CardEventType.Use)
            .WhenSourceHasTag("商人")
            .NeedTag("玩家", 1)
            .NeedSourceId("金币", 5) // 需要玩家有5个金币
            .DoInvoke((ctx, matched) =>
            {
                // 找到玩家
                var player = matched.FirstOrDefault(c => c.HasTag("玩家"));
                if (player != null)
                {
                    // 移除5个金币
                    var coins = player.Children.Where(c => c.Id == "金币").Take(5).ToList();
                    foreach (var coin in coins)
                    {
                        player.RemoveChild(coin);
                    }
                    
                    // 添加1个宝石
                    var gem = ctx.Factory.Create("宝石");
                    player.AddChild(gem);
                    
                    Debug.Log($"交易成功! 玩家现有金币: {player.Children.Count(c => c.Id == "金币")}, 宝石: {player.Children.Count(c => c.Id == "宝石")}");
                }
            })
        );
        
        Debug.Log($"交易前 - 金币: {player.Children.Count(c => c.Id == "金币")}, 宝石: {player.Children.Count(c => c.Id == "宝石")}");
        
        merchant.Use();
        engine.Pump();
        
        Debug.Log($"交易后 - 金币: {player.Children.Count(c => c.Id == "金币")}, 宝石: {player.Children.Count(c => c.Id == "宝石")}");
    }
}
```

---

### 自定义效果

创建自定义效果类:

```csharp
using System.Collections.Generic;
using EasyPack.EmeCardSystem;
using UnityEngine;

/// <summary>
/// 自定义效果: 日志输出
/// </summary>
public class LogEffect : IRuleEffect
{
    private string _message;
    
    public LogEffect(string message)
    {
        _message = message;
    }
    
    public void Execute(CardRuleContext ctx, IReadOnlyList<Card> matched)
    {
        Debug.Log($"[LogEffect] {_message}");
        Debug.Log($"  触发源: {ctx.Source.Name}");
        Debug.Log($"  容器: {ctx.Container.Name}");
        Debug.Log($"  匹配数量: {matched.Count}");
    }
}

// 使用示例
public class CustomEffectExample : MonoBehaviour
{
    private void Start()
    {
        var factory = new CardFactory();
        var engine = new CardEngine(factory);
        
        factory.Register("测试卡", () => new Card(new CardData("测试卡", "测试卡", "", 
            CardCategory.Object, new[] { "测试" })));
        
        var world = new Card(new CardData("世界", "游戏世界"));
        engine.AddCard(world);
        
        var card = engine.CreateCard("测试卡");
        world.AddChild(card);
        
        // 使用自定义效果
        engine.RegisterRule(b => b
            .On(CardEventType.Use)
            .NeedTag("测试", 1)
            .Do(new LogEffect("规则被触发"))
        );
        
        card.Use();
        engine.Pump();
    }
}
```

---

## 作用域控制: 容器与选择详解

在 EmeCard 系统中，**作用域（Scope）** 决定了规则"在哪里"寻找符合条件的卡牌。`CardRuleBuilder` 通过三个维度来精确控制作用域：

### 维度1: 容器位置 — OwnerHops()

`OwnerHops()` 方法决定**以哪张卡作为搜索的容器**。

| 方法 | OwnerHops 值 | 容器 | 说明 |
|------|-------------|------|------|
| `AtSelf()` | 0 | 事件源本身 | 规则作用在事件源卡上 |
| `AtParent()` | 1 | 事件源的父卡 | 向上跳一层作为容器 |
| `AtRoot()` | -1 | 树的根节点 | 总是以树根作为容器 |
| `OwnerHops(N)` | N > 1 | 向上第N层 | 向上跳N层作为容器 |

**示例对比:**

```csharp
var factory = new CardFactory();
var engine = new CardEngine(factory);

// 搭建卡牌树
var world = engine.CreateCard("世界");
engine.AddCard(world);

var kingdom = engine.CreateCard("王国");
world.AddChild(kingdom);

var player = engine.CreateCard("玩家");
kingdom.AddChild(player);

var gold = engine.CreateCard("金币");
player.AddChild(gold);

// 从 gold 发出 Use 事件
// gold.Owner = player
// player.Owner = kingdom
// kingdom.Owner = world

// 规则A: AtSelf() - 容器是 gold 本身
engine.RegisterRule(b => b
    .On(CardEventType.Use)
    .AtSelf()
    .NeedId("金币", 1) // ✓ 找到: gold 本身
    .DoInvoke((ctx, matched) => Debug.Log($"AtSelf: 在 {ctx.Container.Name} 中找到"))
);

// 规则B: AtParent() - 容器是 player (gold 的父卡)
engine.RegisterRule(b => b
    .On(CardEventType.Use)
    .AtParent()
    .NeedId("玩家", 1) // ✓ 找到: player
    .DoInvoke((ctx, matched) => Debug.Log($"AtParent: 在 {ctx.Container.Name} 中找到"))
);

// 规则C: AtRoot() - 容器是 world (树根)
engine.RegisterRule(b => b
    .On(CardEventType.Use)
    .AtRoot()
    .NeedId("世界", 1) // ✓ 找到: world
    .DoInvoke((ctx, matched) => Debug.Log($"AtRoot: 在 {ctx.Container.Name} 中找到"))
);

// 规则D: OwnerHops(2) - 容器是 kingdom (向上跳2层)
engine.RegisterRule(b => b
    .On(CardEventType.Use)
    .OwnerHops(2)
    .NeedId("王国", 1) // ✓ 找到: kingdom
    .DoInvoke((ctx, matched) => Debug.Log($"OwnerHops(2): 在 {ctx.Container.Name} 中找到"))
);

gold.Use();
engine.Pump();
```

---

### 维度2: 选择范围 — SelectionRoot 与 TargetScope

确定容器后，需要指定**从容器内搜索什么**。这通过 `Need()` 方法的参数控制：

| SelectionRoot | TargetScope | 说明 |
|---------------|------------|------|
| `SelectionRoot.Container` | `TargetScope.Children` | 仅搜索容器的直接子卡 |
| `SelectionRoot.Container` | `TargetScope.Descendants` | 递归搜索容器的所有后代 |
| `SelectionRoot.Container` | `TargetScope.Matched` | 仅在已匹配的卡上操作 |
| `SelectionRoot.Source` | `TargetScope.Children` | 搜索事件源的直接子卡（忽略容器） |
| `SelectionRoot.Source` | `TargetScope.Descendants` | 递归搜索事件源的所有后代 |

**便捷方法对应关系:**

```csharp
// 容器的直接子卡
NeedTag(tag)           // 等价于 Need(SelectionRoot.Container, TargetScope.Children, CardFilterMode.ByTag, tag, ...)
NeedId(id)             // 等价于 Need(SelectionRoot.Container, TargetScope.Children, CardFilterMode.ById, id, ...)
NeedCategory(category) // 等价于 Need(SelectionRoot.Container, TargetScope.Children, CardFilterMode.ByCategory, ...)

// 容器的所有后代（递归）
NeedTagRecursive(tag, minCount, maxMatched, maxDepth)           // 检查 Descendants
NeedIdRecursive(id, minCount, maxMatched, maxDepth)             // 检查 Descendants
NeedCategoryRecursive(category, minCount, maxMatched, maxDepth) // 检查 Descendants

// 事件源的直接子卡（忽略OwnerHops）
NeedSourceTag(tag)     // SelectionRoot.Source, TargetScope.Children
NeedSourceId(id)       // SelectionRoot.Source, TargetScope.Children

// 事件源的所有后代（忽略OwnerHops）
NeedSourceTagRecursive(tag, minCount, maxMatched, maxDepth)     // SelectionRoot.Source, TargetScope.Descendants
```

**示例对比:**

```csharp
var factory = new CardFactory();
var engine = new CardEngine(factory);

var world = engine.CreateCard("世界");
engine.AddCard(world);

var kingdom = engine.CreateCard("王国");
world.AddChild(kingdom);

var gold1 = engine.CreateCard("金币");
kingdom.AddChild(gold1);

var gold2 = engine.CreateCard("金币");
kingdom.AddChild(gold2);

// 深层结构
var vault = engine.CreateCard("金库");
kingdom.AddChild(vault);

var gold3 = engine.CreateCard("金币");
vault.AddChild(gold3);

// 规则A: 搜索直接子卡
engine.RegisterRule(b => b
    .On(CardEventType.Use)
    .AtSelf()
    .NeedId("金币", minCount: 1)  // 仅 gold1, gold2 (直接子卡)
    .DoInvoke((ctx, matched) => 
        Debug.Log($"直接子卡: 找到 {matched.Count} 个金币"))
);

// 规则B: 递归搜索所有后代
engine.RegisterRule(b => b
    .On(CardEventType.Use)
    .AtSelf()
    .NeedIdRecursive("金币", minCount: 1)  // gold1, gold2, gold3 (所有后代)
    .DoInvoke((ctx, matched) => 
        Debug.Log($"所有后代: 找到 {matched.Count} 个金币"))
);

// 规则C: 限制递归深度
engine.RegisterRule(b => b
    .On(CardEventType.Use)
    .AtSelf()
    .NeedIdRecursive("金币", minCount: 1, maxDepth: 1)  // 仅 gold1, gold2 (深度1以内)
    .DoInvoke((ctx, matched) => 
        Debug.Log($"深度限制: 找到 {matched.Count} 个金币"))
);

kingdom.Use();
engine.Pump();
```

---

### 维度3: 前置条件 — When*** 方法

`When***` 方法在规则触发前进行**条件检查**，决定规则是否执行（独立于搜索结果）。

| 方法 | 检查条件 | 说明 |
|------|---------|------|
| `WhenSourceHasTag(tag)` | 事件源有标签 | `ctx.Source.HasTag(tag)` |
| `WhenSourceNotHasTag(tag)` | 事件源无标签 | `!(ctx.Source.HasTag(tag))` |
| `WhenSourceId(id)` | 事件源 ID 匹配 | `ctx.Source.Id == id` |
| `WhenSourceCategory(cat)` | 事件源类别匹配 | `ctx.Source.Category == cat` |
| `WhenContainerHasTag(tag)` | 容器有标签 | `ctx.Container.HasTag(tag)` |
| `WhenContainerNotHasTag(tag)` | 容器无标签 | `!(ctx.Container.HasTag(tag))` |
| `WhenEventDataIs<T>()` | 事件数据类型 | `ctx.Event.Data is T` |
| `WhenEventDataNotNull()` | 事件有数据 | `ctx.Event.Data != null` |
| `When(predicate)` | 自定义条件 | 自定义委托判断 |

**示例对比:**

```csharp
var factory = new CardFactory();
var engine = new CardEngine(factory);

var world = engine.CreateCard("世界");
engine.AddCard(world);

// 创建具有不同标签的卡牌
var warrior = engine.CreateCard("战士");
warrior.Data.Tags.Add("战士");
warrior.Data.Tags.Add("单位");
world.AddChild(warrior);

var mage = engine.CreateCard("法师");
mage.Data.Tags.Add("法师");
mage.Data.Tags.Add("单位");
world.AddChild(mage);

var spell = engine.CreateCard("法术");
spell.Data.Tags.Add("法术");
spell.Data.Tags.Add("技能");
world.AddChild(spell);

// 规则A: WhenSourceHasTag - 仅战士触发
engine.RegisterRule(b => b
    .On(CardEventType.Use)
    .WhenSourceHasTag("战士")     // <-- 前置条件
    .NeedTag("单位", minCount: 1)
    .DoInvoke((ctx, matched) => 
        Debug.Log($"战士规则触发"))
);

// 规则B: WhenSourceNotHasTag - 非法术触发
engine.RegisterRule(b => b
    .On(CardEventType.Use)
    .WhenSourceNotHasTag("法术")  // <-- 前置条件
    .NeedTag("技能", minCount: 1)
    .DoInvoke((ctx, matched) => 
        Debug.Log($"非法术规则触发"))
);

// 规则C: When 自定义条件
engine.RegisterRule(b => b
    .On(CardEventType.Use)
    .When(ctx => ctx.Source.Children.Count > 0)  // <-- 自定义条件
    .DoInvoke((ctx, matched) => 
        Debug.Log($"有子卡的卡牌使用"))
);

Debug.Log("=== 战士使用 ===");
warrior.Use();
engine.Pump(); // 规则A 执行 ✓，规则B 执行 ✓，规则C 执行 ✓

Debug.Log("=== 法术使用 ===");
spell.Use();
engine.Pump(); // 规则A 不执行 ✗，规则B 不执行 ✗，规则C 执行 ✓
```

---

### 典型应用模式

| 场景 | 容器 | 搜索范围 | 前置条件 | 示例代码 |
|------|------|---------|---------|---------|
| **自身效果** | `AtSelf()` | (无需) | `WhenSourceHasTag(...)` | `AtSelf().When(...).DoInvoke(...)` |
| **直接子卡** | `AtParent()` | `NeedTag()` | `WhenContainerHasTag(...)` | `AtParent().NeedTag().When(...)` |
| **递归搜索** | `AtRoot()` | `NeedIdRecursive(..., maxDepth: 2)` | — | `AtRoot().NeedIdRecursive(..., maxDepth: 2)` |
| **源卡的子树** | — | `NeedSourceTagRecursive()` | `WhenSourceHasTag(...)` | `NeedSourceTagRecursive().When(...)` |
| **有限递归** | `AtParent()` | `NeedIdRecursive(..., maxDepth: 1)` | — | `AtParent().NeedIdRecursive(..., maxDepth: 1)` |

**实战示例 - 施法者释放法术触发 Buff:**

```csharp
// 场景: 当有"法师"标签的卡牌使用"法术"标签的卡牌时，
//      会触发所有直接子卡中带"被动效果"标签的卡牌

engine.RegisterRule(b => b
    .On(CardEventType.Use)
    .WhenSourceHasTag("法术")              // 前置条件: 事件源是法术
    .AtParent()                             // 容器: 法术的持有者（施法者）
    .NeedTag("被动效果", minCount: 1)      // 搜索: 施法者直接子卡中的被动效果
    .DoInvoke((ctx, matched) =>
    {
        foreach (var buff in matched)
        {
            Debug.Log($"施法者 {ctx.Container.Name} 的 {buff.Name} 被触发");
        }
    })
);

// 使用示例
var mage = engine.CreateCard("法师");
mage.Data.Tags.Add("法师");
engine.AddCard(mage);

var flame = engine.CreateCard("火焰术");
flame.Data.Tags.Add("法术");
mage.AddChild(flame);

var haste = engine.CreateCard("急速");
haste.Data.Tags.Add("被动效果");
mage.AddChild(haste);

flame.Use();
engine.Pump(); // 输出: 施法者 法师 的 急速 被触发
```

---

## 故障排查

### 问题1: 规则不生效

**症状:** 触发事件后规则没有执行

**可能原因:**
1. 忘记调用 `engine.Pump()`
2. 条件要求不满足
3. 事件类型不匹配

**解决方法:**
```csharp
// 1. 确保调用 Pump
card.Use();
engine.Pump(); // 必须调用

// 2. 使用 PrintContext 调试
engine.RegisterRule(b => b
    .On(CardEventType.Use)
    .NeedTag("玩家", 1)
    .PrintContext() // 输出上下文信息
    .DoCreate("金币")
);

// 3. 检查事件类型
card.Use(); // 触发 CardEventType.Use
// 而不是 card.Tick() 或 card.Custom()
```

---

### 问题2: 卡牌没有被创建

**症状:** `DoCreate` 效果执行后找不到新卡牌

**可能原因:**
1. 卡牌ID未在工厂注册
2. 新卡牌被添加到了其他容器

**解决方法:**
```csharp
// 1. 确保注册模板
factory.Register("金币", () => new Card(new CardData("金币", "金币")));

// 2. 确认创建位置
engine.RegisterRule(b => b
    .On(CardEventType.Use)
    .AtParent() // 新卡牌会被添加到父级容器
    .DoCreate("金币")
);

// 3. 检查容器
Debug.Log($"容器子卡数量: {container.Children.Count}");
```

---

### 问题3: 递归选择找不到卡牌

**症状:** `NeedIdRecursive` 返回空结果

**可能原因:**
1. `MaxDepth` 设置过小
2. 卡牌不在预期的层级

**解决方法:**
```csharp
// 1. 增加递归深度
engine.RegisterRule(b => b
    .On(CardEventType.Use)
    .NeedIdRecursive("金币", minCount: 1, maxDepth: 10) // 增加深度
    .DoInvoke((ctx, matched) =>
    {
        Debug.Log($"找到 {matched.Count} 个金币");
    })
);

// 2. 使用无限深度
engine.RegisterRule(b => b
    .On(CardEventType.Use)
    .NeedIdRecursive("金币", minCount: 1, maxDepth: null) // 无限深度
    .DoInvoke((ctx, matched) => { })
);
```

---

### 问题4: 固有子卡无法移除

**症状:** `RemoveChild` 返回 `false`

**可能原因:** 子卡是固有子卡 (`intrinsic=true`)

**解决方法:**
```csharp
// 1. 检查是否固有
if (parent.IsIntrinsic(child))
{
    Debug.Log("这是固有子卡,需要 force=true 才能移除");
}

// 2. 强制移除
parent.RemoveChild(child, force: true);

// 3. 避免在规则中移除固有子卡
engine.RegisterRule(b => b
    .On(CardEventType.Use)
    .DoRemoveById("武器", take: 1) // 不会移除固有子卡
);
```

---

## 术语表

### 核心概念

| 术语 | 英文 | 说明 |
|------|------|------|
| **卡牌** | Card | 系统的基本单元,可持有子卡、属性、标签 |
| **卡牌数据** | CardData | 卡牌的静态配置(ID/名称/描述等) |
| **工厂** | CardFactory | 负责注册和创建卡牌实例 |
| **引擎** | CardEngine | 管理卡牌、规则和事件队列 |
| **规则** | CardRule | 定义事件触发条件和效果 |
| **上下文** | CardRuleContext | 规则执行时的环境信息 |

### 事件类型

| 术语 | 英文 | 说明 |
|------|------|------|
| **添加到持有者** | AddedToOwner | 卡牌成为子卡时触发 |
| **从持有者移除** | RemovedFromOwner | 卡牌从持有者移除时触发 |
| **按时事件** | Tick | 时间驱动的事件 |
| **使用事件** | Use | 主动使用卡牌时触发 |
| **自定义事件** | Custom | 用户自定义的事件类型 |

### 规则组件

| 术语 | 英文 | 说明 |
|------|------|------|
| **要求项** | Requirement | 规则匹配的前置条件 |
| **效果** | Effect | 规则生效时执行的操作 |
| **触发器** | Trigger | 事件类型,决定何时检查规则 |
| **优先级** | Priority | 规则执行顺序(数值越小越优先) |

### 选择系统

| 术语 | 英文 | 说明 |
|------|------|------|
| **选择根** | SelectionRoot | 选择起点(容器/源卡) |
| **选择范围** | TargetScope | 选择范围(子卡/后代/匹配结果) |
| **过滤模式** | CardFilterMode | 过滤方式(按ID/标签/类别) |
| **递归深度** | MaxDepth | 向下搜索的最大层数 |

### 卡牌结构

| 术语 | 英文 | 说明 |
|------|------|------|
| **持有者** | Owner | 当前卡牌的父卡牌 |
| **子卡** | Children | 当前卡牌持有的子卡牌列表 |
| **固有子卡** | Intrinsic | 不可被规则移除的特殊子卡 |
| **属性** | Property | 数值属性(集成GamePropertySystem) |
| **标签** | Tag | 用于过滤和匹配的字符串标记 |

### 策略配置

| 术语 | 英文 | 说明 |
|------|------|------|
| **去重匹配** | DistinctMatched | 是否对匹配结果去重 |
| **中止传播** | StopEventOnSuccess | 规则成功后是否停止处理其他规则 |
| **规则选择模式** | RuleSelectionMode | 按注册顺序或优先级执行 |

---

**相关文档:**

- [API 参考文档](./APIReference.md)
- [Mermaid 图集](./Diagrams.md)