using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasyPack.GamePropertySystem
{
    /// <summary>
    /// GameProperty 系统完整示例
    /// 本示例按照从简单到复杂的顺序，演示了 GameProperty 系统的各种功能
    /// </summary>
    public class GamePropertyExample : MonoBehaviour
    {
        public GamePropertyManager CombineGamePropertyManager;

        void Start()
        {
            CombineGamePropertyManager = new();

            Debug.Log("=== GameProperty 系统示例开始 ===\n");

            // 按照学习顺序执行示例
            Example_1_BasicGameProperty();
            Example_2_ModifierSystem();
            Example_3_PropertyDependencies();
            Example_4_CombineProperties();
            Example_5_PropertyManager();
            Example_6_RealWorldRPGExample();
            Example_7_SerializationExample();
            Example_8_AdvancedFeatures();
            Example_9_ErrorHandlingAndBestPractices();


            Debug.Log("\n=== GameProperty 系统示例结束 ===");
        }

        /// <summary>
        /// 示例1：基础 GameProperty 使用
        /// 学习目标：了解 GameProperty 的基本概念和基础操作
        /// </summary>
        void Example_1_BasicGameProperty()
        {
            Debug.Log("=== 示例1：基础 GameProperty 使用 ===");

            // 1.1 创建一个基础属性
            var health = new GameProperty("Health", 100f);
            Debug.Log($"创建角色生命值: {health.GetValue()}");

            // 1.2 修改基础值
            health.SetBaseValue(150f);
            Debug.Log($"升级后生命值: {health.GetValue()}");

            // 1.3 监听属性变化
            health.OnValueChanged += (oldValue, newValue) =>
            {
                Debug.Log($"生命值变化: {oldValue} -> {newValue}");
            };

            health.SetBaseValue(200f);
            Debug.Log("基础 GameProperty 示例完成\n");
        }

        /// <summary>
        /// 示例2：修饰器系统
        /// 学习目标：了解如何使用修饰器动态改变属性值
        /// </summary>
        void Example_2_ModifierSystem()
        {
            Debug.Log("=== 示例2：修饰器系统 ===");

            var attack = new GameProperty("Attack", 50f);
            Debug.Log($"基础攻击力: {attack.GetValue()}");

            // 2.1 加法修饰器（装备加成）
            var weaponBonus = new FloatModifier(ModifierType.Add, 0, 25f);
            attack.AddModifier(weaponBonus);
            Debug.Log($"装备武器后攻击力: {attack.GetValue()}");

            // 2.2 乘法修饰器（技能加成）
            var skillBonus = new FloatModifier(ModifierType.Mul, 0, 1.5f);
            attack.AddModifier(skillBonus);
            Debug.Log($"使用技能后攻击力: {attack.GetValue()}"); // (50+25)*1.5 = 112.5

            // 2.3 范围限制修饰器
            var attackCap = new RangeModifier(ModifierType.Clamp, 0, new Vector2(0, 100));
            attack.AddModifier(attackCap);
            Debug.Log($"应用攻击力上限后: {attack.GetValue()}"); // 被限制在100

            // 2.4 移除修饰器
            attack.RemoveModifier(attackCap);
            Debug.Log($"移除攻击力上限后: {attack.GetValue()}"); // 回到112.5

            // 2.5 批量操作修饰器
            var tempBuffs = new List<IModifier> {
                new FloatModifier(ModifierType.Add, 1, 20f),
                new FloatModifier(ModifierType.Mul, 1, 1.2f)
            };

            attack.AddModifiers(tempBuffs);
            Debug.Log($"临时Buff加成后: {attack.GetValue()}");

            attack.RemoveModifiers(tempBuffs);
            Debug.Log($"Buff结束后: {attack.GetValue()}");

            Debug.Log("修饰器系统示例完成\n");
        }

        /// <summary>
        /// 示例3：属性依赖关系
        /// 学习目标：了解如何建立属性间的依赖关系
        /// </summary>
        void Example_3_PropertyDependencies()
        {
            Debug.Log("=== 示例3：属性依赖关系 ===");

            // 3.1 简单依赖关系
            var strength = new GameProperty("Strength", 10f);
            var carryWeight = new GameProperty("CarryWeight", 0f);

            // 建立依赖：负重依赖于力量
            carryWeight.AddDependency(strength, (dep, newVal) => newVal * 5f);

            Debug.Log($"力量: {strength.GetValue()}, 负重: {carryWeight.GetValue()}");

            strength.SetBaseValue(15f);
            Debug.Log($"力量提升后 - 力量: {strength.GetValue()}, 负重: {carryWeight.GetValue()}");

            // 3.2 多重依赖关系 - 错误示例
            var agility = new GameProperty("Agility", 8f);
            var attackSpeed = new GameProperty("AttackSpeed", 1f);

            // 错误的AI，可恶的AI，这样会导致后一个依赖覆盖前一个依赖的结果
            // attackSpeed.AddDependency(agility, (dep, newVal) => 1f + newVal * 0.1f);
            // attackSpeed.AddDependency(strength, (dep, newVal) => attackSpeed.GetBaseValue() + newVal * 0.05f);

            // 正确做法1：创建中间属性分别计算贡献，然后汇总
            var agilityContribution = new GameProperty("AgilityContribution", 0f);
            var strengthContribution = new GameProperty("StrengthContribution", 0f);

            agilityContribution.AddDependency(agility, (dep, newVal) => newVal * 0.1f);
            strengthContribution.AddDependency(strength, (dep, newVal) => newVal * 0.05f);

            // attackSpeed 依赖于两个贡献属性
            attackSpeed.AddDependency(agilityContribution, (dep, newVal) =>
            {
                return 1f + agilityContribution.GetValue() + strengthContribution.GetValue();
            });

            Debug.Log($"敏捷: {agility.GetValue()}, 力量: {strength.GetValue()}, 攻击速度: {attackSpeed.GetValue()}");
            Debug.Log($"  (计算: 1.0 + {agility.GetValue()}*0.1 + {strength.GetValue()}*0.05 = {attackSpeed.GetValue()})");

            agility.SetBaseValue(12f);
            Debug.Log($"敏捷提升后 - 攻击速度: {attackSpeed.GetValue()}");
            Debug.Log($"  (计算: 1.0 + {agility.GetValue()}*0.1 + {strength.GetValue()}*0.05 = {attackSpeed.GetValue()})");

            // 正确做法2：使用 CombinePropertyCustom
            Debug.Log("\n--- 使用 CombinePropertyCustom 的更佳实现 ---");
            var agility2 = new GameProperty("Agility2", 8f);
            var strength2 = new GameProperty("Strength2", 15f);

            var attackSpeed2 = new CombinePropertyCustom("AttackSpeed2", 1f);
            attackSpeed2.RegisterProperty(agility2);
            attackSpeed2.RegisterProperty(strength2);

            // 自定义计算：基础值 + 敏捷*0.1 + 力量*0.05
            attackSpeed2.Calculater = (combine) =>
            {
                var baseSpeed = combine.GetBaseValue();
                var agi = combine.GetProperty("Agility2").GetValue();
                var str = combine.GetProperty("Strength2").GetValue();
                return baseSpeed + agi * 0.1f + str * 0.05f;
            };

            Debug.Log($"敏捷: {agility2.GetValue()}, 力量: {strength2.GetValue()}, 攻击速度: {attackSpeed2.GetValue()}");
            Debug.Log($"  (计算: {attackSpeed2.GetBaseValue()} + {agility2.GetValue()}*0.1 + {strength2.GetValue()}*0.05 = {attackSpeed2.GetValue()})");

            agility2.SetBaseValue(12f);
            Debug.Log($"敏捷提升后 - 攻击速度: {attackSpeed2.GetValue()}");
            Debug.Log($"  (计算: {attackSpeed2.GetBaseValue()} + {agility2.GetValue()}*0.1 + {strength2.GetValue()}*0.05 = {attackSpeed2.GetValue()})");

            // 3.3 循环依赖检测
            var propA = new GameProperty("PropA", 10f);
            var propB = new GameProperty("PropB", 20f);

            propA.AddDependency(propB);
            // 尝试创建循环依赖（会被阻止）
            propB.AddDependency(propA);

            Debug.Log("循环依赖检测测试完成");
            Debug.Log("属性依赖关系示例完成\n");
        }

        /// <summary>
        /// 示例4：组合属性系统
        /// 学习目标：了解如何使用更高级的组合属性
        /// </summary>
        void Example_4_CombineProperties()
        {
            Debug.Log("=== 示例4：组合属性系统 ===");

            // 4.1 单一组合属性
            var singleProp = new CombinePropertySingle("MagicPower");
            singleProp.ResultHolder.SetBaseValue(80f);
            singleProp.ResultHolder.AddModifier(new FloatModifier(ModifierType.Add, 0, 20f));
            Debug.Log($"单一组合属性值: {singleProp.GetValue()}");

            // 4.2 自定义组合属性
            var customProp = new CombinePropertyCustom("CustomDamage");

            var baseDamage = new GameProperty("BaseDamage", 50f);
            var critChance = new GameProperty("CritChance", 0.2f);
            var critMultiplier = new GameProperty("CritMultiplier", 2f);

            customProp.RegisterProperty(baseDamage);
            customProp.RegisterProperty(critChance);
            customProp.RegisterProperty(critMultiplier);

            // 自定义计算逻辑：期望伤害 = 基础伤害 * (1 + 暴击率 * (暴击倍数 - 1))
            customProp.Calculater = (combine) =>
            {
                var baseDmg = combine.GetProperty("BaseDamage").GetValue();
                var critRate = combine.GetProperty("CritChance").GetValue();
                var critMul = combine.GetProperty("CritMultiplier").GetValue();
                return baseDmg * (1f + critRate * (critMul - 1f));
            };

            Debug.Log($"自定义期望伤害: {customProp.GetValue()}");

            // 修改暴击率观察变化
            critChance.SetBaseValue(0.5f);
            Debug.Log($"暴击率提升后期望伤害: {customProp.GetValue()}");

            Debug.Log("组合属性系统示例完成\n");
        }

        /// <summary>
        /// 示例5：属性管理器
        /// 学习目标：了解如何统一管理多个属性
        /// </summary>
        void Example_5_PropertyManager()
        {
            Debug.Log("=== 示例5：属性管理器 ===");

            // 创建多个属性并添加到管理器
            var healthProp = new CombinePropertySingle("PlayerHealth");
            healthProp.ResultHolder.SetBaseValue(100f);

            var manaProp = new CombinePropertySingle("PlayerMana");
            manaProp.ResultHolder.SetBaseValue(50f);

            var staminaProp = new CombinePropertySingle("PlayerStamina");
            staminaProp.ResultHolder.SetBaseValue(80f);

            // 添加到管理器
            CombineGamePropertyManager.AddOrUpdate(healthProp);
            CombineGamePropertyManager.AddOrUpdate(manaProp);
            CombineGamePropertyManager.AddOrUpdate(staminaProp);

            Debug.Log($"管理器中的属性数量: {CombineGamePropertyManager.Count}");

            // 遍历所有属性
            foreach (var prop in CombineGamePropertyManager.GetAll())
            {
                Debug.Log($"属性 {prop.ID}: {prop.GetValue()}");
            }

            // 通过ID获取特定属性
            var health = CombineGamePropertyManager.Get("PlayerHealth");
            if (health != null)
            {
                Debug.Log($"获取到生命值属性: {health.GetValue()}");
            }

            // 直接获取属性内部的GameProperty
            var healthGameProp = CombineGamePropertyManager.GetGamePropertyFromCombine("PlayerHealth");
            if (healthGameProp != null)
            {
                healthGameProp.AddModifier(new FloatModifier(ModifierType.Add, 0, 50f));
                Debug.Log($"添加修饰器后生命值: {health.GetValue()}");
            }

            Debug.Log("\n=== 新功能演示：直接管理 GameProperty ===");

            // 5.1 直接包装 GameProperty（带修饰符）
            var baseDamage = new GameProperty("BaseDamage", 50f);
            baseDamage.AddModifier(new FloatModifier(ModifierType.Add, 0, 20f));      // +20
            baseDamage.AddModifier(new FloatModifier(ModifierType.Mul, 0, 1.5f));     // *1.5

            Debug.Log($"包装前 BaseDamage 值: {baseDamage.GetValue()}");              // (50+20)*1.5 = 105

            var wrappedDamage = CombineGamePropertyManager.Wrap(baseDamage);
            Debug.Log($"包装后的属性值: {wrappedDamage.GetValue()}");                  // 应该也是 105
            Debug.Log($"包装后 ResultHolder 的修饰符数量: {wrappedDamage.ResultHolder.ModifierCount}");  // 应该是 2

            // 5.2 使用便利方法进行链式调用
            var attackPower = new CombinePropertySingle("AttackPower", 100f);
            attackPower
                .SetBaseValue(120f)
                .AddModifier(new FloatModifier(ModifierType.Add, 0, 30f))
                .AddModifier(new FloatModifier(ModifierType.Mul, 0, 1.2f))
                .SubscribeValueChanged((oldVal, newVal) =>
                {
                    Debug.Log($"攻击力变化: {oldVal} -> {newVal}");
                });

            CombineGamePropertyManager.AddOrUpdate(attackPower);
            Debug.Log($"攻击力最终值: {attackPower.GetValue()}"); // (120+30)*1.2 = 180

            // 5.3 批量包装 GameProperty（带修饰符）
            var strength = new GameProperty("Strength", 20f);
            strength.AddModifier(new FloatModifier(ModifierType.Add, 0, 5f));  // +5

            var agility = new GameProperty("Agility", 15f);
            agility.AddModifier(new FloatModifier(ModifierType.Mul, 0, 1.2f)); // *1.2

            var intelligence = new GameProperty("Intelligence", 18f);

            var baseProperties = new List<GameProperty> { strength, agility, intelligence };

            Debug.Log("\n--- 批量包装带修饰符的属性 ---");
            foreach (var prop in baseProperties)
            {
                Debug.Log($"包装前 {prop.ID}: 值={prop.GetValue()}, 修饰符数={prop.ModifierCount}");
            }

            var wrappedProps = CombineGamePropertyManager.WrapRange(baseProperties).ToList();

            foreach (var wrapped in wrappedProps)
            {
                Debug.Log($"包装后 {wrapped.ID}: 值={wrapped.GetValue()}, 修饰符数={wrapped.ResultHolder.ModifierCount}");
            }

            Debug.Log($"\n批量包装了 {wrappedProps.Count} 个属性");

            // 5.4 使用类型安全的获取方法
            var singleProp = CombineGamePropertyManager.GetSingle("Strength");
            if (singleProp != null)
            {
                Debug.Log($"力量值: {singleProp.GetValue()}"); // 应该是 25 (20+5)
            }

            // 5.5 检查属性类型
            if (CombineGamePropertyManager.IsSingle("Strength"))
            {
                Debug.Log("Strength 是 CombinePropertySingle 类型");
            }

            // 5.6 验证修饰符复制的正确性
            Debug.Log("\n--- 验证修饰符复制 ---");
            var wrappedStrength = CombineGamePropertyManager.GetSingle("Strength");
            if (wrappedStrength != null)
            {
                Debug.Log($"Strength 包装后:");
                Debug.Log($"  - 基础值: {wrappedStrength.ResultHolder.GetBaseValue()}");
                Debug.Log($"  - 最终值: {wrappedStrength.GetValue()}");
                Debug.Log($"  - 修饰符数量: {wrappedStrength.ResultHolder.ModifierCount}");

                // 添加新的修饰符测试
                wrappedStrength.AddModifier(new FloatModifier(ModifierType.Add, 0, 10f));
                Debug.Log($"  - 添加修饰符后最终值: {wrappedStrength.GetValue()}"); // 应该是 35 (20+5+10)
            }

            Debug.Log("属性管理器示例完成\n");
        }

        /// <summary>
        /// 示例6：真实RPG游戏案例
        /// 学习目标：了解在实际游戏中如何应用GameProperty系统
        /// </summary>
        void Example_6_RealWorldRPGExample()
        {
            Debug.Log("=== 示例6：真实RPG游戏案例 ===");

            // 创建角色基础属性
            var strength = new GameProperty("Strength", 15f);
            var agility = new GameProperty("Agility", 12f);
            var intelligence = new GameProperty("Intelligence", 10f);
            var vitality = new GameProperty("Vitality", 18f);

            // 创建派生属性
            var maxHealth = new GameProperty("MaxHealth", 0f);
            var maxMana = new GameProperty("MaxMana", 0f);
            var physicalAttack = new GameProperty("PhysicalAttack", 0f);
            var magicAttack = new GameProperty("MagicAttack", 0f);
            var defense = new GameProperty("Defense", 0f);
            var attackSpeed = new GameProperty("AttackSpeed", 1f);

            // 建立属性依赖关系
            maxHealth.AddDependency(vitality, (dep, newVal) => newVal * 10f + 50f);
            maxMana.AddDependency(intelligence, (dep, newVal) => newVal * 5f + 20f);
            physicalAttack.AddDependency(strength, (dep, newVal) => newVal * 2f + 10f);
            magicAttack.AddDependency(intelligence, (dep, newVal) => newVal * 1.5f + 5f);
            defense.AddDependency(vitality, (dep, newVal) => newVal * 1.2f + 8f);
            attackSpeed.AddDependency(agility, (dep, newVal) => 1f + newVal * 0.08f);

            Debug.Log("=== 角色初始属性 ===");
            Debug.Log($"力量: {strength.GetValue()}, 敏捷: {agility.GetValue()}, 智力: {intelligence.GetValue()}, 体力: {vitality.GetValue()}");
            Debug.Log($"最大生命: {maxHealth.GetValue()}, 最大法力: {maxMana.GetValue()}");
            Debug.Log($"物理攻击: {physicalAttack.GetValue()}, 魔法攻击: {magicAttack.GetValue()}");
            Debug.Log($"防御力: {defense.GetValue()}, 攻击速度: {attackSpeed.GetValue()}");

            // 模拟装备系统
            Debug.Log("\n=== 装备武器和防具 ===");
            physicalAttack.AddModifier(new FloatModifier(ModifierType.Add, 0, 25f)); // 武器+25攻击
            defense.AddModifier(new FloatModifier(ModifierType.Add, 0, 15f));        // 防具+15防御
            attackSpeed.AddModifier(new FloatModifier(ModifierType.Mul, 0, 1.1f));   // 装备+10%攻击速度

            Debug.Log($"装备后 - 物理攻击: {physicalAttack.GetValue()}, 防御: {defense.GetValue()}, 攻击速度: {attackSpeed.GetValue()}");

            // 模拟技能/魔法Buff系统
            Debug.Log("\n=== 施放强化魔法 ===");
            var strengthBuff = new FloatModifier(ModifierType.Add, 1, 8f);  // 力量+8
            var speedBuff = new FloatModifier(ModifierType.Mul, 1, 1.3f);   // 攻击速度+30%

            strength.AddModifier(strengthBuff);
            attackSpeed.AddModifier(speedBuff);

            Debug.Log($"Buff后 - 力量: {strength.GetValue()}, 物理攻击: {physicalAttack.GetValue()}, 攻击速度: {attackSpeed.GetValue()}");

            // 模拟角色升级
            Debug.Log("\n=== 角色升级 ===");
            strength.SetBaseValue(18f);
            agility.SetBaseValue(15f);
            intelligence.SetBaseValue(12f);
            vitality.SetBaseValue(22f);

            Debug.Log($"升级后 - 力量: {strength.GetValue()}, 敏捷: {agility.GetValue()}, 智力: {intelligence.GetValue()}, 体力: {vitality.GetValue()}");
            Debug.Log($"升级后 - 最大生命: {maxHealth.GetValue()}, 物理攻击: {physicalAttack.GetValue()}, 防御: {defense.GetValue()}");

            // 计算最终DPS
            var finalDPS = physicalAttack.GetValue() * attackSpeed.GetValue();
            Debug.Log($"最终DPS: {finalDPS}");

            Debug.Log("真实RPG游戏案例完成\n");
        }

        /// <summary>
        /// 示例7：序列化与反序列化
        /// 学习目标：了解如何保存和加载属性数据
        /// </summary>
        void Example_7_SerializationExample()
        {
            Debug.Log("=== 示例7：序列化与反序列化（使用新统一序列化API） ===");

            // 确保序列化器已初始化
            GamePropertySerializationInitializer.ManualInitialize();

            // 创建一个复杂的属性
            var playerPower = new GameProperty("PlayerPower", 100f);
            playerPower.AddModifier(new FloatModifier(ModifierType.Add, 0, 50f));
            playerPower.AddModifier(new FloatModifier(ModifierType.Mul, 1, 1.5f));
            playerPower.AddModifier(new RangeModifier(ModifierType.Clamp, 2, new Vector2(0, 300)));

            Debug.Log($"原始属性值: {playerPower.GetValue()}");

            // 使用新的统一序列化服务进行序列化
            string jsonData = SerializationServiceManager.SerializeToJson(playerPower);
            Debug.Log($"序列化成功 (JSON): {jsonData}");

            // 使用新的统一序列化服务进行反序列化
            var deserializedProp = SerializationServiceManager.DeserializeFromJson<GameProperty>(jsonData);
            Debug.Log($"反序列化后属性值: {deserializedProp.GetValue()}");
            Debug.Log($"反序列化后修饰器数量: {deserializedProp.ModifierCount}");

            // 验证序列化完整性
            bool isEqual = Mathf.Approximately(playerPower.GetValue(), deserializedProp.GetValue());
            Debug.Log($"序列化完整性验证: {(isEqual ? "成功" : "失败")}");

            // 注意：依赖关系不会被序列化
            Debug.Log("注意：新的序列化系统只序列化属性数据本身，不序列化依赖关系");
            Debug.Log("如需序列化依赖关系，请在反序列化后手动重建");

            Debug.Log("序列化与反序列化示例完成\n");
        }

        /// <summary>
        /// 示例8：高级特性
        /// 学习目标：了解一些高级用法和最佳实践
        /// </summary>
        void Example_8_AdvancedFeatures()
        {
            Debug.Log("=== 示例8：高级特性 ===");

            // 8.1 脏数据追踪
            var trackedProp = new GameProperty("TrackedProp", 50f);
            trackedProp.OnDirty(() => Debug.Log("属性被标记为脏数据"));

            trackedProp.AddModifier(new FloatModifier(ModifierType.Add, 0, 10f));
            trackedProp.GetValue(); // 触发脏数据回调

            // 8.2 属性查询功能
            var queryProp = new GameProperty("QueryProp", 80f);
            queryProp.AddModifier(new FloatModifier(ModifierType.Add, 0, 20f));
            queryProp.AddModifier(new FloatModifier(ModifierType.Mul, 0, 1.5f));
            queryProp.AddModifier(new FloatModifier(ModifierType.Add, 1, 30f));

            Debug.Log($"是否有修饰器: {queryProp.HasModifiers}");
            Debug.Log($"修饰器总数: {queryProp.ModifierCount}");
            Debug.Log($"Add类型修饰器数量: {queryProp.GetModifierCountOfType(ModifierType.Add)}");
            Debug.Log($"是否有Mul类型修饰器: {queryProp.ContainModifierOfType(ModifierType.Mul)}");

            // 8.3 复杂依赖链
            var chainA = new GameProperty("ChainA", 10f);
            var chainB = new GameProperty("ChainB", 0f);
            var chainC = new GameProperty("ChainC", 0f);
            var chainD = new GameProperty("ChainD", 0f);

            // 建立依赖链: A -> B -> C -> D
            chainB.AddDependency(chainA, (dep, newVal) => newVal * 2f);
            chainC.AddDependency(chainB, (dep, newVal) => newVal + 5f);
            chainD.AddDependency(chainC, (dep, newVal) => newVal * 1.5f);

            Debug.Log($"依赖链初始值 - A: {chainA.GetValue()}, B: {chainB.GetValue()}, C: {chainC.GetValue()}, D: {chainD.GetValue()}");

            chainA.SetBaseValue(15f);
            Debug.Log($"修改A后 - A: {chainA.GetValue()}, B: {chainB.GetValue()}, C: {chainC.GetValue()}, D: {chainD.GetValue()}");

            // 8.4 共享属性应用
            var sharedStat = new GameProperty("SharedStat", 100f);

            var derivedProp1 = new CombinePropertyCustom("Derived1");
            var derivedProp2 = new CombinePropertyCustom("Derived2");

            derivedProp1.RegisterProperty(sharedStat);
            derivedProp2.RegisterProperty(sharedStat);

            derivedProp1.Calculater = (combine) => combine.GetProperty("SharedStat").GetValue() * 0.8f;
            derivedProp2.Calculater = (combine) => combine.GetProperty("SharedStat").GetValue() * 1.2f;

            Debug.Log($"共享属性应用 - 原始: {sharedStat.GetValue()}, 派生1: {derivedProp1.GetValue()}, 派生2: {derivedProp2.GetValue()}");

            sharedStat.SetBaseValue(150f);
            Debug.Log($"修改共享属性后 - 原始: {sharedStat.GetValue()}, 派生1: {derivedProp1.GetValue()}, 派生2: {derivedProp2.GetValue()}");

            Debug.Log("高级特性示例完成\n");
        }

        /// <summary>
        /// 示例9：错误处理和最佳实践
        /// 学习目标：了解各种错误情况以及如何正确处理它们
        /// </summary>
        void Example_9_ErrorHandlingAndBestPractices()
        {
            Debug.Log("=== 示例9：错误处理和最佳实践 ===");

            // 9.1 访问已释放的对象 (ObjectDisposedException)
            Debug.Log("9.1 测试访问已释放的对象");
            var disposableProperty = new CombinePropertySingle("DisposableTest", 100f);
            Debug.Log($"释放前属性值: {disposableProperty.GetValue()}");

            // 释放对象
            disposableProperty.Dispose();
            Debug.Log("对象已释放");

            // 尝试访问已释放的对象
            try
            {
                var value = disposableProperty.GetValue();
                Debug.Log($"意外成功获取值: {value}");
            }
            catch (System.ObjectDisposedException ex)
            {
                Debug.Log($"✓ 正确捕获ObjectDisposedException: {ex.Message}");
            }

            // 9.2 循环依赖错误处理
            Debug.Log("\n9.2 测试循环依赖检测");
            var circularA = new GameProperty("CircularA", 10f);
            var circularB = new GameProperty("CircularB", 20f);
            var circularC = new GameProperty("CircularC", 30f);

            // 建立依赖链: A -> B -> C
            circularA.AddDependency(circularB, (dep, newVal) => newVal * 2f);
            circularB.AddDependency(circularC, (dep, newVal) => newVal + 5f);
            Debug.Log($"建立依赖链 A->B->C: A={circularA.GetValue()}, B={circularB.GetValue()}, C={circularC.GetValue()}");

            // 尝试创建循环依赖: C -> A (这会形成循环)
            try
            {
                circularC.AddDependency(circularA, (dep, newVal) => newVal * 0.5f);
                Debug.Log($"循环依赖创建后: A={circularA.GetValue()}, B={circularB.GetValue()}, C={circularC.GetValue()}");
            }
            catch (System.Exception ex)
            {
                Debug.Log($"✓ 循环依赖被正确阻止: {ex.Message}");
            }

            // 9.3 自依赖错误处理
            Debug.Log("\n9.3 测试自依赖检测");
            var selfDependentProp = new GameProperty("SelfDependent", 100f);

            try
            {
                selfDependentProp.AddDependency(selfDependentProp, (dep, newVal) => newVal * 2f);
                Debug.Log($"自依赖创建后值: {selfDependentProp.GetValue()}");
            }
            catch (System.Exception ex)
            {
                Debug.Log($"✓ 自依赖被正确阻止: {ex.Message}");
            }

            // 9.4 空值处理
            Debug.Log("\n9.4 测试空值处理");
            var nullTestProp = new GameProperty("NullTest", 50f);

            try
            {
                nullTestProp.AddDependency(null);
                Debug.Log("空依赖意外成功添加");
            }
            catch (System.ArgumentNullException ex)
            {
                Debug.Log($"✓ 正确处理空依赖: {ex.Message}");
            }

            try
            {
                nullTestProp.AddModifier(null);
                Debug.Log("空修饰器意外成功添加");
            }
            catch (System.ArgumentNullException ex)
            {
                Debug.Log($"✓ 正确处理空修饰器: {ex.Message}");
            }

            // 9.5 管理器错误处理
            Debug.Log("\n9.5 测试管理器错误处理");
            var testManager = new GamePropertyManager();

            // 添加空属性
            testManager.AddOrUpdate(null);
            Debug.Log($"添加空属性后管理器数量: {testManager.Count}");

            // 获取不存在的属性
            var nonExistentProp = testManager.Get("NonExistent");
            Debug.Log($"获取不存在属性结果: {(nonExistentProp == null ? "null" : nonExistentProp.ID)}");

            // 获取空ID的属性
            var nullIdProp = testManager.Get(null);
            Debug.Log($"获取空ID属性结果: {(nullIdProp == null ? "null" : nullIdProp.ID)}");

            // 9.6 最佳实践示例
            Debug.Log("\n9.6 最佳实践示例");

            // 安全的属性访问模式
            var safeProperty = new CombinePropertySingle("SafeTest", 100f);
            CombineGamePropertyManager.AddOrUpdate(safeProperty);

            // 使用IsValid检查对象状态
            if (safeProperty.IsValid())
            {
                Debug.Log($"✓ 安全访问属性值: {safeProperty.GetValue()}");
            }

            // 安全的依赖建立
            var baseProp = new GameProperty("BaseProp", 10f);
            var dependentProp = new GameProperty("DependentProp", 0f);

            if (baseProp != null && dependentProp != null && baseProp != dependentProp)
            {
                dependentProp.AddDependency(baseProp, (dep, newVal) => newVal * 3f);
                Debug.Log($"✓ 安全建立依赖: {dependentProp.GetValue()}");
            }

            // 安全的管理器操作
            var retrievedProp = CombineGamePropertyManager.Get("SafeTest");
            if (retrievedProp != null && retrievedProp.IsValid())
            {
                Debug.Log($"✓ 安全从管理器获取属性: {retrievedProp.GetValue()}");
            }

            // 9.7 资源清理最佳实践
            Debug.Log("\n9.7 资源清理最佳实践");

            // 创建临时属性
            var tempProp = new CombinePropertySingle("TempProp", 50f);
            CombineGamePropertyManager.AddOrUpdate(tempProp);

            Debug.Log($"临时属性创建: {tempProp.GetValue()}");

            // 手动清理
            tempProp.Dispose();
            Debug.Log("手动释放临时属性");

            // 清理管理器中的无效属性
            int cleanedCount = CombineGamePropertyManager.CleanupInvalidProperties();
            Debug.Log($"✓ 清理无效属性数量: {cleanedCount}");

            // 最终清理
            CombineGamePropertyManager.Clear();
            Debug.Log("✓ 管理器已清理");

            Debug.Log("错误处理和最佳实践示例完成\n");
        }
    }
}
