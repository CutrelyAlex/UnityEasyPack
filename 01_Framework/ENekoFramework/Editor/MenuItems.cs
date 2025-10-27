using UnityEditor;
using EasyPack.ENekoFramework.Editor.Windows;


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
        private const string SettingsMenu = MenuRoot + "Settings/";
        
        #region Visualization Windows
        
        /// <summary>
        /// 打开架构总览窗口
        /// </summary>
        [MenuItem(VisualizationMenu + "Architecture Overview")]
        public static void OpenArchitectureOverview()
        {
            ArchitectureOverviewWindow.ShowWindow();
        }
        
        /// <summary>
        /// 打开服务总览窗口
        /// </summary>
        [MenuItem(VisualizationMenu + "Service Overview")]
        public static void OpenServiceOverview()
        {
            ServiceOverviewWindow.ShowWindow();
        }
        
        /// <summary>
        /// 打开事件监视器窗口
        /// </summary>
        [MenuItem(VisualizationMenu + "Event Monitor")]
        public static void OpenEventMonitor()
        {
            EventMonitorWindow.ShowWindow();
        }
        
        /// <summary>
        /// 打开命令历史窗口
        /// </summary>
        [MenuItem(VisualizationMenu + "Command History")]
        public static void OpenCommandHistory()
        {
            CommandHistoryWindow.ShowWindow();
        }
        
        /// <summary>
        /// 打开依赖关系图窗口
        /// </summary>
        [MenuItem(VisualizationMenu + "Dependency Graph")]
        public static void OpenDependencyGraph()
        {
            DependencyGraphWindow.ShowWindow();
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
        
        #region Settings
        
        /// <summary>
        /// 打开编辑器监控偏好设置
        /// </summary>
        [MenuItem(SettingsMenu + "Monitoring Preferences")]
        public static void OpenMonitoringPreferences()
        {
            EditorMonitoringPreferences.ShowWindow();
        }
        
        #endregion
    }
}
