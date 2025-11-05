using EasyPack.BuffSystem;
using EasyPack.ENekoFramework;
using EasyPack.InventorySystem;
using System;
using System.Threading.Tasks;
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
    }
}
