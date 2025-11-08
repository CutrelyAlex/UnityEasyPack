# Inventory System - API 参考文档

**适用EasyPack版本：** EasyPack v1.7.0  
**最后更新：** 2025-11-06

---

## 概述

本文档提供 **Inventory System** 的完整 API 参考。包含所有公开类、方法、属性的签名和参数说明。


---

## 目录

- [概述](#概述)
- [服务类](#服务类)
  - [IInventoryService 接口](#iinventoryservice-接口)
  - [InventoryService 类](#inventoryservice-类)
- [核心类](#核心类)
  - [Item 类](#item-类)
  - [Container 类（抽象）](#container-类抽象)
  - [LinerContainer 类](#linercontainer-类)
  - [GridContainer 类](#gridcontainer-类)
  - [GridItem 类](#griditem-类)
  - [Slot 类](#slot-类)
- [条件类](#条件类)
  - [IItemCondition 接口](#iitemcondition-接口)
  - [ItemTypeCondition 类](#itemtypecondition-类)
  - [AttributeCondition 类](#attributecondition-类)
  - [CustomItemCondition 类](#customitemcondition-类)
  - [AllCondition 类](#allcondition-类)
  - [AnyCondition 类](#anycondition-类)
- [工具类](#工具类)
  - [CustomDataUtility 类](#customdatautility-类)
- [序列化类](#序列化类)
  - [ContainerJsonSerializer 类](#containerjsonserializer-类)
  - [ItemJsonSerializer 类](#itemjsonserializer-类)
- [枚举类型](#枚举类型)
  - [AddItemResult 枚举](#additemresult-枚举)
  - [RemoveItemResult 枚举](#removeitemresult-枚举)
  - [ServiceLifecycleState 枚举](#servicelifecyclestate-枚举)
- [延伸阅读](#延伸阅读)

---

## 服务类

### IInventoryService 接口

**命名空间：** `EasyPack.InventorySystem`

**继承关系：**
```
IService (from EasyPack.ENekoFramework)
  └─ IInventoryService
```

**说明：**  
库存服务接口，定义库存系统的核心功能，包括容器管理、跨容器操作、全局搜索和条件控制。

---

#### 主要方法

##### `Task InitializeAsync()`

**说明：** 异步初始化服务，注册序列化器到 `ISerializationService`。

**返回值：** `Task`

**使用示例：**
```csharp
var service = await EasyPackArchitecture.GetInventoryServiceAsync();
// 服务已自动初始化
```

---

##### `bool RegisterContainer(Container container, int priority = 0, string category = "Default")`

**说明：** 注册容器到服务。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `container` | `Container` | 必填 | 要注册的容器 | - |
| `priority` | `int` | 可选 | 容器优先级，数值越高优先级越高 | `0` |
| `category` | `string` | 可选 | 容器分类 | `"Default"` |

**返回值：**
- **类型：** `bool`
- **成功情况：** 返回 `true`
- **失败情况：** 返回 `false`（容器为 null 或服务未就绪）

**使用示例：**
```csharp
var backpack = new LinerContainer("player_backpack", "背包", "Backpack", 20);
bool success = inventoryService.RegisterContainer(backpack, priority: 1, category: "Player");
```

---

##### `(MoveResult result, int transferredCount) TransferItems(string itemId, int count, string fromContainerId, string toContainerId)`

**说明：** 跨容器转移指定数量的物品。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemId` | `string` | 必填 | 物品 ID | - |
| `count` | `int` | 必填 | 转移数量 | - |
| `fromContainerId` | `string` | 必填 | 源容器 ID | - |
| `toContainerId` | `string` | 必填 | 目标容器 ID | - |

**返回值：**
- **类型：** `(MoveResult result, int transferredCount)`
- **成功情况：** `result = MoveResult.Success`, `transferredCount` 为实际转移数量
- **失败情况：** `result` 为失败原因，`transferredCount = 0`

**使用示例：**
```csharp
var (result, count) = inventoryService.TransferItems("health_potion", 10, "backpack", "warehouse");
if (result == InventoryService.MoveResult.Success)
{
    Debug.Log($"成功转移 {count} 个物品");
}
```

---

##### `List<GlobalItemResult> FindItemGlobally(string itemId)`

**说明：** 在所有注册的容器中搜索指定物品。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemId` | `string` | 必填 | 物品 ID | - |

**返回值：**
- **类型：** `List<GlobalItemResult>`
- **说明：** 包含容器 ID、槽位索引、物品引用和数量的结果列表

**使用示例：**
```csharp
var results = inventoryService.FindItemGlobally("health_potion");
foreach (var result in results)
{
    Debug.Log($"在容器 {result.ContainerId} 的槽位 {result.SlotIndex} 找到 {result.Count} 个");
}
```

---

### InventoryService 类

**命名空间：** `EasyPack.InventorySystem`

**继承关系：**
```
System.Object
  └─ InventoryService (implements IInventoryService)
```

**说明：**  
库存服务的具体实现，负责管理多个容器、执行跨容器操作、全局搜索和条件控制。

---

#### 属性

##### `ServiceLifecycleState State { get; }`

**说明：** 服务当前的生命周期状态。

**类型：** `ServiceLifecycleState`

**可能值：**
- `Uninitialized` - 未初始化
- `Initializing` - 初始化中
- `Ready` - 就绪
- `Paused` - 已暂停
- `Disposed` - 已释放

---

##### `int ContainerCount { get; }`

**说明：** 当前注册的容器数量。

**类型：** `int`

---

#### 方法

##### `void SetGlobalConditionsEnabled(bool enable)`

**说明：** 启用或禁用全局物品条件。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `enable` | `bool` | 必填 | 是否启用全局条件 | - |

**使用示例：**
```csharp
// 添加全局条件
inventoryService.AddGlobalItemCondition(new ItemTypeCondition("Weapon"));

// 启用全局条件（所有容器将应用此条件）
inventoryService.SetGlobalConditionsEnabled(true);
```

---

##### `void Reset()`

**说明：** 重置服务状态，清空所有容器、条件和缓存，但保留服务的初始化状态。

**使用示例：**
```csharp
inventoryService.Reset(); // 清空所有数据但服务仍可用
```

---

## 核心类

### Item 类

**命名空间：** `EasyPack.InventorySystem`

**继承关系：**
```
System.Object
  └─ Item (implements IItem)
```

**说明：**  
代表游戏中的物品实例，包含物品的基本属性（ID、名称、类型）、堆叠设置、重量、自定义属性等。

---

#### 属性

##### `string ID { get; set; }`

**说明：** 物品的唯一标识符，用于区分不同物品。

**类型：** `string`

**默认值：** `null`

**使用示例：**
```csharp
var item = new Item { ID = "health_potion" };
```

---

##### `string Name { get; set; }`

**说明：** 物品的显示名称。

**类型：** `string`

**默认值：** `null`

---

##### `string Type { get; set; }`

**说明：** 物品类型，用于分类和条件过滤。

**类型：** `string`

**默认值：** `"Default"`

**使用示例：**
```csharp
var sword = new Item { ID = "sword", Type = "Weapon" };
var potion = new Item { ID = "potion", Type = "Consumable" };
```

---

##### `string Description { get; set; }`

**说明：** 物品描述文本。

**类型：** `string`

**默认值：** `""`

---

##### `float Weight { get; set; }`

**说明：** 物品重量，可用于负重系统。

**类型：** `float`

**默认值：** `1f`

---

##### `bool IsStackable { get; set; }`

**说明：** 物品是否可堆叠。

**类型：** `bool`

**默认值：** `true`

**使用示例：**
```csharp
var arrow = new Item { IsStackable = true, MaxStackCount = 20 };
var sword = new Item { IsStackable = false };
```

---

##### `int MaxStackCount { get; set; }`

**说明：** 单个槽位的最大堆叠数量，`-1` 表示无限堆叠。

**类型：** `int`

**默认值：** `-1`

---

##### `List<CustomDataEntry> CustomData { get; set; }`

**说明：** 物品自定义数据列表，使用 EasyPack 的 CustomDataEntry 系统存储多种类型的数据。  
支持的类型包括：int、float、bool、string、Vector2、Vector3、Color 以及自定义 JSON 序列化对象。

**类型：** `List<CustomDataEntry>`

**默认值：** 空列表

**使用示例：**
```csharp
var item = new Item();

// 设置自定义数据
item.SetCustomData("Rarity", "Legendary");
item.SetCustomData("Level", 50);
item.SetCustomData("Durability", 100f);
item.SetCustomData("Position", new Vector3(1, 2, 3));

// 获取自定义数据
string rarity = item.GetCustomData<string>("Rarity");
int level = item.GetCustomData<int>("Level", defaultValue: 1);
float durability = item.GetCustomData<float>("Durability");

// 检查是否存在
if (item.HasCustomData("Level"))
{
    Debug.Log("物品有等级属性");
}

// 移除自定义数据
item.RemoveCustomData("Durability");
```

**迁移说明：**  
旧版本使用 `Dictionary<string, object> Attributes`，新版本改用 `List<CustomDataEntry> CustomData`。  
如果需要访问旧数据，请使用 `GetCustomData<T>()` 和 `SetCustomData()` 方法。

---

##### `bool IsContanierItem { get; set; }`

**说明：** 标记该物品是否为容器类物品（如背包、箱子）。

**类型：** `bool`

**默认值：** `false`

---

##### `List<string> ContainerIds { get; set; }`

**说明：** 容器类物品关联的容器 ID 列表。

**类型：** `List<string>`

**默认值：** `null`

---

#### 方法

##### `IItem Clone()`

**说明：** 创建物品的深拷贝副本，包括所有自定义数据。

**返回值：**
- **类型：** `IItem`
- **说明：** 返回一个新的物品实例，包含所有属性和自定义数据的副本

**使用示例：**
```csharp
var originalItem = new Item { ID = "potion", Name = "药水" };
originalItem.SetCustomData("Quality", 5);

var clonedItem = originalItem.Clone();

// 修改克隆不影响原物品
clonedItem.Name = "高级药水";
Debug.Log(originalItem.Name); // "药水"
```

---

##### `T GetCustomData<T>(string id, T defaultValue = default)`

**说明：** 获取指定 ID 的自定义数据值。

**参数：**
- `id` - 自定义数据的标识符
- `defaultValue` - 如果数据不存在时返回的默认值

**返回值：**
- **类型：** `T`
- **说明：** 返回指定类型的数据值，如果不存在或类型不匹配则返回默认值

**使用示例：**
```csharp
int level = item.GetCustomData<int>("Level", defaultValue: 1);
string rarity = item.GetCustomData<string>("Rarity", defaultValue: "Common");
```

---

##### `void SetCustomData(string id, object value)`

**说明：** 设置自定义数据值。如果 ID 已存在，则更新值；否则添加新条目。

**参数：**
- `id` - 自定义数据的标识符
- `value` - 要存储的值（支持 int、float、bool、string、Vector2、Vector3、Color 及 JSON 序列化对象）

**使用示例：**
```csharp
item.SetCustomData("Level", 50);
item.SetCustomData("Rarity", "Legendary");
item.SetCustomData("Position", new Vector3(1, 2, 3));
```

---

##### `bool RemoveCustomData(string id)`

**说明：** 移除指定 ID 的自定义数据。

**参数：**
- `id` - 要移除的自定义数据标识符

**返回值：**
- **类型：** `bool`
- **说明：** 如果成功移除返回 `true`，如果数据不存在返回 `false`

**使用示例：**
```csharp
bool removed = item.RemoveCustomData("Durability");
```

---

##### `bool HasCustomData(string id)`

**说明：** 检查是否存在指定 ID 的自定义数据。

**参数：**
- `id` - 要检查的自定义数据标识符

**返回值：**
- **类型：** `bool`
- **说明：** 如果存在返回 `true`，否则返回 `false`

**使用示例：**
```csharp
if (item.HasCustomData("Level"))
{
    int level = item.GetCustomData<int>("Level");
    Debug.Log($"物品等级: {level}");
}
```

---

### Container 类（抽象）

**命名空间：** `EasyPack.InventorySystem`

**继承关系：**
```
System.Object
  └─ Container (abstract, implements IContainer)
       ├─ LinerContainer
       └─ GridContainer
```

**说明：**  
容器基类，提供物品存储、添加、移除、查询等核心功能。具体实现由子类（LinerContainer、GridContainer）提供。

---

#### 属性

##### `string ID { get; }`

**说明：** 容器的唯一标识符。

**类型：** `string`

**访问权限：** 只读（构造时设置）

---

##### `string Name { get; }`

**说明：** 容器的显示名称。

**类型：** `string`

**访问权限：** 只读（构造时设置）

---

##### `string Type { get; set; }`

**说明：** 容器类型，用于分类管理。

**类型：** `string`

**默认值：** `""`

---

##### `int Capacity { get; set; }`

**说明：** 容器容量（槽位数量），`-1` 表示无限容量。

**类型：** `int`

**默认值：** 构造时设置

---

##### `int UsedSlots { get; }`

**说明：** 已使用的槽位数量。

**类型：** `int`

**访问权限：** 只读

---

##### `int FreeSlots { get; }`

**说明：** 剩余空闲槽位数量。

**类型：** `int`

**访问权限：** 只读

**使用示例：**
```csharp
var container = new LinerContainer("id", "背包", "Backpack", 20);
Debug.Log($"已用：{container.UsedSlots}，剩余：{container.FreeSlots}");
```

---

##### `abstract bool IsGrid { get; }`

**说明：** 是否为网格容器（由子类实现）。

**类型：** `bool`

---

##### `abstract Vector2 Grid { get; }`

**说明：** 网格容器的尺寸（宽, 高），线性容器返回 `(-1, -1)`。

**类型：** `Vector2`

---

##### `List<IItemCondition> ContainerCondition { get; set; }`

**说明：** 容器物品条件列表，所有条件必须满足才能添加物品。

**类型：** `List<IItemCondition>`

**使用示例：**
```csharp
var equipment = new LinerContainer("eq", "装备栏", "Equipment", 5);
equipment.ContainerCondition.Add(new ItemTypeCondition("Equipment"));
```

---

##### `IReadOnlyList<ISlot> Slots { get; }`

**说明：** 容器槽位的只读列表。

**类型：** `IReadOnlyList<ISlot>`

**访问权限：** 只读

---

#### 方法

##### `(AddItemResult result, int addedCount) AddItems(IItem item, int count)`

**说明：** 向容器中添加指定数量的物品。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `item` | `IItem` | 必填 | 要添加的物品 | - |
| `count` | `int` | 必填 | 添加数量 | - |

**返回值：**
- **类型：** `(AddItemResult result, int addedCount)`
- **成功情况：** `result = AddItemResult.Success`, `addedCount` 为实际添加的数量
- **失败情况：** `result` 为具体失败原因（如 `ContainerIsFull`、`ItemConditionNotMet`），`addedCount = 0`
- **可能的异常：** 无（使用返回值表示错误）

**使用示例：**

```csharp
var container = new LinerContainer("backpack", "背包", "Backpack", 10);
var potion = new Item { ID = "potion", Name = "药水", IsStackable = true };

var (result, addedCount) = container.AddItems(potion, 5);

if (result == AddItemResult.Success)
{
    Debug.Log($"成功添加 {addedCount} 个物品");
}
else
{
    Debug.LogError($"添加失败：{result}");
}
```

---

##### `(RemoveItemResult result, int removedCount) RemoveItems(string itemId, int count)`

**说明：** 从容器中移除指定数量的物品。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemId` | `string` | 必填 | 物品 ID | - |
| `count` | `int` | 必填 | 移除数量 | - |

**返回值：**
- **类型：** `(RemoveItemResult result, int removedCount)`
- **成功情况：** `result = RemoveItemResult.Success`, `removedCount` 为实际移除的数量
- **失败情况：** `result` 为具体失败原因（如 `ItemNotFound`、`InsufficientQuantity`），`removedCount = 0`

**使用示例：**

```csharp
var (result, removedCount) = container.RemoveItems("potion", 3);

if (result == RemoveItemResult.Success)
{
    Debug.Log($"成功移除 {removedCount} 个物品");
}
else
{
    Debug.LogError($"移除失败：{result}");
}
```

---

##### `int GetItemTotalCount(string itemId)`

**说明：** 获取指定物品在容器中的总数量。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemId` | `string` | 必填 | 物品 ID | - |

**返回值：**
- **类型：** `int`
- **说明：** 物品的总数量，不存在时返回 `0`

**使用示例：**

```csharp
int potionCount = container.GetItemTotalCount("potion");
Debug.Log($"药水总数：{potionCount}");
```

---

##### `bool HasItem(string itemId, int count = 1)`

**说明：** 检查容器中是否有指定数量的物品。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemId` | `string` | 必填 | 物品 ID | - |
| `count` | `int` | 可选 | 检查的数量 | `1` |

**返回值：**
- **类型：** `bool`
- **说明：** `true` 表示数量足够，`false` 表示不足或不存在

**使用示例：**

```csharp
if (container.HasItem("gold_coin", 100))
{
    Debug.Log("金币足够");
}
```

---

##### `int FindItemSlotIndex(string itemId)`

**说明：** 查找指定物品所在的第一个槽位索引。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemId` | `string` | 必填 | 物品 ID | - |

**返回值：**
- **类型：** `int`
- **说明：** 槽位索引，未找到时返回 `-1`

**使用示例：**

```csharp
int slotIndex = container.FindItemSlotIndex("potion");
if (slotIndex >= 0)
{
    Debug.Log($"药水在槽位 {slotIndex}");
}
```

---

##### `List<int> FindAllItemSlotIndices(string itemId)`

**说明：** 查找指定物品在容器中的所有槽位索引。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemId` | `string` | 必填 | 物品 ID | - |

**返回值：**
- **类型：** `List<int>`
- **说明：** 包含所有槽位索引的列表，未找到时返回空列表

---

##### `IItem GetItemReference(string itemId)`

**说明：** 获取指定物品的引用（任意一个槽位中的实例）。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemId` | `string` | 必填 | 物品 ID | - |

**返回值：**
- **类型：** `IItem`
- **说明：** 物品引用，未找到时返回 `null`

---

##### `void ClearContainer()`

**说明：** 清空容器中的所有物品。

**返回值：** 无

**使用示例：**

```csharp
container.ClearContainer();
Debug.Log($"清空后槽位数：{container.UsedSlots}"); // 0
```

---

##### `void BeginBatch()`

**说明：** 开启批处理模式，延迟事件触发和缓存更新。

**使用示例：**

```csharp
container.BeginBatch();
for (int i = 0; i < 100; i++)
{
    container.AddItems(item, 1);
}
container.EndBatch(); // 一次性触发所有事件
```

---

##### `void EndBatch()`

**说明：** 结束批处理模式，触发所有累积的事件和缓存更新。

---

##### `List<IItem> FindItemsByCondition(IItemCondition condition)`

**说明：** 查找满足指定条件的所有物品。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `condition` | `IItemCondition` | 必填 | 物品条件 | - |

**返回值：**
- **类型：** `List<IItem>`
- **说明：** 满足条件的物品列表（去重）

**使用示例：**

```csharp
var weaponCondition = new ItemTypeCondition("Weapon");
var weapons = container.FindItemsByCondition(weaponCondition);
Debug.Log($"找到 {weapons.Count} 件武器");
```

---

#### 事件

##### `event Action<IItem, int, int, AddItemResult, List<int>> OnItemAddResult`

**说明：** 添加物品操作结果事件（成功或失败都会触发）。

**参数：**
1. `IItem item` - 操作的物品
2. `int requestedCount` - 请求添加的数量
3. `int actualCount` - 实际添加的数量
4. `AddItemResult result` - 操作结果
5. `List<int> affectedSlots` - 涉及的槽位索引列表

**使用示例：**

```csharp
container.OnItemAddResult += (item, requested, actual, result, slots) =>
{
    Debug.Log($"添加 {item.Name}：请求 {requested}，实际 {actual}，结果 {result}");
};
```

---

##### `event Action<string, int, int, RemoveItemResult, List<int>> OnItemRemoveResult`

**说明：** 移除物品操作结果事件。

**参数：**
1. `string itemId` - 物品 ID
2. `int requestedCount` - 请求移除的数量
3. `int actualCount` - 实际移除的数量
4. `RemoveItemResult result` - 操作结果
5. `List<int> affectedSlots` - 涉及的槽位索引列表

---

##### `event Action<int, IItem, int, int> OnSlotCountChanged`

**说明：** 槽位物品数量变更事件。

**参数：**
1. `int slotIndex` - 槽位索引
2. `IItem item` - 变更的物品
3. `int oldCount` - 原数量
4. `int newCount` - 新数量

---

##### `event Action<string, IItem, int, int> OnItemTotalCountChanged`

**说明：** 物品总数变更事件。

**参数：**
1. `string itemId` - 物品 ID
2. `IItem item` - 物品引用（可能为 null）
3. `int oldTotalCount` - 旧总数
4. `int newTotalCount` - 新总数

---

### LinerContainer 类

**命名空间：** `EasyPack.InventorySystem`

**继承关系：**
```
Container
  └─ LinerContainer
```

**说明：**  
线性容器，槽位按索引顺序排列（0, 1, 2, ...），适用于传统背包、列表式物品栏。

---

#### 构造函数

##### `LinerContainer(string id, string name, string type, int capacity = -1)`

**说明：** 创建线性容器实例。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `id` | `string` | 必填 | 容器 ID | - |
| `name` | `string` | 必填 | 容器名称 | - |
| `type` | `string` | 必填 | 容器类型 | - |
| `capacity` | `int` | 可选 | 容器容量（-1 表示无限） | `-1` |

**使用示例：**

```csharp
var backpack = new LinerContainer("player_backpack", "背包", "Backpack", 20);
```

---

#### 方法

##### `bool MoveItemToContainer(int sourceSlotIndex, Container targetContainer)`

**说明：** 将指定槽位的物品转移到目标容器。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `sourceSlotIndex` | `int` | 必填 | 源槽位索引 | - |
| `targetContainer` | `Container` | 必填 | 目标容器 | - |

**返回值：**
- **类型：** `bool`
- **说明：** `true` 表示转移成功，`false` 表示失败

**使用示例：**

```csharp
var backpack = new LinerContainer("bp", "背包", "Backpack", 10);
var warehouse = new LinerContainer("wh", "仓库", "Storage", 50);

int slotIndex = backpack.FindItemSlotIndex("iron_ore");
bool success = backpack.MoveItemToContainer(slotIndex, warehouse);
```

---

##### `void SortInventory()`

**说明：** 整理容器，按物品类型和名称排序。

**使用示例：**

```csharp
backpack.SortInventory();
Debug.Log("背包已排序");
```

---

##### `void ConsolidateItems()`

**说明：** 合并相同物品到较少的槽位中（压缩堆叠）。

**使用示例：**

```csharp
backpack.ConsolidateItems();
Debug.Log("物品已合并");
```

---

##### `void OrganizeInventory()`

**说明：** 整理容器（= ConsolidateItems + SortInventory）。

**使用示例：**

```csharp
backpack.OrganizeInventory();
Debug.Log("背包已整理");
```

---

### GridContainer 类

**命名空间：** `EasyPack.InventorySystem`

**继承关系：**
```
Container
  └─ GridContainer
```

**说明：**  
网格容器，支持二维布局和网格物品（占多个格子），适用于《暗黑破坏神》风格的背包。

---

#### 构造函数

##### `GridContainer(string id, string name, string type, int gridWidth, int gridHeight)`

**说明：** 创建网格容器实例。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `id` | `string` | 必填 | 容器 ID | - |
| `name` | `string` | 必填 | 容器名称 | - |
| `type` | `string` | 必填 | 容器类型 | - |
| `gridWidth` | `int` | 必填 | 网格宽度（列数） | - |
| `gridHeight` | `int` | 必填 | 网格高度（行数） | - |

**可能的异常：**
- `ArgumentException`：当 `gridWidth` 或 `gridHeight` <= 0 时抛出

**使用示例：**

```csharp
var gridBackpack = new GridContainer("grid_bp", "网格背包", "GridBackpack", 5, 4);
// 创建 5x4 = 20 格的网格容器
```

---

#### 属性

##### `int GridWidth { get; }`

**说明：** 网格宽度（列数）。

**类型：** `int`

**访问权限：** 只读

---

##### `int GridHeight { get; }`

**说明：** 网格高度（行数）。

**类型：** `int`

**访问权限：** 只读

---

#### 方法

##### `(AddItemResult result, int addedCount) AddItemsAtPosition(IItem item, int count, int x, int y)`

**说明：** 在指定网格位置添加物品。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `item` | `IItem` | 必填 | 要添加的物品（需是 GridItem） | - |
| `count` | `int` | 必填 | 添加数量 | - |
| `x` | `int` | 必填 | 网格 X 坐标（列） | - |
| `y` | `int` | 必填 | 网格 Y 坐标（行） | - |

**返回值：**
- **类型：** `(AddItemResult result, int addedCount)`
- **说明：** 操作结果和实际添加数量

**使用示例：**

```csharp
var gridItem = new GridItem
{
    ID = "armor",
    Name = "盔甲",
    GridWidth = 2,
    GridHeight = 2
};

var (result, count) = gridContainer.AddItemsAtPosition(gridItem, 1, 0, 0);
if (result == AddItemResult.Success)
{
    Debug.Log("盔甲放置在 (0,0)");
}
```

---

##### `IItem GetItemAt(int x, int y)`

**说明：** 获取指定网格坐标的物品。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `x` | `int` | 必填 | X 坐标 | - |
| `y` | `int` | 必填 | Y 坐标 | - |

**返回值：**
- **类型：** `IItem`
- **说明：** 该位置的物品，无物品时返回 `null`

---

##### `bool MoveItemToPosition(int sourceX, int sourceY, int targetX, int targetY)`

**说明：** 将物品从源位置移动到目标位置。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `sourceX` | `int` | 必填 | 源 X 坐标 | - |
| `sourceY` | `int` | 必填 | 源 Y 坐标 | - |
| `targetX` | `int` | 必填 | 目标 X 坐标 | - |
| `targetY` | `int` | 必填 | 目标 Y 坐标 | - |

**返回值：**
- **类型：** `bool`
- **说明：** 移动是否成功

---

##### `bool IsPositionAvailable(int x, int y, int width, int height)`

**说明：** 检查指定区域是否可用（未被占用且不超出边界）。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `x` | `int` | 必填 | X 坐标 | - |
| `y` | `int` | 必填 | Y 坐标 | - |
| `width` | `int` | 必填 | 宽度 | - |
| `height` | `int` | 必填 | 高度 | - |

**返回值：**
- **类型：** `bool`
- **说明：** `true` 表示可用，`false` 表示被占用或越界

**使用示例：**

```csharp
if (gridContainer.IsPositionAvailable(2, 2, 2, 2))
{
    Debug.Log("位置 (2,2) 可放置 2x2 物品");
}
```

---

### GridItem 类

**命名空间：** `EasyPack.InventorySystem`

**继承关系：**
```
Item
  └─ GridItem
```

**说明：**  
网格物品，继承自 `Item` 并添加形状、旋转等网格布局属性。支持矩形和任意复杂形状。

---

#### 属性

##### `List<(int x, int y)> Shape { get; set; }`

**说明：** 物品的形状，由占据的单元格坐标列表组成（相对于左上角原点）。默认为 1x1 单格物品。

**类型：** `List<(int x, int y)>`

**默认值：** `new List<(int x, int y)> { (0, 0) }`

**使用示例：**

```csharp
// 创建 2x2 矩形物品
var armor = new GridItem
{
    ID = "plate_armor",
    Name = "板甲",
    Shape = GridItem.CreateRectangleShape(2, 2)
};

// 创建 L 形物品
var lShapedKey = new GridItem
{
    ID = "l_shaped_key",
    Name = "L形钥匙",
    Shape = new List<(int x, int y)>
    {
        (0, 0), (1, 0),  // 水平的两格
        (0, 1)           // 下方的一格
    }
};

// 创建十字形物品
var cross = new GridItem
{
    ID = "cross_item",
    Name = "十字形物品",
    Shape = new List<(int x, int y)>
    {
        (1, 0),          // 上
        (0, 1), (1, 1), (2, 1),  // 中间行
        (1, 2)           // 下
    }
};
```

---

##### `bool CanRotate { get; set; }`

**说明：** 物品是否可以旋转。

**类型：** `bool`

**默认值：** `false`

---

##### `RotationAngle Rotation { get; set; }`

**说明：** 物品当前旋转角度。

**类型：** `RotationAngle` (枚举值：`Rotate0`、`Rotate90`、`Rotate180`、`Rotate270`)

**默认值：** `RotationAngle.Rotate0`

**使用示例：**

```csharp
var staff = new GridItem
{
    ID = "staff",
    Name = "法杖",
    Shape = GridItem.CreateRectangleShape(1, 3),  // 竖直 1x3
    CanRotate = true
};

// 旋转 90 度（变为 3x1）
staff.Rotate();
Debug.Log($"旋转后 - 宽度: {staff.ActualWidth}, 高度: {staff.ActualHeight}");
```

---

##### `int ActualWidth { get; }`

**说明：** 物品当前实际占用的宽度（考虑旋转）。

**类型：** `int`

**访问权限：** 只读

---

##### `int ActualHeight { get; }`

**说明：** 物品当前实际占用的高度（考虑旋转）。

**类型：** `int`

**访问权限：** 只读

---

#### 方法

##### `static List<(int x, int y)> CreateRectangleShape(int width, int height)`

**说明：** 辅助方法，快速创建矩形形状的单元格列表。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `width` | `int` | 必填 | 矩形宽度 | - |
| `height` | `int` | 必填 | 矩形高度 | - |

**返回值：**
- **类型：** `List<(int x, int y)>`
- **说明：** 包含所有单元格坐标的列表

**使用示例：**

```csharp
// 快速创建 3x2 矩形
var shape = GridItem.CreateRectangleShape(3, 2);
// 结果：[(0,0), (1,0), (2,0), (0,1), (1,1), (2,1)]

var backpack = new GridItem
{
    ID = "backpack",
    Name = "背包",
    Shape = GridItem.CreateRectangleShape(2, 3)
};
```

---

##### `List<(int x, int y)> GetOccupiedCells()`

**说明：** 获取物品当前占据的单元格列表（考虑旋转）。

**返回值：**
- **类型：** `List<(int x, int y)>`
- **说明：** 占据的单元格坐标列表

**使用示例：**

```csharp
var item = new GridItem
{
    ID = "item",
    Shape = GridItem.CreateRectangleShape(2, 2),
    CanRotate = true
};

var cells = item.GetOccupiedCells();
Debug.Log($"物品占据 {cells.Count} 个格子");

item.Rotate();  // 旋转 90 度
var cellsAfterRotation = item.GetOccupiedCells();
Debug.Log($"旋转后仍占据 {cellsAfterRotation.Count} 个格子");
```

---

##### `bool Rotate()`

**说明：** 顺时针旋转物品 90 度（如果启用了 `CanRotate`）。

**返回值：**
- **类型：** `bool`
- **说明：** 旋转是否成功（如果未启用 `CanRotate` 则返回 `false`）

**使用示例：**

```csharp
var staff = new GridItem
{
    ID = "staff",
    Name = "法杖",
    Shape = GridItem.CreateRectangleShape(1, 3),
    CanRotate = true
};

if (staff.Rotate())
{
    Debug.Log($"旋转成功！新大小：{staff.ActualWidth}x{staff.ActualHeight}");
}
```

---

##### `bool SetRotation(RotationAngle angle)`

**说明：** 设置物品的旋转角度。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `angle` | `RotationAngle` | 必填 | 目标旋转角度 | - |

**返回值：**
- **类型：** `bool`
- **说明：** 设置是否成功

**使用示例：**

```csharp
var item = new GridItem
{
    ID = "item",
    Shape = GridItem.CreateRectangleShape(1, 3),
    CanRotate = true
};

// 直接设置为 180 度旋转
item.SetRotation(RotationAngle.Rotate180);
Debug.Log($"当前旋转角度: {item.Rotation}");
```

---

##### `new GridItem Clone()`

**说明：** 克隆网格物品（包括形状、旋转、所有属性）。

**返回值：**
- **类型：** `GridItem`
- **说明：** 新的物品副本

**使用示例：**

```csharp
var original = new GridItem { ID = "sword", Shape = GridItem.CreateRectangleShape(1, 2) };
var copy = original.Clone();
copy.ID = "sword_copy";
```

---

### InventoryManager 类

**命名空间：** `EasyPack.InventorySystem`

**说明：**  
管理多个容器的中央系统，提供容器注册、跨容器操作、全局搜索等功能。

---

#### 方法

##### `bool RegisterContainer(Container container, int priority = 0, string category = "Default")`

**说明：** 注册容器到管理器。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `container` | `Container` | 必填 | 要注册的容器 | - |
| `priority` | `int` | 可选 | 容器优先级（数值越高优先级越高） | `0` |
| `category` | `string` | 可选 | 容器分类 | `"Default"` |

**返回值：**
- **类型：** `bool`
- **说明：** 注册是否成功

**使用示例：**

```csharp
var manager = new InventoryManager();
var backpack = new LinerContainer("bp", "背包", "Backpack", 20);

manager.RegisterContainer(backpack, priority: 1, category: "Player");
```

---

##### `bool UnregisterContainer(string containerId)`

**说明：** 注销指定容器。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `containerId` | `string` | 必填 | 容器 ID | - |

**返回值：**
- **类型：** `bool`
- **说明：** 注销是否成功

---

##### `Container GetContainer(string containerId)`

**说明：** 获取指定 ID 的容器。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `containerId` | `string` | 必填 | 容器 ID | - |

**返回值：**
- **类型：** `Container`
- **说明：** 容器实例，未找到时返回 `null`

---

##### `List<Container> GetContainersByType(string containerType)`

**说明：** 按类型获取容器列表。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `containerType` | `string` | 必填 | 容器类型 | - |

**返回值：**
- **类型：** `List<Container>`
- **说明：** 指定类型的容器列表

---

##### `List<Container> GetContainersByCategory(string category)`

**说明：** 按分类获取容器列表。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `category` | `string` | 必填 | 容器分类 | - |

**返回值：**
- **类型：** `List<Container>`
- **说明：** 指定分类的容器列表

---

##### `bool TransferItems(string sourceContainerId, string targetContainerId, string itemId, int count)`

**说明：** 在两个容器间转移物品。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `sourceContainerId` | `string` | 必填 | 源容器 ID | - |
| `targetContainerId` | `string` | 必填 | 目标容器 ID | - |
| `itemId` | `string` | 必填 | 物品 ID | - |
| `count` | `int` | 必填 | 转移数量 | - |

**返回值：**
- **类型：** `bool`
- **说明：** 转移是否成功

**使用示例：**

```csharp
bool success = manager.TransferItems("backpack", "warehouse", "iron_ore", 50);
if (success)
{
    Debug.Log("转移成功");
}
```

---

##### `List<Container> FindItemInContainers(string itemId)`

**说明：** 在所有注册的容器中搜索包含指定物品的容器。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemId` | `string` | 必填 | 物品 ID | - |

**返回值：**
- **类型：** `List<Container>`
- **说明：** 包含该物品的容器列表

---

##### `int GetItemTotalCountAcrossContainers(string itemId)`

**说明：** 获取物品在所有容器中的总数量。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemId` | `string` | 必填 | 物品 ID | - |

**返回值：**
- **类型：** `int`
- **说明：** 总数量

**使用示例：**

```csharp
int totalGold = manager.GetItemTotalCountAcrossContainers("gold_coin");
Debug.Log($"所有容器中的金币总数：{totalGold}");
```

---

##### `bool DistributeItems(IItem item, int totalCount, string[] targetContainerIds)`

**说明：** 将物品分配到多个容器中（尽量平均分配）。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `item` | `IItem` | 必填 | 要分配的物品 | - |
| `totalCount` | `int` | 必填 | 总数量 | - |
| `targetContainerIds` | `string[]` | 必填 | 目标容器 ID 数组 | - |

**返回值：**
- **类型：** `bool`
- **说明：** 分配是否成功

---

#### 事件

##### `event Action<Container> OnContainerRegistered`

**说明：** 容器注册事件。

**参数：** `Container container` - 注册的容器

---

##### `event Action<Container> OnContainerUnregistered`

**说明：** 容器注销事件。

**参数：** `Container container` - 注销的容器

---

### Slot 类

**命名空间：** `EasyPack.InventorySystem`

**说明：**  
容器槽位，存储单个物品及其数量。

---

#### 属性

##### `int Index { get; set; }`

**说明：** 槽位索引。

**类型：** `int`

---

##### `Container Container { get; set; }`

**说明：** 槽位所属的容器。

**类型：** `Container`

---

##### `IItem Item { get; }`

**说明：** 槽位中的物品。

**类型：** `IItem`

**访问权限：** 只读

---

##### `int ItemCount { get; }`

**说明：** 槽位中物品的数量。

**类型：** `int`

**访问权限：** 只读

---

##### `bool IsOccupied { get; }`

**说明：** 槽位是否被占用。

**类型：** `bool`

**访问权限：** 只读

---

## 条件类

### IItemCondition 接口

**命名空间：** `EasyPack.InventorySystem`

**说明：**  
物品条件接口，用于过滤物品。

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

## 序列化类

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
- 自定义属性（Attributes，使用 CustomDataUtility 转换）

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

## 延伸阅读

- [用户使用指南](./UserGuide.md) - 查看完整使用场景和最佳实践
- [Mermaid 图集](./Diagrams.md) - 查看类关系和数据流图

---

**维护者：** NEKOPACK 团队  
**反馈渠道：** [GitHub Issues](https://github.com/CutrelyAlex/NEKOPACK-GITHUB/issues)
