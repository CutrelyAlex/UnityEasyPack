# Inventory System - API 参考文档 (Part 2)

**适用EasyPack版本：** EasyPack v1.7.0  
**最后更新：** 2025-11-10

---

## 概述

本文档是 **Inventory System** API 参考文档的第二部分，包含容器实现类。

**文档系列：**
- **Part 1 - 核心接口与服务类**：系统的服务层和核心接口定义
- **Part 2 - 容器实现类**（当前文档）：容器的具体实现类和缓存服务
- **Part 3 - 物品与网格系统**：物品类、网格物品和查询服务
- **Part 4 - 条件、工具与序列化**：条件系统、工具类和序列化支持

---

## 目录

- [概述](#概述)
- [抽象容器](#抽象容器)
  - [Container 类（抽象）](#container-类抽象)
- [容器实现](#容器实现)
  - [LinerContainer 类](#linercontainer-类)
  - [GridContainer 类](#gridcontainer-类)
- [槽位实现](#槽位实现)
  - [Slot 类](#slot-类)
- [缓存服务](#缓存服务)
  - [ContainerCacheService 类](#containercacheservice-类)

---

## 抽象容器

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

##### `bool Full { get; }`

**说明：** 检查容器是否已满。当所有槽位都被占用，且每个占用的槽位物品都不可堆叠或已达到堆叠上限时，容器才被认为是满的。

**类型：** `bool`

**访问权限：** 只读

**使用示例：**
```csharp
if (container.Full)
{
    Debug.Log("容器已满，无法添加更多物品");
}
else
{
    // 可以继续添加物品
    container.AddItems(new Item { ID = "item", Name = "物品" }, 1);
}
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

##### `bool ValidateItemCondition(IItem item)`

**说明：** 检查物品是否满足容器的所有条件。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `item` | `IItem` | 必填 | 要验证的物品 | - |

**返回值：**
- **类型：** `bool`
- **说明：** `true` 表示满足所有条件，`false` 表示不满足任一条件

**使用示例：**

```csharp
var container = new LinerContainer("equipment", "装备栏", "Equipment", 5);
container.ContainerCondition.Add(new ItemTypeCondition("Equipment"));

var sword = new Item { ID = "sword", Name = "铁剑", Type = "Equipment" };
bool canAdd = container.ValidateItemCondition(sword); // true

var potion = new Item { ID = "potion", Name = "药水", Type = "Consumable" };
bool canAddPotion = container.ValidateItemCondition(potion); // false
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

##### `RemoveItemResult RemoveItemAtIndex(int index, int count, string expectedItemId)`

**说明：** 从指定槽位索引移除物品。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `index` | `int` | 必填 | 槽位索引 | - |
| `count` | `int` | 必填 | 移除数量 | - |
| `expectedItemId` | `string` | 可选 | 预期物品ID，用于验证 | `null` |

**返回值：**
- **类型：** `RemoveItemResult`
- **说明：** 移除操作结果

**使用示例：**

```csharp
// 从槽位 0 移除 1 个物品
var result = container.RemoveItemAtIndex(0, 1);
if (result == RemoveItemResult.Success)
{
    Debug.Log("物品移除成功");
}

// 移除指定物品ID的物品
var result2 = container.RemoveItemAtIndex(1, 2, "potion");
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

##### `bool HasItem(string itemId)`

**说明：** 检查容器中是否包含指定物品。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemId` | `string` | 必填 | 物品 ID | - |

**返回值：**
- **类型：** `bool`
- **说明：** `true` 表示存在，`false` 表示不存在

**使用示例：**

```csharp
if (container.HasItem("gold_coin"))
{
    Debug.Log("有金币");
}
```

---

##### `bool HasEnoughItems(string itemId, int requiredCount)`

**说明：** 检查容器中是否有足够数量的物品。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemId` | `string` | 必填 | 物品 ID | - |
| `requiredCount` | `int` | 必填 | 需要的数量 | - |

**返回值：**
- **类型：** `bool`
- **说明：** `true` 表示数量足够，`false` 表示不足或不存在

**使用示例：**

```csharp
if (container.HasEnoughItems("gold_coin", 100))
{
    Debug.Log("金币足够");
}
```

---

##### `int FindFirstSlotIndex(string itemId)`

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
int slotIndex = container.FindFirstSlotIndex("potion");
if (slotIndex >= 0)
{
    Debug.Log($"药水在槽位 {slotIndex}");
}
```

---

##### `List<int> FindSlotIndices(string itemId)`

**说明：** 查找指定物品在容器中的所有槽位索引。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemId` | `string` | 必填 | 物品 ID | - |

**返回值：**
- **类型：** `List<int>`
- **说明：** 包含所有槽位索引的列表，未找到时返回空列表

**使用示例：**

```csharp
var slotIndices = container.FindSlotIndices("potion");
Debug.Log($"药水在 {slotIndices.Count} 个槽位中");
```

---

##### `void ClearAllSlots()`

**说明：** 清空容器中的所有物品。

**返回值：** 无

**使用示例：**

```csharp
container.ClearAllSlots();
Debug.Log($"清空后槽位数：{container.UsedSlots}"); // 0
```

---

##### `List<(IItem item, AddItemResult result, int addedCount, int exceededCount)> AddItemsBatch(List<(IItem item, int count)> itemsToAdd)`

**说明：** 批量添加多种物品到容器，自动处理批处理模式以减少事件触发。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemsToAdd` | `List<(IItem item, int count)>` | 必填 | 要添加的物品和数量列表 | - |

**返回值：**
- **类型：** `List<(IItem item, AddItemResult result, int addedCount, int exceededCount)>`
- **说明：** 每个物品的添加结果，包含结果状态、实际添加数量和超出数量

**使用示例：**

```csharp
var itemsToAdd = new List<(IItem item, int count)>
{
    (new Item { ID = "wood", Name = "木材" }, 100),
    (new Item { ID = "stone", Name = "石料" }, 50)
};

var results = container.AddItemsBatch(itemsToAdd);
foreach (var (item, result, addedCount, exceededCount) in results)
{
    Debug.Log($"{item.Name}: {result}, 添加了 {addedCount} 个，超出 {exceededCount} 个");
}
```

---

##### `Task<(AddItemResult result, int addedCount)> AddItemsAsync(IItem item, int count, CancellationToken cancellationToken)`

**说明：** 异步添加物品到容器。对于大量物品或复杂容器，自动使用异步处理以避免阻塞主线程。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `item` | `IItem` | 必填 | 要添加的物品 | - |
| `count` | `int` | 必填 | 添加数量 | - |
| `cancellationToken` | `CancellationToken` | 可选 | 取消令牌 | `default` |

**返回值：**
- **类型：** `Task<(AddItemResult result, int addedCount)>`
- **说明：** 异步任务，返回添加结果和实际添加数量

**使用示例：**

```csharp
using System.Threading;
using System.Threading.Tasks;

var cts = new CancellationTokenSource();
var task = container.AddItemsAsync(largeItem, 10000, cts.Token);

var (result, addedCount) = await task;
if (result == AddItemResult.Success)
{
    Debug.Log($"异步添加完成：{addedCount} 个物品");
}
```

---

##### `List<(int slotIndex, IItem item, int count)> GetItemsWhere(Func<IItem, bool> condition)`

**说明：** 查找满足指定条件的所有物品（使用 lambda 表达式）。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `condition` | `Func<IItem, bool>` | 必填 | 物品条件函数 | - |

**返回值：**
- **类型：** `List<(int slotIndex, IItem item, int count)>`
- **说明：** 满足条件的物品列表，包含槽位索引、物品引用和数量

**使用示例：**

```csharp
// 使用条件对象
var weaponCondition = new ItemTypeCondition("Weapon");
var weapons = container.GetItemsWhere(item => weaponCondition.CheckCondition(item));
Debug.Log($"找到 {weapons.Count} 件武器");

// 或直接使用 lambda
var highLevelWeapons = container.GetItemsWhere(item => 
    item.Type == "Weapon" && item.GetCustomData<int>("Level", 0) >= 50);
```

---

##### `List<(int slotIndex, IItem item, int count)> GetItemsByType(string itemType)`

**说明：** 获取指定类型的所有物品。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemType` | `string` | 必填 | 物品类型 | - |

**返回值：**
- **类型：** `List<(int slotIndex, IItem item, int count)>`
- **说明：** 指定类型的所有物品列表，包含槽位索引、物品引用和数量

**使用示例：**

```csharp
var weapons = container.GetItemsByType("Weapon");
Debug.Log($"找到 {weapons.Count} 件武器");
```

---

##### `List<(int slotIndex, IItem item, int count)> GetItemsByAttribute(string attributeName, object attributeValue)`

**说明：** 获取具有指定属性值的所有物品。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `attributeName` | `string` | 必填 | 属性名称 | - |
| `attributeValue` | `object` | 必填 | 属性值 | - |

**返回值：**
- **类型：** `List<(int slotIndex, IItem item, int count)>`
- **说明：** 符合条件的物品列表，包含槽位索引、物品引用和数量

**使用示例：**

```csharp
var ironItems = container.GetItemsByAttribute("Material", "Iron");
Debug.Log($"找到 {ironItems.Count} 件铁质物品");
```

---

##### `List<(int slotIndex, IItem item, int count)> GetItemsByName(string namePattern)`

**说明：** 获取名称包含指定模式的所有物品。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `namePattern` | `string` | 必填 | 名称模式（子字符串） | - |

**返回值：**
- **类型：** `List<(int slotIndex, IItem item, int count)>`
- **说明：** 符合条件的物品列表，包含槽位索引、物品引用和数量

**使用示例：**

```csharp
var potions = container.GetItemsByName("药水");
Debug.Log($"找到 {potions.Count} 个药水类物品");
```

---

##### `Dictionary<string, int> GetAllItemCountsDict()`

**说明：** 获取容器中所有物品的计数字典。

**返回值：**
- **类型：** `Dictionary<string, int>`
- **说明：** 物品 ID 和数量的字典

**使用示例：**

```csharp
var counts = container.GetAllItemCountsDict();
foreach (var kvp in counts)
{
    Debug.Log($"{kvp.Key}: {kvp.Value}");
}
```

---

##### `List<(int slotIndex, IItem item, int count)> GetAllItems()`

**说明：** 获取容器中所有物品的列表。

**返回值：**
- **类型：** `List<(int slotIndex, IItem item, int count)>`
- **说明：** 所有物品列表，包含槽位索引、物品引用和数量

**使用示例：**

```csharp
var allItems = container.GetAllItems();
Debug.Log($"容器中共有 {allItems.Count} 个物品槽位被占用");
```

---

##### `int GetUniqueItemCount()`

**说明：** 获取容器中不同物品的种类数量。

**返回值：**
- **类型：** `int`
- **说明：** 不同物品的种类数量

**使用示例：**

```csharp
int uniqueCount = container.GetUniqueItemCount();
Debug.Log($"容器中有 {uniqueCount} 种不同的物品");
```

---

##### `bool IsEmpty()`

**说明：** 检查容器是否为空。

**返回值：**
- **类型：** `bool`
- **说明：** `true` 表示容器为空，`false` 表示有物品

**使用示例：**

```csharp
if (container.IsEmpty())
{
    Debug.Log("容器是空的");
}
```

---

##### `float GetTotalWeight()`

**说明：** 获取容器中所有物品的总重量。

**返回值：**
- **类型：** `float`
- **说明：** 所有物品的总重量

**使用示例：**

```csharp
float totalWeight = container.GetTotalWeight();
Debug.Log($"容器总重量：{totalWeight}");
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



---

## 容器实现

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

#### 属性

##### `int MainSlotIndex { get; }`

**说明：** 主槽位索引，通常为线性容器的第一个槽位 (索引 0)。

**类型：** `int`

**访问权限：** 只读

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

int slotIndex = backpack.FindFirstSlotIndex("iron_ore");
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

##### `int MainSlotIndex { get; }`

**说明：** 主槽位索引，通常为网格容器的左上角位置 (0,0) 对应的槽位索引。

**类型：** `int`

**访问权限：** 只读

---

#### 方法

##### `(AddItemResult result, int addedCount) AddItemAt(IItem item, int x, int y, int count = 1)`

**说明：** 在指定网格位置添加物品。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `item` | `IItem` | 必填 | 要添加的物品（需是 GridItem） | - |
| `x` | `int` | 必填 | 网格 X 坐标（列） | - |
| `y` | `int` | 必填 | 网格 Y 坐标（行） | - |
| `count` | `int` | 可选 | 添加数量（对于网格物品通常为 1） | `1` |

**返回值：**
- **类型：** `(AddItemResult result, int addedCount)`
- **说明：** 操作结果和实际添加数量

**使用示例：**

```csharp
var gridItem = new GridItem
{
    ID = "armor",
    Name = "盔甲",
    Shape = GridItem.CreateRectangleShape(2, 2)
};

var (result, count) = gridContainer.AddItemAt(gridItem, 0, 0, 1);
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

##### `bool TryRotateItemAt(int x, int y)`

**说明：** 尝试旋转指定位置的物品（如果物品支持旋转）。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `x` | `int` | 必填 | X 坐标 | - |
| `y` | `int` | 必填 | Y 坐标 | - |

**返回值：**
- **类型：** `bool`
- **说明：** 旋转是否成功（物品支持旋转且旋转后可以放置）

**使用示例：**

```csharp
bool success = gridContainer.TryRotateItemAt(0, 0);
if (success)
{
    Debug.Log("物品旋转成功");
}
```

---

##### `bool CanPlaceAt(int x, int y, int width, int height, int excludeIndex = -1)`

**说明：** 检查指定区域是否可以放置物品（未被占用且不超出边界）。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `x` | `int` | 必填 | X 坐标 | - |
| `y` | `int` | 必填 | Y 坐标 | - |
| `width` | `int` | 必填 | 宽度 | - |
| `height` | `int` | 必填 | 高度 | - |
| `excludeIndex` | `int` | 可选 | 排除的槽位索引（用于移动物品时） | `-1` |

**返回值：**
- **类型：** `bool`
- **说明：** `true` 表示可用，`false` 表示被占用或越界

**使用示例：**

```csharp
if (gridContainer.CanPlaceAt(2, 2, 2, 2))
{
    Debug.Log("位置 (2,2) 可放置 2x2 物品");
}
```

---

##### `bool CanPlaceGridItem(GridItem gridItem, int slotIndex, int excludeIndex = -1)`

**说明：** 检查指定位置是否可以放置网格物品（支持任意形状）。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `gridItem` | `GridItem` | 必填 | 要放置的网格物品 | - |
| `slotIndex` | `int` | 必填 | 放置的起始槽位索引 | - |
| `excludeIndex` | `int` | 可选 | 排除的槽位索引（用于移动物品时） | `-1` |

**返回值：**
- **类型：** `bool`
- **说明：** `true` 表示可以放置，`false` 表示被占用或越界

**使用示例：**

```csharp
var gridItem = new GridItem { Shape = GridItem.CreateRectangleShape(2, 2) };
int slotIndex = gridContainer.CoordToIndex(2, 2);
if (gridContainer.CanPlaceGridItem(gridItem, slotIndex))
{
    Debug.Log("可以放置网格物品");
}
```

**注意：** GridContainer 没有 `MoveItemToPosition` 方法。如需移动物品，需要先使用 `RemoveItem` 移除，再在新位置使用 `AddItemAt` 添加。

---

##### `int CoordToIndex(int x, int y)`

**说明：** 将二维坐标转换为一维索引。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `x` | `int` | 必填 | X 坐标（列） | - |
| `y` | `int` | 必填 | Y 坐标（行） | - |

**返回值：**
- **类型：** `int`
- **说明：** 一维索引，坐标无效时返回 `-1`

**使用示例：**

```csharp
int index = gridContainer.CoordToIndex(2, 3);
if (index >= 0)
{
    Debug.Log($"坐标 (2,3) 对应索引 {index}");
}
```

---

##### `(int x, int y) IndexToCoord(int index)`

**说明：** 将一维索引转换为二维坐标。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `index` | `int` | 必填 | 一维索引 | - |

**返回值：**
- **类型：** `(int x, int y)`
- **说明：** 二维坐标，索引无效时返回 `(-1, -1)`

**使用示例：**

```csharp
var (x, y) = gridContainer.IndexToCoord(10);
Debug.Log($"索引 10 对应坐标 ({x},{y})");
```

---

##### `string GetGridVisualization()`

**说明：** 获取网格的可视化字符串表示，用于调试和日志输出。

**返回值：**
- **类型：** `string`
- **说明：** 多行字符串，显示网格布局。'O' 表示空闲格子，'X' 表示被占用的格子

**使用示例：**

```csharp
string visualization = gridContainer.GetGridVisualization();
Debug.Log("网格状态：\n" + visualization);
// 输出类似：
// O O X X
// O O X X  
// X X X X
// X X X X
```

---



---

## 槽位实现

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

##### `List<IItemCondition> SlotCondition { get; set; }`

**说明：** 槽位物品条件列表，所有条件必须满足才能放置物品。

**类型：** `List<IItemCondition>`

**默认值：** 空列表

---


---

## 缓存服务

### ContainerCacheService 类

**命名空间：** `EasyPack.InventorySystem`

**说明：**  
容器缓存管理器，用于高效查询容器的高频只读操作。通过维护多个缓存结构来加速物品查询、槽位查询和统计操作。

---

#### 构造函数

##### `ContainerCacheService(int capacity)`

**说明：** 初始化容器缓存服务。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `capacity` | `int` | 必填 | 容器容量，用于预分配缓存空间 | - |

**使用示例：**

```csharp
// 为大容量容器创建缓存服务
var cacheService = new ContainerCacheService(1000);
```

---

#### 缓存更新方法

##### `void UpdateItemSlotIndexCache(string itemId, int slotIndex, bool isAdding)`

**说明：** 更新物品槽位索引缓存。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemId` | `string` | 必填 | 物品 ID | - |
| `slotIndex` | `int` | 必填 | 槽位索引 | - |
| `isAdding` | `bool` | 必填 | 是否为添加操作 | - |

---

##### `void UpdateEmptySlotCache(int slotIndex, bool isEmpty)`

**说明：** 更新空槽位缓存。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `slotIndex` | `int` | 必填 | 槽位索引 | - |
| `isEmpty` | `bool` | 必填 | 是否为空 | - |

---

##### `void UpdateItemTypeCache(string itemType, int slotIndex, bool isAdding)`

**说明：** 更新物品类型槽位索引缓存。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemType` | `string` | 必填 | 物品类型 | - |
| `slotIndex` | `int` | 必填 | 槽位索引 | - |
| `isAdding` | `bool` | 必填 | 是否为添加操作 | - |

---

##### `void UpdateItemCountCache(string itemId, int delta)`

**说明：** 更新物品数量缓存。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemId` | `string` | 必填 | 物品 ID | - |
| `delta` | `int` | 必填 | 数量变化量 | - |

---

#### 缓存查询方法

##### `bool HasItemInCache(string itemId)`

**说明：** 检查缓存中是否存在指定物品。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemId` | `string` | 必填 | 物品 ID | - |

**返回值：**
- **类型：** `bool`
- **说明：** 是否存在该物品

---

##### `bool TryGetItemSlotIndices(string itemId, out HashSet<int> indices)`

**说明：** 尝试获取物品的槽位索引集合。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemId` | `string` | 必填 | 物品 ID | - |
| `indices` | `out HashSet<int>` | 必填 | 输出参数，槽位索引集合 | - |

**返回值：**
- **类型：** `bool`
- **说明：** 是否成功获取

---

##### `bool TryGetItemTypeIndices(string itemType, out HashSet<int> indices)`

**说明：** 尝试获取物品类型的槽位索引集合。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemType` | `string` | 必填 | 物品类型 | - |
| `indices` | `out HashSet<int>` | 必填 | 输出参数，槽位索引集合 | - |

**返回值：**
- **类型：** `bool`
- **说明：** 是否成功获取

---

##### `bool TryGetItemCount(string itemId, out int count)`

**说明：** 尝试获取物品的总数量。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemId` | `string` | 必填 | 物品 ID | - |
| `count` | `out int` | 必填 | 输出参数，物品数量 | - |

**返回值：**
- **类型：** `bool`
- **说明：** 是否成功获取

---

##### `IItem GetItemReference(string itemId, IReadOnlyList<ISlot> slots)`

**说明：** 获取物品引用。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemId` | `string` | 必填 | 物品 ID | - |
| `slots` | `IReadOnlyList<ISlot>` | 必填 | 槽位列表 | - |

**返回值：**
- **类型：** `IItem`
- **说明：** 物品引用，如果未找到则返回 null

---

##### `SortedSet<int> GetEmptySlotIndices()`

**说明：** 获取所有空槽位索引。

**返回值：**
- **类型：** `SortedSet<int>`
- **说明：** 空槽位索引集合

---

##### `int GetCachedItemCount()`

**说明：** 获取缓存中的物品种类数量。

**返回值：**
- **类型：** `int`
- **说明：** 缓存的物品种类数量

---

##### `Dictionary<string, int> GetAllItemCounts()`

**说明：** 获取所有物品的数量字典。

**返回值：**
- **类型：** `Dictionary<string, int>`
- **说明：** 物品ID到数量的映射字典

---

#### 缓存维护方法

##### `void RebuildCaches(IReadOnlyList<ISlot> slots)`

**说明：** 重建所有缓存。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `slots` | `IReadOnlyList<ISlot>` | 必填 | 槽位列表 | - |

---

##### `void ValidateCaches(IReadOnlyList<ISlot> slots)`

**说明：** 验证并修复缓存一致性。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `slots` | `IReadOnlyList<ISlot>` | 必填 | 槽位列表 | - |

---

##### `void ClearAllCaches()`

**说明：** 清空所有缓存。

---

##### `bool HasItemInCache(string itemId)`

**说明：** 检查缓存中是否包含指定物品。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemId` | `string` | 必填 | 物品 ID | - |

**返回值：**
- **类型：** `bool`
- **说明：** 是否包含

---

##### `bool TryGetItemSlotIndices(string itemId, out HashSet<int> indices)`

**说明：** 尝试获取物品的槽位索引集合。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemId` | `string` | 必填 | 物品 ID | - |
| `indices` | `out HashSet<int>` | 必填 | 输出槽位索引集合 | - |

**返回值：**
- **类型：** `bool`
- **说明：** 是否成功获取

---

##### `bool TryGetItemTypeIndices(string itemType, out HashSet<int> indices)`

**说明：** 尝试获取物品类型的槽位索引集合。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemType` | `string` | 必填 | 物品类型 | - |
| `indices` | `out HashSet<int>` | 必填 | 输出槽位索引集合 | - |

**返回值：**
- **类型：** `bool`
- **说明：** 是否成功获取

---

##### `bool TryGetItemCount(string itemId, out int count)`

**说明：** 尝试获取物品数量。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemId` | `string` | 必填 | 物品 ID | - |
| `count` | `out int` | 必填 | 输出数量 | - |

**返回值：**
- **类型：** `bool`
- **说明：** 是否成功获取

---

##### `IItem GetItemReference(string itemId, IReadOnlyList<ISlot> slots)`

**说明：** 获取物品引用。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemId` | `string` | 必填 | 物品 ID | - |
| `slots` | `IReadOnlyList<ISlot>` | 必填 | 槽位列表 | - |

**返回值：**
- **类型：** `IItem`
- **说明：** 物品引用

---

##### `SortedSet<int> GetEmptySlotIndices()`

**说明：** 获取空槽位索引集合。

**返回值：**
- **类型：** `SortedSet<int>`
- **说明：** 空槽位索引

---

##### `int GetCachedItemCount()`

**说明：** 获取缓存中的物品总数。

**返回值：**
- **类型：** `int`
- **说明：** 物品总数

---

##### `Dictionary<string, int> GetAllItemCounts()`

**说明：** 获取所有物品的数量字典。

**返回值：**
- **类型：** `Dictionary<string, int>`
- **说明：** 物品 ID 到数量的映射

---

##### `void ValidateCaches(IReadOnlyList<ISlot> slots)`

**说明：** 验证缓存一致性。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `slots` | `IReadOnlyList<ISlot>` | 必填 | 槽位列表 | - |

---


