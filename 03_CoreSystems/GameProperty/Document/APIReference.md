# GameProperty 系统 API 参考文档

**适用 EasyPack 版本：** EasyPack v1.6.0  
**最后更新：** 2025-11-04

---

## 目录

- [核心类](#核心类)
  - [GameProperty 类](#gameproperty-类)
  - [GamePropertyManager 类](#gamepropertymanager-类)
  - [PropertyMetadata 类](#propertymetadata-类)
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
var buffBonus = new FloatModifier(ModifierType.Mul, 100, 1.5f);
attack.AddModifier(buffBonus);

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

---

### GamePropertyManager 类

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
var buff = new FloatModifier(ModifierType.Mul, 100, 1.5f);
var result = manager.ApplyModifierToCategory("Character.Vital", buff);

Debug.Log($"成功数: {result.SuccessCount}, 失败数: {result.FailureCount}");
// 输出: 成功数: 2, 失败数: 0

if (result.IsFullSuccess)
{
    Debug.Log("所有属性已应用 BUFF");
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

var buff = new FloatModifier(ModifierType.Mul, 100, 1.5f);
manager.ApplyModifierToCategory("Character.Vital", buff);

// BUFF 时间结束，移除修饰符
var result = manager.RemoveModifierFromCategory("Character.Vital", buff);
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

var buff = new FloatModifier(ModifierType.Mul, 100, 1.5f);
var result = manager.ApplyModifierToCategory("Character.Vital", buff);

Debug.Log($"成功: {result.SuccessCount}, 失败: {result.FailureCount}");

if (result.IsFullSuccess)
{
    Debug.Log("所有属性已应用 BUFF");
}
else if (result.IsPartialSuccess)
{
    Debug.Log("部分属性应用 BUFF");
}
else if (result.IsFullFailure)
{
    Debug.Log("BUFF 应用完全失败");
}
```

---
维护者： NEKOPACK 团队
联系方式： 提交 GitHub Issue 或 Pull Request
许可证： 遵循项目主许可证