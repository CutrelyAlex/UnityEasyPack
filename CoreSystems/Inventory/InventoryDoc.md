# Inventory系统使用指南

## 目录
- [系统概述](#系统概述)
- [核心组件](#核心组件)
- [基本使用流程](#基本使用流程)
- [容器与物品](#容器与物品)
- [条件系统](#条件系统)
- [事件系统](#事件系统)
- [序列化](#序列化)
- [API参考](#api参考)
  - [容器 IContainer/Container/LinerContainer](#容器-icontainercontainerlinercontainer)
  - [InventoryManager](#inventorymanager)
- [查询与统计](#查询与统计)
- [添加与移除](#添加与移除)
  - [指定槽位添加规则](#指定槽位添加规则)
  - [满 Full 判定细节](#满-full-判定细节)
- [整理与排序](#整理与排序)
  - [Consolidate / Sort / Organize 差异](#consolidate--sort--organize-差异)
- [跨容器与高级操作](#跨容器与高级操作)
  - [MoveItem / TransferItems / AutoMoveItem](#moveitem--transferitems--automoveitem)
  - [BatchMoveItems 批量移动](#batchmoveitems-批量移动)
  - [DistributeItems 分发逻辑](#distributeitems-分发逻辑)
  - [全局条件 Global Conditions](#全局条件-global-conditions)
- [性能优化](#性能优化)
- [调试与校验](#调试与校验)
- [常见用例](#常见用例)
- [最佳实践](#最佳实践)
- [常见问题](#常见问题)

## 系统概述
Inventory 系统用于管理游戏内的物品存储、查询、移动与整理，支持单容器与多容器（全局）管理，具备高性能缓存、批处理与事件模型。  
你可以查看 Inventory/Example/InventoryExample.cs 中的案例（已包含案例1~15）获得直观示例。

### 特性
- 高性能缓存：物品总数、槽位索引、类型索引、空槽位索引
- 批处理与事件折叠：批量添加/移除时合并事件
- 丰富查询：按ID、类型、属性、名称、自定义条件
- 全局管理：容器注册、优先级、分类、全局物品条件
- 高级跨容器：移动 / 指定数量转移 / 自动搬运 / 批量移动 / 优先级分发
- 序列化：容器元数据、槽位、物品属性、条件
- 易扩展：自定义 IItem / IItemCondition / 容器实现

## 核心组件
- IItem：物品接口（ID / Name / Type / IsStackable / MaxStackCount / Weight / Attributes）
- ISlot：槽位（Item / ItemCount / IsOccupied / 条件校验）
- Container / LinerContainer：线性容器实现
- InventoryManager：多容器调度与全局操作
- 条件（IItemCondition）：ItemTypeCondition、AttributeCondition、自定义
- 序列化：ContainerSerializer + DTO (SerializableContainer / SerializableSlot / ConditionDTO)

## 基本使用流程
```
var bag = new LinerContainer("player_backpack", "玩家背包", "Backpack", 20);
var potion = new Item { ID="health_potion", Name="生命药水", IsStackable=true, MaxStackCount=20, Type="Consumable" };
var (addResult, added) = bag.AddItems(potion, 5);
bool hasPotion = bag.HasItem("health_potion");
int total = bag.GetItemTotalCount("health_potion");
var slots = bag.FindSlotIndices("health_potion");
var removeRes = bag.RemoveItem("health_potion", 2);
```

## 容器与物品
- 容量 Capacity：-1 表示无限
- Full：无空槽位且所有占用槽位不可继续堆叠
- IsEmpty：无任何占用槽位（已自动跳过空引用）
- 物品克隆：不可堆叠建议使用 Clone() 复制独立实例（防止共享状态）
- 权重统计：GetTotalWeight O(1) 汇总（缓存增量维护）

## 条件系统
容器可附加多个 IItemCondition，所有条件均需通过才允许放入。  
内置：
- ItemTypeCondition(string type)
- AttributeCondition(string key, object value[, 比较类型])
  - 支持数值比较：GreaterThan / LessThan / GreaterThanOrEqual / LessThanOrEqual / Equal / NotEqual

自定义：
```
public class CustomItemCondition : IItemCondition {
    public bool IsMatch(IItem item) { /* ... */ }
    public string Description => "自定义说明";
}
```
序列化：
- 仅实现 ISerializableCondition 的条件会写入 ConditionDTO
- 反序列化按 Kind 还原（未识别的 Kind 会跳过并告警）

## 事件系统
容器事件：
- OnItemAddResult(IItem item, requested, actual, AddItemResult result, List<int> slots)
- OnItemRemoveResult(string itemId, requested, actual, RemoveItemResult result, List<int> slots)
- OnSlotCountChanged(int slotIndex, IItem item, int oldCount, int newCount)
- OnItemTotalCountChanged(string itemId, IItem itemRef, int oldTotal, int newTotal)

InventoryManager 事件：
- OnContainerRegistered / OnContainerUnregistered
- OnContainerPriorityChanged / OnContainerCategoryChanged
- OnGlobalConditionAdded / OnGlobalConditionRemoved
- OnItemMoved (MoveItem 成功)
- OnItemsTransferred (TransferItems 成功)
- OnBatchMoveCompleted (批量移动结束)
- OnItemsDistributed (分发结束)

批量模式（BeginBatchUpdate / EndBatchUpdate）会聚合总量变化事件，减少 UI 频繁刷新。

## 序列化
使用 ContainerSerializer：
```
string json = ContainerSerializer.ToJson(container, prettyPrint:true);
var restored = ContainerSerializer.FromJson(json);
```
保存内容：
- ContainerKind / ID / Name / Type / Capacity / IsGrid / Grid
- 槽位：Index / ItemJson / ItemCount
- 物品 JSON 内含：ID / Name / Type / IsStackable / MaxStackCount / Weight / Attributes / 自定义字段
- 条件：Kind / 参数（仅 ISerializableCondition）

注意：
- 反序列化当前默认创建 LinerContainer
- 不存在的条件 Kind 会跳过
- 可用于关卡存档 / 快速调试重放

## API参考

### 容器 IContainer/Container/LinerContainer
- OrganizeInventory(): Consolidate + Sort
- ConsolidateItems(): 只合并堆叠
- SortInventory(): 按 Type->Name 排序
- MoveItemToContainer(int fromSlot, Container target, int targetSlot=-1)

其余查询/添加/移除参见下文。

### InventoryManager
- TransferItems(string itemId, int count, string from, string to) 指定数量移动
- AutoMoveItem(string itemId, string from, string to) 移动所有剩余
- BatchMoveItems(List<MoveRequest>) 多操作执行（返回结果列表）
- DistributeItems(IItem itemProto, int totalCount, List<string> targets)
  - 按注册优先级排序（高→低）依次分发
  - 遇到满或条件不满足会跳过继续下一个
- 全局条件：
  - AddGlobalItemCondition / RemoveGlobalItemCondition
  - SetGlobalConditionsEnabled(true/false)
  - 启用后会动态注入至已注册及后续注册的容器
  - 关闭时自动清除这些全局条件条目

## 查询与统计
- GetUniqueItemCount(): 当前不同物品 ID 数
- GetAllItemCountsDict(): 字典形式统计
- GetAllItems(): 返回 (item, count, slotIndex)
- GetItemsByName(pattern): 模糊匹配
- GetItemsWhere(predicate): 自定义过滤
- GetTotalWeight(): 增量更新避免全表遍历

示例：
```
var uniques = bag.GetUniqueItemCount();
float weight = bag.GetTotalWeight();
var byAttr = bag.GetItemsByAttribute("Rarity", "Epic");
var heavy = bag.GetItemsWhere(i => i.Weight > 5f);
```

## 添加与移除
内部流程简单示例：
1. 指定槽位（提供 slotIndex 并尝试校验兼容）
2. 已有堆叠槽位（同ID未满优先）
3. 空槽位缓存
4. 容量允许下创建新槽（线性容器）
5. 事件派发与缓存增量更新

### 指定槽位添加规则
失败情况：
- 槽位越界
- 槽位占用且 Item.ID 不同
- 可堆叠但已满 / 不可堆叠且已占用
- 条件不满足
成功追加：
- 同 ID 且未达 MaxStackCount

### 满 Full 判定细节
Full = (无可再用空槽位) 且 (所有占用槽位：不可堆叠 或 已达上限)  
因此：
- 仍可堆叠时可 AddItems
- 移除使某槽位未满会立刻使 Full=false（缓存立即更新）

## 整理与排序
- ConsolidateItems：逐 ID 合并（保持相对顺序不保证紧凑排序）
- SortInventory：仅排序，不合并
- OrganizeInventory：先 Consolidate，再 Sort，再压缩空隙（示例案例8）

### Consolidate / Sort / Organize 差异
| 操作 | 合并 | 排序 | 去空洞 | 典型用途 |
|------|------|------|--------|----------|
| ConsolidateItems | 是 | 否 | 否 | 仅回收零散堆叠 |
| SortInventory | 否 | 是 | 否 | 分类展示 |
| OrganizeInventory | 是 | 是 | 是 | 玩家“一键整理” |

## 跨容器与高级操作

### MoveItem / TransferItems / AutoMoveItem
- MoveItem：基于槽位（一个槽→目标容器可堆叠或空位），失败返回枚举
- TransferItems：按数量跨容器提取（聚合多个槽）
- AutoMoveItem：移动全部剩余同 ID 物品

返回枚举 (InventoryManager.MoveResult)：
- Success / SourceContainerNotFound / TargetContainerNotFound
- SourceSlotNotFound / SourceSlotEmpty / ItemNotFound
- TargetContainerFull / InsufficientQuantity / InvalidRequest
- ItemConditionNotMet

### BatchMoveItems 批量移动
传入 List<MoveRequest>：
```
new MoveRequest(fromId, fromSlot, toId, toSlotIndex=-1, count=-1, expectedItemId=null)
```
- count=-1 表示整槽
- expectedItemId 不为空时校验 Item.ID 一致
- 返回与输入顺序对应的结果列表（不短路）

### DistributeItems 分发逻辑
流程：
1. 目标容器按优先级（高→低）排序
2. 逐容器执行 AddItems
3. 可堆叠溢出继续下一个容器
4. 返回字典：containerId → 实际分配数量
典型：战利品、批量产出、自动填装

### 全局条件 Global Conditions
- 添加时若已启用：立即附加到所有已注册容器（去重）
- 启用：注入
- 禁用：移除所有曾注入的全局条件（不影响容器原生条件）
- ValidateGlobalItemConditions(IItem) 可独立预检
常见用途：活动期间全世界容器只接收指定稀有度物品等

## 调试与校验
- ValidateCaches(): 断言缓存一致性
- RebuildCaches(): 强制重建缓存

## 常见问题
Q: 指定槽位添加失败原因？  
A: 槽位越界 / ID 不同 / 已满 / 条件不满足。

Q: 我需要先 Sort 还是 Organize？  
A: 只排序 → SortInventory；同时合并+排序 → OrganizeInventory。

Q: 批量移动有局部失败会回滚吗？  
A: 不会。BatchMoveItems 是“逐条执行+逐条结果”，需要事务语义可自行封装。

Q: 序列化后条件丢失？  
A: 未实现 ISerializableCondition / Kind 未识别会被忽略。

Q: 统计不一致？  
A: 调用 ValidateCaches()；若失败 RebuildCaches() 并检查是否绕过 API 修改槽位。

---
