# GameProperty 系统使用指南
** 本文由 Claude Sonnet 3.7 生成，注意甄别。**
## 目录
- [系统概述](#系统概述)
- [核心组件](#核心组件)
- [基本使用流程](#基本使用流程)
  - [创建基础属性](#创建基础属性)
  - [添加修饰器](#添加修饰器)
  - [管理属性依赖](#管理属性依赖)
- [组合属性](#组合属性)
  - [CombinePropertySingle](#combinepropertysingle)
  - [CombinePropertyClassic](#combinepropertyclassic)
  - [CombinePropertyCustom](#combinepropertycustom)
- [属性管理器](#属性管理器)
- [修饰器类型与策略](#修饰器类型与策略)
- [使用规范](#使用规范)
- [高级用法](#高级用法)
  - [属性依赖链](#属性依赖链)
  - [循环依赖检测](#循环依赖检测)
  - [脏数据追踪](#脏数据追踪)
  - [属性序列化](#属性序列化)
- [与Buff系统集成](#与buff系统集成)
- [最佳实践与性能优化](#最佳实践与性能优化)
- [常见用例](#常见用例)
  - [角色属性系统](#角色属性系统)
  - [装备加成系统](#装备加成系统)
  - [技能效果系统](#技能效果系统)

## 系统概述

GameProperty系统是一个灵活的游戏属性管理框架，专为RPG、策略等游戏类型设计。它提供了处理数值属性的各种功能，包括修饰器应用、属性依赖关系、事件监听等。系统基于组件化设计，通过不同的修饰器和属性组合方式，可以实现各种复杂的属性计算逻辑。

## 核心组件

- **GameProperty**: 单一的可修饰数值属性，支持修饰器、依赖关系和脏数据追踪
- **CombineProperty系列**: 组合多个GameProperty的不同实现方式
- **CombineGamePropertyManager**: 全局属性管理器，处理属性的注册与查询
- **修饰器(IModifier)**: 定义如何修改属性值的接口，有多种具体实现
- **GamePropertySerializer**: 处理属性的序列化与反序列化

## 基本使用流程

### 创建基础属性

```
// 创建一个基础属性，设置ID和初始值
var hp = new GameProperty("HP", 100f);

// 获取基础属性值
float baseValue = hp.GetBaseValue(); // 100

// 设置基础属性值
hp.SetBaseValue(120f);
```

### 添加修饰器

```
// 添加加法修饰器：增加20点生命值
hp.AddModifier(new FloatModifier(ModifierType.Add, 0, 20f));

// 添加乘法修饰器：增加50%生命值
hp.AddModifier(new FloatModifier(ModifierType.Mul, 0, 1.5f));

// 添加范围限制修饰器：限制生命值在0-200之间
hp.AddModifier(new RangeModifier(ModifierType.Clamp, 0, new Vector2(0, 200)));

// 获取应用所有修饰器后的最终值
float finalValue = hp.GetValue(); // 结果：min(180, 200) = 180

// 移除特定修饰器
hp.RemoveModifier(someModifier);

// 清除所有修饰器
hp.ClearModifiers();
```

### 管理属性依赖

```
// 创建两个属性
var strength = new GameProperty("Strength", 10f); // 力量
var attackPower = new GameProperty("AttackPower", 0f); // 攻击力

// 添加依赖关系：攻击力依赖于力量
attackPower.AddDependency(strength);

// 设置力量变化时更新攻击力的逻辑
strength.OnValueChanged += (oldVal, newVal) => {
    attackPower.SetBaseValue(strength.GetValue() * 2);
};

// 初始计算攻击力
attackPower.SetBaseValue(strength.GetValue() * 2);

// 当力量变化时，攻击力会自动更新
strength.SetBaseValue(15f);
float newAttack = attackPower.GetValue(); // 30
```

## 组合属性

组合属性用于将多个GameProperty以特定方式组合，提供了三种不同的实现方式。

### CombinePropertySingle

最简单的组合属性，本质上是单一GameProperty的包装器。

```
// 创建单一组合属性
var single = new CombinePropertySingle("SingleProp");

// 设置基础值
single.ResultHolder.SetBaseValue(50f);

// 添加修饰器
single.ResultHolder.AddModifier(new FloatModifier(ModifierType.Add, 0, 10f));

// 获取最终值
float value = single.GetValue(); // 60
```

### CombinePropertyClassic

经典的属性组合方式，适用于RPG游戏中常见的属性计算公式。

公式：最终属性 = (基础+加成) × (1+加成百分比) - 减益 × (1+减益百分比)

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
// 创建被共享的基础属性
var sharedProp = new GameProperty("Shared", 100f);

// 创建自定义组合属性A
var combineA = new CombinePropertyCustom("A");
combineA.RegisterProperty(sharedProp);
combineA.Calculater = c => c.GetProperty("Shared").GetValue() + 10;

// 创建自定义组合属性B
var combineB = new CombinePropertyCustom("B");
combineB.RegisterProperty(sharedProp);
combineB.Calculater = c => c.GetProperty("Shared").GetValue() * 2;

// 获取各自的计算结果
float valueA = combineA.GetValue(); // 110
float valueB = combineB.GetValue(); // 200

// 修改共享属性后，两个组合属性都会相应更新
sharedProp.SetBaseValue(50f);
valueA = combineA.GetValue(); // 60
valueB = combineB.GetValue(); // 100
```

## 属性管理器

CombineGamePropertyManager提供了全局管理组合属性的功能。

```
// 注册组合属性
CombineGamePropertyManager.AddOrUpdate(classic);
CombineGamePropertyManager.AddOrUpdate(single);

// 通过ID获取组合属性
var prop = CombineGamePropertyManager.Get("AttackPower");

// 遍历所有注册的组合属性
foreach (var p in CombineGamePropertyManager.GetAll())
{
    Debug.Log($"属性ID: {p.ID}, 当前值: {p.GetValue()}");
}

// 移除组合属性
CombineGamePropertyManager.Remove("SingleProp");
```

## 修饰器类型与策略

GameProperty系统支持多种修饰器类型，每种类型有特定的应用策略：

1. **Add**: 直接添加值
2. **PriorityAdd**: 按优先级添加值
3. **Mul**: 直接乘以值
4. **PriorityMul**: 按优先级乘以值
5. **AfterAdd**: 在乘法修饰后再添加值
6. **Override**: 直接覆盖属性值
7. **Clamp**: 限制属性值范围

```
// 创建不同类型的修饰器
var addMod = new FloatModifier(ModifierType.Add, 0, 50f);  // +50
var mulMod = new FloatModifier(ModifierType.Mul, 0, 1.5f); // ×1.5
var clampMod = new RangeModifier(ModifierType.Clamp, 0, new Vector2(0, 200)); // 限制范围0-200
var overrideMod = new FloatModifier(ModifierType.Override, 0, 100f); // 直接设为100

// 修饰器优先级影响应用顺序
var highPriorityAdd = new FloatModifier(ModifierType.Add, 10, 20f); // 高优先级，先应用
var lowPriorityAdd = new FloatModifier(ModifierType.Add, 0, 10f);  // 低优先级，后应用
```
## 使用规范
### 基本
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
•	优先使用 CombineProperty 而非直接使用 GameProperty
•	依赖用于简单的一对一关系
•	组合用于复杂的多对一计算
•	修饰器用于动态的临时效果
### 何时使用依赖 vs 组合
使用依赖的场景:
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

使用组合的场景：
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
### GameProperty的使用规范建议
- 提供有意义的ID和合理的初始值
- 使用OnValueChanged事件监听属性变化，而非轮询，记得移除事件监听
- 避免使用SetBaseValue()直接修改基础值，优先使用修饰器

## 高级用法

### 属性依赖链

GameProperty支持构建复杂的属性依赖链，便于实现RPG游戏中的属性关联计算。

```
// 创建基础属性
var strength = new GameProperty("Strength", 10f); // 力量
var agility = new GameProperty("Agility", 8f);    // 敏捷
var intelligence = new GameProperty("Intelligence", 12f); // 智力

// 创建二级属性
var attackPower = new GameProperty("AttackPower", 0f); // 攻击力 = 力量*2 + 敏捷*0.5
var attackSpeed = new GameProperty("AttackSpeed", 0f); // 攻击速度 = 敏捷*0.1 + 1
var spellPower = new GameProperty("SpellPower", 0f);   // 法术强度 = 智力*3

// 建立依赖关系
attackPower.AddDependency(strength);
attackPower.AddDependency(agility);
attackSpeed.AddDependency(agility);
spellPower.AddDependency(intelligence);

// 设置计算逻辑
strength.OnValueChanged += (_, __) => {
    attackPower.SetBaseValue(strength.GetValue() * 2 + agility.GetValue() * 0.5f);
};

agility.OnValueChanged += (_, __) => {
    attackPower.SetBaseValue(strength.GetValue() * 2 + agility.GetValue() * 0.5f);
    attackSpeed.SetBaseValue(agility.GetValue() * 0.1f + 1f);
};

intelligence.OnValueChanged += (_, __) => {
    spellPower.SetBaseValue(intelligence.GetValue() * 3);
};

// 创建三级属性：DPS(每秒伤害) = 攻击力 * 攻击速度
var dps = new GameProperty("DPS", 0f); 
dps.AddDependency(attackPower);
dps.AddDependency(attackSpeed);

attackPower.OnValueChanged += (_, __) => {
    dps.SetBaseValue(attackPower.GetValue() * attackSpeed.GetValue());
};

attackSpeed.OnValueChanged += (_, __) => {
    dps.SetBaseValue(attackPower.GetValue() * attackSpeed.GetValue());
};
```

### 循环依赖检测

系统自动检测并防止循环依赖，避免无限递归造成的崩溃。

```
var propA = new GameProperty("A", 10f);
var propB = new GameProperty("B", 20f);

// 建立依赖关系: A -> B
propA.AddDependency(propB);

// 尝试建立循环依赖: B -> A（会被系统阻止）
propB.AddDependency(propA); // 不会生效，控制台会输出警告
```

### 脏数据追踪

GameProperty通过脏标记机制，避免不必要的重复计算，提高性能。

```
// 监听属性被标记为脏的事件
property.OnDirty(() => {
    Debug.Log("属性需要重新计算");
});

// 手动将属性标记为脏（通常不需要手动调用）
property.MakeDirty();

// 移除脏数据监听
property.RemoveOnDirty(someAction);
```

### 属性序列化

GameProperty支持序列化与反序列化，便于存档和加载。

```
// 创建带修饰器的属性
var prop = new GameProperty("MP", 80f);
prop.AddModifier(new FloatModifier(ModifierType.Add, 1, 10f));
prop.AddModifier(new FloatModifier(ModifierType.Mul, 2, 2f));

// 序列化
var serialized = GamePropertySerializer.Serialize(prop);

// 反序列化
var deserialized = GamePropertySerializer.FromSerializable(serialized);

// 验证值是否一致
float originalValue = prop.GetValue();
float deserializedValue = deserialized.GetValue();
```

## 与Buff系统集成

GameProperty系统可以与Buff系统无缝集成，实现属性的动态修改。

```
// 创建一个修改力量属性的Buff
var buffData = new BuffData
{
    ID = "Buff_Strength",
    Name = "力量增益",
    Description = "增加角色的力量属性",
    Duration = 10f  // 持续10秒
};

// 创建一个修改力量属性的修饰符
var strengthModifier = new FloatModifier(ModifierType.Add, 0, 5f);  // 增加5点力量

// 创建Module并添加到BuffData
var propertyModule = new CastModifierToProperty(strengthModifier, "Strength");
buffData.BuffModules.Add(propertyModule);

// 通过BuffManager应用这个Buff
buffManager.AddBuff(buffData, caster, target);
```

## 最佳实践与性能优化

1. **优先使用组合属性**：对于复杂属性计算，优先使用CombineProperty而非直接使用GameProperty。

2. **避免频繁修改基础值**：对于静态属性（如最大生命值、基础攻击力），应通过修饰器动态调整，而非直接修改基础值。

3. **合理设置修饰器优先级**：修饰器的应用顺序可能影响最终结果，特别是在混合使用不同类型的修饰器时。

4. **慎用属性依赖**：虽然系统支持复杂的属性依赖链，但过度复杂的依赖关系可能导致维护困难和性能问题。

5. **利用脏标记机制**：系统内置的脏标记机制可以避免不必要的重复计算，提高性能。

## 常见用例

### 角色属性系统

```
// 创建基础属性
var strength = new GameProperty("Strength", 10f);
var agility = new GameProperty("Agility", 8f);
var intelligence = new GameProperty("Intelligence", 12f);

// 创建派生属性
var health = new CombinePropertyCustom("Health");
health.RegisterProperty(strength);
health.Calculater = c => c.GetProperty("Strength").GetValue() * 10;

var mana = new CombinePropertyCustom("Mana");
mana.RegisterProperty(intelligence);
mana.Calculater = c => c.GetProperty("Intelligence").GetValue() * 10;

// 注册到全局管理器
CombineGamePropertyManager.AddOrUpdate(health);
CombineGamePropertyManager.AddOrUpdate(mana);
```

### 装备加成系统
案例经供参考
```
// 角色基础属性
var baseStrength = new GameProperty("BaseStrength", 10f);

// 创建经典组合属性计算总力量
var totalStrength = new CombinePropertyClassic(
    "TotalStrength", baseStrength.GetValue(), "Base", "Equipment", "EquipmentMul", "Debuff", "DebuffMul"
);

// 装备提供的力量加成
void EquipItem(Item item)
{
    // 假设item.StrengthBonus是装备提供的力量加成
    totalStrength.GetProperty("Equipment").AddModifier(
        new FloatModifier(ModifierType.Add, item.Priority, item.StrengthBonus)
    );
    
    // 刷新显示
    UpdateUI();
}

// 卸下装备
void UnequipItem(Item item)
{
    // 移除装备提供的加成
    totalStrength.GetProperty("Equipment").RemoveModifier(
        new FloatModifier(ModifierType.Add, item.Priority, item.StrengthBonus)
    );
    
    // 刷新显示
    UpdateUI();
}
```

### 技能效果系统
案例经供参考，实际的技能系统会更加复杂
```
// 定义技能效果
void ApplyFireballEffect(Character target)
{
    // 获取目标的魔法抗性
    var magicResist = CombineGamePropertyManager.Get("MagicResist");
    float resistValue = magicResist != null ? magicResist.GetValue() : 0;
    
    // 获取施法者的法术强度
    var spellPower = CombineGamePropertyManager.Get("SpellPower");
    float spellPowerValue = spellPower != null ? spellPower.GetValue() : 0;
    
    // 计算伤害
    float baseDamage = 50;
    float finalDamage = baseDamage + spellPowerValue * 0.8f;
    finalDamage *= (1 - resistValue / 100);
    
    // 应用伤害
    target.TakeDamage(finalDamage);
    
    // 添加灼烧效果Buff
    var burnBuff = new BuffData { ID = "Burn", Duration = 3f };
    burnBuff.BuffModules.Add(new DamageOverTimeModule(finalDamage * 0.1f));
    buffManager.AddBuff(burnBuff, caster, target);
}
```

---

通过合理组合GameProperty系统的各种功能，可以构建出复杂而灵活的游戏属性系统，满足不同类型游戏的需求。系统的模块化设计使不同的属性逻辑可以分离并重复使用，方便扩展和维护。
