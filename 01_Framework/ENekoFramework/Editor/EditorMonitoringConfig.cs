using UnityEditor;
using UnityEngine;

namespace EasyPack.ENekoFramework.Editor
{
    /// <summary>
    /// 编辑器监控配置
    /// 管理各种编辑器监控功能的开关
    /// </summary>
    public static class EditorMonitoringConfig
    {
        private const string EnableEventMonitoringKey = "EasyPack.EditorMonitoring.EnableEventMonitoring";
        private const string EnableServiceMonitoringKey = "EasyPack.EditorMonitoring.EnableServiceMonitoring";
        private const string EnableCommandMonitoringKey = "EasyPack.EditorMonitoring.EnableCommandMonitoring";
        
        private static bool? _enableEventMonitoring;
        private static bool? _enableServiceMonitoring;
        private static bool? _enableCommandMonitoring;

        /// <summary>
        /// 是否启用事件监控
        /// </summary>
        public static bool EnableEventMonitoring
        {
            get
            {
                if (!_enableEventMonitoring.HasValue)
                {
                    _enableEventMonitoring = EditorPrefs.GetBool(EnableEventMonitoringKey, true);
                }
                return _enableEventMonitoring.Value;
            }
            set
            {
                if (_enableEventMonitoring != value)
                {
                    _enableEventMonitoring = value;
                    EditorPrefs.SetBool(EnableEventMonitoringKey, value);
                }
            }
        }

        /// <summary>
        /// 是否启用服务监控
        /// </summary>
        public static bool EnableServiceMonitoring
        {
            get
            {
                if (!_enableServiceMonitoring.HasValue)
                {
                    _enableServiceMonitoring = EditorPrefs.GetBool(EnableServiceMonitoringKey, true);
                }
                return _enableServiceMonitoring.Value;
            }
            set
            {
                if (_enableServiceMonitoring != value)
                {
                    _enableServiceMonitoring = value;
                    EditorPrefs.SetBool(EnableServiceMonitoringKey, value);
                }
            }
        }

        /// <summary>
        /// 是否启用命令监控
        /// </summary>
        public static bool EnableCommandMonitoring
        {
            get
            {
                if (!_enableCommandMonitoring.HasValue)
                {
                    _enableCommandMonitoring = EditorPrefs.GetBool(EnableCommandMonitoringKey, true);
                }
                return _enableCommandMonitoring.Value;
            }
            set
            {
                if (_enableCommandMonitoring != value)
                {
                    _enableCommandMonitoring = value;
                    EditorPrefs.SetBool(EnableCommandMonitoringKey, value);
                }
            }
        }

        /// <summary>
        /// 重置所有监控配置到默认值
        /// </summary>
        public static void ResetToDefault()
        {
            EnableEventMonitoring = true;
            EnableServiceMonitoring = true;
            EnableCommandMonitoring = true;
        }
    }
}
