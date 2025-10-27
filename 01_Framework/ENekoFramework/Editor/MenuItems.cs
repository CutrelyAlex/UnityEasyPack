using UnityEditor;

namespace EasyPack.ENekoFramework.Editor
{
    /// <summary>
    /// ENekoFramework 编辑器菜单项
    /// </summary>
    public static class MenuItems
    {
        private const string MenuRoot = "ENekoFramework/";
        private const string VisualizationMenu = MenuRoot + "Visualization/";
        private const string CodeGenMenu = MenuRoot + "Code Generation/";
        
        #region Visualization Windows
        
        /// <summary>
        /// 打开架构总览窗口
        /// </summary>
        [MenuItem(VisualizationMenu + "Architecture Overview")]
        public static void OpenArchitectureOverview()
        {
            Windows.ArchitectureOverviewWindow.ShowWindow();
        }
        
        /// <summary>
        /// 打开服务总览窗口
        /// </summary>
        [MenuItem(VisualizationMenu + "Service Overview")]
        public static void OpenServiceOverview()
        {
            Windows.ServiceOverviewWindow.ShowWindow();
        }
        
        /// <summary>
        /// 打开事件监视器窗口
        /// </summary>
        [MenuItem(VisualizationMenu + "Event Monitor")]
        public static void OpenEventMonitor()
        {
            Windows.EventMonitorWindow.ShowWindow();
        }
        
        /// <summary>
        /// 打开命令历史窗口
        /// </summary>
        [MenuItem(VisualizationMenu + "Command History")]
        public static void OpenCommandHistory()
        {
            Windows.CommandHistoryWindow.ShowWindow();
        }
        
        /// <summary>
        /// 打开依赖关系图窗口
        /// </summary>
        [MenuItem(VisualizationMenu + "Dependency Graph")]
        public static void OpenDependencyGraph()
        {
            Windows.DependencyGraphWindow.ShowWindow();
        }
        
        #endregion
        
        #region Code Generation
        
        /// <summary>
        /// 生成服务脚手架代码
        /// </summary>
        [MenuItem(CodeGenMenu + "Generate Service...")]
        public static void GenerateService()
        {
            ServiceScaffold.ShowWizard();
        }
        
        /// <summary>
        /// 生成命令脚手架代码
        /// </summary>
        [MenuItem(CodeGenMenu + "Generate Command...")]
        public static void GenerateCommand()
        {
            ServiceScaffold.ShowWizard("Command");
        }
        
        /// <summary>
        /// 生成查询脚手架代码
        /// </summary>
        [MenuItem(CodeGenMenu + "Generate Query...")]
        public static void GenerateQuery()
        {
            ServiceScaffold.ShowWizard("Query");
        }
        
        /// <summary>
        /// 生成事件脚手架代码
        /// </summary>
        [MenuItem(CodeGenMenu + "Generate Event...")]
        public static void GenerateEvent()
        {
            ServiceScaffold.ShowWizard("Event");
        }
        
        #endregion
    }
}
