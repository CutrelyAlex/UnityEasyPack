# GameProperty 系统使用指南
** 本文档适应Claude Sonnet 4 编写 注意核对 **

## 目录
- [系统概述](#系统概述)
- [核心组件](#核心组件)
- [API参考](#api参考)
  - [GameProperty API](#gameproperty-api)
  - [CombineProperty API](#combineproperty-api)
  - [CombineGamePropertyManager API](#combinegamepropertymanager-api)
  - [修饰器 API](#修饰器-api)
  - [序列化 API](#序列化-api)
- [基本使用流程](#基本使用流程)
  - [创建基础属性](#创建基础属性)
  - [添加修饰器](#添加修饰器)
  - [管理属性依赖](#管理属性依赖)
- [组合属性详解](#组合属性详解)
  - [CombinePropertySingle](#combinepropertysingle)
  - [CombinePropertyCustom](#combinepropertycustom)
- [属性管理器](#属性管理器)
- [修饰器系统](#修饰器系统)
- [使用规范与最佳实践](#使用规范与最佳实践)
- [高级功能](#高级功能)
  - [属性依赖管理](#属性依赖管理)
  - [事件系统](#事件系统)
  - [属性序列化](#属性序列化)
- [与其他系统集成](#与其他系统集成)
- [性能优化](#性能优化)
- [常见用例](#常见用例)
- [故障排除](#故障排除)

## 系统概述

GameProperty系统是一个灵活的游戏属性管理框架，专为RPG、策略等游戏类型设计。它提供了处理数值属性的各种功能，包括修饰器应用、属性依赖关系、事件监听、序列化等。系统基于组件化设计，通过不同的修饰器和属性组合方式，可以实现各种复杂的属性计算逻辑。系统使用 EasyPack 统一序列化服务进行数据持久化。
你可以查看GameProperty/Example/GamePropertyExample.cs中的示例代码来获得更加直观的案例

### 系统特性

- **模块化设计**: 通过不同组件组合实现复杂属性逻辑
- **修饰器系统**: 支持多种修饰器类型和优先级
- **属性依赖**: 支持属性间的依赖关系和自动更新
- **事件驱动**: 提供完整的属性变化事件监听
- **统一序列化**: 使用 EasyPack 统一序列化服务进行 JSON 序列化/反序列化
- **性能优化**: 包含脏标记机制和缓存优化

## 核心组件

- **GameProperty**: 单一的可修饰数值属性，支持修饰器、依赖关系和事件
- **CombineProperty系列**: 组合多个GameProperty的不同实现方式
- **CombineGamePropertyManager**: 全局属性管理器，处理属性的注册与查询
- **修饰器(IModifier)**: 定义如何修改属性值的接口，有多种具体实现
- **SerializationServiceManager**: 统一序列化服务管理器（推荐用于序列化）
- **GamePropertySerializer**: ~~处理属性的序列化与反序列化~~ （已废弃，请使用 SerializationServiceManager）

## API参考

### GameProperty API

#### 构造函数
```
GameProperty(string id, float initValue)       // 创建属性
```

#### 基础值操作
```
float GetBaseValue()                            // 获取基础值
IProperty<float> SetBaseValue(float value)     // 设置基础值，返回自身用于链式调用
float GetValue()                                // 获取计算后的最终值
```

#### 修饰器管理
```
IProperty<float> AddModifier(IModifier modifier)           // 添加修饰器，返回自身
IProperty<float> RemoveModifier(IModifier modifier)        // 移除特定修饰器，返回自身
IProperty<float> ClearModifiers()                          // 清除所有修饰器，返回自身
IProperty<float> AddModifiers(IEnumerable<IModifier> modifiers)     // 批量添加修饰器
IProperty<float> RemoveModifiers(IEnumerable<IModifier> modifiers)  // 批量移除修饰器
List<IModifier> Modifiers { get; }                         // 获取所有修饰器（属性）
```

#### 修饰器查询
```
bool HasModifiers { get; }                                 // 检查是否有任何修饰器
int ModifierCount { get; }                                 // 获取修饰器总数量
bool ContainModifierOfType(ModifierType type)              // 检查是否有指定类型的修饰器
int GetModifierCountOfType(ModifierType type)              // 获取指定类型的修饰器数量
```

#### 依赖关系管理
```
IProperty<float> AddDependency(GameProperty dependency)                                    // 添加简单依赖
IProperty<float> AddDependency(GameProperty dependency, Func<GameProperty, float, float> calculator) // 添加带计算器的依赖
IProperty<float> RemoveDependency(GameProperty dependency)              // 移除依赖属性
```

#### 事件管理
```
event Action<float, float> OnValueChanged      // 值变化事件
void OnDirty(Action callback)                  // 添加脏标记监听
void RemoveOnDirty(Action callback)            // 移除脏标记监听
```

#### 其他方法
```
void MakeDirty()                               // 手动标记为脏
string ID { get; set; }                       // 属性ID
```

### CombineProperty API

#### 通用接口 (ICombineGameProperty)
```
string ID { get; }                             // 属性ID
float GetValue()                               // 获取计算值
float GetBaseValue()                           // 获取基础值
GameProperty GetProperty(string id);           // 获取子属性
GameProperty ResultHolder { get; }             // 结果持有者
Func<ICombineGameProperty, float> Calculater { get; set; }  // 计算函数
bool IsValid()                                 // 验证有效性
void Dispose()                                 // 释放资源
```

#### CombineGameProperty基类额外方法
```
event Action<float, float> OnValueChanged                   // 值变化事件（代理到ResultHolder）
void AddModifierToHolder(IModifier modifier)               // 向ResultHolder添加修饰器
void RemoveModifierFromHolder(IModifier modifier)          // 从ResultHolder移除修饰器
void ClearModifiersFromHolder()                            // 清空ResultHolder的修饰器
```

#### CombinePropertySingle
```
CombinePropertySingle(string id, float baseValue = 0f)     // 构造函数
GameProperty ResultHolder { get; }                         // 内部属性持有者
GameProperty GetProperty()                                  // 获取ResultHolder
void AddModifier(IModifier modifier)               // 向ResultHolder添加修饰器
void RemoveModifier(IModifier modifier)          // 从ResultHolder移除修饰器
void ClearModifiers()                            // 清空ResultHolder的修饰器
```

#### CombinePropertyCustom
```
CombinePropertyCustom(string id, float baseValue = 0f)     // 构造函数
GameProperty RegisterProperty(GameProperty gameProperty, Action<float, float> handler = null)  // 注册属性
void UnRegisterProperty(GameProperty gameProperty)         // 注销属性
Func<ICombineGameProperty, float> Calculater { get; set; } // 计算函数
```

### CombineGamePropertyManager API

#### 实例方法
```
void AddOrUpdate(ICombineGameProperty property)                        // 添加或更新属性
CombinePropertySingle Wrap(GameProperty property)                       // 包装 GameProperty（自动复制修饰符）
IEnumerable<CombinePropertySingle> WrapRange(IEnumerable<GameProperty> properties) // 批量包装
ICombineGameProperty Get(string id)                                   // 获取属性
CombinePropertySingle GetSingle(string id)                            // 获取 Single 类型
CombinePropertyCustom GetCustom(string id)                            // 获取 Custom 类型
GameProperty GetGameProperty(string id, string subId = "")            // 获取 GameProperty
GameProperty GetGamePropertyFromCombine(string combinePropertyID, string id = "") // 从组合属性提取
bool Remove(string id)                                                 // 移除属性
IEnumerable<ICombineGameProperty> GetAll()                            // 获取所有属性
IEnumerable<CombinePropertySingle> GetAllSingles()                    // 获取所有 Single
IEnumerable<CombinePropertyCustom> GetAllCustoms()                    // 获取所有 Custom
void Clear()                                                           // 清空所有属性
int CleanupInvalidProperties()                                        // 清理无效属性
int Count { get; }                                                     // 属性总数
bool Contains(string id)                                               // 检查是否包含
bool IsSingle(string id)                                               // 检查是否为 Single
bool IsCustom(string id)                                               // 检查是否为 Custom
void AddOrUpdateRange(IEnumerable<ICombineGameProperty> properties)  // 批量添加
int RemoveRange(IEnumerable<string> ids)                              // 批量移除
```

### 修饰器 API

#### IModifier 接口
```
ModifierType Type { get; }                     // 修饰器类型
int Priority { get; set; }                     // 优先级
IModifier Clone()                              // 克隆修饰器
```

#### IModifier<T> 接口
```
T Value { get; set; }                          // 修饰值
```

#### FloatModifier
```
FloatModifier(ModifierType type, int priority, float value)   // 构造函数
```

#### RangeModifier
```
RangeModifier(ModifierType type, int priority, Vector2 range) // 构造函数
```

#### ModifierType 枚举
```
None             // 无修饰
Add              // 加法修饰
AfterAdd         // 后置加法
PriorityAdd      // 优先级加法
Mul              // 乘法修饰
PriorityMul      // 优先级乘法
Override         // 覆盖值
Clamp            // 范围限制
```

### 序列化 API

#### 统一序列化服务（推荐使用）

GameProperty 系统现在使用 EasyPack 统一序列化服务进行序列化操作：

```csharp
// 序列化 GameProperty 到 JSON
string json = SerializationServiceManager.SerializeToJson(gameProperty);

// 从 JSON 反序列化 GameProperty
var gameProperty = SerializationServiceManager.DeserializeFromJson<GameProperty>(json);

// 序列化 CombinePropertySingle
string json = SerializationServiceManager.SerializeToJson(combineProperty);

// 反序列化 CombinePropertySingle
var combineProperty = SerializationServiceManager.DeserializeFromJson<CombinePropertySingle>(json);

// 序列化 CombinePropertyCustom
string json = SerializationServiceManager.SerializeToJson(customProperty);

// 反序列化 CombinePropertyCustom（注意：Calculator 和 RegisteredProperties 不会被序列化）
var customProperty = SerializationServiceManager.DeserializeFromJson<CombinePropertyCustom>(json);
```

**重要说明**：
- 序列化器会自动在运行时初始化（通过 `GamePropertySerializationInitializer`）
- 只序列化属性数据本身，**不序列化依赖关系**
- `CombinePropertyCustom` 的 `Calculator` 函数和 `RegisteredProperties` 无法序列化，需要在反序列化后手动重新注册

#### GamePropertySerializer（已废弃）

> ⚠️ **废弃警告**：`GamePropertySerializer` 和 `CombineGamePropertySerializer` 已被标记为过时。请使用上面的统一序列化服务。

旧 API（仅供参考，不建议使用）：
```csharp
static SerializableGameProperty Serialize(GameProperty property)              // 序列化
static GameProperty FromSerializable(SerializableGameProperty serializable)  // 反序列化
```

#### CombineGamePropertySerializer（已废弃）

旧 API（仅供参考，不建议使用）：
```csharp
static SerializableCombineGameProperty Serialize(ICombineGameProperty property)     // 序列化
static ICombineGameProperty FromSerializable(SerializableCombineGameProperty data)  // 反序列化
```

## 基本使用流程

### 创建基础属性

```
// 创建一个基础属性，设置ID和初始值
var hp = new GameProperty("HP", 100f);

// 获取基础属性值
float baseValue = hp.GetBaseValue(); // 100

// 设置基础属性值（支持链式调用）
hp.SetBaseValue(120f);

// 获取最终值（应用所有修饰器后）
float finalValue = hp.GetValue();
```

### 添加修饰器

```
// 链式调用添加多个修饰器
hp.AddModifier(new FloatModifier(ModifierType.Add, 0, 20f))      // 增加20点生命值
  .AddModifier(new FloatModifier(ModifierType.Mul, 0, 1.5f))     // 增加50%生命值
  .AddModifier(new RangeModifier(ModifierType.Clamp, 0, new Vector2(0, 200))); // 限制范围0-200

// 获取应用所有修饰器后的最终值
float finalValue = hp.GetValue(); // 结果：min((120+20)*1.5, 200) = 200

// 查询修饰器
bool hasModifiers = hp.HasModifiers;                           // 检查是否有修饰器
int modifierCount = hp.ModifierCount;                          // 获取修饰器总数
bool hasAddModifiers = hp.ContainModifierOfType(ModifierType.Add); // 检查是否有加法修饰器
int addModifierCount = hp.GetModifierCountOfType(ModifierType.Add); // 获取加法修饰器数量

// 移除和清理
hp.RemoveModifier(someModifier);   // 移除特定修饰器
hp.ClearModifiers();               // 清除所有修饰器
```

### 管理属性依赖

```
// 创建两个属性
var strength = new GameProperty("Strength", 10f);     // 力量
var attackPower = new GameProperty("AttackPower", 0f); // 攻击力

// 添加带计算器的依赖关系：攻击力 = 力量 × 2
attackPower.AddDependency(strength, (dep, newVal) => newVal * 2f);

// 当力量变化时，攻击力会自动更新
strength.SetBaseValue(15f);
float newAttack = attackPower.GetValue(); // 30

// 添加简单依赖（需要手动处理更新逻辑）
var agility = new GameProperty("Agility", 8f);
attackPower.AddDependency(agility);

// 监听变化事件手动处理复杂依赖
// 注意，这种情况下，事件链不完整：只监听了 strength 的变化，但 agility 变化时不会触发重计算
// 如果需要复杂的属性计算，建议使用组合属性
strength.OnValueChanged += (oldVal, newVal) => {
    // 复杂计算：攻击力 = 力量×2 + 敏捷×0.5
    float newAttackPower = strength.GetValue() * 2f + agility.GetValue() * 0.5f;
    attackPower.SetBaseValue(newAttackPower);
};
```

## 组合属性详解

组合属性用于将多个GameProperty以特定方式组合，目前提供两种不同的实现方式。

### CombinePropertySingle

最简单的组合属性，本质上是单一GameProperty的包装器。

```
// 创建单一组合属性
var single = new CombinePropertySingle("SingleProp", 50f);

// 访问内部属性
single.ResultHolder.AddModifier(new FloatModifier(ModifierType.Add, 0, 10f));

// 或者使用包装方法
single.AddModifier(new FloatModifier(ModifierType.Add, 0, 5f));

// 获取最终值
float value = single.GetValue(); // 65

// 监听值变化
single.OnValueChanged += (oldVal, newVal) => {
    Debug.Log($"属性值从{oldVal}变为{newVal}");
};

// 注册到管理器
var manager = new CombineGamePropertyManager();
manager.AddOrUpdate(single);
```

### CombinePropertyCustom

完全自定义的组合方式，通过委托函数灵活定义属性组合逻辑。这是系统中最强大和灵活的组合属性实现。

#### 基础使用

```
// 创建自定义组合属性
var customAttack = new CombinePropertyCustom("AttackPower", 0f);

// 创建基础属性
var strength = new GameProperty("Strength", 10f);
var agility = new GameProperty("Agility", 8f);
var level = new GameProperty("Level", 5f);

// 注册属性到组合属性中
customAttack.RegisterProperty(strength);
customAttack.RegisterProperty(agility);
customAttack.RegisterProperty(level);

// 设置自定义计算逻辑：攻击力 = 力量×2 + 敏捷×0.5 + 等级×1.5
customAttack.Calculater = (combine) => {
    var str = combine.GetProperty("Strength").GetValue();
    var agi = combine.GetProperty("Agility").GetValue();
    var lvl = combine.GetProperty("Level").GetValue();
    return str * 2f + agi * 0.5f + lvl * 1.5f;
};

// 获取计算结果
float attackValue = customAttack.GetValue(); // 10*2 + 8*0.5 + 5*1.5 = 31.5
```

#### 事件处理与自动更新

```
// 创建带事件处理的自定义属性
var healthSystem = new CombinePropertyCustom("TotalHealth", 0f);

var constitution = new GameProperty("Constitution", 20f);
var endurance = new GameProperty("Endurance", 15f);

// 注册属性时可以指定事件处理器
healthSystem.RegisterProperty(constitution, (oldVal, newVal) => {
    Debug.Log($"体质从 {oldVal} 变为 {newVal}，重新计算生命值");
});

healthSystem.RegisterProperty(endurance, (oldVal, newVal) => {
    Debug.Log($"耐力从 {oldVal} 变为 {newVal}，重新计算生命值");
});

// 设置复杂的生命值计算公式
healthSystem.Calculater = (combine) => {
    var con = combine.GetProperty("Constitution").GetValue();
    var end = combine.GetProperty("Endurance").GetValue();
    
    // 复杂计算：基础生命 + 体质加成 + 耐力加成 + 组合加成
    float baseHealth = 100f;
    float conBonus = con * 5f;
    float endBonus = end * 3f;
    float synergyBonus = (con + end) * 0.1f; // 体质和耐力的协同加成
    
    return baseHealth + conBonus + endBonus + synergyBonus;
};

// 当基础属性变化时，组合属性会自动重新计算
constitution.SetBaseValue(25f); // 触发事件和重新计算
```

#### 经典RPG属性计算示例

```
// 模拟经典RPG的复杂属性计算
public class RPGAttackPowerSystem
{
    private CombinePropertyCustom _finalAttackPower;
    private CombineGamePropertyManager _manager;
    
    public void Initialize()
    {
        _manager = new CombineGamePropertyManager();
        
        // 创建基础属性
        var baseAttack = new GameProperty("BaseAttack", 50f);
        var weaponDamage = new GameProperty("WeaponDamage", 30f);
        var strengthMod = new GameProperty("StrengthModifier", 0f);
        var criticalPower = new GameProperty("CriticalPower", 0f);
        
        // 创建自定义组合属性
        _finalAttackPower = new CombinePropertyCustom("FinalAttackPower", 0f);
        
        // 注册所有相关属性
        _finalAttackPower.RegisterProperty(baseAttack);
        _finalAttackPower.RegisterProperty(weaponDamage);
        _finalAttackPower.RegisterProperty(strengthMod);
        _finalAttackPower.RegisterProperty(criticalPower);
        
        // 设置复杂的攻击力计算公式
        _finalAttackPower.Calculater = (combine) => {
            var baseAtk = combine.GetProperty("BaseAttack").GetValue();
            var weaponDmg = combine.GetProperty("WeaponDamage").GetValue();
            var strMod = combine.GetProperty("StrengthModifier").GetValue();
            var critPower = combine.GetProperty("CriticalPower").GetValue();
            
            // 经典RPG计算：(基础攻击 + 武器伤害) × (1 + 力量修正) × (1 + 暴击强度)
            float totalAttack = (baseAtk + weaponDmg) * (1f + strMod) * (1f + critPower);
            
            // 应用其他复杂逻辑
            if (totalAttack > 1000f)
            {
                // 高攻击力惩罚
                totalAttack = 1000f + (totalAttack - 1000f) * 0.8f;
            }
            
            return Mathf.Max(totalAttack, 1f); // 最小攻击力为1
        };
        
        // 注册到管理器
        _manager.AddOrUpdate(_finalAttackPower);
    }
    
    public float GetCurrentAttackPower()
    {
        return _finalAttackPower.GetValue();
    }
    
    public void ApplyStrengthBonus(float strengthValue)
    {
        var strMod = _finalAttackPower.GetProperty("StrengthModifier");
        strMod?.SetBaseValue(strengthValue * 0.05f); // 每点力量提供5%攻击加成
    }
}
```

#### 多阶段计算系统

```
// 构建多阶段的复杂计算系统
public class MultiStageCalculationExample
{
    public void BuildComplexSystem()
    {
        var manager = new CombineGamePropertyManager();
        
        // 第一阶段：基础属性
        var strength = new GameProperty("Strength", 20f);
        var agility = new GameProperty("Agility", 15f);
        var intelligence = new GameProperty("Intelligence", 18f);
        
        // 第二阶段：派生属性
        var physicalPower = new CombinePropertyCustom("PhysicalPower", 0f);
        physicalPower.RegisterProperty(strength);
        physicalPower.RegisterProperty(agility);
        physicalPower.Calculater = combine => {
            var str = combine.GetProperty("Strength").GetValue();
            var agi = combine.GetProperty("Agility").GetValue();
            return str * 1.5f + agi * 0.8f;
        };
        
        var magicalPower = new CombinePropertyCustom("MagicalPower", 0f);
        magicalPower.RegisterProperty(intelligence);
        magicalPower.RegisterProperty(agility); // 敏捷也影响法术威力
        magicalPower.Calculater = combine => {
            var intel = combine.GetProperty("Intelligence").GetValue();
            var agi = combine.GetProperty("Agility").GetValue();
            return intel * 2f + agi * 0.3f;
        };
        
        // 第三阶段：最终属性
        var combatRating = new CombinePropertyCustom("CombatRating", 0f);
        
        combatRating.Calculater = combine => {
            var physical = physicalPower.GetValue();
            var magical = magicalPower.GetValue();
            
            // 物理和魔法威力的综合评分
            return Mathf.Sqrt(physical * magical) * 2f;
        };
        
        // 注册所有属性
        manager.AddOrUpdate(physicalPower);
        manager.AddOrUpdate(magicalPower);
        manager.AddOrUpdate(combatRating);
        
        // 测试系统
        Debug.Log($"初始战斗力评分: {combatRating.GetValue()}");
        
        // 修改基础属性，观察级联更新
        strength.SetBaseValue(25f);
        Debug.Log($"提升力量后战斗力评分: {combatRating.GetValue()}");
    }
}
```

#### 条件性计算

```
// 实现带条件逻辑的属性计算
public class ConditionalCalculationExample
{
    public void SetupConditionalSystem()
    {
        var manager = new CombineGamePropertyManager();
        
        // 基础属性
        var health = new GameProperty("Health", 100f);
        var maxHealth = new GameProperty("MaxHealth", 100f);
        var rage = new GameProperty("Rage", 0f);
        
        // 狂暴状态下的攻击力
        var berserkerAttack = new CombinePropertyCustom("BerserkerAttack", 50f);
        berserkerAttack.RegisterProperty(health);
        berserkerAttack.RegisterProperty(maxHealth);
        berserkerAttack.RegisterProperty(rage);
        
        berserkerAttack.Calculater = combine => {
            var currentHp = combine.GetProperty("Health").GetValue();
            var maxHp = combine.GetProperty("MaxHealth").GetValue();
            var currentRage = combine.GetProperty("Rage").GetValue();
            
            float baseAttack = combine.GetBaseValue(); // 50f
            
            // 血量越低，攻击力越高
            float healthRatio = currentHp / maxHp;
            float lowHealthBonus = 1f + (1f - healthRatio) * 2f; // 最多3倍攻击力
            
            // 狂暴值加成
            float rageBonus = 1f + currentRage * 0.01f; // 每点狂暴+1%攻击
            
            // 条件性加成：血量低于25%时激活狂暴模式
            if (healthRatio < 0.25f)
            {
                lowHealthBonus *= 1.5f; // 额外50%加成
                Debug.Log("狂暴模式激活！");
            }
            
            return baseAttack * lowHealthBonus * rageBonus;
        };
        
        manager.AddOrUpdate(berserkerAttack);
        
        // 测试条件系统
        Debug.Log($"满血攻击力: {berserkerAttack.GetValue()}");
        
        health.SetBaseValue(20f); // 血量降至20%
        rage.SetBaseValue(50f);   // 增加狂暴值
        Debug.Log($"低血量+狂暴攻击力: {berserkerAttack.GetValue()}");
    }
}
```

#### 属性注销和清理

```
// 正确的属性注销和资源清理
public void PropertyManagementExample()
{
    var customProp = new CombinePropertyCustom("TestProp", 0f);
    
    var prop1 = new GameProperty("Prop1", 10f);
    var prop2 = new GameProperty("Prop2", 20f);
    
    // 注册属性
    customProp.RegisterProperty(prop1);
    customProp.RegisterProperty(prop2);
    
    // 设置计算器
    customProp.Calculater = combine => {
        return combine.GetProperty("Prop1").GetValue() + 
               combine.GetProperty("Prop2").GetValue();
    };
    
    Debug.Log($"注册后的值: {customProp.GetValue()}"); // 30
    
    // 注销一个属性
    customProp.UnRegisterProperty(prop1);
    
    // 更新计算器以适应变化
    customProp.Calculater = combine => {
        var p2 = combine.GetProperty("Prop2");
        return p2?.GetValue() ?? 0f;
    };
    
    Debug.Log($"注销Prop1后的值: {customProp.GetValue()}"); // 20
    
    // 清理资源
    customProp.Dispose(); // 自动清理所有注册的属性和事件处理器
}
```

## 属性管理器

CombineGamePropertyManager提供了全局管理组合属性的功能，使用线程安全的设计。

### 包装 GameProperty

```
var manager = new GamePropertyManager();

// Wrap：包装单个 GameProperty（自动复制修饰符）
var baseDamage = new GameProperty("BaseDamage", 50f);
baseDamage.AddModifier(new FloatModifier(ModifierType.Add, 0, 20f));
baseDamage.AddModifier(new FloatModifier(ModifierType.Mul, 0, 1.5f));

var wrapped = manager.Wrap(baseDamage);
Debug.Log($"包装后值: {wrapped.GetValue()}");  // (50+20)*1.5 = 105
Debug.Log($"修饰符数: {wrapped.ResultHolder.ModifierCount}");  // 2

// WrapRange：批量包装
var props = new List<GameProperty>();
for (int i = 0; i < 5; i++)
{
    var prop = new GameProperty($"Prop{i}", i * 10f);
    prop.AddModifier(new FloatModifier(ModifierType.Add, 0, i * 5f));
    props.Add(prop);
}

var wrappedList = manager.WrapRange(props);
foreach (var wp in wrappedList)
{
    Debug.Log($"{wp.ID}: {wp.GetValue()}");
}
```

### 基础操作

```
// 创建管理器实例
var manager = new GamePropertyManager();

// 注册组合属性
var single = new CombinePropertySingle("SingleProp", 100f);
var custom = new CombinePropertyCustom("CustomProp", 50f);

manager.AddOrUpdate(single);
manager.AddOrUpdate(custom);

// 通过ID获取组合属性
var prop = manager.Get("SingleProp");
if (prop != null && prop.IsValid())
{
    Debug.Log($"单一属性值: {prop.GetValue()}");
}

// 类型安全的获取
var singleProp = manager.GetSingle("SingleProp");
var customProp = manager.GetCustom("CustomProp");

// 获取组合属性中的子属性
var resultHolder = manager.GetGamePropertyFromCombine("SingleProp");
var subProperty = manager.GetGamePropertyFromCombine("CustomProp", "SomeChildProp");

// 遍历所有注册的组合属性
foreach (var p in manager.GetAll())
{
    if (p.IsValid())
    {
        Debug.Log($"属性ID: {p.ID}, 当前值: {p.GetValue()}");
    }
}

// 类型过滤遍历
foreach (var single in manager.GetAllSingles())
{
    Debug.Log($"Single: {single.ID}");
}

foreach (var custom in manager.GetAllCustoms())
{
    Debug.Log($"Custom: {custom.ID}");
}

// 检查属性是否存在
if (manager.Contains("CustomProp"))
{
    Debug.Log("CustomProp 存在");
}

// 检查属性类型
if (manager.IsSingle("SingleProp"))
{
    Debug.Log("SingleProp 是 Single 类型");
}

// 获取属性总数
Debug.Log($"管理器中有 {manager.Count} 个属性");

// 移除组合属性
bool removed = manager.Remove("SingleProp");
Debug.Log($"移除操作结果: {removed}");

// 批量操作
var propsToAdd = new List<ICombineGameProperty> { single, custom };
manager.AddOrUpdateRange(propsToAdd);

var idsToRemove = new List<string> { "SingleProp", "CustomProp" };
int removedCount = manager.RemoveRange(idsToRemove);

// 清理无效属性
int cleanedCount = manager.CleanupInvalidProperties();
Debug.Log($"清理了 {cleanedCount} 个无效属性");

// 清空所有属性
manager.Clear();
```

## 修饰器系统

GameProperty系统支持多种修饰器类型，每种类型有特定的应用策略和优先级：

### 修饰器类型详解

1. **None**: 无修饰
2. **Add**: 直接添加数值
3. **AfterAdd**: 在乘法修饰后再添加数值
4. **PriorityAdd**: 按优先级添加数值
5. **Mul**: 直接乘以倍数
6. **PriorityMul**: 按优先级乘以倍数
7. **Override**: 直接覆盖属性值（忽略其他修饰器）
8. **Clamp**: 限制属性值范围

### 修饰器应用顺序

1. Override修饰器（如果存在）
2. Add和PriorityAdd修饰器（按优先级排序）
3. Mul和PriorityMul修饰器（按优先级排序）
4. AfterAdd修饰器（按优先级排序）
5. Clamp修饰器（范围限制）

```
// 创建不同类型的修饰器
var addMod = new FloatModifier(ModifierType.Add, 0, 50f);  // +50
var mulMod = new FloatModifier(ModifierType.Mul, 0, 1.5f); // ×1.5
var clampMod = new RangeModifier(ModifierType.Clamp, 0, new Vector2(0, 200)); // 限制范围0-200
var overrideMod = new FloatModifier(ModifierType.Override, 0, 100f); // 直接设为100

// 优先级影响应用顺序（数值越大优先级越高）
var highPriorityAdd = new FloatModifier(ModifierType.Add, 10, 20f); // 高优先级，先应用
var lowPriorityAdd = new FloatModifier(ModifierType.Add, 0, 10f);  // 低优先级，后应用

// 应用示例
var property = new GameProperty("Test", 100f);
property.AddModifier(addMod)     // 100 + 50 = 150
        .AddModifier(mulMod)     // 150 * 1.5 = 225
        .AddModifier(clampMod);  // min(225, 200) = 200

// 查询修饰器状态
Debug.Log($"修饰器总数: {property.ModifierCount}");
Debug.Log($"是否有加法修饰器: {property.ContainModifierOfType(ModifierType.Add)}");
Debug.Log($"加法修饰器数量: {property.GetModifierCountOfType(ModifierType.Add)}");
```

## 使用规范与最佳实践

### 系统架构

```
CombineGamePropertyManager (全局管理器)
├── CombineProperty (组合属性层)
│   ├── CombinePropertySingle (单一属性包装)
│   └── CombinePropertyCustom (自定义组合)
└── GameProperty (核心属性层)
    ├── 基础值 (BaseValue)
    ├── 修饰器 (Modifiers)
    └── 依赖关系 (Dependencies)
```

### 设计原则

1. **优先使用组合属性**: 对外暴露CombineProperty而非直接使用GameProperty
2. **合理选择依赖vs组合**:
   - 依赖用于简单的一对一关系
   - 组合用于复杂的多对一计算
3. **修饰器用于动态效果**: 临时的、可变的属性修改使用修饰器
4. **事件监听管理**: 及时添加和移除事件监听，避免内存泄漏
5. **链式调用**: 利用返回IProperty<float>的特性进行链式调用

### 何时使用依赖 vs 组合

**使用依赖的场景**:
```
// 简单一对一关系：负重 = 力量 × 5
var strength = new GameProperty("Strength", 10f);
var carryWeight = new GameProperty("CarryWeight", 0f);
carryWeight.AddDependency(strength, (dep, newVal) => newVal * 5f);

// 简单依赖链：A → B → C
var baseAttack = new GameProperty("BaseAttack", 50f);
var weaponAttack = new GameProperty("WeaponAttack", 0f);
var finalAttack = new GameProperty("FinalAttack", 0f);

weaponAttack.AddDependency(baseAttack, (dep, newVal) => newVal + 25f);
finalAttack.AddDependency(weaponAttack, (dep, newVal) => newVal * 1.2f);
```

**使用组合的场景**:
```
// 多属性组合：攻击力 = 力量×2 + 敏捷×0.5 + 等级×1.5
var customAttack = new CombinePropertyCustom("AttackPower", 0f);
customAttack.RegisterProperty(strength);
customAttack.RegisterProperty(agility);
customAttack.RegisterProperty(level);

customAttack.Calculater = (combine) => {
    var str = combine.GetProperty("Strength").GetValue();
    var agi = combine.GetProperty("Agility").GetValue();
    var lvl = combine.GetProperty("Level").GetValue();
    return str * 2f + agi * 0.5f + lvl * 1.5f;
};
```

### GameProperty使用规范

```
// ✅ 好的做法
var hp = new GameProperty("HP", 100f);

// 使用链式调用
hp.SetBaseValue(120f)
  .AddModifier(new FloatModifier(ModifierType.Add, 0, 20f))
  .AddModifier(new FloatModifier(ModifierType.Mul, 0, 1.2f));

// 使用事件监听变化
hp.OnValueChanged += (oldVal, newVal) => {
    Debug.Log($"HP从{oldVal}变为{newVal}");
    UpdateHealthBar(newVal);
};

// 查询修饰器状态
if (hp.HasModifiers)
{
    Debug.Log($"当前有{hp.ModifierCount}个修饰器");
}

// 记得清理事件监听
void OnDestroy() {
    hp.OnValueChanged -= SomeHandler;
    // 注意：GameProperty没有Dispose方法
}

// ❌ 避免的做法
// 频繁使用SetBaseValue进行动态修改
hp.SetBaseValue(hp.GetBaseValue() + 10f); // 应该使用修饰器

// 不清理事件监听导致内存泄漏
// 试图调用不存在的方法
// hp.ClearDependencies(); // 这个方法不存在
// hp.Dispose(); // GameProperty没有这个方法
```

## 高级功能

### 属性依赖管理

GameProperty支持构建复杂的属性依赖链，便于实现RPG游戏中的属性关联计算。

```
// 创建基础属性
var strength = new GameProperty("Strength", 10f);
var agility = new GameProperty("Agility", 8f);
var intelligence = new GameProperty("Intelligence", 12f);

// 创建二级属性
var attackPower = new GameProperty("AttackPower", 0f);
var attackSpeed = new GameProperty("AttackSpeed", 0f);
var spellPower = new GameProperty("SpellPower", 0f);

// 建立带计算器的依赖关系
attackSpeed.AddDependency(agility, (dep, newVal) => newVal * 0.1f + 1f);
spellPower.AddDependency(intelligence, (dep, newVal) => newVal * 3f);

// 复杂依赖关系：攻击力依赖于力量和敏捷
attackPower.AddDependency(strength);
attackPower.AddDependency(agility);

// 手动处理复杂依赖
Action updateAttackPower = () => {
    float newAttackPower = strength.GetValue() * 2f + agility.GetValue() * 0.5f;
    attackPower.SetBaseValue(newAttackPower);
};

strength.OnValueChanged += (_, __) => updateAttackPower();
agility.OnValueChanged += (_, __) => updateAttackPower();

// 初始计算
updateAttackPower();
```

### 事件系统

```
var property = new GameProperty("TestProp", 100f);

// 监听值变化
property.OnValueChanged += (oldVal, newVal) => {
    Debug.Log($"属性值从{oldVal}变为{newVal}");
};

// 监听脏标记（性能优化相关）
property.OnDirty(() => {
    Debug.Log("属性需要重新计算");
});

// 移除监听器
Action<float, float> handler = (oldVal, newVal) => { /* some logic */ };
property.OnValueChanged += handler;
property.OnValueChanged -= handler; // 记得移除

Action dirtyHandler = () => { /* some logic */ };
property.OnDirty(dirtyHandler);
property.RemoveOnDirty(dirtyHandler); // 记得移除
```

### 属性序列化

使用统一序列化服务进行属性的序列化和反序列化：

```csharp
// 序列化单个GameProperty
var prop = new GameProperty("MP", 80f);
prop.AddModifier(new FloatModifier(ModifierType.Add, 1, 10f))
    .AddModifier(new FloatModifier(ModifierType.Mul, 2, 2f));

// 使用统一序列化服务序列化到 JSON
string json = SerializationServiceManager.SerializeToJson(prop);
Debug.Log($"Serialized JSON: {json}");

// 从 JSON 反序列化
var restoredProp = SerializationServiceManager.DeserializeFromJson<GameProperty>(json);

// 验证值是否一致
float originalValue = prop.GetValue();
float deserializedValue = restoredProp.GetValue();
Debug.Assert(Mathf.Approximately(originalValue, deserializedValue));
Debug.Log($"Serialization test passed! Original: {originalValue}, Deserialized: {deserializedValue}");

// 序列化 CombinePropertySingle
var combineProp = new CombinePropertySingle("TestCombine", 50f);
combineProp.AddModifier(new FloatModifier(ModifierType.Add, 1, 20f));

string combineJson = SerializationServiceManager.SerializeToJson(combineProp);

// 反序列化 CombinePropertySingle
var restoredCombine = SerializationServiceManager.DeserializeFromJson<CombinePropertySingle>(combineJson);
Debug.Log($"CombineProperty value after deserialization: {restoredCombine.GetValue()}");

// 序列化 CombinePropertyCustom（有限制）
var customProp = new CombinePropertyCustom("CustomProp", 100f);
var subProp1 = new GameProperty("SubProp1", 50f);
var subProp2 = new GameProperty("SubProp2", 30f);

customProp.RegisterProperty(subProp1);
customProp.RegisterProperty(subProp2);
customProp.Calculater = (combine) => {
    var custom = combine as CombinePropertyCustom;
    return custom.GetProperty("SubProp1").GetValue() + 
           custom.GetProperty("SubProp2").GetValue();
};

string customJson = SerializationServiceManager.SerializeToJson(customProp);
var restoredCustom = SerializationServiceManager.DeserializeFromJson<CombinePropertyCustom>(customJson);

// 重要：需要手动重新注册属性和计算器
restoredCustom.RegisterProperty(subProp1);
restoredCustom.RegisterProperty(subProp2);
restoredCustom.Calculater = (combine) => {
    var custom = combine as CombinePropertyCustom;
    return custom.GetProperty("SubProp1").GetValue() + 
           custom.GetProperty("SubProp2").GetValue();
};

Debug.Log($"CustomProperty value after re-registration: {restoredCustom.GetValue()}");
```

**序列化限制说明**：

1. **依赖关系不被序列化**：属性之间的依赖关系（通过 `DependencyManager`）不会被序列化，需要在反序列化后手动重建。

2. **CombinePropertyCustom 的特殊限制**：
   - `Calculator` 函数无法序列化（C# 的 `Func<>` 类型不可序列化）
   - `RegisteredProperties` 不会被序列化
   - 反序列化后必须手动重新注册属性和设置计算器

3. **迁移指南**（从旧 API 迁移）：

```csharp
// 旧 API（已废弃）
var serialized = GamePropertySerializer.Serialize(prop);
var json = JsonUtility.ToJson(serialized);
var deserialized = JsonUtility.FromJson<SerializableGameProperty>(json);
var restored = GamePropertySerializer.FromSerializable(deserialized);

// 新 API（推荐使用）
var json = SerializationServiceManager.SerializeToJson(prop);
var restored = SerializationServiceManager.DeserializeFromJson<GameProperty>(json);
```

## 与其他系统集成

### 与Buff系统集成

GameProperty系统可以与Buff系统无缝集成，实现属性的动态修改。

```
// 创建一个修改力量属性的Buff
var buffData = new BuffData
{
    ID = "Buff_Strength",
    Name = "力量增益",
    Description = "增加角色的力量属性",
    Duration = 10f
};

// 创建修饰符并添加到CastModifierToProperty模块
var strengthModifier = new FloatModifier(ModifierType.Add, 0, 5f);
var propertyModule = new CastModifierToProperty(strengthModifier, "Strength");

// 设置属性管理器引用
propertyModule.CombineGamePropertyManager = combineGamePropertyManager;

buffData.BuffModules.Add(propertyModule);

// 通过BuffManager应用Buff
buffManager.CreateBuff(buffData, caster, target);
```

### 与装备系统集成

```
// 装备系统示例
public class EquipmentSystem
{
    private CombineGamePropertyManager _manager;
    
    public EquipmentSystem(CombineGamePropertyManager manager)
    {
        _manager = manager;
    }
    
    public void EquipItem(Item item, string propertyId)
    {
        var property = _manager.Get(propertyId);
        if (property != null && property.IsValid())
        {
            var subProperty = property.GetProperty("Equipment");
            if (subProperty != null)
            {
                // 添加装备提供的属性加成
                subProperty.AddModifier(new FloatModifier(
                    ModifierType.Add, 
                    item.Priority, 
                    item.GetAttributeValue(propertyId)
                ));
            }
        }
    }
    
    public void UnequipItem(Item item, string propertyId)
    {
        var property = _manager.Get(propertyId);
        if (property != null && property.IsValid())
        {
            var subProperty = property.GetProperty("Equipment");
            if (subProperty != null)
            {
                // 移除装备提供的属性加成
                subProperty.RemoveModifier(new FloatModifier(
                    ModifierType.Add, 
                    item.Priority, 
                    item.GetAttributeValue(propertyId)
                ));
            }
        }
    }
}
```

## 性能优化

### 脏标记机制

系统内置脏标记机制，避免不必要的重复计算：

```
// 监听脏标记事件进行性能调试
property.OnDirty(() => {
    Debug.Log($"属性{property.ID}被标记为脏，需要重新计算");
});

// 手动标记为脏（通常不需要）
property.MakeDirty();
```

### 最佳实践

1. **链式调用**: 利用返回IProperty<float>的特性进行链式调用
2. **批量操作**: 使用AddModifiers和RemoveModifiers进行批量操作
3. **合理使用依赖**: 避免过深的依赖链
4. **及时清理**: 移除不需要的事件监听
5. **缓存计算结果**: 对于复杂计算，考虑缓存中间结果

```
// 链式调用和批量操作示例
var strength = new GameProperty("Strength", 10f);

// 链式调用
strength.SetBaseValue(15f)
        .AddModifier(mod1)
        .AddModifier(mod2)
        .AddModifier(mod3);

// 批量操作
var modifiers = new List<IModifier> { mod1, mod2, mod3 };
strength.AddModifiers(modifiers);

// 资源清理
void OnDestroy()
{
    // 移除事件监听
    foreach (var prop in properties)
    {
        prop.OnValueChanged -= SomeHandler;
    }
    
    // 清理组合属性（有Dispose方法）
    foreach (var combine in combineProperties)
    {
        combine.Dispose();
    }
    
    properties.Clear();
    combineProperties.Clear();
}
```

通过合理组合GameProperty系统的各种功能，特别是灵活使用CombinePropertyCustom，可以构建出复杂而灵活的游戏属性系统，满足不同类型游戏的需求。系统的模块化设计使不同的属性逻辑可以分离并重复使用，方便扩展和维护。