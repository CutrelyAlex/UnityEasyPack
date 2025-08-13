# Inventory系统使用指南

## 目录
- [系统概述](#系统概述)
- [核心组件](#核心组件)
- [基本使用流程](#基本使用流程)
- [容器与物品](#容器与物品)
- [事件系统](#事件系统)
- [API参考](#api参考)
  - [容器 IContainer/Container/LinerContainer](#容器-icontainercontainerlinercontainer)
  - [InventoryManager](#inventorymanager)
- [查询与统计](#查询与统计)
- [添加与移除](#添加与移除)
- [跨容器与批量操作](#跨容器与批量操作)
- [整理与排序](#整理与排序)
- [性能优化](#性能优化)
- [测试与调试](#测试与调试)
- [常见用例](#常见用例)
- [最佳实践](#最佳实践)
- [常见问题](#常见问题)

## 系统概述
Inventory 系统用于管理游戏内的物品存储、查询、移动与整理，支持单容器与多容器（全局）管理，具备高性能缓存、批处理与事件模型。
你可以查看Inventory/Example/InventoryExample.cs中的示例代码来获得更加直观的案例

### 特性
- 高性能缓存：物品总数、槽位索引、类型索引、空槽位索引
- 批处理与事件折叠：批量添加/移除时合并事件
- 丰富查询：按ID、类型、属性、名称、条件等
- 强大的管理器：容器注册、优先级/分类、全局条件、跨容器操作
- 易扩展：线性容器、网格容器可扩展

## 核心组件
- IItem：物品接口（ID、Name、Type、IsStackable、MaxStackCount、Weight、Attributes 等）
- ISlot：槽位接口（Item、ItemCount、IsOccupied、CheckSlotCondition、SetItem、ClearSlot）
- IContainer/Container：容器抽象与实现（缓存、查询、添加/移除、事件）
- LinerContainer：线性容器实现（常用背包/箱子）
- ContainerCacheManager：容器缓存管理（计数、索引、空槽位）
- ItemQueryService：查询服务（封装缓存读取）
- InventoryManager：多容器管理器（注册/全局查询/跨容器/分发/批量等）

## 基本使用流程
```
// 创建容器（容量 -1 表示无限） 
var bag = new LinerContainer("player_backpack", "玩家背包", "Backpack", 20);
// 创建物品
var potion = new Item { ID="health_potion", Name="生命药水", IsStackable=true, MaxStackCount=20, Type="Consumable" };
// 添加物品 
var (addResult, added) = bag.AddItems(potion, 5);
// 查询 
bool hasPotion = bag.HasItem("health_potion"); int total = bag.GetItemTotalCount("health_potion"); var slots = bag.FindSlotIndices("health_potion");
// 移除 
var removeRes = bag.RemoveItem("health_potion", 2);
```

## 容器与物品
- 容量 Capacity：整数；-1 表示无限容量
- Full 判定：仅当无空槽位且所有占用槽位都不可再堆叠时才满（系统内部做 O(1) 优化）
- 条件限制：ContainerCondition 支持 IItemCondition（如 ItemTypeCondition、AttributeCondition）

## 事件系统
容器提供统一的操作结果事件，便于 UI/日志/音效联动：
- OnItemAddResult(IItem item, int requestedCount, int actualCount, AddItemResult result, List<int> affectedSlots)
- OnItemRemoveResult(string itemId, int requestedCount, int actualCount, RemoveItemResult result, List<int> affectedSlots)
- OnSlotCountChanged(int slotIndex, IItem item, int oldCount, int newCount)
- OnItemTotalCountChanged(string itemId, IItem itemRef, int oldTotal, int newTotal)

批量模式（BeginBatchUpdate/EndBatchUpdate）下会合并总量变化事件，减少抖动。

## API参考

### 容器 IContainer/Container/LinerContainer
常用方法（选摘）：
- 查询
  - HasItem(string itemId)
  - GetItemTotalCount(string itemId)
  - HasEnoughItems(string itemId, int requiredCount)
  - FindSlotIndices(string itemId)
  - FindFirstSlotIndex(string itemId)
  - GetItemsByType(string type)
  - GetItemsByAttribute(string name, object value)
  - GetItemsByName(string pattern)
  - GetItemsWhere(Func<IItem,bool> predicate)
  - GetAllItemCountsDict()
  - GetAllItems()
  - GetUniqueItemCount()
  - IsEmpty()
  - GetTotalWeight()
- 添加
  - AddItems(IItem item, int count=1, int slotIndex=-1)
  - AddItemsWithCount(IItem item, out int exceededCount, int count=1, int slotIndex=-1)
  - AddItemsBatch(List<(IItem item,int count)> items)
  - AddItemsAsync(IItem item, int count, CancellationToken token=default)
- 移除
  - RemoveItem(string itemId, int count=1)
  - RemoveItemAtIndex(int index, int count=1, string expectedItemId=null)
- 其他
  - RebuildCaches(), ValidateCaches()
  - 属性：ID/Name/Type/Capacity/Slots/Full 等

LinerContainer 额外提供整理相关方法（在示例与测试中使用）：ConsolidateItems(), SortInventory(), OrganizeInventory(), MoveItemToContainer(...)

### InventoryManager
容器注册/管理：
- RegisterContainer(IContainer container, int priority=0, string category="Default")
- UnregisterContainer(string id)
- GetContainer(string id), GetAllContainers()
- GetContainersByType(string type), GetContainersByCategory(string category)
- IsContainerRegistered(string id)
- SetContainerPriority(string id, int priority), GetContainerPriority(string id)
- SetContainerCategory(string id, string category), GetContainerCategory(string id)
- 事件：OnContainerRegistered, OnContainerUnregistered, OnContainerPriorityChanged, OnContainerCategoryChanged

全局条件（自动应用到注册容器）：
- AddGlobalItemCondition(IItemCondition cond), RemoveGlobalItemCondition(IItemCondition cond)
- SetGlobalConditionsEnabled(bool enabled), IsGlobalConditionsEnabled
- ValidateGlobalItemConditions(IItem item)
- RefreshGlobalCache(), ValidateGlobalCache()

跨容器与全局搜索：
- MoveItem(string fromId, int fromSlotIndex, string toId, int toSlotIndex=-1)
- TransferItems(string itemId, int count, string fromId, string toId)
- AutoMoveItem(string itemId, string fromId, string toId)
- BatchMoveItems(List<MoveRequest> requests)
- DistributeItems(IItem item, int totalCount, List<string> toContainerIds)
- FindItemGlobally(string itemId)
- GetGlobalItemCount(string itemId)
- FindContainersWithItem(string itemId)
- SearchItemsByType/Name/Attribute/Condition
- 事件：OnItemMoved, OnItemsTransferred, OnBatchMoveCompleted, OnItemsDistributed

## 查询与统计
高频 O(1)/O(k) 查询通过 ContainerCacheManager 完成：
- 物品总数缓存：GetItemTotalCount
- 物品→槽位索引：FindSlotIndices
- 类型→槽位索引：GetItemsByType
- 空槽位集合：用于快速放置/判定

示例：
```csharp
int arrows = bag.GetItemTotalCount("arrow"); 
var weaponSlots = bag.GetItemsByType("Weapon"); 
var rares = bag.GetItemsByAttribute("Rarity", "Rare"); 
var heavy = bag.GetItemsWhere(i => i.Weight > 5f);
```


## 添加与移除
添加（内部流程）：
1) TryStackItems：优先堆叠（按剩余空间优先）
2) TryAddToSpecificSlot：可选的指定槽位
3) TryAddToEmptySlot：利用空槽位缓存
4) TryAddToNewSlot：容量允许时创建新槽位（线性容器）

移除：
- RemoveItem：按缓存的槽位索引集合有序扣减（避免全表扫描）
- RemoveItemAtIndex：索引移除，支持 expectedItemId 校验

示例：
```csharp
// 添加（自动找位/堆叠） 
var (res, cnt) = bag.AddItems(potion, 25);
// 指定槽位添加 
var (res2, cnt2) = bag.AddItems(potion, 5, slotIndex: 3);
// 移除 
var r1 = bag.RemoveItem("potion", 6); var r2 = bag.RemoveItemAtIndex(3, 2, "potion");
```

## 跨容器与批量操作
```csharp
var mgr = new InventoryManager();
// 注册容器并设置优先级/分类 
mgr.RegisterContainer(bag, 100, "Player"); mgr.RegisterContainer(chest, 50, "Storage");
// 跨容器移动
mgr.MoveItem("player_backpack", 0, "storage_chest", -1);
// 指定数量转移 
mgr.TransferItems("apple", 8, "storage_chest", "player_backpack");
// 自动移动（同类堆叠与空位） 
mgr.AutoMoveItem("potion", "storage_chest", "player_backpack");
// 批量移动 
var results = mgr.BatchMoveItems(new List<InventoryManager.MoveRequest> { new("storage_chest", 0, "player_backpack", -1, 5, "apple"), new("player_backpack", 2, "storage_chest") });
// 优先级分发 
var distribution = mgr.DistributeItems(new Item{ ID="arrow", IsStackable=true, MaxStackCount=99 }, 200, new List<string>{ "high", "medium", "low" });
```

## 整理与排序
- ConsolidateItems：同ID可堆叠物品合并至尽量少的槽位（遵守 MaxStackCount）
- SortInventory：按 Type/Name 等排序
- OrganizeInventory：合并 + 排序的组合操作

## 常见用例
- 背包/储物箱：LinerContainer + 容量限制
- 装备栏：例如附加 ItemTypeCondition("Equipment")
- 工厂/物流：批量 AddItems、跨容器 TransferItems/AutoMoveItem
- 玩家整理：ConsolidateItems/SortInventory/OrganizeInventory

## 常见问题
Q: 容器已满为什么还能添加可堆叠物？
A: Full 的定义是“无空槽位且没有任何可继续堆叠的占用槽位”。若存在同ID未达上限的槽位，则仍可堆叠添加。

Q: AddItems 指定槽位失败的常见原因？
A: 槽位越界、槽位占用且物品ID不同、不可堆叠或已达上限、槽位条件不满足。

Q: 大量添加为何建议使用批量或异步？
A: 批量模式合并事件与缓存写入；异步在极大规模时避免阻塞主线程（注意容器非线程安全、避免并发访问）。

Q: 统计结果不一致如何处理？
A: 调用 ValidateCaches()/RebuildCaches() 校验/重建；检查是否有自定义代码绕过了容器 API 修改槽位。

---
