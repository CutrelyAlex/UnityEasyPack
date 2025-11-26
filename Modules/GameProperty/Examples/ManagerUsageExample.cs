using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using EasyPack.Architecture;
using EasyPack.Modifiers;

namespace EasyPack.GamePropertySystem.Example
{
    /// <summary>
    ///     GamePropertyManager使用示例
    ///     演示新API的注册、查询、批量操作等功能
    /// </summary>
    public class ManagerUsageExample : MonoBehaviour
    {
        private IGamePropertyService _manager;

        private async void Start()
        {
            try
            {
                // 从 EasyPack 架构获取 GamePropertyManager 服务
                _manager = await EasyPackArchitecture.Instance.ResolveAsync<IGamePropertyService>();

                // 清理之前可能存在的示例数据
                CleanupExampleData();

                await DemoBasicUsage();
                await DemoMetadataUsage();
                await DemoBatchOperations();
                await DemoAdvancedQueries();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ManagerUsageExample] 示例执行失败: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        ///     清理之前运行的示例数据
        /// </summary>
        private void CleanupExampleData()
        {
            string[] exampleIds = new[]
            {
                "hp", "mp", "strength", "hp_meta", "hp_batch", "mp_batch", "stamina_batch", "hp_adv", "mp_adv",
                "crit_adv",
            };

            foreach (string id in exampleIds) _manager.Unregister(id);
        }

        /// <summary>
        ///     示例1: 基础用法 - 注册和查询
        /// </summary>
        private Task DemoBasicUsage()
        {
            Debug.Log("=== 示例1: 基础用法 ===");

            // 1. 创建角色基础属性
            var hp = new GameProperty("hp", 100);
            var mp = new GameProperty("mp", 50);
            var strength = new GameProperty("strength", 10);

            // 2. 注册到分类
            _manager.Register(hp, "Character.Vital");
            _manager.Register(mp, "Character.Vital");
            _manager.Register(strength, "Character.Base");

            // 3. 查询属性
            GameProperty retrievedHp = _manager.Get("hp");
            Debug.Log($"生命值: {retrievedHp.GetValue()}");

            // 4. 按分类查询
            var vitalProps = _manager.GetByCategory("Character.Vital");
            foreach (GameProperty prop in vitalProps) Debug.Log($"重要属性: {prop.ID} = {prop.GetValue()}");

            return Task.CompletedTask;
        }

        /// <summary>
        ///     示例2: 元数据用法
        /// </summary>
        private Task DemoMetadataUsage()
        {
            Debug.Log("=== 示例2: 元数据用法 ===");

            // 创建属性并附加元数据
            var hp = new GameProperty("hp_meta", 100);
            var metadata = new PropertyMetadata
            {
                DisplayName = "生命值",
                Description = "角色当前生命值",
                IconPath = "Icons/Stats/HP",
                Tags = new[] { "vital", "displayInUI", "saveable" },
            };

            // 设置自定义数据
            metadata.SetCustomData("localizationKey", "stats.hp");
            metadata.SetCustomData("color", "#FF0000");
            metadata.SetCustomData("sortOrder", 1);

            _manager.Register(hp, "Character.Vital", metadata);

            // 获取并使用元数据
            PropertyMetadata retrievedMeta = _manager.GetMetadata("hp_meta");
            Debug.Log($"显示名: {retrievedMeta.DisplayName}");
            Debug.Log($"描述: {retrievedMeta.Description}");
            Debug.Log($"标签: {string.Join(", ", retrievedMeta.Tags)}");

            // 使用自定义数据
            string color = retrievedMeta.GetCustomData<string>("color");
            Debug.Log($"UI颜色: {color}");

            int sortOrder = retrievedMeta.GetCustomData<int>("sortOrder");
            Debug.Log($"排序顺序: {sortOrder}");

            return Task.CompletedTask;
        }

        /// <summary>
        ///     示例3: 批量操作
        /// </summary>
        private Task DemoBatchOperations()
        {
            Debug.Log("=== 示例3: 批量操作 ===");

            // 批量注册
            var properties = new[]
            {
                new GameProperty("hp_batch", 100), new GameProperty("mp_batch", 50),
                new GameProperty("stamina_batch", 80),
            };
            _manager.RegisterRange(properties, "Character.Resources");

            // 批量应用修饰符
            var buffModifier = new FloatModifier(ModifierType.Mul, 100, 1.5f);
            var result = _manager.ApplyModifierToCategory("Character.Resources", buffModifier);

            if (result.IsFullSuccess)
            {
                Debug.Log($"成功为 {result.SuccessCount} 个属性应用BUFF");
                foreach (string propId in result.SuccessData)
                {
                    GameProperty prop = _manager.Get(propId);
                    Debug.Log($"{propId}: {prop.GetValue()}");
                }
            }
            else
            {
                Debug.LogWarning($"部分成功: {result.SuccessCount}/{result.SuccessCount + result.Failures.Count}");
                foreach (FailureRecord failure in result.Failures)
                    Debug.LogError($"失败: {failure.ItemId} - {failure.ErrorMessage}");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        ///     示例4: 高级查询
        /// </summary>
        private Task DemoAdvancedQueries()
        {
            Debug.Log("=== 示例4: 高级查询 ===");

            // 创建层级分类
            _manager.Register(new("hp_adv", 100), "Character.Vital.HP",
                new() { Tags = new[] { "vital", "ui" } });
            _manager.Register(new("mp_adv", 50), "Character.Vital.MP",
                new() { Tags = new[] { "vital", "ui" } });
            _manager.Register(new("crit_adv", 0.1f), "Character.Combat.Offense",
                new() { Tags = new[] { "combat", "ui" } });

            // 查询所有Vital子分类
            var vitalProps = _manager.GetByCategory("Character.Vital", true);
            Debug.Log($"Vital分类属性数: {vitalProps.Count()}");

            // 按标签查询
            var uiProps = _manager.GetByTag("ui");
            Debug.Log($"UI显示属性数: {uiProps.Count()}");

            // 组合查询: Vital分类 + UI标签
            var vitalUIProps = _manager.GetByCategoryAndTag("Character.Vital", "ui");
            foreach (GameProperty prop in vitalUIProps) Debug.Log($"Vital UI属性: {prop.ID}");

            // 获取所有分类
            var categories = _manager.GetAllCategories();
            Debug.Log($"所有分类: {string.Join(", ", categories)}");

            return Task.CompletedTask;
        }

        /// <summary>
        ///     示例5: 属性依赖系统集成
        /// </summary>
        public async Task DemoDependencyIntegration()
        {
            Debug.Log("=== 示例5: 属性依赖系统 ===");

            _manager = new GamePropertyService();
            await _manager.InitializeAsync();

            // 创建基础属性
            var baseDamage = new GameProperty("baseDamage", 50);
            var strength = new GameProperty("strength", 10);

            // 创建派生属性（依赖于strength）
            var finalDamage = new GameProperty("finalDamage", 0);
            finalDamage.AddDependency(baseDamage, (dep, val) => val);
            finalDamage.AddDependency(strength, (dep, val) => val * 2);

            // 注册所有属性
            _manager.Register(baseDamage, "Character.Base");
            _manager.Register(strength, "Character.Base");
            _manager.Register(finalDamage, "Character.Derived");

            Debug.Log($"初始伤害: {finalDamage.GetValue()}"); // 50 + 10*2 = 70

            // 修改基础属性，派生属性自动更新
            strength.SetBaseValue(20);
            Debug.Log($"提升力量后伤害: {finalDamage.GetValue()}"); // 50 + 20*2 = 90
        }

        private void OnDestroy() { _manager?.Dispose(); }
    }
}