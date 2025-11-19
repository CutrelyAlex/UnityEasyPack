# GameProperty 系统 API 参考文档

**适用 EasyPack 版本：** EasyPack v1.7.0  
**最后更新：** 2025-11-04

---

## 目录

- [核心类](#核心类)
  - [GameProperty 类](#gameproperty-类)
  - [IGamePropertyService 接口](#igamepropertyservice-接口)
  - [GamePropertyService 类](#gamepropertyservice-类)
  - [GamePropertyManager 类](#gamepropertymanager-类)
  - [PropertyMetadata 类](#propertymetadata-类)
  - [OperationResult 类](#operationresult-类)
  - [PropertyDependencyManager 类](#propertydependencymanager-类)
- [修饰符类](#修饰符类)
  - [IModifier 接口](#imodifier-接口)
  - [FloatModifier 类](#floatmodifier-类)
  - [RangeModifier 类](#rangemodifier-类)
- [枚举类型](#枚举类型)
  - [ModifierType 枚举](#modifiertype-枚举)
- [结果类型](#结果类型)
  - [BatchModifierResult 类](#batchmodifierresult-类)
- [使用示例](#使用示例)

---

## 核心类

### GameProperty 类

表示游戏中的一个可修改数值属性，支持修饰符系统和依赖关系。

**命名空间：** `EasyPack.GamePropertySystem`

#### 构造函数

##### GameProperty(string id, float baseValue = 0f)

创建一个新的游戏属性实例。

**参数：**

| 参数名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| id | string | - | 属性的唯一标识符 |
| baseValue | float | 0f | 属性的初始基础值 |

**示例：**

```csharp
using EasyPack.GamePropertySystem;

// 创建攻击力属性，基础值 50
var attack = new GameProperty("attack", 50f);

// 创建生命值属性，基础值 100
var health = new GameProperty("health", 100f);
```

#### 属性

##### ID

```csharp
public string ID { get; }
```

获取属性的唯一标识符。

**返回值：** 字符串类型的属性 ID

**示例：**

```csharp
var prop = new GameProperty("hp", 100f);
Debug.Log(prop.ID); // 输出: hp
```

##### Modifiers

```csharp
public IReadOnlyList<IModifier> Modifiers { get; }
```

获取当前应用的所有修饰符的只读列表。

**返回值：** 修饰符列表（只读）

**示例：**

```csharp
var prop = new GameProperty("attack", 50f);
prop.AddModifier(new FloatModifier(ModifierType.Add, 100, 20f));

Debug.Log($"修饰符数量: {prop.Modifiers.Count}"); // 输出: 1
foreach (var mod in prop.Modifiers)
{
    Debug.Log($"类型: {mod.Type}, 优先级: {mod.Priority}");
}
```

##### Dependencies

```csharp
public IReadOnlyList<GameProperty> Dependencies { get; }
```

获取当前属性依赖的所有属性的只读列表。

**返回值：** 依赖属性列表（只读）

**示例：**

```csharp
var strength = new GameProperty("strength", 10f);
var maxHealth = new GameProperty("maxHealth", 0f);
maxHealth.AddDependency(strength, (dep, val) => val * 10f);

Debug.Log($"依赖数量: {maxHealth.Dependencies.Count}"); // 输出: 1
Debug.Log($"依赖属性: {maxHealth.Dependencies[0].ID}"); // 输出: strength
```

#### 核心方法

##### GetValue()

```csharp
public float GetValue()
```

获取属性的最终计算值（应用所有修饰符后）。

**返回值：** 属性的最终值（float）

**说明：**
- 首次调用或属性变脏时会重新计算
- 使用缓存机制，避免重复计算

**示例：**

```csharp
var attack = new GameProperty("attack", 50f);
Debug.Log(attack.GetValue()); // 输出: 50

attack.AddModifier(new FloatModifier(ModifierType.Add, 100, 20f));
Debug.Log(attack.GetValue()); // 输出: 70
```

##### GetBaseValue()

```csharp
public float GetBaseValue()
```

获取属性的基础值（不受修饰符影响）。

**返回值：** 属性的基础值（float）

**示例：**

```csharp
var attack = new GameProperty("attack", 50f);
attack.AddModifier(new FloatModifier(ModifierType.Add, 100, 20f));

Debug.Log(attack.GetBaseValue()); // 输出: 50
Debug.Log(attack.GetValue());     // 输出: 70
```

##### SetBaseValue(float value)

```csharp
public void SetBaseValue(float value)
```

设置属性的基础值。

**参数：**

| 参数名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| value | float | - | 新的基础值 |

**返回值：** 无

**副作用：**
- 触发 `OnBaseValueChanged` 事件
- 标记属性为脏状态
- 触发依赖此属性的其他属性更新

**示例：**

```csharp
var health = new GameProperty("health", 100f);
health.OnBaseValueChanged += (oldValue, newValue) =>
{
    Debug.Log($"基础值变化: {oldValue} -> {newValue}");
};

health.SetBaseValue(150f);
// 输出: 基础值变化: 100 -> 150
```

##### AddModifier(IModifier modifier)

```csharp
public void AddModifier(IModifier modifier)
```

添加一个修饰符到属性。

**参数：**

| 参数名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| modifier | IModifier | - | 要添加的修饰符 |

**返回值：** 无

**副作用：**
- 标记属性为脏状态
- 触发 `OnDirty` 回调
- 下次 GetValue() 时重新计算

**示例：**

```csharp
var attack = new GameProperty("attack", 50f);

// 添加固定值修饰符
var weaponBonus = new FloatModifier(ModifierType.Add, 100, 20f);
attack.AddModifier(weaponBonus);

// 添加百分比修饰符
var ValueBonus = new FloatModifier(ModifierType.Mul, 100, 1.5f);
attack.AddModifier(ValueBonus);

Debug.Log(attack.GetValue()); // 输出: 105 ((50 + 20) × 1.5)
```

##### RemoveModifier(IModifier modifier)

```csharp
public bool RemoveModifier(IModifier modifier)
```

移除指定的修饰符。

**参数：**

| 参数名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| modifier | IModifier | - | 要移除的修饰符实例 |

**返回值：**
- `true`：成功移除修饰符
- `false`：修饰符不存在

**副作用：**
- 标记属性为脏状态
- 触发 `OnDirty` 回调

**示例：**

```csharp
var attack = new GameProperty("attack", 50f);
var modifier = new FloatModifier(ModifierType.Add, 100, 20f);

attack.AddModifier(modifier);
Debug.Log(attack.GetValue()); // 输出: 70

bool removed = attack.RemoveModifier(modifier);
Debug.Log($"移除成功: {removed}"); // 输出: 移除成功: True
Debug.Log(attack.GetValue()); // 输出: 50
```

##### ClearModifiers()

```csharp
public void ClearModifiers()
```

清除所有修饰符。

**返回值：** 无

**副作用：**
- 标记属性为脏状态
- 触发 `OnDirty` 回调

**示例：**

```csharp
var attack = new GameProperty("attack", 50f);
attack.AddModifier(new FloatModifier(ModifierType.Add, 100, 20f));
attack.AddModifier(new FloatModifier(ModifierType.Mul, 100, 1.5f));

Debug.Log(attack.GetValue()); // 输出: 105

attack.ClearModifiers();
Debug.Log(attack.GetValue()); // 输出: 50
Debug.Log(attack.Modifiers.Count); // 输出: 0
```

##### AddModifiers(IEnumerable<IModifier> modifiers)

```csharp
public void AddModifiers(IEnumerable<IModifier> modifiers)
```

批量添加多个修饰符。

| 参数 | 类型 | 说明 |
|------|------|------|
| `modifiers` | `IEnumerable<IModifier>` | 要添加的修饰符集合 |

**返回值：** 无

**副作用：**
- 标记属性为脏状态
- 触发 `OnDirty` 回调

**示例：**
```csharp
var attack = new GameProperty("attack", 50f);
var modifiers = new List<IModifier>
{
    new FloatModifier(ModifierType.Add, 100, 20f),
    new FloatModifier(ModifierType.Mul, 100, 1.2f)
};

attack.AddModifiers(modifiers);
Debug.Log(attack.GetValue()); // 输出: 84 ((50 + 20) × 1.2)
```

##### RemoveModifiers(IEnumerable<IModifier> modifiers)

```csharp
public void RemoveModifiers(IEnumerable<IModifier> modifiers)
```

批量移除多个修饰符。

| 参数 | 类型 | 说明 |
|------|------|------|
| `modifiers` | `IEnumerable<IModifier>` | 要移除的修饰符集合 |

**返回值：** 无

**副作用：**
- 标记属性为脏状态
- 触发 `OnDirty` 回调

**示例：**
```csharp
var attack = new GameProperty("attack", 50f);
var mod1 = new FloatModifier(ModifierType.Add, 100, 20f);
var mod2 = new FloatModifier(ModifierType.Mul, 100, 1.2f);

attack.AddModifiers(new[] { mod1, mod2 });
Debug.Log(attack.GetValue()); // 输出: 84

attack.RemoveModifiers(new[] { mod1 });
Debug.Log(attack.GetValue()); // 输出: 60 (50 × 1.2)
```

##### HasNonClampRangeModifiers()

```csharp
public bool HasNonClampRangeModifiers()
```

检查是否存在非夹具范围修饰符。

**返回值：** `bool` - 存在返回 `true`，否则返回 `false`

**说明：**
- 用于优化计算逻辑
- 夹具修饰符会限制值的范围

**示例：**
```csharp
var health = new GameProperty("health", 100f);
health.AddModifier(new RangeModifier(ModifierType.Clamp, 100, 0f, 200f)); // 夹具修饰符

Debug.Log(health.HasNonClampRangeModifiers()); // 输出: False
```

##### ContainModifierOfType(ModifierType type)

```csharp
public bool ContainModifierOfType(ModifierType type)
```

检查是否包含指定类型的修饰符。

| 参数 | 类型 | 说明 |
|------|------|------|
| `type` | `ModifierType` | 要检查的修饰符类型 |

**返回值：** `bool` - 包含返回 `true`，否则返回 `false`

**示例：**
```csharp
var attack = new GameProperty("attack", 50f);
attack.AddModifier(new FloatModifier(ModifierType.Add, 100, 20f));

Debug.Log(attack.ContainModifierOfType(ModifierType.Add)); // 输出: True
Debug.Log(attack.ContainModifierOfType(ModifierType.Mul)); // 输出: False
```

##### GetModifierCountOfType(ModifierType type)

```csharp
public int GetModifierCountOfType(ModifierType type)
```

获取指定类型修饰符的数量。

| 参数 | 类型 | 说明 |
|------|------|------|
| `type` | `ModifierType` | 要统计的修饰符类型 |

**返回值：** `int` - 指定类型修饰符的数量

**示例：**
```csharp
var attack = new GameProperty("attack", 50f);
attack.AddModifier(new FloatModifier(ModifierType.Add, 100, 20f));
attack.AddModifier(new FloatModifier(ModifierType.Add, 100, 10f));

Debug.Log(attack.GetModifierCountOfType(ModifierType.Add)); // 输出: 2
```

#### 依赖系统方法

##### AddDependency(GameProperty dependency, DependencyCalculator calculator)

```csharp
public bool AddDependency(GameProperty dependency, DependencyCalculator calculator)
```

添加一个依赖关系，使当前属性依赖于另一个属性。

**参数：**

| 参数名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| dependency | GameProperty | - | 被依赖的属性 |
| calculator | DependencyCalculator | - | 依赖计算函数，签名为 `float Calculator(GameProperty dep, float depValue)` |

**返回值：**
- `true`：成功添加依赖
- `false`：检测到循环依赖，添加失败

**说明：**
- 系统会自动检测并阻止循环依赖
- 被依赖属性变化时，会自动触发当前属性更新

**示例：**

```csharp
var strength = new GameProperty("strength", 10f);
var maxHealth = new GameProperty("maxHealth", 0f);

// 最大生命值 = 力量 × 10
bool success = maxHealth.AddDependency(strength, (dep, val) => val * 10f);
Debug.Log($"添加成功: {success}"); // 输出: True
Debug.Log(maxHealth.GetValue()); // 输出: 100

strength.SetBaseValue(15f);
Debug.Log(maxHealth.GetValue()); // 输出: 150
```

##### RemoveDependency(GameProperty dependency)

```csharp
public bool RemoveDependency(GameProperty dependency)
```

移除对指定属性的依赖关系。

**参数：**

| 参数名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| dependency | GameProperty | - | 要移除依赖的属性 |

**返回值：**
- `true`：成功移除依赖
- `false`：依赖关系不存在

**示例：**

```csharp
var strength = new GameProperty("strength", 10f);
var maxHealth = new GameProperty("maxHealth", 0f);

maxHealth.AddDependency(strength, (dep, val) => val * 10f);
Debug.Log(maxHealth.GetValue()); // 输出: 100

bool removed = maxHealth.RemoveDependency(strength);
Debug.Log($"移除成功: {removed}"); // 输出: True
Debug.Log(maxHealth.GetValue()); // 输出: 0
```

##### ClearDependencies()

```csharp
public void ClearDependencies()
```

清除所有依赖关系。

**返回值：** 无

**示例：**

```csharp
var strength = new GameProperty("strength", 10f);
var agility = new GameProperty("agility", 8f);
var combatPower = new GameProperty("combatPower", 0f);

combatPower.AddDependency(strength, (dep, val) => val * 5f);
combatPower.AddDependency(agility, (dep, val) => val * 3f);

Debug.Log(combatPower.Dependencies.Count); // 输出: 2

combatPower.ClearDependencies();
Debug.Log(combatPower.Dependencies.Count); // 输出: 0
```

#### 事件系统

##### OnValueChanged

```csharp
public event Action<float, float> OnValueChanged;
```

属性最终值变化时触发的事件。

**事件参数：**

| 参数名 | 类型 | 说明 |
|--------|------|------|
| oldValue | float | 变化前的值 |
| newValue | float | 变化后的值 |

**触发时机：** 调用 `NotifyIfChanged()` 后，如果值确实发生了变化

**示例：**

```csharp
var health = new GameProperty("health", 100f);

health.OnValueChanged += (oldValue, newValue) =>
{
    Debug.Log($"生命值变化: {oldValue} -> {newValue}");
    
    if (newValue <= 0)
    {
        Debug.Log("角色死亡！");
    }
};

health.SetBaseValue(50f);
health.NotifyIfChanged();
// 输出: 生命值变化: 100 -> 50
```

##### OnBaseValueChanged

```csharp
public event Action<float, float> OnBaseValueChanged;
```

属性基础值变化时触发的事件。

**事件参数：**

| 参数名 | 类型 | 说明 |
|--------|------|------|
| oldValue | float | 变化前的基础值 |
| newValue | float | 变化后的基础值 |

**触发时机：** 调用 `SetBaseValue()` 时

**示例：**

```csharp
var attack = new GameProperty("attack", 50f);

attack.OnBaseValueChanged += (oldValue, newValue) =>
{
    Debug.Log($"基础攻击力变化: {oldValue} -> {newValue}");
};

attack.SetBaseValue(60f);
// 输出: 基础攻击力变化: 50 -> 60
```

##### OnDirty

```csharp
public void OnDirty(Action callback)
```

注册属性变为脏状态时的回调函数。

**参数：**

| 参数名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| callback | Action | - | 脏标记触发时的回调函数 |

**返回值：** 无

**触发时机：**
- 调用 `AddModifier()`
- 调用 `RemoveModifier()`
- 调用 `ClearModifiers()`
- 调用 `SetBaseValue()`
- 依赖的属性变化

**示例：**

```csharp
var attack = new GameProperty("attack", 50f);

attack.OnDirty(() =>
{
    Debug.Log("攻击力需要重新计算");
});

attack.AddModifier(new FloatModifier(ModifierType.Add, 100, 20f));
// 输出: 攻击力需要重新计算
```

##### OnDirtyAndValueChanged

```csharp
public void OnDirtyAndValueChanged(Action<float, float> callback)
```

注册属性变脏时立即计算并检查值是否变化的回调。

**参数：**

| 参数名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| callback | Action<float, float> | - | 回调函数，参数为 (旧值, 新值) |

**返回值：** 无

**说明：**
- 属性变脏时立即计算新值
- 如果新值与旧值不同，触发回调
- 适合需要立即响应的场景（如 UI 更新）

**示例：**

```csharp
using UnityEngine.UI;

var attack = new GameProperty("attack", 50f);
Text attackText = ...; // UI Text 组件

attack.OnDirtyAndValueChanged((oldValue, newValue) =>
{
    attackText.text = $"攻击力: {newValue:F0}";
    Debug.Log($"UI 更新: {oldValue} -> {newValue}");
});

attack.AddModifier(new FloatModifier(ModifierType.Add, 100, 20f));
// 立即输出: UI 更新: 50 -> 70
// UI Text 立即更新为 "攻击力: 70"
```

##### NotifyIfChanged()

```csharp
public void NotifyIfChanged()
```

手动触发值变化检查和事件通知。

**返回值：** 无

**说明：**
- 如果属性为脏状态，重新计算值
- 如果新值与旧值不同，触发 `OnValueChanged` 事件

**示例：**

```csharp
var health = new GameProperty("health", 100f);

health.OnValueChanged += (oldValue, newValue) =>
{
    Debug.Log($"生命值: {oldValue} -> {newValue}");
};

health.SetBaseValue(80f);
// 此时不会触发 OnValueChanged

health.NotifyIfChanged();
// 输出: 生命值: 100 -> 80
```

##### MakeDirty()

```csharp
public void MakeDirty()
```

手动标记属性为脏状态。

**返回值：** 无

**副作用：**
- 触发 `OnDirty` 回调
- 下次 `GetValue()` 时会重新计算
- 不会立即触发 `OnValueChanged` 事件

**说明：**
- 强制属性在下次访问时重新计算
- 适合批量修改后的统一更新

**示例：**
```csharp
var attack = new GameProperty("attack", 50f);

attack.OnDirty(() => Debug.Log("属性变脏"));
attack.OnValueChanged += (old, newVal) => Debug.Log($"值变化: {old} -> {newVal}");

attack.MakeDirty();
// 输出: 属性变脏
// 此时不会触发 OnValueChanged

float value = attack.GetValue();
// 触发 OnValueChanged (如果值确实变化)
```

##### RemoveOnDirty(Action action)

```csharp
public void RemoveOnDirty(Action action)
```

移除指定的脏标记回调函数。

| 参数 | 类型 | 说明 |
|------|------|------|
| `action` | `Action` | 要移除的回调函数 |

**返回值：** 无

**说明：**
- 移除通过 `OnDirty()` 注册的回调
- 必须传入相同的委托实例才能正确移除

**示例：**
```csharp
var attack = new GameProperty("attack", 50f);

void OnAttackDirty() => Debug.Log("攻击力变脏");
attack.OnDirty(OnAttackDirty);

attack.MakeDirty(); // 输出: 攻击力变脏

attack.RemoveOnDirty(OnAttackDirty);
attack.MakeDirty(); // 不再输出
```

---

### IGamePropertyService 接口

游戏属性服务接口，定义了属性管理的核心契约。

**命名空间：** `EasyPack.GamePropertySystem`  
**继承：** `IService`

#### 方法

##### Register(GameProperty property, string category = "Default", PropertyMetadata metadata = null)

注册单个游戏属性到管理器中。

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `property` | `GameProperty` | - | 要注册的属性实例 |
| `category` | `string` | `"Default"` | 属性所属的分类 |
| `metadata` | `PropertyMetadata` | `null` | 属性的元数据信息 |

**返回值：** 无

**说明：**
- 如果属性已存在，会更新其分类和元数据
- 分类支持层级结构，如 "Character.Vital"

**示例：**
```csharp
var health = new GameProperty("health", 100f);
var metadata = new PropertyMetadata { Description = "生命值" };

await _propertyService.Register(health, "Character.Vital", metadata);
```

##### RegisterRange(IEnumerable<GameProperty> properties, string category = "Default")

批量注册多个游戏属性。

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `properties` | `IEnumerable<GameProperty>` | - | 要注册的属性集合 |
| `category` | `string` | `"Default"` | 属性所属的分类 |

**返回值：** 无

**说明：**
- 批量操作，性能优于多次调用 `Register`
- 所有属性使用相同的分类

**示例：**
```csharp
var properties = new List<GameProperty>
{
    new GameProperty("strength", 10f),
    new GameProperty("agility", 8f),
    new GameProperty("intelligence", 12f)
};

await _propertyService.RegisterRange(properties, "Character.Attributes");
```

##### Get(string id)

根据 ID 获取游戏属性。

| 参数 | 类型 | 说明 |
|------|------|------|
| `id` | `string` | 属性的唯一标识符 |

**返回值：** `GameProperty` - 找到的属性实例，不存在返回 `null`

**示例：**
```csharp
var health = await _propertyService.Get("health");
if (health != null)
{
    Debug.Log($"当前生命值: {health.GetValue()}");
}
```

##### GetByCategory(string category, bool includeChildren = false)

获取指定分类下的所有属性。

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `category` | `string` | - | 分类名称 |
| `includeChildren` | `bool` | `false` | 是否包含子分类 |

**返回值：** `IEnumerable<GameProperty>` - 该分类下的属性集合

**说明：**
- `includeChildren = true` 时，会递归包含所有子分类的属性
- 如 "Character" 分类会包含 "Character.Vital"、"Character.Attributes" 等

**示例：**
```csharp
// 获取所有角色属性
var characterProps = await _propertyService.GetByCategory("Character", true);

// 获取基础属性
var baseProps = await _propertyService.GetByCategory("Character.Attributes");
```

##### GetByTag(string tag)

根据标签获取属性。

| 参数 | 类型 | 说明 |
|------|------|------|
| `tag` | `string` | 标签名称 |

**返回值：** `IEnumerable<GameProperty>` - 带有指定标签的属性集合

**示例：**
```csharp
// 获取所有可升级的属性
var upgradableProps = await _propertyService.GetByTag("Upgradable");
```

##### GetByCategoryAndTag(string category, string tag)

根据分类和标签组合获取属性。

| 参数 | 类型 | 说明 |
|------|------|------|
| `category` | `string` | 分类名称 |
| `tag` | `string` | 标签名称 |

**返回值：** `IEnumerable<GameProperty>` - 满足条件的属性集合

**示例：**
```csharp
// 获取角色战斗属性中可升级的
var upgradableCombatProps = await _propertyService.GetByCategoryAndTag("Character.Combat", "Upgradable");
```

##### GetMetadata(string id)

获取属性的元数据信息。

| 参数 | 类型 | 说明 |
|------|------|------|
| `id` | `string` | 属性 ID |

**返回值：** `PropertyMetadata` - 属性的元数据，不存在返回 `null`

**示例：**
```csharp
var metadata = await _propertyService.GetMetadata("health");
if (metadata != null)
{
    Debug.Log($"描述: {metadata.Description}");
}
```

##### GetAllPropertyIds()

获取所有已注册属性的 ID 列表。

**返回值：** `IEnumerable<string>` - 所有属性 ID 的集合

**示例：**
```csharp
var allIds = await _propertyService.GetAllPropertyIds();
Debug.Log($"总属性数量: {allIds.Count()}");
```

##### GetAllCategories()

获取所有已使用的分类名称。

**返回值：** `IEnumerable<string>` - 所有分类名称的集合

**示例：**
```csharp
var categories = await _propertyService.GetAllCategories();
foreach (var category in categories)
{
    Debug.Log($"分类: {category}");
}
```

##### Unregister(string id)

从管理器中移除指定属性。

| 参数 | 类型 | 说明 |
|------|------|------|
| `id` | `string` | 要移除的属性 ID |

**返回值：** `bool` - 成功移除返回 `true`，属性不存在返回 `false`

**示例：**
```csharp
bool removed = await _propertyService.Unregister("temporary_Value");
if (removed)
{
    Debug.Log("临时Value已移除");
}
```

##### UnregisterCategory(string category)

移除整个分类及其所有属性。

| 参数 | 类型 | 说明 |
|------|------|------|
| `category` | `string` | 要移除的分类名称 |

**返回值：** 无

**说明：**
- 会递归移除该分类及其所有子分类下的属性
- 谨慎使用，可能影响大量属性

**示例：**
```csharp
// 移除所有临时效果
await _propertyService.UnregisterCategory("Temporary");
```

##### SetCategoryActive(string category, bool active)

设置分类的激活状态，影响该分类下所有属性的行为。

| 参数 | 类型 | 说明 |
|------|------|------|
| `category` | `string` | 分类名称 |
| `active` | `bool` | 是否激活 |

**返回值：** `OperationResult<List<string>>` - 操作结果，包含成功/失败的属性ID列表

**说明：**
- 激活状态影响属性的更新和计算
- 可用于暂停/恢复某些属性系统

**示例：**
```csharp
var result = await _propertyService.SetCategoryActive("Character.Values", false);
if (result.IsFullSuccess)
{
    Debug.Log("所有Value已暂停");
}
```

##### ApplyModifierToCategory(string category, IModifier modifier)

向指定分类下的所有属性应用修饰符。

| 参数 | 类型 | 说明 |
|------|------|------|
| `category` | `string` | 分类名称 |
| `modifier` | `IModifier` | 要应用的修饰符 |

**返回值：** `OperationResult<List<string>>` - 操作结果，包含成功/失败的属性ID列表

**说明：**
- 批量操作，适合全局效果如"全体攻击力提升"
- 修饰符会添加到每个属性上

**示例：**
```csharp
// 全体属性提升20%
var Value = new FloatModifier(ModifierType.Mul, 100, 1.2f);
var result = await _propertyService.ApplyModifierToCategory("Character.Attributes", Value);
Debug.Log($"成功应用到 {result.SuccessCount} 个属性");
```

---

### GamePropertyService 类

集中管理大量游戏属性的服务，提供注册、查询、批量操作等功能。

**命名空间：** `EasyPack.GamePropertySystem`  
**接口：** `IGamePropertyManager`

#### 注册与注销

##### Register(GameProperty property, string category = null, PropertyMetadata metadata = null)

```csharp
bool Register(GameProperty property, string category = null, PropertyMetadata metadata = null)
```

注册一个属性到管理器。

**参数：**

| 参数名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| property | GameProperty | - | 要注册的属性实例 |
| category | string | null | 属性分类（支持层级，如 "Character.Vital"） |
| metadata | PropertyMetadata | null | 属性元数据 |

**返回值：**
- `true`：注册成功
- `false`：属性 ID 已存在，注册失败

**示例：**

```csharp
using System.Threading.Tasks;
using EasyPack;
using EasyPack.GamePropertySystem;

async Task Example()
{
    var manager = await EasyPackArchitecture.Instance.ResolveAsync<IGamePropertyManager>();
    
    var health = new GameProperty("health", 100f);
    
    bool success = manager.Register(health, "Character.Vital", new PropertyMetadata
    {
        DisplayName = "生命值",
        Description = "角色当前生命值",
        Tags = new[] { "vital", "displayInUI" }
    });
    
    Debug.Log($"注册成功: {success}"); // 输出: True
}
```

##### Unregister(string propertyId)

```csharp
bool Unregister(string propertyId)
```

从管理器注销一个属性。

**参数：**

| 参数名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| propertyId | string | - | 属性的 ID |

**返回值：**
- `true`：注销成功
- `false`：属性不存在

**示例：**

```csharp
var manager = await EasyPackArchitecture.Instance.ResolveAsync<IGamePropertyManager>();

manager.Register(new GameProperty("temp", 0f));
bool removed = manager.Unregister("temp");
Debug.Log($"注销成功: {removed}"); // 输出: True
```

##### Clear()

```csharp
void Clear()
```

清除所有已注册的属性。

**返回值：** 无

**示例：**

```csharp
var manager = await EasyPackArchitecture.Instance.ResolveAsync<IGamePropertyManager>();

manager.Register(new GameProperty("hp", 100f));
manager.Register(new GameProperty("mp", 50f));

Debug.Log(manager.GetAllPropertyIds().Count()); // 输出: 2

manager.Clear();
Debug.Log(manager.GetAllPropertyIds().Count()); // 输出: 0
```

#### 查询方法

##### Get(string propertyId)

```csharp
GameProperty Get(string propertyId)
```

根据 ID 获取属性实例。

**参数：**

| 参数名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| propertyId | string | - | 属性的 ID |

**返回值：**
- 成功：返回 GameProperty 实例
- 失败：返回 `null`

**示例：**

```csharp
var manager = await EasyPackArchitecture.Instance.ResolveAsync<IGamePropertyManager>();

manager.Register(new GameProperty("attack", 50f));

var prop = manager.Get("attack");
if (prop != null)
{
    Debug.Log($"攻击力: {prop.GetValue()}"); // 输出: 50
}
```

##### TryGet(string propertyId, out GameProperty property)

```csharp
bool TryGet(string propertyId, out GameProperty property)
```

尝试获取属性实例（推荐使用）。

**参数：**

| 参数名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| propertyId | string | - | 属性的 ID |
| property | GameProperty | out | 输出的属性实例 |

**返回值：**
- `true`：找到属性
- `false`：属性不存在

**示例：**

```csharp
var manager = await EasyPackArchitecture.Instance.ResolveAsync<IGamePropertyManager>();

if (manager.TryGet("attack", out var attack))
{
    Debug.Log($"攻击力: {attack.GetValue()}");
}
else
{
    Debug.Log("属性不存在");
}
```

##### GetByCategory(string category, bool includeSubcategories = false)

```csharp
IEnumerable<GameProperty> GetByCategory(string category, bool includeSubcategories = false)
```

查询指定分类下的所有属性。

**参数：**

| 参数名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| category | string | - | 分类名称 |
| includeSubcategories | bool | false | 是否包含子分类 |

**返回值：** 属性集合（IEnumerable）

**示例：**

```csharp
var manager = await EasyPackArchitecture.Instance.ResolveAsync<IGamePropertyManager>();

manager.Register(new GameProperty("hp", 100f), "Character.Vital");
manager.Register(new GameProperty("mp", 50f), "Character.Vital");
manager.Register(new GameProperty("stamina", 80f), "Character.Resource");

// 仅查询 Vital 分类
var vitalProps = manager.GetByCategory("Character.Vital");
Debug.Log($"Vital 属性数量: {vitalProps.Count()}"); // 输出: 2

// 查询 Character 及其子分类
var allCharProps = manager.GetByCategory("Character", includeSubcategories: true);
Debug.Log($"Character 所有属性数量: {allCharProps.Count()}"); // 输出: 3
```

##### GetByTag(string tag)

```csharp
IEnumerable<GameProperty> GetByTag(string tag)
```

查询包含指定标签的所有属性。

**参数：**

| 参数名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| tag | string | - | 标签名称 |

**返回值：** 属性集合（IEnumerable）

**示例：**

```csharp
var manager = await EasyPackArchitecture.Instance.ResolveAsync<IGamePropertyManager>();

manager.Register(new GameProperty("hp", 100f), "Character", new PropertyMetadata
{
    Tags = new[] { "vital", "displayInUI" }
});

manager.Register(new GameProperty("mp", 50f), "Character", new PropertyMetadata
{
    Tags = new[] { "vital" }
});

var displayProps = manager.GetByTag("displayInUI");
Debug.Log($"需要显示的属性数量: {displayProps.Count()}"); // 输出: 1
```

##### GetAllPropertyIds()

```csharp
IEnumerable<string> GetAllPropertyIds()
```

获取所有已注册属性的 ID 列表。

**返回值：** 属性 ID 集合（IEnumerable<string>）

**示例：**

```csharp
var manager = await EasyPackArchitecture.Instance.ResolveAsync<IGamePropertyManager>();

manager.Register(new GameProperty("hp", 100f));
manager.Register(new GameProperty("mp", 50f));

var ids = manager.GetAllPropertyIds();
Debug.Log($"所有属性: {string.Join(", ", ids)}");
// 输出: 所有属性: hp, mp
```

##### GetAllCategories()

```csharp
IEnumerable<string> GetAllCategories()
```

获取所有分类名称列表。

**返回值：** 分类名称集合（IEnumerable<string>）

**示例：**

```csharp
var manager = await EasyPackArchitecture.Instance.ResolveAsync<IGamePropertyManager>();

manager.Register(new GameProperty("hp", 100f), "Character.Vital");
manager.Register(new GameProperty("gold", 0f), "Economy");

var categories = manager.GetAllCategories();
Debug.Log($"所有分类: {string.Join(", ", categories)}");
// 输出: 所有分类: Character.Vital, Economy
```

#### 批量操作

##### ApplyModifierToCategory(string category, IModifier modifier, bool includeSubcategories = false)

```csharp
BatchModifierResult ApplyModifierToCategory(string category, IModifier modifier, bool includeSubcategories = false)
```

为指定分类下的所有属性应用修饰符。

**参数：**

| 参数名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| category | string | - | 分类名称 |
| modifier | IModifier | - | 要应用的修饰符 |
| includeSubcategories | bool | false | 是否包含子分类 |

**返回值：** BatchModifierResult 结果对象

**示例：**

```csharp
var manager = await EasyPackArchitecture.Instance.ResolveAsync<IGamePropertyManager>();

manager.Register(new GameProperty("hp", 100f), "Character.Vital");
manager.Register(new GameProperty("mp", 50f), "Character.Vital");

// 全体增益：所有生命值相关属性 +50%
var Value = new FloatModifier(ModifierType.Mul, 100, 1.5f);
var result = manager.ApplyModifierToCategory("Character.Vital", Value);

Debug.Log($"成功数: {result.SuccessCount}, 失败数: {result.FailureCount}");
// 输出: 成功数: 2, 失败数: 0

if (result.IsFullSuccess)
{
    Debug.Log("所有属性已应用 Value");
}
```

##### ApplyModifierToTag(string tag, IModifier modifier)

```csharp
BatchModifierResult ApplyModifierToTag(string tag, IModifier modifier)
```

为包含指定标签的所有属性应用修饰符。

**参数：**

| 参数名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| tag | string | - | 标签名称 |
| modifier | IModifier | - | 要应用的修饰符 |

**返回值：** BatchModifierResult 结果对象

**示例：**

```csharp
var manager = await EasyPackArchitecture.Instance.ResolveAsync<IGamePropertyManager>();

manager.Register(new GameProperty("hp", 100f), null, new PropertyMetadata
{
    Tags = new[] { "saveable" }
});

manager.Register(new GameProperty("mp", 50f), null, new PropertyMetadata
{
    Tags = new[] { "saveable" }
});

// 为所有可保存的属性添加调试加成
var debugBonus = new FloatModifier(ModifierType.Add, 100, 999f);
var result = manager.ApplyModifierToTag("saveable", debugBonus);

Debug.Log($"应用成功: {result.IsFullSuccess}");
```

##### RemoveModifierFromCategory(string category, IModifier modifier, bool includeSubcategories = false)

```csharp
BatchModifierResult RemoveModifierFromCategory(string category, IModifier modifier, bool includeSubcategories = false)
```

从指定分类下的所有属性移除修饰符。

**参数：**

| 参数名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| category | string | - | 分类名称 |
| modifier | IModifier | - | 要移除的修饰符 |
| includeSubcategories | bool | false | 是否包含子分类 |

**返回值：** BatchModifierResult 结果对象

**示例：**

```csharp
var manager = await EasyPackArchitecture.Instance.ResolveAsync<IGamePropertyManager>();

var Value = new FloatModifier(ModifierType.Mul, 100, 1.5f);
manager.ApplyModifierToCategory("Character.Vital", Value);

// Value 时间结束，移除修饰符
var result = manager.RemoveModifierFromCategory("Character.Vital", Value);
Debug.Log($"移除成功数: {result.SuccessCount}");
```

##### RemoveModifierFromTag(string tag, IModifier modifier)

```csharp
BatchModifierResult RemoveModifierFromTag(string tag, IModifier modifier)
```

从包含指定标签的所有属性移除修饰符。

**参数：**

| 参数名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| tag | string | - | 标签名称 |
| modifier | IModifier | - | 要移除的修饰符 |

**返回值：** BatchModifierResult 结果对象

#### 元数据管理

##### GetMetadata(string propertyId)

```csharp
PropertyMetadata GetMetadata(string propertyId)
```

获取属性的元数据。

**参数：**

| 参数名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| propertyId | string | - | 属性的 ID |

**返回值：**
- 成功：返回 PropertyMetadata 实例
- 失败：返回 `null`

**示例：**

```csharp
var manager = await EasyPackArchitecture.Instance.ResolveAsync<IGamePropertyManager>();

manager.Register(new GameProperty("hp", 100f), null, new PropertyMetadata
{
    DisplayName = "生命值",
    Description = "角色当前生命值"
});

var metadata = manager.GetMetadata("hp");
if (metadata != null)
{
    Debug.Log($"显示名称: {metadata.DisplayName}");
    Debug.Log($"描述: {metadata.Description}");
}
```

##### UpdateMetadata(string propertyId, PropertyMetadata metadata)

```csharp
bool UpdateMetadata(string propertyId, PropertyMetadata metadata)
```

更新属性的元数据。

**参数：**

| 参数名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| propertyId | string | - | 属性的 ID |
| metadata | PropertyMetadata | - | 新的元数据 |

**返回值：**
- `true`：更新成功
- `false`：属性不存在

**示例：**

```csharp
var manager = await EasyPackArchitecture.Instance.ResolveAsync<IGamePropertyManager>();

manager.Register(new GameProperty("hp", 100f));

var newMetadata = new PropertyMetadata
{
    DisplayName = "生命值",
    Description = "角色最大生命值",
    Tags = new[] { "vital", "maxValue" }
};

bool updated = manager.UpdateMetadata("hp", newMetadata);
Debug.Log($"更新成功: {updated}");
```

---

### PropertyMetadata 类

存储属性的元数据信息。

**命名空间：** `EasyPack.GamePropertySystem`

---

## 序列化类

### PropertyManagerSerializer 类

GamePropertyManager 的序列化器，实现 `ITypeSerializer<GamePropertyManager, PropertyManagerDTO>` 接口。

**命名空间：** `EasyPack`

#### 方法

##### SerializeToJson(GamePropertyManager obj)

将 GamePropertyManager 序列化为 JSON 字符串。

**参数：**

| 参数名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| obj | GamePropertyManager | - | 要序列化的管理器实例 |

**返回值：** JSON 字符串

**示例：**

```csharp
var serializer = new PropertyManagerSerializer();
string json = serializer.SerializeToJson(manager);
```

##### DeserializeFromJson(string json)

从 JSON 字符串反序列化为 GamePropertyManager。

**参数：**

| 参数名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| json | string | - | JSON 字符串 |

**返回值：** GamePropertyManager 实例

**异常：**
- `SerializationException` - JSON 格式无效或反序列化失败

**示例：**

```csharp
var serializer = new PropertyManagerSerializer();
var manager = serializer.DeserializeFromJson(json);
```

---

### GamePropertyJsonSerializer 类

专门用于 GameProperty 对象的 JSON 序列化/反序列化工具。

**命名空间：** `EasyPack.GamePropertySystem.Serializer`

#### 方法

##### SerializeToJson(GameProperty gameProperty)

```csharp
public string SerializeToJson(GameProperty gameProperty)
```

将 GameProperty 对象序列化为 JSON 字符串。

| 参数 | 类型 | 说明 |
|------|------|------|
| `gameProperty` | `GameProperty` | 要序列化的属性对象 |

**返回值：** `string` - JSON 格式的字符串

**说明：**
- 包含基础值、修饰符列表、依赖关系
- 不包含运行时状态（如脏标记）

**示例：**
```csharp
var attack = new GameProperty("attack", 50f);
attack.AddModifier(new FloatModifier(ModifierType.Add, 100, 20f));

var serializer = new GamePropertyJsonSerializer();
string json = serializer.SerializeToJson(attack);
Debug.Log(json);
// 输出: {"id":"attack","baseValue":50.0,"modifiers":[...]}
```

##### DeserializeFromJson(string json)

```csharp
public GameProperty DeserializeFromJson(string json)
```

从 JSON 字符串反序列化 GameProperty 对象。

| 参数 | 类型 | 说明 |
|------|------|------|
| `json` | `string` | JSON 格式的字符串 |

**返回值：** `GameProperty` - 反序列化的属性对象

**异常：**
- `SerializationException` - JSON 格式无效或反序列化失败

**示例：**
```csharp
var serializer = new GamePropertyJsonSerializer();
string json = "{\"id\":\"attack\",\"baseValue\":50.0,\"modifiers\":[]}";
GameProperty attack = serializer.DeserializeFromJson(json);
Debug.Log(attack.ID); // 输出: attack
```

---

### PropertyManagerDTO 类

GamePropertyService 的数据传输对象，用于序列化/反序列化。

**命名空间：** `EasyPack.GamePropertySystem.Serializer`

#### 属性

##### Properties

```csharp
public List<SerializableGameProperty> Properties { get; set; }
```

序列化的属性列表。

**类型：** `List<SerializableGameProperty>`

##### Categories

```csharp
public Dictionary<string, List<string>> Categories { get; set; }
```

分类信息，键为分类名，值为属性ID列表。

**类型：** `Dictionary<string, List<string>>`

##### Metadata

```csharp
public Dictionary<string, PropertyMetadata> Metadata { get; set; }
```

属性元数据字典。

**类型：** `Dictionary<string, PropertyMetadata>`

---

## Editor 工具

### GamePropertyManagerWindow 类

GamePropertyManager 的可视化管理窗口，通过 EasyPack 架构安全解析服务。

**命名空间：** `EasyPack.Editor`

**打开方式：** 菜单 `EasyPack/CoreSystems/游戏属性(GameProperty)/管理器窗口`

#### 解析流程

窗口在 `OnEnable` 时执行以下检查：

1. 检查服务是否已注册
2. 检查服务是否已实例化
3. 检查服务状态是否为 Ready
4. 仅在所有条件满足时才解析服务

**重要**：窗口不会主动初始化服务，只会在服务已就绪时解析。

#### 功能特性

- 实时显示所有属性的 ID、基础值、最终值
- 文本搜索（支持搜索 ID、显示名、描述）
- 分类过滤（下拉菜单）
- 标签过滤（下拉菜单）
- 可切换的元数据显示
- 可切换的修饰符显示
- 安全的服务解析（不主动初始化）

#### 使用要求

- EasyPack 架构必须已初始化
- `IGamePropertyManager` 服务必须已注册并初始化为 Ready 状态
---

### PropertyMetadata 类（继续）

存储属性的元数据信息。

**命名空间：** `EasyPack.GamePropertySystem`

#### 属性

##### DisplayName

```csharp
public string DisplayName { get; set; }
```

属性的显示名称（用于 UI 展示）。

**示例：**

```csharp
var metadata = new PropertyMetadata
{
    DisplayName = "生命值"
};
```

##### Description

```csharp
public string Description { get; set; }
```

属性的详细描述。

**示例：**

```csharp
var metadata = new PropertyMetadata
{
    Description = "角色当前生命值，归零时角色死亡"
};
```

##### Icon

```csharp
public Sprite Icon { get; set; }
```

属性的图标（Unity Sprite）。

**示例：**

```csharp
using UnityEngine;

var metadata = new PropertyMetadata
{
    Icon = Resources.Load<Sprite>("Icons/Health")
};
```

##### Tags

```csharp
public string[] Tags { get; set; }
```

属性的标签数组，用于分组和查询。

**示例：**

```csharp
var metadata = new PropertyMetadata
{
    Tags = new[] { "vital", "displayInUI", "saveable" }
};
```

---

## 修饰符类

### IModifier 接口

所有修饰符的基础接口。

**命名空间：** `EasyPack`

#### 属性

##### Type

```csharp
ModifierType Type { get; }
```

修饰符的类型。

##### Priority

```csharp
int Priority { get; }
```

修饰符的优先级（在同类型中，Priority 越高越先应用）。

#### 方法

##### Apply(float currentValue)

```csharp
float Apply(float currentValue)
```

应用修饰符到当前值。

**参数：**

| 参数名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| currentValue | float | - | 当前值 |

**返回值：** 应用修饰符后的新值

---

### FloatModifier 类

使用固定浮点数值的修饰符。

**命名空间：** `EasyPack`

#### 构造函数

##### FloatModifier(ModifierType type, int priority, float value)

```csharp
public FloatModifier(ModifierType type, int priority, float value)
```

创建一个浮点修饰符。

**参数：**

| 参数名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| type | ModifierType | - | 修饰符类型 |
| priority | int | - | 优先级 |
| value | float | - | 修饰符的数值 |

**示例：**

```csharp
using EasyPack;

// 加法修饰符：+20
var addMod = new FloatModifier(ModifierType.Add, 100, 20f);

// 乘法修饰符：×1.5
var mulMod = new FloatModifier(ModifierType.Mul, 100, 1.5f);

// 覆盖修饰符：直接设为 999
var overrideMod = new FloatModifier(ModifierType.Override, 100, 999f);
```

#### 属性

##### Value

```csharp
public float Value { get; }
```

修饰符的固定数值。

**示例：**

```csharp
var mod = new FloatModifier(ModifierType.Add, 100, 20f);
Debug.Log($"修饰符值: {mod.Value}"); // 输出: 20
```

---

### RangeModifier 类

使用范围值的修饰符，支持随机范围和限制范围。

**命名空间：** `EasyPack`

#### 构造函数

##### RangeModifier(ModifierType type, int priority, Vector2 range)

```csharp
public RangeModifier(ModifierType type, int priority, Vector2 range)
```

创建一个区间修饰符。

**参数：**

| 参数名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| type | ModifierType | - | 修饰符类型 |
| priority | int | - | 优先级 |
| range | Vector2 | - | 范围值（x 为最小值，y 为最大值） |

**示例：**

```csharp
using UnityEngine;
using EasyPack;

// 随机加法：+10 到 +30 之间的随机值
var randomAdd = new RangeModifier(ModifierType.Add, 100, new Vector2(10f, 30f));

// Clamp 限制：将值限制在 50-150 之间
var clamp = new RangeModifier(ModifierType.Clamp, 100, new Vector2(50f, 150f));
```

#### 属性

##### Range

```csharp
public Vector2 Range { get; }
```

修饰符的范围值。

**示例：**

```csharp
var mod = new RangeModifier(ModifierType.Add, 100, new Vector2(10f, 30f));
Debug.Log($"范围: {mod.Range.x} - {mod.Range.y}"); // 输出: 范围: 10 - 30
```

---

## 枚举类型

### ModifierType 枚举

定义修饰符的计算类型和应用顺序。

**命名空间：** `EasyPack`

#### 枚举值

| 枚举值 | 数值 | 说明 | 应用公式 |
|--------|------|------|----------|
| Override | 0 | 覆盖值 | `newValue = modifierValue` |
| PriorityAdd | 1 | 优先级加法 | `newValue = currentValue + modifierValue` |
| Add | 2 | 普通加法 | `newValue = currentValue + modifierValue` |
| PriorityMul | 3 | 优先级乘法 | `newValue = currentValue × modifierValue` |
| Mul | 4 | 普通乘法 | `newValue = currentValue × modifierValue` |
| AfterAdd | 5 | 后置加法 | `newValue = currentValue + modifierValue` |
| Clamp | 6 | 限制范围 | `newValue = Clamp(currentValue, min, max)` |

**应用顺序：** Override → PriorityAdd → Add → PriorityMul → Mul → AfterAdd → Clamp

**示例：**

```csharp
using EasyPack;
using EasyPack.GamePropertySystem;

var prop = new GameProperty("value", 100f);

// 应用顺序演示
prop.AddModifier(new FloatModifier(ModifierType.Add, 100, 20f));           // 第3步
prop.AddModifier(new FloatModifier(ModifierType.PriorityAdd, 100, 10f));   // 第2步
prop.AddModifier(new FloatModifier(ModifierType.Mul, 100, 1.5f));          // 第5步
prop.AddModifier(new FloatModifier(ModifierType.Override, 100, 50f));      // 第1步（覆盖基础值）

// 计算过程：
// 1. Override: 50 (覆盖基础值 100)
// 2. PriorityAdd: 50 + 10 = 60
// 3. Add: 60 + 20 = 80
// 4. PriorityMul: (无)
// 5. Mul: 80 × 1.5 = 120
// 6. AfterAdd: (无)
// 7. Clamp: (无)

Debug.Log(prop.GetValue()); // 输出: 120
```

---

## 结果类型

### OperationResult 类

通用操作结果类，用于表示批量操作的成功/失败状态。

**命名空间：** `EasyPack.GamePropertySystem`

#### 构造函数

##### OperationResult(T data, int count)

创建成功的操作结果。

| 参数 | 类型 | 说明 |
|------|------|------|
| `data` | `T` | 操作返回的数据 |
| `count` | `int` | 影响的项目数量 |

**示例：**
```csharp
var result = OperationResult.Success(propertyList, 5);
```

##### OperationResult(T data, int successCount, List<FailureRecord> failures)

创建部分成功的操作结果。

| 参数 | 类型 | 说明 |
|------|------|------|
| `data` | `T` | 操作返回的数据 |
| `successCount` | `int` | 成功的项目数量 |
| `failures` | `List<FailureRecord>` | 失败记录列表 |

**示例：**
```csharp
var failures = new List<FailureRecord> { new FailureRecord("property1", "Invalid value") };
var result = OperationResult.PartialSuccess(propertyList, 3, failures);
```

#### 属性

##### Data

```csharp
public T Data { get; }
```

获取操作返回的数据。

**类型：** `T`

##### SuccessCount

```csharp
public int SuccessCount { get; }
```

获取成功的项目数量。

**类型：** `int`

##### FailureCount

```csharp
public int FailureCount { get; }
```

获取失败的项目数量。

**类型：** `int`

##### IsSuccess

```csharp
public bool IsSuccess { get; }
```

判断操作是否完全成功（无失败项目）。

**类型：** `bool`  
**返回值：** `FailureCount == 0`

##### IsPartialSuccess

```csharp
public bool IsPartialSuccess { get; }
```

判断操作是否部分成功（有成功也有失败）。

**类型：** `bool`  
**返回值：** `SuccessCount > 0 && FailureCount > 0`

##### IsFullFailure

```csharp
public bool IsFullFailure { get; }
```

判断操作是否完全失败（无成功项目）。

**类型：** `bool`  
**返回值：** `SuccessCount == 0`

##### Failures

```csharp
public IReadOnlyList<FailureRecord> Failures { get; }
```

获取失败记录的只读列表。

**类型：** `IReadOnlyList<FailureRecord>`

#### 方法

##### Success(T data, int count)

创建成功的操作结果的静态方法。

| 参数 | 类型 | 说明 |
|------|------|------|
| `data` | `T` | 操作数据 |
| `count` | `int` | 成功数量 |

**返回值：** `OperationResult<T>`

##### PartialSuccess(T data, int successCount, List<FailureRecord> failures)

创建部分成功的操作结果的静态方法。

| 参数 | 类型 | 说明 |
|------|------|------|
| `data` | `T` | 操作数据 |
| `successCount` | `int` | 成功数量 |
| `failures` | `List<FailureRecord>` | 失败记录 |

**返回值：** `OperationResult<T>`

**示例：**
```csharp
// 完全成功
var successResult = OperationResult.Success(modifiedProperties, 10);

// 部分成功
var failures = new List<FailureRecord> { 
    new FailureRecord("health", "Value out of range") 
};
var partialResult = OperationResult.PartialSuccess(modifiedProperties, 8, failures);

// 检查结果
if (result.IsSuccess)
{
    Debug.Log("操作完全成功");
}
else if (result.IsPartialSuccess)
{
    Debug.Log($"部分成功: {result.SuccessCount}/{result.SuccessCount + result.FailureCount}");
}
```

---

### BatchModifierResult 类

批量修饰符操作的结果信息。

**命名空间：** `EasyPack.GamePropertySystem`

#### 属性

##### SuccessCount

```csharp
public int SuccessCount { get; }
```

成功应用/移除修饰符的属性数量。

##### FailureCount

```csharp
public int FailureCount { get; }
```

失败的操作数量。

##### IsFullSuccess

```csharp
public bool IsFullSuccess { get; }
```

是否所有操作都成功（FailureCount == 0）。

##### IsPartialSuccess

```csharp
public bool IsPartialSuccess { get; }
```

是否部分成功（SuccessCount > 0 且 FailureCount > 0）。

##### IsFullFailure

```csharp
public bool IsFullFailure { get; }
```

是否所有操作都失败（SuccessCount == 0）。

**示例：**

```csharp
var manager = await EasyPackArchitecture.Instance.ResolveAsync<IGamePropertyManager>();

var Value = new FloatModifier(ModifierType.Mul, 100, 1.5f);
var result = manager.ApplyModifierToCategory("Character.Vital", Value);

Debug.Log($"成功: {result.SuccessCount}, 失败: {result.FailureCount}");

if (result.IsFullSuccess)
{
    Debug.Log("所有属性已应用 Value");
}
else if (result.IsPartialSuccess)
{
    Debug.Log("部分属性应用 Value");
}
else if (result.IsFullFailure)
{
    Debug.Log("Value 应用完全失败");
}
```

---

### PropertyDependencyManager 类

管理游戏属性间的依赖关系，提供依赖解析和级联更新的功能。

**命名空间：** `EasyPack.GamePropertySystem`

#### 属性

##### DependencyDepth

```csharp
public int DependencyDepth { get; }
```

获取依赖深度（依赖链的长度）。

**类型：** `int`  
**说明：** 用于检测循环依赖和优化更新顺序

##### HasRandomDependency

```csharp
public bool HasRandomDependency { get; }
```

判断是否存在随机依赖关系。

**类型：** `bool`  
**说明：** 影响属性值的确定性和缓存策略

##### DependencyCount

```csharp
public int DependencyCount { get; }
```

获取直接依赖的数量。

**类型：** `int`

##### DependentCount

```csharp
public int DependentCount { get; }
```

获取被多少其他属性依赖的数量。

**类型：** `int`

#### 方法

##### AddDependency(GameProperty dependency, Func<GameProperty, float, float> calculator = null)

添加一个依赖关系。

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `dependency` | `GameProperty` | - | 被依赖的属性 |
| `calculator` | `Func<GameProperty, float, float>` | `null` | 计算函数，参数为(依赖属性, 依赖值) |

**返回值：** `bool` - 成功添加返回 `true`，检测到循环依赖返回 `false`

**说明：**
- 系统会自动检测并阻止循环依赖
- calculator 为 null 时使用默认的传递函数

**示例：**
```csharp
var strength = new GameProperty("strength", 10f);
var attack = new GameProperty("attack", 0f);

// 攻击力 = 力量 × 2
bool success = attack.DependencyManager.AddDependency(strength, (dep, val) => val * 2f);
```

##### RemoveDependency(GameProperty dependency)

移除对指定属性的依赖关系。

| 参数 | 类型 | 说明 |
|------|------|------|
| `dependency` | `GameProperty` | 要移除依赖的属性 |

**返回值：** `bool` - 成功移除返回 `true`，依赖不存在返回 `false`

**示例：**
```csharp
bool removed = attack.DependencyManager.RemoveDependency(strength);
```

##### TriggerDependentUpdates(float currentValue)

触发所有依赖此属性的其他属性的更新。

| 参数 | 类型 | 说明 |
|------|------|------|
| `currentValue` | `float` | 当前属性值 |

**返回值：** 无

**说明：**
- 会递归触发整个依赖链的更新
- 用于级联更新依赖属性

##### PropagateDirtyTowardsDependents()

向所有依赖此属性的属性传播脏标记。

**返回值：** 无

**说明：**
- 标记所有依赖属性为脏状态
- 下次访问时会重新计算值

##### HasDirtyDependencies()

检查是否存在脏依赖关系。

**返回值：** `bool` - 存在脏依赖返回 `true`

**说明：**
- 用于优化更新逻辑，避免不必要的计算

##### InvalidateDirtyCache()

使脏标记缓存失效。

**返回值：** 无

**说明：**
- 强制重新评估依赖状态
- 用于处理复杂的状态变化

##### UpdateDependencies()

更新所有依赖关系的状态。

**返回值：** 无

**说明：**
- 重新计算依赖链
- 更新依赖深度和随机性标记

##### UpdateRandomDependencyState()

更新随机依赖的状态。

**返回值：** 无

**说明：**
- 专门处理随机依赖的更新逻辑
- 影响缓存策略

##### ClearAll()

清除所有依赖关系。

**返回值：** 无

**说明：**
- 移除所有依赖和被依赖关系
- 重置依赖管理器状态

**示例：**
```csharp
// 清除所有依赖
property.DependencyManager.ClearAll();
Debug.Log($"依赖数量: {property.DependencyManager.DependencyCount}"); // 输出: 0
```

---

### 维护者： NEKOPACK 团队
联系方式： 提交 GitHub Issue 或 Pull Request
许可证： 遵循项目主许可证