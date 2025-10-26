# Buff System - User Guide

**适用EasyPack版本：** EasyPack v1.5.30
**最后更新：** 2025-10-26

---

## 概述

**Buff System** 是一个灵活的增益/减益效果管理系统，支持时间管理、堆叠控制、模块化效果和事件回调机制。可用于实现 RPG 游戏中的各种临时状态效果、持续伤害/治疗、属性加成等功能。

### 核心特性

- ✅ **生命周期管理**：自动处理 Buff 的创建、更新、过期和移除
- ✅ **堆叠系统**：支持多种堆叠策略（叠加、重置、保持）
- ✅ **模块化设计**：通过 BuffModule 实现可复用的 Buff 效果
- ✅ **标签和层级**：使用标签和层级批量管理和查询 Buff
- ✅ **事件系统**：提供完整的生命周期事件回调
- ✅ **性能优化**：批量移除、索引查询、swap-remove 优化

### 适用场景

- RPG 角色状态系统（增益、减益、光环）
- 持续伤害/治疗效果（DoT/HoT）
- 临时属性加成（装备、技能、药水）
- 条件触发效果（低血量触发、连击触发）
- 战斗状态管理（眩晕、沉默、无敌）

---

## 目录

- [概述](#概述)
- [快速开始](#快速开始)
- [常见场景](#常见场景)
- [进阶用法](#进阶用法)
- [故障排查](#故障排查)
- [术语表](#术语表)
- [相关资源](#相关资源)

---

## 快速开始

### 前置条件

- EasyPack.GamePropertySystem（如需属性修改功能）

### 安装步骤

1. 将 `EasyPack/03_CoreSystems/Buff` 文件夹复制到项目的 `Assets` 目录
2. 如需属性修改功能，确保已安装 `GamePropertySystem`
3. 确保项目已引用 Unity 核心库

### 第一示例

创建一个简单的增益 Buff 并应用到角色：

```csharp
using EasyPack.BuffSystem;
using UnityEngine;

public class QuickStartExample : MonoBehaviour
{
    private BuffManager buffManager;
    private GameObject player;
    private GameObject buffCreator;

    void Start()
    {
        // 1. 初始化 Buff 管理器
        buffManager = new BuffManager();
        
        // 2. 创建目标对象和施法者
        player = new GameObject("Player");
        buffCreator = gameObject;

        // 3. 创建 Buff 配置数据
        var speedBoost = new BuffData
        {
            ID = "SpeedBoost",
            Name = "速度提升",
            Description = "移动速度提升 30%",
            Duration = 10f,  // 持续 10 秒
            MaxStacks = 1
        };

        // 4. 应用 Buff 到目标
        Buff buff = buffManager.CreateBuff(speedBoost, buffCreator, player);
        Debug.Log($"Buff 创建成功: {buff.BuffData.Name}");
        Debug.Log($"持续时间: {buff.DurationTimer} 秒");

        // 5. 检查 Buff 是否存在
        bool hasSpeedBoost = buffManager.ContainsBuff(player, "SpeedBoost");
        Debug.Log($"玩家是否有速度提升: {hasSpeedBoost}");
    }

    void Update()
    {
        // 6. 每帧更新 Buff 管理器（处理时间和触发）
        buffManager.Update(Time.deltaTime);
    }
}
```

**运行结果：** Buff 会在 10 秒后自动过期并移除。

---

## 常见场景

### 场景 1：创建持续伤害效果（DoT）

实现中毒、灼烧等持续伤害效果：

```csharp
using EasyPack.BuffSystem;
using UnityEngine;

public class DotBuffExample : MonoBehaviour
{
    private BuffManager buffManager;
    private GameObject target;

    void Start()
    {
        buffManager = new BuffManager();
        target = new GameObject("Enemy");

        // 创建中毒 Buff
        var poisonBuff = new BuffData
        {
            ID = "Poison",
            Name = "中毒",
            Description = "每秒受到 5 点毒素伤害",
            Duration = 10f,         // 持续 10 秒
            TriggerInterval = 1f,   // 每 1 秒触发一次
            MaxStacks = 3,          // 最多堆叠 3 层
            BuffSuperpositionStacksStrategy = BuffSuperpositionStacksType.Add
        };

        // 创建伤害模块
        var damageModule = new PoisonDamageModule(5f);
        poisonBuff.BuffModules.Add(damageModule);

        // 应用中毒效果
        Buff buff = buffManager.CreateBuff(poisonBuff, gameObject, target);
        
        // 设置触发事件
        buff.OnTrigger += (b) =>
        {
            float totalDamage = 5f * b.CurrentStacks;
            Debug.Log($"中毒效果触发！造成 {totalDamage} 点伤害（{b.CurrentStacks} 层）");
        };

        Debug.Log($"中毒效果已应用，持续时间: {buff.DurationTimer} 秒");
    }

    void Update()
    {
        buffManager.Update(Time.deltaTime);
    }
}

// 自定义伤害模块
public class PoisonDamageModule : BuffModule
{
    private float damagePerTick;

    public PoisonDamageModule(float damage)
    {
        damagePerTick = damage;
        RegisterCallback(BuffCallBackType.OnTick, OnTick);
    }

    private void OnTick(Buff buff, object[] parameters)
    {
        float damage = damagePerTick * buff.CurrentStacks;
        Debug.Log($"造成 {damage} 点毒素伤害");
        // 这里实现实际的伤害逻辑
    }
}
```

**要点：** `TriggerInterval` 控制触发频率，`OnTrigger` 事件在每次触发时执行。

---

### 场景 2：使用 Buff 修改角色属性

通过 Buff 临时增加角色的力量、敏捷等属性：

```csharp
using EasyPack.BuffSystem;
using EasyPack.GamePropertySystem;
using EasyPack;
using UnityEngine;

public class AttributeBuffExample : MonoBehaviour
{
    private BuffManager buffManager;
    private GamePropertyManager propertyManager;
    private GameObject player;

    void Start()
    {
        buffManager = new BuffManager();
        propertyManager = new GamePropertyManager();
        player = new GameObject("Player");

        // 初始化角色属性
        var strength = new CombinePropertySingle("Strength", 10f);
        var agility = new CombinePropertySingle("Agility", 8f);
        propertyManager.AddOrUpdate(strength);
        propertyManager.AddOrUpdate(agility);

        Debug.Log($"初始力量: {strength.GetValue()}");
        Debug.Log($"初始敏捷: {agility.GetValue()}");

        // 创建力量增益 Buff
        var strengthBuff = new BuffData
        {
            ID = "StrengthBuff",
            Name = "力量增益",
            Duration = 15f,
            MaxStacks = 5,
            BuffSuperpositionStacksStrategy = BuffSuperpositionStacksType.Add
        };

        // 添加属性修改模块（每层 +5 力量）
        var strengthModifier = new FloatModifier(ModifierType.Add, 0, 5f);
        var strengthModule = new CastModifierToProperty(
            strengthModifier, 
            "Strength", 
            propertyManager
        );
        strengthBuff.BuffModules.Add(strengthModule);

        // 创建敏捷增益 Buff（百分比加成）
        var agilityBuff = new BuffData
        {
            ID = "AgilityBuff",
            Name = "敏捷增益",
            Duration = 12f
        };

        // 添加百分比修改模块（+50% 敏捷）
        var agilityModifier = new FloatModifier(ModifierType.Mul, 0, 1.5f);
        var agilityModule = new CastModifierToProperty(
            agilityModifier, 
            "Agility", 
            propertyManager
        );
        agilityBuff.BuffModules.Add(agilityModule);

        // 应用 Buff
        buffManager.CreateBuff(strengthBuff, gameObject, player);
        buffManager.CreateBuff(strengthBuff, gameObject, player); // 堆叠第 2 层
        buffManager.CreateBuff(strengthBuff, gameObject, player); // 堆叠第 3 层
        buffManager.CreateBuff(agilityBuff, gameObject, player);

        Debug.Log($"Buff 后力量: {strength.GetValue()}"); // 输出：25 (10 + 5*3)
        Debug.Log($"Buff 后敏捷: {agility.GetValue()}");  // 输出：12 (8 * 1.5)
    }

    void Update()
    {
        buffManager.Update(Time.deltaTime);
    }
}
```

**要点：** `CastModifierToProperty` 模块会在 Buff 创建/堆叠时添加修饰符，移除时自动清理。

---

### 场景 3：使用标签和层级管理 Buff

通过标签和层级批量操作相关的 Buff：

```csharp
using EasyPack.BuffSystem;
using UnityEngine;
using System.Collections.Generic;

public class TagLayerExample : MonoBehaviour
{
    private BuffManager buffManager;
    private GameObject player;

    void Start()
    {
        buffManager = new BuffManager();
        player = new GameObject("Player");

        // 创建不同类型的 Buff
        var blessingBuff = new BuffData
        {
            ID = "Blessing",
            Name = "祝福",
            Duration = 30f,
            Tags = new List<string> { "Positive", "Magic", "Removable" },
            Layers = new List<string> { "Enhancement", "Temporary" }
        };

        var curseBuff = new BuffData
        {
            ID = "Curse",
            Name = "诅咒",
            Duration = 20f,
            Tags = new List<string> { "Negative", "Magic", "Removable" },
            Layers = new List<string> { "Debuff", "Temporary" }
        };

        var passiveAura = new BuffData
        {
            ID = "PassiveAura",
            Name = "被动光环",
            Duration = -1f,  // 永久
            Tags = new List<string> { "Positive", "Passive" },
            Layers = new List<string> { "Enhancement", "Permanent" }
        };

        var stunBuff = new BuffData
        {
            ID = "Stun",
            Name = "眩晕",
            Duration = 3f,
            Tags = new List<string> { "Negative", "Control" },
            Layers = new List<string> { "Debuff", "CC" }
        };

        // 应用所有 Buff
        buffManager.CreateBuff(blessingBuff, gameObject, player);
        buffManager.CreateBuff(curseBuff, gameObject, player);
        buffManager.CreateBuff(passiveAura, gameObject, player);
        buffManager.CreateBuff(stunBuff, gameObject, player);

        Debug.Log($"玩家身上的 Buff 总数: {buffManager.GetTargetBuffs(player).Count}");

        // 按标签查询
        var magicBuffs = buffManager.GetBuffsByTag(player, "Magic");
        var negativeBuffs = buffManager.GetBuffsByTag(player, "Negative");
        Debug.Log($"魔法类 Buff 数量: {magicBuffs.Count}");
        Debug.Log($"负面 Buff 数量: {negativeBuffs.Count}");

        // 按层级查询
        var debuffs = buffManager.GetBuffsByLayer(player, "Debuff");
        var enhancements = buffManager.GetBuffsByLayer(player, "Enhancement");
        Debug.Log($"减益效果数量: {debuffs.Count}");
        Debug.Log($"增益效果数量: {enhancements.Count}");

        // 批量移除：使用净化技能移除所有可移除的魔法效果
        Debug.Log("\n使用净化技能...");
        buffManager.RemoveBuffsByTag(player, "Removable");
        Debug.Log($"净化后剩余 Buff 数量: {buffManager.GetTargetBuffs(player).Count}");

        // 批量移除：解除所有控制效果
        Debug.Log("\n解除控制效果...");
        buffManager.RemoveBuffsByLayer(player, "CC");
        Debug.Log($"解除控制后剩余 Buff 数量: {buffManager.GetTargetBuffs(player).Count}");
    }

    void Update()
    {
        buffManager.Update(Time.deltaTime);
    }
}
```

**要点：** 合理使用标签和层级可以轻松实现净化、驱散等功能。

---

### 场景 4：Buff 堆叠策略

理解和使用不同的堆叠策略：

```csharp
using EasyPack.BuffSystem;
using UnityEngine;

public class StackingStrategyExample : MonoBehaviour
{
    private BuffManager buffManager;
    private GameObject target;

    void Start()
    {
        buffManager = new BuffManager();
        target = new GameObject("Target");

        // 策略 1: Add - 持续时间叠加
        TestDurationStrategy("Add 策略", BuffSuperpositionDurationType.Add);

        // 策略 2: Reset - 持续时间重置
        TestDurationStrategy("Reset 策略", BuffSuperpositionDurationType.Reset);

        // 策略 3: ResetThenAdd - 先重置再叠加
        TestDurationStrategy("ResetThenAdd 策略", BuffSuperpositionDurationType.ResetThenAdd);

        // 策略 4: Keep - 保持原有时间
        TestDurationStrategy("Keep 策略", BuffSuperpositionDurationType.Keep);
    }

    void TestDurationStrategy(string name, BuffSuperpositionDurationType strategy)
    {
        Debug.Log($"\n=== {name} ===");

        var buffData = new BuffData
        {
            ID = $"TestBuff_{strategy}",
            Name = $"测试 Buff ({strategy})",
            Duration = 5f,
            BuffSuperpositionStrategy = strategy
        };

        // 第一次应用
        var buff = buffManager.CreateBuff(buffData, gameObject, target);
        Debug.Log($"初始持续时间: {buff.DurationTimer} 秒");

        // 模拟过去 2 秒
        buffManager.Update(2f);
        Debug.Log($"2 秒后持续时间: {buff.DurationTimer} 秒");

        // 再次应用相同 Buff
        buffManager.CreateBuff(buffData, gameObject, target);
        Debug.Log($"再次应用后持续时间: {buff.DurationTimer} 秒");

        // 清理
        buffManager.RemoveAllBuffs(target);
    }

    void Update()
    {
        buffManager.Update(Time.deltaTime);
    }
}
```

**输出示例：**
- **Add 策略**：2 秒后剩余 3 秒，再次应用后为 8 秒（3 + 5）
- **Reset 策略**：2 秒后剩余 3 秒，再次应用后重置为 5 秒
- **ResetThenAdd 策略**：再次应用后为 10 秒（5 + 5）
- **Keep 策略**：再次应用后保持 3 秒不变

---

### 场景 5：生命周期事件监听

监听 Buff 的各种生命周期事件：

```csharp
using EasyPack.BuffSystem;
using UnityEngine;

public class LifecycleEventExample : MonoBehaviour
{
    private BuffManager buffManager;
    private GameObject player;

    void Start()
    {
        buffManager = new BuffManager();
        player = new GameObject("Player");

        // 创建 Buff
        var rageBuff = new BuffData
        {
            ID = "Rage",
            Name = "狂暴",
            Duration = 10f,
            TriggerInterval = 2f,
            MaxStacks = 5,
            BuffSuperpositionStacksStrategy = BuffSuperpositionStacksType.Add,
            TriggerOnCreate = true  // 创建时立即触发一次
        };

        var buff = buffManager.CreateBuff(rageBuff, gameObject, player);

        // 设置所有生命周期事件
        buff.OnCreate += (b) =>
        {
            Debug.Log($"[事件] {b.BuffData.Name} 被创建");
        };

        buff.OnTrigger += (b) =>
        {
            Debug.Log($"[事件] {b.BuffData.Name} 触发效果（堆叠: {b.CurrentStacks}）");
        };

        buff.OnUpdate += (b) =>
        {
            // 每帧更新都会触发，建议谨慎使用
            if (Mathf.Approximately(b.DurationTimer % 1f, 0f))
            {
                Debug.Log($"[事件] {b.BuffData.Name} 更新（剩余: {b.DurationTimer:F1}s）");
            }
        };

        buff.OnAddStack += (b) =>
        {
            Debug.Log($"[事件] {b.BuffData.Name} 增加堆叠（当前: {b.CurrentStacks}/{b.BuffData.MaxStacks}）");
            
            if (b.CurrentStacks == b.BuffData.MaxStacks)
            {
                Debug.Log("狂暴已达到最大层数！触发特殊效果！");
            }
        };

        buff.OnReduceStack += (b) =>
        {
            Debug.Log($"[事件] {b.BuffData.Name} 减少堆叠（当前: {b.CurrentStacks}）");
        };

        buff.OnRemove += (b) =>
        {
            Debug.Log($"[事件] {b.BuffData.Name} 被移除");
        };

        // 测试堆叠事件
        buffManager.CreateBuff(rageBuff, gameObject, player); // 触发 OnAddStack
        buffManager.CreateBuff(rageBuff, gameObject, player); // 触发 OnAddStack

        Debug.Log("\n开始模拟时间流逝...");
    }

    void Update()
    {
        buffManager.Update(Time.deltaTime);
    }
}
```

**要点：** 
- `OnCreate` 在 Buff 首次创建时触发
- `OnTrigger` 按 `TriggerInterval` 周期触发
- `OnUpdate` 每帧触发，谨慎使用
- `OnAddStack/OnReduceStack` 在堆叠变化时触发
- `OnRemove` 在 Buff 完全移除时触发

---

## 进阶用法

### 自定义 Buff 模块

创建复杂的自定义 Buff 效果：

```csharp
using EasyPack.BuffSystem;
using UnityEngine;

/// <summary>
/// 治疗光环模块 - 每次触发时治疗目标
/// </summary>
public class HealingAuraModule : BuffModule
{
    private float healingAmount;
    private float healingMultiplier;

    public HealingAuraModule(float baseHealing, float multiplier = 1f)
    {
        healingAmount = baseHealing;
        healingMultiplier = multiplier;

        // 注册生命周期回调
        RegisterCallback(BuffCallBackType.OnCreate, OnCreate);
        RegisterCallback(BuffCallBackType.OnTick, OnTick);
        RegisterCallback(BuffCallBackType.OnAddStack, OnStackChanged);
        RegisterCallback(BuffCallBackType.OnRemove, OnRemove);
        
        // 设置优先级（数字越大越先执行）
        Priority = 10;
    }

    private void OnCreate(Buff buff, object[] parameters)
    {
        Debug.Log($"[{buff.BuffData.Name}] 治疗光环激活！");
    }

    private void OnTick(Buff buff, object[] parameters)
    {
        // 根据堆叠数计算治疗量
        float totalHealing = healingAmount * buff.CurrentStacks * healingMultiplier;
        Debug.Log($"[{buff.BuffData.Name}] 治疗 {totalHealing} 点生命值");
        
        // 这里实现实际的治疗逻辑
        // 例如：buff.Target.GetComponent<HealthComponent>().Heal(totalHealing);
    }

    private void OnStackChanged(Buff buff, object[] parameters)
    {
        Debug.Log($"[{buff.BuffData.Name}] 治疗效果增强！当前治疗倍率: {buff.CurrentStacks}x");
    }

    private void OnRemove(Buff buff, object[] parameters)
    {
        Debug.Log($"[{buff.BuffData.Name}] 治疗光环消失");
    }
}

/// <summary>
/// 条件触发模块 - 满足特定条件时触发效果
/// </summary>
public class ConditionalBuffModule : BuffModule
{
    private float healthThreshold;

    public ConditionalBuffModule(float threshold)
    {
        healthThreshold = threshold;

        // 设置触发条件
        TriggerCondition = (buff) =>
        {
            // 这里检查条件，例如检查目标血量
            // var health = buff.Target.GetComponent<HealthComponent>();
            // return health != null && health.GetHealthPercentage() < healthThreshold;
            return true; // 示例：总是返回 true
        };

        RegisterCallback(BuffCallBackType.OnUpdate, OnUpdate);
        RegisterCallback("EmergencyHeal", OnEmergencyHeal);
    }

    private void OnUpdate(Buff buff, object[] parameters)
    {
        // 仅在条件满足时执行（通过 TriggerCondition）
        Debug.Log($"[{buff.BuffData.Name}] 条件检查通过，持续监控中...");
    }

    private void OnEmergencyHeal(Buff buff, object[] parameters)
    {
        Debug.Log($"[{buff.BuffData.Name}] 紧急治疗触发！");
    }

    // 手动触发自定义事件
    public void TriggerEmergencyHeal(Buff buff)
    {
        Execute(buff, BuffCallBackType.Custom, "EmergencyHeal");
    }
}

// 使用示例
public class CustomModuleExample : MonoBehaviour
{
    void Start()
    {
        var buffManager = new BuffManager();
        var target = new GameObject("Target");

        // 创建使用自定义模块的 Buff
        var healingAura = new BuffData
        {
            ID = "HealingAura",
            Name = "治疗光环",
            Duration = 30f,
            TriggerInterval = 3f,
            MaxStacks = 3,
            BuffSuperpositionStacksStrategy = BuffSuperpositionStacksType.Add
        };

        healingAura.BuffModules.Add(new HealingAuraModule(10f, 1.2f));

        var buff = buffManager.CreateBuff(healingAura, gameObject, target);
        
        // 在 Update 中更新管理器
        // buffManager.Update(Time.deltaTime);
    }
}
```

**要点：**
- 通过 `RegisterCallback` 注册不同生命周期的处理方法
- 使用 `TriggerCondition` 设置条件触发逻辑
- `Priority` 控制多个模块的执行顺序
- 支持自定义事件名称和参数传递

---

### 组合多个 Buff 模块

一个 Buff 可以包含多个模块，实现复合效果：

```csharp
using EasyPack.BuffSystem;
using EasyPack.GamePropertySystem;
using EasyPack;
using UnityEngine;
using System.Collections.Generic;

public class CompositeBuffExample : MonoBehaviour
{
    private BuffManager buffManager;
    private GamePropertyManager propertyManager;
    private GameObject player;

    void Start()
    {
        buffManager = new BuffManager();
        propertyManager = new GamePropertyManager();
        player = new GameObject("Player");

        // 初始化属性
        var strength = new CombinePropertySingle("Strength", 20f);
        var health = new CombinePropertySingle("Health", 100f);
        propertyManager.AddOrUpdate(strength);
        propertyManager.AddOrUpdate(health);

        // 创建"战斗狂热" Buff - 包含多种效果
        var battleFrenzy = new BuffData
        {
            ID = "BattleFrenzy",
            Name = "战斗狂热",
            Duration = 15f,
            TriggerInterval = 2f,
            MaxStacks = 3,
            BuffSuperpositionStacksStrategy = BuffSuperpositionStacksType.Add
        };

        // 模块 1: 增加力量
        var strengthBoost = new CastModifierToProperty(
            new FloatModifier(ModifierType.Add, 0, 5f),
            "Strength",
            propertyManager
        );

        // 模块 2: 增加生命值上限
        var healthBoost = new CastModifierToProperty(
            new FloatModifier(ModifierType.Mul, 0, 1.2f),
            "Health",
            propertyManager
        );

        // 模块 3: 持续回复生命
        var regenModule = new HealingAuraModule(3f);

        // 模块 4: 视觉效果（自定义）
        var visualModule = new VisualEffectModule("BattleFrenzyEffect");

        // 按优先级添加模块
        strengthBoost.Priority = 100;  // 优先应用属性修改
        healthBoost.Priority = 90;
        regenModule.Priority = 50;
        visualModule.Priority = 10;    // 最后处理视觉效果

        battleFrenzy.BuffModules.AddRange(new BuffModule[] 
        { 
            strengthBoost, 
            healthBoost, 
            regenModule, 
            visualModule 
        });

        // 应用 Buff
        var buff = buffManager.CreateBuff(battleFrenzy, gameObject, player);
        
        Debug.Log($"战斗狂热效果已激活！");
        Debug.Log($"力量: {strength.GetValue()}");
        Debug.Log($"生命值上限: {health.GetValue()}");
    }

    void Update()
    {
        buffManager.Update(Time.deltaTime);
    }
}

// 视觉效果模块示例
public class VisualEffectModule : BuffModule
{
    private string effectName;
    private GameObject effectInstance;

    public VisualEffectModule(string name)
    {
        effectName = name;
        RegisterCallback(BuffCallBackType.OnCreate, OnCreate);
        RegisterCallback(BuffCallBackType.OnRemove, OnRemove);
    }

    private void OnCreate(Buff buff, object[] parameters)
    {
        Debug.Log($"[{buff.BuffData.Name}] 播放视觉效果: {effectName}");
        // effectInstance = Instantiate(effectPrefab, buff.Target.transform);
    }

    private void OnRemove(Buff buff, object[] parameters)
    {
        Debug.Log($"[{buff.BuffData.Name}] 移除视觉效果");
        // if (effectInstance != null) Destroy(effectInstance);
    }
}
```

**要点：** 模块按 `Priority` 从高到低执行，合理设置优先级可以确保正确的执行顺序。

---

### 批量管理和性能优化

大量 Buff 场景下的最佳实践：

```csharp
using EasyPack.BuffSystem;
using UnityEngine;
using System.Collections.Generic;

public class PerformanceExample : MonoBehaviour
{
    private BuffManager buffManager;

    void Start()
    {
        buffManager = new BuffManager();

        // 场景 1: 批量清理相同类型的 Buff
        BatchRemovalExample();

        // 场景 2: 使用索引快速查询
        IndexedQueryExample();

        // 场景 3: 避免频繁创建销毁
        BuffReuseExample();
    }

    void BatchRemovalExample()
    {
        Debug.Log("=== 批量移除示例 ===");

        var targets = new List<GameObject>();
        for (int i = 0; i < 10; i++)
        {
            targets.Add(new GameObject($"Target_{i}"));
        }

        // 给所有目标添加临时 Buff
        foreach (var target in targets)
        {
            var tempBuff = new BuffData
            {
                ID = "TempEffect",
                Tags = new List<string> { "Temporary", "AOE" }
            };
            buffManager.CreateBuff(tempBuff, gameObject, target);
        }

        Debug.Log($"创建了 {targets.Count} 个目标的 Buff");

        // 好的做法：使用全局批量移除
        buffManager.RemoveAllBuffsByTag("Temporary");
        buffManager.FlushPendingRemovals();  // 立即处理批量移除

        Debug.Log("批量移除完成");

        // 清理测试对象
        foreach (var target in targets)
        {
            Destroy(target);
        }
    }

    void IndexedQueryExample()
    {
        Debug.Log("\n=== 索引查询示例 ===");

        var player = new GameObject("Player");

        // 创建带有标签的 Buff
        for (int i = 0; i < 20; i++)
        {
            var buffData = new BuffData
            {
                ID = $"Buff_{i}",
                Tags = new List<string> { i % 2 == 0 ? "Even" : "Odd", "Test" }
            };
            buffManager.CreateBuff(buffData, gameObject, player);
        }

        // 使用索引快速查询（O(1) 时间复杂度）
        var evenBuffs = buffManager.GetBuffsByTag(player, "Even");
        var oddBuffs = buffManager.GetBuffsByTag(player, "Odd");

        Debug.Log($"偶数 Buff: {evenBuffs.Count}");
        Debug.Log($"奇数 Buff: {oddBuffs.Count}");

        // 全局查询
        bool hasTestBuffs = buffManager.ContainsBuffWithTag("Test");
        Debug.Log($"全局是否有测试 Buff: {hasTestBuffs}");

        buffManager.RemoveAllBuffs(player);
        Destroy(player);
    }

    void BuffReuseExample()
    {
        Debug.Log("\n=== Buff 复用示例 ===");

        // 避免频繁创建销毁 BuffData
        // 好的做法：复用 BuffData 配置
        var sharedBuffData = new BuffData
        {
            ID = "SharedBuff",
            Name = "共享 Buff",
            Duration = 5f
        };

        var targets = new List<GameObject>();
        for (int i = 0; i < 5; i++)
        {
            var target = new GameObject($"Target_{i}");
            targets.Add(target);
            
            // 复用相同的 BuffData
            buffManager.CreateBuff(sharedBuffData, gameObject, target);
        }

        Debug.Log($"使用同一个 BuffData 创建了 {targets.Count} 个 Buff 实例");

        // 清理
        foreach (var target in targets)
        {
            buffManager.RemoveAllBuffs(target);
            Destroy(target);
        }
    }

    void Update()
    {
        buffManager.Update(Time.deltaTime);
    }
}
```

**性能建议：**
- ✅ 使用标签和层级批量操作，避免遍历
- ✅ 复用 `BuffData` 配置，减少内存分配
- ✅ 调用 `FlushPendingRemovals()` 立即处理批量移除
- ✅ 合理设置 `TriggerInterval`，避免过于频繁的触发（建议 ≥ 0.5 秒）
- ❌ 避免在 `OnUpdate` 中执行耗时操作

---

## 故障排查

### 常见问题

#### 问题 1：编译错误 - 找不到类型 `BuffManager`

**症状：** 提示 `The type or namespace name 'BuffManager' could not be found`  
**原因：** 缺少命名空间引用  
**解决方法：** 在文件头部添加命名空间：

```csharp
using EasyPack.BuffSystem;
using EasyPack.GamePropertySystem; // 如果使用属性修改功能
using EasyPack; // 如果使用修饰符
```

---

#### 问题 2：Buff 不会自动过期

**症状：** 设置了 `Duration` 但 Buff 永不消失  
**原因：** 未在 `Update` 中调用 `BuffManager.Update()`  
**解决方法：** 确保每帧更新管理器：

```csharp
void Update()
{
    buffManager.Update(Time.deltaTime);
}
```

---

#### 问题 3：Buff 触发事件不执行

**症状：** `OnTrigger` 事件未被调用  
**原因 1：** 未设置 `TriggerInterval`  
**原因 2：** 未调用 `BuffManager.Update()`  
**解决方法：**

```csharp
// 确保设置了触发间隔
var buffData = new BuffData
{
    ID = "TestBuff",
    TriggerInterval = 1f  // 每秒触发一次
};

// 确保每帧更新
void Update()
{
    buffManager.Update(Time.deltaTime);
}
```

---

#### 问题 4：堆叠数没有增加

**症状：** 多次应用同一个 Buff 但堆叠数始终为 1  
**原因：** 堆叠策略设置为 `Keep` 或 `Reset`  
**解决方法：** 使用正确的堆叠策略：

```csharp
var buffData = new BuffData
{
    ID = "StackableBuff",
    MaxStacks = 5,
    BuffSuperpositionStacksStrategy = BuffSuperpositionStacksType.Add  // 确保使用 Add
};
```

---

#### 问题 5：属性修改不生效

**症状：** 使用 `CastModifierToProperty` 但属性值没有变化  
**原因 1：** `GamePropertyManager` 未正确初始化  
**原因 2：** 属性 ID 不匹配  
**解决方法：**

```csharp
// 确保属性已添加到管理器
var propertyManager = new GamePropertyManager();
var strength = new CombinePropertySingle("Strength", 10f);
propertyManager.AddOrUpdate(strength);

// 确保 ID 匹配
var module = new CastModifierToProperty(
    modifier,
    "Strength",  // 必须与属性 ID 完全匹配
    propertyManager
);
```

---

#### 问题 6：Buff 移除后属性没有恢复

**症状：** 移除 Buff 后属性修饰符仍然存在  
**原因：** `CastModifierToProperty` 的 `OnRemove` 回调未正确执行  
**解决方法：** 确保使用正确的移除方式：

```csharp
// 正确：使用管理器移除
buffManager.RemoveBuff(buff);

// 正确：按 ID 移除
buffManager.RemoveBuffByID(target, "BuffID");

// 错误：直接从列表移除不会触发回调
// buffs.Remove(buff);  ❌ 不要这样做
```

---

### FAQ 更新记录

*本节持续更新，记录用户反馈的新问题。*

#### 问题 X：（待补充）

*如遇到未列出的问题，请提交 GitHub Issue 或联系维护者。*

---

## 术语表

### Buff（增益/减益效果）
临时应用到游戏对象的状态效果，可以是正面（增益）或负面（减益）。包含持续时间、堆叠数、触发逻辑等属性。

### BuffData（Buff 配置数据）
定义 Buff 的静态属性和行为的配置对象，包括 ID、名称、持续时间、堆叠策略、模块列表等。可以被多个 Buff 实例复用。

### BuffModule（Buff 模块）
实现 Buff 具体行为的可复用组件，通过注册生命周期回调函数来响应 Buff 事件。一个 Buff 可以包含多个模块。

### BuffManager（Buff 管理器）
管理所有 Buff 实例的生命周期，负责创建、更新、移除和查询 Buff。提供批量操作和索引查询功能。

### 堆叠（Stack）
同一个 Buff 多次应用时的累积效果。`CurrentStacks` 表示当前堆叠层数，`MaxStacks` 表示最大堆叠层数。

### 叠加策略（Superposition Strategy）
定义多次应用同一个 Buff 时如何处理持续时间和堆叠数。包括 Add（叠加）、Reset（重置）、Keep（保持）、ResetThenAdd（先重置再叠加）。

### 触发间隔（Trigger Interval）
Buff 周期性触发效果的时间间隔（秒）。`OnTrigger` 事件按此间隔调用。

### 标签（Tag）
用于分类和批量操作 Buff 的字符串标识，如 "Positive"、"Negative"、"Magic" 等。一个 Buff 可以有多个标签。

### 层级（Layer）
用于更高层次分类 Buff 的标识，如 "Enhancement"、"Debuff"、"Control" 等。用于实现优先级和互斥逻辑。

### DoT/HoT
- **DoT (Damage over Time)**：持续伤害效果，如中毒、灼烧
- **HoT (Healing over Time)**：持续治疗效果，如恢复光环

---

## 相关资源

- [API 参考文档](./APIReference.md) - 详细的方法签名和参数说明
- [Mermaid 图集](./Diagrams.md) - 系统架构和数据流可视化
- [示例代码](../Example/BuffExample.cs) - 完整的使用示例

---

**维护者：** EasyPack 团队  
**联系方式：** 提交 GitHub Issue 或 Pull Request  
**许可证：** 遵循项目主许可证
