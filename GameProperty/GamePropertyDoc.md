# GameProperty 系统使用指南
** 本文由 Claude Sonnet 3.7 生成，注意甄别。**

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
  - [CombinePropertyClassic](#combinepropertyclassic)
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

GameProperty系统是一个灵活的游戏属性管理框架，专为RPG、策略等游戏类型设计。它提供了处理数值属性的各种功能，包括修饰器应用、属性依赖关系、事件监听、序列化等。系统基于组件化设计，通过不同的修饰器和属性组合方式，可以实现各种复杂的属性计算逻辑。

### 系统特性

- **模块化设计**: 通过不同组件组合实现复杂属性逻辑
- **修饰器系统**: 支持多种修饰器类型和优先级
- **属性依赖**: 支持属性间的依赖关系和自动更新
- **事件驱动**: 提供完整的属性变化事件监听
- **序列化支持**: 内置序列化和反序列化功能
- **性能优化**: 包含脏标记机制和缓存优化

## 核心组件

- **GameProperty**: 单一的可修饰数值属性，支持修饰器、依赖关系和事件
- **CombineProperty系列**: 组合多个GameProperty的不同实现方式
- **CombineGamePropertyManager**: 全局属性管理器，处理属性的注册与查询
- **修饰器(IModifier)**: 定义如何修改属性值的接口，有多种具体实现
- **GamePropertySerializer**: 处理属性的序列化与反序列化

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
```

#### CombinePropertyClassic
```
CombinePropertyClassic(string id, float baseValue,         // 构造函数
    string basePropertyId, string buffPropertyId, 
    string buffMulPropertyId, string debuffPropertyId, 
    string debuffMulPropertyId)
```

#### CombinePropertyCustom
```
CombinePropertyCustom(string id)                           // 构造函数
void RegisterProperty(GameProperty gameProperty)           // 注册属性
void UnRegisterProperty(GameProperty gameProperty)         // 注销属性
Func<ICombineGameProperty, float> Calculater { get; set; } // 计算函数
```

### CombineGamePropertyManager API

#### 静态方法
```
void AddOrUpdate(ICombineGameProperty property)                        // 添加或更新属性
ICombineGameProperty Get(string id)                                   // 获取属性
GameProperty GetGamePropertyFromCombine(string combinePropertyID, string id = "") // 获取子属性
bool Remove(string id)                                                 // 移除属性
IEnumerable<ICombineGameProperty> GetAll()                            // 获取所有属性
void Clear()                                                           // 清空所有属性
int CleanupInvalidProperties()                                        // 清理无效属性
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

#### GamePropertySerializer
```
static SerializableGameProperty Serialize(GameProperty property)              // 序列化
static GameProperty FromSerializable(SerializableGameProperty serializable)  // 反序列化
```

#### CombineGamePropertySerializer  
```
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
strength.OnValueChanged += (oldVal, newVal) => {
    // 复杂计算：攻击力 = 力量×2 + 敏捷×0.5
    float newAttackPower = strength.GetValue() * 2f + agility.GetValue() * 0.5f;
    attackPower.SetBaseValue(newAttackPower);
};
```

## 组合属性详解

组合属性用于将多个GameProperty以特定方式组合，提供了三种不同的实现方式。

### CombinePropertySingle

最简单的组合属性，本质上是单一GameProperty的包装器。

```
// 创建单一组合属性
var single = new CombinePropertySingle("SingleProp", 50f);

// 访问内部属性
single.ResultHolder.AddModifier(new FloatModifier(ModifierType.Add, 0, 10f));

// 或者使用包装方法
single.AddModifierToHolder(new FloatModifier(ModifierType.Add, 0, 5f));

// 获取最终值
float value = single.GetValue(); // 65

// 监听值变化
single.OnValueChanged += (oldVal, newVal) => {
    Debug.Log($"属性值从{oldVal}变为{newVal}");
};

// 注册到管理器
CombineGamePropertyManager.AddOrUpdate(single);
```

### CombinePropertyClassic

经典的属性组合方式，适用于RPG游戏中常见的属性计算公式。

**公式**: `最终属性 = (基础+加成) × (1+加成百分比) - 减益 × (1+减益百分比)`

```
// 创建经典组合属性
var classic = new CombinePropertyClassic(
    "AttackPower", // ID
    50f,           // 初始基础值
    "Base",        // 基础属性名
    "Buff",        // Buff属性名
    "BuffMul",     // Buff百分比名
    "Debuff",      // Debuff属性名
    "DebuffMul"    // Debuff百分比名
);

// 设置各部分值
classic.GetProperty("Base").AddModifier(new FloatModifier(ModifierType.Add, 0, 20f)); // 武器加成
classic.GetProperty("Buff").SetBaseValue(10f);  // Buff加成
classic.GetProperty("BuffMul").SetBaseValue(0.2f);  // Buff百分比(+20%)
classic.GetProperty("Debuff").SetBaseValue(5f);  // Debuff减益
classic.GetProperty("DebuffMul").SetBaseValue(0.5f);  // Debuff百分比(+50%)

// 计算最终值: (50+20+10)*(1+0.2) - 5*(1+0.5) = 80*1.2 - 5*1.5 = 96 - 7.5 = 88.5
float finalAttack = classic.GetValue(); // 88.5
```

### CombinePropertyCustom

完全自定义的组合方式，通过委托函数灵活定义属性组合逻辑。

```
// 创建共享的基础属性
var strength = new GameProperty("Strength", 10f);
var agility = new GameProperty("Agility", 8f);
var level = new GameProperty("Level", 5f);

// 创建自定义组合属性：攻击力 = 力量×2 + 敏捷×0.5 + 等级×1.5
var customAttack = new CombinePropertyCustom("CustomAttack");
customAttack.RegisterProperty(strength);
customAttack.RegisterProperty(agility);
customAttack.RegisterProperty(level);

customAttack.Calculater = (combine) => {
    var str = combine.GetProperty("Strength").GetValue();
    var agi = combine.GetProperty("Agility").GetValue();
    var lvl = combine.GetProperty("Level").GetValue();
    return str * 2f + agi * 0.5f + lvl * 1.5f;
};

// 获取计算结果
float attackValue = customAttack.GetValue(); // 10*2 + 8*0.5 + 5*1.5 = 31.5

// 修改基础属性后自动更新
strength.SetBaseValue(15f);
attackValue = customAttack.GetValue(); // 15*2 + 8*0.5 + 5*1.5 = 41.5

// 注销不需要的属性
customAttack.UnRegisterProperty(level);
```

## 属性管理器

CombineGamePropertyManager提供了全局管理组合属性的功能，使用线程安全的设计。

```
// 注册组合属性
CombineGamePropertyManager.AddOrUpdate(classic);
CombineGamePropertyManager.AddOrUpdate(single);

// 通过ID获取组合属性
var prop = CombineGamePropertyManager.Get("AttackPower");
if (prop != null && prop.IsValid())
{
    Debug.Log($"攻击力: {prop.GetValue()}");
}

// 获取组合属性中的子属性
var baseProperty = CombineGamePropertyManager.GetGamePropertyFromCombine("AttackPower", "Base");

// 遍历所有注册的组合属性
foreach (var p in CombineGamePropertyManager.GetAll())
{
    if (p.IsValid())
    {
        Debug.Log($"属性ID: {p.ID}, 当前值: {p.GetValue()}");
    }
}

// 移除组合属性
bool removed = CombineGamePropertyManager.Remove("SingleProp");

// 清理无效属性
int cleanedCount = CombineGamePropertyManager.CleanupInvalidProperties();

// 清空所有属性
CombineGamePropertyManager.Clear();
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
│   ├── CombinePropertyClassic (经典RPG公式)
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
var customAttack = new CombinePropertyCustom("AttackPower");
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

```
// 序列化单个GameProperty
var prop = new GameProperty("MP", 80f);
prop.AddModifier(new FloatModifier(ModifierType.Add, 1, 10f))
    .AddModifier(new FloatModifier(ModifierType.Mul, 2, 2f));

var serialized = GamePropertySerializer.Serialize(prop);
var json = JsonUtility.ToJson(serialized);

// 反序列化
var deserialized = JsonUtility.FromJson<SerializableGameProperty>(json);
var restoredProp = GamePropertySerializer.FromSerializable(deserialized);

// 验证值是否一致
float originalValue = prop.GetValue();
float deserializedValue = restoredProp.GetValue();
Debug.Assert(Mathf.Approximately(originalValue, deserializedValue));

// 序列化组合属性
var combineProp = new CombinePropertySingle("TestCombine", 50f);
var combineSerialized = CombineGamePropertySerializer.Serialize(combineProp);
var combineJson = JsonUtility.ToJson(combineSerialized);

// 反序列化组合属性
var combineDeserialized = JsonUtility.FromJson<SerializableCombineGameProperty>(combineJson);
var restoredCombine = CombineGamePropertySerializer.FromSerializable(combineDeserialized);
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
    public void EquipItem(Item item, string propertyId)
    {
        var property = CombineGamePropertyManager.Get(propertyId);
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
        var property = CombineGamePropertyManager.Get(propertyId);
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
// ✅ 链式调用和批量操作示例
var strength = new GameProperty("Strength", 10f);

// 链式调用
strength.SetBaseValue(15f)
        .AddModifier(mod1)
        .AddModifier(mod2)
        .AddModifier(mod3);

// 批量操作
var modifiers = new List<IModifier> { mod1, mod2, mod3 };
strength.AddModifiers(modifiers);

// ✅ 资源清理
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

## 常见用例

### 角色属性系统

```
public class CharacterAttributes
{
    // 基础属性
    public CombinePropertySingle Strength { get; private set; }
    public CombinePropertySingle Agility { get; private set; }
    public CombinePropertySingle Intelligence { get; private set; }
    
    // 派生属性
    public CombinePropertyCustom Health { get; private set; }
    public CombinePropertyCustom Mana { get; private set; }
    public CombinePropertyCustom AttackPower { get; private set; }
    
    public CharacterAttributes()
    {
        // 创建基础属性
        Strength = new CombinePropertySingle("Strength", 10f);
        Agility = new CombinePropertySingle("Agility", 8f);
        Intelligence = new CombinePropertySingle("Intelligence", 12f);
        
        // 创建派生属性
        Health = new CombinePropertyCustom("Health");
        Health.RegisterProperty(Strength.ResultHolder);
        Health.Calculater = c => c.GetProperty("Strength").GetValue() * 10;
        
        Mana = new CombinePropertyCustom("Mana");
        Mana.RegisterProperty(Intelligence.ResultHolder);
        Mana.Calculater = c => c.GetProperty("Intelligence").GetValue() * 10;
        
        AttackPower = new CombinePropertyCustom("AttackPower");
        AttackPower.RegisterProperty(Strength.ResultHolder);
        AttackPower.RegisterProperty(Agility.ResultHolder);
        AttackPower.Calculater = c => {
            var str = c.GetProperty("Strength").GetValue();
            var agi = c.GetProperty("Agility").GetValue();
            return str * 2f + agi * 0.5f;
        };
        
        // 注册到全局管理器
        CombineGamePropertyManager.AddOrUpdate(Strength);
        CombineGamePropertyManager.AddOrUpdate(Agility);
        CombineGamePropertyManager.AddOrUpdate(Intelligence);
        CombineGamePropertyManager.AddOrUpdate(Health);
        CombineGamePropertyManager.AddOrUpdate(Mana);
        CombineGamePropertyManager.AddOrUpdate(AttackPower);
    }
    
    public void Dispose()
    {
        // 清理组合属性
        Strength?.Dispose();
        Agility?.Dispose();
        Intelligence?.Dispose();
        Health?.Dispose();
        Mana?.Dispose();
        AttackPower?.Dispose();
    }
}
```

### 装备加成系统

```
public class EquipmentManager
{
    private Dictionary<string, CombinePropertyClassic> _attributes;
    
    public EquipmentManager()
    {
        _attributes = new Dictionary<string, CombinePropertyClassic>();
        
        // 创建经典组合属性用于装备系统
        var totalStrength = new CombinePropertyClassic(
            "TotalStrength", 10f, "Base", "Equipment", "EquipmentMul", "Debuff", "DebuffMul"
        );
        
        _attributes["Strength"] = totalStrength;
        CombineGamePropertyManager.AddOrUpdate(totalStrength);
    }
    
    public void EquipItem(EquipmentItem item)
    {
        foreach (var bonus in item.AttributeBonuses)
        {
            if (_attributes.TryGetValue(bonus.AttributeName, out var attribute))
            {
                var equipProp = attribute.GetProperty("Equipment");
                equipProp?.AddModifier(new FloatModifier(
                    ModifierType.Add, 
                    item.Priority, 
                    bonus.Value
                ));
                
                // 如果有百分比加成
                if (bonus.PercentageBonus > 0)
                {
                    var equipMulProp = attribute.GetProperty("EquipmentMul");
                    equipMulProp?.AddModifier(new FloatModifier(
                        ModifierType.Add, 
                        item.Priority, 
                        bonus.PercentageBonus
                    ));
                }
            }
        }
    }
    
    public void UnequipItem(EquipmentItem item)
    {
        foreach (var bonus in item.AttributeBonuses)
        {
            if (_attributes.TryGetValue(bonus.AttributeName, out var attribute))
            {
                var equipProp = attribute.GetProperty("Equipment");
                equipProp?.RemoveModifier(new FloatModifier(
                    ModifierType.Add, 
                    item.Priority, 
                    bonus.Value
                ));
                
                if (bonus.PercentageBonus > 0)
                {
                    var equipMulProp = attribute.GetProperty("EquipmentMul");
                    equipMulProp?.RemoveModifier(new FloatModifier(
                        ModifierType.Add, 
                        item.Priority, 
                        bonus.PercentageBonus
                    ));
                }
            }
        }
    }
    
    public void Dispose()
    {
        foreach (var attribute in _attributes.Values)
        {
            attribute.Dispose();
        }
        _attributes.Clear();
    }
}
```

### 技能效果系统

```
public class SkillSystem
{
    public void ApplyFireballSkill(Character caster, Character target)
    {
        // 获取施法者的法术强度
        var spellPower = CombineGamePropertyManager.Get("SpellPower");
        float spellPowerValue = spellPower?.GetValue() ?? 0;
        
        // 获取目标的魔法抗性
        var magicResist = CombineGamePropertyManager.Get("MagicResist");
        float resistValue = magicResist?.GetValue() ?? 0;
        
        // 计算伤害
        float baseDamage = 50;
        float finalDamage = baseDamage + spellPowerValue * 0.8f;
        finalDamage *= (1 - resistValue / 100);
        
        // 应用伤害
        target.TakeDamage(finalDamage);
        
        // 添加灼烧效果Buff（与Buff系统集成）
        ApplyBurnEffect(caster, target, finalDamage * 0.1f);
    }
    
    private void ApplyBurnEffect(Character caster, Character target, float dotDamage)
    {
        var burnBuff = new BuffData 
        { 
            ID = "Burn", 
            Duration = 3f,
            TriggerInterval = 1f
        };
        
        // 这里会需要自定义的DOT模块
        // burnBuff.BuffModules.Add(new DamageOverTimeModule(dotDamage));
        
        // buffManager.CreateBuff(burnBuff, caster.gameObject, target.gameObject);
    }
}
```

## 故障排除

### 常见问题

1. **属性值不更新**
   - 检查是否正确设置了OnValueChanged事件监听
   - 确认依赖关系是否正确建立（使用带计算器的AddDependency）
   - 验证修饰器是否正确添加

2. **内存泄漏**
   - 确保在适当时机移除事件监听
   - 对CombineProperty调用Dispose()方法释放资源
   - 检查依赖关系是否形成循环引用

3. **性能问题**
   - 避免过深的依赖链
   - 使用批量操作（AddModifiers/RemoveModifiers）
   - 使用脏标记机制监控性能热点

4. **序列化失败**
   - 确保所有相关类型都可序列化
   - 检查修饰器类型是否支持序列化

5. **API调用错误**
   - GameProperty没有Dispose方法，只有CombineProperty有
   - 使用ModifierCount属性而不是GetModifierCount()方法
   - 没有ClearDependencies()方法

### 调试技巧

```
// 启用详细日志
var property = new GameProperty("Debug", 100f);
property.OnValueChanged += (oldVal, newVal) => {
    Debug.Log($"[DEBUG] {property.ID}: {oldVal} -> {newVal}");
};

property.OnDirty(() => {
    Debug.Log($"[DEBUG] {property.ID} marked as dirty");
});

// 检查修饰器状态
Debug.Log($"修饰器数量: {property.ModifierCount}");
Debug.Log($"是否有修饰器: {property.HasModifiers}");

foreach (var modifier in property.Modifiers)
{
    Debug.Log($"修饰器: {modifier.Type}, 优先级: {modifier.Priority}");
}

// 检查特定类型修饰器
foreach (ModifierType type in Enum.GetValues(typeof(ModifierType)))
{
    if (property.ContainModifierOfType(type))
    {
        int count = property.GetModifierCountOfType(type);
        Debug.Log($"{type}类型修饰器数量: {count}");
    }
}

// 验证组合属性有效性
var combineProperty = CombineGamePropertyManager.Get("SomeProperty");
if (combineProperty != null)
{
    Debug.Log($"组合属性有效性: {combineProperty.IsValid()}");
    Debug.Log($"当前值: {combineProperty.GetValue()}");
    Debug.Log($"基础值: {combineProperty.GetBaseValue()}");
}
```

---

通过合理组合GameProperty系统的各种功能，可以构建出复杂而灵活的游戏属性系统，满足不同类型游戏的需求。系统的模块化设计使不同的属性逻辑可以分离并重复使用，方便扩展和维护。
