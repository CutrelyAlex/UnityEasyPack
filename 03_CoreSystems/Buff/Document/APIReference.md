# Buff System - API Reference

**适用EasyPack版本：** EasyPack v1.5.30
**最后更新：** 2025-10-26

---

## 目录

- [核心类](#核心类)
- [枚举类型](#枚举类型)
- [使用示例](#使用示例)
- [相关资源](#相关资源)

---

## 核心类

### Buff

**命名空间：** `EasyPack.BuffSystem`  
**继承：** `object`

代表一个应用到游戏对象的 Buff 实例，包含生命周期管理和事件系统。

#### 构造函数

```csharp
public Buff(BuffData buffData, GameObject creator, GameObject target)
```

创建一个新的 Buff 实例。

| 参数 | 类型 | 说明 |
|------|------|------|
| `buffData` | `BuffData` | Buff 的配置数据 |
| `creator` | `GameObject` | 施加 Buff 的对象（施法者） |
| `target` | `GameObject` | 接受 Buff 的对象（目标） |

**返回值：** `Buff` 实例

---

#### 属性

##### BuffData
```csharp
public BuffData BuffData { get; }
```

获取 Buff 的配置数据（只读）。

**类型：** `BuffData`  
**说明：** 包含 Buff 的静态属性（ID、名称、持续时间等）

---

##### Creator
```csharp
public GameObject Creator { get; }
```

获取施加此 Buff 的游戏对象（只读）。

**类型：** `GameObject`  
**说明：** 施法者，可能为 `null`

---

##### Target
```csharp
public GameObject Target { get; }
```

获取接受此 Buff 的游戏对象（只读）。

**类型：** `GameObject`  
**说明：** Buff 的目标对象，不能为 `null`

---

##### CurrentStacks
```csharp
public int CurrentStacks { get; set; }
```

获取或设置当前堆叠层数。

**类型：** `int`  
**默认值：** `1`  
**说明：** 范围 [0, BuffData.MaxStacks]，设置为 0 会触发移除

---

##### DurationTimer
```csharp
public float DurationTimer { get; set; }
```

获取或设置剩余持续时间（秒）。

**类型：** `float`  
**默认值：** `BuffData.Duration`  
**说明：** 
- `-1` 表示永久 Buff
- `> 0` 表示剩余时间
- `<= 0` 会触发自动移除

---

##### TriggerTimer
```csharp
public float TriggerTimer { get; set; }
```

获取或设置触发计时器（秒）。

**类型：** `float`  
**默认值：** `BuffData.TriggerInterval`  
**说明：** 用于周期性触发 `OnTrigger` 事件

---

##### IsPermanent
```csharp
public bool IsPermanent { get; }
```

判断 Buff 是否为永久 Buff（只读）。

**类型：** `bool`  
**返回值：** `DurationTimer == -1` 时返回 `true`

---

#### 事件

##### OnCreate
```csharp
public event Action<Buff> OnCreate
```

Buff 创建时触发的事件。

**参数类型：** `Action<Buff>`  
**触发时机：** `BuffManager.CreateBuff()` 创建实例后

---

##### OnTrigger
```csharp
public event Action<Buff> OnTrigger
```

Buff 周期性触发效果时触发的事件。

**参数类型：** `Action<Buff>`  
**触发时机：** 根据 `TriggerInterval` 周期触发  
**触发条件：** `TriggerInterval > 0`

---

##### OnUpdate
```csharp
public event Action<Buff> OnUpdate
```

Buff 每帧更新时触发的事件。

**参数类型：** `Action<Buff>`  
**触发时机：** `BuffManager.Update()` 每次调用  
**性能警告：** ⚠️ 每帧触发，谨慎使用耗时操作

---

##### OnAddStack
```csharp
public event Action<Buff> OnAddStack
```

Buff 堆叠增加时触发的事件。

**参数类型：** `Action<Buff>`  
**触发时机：** `CurrentStacks` 增加时

---

##### OnReduceStack
```csharp
public event Action<Buff> OnReduceStack
```

Buff 堆叠减少时触发的事件。

**参数类型：** `Action<Buff>`  
**触发时机：** `CurrentStacks` 减少时（不包括减少到 0）

---

##### OnRemove
```csharp
public event Action<Buff> OnRemove
```

Buff 被移除时触发的事件。

**参数类型：** `Action<Buff>`  
**触发时机：** Buff 完全移除之前（堆叠为 0 或手动移除）

---

#### 方法

##### AddStack
```csharp
public void AddStack(int amount = 1)
```

增加 Buff 堆叠层数。

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `amount` | `int` | `1` | 增加的层数 |

**说明：** 
- 自动限制在 `[0, MaxStacks]` 范围内
- 触发 `OnAddStack` 事件

---

##### ReduceStack
```csharp
public void ReduceStack(int amount = 1)
```

减少 Buff 堆叠层数。

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `amount` | `int` | `1` | 减少的层数 |

**说明：** 
- 堆叠减至 0 时触发 `OnRemove` 并移除 Buff
- 触发 `OnReduceStack` 事件（堆叠 > 0 时）

---

---

### BuffData

**命名空间：** `EasyPack.BuffSystem`  
**继承：** `object`

定义 Buff 的静态配置数据，可被多个 Buff 实例共享。

#### 构造函数

```csharp
public BuffData()
```

创建一个新的 BuffData 实例，所有属性使用默认值。

---

#### 属性

##### ID
```csharp
public string ID { get; set; }
```

Buff 的唯一标识符。

**类型：** `string`  
**默认值：** `string.Empty`  
**说明：** 用于查询和管理 Buff，建议使用有意义的名称

---

##### Name
```csharp
public string Name { get; set; }
```

Buff 的显示名称。

**类型：** `string`  
**默认值：** `string.Empty`  
**说明：** 用于 UI 显示

---

##### Description
```csharp
public string Description { get; set; }
```

Buff 的描述文本。

**类型：** `string`  
**默认值：** `string.Empty`  
**说明：** 详细说明 Buff 效果，用于 UI 显示

---

##### Sprite
```csharp
public Sprite Sprite { get; set; }
```

Buff 的图标。

**类型：** `Sprite`  
**默认值：** `null`  
**说明：** 用于 UI 显示

---

##### CustomData
```csharp
public object CustomData { get; set; }
```

自定义数据。

**类型：** `object`  
**默认值：** `null`  
**说明：** 可存储任意自定义数据，供模块使用

---

##### MaxStacks
```csharp
public int MaxStacks { get; set; }
```

最大堆叠层数。

**类型：** `int`  
**默认值：** `1`  
**说明：** 
- `1` 表示不可堆叠
- `> 1` 表示可堆叠的最大层数

---

##### Duration
```csharp
public float Duration { get; set; }
```

Buff 持续时间（秒）。

**类型：** `float`  
**默认值：** `-1`  
**说明：** 
- `-1` 表示永久 Buff
- `> 0` 表示有限时间 Buff
- `0` 表示瞬时 Buff（立即移除）

---

##### TriggerInterval
```csharp
public float TriggerInterval { get; set; }
```

触发间隔（秒）。

**类型：** `float`  
**默认值：** `-1`  
**说明：** 
- `-1` 表示不周期触发
- `> 0` 表示周期触发间隔
- 触发时调用 `OnTrigger` 事件

---

##### TriggerOnCreate
```csharp
public bool TriggerOnCreate { get; set; }
```

创建时是否立即触发一次。

**类型：** `bool`  
**默认值：** `false`  
**说明：** `true` 时在 `OnCreate` 后立即调用 `OnTrigger`

---

##### BuffSuperpositionStrategy
```csharp
public BuffSuperpositionDurationType BuffSuperpositionStrategy { get; set; }
```

持续时间叠加策略。

**类型：** `BuffSuperpositionDurationType`  
**默认值：** `BuffSuperpositionDurationType.Reset`  
**可选值：**
- `Add` - 持续时间叠加
- `Reset` - 重置为初始持续时间
- `Keep` - 保持当前持续时间
- `ResetThenAdd` - 先重置再叠加

---

##### BuffSuperpositionStacksStrategy
```csharp
public BuffSuperpositionStacksType BuffSuperpositionStacksStrategy { get; set; }
```

堆叠数叠加策略。

**类型：** `BuffSuperpositionStacksType`  
**默认值：** `BuffSuperpositionStacksType.Keep`  
**可选值：**
- `Add` - 堆叠数增加
- `Keep` - 保持当前堆叠数

---

##### BuffRemoveStrategy
```csharp
public BuffRemoveType BuffRemoveStrategy { get; set; }
```

Buff 移除策略。

**类型：** `BuffRemoveType`  
**默认值：** `BuffRemoveType.RemoveAll`  
**可选值：**
- `RemoveAll` - 移除所有堆叠
- `ReduceStack` - 每次移除一层堆叠

---

##### Tags
```csharp
public List<string> Tags { get; set; }
```

Buff 标签列表。

**类型：** `List<string>`  
**默认值：** 空列表  
**说明：** 用于分类和批量查询/移除

---

##### Layers
```csharp
public List<string> Layers { get; set; }
```

Buff 层级列表。

**类型：** `List<string>`  
**默认值：** 空列表  
**说明：** 用于更高层次的分类和互斥逻辑

---

##### BuffModules
```csharp
public List<BuffModule> BuffModules { get; set; }
```

Buff 模块列表。

**类型：** `List<BuffModule>`  
**默认值：** 空列表  
**说明：** 定义 Buff 的具体行为，按 `Priority` 排序执行

---

---

### BuffManager

**命名空间：** `EasyPack.BuffSystem`  
**继承：** `object`

管理所有 Buff 实例的生命周期，提供创建、查询、更新和移除功能。

#### 构造函数

```csharp
public BuffManager()
```

创建一个新的 BuffManager 实例。

---

#### 方法

##### CreateBuff
```csharp
public Buff CreateBuff(BuffData buffData, GameObject creator, GameObject target)
```

创建并应用一个 Buff 到目标对象。

| 参数 | 类型 | 说明 |
|------|------|------|
| `buffData` | `BuffData` | Buff 配置数据 |
| `creator` | `GameObject` | 施法者（可为 `null`） |
| `target` | `GameObject` | 目标对象（不能为 `null`） |

**返回值：** `Buff` - 新创建的 Buff 实例（如果 Buff 已存在则返回已有实例）

**说明：** 
- 如果目标已有相同 ID 的 Buff，根据叠加策略处理
- 自动初始化并执行 Buff 模块
- 触发 `OnCreate` 事件

---

##### RemoveBuff
```csharp
public void RemoveBuff(Buff buff)
```

移除指定的 Buff 实例。

| 参数 | 类型 | 说明 |
|------|------|------|
| `buff` | `Buff` | 要移除的 Buff 实例 |

**说明：** 
- 触发 `OnRemove` 事件
- 执行模块的清理回调
- 从所有索引中移除

---

##### RemoveBuffByID
```csharp
public void RemoveBuffByID(GameObject target, string buffID)
```

移除目标身上指定 ID 的 Buff。

| 参数 | 类型 | 说明 |
|------|------|------|
| `target` | `GameObject` | 目标对象 |
| `buffID` | `string` | Buff 的 ID |

**说明：** 根据 `BuffRemoveStrategy` 决定是移除全部还是减少一层

---

##### RemoveAllBuffs
```csharp
public void RemoveAllBuffs(GameObject target)
```

移除目标身上的所有 Buff。

| 参数 | 类型 | 说明 |
|------|------|------|
| `target` | `GameObject` | 目标对象 |

---

##### RemoveBuffsByTag
```csharp
public void RemoveBuffsByTag(GameObject target, string tag)
```

移除目标身上带有指定标签的所有 Buff。

| 参数 | 类型 | 说明 |
|------|------|------|
| `target` | `GameObject` | 目标对象 |
| `tag` | `string` | 标签名称 |

---

##### RemoveBuffsByLayer
```csharp
public void RemoveBuffsByLayer(GameObject target, string layer)
```

移除目标身上带有指定层级的所有 Buff。

| 参数 | 类型 | 说明 |
|------|------|------|
| `target` | `GameObject` | 目标对象 |
| `layer` | `string` | 层级名称 |

---

##### RemoveAllBuffsByTag
```csharp
public void RemoveAllBuffsByTag(string tag)
```

移除所有目标身上带有指定标签的 Buff（全局移除）。

| 参数 | 类型 | 说明 |
|------|------|------|
| `tag` | `string` | 标签名称 |

**性能提示：** 批量操作，适合大范围清理

---

##### RemoveAllBuffsByLayer
```csharp
public void RemoveAllBuffsByLayer(string layer)
```

移除所有目标身上带有指定层级的 Buff（全局移除）。

| 参数 | 类型 | 说明 |
|------|------|------|
| `layer` | `string` | 层级名称 |

**性能提示：** 批量操作，适合大范围清理

---

##### FlushPendingRemovals
```csharp
public void FlushPendingRemovals()
```

立即处理所有待移除的 Buff。

**说明：** 
- 批量移除操作会先标记为待移除，调用此方法立即执行
- 在关键逻辑前调用以确保状态同步

---

##### ContainsBuff
```csharp
public bool ContainsBuff(GameObject target, string buffID)
```

检查目标是否有指定 ID 的 Buff。

| 参数 | 类型 | 说明 |
|------|------|------|
| `target` | `GameObject` | 目标对象 |
| `buffID` | `string` | Buff 的 ID |

**返回值：** `bool` - 存在返回 `true`，否则返回 `false`

---

##### ContainsBuffWithTag
```csharp
public bool ContainsBuffWithTag(string tag)
```

检查全局是否有带指定标签的 Buff。

| 参数 | 类型 | 说明 |
|------|------|------|
| `tag` | `string` | 标签名称 |

**返回值：** `bool` - 存在返回 `true`，否则返回 `false`

---

##### ContainsBuffWithLayer
```csharp
public bool ContainsBuffWithLayer(string layer)
```

检查全局是否有带指定层级的 Buff。

| 参数 | 类型 | 说明 |
|------|------|------|
| `layer` | `string` | 层级名称 |

**返回值：** `bool` - 存在返回 `true`，否则返回 `false`

---

##### GetBuff
```csharp
public Buff GetBuff(GameObject target, string buffID)
```

获取目标身上指定 ID 的 Buff。

| 参数 | 类型 | 说明 |
|------|------|------|
| `target` | `GameObject` | 目标对象 |
| `buffID` | `string` | Buff 的 ID |

**返回值：** `Buff` - Buff 实例，不存在返回 `null`

---

##### GetTargetBuffs
```csharp
public List<Buff> GetTargetBuffs(GameObject target)
```

获取目标身上的所有 Buff。

| 参数 | 类型 | 说明 |
|------|------|------|
| `target` | `GameObject` | 目标对象 |

**返回值：** `List<Buff>` - Buff 列表，目标不存在返回空列表

---

##### GetBuffsByTag
```csharp
public List<Buff> GetBuffsByTag(GameObject target, string tag)
```

获取目标身上带有指定标签的所有 Buff。

| 参数 | 类型 | 说明 |
|------|------|------|
| `target` | `GameObject` | 目标对象 |
| `tag` | `string` | 标签名称 |

**返回值：** `List<Buff>` - Buff 列表

---

##### GetBuffsByLayer
```csharp
public List<Buff> GetBuffsByLayer(GameObject target, string layer)
```

获取目标身上带有指定层级的所有 Buff。

| 参数 | 类型 | 说明 |
|------|------|------|
| `target` | `GameObject` | 目标对象 |
| `layer` | `string` | 层级名称 |

**返回值：** `List<Buff>` - Buff 列表

---

##### Update
```csharp
public void Update(float deltaTime)
```

更新所有 Buff 的计时器和触发逻辑。

| 参数 | 类型 | 说明 |
|------|------|------|
| `deltaTime` | `float` | 时间增量（秒） |

**说明：** 
- 更新持续时间计时器
- 更新触发计时器
- 处理过期 Buff
- 调用 `OnUpdate` 和 `OnTrigger` 事件
- **必须在每帧调用（通常在 MonoBehaviour.Update 中）**

---

---

### BuffModule

**命名空间：** `EasyPack.BuffSystem`  
**继承：** `object`  
**类型：** 抽象类

定义 Buff 的具体行为模块，通过回调系统响应生命周期事件。

#### 构造函数

```csharp
public BuffModule()
```

创建一个新的 BuffModule 实例。

---

#### 属性

##### Priority
```csharp
public int Priority { get; set; }
```

模块执行优先级。

**类型：** `int`  
**默认值：** `0`  
**说明：** 数值越大优先级越高，执行越早

---

##### TriggerCondition
```csharp
public Func<Buff, bool> TriggerCondition { get; set; }
```

触发条件检查函数。

**类型：** `Func<Buff, bool>`  
**默认值：** `null`  
**说明：** 
- 返回 `true` 时模块才会执行
- `null` 表示无条件执行

---

#### 方法

##### RegisterCallback
```csharp
protected void RegisterCallback(BuffCallBackType callbackType, Action<Buff, object[]> callback)
```

注册生命周期回调。

| 参数 | 类型 | 说明 |
|------|------|------|
| `callbackType` | `BuffCallBackType` | 回调类型枚举 |
| `callback` | `Action<Buff, object[]>` | 回调函数 |

**可用回调类型：**
- `BuffCallBackType.OnCreate` - Buff 创建时
- `BuffCallBackType.OnTick` - 周期触发时
- `BuffCallBackType.OnUpdate` - 每帧更新时
- `BuffCallBackType.OnAddStack` - 堆叠增加时
- `BuffCallBackType.OnReduceStack` - 堆叠减少时
- `BuffCallBackType.OnRemove` - Buff 移除时
- `BuffCallBackType.Custom` - 自定义事件

**使用示例：**
```csharp
public class MyModule : BuffModule
{
    public MyModule()
    {
        RegisterCallback(BuffCallBackType.OnCreate, OnCreate);
        RegisterCallback(BuffCallBackType.OnTick, OnTick);
    }

    private void OnCreate(Buff buff, object[] parameters)
    {
        Debug.Log("Buff 创建");
    }

    private void OnTick(Buff buff, object[] parameters)
    {
        Debug.Log("周期触发");
    }
}
```

---

##### RegisterCallback (自定义事件)
```csharp
protected void RegisterCallback(string customEventName, Action<Buff, object[]> callback)
```

注册自定义事件回调。

| 参数 | 类型 | 说明 |
|------|------|------|
| `customEventName` | `string` | 自定义事件名称 |
| `callback` | `Action<Buff, object[]>` | 回调函数 |

**使用示例：**
```csharp
RegisterCallback("OnCriticalHit", OnCriticalHit);

private void OnCriticalHit(Buff buff, object[] parameters)
{
    float damage = (float)parameters[0];
    Debug.Log($"暴击伤害: {damage}");
}
```

---

##### Execute
```csharp
public void Execute(Buff buff, BuffCallBackType callbackType, string customEventName = null, params object[] parameters)
```

执行模块的回调函数。

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `buff` | `Buff` | - | 触发的 Buff 实例 |
| `callbackType` | `BuffCallBackType` | - | 回调类型 |
| `customEventName` | `string` | `null` | 自定义事件名称（仅 Custom 类型时有效） |
| `parameters` | `object[]` | 空数组 | 传递给回调的参数 |

**说明：** 
- 自动检查 `TriggerCondition`
- 仅在条件满足时执行

---

---

### CastModifierToProperty

**命名空间：** `EasyPack.BuffSystem`  
**继承：** `BuffModule`

将修饰符应用到 GameProperty 的模块，支持自动添加/移除修饰符。

#### 构造函数

```csharp
public CastModifierToProperty(IModifier modifier, string propertyID, GamePropertyManager propertyManager)
```

创建一个属性修饰符模块。

| 参数 | 类型 | 说明 |
|------|------|------|
| `modifier` | `IModifier` | 要应用的修饰符 |
| `propertyID` | `string` | 目标属性的 ID |
| `propertyManager` | `GamePropertyManager` | 属性管理器实例 |

**说明：** 
- 在 `OnCreate` 和 `OnAddStack` 时添加修饰符
- 在 `OnRemove` 和 `OnReduceStack` 时移除修饰符

**使用示例：**
```csharp
var strengthModifier = new FloatModifier(ModifierType.Add, 0, 10f);
var module = new CastModifierToProperty(
    strengthModifier, 
    "Strength", 
    propertyManager
);
buffData.BuffModules.Add(module);
```

---

---

## 枚举类型

### BuffSuperpositionDurationType

持续时间叠加策略。

**命名空间：** `EasyPack.BuffSystem`

| 枚举值 | 说明 |
|--------|------|
| `Add` | 持续时间叠加（当前时间 + 新时间） |
| `Reset` | 重置为初始持续时间 |
| `Keep` | 保持当前持续时间不变 |
| `ResetThenAdd` | 先重置再叠加（初始时间 + 新时间） |

---

### BuffSuperpositionStacksType

堆叠数叠加策略。

**命名空间：** `EasyPack.BuffSystem`

| 枚举值 | 说明 |
|--------|------|
| `Add` | 堆叠数增加（限制在 MaxStacks 以内） |
| `Keep` | 保持当前堆叠数不变 |

---

### BuffRemoveType

Buff 移除策略。

**命名空间：** `EasyPack.BuffSystem`

| 枚举值 | 说明 |
|--------|------|
| `RemoveAll` | 移除所有堆叠（完全移除 Buff） |
| `ReduceStack` | 每次移除一层堆叠 |

---

### BuffCallBackType

Buff 回调类型。

**命名空间：** `EasyPack.BuffSystem`

| 枚举值 | 说明 |
|--------|------|
| `OnCreate` | Buff 创建时触发 |
| `OnTick` | 周期触发（根据 TriggerInterval） |
| `OnUpdate` | 每帧更新时触发 |
| `OnAddStack` | 堆叠增加时触发 |
| `OnReduceStack` | 堆叠减少时触发 |
| `OnRemove` | Buff 移除时触发 |
| `Custom` | 自定义事件触发 |

---

## 使用示例

### 完整示例：创建带模块的 Buff

```csharp
using EasyPack.BuffSystem;
using EasyPack.GamePropertySystem;
using EasyPack;
using UnityEngine;

public class CompleteExample : MonoBehaviour
{
    private BuffManager buffManager;
    private GamePropertyManager propertyManager;
    private GameObject player;

    void Start()
    {
        // 1. 初始化管理器
        buffManager = new BuffManager();
        propertyManager = new GamePropertyManager();
        player = new GameObject("Player");

        // 2. 初始化属性
        var strength = new CombinePropertySingle("Strength", 10f);
        propertyManager.AddOrUpdate(strength);

        // 3. 创建 BuffData
        var powerBuff = new BuffData
        {
            ID = "PowerBuff",
            Name = "力量增益",
            Description = "增加 15 点力量",
            Duration = 10f,
            TriggerInterval = 2f,
            MaxStacks = 3,
            BuffSuperpositionStrategy = BuffSuperpositionDurationType.Reset,
            BuffSuperpositionStacksStrategy = BuffSuperpositionStacksType.Add,
            Tags = new System.Collections.Generic.List<string> { "Positive", "Enhancement" }
        };

        // 4. 创建并添加模块
        var strengthModifier = new FloatModifier(ModifierType.Add, 0, 15f);
        var propertyModule = new CastModifierToProperty(
            strengthModifier,
            "Strength",
            propertyManager
        );
        powerBuff.BuffModules.Add(propertyModule);

        // 5. 应用 Buff
        Buff buff = buffManager.CreateBuff(powerBuff, gameObject, player);

        // 6. 注册事件
        buff.OnCreate += (b) => Debug.Log($"{b.BuffData.Name} 已创建");
        buff.OnTrigger += (b) => Debug.Log($"{b.BuffData.Name} 触发效果");
        buff.OnRemove += (b) => Debug.Log($"{b.BuffData.Name} 已移除");

        // 7. 查询 Buff
        bool hasBuff = buffManager.ContainsBuff(player, "PowerBuff");
        Debug.Log($"玩家有力量增益: {hasBuff}");
        Debug.Log($"当前力量: {strength.GetValue()}");
    }

    void Update()
    {
        // 8. 每帧更新
        buffManager.Update(Time.deltaTime);
    }
}
```

---

## 相关资源

- [用户指南](./UserGuide.md) - 任务导向的使用指南
- [Mermaid 图集](./Diagrams.md) - 系统架构可视化
- [示例代码](../Example/BuffExample.cs) - 完整的使用示例

---

**维护者：** EasyPack 团队  
**许可证：** 遵循项目主许可证
