# Inventory System - Mermaid 图集文档

**适用EasyPack版本：** EasyPack v1.7.0  
**最后更新:** 2025-11-06

---

## 概述

本文档提供 **Inventory System** 的可视化架构和数据流图表，帮助开发者快速理解系统设计。

**图表类型：**
- [类图 (Class Diagram)](#类图) - 展示类型结构和关系
- [流程图 (Flowchart)](#流程图) - 展示执行流程和逻辑分支
- [序列图 (Sequence Diagram)](#序列图) - 展示对象间交互时序
- [状态图 (State Diagram)](#状态图) - 展示状态转换逻辑

---

## 目录

- [Inventory System - Mermaid 图集文档](#inventory-system---mermaid-图集文档)
  - [概述](#概述)
  - [目录](#目录)
  - [服务架构图](#服务架构图)
    - [InventoryService 架构图](#inventoryservice-架构图)
    - [服务初始化流程图](#服务初始化流程图)
  - [类图](#类图)
    - [核心类图](#核心类图)
    - [条件系统类图](#条件系统类图)
  - [流程图](#流程图)
    - [添加物品流程图](#添加物品流程图)
    - [跨容器转移流程图](#跨容器转移流程图)
    - [序列化流程图](#序列化流程图)
  - [序列图](#序列图)
    - [服务初始化序列图](#服务初始化序列图)
    - [物品添加序列图](#物品添加序列图)
    - [InventoryService 跨容器操作序列图](#inventoryservice-跨容器操作序列图)
  - [状态图](#状态图)
    - [槽位状态转换图](#槽位状态转换图)
    - [服务生命周期状态图](#服务生命周期状态图)
  - [数据流图](#数据流图)
    - [物品数据流图](#物品数据流图)
  - [延伸阅读](#延伸阅读)

---

## 服务架构图

### InventoryService 架构图

**说明：**  
展示 InventoryService 的服务架构，包括与 EasyPack 架构的集成关系。

```mermaid
graph TB
    subgraph EasyPack["EasyPack 架构"]
        Architecture[EasyPackArchitecture<br/>架构核心]
        IService[IService 接口<br/>服务契约]
        SerializationService[ISerializationService<br/>序列化服务]
    end
    
    subgraph InventorySystem["Inventory System"]
        IInventoryService[IInventoryService 接口]
        InventoryService[InventoryService 实现]
        
        subgraph Serializers["序列化器"]
            ItemSerializer[ItemJsonSerializer]
            ContainerSerializer[ContainerJsonSerializer]
            ConditionSerializer[ConditionJsonSerializer]
            GridItemSerializer[GridItemJsonSerializer]
        end
        
        subgraph Containers["容器管理"]
            Container1[Container 1]
            Container2[Container 2]
            ContainerN[Container N]
        end
    end
    
    Architecture -->|注册| InventoryService
    IService -->|实现| IInventoryService
    IInventoryService -->|实现| InventoryService
    
    InventoryService -->|初始化时注册| SerializationService
    ItemSerializer -->|注册到| SerializationService
    ContainerSerializer -->|注册到| SerializationService
    ConditionSerializer -->|注册到| SerializationService
    GridItemSerializer -->|注册到| SerializationService
    
    InventoryService o--o Container1
    InventoryService o--o Container2
    InventoryService o--o ContainerN
    
    ContainerSerializer -->|依赖注入| SerializationService
    
    style Architecture fill:#e1f5e1
    style InventoryService fill:#fff4e1
    style SerializationService fill:#e1e5ff
```

**架构要点：**

1. **服务集成**：InventoryService 实现 `IService` 接口，由 EasyPackArchitecture 管理
2. **自动注册**：服务初始化时自动注册所有序列化器到 `ISerializationService`
3. **依赖注入**：ContainerSerializer 通过构造函数注入 `ISerializationService`
4. **容器管理**：服务管理多个容器实例，支持优先级和分类

---

### 服务初始化流程图

**说明：**  
展示 InventoryService 的初始化过程。

```mermaid
flowchart TD
    Start([开始: InitializeAsync]) --> CheckState{当前状态?}
    
    CheckState -->|已初始化| SkipInit[跳过初始化]
    CheckState -->|未初始化| SetInitializing[设置状态为 Initializing]
    
    SetInitializing --> InitData[初始化内部数据结构]
    InitData --> GetSerializationService[获取 ISerializationService]
    
    GetSerializationService --> RegisterSerializers[注册序列化器]
    
    subgraph RegisterSerializers [注册序列化器]
        RegItem[注册 ItemJsonSerializer]
        RegGridItem[注册 GridItemJsonSerializer]
        RegContainer[注册 ContainerJsonSerializer]
        RegGridContainer[注册 GridContainerJsonSerializer]
        RegCondition[注册 ConditionJsonSerializer]
        
        RegItem --> RegGridItem
        RegGridItem --> RegContainer
        RegContainer --> RegGridContainer
        RegGridContainer --> RegCondition
    end
    
    RegisterSerializers --> SetReady[设置状态为 Ready]
    SetReady --> LogSuccess[输出初始化成功日志]
    
    SkipInit --> End([结束])
    LogSuccess --> End
    
    style Start fill:#e1f5e1
    style End fill:#ffe1e1
    style RegisterSerializers fill:#e1e5ff
    style SetReady fill:#d4f4dd
```

**初始化说明：**

1. **状态检查**：避免重复初始化
2. **数据初始化**：初始化内部字典和集合
3. **序列化器注册**：
   - 获取 `ISerializationService` 实例
   - 注册所有类型的序列化器
   - ContainerJsonSerializer 注入 `ISerializationService`
4. **状态更新**：设置为 Ready 状态，服务可用

---

## 类图

### 核心类图

**说明：**  
展示 Inventory System 的核心类型、继承关系和主要依赖。

```mermaid
classDiagram
    class IService {
        <<Interface>>
        +ServiceLifecycleState State
        +InitializeAsync() Task
        +Pause()
        +Resume()
        +Dispose()
    }
    
    class IInventoryService {
        <<Interface>>
        +int ContainerCount
        +RegisterContainer(Container, int, string) bool
        +UnregisterContainer(string) bool
        +GetContainer(string) Container
        +TransferItems(string, int, string, string) (MoveResult, int)
        +FindItemGlobally(string) List~GlobalItemResult~
        +SetGlobalConditionsEnabled(bool)
        +Reset()
    }
    
    class InventoryService {
        +ServiceLifecycleState State
        +int ContainerCount
        -Dictionary~string, Container~ _containers
        -Dictionary~string, HashSet~string~~ _containersByType
        -Dictionary~string, int~ _containerPriorities
        -List~IItemCondition~ _globalItemConditions
        +InitializeAsync() Task
        +RegisterContainer(Container, int, string) bool
        +TransferItems(...) (MoveResult, int)
        -RegisterSerializers() Task
    }
    
    class IItem {
        <<Interface>>
        +string ID
        +string Name
        +string Type
        +bool IsStackable
        +int MaxStackCount
        +List~CustomDataEntry~ CustomData
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
        +List~CustomDataEntry~ CustomData
        +bool IsContanierItem
        +List~string~ ContainerIds
        +GetCustomData~T~(string, T) T
        +SetCustomData(string, object)
        +RemoveCustomData(string) bool
        +HasCustomData(string) bool
        +IItem Clone()
    }
    
    class GridItem {
        +List~(int x, int y)~ Shape
        +bool CanRotate
        +RotationAngle Rotation
        +int ActualWidth
        +int ActualHeight
        +GetOccupiedCells() List~(int x, int y)~
        +Rotate() bool
        +static CreateRectangleShape(int, int) List~(int x, int y)~
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
        +List~IItemCondition~ ContainerCondition
        +IReadOnlyList~ISlot~ Slots
        +AddItems(IItem, int) (AddItemResult, int)
        +RemoveItems(string, int) (RemoveItemResult, int)
        +BeginBatch()
        +EndBatch()
    }
    
    class LinerContainer {
        +bool IsGrid = false
        +MoveItemToContainer(int, Container) bool
        +SortInventory()
        +OrganizeInventory()
    }
    
    class GridContainer {
        +bool IsGrid = true
        +int GridWidth
        +int GridHeight
        +AddItemsAtPosition(IItem, int, int, int) (AddItemResult, int)
        +GetItemAt(int, int) IItem
        +MoveItemToPosition(int, int, int, int) bool
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
    
    IService <|-- IInventoryService
    IInventoryService <|.. InventoryService
    
    IItem <|.. Item
    Item <|-- GridItem
    IContainer <|.. Container
    Container <|-- LinerContainer
    Container <|-- GridContainer
    
    InventoryService o-- "0..*" Container
    Container o-- "1..*" Slot
    Container --> "0..*" IItemCondition
```

**图例说明：**
- `<<Interface>>`：接口，定义契约
- `<<Abstract>>`：抽象类，不可直接实例化
- `|--`：继承关系（实线 + 空心三角）
- `..|>`：接口实现（虚线 + 空心三角）
- `-->`：依赖关系（实线 + 箭头）
- `o--`：聚合关系（空心菱形）

**设计要点：**
1. `InventoryService` 实现 `IService` 接口，集成到 EasyPack 架构
2. 服务管理多个容器，支持优先级、分类和全局条件
3. `Container` 是抽象基类，提供通用的物品管理功能
4. `LinerContainer` 适用于传统背包，`GridContainer` 适用于网格布局

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
---

### 跨容器转移流程图

**说明：**  
展示 `InventoryManager.TransferItems()` 的执行流程。

```mermaid
flowchart TD
    Start([开始: TransferItems]) --> ValidateParams[验证参数有效性]
    
    ValidateParams --> GetContainers[获取源容器和目标容器]
    
    GetContainers --> CheckContainersExist{容器都存在?}
    CheckContainersExist -->|否| ReturnFail[返回失败结果]
    CheckContainersExist -->|是| QuickFind[QuickFindItem: 单次遍历查找物品]
    
    QuickFind --> CheckItemFound{找到足够数量的物品?}
    CheckItemFound -->|否| ReturnFail
    CheckItemFound -->|是| ValidateConditions[验证全局物品条件]
    
    ValidateConditions --> CheckConditionsMet{条件满足?}
    CheckConditionsMet -->|否| ReturnFail
    CheckConditionsMet -->|是| TryAddToTarget[尝试添加到目标容器]
    
    TryAddToTarget --> CheckAddSuccess{添加成功?}
    CheckAddSuccess -->|否| ReturnFail
    CheckAddSuccess -->|是| RemoveFromSource[从源容器移除物品]
    
    RemoveFromSource --> CheckRemoveSuccess{移除成功?}
    CheckRemoveSuccess -->|否| ReturnFail
    CheckRemoveSuccess -->|是| TriggerEvents[触发转移事件]
    
    TriggerEvents --> ReturnSuccess[返回成功结果]
    
    ReturnFail --> End([结束])
    ReturnSuccess --> End
    
    style Start fill:#e1f5e1
    style End fill:#ffe1e1
    style QuickFind fill:#fff2cc
    style ReturnSuccess fill:#d4f4dd
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

### 序列化流程图

**说明：**  
展示物品和容器序列化的完整流程，包括依赖注入和自动注册。

```mermaid
flowchart TD
    Start([开始: 序列化操作]) --> GetSerializationService[获取 ISerializationService]
    
    GetSerializationService --> CheckServiceExists{服务存在?}
    CheckServiceExists -->|否| ReturnError[返回错误]
    CheckServiceExists -->|是| DetermineType{序列化类型?}
    
    DetermineType -->|Item| ItemSerializer[使用 ItemJsonSerializer]
    DetermineType -->|GridItem| GridItemSerializer[使用 GridItemJsonSerializer]
    DetermineType -->|Container| ContainerSerializer[使用 ContainerJsonSerializer]
    DetermineType -->|Condition| ConditionSerializer[使用 ConditionJsonSerializer]
    
    ItemSerializer --> SerializeToJson[序列化为 JSON]
    GridItemSerializer --> SerializeToJson
    ConditionSerializer --> SerializeToJson
    
    ContainerSerializer --> CheckDependencyInjection{ISerializationService<br/>已注入?}
    CheckDependencyInjection -->|否| InjectService[注入 ISerializationService]
    CheckDependencyInjection -->|是| SerializeContainer[序列化容器]
    
    InjectService --> SerializeContainer
    SerializeContainer --> SerializeSlots[序列化所有槽位]
    SerializeSlots --> SerializeItems[递归序列化物品]
    SerializeItems --> SerializeToJson
    
    SerializeToJson --> ValidateJson[验证 JSON 格式]
    ValidateJson --> ReturnJson[返回 JSON 字符串]
    
    ReturnError --> End([结束])
    ReturnJson --> End
    
    style Start fill:#e1f5e1
    style End fill:#ffe1e1
    style ContainerSerializer fill:#fff4e1
    style InjectService fill:#e1e5ff
    style ReturnJson fill:#d4f4dd
```

**序列化说明：**

1. **服务获取**：通过依赖注入获取 `ISerializationService`
2. **类型识别**：根据对象类型选择对应的序列化器
3. **依赖注入**：
   - `ContainerJsonSerializer` 需要注入 `ISerializationService`
   - 用于递归序列化容器内的物品
4. **递归序列化**：
   - 容器序列化时递归序列化所有槽位
   - 槽位序列化时递归序列化物品
5. **格式验证**：确保生成的 JSON 格式正确

**自动注册流程：**

```mermaid
flowchart LR
    subgraph Init["服务初始化"]
        InventoryService -->|InitializeAsync| RegisterSerializers[注册序列化器]
    end
    
    subgraph Registration["序列化器注册"]
        RegisterSerializers --> ItemReg[注册 ItemJsonSerializer]
        RegisterSerializers --> GridItemReg[注册 GridItemJsonSerializer]
        RegisterSerializers --> ContainerReg[注册 ContainerJsonSerializer]
        RegisterSerializers --> ConditionReg[注册 ConditionJsonSerializer]
    end
    
    subgraph Service["序列化服务"]
        ItemReg --> ISerializationService
        GridItemReg --> ISerializationService
        ContainerReg --> ISerializationService
        ConditionReg --> ISerializationService
    end
    
    style Init fill:#e1f5e1
    style Registration fill:#fff4e1
    style Service fill:#e1e5ff
```

**注册说明：**
- 服务初始化时自动注册所有序列化器
- `ContainerJsonSerializer` 构造函数接收 `ISerializationService` 参数
- 注册后即可通过服务进行统一序列化管理

---

## 序列图

### 服务初始化序列图

**说明：**  
展示 InventoryService 初始化过程，包括序列化器自动注册。

```mermaid
sequenceDiagram
    participant User as 用户代码
    participant Arch as EasyPackArchitecture
    participant Service as InventoryService
    participant SerSvc as ISerializationService
    participant ItemSer as ItemJsonSerializer
    participant GridItemSer as GridItemJsonSerializer
    participant ContainerSer as ContainerJsonSerializer
    participant CondSer as ConditionJsonSerializer
    
    User->>+Arch: GetInventoryServiceAsync()
    Arch->>Arch: 检查服务是否已创建
    
    alt 服务未创建
        Arch->>+Service: new InventoryService()
        Service-->>-Arch: 服务实例
        Arch->>Arch: 注册到服务容器
    end
    
    Arch->>+Service: InitializeAsync()
    Service->>Service: 检查当前状态
    
    alt 状态为 Uninitialized
        Service->>Service: 设置状态为 Initializing
        Service->>Service: 初始化内部数据结构
        
        Service->>+SerSvc: 获取服务实例
        SerSvc-->>-Service: ISerializationService 实例
        
        Service->>+ItemSer: new ItemJsonSerializer()
        ItemSer-->>-Service: 序列化器实例
        
        Service->>+GridItemSer: new GridItemJsonSerializer()
        GridItemSer-->>-Service: 序列化器实例
        
        Service->>+ContainerSer: new ContainerJsonSerializer(SerSvc)
        ContainerSer->>ContainerSer: 注入 ISerializationService
        ContainerSer-->>-Service: 序列化器实例
        
        Service->>+CondSer: new ConditionJsonSerializer()
        CondSer-->>-Service: 序列化器实例
        
        Service->>+SerSvc: RegisterSerializer(ItemSer)
        SerSvc->>SerSvc: 注册 Item 序列化器
        SerSvc-->>-Service: 注册成功
        
        Service->>+SerSvc: RegisterSerializer(GridItemSer)
        SerSvc->>SerSvc: 注册 GridItem 序列化器
        SerSvc-->>-Service: 注册成功
        
        Service->>+SerSvc: RegisterSerializer(ContainerSer)
        SerSvc->>SerSvc: 注册 Container 序列化器
        SerSvc-->>-Service: 注册成功
        
        Service->>+SerSvc: RegisterSerializer(CondSer)
        SerSvc->>SerSvc: 注册 Condition 序列化器
        SerSvc-->>-Service: 注册成功
        
        Service->>Service: 设置状态为 Ready
        Service->>Service: 输出初始化成功日志
    else 状态为 Ready
        Service->>Service: 跳过重复初始化
    end
    
    Service-->>-Arch: 初始化完成
    Arch-->>-User: InventoryService 实例
```

**时序说明：**

1. **服务获取**：通过 `EasyPackArchitecture.GetInventoryServiceAsync()` 获取服务，延迟初始化模式
2. **初始化检查**：检查服务状态，避免重复初始化
3. **状态转换**：设置为 `Initializing` 状态，初始化内部数据结构
4. **序列化器创建**：获取 `ISerializationService`，创建所有序列化器，`ContainerJsonSerializer` 依赖注入
5. **自动注册**：依次注册所有序列化器到 `ISerializationService`
6. **完成初始化**：设置状态为 `Ready`，返回服务实例

**设计要点：**
- **延迟初始化**：服务在首次使用时才初始化
- **依赖注入**：`ContainerJsonSerializer` 需要 `ISerializationService`
- **自动注册**：初始化时自动注册所有序列化器
- **状态管理**：通过 `ServiceLifecycleState` 管理生命周期

---

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

### InventoryService 跨容器操作序列图

**说明：**  
展示 `InventoryService` 管理多个容器时的交互。

```mermaid
sequenceDiagram
    participant User as 用户代码
    participant Service as InventoryService
    participant Backpack as Container(背包)
    participant Warehouse as Container(仓库)
    
    User->>+Service: RegisterContainer(backpack, priority=1, "equipment")
    Service->>Service: 添加到 _containers 字典
    Service->>Service: 建立类型索引 _containersByType
    Service->>Service: 设置优先级 _containerPriorities
    Service-->>-User: true
    
    User->>+Service: RegisterContainer(warehouse, priority=0, "storage")
    Service->>Service: 注册容器
    Service-->>-User: true
    
    User->>+Service: TransferItems("backpack", 0, "warehouse", "iron_ore", 50)
    Service->>Service: GetContainer("backpack")
    Service->>Service: GetContainer("warehouse")
    
    Service->>+Backpack: HasItem("iron_ore", 50)
    Backpack-->>-Service: true
    
    Service->>+Backpack: GetItemReference("iron_ore")
    Backpack-->>-Service: item 引用
    
    Service->>+Backpack: RemoveItems("iron_ore", 50)
    Backpack->>Backpack: 移除物品
    Backpack-->>-Service: (Success, 50)
    
    Service->>+Warehouse: AddItems(item, 50)
    Warehouse->>Warehouse: 添加物品
    Warehouse-->>-Service: (Success, 50)
    
    Service-->>-User: (MoveResult.Success, 50)
    
    Note over Service: 如果添加失败，会回滚到源容器
```

**时序说明：**

1. **容器注册**（1-7）：
   - 注册背包和仓库到服务
   - 设置优先级（背包优先级更高）
   - 支持分类标签
2. **转移操作**（8-20）：
   - 验证源容器和目标容器存在
   - 从源容器移除物品
   - 添加到目标容器
   - 返回转移结果和实际转移数量
   - 失败时自动回滚

**优势：**
- `InventoryService` 实现 `IService` 接口，集成到 EasyPack 架构
- 自动处理回滚，确保数据一致性
- 支持全局物品搜索和跨容器操作

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

### 服务生命周期状态图

**说明：**  
展示 InventoryService 的生命周期状态和转换条件。

```mermaid
stateDiagram-v2
    [*] --> Uninitialized : 创建实例
    
    Uninitialized --> Initializing : InitializeAsync()
    Initializing --> Ready : 初始化完成
    Initializing --> Uninitialized : 初始化失败
    
    Ready --> Paused : Pause()
    Paused --> Ready : Resume()
    
    Ready --> Disposed : Dispose()
    Paused --> Disposed : Dispose()
    
    Disposed --> [*] : 垃圾回收
    
    note right of Uninitialized
        未初始化状态
        State = Uninitialized
        容器未注册
        序列化器未注册
    end note
    
    note right of Initializing
        初始化中状态
        State = Initializing
        正在注册序列化器
        异步操作进行中
    end note
    
    note right of Ready
        就绪状态
        State = Ready
        服务完全可用
        可以执行所有操作
    end note
    
    note right of Paused
        暂停状态
        State = Paused
        服务暂停
        只能执行查询操作
    end note
    
    note right of Disposed
        已销毁状态
        State = Disposed
        资源已清理
        不可再使用
    end note
```

**状态说明：**

| 状态 | 枚举值 | 说明 | 允许的操作 |
|------|--------|------|-----------|
| `Uninitialized` | `ServiceLifecycleState.Uninitialized` | 服务刚创建，未初始化 | `InitializeAsync()` |
| `Initializing` | `ServiceLifecycleState.Initializing` | 正在初始化 | 无（等待完成） |
| `Ready` | `ServiceLifecycleState.Ready` | 初始化完成，服务可用 | 所有操作 |
| `Paused` | `ServiceLifecycleState.Paused` | 服务暂停 | 查询操作 |
| `Disposed` | `ServiceLifecycleState.Disposed` | 服务已销毁 | 无 |

**转换条件：**

1. `Uninitialized → Initializing`：调用 `InitializeAsync()`
2. `Initializing → Ready`：初始化成功完成
3. `Initializing → Uninitialized`：初始化失败，回滚
4. `Ready → Paused`：调用 `Pause()`
5. `Paused → Ready`：调用 `Resume()`
6. `Ready → Disposed`：调用 `Dispose()`
7. `Paused → Disposed`：调用 `Dispose()`

**最佳实践：**
- 总是检查 `State` 属性再执行操作
- 使用 `InitializeAsync()` 初始化服务，避免重复初始化
- 在应用暂停时调用 `Pause()`，恢复时调用 `Resume()`
- 在应用退出前调用 `Dispose()` 清理资源

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
**反馈渠道：** [GitHub Issues](https://github.com/CutrelyAlex/UnityEasyPack/issues)
