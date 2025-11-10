# Inventory System - API 参考文档 (Part 4)

**适用EasyPack版本：** EasyPack v1.7.0  
**最后更新：** 2025-11-10

---

## 概述

本文档是 **Inventory System** API 参考文档的第四部分，包含条件、工具与序列化系统。

**文档系列：**
- **Part 1 - 核心接口与服务类**：系统的服务层和核心接口定义
- **Part 2 - 容器实现类**：容器的具体实现类和缓存服务
- **Part 3 - 物品与网格系统**：物品类、网格物品和查询服务
- **Part 4 - 条件、工具与序列化**（当前文档）：条件系统、工具类和序列化支持

---

## 目录

- [概述](#概述)
- [条件类](#条件类)
  - [IItemCondition 接口](#iitemcondition-接口)
  - [ISerializableCondition 接口](#iserializablecondition-接口)
  - [NotCondition 类](#notcondition-类)
  - [ItemTypeCondition 类](#itemtypecondition-类)
  - [AttributeCondition 类](#attributecondition-类)
  - [CustomItemCondition 类](#customitemcondition-类)
  - [AllCondition 类](#allcondition-类)
  - [AnyCondition 类](#anycondition-类)
- [工具类](#工具类)
  - [CustomDataUtility 类](#customdatautility-类)
  - [IItemExtensions 类](#iitemextensions-类)
- [序列化类](#序列化类)
  - [SerializedCondition 类](#serializedcondition-类)
  - [SerializedContainer 类](#serializedcontainer-类)
  - [SerializedItem 类](#serializeditem-类)
  - [SerializedSlot 类](#serializedslot-类)
  - [ContainerJsonSerializer 类](#containerjsonserializer-类)
  - [ItemJsonSerializer 类](#itemjsonserializer-类)
  - [GridItemJsonSerializer 类](#griditemjsonserializer-类)
- [枚举类型](#枚举类型)
- [延伸阅读](#延伸阅读)

---

### IItemCondition 接口

**命名空间：** `EasyPack.InventorySystem`

**说明：**  
物品条件接口，用于过滤和验证物品。所有条件类都实现此接口。

---

#### 方法

##### `bool CheckCondition(IItem item)`

**说明：** 检查物品是否满足条件。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `item` | `IItem` | 必填 | 要检查的物品 | - |

**返回值：**
- **类型：** `bool`
- **说明：** `true` 表示满足条件，`false` 表示不满足

---

### ISerializableCondition 接口

**命名空间：** `EasyPack.InventorySystem`

**继承关系：**
```
IItemCondition
  └─ ISerializableCondition
```

**说明：**  
可序列化条件接口，扩展 IItemCondition 以支持序列化和反序列化。实现此接口的条件可以被保存和加载。

---

#### 属性

##### `string Kind { get; }`

**说明：** 条件类型标识符，用于序列化时的类型识别。

**类型：** `string`

---

#### 方法

##### `bool CheckCondition(IItem item)`

**说明：** 检查物品是否满足条件（继承自 IItemCondition）。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `item` | `IItem` | 必填 | 要检查的物品 | - |

**返回值：**
- **类型：** `bool`
- **说明：** `true` 表示满足条件，`false` 表示不满足

---

##### `SerializedCondition ToDto()`

**说明：** 将条件转换为序列化数据传输对象。

**返回值：**
- **类型：** `SerializedCondition`
- **说明：** 序列化的条件数据

---

##### `ISerializableCondition FromDto(SerializedCondition dto)`

**说明：** 从序列化数据传输对象恢复条件实例。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `dto` | `SerializedCondition` | 必填 | 序列化的条件数据 | - |

**返回值：**
- **类型：** `ISerializableCondition`
- **说明：** 恢复的条件实例

---

### NotCondition 类

**命名空间：** `EasyPack.InventorySystem`

**继承关系：**
```
IItemCondition
  └─ ISerializableCondition
      └─ NotCondition
```

**说明：**  
对单一子条件取反的条件类。子条件不成立时为真；如果内部条件为 null，视为真。支持序列化。

---

#### 属性

##### `IItemCondition Inner { get; set; }`

**说明：** 要取反的内部条件。

**类型：** `IItemCondition`

---

##### `string Kind { get; }`

**说明：** 条件类型标识符，用于序列化时的类型识别。返回 "Not"。

**类型：** `string`

---

#### 构造函数

##### `NotCondition()`

**说明：** 默认构造函数。

---

##### `NotCondition(IItemCondition inner)`

**说明：** 使用指定的内部条件初始化。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `inner` | `IItemCondition` | 必填 | 要取反的内部条件 | - |

---

#### 方法

##### `bool CheckCondition(IItem item)`

**说明：** 检查物品是否满足条件。如果内部条件为 null 则返回 true；否则返回内部条件的取反结果。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `item` | `IItem` | 必填 | 要检查的物品 | - |

**返回值：**
- **类型：** `bool`
- **说明：** `true` 表示满足条件（内部条件不成立或为 null），`false` 表示不满足

---

##### `NotCondition Set(IItemCondition inner)`

**说明：** 设置内部条件并返回自身，支持链式调用。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `inner` | `IItemCondition` | 必填 | 要设置的内部条件 | - |

**返回值：**
- **类型：** `NotCondition`
- **说明：** 返回自身，支持链式调用

---

##### `SerializedCondition ToDto()`

**说明：** 将条件转换为序列化数据传输对象。

**返回值：**
- **类型：** `SerializedCondition`
- **说明：** 序列化的条件数据

---

##### `ISerializableCondition FromDto(SerializedCondition dto)`

**说明：** 从序列化数据传输对象恢复条件实例。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `dto` | `SerializedCondition` | 必填 | 序列化的条件数据 | - |

**返回值：**
- **类型：** `ISerializableCondition`
- **说明：** 恢复的条件实例

---

#### 使用示例

```csharp
// 创建一个取反条件：物品类型不是武器
var weaponCondition = new ItemTypeCondition(ItemType.Weapon);
var notWeaponCondition = new NotCondition(weaponCondition);

// 或者使用链式调用
var notWeaponCondition2 = new NotCondition().Set(new ItemTypeCondition(ItemType.Weapon));

// 检查物品
bool result = notWeaponCondition.CheckCondition(someItem);
```

---

### ItemTypeCondition 类

**命名空间：** `EasyPack.InventorySystem`

**实现接口：** `IItemCondition`, `ISerializableCondition`

**说明：**  
按物品类型过滤的条件。

---

#### 构造函数

##### `ItemTypeCondition(string itemType)`

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemType` | `string` | 必填 | 物品类型 | - |

**使用示例：**

```csharp
var weaponCondition = new ItemTypeCondition("Weapon");
var isWeapon = weaponCondition.CheckCondition(item);
```

---

### AttributeCondition 类

**命名空间：** `EasyPack.InventorySystem`

**实现接口：** `IItemCondition`, `ISerializableCondition`

**说明：**  
按物品属性（Attributes）过滤的条件。

---

#### 构造函数

##### `AttributeCondition(string attributeKey, object attributeValue)`

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `attributeKey` | `string` | 必填 | 属性键 | - |
| `attributeValue` | `object` | 必填 | 属性值 | - |

**使用示例：**

```csharp
var legendaryCondition = new AttributeCondition("Rarity", "Legendary");
var isLegendary = legendaryCondition.CheckCondition(item);
```

---

### CustomItemCondition 类

**命名空间：** `EasyPack.InventorySystem`

**实现接口：** `IItemCondition`

**说明：**  
自定义条件，使用 lambda 表达式或委托。

---

#### 构造函数

##### `CustomItemCondition(Func<IItem, bool> conditionFunc)`

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `conditionFunc` | `Func<IItem, bool>` | 必填 | 条件判断函数 | - |

**使用示例：**

```csharp
var highLevelCondition = new CustomItemCondition(item =>
{
    int level = item.GetCustomData<int>("Level", 0);
    return level >= 50;
});

var isHighLevel = highLevelCondition.CheckCondition(item);
```

---

### AllCondition 类

**命名空间：** `EasyPack.InventorySystem`

**实现接口：** `IItemCondition`, `ISerializableCondition`

**说明：**  
组合条件，要求所有子条件都满足（AND 逻辑）。

---

#### 构造函数

##### `AllCondition(params IItemCondition[] conditions)`

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `conditions` | `IItemCondition[]` | 必填 | 子条件数组 | - |

**使用示例：**

```csharp
var allCondition = new AllCondition(
    new ItemTypeCondition("Weapon"),
    new AttributeCondition("Rarity", "Legendary")
);

// 只接受传奇武器
```

---

### AnyCondition 类

**命名空间：** `EasyPack.InventorySystem`

**实现接口：** `IItemCondition`, `ISerializableCondition`

**说明：**  
组合条件，要求任一子条件满足即可（OR 逻辑）。

---

#### 构造函数

##### `AnyCondition(params IItemCondition[] conditions)`

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `conditions` | `IItemCondition[]` | 必填 | 子条件数组 | - |

**使用示例：**

```csharp
var anyCondition = new AnyCondition(
    new ItemTypeCondition("Weapon"),
    new ItemTypeCondition("Armor")
);

// 接受武器或护甲
```

---

## 工具类

### CustomDataUtility 类

**命名空间：** `EasyPack`

**说明：**  
CustomData 工具类，提供 `List<CustomDataEntry>` 的常用操作方法。所有方法均为静态方法，可直接调用。

**特性：**
- 统一的数据访问接口
- 类型安全的泛型方法
- 支持批量操作
- 提供深拷贝功能

---

#### 主要方法

##### `T GetValue<T>(List<CustomDataEntry> entries, string id, T defaultValue = default)`

**说明：** 获取自定义数据值，如果不存在则返回默认值。

**参数：**
- `entries` - CustomData 列表
- `id` - 数据键
- `defaultValue` - 默认值

**返回值：** 找到的值或默认值

**使用示例：**
```csharp
var entries = new List<CustomDataEntry>();
int level = CustomDataUtility.GetValue(entries, "Level", 1);
```

---

##### `bool TryGetValue<T>(IEnumerable<CustomDataEntry> entries, string id, out T value)`

**说明：** 尝试获取自定义数据值。

**参数：**
- `entries` - CustomData 列表
- `id` - 数据键
- `value` - 输出值

**返回值：** 如果找到并成功转换返回 true，否则返回 false

---

##### `void SetValue(List<CustomDataEntry> entries, string id, object value)`

**说明：** 设置自定义数据值（如果已存在则更新，不存在则添加）。

**参数：**
- `entries` - CustomData 列表
- `id` - 数据键
- `value` - 数据值

---

##### `bool RemoveValue(List<CustomDataEntry> entries, string id)`

**说明：** 移除自定义数据。

**参数：**
- `entries` - CustomData 列表
- `id` - 数据键

**返回值：** 如果移除成功返回 true，否则返回 false

---

##### `bool HasValue(List<CustomDataEntry> entries, string id)`

**说明：** 检查是否存在指定的自定义数据。

**参数：**
- `entries` - CustomData 列表
- `id` - 数据键

**返回值：** 如果存在返回 true，否则返回 false

---

##### `List<CustomDataEntry> Clone(List<CustomDataEntry> source)`

**说明：** 深拷贝 CustomData 列表。

**参数：**
- `source` - 源列表

**返回值：** 拷贝后的新列表

**使用示例：**
```csharp
var original = new List<CustomDataEntry>();
var cloned = CustomDataUtility.Clone(original);
```

---

##### `void SetValues(List<CustomDataEntry> entries, Dictionary<string, object> values)`

**说明：** 批量设置多个自定义数据。

**参数：**
- `entries` - CustomData 列表
- `values` - 要设置的键值对

**使用示例：**
```csharp
var entries = new List<CustomDataEntry>();
CustomDataUtility.SetValues(entries, new Dictionary<string, object>
{
    { "Level", 10 },
    { "Rarity", "Rare" },
    { "Durability", 85f }
});
```

---

##### `IEnumerable<string> GetKeys(List<CustomDataEntry> entries)`

**说明：** 获取所有数据的键。

**参数：**
- `entries` - CustomData 列表

**返回值：** 键的集合

---

##### `List<CustomDataEntry> ToEntries(Dictionary<string, object> dict, ICustomDataSerializer fallbackSerializer = null)`

**说明：** 将字典转换为 CustomDataEntry 列表。

**参数：**
- `dict` - 源字典
- `fallbackSerializer` - 可选的自定义序列化器

**返回值：** CustomDataEntry 列表

---

##### `Dictionary<string, object> ToDictionary(IEnumerable<CustomDataEntry> entries)`

**说明：** 将 CustomDataEntry 列表转换为字典。

**参数：**
- `entries` - CustomDataEntry 列表

**返回值：** 字典

---

### IItemExtensions 类

**命名空间：** `EasyPack.InventorySystem`

**说明：**  
IItem 接口的扩展方法类，提供便捷的自定义数据操作功能。通过扩展方法的方式为物品对象添加数据存取功能。

---

#### 方法

##### `T GetCustomData<T>(this IItem item, string id, T defaultValue = default)`

**说明：** 获取物品的自定义数据值。

**类型参数：**
- `T` - 期望的值类型

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `item` | `IItem` | 必填 | 物品实例 | - |
| `id` | `string` | 必填 | 数据键 | - |
| `defaultValue` | `T` | 可选 | 默认值 | `default(T)` |

**返回值：**
- **类型：** `T`
- **说明：** 找到的值或默认值

---

##### `void SetCustomData(this IItem item, string id, object value)`

**说明：** 设置物品的自定义数据值。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `item` | `IItem` | 必填 | 物品实例 | - |
| `id` | `string` | 必填 | 数据键 | - |
| `value` | `object` | 必填 | 要设置的值 | - |

**返回值：** 无

---

##### `bool RemoveCustomData(this IItem item, string id)`

**说明：** 移除物品的自定义数据。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `item` | `IItem` | 必填 | 物品实例 | - |
| `id` | `string` | 必填 | 数据键 | - |

**返回值：**
- **类型：** `bool`
- **说明：** 是否成功移除

---

##### `bool HasCustomData(this IItem item, string id)`

**说明：** 检查物品是否存在指定的自定义数据。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `item` | `IItem` | 必填 | 物品实例 | - |
| `id` | `string` | 必填 | 数据键 | - |

**返回值：**
- **类型：** `bool`
- **说明：** 是否存在指定的自定义数据

---

#### 使用示例

```csharp
// 创建物品
var sword = new Item { ID = "sword", Name = "铁剑" };

// 设置自定义数据
sword.SetCustomData("Level", 5);
sword.SetCustomData("Durability", 100);

// 获取自定义数据
int level = sword.GetCustomData("Level", 1); // 返回 5
float durability = sword.GetCustomData("Durability", 0f); // 返回 100
string rarity = sword.GetCustomData("Rarity", "Common"); // 返回 "Common" (默认值)

// 检查数据存在性
bool hasLevel = sword.HasCustomData("Level"); // true

// 移除数据
bool removed = sword.RemoveCustomData("Durability"); // true
```

---

## 序列化类

### SerializedCondition 类

**命名空间：** `EasyPack.InventorySystem`

**继承关系：**
```
System.Object
  └─ SerializedCondition (implements ISerializable)
```

**说明：**  
序列化条件数据传输对象，用于在序列化过程中表示物品条件的数据结构。支持不同类型的条件参数存储。

---

#### 属性

##### `string Kind`

**说明：** 条件类型标识符，用于反序列化时确定具体的条件类型。

**类型：** `string`

---

##### `List<CustomDataEntry> Params`

**说明：** 条件参数列表，存储条件对象的具体参数数据。

**类型：** `List<CustomDataEntry>`

---

### SerializedContainer 类

**命名空间：** `EasyPack.InventorySystem`

**继承关系：**
```
System.Object
  └─ SerializedContainer (implements ISerializable)
```

**说明：**  
序列化容器数据传输对象，用于在序列化过程中表示容器的完整状态。包含容器的基本信息、网格配置、槽位列表和条件列表。

---

#### 属性

##### `string ContainerKind`

**说明：** 容器类型标识符，用于反序列化时确定具体的容器实现类。

**类型：** `string`

---

##### `string ID`

**说明：** 容器唯一标识符。

**类型：** `string`

---

##### `string Name`

**说明：** 容器显示名称。

**类型：** `string`

---

##### `string Type`

**说明：** 容器类型分类。

**类型：** `string`

---

##### `int Capacity`

**说明：** 容器容量。

**类型：** `int`

---

##### `bool IsGrid`

**说明：** 是否为网格容器。

**类型：** `bool`

---

##### `Vector2 Grid`

**说明：** 网格容器的尺寸（宽度, 高度）。

**类型：** `Vector2`

---

##### `List<SerializedSlot> Slots`

**说明：** 容器中所有槽位的序列化数据。

**类型：** `List<SerializedSlot>`

---

##### `List<SerializedCondition> ContainerConditions`

**说明：** 容器的物品条件列表。

**类型：** `List<SerializedCondition>`

---

### SerializedItem 类

**命名空间：** `EasyPack.InventorySystem`

**继承关系：**
```
System.Object
  └─ SerializedItem (implements ISerializable)
```

**说明：**  
序列化物品数据传输对象，用于在序列化过程中表示物品的完整状态。包含物品的基本属性、堆叠信息和自定义数据。

---

#### 属性

##### `string ID`

**说明：** 物品唯一标识符。

**类型：** `string`

---

##### `string Name`

**说明：** 物品显示名称。

**类型：** `string`

---

##### `string Type`

**说明：** 物品类型分类。

**类型：** `string`

---

##### `string Description`

**说明：** 物品描述文本。

**类型：** `string`

---

##### `float Weight`

**说明：** 物品重量。

**类型：** `float`

---

##### `bool IsStackable`

**说明：** 是否可堆叠。

**类型：** `bool`

---

##### `int MaxStackCount`

**说明：** 最大堆叠数量。

**类型：** `int`

---

##### `bool isContanierItem`

**说明：** 是否为容器物品（可能包含其他物品）。

**类型：** `bool`

---

##### `List<CustomDataEntry> CustomData`

**说明：** 自定义数据列表。

**类型：** `List<CustomDataEntry>`

---

##### `List<string> ContainerIds`

**说明：** 物品所在容器的ID列表。

**类型：** `List<string>`

---

### SerializedSlot 类

**命名空间：** `EasyPack.InventorySystem`

**继承关系：**
```
System.Object
  └─ SerializedSlot (implements ISerializable)
```

**说明：**  
序列化槽位数据传输对象，用于在序列化过程中表示槽位的完整状态。包含槽位索引、物品数据和条件信息。

---

#### 属性

##### `int Index`

**说明：** 槽位索引位置。

**类型：** `int`

---

##### `string ItemJson`

**说明：** 槽位中物品的JSON序列化字符串。

**类型：** `string`

---

##### `int ItemCount`

**说明：** 槽位中物品的数量。

**类型：** `int`

---

##### `SerializedCondition SlotCondition`

**说明：** 槽位的物品条件。

**类型：** `SerializedCondition`

---

### ContainerJsonSerializer 类

**命名空间：** `EasyPack.InventorySystem`

**继承关系：**
```
JsonSerializerBase<Container>
  └─ ContainerJsonSerializer
```

**说明：**  
Container 类型的 JSON 序列化器，支持依赖注入 `ISerializationService`。

---

#### 构造函数

##### `ContainerJsonSerializer(ISerializationService serializationService)`

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `serializationService` | `ISerializationService` | 必填 | 序列化服务实例，用于递归序列化 | - |

**使用示例：**

```csharp
// 通常不需要手动创建，InventoryService 会自动注册
var serializationService = await EasyPackArchitecture.Instance.ResolveAsync<ISerializationService>();
var serializer = new ContainerJsonSerializer(serializationService);
```

---

#### 方法

##### `string SerializeToJson(Container obj)`

**说明：** 将容器序列化为 JSON 字符串。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `obj` | `Container` | 必填 | 要序列化的容器 | - |

**返回值：**
- **类型：** `string`
- **说明：** JSON 字符串，失败时返回 `null`

**使用示例：**

```csharp
// 推荐使用 ISerializationService
var serializationService = await EasyPackArchitecture.Instance.ResolveAsync<ISerializationService>();
string json = serializationService.SerializeToJson(container);
```

**序列化内容：**
- 容器类型（ContainerKind）
- 基本属性（ID、Name、Type、Capacity）
- 网格信息（IsGrid、Grid）
- 容器条件（ContainerCondition）
- 所有槽位及其物品

---

##### `Container DeserializeFromJson(string json)`

**说明：** 从 JSON 字符串反序列化为容器实例。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `json` | `string` | 必填 | JSON 字符串 | - |

**返回值：**
- **类型：** `Container`
- **说明：** 容器实例，失败时返回 `null`

**使用示例：**

```csharp
// 推荐使用 ISerializationService
var serializationService = await EasyPackArchitecture.Instance.ResolveAsync<ISerializationService>();
var container = serializationService.DeserializeFromJson<Container>(json);
```

**注意事项：**
- 会根据 `ContainerKind` 创建正确的容器类型（LinerContainer 或 GridContainer）
- 物品和条件使用注入的 `ISerializationService` 进行递归反序列化
- 反序列化失败时会在控制台输出错误信息

---

### ItemJsonSerializer 类

**命名空间：** `EasyPack.InventorySystem`

**继承关系：**
```
JsonSerializerBase<Item>
  └─ ItemJsonSerializer
```

**说明：**  
Item 类型的 JSON 序列化器。

---

#### 方法

##### `string SerializeToJson(Item obj)`

**说明：** 将物品序列化为 JSON 字符串。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `obj` | `Item` | 必填 | 要序列化的物品 | - |

**返回值：**
- **类型：** `string`
- **说明：** JSON 字符串，失败时返回 `null`

**使用示例：**

```csharp
var serializationService = await EasyPackArchitecture.Instance.ResolveAsync<ISerializationService>();
string json = serializationService.SerializeToJson(item);
```

**序列化内容：**
- 基本属性（ID、Name、Type、Description、Weight）
- 堆叠设置（IsStackable、MaxStackCount）
- 容器物品标记（IsContainerItem、ContainerIds）
- 自定义属性（CustomData，使用 CustomDataUtility 转换）

---

##### `Item DeserializeFromJson(string json)`

**说明：** 从 JSON 字符串反序列化为物品实例。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `json` | `string` | 必填 | JSON 字符串 | - |

**返回值：**
- **类型：** `Item`
- **说明：** 物品实例，失败时返回 `null`

**使用示例：**

```csharp
var serializationService = await EasyPackArchitecture.Instance.ResolveAsync<ISerializationService>();
var item = serializationService.DeserializeFromJson<Item>(json);
```

---

### GridItemJsonSerializer 类

**命名空间：** `EasyPack.InventorySystem`

**说明：**  
GridItem 类型的 JSON 序列化器，支持网格物品的形状信息序列化。

**使用方式：** 同 ItemJsonSerializer，但支持 GridItem 特有的 Shape 属性。

---

### ConditionJsonSerializer 类

**命名空间：** `EasyPack.InventorySystem`

**说明：**  
IItemCondition 的通用序列化器，支持所有实现 `ISerializableCondition` 接口的条件类型。

**支持类型：**
- `ItemTypeCondition`
- `AttributeCondition`
- `AllCondition`
- `AnyCondition`
- `NotCondition`

**使用方式：**
```csharp
var serializationService = await EasyPackArchitecture.Instance.ResolveAsync<ISerializationService>();
string json = serializationService.SerializeToJson(condition, condition.GetType());
var loaded = serializationService.DeserializeFromJson<IItemCondition>(json);
```

---

## 枚举类型

### AddItemResult 枚举

**命名空间：** `EasyPack.InventorySystem`

**说明：** 添加物品操作的结果状态。

**枚举值：**

| 枚举值 | 说明 |
|--------|------|
| `Success` | 添加成功 |
| `ItemIsNull` | 物品为 null |
| `ContainerIsFull` | 容器已满 |
| `StackLimitReached` | 达到堆叠上限 |
| `SlotNotFound` | 槽位未找到 |
| `ItemConditionNotMet` | 不满足物品条件 |
| `NoSuitableSlotFound` | 未找到合适的槽位 |
| `AddNothingLOL` | 添加数量为 0 |

---

### RemoveItemResult 枚举

**命名空间：** `EasyPack.InventorySystem`

**说明：** 移除物品操作的结果状态。

**枚举值：**

| 枚举值 | 说明 |
|--------|------|
| `Success` | 移除成功 |
| `InvalidItemId` | 无效的物品 ID |
| `ItemNotFound` | 物品未找到 |
| `SlotNotFound` | 槽位未找到 |
| `InsufficientQuantity` | 数量不足 |
| `Failed` | 移除失败（通用错误） |

---

### ServiceLifecycleState 枚举

**命名空间：** `EasyPack.ENekoFramework`

**说明：** 服务生命周期状态。

**枚举值：**

| 枚举值 | 说明 |
|--------|------|
| `Uninitialized` | 未初始化 |
| `Initializing` | 初始化中 |
| `Ready` | 就绪（可用） |
| `Paused` | 已暂停 |
| `Disposed` | 已释放 |

**使用示例：**

```csharp
var inventoryService = await EasyPackArchitecture.GetInventoryServiceAsync();
if (inventoryService.State == ServiceLifecycleState.Ready)
{
    Debug.Log("服务已就绪");
}
```

---

## 查询服务

### IItemQueryService 接口

**命名空间：** `EasyPack.InventorySystem`

**说明：**  
物品查询服务接口，提供高效的物品查询和统计功能。使用缓存优化查询性能。

---

#### 方法

##### `bool HasItem(string itemId)`

**说明：** 检查容器中是否包含指定物品。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemId` | `string` | 必填 | 物品 ID | - |

**返回值：**
- **类型：** `bool`
- **说明：** `true` 表示存在，`false` 表示不存在

---

##### `IItem GetItemReference(string itemId)`

**说明：** 获取指定物品的引用对象。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemId` | `string` | 必填 | 物品 ID | - |

**返回值：**
- **类型：** `IItem`
- **说明：** 物品引用，未找到时返回 `null`

---

##### `int GetItemTotalCount(string itemId)`

**说明：** 获取指定物品的总数量。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemId` | `string` | 必填 | 物品 ID | - |

**返回值：**
- **类型：** `int`
- **说明：** 物品总数量

---

##### `bool HasEnoughItems(string itemId, int requiredCount)`

**说明：** 检查是否有足够数量的指定物品。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemId` | `string` | 必填 | 物品 ID | - |
| `requiredCount` | `int` | 必填 | 需要的数量 | - |

**返回值：**
- **类型：** `bool`
- **说明：** `true` 表示数量足够，`false` 表示不足

---

##### `List<int> FindSlotIndices(string itemId)`

**说明：** 查找包含指定物品的所有槽位索引。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemId` | `string` | 必填 | 物品 ID | - |

**返回值：**
- **类型：** `List<int>`
- **说明：** 包含物品的槽位索引列表

---

##### `int FindFirstSlotIndex(string itemId)`

**说明：** 查找包含指定物品的第一个槽位索引。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemId` | `string` | 必填 | 物品 ID | - |

**返回值：**
- **类型：** `int`
- **说明：** 第一个槽位索引，未找到时返回 `-1`

---

##### `List<(int slotIndex, IItem item, int count)> GetItemsByType(string itemType)`

**说明：** 获取指定类型的所有物品。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemType` | `string` | 必填 | 物品类型 | - |

**返回值：**
- **类型：** `List<(int slotIndex, IItem item, int count)>`
- **说明：** 指定类型的物品列表，包含槽位索引、物品引用和数量

---

##### `List<(int slotIndex, IItem item, int count)> GetItemsByAttribute(string attributeName, object attributeValue)`

**说明：** 获取具有指定属性值的物品。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `attributeName` | `string` | 必填 | 属性名称 | - |
| `attributeValue` | `object` | 必填 | 属性值 | - |

**返回值：**
- **类型：** `List<(int slotIndex, IItem item, int count)>`
- **说明：** 符合条件的物品列表

---

##### `List<(int slotIndex, IItem item, int count)> GetItemsByName(string namePattern)`

**说明：** 获取名称包含指定模式的所有物品。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `namePattern` | `string` | 必填 | 名称模式 | - |

**返回值：**
- **类型：** `List<(int slotIndex, IItem item, int count)>`
- **说明：** 符合条件的物品列表

---

##### `List<(int slotIndex, IItem item, int count)> GetItemsWhere(Func<IItem, bool> condition)`

**说明：** 获取满足指定条件的所有物品。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `condition` | `Func<IItem, bool>` | 必填 | 条件函数 | - |

**返回值：**
- **类型：** `List<(int slotIndex, IItem item, int count)>`
- **说明：** 符合条件的物品列表

---

##### `Dictionary<string, int> GetAllItemCountsDict()`

**说明：** 获取所有物品的计数字典。

**返回值：**
- **类型：** `Dictionary<string, int>`
- **说明：** 物品ID到数量的映射字典

---

##### `List<(int slotIndex, IItem item, int count)> GetAllItems()`

**说明：** 获取容器中的所有物品。

**返回值：**
- **类型：** `List<(int slotIndex, IItem item, int count)>`
- **说明：** 所有物品的列表

---

##### `int GetUniqueItemCount()`

**说明：** 获取唯一物品的数量（不同ID的物品种类数）。

**返回值：**
- **类型：** `int`
- **说明：** 唯一物品的数量

---

##### `bool IsEmpty()`

**说明：** 检查容器是否为空。

**返回值：**
- **类型：** `bool`
- **说明：** `true` 表示容器为空，`false` 表示包含物品

---

##### `float GetTotalWeight()`

**说明：** 获取容器中所有物品的总重量。

**返回值：**
- **类型：** `float`
- **说明：** 总重量

---

### IConditionSerializer 接口

**命名空间：** `EasyPack.InventorySystem`

**说明：**  
条件序列化器接口，用于将物品条件对象与序列化数据传输对象之间进行转换。支持不同类型条件的序列化和反序列化。

---

#### 属性

##### `string Kind { get; }`

**说明：** 序列化器处理的条件类型标识符。

**类型：** `string`

---

#### 方法

##### `bool CanHandle(IItemCondition condition)`

**说明：** 检查此序列化器是否能处理指定的条件对象。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `condition` | `IItemCondition` | 必填 | 要检查的条件对象 | - |

**返回值：**
- **类型：** `bool`
- **说明：** `true` 表示能处理此条件类型，`false` 表示不能

---

##### `SerializedCondition Serialize(IItemCondition condition)`

**说明：** 将条件对象序列化为数据传输对象。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `condition` | `IItemCondition` | 必填 | 要序列化的条件对象 | - |

**返回值：**
- **类型：** `SerializedCondition`
- **说明：** 序列化后的数据传输对象

---

##### `IItemCondition Deserialize(SerializedCondition dto)`

**说明：** 从数据传输对象反序列化为条件对象。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `dto` | `SerializedCondition` | 必填 | 要反序列化的数据传输对象 | - |

**返回值：**
- **类型：** `IItemCondition`
- **说明：** 反序列化后的条件对象

---

##### `string SerializeToJson(IItemCondition condition)`

**说明：** 将条件对象序列化为 JSON 字符串。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `condition` | `IItemCondition` | 必填 | 要序列化的条件对象 | - |

**返回值：**
- **类型：** `string`
- **说明：** JSON 字符串

---

##### `IItemCondition DeserializeFromJson(string json)`

**说明：** 从 JSON 字符串反序列化为条件对象。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `json` | `string` | 必填 | JSON 字符串 | - |

**返回值：**
- **类型：** `IItemCondition`
- **说明：** 反序列化后的条件对象

---

##### `void RegisterConditionType(string kind, Type conditionType)`

**说明：** 注册条件类型。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `kind` | `string` | 必填 | 条件种类 | - |
| `conditionType` | `Type` | 必填 | 条件类型 | - |

---

##### `bool IsRegistered(string kind)`

**说明：** 检查条件类型是否已注册。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `kind` | `string` | 必填 | 条件种类 | - |

**返回值：**
- **类型：** `bool`
- **说明：** 是否已注册

---

##### `IEnumerable<string> GetRegisteredKinds()`

**说明：** 获取所有已注册的条件种类。

**返回值：**
- **类型：** `IEnumerable<string>`
- **说明：** 条件种类列表

---

##### `bool CanHandle(IItemCondition condition)`

**说明：** 检查是否可以处理指定条件。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `condition` | `IItemCondition` | 必填 | 条件对象 | - |

**返回值：**
- **类型：** `bool`
- **说明：** 是否可以处理

---

##### `SerializedCondition Serialize(IItemCondition condition)`

**说明：** 序列化条件对象。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `condition` | `IItemCondition` | 必填 | 要序列化的条件对象 | - |

**返回值：**
- **类型：** `SerializedCondition`
- **说明：** 序列化后的数据传输对象

---

##### `IItemCondition Deserialize(SerializedCondition dto)`

**说明：** 反序列化条件对象。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `dto` | `SerializedCondition` | 必填 | 要反序列化的数据传输对象 | - |

**返回值：**
- **类型：** `IItemCondition`
- **说明：** 反序列化后的条件对象

---

## 延伸阅读

- [用户使用指南](./UserGuide.md) - 查看完整使用场景和最佳实践
- [Mermaid 图集](./Diagrams.md) - 查看类关系和数据流图

---

**维护者：** NEKOPACK 团队  
**反馈渠道：** [GitHub Issues](https://github.com/CutrelyAlex/UnityEasyPack/issues)


