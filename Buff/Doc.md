# Buff系统使用指南

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
- [常见用例](#常见用例)
  - [属性修改型Buff](#属性修改型buff)
  - [定时触发效果](#定时触发效果)
  - [多层堆叠效果](#多层堆叠效果)

## 系统概述

Buff系统是一个灵活的状态效果管理框架，用于处理游戏中的各种临时状态效果（如增益、减益等）。系统基于模块化设计，可以通过组合不同的Module来实现各种复杂效果。

## 核心组件

- **BuffManager**: 负责Buff的生命周期管理和事件触发
- **Buff**: 单个Buff的实例，包含BuffData和运行时状态
- **BuffData**: Buff的静态配置数据
- **BuffModule**: 定义Buff行为的模块基类
- **各种具体Module**: 如`CastModifierToProperty`用于修改游戏属性

## 基本使用流程

### 1. 创建BuffData

```csharp
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

```csharp
// 创建一个修改力量属性的修饰符
var strengthModifier = new FloatModifier(ModifierType.Add, 0, 5f);  // 增加5点力量

// 创建Module并添加到BuffData
var propertyModule = new CastModifierToProperty(strengthModifier, "Strength");
buffData.BuffModules.Add(propertyModule);
```

### 3. 创建BuffManager和应用Buff

```csharp
// 创建BuffManager
var buffManager = new BuffManager();

// 应用Buff到目标
GameObject caster = ...; // Buff的创建者
GameObject target = ...; // Buff的目标
var buff = buffManager.AddBuff(buffData, caster, target);

// 在游戏循环中更新BuffManager
void Update()
{
    buffManager.Update(Time.deltaTime);
}
```

### 4. 管理Buff

```csharp
// 移除特定Buff
buffManager.RemoveBuff(buff);

// 移除目标上的所有Buff
buffManager.RemoveAllBuffs(target);

// 移除目标上特定ID的Buff
buffManager.RemoveBuffByID(target, "Buff_Strength");

// 移除目标上带有特定标签的Buff
buffManager.RemoveBuffsByTag(target, "Positive");

// 检查目标是否有特定Buff
bool hasBuff = buffManager.HasBuff(target, "Buff_Strength");

// 获取目标上的所有Buff
List<Buff> allBuffs = buffManager.GetAllBuffs(target);
```

## Buff的生命周期

Buff在其生命周期中会触发以下事件：

1. **OnCreate**: Buff被创建时
2. **OnTrigger**: Buff按TriggerInterval定时触发时
3. **OnUpdate**: 每帧更新时
4. **OnAddStack**: Buff堆叠增加时
5. **OnReduceStack**: Buff堆叠减少时
6. **OnRemove**: Buff被移除时

## Buff的叠加和持续时间策略

### 持续时间策略 (BuffSuperpositionDurationType)

- **Add**: 叠加持续时间
- **ResetThenAdd**: 重置持续时间后再叠加
- **Reset**: 重置持续时间
- **Keep**: 保持原有持续时间不变

### 堆叠数策略 (BuffSuperpositionStacksType)

- **Add**: 叠加堆叠数
- **ResetThenAdd**: 重置堆叠数后再叠加
- **Reset**: 重置堆叠数
- **Keep**: 保持原有堆叠数不变

### 移除策略 (BuffRemoveType)

- **All**: 完全移除Buff
- **OneStack**: 减少一层堆叠
- **Manual**: 不自动移除，需手动控制

## Modules开发指南

### 创建自定义Module

创建自定义Module需要继承`BuffModule`基类，并实现相关的回调处理：

```csharp
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

```csharp
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

```csharp
// 在合适的位置触发自定义回调
buffManager.ExecuteBuffModules(buff, BuffCallBackType.Custom, "OnTargetDamaged", damageAmount);
```

### 优先级设置

可以设置Module的优先级，控制多个Module的执行顺序：

```csharp
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

## 常见用例

### 属性修改型Buff

使用`CastModifierToProperty`模块修改角色属性：

```csharp
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
speedBuff.BuffModules.Add(speedModule);

// 应用Buff
buffManager.AddBuff(speedBuff, caster, target);
```

### 定时触发效果

创建定时触发效果的Buff（如持续伤害）：

```csharp
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
            target.TakeDamage(_damagePerTick);
        }
    }
}

// 添加Module
dotBuff.BuffModules.Add(new DamageOverTimeModule(5f));  // 每次造成5点伤害
```

### 多层堆叠效果

创建效果随堆叠层数增加的Buff：

```csharp
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

---

通过合理组合BuffData和Module，可以创建各种复杂的游戏效果。Buff系统的模块化设计使得不同的效果逻辑可以分离并重复使用，方便扩展和维护。
