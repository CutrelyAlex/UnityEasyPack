# CustomData 系统

高性能的 Unity 自定义数据存储和查询系统。支持多种数据类型、O(1) 查询、智能缓存和灵活的序列化。

## 核心类

| 类 | 用途 |
|-----|------|
| `CustomDataCollection` | 数据存储容器，基于双缓存架构 |
| `CustomDataEntry` | 单个数据条目 |
| `CustomDataType` | 数据类型枚举 |

## 快速开始

### 基础使用

```csharp
// 创建集合
var data = new CustomDataCollection();

// 设置数据
data.SetValue("health", 100f);
data.SetValue("level", 10);
data.SetValue("name", "Hero");

// 获取数据
float health = data.GetValue("health", 0f);
int level = data.GetValue("level", 1);
string name = data.GetValue("name", "Unknown");
```

### 快捷方法

```csharp
// 使用类型化方法
data.SetInt("score", 100);
data.SetFloat("speed", 5.5f);
data.SetBool("isAlive", true);
data.SetString("nickname", "Player");
data.SetVector3("position", Vector3.zero);
data.SetColor("color", Color.red);

// 获取数据
int score = data.GetInt("score");
float speed = data.GetFloat("speed");
bool isAlive = data.GetBool("isAlive");
string nickname = data.GetString("nickname");
Vector3 position = data.GetVector3("position");
Color color = data.GetColor("color");
```

### 查询操作

```csharp
// 单个查询
float value = data.GetValue("key", 0f);

// 批量查询
var keys = new[] { "hp", "mp", "speed" };
var values = data.GetValues<int>(keys, 0);

// 条件查询
var valuable = data.GetFirstValue((key, value) => value > 1000, 0f);

// 检查存在性
bool exists = data.HasValue("key");
```

### 批量操作

```csharp
// 批量设置
var values = new Dictionary<string, object>
{
    { "hp", 100f },
    { "mp", 50f },
    { "level", 10 }
};
data.SetValues(values);

// 批量删除
var keysToRemove = new[] { "oldKey1", "oldKey2" };
int removedCount = data.RemoveValues(keysToRemove);

// 合并数据
var otherData = new CustomDataCollection();
otherData.SetValue("bonus", 25);
data.Merge(otherData);
```

### 条件操作

```csharp
// 如果存在则执行
data.IfHasValue<string>("skill", skill =>
{
    UseSkill(skill);
});

// If-Else 操作
data.IfElse<int>("gold",
    gold => Debug.Log($"金币：{gold}"),
    () => Debug.Log("无金币")
);
```

## 支持的数据类型

- **原始类型**：`int`, `float`, `bool`, `string`
- **Unity 类型**：`Vector2`, `Vector3`, `Color`
- **自定义类型**：通过 JSON 序列化或 `ICustomDataSerializer`

```csharp
// 自定义类型示例
[System.Serializable]
public class PlayerStats
{
    public int strength;
    public int agility;
}

var stats = new PlayerStats { strength = 10, agility = 8 };
data.SetValue("stats", stats);
var loadedStats = data.GetValue<PlayerStats>("stats");
```

## 高级功能

### 克隆和比较

```csharp
// 深拷贝
var cloned = data.Clone();

// 获取差异
var other = new CustomDataCollection();
var differences = data.GetDifference(other); // 返回other中有但data中没有的键
```

### 数值操作

```csharp
// 增加数值
int newScore = data.AddInt("score", 10);
float newHealth = data.AddFloat("health", -5f);
```

### 数据枚举

```csharp
// 获取所有键
var allKeys = data.GetKeys();

// 按类型获取键
var intKeys = data.GetKeysByType(CustomDataType.Int);

// 条件筛选
var highValues = data.GetEntriesWhere(entry => entry.GetValue() is float f && f > 100);
```

## 最佳实践

### 1. 性能优化

```csharp
// ✅ 使用类型化方法避免装箱
data.SetInt("score", 100);
int score = data.GetInt("score");

// ❌ 频繁的装箱操作
data.SetValue("score", 100);  // 装箱
var score = (int)data.GetValue("score");  // 拆箱
```

### 2. 批量操作

```csharp
// ✅ 批量设置减少缓存重建
var batchData = new Dictionary<string, object>
{
    { "hp", 100 }, { "mp", 50 }, { "exp", 0 }
};
data.SetValues(batchData);

// ❌ 可避免的多次单独设置
data.SetValue("hp", 100);
data.SetValue("mp", 50);
data.SetValue("exp", 0);
```

### 3. 内存管理

```csharp
// ✅ 推荐：及时清理不需要的数据
data.RemoveValue("temporaryKey");

// ✅ 推荐：重用集合对象
var tempData = new CustomDataCollection();
// 使用 tempData...
tempData.Clear();  // 重置而不是创建新对象
```

## 内部架构

```
CustomDataCollection
├─ _list              : List<CustomDataEntry>      // 存储层
├─ _keyIndexMap       : Dict<string, int>          // 索引缓存（O(1) 删除）
└─ _entryCache        : Dict<string, Entry>        // 对象缓存（O(1) 读取）
```

### 缓存机制

- **索引缓存** (`_keyIndexMap`)：键到索引的映射，用于 O(1) 查找和删除
- **对象缓存** (`_entryCache`)：键到 Entry 对象的映射，用于 O(1) 值获取
- **延迟重建**：修改操作后标记脏缓存，下次访问时重建

### 性能特性

| 操作 | 复杂度 | 说明 |
|------|--------|------|
| GetValue | O(1) | 通过对象缓存直接获取 |
| SetValue | O(1) | 更新缓存和存储 |
| HasValue | O(1) | 通过对象缓存检查 |
| RemoveValue | O(1) | 交换删除法 + 缓存更新 |
| GetValues | O(N) | 遍历请求的键 |
| SetValues | O(N) | 批量设置 |

## API 参考

### 基础操作

#### 构造函数
```csharp
new CustomDataCollection()                    // 空集合
new CustomDataCollection(capacity)            // 指定初始容量
new CustomDataCollection(collection)          // 从集合初始化
```

#### 数据操作
```csharp
void SetValue(string key, object value)       // 设置值
T GetValue<T>(string key, T defaultValue)     // 获取值
bool TryGetValue<T>(string key, out T value)  // 尝试获取
bool HasValue(string key)                     // 检查存在
bool RemoveValue(string key)                  // 删除单个
int RemoveValues(IEnumerable<string> keys)    // 批量删除
```

#### 快捷方法
```csharp
// 设置
void SetInt(string key, int value)
void SetFloat(string key, float value)
void SetBool(string key, bool value)
void SetString(string key, string value)
void SetVector2(string key, Vector2 value)
void SetVector3(string key, Vector3 value)
void SetColor(string key, Color value)

// 获取
int GetInt(string key, int defaultValue = 0)
float GetFloat(string key, float defaultValue = 0f)
bool GetBool(string key, bool defaultValue = false)
string GetString(string key, string defaultValue = "")
Vector2 GetVector2(string key, Vector2? defaultValue = null)
Vector3 GetVector3(string key, Vector3? defaultValue = null)
Color GetColor(string key, Color? defaultValue = null)
```

### 高级操作

#### 批量操作
```csharp
Dictionary<string, T> GetValues<T>(IEnumerable<string> keys, T defaultValue)
void SetValues(Dictionary<string, object> values)
void Merge(CustomDataCollection other)
```

#### 查询操作
```csharp
T GetFirstValue<T>(Func<string, T, bool> predicate, T defaultValue)
IEnumerable<string> GetKeys()
IEnumerable<string> GetKeysByType(CustomDataType type)
IEnumerable<CustomDataEntry> GetEntriesWhere(Func<CustomDataEntry, bool> predicate)
```

#### 条件操作
```csharp
bool IfHasValue<T>(string key, Action<T> action)
void IfElse<T>(string key, Action<T> onExists, Action onNotExists)
```

#### 增量操作
```csharp
int AddInt(string key, int delta = 1)
float AddFloat(string key, float delta = 1f)
```

#### 集合操作
```csharp
CustomDataCollection Clone()
IEnumerable<string> GetDifference(CustomDataCollection other)
bool IsEmpty { get; }
int Count { get; }
```

## 序列化支持

CustomDataCollection 实现了 `ISerializationCallbackReceiver`，支持 Unity 序列化：

- 序列化时自动保存 `_list`
- 反序列化后重建缓存
- 支持 Unity Inspector 编辑
