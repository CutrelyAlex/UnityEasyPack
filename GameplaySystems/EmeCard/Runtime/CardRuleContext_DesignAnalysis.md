# CardRuleContext 设计分析报告

> 生成日期: 2025-10-02  
> 分析对象: `CardRuleContext.cs`

---

## 📋 一、类概览

### 基本信息
- **命名空间**: `EasyPack`
- **访问修饰符**: `public sealed`
- **作用**: 规则执行上下文，为规则匹配和效果执行提供必要的上下文信息

### 核心职责
1. **事件传递**: 携带原始事件信息（Source, Event）
2. **容器定位**: 提供规则执行的容器（Container）
3. **服务注入**: 提供工厂服务（Factory）
4. **数据访问**: 提供便捷的事件数据访问方法

---

## 🏗️ 二、设计结构分析

### 2.1 字段组成

| 字段 | 类型 | 职责 | 必要性 |
|------|------|------|--------|
| `Source` | `Card` | 触发规则的事件源卡牌 | ⭐⭐⭐⭐⭐ 核心 |
| `Container` | `Card` | 规则执行的容器（由 OwnerHops 选择） | ⭐⭐⭐⭐⭐ 核心 |
| `Event` | `CardEvent` | 原始事件载体 | ⭐⭐⭐⭐⭐ 核心 |
| `Factory` | `ICardFactory` | 产卡工厂 | ⭐⭐⭐⭐ 重要 |
| `MaxDepth` | `int` | 递归搜索深度限制 | ⭐⭐⭐ 常用 |

### 2.2 便捷属性

```csharp
// ✅ 特化访问 - 针对 Tick 事件
public float DeltaTime { get; }

// ✅ 通用访问 - 适用所有事件
public string EventId { get; }
public Card DataCard { get; }
public T DataAs<T>() where T : class
public bool TryGetData<T>(out T value)
```

**设计评价**: 
- ✅ `DeltaTime` 专门为 Tick 事件优化，避免频繁类型转换
- ✅ 提供泛型方法增强类型安全
- ✅ `TryGetData` 模式符合 C# 最佳实践

---

## 🔄 三、使用场景分析

### 3.1 创建时机
`CardRuleContext` 在 `CardEngine.BuildContext()` 中创建：

```csharp
private CardRuleContext BuildContext(CardRule rule, Card source, CardEvent evt)
{
    var container = SelectContainer(rule.OwnerHops, source);
    if (container == null) return null;
    
    return new CardRuleContext
    {
        Source = source,
        Container = container,
        Event = evt,
        Factory = CardFactory,
        MaxDepth = rule.MaxDepth
    };
}
```

**生命周期**: 
- **创建**: 每个规则匹配时
- **使用**: Requirements 匹配 → Effects 执行
- **销毁**: 规则执行完毕后自动回收

### 3.2 传递链路

```
Event Trigger (card.Tick())
    ↓
CardEngine.OnCardEvent()
    ↓
CardEngine.Process()
    ↓
[For each matching rule]
    ↓
BuildContext() → CardRuleContext 实例化
    ↓
EvaluateRequirements(ctx, ...) → 匹配检查
    ↓
ExecuteOne(rule, matched, ctx, ...) → 效果执行
    ↓
effect.Execute(ctx, matched) → 具体效果
```

### 3.3 使用者分析

#### IRuleRequirement 接口
```csharp
bool TryMatch(CardRuleContext ctx, out List<Card> matched);
```
**用途**: 
- 访问 `ctx.Source` 判断事件源
- 访问 `ctx.Container` 查找匹配卡牌
- 访问 `ctx.Event` 判断事件类型和数据

#### IRuleEffect 接口
```csharp
void Execute(CardRuleContext ctx, IReadOnlyList<Card> matched);
```
**用途**: 
- 访问 `ctx.Factory` 创建新卡
- 访问 `ctx.Source/Container` 操作卡牌
- 访问 `ctx.DeltaTime` 处理时间相关逻辑
- 访问 `ctx.Event.Data` 获取自定义数据

#### 用户代码（通过 Builder）
```csharp
.When(ctx => ctx.Source.HasTag("可激活"))
.DoInvoke((ctx, matched) => {
    ctx.Source.AddTag("已激活");
    float dt = ctx.DeltaTime;
    // 自定义逻辑
})
```

---

## ✅ 四、设计优点

### 4.1 单一职责原则 (SRP)
✅ **职责明确**: Context 仅负责携带上下文信息，不包含业务逻辑

### 4.2 依赖注入
✅ **解耦合**: 通过 Context 传递依赖（Factory），避免全局单例

### 4.3 不可变性
✅ **线程安全倾向**: 使用 `sealed` 防止继承，字段为引用类型但不在内部修改

### 4.4 扩展性
✅ **便捷方法**: `DeltaTime`, `DataAs<T>()` 等为常用场景提供快捷方式

### 4.5 类型安全
✅ **泛型支持**: `TryGetData<T>` 提供类型安全的数据访问

---

## ⚠️ 五、潜在问题与改进建议

### 5.1 命名问题

**现状 TODO**:
```csharp
// TODO: 评估是否要改名为 CardContext或其他更合适的名字
```

**分析**:
- ✅ **保持现名的理由**:
  - 当前仅用于规则系统，名称明确表达用途
  - 避免与未来可能的其他 Context 混淆
  
- ⚠️ **改名的触发条件**（如注释所述）:
  1. 引入到非规则模块（CardCache、调试、可视化）
  2. 加入与规则无关的服务（日志、配置、随机源）
  3. 出现第二个上下文需要区分

**建议**: 
- 当前阶段保持 `CardRuleContext` 命名 ✅
- 如果未来扩展到规则外，考虑重构为：
  ```csharp
  CardContext (基础上下文)
      ↓
  CardRuleContext (规则专用，继承基础)
  ```

### 5.2 字段可变性

**问题**: 所有字段为 `public` 且可修改

```csharp
public Card Source;         // ⚠️ 可被外部修改
public Card Container;      // ⚠️ 可被外部修改
public CardEvent Event;     // ⚠️ 可被外部修改
```

**风险**:
- 效果执行过程中可能意外修改 Context
- 调试困难（无法确定谁修改了字段）

**改进建议**:
```csharp
// 方案1: 使用只读属性
public Card Source { get; }
public Card Container { get; }
public CardEvent Event { get; }
public ICardFactory Factory { get; }

// 通过构造函数初始化
public CardRuleContext(Card source, Card container, CardEvent evt, ICardFactory factory, int maxDepth)
{
    Source = source;
    Container = container;
    Event = evt;
    Factory = factory;
    MaxDepth = maxDepth;
}
```

**优点**:
- ✅ 防止意外修改
- ✅ 语义更清晰（只读上下文）
- ✅ 符合不可变对象模式

### 5.3 MaxDepth 的双重来源

**问题**: MaxDepth 既在 `CardRule` 中定义，又在 `CardRuleContext` 中复制

```csharp
// CardRule.cs
public int MaxDepth = int.MaxValue;

// CardRuleContext.cs
public int MaxDepth;

// CardEngine.BuildContext()
return new CardRuleContext
{
    MaxDepth = rule.MaxDepth  // 复制值
};
```

**分析**:
- ✅ **优点**: Context 自包含，不依赖 Rule 对象
- ⚠️ **缺点**: 数据重复，可能导致不一致

**建议**: 当前设计合理，但可以考虑：
```csharp
// 方案1: 明确来源（推荐）
public int MaxDepth { get; } // 只读，来自规则配置

// 方案2: 支持动态覆盖
private readonly int _baseMaxDepth;
public int MaxDepth { get; set; } // 可在效果中临时调整
```

### 5.4 缺少状态标志

**潜在需求**: 
- 是否需要标记"Context 是否已失效"？
- 是否需要追踪"执行阶段"（Requirement/Effect）？

**建议**:
```csharp
public enum ContextPhase
{
    RequirementEvaluation,  // 匹配阶段
    EffectExecution         // 执行阶段
}

public ContextPhase Phase { get; internal set; }
```

**用途**:
- 调试时明确当前执行阶段
- 某些操作可能只在特定阶段允许

### 5.5 缺少日志/追踪支持

**问题**: 调试复杂规则时缺少追踪信息

**建议**:
```csharp
// 可选的追踪标识
public int ExecutionId { get; } // 唯一执行ID
public DateTime CreatedAt { get; } // 创建时间

// 或者关联到引擎的全局追踪
public IContextTracer Tracer { get; } // 可选注入
```

---

## 🎯 六、与其他模块的关系

### 6.1 CardEngine
- **创建者**: Engine 负责创建 Context
- **所有权**: Context 生命周期由 Engine 管理

### 6.2 CardRule
- **配置提供**: Rule 提供 OwnerHops, MaxDepth 等配置
- **间接关联**: Context 不直接持有 Rule 引用（✅ 解耦）

### 6.3 IRuleRequirement / IRuleEffect
- **消费者**: 通过接口参数接收 Context
- **只读使用**: 应该仅读取 Context，不修改（目前无强制）

### 6.4 TargetSelector
- **工具类**: 使用 Context 选择目标卡牌
- **创建局部 Context**: 为效果执行创建子上下文

```csharp
var localCtx = new CardRuleContext
{
    Source = ctx.Source,
    Container = root,  // ✅ 切换根容器
    Event = ctx.Event,
    Factory = ctx.Factory,
    MaxDepth = selection.MaxDepth ?? ctx.MaxDepth
};
```

**设计评价**: ✅ 支持嵌套/局部 Context，灵活性高

---

## 🔬 七、性能考量

### 7.1 对象分配
- **问题**: 每个规则匹配都会创建新 Context
- **影响**: 频繁事件（如 Tick）可能产生 GC 压力

**建议**:
```csharp
// 方案1: 对象池
private readonly Stack<CardRuleContext> _contextPool = new();

// 方案2: 结构体（仅当字段全部为值类型或引用时）
public struct CardRuleContext { ... }
```

### 7.2 便捷属性的开销
```csharp
public float DeltaTime
{
    get
    {
        if (Event.Type == CardEventType.Tick && Event.Data is float f)
            return f;
        return 0f;
    }
}
```

**分析**:
- ⚠️ 每次访问都进行类型检查和转换
- ✅ 仅访问几次时影响可忽略

**优化建议**:
```csharp
// 缓存常用值（如果频繁访问）
private float? _cachedDeltaTime;
public float DeltaTime
{
    get
    {
        if (!_cachedDeltaTime.HasValue)
        {
            _cachedDeltaTime = (Event.Type == CardEventType.Tick && Event.Data is float f) 
                ? f : 0f;
        }
        return _cachedDeltaTime.Value;
    }
}
```

---

## 📊 八、设计模式总结

### 使用的模式
1. **上下文对象模式** (Context Object)
   - ✅ 封装请求处理所需的状态
   
2. **依赖注入** (Dependency Injection)
   - ✅ 通过 Context 传递 Factory

3. **便利层** (Convenience Layer)
   - ✅ `DeltaTime`, `DataAs<T>` 简化常见操作

### 未使用但可考虑的模式
1. **构建器模式** (Builder)
   - 如果字段变为只读，可使用构建器创建

2. **对象池** (Object Pooling)
   - 减少高频事件的 GC 压力

---

## 🎓 九、最佳实践对比

| 方面 | 当前实现 | 理想实践 | 评分 |
|------|----------|----------|------|
| 单一职责 | ✅ 仅携带上下文 | ✅ 符合 | ⭐⭐⭐⭐⭐ |
| 不可变性 | ⚠️ 字段可修改 | ❌ 应只读 | ⭐⭐⭐ |
| 类型安全 | ✅ 泛型支持 | ✅ 符合 | ⭐⭐⭐⭐⭐ |
| 性能优化 | ⚠️ 频繁分配 | ⚠️ 可优化 | ⭐⭐⭐ |
| 可扩展性 | ✅ 设计灵活 | ✅ 符合 | ⭐⭐⭐⭐ |
| 文档完整 | ✅ 注释清晰 | ✅ 符合 | ⭐⭐⭐⭐⭐ |

**总体评分**: ⭐⭐⭐⭐ (4/5) - 设计优秀，有小幅改进空间

---

## 🚀 十、改进建议优先级

### 高优先级（建议实施）
1. **将字段改为只读属性** 🔥
   - 防止意外修改，提升代码健壮性
   
2. **添加构造函数** 🔥
   - 配合只读属性，强制正确初始化

### 中优先级（可选实施）
3. **性能优化** ⚡
   - 如果性能分析显示 Context 创建是瓶颈，考虑对象池
   
4. **添加追踪支持** 🔍
   - 仅在复杂项目需要深度调试时考虑

### 低优先级（保持观察）
5. **命名调整** 💭
   - 等到确实需要扩展到规则外场景时再重构
   
6. **添加执行阶段标记** 📍
   - 目前结构简单，暂无必要

---

## 📝 十一、代码重构示例

### 改进后的设计（可选参考）

```csharp
namespace EasyPack
{
    /// <summary>
    /// 规则执行上下文：为效果提供触发源、容器与原始事件等信息。
    /// 不可变设计，创建后内容不可修改。
    /// </summary>
    public sealed class CardRuleContext
    {
        // ========== 构造函数 ==========
        public CardRuleContext(
            Card source, 
            Card container, 
            CardEvent evt, 
            ICardFactory factory, 
            int maxDepth)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Container = container ?? throw new ArgumentNullException(nameof(container));
            Event = evt ?? throw new ArgumentNullException(nameof(evt));
            Factory = factory;
            MaxDepth = maxDepth;
        }

        // ========== 核心字段（只读） ==========
        /// <summary>触发该规则的卡牌（事件源）。</summary>
        public Card Source { get; }

        /// <summary>用于匹配与执行的容器（由规则的 OwnerHops 选择）。</summary>
        public Card Container { get; }

        /// <summary>原始事件载体（包含类型、ID、数据等）。</summary>
        public CardEvent Event { get; }

        /// <summary>产卡工厂。</summary>
        public ICardFactory Factory { get; }

        /// <summary>
        /// 递归搜索最大深度（>0 生效，1 表示仅子级一层）。
        /// </summary>
        public int MaxDepth { get; }

        // ========== 便捷属性（缓存优化） ==========
        private float? _cachedDeltaTime;
        public float DeltaTime
        {
            get
            {
                if (!_cachedDeltaTime.HasValue)
                {
                    _cachedDeltaTime = (Event.Type == CardEventType.Tick && Event.Data is float f) 
                        ? f : 0f;
                }
                return _cachedDeltaTime.Value;
            }
        }

        public string EventId => Event.ID;
        public Card DataCard => Event.Data as Card;
        public T DataAs<T>() where T : class => Event.Data as T;

        public bool TryGetData<T>(out T value)
        {
            if (Event.Data is T v)
            {
                value = v;
                return true;
            }
            value = default;
            return false;
        }

        // ========== 可选：调试支持 ==========
        public override string ToString()
        {
            return $"RuleContext[Source:{Source?.Name}, Event:{Event.Type}, Container:{Container?.Name}]";
        }
    }
}
```

### 对应的 Engine 修改

```csharp
private CardRuleContext BuildContext(CardRule rule, Card source, CardEvent evt)
{
    var container = SelectContainer(rule.OwnerHops, source);
    if (container == null) return null;
    
    // 使用构造函数创建（强制完整初始化）
    return new CardRuleContext(
        source: source,
        container: container,
        evt: evt,
        factory: CardFactory,
        maxDepth: rule.MaxDepth
    );
}
```

---

## 🎯 十二、结论

### 整体评价
`CardRuleContext` 设计**整体优秀** ✅，充分体现了以下优点：
- 职责清晰，接口简洁
- 类型安全，扩展灵活
- 文档完善，易于理解

### 核心优势
1. **解耦性强**: Context 作为中介，解耦了 Engine 与 Effect
2. **扩展性好**: 便捷方法支持常见场景，泛型方法支持自定义场景
3. **易用性高**: Builder 模式隐藏了 Context 的复杂性

### 改进空间
主要在 **不可变性** 和 **性能优化** 方面：
- 字段应改为只读属性（代码健壮性）
- 考虑对象池（性能优化，可选）

### 建议行动
1. **短期**（推荐）: 将字段改为只读属性 + 添加构造函数
2. **中期**（按需）: 性能分析后决定是否引入对象池
3. **长期**（观察）: 根据系统演化决定是否重命名/重构

---

## 📚 附录：相关资源

- **设计模式参考**: 《设计模式：可复用面向对象软件的基础》- Context Object Pattern
- **C# 最佳实践**: Microsoft C# Coding Conventions - Immutability
- **性能优化**: Unity 官方文档 - Understanding Automatic Memory Management

---

**分析完成** ✅  
*如有疑问或需要深入讨论某个方面，请随时提出。*
