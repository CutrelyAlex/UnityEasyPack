using System;
using System.Linq;
using System.Threading.Tasks;
using EasyPack.Architecture;
using EasyPack.CustomData;
using UnityEngine;

namespace EasyPack.GamePropertySystem.Example
{
    /// <summary>
    ///     分类查询示例
    ///     演示GamePropertyManager的分类系统和标签查询功能
    /// </summary>
    public class CategoryQueryExample : MonoBehaviour
    {
        private IGamePropertyService _manager;

        private async void Start()
        {
            try
            {
                // 从 EasyPack 架构获取 GamePropertyManager 服务
                _manager = await EasyPackArchitecture.Instance.ResolveAsync<IGamePropertyService>();

                // 清理之前的示例数据
                CleanupExampleData();

                await DemoHierarchicalCategories();
                await DemoTagQueries();
                await DemoCombinedQueries();
                await DemoWildcardQueries();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CategoryQueryExample] 示例执行失败: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void CleanupExampleData()
        {
            string[] exampleIds =
            {
                "hp", "hpRegen", "mp", "mpRegen", "strength", "intelligence", "hp_tag", "mp_tag", "level_tag",
                "tempBuff", "hp_comb", "mp_comb", "attack_comb", "defense_comb", "crit_wild", "critDamage_wild",
                "armor_wild", "magicResist_wild",
            };

            foreach (string id in exampleIds)
            {
                _manager.Unregister(id);
            }
        }

        /// <summary>
        ///     示例1: 层级分类系统
        /// </summary>
        private Task DemoHierarchicalCategories()
        {
            Debug.Log("=== 示例1: 层级分类系统 ===");

            // 创建层级分类结构
            _manager.Register(new("hp", 100), "Character.Vital.Health");
            _manager.Register(new("hpRegen", 5), "Character.Vital.Health");
            _manager.Register(new("mp", 50), "Character.Vital.Mana");
            _manager.Register(new("mpRegen", 2), "Character.Vital.Mana");
            _manager.Register(new("strength", 10), "Character.Base.Physical");
            _manager.Register(new("intelligence", 8), "Character.Base.Mental");

            // 查询特定分类
            var healthProps = _manager.GetByCategory("Character.Vital.Health");
            Debug.Log($"Health分类属性: {string.Join(", ", healthProps.Select(p => p.ID))}");

            // 查询包含子分类
            var vitalProps = _manager.GetByCategory("Character.Vital", true);
            Debug.Log($"Vital分类及子分类属性: {string.Join(", ", vitalProps.Select(p => p.ID))}");

            // 查询顶层分类
            var characterProps = _manager.GetByCategory("Character", true);
            Debug.Log($"所有Character属性数量: {characterProps.Count()}");

            return Task.CompletedTask;
        }

        /// <summary>
        ///     示例2: 标签查询
        /// </summary>
        private Task DemoTagQueries()
        {
            Debug.Log("=== 示例2: 标签查询 ===");

            // 注册属性并添加标签
            _manager.Register(new("hp_tag", 100), "Character",
                null, new[] { "vital", "displayInUI", "saveable" });

            _manager.Register(new("mp_tag", 50), "Character",
                null, new[] { "vital", "displayInUI", "saveable" });

            _manager.Register(new("level_tag", 1), "Character",
                null, new[] { "displayInUI", "saveable" });

            _manager.Register(new("tempBuff", 10), "Character",
                null, new[] { "temporary" });

            // 按标签查询
            var vitalProps = _manager.GetByTag("vital");
            Debug.Log($"Vital标签属性: {string.Join(", ", vitalProps.Select(p => p.ID))}");

            var uiProps = _manager.GetByTag("displayInUI");
            Debug.Log($"UI显示属性: {string.Join(", ", uiProps.Select(p => p.ID))}");

            var saveableProps = _manager.GetByTag("saveable");
            Debug.Log($"可保存属性: {string.Join(", ", saveableProps.Select(p => p.ID))}");

            var tempProps = _manager.GetByTag("temporary");
            Debug.Log($"临时属性: {string.Join(", ", tempProps.Select(p => p.ID))}");

            return Task.CompletedTask;
        }

        /// <summary>
        ///     示例3: 组合查询（分类 + 标签）
        /// </summary>
        private Task DemoCombinedQueries()
        {
            Debug.Log("=== 示例3: 组合查询 ===");

            // 创建角色属性
            _manager.Register(new("hp_comb", 100), "Character.Vital",
                null, new[] { "combat", "ui" });

            _manager.Register(new("mp_comb", 50), "Character.Vital",
                null, new[] { "ui" });

            _manager.Register(new("attack_comb", 20), "Character.Combat",
                null, new[] { "combat", "ui" });

            _manager.Register(new("defense_comb", 15), "Character.Combat",
                null, new[] { "combat" });

            // 组合查询: Vital分类 + combat标签
            var vitalCombatProps = _manager.GetByCategoryAndTag("Character.Vital", "combat");
            Debug.Log($"Vital + Combat: {string.Join(", ", vitalCombatProps.Select(p => p.ID))}");

            // 组合查询: Combat分类 + ui标签
            var combatUIProps = _manager.GetByCategoryAndTag("Character.Combat", "ui");
            Debug.Log($"Combat + UI: {string.Join(", ", combatUIProps.Select(p => p.ID))}");

            return Task.CompletedTask;
        }

        /// <summary>
        ///     示例4: 通配符查询和动态分类
        /// </summary>
        private Task DemoWildcardQueries()
        {
            Debug.Log("=== 示例4: 通配符查询 ===");

            // 创建复杂的层级结构
            _manager.Register(new("crit_wild", 0.05f), "Character.Combat.Offense.Critical");
            _manager.Register(new("critDamage_wild", 1.5f), "Character.Combat.Offense.Critical");
            _manager.Register(new("armor_wild", 10), "Character.Combat.Defense.Physical");
            _manager.Register(new("magicResist_wild", 5), "Character.Combat.Defense.Magical");

            // 查询所有Offense属性
            var offenseProps = _manager.GetByCategory("Character.Combat.Offense", true);
            Debug.Log($"所有攻击属性: {string.Join(", ", offenseProps.Select(p => p.ID))}");

            // 查询所有Defense属性
            var defenseProps = _manager.GetByCategory("Character.Combat.Defense", true);
            Debug.Log($"所有防御属性: {string.Join(", ", defenseProps.Select(p => p.ID))}");

            // 查询所有Combat属性
            var combatProps = _manager.GetByCategory("Character.Combat", true);
            Debug.Log($"所有战斗属性: {string.Join(", ", combatProps.Select(p => p.ID))}");

            return Task.CompletedTask;
        }

        /// <summary>
        ///     示例5: 自由分类命名
        /// </summary>
        public async Task DemoFlexibleCategoryNaming()
        {
            Debug.Log("=== 示例5: 自由分类命名 ===");

            _manager = new GamePropertyService();
            await _manager.InitializeAsync();

            // 支持各种分类命名风格
            _manager.Register(new("test1", 1), "角色_战斗_攻击");
            _manager.Register(new("test2", 2), "Character-Combat-Attack");
            _manager.Register(new("test3", 3), "Character/Combat/Attack");
            _manager.Register(new("test4", 4), "Character.Combat.Attack");

            // 所有分类都有效
            var categories = _manager.GetAllCategories();
            Debug.Log($"所有分类: {string.Join(", ", categories)}");

            // 可以按各自的命名规则查询
            var prop1 = _manager.GetByCategory("角色_战斗_攻击");
            var prop2 = _manager.GetByCategory("Character-Combat-Attack");
            var prop3 = _manager.GetByCategory("Character/Combat/Attack");
            var prop4 = _manager.GetByCategory("Character.Combat.Attack");

            Debug.Log($"各种命名风格都可查询: {prop1.Count()}, {prop2.Count()}, {prop3.Count()}, {prop4.Count()}");
        }

        /// <summary>
        ///     示例6: 动态UI生成
        /// </summary>
        public Task DemoDynamicUIGeneration()
        {
            Debug.Log("=== 示例6: 动态UI生成 ===");

            // 注册UI显示属性
            var uiProps = new[]
            {
                ("hp_ui", 100f, "生命值", "Icons/HP", 1), ("mp_ui", 50f, "魔法值", "Icons/MP", 2),
                ("stamina_ui", 80f, "耐力", "Icons/Stamina", 3),
            };

            foreach ((string id, float value, string displayName, string icon, int order) in uiProps)
            {
                var property = new GameProperty(id, value);
                var metadata = new PropertyDisplayInfo { DisplayName = displayName, IconPath = icon };

                string[] tags = new[] { "displayInUI" };
                var customData = new CustomData.CustomDataCollection();
                customData.Set("sortOrder", order);

                _manager.Register(property, "Character.Resources", metadata, tags, customData);
            }

            // 模拟UI系统查询和显示
            var displayProps = _manager.GetByTag("displayInUI");
            Debug.Log("UI面板生成:");
            foreach (GameProperty prop in displayProps.OrderBy(p =>
                     {
                         CustomDataCollection customData = _manager.GetCustomData(p.ID);
                         return customData?.Get<int>("sortOrder") ?? 999;
                     }))
            {
                PropertyDisplayInfo meta = _manager.GetPropertyDisplayInfo(prop.ID);
                Debug.Log($"  [{meta.IconPath}] {meta.DisplayName}: {prop.GetValue()}");
            }

            return Task.CompletedTask;
        }

        private void OnDestroy()
        {
            _manager?.Dispose();
        }
    }
}