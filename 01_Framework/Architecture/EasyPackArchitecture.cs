using EasyPack.ENekoFramework;
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
            // 注册核心服务

            Container.Register<ISerializationService, SerializationService>();
        
        }
    }
}
