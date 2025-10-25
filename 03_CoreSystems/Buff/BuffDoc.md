# Buff系统使用指南
** 本文由 Claude Sonnet 3.7 生成，注意甄别。**

## 目录
- [系统概述](#系统概述)
- [核心组件](#核心组件)
- [基本使用流程](#基本使用流程)
- [Buff的生命周期](#buff的生命周期)
- [Buff的叠加和持续时间策略](#buff的叠加和持续时间策略)
- [Modules开发指南](#modules开发指南)
  - [创建自定义Module](#创建自定义module)
  - [Module回调类型](#module回调类型)
  - [自定义回调](#自定义回调)
  - [优先级设置](#优先级设置)
- [API参考](#api参考)
  - [BuffManager方法](#buffmanager方法)
  - [查询操作](#查询操作)
  - [移除操作](#移除操作)
- [性能优化](#性能优化)
- [测试和调试](#测试和调试)
- [常见用例](#常见用例)
  - [属性修改型Buff](#属性修改型buff)
  - [定时触发效果](#定时触发效果)
  - [多层堆叠效果](#多层堆叠效果)
- [最佳实践](#最佳实践)
- [常见问题解答](#常见问题解答)

## 系统概述

Buff系统是一个灵活的状态效果管理框架，用于处理游戏中的各种临时状态效果（如增益、减益等）。系统基于模块化设计，可以通过组合不同的Module来实现各种复杂效果。
你可以查看Buff/Example/BuffExample.cs中的示例代码来获得更加直观的案例。

### 系统特性

- **模块化设计**: 通过组合不同的BuffModule实现复杂效果
- **高性能**: 使用了多种索引优化查询性能，支持批量操作
- **灵活的叠加策略**: 支持多种持续时间和堆叠数的叠加策略
- **事件驱动**: 提供完整的生命周期事件回调
- **标签和层级系统**: 支持按标签和层级批量管理Buff

## 核心组件

- **BuffManager**: 负责Buff的生命周期管理和事件触发
- **Buff**: 单个Buff的实例，包含BuffData和运行时状态
- **BuffData**: Buff的静态配置数据
- **BuffModule**: 定义Buff行为的模块基类
- **各种具体Module**: 如`CastModifierToProperty`用于修改游戏属性

## 基本使用流程

### 1. 创建BuffData

```
// 创建BuffData
var buffData = new BuffData
{
    ID = "Buff_Strength",
    Name = "力量增益",
    Description = "增加角色的力量属性",
    Duration = 10f,                  // 持续10秒
    TriggerInterval = 1f,            // 每秒触发一次
    MaxStacks = 3,                   // 最多叠加3层
    BuffSuperpositionStrategy = BuffSuperpositionDurationType.Add,  // 叠加时增加持续时间
    BuffSuperpositionStacksStrategy = BuffSuperpositionStacksType.Add,  // 叠加增加层数
    TriggerOnCreate = true           // 创建时立即触发一次
};

// 添加标签和层级（可用于分组管理）
buffData.Tags.Add("Positive");      // 正面效果标签
buffData.Layers.Add("Attribute");   // 属性层级
```

### 2. 添加Module

```
// 创建一个修改力量属性的修饰符
var strengthModifier = new FloatModifier(ModifierType.Add, 0, 5f);  // 增加5点力量

// 创建Module并添加到BuffData
var propertyModule = new CastModifierToProperty(strengthModifier, "Strength");
buffData.BuffModules.Add(propertyModule);
```

### 3. 创建BuffManager和应用Buff

```
// 创建BuffManager
var buffManager = new BuffManager();

// 应用Buff到目标 - 注意：使用CreateBuff而不是AddBuff
GameObject caster = ...; // Buff的创建者
GameObject target = ...; // Buff的目标
var buff = buffManager.CreateBuff(buffData, caster, target);

// 在游戏循环中更新BuffManager
void Update()
{
    buffManager.Update(Time.deltaTime);
}
```

### 4. 管理Buff

```
// 移除特定Buff
buffManager.RemoveBuff(buff);

// 移除目标上的所有Buff
buffManager.RemoveAllBuffs(target);

// 移除目标上特定ID的Buff
buffManager.RemoveBuffByID(target, "Buff_Strength");

// 移除目标上带有特定标签的Buff
buffManager.RemoveBuffsByTag(target, "Positive");

// 检查目标是否有特定Buff 
bool hasBuff = buffManager.ContainsBuff(target, "Buff_Strength");

// 获取目标上的所有Buff
List<Buff> allBuffs = buffManager.GetTargetBuffs(target);
```

## Buff的生命周期

Buff在其生命周期中会触发以下事件：

1. **OnCreate**: Buff被创建时
2. **OnTrigger**: Buff按TriggerInterval定时触发时
3. **OnUpdate**: 每帧更新时
4. **OnAddStack**: Buff堆叠增加时
5. **OnReduceStack**: Buff堆叠减少时
6. **OnRemove**: Buff被移除时

### 生命周期示例

```
var buff = buffManager.CreateBuff(buffData, creator, target);

// 监听生命周期事件
buff.OnCreate += (b) => Debug.Log($"Buff {b.BuffData.Name} 创建");
buff.OnTrigger += (b) => Debug.Log($"Buff {b.BuffData.Name} 触发");
buff.OnUpdate += (b) => Debug.Log($"Buff {b.BuffData.Name} 更新");
buff.OnAddStack += (b) => Debug.Log($"Buff {b.BuffData.Name} 增加堆叠");
buff.OnReduceStack += (b) => Debug.Log($"Buff {b.BuffData.Name} 减少堆叠");
buff.OnRemove += (b) => Debug.Log($"Buff {b.BuffData.Name} 移除");
```

## Buff的叠加和持续时间策略

### 持续时间策略 (BuffSuperpositionDurationType)

- **Add**: 叠加持续时间
- **ResetThenAdd**: 重置持续时间后再叠加（重置为2倍原时间）
- **Reset**: 重置持续时间
- **Keep**: 保持原有持续时间不变

### 堆叠数策略 (BuffSuperpositionStacksType)

- **Add**: 叠加堆叠数
- **ResetThenAdd**: 重置堆叠数为1后再叠加
- **Reset**: 重置堆叠数为1
- **Keep**: 保持原有堆叠数不变

### 移除策略 (BuffRemoveType)

- **All**: 完全移除Buff
- **OneStack**: 减少一层堆叠
- **Manual**: 不自动移除，需手动控制

### 策略组合示例

```
// 创建一个叠加时间和层数的Buff
var buffData = new BuffData
{
    ID = "StackingBuff",
    Duration = 5f,
    MaxStacks = 3,
    BuffSuperpositionStrategy = BuffSuperpositionDurationType.Add,     // 时间叠加
    BuffSuperpositionStacksStrategy = BuffSuperpositionStacksType.Add, // 层数叠加
    BuffRemoveStrategy = BuffRemoveType.OneStack                       // 逐层移除
};
```

## Modules开发指南

### 创建自定义Module

创建自定义Module需要继承`BuffModule`基类，并实现相关的回调处理：

```
public class MyCustomBuffModule : BuffModule
{
    public MyCustomBuffModule()
    {
        // 注册对特定回调类型感兴趣
        RegisterCallback(BuffCallBackType.OnCreate, OnCreate);
        RegisterCallback(BuffCallBackType.OnRemove, OnRemove);
        RegisterCallback(BuffCallBackType.OnTick, OnTick);
    }

    private void OnCreate(Buff buff, object[] parameters)
    {
        // Buff创建时的逻辑
        Debug.Log($"Buff {buff.BuffData.Name} 已创建!");
        
        // 可以访问buff的各种属性
        GameObject target = buff.Target;
        int currentStacks = buff.CurrentStacks;
        
        // 执行自定义逻辑...
    }

    private void OnRemove(Buff buff, object[] parameters)
    {
        // Buff移除时的逻辑
        Debug.Log($"Buff {buff.BuffData.Name} 已移除!");
        
        // 清理资源或状态...
    }
    
    private void OnTick(Buff buff, object[] parameters)
    {
        // Buff定时触发时的逻辑
        Debug.Log($"Buff {buff.BuffData.Name} 触发效果!");
        
        // 例如：每次触发造成伤害
        // DamageSystem.ApplyDamage(buff.Target, 10f);
    }
}
```

### Module回调类型

`BuffCallBackType`枚举定义了以下回调类型：

- **OnCreate**: Buff创建时
- **OnRemove**: Buff移除时
- **OnAddStack**: Buff堆叠增加时
- **OnReduceStack**: Buff堆叠减少时
- **OnUpdate**: 每帧更新时
- **OnTick**: Buff按间隔触发时
- **Custom**: 自定义回调

### 自定义回调

除了标准回调外，还可以注册自定义回调：

```
public class AdvancedBuffModule : BuffModule
{
    public AdvancedBuffModule()
    {
        // 注册标准回调
        RegisterCallback(BuffCallBackType.OnCreate, OnCreate);
        
        // 注册自定义回调
        RegisterCustomCallback("OnTargetDamaged", OnTargetDamaged);
        RegisterCustomCallback("OnSkillCast", OnSkillCast);
    }
    
    private void OnCreate(Buff buff, object[] parameters)
    {
        // 常规创建逻辑
    }
    
    private void OnTargetDamaged(Buff buff, object[] parameters)
    {
        // 当目标受伤时的特殊处理
        float damageAmount = (float)parameters[0];
        Debug.Log($"Buff响应伤害事件: {damageAmount}");
        
        // 特殊效果...
    }
    
    private void OnSkillCast(Buff buff, object[] parameters)
    {
        // 当技能施放时的特殊处理
        string skillId = (string)parameters[0];
        Debug.Log($"Buff响应技能施放: {skillId}");
        
        // 特殊效果...
    }
}
```

在游戏代码中触发自定义回调：

```
// 在合适的位置触发自定义回调
// 注意：使用InvokeBuffModules
buffManager.InvokeBuffModules(buff, BuffCallBackType.Custom, "OnTargetDamaged", damageAmount);
```

### 优先级设置

可以设置Module的优先级，控制多个Module的执行顺序：

```
public class HighPriorityModule : BuffModule
{
    public HighPriorityModule()
    {
        // 设置高优先级，将会先于低优先级模块执行
        Priority = 100;
        
        RegisterCallback(BuffCallBackType.OnCreate, OnCreate);
    }
    
    private void OnCreate(Buff buff, object[] parameters)
    {
        // 先执行的逻辑
        Debug.Log("高优先级模块执行");
    }
}

public class LowPriorityModule : BuffModule
{
    public LowPriorityModule()
    {
        // 设置低优先级，将会后于高优先级模块执行
        Priority = 0;
        
        RegisterCallback(BuffCallBackType.OnCreate, OnCreate);
    }
    
    private void OnCreate(Buff buff, object[] parameters)
    {
        // 后执行的逻辑
        Debug.Log("低优先级模块执行");
    }
}
```

## API参考

### BuffManager方法

#### 创建和添加
- `CreateBuff(BuffData buffData, GameObject creator, GameObject target)`: 创建新Buff
- `FlushPendingRemovals()`: 立即处理所有待移除的Buff

#### 查询操作
- `ContainsBuff(object target, string buffID)`: 检查目标是否有指定ID的Buff
- `GetBuff(object target, string buffID)`: 获取目标上指定ID的Buff
- `GetTargetBuffs(object target)`: 获取目标上的所有Buff
- `GetBuffsByTag(object target, string tag)`: 获取目标上带有指定标签的Buff
- `GetBuffsByLayer(object target, string layer)`: 获取目标上指定层级的Buff

#### 全局查询
- `GetAllBuffsByID(string buffID)`: 获取所有指定ID的Buff（跨所有目标）
- `GetAllBuffsByTag(string tag)`: 获取所有带有指定标签的Buff
- `GetAllBuffsByLayer(string layer)`: 获取所有指定层级的Buff
- `ContainsBuffWithID(string buffID)`: 检查是否存在指定ID的Buff
- `ContainsBuffWithTag(string tag)`: 检查是否存在带有指定标签的Buff
- `ContainsBuffWithLayer(string layer)`: 检查是否存在指定层级的Buff

#### 移除操作
- `RemoveBuff(Buff buff)`: 移除指定Buff
- `RemoveAllBuffs(object target)`: 移除目标上的所有Buff
- `RemoveBuffByID(object target, string buffID)`: 移除目标上指定ID的Buff
- `RemoveBuffsByTag(object target, string tag)`: 移除目标上带有指定标签的Buff
- `RemoveBuffsByLayer(object target, string layer)`: 移除目标上指定层级的Buff
- `RemoveAllBuffsByID(string buffID)`: 移除所有指定ID的Buff
- `RemoveAllBuffsByTag(string tag)`: 移除所有带有指定标签的Buff
- `RemoveAllBuffsByLayer(string layer)`: 移除所有指定层级的Buff

#### 更新
- `Update(float deltaTime)`: 更新所有Buff的时间和状态

## 性能优化

BuffManager内置了多项性能优化：

### 索引优化
- **ID索引**: 按Buff ID快速查找
- **标签索引**: 按标签快速查找相关Buff
- **层级索引**: 按层级快速查找相关Buff
- **位置索引**: 使用O(1)的swap-remove算法快速移除

### 批量操作
- **批量移除**: 所有移除操作都会被收集并批量处理
- **分类存储**: 按生命周期（有限时间vs永久）分类存储，提高更新效率
- **缓存重用**: 重用临时集合减少GC压力

### 最佳实践
```
// 好的做法：批量操作
buffManager.RemoveBuffsByTag(target, "Debuff");
buffManager.FlushPendingRemovals(); // 立即处理批量移除

// 避免的做法：逐个移除
foreach(var buff in buffs)
{
    buffManager.RemoveBuff(buff); // 每次都会触发批量处理
}
```

## 常见用例

### 属性修改型Buff

使用`CastModifierToProperty`模块修改角色属性：

```
// 创建增加移动速度20%的Buff
var speedBuff = new BuffData
{
    ID = "Speed_Boost",
    Name = "疾跑",
    Duration = 5f
};

// 创建乘法修饰符（增加20%）
var speedModifier = new FloatModifier(ModifierType.Mul, 1, 1.2f);

// 创建并添加Module
var speedModule = new CastModifierToProperty(speedModifier, "MovementSpeed");
speedModule.CombineGamePropertyManager = combineGamePropertyManager; // 重要：设置属性管理器
speedBuff.BuffModules.Add(speedModule);

// 应用Buff
buffManager.CreateBuff(speedBuff, caster, target);
```

### 定时触发效果

创建定时触发效果的Buff（如持续伤害）：

```
// 创建持续伤害Buff
var dotBuff = new BuffData
{
    ID = "Poison",
    Name = "中毒",
    Duration = 10f,
    TriggerInterval = 1f,  // 每秒触发一次
};

// 创建自定义Module处理伤害逻辑
public class DamageOverTimeModule : BuffModule
{
    private float _damagePerTick;
    
    public DamageOverTimeModule(float damagePerTick)
    {
        _damagePerTick = damagePerTick;
        RegisterCallback(BuffCallBackType.OnTick, OnTick);
    }
    
    private void OnTick(Buff buff, object[] parameters)
    {
        // 造成伤害
        var target = buff.Target.GetComponent<Health>();
        if (target != null)
        {
            target.TakeDamage(_damagePerTick * buff.CurrentStacks); // 考虑堆叠效果
        }
    }
}

// 添加Module
dotBuff.BuffModules.Add(new DamageOverTimeModule(5f));  // 每次造成5点伤害
```

### 多层堆叠效果

创建效果随堆叠层数增加的Buff：

```
// 创建可堆叠的攻击力Buff
var stackableBuff = new BuffData
{
    ID = "Rage",
    Name = "怒气",
    MaxStacks = 5,  // 最多5层
    Duration = 8f,
    BuffSuperpositionStrategy = BuffSuperpositionDurationType.Reset,  // 重置持续时间
    BuffSuperpositionStacksStrategy = BuffSuperpositionStacksType.Add,  // 叠加层数
};

// 创建会根据堆叠数增加效果的模块
public class StackableEffectModule : BuffModule
{
    private float _baseEffect;
    
    public StackableEffectModule(float baseEffect)
    {
        _baseEffect = baseEffect;
        RegisterCallback(BuffCallBackType.OnCreate, ApplyEffect);
        RegisterCallback(BuffCallBackType.OnAddStack, ApplyEffect);
        RegisterCallback(BuffCallBackType.OnReduceStack, ApplyEffect);
        RegisterCallback(BuffCallBackType.OnRemove, RemoveEffect);
    }
    
    private void ApplyEffect(Buff buff, object[] parameters)
    {
        // 移除旧效果
        RemoveEffect(buff, parameters);
        
        // 计算当前效果值
        float currentEffect = _baseEffect * buff.CurrentStacks;
        
        // 应用效果
        var attackPower = CombineGamePropertyManager.GetGameProperty("AttackPower");
        if (attackPower != null)
        {
            var modifier = new FloatModifier(ModifierType.Add, 0, currentEffect);
            attackPower.AddModifier(modifier);
            
            // 存储modifier以便后续移除
            buff.BuffData.CustomData["CurrentModifier"] = modifier;
        }
    }
    
    private void RemoveEffect(Buff buff, object[] parameters)
    {
        var attackPower = CombineGamePropertyManager.GetGameProperty("AttackPower");
        if (attackPower != null && buff.BuffData.CustomData.TryGetValue("CurrentModifier", out object modObj))
        {
            var modifier = modObj as IModifier;
            attackPower.RemoveModifier(modifier);
        }
    }
}

// 添加Module
stackableBuff.BuffModules.Add(new StackableEffectModule(5f));  // 每层增加5点攻击力
```

## 最佳实践

### 1. Buff设计原则
- **单一职责**: 每个BuffModule只负责一个特定功能
- **数据驱动**: 通过BuffData配置而非硬编码
- **可复用性**: 设计通用的Module以便在不同Buff中重用

### 2. 性能考虑
- **批量操作**: 优先使用批量移除方法
- **合理的更新频率**: 根据需要设置TriggerInterval
- **及时清理**: 使用FlushPendingRemovals()及时处理移除

### 3. 错误处理
```
// 安全的Buff创建
public Buff SafeCreateBuff(BuffData buffData, GameObject creator, GameObject target)
{
    if (buffData == null)
    {
        Debug.LogError("BuffData不能为null");
        return null;
    }
    
    if (target == null)
    {
        Debug.LogError("目标对象不能为null");
        return null;
    }
    
    return buffManager.CreateBuff(buffData, creator, target);
}
```

### 4. Module配置
```
// 为CastModifierToProperty设置属性管理器
var propertyModule = new CastModifierToProperty(modifier, propertyId);
propertyModule.CombineGamePropertyManager = combineGamePropertyManager;
```

## 常见问题解答

### Q: 为什么我的Buff没有生效？
A: 检查以下几点：
1. BuffModule是否正确添加到BuffData
2. CastModifierToProperty是否设置了CombineGamePropertyManager
3. 目标属性是否已在CombineGamePropertyManager中注册
4. Buff是否被正确创建（检查返回值）

### Q: 如何处理Buff的持久化？
A: 可以序列化BuffData和当前状态：
```
// 保存Buff状态
var buffState = new BuffSaveData
{
    BuffDataID = buff.BuffData.ID,
    CurrentStacks = buff.CurrentStacks,
    DurationTimer = buff.DurationTimer,
    TriggerTimer = buff.TriggerTimer
};
```

### Q: 如何实现条件触发的Buff？
A: 使用自定义回调：
```
// 在合适的时机触发
if (healthPercentage < 0.3f)
{
    buffManager.InvokeBuffModules(buff, BuffCallBackType.Custom, "OnLowHealth");
}
```

---

通过合理组合BuffData和Module，可以创建各种复杂的游戏效果。Buff系统的模块化设计使得不同的效果逻辑可以分离并重复使用，方便扩展和维护。

