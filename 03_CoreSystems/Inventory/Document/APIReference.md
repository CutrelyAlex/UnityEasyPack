# Inventory System - API 参考文档

**适用EasyPack版本：** EasyPack v1.5.30  
**最后更新：** 2025-10-26

---

## 概述

本文档提供 **Inventory System** 的完整 API 参考。包含所有公开类、方法、属性的签名和参数说明。

---

## 目录

- [概述](#概述)
- [核心类](#核心类)
  - [Item 类](#item-类)
  - [Container 类（抽象）](#container-类抽象)
  - [LinerContainer 类](#linercontainer-类)
  - [GridContainer 类](#gridcontainer-类)
  - [GridItem 类](#griditem-类)
  - [InventoryManager 类](#inventorymanager-类)
  - [Slot 类](#slot-类)
- [条件类](#条件类)
  - [IItemCondition 接口](#iitemcondition-接口)
  - [ItemTypeCondition 类](#itemtypecondition-类)
  - [AttributeCondition 类](#attributecondition-类)
  - [CustomItemCondition 类](#customitemcondition-类)
  - [AllCondition 类](#allcondition-类)
  - [AnyCondition 类](#anycondition-类)
- [序列化类](#序列化类)
  - [ContainerJsonSerializer 类](#containerjsonserializer-类)
  - [ItemJsonSerializer 类](#itemjsonserializer-类)
- [枚举类型](#枚举类型)
  - [AddItemResult 枚举](#additemresult-枚举)
  - [RemoveItemResult 枚举](#removeitemresult-枚举)
- [延伸阅读](#延伸阅读)

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

##### `Dictionary<string, object> Attributes { get; set; }`

**说明：** 物品自定义属性字典，可存储任意键值对（如品质、等级、耐久等）。

**类型：** `Dictionary<string, object>`

**默认值：** 空字典

**使用示例：**
```csharp
var item = new Item
{
    Attributes = new Dictionary<string, object>
    {
        { "Rarity", "Legendary" },
        { "Level", 50 },
        { "Durability", 100 }
    }
};
```

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

**说明：** 创建物品的深拷贝副本。

**返回值：**
- **类型：** `IItem`
- **说明：** 返回一个新的物品实例，包含所有属性的副本

**使用示例：**
```csharp
var originalItem = new Item { ID = "potion", Name = "药水" };
var clonedItem = originalItem.Clone();

// 修改克隆不影响原物品
clonedItem.Name = "高级药水";
Debug.Log(originalItem.Name); // "药水"
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
网格物品，继承自 `Item` 并添加网格尺寸属性。

---

#### 属性

##### `int GridWidth { get; set; }`

**说明：** 物品在网格中的宽度（列数）。

**类型：** `int`

**默认值：** `1`

---

##### `int GridHeight { get; set; }`

**说明：** 物品在网格中的高度（行数）。

**类型：** `int`

**默认值：** `1`

**使用示例：**

```csharp
var largeItem = new GridItem
{
    ID = "shield",
    Name = "大盾",
    GridWidth = 2,
    GridHeight = 3
};
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
    return item.Attributes.TryGetValue("Level", out var level) &&
           level is int lvl && lvl >= 50;
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

## 序列化类

### ContainerJsonSerializer 类

**命名空间：** `EasyPack.InventorySystem`

**说明：**  
容器的 JSON 序列化/反序列化工具。

---

#### 方法

##### `static string ToJson(Container container)`

**说明：** 将容器序列化为 JSON 字符串。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `container` | `Container` | 必填 | 要序列化的容器 | - |

**返回值：**
- **类型：** `string`
- **说明：** JSON 字符串

**使用示例：**

```csharp
string json = ContainerJsonSerializer.ToJson(container);
File.WriteAllText("container.json", json);
```

---

##### `static T FromJson<T>(string json) where T : Container`

**说明：** 从 JSON 字符串反序列化容器。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `json` | `string` | 必填 | JSON 字符串 | - |

**返回值：**
- **类型：** `T` （容器类型）
- **说明：** 反序列化的容器实例

**使用示例：**

```csharp
string json = File.ReadAllText("container.json");
var container = ContainerJsonSerializer.FromJson<LinerContainer>(json);
```

---

### ItemJsonSerializer 类

**命名空间：** `EasyPack.InventorySystem`

**说明：**  
物品的 JSON 序列化/反序列化工具。

---

#### 方法

##### `static string ToJson(IItem item)`

**说明：** 将物品序列化为 JSON 字符串。

---

##### `static IItem FromJson(string json)`

**说明：** 从 JSON 字符串反序列化物品。

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

## 延伸阅读

- [用户使用指南](./UserGuide.md) - 查看完整使用场景和最佳实践
- [Mermaid 图集](./Diagrams.md) - 查看类关系和数据流图

---

**维护者：** NEKOPACK 团队  
**反馈渠道：** [GitHub Issues](https://github.com/CutrelyAlex/NEKOPACK-GITHUB/issues)
