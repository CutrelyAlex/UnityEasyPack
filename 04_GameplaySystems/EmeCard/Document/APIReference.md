# EmeCard 系统 - API 参考文档

**适用 EasyPack 版本：** EasyPack v1.7.0  
**最后更新：** 2025-11-09

---

## 目录

1. [核心类](#核心类)
   - [Card](#card-类)
   - [CardData](#carddata-类)
   - [CardEvent](#cardevent-结构体)
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
   - [AllRequirement](#allrequirement-类)
   - [AnyRequirement](#anyrequirement-类)
   - [NotRequirement](#notrequirement-类)
4. [内置效果](#内置效果)
   - [CreateCardsEffect](#createcardseffect-类)
   - [RemoveCardsEffect](#removecardseffect-类)
   - [ModifyPropertyEffect](#modifypropertyeffect-类)
   - [AddTagEffect](#addtageffect-类)
   - [InvokeEffect](#invokeeffect-类)
5. [工具类](#工具类)
   - [CardRuleBuilder](#cardrulebuilder-类)
   - [RuleRegistrationExtensions](#ruleregistrationextensions-类)
   - [TargetSelector](#targetselector-类)
   - [RulePolicy](#rulepolicy-类)
   - [EnginePolicy](#enginepolicy-类)
6. [枚举类型](#枚举类型)

---

## 核心类

### Card 类

**命名空间：** `EasyPack.EmeCardSystem`

**描述：** 卡牌实例，系统的基本单元。可持有子卡牌、关联属性、携带标签，并通过事件驱动规则引擎。

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

**参数：**

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `data` | `CardData` | - | 卡牌静态数据 |
| `gameProperty` | `GameProperty` | `null` | 可选的单个游戏属性 |
| `extraTags` | `string[]` | - | 额外标签（除默认标签外） |

**示例：**
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

**参数：**

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `data` | `CardData` | - | 卡牌静态数据 |
| `properties` | `IEnumerable<GameProperty>` | - | 属性列表（`null` 时创建空列表） |
| `extraTags` | `string[]` | - | 额外标签 |

**示例：**
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
简化构造函数，仅提供卡牌数据和标签（无属性）。

**参数：**

| 参数 | 类型 | 说明 |
|------|------|------|
| `data` | `CardData` | 卡牌静态数据 |
| `extraTags` | `string[]` | 额外标签 |

**示例：**
```csharp
var card = new Card(data, "武器", "近战");
```

---

#### 属性

##### Data
```csharp
public CardData Data { get; set; }
```
卡牌的静态数据（ID/名称/描述/默认标签等）。

**类型：** `CardData`

**注意：** 赋值时会清空并重新加载默认标签。

---

##### Index
```csharp
public int Index { get; set; }
```
实例索引，用于区分同一 ID 的多个实例。

**类型：** `int`  
**默认值：** `0`

**说明：** 由引擎在 `AddCard` 时分配，从 0 起。

---

##### Id
```csharp
public string Id { get; }
```
卡牌标识（只读），来自 `Data.ID`。

**类型：** `string`  
**返回值：** 卡牌 ID，若 `Data` 为 `null` 返回空字符串

---

##### Name
```csharp
public string Name { get; }
```
卡牌显示名称（只读），来自 `Data.Name`。

**类型：** `string`

---

##### Description
```csharp
public string Description { get; }
```
卡牌描述（只读），来自 `Data.Description`。

**类型：** `string`

---

##### Category
```csharp
public CardCategory Category { get; }
```
卡牌类别（只读），来自 `Data.Category`。

**类型：** `CardCategory`  
**返回值：** 若 `Data` 为 `null` 返回 `CardCategory.Object`

---

##### Properties
```csharp
public List<GameProperty> Properties { get; set; }
```
数值属性列表。

**类型：** `List<GameProperty>`

---

##### Tags
```csharp
public IReadOnlyCollection<string> Tags { get; }
```
标签集合（只读）。

**类型：** `IReadOnlyCollection<string>`

**说明：** 标签用于规则匹配，大小写敏感。

---

##### Owner
```csharp
public Card Owner { get; }
```
当前卡牌的持有者（父卡牌），只读。

**类型：** `Card`  
**返回值：** 持有者实例，若无持有者返回 `null`

---

##### Children
```csharp
public IReadOnlyList<Card> Children { get; }
```
子卡牌列表（只读视图）。

**类型：** `IReadOnlyList<Card>`

---

##### ChildrenCount
```csharp
public int ChildrenCount { get; }
```
子卡牌数量（只读）。

**类型：** `int`

---

##### Intrinsics
```csharp
public IReadOnlyList<Card> Intrinsics { get; }
```
固有子卡牌列表（只读视图）。

**类型：** `IReadOnlyList<Card>`

**说明：** 固有子卡不可被规则消耗或普通移除（需 `force=true`）。

---

#### 方法

##### GetProperty
```csharp
public GameProperty GetProperty(string id)
```
根据 ID 获取属性。

**参数：**

| 参数 | 类型 | 说明 |
|------|------|------|
| `id` | `string` | 属性 ID |

**返回值：**
- **成功：** 匹配的 `GameProperty` 实例
- **失败：** `null`（未找到）

**示例：**
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

**参数：**

| 参数 | 类型 | 说明 |
|------|------|------|
| `tag` | `string` | 标签文本 |

**返回值：**
- `true`：包含该标签
- `false`：不包含或标签为空

---

##### AddTag
```csharp
public bool AddTag(string tag)
```
添加一个标签。

**参数：**

| 参数 | 类型 | 说明 |
|------|------|------|
| `tag` | `string` | 标签文本 |

**返回值：**
- `true`：成功新增（之前不存在）
- `false`：已存在或标签为空

---

##### RemoveTag
```csharp
public bool RemoveTag(string tag)
```
移除一个标签。

**参数：**

| 参数 | 类型 | 说明 |
|------|------|------|
| `tag` | `string` | 标签文本 |

**返回值：**
- `true`：成功移除
- `false`：不存在或标签为空

---

##### AddChild
```csharp
public Card AddChild(Card child, bool intrinsic = false)
```
将子卡牌加入当前卡牌作为持有者。

**参数：**

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `child` | `Card` | - | 子卡牌实例 |
| `intrinsic` | `bool` | `false` | 是否作为"固有子卡"（无法被规则消耗或移除） |

**返回值：** 返回当前卡牌实例（支持链式调用）

**异常：**
- `ArgumentNullException`：`child` 为 `null`
- `InvalidOperationException`：子卡已被其他卡牌持有
- `Exception`：尝试添加自身为子卡

**副作用：** 向子卡派发 `CardEventType.AddedToOwner` 事件

**示例：**
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

**参数：**

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `child` | `Card` | - | 要移除的子卡牌 |
| `force` | `bool` | `false` | 是否强制移除（`true` 时可移除固有子卡） |

**返回值：**
- `true`：移除成功
- `false`：移除失败（子卡不存在或为固有子卡且 `force=false`）

**副作用：** 向子卡派发 `CardEventType.RemovedFromOwner` 事件

---

##### IsIntrinsic
```csharp
public bool IsIntrinsic(Card child)
```
判断某子卡是否为固有子卡。

**参数：**

| 参数 | 类型 | 说明 |
|------|------|------|
| `child` | `Card` | 要检查的子卡 |

**返回值：**
- `true`：是固有子卡
- `false`：不是或 `child` 为 `null`

---

##### IsChild
```csharp
public bool IsChild(Card child)
```
判断某卡牌是否为当前卡牌的子卡。

**参数：**

| 参数 | 类型 | 说明 |
|------|------|------|
| `child` | `Card` | 要检查的卡牌 |

**返回值：**
- `true`：是子卡
- `false`：不是或 `child` 为 `null`

---

##### IsRecursiveParent
```csharp
public bool IsRecursiveParent(Card potentialChild)
```
检测传入卡牌是否是当前卡牌的祖先卡牌（或自身）。

**参数：**

| 参数 | 类型 | 说明 |
|------|------|------|
| `potentialChild` | `Card` | 要检查的卡牌 |

**返回值：**
- `true`：`potentialChild` 是当前卡牌的祖先或自身
- `false`：不是或 `potentialChild` 为 `null`

**说明：** 用于防止循环依赖。若 `potentialChild == this` 返回 `true`。

---

##### RaiseEvent
```csharp
public void RaiseEvent(CardEvent evt)
```
分发一个卡牌事件到 `OnEvent`。

**参数：**

| 参数 | 类型 | 说明 |
|------|------|------|
| `evt` | `CardEvent` | 事件载体 |

---

##### Tick
```csharp
public void Tick(float deltaTime)
```
触发按时事件（`CardEventType.Tick`）。

**参数：**

| 参数 | 类型 | 说明 |
|------|------|------|
| `deltaTime` | `float` | 时间步长（秒） |

**示例：**
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

**参数：**

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `data` | `object` | `null` | 可选自定义信息（如目标） |

---

##### Custom
```csharp
public void Custom(string id, object data = null)
```
触发自定义事件（`CardEventType.Custom`）。

**参数：**

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
卡牌统一事件回调。

**参数：**
- `Card`：触发事件的卡牌
- `CardEvent`：事件载体

**说明：** 订阅者（如规则引擎）可监听以实现规则匹配与效果执行。

**示例：**
```csharp
card.OnEvent += (source, evt) =>
{
    Debug.Log($"卡牌 {source.Id} 触发了 {evt.Type} 事件");
};
```

---

### CardData 类

**命名空间：** `EasyPack.EmeCardSystem`

**描述：** 卡牌的静态数据，不包含运行时状态。

#### 构造函数

##### CardData
```csharp
public CardData(string id, string name = "Default", string desc = "",
                CardCategory category = CardCategory.Object, 
                string[] defaultTags = null, Sprite sprite = null)
```

**参数：**

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `id` | `string` | - | 逻辑 ID（建议全局唯一） |
| `name` | `string` | `"Default"` | 展示名（可本地化） |
| `desc` | `string` | `""` | 描述文本 |
| `category` | `CardCategory` | `CardCategory.Object` | 卡牌类别 |
| `defaultTags` | `string[]` | `null` | 默认标签（`null` 时使用空数组） |
| `sprite` | `Sprite` | `null` | 卡牌图标（`null` 时从 Resources 加载） |

**示例：**
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
默认标签集合（只读）。

**注意：** 应视为只读元数据，不建议运行时修改。

---

### CardEvent 结构体

**命名空间:** `EasyPack.EmeCardSystem`

**描述:** 卡牌事件载体（只读结构体）。

#### 构造函数

##### CardEvent
```csharp
public CardEvent(CardEventType type, string id = null, object data = null)
```

**参数:**

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `type` | `CardEventType` | - | 事件类型 |
| `id` | `string` | `null` | 事件 ID（用于 `Custom` 类型） |
| `data` | `object` | `null` | 附加数据 |

**示例:**
```csharp
// 创建 Tick 事件
var tickEvent = new CardEvent(CardEventType.Tick, data: 0.016f);

// 创建自定义事件
var customEvent = new CardEvent(CardEventType.Custom, "OnLevelUp", data: 5);
```

---

#### 属性

##### Type
```csharp
public CardEventType Type { get; }
```
事件类型（只读）。

**类型:** `CardEventType`

---

##### ID
```csharp
public string ID { get; }
```
事件 ID（只读），用于 `Custom` 事件类型。

**类型:** `string`

---

##### Data
```csharp
public object Data { get; }
```
附加数据（只读），可存储任意类型的上下文信息。

**类型:** `object`

---

### CardEngine 类

**命名空间：** `EasyPack.EmeCardSystem`

**描述：** 卡牌引擎，管理卡牌实例、规则注册、事件分发。

#### 构造函数

##### CardEngine
```csharp
public CardEngine(ICardFactory factory)
```

**参数：**

| 参数 | 类型 | 说明 |
|------|------|------|
| `factory` | `ICardFactory` | 卡牌工厂实例 |

**示例：**
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

**参数：**

| 参数 | 类型 | 说明 |
|------|------|------|
| `rule` | `CardRule` | 规则实例 |

**异常：**
- `ArgumentNullException`：`rule` 为 `null`

---

##### RegisterRule(Func<CardRuleBuilder, CardRuleBuilder>)
```csharp
public void RegisterRule(Func<CardRuleBuilder, CardRuleBuilder> builder)
```
使用流式构建器注册规则（扩展方法）。

**参数：**

| 参数 | 类型 | 说明 |
|------|------|------|
| `builder` | `Func<CardRuleBuilder, CardRuleBuilder>` | 构建器委托 |

**示例：**
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

**参数：**

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `maxEvents` | `int` | `2048` | 最大处理事件数（防止死循环） |

**副作用：** 处理主队列和延迟队列中的事件

---

##### CreateCard
```csharp
public Card CreateCard(string id)
```
按 ID 创建并注册卡牌实例（`Card` 类型）。

**参数：**

| 参数 | 类型 | 说明 |
|------|------|------|
| `id` | `string` | 卡牌 ID |

**返回值：**
- **成功：** 创建的卡牌实例
- **失败：** `null`（工厂中未注册该 ID）

**异常：**
- `ArgumentNullException`：`id` 为 `null`

---

##### CreateCard<T>
```csharp
public T CreateCard<T>(string id) where T : Card
```
按 ID 创建并注册卡牌实例（泛型版本）。

**类型参数：**
- `T`：卡牌类型（必须继承自 `Card`）

**返回值：**
- **成功：** 创建的卡牌实例（`T` 类型）
- **失败：** `null`

---

##### AddCard
```csharp
public Card AddCard(Card card)
```
将已存在的卡牌实例注册到引擎。

**参数:**

| 参数 | 类型 | 说明 |
|------|------|------|
| `card` | `Card` | 卡牌实例 |

**返回值:** 返回传入的卡牌实例（支持链式调用）

**异常:**
- `ArgumentNullException`: `card` 为 `null`

**副作用:** 
- 自动分配 `Index` 值
- 订阅卡牌的 `OnEvent` 事件
- 触发 `CardEventType.Added` 事件

**示例:**
```csharp
var card = new Card(data);
engine.AddCard(card);
```

---

##### RemoveCard
```csharp
public CardEngine RemoveCard(Card card)
```
从引擎移除卡牌实例。

**参数:**

| 参数 | 类型 | 说明 |
|------|------|------|
| `card` | `Card` | 要移除的卡牌 |

**返回值:** 返回引擎实例（支持链式调用）

**副作用:**
- 取消订阅 `OnEvent` 事件
- 清理卡牌索引映射

---

##### GetCardByKey
```csharp
public Card GetCardByKey(string id, int index)
```
按 ID 和索引精确查找卡牌。

**参数:**

| 参数 | 类型 | 说明 |
|------|------|------|
| `id` | `string` | 卡牌 ID |
| `index` | `int` | 实例索引 |

**返回值:**
- **成功:** 匹配的卡牌实例
- **失败:** `null`（未找到或 ID 为空）

**示例:**
```csharp
var card = engine.GetCardByKey("sword", 0);
```

---

##### GetCardsById
```csharp
public IEnumerable<Card> GetCardsById(string id)
```
按 ID 获取所有匹配的卡牌实例（迭代器）。

**参数:**

| 参数 | 类型 | 说明 |
|------|------|------|
| `id` | `string` | 卡牌 ID |

**返回值:** 匹配的卡牌序列（惰性求值）

---

##### GetCardById
```csharp
public Card GetCardById(string id)
```
按 ID 获取第一个匹配的卡牌实例。

**参数:**

| 参数 | 类型 | 说明 |
|------|------|------|
| `id` | `string` | 卡牌 ID |

**返回值:** 
- **成功:** 第一个匹配的卡牌实例
- **失败:** `null`（未找到或 ID 为空）

---

### CardFactory 类

**命名空间:** `EasyPack.EmeCardSystem`

**描述:** 卡牌工厂，负责注册卡牌构造函数并按需创建卡牌实例。

#### 接口

##### ICardFactory
```csharp
public interface ICardFactory
{
    Card Create(string id);
    T Create<T>(string id) where T : Card;
    CardEngine Owner { get; set; }
}
```

---

#### 构造函数

##### CardFactory
```csharp
public CardFactory()
```
创建一个空的卡牌工厂实例。

---

#### 属性

##### Owner
```csharp
public CardEngine Owner { get; set; }
```
关联的卡牌引擎实例。

**类型:** `CardEngine`

---

#### 方法

##### Register(string, Func<Card>)
```csharp
public void Register(string id, Func<Card> ctor)
```
注册卡牌构造函数。

**参数:**

| 参数 | 类型 | 说明 |
|------|------|------|
| `id` | `string` | 卡牌 ID |
| `ctor` | `Func<Card>` | 卡牌构造函数委托 |

**异常:**
- `ArgumentNullException`: `id` 为空或 `ctor` 为 `null`

**示例:**
```csharp
var factory = new CardFactory();
factory.Register("sword", () => 
    new Card(new CardData("sword", "铁剑"), "武器"));
```

---

##### Register(IReadOnlyDictionary<string, Func<Card>>)
```csharp
public void Register(IReadOnlyDictionary<string, Func<Card>> productionList)
```
批量注册卡牌构造函数。

**参数:**

| 参数 | 类型 | 说明 |
|------|------|------|
| `productionList` | `IReadOnlyDictionary<string, Func<Card>>` | 卡牌 ID 到构造函数的映射 |

**示例:**
```csharp
var dict = new Dictionary<string, Func<Card>>
{
    ["sword"] = () => new Card(new CardData("sword", "剑")),
    ["shield"] = () => new Card(new CardData("shield", "盾"))
};
factory.Register(dict);
```

---

##### Create
```csharp
public Card Create(string id)
```
按 ID 创建卡牌实例（`Card` 类型）。

**参数:**

| 参数 | 类型 | 说明 |
|------|------|------|
| `id` | `string` | 卡牌 ID |

**返回值:**
- **成功:** 新创建的卡牌实例
- **失败:** `null`（ID 未注册或为空）

---

##### Create<T>
```csharp
public T Create<T>(string id) where T : Card
```
按 ID 创建卡牌实例（泛型版本）。

**类型参数:**
- `T`: 卡牌类型（必须继承自 `Card`）

**返回值:**
- **成功:** 新创建的卡牌实例（`T` 类型）
- **失败:** `null`（ID 未注册、类型不匹配或为空）

**示例:**
```csharp
public class HeroCard : Card { }

factory.Register("hero", () => new HeroCard());
var hero = factory.Create<HeroCard>("hero");
```

---

### CardRule 类

**命名空间:** `EasyPack.EmeCardSystem`

**描述:** 数据驱动的卡牌规则，定义触发条件、要求项和效果。

#### 字段

##### Trigger
```csharp
public CardEventType Trigger
```
事件触发类型。

**类型:** `CardEventType`

---

##### CustomId
```csharp
public string CustomId
```
自定义事件 ID（仅当 `Trigger` 为 `Custom` 时生效）。

**类型:** `string`

---

##### OwnerHops
```csharp
public int OwnerHops = 1
```
容器锚点选择策略。

**类型:** `int`  
**默认值:** `1`（父级）

**说明:**
- `0`: 触发卡牌自身
- `1`: 直接父级（默认）
- `>1`: 向上 N 层
- `-1`: 根容器

---

##### MaxDepth
```csharp
public int MaxDepth = int.MaxValue
```
递归选择的最大深度。

**类型:** `int`  
**默认值:** `int.MaxValue`（无限制）

---

##### Priority
```csharp
public int Priority = 0
```
规则优先级（数值越小优先级越高）。

**类型:** `int`  
**默认值:** `0`

**说明:** 当引擎 `Policy.RuleSelection` 为 `Priority` 时生效。

---

##### Requirements
```csharp
public List<IRuleRequirement> Requirements
```
匹配条件集合（与关系：所有条件必须同时满足）。

**类型:** `List<IRuleRequirement>`

---

##### Effects
```csharp
public List<IRuleEffect> Effects
```
命中后执行的效果管线。

**类型:** `List<IRuleEffect>`

---

##### Policy
```csharp
public RulePolicy Policy { get; set; }
```
规则执行策略。

**类型:** `RulePolicy`  
**默认值:** 新 `RulePolicy` 实例

---

### CardRuleContext 类

**命名空间:** `EasyPack.EmeCardSystem`

**描述:** 规则执行上下文，为效果提供触发源、容器与原始事件等信息。

#### 构造函数

##### CardRuleContext
```csharp
public CardRuleContext(Card source, Card container, CardEvent evt, 
                      ICardFactory factory, int maxDepth)
```

**参数:**

| 参数 | 类型 | 说明 |
|------|------|------|
| `source` | `Card` | 触发该规则的卡牌（事件源） |
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
触发该规则的卡牌（事件源）。

**类型:** `Card`（只读）

---

##### Container
```csharp
public Card Container { get; }
```
用于匹配与执行的容器（由规则的 `OwnerHops` 选择）。

**类型:** `Card`（只读）

---

##### Event
```csharp
public CardEvent Event { get; }
```
原始事件载体（包含类型、ID、数据等）。

**类型:** `CardEvent`（只读）

---

##### Factory
```csharp
public ICardFactory Factory { get; }
```
产卡工厂。

**类型:** `ICardFactory`（只读）

---

##### MaxDepth
```csharp
public int MaxDepth { get; }
```
递归搜索最大深度（`>0` 生效，`1` 表示仅子级一层）。

**类型:** `int`（只读）

---

##### DeltaTime
```csharp
public float DeltaTime { get; }
```
从 `Tick` 事件中获取时间增量。

**类型:** `float`（只读）  
**返回值:** 仅当事件类型为 `Tick` 且数据为 `float` 时返回有效值，否则返回 `0`

**示例:**
```csharp
.DoInvoke((ctx, matched) =>
{
    if (ctx.Event.Type == CardEventType.Tick)
    {
        Debug.Log($"经过了 {ctx.DeltaTime} 秒");
    }
})
```

---

##### EventId
```csharp
public string EventId { get; }
```
获取事件的 ID（只读）。

**类型:** `string`

---

##### DataCard
```csharp
public Card DataCard { get; }
```
将事件数据作为 `Card` 类型返回。

**类型:** `Card`（只读）  
**返回值:** 转换成功返回卡牌实例，失败返回 `null`

---

#### 方法

##### DataCardAs<T>
```csharp
public T DataCardAs<T>() where T : Card
```
将事件数据作为指定 `Card` 子类型返回。

**类型参数:**
- `T`: 目标卡牌类型

**返回值:** 转换后的卡牌对象，失败返回 `null`

---

##### GetSource<T>
```csharp
public T GetSource<T>() where T : Card
```
将触发源卡牌转换为指定类型。

**类型参数:**
- `T`: 目标卡牌类型

**返回值:** 转换后的卡牌对象，失败返回 `null`

---

##### GetContainer<T>
```csharp
public T GetContainer<T>() where T : Card
```
将容器卡牌转换为指定类型。

**类型参数:**
- `T`: 目标卡牌类型

**返回值:** 转换后的卡牌对象，失败返回 `null`

---

##### DataAs<T>
```csharp
public T DataAs<T>() where T : class
```
将事件数据作为指定引用类型返回。

**类型参数:**
- `T`: 目标引用类型

**返回值:** 转换后的对象，失败返回 `null`

**示例:**
```csharp
.DoInvoke((ctx, matched) =>
{
    var playerName = ctx.DataAs<string>();
    Debug.Log($"玩家名称：{playerName}");
})
```

---

##### DataAs<T>(int)
```csharp
public T DataAs<T>(int i) where T : class
```
从事件数据数组中获取指定索引的元素（引用类型）。

**参数:**

| 参数 | 类型 | 说明 |
|------|------|------|
| `i` | `int` | 数组索引 |

**返回值:** 转换后的对象，失败返回 `null`

---

##### DataIs<T>
```csharp
public T DataIs<T>() where T : struct
```
将事件数据作为指定值类型返回。

**类型参数:**
- `T`: 目标值类型

**返回值:** 转换后的值

**示例:**
```csharp
.DoInvoke((ctx, matched) =>
{
    var damage = ctx.DataIs<int>();
    Debug.Log($"伤害值：{damage}");
})
```

---

##### DataIs<T>(int)
```csharp
public T DataIs<T>(int i) where T : struct
```
从事件数据数组中获取指定索引的元素（值类型）。

**参数:**

| 参数 | 类型 | 说明 |
|------|------|------|
| `i` | `int` | 数组索引 |

**返回值:** 转换后的值

---

##### TryGetData<T>
```csharp
public bool TryGetData<T>(out T value)
```
尝试安全地获取事件数据为指定类型。

**类型参数:**
- `T`: 目标类型

**参数:**

| 参数 | 类型 | 说明 |
|------|------|------|
| `value` | `T` | 输出参数，获取成功时为转换后的值，失败时为默认值 |

**返回值:**
- `true`: 转换成功
- `false`: 转换失败

**示例:**
```csharp
.DoInvoke((ctx, matched) =>
{
    if (ctx.TryGetData<int>(out var level))
    {
        Debug.Log($"等级：{level}");
    }
})
```

---

##### TryGetData<T>(int, out T)
```csharp
public bool TryGetData<T>(int i, out T value)
```
尝试从事件数据数组中获取指定索引的元素。

**参数:**

| 参数 | 类型 | 说明 |
|------|------|------|
| `i` | `int` | 数组索引 |
| `value` | `T` | 输出参数 |

**返回值:**
- `true`: 转换成功且索引有效
- `false`: 转换失败或索引无效

---

##### ToString
```csharp
public override string ToString()
```
返回上下文的字符串表示（调试用）。

**返回值:** 包含 `Source`, `Container`, `Event`, `Factory`, `MaxDepth`, `DeltaTime` 的格式化字符串

---

## 规则组件接口

### IRuleRequirement 接口

**命名空间:** `EasyPack.EmeCardSystem`

**描述:** 规则要求项接口，用于判断规则是否满足执行条件。

#### 方法

##### TryMatch
```csharp
bool TryMatch(CardRuleContext ctx, out List<Card> matched)
```
在给定上下文下尝试匹配。

**参数:**

| 参数 | 类型 | 说明 |
|------|------|------|
| `ctx` | `CardRuleContext` | 规则上下文 |
| `matched` | `List<Card>` | 输出参数：本要求项匹配到的卡牌集合 |

**返回值:**
- `true`: 匹配成功
- `false`: 匹配失败

---

### IRuleEffect 接口

**命名空间:** `EasyPack.EmeCardSystem`

**描述:** 规则效果接口，定义规则生效时的行为。

#### 方法

##### Execute
```csharp
void Execute(CardRuleContext ctx, IReadOnlyList<Card> matched)
```
执行效果。

**参数:**

| 参数 | 类型 | 说明 |
|------|------|------|
| `ctx` | `CardRuleContext` | 规则上下文 |
| `matched` | `IReadOnlyList<Card>` | 匹配阶段的命中集合 |

---

### ITargetSelection 接口

**命名空间:** `EasyPack.EmeCardSystem`

**描述:** 目标选择接口，用于从引擎中筛选目标卡牌。

#### 方法

##### Select
```csharp
List<Card> Select(CardRuleContext context)
```
选择目标卡牌。

**参数:**

| 参数 | 类型 | 说明 |
|------|------|------|
| `context` | `CardRuleContext` | 规则上下文 |

**返回值:** 选中的卡牌列表（可能为空）

---

## 内置要求项

### CardsRequirement 类

**命名空间:** `EasyPack.EmeCardSystem.Requirements`

**描述:** 要求指定数量的特定卡牌存在。

#### 构造函数

##### CardsRequirement
```csharp
public CardsRequirement(string cardId, int count)
```

**参数:**

| 参数 | 类型 | 说明 |
|------|------|------|
| `cardId` | `string` | 目标卡牌 ID |
| `count` | `int` | 所需数量 |

**示例:**
```csharp
// 要求至少 3 个金币
var req = new CardsRequirement("金币", 3);
```

---

#### 属性

##### CardId
```csharp
public string CardId { get; set; }
```
目标卡牌 ID。

---

##### Count
```csharp
public int Count { get; set; }
```
所需数量。

---

### ConditionRequirement 类

**命名空间:** `EasyPack.EmeCardSystem.Requirements`

**描述:** 基于自定义条件函数的要求项。

#### 构造函数

##### ConditionRequirement
```csharp
public ConditionRequirement(Func<CardRuleContext, bool> condition)
```

**参数:**

| 参数 | 类型 | 说明 |
|------|------|------|
| `condition` | `Func<CardRuleContext, bool>` | 条件判断函数 |

**示例:**
```csharp
// 要求触发卡牌拥有"稀有"标签
var req = new ConditionRequirement(ctx => ctx.Source.HasTag("稀有"));
```

---

### 组合要求项

#### AllRequirement 类

**命名空间:** `EasyPack.EmeCardSystem.Requirements`

**描述:** 逻辑与组合，所有子要求必须同时满足。

##### 构造函数
```csharp
public AllRequirement(params IRuleRequirement[] requirements)
```

**参数:**

| 参数 | 类型 | 说明 |
|------|------|------|
| `requirements` | `IRuleRequirement[]` | 子要求列表 |

**示例:**
```csharp
var req = new AllRequirement(
    new CardsRequirement("金币", 5),
    new ConditionRequirement(ctx => ctx.Source.HasTag("玩家"))
);
```

---

#### AnyRequirement 类

**命名空间:** `EasyPack.EmeCardSystem.Requirements`

**描述:** 逻辑或组合，任一子要求满足即可。

##### 构造函数
```csharp
public AnyRequirement(params IRuleRequirement[] requirements)
```

**参数:**

| 参数 | 类型 | 说明 |
|------|------|------|
| `requirements` | `IRuleRequirement[]` | 子要求列表 |

**示例:**
```csharp
var req = new AnyRequirement(
    new CardsRequirement("金币", 10),
    new CardsRequirement("宝石", 5)
);
```

---

#### NotRequirement 类

**命名空间:** `EasyPack.EmeCardSystem.Requirements`

**描述:** 逻辑非，对子要求的结果取反。

##### 构造函数
```csharp
public NotRequirement(IRuleRequirement requirement)
```

**参数:**

| 参数 | 类型 | 说明 |
|------|------|------|
| `requirement` | `IRuleRequirement` | 要取反的子要求 |

**示例:**
```csharp
// 要求不存在"毒"标签
var req = new NotRequirement(
    new ConditionRequirement(ctx => ctx.Source.HasTag("毒"))
);
```

---

## 内置效果

### CreateCardsEffect 类

**命名空间:** `EasyPack.EmeCardSystem.Effects`

**描述:** 在容器中创建指定 ID 的新卡牌。

#### 构造函数

##### CreateCardsEffect
```csharp
public CreateCardsEffect()
```
默认构造函数（使用属性初始化器配置）。

---

#### 属性

##### CardIds
```csharp
public List<string> CardIds { get; set; }
```
要创建的卡牌 ID 列表。

**类型:** `List<string>`  
**默认值:** 空列表

---

##### CountPerId
```csharp
public int CountPerId { get; set; }
```
每个 ID 的创建数量。

**类型:** `int`  
**默认值:** `1`

**示例:**
```csharp
// 创建 2 个宝箱和 3 个金币
var effect = new CreateCardsEffect
{
    CardIds = new List<string> { "宝箱", "金币" },
    CountPerId = 2  // 每个 ID 创建 2 份
};
```

---

### RemoveCardsEffect 类

**命名空间:** `EasyPack.EmeCardSystem.Effects`

**描述:** 从容器移除指定卡牌（不会移除固有子卡）。

#### 构造函数

##### RemoveCardsEffect
```csharp
public RemoveCardsEffect()
```
默认构造函数（使用属性初始化器配置）。

---

#### 属性

##### Root
```csharp
public SelectionRoot Root { get; set; }
```
选择起点。

**类型:** `SelectionRoot`  
**默认值:** `SelectionRoot.Container`

---

##### Scope
```csharp
public TargetScope Scope { get; set; }
```
选择范围。

**类型:** `TargetScope`  
**默认值:** `TargetScope.Matched`

---

##### Filter
```csharp
public CardFilterMode Filter { get; set; }
```
过滤模式。

**类型:** `CardFilterMode`  
**默认值:** `CardFilterMode.None`

---

##### FilterValue
```csharp
public string FilterValue { get; set; }
```
过滤值（配合 `Filter` 使用）。

**类型:** `string`

---

##### Take
```csharp
public int? Take { get; set; }
```
最多移除的卡牌数量（`null` 表示不限制）。

**类型:** `int?`  
**默认值:** `null`

---

##### MaxDepth
```csharp
public int? MaxDepth { get; set; }
```
递归深度限制（仅对 `Scope=Descendants` 生效）。

**类型:** `int?`  
**默认值:** `null`

**示例:**
```csharp
// 从匹配结果中移除前 2 个金币
var effect = new RemoveCardsEffect
{
    Scope = TargetScope.Matched,
    Filter = CardFilterMode.ById,
    FilterValue = "金币",
    Take = 2
};
```

---

### ModifyPropertyEffect 类

**命名空间:** `EasyPack.EmeCardSystem.Effects`

**描述:** 修改目标卡牌的属性值（支持修饰符和基础值操作）。

#### 构造函数

##### ModifyPropertyEffect
```csharp
public ModifyPropertyEffect()
```
默认构造函数（使用属性初始化器配置）。

---

#### 属性

##### PropertyName
```csharp
public string PropertyName { get; set; }
```
要修改的属性名（空字符串表示全部属性）。

**类型:** `string`  
**默认值:** `""`

---

##### ApplyMode
```csharp
public Mode ApplyMode { get; set; }
```
应用模式。

**类型:** `ModifyPropertyEffect.Mode`  
**默认值:** `Mode.AddToBase`

**枚举值:**
- `AddModifier`: 添加修饰符
- `RemoveModifier`: 移除修饰符
- `AddToBase`: 对基础值加上 `Value`
- `SetBase`: 将基础值设为 `Value`

---

##### Modifier
```csharp
public IModifier Modifier { get; set; }
```
要添加/移除的修饰符（仅 `AddModifier`/`RemoveModifier` 模式使用）。

**类型:** `IModifier`

---

##### Value
```csharp
public float Value { get; set; }
```
数值参数（用于 `AddToBase`/`SetBase` 模式）。

**类型:** `float`  
**默认值:** `0f`

---

##### Root
```csharp
public SelectionRoot Root { get; set; }
```
选择起点。

**类型:** `SelectionRoot`  
**默认值:** `SelectionRoot.Container`

---

##### Scope
```csharp
public TargetScope Scope { get; set; }
```
选择范围。

**类型:** `TargetScope`  
**默认值:** `TargetScope.Matched`

---

##### Filter
```csharp
public CardFilterMode Filter { get; set; }
```
过滤模式。

**类型:** `CardFilterMode`  
**默认值:** `CardFilterMode.None`

---

##### FilterValue
```csharp
public string FilterValue { get; set; }
```
过滤值（配合 `Filter` 使用）。

**类型:** `string`

---

##### Take
```csharp
public int? Take { get; set; }
```
仅作用前 N 个目标（`null` 表示不限制）。

**类型:** `int?`  
**默认值:** `null`

---

##### MaxDepth
```csharp
public int? MaxDepth { get; set; }
```
递归深度限制（仅对 `Scope=Descendants` 生效，`null` 表示不限制）。

**类型:** `int?`  
**默认值:** `null`

**示例:**
```csharp
// 为匹配的卡牌增加 10 点攻击力
var effect = new ModifyPropertyEffect
{
    PropertyName = "攻击力",
    ApplyMode = ModifyPropertyEffect.Mode.AddToBase,
    Value = 10f
};
```

---

### AddTagEffect 类

**命名空间:** `EasyPack.EmeCardSystem.Effects`

**描述:** 为目标卡牌添加标签。

#### 构造函数

##### AddTagEffect
```csharp
public AddTagEffect()
```
默认构造函数（使用属性初始化器配置）。

---

#### 属性

##### Tag
```csharp
public string Tag { get; set; }
```
要添加的标签文本。

**类型:** `string`

---

##### Root
```csharp
public SelectionRoot Root { get; set; }
```
选择起点。

**类型:** `SelectionRoot`  
**默认值:** `SelectionRoot.Container`

---

##### Scope
```csharp
public TargetScope Scope { get; set; }
```
选择范围。

**类型:** `TargetScope`  
**默认值:** `TargetScope.Matched`

---

##### Filter
```csharp
public CardFilterMode Filter { get; set; }
```
过滤模式。

**类型:** `CardFilterMode`  
**默认值:** `CardFilterMode.None`

---

##### FilterValue
```csharp
public string FilterValue { get; set; }
```
过滤值（配合 `Filter` 使用）。

**类型:** `string`

---

##### Take
```csharp
public int? Take { get; set; }
```
仅作用前 N 个目标（`null` 表示不限制）。

**类型:** `int?`  
**默认值:** `null`

---

##### MaxDepth
```csharp
public int? MaxDepth { get; set; }
```
递归深度限制（仅对 `Scope=Descendants` 生效）。

**类型:** `int?`  
**默认值:** `null`

**示例:**
```csharp
// 为匹配的卡牌添加"强化"标签
var effect = new AddTagEffect
{
    Tag = "强化",
    Scope = TargetScope.Matched
};
```

---

### InvokeEffect 类

**命名空间:** `EasyPack.EmeCardSystem.Effects`

**描述:** 基于自定义委托的效果。

#### 构造函数

##### InvokeEffect
```csharp
public InvokeEffect(Action<CardRuleContext, IReadOnlyList<Card>> action)
```

**参数:**

| 参数 | 类型 | 说明 |
|------|------|------|
| `action` | `Action<CardRuleContext, IReadOnlyList<Card>>` | 自定义效果函数 |

**示例:**
```csharp
var effect = new InvokeEffect((ctx, matched) =>
{
    Debug.Log($"卡牌 {ctx.Source.Name} 触发了规则");
    Debug.Log($"匹配到 {matched.Count} 个目标");
    // 自定义逻辑
});
```

---

## 工具类

### CardRuleBuilder 类

**命名空间:** `EasyPack.EmeCardSystem`

**描述:** 流式 API 构建器，用于简化规则创建。采用链式调用模式，支持条件要求、效果执行和策略配置。

#### 示例用法

```csharp
using EasyPack.EmeCardSystem;

// 基础示例：使用制作工具消耗树木创建木棍
engine.RegisterRule(b => b
    .On(CardEventType.Use)
    .When(ctx => ctx.Source.HasTag("制作"))
    .NeedTag("玩家")
    .NeedId("树木")
    .DoRemoveById("树木", take: 1)
    .DoCreate("木棍")
    .StopPropagation()
);

// 复杂示例：火把燃烧规则
engine.RegisterRule(b => b
    .On(CardEventType.Tick)
    .AtSelf()
    .NeedTag("火把")
    .DoModifyTag("火把", "Ticks", 1f)
    .Priority(100)
);
```

---

#### 基础配置方法

##### On
```csharp
public CardRuleBuilder On(CardEventType eventType, string customId = null)
```
设置事件触发类型。

**参数:**

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `eventType` | `CardEventType` | - | 事件类型 |
| `customId` | `string` | `null` | 自定义事件 ID（仅 `Custom` 事件使用） |

**示例:**
```csharp
.On(CardEventType.Use)
.On(CardEventType.Custom, "player_level_up")
```

---

##### OwnerHops
```csharp
public CardRuleBuilder OwnerHops(int hops)
```
设置容器锚点选择策略。

**参数:**

| 参数 | 类型 | 说明 |
|------|------|------|
| `hops` | `int` | 向上跳跃层数（`0`=自身，`1`=父级，`-1`=根，`>1`=向上 N 层） |

---

##### AtSelf
```csharp
public CardRuleBuilder AtSelf()
```
以触发卡牌自身为容器（等效于 `OwnerHops(0)`）。

---

##### AtParent
```csharp
public CardRuleBuilder AtParent()
```
以直接父级为容器（等效于 `OwnerHops(1)`，默认行为）。

---

##### AtRoot
```csharp
public CardRuleBuilder AtRoot()
```
以根容器为容器（等效于 `OwnerHops(-1)`）。

---

##### MaxDepth
```csharp
public CardRuleBuilder MaxDepth(int depth)
```
设置递归选择的最大深度。

**参数:**

| 参数 | 类型 | 说明 |
|------|------|------|
| `depth` | `int` | 最大深度（仅对 `Descendants` 作用域生效） |

---

##### Priority
```csharp
public CardRuleBuilder Priority(int priority)
```
设置规则优先级（数值越小越优先）。

**参数:**

| 参数 | 类型 | 说明 |
|------|------|------|
| `priority` | `int` | 优先级值 |

---

##### DistinctMatched
```csharp
public CardRuleBuilder DistinctMatched(bool enabled = true)
```
是否对匹配结果去重。

**参数:**

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `enabled` | `bool` | `true` | 是否启用去重 |

---

##### StopPropagation
```csharp
public CardRuleBuilder StopPropagation(bool stop = true)
```
执行后中止事件传播。

**参数:**

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `stop` | `bool` | `true` | 是否中止传播 |

---

#### 条件要求 - 核心方法

##### When
```csharp
public CardRuleBuilder When(Func<CardRuleContext, bool> predicate)
```
添加条件判断要求。

**参数:**

| 参数 | 类型 | 说明 |
|------|------|------|
| `predicate` | `Func<CardRuleContext, bool>` | 条件判断委托 |

**示例:**
```csharp
.When(ctx => ctx.Source.HasTag("稀有"))
.When(ctx => ctx.Container.ChildrenCount > 5)
```

---

##### WhenWithCards
```csharp
public CardRuleBuilder WhenWithCards(
    Func<CardRuleContext, (bool matched, List<Card> cards)> predicate)
```
添加条件判断并返回匹配的卡牌列表。

**参数:**

| 参数 | 类型 | 说明 |
|------|------|------|
| `predicate` | `Func<CardRuleContext, (bool, List<Card>)>` | 返回匹配结果和卡牌列表的委托 |

**优势:** 委托仅被调用一次，避免重复计算。

**示例:**
```csharp
.WhenWithCards(ctx =>
{
    var burnedTorches = ctx.Container.Children
        .Where(c => c.HasTag("火把") && c.GetProperty("Ticks").GetBaseValue() >= 5f)
        .ToList();
    return (burnedTorches.Count > 0, burnedTorches);
})
```

---

##### Need
```csharp
public CardRuleBuilder Need(
    SelectionRoot root,
    TargetScope scope,
    CardFilterMode filter = CardFilterMode.None,
    string filterValue = null,
    int minCount = 1,
    int maxMatched = -1,
    int? maxDepth = null)
```
添加卡牌选择要求（核心方法）。

**参数:**

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `root` | `SelectionRoot` | - | 选择起点（`Container`/`Source`） |
| `scope` | `TargetScope` | - | 选择范围（`Children`/`Descendants`/`Matched`） |
| `filter` | `CardFilterMode` | `None` | 过滤模式 |
| `filterValue` | `string` | `null` | 过滤值 |
| `minCount` | `int` | `1` | 最少需要数量 |
| `maxMatched` | `int` | `-1` | 最多返回数量（`-1`=使用 `minCount`） |
| `maxDepth` | `int?` | `null` | 递归最大深度 |

---

##### AddRequirement
```csharp
public CardRuleBuilder AddRequirement(IRuleRequirement requirement)
```
添加自定义要求项。

**参数:**

| 参数 | 类型 | 说明 |
|------|------|------|
| `requirement` | `IRuleRequirement` | 自定义要求项实例 |

---

#### 条件要求 - When 语法糖

##### WhenSourceCategory
```csharp
public CardRuleBuilder WhenSourceCategory(CardCategory category)
```
要求源卡牌的类别为指定类别。

---

##### WhenSourceIsObject / WhenSourceIsAction / WhenSourceIsAttribute
```csharp
public CardRuleBuilder WhenSourceIsObject()
public CardRuleBuilder WhenSourceIsAction()
public CardRuleBuilder WhenSourceIsAttribute()
```
要求源卡牌为指定类别。

---

##### WhenSourceHasTag
```csharp
public CardRuleBuilder WhenSourceHasTag(string tag)
```
要求源卡牌包含指定标签。

---

##### WhenSourceNotHasTag
```csharp
public CardRuleBuilder WhenSourceNotHasTag(string tag)
```
要求源卡牌不包含指定标签。

---

##### WhenSourceId
```csharp
public CardRuleBuilder WhenSourceId(string id)
```
要求源卡牌的 ID 为指定值。

---

##### WhenContainerCategory / WhenContainerHasTag
```csharp
public CardRuleBuilder WhenContainerCategory(CardCategory category)
public CardRuleBuilder WhenContainerHasTag(string tag)
public CardRuleBuilder WhenContainerNotHasTag(string tag)
```
容器相关条件判断。

---

##### WhenEventDataIs / WhenEventDataNotNull
```csharp
public CardRuleBuilder WhenEventDataIs<T>() where T : class
public CardRuleBuilder WhenEventDataNotNull()
```
事件数据相关条件判断。

---

#### 条件要求 - Need 便捷语法糖

##### NeedTag
```csharp
public CardRuleBuilder NeedTag(string tag, int minCount = 1, int maxMatched = -1)
```
需要容器的直接子卡中有指定标签的卡牌。

**示例:**
```csharp
.NeedTag("玩家")              // 至少 1 个玩家
.NeedTag("金币", 10)          // 至少 10 个金币
.NeedTag("士兵", 5, 3)        // 至少 5 个士兵，但只返回 3 个给效果
```

---

##### NeedId
```csharp
public CardRuleBuilder NeedId(string id, int minCount = 1, int maxMatched = -1)
```
需要容器的直接子卡中有指定 ID 的卡牌。

---

##### NeedCategory
```csharp
public CardRuleBuilder NeedCategory(CardCategory category, int minCount = 1, int maxMatched = -1)
```
需要容器的直接子卡中有指定类别的卡牌。

---

##### NeedTagRecursive
```csharp
public CardRuleBuilder NeedTagRecursive(string tag, int minCount = 1, 
                                       int maxMatched = -1, int? maxDepth = null)
```
需要容器的所有后代中有指定标签的卡牌（递归搜索）。

---

##### NeedIdRecursive / NeedCategoryRecursive
```csharp
public CardRuleBuilder NeedIdRecursive(string id, int minCount = 1, 
                                      int maxMatched = -1, int? maxDepth = null)
public CardRuleBuilder NeedCategoryRecursive(CardCategory category, int minCount = 1,
                                            int maxMatched = -1, int? maxDepth = null)
```
递归搜索指定 ID 或类别的卡牌。

---

##### NeedSourceTag / NeedSourceId
```csharp
public CardRuleBuilder NeedSourceTag(string tag, int minCount = 1, int maxMatched = -1)
public CardRuleBuilder NeedSourceId(string id, int minCount = 1, int maxMatched = -1)
```
需要源卡的直接子卡满足条件。

---

##### NeedSourceTagRecursive
```csharp
public CardRuleBuilder NeedSourceTagRecursive(string tag, int minCount = 1,
                                             int maxMatched = -1, int? maxDepth = null)
```
需要源卡的所有后代满足条件。

---

#### 效果执行 - 核心方法

##### Do
```csharp
public CardRuleBuilder Do(IRuleEffect effect)
public CardRuleBuilder Do(params IRuleEffect[] effects)
public CardRuleBuilder Do(IEnumerable<IRuleEffect> effects)
```
添加一个或多个自定义效果。

**参数:**

| 参数 | 类型 | 说明 |
|------|------|------|
| `effect` | `IRuleEffect` | 单个效果实例 |
| `effects` | `IRuleEffect[]` / `IEnumerable<IRuleEffect>` | 多个效果 |

---

##### DoRemove
```csharp
public CardRuleBuilder DoRemove(
    SelectionRoot root = SelectionRoot.Container,
    TargetScope scope = TargetScope.Matched,
    CardFilterMode filter = CardFilterMode.None,
    string filterValue = null,
    int? take = null,
    int? maxDepth = null)
```
移除卡牌效果。

**参数:**

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `root` | `SelectionRoot` | `Container` | 选择起点 |
| `scope` | `TargetScope` | `Matched` | 选择范围 |
| `filter` | `CardFilterMode` | `None` | 过滤模式 |
| `filterValue` | `string` | `null` | 过滤值 |
| `take` | `int?` | `null` | 最多移除数量 |
| `maxDepth` | `int?` | `null` | 递归深度 |

---

##### DoModify
```csharp
public CardRuleBuilder DoModify(
    string propertyName,
    float value,
    ModifyPropertyEffect.Mode mode = ModifyPropertyEffect.Mode.AddToBase,
    SelectionRoot root = SelectionRoot.Container,
    TargetScope scope = TargetScope.Matched,
    CardFilterMode filter = CardFilterMode.None,
    string filterValue = null,
    int? take = null,
    int? maxDepth = null)
```
修改属性效果。

**参数:**

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `propertyName` | `string` | - | 属性名称 |
| `value` | `float` | - | 修改值 |
| `mode` | `ModifyPropertyEffect.Mode` | `AddToBase` | 修改模式（加法/乘法/设置/添加修饰器） |
| 其他参数 | - | - | 同 `DoRemove` |

---

##### DoAddTag
```csharp
public CardRuleBuilder DoAddTag(
    string tag,
    SelectionRoot root = SelectionRoot.Container,
    TargetScope scope = TargetScope.Matched,
    CardFilterMode filter = CardFilterMode.None,
    string filterValue = null,
    int? take = null,
    int? maxDepth = null)
```
添加标签效果。

---

##### DoCreate
```csharp
public CardRuleBuilder DoCreate(string cardId, int count = 1)
```
创建卡牌效果。

**参数:**

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `cardId` | `string` | - | 要创建的卡牌 ID |
| `count` | `int` | `1` | 创建数量 |

---

##### DoInvoke
```csharp
public CardRuleBuilder DoInvoke(Action<CardRuleContext, IReadOnlyList<Card>> action)
```
执行自定义逻辑。

**参数:**

| 参数 | 类型 | 说明 |
|------|------|------|
| `action` | `Action<CardRuleContext, IReadOnlyList<Card>>` | 自定义效果委托 |

**示例:**
```csharp
.DoInvoke((ctx, matched) =>
{
    Debug.Log($"规则触发，匹配了 {matched.Count} 张卡牌");
    // 自定义逻辑
})
```

---

#### 效果执行 - 便捷语法糖

##### DoRemoveByTag / DoRemoveById
```csharp
public CardRuleBuilder DoRemoveByTag(string tag, int? take = null)
public CardRuleBuilder DoRemoveById(string id, int? take = null)
```
移除匹配结果中指定标签/ID 的卡牌。

---

##### DoRemoveInChildByTag / DoRemoveInChildById
```csharp
public CardRuleBuilder DoRemoveInChildByTag(string tag, int? take = null)
public CardRuleBuilder DoRemoveInChildById(string id, int? take = null)
```
移除容器子卡中指定标签/ID 的卡牌。

---

##### DoAddTagToMatched
```csharp
public CardRuleBuilder DoAddTagToMatched(string tag)
```
给匹配结果添加标签。

---

##### DoAddTagToSource / DoAddTagToContainer
```csharp
public CardRuleBuilder DoAddTagToSource(string tag)
public CardRuleBuilder DoAddTagToContainer(string tag)
```
给源卡牌/容器自身添加标签。

---

##### DoAddTagToTag / DoAddTagToId
```csharp
public CardRuleBuilder DoAddTagToTag(string targetTag, string newTag, int? take = null)
public CardRuleBuilder DoAddTagToId(string targetId, string newTag, int? take = null)
```
给容器子卡中指定标签/ID 的卡牌添加新标签。

---

##### DoRemoveTagFromSource / DoRemoveTagFromContainer
```csharp
public CardRuleBuilder DoRemoveTagFromSource(string tag)
public CardRuleBuilder DoRemoveTagFromContainer(string tag)
```
从源卡牌/容器移除标签。

---

##### DoModifyTag
```csharp
public CardRuleBuilder DoModifyTag(
    string tag,
    string propertyName,
    float value,
    ModifyPropertyEffect.Mode mode = ModifyPropertyEffect.Mode.AddToBase,
    int? take = null)
```
修改容器子卡中指定标签的卡牌的属性。

---

##### DoModifyMatched
```csharp
public CardRuleBuilder DoModifyMatched(
    string propertyName,
    float value,
    ModifyPropertyEffect.Mode mode = ModifyPropertyEffect.Mode.AddToBase)
```
修改匹配结果的属性。

---

##### DoBatchCustom
```csharp
public CardRuleBuilder DoBatchCustom(string eventId, 
                                    Func<CardRuleContext, object> data = null,
                                    bool haveSource = true)
```
批量触发匹配卡牌的自定义事件。

**参数:**

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `eventId` | `string` | - | 自定义事件 ID |
| `data` | `Func<CardRuleContext, object>` | `null` | 事件数据生成函数 |
| `haveSource` | `bool` | `true` | 是否也触发源卡牌的事件 |

---

#### 调试方法

##### PrintContext
```csharp
public CardRuleBuilder PrintContext()
```
打印规则上下文信息到控制台（调试用）。

---

#### 构建方法

##### Build
```csharp
public CardRule Build()
```
构建并返回规则实例。

**返回值:** `CardRule` 实例

---

### RuleRegistrationExtensions 类

**命名空间:** `EasyPack.EmeCardSystem`

**描述:** 规则注册扩展方法，为 `CardEngine` 提供便捷的规则注册 API。

#### 扩展方法

##### RegisterRule(Action<CardRuleBuilder>)
```csharp
public static CardRule RegisterRule(this CardEngine engine, 
                                   Action<CardRuleBuilder> configure)
```
使用构建器委托注册规则。

**参数:**

| 参数 | 类型 | 说明 |
|------|------|------|
| `engine` | `CardEngine` | 引擎实例 |
| `configure` | `Action<CardRuleBuilder>` | 构建器配置委托 |

**返回值:** 构建并注册的 `CardRule` 实例

**示例:**
```csharp
engine.RegisterRule(b => b
    .On(CardEventType.Use)
    .NeedTag("玩家")
    .DoCreate("金币", 5)
);
```

---

##### RegisterRule(IReadOnlyList<Action<CardRuleBuilder>>)
```csharp
public static void RegisterRule(this CardEngine engine,
                               IReadOnlyList<Action<CardRuleBuilder>> configures)
```
批量注册规则集合。

**参数:**

| 参数 | 类型 | 说明 |
|------|------|------|
| `engine` | `CardEngine` | 引擎实例 |
| `configures` | `IReadOnlyList<Action<CardRuleBuilder>>` | 规则构建器列表 |

**示例:**
```csharp
var rules = new List<Action<CardRuleBuilder>>
{
    b => b.On(CardEventType.Use).NeedTag("A").DoCreate("B"),
    b => b.On(CardEventType.Tick).NeedTag("C").DoModify("HP", -1f)
};
engine.RegisterRule(rules);
```

---

### TargetSelector 类

**命名空间:** `EasyPack.EmeCardSystem`

**描述:** 静态工具类，根据 `TargetScope`、`FilterMode` 等参数从上下文中选择卡牌。

#### 静态方法

##### Select
```csharp
public static IReadOnlyList<Card> Select(
    TargetScope scope,
    CardFilterMode filter,
    CardRuleContext ctx,
    string filterValue = null,
    int? maxDepth = null)
```
根据作用域和过滤条件选择目标卡牌。

**参数:**

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `scope` | `TargetScope` | - | 选择范围（`Children`/`Descendants`） |
| `filter` | `CardFilterMode` | - | 过滤模式 |
| `ctx` | `CardRuleContext` | - | 规则上下文 |
| `filterValue` | `string` | `null` | 过滤值（标签名/ID/Category名） |
| `maxDepth` | `int?` | `null` | 递归最大深度（仅对 `Descendants` 生效） |

**返回值:** 符合条件的卡牌列表（只读）

**说明:** `Matched` 作用域不在此处理，由调用方直接使用匹配结果。

---

##### SelectForEffect
```csharp
public static IReadOnlyList<Card> SelectForEffect(ITargetSelection selection, 
                                                  CardRuleContext ctx)
```
供效果使用的选择方法：根据 `ITargetSelection` 配置构建局部上下文并选择目标。

**参数:**

| 参数 | 类型 | 说明 |
|------|------|------|
| `selection` | `ITargetSelection` | 目标选择配置 |
| `ctx` | `CardRuleContext` | 当前规则上下文 |

**返回值:** 符合条件的卡牌列表（只读）

**说明:**
- 根据 `selection.Root` 确定根容器
- 构建局部上下文并调用 `Select` 方法
- 应用 `Take` 限制

---

##### ApplyFilter
```csharp
public static IReadOnlyList<Card> ApplyFilter(IReadOnlyList<Card> cards, 
                                              CardFilterMode filter, 
                                              string filterValue)
```
对已有的卡牌列表应用过滤条件。

**参数:**

| 参数 | 类型 | 说明 |
|------|------|------|
| `cards` | `IReadOnlyList<Card>` | 要过滤的卡牌列表 |
| `filter` | `CardFilterMode` | 过滤模式 |
| `filterValue` | `string` | 过滤值 |

**返回值:** 过滤后的卡牌列表（只读）

**示例:**
```csharp
var allCards = ctx.Container.Children;
var weaponCards = TargetSelector.ApplyFilter(allCards, CardFilterMode.ByTag, "武器");
```

---

### RulePolicy 类

**命名空间:** `EasyPack.EmeCardSystem`

**描述:** 规则执行策略配置。

#### 属性

##### DistinctMatched
```csharp
public bool DistinctMatched { get; set; } = true
```
是否对聚合的匹配结果（`matched`）去重。

**类型:** `bool`  
**默认值:** `true`

**说明:** 当多个要求项返回的卡牌列表有重复时，是否去重后再传递给效果。

---

##### StopEventOnSuccess
```csharp
public bool StopEventOnSuccess { get; set; } = false
```
该规则命中并执行后，是否中止本次事件的后续规则。

**类型:** `bool`  
**默认值:** `false`

**说明:** 设置为 `true` 时，本规则执行成功后将停止处理同一事件的其他规则。

---

### EnginePolicy 类

**命名空间:** `EasyPack.EmeCardSystem`

**描述:** 引擎全局策略配置。

#### 属性

##### FirstMatchOnly
```csharp
public bool FirstMatchOnly { get; set; } = false
```
是否只执行第一条命中的规则（跨所有规则）。

**类型:** `bool`  
**默认值:** `false`

**说明:** 设置为 `true` 时，引擎每次只执行第一个满足条件的规则。

---

##### RuleSelection
```csharp
public RuleSelectionMode RuleSelection { get; set; } = RuleSelectionMode.RegistrationOrder
```
命中规则的裁决方式。

**类型:** `RuleSelectionMode`  
**默认值:** `RuleSelectionMode.RegistrationOrder`

**说明:**
- `RegistrationOrder`: 按规则注册顺序执行
- `Priority`: 按规则优先级执行（数值越小优先级越高）

---

## 枚举类型

### CardEventType 枚举

**命名空间:** `EasyPack.EmeCardSystem`

**描述:** 卡牌事件类型。

```csharp
public enum CardEventType
{
    AddedToOwner,       // 卡牌成为子卡（向子卡分发）
    RemovedFromOwner,   // 卡牌从持有者移除（向子卡分发）
    Tick,               // 按时事件
    Use,                // 主动使用
    Custom              // 自定义事件
}
```

**说明:**
- `AddedToOwner`: 当卡牌通过 `AddChild` 成为另一张卡的子卡时触发
- `RemovedFromOwner`: 当卡牌通过 `RemoveChild` 从持有者移除时触发
- `Tick`: 按时事件，通常用于时间驱动的游戏逻辑
- `Use`: 主动使用事件，代表玩家或AI主动使用卡牌
- `Custom`: 自定义事件，配合 `CustomId` 使用

---

### CardCategory 枚举

**命名空间:** `EasyPack.EmeCardSystem`

**描述:** 卡牌类别。

```csharp
public enum CardCategory
{
    Object,        // 物品/实体类
    Attribute,     // 属性/状态类
    Action,        // 行为/动作类
    Environment    // 环境类
}
```

**说明:**
- `Object`: 物品、实体、角色等具体对象
- `Attribute`: 属性、状态、特质等抽象概念
- `Action`: 行为、动作、技能等
- `Environment`: 环境、场景、区域等

---

### SelectionRoot 枚举

**命名空间:** `EasyPack.EmeCardSystem`

**描述:** 目标选择起点。

```csharp
public enum SelectionRoot
{
    Container,  // 以上下文容器（ctx.Container）为根
    Source      // 以触发源（ctx.Source）为根
}
```

---

### TargetScope 枚举

**命名空间:** `EasyPack.EmeCardSystem`

**描述:** 目标选择范围。

```csharp
public enum TargetScope
{
    Matched,      // 来自所有要求项返回的匹配卡集合的聚合
    Children,     // 选定根的一层子卡（不递归）
    Descendants   // 选定根的所有后代（递归）
}
```

---

### CardFilterMode 枚举

**命名空间:** `EasyPack.EmeCardSystem`

**描述:** 卡牌过滤模式。

```csharp
public enum CardFilterMode
{
    None,        // 不过滤（返回所有目标）
    ByTag,       // 按标签过滤
    ById,        // 按ID过滤
    ByCategory   // 按类别过滤
}
```

---

### ModifyPropertyEffect.Mode 枚举

**命名空间:** `EasyPack.EmeCardSystem`

**描述:** 属性修改模式。

```csharp
public enum Mode
{
    AddModifier,     // 添加修饰符
    RemoveModifier,  // 移除修饰符
    AddToBase,       // 对基础值加上指定值
    SetBase          // 将基础值设为指定值
}
```

**说明:**
- `AddModifier`: 使用 GamePropertySystem 的 Modifier 系统添加修饰符
- `RemoveModifier`: 移除之前添加的修饰符
- `AddToBase`: 直接修改基础值（加法）
- `SetBase`: 直接设置基础值

---

### RuleSelectionMode 枚举

**命名空间:** `EasyPack.EmeCardSystem`

**描述:** 规则选择模式。

```csharp
public enum RuleSelectionMode
{
    RegistrationOrder,  // 按注册顺序
    Priority            // 按规则优先级（数值越小越优先）
}
```

---

**相关文档:**

- [用户使用指南](./UserGuide.md)
- [Mermaid 图集](./Diagrams.md)