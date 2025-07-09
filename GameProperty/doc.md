# GameProperty 系统文档

本文由 GPT-4.1 生成，请注意甄别！

## 1. Core：GameProperty

### 简介
GameProperty 是基础属性系统的核心，表示一个可被修饰、可依赖的数值属性。支持修饰器（如加法、乘法、范围限制）和属性依赖链，适用于角色属性、装备属性等场景。

### 主要功能
- **基础值与修饰器**：可通过 AddModifier 添加多种修饰器（如加法、乘法、Clamp）。
- **依赖关系**：支持属性间依赖，自动处理依赖链的更新与循环依赖检测。
- **事件监听**：属性值变化时可触发 OnValueChanged 事件，便于响应属性变更。
- **序列化支持**：支持 GameProperty 的序列化与反序列化（不支持组合属性的直接序列化）。

### 示例
```
var hp = new GameProperty("HP", 100f);
hp.AddModifier(new FloatModifier(ModifierType.Add, 0, 20f)); // +20
hp.AddModifier(new FloatModifier(ModifierType.Mul, 0, 1.5f)); // ×1.5
hp.AddModifier(new RangeModifier(ModifierType.Clamp, 0, new Vector2(0, 150))); // 限制范围
float value = hp.GetValue(); // 结果：HP=150
```

### 依赖链示例
```
var strength = new GameProperty("Strength", 10f);
var attack = new GameProperty("Attack", 0f);
attack.AddDependency(strength);
strength.OnValueChanged += (_, __) => {
    attack.SetBaseValue(strength.GetValue() * 2);
};
```

---

## 2. CombineGameProperties：组合属性

### 简介
组合属性用于将多个 GameProperty 以特定方式组合，适合复杂属性计算（如攻击力=基础+Buff×百分比-减益等）。主要有三种实现：

- **CombinePropertySingle**：单一属性包装
- **CombinePropertyClassic**：经典加减乘除组合
- **CombinePropertyCustom**：自定义组合逻辑

### 2.1 CombinePropertySingle
- 仅包含一个 GameProperty，直接返回其值。
- 适合无需组合的简单场景。

**示例：**
```
var single = new CombinePropertySingle("SingleProp");
single.ResultHolder.SetBaseValue(50f);
single.ResultHolder.AddModifier(new FloatModifier(ModifierType.Add, 0, 10f));
float value = single.GetValue(); // 结果：60
```

### 2.2 CombinePropertyClassic
- 组合多个 GameProperty（如基础、Buff、Debuff等），并通过经典公式计算最终值。
- 公式：最终属性 = (基础+加成) × (1+加成百分比) - 减益 × (1+减益百分比)

**示例：**
```
var classic = new CombinePropertyClassic(
    "Atk", 100f, "Base", "Buff", "BuffMul", "Debuff", "DebuffMul"
);
classic.GetProperty("Buff").SetBaseValue(30f);
classic.GetProperty("BuffMul").SetBaseValue(0.2f);
classic.GetProperty("Debuff").SetBaseValue(10f);
classic.GetProperty("DebuffMul").SetBaseValue(0.5f);
float value = classic.GetValue(); // 计算结果
```

### 2.3 CombinePropertyCustom
- 支持自定义组合逻辑，通过委托（Func）灵活定义属性计算方式。
- 适合特殊或复杂的属性组合需求。

**示例：**
```
var sharedProp = new GameProperty("Shared", 100f);
var combineA = new CombinePropertyCustom("A");
combineA.RegisterProperty(sharedProp);
combineA.Calculater = c => c.GetProperty("Shared").GetValue() + 10;
float valueA = combineA.GetValue(); // 110
```

---

## 3. 属性管理器

### CombineGamePropertyManager
- 提供组合属性的统一注册、查询、遍历和移除等管理功能。
- 支持通过 ID 获取、遍历所有组合属性。

**示例：**
```
CombineGamePropertyManager.AddOrUpdate(classic);
var prop = CombineGamePropertyManager.Get("Atk");
```

---

## 4. 进阶用法与注意事项

- 支持复杂依赖链与事件联动，适合 RPG、策略等复杂属性系统。
- 禁止自依赖或循环依赖，例如A->B->A
- 除了游戏时动态属性(例如血量)，对于静态属性(例如最大血量、防御力)建议通过修饰器动态调整属性，避免直接修改基础值。

---

## 5. 参考示例

详见 `GamePropertyExample.cs`，涵盖单属性、组合属性、依赖链、序列化等多种用法。