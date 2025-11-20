using EasyPack.BuffSystem;
using EasyPack.CategoryService;
using EasyPack.ENekoFramework;
using EasyPack.GamePropertySystem;
using EasyPack.InventorySystem;
using EasyPack.ObjectPool;
using EasyPack.Serialization;
using System.Threading.Tasks;

namespace EasyPack.Architecture
{
    /// <summary>
    /// EasyPack 统一架构容器
    /// 封装所有 EasyPack 系统和服务
    /// </summary>
    public class EasyPackArchitecture : ENekoArchitecture<EasyPackArchitecture>
    {
        protected override void OnInit()
        {

            // Moules
            Container.Register<IGamePropertyService, GamePropertyService>();
            Container.Register<IBuffService, BuffService>();
            Container.Register<IInventoryService, InventoryService>();

            // Services
            Container.Register<ICategoryService, CategoryService.CategoryService>();
            Container.Register<ISerializationService, SerializationService>();
            Container.Register<IObjectPoolService, ObjectPoolService>();
        }

        public static async Task<ISerializationService> GetSerializationServiceAsync()
        {
            return await Instance.Container.ResolveAsync<ISerializationService>();
        }

        public static async Task<IInventoryService> GetInventoryServiceAsync()
        {
            return await Instance.Container.ResolveAsync<IInventoryService>();
        }

        public static async Task<IGamePropertyService> GetGamePropertyServiceAsync()
        {
            return await Instance.Container.ResolveAsync<IGamePropertyService>();
        }

        public static async Task<IBuffService> GetBuffServiceAsync()
        {
            return await Instance.Container.ResolveAsync<IBuffService>();
        }

        public static async Task<IObjectPoolService> GetObjectPoolServiceAsync()
        {
            return await Instance.Container.ResolveAsync<IObjectPoolService>();
        }
    }
}
