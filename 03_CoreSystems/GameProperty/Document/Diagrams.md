# GameProperty System - Diagram Collection


**适用EasyPack版本：** EasyPack v1.6.0
**最后更新：** 2025-10-31

---

## 概述

本文档提供 GameProperty System 的可视化架构图和数据流图，帮助开发者快速理解系统设计和运作机制。

---

## 目录

- [概述](#概述)
- [核心类图](#核心类图)
  - [GameProperty 核心类图](#gameproperty-核心类图)
  - [组合属性类图](#组合属性类图)
  - [管理器类图](#管理器类图)
- [数据流程图](#数据流程图)
  - [属性值计算流程](#属性值计算流程)
  - [依赖关系建立与更新流程](#依赖关系建立与更新流程)
- [序列图](#序列图)
  - [创建并修改属性的完整流程](#创建并修改属性的完整流程)
  - [事件系统交互流程](#事件系统交互流程)
  - [依赖关系建立与传播流程](#依赖关系建立与传播流程)
- [状态图](#状态图)
  - [GameProperty 状态转换](#gameproperty-状态转换)
  - [CombineGameProperty 生命周期状态](#combinegameproperty-生命周期状态)
- [性能优化相关](#性能优化相关)
- [相关资源](#相关资源)

---

## 核心类图

### GameProperty 核心类图

展示 GameProperty 系统的核心类及其关系。

```mermaid
classDiagram
    class IReadableProperty~T~ {
        <<interface>>
        +string ID
        +T GetValue()
    }

    class IModifiableProperty~T~ {
        <<interface>>
        +List~IModifier~ Modifiers
        +AddModifier(IModifier) IModifiableProperty~T~
        +RemoveModifier(IModifier) IModifiableProperty~T~
        +ClearModifiers() IModifiableProperty~T~
    }

    class IDrityTackable {
        <<interface>>
        +MakeDirty() void
        +OnDirty(Action) void
    }

    class GameProperty {
        +string ID
        +List~IModifier~ Modifiers
        -float _baseValue
        -float _cacheValue
        -bool _isDirty
        +bool IsDirty
        +PropertyDependencyManager DependencyManager
        +GameProperty(string id, float initValue)
        +float GetValue()
        +float GetBaseValue()
        +SetBaseValue(float) IModifiableProperty~float~
        +AddModifier(IModifier) IModifiableProperty~float~
        +RemoveModifier(IModifier) IModifiableProperty~float~
        +ClearModifiers() IModifiableProperty~float~
        +AddDependency(GameProperty, Func) IModifiableProperty~float~
        +RemoveDependency(GameProperty) IModifiableProperty~float~
        +MakeDirty() void
        +OnDirty(Action) void
        +event Action~float, float~ OnValueChanged
        +event Action~float, float~ OnBaseValueChanged
        +OnDirtyAndValueChanged(Action~float, float~) void
        +NotifyIfChanged() IModifiableProperty~float~
    }

    class PropertyDependencyManager {
        -GameProperty _owner
        -HashSet~GameProperty~ _dependencies
        -HashSet~GameProperty~ _dependents
        -Dictionary~GameProperty, Func~ _dependencyCalculators
        +int DependencyDepth
        +bool HasRandomDependency
        +int DependencyCount
        +int DependentCount
        +AddDependency(GameProperty, Func) bool
        +RemoveDependency(GameProperty) bool
        +TriggerDependentUpdates(float) void
        +UpdateDependencies() void
        +HasDirtyDependencies() bool
    }

    class IModifier {
        <<interface>>
        +ModifierType Type
        +int Priority
        +IModifier Clone()
    }

    class FloatModifier {
        +ModifierType Type
        +int Priority
        +float Value
        +FloatModifier(ModifierType, int, float)
        +IModifier Clone()
    }

    class RangeModifier {
        +ModifierType Type
        +int Priority
        +Vector2 Value
        +RangeModifier(ModifierType, int, Vector2)
        +IModifier Clone()
    }

    IReadableProperty~T~ <|-- IModifiableProperty~T~
    IModifiableProperty~T~ <|.. GameProperty
    IDrityTackable <|.. GameProperty
    GameProperty *-- PropertyDependencyManager
    GameProperty o-- IModifier
    IModifier <|.. FloatModifier
    IModifier <|.. RangeModifier
```

**说明：**
- `GameProperty` 是核心类，实现了 `IModifiableProperty<float>` 和 `IDrityTackable` 接口
- `PropertyDependencyManager` 负责管理属性间的依赖关系，与 `GameProperty` 是组合关系
- `IModifier` 是修饰符接口，`FloatModifier` 和 `RangeModifier` 是具体实现
- 使用泛型接口设计，支持扩展到其他类型（如 `int`、`bool`）

---

### 组合属性类图

展示组合属性的继承层次和结构。

```mermaid
classDiagram
    class ICombineGameProperty {
        <<interface>>
        +string ID
        +GameProperty ResultHolder
        +Func~ICombineGameProperty, float~ Calculater
        +float GetValue()
        +float GetBaseValue()
        +GameProperty GetProperty(string id)
        +bool IsValid()
        +void Dispose()
    }

    class CombineGameProperty {
        <<abstract>>
        +string ID
        +GameProperty ResultHolder
        +Func~ICombineGameProperty, float~ Calculater
        #float _baseCombineValue
        #bool _isDisposed
        #CombineGameProperty(string id, float baseValue)
        +float GetValue()
        +float GetBaseValue()
        +GameProperty GetProperty(string id)* 
        +bool IsValid()
        +void Dispose()
        #float GetCalculatedValue()*
        +event Action~float, float~ OnValueChanged
    }

    class CombinePropertySingle {
        +CombinePropertySingle(string id, float baseValue)
        +GameProperty GetProperty(string id)
        +CombinePropertySingle SetBaseValue(float)
        +CombinePropertySingle AddModifier(IModifier)
        +CombinePropertySingle RemoveModifier(IModifier)
        +CombinePropertySingle ClearModifiers()
        +CombinePropertySingle SubscribeValueChanged(Action)
        #float GetCalculatedValue()
    }

    class CombinePropertyCustom {
        -Dictionary~string, GameProperty~ _gameProperties
        -Dictionary~GameProperty, Action~ _eventHandlers
        +CombinePropertyCustom(string id, float baseValue)
        +GameProperty RegisterProperty(GameProperty, Action)
        +void UnRegisterProperty(GameProperty)
        +GameProperty GetProperty(string id)
        #void DisposeCore()
    }

    class GameProperty {
        +string ID
        +float GetValue()
        +float GetBaseValue()
        +AddModifier(IModifier)
        +RemoveModifier(IModifier)
    }

    ICombineGameProperty <|.. CombineGameProperty
    CombineGameProperty <|-- CombinePropertySingle
    CombineGameProperty <|-- CombinePropertyCustom
    CombineGameProperty *-- GameProperty : ResultHolder
    CombinePropertyCustom o-- GameProperty : Sub Properties
```

**说明：**
- `ICombineGameProperty` 是组合属性接口，定义了统一的访问方式
- `CombineGameProperty` 是抽象基类，提供通用实现
- `CombinePropertySingle` 包装单一 `GameProperty`，适用于简单场景
- `CombinePropertyCustom` 支持多个子属性和自定义计算逻辑，适用于复杂场景
- 所有组合属性都包含 `ResultHolder`（`GameProperty`），用于存储计算结果并应用修饰符

---

### 管理器类图

展示 GamePropertyManager 的结构和管理机制。

```mermaid
classDiagram
    class GamePropertyManager {
        -ConcurrentDictionary~string, ICombineGameProperty~ _properties
        +int Count
        +void AddOrUpdate(ICombineGameProperty)
        +CombinePropertySingle Wrap(GameProperty)
        +IEnumerable~CombinePropertySingle~ WrapRange(IEnumerable~GameProperty~)
        +ICombineGameProperty Get(string id)
        +CombinePropertySingle GetSingle(string id)
        +CombinePropertyCustom GetCustom(string id)
        +GameProperty GetGameProperty(string id, string subId)
        +bool Remove(string id)
        +IEnumerable~ICombineGameProperty~ GetAll()
        +int CleanupInvalidProperties()
        +void Clear()
        +bool IsSingle(string id)
        +bool IsCustom(string id)
    }

    class ICombineGameProperty {
        <<interface>>
        +string ID
        +bool IsValid()
        +void Dispose()
    }

    class CombinePropertySingle {
        +string ID
        +GameProperty ResultHolder
    }

    class CombinePropertyCustom {
        +string ID
        +GameProperty ResultHolder
        +Dictionary~string, GameProperty~ SubProperties
    }

    GamePropertyManager o-- ICombineGameProperty : manages
    ICombineGameProperty <|.. CombinePropertySingle
    ICombineGameProperty <|.. CombinePropertyCustom
```

**说明：**
- `GamePropertyManager` 使用线程安全的 `ConcurrentDictionary` 存储属性
- 支持添加、查询、删除、清理等操作
- 提供 `Wrap()` 方法将普通 `GameProperty` 自动包装为 `CombinePropertySingle`
- 管理器负责属性的生命周期，包括自动释放资源

---

## 数据流程图

### 属性值计算流程

展示 `GetValue()` 方法的计算流程和优化机制。

```mermaid
flowchart TD
    Start([调用 GetValue]) --> CheckRecalc{需要重新计算?}
    
    Note1[检查条件:<br/>1. 有非Clamp的RangeModifier<br/>2. 有随机依赖<br/>3. _isDirty为true<br/>4. 依赖项中有脏数据]
    
    CheckRecalc -->|否| ReturnCache[返回缓存值]
    CheckRecalc -->|是| HasDependencies{有依赖项?}
    HasDependencies -->|是| UpdateDependencies[更新所有依赖属性<br/>调用 UpdateDependencies]
    HasDependencies -->|否| GetBaseValue[获取基础值]
    UpdateDependencies --> GetBaseValue
    GetBaseValue --> ApplyModifiers[按顺序应用修饰符]
    
    ApplyModifiers --> Override{有 Override 修饰符?}
    Override -->|是| ApplyOverride[应用覆盖值]
    Override -->|否| PriorityAdd{有 PriorityAdd?}
    
    ApplyOverride --> PriorityAdd
    
    ApplyModifiers --> Override{有 Override 修饰符?}
    Override -->|是| ApplyOverride[应用覆盖值]
    Override -->|否| PriorityAdd{有 PriorityAdd?}
    
    ApplyOverride --> PriorityAdd
    PriorityAdd -->|是| ApplyPriorityAdd[应用优先级加法]
    PriorityAdd -->|否| Add{有 Add?}
    
    ApplyPriorityAdd --> Add
    Add -->|是| ApplyAdd[应用普通加法]
    Add -->|否| PriorityMul{有 PriorityMul?}
    
    ApplyAdd --> PriorityMul
    PriorityMul -->|是| ApplyPriorityMul[应用优先级乘法]
    PriorityMul -->|否| Mul{有 Mul?}
    
    ApplyPriorityMul --> Mul
    Mul -->|是| ApplyMul[应用普通乘法]
    Mul -->|否| AfterAdd{有 AfterAdd?}
    
    ApplyMul --> AfterAdd
    AfterAdd -->|是| ApplyAfterAdd[应用后加法]
    AfterAdd -->|否| Clamp{有 Clamp?}
    
    ApplyAfterAdd --> Clamp
    Clamp -->|是| ApplyClamp[应用范围限制]
    Clamp -->|否| CacheResult[缓存计算结果]
    
    ApplyClamp --> CacheResult
    CacheResult --> ClearDirtyFlag{无随机性?}
    ClearDirtyFlag -->|是| ClearDirty[清除脏标记]
    ClearDirtyFlag -->|否| KeepDirty[保持脏标记]
    
    ClearDirty --> CheckChange{值是否变化?}
    KeepDirty --> CheckChange
    CheckChange -->|是| TriggerEvent[触发 OnValueChanged 事件]
    CheckChange -->|否| ReturnResult[返回最终值]
    
    TriggerEvent --> UpdateDependents[触发依赖者更新]
    UpdateDependents --> ReturnResult
    
    ReturnCache --> End([结束])
    ReturnResult --> End

    style Start fill:#e1f5ff
    style End fill:#e1f5ff
    style CheckRecalc fill:#fff4e1
    style CheckChange fill:#fff4e1
    style ReturnCache fill:#e8f5e9
    style ReturnResult fill:#e8f5e9
    style Note1 fill:#fffacd
```

**说明：**
- **脏标记机制**：检查三个条件（非Clamp随机修饰符、随机依赖、自身脏）
- **主动传播**：属性变脏时主动传播脏标记到所有依赖者
- **修饰符执行顺序**：Override → PriorityAdd → Add → PriorityMul → Mul → AfterAdd → Clamp
- **事件触发**：仅在值实际变化时触发 `OnValueChanged` 事件
- **依赖传播**：值变化后自动触发所有依赖者更新

### 依赖关系建立与更新流程

展示属性依赖系统的工作机制。

```mermaid
flowchart TD
    Start([调用 AddDependency]) --> CheckNull{dependency 是否为 null?}
    CheckNull -->|是| ThrowException[抛出 ArgumentNullException]
    CheckNull -->|否| CheckSelf{是否自依赖?}
    
    CheckSelf -->|是| LogWarning1[记录警告: 自依赖]
    CheckSelf -->|否| CheckCycle{是否形成循环依赖?}
    
    CheckCycle -->|是| LogWarning2[记录警告: 循环依赖]
    CheckCycle -->|否| AddToSet[添加到依赖集合]
    
    AddToSet --> AddReverse[在 dependency 中添加反向引用]
    AddReverse --> UpdateDepth[更新依赖深度]
    UpdateDepth --> HasCalculator{是否有计算函数?}
    
    HasCalculator -->|是| SaveCalculator[保存计算函数]
    HasCalculator -->|否| MarkDirty[标记为脏数据]
    
    SaveCalculator --> ApplyCalculator[立即应用计算函数]
    ApplyCalculator --> SetBaseValue[设置新基础值]
    SetBaseValue --> Success[依赖添加成功]
    
    MarkDirty --> Success
    LogWarning1 --> Fail[依赖添加失败]
    LogWarning2 --> Fail
    ThrowException --> Fail
    
    Success --> End([结束])
    Fail --> End

    style Start fill:#e1f5ff
    style End fill:#e1f5ff
    style CheckNull fill:#fff4e1
    style CheckSelf fill:#fff4e1
    style CheckCycle fill:#fff4e1
    style Success fill:#e8f5e9
    style Fail fill:#ffebee
```

**依赖更新流程（当 dependency 值变化时）：**

```mermaid
flowchart TD
    Start([dependency 值变化]) --> TriggerEvent[触发 OnValueChanged]
    TriggerEvent --> GetDependents[获取所有依赖者列表]
    GetDependents --> LoopStart{遍历依赖者}
    
    LoopStart -->|有下一个| HasCalculator{有计算函数?}
    LoopStart -->|无| End([结束])
    
    HasCalculator -->|是| CallCalculator[调用计算函数]
    HasCalculator -->|否| MarkDirty[标记依赖者为脏数据]
    
    CallCalculator --> GetNewValue[获取新计算值]
    GetNewValue --> CheckChange{值是否变化?}
    
    CheckChange -->|是| SetBaseValue[设置依赖者基础值]
    CheckChange -->|否| LoopStart
    
    SetBaseValue --> RecalculateDependent[依赖者重新计算]
    MarkDirty --> GetValueDependent[依赖者调用 GetValue]
    
    RecalculateDependent --> LoopStart
    GetValueDependent --> LoopStart

    style Start fill:#e1f5ff
    style End fill:#e1f5ff
    style LoopStart fill:#fff4e1
    style CheckChange fill:#fff4e1
```

**说明：**
- 循环依赖检测：使用深度优先搜索检测循环，最大深度限制为 100 层
- 自动传播：依赖属性值变化时，所有依赖者自动更新
- 计算函数：支持自定义计算逻辑，接收依赖属性和新值，返回依赖者的新基础值
- 反向引用：维护双向关系，方便快速查找依赖者

---
## 序列图

### 创建并修改属性的完整流程

展示从创建属性到修改值的完整交互过程。

```mermaid
sequenceDiagram
    participant User as 用户代码
    participant GP as GameProperty
    participant DM as DependencyManager
    participant Modifier as IModifier

    User->>GP: new GameProperty("health", 100)
    activate GP
    GP->>GP: 初始化 _baseValue = 100
    GP->>GP: 初始化 _cacheValue = 100
    GP->>DM: 创建 DependencyManager
    GP->>GP: MakeDirty()
    GP-->>User: 返回 GameProperty 实例
    deactivate GP

    User->>GP: AddModifier(FloatModifier(Add, 0, 50))
    activate GP
    GP->>GP: 添加修饰符到 Modifiers 列表
    GP->>GP: 按类型分组到 _groupedModifiers
    GP->>GP: MakeDirty()
    GP-->>User: 返回 this
    deactivate GP

    User->>GP: GetValue()
    activate GP
    GP->>GP: 检查脏标记 _isDirty
    GP->>DM: UpdateDependencies()
    activate DM
    DM-->>GP: 依赖已更新
    deactivate DM
    GP->>GP: ApplyModifiers(ref value)
    GP->>Modifier: 应用 Add 类型修饰符
    activate Modifier
    Modifier-->>GP: 返回修改后的值
    deactivate Modifier
    GP->>GP: 缓存结果 _cacheValue = 150
    GP->>GP: 清除脏标记 _isDirty = false
    GP->>User: 触发 OnValueChanged(100, 150)
    GP-->>User: 返回 150
    deactivate GP

    User->>GP: SetBaseValue(120)
    activate GP
    GP->>GP: 比较新旧值
    GP->>GP: 更新 _baseValue = 120
    GP->>GP: MakeDirty()
    GP->>GP: GetValue() 立即计算
    GP->>GP: ApplyModifiers(ref value)
    GP->>GP: _cacheValue = 170
    GP->>User: 触发 OnValueChanged(150, 170)
    GP->>DM: TriggerDependentUpdates(170)
    activate DM
    DM->>DM: 更新所有依赖者
    DM-->>GP: 更新完成
    deactivate DM
    GP-->>User: 返回 this
    deactivate GP
```

**说明：**
- 构造函数会初始化基础值、缓存值并标记为脏数据
- 添加修饰符会立即标记为脏，但不立即计算
- `GetValue()` 检查脏标记，仅在必要时重新计算
- `SetBaseValue()` 会立即触发计算并传播到依赖者

---

### 事件系统交互流程

展示三种事件（OnBaseValueChanged、OnValueChanged、OnDirtyAndValueChanged）的触发时机和交互过程。

```mermaid
sequenceDiagram
    participant User as 用户代码
    participant GP as GameProperty
    participant UI as UI监听器
    participant Save as 存档监听器
    participant Battle as 战斗监听器

    Note over User,Battle: 场景1：SetBaseValue触发所有相关事件

    User->>GP: 注册事件监听
    User->>GP: OnBaseValueChanged += SaveHandler
    User->>GP: OnValueChanged += BattleHandler
    User->>GP: OnDirtyAndValueChanged(UIHandler)

    User->>GP: SetBaseValue(150)
    activate GP
    GP->>GP: 检查值变化
    GP->>Save: 触发 OnBaseValueChanged(100, 150)
    activate Save
    Save-->>GP: 保存基础值到文件
    deactivate Save
    
    GP->>GP: MakeDirty()
    GP->>UI: 触发 OnDirty 回调
    activate UI
    UI->>GP: GetValue() (自动调用)
    GP-->>UI: 返回 150
    UI->>UI: 检查值变化 (100 vs 150)
    UI-->>UI: UpdateUI(150)
    deactivate UI
    
    GP->>GP: GetValue() (SetBaseValue内部调用)
    GP->>GP: 计算最终值
    GP->>Battle: 触发 OnValueChanged(100, 150)
    activate Battle
    Battle-->>GP: 更新战斗显示
    deactivate Battle
    deactivate GP

    Note over User,Battle: 场景2：AddModifier仅触发UI和战斗事件

    User->>GP: AddModifier(FloatModifier(Add, 0, 50))
    activate GP
    GP->>GP: MakeDirty()
    GP->>UI: 触发 OnDirty 回调
    activate UI
    UI->>GP: GetValue() (自动调用)
    GP->>GP: 计算最终值 = 200
    GP-->>UI: 返回 200
    UI->>UI: 检查值变化 (150 vs 200)
    UI-->>UI: UpdateUI(200)
    UI->>Battle: 触发 OnValueChanged(150, 200)
    activate Battle
    Battle-->>Battle: 更新战斗显示
    deactivate Battle
    deactivate UI
    
    Note right of Save: OnBaseValueChanged 不会触发<br/>因为基础值未变
    deactivate GP

    Note over User,Battle: 场景3：批量操作 + NotifyIfChanged优化

    User->>GP: AddModifier(mod1)
    activate GP
    GP->>GP: MakeDirty()
    Note right of GP: 不立即计算
    deactivate GP

    User->>GP: AddModifier(mod2)
    activate GP
    GP->>GP: MakeDirty()
    Note right of GP: 不立即计算
    deactivate GP

    User->>GP: NotifyIfChanged()
    activate GP
    GP->>GP: GetValue() (手动触发)
    GP->>GP: 计算最终值
    GP->>Battle: 触发 OnValueChanged (仅一次)
    activate Battle
    Battle-->>GP: 更新战斗显示
    deactivate Battle
    deactivate GP
    
    Note over User,Battle: 性能优势：2次AddModifier只计算1次
```

**说明：**

**事件触发规则：**
1. **OnBaseValueChanged**：仅 `SetBaseValue()` 触发
2. **OnValueChanged**：`GetValue()` 计算后值变化时触发
3. **OnDirtyAndValueChanged**：脏标记时自动调用 `GetValue()` 并在值变化时触发回调

**性能对比：**
- **场景1**（SetBaseValue）：3个事件触发，1次计算
- **场景2**（AddModifier + OnDirtyAndValueChanged）：2个事件触发，1次计算
- **场景3**（批量 + NotifyIfChanged）：1个事件触发，1次计算（最优）

**使用建议：**
- 存档系统：监听 `OnBaseValueChanged`
- 战斗/计算：监听 `OnValueChanged`（延迟计算）
- UI 更新：使用 `OnDirtyAndValueChanged`（立即响应）
- 批量操作：使用 `NotifyIfChanged()` 手动触发

---

### 依赖关系建立与传播流程

展示属性间依赖关系的建立和值变化传播过程。

```mermaid
sequenceDiagram
    participant User as 用户代码
    participant PropA as GameProperty A
    participant PropB as GameProperty B (依赖者)
    participant DM_A as A.DependencyManager
    participant DM_B as B.DependencyManager

    User->>PropA: new GameProperty("strength", 15)
    activate PropA
    PropA-->>User: 返回 PropA
    deactivate PropA

    User->>PropB: new GameProperty("attack", 0)
    activate PropB
    PropB-->>User: 返回 PropB
    deactivate PropB

    User->>PropB: AddDependency(PropA, calculator)
    activate PropB
    PropB->>DM_B: AddDependency(PropA, calculator)
    activate DM_B
    DM_B->>DM_B: 检查循环依赖
    DM_B->>DM_B: 添加到 _dependencies
    DM_B->>DM_A: 添加 PropB 到 _dependents
    activate DM_A
    DM_A-->>DM_B: 添加成功
    deactivate DM_A
    DM_B->>DM_B: 保存 calculator
    DM_B->>PropA: GetValue()
    activate PropA
    PropA-->>DM_B: 返回 15
    deactivate PropA
    DM_B->>DM_B: calculator(PropA, 15) = 40
    DM_B->>PropB: SetBaseValue(40)
    PropB->>PropB: _baseValue = 40
    PropB-->>User: 返回 this
    deactivate DM_B
    deactivate PropB

    Note over User: PropA 值变化时自动更新 PropB
    User->>PropA: SetBaseValue(20)
    activate PropA
    PropA->>PropA: _baseValue = 20
    PropA->>PropA: GetValue() 立即计算
    PropA->>PropA: OnValueChanged(15, 20)
    PropA->>DM_A: TriggerDependentUpdates(20)
    activate DM_A
    DM_A->>DM_A: 遍历 _dependents (PropB)
    DM_A->>DM_B: 获取 calculator
    activate DM_B
    DM_B-->>DM_A: calculator(PropA, 20) = 50
    deactivate DM_B
    DM_A->>PropB: SetBaseValue(50)
    activate PropB
    PropB->>PropB: _baseValue = 50
    PropB->>PropB: GetValue() 立即计算
    PropB->>PropB: OnValueChanged(40, 50)
    PropB-->>DM_A: 更新完成
    deactivate PropB
    DM_A-->>PropA: 所有依赖者已更新
    deactivate DM_A
    PropA-->>User: 返回 this
    deactivate PropA
```

**说明：**
- 依赖建立时会立即计算并设置依赖者的基础值
- 双向引用：依赖者记录依赖项，依赖项记录依赖者
- 自动传播：值变化时自动触发所有依赖者的计算函数
- 链式更新：依赖者的值变化会继续传播到其依赖者（形成依赖链）

---

## 状态图

### GameProperty 状态转换

展示 GameProperty 的内部状态和转换条件。

```mermaid
stateDiagram-v2
    [*] --> Clean: 创建属性 / 初始化为脏数据
    
    Clean --> Dirty: 添加/移除修饰符
    Clean --> Dirty: 设置基础值
    Clean --> Dirty: 依赖项变化通知
    
    Dirty --> Calculating: 调用 GetValue()
    
    Calculating --> Clean: 无随机性修饰符 / 缓存结果
    Calculating --> Dirty: 有随机性修饰符 / 每次重新计算
    
    Dirty --> Dirty: 添加更多修饰符
    
    Clean --> [*]: 对象销毁
    Dirty --> [*]: 对象销毁

    note right of Clean
        脏标记: false
        可直接返回缓存值
    end note

    note right of Dirty
        脏标记: true
        需要重新计算
    end note

    note right of Calculating
        正在执行:
        1. 更新依赖
        2. 应用修饰符
        3. 缓存结果
        4. 触发事件
    end note
```

**状态说明：**
- **Clean（干净）**：缓存值有效，可直接返回，无需重新计算
- **Dirty（脏数据）**：缓存值过期，需要重新计算
- **Calculating（计算中）**：正在执行值计算流程

**转换条件：**
- `Clean → Dirty`：修改基础值、添加/移除修饰符、依赖项通知
- `Dirty → Calculating`：调用 `GetValue()`
- `Calculating → Clean`：计算完成且无随机性修饰符
- `Calculating → Dirty`：计算完成但存在随机性修饰符（如非 Clamp 的 RangeModifier）

---

### CombineGameProperty 生命周期状态

展示组合属性的生命周期状态。

```mermaid
stateDiagram-v2
    [*] --> Created: 构造函数
    
    Created --> Active: 添加到管理器
    Created --> Active: 注册子属性 (Custom)
    Created --> Active: 添加修饰符 (Single)
    
    Active --> Active: 修改值
    Active --> Active: 添加/移除修饰符
    Active --> Active: 注册/注销子属性
    
    Active --> Disposing: 调用 Dispose()
    Active --> Disposing: 管理器移除
    
    Disposing --> Disposed: 清理资源完成
    
    Disposed --> [*]: 对象销毁

    note right of Created
        IsValid: true
        _isDisposed: false
    end note

    note right of Active
        IsValid: true
        可正常访问所有方法
    end note

    note right of Disposing
        IsValid: false
        正在清理:
        - 清除事件订阅
        - 释放子属性
        - 清空修饰符
    end note

    note right of Disposed
        IsValid: false
        _isDisposed: true
        访问会抛出 ObjectDisposedException
    end note
```

**状态说明：**
- **Created（已创建）**：对象刚创建，尚未使用
- **Active（活跃）**：正常使用中，可读写操作
- **Disposing（释放中）**：正在清理资源
- **Disposed（已释放）**：资源已释放，不可再使用

**最佳实践：**
- 使用前检查 `IsValid()` 确保对象未释放
- 不再使用时调用 `Dispose()` 释放资源
- 管理器的 `Remove()` 会自动调用 `Dispose()`

---

## 性能优化相关

### 脏标记优化机制

```mermaid
flowchart LR
    A[修改属性] --> D[标记为脏<br/>_isDirty = true]
    D --> E[主动传播脏标记<br/>到所有依赖者]
    E --> F[触发 OnDirty 回调]
    
    G[读取属性] --> H{需要重新计算?}
    H -->|否| I[返回缓存值]
    H -->|是| J[重新计算<br/>应用修饰符]
    J --> K{有随机修饰符?}
    K -->|否| L[缓存结果<br/>清除脏标记]
    K -->|是| M[缓存结果<br/>保持脏标记]
    L --> N[返回值]
    M --> N
    
    Note1[重新计算条件:<br/>1. 有非Clamp随机修饰符<br/>2. 有随机依赖<br/>3. _isDirty为true]

    style I fill:#e8f5e9
    style N fill:#e8f5e9
    style Note1 fill:#fffacd
```

**性能特性：**
- **主动传播**：MakeDirty时主动传播脏标记到依赖者，避免GetValue时递归检查
- **缓存失效**：脏标记传播时同时失效HasDirtyDependencies缓存
- **早期返回**：已脏属性调用MakeDirty立即返回，避免重复传播
- **批量操作**：批量添加修饰符使用AddModifiers而非多次AddModifier
- **随机修饰符**：非Clamp的RangeModifier会导致每次重新计算
- **线程安全**：GamePropertyManager使用ConcurrentDictionary支持并发访问

---

## 相关资源

- [用户使用指南](./UserGuide.md) - 快速开始和常见场景
- [API 参考文档](./APIReference.md) - 详细的方法签名和参数说明

---

**维护者：** EasyPack 团队  
**联系方式：** 提交 GitHub Issue 或 Pull Request  
**许可证：** 遵循项目主许可证
