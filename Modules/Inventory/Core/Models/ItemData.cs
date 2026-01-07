using EasyPack.CustomData;

namespace EasyPack.InventorySystem
{
    /// <summary>
    ///     物品模板数据，定义物品的静态属性。
    /// </summary>
    public class ItemData
    {
        /// <summary>物品定义ID（如"apple"）</summary>
        public string ID { get; set; }

        /// <summary>物品显示名称</summary>
        public string Name { get; set; }

        /// <summary>分类路径（如"Food.Fruit"），用于CategoryManager注册</summary>
        public string Category { get; set; }

        /// <summary>默认标签（如["consumable", "stackable"]），Item创建时拷贝</summary>
        public string[] DefaultTags { get; set; } = System.Array.Empty<string>();

        /// <summary>默认元数据（如{"nutrition": 10}），Item创建时深拷贝为RuntimeMetadata</summary>
        public CustomDataCollection DefaultMetadata { get; set; }

        /// <summary>是否可堆叠（默认true）</summary>
        public bool IsStackable { get; set; } = true;

        /// <summary>最大堆叠数（-1表示无限，默认1）</summary>
        public int MaxStackCount { get; set; } = 1;

        /// <summary>物品描述</summary>
        public string Description { get; set; }

        /// <summary>物品权重</summary>
        public float Weight { get; set; } = 1f;

        /// <summary>
        ///     克隆ItemData，创建新的模板实例
        /// </summary>
        /// <param name="newId">新ID（可选）</param>
        /// <returns>克隆的ItemData实例</returns>
        public ItemData Clone(string newId = null)
        {
            return new ItemData
            {
                ID = newId ?? ID,
                Name = Name,
                Category = Category,
                DefaultTags = DefaultTags?.Length > 0 ? (string[])DefaultTags.Clone() : System.Array.Empty<string>(),
                DefaultMetadata = DefaultMetadata?.Clone(),
                IsStackable = IsStackable,
                MaxStackCount = MaxStackCount,
                Description = Description,
                Weight = Weight
            };
        }
    }
}
