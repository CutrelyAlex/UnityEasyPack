using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyPack
{
    /// <summary>
    /// Buff 系统完整示例
    /// 本示例按照从简单到复杂的顺序，演示了 Buff 系统的各种功能
    /// </summary>
    public class BuffExample : MonoBehaviour
    {
        private BuffManager _buffManager;
        private GameObject _dummyTarget;
        private GameObject _dummyCreator;
        private GamePropertyManager _combineGamePropertyManager;

        void Start()
        {
            // 初始化基础组件
            _buffManager = new BuffManager();
            _dummyTarget = new GameObject("DummyTarget");
            _dummyCreator = new GameObject("DummyCreator");
            _combineGamePropertyManager = new GamePropertyManager();

            Debug.Log("=== Buff 系统示例开始 ===\n");

            // 按照学习顺序执行示例
            Example_1_BasicBuff();
            Example_2_BuffDuration();
            Example_3_BuffStacking();
            Example_4_BuffSuperpositionStrategies();
            Example_5_PropertyModifierBuffs();
            Example_6_CustomBuffModules();
            Example_7_BuffTagsAndLayers();
            Example_8_BuffLifecycleEvents();
            Example_9_ComplexRPGBuffs();
            Example_10_BuffPerformanceAndBestPractices();
            Example_11_ErrorHandlingAndDebugging();

            Debug.Log("\n=== Buff 系统示例结束 ===");

            // 清理测试对象
            Destroy(_dummyTarget);
            Destroy(_dummyCreator);
        }

        /// <summary>
        /// 示例1：基础 Buff 使用
        /// 学习目标：了解 Buff 的基本概念和创建流程
        /// </summary>
        void Example_1_BasicBuff()
        {
            Debug.Log("=== 示例1：基础 Buff 使用 ===");

            // 1.1 创建最简单的 BuffData
            var simpleBuff = new BuffData
            {
                ID = "SimpleBuff",
                Name = "简单Buff",
                Description = "这是一个最基础的Buff示例",
                Duration = -1f,  // -1 表示永久有效
                MaxStacks = 1
            };

            Debug.Log($"创建BuffData: {simpleBuff.Name}");

            // 1.2 应用Buff到目标
            var buff = _buffManager.CreateBuff(simpleBuff, _dummyCreator, _dummyTarget);
            Debug.Log($"Buff创建成功，ID: {buff.BuffData.ID}，堆叠数: {buff.CurrentStacks}");

            // 1.3 检查Buff是否存在
            bool hasSimpleBuff = _buffManager.ContainsBuff(_dummyTarget, "SimpleBuff");
            Debug.Log($"目标是否有SimpleBuff: {hasSimpleBuff}");

            // 1.4 获取目标上的所有Buff
            var targetBuffs = _buffManager.GetTargetBuffs(_dummyTarget);
            Debug.Log($"目标上的Buff数量: {targetBuffs.Count}");

            // 1.5 移除Buff
            _buffManager.RemoveBuff(buff);
            bool hasBuffAfterRemove = _buffManager.ContainsBuff(_dummyTarget, "SimpleBuff");
            Debug.Log($"移除后是否还有SimpleBuff: {hasBuffAfterRemove}");

            Debug.Log("基础 Buff 示例完成\n");
        }

        /// <summary>
        /// 示例2：Buff 持续时间
        /// 学习目标：了解如何使用有限时间的 Buff
        /// </summary>
        void Example_2_BuffDuration()
        {
            Debug.Log("=== 示例2：Buff 持续时间 ===");

            // 2.1 创建有限时间的Buff
            var timedBuff = new BuffData
            {
                ID = "TimedBuff",
                Name = "定时Buff",
                Duration = 5.0f  // 持续5秒
            };

            var buff = _buffManager.CreateBuff(timedBuff, _dummyCreator, _dummyTarget);
            Debug.Log($"创建定时Buff，初始持续时间: {buff.DurationTimer}秒");

            // 2.2 模拟时间流逝
            Debug.Log("模拟时间流逝...");
            _buffManager.Update(2.0f);  // 过去2秒
            Debug.Log($"2秒后剩余时间: {buff.DurationTimer}秒");

            _buffManager.Update(2.0f);  // 再过2秒
            Debug.Log($"4秒后剩余时间: {buff.DurationTimer}秒");

            // 2.3 检查Buff是否仍然存在
            bool buffExists = _buffManager.ContainsBuff(_dummyTarget, "TimedBuff");
            Debug.Log($"4秒后Buff是否仍存在: {buffExists}");

            // 2.4 让Buff过期
            _buffManager.Update(2.0f);  // 再过2秒，总共6秒
            bool buffExistsAfterExpire = _buffManager.ContainsBuff(_dummyTarget, "TimedBuff");
            Debug.Log($"6秒后Buff是否仍存在: {buffExistsAfterExpire}");

            Debug.Log("Buff 持续时间示例完成\n");
        }

        /// <summary>
        /// 示例3：Buff 堆叠
        /// 学习目标：了解 Buff 的堆叠机制
        /// </summary>
        void Example_3_BuffStacking()
        {
            Debug.Log("=== 示例3：Buff 堆叠 ===");

            // 3.1 创建可堆叠的Buff
            var stackableBuff = new BuffData
            {
                ID = "StackableBuff",
                Name = "可堆叠Buff",
                Duration = -1f,
                MaxStacks = 3,  // 最多堆叠3层
                BuffSuperpositionStacksStrategy = BuffSuperpositionStacksType.Add
            };

            // 3.2 第一次添加
            var buff1 = _buffManager.CreateBuff(stackableBuff, _dummyCreator, _dummyTarget);
            Debug.Log($"第1次添加Buff，当前堆叠数: {buff1.CurrentStacks}");

            // 3.3 第二次添加（应该堆叠）
            _buffManager.CreateBuff(stackableBuff, _dummyCreator, _dummyTarget);
            Debug.Log($"第2次添加Buff，当前堆叠数: {buff1.CurrentStacks}");

            // 3.4 第三次添加
            _buffManager.CreateBuff(stackableBuff, _dummyCreator, _dummyTarget);
            Debug.Log($"第3次添加Buff，当前堆叠数: {buff1.CurrentStacks}");

            // 3.5 第四次添加（应该不会超过最大堆叠数）
            _buffManager.CreateBuff(stackableBuff, _dummyCreator, _dummyTarget);
            Debug.Log($"第4次添加Buff（超过最大堆叠），当前堆叠数: {buff1.CurrentStacks}");

            // 3.6 测试逐层移除
            buff1.BuffData.BuffRemoveStrategy = BuffRemoveType.OneStack;
            _buffManager.RemoveBuff(buff1);
            Debug.Log($"移除一层后，当前堆叠数: {buff1.CurrentStacks}");

            // 清理
            _buffManager.RemoveAllBuffs(_dummyTarget);
            Debug.Log("Buff 堆叠示例完成\n");
        }

        /// <summary>
        /// 示例4：Buff 叠加策略
        /// 学习目标：了解不同的叠加策略对持续时间和堆叠数的影响
        /// </summary>
        void Example_4_BuffSuperpositionStrategies()
        {
            Debug.Log("=== 示例4：Buff 叠加策略 ===");

            // 4.1 测试持续时间Add策略
            Debug.Log("--- 持续时间Add策略 ---");
            var addDurationBuff = new BuffData
            {
                ID = "AddDurationBuff",
                Name = "时间叠加Buff",
                Duration = 5.0f,
                BuffSuperpositionStrategy = BuffSuperpositionDurationType.Add
            };

            var buff1 = _buffManager.CreateBuff(addDurationBuff, _dummyCreator, _dummyTarget);
            Debug.Log($"初始持续时间: {buff1.DurationTimer}秒");

            _buffManager.CreateBuff(addDurationBuff, _dummyCreator, _dummyTarget);
            Debug.Log($"Add策略叠加后持续时间: {buff1.DurationTimer}秒");

            _buffManager.RemoveAllBuffs(_dummyTarget);

            // 4.2 测试持续时间Reset策略
            Debug.Log("--- 持续时间Reset策略 ---");
            var resetDurationBuff = new BuffData
            {
                ID = "ResetDurationBuff",
                Name = "时间重置Buff",
                Duration = 5.0f,
                BuffSuperpositionStrategy = BuffSuperpositionDurationType.Reset
            };

            var buff2 = _buffManager.CreateBuff(resetDurationBuff, _dummyCreator, _dummyTarget);
            _buffManager.Update(2.0f);  // 过去2秒
            Debug.Log($"2秒后持续时间: {buff2.DurationTimer}秒");

            _buffManager.CreateBuff(resetDurationBuff, _dummyCreator, _dummyTarget);
            Debug.Log($"Reset策略叠加后持续时间: {buff2.DurationTimer}秒");

            _buffManager.RemoveAllBuffs(_dummyTarget);

            // 4.3 测试堆叠数不同策略
            Debug.Log("--- 堆叠数策略对比 ---");
            var stackStrategies = new[]
            {
                BuffSuperpositionStacksType.Add,
                BuffSuperpositionStacksType.Reset,
                BuffSuperpositionStacksType.Keep,
                BuffSuperpositionStacksType.ResetThenAdd
            };

            foreach (var strategy in stackStrategies)
            {
                var testBuff = new BuffData
                {
                    ID = $"StackTest_{strategy}",
                    Name = $"堆叠测试_{strategy}",
                    MaxStacks = 5,
                    BuffSuperpositionStacksStrategy = strategy
                };

                var buff = _buffManager.CreateBuff(testBuff, _dummyCreator, _dummyTarget);
                _buffManager.CreateBuff(testBuff, _dummyCreator, _dummyTarget);  // 再次添加
                Debug.Log($"{strategy} 策略下堆叠数: {buff.CurrentStacks}");

                _buffManager.RemoveAllBuffs(_dummyTarget);
            }

            Debug.Log("Buff 叠加策略示例完成\n");
        }

        /// <summary>
        /// 示例5：属性修改型 Buff
        /// 学习目标：了解如何使用 Buff 修改游戏属性
        /// </summary>
        void Example_5_PropertyModifierBuffs()
        {
            Debug.Log("=== 示例5：属性修改型 Buff ===");

            // 5.1 创建测试属性
            var strength = new CombinePropertySingle("Strength", 10f);
            var health = new CombinePropertySingle("Health", 100f);
            _combineGamePropertyManager.AddOrUpdate(strength);
            _combineGamePropertyManager.AddOrUpdate(health);

            Debug.Log($"初始力量: {strength.GetValue()}");
            Debug.Log($"初始生命值: {health.GetValue()}");

            // 5.2 创建增加力量的Buff
            var strengthBuff = new BuffData
            {
                ID = "StrengthBuff",
                Name = "力量增益",
                Duration = 10f,
                MaxStacks = 3,
                BuffSuperpositionStacksStrategy = BuffSuperpositionStacksType.Add
            };

            // 5.3 添加属性修改模块
            var strengthModifier = new FloatModifier(ModifierType.Add, 0, 5f);  // 增加5点力量
            var strengthModule = new CastModifierToProperty(strengthModifier, "Strength", _combineGamePropertyManager)
            {
                CombineGamePropertyManager = _combineGamePropertyManager
            };
            strengthBuff.BuffModules.Add(strengthModule);

            // 5.4 应用Buff并观察效果
            var buff = _buffManager.CreateBuff(strengthBuff, _dummyCreator, _dummyTarget);
            Debug.Log($"应用力量Buff后力量: {strength.GetValue()}");

            // 5.5 堆叠效果
            _buffManager.CreateBuff(strengthBuff, _dummyCreator, _dummyTarget);
            _buffManager.CreateBuff(strengthBuff, _dummyCreator, _dummyTarget);
            Debug.Log($"堆叠3层后力量: {strength.GetValue()}");

            // 5.6 创建百分比加成的Buff
            var healthBuff = new BuffData
            {
                ID = "HealthBuff",
                Name = "生命值增益",
                Duration = 8f
            };

            var healthModifier = new FloatModifier(ModifierType.Mul, 0, 1.5f);  // 增加50%生命值
            var healthModule = new CastModifierToProperty(healthModifier, "Health", _combineGamePropertyManager)
            {
                CombineGamePropertyManager = _combineGamePropertyManager
            };
            healthBuff.BuffModules.Add(healthModule);

            _buffManager.CreateBuff(healthBuff, _dummyCreator, _dummyTarget);
            Debug.Log($"应用生命值百分比Buff后生命值: {health.GetValue()}");

            // 5.7 移除Buff观察恢复
            _buffManager.RemoveAllBuffs(_dummyTarget);
            Debug.Log($"移除所有Buff后力量: {strength.GetValue()}");
            Debug.Log($"移除所有Buff后生命值: {health.GetValue()}");

            Debug.Log("属性修改型 Buff 示例完成\n");
        }

        /// <summary>
        /// 示例6：自定义 Buff 模块
        /// 学习目标：了解如何创建自定义的 Buff 效果
        /// </summary>
        void Example_6_CustomBuffModules()
        {
            Debug.Log("=== 示例6：自定义 Buff 模块 ===");

            // 6.1 创建持续伤害模块
            var dotModule = new DamageOverTimeModule(5f);

            var poisonBuff = new BuffData
            {
                ID = "Poison",
                Name = "中毒",
                Duration = 6f,
                TriggerInterval = 2f,  // 每2秒触发一次
                BuffModules = new List<BuffModule> { dotModule }
            };

            Debug.Log("创建中毒Buff（每2秒造成5点伤害）");
            var poisonBuffInstance = _buffManager.CreateBuff(poisonBuff, _dummyCreator, _dummyTarget);

            // 6.2 模拟时间流逝观察触发效果
            Debug.Log("开始模拟时间流逝...");
            for (int i = 1; i <= 3; i++)
            {
                _buffManager.Update(2f);
                Debug.Log($"经过 {i * 2} 秒");
            }

            // 6.3 创建条件触发模块
            var conditionalModule = new ConditionalEffectModule();

            var conditionalBuff = new BuffData
            {
                ID = "ConditionalBuff",
                Name = "条件Buff",
                Duration = -1f,
                BuffModules = new List<BuffModule> { conditionalModule }
            };

            Debug.Log("创建条件触发Buff");
            var condBuff = _buffManager.CreateBuff(conditionalBuff, _dummyCreator, _dummyTarget);

            // 6.4 触发自定义事件
            conditionalModule.TriggerCustomEvent(condBuff, "LowHealth", 0.2f);

            // 清理
            _buffManager.RemoveAllBuffs(_dummyTarget);
            Debug.Log("自定义 Buff 模块示例完成\n");
        }

        /// <summary>
        /// 示例7：Buff 标签和层级系统
        /// 学习目标：了解如何使用标签和层级管理 Buff
        /// </summary>
        void Example_7_BuffTagsAndLayers()
        {
            Debug.Log("=== 示例7：Buff 标签和层级系统 ===");

            // 7.1 创建带有不同标签的Buff
            var positiveBuff = new BuffData
            {
                ID = "PositiveBuff",
                Name = "正面效果",
                Tags = new List<string> { "Positive", "Temporary" },
                Layers = new List<string> { "Enhancement" }
            };

            var negativeBuff = new BuffData
            {
                ID = "NegativeBuff",
                Name = "负面效果",
                Tags = new List<string> { "Negative", "Temporary" },
                Layers = new List<string> { "Debuff" }
            };

            var permanentBuff = new BuffData
            {
                ID = "PermanentBuff",
                Name = "永久效果",
                Duration = -1f,
                Tags = new List<string> { "Positive", "Permanent" },
                Layers = new List<string> { "Enhancement", "Passive" }
            };

            // 7.2 应用多个Buff
            _buffManager.CreateBuff(positiveBuff, _dummyCreator, _dummyTarget);
            _buffManager.CreateBuff(negativeBuff, _dummyCreator, _dummyTarget);
            _buffManager.CreateBuff(permanentBuff, _dummyCreator, _dummyTarget);

            Debug.Log($"总Buff数量: {_buffManager.GetTargetBuffs(_dummyTarget).Count}");

            // 7.3 按标签查询
            var positiveBuffs = _buffManager.GetBuffsByTag(_dummyTarget, "Positive");
            var temporaryBuffs = _buffManager.GetBuffsByTag(_dummyTarget, "Temporary");
            Debug.Log($"正面效果Buff数量: {positiveBuffs.Count}");
            Debug.Log($"临时效果Buff数量: {temporaryBuffs.Count}");

            // 7.4 按层级查询
            var enhancementBuffs = _buffManager.GetBuffsByLayer(_dummyTarget, "Enhancement");
            Debug.Log($"Enhancement层级Buff数量: {enhancementBuffs.Count}");

            // 7.5 按标签批量移除
            Debug.Log("移除所有临时效果...");
            _buffManager.RemoveBuffsByTag(_dummyTarget, "Temporary");
            Debug.Log($"移除临时效果后剩余Buff数量: {_buffManager.GetTargetBuffs(_dummyTarget).Count}");

            // 7.6 按层级批量移除
            Debug.Log("移除Enhancement层级...");
            _buffManager.RemoveBuffsByLayer(_dummyTarget, "Enhancement");
            Debug.Log($"移除Enhancement层级后剩余Buff数量: {_buffManager.GetTargetBuffs(_dummyTarget).Count}");

            Debug.Log("Buff 标签和层级系统示例完成\n");
        }

        /// <summary>
        /// 示例8：Buff 生命周期事件
        /// 学习目标：了解如何监听和响应 Buff 的各种生命周期事件
        /// </summary>
        void Example_8_BuffLifecycleEvents()
        {
            Debug.Log("=== 示例8：Buff 生命周期事件 ===");

            // 8.1 创建用于事件监听的Buff
            var eventBuff = new BuffData
            {
                ID = "EventBuff",
                Name = "事件测试Buff",
                Duration = 4f,
                TriggerInterval = 1f,
                MaxStacks = 3,
                BuffSuperpositionStacksStrategy = BuffSuperpositionStacksType.Add,
                TriggerOnCreate = true
            };

            // 8.2 创建Buff实例并设置事件监听
            var buff = _buffManager.CreateBuff(eventBuff, _dummyCreator, _dummyTarget);

            // 设置所有生命周期事件监听
            buff.OnCreate += (b) => Debug.Log($"[事件] {b.BuffData.Name} 被创建");
            buff.OnTrigger += (b) => Debug.Log($"[事件] {b.BuffData.Name} 触发效果");
            buff.OnUpdate += (b) => Debug.Log($"[事件] {b.BuffData.Name} 更新 (剩余: {b.DurationTimer:F1}s)");
            buff.OnAddStack += (b) => Debug.Log($"[事件] {b.BuffData.Name} 增加堆叠 (当前: {b.CurrentStacks})");
            buff.OnReduceStack += (b) => Debug.Log($"[事件] {b.BuffData.Name} 减少堆叠 (当前: {b.CurrentStacks})");
            buff.OnRemove += (b) => Debug.Log($"[事件] {b.BuffData.Name} 被移除");

            // 8.3 触发各种事件
            Debug.Log("--- 堆叠事件测试 ---");
            _buffManager.CreateBuff(eventBuff, _dummyCreator, _dummyTarget);  // 触发OnAddStack
            _buffManager.CreateBuff(eventBuff, _dummyCreator, _dummyTarget);  // 再次触发OnAddStack

            Debug.Log("--- 时间更新事件测试 ---");
            _buffManager.Update(1.1f);  // 触发OnTrigger和OnUpdate

            Debug.Log("--- 堆叠减少事件测试 ---");
            buff.BuffData.BuffRemoveStrategy = BuffRemoveType.OneStack;
            _buffManager.RemoveBuff(buff);  // 触发OnReduceStack

            Debug.Log("--- 完全移除事件测试 ---");
            buff.BuffData.BuffRemoveStrategy = BuffRemoveType.All;
            _buffManager.RemoveBuff(buff);  // 触发OnRemove

            Debug.Log("Buff 生命周期事件示例完成\n");
        }

        /// <summary>
        /// 示例9：复杂 RPG Buff 组合
        /// 学习目标：了解在实际游戏中如何组合各种 Buff 功能
        /// </summary>
        void Example_9_ComplexRPGBuffs()
        {
            Debug.Log("=== 示例9：复杂 RPG Buff 组合 ===");

            // 9.1 设置角色属性系统
            var strength = new CombinePropertySingle("Strength", 15f);
            var agility = new CombinePropertySingle("Agility", 12f);
            var health = new CombinePropertySingle("Health", 100f);
            var mana = new CombinePropertySingle("Mana", 50f);

            _combineGamePropertyManager.AddOrUpdate(strength);
            _combineGamePropertyManager.AddOrUpdate(agility);
            _combineGamePropertyManager.AddOrUpdate(health);
            _combineGamePropertyManager.AddOrUpdate(mana);

            Debug.Log("=== 角色初始属性 ===");
            Debug.Log($"力量: {strength.GetValue()}, 敏捷: {agility.GetValue()}");
            Debug.Log($"生命值: {health.GetValue()}, 法力值: {mana.GetValue()}");

            // 9.2 创建狂暴状态Buff（复合效果）
            var rageBuff = new BuffData
            {
                ID = "Rage",
                Name = "狂暴",
                Duration = 15f,
                MaxStacks = 5,
                TriggerInterval = 3f,
                BuffSuperpositionStacksStrategy = BuffSuperpositionStacksType.Add,
                BuffSuperpositionStrategy = BuffSuperpositionDurationType.Reset,
                Tags = new List<string> { "Enhancement", "Combat" },
                Layers = new List<string> { "Temporary", "Stackable" }
            };

            // 添加多个效果模块
            var strBoost = new CastModifierToProperty(new FloatModifier(ModifierType.Add, 0, 3f), "Strength", _combineGamePropertyManager)
            {
                CombineGamePropertyManager = _combineGamePropertyManager
            };

            var agiBoost = new CastModifierToProperty(new FloatModifier(ModifierType.Add, 0, 2f), "Agility", _combineGamePropertyManager)
            {
                CombineGamePropertyManager = _combineGamePropertyManager
            };

            var rageModule = new RageEffectModule();

            rageBuff.BuffModules.AddRange(new BuffModule[] { strBoost, agiBoost, rageModule });

            // 9.3 创建持续伤害Buff
            var burnBuff = new BuffData
            {
                ID = "Burn",
                Name = "灼烧",
                Duration = 8f,
                TriggerInterval = 1f,
                MaxStacks = 3,
                BuffSuperpositionStacksStrategy = BuffSuperpositionStacksType.Add,
                Tags = new List<string> { "DoT", "Fire" },
                Layers = new List<string> { "Debuff" }
            };

            burnBuff.BuffModules.Add(new DamageOverTimeModule(3f));

            // 9.4 创建治疗光环Buff
            var healingAura = new BuffData
            {
                ID = "HealingAura",
                Name = "治疗光环",
                Duration = 20f,
                TriggerInterval = 2f,
                Tags = new List<string> { "Healing", "Aura" },
                Layers = new List<string> { "Support" }
            };

            healingAura.BuffModules.Add(new HealingModule(8f));

            // 9.5 应用复杂Buff组合
            Debug.Log("\n=== 应用Buff组合 ===");
            var rageBuff1 = _buffManager.CreateBuff(rageBuff, _dummyCreator, _dummyTarget);
            var rageBuff2 = _buffManager.CreateBuff(rageBuff, _dummyCreator, _dummyTarget);  // 堆叠
            var rageBuff3 = _buffManager.CreateBuff(rageBuff, _dummyCreator, _dummyTarget);  // 再次堆叠

            Debug.Log($"狂暴3层后 - 力量: {strength.GetValue()}, 敏捷: {agility.GetValue()}");

            _buffManager.CreateBuff(burnBuff, _dummyCreator, _dummyTarget);
            _buffManager.CreateBuff(burnBuff, _dummyCreator, _dummyTarget);  // 灼烧堆叠

            _buffManager.CreateBuff(healingAura, _dummyCreator, _dummyTarget);

            Debug.Log($"所有Buff应用后，目标身上的Buff总数: {_buffManager.GetTargetBuffs(_dummyTarget).Count}");

            // 9.6 模拟战斗过程
            Debug.Log("\n=== 模拟战斗过程 ===");
            for (int i = 1; i <= 5; i++)
            {
                _buffManager.Update(1f);
                Debug.Log($"第{i}秒后状态");

                // 检查各种类型的Buff
                var combatBuffs = _buffManager.GetBuffsByTag(_dummyTarget, "Combat");
                var debuffs = _buffManager.GetBuffsByLayer(_dummyTarget, "Debuff");
                Debug.Log($"  战斗Buff: {combatBuffs.Count}, 减益效果: {debuffs.Count}");
            }

            // 9.7 清除特定类型的Buff
            Debug.Log("\n=== 清除减益效果 ===");
            _buffManager.RemoveBuffsByLayer(_dummyTarget, "Debuff");
            Debug.Log($"清除减益后剩余Buff数量: {_buffManager.GetTargetBuffs(_dummyTarget).Count}");

            // 清理
            _buffManager.RemoveAllBuffs(_dummyTarget);
            Debug.Log("复杂 RPG Buff 组合示例完成\n");
        }

        /// <summary>
        /// 示例10：Buff 性能和最佳实践
        /// 学习目标：了解 Buff 系统的性能优化和使用最佳实践
        /// </summary>
        void Example_10_BuffPerformanceAndBestPractices()
        {
            Debug.Log("=== 示例10：Buff 性能和最佳实践 ===");

            // 10.1 批量操作的重要性
            Debug.Log("--- 批量操作测试 ---");
            var testBuffs = new List<Buff>();

            // 创建多个Buff用于测试
            for (int i = 0; i < 10; i++)
            {
                var buffData = new BuffData
                {
                    ID = $"TestBuff_{i}",
                    Name = $"测试Buff {i}",
                    Tags = new List<string> { "Test", i % 2 == 0 ? "Even" : "Odd" }
                };

                var buff = _buffManager.CreateBuff(buffData, _dummyCreator, _dummyTarget);
                testBuffs.Add(buff);
            }

            Debug.Log($"创建了 {testBuffs.Count} 个测试Buff");

            // 好的做法：批量移除
            var startTime = Time.realtimeSinceStartup;
            _buffManager.RemoveBuffsByTag(_dummyTarget, "Test");
            _buffManager.FlushPendingRemovals();  // 立即处理批量移除
            var batchTime = Time.realtimeSinceStartup - startTime;

            Debug.Log($"批量移除耗时: {batchTime * 1000:F2}ms");

            // 10.2 合理的更新频率
            Debug.Log("--- 更新频率优化 ---");
            var frequentBuff = new BuffData
            {
                ID = "FrequentBuff",
                Name = "高频Buff",
                Duration = 5f,
                TriggerInterval = 0.1f  // 避免过于频繁的触发
            };

            var moderateBuff = new BuffData
            {
                ID = "ModerateBuff",
                Name = "适中Buff",
                Duration = 5f,
                TriggerInterval = 1f  // 更合理的触发频率
            };

            Debug.Log("避免过于频繁的触发间隔，建议 >= 0.5秒");

            // 10.3 资源管理最佳实践
            Debug.Log("--- 资源管理 ---");
            var resourceBuff = new BuffData
            {
                ID = "ResourceBuff",
                Name = "资源测试Buff",
                Duration = 3f
            };

            var resourceBuffInstance = _buffManager.CreateBuff(resourceBuff, _dummyCreator, _dummyTarget);

            // 设置事件监听（记得在适当时候清理）
            System.Action<Buff> onRemoveHandler = (buff) =>
            {
                Debug.Log("资源Buff被移除，进行清理工作");
                // 在这里进行必要的清理工作
            };

            resourceBuffInstance.OnRemove += onRemoveHandler;

            // 10.4 错误预防
            Debug.Log("--- 错误预防 ---");

            // 安全的Buff创建
            BuffData safeBuff = null;
            try
            {
                var safeBuffInstance = _buffManager.CreateBuff(safeBuff, _dummyCreator, _dummyTarget);
                Debug.Log("这行不应该执行");
            }
            catch (System.Exception)
            {
                Debug.Log("✓ 正确处理了空BuffData的情况");
            }

            // 安全的查询
            bool safeQuery = _buffManager.ContainsBuff(null, "TestBuff");
            Debug.Log($"空目标查询结果: {safeQuery}");

            // 清理
            _buffManager.RemoveAllBuffs(_dummyTarget);
            Debug.Log("Buff 性能和最佳实践示例完成\n");
        }

        /// <summary>
        /// 示例11：错误处理和调试
        /// 学习目标：了解常见错误和调试技巧
        /// </summary>
        void Example_11_ErrorHandlingAndDebugging()
        {
            Debug.Log("=== 示例11：错误处理和调试 ===");

            // 11.2 调试技巧
            Debug.Log("--- 调试技巧 ---");

            // 创建用于调试的Buff
            var debugBuff = new BuffData
            {
                ID = "DebugBuff",
                Name = "调试Buff",
                Duration = 5f,
                TriggerInterval = 1f,
                MaxStacks = 2
            };

            debugBuff.BuffModules.Add(new DebugModule());

            var debugBuffInstance = _buffManager.CreateBuff(debugBuff, _dummyCreator, _dummyTarget);

            // 详细的状态检查
            Debug.Log($"Buff详细信息:");
            Debug.Log($"  ID: {debugBuffInstance.BuffData.ID}");
            Debug.Log($"  持续时间: {debugBuffInstance.DurationTimer}");
            Debug.Log($"  触发计时器: {debugBuffInstance.TriggerTimer}");
            Debug.Log($"  当前堆叠: {debugBuffInstance.CurrentStacks}");
            Debug.Log($"  最大堆叠: {debugBuffInstance.BuffData.MaxStacks}");

            // 模拟更新并观察状态变化
            for (int i = 1; i <= 3; i++)
            {
                _buffManager.Update(1f);
                Debug.Log($"第{i}秒后状态: 持续时间={debugBuffInstance.DurationTimer:F1}, 触发计时器={debugBuffInstance.TriggerTimer:F1}");
            }

            // 11.3 性能监控
            Debug.Log("--- 性能监控示例 ---");
            var performanceStartTime = Time.realtimeSinceStartup;

            // 创建大量Buff进行性能测试
            for (int i = 0; i < 100; i++)
            {
                var perfBuff = new BuffData { ID = $"PerfBuff_{i}", Duration = 1f };
                _buffManager.CreateBuff(perfBuff, _dummyCreator, _dummyTarget);
            }

            var creationTime = Time.realtimeSinceStartup - performanceStartTime;
            Debug.Log($"创建100个Buff耗时: {creationTime * 1000:F2}ms");

            var updateStartTime = Time.realtimeSinceStartup;
            _buffManager.Update(0.016f);  // 模拟60FPS的一帧
            var updateTime = Time.realtimeSinceStartup - updateStartTime;
            Debug.Log($"更新100个Buff耗时: {updateTime * 1000:F2}ms");

            // 清理
            _buffManager.RemoveAllBuffs(_dummyTarget);
            Debug.Log("错误处理和调试示例完成\n");
        }

        void Update()
        {
            // 在实际游戏中，需要在Update中调用BuffManager的Update方法
            // _buffManager.Update(Time.deltaTime);
        }
    }

    #region 自定义 Buff 模块示例

    /// <summary>
    /// 持续伤害模块示例
    /// </summary>
    public class DamageOverTimeModule : BuffModule
    {
        private float _damagePerTick;

        public DamageOverTimeModule(float damagePerTick)
        {
            _damagePerTick = damagePerTick;
            RegisterCallback(BuffCallBackType.OnTick, OnTick);
        }

        private void OnTick(Buff buff, object[] parameters)
        {
            float totalDamage = _damagePerTick * buff.CurrentStacks;
            Debug.Log($"[{buff.BuffData.Name}] 造成 {totalDamage} 点伤害（每层{_damagePerTick}）");
        }
    }

    /// <summary>
    /// 条件效果模块示例
    /// </summary>
    public class ConditionalEffectModule : BuffModule
    {
        public ConditionalEffectModule()
        {
            RegisterCallback(BuffCallBackType.OnCreate, OnCreate);
            RegisterCallback("LowHealth", OnLowHealth);
            RegisterCallback("HighDamage", OnHighDamage);
        }

        private void OnCreate(Buff buff, object[] parameters)
        {
            Debug.Log($"[{buff.BuffData.Name}] 条件Buff已激活，等待触发条件");
        }

        private void OnLowHealth(Buff buff, object[] parameters)
        {
            float healthPercentage = (float)parameters[0];
            Debug.Log($"[{buff.BuffData.Name}] 低血量触发！当前血量百分比: {healthPercentage:P}");
            Debug.Log("激活紧急治疗效果!");
        }

        private void OnHighDamage(Buff buff, object[] parameters)
        {
            float damage = (float)parameters[0];
            Debug.Log($"[{buff.BuffData.Name}] 高伤害触发！伤害值: {damage}");
            Debug.Log("激活伤害反弹效果!");
        }

        public void TriggerCustomEvent(Buff buff, string eventName, params object[] parameters)
        {
            Execute(buff, BuffCallBackType.Custom, eventName, parameters);
        }
    }

    /// <summary>
    /// 狂暴效果模块示例
    /// </summary>
    public class RageEffectModule : BuffModule
    {
        public RageEffectModule()
        {
            RegisterCallback(BuffCallBackType.OnAddStack, OnAddStack);
            RegisterCallback(BuffCallBackType.OnTick, OnTick);
        }

        private void OnAddStack(Buff buff, object[] parameters)
        {
            Debug.Log($"[{buff.BuffData.Name}] 狂暴等级提升！当前等级: {buff.CurrentStacks}");
            if (buff.CurrentStacks >= 3)
            {
                Debug.Log("狂暴达到高等级，激活特殊效果！");
            }
        }

        private void OnTick(Buff buff, object[] parameters)
        {
            Debug.Log($"[{buff.BuffData.Name}] 狂暴脉冲！等级 {buff.CurrentStacks}");
        }
    }

    /// <summary>
    /// 治疗模块示例
    /// </summary>
    public class HealingModule : BuffModule
    {
        private float _healingAmount;

        public HealingModule(float healingAmount)
        {
            _healingAmount = healingAmount;
            RegisterCallback(BuffCallBackType.OnTick, OnTick);
        }

        private void OnTick(Buff buff, object[] parameters)
        {
            Debug.Log($"[{buff.BuffData.Name}] 治疗 {_healingAmount} 点生命值");
        }
    }

    /// <summary>
    /// 调试模块示例
    /// </summary>
    public class DebugModule : BuffModule
    {
        public DebugModule()
        {
            RegisterCallback(BuffCallBackType.OnCreate, OnCreate);
            RegisterCallback(BuffCallBackType.OnTick, OnTick);
            RegisterCallback(BuffCallBackType.OnUpdate, OnUpdate);
            RegisterCallback(BuffCallBackType.OnRemove, OnRemove);
        }

        private void OnCreate(Buff buff, object[] parameters)
        {
            Debug.Log($"[DEBUG] {buff.BuffData.Name} 创建");
        }

        private void OnTick(Buff buff, object[] parameters)
        {
            Debug.Log($"[DEBUG] {buff.BuffData.Name} 触发");
        }

        private void OnUpdate(Buff buff, object[] parameters)
        {
            // 避免每帧都输出，只在特定条件下输出
            if (Mathf.Approximately(buff.DurationTimer % 1f, 0f))
            {
                Debug.Log($"[DEBUG] {buff.BuffData.Name} 更新 - 剩余时间: {buff.DurationTimer:F1}s");
            }
        }

        private void OnRemove(Buff buff, object[] parameters)
        {
            Debug.Log($"[DEBUG] {buff.BuffData.Name} 移除");
        }
    }

    #endregion
}