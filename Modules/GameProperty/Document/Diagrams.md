# GameProperty 系统 Mermaid 图集文档

**适用 EasyPack 版本：** EasyPack v1.7.0  
**最后更新：** 2025-11-04

---

## 目录

- [GameProperty 系统 Mermaid 图集文档](#gameproperty-系统-mermaid-图集文档)
  - [目录](#目录)
  - [核心类图](#核心类图)
  - [修饰符应用流程图](#修饰符应用流程图)
  - [依赖系统序列图](#依赖系统序列图)
  - [属性状态机图](#属性状态机图)
  - [管理器交互时序图](#管理器交互时序图)
  - [序列化流程图](#序列化流程图)

---

## 核心类图

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
        +IModifiableProperty~T~ AddModifier(IModifier)
        +IModifiableProperty~T~ RemoveModifier(IModifier)
        +IModifiableProperty~T~ ClearModifiers()
    }
    
    class IDirtyTrackable {
        <<interface>>
        +void MakeDirty()
        +void OnDirty(Action)
    }
    
    class GameProperty {
        -string _id
        -float _baseValue
        -float _cacheValue
        -bool _isDirty
        -List~IModifier~ _modifiers
        -PropertyDependencyManager _dependencyManager
        +string ID
        +List~IModifier~ Modifiers
        +GameProperty(string id, float baseValue)
        +float GetValue()
        +float GetBaseValue()
        +void SetBaseValue(float value)
        +void AddModifier(IModifier modifier)
        +bool RemoveModifier(IModifier modifier)
        +void ClearModifiers()
        +bool AddDependency(GameProperty dep, Func calculator)
        +bool RemoveDependency(GameProperty dep)
        +void MakeDirty()
        +void OnDirty(Action callback)
        +void OnDirtyAndValueChanged(Action callback)
        +void NotifyIfChanged()
        +event Action~float,float~ OnValueChanged
        +event Action~float,float~ OnBaseValueChanged
    }
    
    class PropertyDependencyManager {
        -GameProperty _owner
        -HashSet~GameProperty~ _dependencies
        -HashSet~GameProperty~ _dependents
        -Dictionary~GameProperty,Func~ _calculators
        -int _dependencyDepth
        -bool _hasRandomDependency
        +int DependencyDepth
        +bool HasRandomDependency
        +int DependencyCount
        +int DependentCount
        +bool AddDependency(GameProperty, Func)
        +bool RemoveDependency(GameProperty)
        +void TriggerDependentUpdates(float)
        +void PropagateDirtyTowardsDependents()
        +bool HasDirtyDependencies()
        +void UpdateDependencies()
        -bool WouldCreateCyclicDependency(GameProperty)
        -void UpdateDependencyDepth()
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
    
    class ModifierType {
        <<enumeration>>
        Override
        PriorityAdd
        Add
        PriorityMul
        Mul
        AfterAdd
        Clamp
    }
    
    class IGamePropertyManager {
        <<interface>>
        +void Register(GameProperty, string, PropertyDisplayInfo)
        +GameProperty Get(string)
        +IEnumerable~GameProperty~ GetByCategory(string, bool)
        +IEnumerable~GameProperty~ GetByTag(string)
        +bool Unregister(string)
        +OperationResult ApplyModifierToCategory(string, IModifier)
    }
    
    class GamePropertyManager {
        -ConcurrentDictionary~string,GameProperty~ _properties
        -ConcurrentDictionary~string,PropertyDisplayInfo~ _metadata
        -ConcurrentDictionary~string,string~ _propertyToCategory
        -ConcurrentDictionary~string,HashSet~string~~ _categories
        -ConcurrentDictionary~string,HashSet~string~~ _tagIndex
        +void Register(GameProperty, string, PropertyDisplayInfo)
        +GameProperty Get(string)
        +IEnumerable~GameProperty~ GetByCategory(string, bool)
        +IEnumerable~GameProperty~ GetByTag(string)
        +IEnumerable~GameProperty~ GetByCategoryAndTag(string, string)
        +PropertyDisplayInfo GetMetadata(string)
        +bool Unregister(string)
        +void UnregisterCategory(string)
        +OperationResult SetCategoryActive(string, bool)
        +OperationResult ApplyModifierToCategory(string, IModifier)
    }
    
    class PropertyDisplayInfo {
        +string DisplayName
        +string Description
        +string IconPath
        +string[] Tags
        +List~CustomDataEntry~ CustomData
        +T GetCustomData~T~(string, T)
        +void SetCustomData(string, object)
    }
    
    IReadableProperty~T~ <|.. IModifiableProperty~T~
    IModifiableProperty~T~ <|.. GameProperty
    IDirtyTrackable <|.. GameProperty
    GameProperty *-- PropertyDependencyManager : contains
    GameProperty o-- IModifier : uses
    IModifier <|.. FloatModifier
    IModifier <|.. RangeModifier
    IModifier ..> ModifierType : uses
    IGamePropertyManager <|.. GamePropertyManager
    GamePropertyManager o-- GameProperty : manages
    GamePropertyManager o-- PropertyDisplayInfo : stores
```

**说明：**

- **GameProperty**：核心属性类，实现了可修改属性和脏标记接口
- **PropertyDependencyManager**：内部依赖管理器，处理属性间的依赖关系
- **IModifier**：修饰符接口，支持 FloatModifier 和 RangeModifier 两种实现
- **GamePropertyManager**：集中管理多个属性，提供分类、标签查询功能
- **PropertyDisplayInfo**：存储属性的显示信息和扩展数据

---

## 修饰符应用流程图

展示 GameProperty 如何应用修饰符并计算最终值。

```mermaid
flowchart TD
    Start([调用 GetValue]) --> CheckDirty{是否需要<br/>重新计算?}
    
    CheckDirty -->|否<br/>缓存有效| ReturnCache[返回缓存值]
    CheckDirty -->|是<br/>属性脏了| UpdateDeps[更新依赖项]
    
    UpdateDeps --> InitValue[初始化:<br/>ret = baseValue]
    InitValue --> CheckModifiers{有修饰符?}
    
    CheckModifiers -->|否| UpdateCache[更新缓存]
    CheckModifiers -->|是| GroupModifiers[按类型分组修饰符]
    
    GroupModifiers --> ApplyOverride[1. Override 类型]
    ApplyOverride --> ApplyPriorityAdd[2. PriorityAdd 类型]
    ApplyPriorityAdd --> ApplyAdd[3. Add 类型]
    ApplyAdd --> ApplyPriorityMul[4. PriorityMul 类型]
    ApplyPriorityMul --> ApplyMul[5. Mul 类型]
    ApplyMul --> ApplyAfterAdd[6. AfterAdd 类型]
    ApplyAfterAdd --> ApplyClamp[7. Clamp 类型]
    
    ApplyClamp --> UpdateCache
    UpdateCache --> CheckValueChanged{值是否<br/>改变?}
    
    CheckValueChanged -->|否| ClearDirty[清除脏标记]
    CheckValueChanged -->|是| TriggerEvent[触发 OnValueChanged 事件]
    
    TriggerEvent --> CheckDependents{有依赖者?}
    CheckDependents -->|是| NotifyDependents[通知所有依赖者]
    CheckDependents -->|否| ClearDirty
    
    NotifyDependents --> ClearDirty
    ClearDirty --> ReturnValue[返回最终值]
    ReturnCache --> End([结束])
    ReturnValue --> End
    
    style Start fill:#e1f5e1
    style End fill:#ffe1e1
    style ApplyOverride fill:#fff4e1
    style ApplyPriorityAdd fill:#fff4e1
    style ApplyAdd fill:#fff4e1
    style ApplyPriorityMul fill:#fff4e1
    style ApplyMul fill:#fff4e1
    style ApplyAfterAdd fill:#fff4e1
    style ApplyClamp fill:#fff4e1
```

**说明：**

1. **缓存机制**：首次调用或属性脏时才重新计算，避免重复计算
2. **修饰符顺序**：严格按照 7 种类型的顺序依次应用
3. **脏标记清理**：仅在无随机性时清除脏标记
4. **事件通知**：值变化时触发事件并传播到依赖者

**性能优化点：**
- 使用 `_isDirty` 标记避免无效计算
- 按类型分组修饰符，减少遍历次数
- 使用策略模式缓存，避免重复创建策略对象

---

## 依赖系统序列图

展示属性间的依赖关系和更新传播机制。

```mermaid
sequenceDiagram
    participant User as 用户代码
    participant Strength as strength<br/>(GameProperty)
    participant MaxHealth as maxHealth<br/>(GameProperty)
    participant DepMgr as DependencyManager
    participant Health as health<br/>(GameProperty)
    
    Note over User,Health: 场景：最大生命值依赖于力量属性
    
    User->>MaxHealth: AddDependency(strength, calc)
    activate MaxHealth
    MaxHealth->>DepMgr: AddDependency(strength, calc)
    activate DepMgr
    
    DepMgr->>DepMgr: 检查循环依赖
    DepMgr->>Strength: 添加反向引用<br/>(dependents.Add(maxHealth))
    DepMgr->>DepMgr: 更新依赖深度
    DepMgr->>MaxHealth: 立即计算初始值
    MaxHealth->>Strength: GetValue()
    Strength-->>MaxHealth: 返回 10
    MaxHealth->>MaxHealth: calculator(strength, 10) = 100
    MaxHealth->>MaxHealth: SetBaseValue(100)
    deactivate DepMgr
    deactivate MaxHealth
    
    Note over User,Health: 力量属性变化触发依赖更新
    
    User->>Strength: SetBaseValue(15)
    activate Strength
    Strength->>Strength: _baseValue = 15
    Strength->>Strength: MakeDirty()
    Strength->>Strength: 触发 OnBaseValueChanged 事件
    Strength->>Strength: GetValue() 重新计算
    Strength->>DepMgr: TriggerDependentUpdates(15)
    activate DepMgr
    
    DepMgr->>MaxHealth: 计算新值 = calculator(strength, 15)
    activate MaxHealth
    MaxHealth->>MaxHealth: newValue = 150
    MaxHealth->>MaxHealth: SetBaseValue(150)
    MaxHealth->>MaxHealth: MakeDirty()
    MaxHealth->>MaxHealth: GetValue() 重新计算
    MaxHealth->>MaxHealth: 触发 OnValueChanged(100, 150)
    MaxHealth->>Health: 如果 health 依赖 maxHealth<br/>则触发其更新
    deactivate MaxHealth
    
    deactivate DepMgr
    deactivate Strength
    
    Note over User,Health: 依赖链式传播完成
```

**说明：**

1. **添加依赖**：建立双向引用（正向依赖 + 反向依赖者）
2. **循环检测**：使用 DFS 算法检测并阻止循环依赖
3. **自动更新**：被依赖属性变化时，自动触发依赖者重新计算
4. **级联传播**：支持多层依赖链的级联更新

**关键机制：**
- **依赖深度**：限制依赖链深度（最大 100 层），防止栈溢出
- **脏标记传播**：依赖项变脏时，向上传播到所有依赖者
- **计算函数**：支持自定义计算逻辑，灵活实现各种依赖关系

---

## 属性状态机图

展示 GameProperty 的脏标记状态转换。

```mermaid
stateDiagram-v2
    [*] --> Clean: 创建属性
    
    Clean --> Dirty: AddModifier()<br/>RemoveModifier()<br/>SetBaseValue()<br/>依赖项变脏
    
    Dirty --> Computing: GetValue() 调用
    
    Computing --> Clean: 计算完成<br/>(无随机性)
    Computing --> AlwaysDirty: 计算完成<br/>(有随机修饰符)
    
    AlwaysDirty --> Computing: GetValue() 再次调用
    
    Clean --> [*]: 属性销毁
    Dirty --> [*]: 属性销毁
    AlwaysDirty --> [*]: 属性销毁
    
    note right of Clean
        缓存有效状态
        GetValue() 直接返回缓存值
    end note
    
    note right of Dirty
        缓存失效状态
        需要重新计算
    end note
    
    note right of Computing
        计算中状态
        应用所有修饰符
        触发事件通知
    end note
    
    note right of AlwaysDirty
        永久脏状态
        每次 GetValue() 都重新计算
        适用于随机范围修饰符
    end note
```

**说明：**

- **Clean（干净）**：缓存有效，直接返回缓存值
- **Dirty（脏）**：缓存失效，需要重新计算
- **Computing（计算中）**：正在应用修饰符并计算最终值
- **AlwaysDirty（永久脏）**：包含随机修饰符，每次都重新计算

**状态转换条件：**

1. **Clean → Dirty**：
   - 添加/移除修饰符
   - 修改基础值
   - 依赖项变脏

2. **Dirty → Computing**：
   - 调用 `GetValue()`

3. **Computing → Clean**：
   - 计算完成且无随机性修饰符

4. **Computing → AlwaysDirty**：
   - 计算完成但存在随机性修饰符（如 RangeModifier）

**性能影响：**
- Clean 状态：性能最优，无计算开销
- AlwaysDirty 状态：性能较低，适合需要实时随机的场景

---

## 管理器交互时序图

展示用户代码与 GamePropertyManager 的交互流程。

```mermaid
sequenceDiagram
    participant User as 用户代码
    participant Arch as EasyPackArchitecture
    participant Manager as GamePropertyManager
    participant Props as Properties 字典
    participant Meta as Metadata 字典
    participant Category as Category 索引
    participant Tag as Tag 索引
    
    Note over User,Tag: 初始化阶段
    
    User->>Arch: ResolveAsync<IGamePropertyManager>()
    Arch->>Manager: 获取单例实例
    Arch-->>User: 返回 Manager
    
    Note over User,Tag: 注册属性
    
    User->>Manager: Register(hp, "Character.Vital", metadata)
    activate Manager
    Manager->>Manager: 验证服务状态 (Ready)
    Manager->>Manager: 检查 ID 唯一性
    Manager->>Props: _properties[hp.ID] = hp
    Manager->>Meta: _metadata[hp.ID] = metadata
    Manager->>Category: _categories["Character.Vital"].Add(hp.ID)
    
    loop 每个 Tag
        Manager->>Tag: _tagIndex[tag].Add(hp.ID)
    end
    
    Manager-->>User: 返回 true (成功)
    deactivate Manager
    
    Note over User,Tag: 查询属性
    
    User->>Manager: GetByCategory("Character.Vital")
    activate Manager
    Manager->>Category: 查找分类下的 ID 列表
    Category-->>Manager: 返回 ["hp", "mp"]
    
    loop 每个 ID
        Manager->>Props: _properties[id]
        Props-->>Manager: 返回 GameProperty
    end
    
    Manager-->>User: 返回 IEnumerable<GameProperty>
    deactivate Manager
    
    Note over User,Tag: 批量操作
    
    User->>Manager: ApplyModifierToCategory("Character.Vital", buff)
    activate Manager
    Manager->>Manager: GetByCategory("Character.Vital")
    
    loop 每个属性
        Manager->>Manager: modifier.Clone()
        Manager->>Props: property.AddModifier(clonedModifier)
        Props->>Props: MakeDirty()
        Props-->>Manager: 成功
        Manager->>Manager: successIds.Add(property.ID)
    end
    
    Manager->>Manager: 构建 OperationResult
    Manager-->>User: 返回结果<br/>(SuccessCount, Failures)
    deactivate Manager
    
    Note over User,Tag: 标签查询
    
    User->>Manager: GetByTag("displayInUI")
    activate Manager
    Manager->>Tag: 查找标签下的 ID 列表
    Tag-->>Manager: 返回 ["hp", "mp", "level"]
    
    loop 每个 ID
        Manager->>Props: _properties[id]
        Props-->>Manager: 返回 GameProperty
    end
    
    Manager-->>User: 返回 IEnumerable<GameProperty>
    deactivate Manager
```

**说明：**

1. **服务获取**：通过 `EasyPackArchitecture.Instance.ResolveAsync<IGamePropertyManager>()` 异步解析管理器服务
2. **属性注册**：同时更新主表、元数据、分类索引和标签索引
3. **分类查询**：通过分类索引快速定位属性 ID，再从主表获取实例
4. **批量操作**：遍历分类下的所有属性，克隆修饰符后逐个应用
5. **标签查询**：通过标签索引快速查找，支持多标签组合查询

**架构集成模式**：

Manager 作为服务注册到 EasyPack 架构中：

```csharp
// 在 EasyPackArchitecture.OnInit() 中
Container.Register<IGamePropertyManager, GamePropertyManager>();

// 用户代码中获取
var manager = await EasyPackArchitecture.Instance.ResolveAsync<IGamePropertyManager>();

// Editor 窗口安全解析
// 打开菜单 EasyPack/CoreSystems/游戏属性(GameProperty)/管理器窗口
// 窗口会先检查服务状态（已注册、已实例化、已就绪）
// 只在服务 Ready 时才解析，不会主动初始化
```

**索引优化：**
- **分类索引**：支持层级分类（如 "Character.Vital.HP"）
- **标签索引**：支持一个属性多个标签，快速交集查询
- **并发安全**：使用 ConcurrentDictionary 保证线程安全

**典型使用场景：**
- **角色属性系统**：按 "Character.Base" 查询所有基础属性
- **UI 显示**：按 "displayInUI" 标签筛选需要显示的属性
- **存档系统**：按 "saveable" 标签筛选需要保存的属性
- **Buff 系统**：批量为某分类属性应用修饰符

---

## 序列化流程图

展示 GamePropertyManager 的序列化和反序列化过程。

```mermaid
flowchart TD
    Start([序列化请求]) --> ToSerializable[ToSerializable]
    ToSerializable --> CollectProps[遍历所有属性]
    CollectProps --> SerializeProp[序列化 GameProperty]
    SerializeProp --> GetCategory[获取属性所属分类]
    GetCategory --> GetMetadata[获取元数据]
    GetMetadata --> BuildDTO[构建 PropertyManagerDTO]
    BuildDTO --> ToJson[JsonUtility.ToJson]
    ToJson --> End1([JSON 字符串])

    Start2([反序列化请求]) --> FromJson[JsonUtility.FromJson]
    FromJson --> CreateManager[创建新 Manager]
    CreateManager --> Init[InitializeAsync]
    Init --> DeserializeProps[遍历 DTO.Properties]
    DeserializeProps --> DeserializeProp[反序列化 GameProperty]
    DeserializeProp --> FindMetadata[查找对应元数据]
    FindMetadata --> Register[Register 属性]
    Register --> RebuildIndex[自动重建索引]
    RebuildIndex --> End2([Manager 实例])

    style Start fill:#e1f5e1
    style End1 fill:#ffe1e1
    style Start2 fill:#e1f5e1
    style End2 fill:#ffe1e1
    style RebuildIndex fill:#fff4e1
```

**说明：**

1. **序列化流程**：
   - 遍历所有属性并序列化为 JSON
   - 收集元数据和分类信息
   - 构建 DTO 对象并转换为 JSON

2. **反序列化流程**：
   - 解析 JSON 为 DTO 对象
   - 创建新的 Manager 实例并初始化
   - 逐个反序列化属性并注册
   - **自动重建索引**（分类索引、标签索引）

3. **关键优化**：
   - 索引不序列化，运行时重建
   - 部分失败不影响其他属性
   - 使用 Unity JsonUtility 保证兼容性

---

**维护者：** NEKOPACK 团队  
**联系方式：** 提交 GitHub Issue 或 Pull Request  
**许可证：** 遵循项目主许可证
