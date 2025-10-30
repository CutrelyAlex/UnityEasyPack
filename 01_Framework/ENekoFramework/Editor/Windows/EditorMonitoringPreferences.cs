using UnityEditor;
using UnityEngine;

namespace EasyPack.ENekoFramework.Editor.Windows
{
    /// <summary>
    /// 编辑器监控偏好设置
    /// 集中管理所有监控相关的配置
    /// </summary>
    public class EditorMonitoringPreferences : EditorWindow
    {
        private Vector2 _scrollPosition;

        /// <summary>
        /// 显示偏好设置窗口
        /// </summary>
        public static void ShowWindow()
        {
            var window = GetWindow<EditorMonitoringPreferences>("Monitoring Preferences");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("编辑器监控配置", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // 事件监控
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("事件监控", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("启用事件监控会在事件发布时通知编辑器窗口。关闭此选项可提高运行时性能。", MessageType.Info);
            
            var eventEnabled = EditorMonitoringConfig.EnableEventMonitoring;
            var newEventState = EditorGUILayout.Toggle("启用事件监控", eventEnabled);
            if (newEventState != eventEnabled)
            {
                EditorMonitoringConfig.EnableEventMonitoring = newEventState;
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();

            // 服务监控
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("服务监控", EditorStyles.boldLabel);
            var serviceEnabled = EditorMonitoringConfig.EnableServiceMonitoring;
            var newServiceState = EditorGUILayout.Toggle("启用服务监控", serviceEnabled);
            if (newServiceState != serviceEnabled)
            {
                EditorMonitoringConfig.EnableServiceMonitoring = newServiceState;
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();

            // 命令监控
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("命令监控", EditorStyles.boldLabel);

            var commandEnabled = EditorMonitoringConfig.EnableCommandMonitoring;
            var newCommandState = EditorGUILayout.Toggle("启用命令监控", commandEnabled);
            if (newCommandState != commandEnabled)
            {
                EditorMonitoringConfig.EnableCommandMonitoring = newCommandState;
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            EditorGUILayout.Separator();

            // 按钮
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("启用所有监控"))
            {
                EditorMonitoringConfig.EnableEventMonitoring = true;
                EditorMonitoringConfig.EnableServiceMonitoring = true;
                EditorMonitoringConfig.EnableCommandMonitoring = true;
            }
            
            if (GUILayout.Button("禁用所有监控"))
            {
                EditorMonitoringConfig.EnableEventMonitoring = false;
                EditorMonitoringConfig.EnableServiceMonitoring = false;
                EditorMonitoringConfig.EnableCommandMonitoring = false;
            }
            
            if (GUILayout.Button("恢复默认设置"))
            {
                EditorMonitoringConfig.ResetToDefault();
            }
            
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("注意：所有监控功能仅在编辑器中运行时才会生效，不会影响构建版本的性能。", MessageType.Info);
        }
    }
}
