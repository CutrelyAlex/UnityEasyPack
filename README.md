# UnityEasyPack (EasyPack)

轻量、模块化的Unity功能解决方案包，面向小型项目与Demo。目标是提供各类可复用的游戏系统，即插即用。

## 联系作者

<div align="center">

**猫猫D菌**  
*UnityEasyPack 项目维护者*

<p align="center">
  <a href="https://qm.qq.com/q/Y9qMERwx4Q">
    <img src="https://img.shields.io/badge/QQ-EB1923?style=for-the-badge&logo=QQ&logoColor=white" alt="QQ" />
  </a>
  <a href="https://space.bilibili.com/104644407">
    <img src="https://img.shields.io/badge/Bilibili-00A1D6?style=for-the-badge&logo=bilibili&logoColor=white" alt="Bilibili" />
  </a>
</p>

---

**欢迎交流技术问题、使用建议和功能需求**

</div>

## 注意事项

由于目前EasyPack版本仍在Dev阶段，所以可能对部分系统产生较大的重构，如需更新可能对用户使用造成影响，暂不建议将本游戏包直接用于正式的项目中，目前可以安装Release包用于Demo、Gamejam或者快速构建.

## 系统简介

每个已基本完成的系统目录下方都有Example目录，可以查看具体的使用方案

### 01_Framework

- ENekoFramework

### 02_Foundation（基础组件）

- Modifier（修饰器）
  - 通用的值变换接口 `IModifier<T>`，以及常用实现（`FloatModifier`、`RangeModifier`等）
  - 设计用于可组合的变换步骤（百分比、加法、最小/最大约束等）

- CustomData（可扩展数据）
  - 轻量的键值属性系统，支持类型存储（`CustomDataEntry`、`CustomDataType`）

### 03_CoreSystems（核心系统）
- GameProperty（游戏属性）
  - 基于 `Modifier` 系统实现的游戏属性框架。
  - 支持动态属性叠加、优先级（用于临时增益/伤害加成）。
  - 角色属性（力量、敏捷、最大生命值等）、装备属性叠加等。

- Buff
  - 灵活的状态效果管理框架，用于处理游戏中的临时状态效果（如增益、减益等）。
  - 基于模块化设计，通过组合不同的 `BuffModule` 实现复杂效果。
  - 支持多种叠加策略、标签和层级系统、事件驱动的生命周期管理。
  - 可用于属性修改、定时触发效果、多层堆叠效果等。

- Inventory
  - 基于 `Item` `Slot` 和 `Container`概念的背包系统
  - 可以用于各类背包系统，包含线性背包和网格背包

### 04_GameplaySystems（游戏系统）
- EmeCard（规则涌现卡牌系统）
  - 可以自定"规则"的卡牌系统

- SynthesisSystem（合成系统）
  - 物品合成和配方管理系统（暂挖坑）

### 05_Services（服务系统）

- Serialization（序列化系统）
  - 支持 JSON、二进制及自定义策略

### 06_Tools（工具系统）
- AI
- Sprite


## 贡献

- 感谢一冰在北对本系统的贡献
- 感谢各位使用者提出的建议
- 欢迎各位加入本系统的开发之中





