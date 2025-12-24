using System.Threading.Tasks;
using EasyPack.BuffSystem;
using EasyPack.Category;
using EasyPack.ENekoFramework;
using EasyPack.GamePropertySystem;
using EasyPack.InventorySystem;
using EasyPack.ObjectPool;
using EasyPack.Serialization;

namespace EasyPack.Architecture
{
    /// <summary>
    ///     EasyPack 统一架构容器
    ///     封装所有 EasyPack 系统和服务
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
            Container.Register<ISerializationService, SerializationService>();
            Container.Register<IObjectPoolService, ObjectPoolService>();

            // 异步预热专用对象池（防止首次使用时的阻塞初始化）
            _ = InitializePoolsAsync();
        }

        /// <summary>
        ///     异步预热常用集合池
        /// </summary>
        private async Task InitializePoolsAsync()
        {
            // 预热常用类型的专用池
            await ListPool<int>.InitializeAsync(1024 * 16);
            await ListPool<string>.InitializeAsync(1024 * 16);
            await ListPool<double>.InitializeAsync(1024 * 16);
            await HashSetPool<int>.InitializeAsync(1024 * 16);
            await HashSetPool<string>.InitializeAsync(1024 * 16);
            await HashSetPool<double>.InitializeAsync(1024 * 16);
            await StackPool<int>.InitializeAsync(1024 * 16);
        }

        public static async Task<ISerializationService> GetSerializationServiceAsync() =>
            await Instance.Container.ResolveAsync<ISerializationService>();

        public static async Task<IInventoryService> GetInventoryServiceAsync() =>
            await Instance.Container.ResolveAsync<IInventoryService>();

        public static async Task<IGamePropertyService> GetGamePropertyServiceAsync() =>
            await Instance.Container.ResolveAsync<IGamePropertyService>();

        public static async Task<IBuffService> GetBuffServiceAsync() =>
            await Instance.Container.ResolveAsync<IBuffService>();

        public static async Task<IObjectPoolService> GetObjectPoolServiceAsync() =>
            await Instance.Container.ResolveAsync<IObjectPoolService>();
    }
}