using System;

namespace EasyPack.GamePropertySystem
{
    /// <summary>
    ///     属性元数据
    ///     存储属性的显示信息
    /// </summary>
    [Serializable]
    public class PropertyDisplayInfo
    {
        /// <summary>显示名称</summary>
        public string DisplayName;

        /// <summary>详细描述</summary>
        public string Description;

        /// <summary>图标资源路径</summary>
        public string IconPath;

        /// <summary>
        ///     默认构造函数
        /// </summary>
        public PropertyDisplayInfo() { }
    }
}