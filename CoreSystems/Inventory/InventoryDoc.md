# Inventory系统文档

## 目录
- [系统概述](#系统概述)
- [核心组件](#核心组件)
- [快速开始](#快速开始)
- [条件系统](#条件系统)
- [序列化系统](#序列化系统)
- [API参考](#api参考)
- [高级功能](#高级功能)
- [性能优化](#性能优化)
- [最佳实践](#最佳实践)

---

## 系统概述

Inventory系统是一个**生产级**的RPG背包解决方案，提供高性能物品管理、灵活的条件过滤和完整的序列化支持。

### 核心特性

- ⚡ **高性能缓存** - O(1)查询，增量更新
- 🎯 **灵活条件系统** - 支持组合条件和自定义扩展
- 📦 **注册器模式序列化** - 零硬编码，易于扩展
- 🔄 **跨容器操作** - 移动、转移、批量、分发
- 📊 **事件驱动** - 完整的生命周期事件
- 🧩 **模块化设计** - 易于集成和定制

### 性能指标

| 操作 | 时间复杂度 | 说明 |
|------|-----------|------|
| 查询物品总数 | O(1) | 增量缓存 |
| 查找槽位 | O(1) | 哈希表索引 |
| 添加物品 | O(1) | 空槽缓存 |
| 整理背包 | O(n log n) | 排序算法 |

---

## 核心组件

### 接口层
```csharp
IItem           // 物品接口
ISlot           // 槽位接口
IItemCondition  // 条件接口
IConditionSerializer // 序列化器接口
```

### 实现层
```csharp
Item            // 物品实现
Slot            // 槽位实现
LinerContainer  // 线性容器
InventoryManager // 全局管理器
```

### 序列化层
```csharp
SerializationRegistry     // 注册器（核心）
ContainerSerializer       // 容器序列化器
ConditionSerializers     // 条件序列化器集合
```

---

## 快速开始

### 创建容器
### 创建容器

```csharp
// 创建线性容器
var backpack = new LinerContainer("player_bag", "玩家背包", "Backpack", 20);

// 添加条件限制（仅接受装备）
backpack.ContainerCondition.Add(new ItemTypeCondition("Equipment"));
```

### 物品操作

```csharp
// 创建物品
var sword = new Item 
{ 
    ID = "iron_sword", 
    Name = "铁剑", 
    Type = "Equipment",
    IsStackable = false,
    Weight = 5.0f
};

// 添加物品
var (result, count) = backpack.AddItems(sword);

// 查询物品
bool hasItem = backpack.HasItem("iron_sword");
int total = backpack.GetItemTotalCount("iron_sword");

// 移除物品
var removeResult = backpack.RemoveItem("iron_sword", 1);
```

### 容器管理

```csharp
// 创建全局管理器
var manager = new InventoryManager();

// 注册容器
manager.RegisterContainer(backpack, priority: 100, category: "Player");

// 跨容器转移
manager.TransferItems("iron_sword", 1, "player_bag", "storage_chest");
```

---

## 条件系统

### 内置条件

#### 1. ItemTypeCondition（类型过滤）
```csharp
var condition = new ItemTypeCondition("Equipment");
container.ContainerCondition.Add(condition);
```

#### 2. AttributeCondition（属性过滤）
```csharp
// 等于判断
var condition1 = new AttributeCondition("Rarity", "Epic");

// 数值比较
var condition2 = new AttributeCondition(
    "Level", 
    10, 
    AttributeComparisonType.GreaterThanOrEqual
);

// 支持的比较类型
// Equal, NotEqual, GreaterThan, LessThan, 
// GreaterThanOrEqual, LessThanOrEqual, Contains, NotContains, Exists
```

### 组合条件

#### AllCondition（全部满足）
```csharp
var allCondition = new AllCondition(
    new ItemTypeCondition("Equipment"),
    new AttributeCondition("Level", 10, AttributeComparisonType.GreaterThanOrEqual),
    new AttributeCondition("Rarity", "Epic")
);
// 物品必须同时满足：是装备 AND 等级≥10 AND 稀有度为Epic
```

#### AnyCondition（任一满足）
```csharp
var anyCondition = new AnyCondition(
    new ItemTypeCondition("Weapon"),
    new ItemTypeCondition("Armor")
);
// 物品只需满足：是武器 OR 是防具
```

#### NotCondition（条件取反）
```csharp
var notCondition = new NotCondition(
    new AttributeCondition("Broken", true)
);
// 物品必须：未损坏
```

### 复杂嵌套条件

```csharp
// 装备背包：接受未损坏的史诗级以上武器或防具
var complexCondition = new AllCondition(
    // 必须是装备
    new ItemTypeCondition("Equipment"),
    
    // 是武器或防具
    new AnyCondition(
        new AttributeCondition("Category", "Weapon"),
        new AttributeCondition("Category", "Armor")
    ),
    
    // 稀有度为Epic或Legendary
    new AnyCondition(
        new AttributeCondition("Rarity", "Epic"),
        new AttributeCondition("Rarity", "Legendary")
    ),
    
    // 未损坏
    new NotCondition(new AttributeCondition("Broken", true))
);

container.ContainerCondition.Add(complexCondition);
```

### 自定义条件

#### 方法1：简单条件（不需要序列化）

```csharp
public class WeightLimitCondition : IItemCondition
{
    public float MaxWeight { get; set; }
    
    public WeightLimitCondition(float maxWeight)
    {
        MaxWeight = maxWeight;
    }
    
    public bool CheckCondition(IItem item)
    {
        return item != null && item.Weight <= MaxWeight;
    }
}

// 使用
container.ContainerCondition.Add(new WeightLimitCondition(10f));
```

#### 方法2：支持序列化的条件

```csharp
// 1. 实现条件类
public class WeightLimitCondition : IItemCondition
{
    public float MaxWeight { get; set; }
    
    public bool CheckCondition(IItem item)
    {
        return item != null && item.Weight <= MaxWeight;
    }
}

// 2. 实现序列化器
public class WeightLimitConditionSerializer : IConditionSerializer
{
    public string Kind => "WeightLimit";
    
    public bool CanHandle(IItemCondition condition)
    {
        return condition is WeightLimitCondition;
    }
    
    public SerializedCondition Serialize(IItemCondition condition)
    {
        var weightCond = condition as WeightLimitCondition;
        var dto = new SerializedCondition { Kind = Kind };
        
        var entry = new CustomDataEntry { Id = "MaxWeight" };
        entry.SetValue(weightCond.MaxWeight, CustomDataType.Float);
        dto.Params.Add(entry);
        
        return dto;
    }
    
    public IItemCondition Deserialize(SerializedCondition dto)
    {
        float maxWeight = 0f;
        foreach (var p in dto.Params)
        {
            if (p?.Id == "MaxWeight")
            {
                maxWeight = p.FloatValue;
                break;
            }
        }
        return new WeightLimitCondition { MaxWeight = maxWeight };
    }
}

// 3. 注册序列化器（游戏启动时）
void Awake()
{
    SerializationRegistry.RegisterConditionSerializer(
        new WeightLimitConditionSerializer()
    );
}
```

---

## 序列化系统

### 基本序列化

```csharp
// 序列化容器
string json = ContainerSerializer.ToJson(container, prettyPrint: true);

// 反序列化容器
var restored = ContainerSerializer.FromJson(json);
```

### 条件序列化

```csharp
// 序列化条件（包括嵌套的组合条件）
var condition = new AllCondition(
    new ItemTypeCondition("Equipment"),
    new AnyCondition(
        new AttributeCondition("Rarity", "Epic"),
        new AttributeCondition("Rarity", "Legendary")
    )
);

var dto = ContainerSerializer.SerializeCondition(condition);
string condJson = JsonUtility.ToJson(dto, true);

// 反序列化条件
var restoredCond = ContainerSerializer.DeserializeCondition(dto);
```

### 注册自定义容器类型

```csharp
// 假设实现了GridContainer
SerializationRegistry.RegisterContainerFactory("GridContainer", dto =>
{
    return new GridContainer(dto.ID, dto.Name, dto.Type, dto.Grid);
});

// 之后GridContainer会自动支持序列化
var grid = new GridContainer("storage", "仓库", "Storage", new Vector2(10, 10));
string json = ContainerSerializer.ToJson(grid);
var restored = ContainerSerializer.FromJson(json); // 自动识别类型
```

---

## API参考

### Container（容器）

#### 查询方法
```csharp
bool HasItem(string itemId)                          // 是否包含物品
int GetItemTotalCount(string itemId)                 // 物品总数
List<int> FindSlotIndices(string itemId)             // 查找槽位
List<(IItem, int, int)> GetAllItems()                // 所有物品
float GetTotalWeight()                               // 总重量
int GetUniqueItemCount()                             // 不同物品种类数
bool IsFull                                          // 是否已满
int EmptySlotCount                                   // 空槽位数量
```
#### 添加方法
```csharp
(AddItemResult result, int addedCount) AddItems(
    IItem item, 
    int count = 1, 
    int slotIndex = -1
)
// 返回：结果枚举和实际添加数量
// slotIndex=-1表示自动分配

// 结果枚举
enum AddItemResult
{
    Success,                    // 成功
    ItemNull,                   // 物品为空
    InvalidCount,               // 数量无效
    ContainerFull,              // 容器已满
    ItemConditionNotMet,        // 不满足条件
    SlotOccupied,              // 槽位已占用
    InvalidSlotIndex,          // 槽位索引无效
    StackLimitReached          // 堆叠上限
}
```

#### 移除方法
```csharp
(RemoveItemResult result, int removedCount) RemoveItem(
    string itemId, 
    int count
)

(RemoveItemResult result, int removedCount) RemoveItemAtSlot(
    int slotIndex, 
    int count
)

void ClearSlot(int slotIndex)                        // 清空槽位
void ClearAll()                                      // 清空容器

// 结果枚举
enum RemoveItemResult
{
    Success,
    ItemNotFound,
    InsufficientQuantity,
    InvalidSlotIndex,
    SlotEmpty
}
```

#### 整理方法
```csharp
void ConsolidateItems()                              // 合并堆叠
void SortInventory()                                 // 排序物品
void OrganizeInventory()                             // 整理（合并+排序+压缩）
```

#### 批量操作
```csharp
void BeginBatchUpdate()                              // 开始批量模式
void EndBatchUpdate()                                // 结束批量模式（触发事件）
```

#### 跨容器移动
```csharp
MoveItemResult MoveItemToContainer(
    int fromSlotIndex,
    Container targetContainer,
    int targetSlotIndex = -1
)
```

#### 事件
```csharp
// 物品变化事件
event Action<string, int> OnItemAdded               // (itemId, count)
event Action<string, int> OnItemRemoved
event Action<AddItemResult, string, int, int> OnItemAddResult
event Action<RemoveItemResult, string, int, int> OnItemRemoveResult

// 槽位事件
event Action<int> OnSlotCleared                     // (slotIndex)
event Action<int, int> OnSlotChanged                // (slotIndex, newCount)

// 容器状态事件
event Action OnContainerFullChanged                 // Full状态变化
event Action OnContainerCleared                     // 容器清空

// 批量操作事件
event Action OnBatchUpdateCompleted                 // 批量更新完成
```

---

### InventoryManager（全局管理器）

#### 容器注册
```csharp
void RegisterContainer(Container container, int priority = 0, string category = "")
void UnregisterContainer(string containerId)
Container GetContainer(string containerId)
List<Container> GetContainersByCategory(string category)
List<Container> GetAllContainers()
```

#### 跨容器操作
```csharp
// 转移指定数量
MoveResult TransferItems(
    string itemId, 
    int count, 
    string sourceContainerId, 
    string targetContainerId
)

// 自动移动全部
MoveResult AutoMoveItem(
    string itemId, 
    string sourceContainerId, 
    string targetContainerId
)

// 批量移动
List<MoveResult> BatchMoveItems(List<MoveRequest> requests)

// 分发物品（按优先级分配到多个容器）
Dictionary<string, int> DistributeItems(
    IItem itemPrototype, 
    int totalCount, 
    List<string> targetContainerIds
)

// 结果枚举
enum MoveResult
{
    Success,
    SourceContainerNotFound,
    TargetContainerNotFound,
    SourceSlotNotFound,
    SourceSlotEmpty,
    ItemNotFound,
    TargetContainerFull,
    InsufficientQuantity,
    InvalidRequest,
    ItemConditionNotMet
}
```

#### 全局条件
```csharp
void AddGlobalItemCondition(IItemCondition condition)
void RemoveGlobalItemCondition(IItemCondition condition)
void ClearGlobalItemConditions()
void SetGlobalConditionsEnabled(bool enabled)
bool ValidateGlobalItemConditions(IItem item)
```

---

### ContainerSerializer（序列化器）

```csharp
// 容器序列化
string ToJson(Container container, bool prettyPrint = false)
Container FromJson(string json)

// 条件序列化
SerializedCondition SerializeCondition(IItemCondition condition)
IItemCondition DeserializeCondition(SerializedCondition dto)
```

---

### SerializationRegistry（序列化注册器）

```csharp
// 注册容器工厂
void RegisterContainerFactory<T>(
    string kind, 
    Func<SerializedContainer, T> factory
) where T : Container

// 注册条件序列化器
void RegisterConditionSerializer(IConditionSerializer serializer)

// 查询
IConditionSerializer GetConditionSerializer(string kind)
bool CanDeserializeContainer(string kind)
bool CanSerializeCondition(IItemCondition condition)
```

---

## 高级功能

### 指定槽位添加规则

**成功情况**：
- 槽位为空
- 槽位已有相同ID的可堆叠物品且未满

**失败情况**：
- 槽位索引越界
- 槽位已占用且物品ID不同
- 可堆叠物品已达上限
- 不可堆叠物品且槽位已占用
- 不满足容器条件

### 容器满判定

```csharp
// 容器满的条件：
// 1. 无空槽位
// 2. 所有已占用槽位都是：不可堆叠 OR 已达堆叠上限
```

**实时缓存更新**：
- 添加物品时检测空槽消耗
- 移除物品时检测是否产生新空槽或可堆叠槽
- O(1)时间复杂度判定

### 整理与排序差异

| 操作 | 合并堆叠 | 排序 | 压缩空隙 | 使用场景 |
|------|---------|------|---------|----------|
| ConsolidateItems | ✅ | ❌ | ❌ | 回收零散堆叠 |
| SortInventory | ❌ | ✅ | ❌ | 分类展示 |
| OrganizeInventory | ✅ | ✅ | ✅ | 一键整理 |

### 全局条件系统

**工作原理**：
1. 添加全局条件时，如果已启用则立即注入到所有已注册容器
2. 新注册的容器会自动接收已启用的全局条件
3. 禁用时自动从所有容器移除全局条件
4. 不影响容器的原生条件

**典型应用**：
```csharp
// 活动期间：全服容器只接收史诗级以上物品
manager.AddGlobalItemCondition(new AnyCondition(
    new AttributeCondition("Rarity", "Epic"),
    new AttributeCondition("Rarity", "Legendary")
));
manager.SetGlobalConditionsEnabled(true);

// 活动结束
manager.SetGlobalConditionsEnabled(false);
```

### 批量移动详解

```csharp
public class MoveRequest
{
    public string SourceContainerId;    // 源容器ID
    public int SourceSlotIndex;         // 源槽位索引
    public string TargetContainerId;    // 目标容器ID
    public int TargetSlotIndex;         // 目标槽位（-1自动）
    public int Count;                   // 移动数量（-1整槽）
    public string ExpectedItemId;       // 预期物品ID（可选校验）
}

// 批量移动特性：
// - 不短路：部分失败不影响后续操作
// - 返回对应结果列表
// - 支持ID校验（防止UI滞后导致的误操作）
```

### 分发算法

```csharp
// 按优先级排序目标容器
// 逐容器尝试AddItems
// 可堆叠物品溢出继续下一容器
// 返回每个容器实际分配数量

// 示例：战利品分发
var loot = new Item { ID = "gold", IsStackable = true, MaxStackCount = 999 };
var distribution = manager.DistributeItems(
    loot, 
    5000, 
    new List<string> { "bag", "storage", "bank" }
);
// bag: 999, storage: 999, bank: 999, ...
```

---

## 性能优化

### 缓存系统

**多级缓存架构**：
```
ContainerCacheService
├── _itemSlotIndexCache       // 物品→槽位索引映射
├── _emptySlotIndices          // 空槽位索引集合
├── _itemTypeIndexCache        // 物品类型→槽位映射
├── _itemCountCache            // 物品→总数量映射
└── _notFullStackSlotsCount    // 未满堆叠槽位计数
```

**增量更新机制**：
- 添加物品：仅更新相关物品的缓存
- 移除物品：检测是否需要加入空槽缓存
- 批量模式：延迟事件触发

### 批量模式最佳实践

```csharp
// 大量操作使用批量模式
container.BeginBatchUpdate();
try
{
    for (int i = 0; i < 100; i++)
    {
        container.AddItems(items[i]);
    }
}
finally
{
    container.EndBatchUpdate(); // 确保调用
}

```

### 查询优化技巧

```csharp
// ✅ 使用缓存查询
int count = container.GetItemTotalCount("sword");  // O(1)

// ❌ 避免遍历
var items = container.GetAllItems();  // O(n)
int count = items.Where(x => x.Item.ID == "sword").Sum(x => x.Count);

// ✅ 使用服务查询
var service = container.GetService<ItemQueryService>();
var byType = service.GetItemsByType("Equipment");  // O(1)
```

### 容器容量建议
- 超过500槽位可能影响性能考虑分页或多容器方案

---

## 最佳实践

### 条件设计原则

```csharp
// ✅ 推荐：组合简单条件
var condition = new AllCondition(
    new ItemTypeCondition("Equipment"),
    new AttributeCondition("Level", 10, AttributeComparisonType.GreaterThanOrEqual)
);

// ❌ 避免：过度复杂的嵌套（影响序列化性能）
var badCondition = new AllCondition(
    new AnyCondition(
        new AllCondition(...),  // 嵌套层级过深
        new NotCondition(new AnyCondition(...))
    )
);
```

### 序列化注意事项

```csharp
// ✅ 游戏启动时注册所有自定义类型
void Awake()
{
    // 注册自定义容器
    SerializationRegistry.RegisterContainerFactory("GridContainer", dto => 
        new GridContainer(dto.ID, dto.Name, dto.Type, dto.Grid)
    );
    
    // 注册自定义条件
    SerializationRegistry.RegisterConditionSerializer(
        new WeightLimitConditionSerializer()
    );
}

// ✅ 存档前验证
string json = ContainerSerializer.ToJson(container);
var test = ContainerSerializer.FromJson(json);
Debug.Assert(test.GetItemTotalCount("sword") == container.GetItemTotalCount("sword"));
```

### 错误处理

```csharp
// ✅ 检查操作结果
var (result, count) = container.AddItems(item);
if (result == AddItemResult.ContainerFull)
{
    ShowMessage("背包已满");
}
else if (result == AddItemResult.ItemConditionNotMet)
{
    ShowMessage("该物品无法放入此容器");
}

// ✅ 跨容器操作检查
var moveResult = manager.TransferItems("sword", 1, "bag", "storage");
if (moveResult != InventoryManager.MoveResult.Success)
{
    Debug.LogWarning($"移动失败: {moveResult}");
}
```

### 事件订阅管理

```csharp
// ✅ 组件生命周期管理
void OnEnable()
{
    container.OnItemAdded += HandleItemAdded;
    container.OnItemRemoved += HandleItemRemoved;
}

void OnDisable()
{
    container.OnItemAdded -= HandleItemAdded;
    container.OnItemRemoved -= HandleItemRemoved;
}

// ✅ 使用批量事件而非单次事件
container.OnBatchUpdateCompleted += RefreshUI;  // 整理后刷新一次
// ❌ container.OnSlotChanged += RefreshSlot;     // 每个槽位变化都刷新
```

---

## 常见问题

### Q: 指定槽位添加失败的原因？
**A**: 
- 槽位索引越界
- 槽位已有不同ID的物品
- 可堆叠物品已达上限
- 不满足容器条件

### Q: 整理背包应该用哪个方法？
**A**: 
- 只合并堆叠 → `ConsolidateItems()`
- 只排序 → `SortInventory()`
- 完整整理 → `OrganizeInventory()` （推荐）

### Q: 批量移动失败会回滚吗？
**A**: 不会。`BatchMoveItems`逐条执行并返回对应结果，需要事务语义请自行封装。

### Q: 序列化后条件丢失？
**A**: 
- 自定义条件必须实现`ISerializableCondition`
- 序列化器必须注册到`SerializationRegistry`
- 检查条件的`Kind`是否正确

### Q: 统计数据不一致？
**A**: 
1. 调用`ValidateCaches()`检测
2. 如果断言失败，调用`RebuildCaches()`
3. 检查是否直接修改了槽位（应使用容器API）

### Q: 如何实现网格背包（2D布局）？
**A**: 
- 马上会实现的！！！


---

## 示例参考

完整示例代码请参考：
- InventoryExample.cs - 基础功能示例
---
