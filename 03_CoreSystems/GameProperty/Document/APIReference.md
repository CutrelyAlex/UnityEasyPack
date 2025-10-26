# GameProperty System - API Reference

**适用EasyPack版本：** EasyPack v1.5.30
**最后更新：** 2025-10-26

---

## 概述

本文档提供 GameProperty System 的 API 参考，包括类、接口、方法的详细签名和参数说明。

---

## 目录

- [概述](#概述)
- [核心类](#核心类)
- [组合属性类](#组合属性类)
- [属性管理器](#属性管理器)
- [修饰符类](#修饰符类)
- [枚举类型](#枚举类型)
- [序列化相关](#序列化相关)
- [相关资源](#相关资源)

---

## 核心类

### `GameProperty` 类

基于浮点数值的游戏属性类，支持修饰符系统、依赖系统、脏标记系统。

#### 构造函数

```csharp
public GameProperty(string id, float initValue)
```

**参数说明：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `id` | `string` | 必填 | 属性的唯一标识符 | - |
| `initValue` | `float` | 必填 | 属性的初始基础值 | - |

**返回值：**
- **类型：** `GameProperty`
- **成功情况：** 返回新创建的 `GameProperty` 对象
- **失败情况：** 无（构造函数不返回 null）
- **可能的异常：** 无
- **示例值：** `GameProperty { ID = "health", BaseValue = 100f }`

**使用示例：**

```csharp
using EasyPack.GamePropertySystem;

var health = new GameProperty("health", 100f);
Debug.Log($"创建属性: {health.ID}, 值: {health.GetValue()}");
```

---

#### 属性

##### `ID`

```csharp
public string ID { get; set; }
```

**说明：** 属性的唯一标识符。

**类型：** `string`

**使用示例：**

```csharp
var prop = new GameProperty("attack", 50f);
Debug.Log($"属性ID: {prop.ID}"); // 输出：attack
```

---

##### `Modifiers`

```csharp
public List<IModifier> Modifiers { get; }
```

**说明：** 应用于此属性的所有修饰符列表（只读）。

**类型：** `List<IModifier>`

**使用示例：**

```csharp
var prop = new GameProperty("defense", 30f);
prop.AddModifier(new FloatModifier(ModifierType.Add, 0, 15f));
Debug.Log($"修饰符数量: {prop.Modifiers.Count}"); // 输出：1
```

---

#### 核心方法

##### `GetValue()`

获取属性的最终值（应用所有修饰符后）。

```csharp
public float GetValue()
```

**参数说明：** 无

**返回值：**
- **类型：** `float`
- **成功情况：** 返回应用所有修饰符后的最终值
- **失败情况：** 无（总是返回有效值）
- **可能的异常：** 无
- **示例值：** `150.5f`

**使用示例：**

```csharp
var mana = new GameProperty("mana", 100f);
mana.AddModifier(new FloatModifier(ModifierType.Mul, 0, 1.5f));
Debug.Log(mana.GetValue()); // 输出：150
```

---

##### `GetBaseValue()`

获取属性的基础值（未应用修饰符）。

```csharp
public float GetBaseValue()
```

**参数说明：** 无

**返回值：**
- **类型：** `float`
- **成功情况：** 返回基础值
- **失败情况：** 无
- **可能的异常：** 无
- **示例值：** `100f`

**使用示例：**

```csharp
var stamina = new GameProperty("stamina", 80f);
stamina.AddModifier(new FloatModifier(ModifierType.Add, 0, 20f));
Debug.Log($"基础值: {stamina.GetBaseValue()}"); // 输出：80
Debug.Log($"最终值: {stamina.GetValue()}");     // 输出：100
```

---

##### `SetBaseValue()`

设置属性的基础值并触发重新计算。

```csharp
public IModifiableProperty<float> SetBaseValue(float value)
```

**参数说明：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `value` | `float` | 必填 | 新的基础值 | - |

**返回值：**
- **类型：** `IModifiableProperty<float>`
- **成功情况：** 返回属性自身（支持链式调用）
- **失败情况：** 无
- **可能的异常：** 无
- **示例值：** `GameProperty` 实例

**使用示例：**

```csharp
var power = new GameProperty("power", 50f);
power.SetBaseValue(80f).AddModifier(new FloatModifier(ModifierType.Mul, 0, 1.2f));
Debug.Log(power.GetValue()); // 输出：96 (80 * 1.2)
```

---

#### 修饰符方法

##### `AddModifier()`

向属性添加一个修饰符，修饰符会影响最终值。

```csharp
public IModifiableProperty<float> AddModifier(IModifier modifier)
```

**参数说明：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `modifier` | `IModifier` | 必填 | 要添加的修饰符对象 | - |

**返回值：**
- **类型：** `IModifiableProperty<float>`
- **成功情况：** 返回属性自身（支持链式调用）
- **失败情况：** 无
- **可能的异常：** 无
- **示例值：** `GameProperty` 实例

**使用示例：**

```csharp
var damage = new GameProperty("damage", 100f);
damage.AddModifier(new FloatModifier(ModifierType.Add, 0, 50f));
Debug.Log(damage.GetValue()); // 输出：150
```

---

##### `RemoveModifier()`

从属性中移除一个特定的修饰符。

```csharp
public IModifiableProperty<float> RemoveModifier(IModifier modifier)
```

**参数说明：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `modifier` | `IModifier` | 必填 | 要移除的修饰符对象引用 | - |

**返回值：**
- **类型：** `IModifiableProperty<float>`
- **成功情况：** 返回属性自身（支持链式调用）
- **失败情况：** 如果修饰符不存在，不执行任何操作
- **可能的异常：** 无
- **示例值：** `GameProperty` 实例

**使用示例：**

```csharp
var speed = new GameProperty("speed", 10f);
var buff = new FloatModifier(ModifierType.Mul, 0, 1.5f);
speed.AddModifier(buff);
Debug.Log(speed.GetValue()); // 输出：15

speed.RemoveModifier(buff);
Debug.Log(speed.GetValue()); // 输出：10
```

---

##### `ClearModifiers()`

清除所有修饰符，属性值将回到基础值。

```csharp
public IModifiableProperty<float> ClearModifiers()
```

**参数说明：** 无

**返回值：**
- **类型：** `IModifiableProperty<float>`
- **成功情况：** 返回属性自身（支持链式调用）
- **失败情况：** 无
- **可能的异常：** 无
- **示例值：** `GameProperty` 实例

**使用示例：**

```csharp
var resistance = new GameProperty("resistance", 20f);
resistance.AddModifier(new FloatModifier(ModifierType.Add, 0, 30f));
resistance.AddModifier(new FloatModifier(ModifierType.Mul, 0, 1.5f));
Debug.Log(resistance.GetValue()); // 输出：75 ((20+30)*1.5)

resistance.ClearModifiers();
Debug.Log(resistance.GetValue()); // 输出：20
```

---

##### `AddModifiers()`

批量添加多个修饰符到属性。

```csharp
public IModifiableProperty<float> AddModifiers(IEnumerable<IModifier> modifiers)
```

**参数说明：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `modifiers` | `IEnumerable<IModifier>` | 必填 | 要添加的修饰符集合 | - |

**返回值：**
- **类型：** `IModifiableProperty<float>`
- **成功情况：** 返回属性自身（支持链式调用）
- **失败情况：** 无
- **可能的异常：** 
  - `ArgumentNullException`：当集合中包含 `null` 元素时抛出
- **示例值：** `GameProperty` 实例

**使用示例：**

```csharp
var prop = new GameProperty("test", 50f);
var buffs = new List<IModifier>
{
    new FloatModifier(ModifierType.Add, 0, 20f),
    new FloatModifier(ModifierType.Mul, 0, 1.3f)
};
prop.AddModifiers(buffs);
Debug.Log(prop.GetValue()); // 输出：91 ((50+20)*1.3)
```

---

##### `RemoveModifiers()`

批量移除多个修饰符从属性。

```csharp
public IModifiableProperty<float> RemoveModifiers(IEnumerable<IModifier> modifiers)
```

**参数说明：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `modifiers` | `IEnumerable<IModifier>` | 必填 | 要移除的修饰符集合 | - |

**返回值：**
- **类型：** `IModifiableProperty<float>`
- **成功情况：** 返回属性自身（支持链式调用）
- **失败情况：** 如果修饰符不存在，不执行任何操作
- **可能的异常：** 无
- **示例值：** `GameProperty` 实例

**使用示例：**

```csharp
var prop = new GameProperty("test", 100f);
var tempBuffs = new List<IModifier>
{
    new FloatModifier(ModifierType.Add, 0, 50f),
    new FloatModifier(ModifierType.Mul, 0, 1.5f)
};
prop.AddModifiers(tempBuffs);
Debug.Log(prop.GetValue()); // 输出：225

prop.RemoveModifiers(tempBuffs);
Debug.Log(prop.GetValue()); // 输出：100
```

---

#### 依赖系统方法

##### `AddDependency()`

添加一个依赖项，当 dependency 的值改变时，会调用 calculator 来计算新值。

```csharp
public IModifiableProperty<float> AddDependency(GameProperty dependency, Func<GameProperty, float, float> calculator = null)
```

**参数说明：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `dependency` | `GameProperty` | 必填 | 被依赖的属性 | - |
| `calculator` | `Func<GameProperty, float, float>` | 可选 | 计算函数，接收依赖属性和新值，返回当前属性的新基础值 | `null` |

**返回值：**
- **类型：** `IModifiableProperty<float>`
- **成功情况：** 返回属性自身（支持链式调用）
- **失败情况：** 如果检测到循环依赖，不添加依赖关系并记录警告
- **可能的异常：** 
  - `ArgumentNullException`：当 `dependency` 为 `null` 时抛出
- **示例值：** `GameProperty` 实例

**使用示例：**

```csharp
var strength = new GameProperty("strength", 15f);
var attack = new GameProperty("attack", 0f);

// 攻击力 = 力量 × 2 + 10
attack.AddDependency(strength, (dep, newVal) => newVal * 2f + 10f);
Debug.Log(attack.GetValue()); // 输出：40 (15*2+10)

strength.SetBaseValue(20f);
Debug.Log(attack.GetValue()); // 输出：50 (20*2+10)
```

---

##### `RemoveDependency()`

移除依赖关系。

```csharp
public IModifiableProperty<float> RemoveDependency(GameProperty dependency)
```

**参数说明：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `dependency` | `GameProperty` | 必填 | 要移除的依赖属性 | - |

**返回值：**
- **类型：** `IModifiableProperty<float>`
- **成功情况：** 返回属性自身（支持链式调用）
- **失败情况：** 如果依赖不存在，不执行任何操作
- **可能的异常：** 无
- **示例值：** `GameProperty` 实例

**使用示例：**

```csharp
var agility = new GameProperty("agility", 10f);
var dodge = new GameProperty("dodge", 0f);

dodge.AddDependency(agility, (dep, newVal) => newVal * 0.5f);
Debug.Log(dodge.GetValue()); // 输出：5

dodge.RemoveDependency(agility);
agility.SetBaseValue(20f);
Debug.Log(dodge.GetValue()); // 输出：5 (不再更新)
```

---

#### 脏标记方法

##### `MakeDirty()`

将属性标记为脏状态，表示需要重新计算值。

```csharp
public void MakeDirty()
```

**参数说明：** 无

**返回值：** 无（`void`）

**使用示例：**

```csharp
var prop = new GameProperty("test", 100f);
prop.OnDirty(() => Debug.Log("属性需要重新计算"));
prop.MakeDirty(); // 输出：属性需要重新计算
```

---

##### `OnDirty()`

注册一个在属性变为脏状态时的回调函数。

```csharp
public void OnDirty(Action action)
```

**参数说明：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `action` | `Action` | 必填 | 脏状态回调函数 | - |

**返回值：** 无（`void`）

**使用示例：**

```csharp
var prop = new GameProperty("monitored", 50f);
prop.OnDirty(() => Debug.Log("属性被修改"));
prop.AddModifier(new FloatModifier(ModifierType.Add, 0, 10f));
// 输出：属性被修改
```

---

##### `RemoveOnDirty()`

移除脏状态变化的回调函数。

```csharp
public void RemoveOnDirty(Action action)
```

**参数说明：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `action` | `Action` | 必填 | 要移除的回调函数 | - |

**返回值：** 无（`void`）

**使用示例：**

```csharp
var prop = new GameProperty("test", 100f);
Action callback = () => Debug.Log("脏数据");
prop.OnDirty(callback);
prop.RemoveOnDirty(callback);
prop.MakeDirty(); // 不会输出任何内容
```

---

#### 事件

##### `OnValueChanged`

当属性值发生变化时触发。

```csharp
public event Action<float, float> OnValueChanged;
```

**参数说明：**
- 第一个参数：`float` - 旧值
- 第二个参数：`float` - 新值

**使用示例：**

```csharp
var health = new GameProperty("health", 100f);
health.OnValueChanged += (oldVal, newVal) =>
{
    Debug.Log($"生命值变化: {oldVal} -> {newVal}");
};

health.SetBaseValue(150f);
// 输出：生命值变化: 100 -> 150
```

---

#### 查询方法

##### `HasModifiers`

检查是否有任何修饰符。

```csharp
public bool HasModifiers { get; }
```

**类型：** `bool`

**使用示例：**

```csharp
var prop = new GameProperty("test", 50f);
Debug.Log(prop.HasModifiers); // 输出：False

prop.AddModifier(new FloatModifier(ModifierType.Add, 0, 10f));
Debug.Log(prop.HasModifiers); // 输出：True
```

---

##### `ModifierCount`

获取修饰符总数。

```csharp
public int ModifierCount { get; }
```

**类型：** `int`

**使用示例：**

```csharp
var prop = new GameProperty("test", 50f);
prop.AddModifier(new FloatModifier(ModifierType.Add, 0, 10f));
prop.AddModifier(new FloatModifier(ModifierType.Mul, 0, 1.5f));
Debug.Log($"修饰符数量: {prop.ModifierCount}"); // 输出：2
```

---

##### `ContainModifierOfType()`

检查是否包含指定类型的修饰符。

```csharp
public bool ContainModifierOfType(ModifierType type)
```

**参数说明：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `type` | `ModifierType` | 必填 | 修饰符类型枚举值 | - |

**返回值：**
- **类型：** `bool`
- **成功情况：** 如果包含该类型修饰符返回 `true`，否则返回 `false`
- **失败情况：** 无
- **可能的异常：** 无
- **示例值：** `true` 或 `false`

**使用示例：**

```csharp
var prop = new GameProperty("test", 50f);
prop.AddModifier(new FloatModifier(ModifierType.Add, 0, 10f));
Debug.Log(prop.ContainModifierOfType(ModifierType.Add)); // 输出：True
Debug.Log(prop.ContainModifierOfType(ModifierType.Mul)); // 输出：False
```

---

##### `GetModifierCountOfType()`

获取指定类型的修饰符数量。

```csharp
public int GetModifierCountOfType(ModifierType type)
```

**参数说明：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `type` | `ModifierType` | 必填 | 修饰符类型枚举值 | - |

**返回值：**
- **类型：** `int`
- **成功情况：** 返回该类型修饰符的数量
- **失败情况：** 如果不存在返回 `0`
- **可能的异常：** 无
- **示例值：** `2`

**使用示例：**

```csharp
var prop = new GameProperty("test", 50f);
prop.AddModifier(new FloatModifier(ModifierType.Add, 0, 10f));
prop.AddModifier(new FloatModifier(ModifierType.Add, 1, 20f));
Debug.Log(prop.GetModifierCountOfType(ModifierType.Add)); // 输出：2
```

---

## 组合属性类

### `CombinePropertySingle` 类

单一属性组合实现，仅包含单一 GameProperty，直接返回该属性的值作为最终结果。

#### 构造函数

```csharp
public CombinePropertySingle(string id, float baseValue = 0)
```

**参数说明：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `id` | `string` | 必填 | 属性的唯一标识符 | - |
| `baseValue` | `float` | 可选 | 属性的初始基础值 | `0` |

**使用示例：**

```csharp
var singleProp = new CombinePropertySingle("magicPower", 80f);
Debug.Log(singleProp.GetValue()); // 输出：80
```

---

#### 方法

##### `SetBaseValue()`

设置基础值。

```csharp
public CombinePropertySingle SetBaseValue(float value)
```

**参数说明：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `value` | `float` | 必填 | 新的基础值 | - |

**返回值：**
- **类型：** `CombinePropertySingle`
- **成功情况：** 返回自身（支持链式调用）
- **失败情况：** 无
- **可能的异常：** 
  - `ObjectDisposedException`：当对象已释放时抛出
- **示例值：** `CombinePropertySingle` 实例

**使用示例：**

```csharp
var prop = new CombinePropertySingle("power", 50f);
prop.SetBaseValue(100f);
Debug.Log(prop.GetValue()); // 输出：100
```

---

##### `AddModifier()`

添加修饰符。

```csharp
public CombinePropertySingle AddModifier(IModifier modifier)
```

**参数说明：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `modifier` | `IModifier` | 必填 | 要添加的修饰符 | - |

**返回值：**
- **类型：** `CombinePropertySingle`
- **成功情况：** 返回自身（支持链式调用）
- **失败情况：** 无
- **可能的异常：** 
  - `ObjectDisposedException`：当对象已释放时抛出
- **示例值：** `CombinePropertySingle` 实例

**使用示例：**

```csharp
var prop = new CombinePropertySingle("attack", 50f);
prop.AddModifier(new FloatModifier(ModifierType.Add, 0, 25f));
Debug.Log(prop.GetValue()); // 输出：75
```

---

##### `SubscribeValueChanged()`

订阅值变化事件。

```csharp
public CombinePropertySingle SubscribeValueChanged(Action<float, float> handler)
```

**参数说明：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `handler` | `Action<float, float>` | 必填 | 事件处理器，接收旧值和新值 | - |

**返回值：**
- **类型：** `CombinePropertySingle`
- **成功情况：** 返回自身（支持链式调用）
- **失败情况：** 如果 `handler` 为 `null`，不执行任何操作
- **可能的异常：** 
  - `ObjectDisposedException`：当对象已释放时抛出
- **示例值：** `CombinePropertySingle` 实例

**使用示例：**

```csharp
var prop = new CombinePropertySingle("health", 100f);
prop.SubscribeValueChanged((oldVal, newVal) =>
{
    Debug.Log($"值变化: {oldVal} -> {newVal}");
});
prop.SetBaseValue(150f);
// 输出：值变化: 100 -> 150
```

---

### `CombinePropertyCustom` 类

自定义组合属性实现，支持完全自定义的计算逻辑。

#### 构造函数

```csharp
public CombinePropertyCustom(string id, float baseValue = 0)
```

**参数说明：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `id` | `string` | 必填 | 属性的唯一标识符 | - |
| `baseValue` | `float` | 可选 | 属性的初始基础值 | `0` |

**使用示例：**

```csharp
var customProp = new CombinePropertyCustom("totalDamage", 10f);
```

---

#### 方法

##### `RegisterProperty()`

注册子属性。

```csharp
public GameProperty RegisterProperty(GameProperty gameProperty, Action<float, float> handler = null)
```

**参数说明：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `gameProperty` | `GameProperty` | 必填 | 要注册的属性 | - |
| `handler` | `Action<float, float>` | 可选 | 自定义事件处理器，接收组合属性的旧值和新值 | `null` |

**返回值：**
- **类型：** `GameProperty`
- **成功情况：** 返回注册的属性
- **失败情况：** 无
- **可能的异常：** 
  - `ArgumentNullException`：当 `gameProperty` 为 `null` 时抛出
  - `ObjectDisposedException`：当对象已释放时抛出
- **示例值：** `GameProperty` 实例

**使用示例：**

```csharp
var custom = new CombinePropertyCustom("damage", 0f);
var baseDamage = new GameProperty("baseDamage", 50f);
var critBonus = new GameProperty("critBonus", 20f);

custom.RegisterProperty(baseDamage);
custom.RegisterProperty(critBonus);

custom.Calculater = (c) => 
    c.GetProperty("baseDamage").GetValue() + 
    c.GetProperty("critBonus").GetValue();

Debug.Log(custom.GetValue()); // 输出：70
```

---

##### `UnRegisterProperty()`

取消注册子属性。

```csharp
public void UnRegisterProperty(GameProperty gameProperty)
```

**参数说明：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `gameProperty` | `GameProperty` | 必填 | 要取消注册的属性 | - |

**返回值：** 无（`void`）

**使用示例：**

```csharp
var custom = new CombinePropertyCustom("test", 0f);
var prop = new GameProperty("temp", 10f);
custom.RegisterProperty(prop);
custom.UnRegisterProperty(prop);
```

---

##### `GetProperty()`

获取指定 ID 的子属性。

```csharp
public override GameProperty GetProperty(string id)
```

**参数说明：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `id` | `string` | 必填 | 子属性的 ID | - |

**返回值：**
- **类型：** `GameProperty`
- **成功情况：** 返回对应的子属性
- **失败情况：** 如果 ID 不存在返回 `null`
- **可能的异常：** 
  - `ObjectDisposedException`：当对象已释放时抛出
- **示例值：** `GameProperty` 实例或 `null`

**使用示例：**

```csharp
var custom = new CombinePropertyCustom("combined", 0f);
var strength = new GameProperty("strength", 15f);
custom.RegisterProperty(strength);

var retrieved = custom.GetProperty("strength");
Debug.Log(retrieved.GetValue()); // 输出：15
```

---

## 属性管理器

### `GamePropertyManager` 类

游戏属性管理器，负责集中管理所有组合属性的生命周期。

#### 方法

##### `AddOrUpdate()`

添加或更新一个 ICombineGameProperty。

```csharp
public void AddOrUpdate(ICombineGameProperty property)
```

**参数说明：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `property` | `ICombineGameProperty` | 必填 | 要添加或更新的组合属性 | - |

**返回值：** 无（`void`）

**使用示例：**

```csharp
var manager = new GamePropertyManager();
var health = new CombinePropertySingle("health", 100f);
manager.AddOrUpdate(health);
```

---

##### `Wrap()`

添加或更新一个 GameProperty（自动包装为 CombinePropertySingle）。

```csharp
public CombinePropertySingle Wrap(GameProperty property)
```

**参数说明：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `property` | `GameProperty` | 必填 | 要包装的 GameProperty | - |

**返回值：**
- **类型：** `CombinePropertySingle`
- **成功情况：** 返回包装后的组合属性
- **失败情况：** 如果 `property` 为 `null`，记录警告并返回 `null`
- **可能的异常：** 无
- **示例值：** `CombinePropertySingle` 实例或 `null`

**使用示例：**

```csharp
var manager = new GamePropertyManager();
var attack = new GameProperty("attack", 50f);
attack.AddModifier(new FloatModifier(ModifierType.Add, 0, 20f));

var wrapped = manager.Wrap(attack);
Debug.Log(wrapped.GetValue()); // 输出：70
```

---

##### `Get()`

根据 ID 获取 ICombineGameProperty。

```csharp
public ICombineGameProperty Get(string id)
```

**参数说明：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `id` | `string` | 必填 | 属性的唯一标识符 | - |

**返回值：**
- **类型：** `ICombineGameProperty`
- **成功情况：** 返回对应的组合属性实例
- **失败情况：** 如果不存在或无效返回 `null`
- **可能的异常：** 无
- **示例值：** `ICombineGameProperty` 实例或 `null`

**使用示例：**

```csharp
var manager = new GamePropertyManager();
var mana = new CombinePropertySingle("mana", 50f);
manager.AddOrUpdate(mana);

var retrieved = manager.Get("mana");
Debug.Log(retrieved.GetValue()); // 输出：50
```

---

##### `GetSingle()`

根据 ID 获取 CombinePropertySingle。

```csharp
public CombinePropertySingle GetSingle(string id)
```

**参数说明：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `id` | `string` | 必填 | 属性 ID | - |

**返回值：**
- **类型：** `CombinePropertySingle`
- **成功情况：** 返回对应的单一组合属性
- **失败情况：** 如果不存在或类型不匹配返回 `null`
- **可能的异常：** 无
- **示例值：** `CombinePropertySingle` 实例或 `null`

**使用示例：**

```csharp
var single = manager.GetSingle("health");
if (single != null)
{
    Debug.Log(single.GetValue());
}
```

---

##### `Remove()`

删除指定 ID 的 ICombineGameProperty 并释放其资源。

```csharp
public bool Remove(string id)
```

**参数说明：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `id` | `string` | 必填 | 要删除的属性 ID | - |

**返回值：**
- **类型：** `bool`
- **成功情况：** 删除成功返回 `true`
- **失败情况：** 如果不存在返回 `false`
- **可能的异常：** 无
- **示例值：** `true` 或 `false`

**使用示例：**

```csharp
var manager = new GamePropertyManager();
var temp = new CombinePropertySingle("temp", 100f);
manager.AddOrUpdate(temp);

bool removed = manager.Remove("temp");
Debug.Log($"删除成功: {removed}"); // 输出：True
```

---

##### `CleanupInvalidProperties()`

清理所有无效的属性。

```csharp
public int CleanupInvalidProperties()
```

**参数说明：** 无

**返回值：**
- **类型：** `int`
- **成功情况：** 返回清理的无效属性数量
- **失败情况：** 无
- **可能的异常：** 无
- **示例值：** `3`

**使用示例：**

```csharp
var manager = new GamePropertyManager();
// ... 添加和释放一些属性 ...
int cleaned = manager.CleanupInvalidProperties();
Debug.Log($"清理了 {cleaned} 个无效属性");
```

---

## 修饰符类

### `FloatModifier` 类

浮点数修饰符。

#### 构造函数

```csharp
public FloatModifier(ModifierType type, int priority, float value)
```

**参数说明：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `type` | `ModifierType` | 必填 | 修饰符类型 | - |
| `priority` | `int` | 必填 | 优先级（同类型修饰符中的执行顺序） | - |
| `value` | `float` | 必填 | 修饰符的值 | - |

**使用示例：**

```csharp
var addModifier = new FloatModifier(ModifierType.Add, 0, 50f);
var mulModifier = new FloatModifier(ModifierType.Mul, 1, 1.5f);
```

---

### `RangeModifier` 类

范围修饰符（用于限制属性值范围或随机值）。

#### 构造函数

```csharp
public RangeModifier(ModifierType type, int priority, Vector2 range)
```

**参数说明：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `type` | `ModifierType` | 必填 | 修饰符类型（通常为 `Clamp`） | - |
| `priority` | `int` | 必填 | 优先级 | - |
| `range` | `Vector2` | 必填 | 范围值（x 为最小值，y 为最大值） | - |

**使用示例：**

```csharp
var clampModifier = new RangeModifier(ModifierType.Clamp, 0, new Vector2(0, 100));
```

---

## 枚举类型

### `ModifierType` 枚举

修饰符类型。

```csharp
public enum ModifierType
{
    None,           // 无效类型
    Add,            // 加法修饰符
    PriorityAdd,    // 优先级加法修饰符（先于普通加法执行）
    Mul,            // 乘法修饰符
    PriorityMul,    // 优先级乘法修饰符（先于普通乘法执行）
    AfterAdd,       // 后加法修饰符（在所有乘法后执行）
    Override,       // 覆盖修饰符（直接覆盖基础值）
    Clamp           // 范围限制修饰符
}
```

**执行顺序：**
1. `Override`
2. `PriorityAdd`
3. `Add`
4. `PriorityMul`
5. `Mul`
6. `AfterAdd`
7. `Clamp`

---

## 序列化相关

### `GamePropertySerializationInitializer` 类

序列化初始化器。

#### 方法

##### `ManualInitialize()`

手动初始化序列化系统。

```csharp
public static void ManualInitialize()
```

**参数说明：** 无

**返回值：** 无（`void`）

**使用示例：**

```csharp
// 在游戏启动时或首次序列化前调用
GamePropertySerializationInitializer.ManualInitialize();
```

---

### `SerializationServiceManager` 类

序列化服务管理器。

#### 方法

##### `SerializeToJson()`

序列化对象为 JSON 字符串。

```csharp
public static string SerializeToJson<T>(T obj)
```

**参数说明：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `obj` | `T` | 必填 | 要序列化的对象 | - |

**返回值：**
- **类型：** `string`
- **成功情况：** 返回 JSON 字符串
- **失败情况：** 无
- **可能的异常：** 序列化异常
- **示例值：** `"{\"ID\":\"health\",\"BaseValue\":100,...}"`

**使用示例：**

```csharp
var prop = new GameProperty("test", 100f);
string json = SerializationServiceManager.SerializeToJson(prop);
```

---

##### `DeserializeFromJson()`

从 JSON 字符串反序列化对象。

```csharp
public static T DeserializeFromJson<T>(string json)
```

**参数说明：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `json` | `string` | 必填 | JSON 字符串 | - |

**返回值：**
- **类型：** `T`
- **成功情况：** 返回反序列化后的对象
- **失败情况：** 无
- **可能的异常：** 反序列化异常
- **示例值：** `GameProperty` 实例

**使用示例：**

```csharp
string json = "{...}";
var prop = SerializationServiceManager.DeserializeFromJson<GameProperty>(json);
```

---

## 相关资源

- [用户使用指南](./UserGuide.md) - 快速开始和常见场景
- [Mermaid 图集](./Diagrams.md) - 系统架构和数据流可视化

---

**维护者：** EasyPack 团队  
**联系方式：** 提交 GitHub Issue 或 Pull Request  
**许可证：** 遵循项目主许可证
