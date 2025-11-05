using EasyPack.BuffSystem;
using EasyPack.ENekoFramework;
using EasyPack.InventorySystem;
using UnityEngine;

namespace EasyPack
{
    /// <summary>
    /// EasyPack 统一架构容器
    /// 封装所有 EasyPack 系统和服务
    /// </summary>
    public class EasyPackArchitecture : ENekoArchitecture<EasyPackArchitecture>
    {
        protected override void OnInit()
        {
            // 02_Foundation
            Container.Register<ISerializationService, SerializationService>();

            // 03_CoreSystems
            Container.Register<IGamePropertyService, GamePropertyService>();
            Container.Register<IBuffService, BuffService>();
            Container.Register<IInventoryService, InventoryService>();
        }

        public static ISerializationService SerializationService => Instance.Container.Resolve<ISerializationService>();
        public static IInventoryService InventoryService => Instance.Container.Resolve<IInventoryService>();
        public static IGamePropertyService GamePropertyService => Instance.Container.Resolve<IGamePropertyService>();
        public static IBuffService BuffService => Instance.Container.Resolve<IBuffService>();

    }
}
