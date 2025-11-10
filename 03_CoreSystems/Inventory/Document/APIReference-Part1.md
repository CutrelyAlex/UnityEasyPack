# Inventory System - API 参考文档 (Part 1)

**适用EasyPack版本：** EasyPack v1.7.0  
**最后更新：** 2025-11-10

---

## 概述

本文档提供 **Inventory System** 的完整 API 参考。本部分（Part 1）包含核心接口与服务类。

**文档系列：**
- **Part 1 - 核心接口与服务类**（当前文档）：系统的服务层和核心接口定义
- **Part 2 - 容器实现类**：容器的具体实现类和缓存服务
- **Part 3 - 物品与网格系统**：物品类、网格物品和查询服务
- **Part 4 - 条件、工具与序列化**：条件系统、工具类和序列化支持

---

## 目录

- [概述](#概述)
- [服务接口](#服务接口)
  - [IInventoryService 接口](#iinventoryservice-接口)
- [服务实现](#服务实现)
  - [InventoryService 类](#inventoryservice-类)
- [核心接口](#核心接口)
  - [IContainer 接口](#icontainer-接口)
  - [ISlot 接口](#islot-接口)

---

## 服务接口

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



---

## 服务实现

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

##### `bool IsGlobalConditionsEnabled { get; }`

**说明：** 全局物品条件是否启用。

**类型：** `bool`

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

##### `Task InitializeAsync()`

**说明：** 异步初始化服务，注册序列化器到 `ISerializationService`。

**返回值：** `Task`

---

##### `void Pause()`

**说明：** 暂停服务操作。

---

##### `void Resume()`

**说明：** 恢复服务操作。

---

##### `void Dispose()`

**说明：** 释放服务资源。

---

##### `InventoryService.MoveResult MoveItem(string fromContainerId, int fromSlot, string toContainerId, int toSlot = -1)`

**说明：** 在容器之间移动物品。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `fromContainerId` | `string` | 必填 | 源容器 ID | - |
| `fromSlot` | `int` | 必填 | 源槽位索引 | - |
| `toContainerId` | `string` | 必填 | 目标容器 ID | - |
| `toSlot` | `int` | 可选 | 目标槽位索引，-1 表示自动查找 | `-1` |

**返回值：**
- **类型：** `InventoryService.MoveResult`
- **说明：** 移动操作结果

---



---

## 核心接口

### IContainer 接口

**命名空间：** `EasyPack.InventorySystem`

**说明：**  
容器接口，定义标准化的容器操作，包括物品添加、移除、查询、条件过滤等核心功能。

---

#### 基本信息属性

##### `string ID { get; }`

**说明：** 容器的唯一标识符。

**类型：** `string`

---

##### `string Name { get; }`

**说明：** 容器显示名称。

**类型：** `string`

---

##### `string Type { get; }`

**说明：** 容器类型，用于分类和条件过滤。

**类型：** `string`

---

##### `int Capacity { get; }`

**说明：** 容器容量（槽位数量），-1 表示无限容量。

**类型：** `int`

---

##### `int UsedSlots { get; }`

**说明：** 已使用的槽位数量。

**类型：** `int`

---

##### `int FreeSlots { get; }`

**说明：** 剩余空闲槽位数量。

**类型：** `int`

---

##### `bool IsGrid { get; }`

**说明：** 是否为网格容器。

**类型：** `bool`

---

##### `List<IItemCondition> ContainerCondition { get; }`

**说明：** 容器条件列表，用于限制可接受的物品类型。

**类型：** `List<IItemCondition>`

---

##### `IReadOnlyList<ISlot> Slots { get; }`

**说明：** 所有槽位的只读视图。

**类型：** `IReadOnlyList<ISlot>`

---

#### 物品操作方法

##### `(AddItemResult result, int actualCount) AddItems(IItem item, int count = 1, int slotIndex = -1)`

**说明：** 添加物品到容器。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `item` | `IItem` | 必填 | 要添加的物品 | - |
| `count` | `int` | 可选 | 添加数量 | `1` |
| `slotIndex` | `int` | 可选 | 指定槽位索引，-1 表示自动查找 | `-1` |

**返回值：**
- **类型：** `(AddItemResult result, int actualCount)`
- **说明：** 操作结果和实际添加数量

---

##### `(RemoveItemResult result, int actualCount) RemoveItems(string itemId, int count = 1)`

**说明：** 从容器移除物品。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemId` | `string` | 必填 | 物品 ID | - |
| `count` | `int` | 可选 | 移除数量 | `1` |

**返回值：**
- **类型：** `(RemoveItemResult result, int actualCount)`
- **说明：** 操作结果和实际移除数量

---

##### `bool HasItem(string itemId)`

**说明：** 检查容器是否包含指定物品。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemId` | `string` | 必填 | 物品 ID | - |

**返回值：**
- **类型：** `bool`
- **说明：** 是否包含该物品

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

##### `List<ISlot> GetItemSlots(string itemId)`

**说明：** 获取包含指定物品的所有槽位。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemId` | `string` | 必填 | 物品 ID | - |

**返回值：**
- **类型：** `List<ISlot>`
- **说明：** 包含该物品的槽位列表

---

#### 槽位操作方法

##### `ISlot GetSlot(int index)`

**说明：** 获取指定索引的槽位。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `index` | `int` | 必填 | 槽位索引 | - |

**返回值：**
- **类型：** `ISlot`
- **说明：** 槽位对象，索引无效返回 null

---

##### `List<ISlot> GetOccupiedSlots()`

**说明：** 获取所有被占用的槽位。

**返回值：**
- **类型：** `List<ISlot>`
- **说明：** 被占用的槽位列表

---

##### `List<ISlot> GetFreeSlots()`

**说明：** 获取所有空闲槽位。

**返回值：**
- **类型：** `List<ISlot>`
- **说明：** 空闲槽位列表

---

##### `bool ClearSlot(int slotIndex)`

**说明：** 清空指定槽位。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `slotIndex` | `int` | 必填 | 槽位索引 | - |

**返回值：**
- **类型：** `bool`
- **说明：** 是否成功清空

---

##### `void ClearAllSlots()`

**说明：** 清空所有槽位。

---

#### 条件过滤方法

##### `List<ISlot> GetSlotsByCondition(IItemCondition condition)`

**说明：** 根据条件获取符合的槽位。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `condition` | `IItemCondition` | 必填 | 过滤条件 | - |

**返回值：**
- **类型：** `List<ISlot>`
- **说明：** 符合条件的槽位列表

---

##### `bool CheckContainerCondition(IItem item)`

**说明：** 检查物品是否满足容器条件。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `item` | `IItem` | 必填 | 要检查的物品 | - |

**返回值：**
- **类型：** `bool`
- **说明：** 是否满足条件

---

#### 容器管理方法

##### `bool ValidateCaches()`

**说明：** 验证容器缓存一致性。

**返回值：**
- **类型：** `bool`
- **说明：** 是否一致

---

##### `void RebuildCaches()`

**说明：** 重建容器缓存。

---

#### 事件

##### `event Action<IItem, int, int, AddItemResult, List<int>> OnItemAddResult`

**说明：** 添加物品操作结果事件。

**参数：**
1. `IItem item` - 操作的物品
2. `int requestedCount` - 请求添加的数量
3. `int actualCount` - 实际添加的数量
4. `AddItemResult result` - 操作结果
5. `List<int> affectedSlots` - 涉及的槽位索引列表

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



### ISlot 接口

**命名空间：** `EasyPack.InventorySystem`

**说明：**  
槽位接口，定义容器中单个槽位的标准操作，包括物品设置、数量管理、条件检查等。

---

#### 属性

##### `int Index { get; }`

**说明：** 槽位索引。

**类型：** `int`

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

##### `Container Container { get; set; }`

**说明：** 槽位所属的容器。

**类型：** `Container`

---

##### `CustomItemCondition SlotCondition { get; }`

**说明：** 槽位条件，用于限制可放置的物品类型。

**类型：** `CustomItemCondition`

---

#### 方法

##### `bool CheckSlotCondition(IItem item)`

**说明：** 检查物品是否满足槽位条件。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `item` | `IItem` | 必填 | 要检查的物品 | - |

**返回值：**
- **类型：** `bool`
- **说明：** 是否满足条件

---

##### `bool SetItem(IItem item, int count = 1)`

**说明：** 设置槽位物品。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `item` | `IItem` | 必填 | 要设置的物品 | - |
| `count` | `int` | 可选 | 物品数量 | `1` |

**返回值：**
- **类型：** `bool`
- **说明：** 设置是否成功

---

##### `void ClearSlot()`

**说明：** 清空槽位。

---


