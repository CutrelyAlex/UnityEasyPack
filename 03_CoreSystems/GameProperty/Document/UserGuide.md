# GameProperty System - User Guide

**适用EasyPack版本：** EasyPack v1.5.30
**最后更新：** 2025-10-26

---

## 概述

**GameProperty System** 是一个基于浮点数值的游戏属性系统，支持修饰符、依赖关系和脏标记机制。可用于实现角色属性、装备属性、Buff/Debuff 等各种游戏数值计算需求。

### 核心特性

- ✅ **修饰符系统**：支持加法、乘法、优先级运算、范围限制等多种修饰符类型
- ✅ **依赖系统**：属性间可建立依赖关系，自动同步计算
- ✅ **组合属性**：支持单一属性和自定义组合属性两种模式
- ✅ **属性管理器**：统一管理和查询多个属性
- ✅ **序列化支持**：可保存和加载属性数据
- ✅ **脏标记优化**：仅在必要时重新计算，提升性能

### 适用场景

- RPG 角色属性系统（生命、攻击、防御等）
- 装备系统（装备加成计算）
- Buff/Debuff 系统（临时属性修改）
- 伤害计算系统（期望伤害、暴击计算）
- 经济系统（价格、收益率计算）

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

- Unity 2021.3 或更高版本
- .NET Standard 2.1

### 安装步骤

1. 将 `EasyPack/03_CoreSystems/GameProperty` 文件夹复制到项目的 `Assets` 目录
2. 将 `EasyPack/02_Foundation/Modifiers` 文件夹复制到项目（修饰符依赖）
3. 确保项目已引用 Unity 核心库

### 第一示例

创建一个简单的角色生命值属性，并应用装备加成：

```csharp
using EasyPack.GamePropertySystem;
using EasyPack;
using UnityEngine;

public class QuickStartExample : MonoBehaviour
{
    void Start()
    {
        // 1. 创建基础生命值属性（基础值 100）
        var health = new GameProperty("health", 100f);
        Debug.Log($"基础生命值: {health.GetValue()}"); // 输出：100

        // 2. 添加装备加成（+50 生命值）
        var armorBonus = new FloatModifier(ModifierType.Add, 0, 50f);
        health.AddModifier(armorBonus);
        Debug.Log($"装备后生命值: {health.GetValue()}"); // 输出：150

        // 3. 添加技能加成（×1.5 倍）
        var skillBonus = new FloatModifier(ModifierType.Mul, 0, 1.5f);
        health.AddModifier(skillBonus);
        Debug.Log($"技能加成后生命值: {health.GetValue()}"); // 输出：225 (150 * 1.5)

        // 4. 监听属性变化
        health.OnValueChanged += (oldVal, newVal) =>
        {
            Debug.Log($"生命值变化: {oldVal} -> {newVal}");
        };

        // 5. 修改基础值（触发事件）
        health.SetBaseValue(120f);
        // 输出：生命值变化: 225 -> 255
        Debug.Log($"升级后生命值: {health.GetValue()}"); // 输出：255 ((120+50)*1.5)
    }
}
```

**运行结果：** 控制台将输出属性的计算过程，展示修饰符的应用效果。

---

## 常见场景

### 场景 1：创建 RPG 角色基础属性

为角色创建力量、敏捷、智力等基础属性，并建立派生属性：

```csharp
using EasyPack.GamePropertySystem;
using EasyPack;
using UnityEngine;

public class RPGAttributesExample : MonoBehaviour
{
    void Start()
    {
        // 创建基础属性
        var strength = new GameProperty("strength", 15f);
        var agility = new GameProperty("agility", 12f);
        var intelligence = new GameProperty("intelligence", 10f);

        // 创建派生属性
        var physicalAttack = new GameProperty("physicalAttack", 0f);
        var magicAttack = new GameProperty("magicAttack", 0f);
        var attackSpeed = new GameProperty("attackSpeed", 1f);

        // 建立依赖关系：物理攻击 = 力量 × 2 + 10
        physicalAttack.AddDependency(strength, (dep, newVal) => newVal * 2f + 10f);

        // 建立依赖关系：魔法攻击 = 智力 × 1.5 + 5
        magicAttack.AddDependency(intelligence, (dep, newVal) => newVal * 1.5f + 5f);

        // 建立依赖关系：攻击速度 = 1 + 敏捷 × 0.08
        attackSpeed.AddDependency(agility, (dep, newVal) => 1f + newVal * 0.08f);

        Debug.Log($"物理攻击: {physicalAttack.GetValue()}"); // 输出：40 (15*2+10)
        Debug.Log($"魔法攻击: {magicAttack.GetValue()}");   // 输出：20 (10*1.5+5)
        Debug.Log($"攻击速度: {attackSpeed.GetValue()}");   // 输出：1.96 (1+12*0.08)

        // 角色升级：力量提升到 20
        strength.SetBaseValue(20f);
        Debug.Log($"升级后物理攻击: {physicalAttack.GetValue()}"); // 输出：50 (20*2+10)
    }
}
```

**要点：** 使用依赖系统可自动同步派生属性，无需手动更新。

---

### 场景 2：装备系统与修饰符管理

实现装备穿戴/脱下功能，动态添加和移除修饰符：

```csharp
using EasyPack.GamePropertySystem;
using EasyPack;
using UnityEngine;
using System.Collections.Generic;

public class EquipmentSystemExample : MonoBehaviour
{
    private GameProperty attack;
    private List<IModifier> currentEquipmentModifiers = new List<IModifier>();

    void Start()
    {
        // 初始化攻击力属性
        attack = new GameProperty("attack", 50f);
        Debug.Log($"初始攻击力: {attack.GetValue()}"); // 输出：50

        // 装备武器
        EquipWeapon();

        // 装备护甲
        EquipArmor();

        // 脱下所有装备
        UnequipAll();
    }

    void EquipWeapon()
    {
        // 武器属性：+30 攻击力，×1.2 暴击倍率
        var weaponAdd = new FloatModifier(ModifierType.Add, 0, 30f);
        var weaponMul = new FloatModifier(ModifierType.Mul, 0, 1.2f);

        attack.AddModifier(weaponAdd);
        attack.AddModifier(weaponMul);

        currentEquipmentModifiers.Add(weaponAdd);
        currentEquipmentModifiers.Add(weaponMul);

        Debug.Log($"装备武器后攻击力: {attack.GetValue()}"); // 输出：96 ((50+30)*1.2)
    }

    void EquipArmor()
    {
        // 护甲属性：+15 攻击力（套装加成）
        var armorBonus = new FloatModifier(ModifierType.Add, 0, 15f);

        attack.AddModifier(armorBonus);
        currentEquipmentModifiers.Add(armorBonus);

        Debug.Log($"装备护甲后攻击力: {attack.GetValue()}"); // 输出：114 ((50+30+15)*1.2)
    }

    void UnequipAll()
    {
        // 移除所有装备修饰符
        attack.RemoveModifiers(currentEquipmentModifiers);
        currentEquipmentModifiers.Clear();

        Debug.Log($"脱下所有装备后攻击力: {attack.GetValue()}"); // 输出：50
    }
}
```

**要点：** 使用 `List<IModifier>` 保存装备修饰符引用，便于批量移除。

---

### 场景 3：使用组合属性实现期望伤害计算

计算角色的期望伤害（考虑暴击率和暴击倍率）：

```csharp
using EasyPack.GamePropertySystem;
using EasyPack;
using UnityEngine;

public class ExpectedDamageExample : MonoBehaviour
{
    void Start()
    {
        // 创建自定义组合属性
        var expectedDamage = new CombinePropertyCustom("expectedDamage", 0f);

        // 注册子属性
        var baseDamage = new GameProperty("baseDamage", 100f);
        var critChance = new GameProperty("critChance", 0.25f); // 25% 暴击率
        var critMultiplier = new GameProperty("critMultiplier", 2f); // 2 倍暴击

        expectedDamage.RegisterProperty(baseDamage);
        expectedDamage.RegisterProperty(critChance);
        expectedDamage.RegisterProperty(critMultiplier);

        // 自定义计算公式：期望伤害 = 基础伤害 × (1 + 暴击率 × (暴击倍率 - 1))
        expectedDamage.Calculater = (combine) =>
        {
            var baseDmg = combine.GetProperty("baseDamage").GetValue();
            var critRate = combine.GetProperty("critChance").GetValue();
            var critMul = combine.GetProperty("critMultiplier").GetValue();
            return baseDmg * (1f + critRate * (critMul - 1f));
        };

        Debug.Log($"期望伤害: {expectedDamage.GetValue()}"); // 输出：125 (100*(1+0.25*(2-1)))

        // 装备增加暴击率
        critChance.SetBaseValue(0.5f); // 提升到 50%
        Debug.Log($"提升暴击率后期望伤害: {expectedDamage.GetValue()}"); // 输出：150 (100*(1+0.5*(2-1)))

        // 为期望伤害添加最终修饰符（如技能加成）
        expectedDamage.ResultHolder.AddModifier(new FloatModifier(ModifierType.Mul, 0, 1.3f));
        Debug.Log($"技能加成后期望伤害: {expectedDamage.GetValue()}"); // 输出：195 (150*1.3)
    }
}
```

**要点：** `CombinePropertyCustom` 支持自定义计算公式，`ResultHolder` 可再应用修饰符。

---

### 场景 4：使用属性管理器统一管理

集中管理角色的所有属性，便于全局访问和序列化：

```csharp
using EasyPack.GamePropertySystem;
using EasyPack;
using UnityEngine;

public class PropertyManagerExample : MonoBehaviour
{
    private GamePropertyManager propertyManager;

    void Start()
    {
        // 初始化管理器
        propertyManager = new GamePropertyManager();

        // 创建并添加属性
        CreatePlayerProperties();

        // 全局访问属性
        AccessProperties();

        // 批量操作
        BatchOperations();
    }

    void CreatePlayerProperties()
    {
        // 创建单一组合属性
        var health = new CombinePropertySingle("health", 100f);
        var mana = new CombinePropertySingle("mana", 50f);
        var stamina = new CombinePropertySingle("stamina", 80f);

        // 添加到管理器
        propertyManager.AddOrUpdate(health);
        propertyManager.AddOrUpdate(mana);
        propertyManager.AddOrUpdate(stamina);

        Debug.Log($"管理器中的属性数量: {propertyManager.Count}"); // 输出：3
    }

    void AccessProperties()
    {
        // 通过 ID 获取属性
        var health = propertyManager.Get("health");
        if (health != null)
        {
            Debug.Log($"生命值: {health.GetValue()}"); // 输出：100
        }

        // 获取底层 GameProperty（用于添加修饰符）
        var healthGameProp = propertyManager.GetGameProperty("health");
        if (healthGameProp != null)
        {
            healthGameProp.AddModifier(new FloatModifier(ModifierType.Add, 0, 50f));
            Debug.Log($"加成后生命值: {health.GetValue()}"); // 输出：150
        }
    }

    void BatchOperations()
    {
        // 遍历所有属性
        foreach (var prop in propertyManager.GetAll())
        {
            Debug.Log($"属性 {prop.ID}: {prop.GetValue()}");
        }

        // 清理无效属性
        int cleaned = propertyManager.CleanupInvalidProperties();
        Debug.Log($"清理了 {cleaned} 个无效属性");
    }
}
```

**要点：** 管理器提供线程安全的并发访问，适合多模块共享属性。

---

### 场景 5：序列化与存档

保存和加载角色属性数据（如存档系统）：

```csharp
using EasyPack.GamePropertySystem;
using EasyPack;
using UnityEngine;

public class SerializationExample : MonoBehaviour
{
    void Start()
    {
        // 初始化序列化系统
        GamePropertySerializationInitializer.ManualInitialize();

        // 创建属性
        var playerPower = new GameProperty("playerPower", 100f);
        playerPower.AddModifier(new FloatModifier(ModifierType.Add, 0, 50f));
        playerPower.AddModifier(new FloatModifier(ModifierType.Mul, 0, 1.5f));

        Debug.Log($"原始属性值: {playerPower.GetValue()}"); // 输出：225 ((100+50)*1.5)

        // 序列化为 JSON
        string jsonData = SerializationServiceManager.SerializeToJson(playerPower);
        Debug.Log($"序列化数据: {jsonData}");

        // 反序列化
        var loadedProperty = SerializationServiceManager.DeserializeFromJson<GameProperty>(jsonData);
        Debug.Log($"加载后属性值: {loadedProperty.GetValue()}"); // 输出：225
        Debug.Log($"修饰符数量: {loadedProperty.ModifierCount}"); // 输出：2

        // 验证完整性
        bool isEqual = Mathf.Approximately(playerPower.GetValue(), loadedProperty.GetValue());
        Debug.Log($"数据完整性验证: {(isEqual ? "通过" : "失败")}");
    }
}
```

**注意事项：**
- 序列化仅保存属性数据和修饰符，不保存依赖关系
- 如需保存依赖关系，需在反序列化后手动重建
- 确保在序列化前调用 `GamePropertySerializationInitializer.ManualInitialize()`

---

## 进阶用法

### 复杂依赖链

建立多层级的属性依赖关系（如 A → B → C → D）：

```csharp
using EasyPack.GamePropertySystem;
using UnityEngine;

public class DependencyChainExample : MonoBehaviour
{
    void Start()
    {
        // 创建依赖链
        var chainA = new GameProperty("chainA", 10f);
        var chainB = new GameProperty("chainB", 0f);
        var chainC = new GameProperty("chainC", 0f);
        var chainD = new GameProperty("chainD", 0f);

        // 建立依赖链：A → B → C → D
        chainB.AddDependency(chainA, (dep, newVal) => newVal * 2f);
        chainC.AddDependency(chainB, (dep, newVal) => newVal + 5f);
        chainD.AddDependency(chainC, (dep, newVal) => newVal * 1.5f);

        Debug.Log($"A: {chainA.GetValue()}, B: {chainB.GetValue()}, C: {chainC.GetValue()}, D: {chainD.GetValue()}");
        // 输出：A: 10, B: 20, C: 25, D: 37.5

        // 修改 A 会自动更新整条依赖链
        chainA.SetBaseValue(15f);
        Debug.Log($"A: {chainA.GetValue()}, B: {chainB.GetValue()}, C: {chainC.GetValue()}, D: {chainD.GetValue()}");
        // 输出：A: 15, B: 30, C: 35, D: 52.5
    }
}
```

**警告：** 系统会自动检测循环依赖并阻止，但依赖深度限制为 100 层。

---

### 多属性联合计算

使用 `CombinePropertyCustom` 实现多个基础属性联合计算攻击速度：

```csharp
using EasyPack.GamePropertySystem;
using EasyPack;
using UnityEngine;

public class MultiAttributeCalculationExample : MonoBehaviour
{
    void Start()
    {
        // 创建基础属性
        var agility = new GameProperty("agility", 12f);
        var strength = new GameProperty("strength", 15f);

        // 创建自定义组合属性
        var attackSpeed = new CombinePropertyCustom("attackSpeed", 1f); // 基础速度 1.0

        // 注册参与计算的属性
        attackSpeed.RegisterProperty(agility);
        attackSpeed.RegisterProperty(strength);

        // 自定义计算：攻击速度 = 基础速度 + 敏捷×0.1 + 力量×0.05
        attackSpeed.Calculater = (combine) =>
        {
            var baseSpeed = combine.GetBaseValue();
            var agi = combine.GetProperty("agility").GetValue();
            var str = combine.GetProperty("strength").GetValue();
            return baseSpeed + agi * 0.1f + str * 0.05f;
        };

        Debug.Log($"攻击速度: {attackSpeed.GetValue()}"); // 输出：2.95 (1+12*0.1+15*0.05)

        // 修改任意基础属性都会自动更新攻击速度
        agility.SetBaseValue(18f);
        Debug.Log($"敏捷提升后攻击速度: {attackSpeed.GetValue()}"); // 输出：3.55 (1+18*0.1+15*0.05)
    }
}
```

**要点：** 注册属性后，任意子属性变化都会自动触发重新计算。

---

### 修饰符优先级与执行顺序

利用修饰符优先级实现精确的计算顺序：

```csharp
using EasyPack.GamePropertySystem;
using EasyPack;
using UnityEngine;

public class ModifierPriorityExample : MonoBehaviour
{
    void Start()
    {
        var damage = new GameProperty("damage", 100f);

        // 普通加法修饰符（优先级 0）
        damage.AddModifier(new FloatModifier(ModifierType.Add, 0, 50f));

        // 高优先级加法修饰符（优先级 1，优先计算）
        damage.AddModifier(new FloatModifier(ModifierType.PriorityAdd, 1, 30f));

        // 乘法修饰符（优先级 0）
        damage.AddModifier(new FloatModifier(ModifierType.Mul, 0, 1.5f));

        // 后加法修饰符（在所有乘法后应用）
        damage.AddModifier(new FloatModifier(ModifierType.AfterAdd, 0, 20f));

        // 计算顺序：PriorityAdd → Add → Mul → AfterAdd
        // 结果：((100 + 30) + 50) * 1.5 + 20 = 290
        Debug.Log($"最终伤害: {damage.GetValue()}"); // 输出：290
    }
}
```

**修饰符类型执行顺序：**
1. `Override`（覆盖）
2. `PriorityAdd`（优先级加法）
3. `Add`（普通加法）
4. `PriorityMul`（优先级乘法）
5. `Mul`（普通乘法）
6. `AfterAdd`（后加法）
7. `Clamp`（范围限制）

---

### 脏标记与性能优化

利用脏标记机制监控属性计算，优化性能敏感场景：

```csharp
using EasyPack.GamePropertySystem;
using EasyPack;
using UnityEngine;

public class DirtyTrackingExample : MonoBehaviour
{
    void Start()
    {
        var property = new GameProperty("trackedProperty", 100f);

        // 注册脏标记回调
        property.OnDirty(() =>
        {
            Debug.Log("属性被标记为脏数据，需要重新计算");
        });

        // 添加修饰符会触发脏标记
        property.AddModifier(new FloatModifier(ModifierType.Add, 0, 50f));
        // 输出：属性被标记为脏数据，需要重新计算

        // 获取值会清除脏标记（如果没有随机性修饰符）
        Debug.Log($"属性值: {property.GetValue()}"); // 输出：150

        // 再次添加修饰符
        property.AddModifier(new FloatModifier(ModifierType.Mul, 0, 1.5f));
        // 输出：属性被标记为脏数据，需要重新计算

        Debug.Log($"属性值: {property.GetValue()}"); // 输出：225
    }
}
```

**优化建议：**
- 频繁读取的属性使用缓存值（脏标记自动管理）
- 避免在依赖链中使用 `RangeModifier`（非 Clamp 类型），会导致每次都重新计算
- 批量添加修饰符使用 `AddModifiers()` 而非多次 `AddModifier()`

---

## 故障排查

### 常见问题

#### 问题 1：编译错误 - 找不到类型 `GameProperty`

**症状：** 提示 `The type or namespace name 'GameProperty' could not be found`  
**原因：** 缺少命名空间引用  
**解决方法：** 在文件头部添加以下命名空间：

```csharp
using EasyPack.GamePropertySystem;
using EasyPack; // 用于修饰符类型
```

---

#### 问题 2：依赖关系未生效

**症状：** 修改依赖属性的值后，依赖者属性未更新  
**原因：** 未提供计算函数，或计算函数返回了固定值  
**解决方法：** 确保提供正确的计算函数：

```csharp
// ❌ 错误：未提供计算函数
dependentProp.AddDependency(baseProp);

// ✅ 正确：提供计算函数
dependentProp.AddDependency(baseProp, (dep, newVal) => newVal * 2f);
```

---

#### 问题 3：循环依赖警告

**症状：** 控制台输出 `检测到循环依赖，取消添加依赖关系`  
**原因：** 尝试创建循环依赖（如 A → B → C → A）  
**解决方法：** 重新设计依赖关系，避免循环。如需多属性联合计算，使用 `CombinePropertyCustom`：

```csharp
// ❌ 错误：循环依赖
propA.AddDependency(propB);
propB.AddDependency(propC);
propC.AddDependency(propA); // 形成循环

// ✅ 正确：使用组合属性
var combined = new CombinePropertyCustom("combined");
combined.RegisterProperty(propA);
combined.RegisterProperty(propB);
combined.RegisterProperty(propC);
combined.Calculater = (c) => /* 自定义计算逻辑 */;
```

---

#### 问题 4：序列化后修饰符丢失

**症状：** 反序列化后属性值不正确，修饰符数量为 0  
**原因：** 未初始化序列化系统  
**解决方法：** 在序列化前调用初始化：

```csharp
// 在游戏启动时或序列化前调用
GamePropertySerializationInitializer.ManualInitialize();

// 然后执行序列化操作
string json = SerializationServiceManager.SerializeToJson(property);
```

---

#### 问题 5：`ObjectDisposedException` 异常

**症状：** 访问属性时抛出 `ObjectDisposedException`  
**原因：** 访问了已释放的组合属性对象  
**解决方法：** 在访问前检查对象是否有效：

```csharp
if (property != null && property.IsValid())
{
    var value = property.GetValue();
}
```

---

#### 问题 6：属性值计算不符合预期

**症状：** 属性最终值与手动计算结果不一致  
**原因：** 修饰符执行顺序理解错误  
**解决方法：** 按照修饰符类型的执行顺序验证计算：

```csharp
// 修饰符执行顺序：Override > PriorityAdd > Add > PriorityMul > Mul > AfterAdd > Clamp

var prop = new GameProperty("test", 100f);
prop.AddModifier(new FloatModifier(ModifierType.Add, 0, 50f));      // 第 3 步
prop.AddModifier(new FloatModifier(ModifierType.Mul, 0, 1.5f));     // 第 5 步
prop.AddModifier(new FloatModifier(ModifierType.AfterAdd, 0, 20f)); // 第 6 步

// 计算过程：((100 + 50) * 1.5) + 20 = 245
Debug.Log(prop.GetValue()); // 输出：245
```

---

### FAQ 更新记录

*本节持续更新，记录用户反馈的新问题。*

#### 问题 X：（待补充）

*如遇到未列出的问题，请提交 GitHub Issue 或联系维护者。*

---

## 术语表

### 修饰符（Modifier）
影响属性最终值的计算逻辑，包括加法、乘法、范围限制等类型。每个修饰符有类型（`ModifierType`）、优先级（`Priority`）和值（`Value`）。

### 依赖关系（Dependency）
属性间的联动关系，当被依赖属性值变化时，依赖者属性会自动重新计算。通过 `AddDependency()` 建立。

### 组合属性（Combine Property）
高级属性类型，支持自定义计算逻辑。包括 `CombinePropertySingle`（单一属性包装）和 `CombinePropertyCustom`（多属性组合）。

### 脏标记（Dirty Flag）
性能优化机制，标记属性需要重新计算。仅在属性被标记为脏时才执行计算，避免不必要的重复计算。

### 属性管理器（Property Manager）
统一管理多个组合属性的容器，提供线程安全的添加、查询、删除功能。类型为 `GamePropertyManager`。

### 结果持有者（Result Holder）
组合属性内部的 `GameProperty` 对象，存储计算后的最终值。可通过 `ResultHolder` 属性访问。

### 基础值（Base Value）
属性的初始值，未应用任何修饰符的原始数值。通过 `GetBaseValue()` 获取，`SetBaseValue()` 修改。

### 最终值（Final Value）
应用所有修饰符后的计算结果。通过 `GetValue()` 获取。

---

## 相关资源

- [API 参考文档](./APIReference.md) - 详细的方法签名和参数说明
- [Mermaid 图集](./Diagrams.md) - 系统架构和数据流可视化
- [示例代码](../Example/GamePropertyExample.cs) - 完整的使用示例

---

**维护者：** EasyPack 团队  
**联系方式：** 提交 GitHub Issue 或 Pull Request  
**许可证：** 遵循项目主许可证
