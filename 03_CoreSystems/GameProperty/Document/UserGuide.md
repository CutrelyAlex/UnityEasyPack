# GameProperty 系统用户使用指南

**适用 EasyPack 版本：** EasyPack v1.6.0  
**最后更新：** 2025-11-04

---

## 概述

### 系统简介

GameProperty 系统是一个强大而灵活的游戏属性管理框架，用于处理角色属性、装备属性、Buff/Debuff 等各种游戏数值计算。它提供了修饰符系统、依赖系统和脏标记优化，能够高效处理复杂的属性关系和数值计算。

### 核心特性

- **修饰符系统**：支持 8 种修饰符类型（Add、Mul、Clamp 等），灵活组合实现复杂数值计算
- **依赖系统**：属性间自动依赖关系，支持派生属性和级联更新
- **脏标记优化**：智能缓存机制，避免不必要的重复计算
- **分类管理**：层级分类系统，支持批量操作和标签查询
- **序列化支持**：完整的 JSON 序列化/反序列化功能
- **事件系统**：值变化监听，支持 UI 自动更新

### 适用场景

- **角色属性系统**：生命值、攻击力、防御力等基础属性及派生属性
- **装备系统**：装备属性加成、套装效果计算
- **Buff/Debuff 系统**：临时增益/减益效果的叠加和管理
- **技能系统**：技能伤害计算、冷却时间管理
- **经济系统**：金币、经验值等资源管理
- **游戏难度调整**：全局数值平衡和动态难度调节

---

## 快速开始

### 前置条件

- Unity 2021.3 或更高版本
- 已安装 EasyPack 框架
- 了解 C# 基础和 Unity MonoBehaviour 生命周期

### 安装步骤

GameProperty 系统已包含在 EasyPack 框架中，无需额外安装。确保以下命名空间可用：

```csharp
using EasyPack;
using EasyPack.GamePropertySystem;
```

### 第一个示例：创建简单属性

以下示例演示如何创建一个基础角色属性并应用修饰符：

```csharp
using UnityEngine;
using EasyPack;
using EasyPack.GamePropertySystem;

public class QuickStartExample : MonoBehaviour
{
    void Start()
    {
        // 1. 创建属性：角色攻击力，基础值 50
        var attackPower = new GameProperty("attack", 50f);
        
        // 2. 读取属性值
        Debug.Log($"基础攻击力: {attackPower.GetValue()}");
        // 输出: 基础攻击力: 50
        
        // 3. 添加修饰符：装备提供 +20 攻击力
        var weaponBonus = new FloatModifier(ModifierType.Add, 100, 20f);
        attackPower.AddModifier(weaponBonus);
        Debug.Log($"装备后攻击力: {attackPower.GetValue()}");
        // 输出: 装备后攻击力: 70
        
        // 4. 添加百分比加成：Buff 提供 50% 攻击力加成
        var buffBonus = new FloatModifier(ModifierType.Mul, 100, 1.5f);
        attackPower.AddModifier(buffBonus);
        Debug.Log($"Buff 后攻击力: {attackPower.GetValue()}");
        // 输出: Buff 后攻击力: 105 (计算: (50 + 20) × 1.5)
        
        // 5. 移除修饰符
        attackPower.RemoveModifier(buffBonus);
        Debug.Log($"Buff 结束后攻击力: {attackPower.GetValue()}");
        // 输出: Buff 结束后攻击力: 70
    }
}
```

**预期输出**：
```
基础攻击力: 50
装备后攻击力: 70
Buff 后攻击力: 105
Buff 结束后攻击力: 70
```

---

## 常见场景

### 场景 1：角色基础属性系统

创建一个完整的角色属性系统，包括生命值、攻击力、防御力等：

```csharp
using UnityEngine;
using EasyPack;
using EasyPack.GamePropertySystem;

public class CharacterStats : MonoBehaviour
{
    // 角色属性
    private GameProperty health;
    private GameProperty attack;
    private GameProperty defense;
    
    void Start()
    {
        // 创建基础属性
        health = new GameProperty("health", 100f);
        attack = new GameProperty("attack", 50f);
        defense = new GameProperty("defense", 20f);
        
        // 监听生命值变化
        health.OnValueChanged += (oldValue, newValue) =>
        {
            Debug.Log($"生命值变化: {oldValue} -> {newValue}");
            
            // 检查角色是否死亡
            if (newValue <= 0)
            {
                Debug.Log("角色死亡！");
            }
        };
        
        // 模拟受到伤害
        float damage = 30f;
        health.SetBaseValue(health.GetBaseValue() - damage);
        // 输出: 生命值变化: 100 -> 70
    }
}
```

### 场景 2：装备系统与属性加成

实现装备穿戴和属性加成效果：

```csharp
using UnityEngine;
using System.Collections.Generic;
using EasyPack;
using EasyPack.GamePropertySystem;

public class EquipmentSystem : MonoBehaviour
{
    private GameProperty attack;
    
    // 装备数据
    private class Equipment
    {
        public string Name;
        public IModifier Modifier;
    }
    
    private Dictionary<string, Equipment> equippedItems = new Dictionary<string, Equipment>();
    
    void Start()
    {
        attack = new GameProperty("attack", 50f);
        Debug.Log($"初始攻击力: {attack.GetValue()}");
        // 输出: 初始攻击力: 50
        
        // 穿戴武器：+30 攻击力
        EquipItem("weapon", "长剑", new FloatModifier(ModifierType.Add, 100, 30f));
        // 输出: 穿戴装备 [长剑]: 攻击力 50 -> 80
        
        // 穿戴戒指：+20% 攻击力
        EquipItem("ring", "力量戒指", new FloatModifier(ModifierType.Mul, 100, 1.2f));
        // 输出: 穿戴装备 [力量戒指]: 攻击力 80 -> 96
        
        // 卸下武器
        UnequipItem("weapon");
        // 输出: 卸下装备 [长剑]: 攻击力 96 -> 60
    }
    
    void EquipItem(string slot, string itemName, IModifier modifier)
    {
        // 如果槽位已有装备，先卸下
        if (equippedItems.ContainsKey(slot))
        {
            UnequipItem(slot);
        }
        
        float oldValue = attack.GetValue();
        attack.AddModifier(modifier);
        
        equippedItems[slot] = new Equipment { Name = itemName, Modifier = modifier };
        Debug.Log($"穿戴装备 [{itemName}]: 攻击力 {oldValue} -> {attack.GetValue()}");
    }
    
    void UnequipItem(string slot)
    {
        if (!equippedItems.ContainsKey(slot)) return;
        
        var equipment = equippedItems[slot];
        float oldValue = attack.GetValue();
        attack.RemoveModifier(equipment.Modifier);
        
        equippedItems.Remove(slot);
        Debug.Log($"卸下装备 [{equipment.Name}]: 攻击力 {oldValue} -> {attack.GetValue()}");
    }
}
```

### 场景 3：属性依赖系统（派生属性）

实现基于基础属性的派生属性计算：

```csharp
using UnityEngine;
using EasyPack;
using EasyPack.GamePropertySystem;

public class DerivedAttributeExample : MonoBehaviour
{
    void Start()
    {
        // 创建基础属性
        var strength = new GameProperty("strength", 10f);  // 力量
        var agility = new GameProperty("agility", 8f);     // 敏捷
        
        // 创建派生属性：最大生命值 = 力量 × 10
        var maxHealth = new GameProperty("maxHealth", 0f);
        maxHealth.AddDependency(strength, (dep, strengthValue) => strengthValue * 10f);
        
        Debug.Log($"初始最大生命值: {maxHealth.GetValue()}");
        // 输出: 初始最大生命值: 100
        
        // 创建复合派生属性：战斗力 = (力量 × 5) + (敏捷 × 3)
        var combatPower = new GameProperty("combatPower", 0f);
        combatPower.AddDependency(strength, (dep, val) => val * 5f);
        combatPower.AddDependency(agility, (dep, val) => val * 3f);
        
        Debug.Log($"初始战斗力: {combatPower.GetValue()}");
        // 输出: 初始战斗力: 74 (10×5 + 8×3)
        
        // 提升力量，派生属性自动更新
        strength.SetBaseValue(15f);
        Debug.Log($"力量提升后最大生命值: {maxHealth.GetValue()}");
        // 输出: 力量提升后最大生命值: 150
        
        Debug.Log($"力量提升后战斗力: {combatPower.GetValue()}");
        // 输出: 力量提升后战斗力: 99 (15×5 + 8×3)
    }
}
```

### 场景 4：GamePropertyManager 集中管理

使用 GamePropertyManager 管理大量属性：

```csharp
using UnityEngine;
using System.Threading.Tasks;
using EasyPack;
using EasyPack.GamePropertySystem;

public class ManagerExample : MonoBehaviour
{
    private IGamePropertyManager manager;
    
    async void Start()
    {
        // 从 EasyPack 架构获取 GamePropertyManager 服务
        manager = await EasyPackArchitecture.Instance.ResolveAsync<IGamePropertyManager>();
        
        // 创建并注册角色属性
        var hp = new GameProperty("hp", 100f);
        var mp = new GameProperty("mp", 50f);
        var stamina = new GameProperty("stamina", 80f);
        
        // 注册到分类，并添加元数据
        manager.Register(hp, "Character.Vital", new PropertyMetadata
        {
            DisplayName = "生命值",
            Description = "角色当前生命值",
            Tags = new[] { "vital", "displayInUI" }
        });
        
        manager.Register(mp, "Character.Vital", new PropertyMetadata
        {
            DisplayName = "魔法值",
            Tags = new[] { "vital", "displayInUI" }
        });
        
        manager.Register(stamina, "Character.Resource");
        
        // 按分类查询
        var vitalProps = manager.GetByCategory("Character.Vital");
        Debug.Log("重要属性列表:");
        foreach (var prop in vitalProps)
        {
            var meta = manager.GetMetadata(prop.ID);
            Debug.Log($"- {meta?.DisplayName ?? prop.ID}: {prop.GetValue()}");
        }
        // 输出:
        // 重要属性列表:
        // - 生命值: 100
        // - 魔法值: 50
        
        // 批量应用修饰符（全体增益 BUFF）
        var globalBuff = new FloatModifier(ModifierType.Mul, 100, 1.5f);
        var result = manager.ApplyModifierToCategory("Character.Vital", globalBuff);
        
        if (result.IsFullSuccess)
        {
            Debug.Log($"成功为 {result.SuccessCount} 个属性应用 BUFF");
            // 输出: 成功为 2 个属性应用 BUFF
        }
    }
}
```

### 场景 5：序列化与存档

保存和加载角色属性数据：

```csharp
using UnityEngine;
using System.Threading.Tasks;
using EasyPack;
using EasyPack.GamePropertySystem;

public class SaveLoadExample : MonoBehaviour
{
    async void Start()
    {
        // 获取序列化服务
        var serializationService = await EasyPackArchitecture.Instance.ResolveAsync<ISerializationService>();
        
        // 创建属性并添加修饰符
        var attack = new GameProperty("attack", 50f);
        attack.AddModifier(new FloatModifier(ModifierType.Add, 100, 20f));
        attack.AddModifier(new FloatModifier(ModifierType.Mul, 100, 1.5f));
        
        Debug.Log($"保存前攻击力: {attack.GetValue()}");
        // 输出: 保存前攻击力: 105
        
        // 序列化为 JSON
        string json = serializationService.Serialize(attack);
        Debug.Log($"JSON 数据: {json}");
        
        // 从 JSON 反序列化
        var loadedAttack = serializationService.Deserialize<GameProperty>(json);
        Debug.Log($"加载后攻击力: {loadedAttack.GetValue()}");
        // 输出: 加载后攻击力: 105
        
        Debug.Log($"修饰符数量: {loadedAttack.Modifiers.Count}");
        // 输出: 修饰符数量: 2
    }
}
```

---

## 进阶用法

### 修饰符优先级控制

修饰符的 Priority 参数决定了同类型修饰符的应用顺序：

```csharp
using UnityEngine;
using EasyPack;
using EasyPack.GamePropertySystem;

public class PriorityExample : MonoBehaviour
{
    void Start()
    {
        var value = new GameProperty("value", 100f);
        
        // 添加两个加法修饰符，优先级不同
        value.AddModifier(new FloatModifier(ModifierType.Add, priority: 100, 20f));
        value.AddModifier(new FloatModifier(ModifierType.Add, priority: 200, 30f));
        
        // Priority 越高越先应用（在同类型中）
        Debug.Log($"结果: {value.GetValue()}");
        // 输出: 结果: 150 (100 + 30 + 20)
    }
}
```

### 区间修饰符（RangeModifier）

RangeModifier 支持随机范围值和限制范围：

```csharp
using UnityEngine;
using EasyPack;
using EasyPack.GamePropertySystem;

public class RangeModifierExample : MonoBehaviour
{
    void Start()
    {
        var damage = new GameProperty("damage", 100f);
        
        // 1. Clamp 类型：限制值的范围
        var clampMod = new RangeModifier(ModifierType.Clamp, 100, new Vector2(50f, 150f));
        damage.AddModifier(clampMod);
        
        Debug.Log($"Clamp 限制后: {damage.GetValue()}");
        // 输出: Clamp 限制后: 100 (在范围内)
        
        // 2. 随机范围加成
        var randomMod = new RangeModifier(ModifierType.Add, 100, new Vector2(10f, 30f));
        damage.AddModifier(randomMod);
        
        // 每次获取值都会重新随机
        Debug.Log($"随机加成 1: {damage.GetValue()}");
        Debug.Log($"随机加成 2: {damage.GetValue()}");
        Debug.Log($"随机加成 3: {damage.GetValue()}");
        // 输出示例:
        // 随机加成 1: 123.45
        // 随机加成 2: 117.89
        // 随机加成 3: 128.12
    }
}
```

### 脏标记系统优化

利用脏标记系统实现高效的批量更新：

```csharp
using UnityEngine;
using EasyPack;
using EasyPack.GamePropertySystem;

public class DirtyFlagExample : MonoBehaviour
{
    void Start()
    {
        var attack = new GameProperty("attack", 100f);
        
        // 注册脏标记回调
        attack.OnDirty(() =>
        {
            Debug.Log("属性变为脏状态，需要重新计算");
        });
        
        // 批量添加修饰符（会触发多次脏标记）
        attack.AddModifier(new FloatModifier(ModifierType.Add, 100, 10f));
        attack.AddModifier(new FloatModifier(ModifierType.Add, 100, 20f));
        attack.AddModifier(new FloatModifier(ModifierType.Mul, 100, 1.5f));
        
        // 手动触发计算和通知
        attack.NotifyIfChanged();
        
        Debug.Log($"最终攻击力: {attack.GetValue()}");
        // 输出: 最终攻击力: 195 (计算: (100 + 10 + 20) × 1.5)
    }
}
```

### 事件监听与 UI 更新

使用事件系统实现属性变化的自动 UI 更新：

```csharp
using UnityEngine;
using UnityEngine.UI;
using EasyPack;
using EasyPack.GamePropertySystem;

public class UIUpdateExample : MonoBehaviour
{
    public Text healthText;
    private GameProperty health;
    
    void Start()
    {
        health = new GameProperty("health", 100f);
        
        // 监听值变化事件
        health.OnValueChanged += (oldValue, newValue) =>
        {
            healthText.text = $"生命值: {newValue:F0}";
            Debug.Log($"UI 更新: {oldValue} -> {newValue}");
        };
        
        // 或使用 OnDirtyAndValueChanged（立即计算并检查）
        health.OnDirtyAndValueChanged((oldValue, newValue) =>
        {
            // 在修饰符添加时立即触发，适合 UI 更新
            healthText.text = $"生命值: {newValue:F0}";
        });
        
        // 添加修饰符会自动触发 UI 更新
        health.AddModifier(new FloatModifier(ModifierType.Add, 100, 50f));
    }
}
```

---

## 故障排查

### 常见问题

#### 问题 1：属性值计算结果不符合预期

**症状**：应用修饰符后，GetValue() 返回的值与预期不一致

**原因**：修饰符应用顺序问题，或对修饰符类型理解有误

**解决方法**：
1. 检查修饰符类型的应用顺序（参见术语表中的"修饰符应用顺序"）
2. 使用 Debug 输出查看每个阶段的值
3. 确认 Priority 参数设置正确

```csharp
// 调试示例
var prop = new GameProperty("test", 100f);
Debug.Log($"基础值: {prop.GetBaseValue()}");

prop.AddModifier(new FloatModifier(ModifierType.Add, 100, 20f));
Debug.Log($"Add 后: {prop.GetValue()}");

prop.AddModifier(new FloatModifier(ModifierType.Mul, 100, 1.5f));
Debug.Log($"Mul 后: {prop.GetValue()}");
// 预期: (100 + 20) × 1.5 = 180
```

#### 问题 2：循环依赖导致无限递归

**症状**：添加依赖关系时出现警告 "检测到循环依赖"

**原因**：属性 A 依赖于 B，B 又依赖于 A，形成循环

**解决方法**：
- GameProperty 系统会自动检测并阻止循环依赖
- 重新设计属性依赖关系，确保依赖图是有向无环图（DAG）

```csharp
// 错误示例（会被系统阻止）
var propA = new GameProperty("A", 10f);
var propB = new GameProperty("B", 20f);

propA.AddDependency(propB, (dep, val) => val * 2f);
propB.AddDependency(propA, (dep, val) => val * 2f);  // 警告: 检测到循环依赖
```

#### 问题 3：Manager 查询返回空结果

**症状**：使用 GetByCategory 或 GetByTag 查询时返回空集合

**原因**：属性未正确注册，或分类/标签名称不匹配

**解决方法**：
1. 确认属性已通过 Register 方法注册
2. 检查分类名称大小写是否一致
3. 使用 GetAllPropertyIds() 查看所有已注册的属性

```csharp
// 调试示例
var manager = await EasyPackArchitecture.Instance.ResolveAsync<IGamePropertyManager>();

// 检查属性是否已注册
var allIds = manager.GetAllPropertyIds();
Debug.Log($"已注册属性: {string.Join(", ", allIds)}");

// 检查分类是否存在
var allCategories = manager.GetAllCategories();
Debug.Log($"所有分类: {string.Join(", ", allCategories)}");
```

#### 问题 4：序列化后依赖关系丢失

**症状**：序列化并反序列化后，属性的依赖关系不再生效

**原因**：GamePropertyJsonSerializer 不序列化依赖关系

**解决方法**：
- 依赖关系需要在代码中重新建立（设计决策：依赖关系通常是运行时逻辑）
- 或实现自定义序列化器保存依赖关系

```csharp
// 推荐做法：封装依赖关系建立逻辑
public class CharacterStats
{
    public GameProperty Strength;
    public GameProperty MaxHealth;
    
    public void SetupDependencies()
    {
        MaxHealth.AddDependency(Strength, (dep, val) => val * 10f);
    }
    
    public void LoadFromJson(string json)
    {
        // 反序列化属性
        Strength = DeserializeProperty(json, "strength");
        MaxHealth = DeserializeProperty(json, "maxHealth");
        
        // 重新建立依赖关系
        SetupDependencies();
    }
}
```

---

## 术语表

### 基础概念

- **GameProperty（游戏属性）**：表示游戏中的一个可修改数值属性，支持基础值和修饰符的组合计算。

- **BaseValue（基础值）**：属性的初始值，不受修饰符影响。通过 `SetBaseValue()` 修改。

- **FinalValue（最终值）**：应用所有修饰符后的计算结果，通过 `GetValue()` 获取。

- **Modifier（修饰符）**：用于动态修改属性值的组件，包括加法、乘法、限制范围等类型。

### 修饰符系统

- **FloatModifier（浮点修饰符）**：使用固定浮点数值的修饰符，如 +20 攻击力。

- **RangeModifier（区间修饰符）**：使用范围值的修饰符，支持随机范围或限制范围。

- **ModifierType（修饰符类型）**：定义修饰符的计算方式，包括：
  - `Add`：普通加法
  - `PriorityAdd`：优先级加法
  - `Mul`：乘法
  - `PriorityMul`：优先级乘法
  - `AfterAdd`：后置加法
  - `Override`：覆盖值
  - `Clamp`：限制范围

- **修饰符应用顺序**：Override → PriorityAdd → Add → PriorityMul → Mul → AfterAdd → Clamp

- **Priority（优先级）**：在**同类型**修饰符中，Priority 越高越先应用。

### 依赖系统

- **Dependency（依赖关系）**：一个属性依赖于另一个属性，当被依赖属性变化时自动更新。

- **DependencyCalculator（依赖计算器）**：定义依赖属性变化时如何计算当前属性值的函数。

- **DerivedProperty（派生属性）**：完全由其他属性计算得出的属性，如 "最大生命值 = 力量 × 10"。

- **CyclicDependency（循环依赖）**：属性间形成循环依赖关系，系统会自动检测并阻止。

### 脏标记系统

- **DirtyFlag（脏标记）**：标记属性需要重新计算的状态标志。

- **Cache（缓存）**：属性的计算结果缓存，只有在脏标记为 true 时才重新计算。

- **OnDirty（脏标记回调）**：属性变为脏状态时触发的回调函数。

### 管理器相关

- **GamePropertyManager（属性管理器）**：集中管理大量属性的服务，提供注册、查询、批量操作功能。

- **Category（分类）**：属性的分类标识，支持层级结构（如 "Character.Vital.Health"）。

- **Tag（标签）**：属性的标签，用于分组查询（如 "displayInUI", "saveable"）。

- **PropertyMetadata（属性元数据）**：属性的附加信息，包括显示名称、描述、图标等。

### 事件系统

- **OnValueChanged（值变化事件）**：属性最终值变化时触发，参数为 (旧值, 新值)。

- **OnBaseValueChanged（基础值变化事件）**：仅在 SetBaseValue 时触发，用于区分基础值和修饰符导致的变化。

- **OnDirtyAndValueChanged（脏标记并值变化事件）**：属性变脏时立即计算并检查值是否变化，适合 UI 更新。

---

**维护者：** NEKOPACK 团队  
**联系方式：** 提交 GitHub Issue 或 Pull Request  
**许可证：** 遵循项目主许可证
