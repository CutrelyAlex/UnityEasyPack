# Inventory System - Mermaid 图集文档

**适用EasyPack版本：** EasyPack v1.5.30  
**最后更新:** 2025-10-26

---

## 概述

本文档提供 **Inventory System** 的可视化架构和数据流图表，帮助开发者快速理解系统设计。

**图表类型：**
- [类图 (Class Diagram)](#类图) - 展示类型结构和关系
- [流程图 (Flowchart)](#流程图) - 展示执行流程和逻辑分支
- [序列图 (Sequence Diagram)](#序列图) - 展示对象间交互时序
- [状态图 (State Diagram)](#状态图) - 展示槽位状态转换逻辑

---

## 目录

- [概述](#概述)
- [类图](#类图)
  - [核心类图](#核心类图)
  - [条件系统类图](#条件系统类图)
- [流程图](#流程图)
  - [添加物品流程图](#添加物品流程图)
  - [跨容器转移流程图](#跨容器转移流程图)
- [序列图](#序列图)
  - [物品添加序列图](#物品添加序列图)
  - [InventoryManager 跨容器操作序列图](#inventorymanager-跨容器操作序列图)
- [状态图](#状态图)
  - [槽位状态转换图](#槽位状态转换图)
- [数据流图](#数据流图)
  - [物品数据流图](#物品数据流图)
- [延伸阅读](#延伸阅读)

---

## 类图

### 核心类图

**说明：**  
展示 Inventory System 的核心类型、继承关系和主要依赖。

```mermaid
classDiagram
    class IItem {
        <<Interface>>
        +string ID
        +string Name
        +string Type
        +bool IsStackable
        +int MaxStackCount
        +Dictionary~string,object~ Attributes
        +IItem Clone()
    }
    
    class Item {
        +string ID
        +string Name
        +string Type
        +string Description
        +float Weight
        +bool IsStackable
        +int MaxStackCount
        +Dictionary~string,object~ Attributes
        +bool IsContanierItem
        +List~string~ ContainerIds
        +IItem Clone()
    }
    
    class GridItem {
        +int GridWidth
        +int GridHeight
        +IItem Clone()
    }
    
    class IContainer {
        <<Interface>>
        +string ID
        +string Name
        +string Type
        +int Capacity
        +int UsedSlots
        +int FreeSlots
    }
    
    class Container {
        <<Abstract>>
        +string ID
        +string Name
        +string Type
        +int Capacity
        +int UsedSlots
        +int FreeSlots
        +bool IsGrid
        +Vector2 Grid
        +List~IItemCondition~ ContainerCondition
        +IReadOnlyList~ISlot~ Slots
        +AddItems(IItem, int) (AddItemResult, int)
        +RemoveItems(string, int) (RemoveItemResult, int)
        +GetItemTotalCount(string) int
        +HasItem(string, int) bool
        +ClearContainer()
        +BeginBatch()
        +EndBatch()
    }
    
    class LinerContainer {
        +bool IsGrid = false
        +Vector2 Grid = (-1,-1)
        +MoveItemToContainer(int, Container) bool
        +SortInventory()
        +ConsolidateItems()
        +OrganizeInventory()
    }
    
    class GridContainer {
        +bool IsGrid = true
        +Vector2 Grid
        +int GridWidth
        +int GridHeight
        +AddItemsAtPosition(IItem, int, int, int) (AddItemResult, int)
        +GetItemAt(int, int) IItem
        +MoveItemToPosition(int, int, int, int) bool
        +IsPositionAvailable(int, int, int, int) bool
    }
    
    class Slot {
        +int Index
        +Container Container
        +IItem Item
        +int ItemCount
        +bool IsOccupied
        +SetItem(IItem, int)
        +ClearSlot()
    }
    
    class InventoryManager {
        +RegisterContainer(Container, int, string) bool
        +UnregisterContainer(string) bool
        +GetContainer(string) Container
        +GetAllContainers() IReadOnlyList~Container~
        +GetContainersByType(string) List~Container~
        +GetContainersByCategory(string) List~Container~
        +TransferItems(string, string, string, int) bool
        +FindItemInContainers(string) List~Container~
        +GetItemTotalCountAcrossContainers(string) int
        +DistributeItems(IItem, int, string[]) bool
    }
    
    IItem <|.. Item : 实现
    Item <|-- GridItem : 继承
    IContainer <|.. Container : 实现
    Container <|-- LinerContainer : 继承
    Container <|-- GridContainer : 继承
    Container o-- "1..*" Slot : 包含
    Slot --> IItem : 存储
    InventoryManager o-- "0..*" Container : 管理
    Container --> "0..*" IItemCondition : 使用条件
```

**图例说明：**
- `<<Interface>>`：接口，定义契约
- `<<Abstract>>`：抽象类，不可直接实例化
- `|--`：继承关系（实线 + 空心三角）
- `..|>`：接口实现（虚线 + 空心三角）
- `-->`：依赖关系（实线 + 箭头）
- `o--`：聚合关系（空心菱形）

**设计要点：**
1. `Container` 是抽象基类，提供通用的物品管理功能
2. `LinerContainer` 适用于传统背包，`GridContainer` 适用于网格布局
3. `InventoryManager` 集中管理多个容器，支持跨容器操作
4. `Slot` 是容器内的存储单元，关联物品和数量
5. `IItemCondition` 系统提供灵活的物品过滤机制

---

### 条件系统类图

**说明：**  
展示物品条件系统的类型和组合关系。

```mermaid
classDiagram
    class IItemCondition {
        <<Interface>>
        +CheckCondition(IItem) bool
    }
    
    class ISerializableCondition {
        <<Interface>>
        +string Kind
        +ToDto() SerializedCondition
        +FromDto(SerializedCondition) ISerializableCondition
    }
    
    class ItemTypeCondition {
        +string ItemType
        +CheckCondition(IItem) bool
        +ToDto() SerializedCondition
    }
    
    class AttributeCondition {
        +string AttributeKey
        +object AttributeValue
        +CheckCondition(IItem) bool
        +ToDto() SerializedCondition
    }
    
    class CustomItemCondition {
        -Func~IItem,bool~ _conditionFunc
        +CheckCondition(IItem) bool
    }
    
    class AllCondition {
        +List~IItemCondition~ Conditions
        +CheckCondition(IItem) bool
    }
    
    class AnyCondition {
        +List~IItemCondition~ Conditions
        +CheckCondition(IItem) bool
    }
    
    IItemCondition <|.. ItemTypeCondition : 实现
    IItemCondition <|.. AttributeCondition : 实现
    IItemCondition <|.. CustomItemCondition : 实现
    IItemCondition <|.. AllCondition : 实现
    IItemCondition <|.. AnyCondition : 实现
    
    ISerializableCondition <|.. ItemTypeCondition : 实现
    ISerializableCondition <|.. AttributeCondition : 实现
    ISerializableCondition <|.. AllCondition : 实现
    ISerializableCondition <|.. AnyCondition : 实现
    
    AllCondition o-- "1..*" IItemCondition : 组合（AND）
    AnyCondition o-- "1..*" IItemCondition : 组合（OR）
```

**设计要点：**
1. `ItemTypeCondition` 用于简单的类型过滤
2. `AttributeCondition` 用于基于自定义属性的过滤
3. `CustomItemCondition` 支持 lambda 表达式自定义逻辑
4. `AllCondition` 和 `AnyCondition` 支持复杂条件组合
5. 实现 `ISerializableCondition` 的条件可序列化

---

## 流程图

### 添加物品流程图

**说明：**  
展示 `Container.AddItems()` 的执行流程，包括堆叠、条件检查、槽位分配。

```mermaid
flowchart TD
    Start([开始: AddItems]) --> CheckNull{物品是否为 null?}
    
    CheckNull -->|是| ReturnItemIsNull[返回 ItemIsNull]
    CheckNull -->|否| CheckCount{添加数量 > 0?}
    
    CheckCount -->|否| ReturnAddNothing[返回 AddNothingLOL]
    CheckCount -->|是| CheckCondition{检查容器条件}
    
    CheckCondition -->|不满足| ReturnConditionNotMet[返回 ItemConditionNotMet]
    CheckCondition -->|满足| CheckStackable{物品可堆叠?}
    
    CheckStackable -->|是| FindExistingSlot[查找已有同类物品的槽位]
    CheckStackable -->|否| FindEmptySlot[查找空槽位]
    
    FindExistingSlot --> HasExistingSlot{找到可堆叠槽位?}
    HasExistingSlot -->|是| CalculateStackSpace[计算可堆叠空间]
    HasExistingSlot -->|否| FindEmptySlot
    
    CalculateStackSpace --> AddToExisting[添加到现有槽位]
    AddToExisting --> CheckRemaining{还有剩余数量?}
    
    CheckRemaining -->|是| FindEmptySlot
    CheckRemaining -->|否| TriggerEvents[触发事件和缓存更新]
    
    FindEmptySlot --> HasEmptySlot{找到空槽位?}
    HasEmptySlot -->|否| CheckCapacity{容量无限?}
    CheckCapacity -->|是| CreateNewSlot[创建新槽位]
    CheckCapacity -->|否| ReturnFull[返回 ContainerIsFull]
    
    HasEmptySlot -->|是| AddToEmptySlot[添加到空槽位]
    CreateNewSlot --> AddToEmptySlot
    
    AddToEmptySlot --> CheckRemainingAgain{还有剩余数量?}
    CheckRemainingAgain -->|是| FindEmptySlot
    CheckRemainingAgain -->|否| TriggerEvents
    
    TriggerEvents --> ReturnSuccess[返回 Success + 添加数量]
    
    ReturnItemIsNull --> End([结束])
    ReturnAddNothing --> End
    ReturnConditionNotMet --> End
    ReturnFull --> End
    ReturnSuccess --> End
    
    style Start fill:#e1f5e1
    style End fill:#ffe1e1
    style CheckCondition fill:#fff4e1
    style TriggerEvents fill:#e1e5ff
    style ReturnSuccess fill:#d4f4dd
```

**流程说明：**

1. **前置检查**：验证物品和数量的合法性
2. **条件检查**：验证容器条件（如类型限制）
3. **堆叠处理**：
   - 可堆叠物品优先填入已有同类物品的槽位
   - 计算堆叠上限，避免超过 `MaxStackCount`
4. **槽位分配**：
   - 优先使用空槽位
   - 无限容量容器自动创建新槽位
5. **事件触发**：更新缓存、触发 `OnItemAddResult` 事件

**典型执行时间：**
- 添加到现有槽位：约 0.1-0.5 毫秒
- 添加到新槽位：约 0.5-1 毫秒
- 批处理模式：约 5-10 毫秒（100 个物品）

---

### 跨容器转移流程图

**说明：**  
展示 `InventoryManager.TransferItems()` 的执行流程。

```mermaid
flowchart TD
    Start([开始: TransferItems]) --> GetSourceContainer[获取源容器]
    
    GetSourceContainer --> CheckSourceExists{源容器存在?}
    CheckSourceExists -->|否| ReturnFalse1[返回 false]
    CheckSourceExists -->|是| GetTargetContainer[获取目标容器]
    
    GetTargetContainer --> CheckTargetExists{目标容器存在?}
    CheckTargetExists -->|否| ReturnFalse2[返回 false]
    CheckTargetExists -->|是| CheckItemInSource{源容器有物品?}
    
    CheckItemInSource -->|否| ReturnFalse3[返回 false]
    CheckItemInSource -->|是| GetItemReference[获取物品引用]
    
    GetItemReference --> RemoveFromSource[从源容器移除物品]
    
    RemoveFromSource --> CheckRemoveSuccess{移除成功?}
    CheckRemoveSuccess -->|否| ReturnFalse4[返回 false]
    CheckRemoveSuccess -->|是| AddToTarget[添加到目标容器]
    
    AddToTarget --> CheckAddSuccess{添加成功?}
    CheckAddSuccess -->|否| Rollback[回滚：重新添加到源容器]
    CheckAddSuccess -->|是| TriggerEvents[触发转移事件]
    
    Rollback --> ReturnFalse5[返回 false]
    TriggerEvents --> ReturnTrue[返回 true]
    
    ReturnFalse1 --> End([结束])
    ReturnFalse2 --> End
    ReturnFalse3 --> End
    ReturnFalse4 --> End
    ReturnFalse5 --> End
    ReturnTrue --> End
    
    style Start fill:#e1f5e1
    style End fill:#ffe1e1
    style Rollback fill:#ffcccc
    style ReturnTrue fill:#d4f4dd
```

**流程说明：**

1. **容器验证**：确认源容器和目标容器都存在
2. **物品检查**：验证源容器中有足够的物品
3. **移除操作**：从源容器移除指定数量
4. **添加操作**：添加到目标容器
5. **回滚机制**：如果添加失败，重新添加回源容器
6. **事件触发**：成功时触发转移相关事件

**最佳实践：**
- 使用 `TransferItems()` 代替手动 `RemoveItems()` + `AddItems()`
- 回滚机制确保数据一致性

---

## 序列图

### 物品添加序列图

**说明：**  
展示用户代码调用 `AddItems()` 时的完整交互时序。

```mermaid
sequenceDiagram
    participant User as 用户代码
    participant Container as Container
    participant Slot as Slot
    participant Cache as CacheService
    participant Event as 事件监听器
    
    User->>+Container: AddItems(item, 5)
    Container->>Container: 检查物品和数量
    Container->>Container: 验证容器条件
    
    alt 物品可堆叠
        Container->>Cache: 查找已有物品的槽位
        Cache-->>Container: 返回槽位索引列表
        
        loop 遍历已有槽位
            Container->>+Slot: 检查堆叠空间
            Slot-->>-Container: 可堆叠数量
            Container->>+Slot: SetItem(item, newCount)
            Slot->>Slot: 更新物品数量
            Slot-->>-Container: 完成
            Container->>Event: OnSlotCountChanged(index, item, oldCount, newCount)
        end
    end
    
    alt 还有剩余数量
        Container->>Cache: 查找空槽位
        Cache-->>Container: 空槽位索引
        Container->>+Slot: SetItem(item, remainingCount)
        Slot-->>-Container: 完成
        Container->>Cache: 更新空槽位缓存
        Container->>Event: OnSlotCountChanged(index, item, 0, count)
    end
    
    Container->>Cache: 更新物品计数缓存
    Container->>Cache: 更新类型索引缓存
    Container->>Event: OnItemAddResult(item, 5, 5, Success, [slots])
    Container->>Event: OnItemTotalCountChanged(itemId, item, oldTotal, newTotal)
    
    Container-->>-User: (Success, 5)
```

**时序说明：**

1. **初始调用**（1-3）：用户调用 `AddItems()`，容器执行前置检查
2. **堆叠处理**（4-10）：
   - 查询缓存获取已有物品槽位
   - 遍历槽位，逐个填充直到堆叠上限
   - 触发槽位数量变更事件
3. **新槽位分配**（11-16）：
   - 剩余数量分配到空槽位
   - 更新缓存索引
4. **事件触发**（17-20）：
   - 触发添加结果事件
   - 触发物品总数变更事件
5. **返回结果**（21）：返回成功状态和实际添加数量

**性能优化：**
- `CacheService` 避免每次遍历所有槽位
- 批处理模式可延迟事件触发

---

### InventoryManager 跨容器操作序列图

**说明：**  
展示 `InventoryManager` 管理多个容器时的交互。

```mermaid
sequenceDiagram
    participant User as 用户代码
    participant Manager as InventoryManager
    participant Backpack as Container(背包)
    participant Warehouse as Container(仓库)
    
    User->>+Manager: RegisterContainer(backpack, priority=1)
    Manager->>Manager: 添加到 _containers 字典
    Manager->>Manager: 建立类型索引
    Manager->>Manager: 设置优先级和分类
    Manager-->>-User: true
    
    User->>+Manager: RegisterContainer(warehouse, priority=0)
    Manager->>Manager: 注册容器
    Manager-->>-User: true
    
    User->>+Manager: TransferItems("backpack", "warehouse", "iron_ore", 50)
    Manager->>Manager: GetContainer("backpack")
    Manager->>Manager: GetContainer("warehouse")
    
    Manager->>+Backpack: HasItem("iron_ore", 50)
    Backpack-->>-Manager: true
    
    Manager->>+Backpack: GetItemReference("iron_ore")
    Backpack-->>-Manager: item 引用
    
    Manager->>+Backpack: RemoveItems("iron_ore", 50)
    Backpack->>Backpack: 移除物品
    Backpack-->>-Manager: (Success, 50)
    
    Manager->>+Warehouse: AddItems(item, 50)
    Warehouse->>Warehouse: 添加物品
    Warehouse-->>-Manager: (Success, 50)
    
    Manager-->>-User: true
    
    Note over Manager: 如果添加失败，会回滚到源容器
```

**时序说明：**

1. **容器注册**（1-7）：
   - 注册背包和仓库到管理器
   - 设置优先级（背包优先级更高）
2. **转移操作**（8-20）：
   - 验证源容器和目标容器存在
   - 从源容器移除物品
   - 添加到目标容器
   - 失败时自动回滚

**优势：**
- `InventoryManager` 封装了复杂的跨容器逻辑
- 自动处理回滚，确保数据一致性

---

## 状态图

### 槽位状态转换图

**说明：**  
展示槽位（Slot）的生命周期状态和转换条件。

```mermaid
stateDiagram-v2
    [*] --> Empty : 初始化
    
    Empty --> Occupied : SetItem(item, count > 0)
    Occupied --> Empty : ClearSlot()
    
    Occupied --> Occupied : SetItem(item, newCount > 0)
    Occupied --> Empty : SetItem(item, 0)
    
    Empty --> Empty : ClearSlot()
    
    note right of Empty
        空槽位状态
        IsOccupied = false
        Item = null
        ItemCount = 0
    end note
    
    note right of Occupied
        占用状态
        IsOccupied = true
        Item != null
        ItemCount > 0
    end note
```

**状态说明：**

| 状态 | 说明 | 允许的操作 |
|------|------|-----------|
| `Empty` | 槽位为空，未存储任何物品 | `SetItem()` |
| `Occupied` | 槽位已占用，存储物品和数量 | `SetItem()`, `ClearSlot()` |

**转换条件：**

1. `Empty → Occupied`：调用 `SetItem(item, count)` 且 `count > 0`
2. `Occupied → Empty`：调用 `ClearSlot()` 或 `SetItem(item, 0)`
3. `Occupied → Occupied`：调用 `SetItem(item, newCount)` 且 `newCount > 0`

**最佳实践：**
- 使用 `IsOccupied` 属性判断槽位状态，不要直接检查 `Item` 是否为 null
- `SetItem()` 会自动处理状态转换和事件触发

---

## 数据流图

### 物品数据流图

**说明：**  
展示物品数据在系统各组件间的流动路径。

```mermaid
flowchart LR
    Input[(用户输入<br/>物品 + 数量)] --> Validate[容器条件验证]
    Validate --> StackLogic[堆叠逻辑处理]
    StackLogic --> SlotAssign[槽位分配]
    SlotAssign --> CacheUpdate[(缓存更新)]
    SlotAssign --> Persist[(槽位存储)]
    
    Persist --> Query[(查询操作)]
    CacheUpdate -.->|加速查询| Query
    
    Query --> Output[(输出结果)]
    
    Persist --> Serialize[(序列化)]
    Serialize --> SaveFile[(保存文件)]
    
    SaveFile -.->|加载| Deserialize[(反序列化)]
    Deserialize --> Persist
    
    style Input fill:#e1f5e1
    style Output fill:#e1f5e1
    style Persist fill:#ffe1e1
    style CacheUpdate fill:#fff4e1
    style SaveFile fill:#e1e5ff
```

**数据流说明：**

1. **输入阶段**：用户提供物品和数量
2. **验证阶段**：检查容器条件（类型、属性等）
3. **堆叠处理**：计算堆叠逻辑，决定数量分配
4. **槽位分配**：将物品分配到具体槽位
5. **持久化**：
   - 存储到槽位（内存）
   - 更新缓存索引
6. **查询阶段**：
   - 缓存加速查询（如 `FindItemSlotIndex()`）
   - 直接从槽位读取
7. **序列化**：
   - 保存到 JSON 文件
   - 反序列化恢复数据

**性能优化：**
- 缓存系统减少遍历次数（空槽位索引、物品位置索引）
- 批处理模式减少事件触发频率

---

## 延伸阅读

- [用户使用指南](./UserGuide.md) - 查看完整使用场景
- [API 参考文档](./APIReference.md) - 查阅详细 API 说明

---

**维护者：** NEKOPACK 团队  
**图表工具：** Mermaid v10.x  
**反馈渠道：** [GitHub Issues](https://github.com/CutrelyAlex/NEKOPACK-GITHUB/issues)
