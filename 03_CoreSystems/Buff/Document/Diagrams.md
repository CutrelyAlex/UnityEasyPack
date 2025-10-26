# Buff System - Diagrams

**适用EasyPack版本：** EasyPack v1.5.30
**最后更新：** 2025-10-26

---

本文档提供 Buff System 的可视化架构图和流程图，帮助理解系统设计和数据流。

---

## 目录

- [概述](#概述)
- [图表索引](#图表索引)
- [1. 类结构图](#1-类结构图)
- [2. BuffManager 更新流程图](#2-buffmanager-更新流程图)
- [3. Buff 生命周期序列图](#3-buff-生命周期序列图)
- [4. Buff 堆叠状态图](#4-buff-堆叠状态图)
- [补充图表](#补充图表)
- [数据流图](#数据流图)
- [图表使用指南](#图表使用指南)
- [相关资源](#相关资源)

---

## 图表索引

1. [类结构图](#1-类结构图) - 核心类及其关系
2. [BuffManager 更新流程图](#2-buffmanager-更新流程图) - Update 方法的执行流程
3. [Buff 生命周期序列图](#3-buff-生命周期序列图) - 创建到移除的完整流程
4. [Buff 堆叠状态图](#4-buff-堆叠状态图) - 堆叠策略状态转换

---

## 1. 类结构图

展示 Buff System 的核心类及其关系。

```mermaid
classDiagram
    class Buff {
        -BuffData buffData
        -GameObject creator
        -GameObject target
        -int currentStacks
        -float durationTimer
        -float triggerTimer
        +OnCreate: Action~Buff~
        +OnTrigger: Action~Buff~
        +OnUpdate: Action~Buff~
        +OnAddStack: Action~Buff~
        +OnReduceStack: Action~Buff~
        +OnRemove: Action~Buff~
        +AddStack(int amount)
        +ReduceStack(int amount)
    }

    class BuffData {
        +string ID
        +string Name
        +string Description
        +Sprite Sprite
        +object CustomData
        +int MaxStacks
        +float Duration
        +float TriggerInterval
        +bool TriggerOnCreate
        +BuffSuperpositionDurationType BuffSuperpositionStrategy
        +BuffSuperpositionStacksType BuffSuperpositionStacksStrategy
        +BuffRemoveType BuffRemoveStrategy
        +List~string~ Tags
        +List~string~ Layers
        +List~BuffModule~ BuffModules
    }

    class BuffManager {
        -Dictionary~GameObject, List~Buff~~ _targetToBuffs
        -Dictionary~string, List~Buff~~ _buffsByID
        -Dictionary~string, List~Buff~~ _buffsByTag
        -Dictionary~string, List~Buff~~ _buffsByLayer
        -List~Buff~ _pendingRemovals
        +CreateBuff(BuffData, GameObject, GameObject): Buff
        +RemoveBuff(Buff)
        +RemoveBuffByID(GameObject, string)
        +RemoveAllBuffs(GameObject)
        +RemoveBuffsByTag(GameObject, string)
        +RemoveBuffsByLayer(GameObject, string)
        +RemoveAllBuffsByTag(string)
        +RemoveAllBuffsByLayer(string)
        +FlushPendingRemovals()
        +ContainsBuff(GameObject, string): bool
        +GetBuff(GameObject, string): Buff
        +GetTargetBuffs(GameObject): List~Buff~
        +GetBuffsByTag(GameObject, string): List~Buff~
        +GetBuffsByLayer(GameObject, string): List~Buff~
        +Update(float)
    }

    class BuffModule {
        <<abstract>>
        +int Priority
        +Func~Buff, bool~ TriggerCondition
        #RegisterCallback(BuffCallBackType, Action)
        #RegisterCallback(string, Action)
        +Execute(Buff, BuffCallBackType, string, params)
    }

    class CastModifierToProperty {
        -IModifier _modifier
        -string _propertyID
        -GamePropertyManager _propertyManager
        +CastModifierToProperty(IModifier, string, GamePropertyManager)
        -OnCreate(Buff, object[])
        -OnRemove(Buff, object[])
        -OnAddStack(Buff, object[])
        -OnReduceStack(Buff, object[])
    }

    class GamePropertyManager {
        <<external>>
        +AddModifier(string, IModifier)
        +RemoveModifier(string, IModifier)
    }

    class IModifier {
        <<interface>>
        +float Modify(float value)
    }

    %% 关系
    Buff "1" --> "1" BuffData : uses
    Buff "1" --> "1" GameObject : creator
    Buff "1" --> "1" GameObject : target
    BuffManager "1" --> "*" Buff : manages
    BuffData "1" --> "*" BuffModule : contains
    BuffModule <|-- CastModifierToProperty : inherits
    CastModifierToProperty --> GamePropertyManager : uses
    CastModifierToProperty --> IModifier : uses

    %% 索引关系
    BuffManager --> Buff : _targetToBuffs
    BuffManager --> Buff : _buffsByID
    BuffManager --> Buff : _buffsByTag
    BuffManager --> Buff : _buffsByLayer
```

**关键点：**
- `Buff` 持有 `BuffData` 配置和生命周期状态
- `BuffManager` 使用多个字典索引 Buff，支持快速查询
- `BuffModule` 是抽象基类，可扩展实现各种效果
- `CastModifierToProperty` 桥接 Buff 系统和属性系统

---

## 2. BuffManager 更新流程图

展示 `BuffManager.Update()` 方法的执行流程。

```mermaid
flowchart TD
    Start([开始 Update deltaTime]) --> ClearPending[清空待移除列表]
    ClearPending --> IterateTargets{遍历所有目标}
    
    IterateTargets -->|有目标| IterateBuffs{遍历目标的 Buff}
    IterateTargets -->|无目标| FlushRemovals[执行批量移除]
    
    IterateBuffs -->|有 Buff| CheckPermanent{是否永久 Buff?}
    IterateBuffs -->|无 Buff| NextTarget[下一个目标]
    
    CheckPermanent -->|是| UpdatePermanent[更新永久 Buff]
    CheckPermanent -->|否| UpdateTimer[更新持续时间计时器]
    
    UpdatePermanent --> CheckTrigger1{是否有触发间隔?}
    UpdateTimer --> CheckExpired{是否已过期?}
    
    CheckExpired -->|是| MarkRemove[标记为待移除]
    CheckExpired -->|否| CheckTrigger2{是否有触发间隔?}
    
    MarkRemove --> NextBuff[下一个 Buff]
    
    CheckTrigger1 -->|是| UpdateTrigger1[更新触发计时器]
    CheckTrigger1 -->|否| ExecuteUpdate1[执行 OnUpdate 事件]
    
    CheckTrigger2 -->|是| UpdateTrigger2[更新触发计时器]
    CheckTrigger2 -->|否| ExecuteUpdate2[执行 OnUpdate 事件]
    
    UpdateTrigger1 --> CheckTriggerFire1{触发计时器到期?}
    UpdateTrigger2 --> CheckTriggerFire2{触发计时器到期?}
    
    CheckTriggerFire1 -->|是| ResetTrigger1[重置触发计时器]
    CheckTriggerFire1 -->|否| ExecuteUpdate1
    
    CheckTriggerFire2 -->|是| ResetTrigger2[重置触发计时器]
    CheckTriggerFire2 -->|否| ExecuteUpdate2
    
    ResetTrigger1 --> ExecuteTrigger1[执行 OnTrigger 事件]
    ResetTrigger2 --> ExecuteTrigger2[执行 OnTrigger 事件]
    
    ExecuteTrigger1 --> ExecuteModules1[执行模块 OnTick 回调]
    ExecuteTrigger2 --> ExecuteModules2[执行模块 OnTick 回调]
    
    ExecuteModules1 --> ExecuteUpdate1
    ExecuteModules2 --> ExecuteUpdate2
    
    ExecuteUpdate1 --> ExecuteUpdateModules1[执行模块 OnUpdate 回调]
    ExecuteUpdate2 --> ExecuteUpdateModules2[执行模块 OnUpdate 回调]
    
    ExecuteUpdateModules1 --> NextBuff
    ExecuteUpdateModules2 --> NextBuff
    
    NextBuff --> IterateBuffs
    NextTarget --> IterateTargets
    
    FlushRemovals --> RemoveLoop{遍历待移除 Buff}
    RemoveLoop -->|有 Buff| TriggerRemove[触发 OnRemove 事件]
    RemoveLoop -->|无 Buff| End([结束])
    
    TriggerRemove --> ExecuteRemoveModules[执行模块 OnRemove 回调]
    ExecuteRemoveModules --> RemoveFromIndexes[从所有索引中移除]
    RemoveFromIndexes --> RemoveLoop

    style Start fill:#e1f5e1
    style End fill:#ffe1e1
    style MarkRemove fill:#fff3cd
    style TriggerRemove fill:#fff3cd
    style ExecuteTrigger1 fill:#d1ecf1
    style ExecuteTrigger2 fill:#d1ecf1
```

**关键点：**
1. **双阶段处理**：先标记待移除，最后批量移除
2. **永久 Buff**：跳过持续时间检查，仅处理触发逻辑
3. **触发优先级**：先处理 `OnTrigger`，再处理 `OnUpdate`
4. **模块执行**：按优先级顺序执行所有模块的回调
5. **Swap-Remove 优化**：移除时使用交换删除算法（O(1) 复杂度）

---

## 3. Buff 生命周期序列图

展示从创建到移除的完整生命周期。

```mermaid
sequenceDiagram
    participant Client
    participant BuffManager
    participant Buff
    participant BuffData
    participant BuffModule
    participant GamePropertyManager

    Note over Client,GamePropertyManager: 场景 1: 创建新 Buff

    Client->>BuffManager: CreateBuff(buffData, creator, target)
    BuffManager->>BuffManager: 检查目标是否已有该 Buff
    
    alt Buff 不存在
        BuffManager->>Buff: new Buff(buffData, creator, target)
        Buff->>BuffData: 获取配置数据
        BuffData-->>Buff: Duration, MaxStacks, Modules...
        Buff->>Buff: 初始化计时器和堆叠数
        BuffManager->>BuffManager: 添加到所有索引
        BuffManager->>BuffModule: 按 Priority 排序模块
        BuffManager->>BuffModule: Execute(OnCreate)
        
        loop 每个模块
            BuffModule->>BuffModule: 检查 TriggerCondition
            BuffModule->>GamePropertyManager: AddModifier(propertyID, modifier)
            GamePropertyManager-->>BuffModule: 修饰符已添加
        end
        
        BuffManager->>Buff: 触发 OnCreate 事件
        Buff-->>Client: OnCreate(buff)
        
        alt TriggerOnCreate == true
            BuffManager->>Buff: 触发 OnTrigger 事件
            Buff-->>Client: OnTrigger(buff)
        end
        
    else Buff 已存在
        BuffManager->>Buff: 应用叠加策略
        
        alt BuffSuperpositionStrategy == Reset
            Buff->>Buff: DurationTimer = Duration
        else BuffSuperpositionStrategy == Add
            Buff->>Buff: DurationTimer += Duration
        else BuffSuperpositionStrategy == ResetThenAdd
            Buff->>Buff: DurationTimer = Duration * 2
        else BuffSuperpositionStrategy == Keep
            Buff->>Buff: DurationTimer 保持不变
        end
        
        alt BuffSuperpositionStacksStrategy == Add
            Buff->>Buff: AddStack()
            BuffManager->>BuffModule: Execute(OnAddStack)
            
            loop 每个模块
                BuffModule->>GamePropertyManager: AddModifier(propertyID, modifier)
            end
            
            BuffManager->>Buff: 触发 OnAddStack 事件
            Buff-->>Client: OnAddStack(buff)
        end
    end

    Note over Client,GamePropertyManager: 场景 2: Update 循环

    loop 每帧
        Client->>BuffManager: Update(deltaTime)
        BuffManager->>Buff: 更新 DurationTimer
        Buff->>Buff: DurationTimer -= deltaTime
        
        alt DurationTimer <= 0 && !IsPermanent
            BuffManager->>BuffManager: 标记为待移除
        end
        
        alt TriggerInterval > 0
            BuffManager->>Buff: 更新 TriggerTimer
            Buff->>Buff: TriggerTimer -= deltaTime
            
            alt TriggerTimer <= 0
                Buff->>Buff: TriggerTimer = TriggerInterval
                BuffManager->>Buff: 触发 OnTrigger 事件
                Buff-->>Client: OnTrigger(buff)
                BuffManager->>BuffModule: Execute(OnTick)
            end
        end
        
        BuffManager->>Buff: 触发 OnUpdate 事件
        Buff-->>Client: OnUpdate(buff)
        BuffManager->>BuffModule: Execute(OnUpdate)
    end

    Note over Client,GamePropertyManager: 场景 3: 移除 Buff

    Client->>BuffManager: RemoveBuff(buff)
    BuffManager->>BuffManager: 添加到 _pendingRemovals
    BuffManager->>BuffManager: FlushPendingRemovals()
    
    loop 每个待移除 Buff
        BuffManager->>Buff: 触发 OnRemove 事件
        Buff-->>Client: OnRemove(buff)
        BuffManager->>BuffModule: Execute(OnRemove)
        
        loop 每个模块
            BuffModule->>GamePropertyManager: RemoveModifier(propertyID, modifier)
            GamePropertyManager-->>BuffModule: 修饰符已移除
        end
        
        BuffManager->>BuffManager: 从所有索引中移除
        BuffManager->>Buff: 销毁 Buff 对象
    end
```

**关键流程：**
1. **创建阶段**：初始化 → 添加索引 → 执行模块 `OnCreate` → 触发事件
2. **叠加处理**：检查已存在 → 应用叠加策略 → 更新堆叠/时间 → 执行模块回调
3. **更新循环**：更新计时器 → 检查过期 → 触发周期事件 → 执行模块回调
4. **移除阶段**：触发 `OnRemove` → 执行模块清理 → 移除索引 → 销毁对象

---

## 4. Buff 堆叠状态图

展示不同堆叠策略下的状态转换。

```mermaid
stateDiagram-v2
    [*] --> NotExist: 初始状态

    NotExist --> Stack1: CreateBuff()
    
    state Stack1 {
        [*] --> Active1
        Active1 --> Active1: Update(OnUpdate)
        Active1 --> Triggering1: TriggerTimer 到期
        Triggering1 --> Active1: OnTrigger 执行完毕
        Active1 --> Expired: DurationTimer <= 0
        Expired --> [*]: OnRemove
    }

    Stack1 --> Stack2: CreateBuff() + Add 策略
    Stack1 --> Stack1: CreateBuff() + Keep 策略
    
    state Stack2 {
        [*] --> Active2
        Active2 --> Active2: Update(OnUpdate)
        Active2 --> Triggering2: TriggerTimer 到期
        Triggering2 --> Active2: OnTrigger 执行完毕
        Active2 --> Expired2: DurationTimer <= 0
        Expired2 --> [*]: OnRemove
    }

    Stack2 --> Stack3: CreateBuff() + Add 策略
    Stack2 --> Stack1: ReduceStack()
    
    state Stack3 {
        [*] --> Active3
        Active3 --> Active3: Update(OnUpdate)
        Active3 --> Triggering3: TriggerTimer 到期
        Triggering3 --> Active3: OnTrigger 执行完毕
        Active3 --> Expired3: DurationTimer <= 0
        Active3 --> MaxStack: CurrentStacks == MaxStacks
        MaxStack --> Active3: 无法继续堆叠
        Expired3 --> [*]: OnRemove
    }

    Stack3 --> StackN: CreateBuff() (最多到 MaxStacks)
    Stack3 --> Stack2: ReduceStack()
    
    state StackN {
        [*] --> ActiveN
        ActiveN --> ActiveN: Update(OnUpdate)
        ActiveN --> TriggeringN: TriggerTimer 到期
        TriggeringN --> ActiveN: OnTrigger 执行完毕
        ActiveN --> ExpiredN: DurationTimer <= 0
        ExpiredN --> [*]: OnRemove
    }

    StackN --> StackN: CreateBuff() (已达 MaxStacks)
    StackN --> Stack3: ReduceStack()

    Stack1 --> NotExist: RemoveBuff() (RemoveAll 策略)
    Stack2 --> NotExist: RemoveBuff() (RemoveAll 策略)
    Stack3 --> NotExist: RemoveBuff() (RemoveAll 策略)
    StackN --> NotExist: RemoveBuff() (RemoveAll 策略)

    Stack2 --> Stack1: RemoveBuff() (ReduceStack 策略)
    Stack3 --> Stack2: RemoveBuff() (ReduceStack 策略)
    StackN --> Stack3: RemoveBuff() (ReduceStack 策略)

    note right of Stack1
        堆叠数: 1
        事件: OnCreate
        模块: OnCreate 回调
    end note

    note right of Stack2
        堆叠数: 2
        事件: OnAddStack
        模块: OnAddStack 回调
    end note

    note right of Stack3
        堆叠数: 3
        事件: OnAddStack
        模块: OnAddStack 回调
    end note

    note right of StackN
        堆叠数: MaxStacks
        事件: 无（已达上限）
        模块: 不执行
    end note
```

**状态说明：**

| 状态 | 触发条件 | 执行操作 |
|------|---------|---------|
| **NotExist** | 初始状态 | 无 Buff 实例 |
| **Stack1** | `CreateBuff()` 首次调用 | 触发 `OnCreate`，执行模块 `OnCreate` |
| **Stack2** | `CreateBuff()` + `Add` 策略 | 触发 `OnAddStack`，执行模块 `OnAddStack` |
| **Stack3...N** | 继续 `CreateBuff()` | 继续堆叠直到 `MaxStacks` |
| **MaxStack** | `CurrentStacks == MaxStacks` | 无法继续堆叠，忽略后续 `CreateBuff()` |
| **Active** | 正常运行 | 每帧执行 `OnUpdate` |
| **Triggering** | `TriggerTimer <= 0` | 执行 `OnTrigger` 和模块 `OnTick` |
| **Expired** | `DurationTimer <= 0` | 触发 `OnRemove`，移除 Buff |

**移除策略：**

- **RemoveAll**：直接从任意堆叠状态转换到 `NotExist`
- **ReduceStack**：每次调用减少一层堆叠，直到堆叠为 0 时转换到 `NotExist`

---

## 补充图表

### 5. 索引查询性能对比

不同查询方式的时间复杂度对比：

```mermaid
graph LR
    A[查询方式] --> B[通过 ID 查询]
    A --> C[通过 Tag 查询]
    A --> D[通过 Layer 查询]
    A --> E[遍历所有 Buff]

    B --> B1[O1 - 字典查询]
    C --> C1[O1 - 字典查询]
    D --> D1[O1 - 字典查询]
    E --> E1[On - 线性遍历]

    style B1 fill:#d1f2eb
    style C1 fill:#d1f2eb
    style D1 fill:#d1f2eb
    style E1 fill:#f8d7da

    B1 -.推荐.-> B
    C1 -.推荐.-> C
    D1 -.推荐.-> D
    E1 -.避免.-> E
```

**性能建议：**
- ✅ 使用索引查询（ID、Tag、Layer）：O(1) 时间复杂度
- ❌ 避免遍历查询：O(n) 时间复杂度，大量 Buff 时性能差

---

### 6. 模块执行优先级示例

展示多个模块的执行顺序：

```mermaid
gantt
    title Buff 模块执行顺序（按 Priority 降序）
    dateFormat X
    axisFormat %s

    section Priority 100
    属性修改模块 (Add Modifier)    :done, 0, 1
    
    section Priority 90
    属性修改模块 (Mul Modifier)    :done, 1, 2
    
    section Priority 50
    治疗光环模块 (Healing)         :done, 2, 3
    
    section Priority 30
    伤害模块 (DoT)                 :done, 3, 4
    
    section Priority 10
    视觉效果模块 (VFX)             :done, 4, 5
    
    section Priority 0
    调试日志模块 (Debug)           :done, 5, 6
```

**优先级设置建议：**
- **100+**：属性修改（Add）
- **90+**：属性修改（Mul）
- **50-89**：游戏逻辑（治疗、伤害、状态检查）
- **10-49**：视觉效果、音效
- **0-9**：调试、日志

---

## 数据流图

### 7. Buff 数据流向

```mermaid
flowchart LR
    BuffData[BuffData 配置] -->|复用| Buff1[Buff 实例 1]
    BuffData -->|复用| Buff2[Buff 实例 2]
    BuffData -->|复用| Buff3[Buff 实例 3]

    Buff1 --> Target1[GameObject Target 1]
    Buff2 --> Target1
    Buff3 --> Target2[GameObject Target 2]

    BuffData -->|包含| Modules[BuffModule 列表]
    Modules --> Module1[CastModifierToProperty]
    Modules --> Module2[HealingAuraModule]
    Modules --> Module3[DamageOverTimeModule]

    Module1 -->|修改| Property1[GameProperty: Strength]
    Module2 -->|治疗| Property2[GameProperty: Health]
    Module3 -->|伤害| Property2

    BuffManager[BuffManager] -->|管理| Buff1
    BuffManager -->|管理| Buff2
    BuffManager -->|管理| Buff3

    BuffManager -->|索引| Index1[_targetToBuffs]
    BuffManager -->|索引| Index2[_buffsByID]
    BuffManager -->|索引| Index3[_buffsByTag]
    BuffManager -->|索引| Index4[_buffsByLayer]

    Index1 -.快速查询.-> Target1
    Index1 -.快速查询.-> Target2
    Index2 -.快速查询.-> Buff1
    Index3 -.快速查询.-> Buff2
    Index4 -.快速查询.-> Buff3

    style BuffData fill:#e1f5e1
    style BuffManager fill:#d1ecf1
    style Property1 fill:#fff3cd
    style Property2 fill:#fff3cd
```

**数据流说明：**
1. **配置复用**：一个 `BuffData` 可被多个 `Buff` 实例复用
2. **模块共享**：`BuffData.BuffModules` 被所有实例共享
3. **多重索引**：`BuffManager` 维护多个字典加速查询
4. **属性修改**：`BuffModule` 通过 `GamePropertyManager` 修改属性

---

## 图表使用指南

### 如何阅读这些图表

1. **类结构图**：理解系统的静态架构和类之间的关系
2. **流程图**：理解 `Update` 方法的执行流程和性能优化点
3. **序列图**：理解 Buff 的完整生命周期和事件触发顺序
4. **状态图**：理解堆叠策略的状态转换和边界条件

---

## 相关资源

- [用户指南](./UserGuide.md) - 任务导向的使用指南
- [API 参考](./APIReference.md) - 详细的方法签名和参数说明
- [示例代码](../Example/BuffExample.cs) - 完整的使用示例

---

**维护者：** EasyPack 团队  
**许可证：** 遵循项目主许可证
