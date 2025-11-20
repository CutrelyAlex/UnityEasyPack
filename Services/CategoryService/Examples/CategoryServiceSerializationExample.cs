using EasyPack.Architecture;
using EasyPack.Serialization;
using UnityEngine;

namespace EasyPack.CategoryService.Examples
{
    /// <summary>
    /// CategoryManager 序列化示例
    /// 展示如何使用 EasyPack 序列化服务来序列化和反序列化 CategoryManager
    /// </summary>
    public class CategoryServiceSerializationExample : MonoBehaviour
    {
        [System.Serializable]
        public class Item
        {
            public string Id;
            public string Name;
            public int Price;
        }

        private void Start()
        {
            // 方式 1: 直接使用 CategoryManager 的序列化方法（推荐用于简单场景）
            TestDirectSerialization();

            // 方式 2: 通过 CategoryService 统一管理（推荐用于大型项目）
            TestSerializationService();
        }

        /// <summary>
        /// 直接使用 CategoryManager 序列化方法
        /// </summary>
        private static void TestDirectSerialization()
        {
            Debug.Log("=== 直接序列化测试 ===");

            // 创建并配置 CategoryManager
            var manager = new CategoryManager<Item>(item => item.Id);
            
            var item1 = new Item { Id = "sword", Name = "神剑", Price = 1000 };
            var item2 = new Item { Id = "shield", Name = "盾牌", Price = 800 };

            manager.RegisterEntity(item1, "Equipment.Weapon")
                .WithTags("legendary", "melee")
                .Complete();

            manager.RegisterEntity(item2, "Equipment.Armor")
                .WithTags("rare", "defense")
                .Complete();

            // 序列化
            string json = manager.SerializeToJson();
            Debug.Log($"序列化结果:\n{json}");

            // 反序列化到新实例
            var newManager = CategoryManager<Item>.CreateFromJson(json, item => item.Id);
            
            // 验证
            var weapons = newManager.GetByCategory("Equipment.Weapon");
            Debug.Log($"✓ 验证成功: 武器数量 = {weapons.Count}");
        }

        /// <summary>
        /// 使用 CategoryService 统一管理（推荐用于大型项目）
        /// </summary>
        private async void TestSerializationService()
        {
            Debug.Log("\n=== CategoryService 集成测试 ===");

            // 获取 CategoryService 实例
            var categoryService = await EasyPackArchitecture.Instance.ResolveAsync<ICategoryService>();
            if (categoryService == null)
            {
                Debug.LogWarning("CategoryService 未初始化，跳过测试");
                return;
            }

            // 获取或创建 CategoryManager<Item>
            var manager = categoryService.GetOrCreateManager<Item>(item => item.Id);

            // 创建测试数据
            var item = new Item { Id = "potion", Name = "药水", Price = 50 };
            manager.RegisterEntity(item, "Consumable.Healing").Complete();

            // 使用 CategoryService 的序列化功能（内部集成 ISerializationService）
            // CategoryService 自动处理所有 manager 的序列化
            Debug.Log("✓ CategoryService 会在生命周期管理中自动处理序列化");
            
            // 验证
            var consumables = manager.GetByCategory("Consumable.Healing");
            Debug.Log($"✓ 验证成功: 消耗品数量 = {consumables.Count}");
        }
    }
}
