using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using EasyPack.ENekoFramework.Editor;

namespace EasyPack.ENekoFramework.Editor.Windows
{
    /// <summary>
    /// 架构总览窗口
    /// 显示所有架构实例的状态、服务数量和核心组件信息
    /// </summary>
    public class ArchitectureOverviewWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private List<ArchitectureInfo> _architectures;
        private ArchitectureInfo _selectedArchitecture;
        private bool _autoRefresh = true;
        private double _lastRefreshTime;
        private bool _isRefreshing = false;
        private double _refreshStartTime;
        private const double RefreshInterval = 1.0;

        // UI样式
        private GUIStyle _headerStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _labelStyle;
        private bool _stylesInitialized;

        /// <summary>
        /// 架构信息
        /// </summary>
        private class ArchitectureInfo
        {
            public object Instance;
            public string TypeName;
            public bool IsInitialized;
            public int ServiceCount;
            public int RegisteredEventCount;
            public bool HasCommandDispatcher;
            public bool HasQueryExecutor;
            public bool HasEventBus;
            public ServiceContainer Container;
        }

        /// <summary>
        /// 显示架构总览窗口
        /// </summary>
        public static void ShowWindow()
        {
            var window = GetWindow<ArchitectureOverviewWindow>("Architecture Overview");
            window.minSize = new Vector2(700, 450);
            window.Show();
        }

        private void OnEnable()
        {
            // RefreshArchitectures();
        }

        private void Update()
        {
            // 检查刷新超时（10秒）
            if (_isRefreshing && EditorApplication.timeSinceStartup - _refreshStartTime > 10.0)
            {
                Debug.LogWarning("ArchitectureOverviewWindow: 刷新操作超时，强制重置状态");
                _isRefreshing = false;
                _architectures = new List<ArchitectureInfo>();
                _selectedArchitecture = null;
                Repaint();
            }

            if (_autoRefresh && !_isRefreshing && EditorApplication.timeSinceStartup - _lastRefreshTime > RefreshInterval)
            {
                // 延迟调用避免阻塞
                EditorApplication.delayCall += () =>
                {
                    RefreshArchitecturesAsync();
                };
                _lastRefreshTime = EditorApplication.timeSinceStartup;
            }
        }

        private void OnGUI()
        {
            InitializeStyles();
            DrawToolbar();
            
            EditorGUILayout.BeginHorizontal();
            DrawArchitectureList();
            DrawArchitectureDetails();
            EditorGUILayout.EndHorizontal();
        }

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                margin = new RectOffset(5, 5, 5, 5)
            };

            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(5, 5, 5, 5)
            };

            _labelStyle = new GUIStyle(EditorStyles.label)
            {
                richText = true
            };

            _stylesInitialized = true;
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                RefreshArchitectures();
            }

            if (_isRefreshing)
            {
                GUILayout.Label("Refreshing...", EditorStyles.toolbarButton, GUILayout.Width(80));
            }
            else
            {
                GUILayout.Space(80);
            }

            GUILayout.Space(10);

            _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto Refresh", EditorStyles.toolbarButton, GUILayout.Width(100));

            GUILayout.FlexibleSpace();

            int totalArchitectures = _architectures?.Count ?? 0;
            int totalServices = _architectures?.Sum(a => a.ServiceCount) ?? 0;
            GUILayout.Label($"Architectures: {totalArchitectures} | Total Services: {totalServices}", EditorStyles.toolbarButton);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawArchitectureList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(300));
            EditorGUILayout.LabelField("架构列表", _headerStyle);

            if (_architectures == null || _architectures.Count == 0)
            {
                EditorGUILayout.HelpBox("未找到架构实例。\n请确保至少有一个架构已被初始化。", MessageType.Info);
            }
            else
            {
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

                foreach (var arch in _architectures)
                {
                    DrawArchitectureItem(arch);
                }

                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawArchitectureItem(ArchitectureInfo arch)
        {
            bool isSelected = _selectedArchitecture == arch;
            
            var backgroundColor = GUI.backgroundColor;
            if (isSelected)
            {
                GUI.backgroundColor = new Color(0.3f, 0.5f, 0.8f);
            }

            EditorGUILayout.BeginVertical(_boxStyle);
            GUI.backgroundColor = backgroundColor;

            // 架构名称
            EditorGUILayout.LabelField(arch.TypeName, EditorStyles.boldLabel);

            // 初始化状态
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("状态:", GUILayout.Width(50));
            var statusColor = arch.IsInitialized ? Color.green : Color.yellow;
            var previousColor = GUI.color;
            GUI.color = statusColor;
            EditorGUILayout.LabelField(arch.IsInitialized ? "已初始化" : "未初始化", _labelStyle);
            GUI.color = previousColor;
            EditorGUILayout.EndHorizontal();

            // 服务数量
            EditorGUILayout.LabelField($"服务数量: {arch.ServiceCount}");

            // 选择按钮
            if (GUILayout.Button(isSelected ? "已选择" : "查看详情"))
            {
                _selectedArchitecture = arch;
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(5);
        }

        private void DrawArchitectureDetails()
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("架构详情", _headerStyle);

            if (_selectedArchitecture == null)
            {
                EditorGUILayout.HelpBox("请从左侧列表选择一个架构查看详情。", MessageType.Info);
            }
            else
            {
                DrawSelectedArchitectureDetails();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSelectedArchitectureDetails()
        {
            var arch = _selectedArchitecture;

            EditorGUILayout.BeginVertical(_boxStyle);

            // 基本信息
            EditorGUILayout.LabelField("基本信息", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"类型: {arch.TypeName}");
            EditorGUILayout.LabelField($"已初始化: {(arch.IsInitialized ? "是" : "否")}");
            EditorGUILayout.Space(10);

            // 核心组件
            EditorGUILayout.LabelField("核心组件", EditorStyles.boldLabel);
            DrawComponentStatus("CommandDispatcher", arch.HasCommandDispatcher);
            DrawComponentStatus("QueryExecutor", arch.HasQueryExecutor);
            DrawComponentStatus("EventBus", arch.HasEventBus);
            EditorGUILayout.Space(10);

            // 服务容器信息
            EditorGUILayout.LabelField("服务容器", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"已注册服务: {arch.ServiceCount}");
            
            if (arch.Container != null && arch.ServiceCount > 0)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("服务列表:", EditorStyles.miniBoldLabel);
                
                var services = arch.Container.GetAllServices();
                foreach (var service in services)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("•", GUILayout.Width(10));
                    EditorGUILayout.LabelField(service.ServiceType.Name);
                    
                    var stateColor = GetStateColor(service.State);
                    var previousColor = GUI.color;
                    GUI.color = stateColor;
                    EditorGUILayout.LabelField(service.State.ToString(), GUILayout.Width(100));
                    GUI.color = previousColor;
                    
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.Space(10);

            // 事件系统信息
            EditorGUILayout.LabelField("事件系统", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"已注册事件类型: {arch.RegisteredEventCount}");

            EditorGUILayout.EndVertical();

            // 操作按钮
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("查看服务详情"))
            {
                ServiceOverviewWindow.ShowWindow();
            }
            
            if (GUILayout.Button("查看依赖关系"))
            {
                DependencyGraphWindow.ShowWindow();
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawComponentStatus(string componentName, bool exists)
        {
            EditorGUILayout.BeginHorizontal();
            var statusIcon = exists ? "✓" : "✗";
            var statusColor = exists ? Color.green : Color.red;
            
            var previousColor = GUI.color;
            GUI.color = statusColor;
            EditorGUILayout.LabelField($"{statusIcon} {componentName}", _labelStyle);
            GUI.color = previousColor;
            
            EditorGUILayout.EndHorizontal();
        }

        private Color GetStateColor(ServiceLifecycleState state)
        {
            switch (state)
            {
                case ServiceLifecycleState.Uninitialized:
                    return new Color(0.7f, 0.7f, 0.7f);
                case ServiceLifecycleState.Initializing:
                    return new Color(1f, 0.8f, 0.3f);
                case ServiceLifecycleState.Ready:
                    return new Color(0.3f, 1f, 0.3f);
                case ServiceLifecycleState.Paused:
                    return new Color(0.9f, 0.6f, 0.3f);
                case ServiceLifecycleState.Disposed:
                    return new Color(1f, 0.3f, 0.3f);
                default:
                    return Color.white;
            }
        }

        private void RefreshArchitectures()
        {
            RefreshArchitecturesAsync();
        }

        private void RefreshArchitecturesAsync()
        {
            if (_isRefreshing) return; 
            
            _isRefreshing = true;
            _refreshStartTime = EditorApplication.timeSinceStartup;
            _lastRefreshTime = EditorApplication.timeSinceStartup;
            
            // 异步执行刷新操作
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    var newArchitectures = RefreshArchitecturesInternal();
                    
                    // 在主线程更新UI
                    EditorApplication.delayCall += () =>
                    {
                        // 检查是否已经超时（10秒超时）
                        if (_isRefreshing && EditorApplication.timeSinceStartup - _refreshStartTime < 10.0)
                        {
                            _architectures = newArchitectures;
                            
                            // 如果当前选择的架构不在新列表中，清除选择
                            if (_selectedArchitecture != null && 
                                !_architectures.Any(a => a.TypeName == _selectedArchitecture.TypeName))
                            {
                                _selectedArchitecture = null;
                            }
                        }
                        
                        _isRefreshing = false;
                        Repaint();
                    };
                }
                catch (Exception ex)
                {
                    Debug.LogError($"ArchitectureOverviewWindow: 刷新架构信息时发生异常 - {ex.Message}\n{ex.StackTrace}");
                    
                    EditorApplication.delayCall += () =>
                    {
                        _architectures = new List<ArchitectureInfo>();
                        _selectedArchitecture = null;
                        _isRefreshing = false;
                        Repaint();
                    };
                }
            });
        }

        private List<ArchitectureInfo> RefreshArchitecturesInternal()
        {
            var architectures = new List<ArchitectureInfo>();

            var instances = ServiceInspector.GetAllArchitectureInstances();
            
            foreach (var instance in instances)
            {
                var info = new ArchitectureInfo
                {
                    Instance = instance,
                    TypeName = instance.GetType().Name
                };

                // 获取IsInitialized属性
                var isInitProp = instance.GetType().GetProperty("IsInitialized");
                if (isInitProp != null)
                {
                    info.IsInitialized = (bool)isInitProp.GetValue(instance);
                }

                // 获取Container
                info.Container = ServiceInspector.GetContainerFromArchitecture(instance);
                if (info.Container != null)
                {
                    var services = info.Container.GetAllServices().ToList();
                    info.ServiceCount = services.Count;
                }

                // 检查核心组件
                info.HasCommandDispatcher = HasProperty(instance, "CommandDispatcher");
                info.HasQueryExecutor = HasProperty(instance, "QueryExecutor");
                info.HasEventBus = HasProperty(instance, "EventBus");

                // 获取事件数量
                var eventBusProp = instance.GetType().GetProperty("EventBus");
                if (eventBusProp != null)
                {
                    var eventBus = eventBusProp.GetValue(instance);
                    if (eventBus != null)
                    {
                        // 尝试获取注册的事件数量
                        var subscribersField = eventBus.GetType().GetField("_subscribers",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (subscribersField != null)
                        {
                            var subscribers = subscribersField.GetValue(eventBus);
                            if (subscribers is System.Collections.IDictionary dict)
                            {
                                info.RegisteredEventCount = dict.Count;
                            }
                        }
                    }
                }

                architectures.Add(info);
            }

            return architectures;
        }

        private bool HasProperty(object instance, string propertyName)
        {
            var prop = instance.GetType().GetProperty(propertyName,
                System.Reflection.BindingFlags.Public | 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            
            return prop != null && prop.GetValue(instance) != null;
        }
    }
}
