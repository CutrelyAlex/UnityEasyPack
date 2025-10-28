using UnityEditor;
using EasyPack.ENekoFramework.Editor.Windows;
using System.Linq;


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
        private const string DebugMenu = MenuRoot + "Debug/";
        
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
        
        #region Debug
        
        /// <summary>
        /// 初始化 EasyPackArchitecture（用于调试）
        /// </summary>
        [MenuItem(DebugMenu + "Initialize EasyPackArchitecture")]
        public static void InitializeEasyPackArchitecture()
        {
            var instance = EasyPack.EasyPackArchitecture.Instance;
            UnityEngine.Debug.Log($"[Debug] EasyPackArchitecture 已初始化: {instance.GetType().Name}");
            UnityEngine.Debug.Log($"[Debug] 初始化状态: {instance.IsInitialized}");
            
            var containerProp = instance.GetType().GetProperty("Container",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (containerProp != null)
            {
                var container = containerProp.GetValue(instance) as ServiceContainer;
                if (container != null)
                {
                    var services = container.GetAllServices();
                    UnityEngine.Debug.Log($"[Debug] 已注册服务数量: {services.Count()}");
                }
            }
        }
        
        /// <summary>
        /// 检查 EasyPackArchitecture 状态（不触发初始化）
        /// </summary>
        [MenuItem(DebugMenu + "Check EasyPackArchitecture Status")]
        public static void CheckEasyPackArchitectureStatus()
        {
            var archType = typeof(EasyPack.EasyPackArchitecture);
            var instanceField = archType.BaseType?.GetField("_instance",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            if (instanceField != null)
            {
                var instance = instanceField.GetValue(null);
                if (instance != null)
                {
                    UnityEngine.Debug.Log($"[Debug] EasyPackArchitecture 已存在实例");
                    var isInitProp = instance.GetType().GetProperty("IsInitialized");
                    if (isInitProp != null)
                    {
                        var isInit = (bool)isInitProp.GetValue(instance);
                        UnityEngine.Debug.Log($"[Debug] 初始化状态: {isInit}");
                    }
                }
                else
                {
                    UnityEngine.Debug.Log($"[Debug] EasyPackArchitecture 尚未创建实例");
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning($"[Debug] 无法访问 _instance 字段");
            }
        }
        
        /// <summary>
        /// 清除所有架构实例（慎用！）
        /// </summary>
        [MenuItem(DebugMenu + "Clear All Architecture Instances")]
        public static void ClearAllArchitectureInstances()
        {
            if (UnityEditor.EditorUtility.DisplayDialog(
                "警告", 
                "此操作将清除所有架构实例，可能导致运行时错误。是否继续？", 
                "确定", 
                "取消"))
            {
                var instances = ServiceInspector.GetAllArchitectureInstances();
                int count = instances.Count;
                
                foreach (var instance in instances)
                {
                    var archType = instance.GetType();
                    var instanceField = archType.BaseType?.GetField("_instance",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    
                    if (instanceField != null)
                    {
                        instanceField.SetValue(null, null);
                    }
                }
                
                UnityEngine.Debug.Log($"[Debug] 已清除 {count} 个架构实例");
            }
        }
        
        #endregion
    }
}
