# EmeCard 系统 - API 参考文档

**适用EasyPack版本：** EasyPack v1.5.30  
**最后更新：** 2025-10-26


---

## 目录

1. [核心类](#核心类)
   - [Card](#card-类)
   - [CardData](#carddata-类)
   - [CardEngine](#cardengine-类)
   - [CardFactory](#cardfactory-类)
   - [CardRule](#cardrule-类)
   - [CardRuleContext](#cardrulecontext-类)
2. [规则组件接口](#规则组件接口)
   - [IRuleRequirement](#irulerequirement-接口)
   - [IRuleEffect](#iruleeffect-接口)
   - [ITargetSelection](#itargetselection-接口)
3. [内置要求项](#内置要求项)
   - [CardsRequirement](#cardsrequirement-类)
   - [ConditionRequirement](#conditionrequirement-类)
4. [内置效果](#内置效果)
   - [CreateCardsEffect](#createcardseffect-类)
   - [RemoveCardsEffect](#removecardseffect-类)
   - [ModifyPropertyEffect](#modifypropertyeffect-类)
   - [AddTagEffect](#addtageffect-类)
   - [InvokeEffect](#invokeeffect-类)
5. [工具类](#工具类)
   - [CardRuleBuilder](#cardrulebuilder-类)
   - [TargetSelector](#targetselector-类)
6. [枚举类型](#枚举类型)

---

## 核心类

### Card 类

**命名空间**：`EasyPack.EmeCardSystem`

**描述**：卡牌实例，系统的基本单元，可持有子卡牌、关联属性、携带标签，并通过事件驱动规则引擎。

#### 构造函数

##### Card()
```csharp
public Card()
```
无参构造函数，创建空白卡牌实例。

---

##### Card(CardData, GameProperty, string[])
```csharp
public Card(CardData data, GameProperty gameProperty = null, params string[] extraTags)
```
创建卡牌实例，可选单个属性。

**参数**：

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `data` | `CardData` | - | 卡牌静态数据 |
| `gameProperty` | `GameProperty` | `null` | 可选的单个游戏属性 |
| `extraTags` | `string[]` | - | 额外标签（除默认标签外） |

**示例**：
```csharp
using EasyPack.EmeCardSystem;
using EasyPack.GamePropertySystem;

var data = new CardData("sword", "铁剑", "", CardCategory.Object);
var property = new GameProperty("攻击力", 50f);
var card = new Card(data, property, "锋利", "稀有");
```

---

##### Card(CardData, IEnumerable<GameProperty>, string[])
```csharp
public Card(CardData data, IEnumerable<GameProperty> properties, params string[] extraTags)
```
创建卡牌实例，可选多个属性。

**参数**：

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `data` | `CardData` | - | 卡牌静态数据 |
| `properties` | `IEnumerable<GameProperty>` | - | 属性列表（`null` 时创建空列表） |
| `extraTags` | `string[]` | - | 额外标签 |

**示例**：
```csharp
var properties = new List<GameProperty>
{
    new GameProperty("生命值", 100f),
    new GameProperty("法力值", 50f)
};
var card = new Card(data, properties, "英雄");
```

---

##### Card(CardData, string[])
```csharp
public Card(CardData data, params string[] extraTags)
```
简化构造函数：仅提供卡牌数据和标签（无属性）。

**参数**：

| 参数 | 类型 | 说明 |
|------|------|------|
| `data` | `CardData` | 卡牌静态数据 |
| `extraTags` | `string[]` | 额外标签 |

**示例**：
```csharp
var card = new Card(data, "武器", "近战");
```

---

#### 属性

##### Data
```csharp
public CardData Data { get; set; }
```
卡牌的静态数据（ID/名称/描述/默认标签等）。赋值时会清空并重新加载默认标签。

**类型**：`CardData`

---

##### Index
```csharp
public int Index { get; set; }
```
实例索引，用于区分同一 ID 的多个实例（由引擎在 `AddCard` 时分配，从 0 起）。

**类型**：`int`  
**默认值**：`0`

---

##### Id
```csharp
public string Id { get; }
```
卡牌标识（只读），来自 `Data.ID`。

**类型**：`string`  
**返回值**：卡牌 ID，若 `Data` 为 `null` 返回空字符串

---

##### Name
```csharp
public string Name { get; }
```
卡牌显示名称（只读），来自 `Data.Name`。

**类型**：`string`

---

##### Description
```csharp
public string Description { get; }
```
卡牌描述（只读），来自 `Data.Description`。

**类型**：`string`

---

##### Category
```csharp
public CardCategory Category { get; }
```
卡牌类别（只读），来自 `Data.Category`，若 `Data` 为 `null` 返回 `CardCategory.Object`。

**类型**：`CardCategory`

---

##### Properties
```csharp
public List<GameProperty> Properties { get; set; }
```
数值属性列表。

**类型**：`List<GameProperty>`

---

##### Tags
```csharp
public IReadOnlyCollection<string> Tags { get; }
```
标签集合（只读）。标签用于规则匹配，大小写敏感。

**类型**：`IReadOnlyCollection<string>`

---

##### Owner
```csharp
public Card Owner { get; }
```
当前卡牌的持有者（父卡），只读。

**类型**：`Card`  
**返回值**：持有者实例，若无持有者返回 `null`

---

##### Children
```csharp
public IReadOnlyList<Card> Children { get; }
```
子卡牌列表（只读视图）。

**类型**：`IReadOnlyList<Card>`

---

##### ChildrenCount
```csharp
public int ChildrenCount { get; }
```
子卡牌数量（只读）。

**类型**：`int`

---

#### 方法

##### GetProperty
```csharp
public GameProperty GetProperty(string id)
```
根据 ID 获取属性。

**参数**：

| 参数 | 类型 | 说明 |
|------|------|------|
| `id` | `string` | 属性 ID |

**返回值**：
- **成功**：匹配的 `GameProperty` 实例
- **失败**：`null`（未找到）

**示例**：
```csharp
var atk = card.GetProperty("攻击力");
if (atk != null)
    Debug.Log($"攻击力：{atk.GetValue()}");
```

---

##### HasTag
```csharp
public bool HasTag(string tag)
```
判断是否包含指定标签。

**参数**：

| 参数 | 类型 | 说明 |
|------|------|------|
| `tag` | `string` | 标签文本 |

**返回值**：
- `true`：包含该标签
- `false`：不包含或标签为空

---

##### AddTag
```csharp
public bool AddTag(string tag)
```
添加一个标签。

**参数**：

| 参数 | 类型 | 说明 |
|------|------|------|
| `tag` | `string` | 标签文本 |

**返回值**：
- `true`：成功新增（之前不存在）
- `false`：已存在或标签为空

---

##### RemoveTag
```csharp
public bool RemoveTag(string tag)
```
移除一个标签。

**参数**：

| 参数 | 类型 | 说明 |
|------|------|------|
| `tag` | `string` | 标签文本 |

**返回值**：
- `true`：成功移除
- `false`：不存在或标签为空

---

##### AddChild
```csharp
public Card AddChild(Card child, bool intrinsic = false)
```
将子卡牌加入当前卡牌作为持有者。

**参数**：

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `child` | `Card` | - | 子卡牌实例 |
| `intrinsic` | `bool` | `false` | 是否作为"固有子卡"（无法被规则消耗或移除） |

**返回值**：返回当前卡牌实例（支持链式调用）

**异常**：
- `ArgumentNullException`：`child` 为 `null`
- `InvalidOperationException`：子卡已被其他卡牌持有
- `Exception`：尝试添加自身为子卡

**副作用**：向子卡派发 `CardEventType.AddedToOwner` 事件

**示例**：
```csharp
var player = new Card(...);
var weapon = new Card(...);
player.AddChild(weapon, intrinsic: true); // 武器是固有装备
```

---

##### RemoveChild
```csharp
public bool RemoveChild(Card child, bool force = false)
```
从当前卡牌移除一个子卡牌。

**参数**：

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `child` | `Card` | - | 要移除的子卡牌 |
| `force` | `bool` | `false` | 是否强制移除（`true` 时可移除固有子卡） |

**返回值**：
- `true`：移除成功
- `false`：移除失败（子卡不存在或为固有子卡且 `force=false`）

**副作用**：向子卡派发 `CardEventType.RemovedFromOwner` 事件

---

##### IsIntrinsic
```csharp
public bool IsIntrinsic(Card child)
```
判断某子卡是否为固有子卡。

**参数**：

| 参数 | 类型 | 说明 |
|------|------|------|
| `child` | `Card` | 要检查的子卡 |

**返回值**：
- `true`：是固有子卡
- `false`：不是或 `child` 为 `null`

---

##### RaiseEvent
```csharp
public void RaiseEvent(CardEvent evt)
```
分发一个卡牌事件到 `OnEvent`。

**参数**：

| 参数 | 类型 | 说明 |
|------|------|------|
| `evt` | `CardEvent` | 事件载体 |

---

##### Tick
```csharp
public void Tick(float deltaTime)
```
触发按时事件（`CardEventType.Tick`）。

**参数**：

| 参数 | 类型 | 说明 |
|------|------|------|
| `deltaTime` | `float` | 时间步长（秒） |

**示例**：
```csharp
void Update()
{
    card.Tick(Time.deltaTime);
    engine.Pump();
}
```

---

##### Use
```csharp
public void Use(object data = null)
```
触发主动使用事件（`CardEventType.Use`）。

**参数**：

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `data` | `object` | `null` | 可选自定义信息（如目标） |

---

##### Custom
```csharp
public void Custom(string id, object data = null)
```
触发自定义事件（`CardEventType.Custom`）。

**参数**：

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `id` | `string` | - | 自定义事件标识 |
| `data` | `object` | `null` | 可选自定义信息 |

---

#### 事件

##### OnEvent
```csharp
public event Action<Card, CardEvent> OnEvent
```
卡牌统一事件回调。订阅者（如规则引擎）可监听以实现规则匹配与效果执行。

**参数**：
- `Card`：触发事件的卡牌
- `CardEvent`：事件载体

**示例**：
```csharp
card.OnEvent += (source, evt) =>
{
    Debug.Log($"卡牌 {source.Id} 触发了 {evt.Type} 事件");
};
```

---

### CardData 类

**命名空间**：`EasyPack.EmeCardSystem`

**描述**：卡牌的静态数据，不包含运行时状态。

#### 构造函数

##### CardData
```csharp
public CardData(string id, string name = "Default", string desc = "",
                CardCategory category = CardCategory.Object, 
                string[] defaultTags = null, Sprite sprite = null)
```

**参数**：

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `id` | `string` | - | 逻辑 ID（建议全局唯一） |
| `name` | `string` | `"Default"` | 展示名（可本地化） |
| `desc` | `string` | `""` | 描述文本 |
| `category` | `CardCategory` | `CardCategory.Object` | 卡牌类别 |
| `defaultTags` | `string[]` | `null` | 默认标签（`null` 时使用空数组） |
| `sprite` | `Sprite` | `null` | 卡牌图标（`null` 时从 Resources 加载） |

**示例**：
```csharp
var data = new CardData(
    id: "fireball",
    name: "火球术",
    desc: "造成 50 点火焰伤害",
    category: CardCategory.Action,
    defaultTags: new[] { "魔法", "火系" }
);
```

---

#### 属性

##### ID
```csharp
public string ID { get; }
```
卡牌唯一标识（只读）。

---

##### Name
```csharp
public string Name { get; }
```
展示名（只读）。

---

##### Description
```csharp
public string Description { get; }
```
文本描述（只读）。

---

##### Category
```csharp
public CardCategory Category { get; }
```
卡牌类别（只读）。

---

##### Sprite
```csharp
public Sprite Sprite { get; set; }
```
卡牌图标（可读写）。

---

##### DefaultTags
```csharp
public string[] DefaultTags { get; }
```
默认标签集合（只读）。应视为只读元数据，不建议运行时修改。

---

### CardEngine 类

**命名空间**：`EasyPack.EmeCardSystem`

**描述**：卡牌引擎，管理卡牌实例、规则注册、事件分发。

#### 构造函数

##### CardEngine
```csharp
public CardEngine(ICardFactory factory)
```

**参数**：

| 参数 | 类型 | 说明 |
|------|------|------|
| `factory` | `ICardFactory` | 卡牌工厂实例 |

**示例**：
```csharp
var factory = new CardFactory();
var engine = new CardEngine(factory);
```

---

#### 属性

##### CardFactory
```csharp
public ICardFactory CardFactory { get; set; }
```
卡牌工厂（可读写）。

---

##### Policy
```csharp
public EnginePolicy Policy { get; }
```
引擎全局策略（只读）。

---

#### 方法

##### RegisterRule(CardRule)
```csharp
public void RegisterRule(CardRule rule)
```
注册一条规则到引擎。

**参数**：

| 参数 | 类型 | 说明 |
|------|------|------|
| `rule` | `CardRule` | 规则实例 |

**异常**：
- `ArgumentNullException`：`rule` 为 `null`

---

##### RegisterRule(Func<CardRuleBuilder, CardRuleBuilder>)
```csharp
public void RegisterRule(Func<CardRuleBuilder, CardRuleBuilder> builder)
```
使用流式构建器注册规则（扩展方法）。

**参数**：

| 参数 | 类型 | 说明 |
|------|------|------|
| `builder` | `Func<CardRuleBuilder, CardRuleBuilder>` | 构建器委托 |

**示例**：
```csharp
engine.RegisterRule(b => b
    .On(CardEventType.Use)
    .NeedTag("玩家")
    .DoCreate("金币")
);
```

---

##### Pump
```csharp
public void Pump(int maxEvents = 2048)
```
事件主循环，依次处理队列中的所有事件。

**参数**：

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `maxEvents` | `int` | `2048` | 最大处理事件数（防止死循环） |

**副作用**：处理主队列和延迟队列中的事件

---

##### CreateCard
```csharp
public Card CreateCard(string id)
```
按 ID 创建并注册卡牌实例（`Card` 类型）。

**参数**：

| 参数 | 类型 | 说明 |
|------|------|------|
| `id` | `string` | 卡牌 ID |

**返回值**：
- **成功**：创建的卡牌实例
- **失败**：`null`（工厂中未注册该 ID）

**异常**：
- `ArgumentNullException`：`id` 为 `null`

---

##### CreateCard<T>
```csharp
public T CreateCard<T>(string id) where T : Card
```
按 ID 创建并注册卡牌实例（泛型版本）。

**类型参数**：
- `T`：卡牌类型（必须继承自 `Card`）

**返回值**：
- **成功**：创建的卡牌实例（`T` 类型）
- **失败**：`null`

---

##### AddCard
```csharp
public CardEngine AddCard(Card c)
```
添加卡牌到引擎，分配唯一 Index 并订阅事件。

**参数**：

| 参数 | 类型 | 说明 |
|------|------|------|
| `c` | `Card` | 卡牌实例 |

**返回值**：返回引擎实例（支持链式调用）

**副作用**：
- 订阅 `card.OnEvent`
- 分配唯一 `Index`
- 缓存到 `_cardMap` 和 `_idIndexes`

---

##### RemoveCard
```csharp
public CardEngine RemoveCard(Card c)
```
移除卡牌，取消事件订阅与索引。

**参数**：

| 参数 | 类型 | 说明 |
|------|------|------|
| `c` | `Card` | 卡牌实例 |

**返回值**：返回引擎实例（支持链式调用）

---

##### GetCardByKey
```csharp
public Card GetCardByKey(string id, int index)
```
按 ID 和 Index 精确查找卡牌。

**参数**：

| 参数 | 类型 | 说明 |
|------|------|------|
| `id` | `string` | 卡牌 ID |
| `index` | `int` | 实例索引 |

**返回值**：
- **成功**：卡牌实例
- **失败**：`null`

---

##### GetCardById
```csharp
public Card GetCardById(string id)
```
按 ID 返回第一个已注册卡牌。

**参数**：

| 参数 | 类型 | 说明 |
|------|------|------|
| `id` | `string` | 卡牌 ID |

**返回值**：
- **成功**：第一个匹配的卡牌实例
- **失败**：`null`

---

##### GetCardsById
```csharp
public IEnumerable<Card> GetCardsById(string id)
```
按 ID 返回所有已注册卡牌。

**参数**：

| 参数 | 类型 | 说明 |
|------|------|------|
| `id` | `string` | 卡牌 ID |

**返回值**：卡牌集合（可能为空）

---

### CardFactory 类

**命名空间**：`EasyPack.EmeCardSystem`

**描述**：按 ID 创建卡牌实例的工厂。

#### 方法

##### Register(string, Func<Card>)
```csharp
public void Register(string id, Func<Card> ctor)
```
注册卡牌构造函数。

**参数**：

| 参数 | 类型 | 说明 |
|------|------|------|
| `id` | `string` | 卡牌 ID |
| `ctor` | `Func<Card>` | 构造函数委托 |

**异常**：
- `ArgumentNullException`：`id` 或 `ctor` 为 `null`

**示例**：
```csharp
factory.Register("sword", () => new Card(
    new CardData("sword", "铁剑"),
    new GameProperty("攻击力", 50f),
    "武器"
));
```

---

##### Register(IReadOnlyDictionary<string, Func<Card>>)
```csharp
public void Register(IReadOnlyDictionary<string, Func<Card>> productionList)
```
批量注册卡牌构造函数。

**参数**：

| 参数 | 类型 | 说明 |
|------|------|------|
| `productionList` | `IReadOnlyDictionary<string, Func<Card>>` | ID 到构造函数的映射 |

---

##### Create
```csharp
public Card Create(string id)
```
按 ID 创建卡牌实例（`Card` 类型）。

**参数**：

| 参数 | 类型 | 说明 |
|------|------|------|
| `id` | `string` | 卡牌 ID |

**返回值**：
- **成功**：新创建的卡牌实例
- **失败**：`null`（未注册或 ID 为空）

---

##### Create<T>
```csharp
public T Create<T>(string id) where T : Card
```
按 ID 创建卡牌实例（泛型版本）。

**类型参数**：
- `T`：卡牌类型

**返回值**：
- **成功**：新创建的卡牌实例（`T` 类型）
- **失败**：`null`

---

### CardRule 类

**命名空间**：`EasyPack.EmeCardSystem`

**描述**：数据驱动的卡牌规则。

#### 字段

##### Trigger
```csharp
public CardEventType Trigger
```
事件触发类型。

---

##### CustomId
```csharp
public string CustomId
```
自定义事件 ID（仅当 `Trigger = CardEventType.Custom` 时生效）。

---

##### OwnerHops
```csharp
public int OwnerHops = 1
```
容器锚点选择：
- `0`：Self（触发源自身）
- `1`：Owner（触发源的父级，默认）
- `N > 1`：向上 N 层
- `-1`：Root（根容器）

---

##### MaxDepth
```csharp
public int MaxDepth = int.MaxValue
```
递归选择的最大深度（仅对递归类 TargetScope 生效）。

---

##### Priority
```csharp
public int Priority = 0
```
规则优先级（数值越小优先级越高）。当引擎 Policy 选择模式为 `Priority` 时生效。

---

##### Requirements
```csharp
public List<IRuleRequirement> Requirements = new()
```
匹配条件集合（与关系）。

---

##### Effects
```csharp
public List<IRuleEffect> Effects = new()
```
命中后执行的效果管线。

---

##### Policy
```csharp
public RulePolicy Policy { get; set; } = new RulePolicy()
```
规则执行策略。

---

### CardRuleContext 类

**命名空间**：`EasyPack.EmeCardSystem`

**描述**：规则执行上下文，为效果提供触发源、容器与原始事件等信息。

#### 构造函数

##### CardRuleContext
```csharp
public CardRuleContext(Card source, Card container, CardEvent evt, 
                       ICardFactory factory, int maxDepth)
```

**参数**：

| 参数 | 类型 | 说明 |
|------|------|------|
| `source` | `Card` | 触发规则的卡牌（事件源） |
| `container` | `Card` | 用于匹配与执行的容器 |
| `evt` | `CardEvent` | 原始事件载体 |
| `factory` | `ICardFactory` | 产卡工厂 |
| `maxDepth` | `int` | 递归搜索最大深度 |

---

#### 属性

##### Source
```csharp
public Card Source { get; }
```
触发该规则的卡牌（事件源）（只读）。

---

##### Container
```csharp
public Card Container { get; }
```
用于匹配与执行的容器（只读）。

---

##### Event
```csharp
public CardEvent Event { get; }
```
原始事件载体（只读）。

---

##### Factory
```csharp
public ICardFactory Factory { get; }
```
产卡工厂（只读）。

---

##### MaxDepth
```csharp
public int MaxDepth { get; }
```
递归搜索最大深度（只读）。

---

##### DeltaTime
```csharp
public float DeltaTime { get; }
```
从 Tick 事件中获取时间增量。仅当事件类型为 `Tick` 且数据为 `float` 时返回有效值，否则返回 `0`。

---

##### EventId
```csharp
public string EventId { get; }
```
获取事件的 ID（只读）。

---

##### DataCard
```csharp
public Card DataCard { get; }
```
将事件数据作为 `Card` 类型返回（失败返回 `null`）（只读）。

---

#### 方法

##### DataCardAs<T>
```csharp
public T DataCardAs<T>() where T : Card
```
将事件数据作为指定 `Card` 子类型返回。

**类型参数**：
- `T`：目标卡牌类型

**返回值**：
- **成功**：转换后的卡牌对象
- **失败**：`null`

---

##### GetSource<T>
```csharp
public T GetSource<T>() where T : Card
```
将触发源卡牌转换为指定类型。

---

##### GetContainer<T>
```csharp
public T GetContainer<T>() where T : Card
```
将容器卡牌转换为指定类型。

---

##### DataAs<T>
```csharp
public T DataAs<T>() where T : class
```
将事件数据作为指定引用类型返回。

---

## 规则组件接口

### IRuleRequirement 接口

**命名空间**：`EasyPack.EmeCardSystem`

**描述**：规则匹配的"要求项"抽象。

#### 方法

##### TryMatch
```csharp
bool TryMatch(CardRuleContext ctx, out List<Card> matched)
```
在给定上下文下尝试匹配。

**参数**：

| 参数 | 类型 | 说明 |
|------|------|------|
| `ctx` | `CardRuleContext` | 规则上下文 |
| `matched` | `out List<Card>` | 本要求项匹配到的卡集合（可为空） |

**返回值**：
- `true`：匹配成功
- `false`：匹配失败

---

### IRuleEffect 接口

**命名空间**：`EasyPack.EmeCardSystem`

**描述**：规则效果接口。

#### 方法

##### Execute
```csharp
void Execute(CardRuleContext ctx, IReadOnlyList<Card> matched)
```
执行效果。

**参数**：

| 参数 | 类型 | 说明 |
|------|------|------|
| `ctx` | `CardRuleContext` | 规则上下文 |
| `matched` | `IReadOnlyList<Card>` | 匹配结果 |

---

### ITargetSelection 接口

**命名空间**：`EasyPack.EmeCardSystem`

**描述**：目标选择配置接口。

#### 属性

##### Root
```csharp
SelectionRoot Root { get; set; }
```
目标选择起点（默认 `Container`）。

---

##### Scope
```csharp
TargetScope Scope { get; set; }
```
选择范围（默认 `Matched`）。

---

##### Filter
```csharp
CardFilterMode Filter { get; set; }
```
过滤模式（默认 `None`）。

---

##### FilterValue
```csharp
string FilterValue { get; set; }
```
目标过滤值。

---

##### Take
```csharp
int? Take { get; set; }
```
仅作用前 N 个目标（`null` 表示不限制）。

---

##### MaxDepth
```csharp
int? MaxDepth { get; set; }
```
递归深度限制。

---

## 内置要求项

### CardsRequirement 类

**命名空间**：`EasyPack.EmeCardSystem`

**描述**：选择器式要求项，根据根、范围、过滤条件选择目标。

#### 字段

##### Root
```csharp
public SelectionRoot Root = SelectionRoot.Container
```
选择起点（默认 `Container`）。

---

##### Scope
```csharp
public TargetScope Scope = TargetScope.Children
```
选择范围（默认 `Children`）。

---

##### FilterMode
```csharp
public CardFilterMode FilterMode = CardFilterMode.None
```
过滤模式（默认 `None`）。

---

##### FilterValue
```csharp
public string FilterValue
```
过滤值（当 `FilterMode` 为 `ByTag`/`ById`/`ByCategory` 时填写）。

---

##### MinCount
```csharp
public int MinCount = 1
```
至少需要命中的数量（默认 `1`，`<=0` 视为无需命中）。

---

##### MaxMatched
```csharp
public int MaxMatched = -1
```
返回给效果的最大卡牌数量：
- `-1`（默认）：使用 `MinCount` 作为上限
- `0`：返回所有选中卡牌
- `> 0`：返回指定数量

---

##### MaxDepth
```csharp
public int? MaxDepth = null
```
递归深度限制（仅对 `Scope=Descendants` 生效，`null` 或 `<=0` 表示不限制）。

---

### ConditionRequirement 类

**命名空间**：`EasyPack.EmeCardSystem`

**描述**：自定义条件要求，使用布尔函数进行校验。

#### 构造函数

##### ConditionRequirement
```csharp
public ConditionRequirement(Func<CardRuleContext, bool> condition)
```

**参数**：

| 参数 | 类型 | 说明 |
|------|------|------|
| `condition` | `Func<CardRuleContext, bool>` | 条件函数 |

**异常**：
- `ArgumentNullException`：`condition` 为 `null`

**示例**：
```csharp
var req = new ConditionRequirement(ctx => ctx.Source.HasTag("玩家"));
```

---

## 内置效果

### CreateCardsEffect 类

**命名空间**：`EasyPack.EmeCardSystem`

**描述**：产卡效果，在上下文容器中创建指定 ID 的新卡。

#### 属性

##### CardIds
```csharp
public List<string> CardIds { get; set; } = new List<string>()
```
要创建的卡牌 ID 列表。

---

##### CountPerId
```csharp
public int CountPerId { get; set; } = 1
```
每个 ID 的创建数量（默认 `1`；`<=0` 时不创建）。

---

### RemoveCardsEffect 类

**命名空间**：`EasyPack.EmeCardSystem`

**描述**：移除卡牌效果，将符合条件的目标卡牌从容器中移除。

**实现接口**：`IRuleEffect`, `ITargetSelection`

#### 属性

参见 [ITargetSelection](#itargetselection-接口)。

**默认值**：
- `Root`：`SelectionRoot.Container`
- `Scope`：`TargetScope.Matched`
- `Filter`：`CardFilterMode.None`

**注意**：固有子卡（intrinsic）不会被移除。

---

### ModifyPropertyEffect 类

**命名空间**：`EasyPack.EmeCardSystem`

**描述**：修改属性效果，对目标卡的 `GameProperty` 进行数值或修饰符层面的调整。

**实现接口**：`IRuleEffect`, `ITargetSelection`

#### 属性

##### PropertyName
```csharp
public string PropertyName { get; set; } = ""
```
要修改的属性名（留空代表全部属性）。

---

##### ApplyMode
```csharp
public Mode ApplyMode { get; set; } = Mode.AddToBase
```
应用模式：
- `AddModifier`：添加修饰符
- `RemoveModifier`：移除修饰符
- `AddToBase`：对基础值加上 `Value`
- `SetBase`：将基础值设为 `Value`

---

##### Value
```csharp
public float Value { get; set; } = 0f
```
数值参数（用于 `AddToBase`/`SetBase` 模式）。

---

##### Modifier
```csharp
public IModifier Modifier { get; set; }
```
修饰符（用于 `AddModifier`/`RemoveModifier` 模式）。

---

### AddTagEffect 类

**命名空间**：`EasyPack.EmeCardSystem`

**描述**：添加标签效果，为目标卡添加指定的标签。

**实现接口**：`IRuleEffect`, `ITargetSelection`

#### 属性

##### Tag
```csharp
public string Tag { get; set; }
```
要添加的标签文本（非空时才会尝试添加）。

---

### InvokeEffect 类

**命名空间**：`EasyPack.EmeCardSystem`

**描述**：委托调用效果，在效果阶段执行一段自定义委托逻辑。

#### 构造函数

##### InvokeEffect
```csharp
public InvokeEffect(Action<CardRuleContext, IReadOnlyList<Card>> action)
```

**参数**：

| 参数 | 类型 | 说明 |
|------|------|------|
| `action` | `Action<CardRuleContext, IReadOnlyList<Card>>` | 要执行的委托 |

**示例**：
```csharp
var effect = new InvokeEffect((ctx, matched) =>
{
    Debug.Log($"匹配到 {matched.Count} 个卡牌");
});
```

---

## 工具类

### CardRuleBuilder 类

**命名空间**：`EasyPack.EmeCardSystem`

**描述**：规则流式构建器，提供链式 API 构建规则。

#### 核心方法

##### On
```csharp
public CardRuleBuilder On(CardEventType eventType, string customId = null)
```
设置事件触发类型。

**示例**：
```csharp
builder.On(CardEventType.Use)
builder.On(CardEventType.Custom, "攻击")
```

---

##### OwnerHops
```csharp
public CardRuleBuilder OwnerHops(int hops)
```
设置容器锚点。

---

##### When
```csharp
public CardRuleBuilder When(Func<CardRuleContext, bool> predicate)
```
添加条件判断。

---

##### Need
```csharp
public CardRuleBuilder Need(SelectionRoot root, TargetScope scope, 
                             CardFilterMode filter = CardFilterMode.None,
                             string filterValue = null, int minCount = 1, 
                             int maxMatched = -1, int? maxDepth = null)
```
添加卡牌需求。

---

##### Do
```csharp
public CardRuleBuilder Do(IRuleEffect effect)
```
添加自定义效果。

---

#### 便捷方法

##### 容器锚点

- `AtSelf()`：以自身为容器（`OwnerHops=0`）
- `AtParent()`：以直接父级为容器（`OwnerHops=1`）
- `AtRoot()`：以根容器为容器（`OwnerHops=-1`）

##### 条件语法糖

- `WhenSourceHasTag(string tag)`：要求源卡有指定标签
- `WhenSourceId(string id)`：要求源卡的 ID 为指定值
- `WhenContainerHasTag(string tag)`：要求容器有指定标签
- `WhenEventDataIs<T>()`：要求事件数据为指定类型

##### 要求项语法糖

- `NeedTag(string tag, int minCount = 1, int maxMatched = -1)`：需要容器的直接子卡中有指定标签
- `NeedId(string id, int minCount = 1, int maxMatched = -1)`：需要容器的直接子卡中有指定 ID
- `NeedTagRecursive(string tag, ...)`：递归查找指定标签

##### 效果语法糖

- `DoCreate(string cardId, int count = 1)`：创建卡牌
- `DoRemoveByTag(string tag, int? take = null)`：移除匹配结果中指定标签的卡牌
- `DoRemoveById(string id, int? take = null)`：移除匹配结果中指定 ID 的卡牌
- `DoAddTagToMatched(string tag)`：给匹配结果添加标签
- `DoModifyMatched(string propertyName, float value, ...)`：修改匹配结果的属性
- `DoInvoke(Action<CardRuleContext, IReadOnlyList<Card>> action)`：执行自定义逻辑

完整 API 请参阅源代码 `CardRuleBuilder.cs`。

---

### TargetSelector 类

**命名空间**：`EasyPack.EmeCardSystem`

**描述**：目标选择器，根据作用域和过滤条件从上下文中选择卡牌。

#### 方法

##### Select
```csharp
public static IReadOnlyList<Card> Select(
    TargetScope scope, CardFilterMode filter, CardRuleContext ctx,
    string filterValue = null, int? maxDepth = null)
```
根据作用域和过滤条件选择目标卡牌。

**参数**：

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `scope` | `TargetScope` | - | 选择范围 |
| `filter` | `CardFilterMode` | - | 过滤模式 |
| `ctx` | `CardRuleContext` | - | 规则上下文 |
| `filterValue` | `string` | `null` | 过滤值 |
| `maxDepth` | `int?` | `null` | 递归最大深度 |

**返回值**：符合条件的卡牌列表

---

##### SelectForEffect
```csharp
public static IReadOnlyList<Card> SelectForEffect(ITargetSelection selection, CardRuleContext ctx)
```
供效果使用的选择方法，根据 `ITargetSelection` 配置选择目标。

---

##### ApplyFilter
```csharp
public static IReadOnlyList<Card> ApplyFilter(IReadOnlyList<Card> cards, 
                                                CardFilterMode filter, string filterValue)
```
对已有的卡牌列表应用过滤条件。

---

## 枚举类型

### CardCategory
```csharp
public enum CardCategory
{
    Object,      // 物品/实体类
    Attribute,   // 属性/状态类
    Action,      // 行为/动作类
    Environment  // 环境类
}
```

---

### CardEventType
```csharp
public enum CardEventType
{
    AddedToOwner,       // 向子卡分发（被添加到持有者时）
    RemovedFromOwner,   // 向子卡分发（从持有者移除时）
    Tick,               // 按时事件
    Use,                // 主动使用
    Custom              // 自定义事件
}
```

---

### SelectionRoot
```csharp
public enum SelectionRoot
{
    Container,  // 以上下文容器为根
    Source      // 以触发源为根
}
```

---

### TargetScope
```csharp
public enum TargetScope
{
    Matched,      // 来自"所有要求项"返回的匹配卡集合
    Children,     // 选定根的一层子卡（不递归）
    Descendants   // 选定根的所有后代（递归）
}
```

---

### CardFilterMode
```csharp
public enum CardFilterMode
{
    None,       // 不过滤
    ByTag,      // 按标签过滤
    ById,       // 按 ID 过滤
    ByCategory  // 按类别过滤
}
```

---

### RuleSelectionMode
```csharp
public enum RuleSelectionMode
{
    RegistrationOrder,  // 按注册顺序
    Priority            // 按规则优先级（数值越小优先）
}
```

---

### ModifyPropertyEffect.Mode
```csharp
public enum Mode
{
    AddModifier,    // 添加修饰符
    RemoveModifier, // 移除修饰符
    AddToBase,      // 对基础值加上 Value
    SetBase         // 将基础值设为 Value
}
```

---

**维护者**：NEKOPACK 团队  
**联系方式**：提交 GitHub Issue 或 Pull Request  
**许可证**：遵循项目主许可证
