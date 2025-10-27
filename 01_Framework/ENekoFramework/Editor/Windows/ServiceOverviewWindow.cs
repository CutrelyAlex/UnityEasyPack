using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace EasyPack.ENekoFramework.Editor.Windows
{
    /// <summary>
    /// 服务总览窗口
    /// 实时显示所有已注册服务的状态、依赖关系和元数据
    /// </summary>
    public class ServiceOverviewWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private List<ServiceDescriptor> _services;
        private ServiceDescriptor _selectedService;
        private bool _autoRefresh = true;
        private double _lastRefreshTime;
        private const double RefreshInterval = 1.0; // 每秒刷新一次
        
        // 状态颜色
        private readonly Color _uninitializedColor = new Color(0.7f, 0.7f, 0.7f);
        private readonly Color _initializingColor = new Color(1f, 0.8f, 0.3f);
        private readonly Color _readyColor = new Color(0.3f, 1f, 0.3f);
        private readonly Color _pausedColor = new Color(0.9f, 0.6f, 0.3f);
        private readonly Color _disposedColor = new Color(1f, 0.3f, 0.3f);

        /// <summary>
        /// 显示服务总览窗口
        /// </summary>
        public static void ShowWindow()
        {
            var window = GetWindow<ServiceOverviewWindow>("Service Overview");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshServices();
        }

        private void Update()
        {
            if (_autoRefresh && EditorApplication.timeSinceStartup - _lastRefreshTime > RefreshInterval)
            {
                RefreshServices();
                Repaint();
            }
        }

        private void OnGUI()
        {
            DrawToolbar();
            
            EditorGUILayout.BeginHorizontal();
            
            // 左侧：服务列表
            DrawServiceList();
            
            // 右侧：服务详情
            DrawServiceDetails();
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                RefreshServices();
            }
            
            _autoRefresh = GUILayout.Toggle(_autoRefresh, "自动刷新", EditorStyles.toolbarButton, GUILayout.Width(80));
            
            GUILayout.FlexibleSpace();
            
            if (_services != null)
            {
                GUILayout.Label($"服务总数: {_services.Count}", EditorStyles.toolbarButton);
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawServiceList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(300));
            
            EditorGUILayout.LabelField("已注册服务", EditorStyles.boldLabel);
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            if (_services != null && _services.Count > 0)
            {
                foreach (var service in _services)
                {
                    DrawServiceItem(service);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("未发现已注册的服务", MessageType.Info);
            }
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawServiceItem(ServiceDescriptor service)
        {
            var isSelected = _selectedService == service;
            var bgColor = isSelected ? new Color(0.3f, 0.5f, 0.8f) : Color.clear;
            
            var prevColor = GUI.backgroundColor;
            GUI.backgroundColor = bgColor;
            
            EditorGUILayout.BeginHorizontal("box");
            
            // 状态指示器
            var stateColor = GetStateColor(service.State);
            var prevContentColor = GUI.contentColor;
            GUI.contentColor = stateColor;
            GUILayout.Label("●", GUILayout.Width(15));
            GUI.contentColor = prevContentColor;
            
            // 服务名称
            if (GUILayout.Button(service.ServiceType.Name, EditorStyles.label))
            {
                _selectedService = service;
            }
            
            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = prevColor;
        }

        private void DrawServiceDetails()
        {
            EditorGUILayout.BeginVertical();
            
            if (_selectedService != null)
            {
                EditorGUILayout.LabelField("服务详情", EditorStyles.boldLabel);
                EditorGUILayout.Space();
                
                // 基本信息
                EditorGUILayout.LabelField("服务类型", _selectedService.ServiceType.FullName);
                EditorGUILayout.LabelField("实现类型", _selectedService.ImplementationType.FullName);
                EditorGUILayout.LabelField("状态", _selectedService.State.ToString());
                
                if (_selectedService.RegisteredAt != default)
                {
                    EditorGUILayout.LabelField("注册时间", _selectedService.RegisteredAt.ToString("yyyy-MM-dd HH:mm:ss"));
                }
                
                if (_selectedService.LastAccessedAt.HasValue && _selectedService.LastAccessedAt.Value != default)
                {
                    EditorGUILayout.LabelField("最后访问", _selectedService.LastAccessedAt.Value.ToString("yyyy-MM-dd HH:mm:ss"));
                }
                
                EditorGUILayout.Space();
                
                // 依赖关系
                DrawDependencies();
                
                EditorGUILayout.Space();
                
                // 元数据
                DrawMetadata();
            }
            else
            {
                EditorGUILayout.HelpBox("选择一个服务以查看详情", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawDependencies()
        {
            EditorGUILayout.LabelField("依赖关系", EditorStyles.boldLabel);
            
            var dependencies = ServiceInspector.GetServiceDependencies(_selectedService.ServiceType);
            
            if (dependencies != null && dependencies.Count > 0)
            {
                foreach (var dep in dependencies)
                {
                    EditorGUILayout.LabelField("  → " + dep.Name);
                }
                
                // 检查循环依赖
                if (ServiceInspector.HasCircularDependency(_selectedService.ServiceType))
                {
                    EditorGUILayout.HelpBox("⚠️ 检测到循环依赖！", MessageType.Warning);
                }
            }
            else
            {
                EditorGUILayout.LabelField("  无依赖");
            }
        }

        private void DrawMetadata()
        {
            EditorGUILayout.LabelField("元数据", EditorStyles.boldLabel);
            
            var metadata = ServiceInspector.GetServiceMetadata(_selectedService);
            
            if (metadata != null)
            {
                EditorGUILayout.LabelField("依赖数量", metadata.Dependencies?.Count.ToString() ?? "0");
                
                if (metadata.HasCircularDependency)
                {
                    EditorGUILayout.HelpBox("⚠️ 此服务存在循环依赖！", MessageType.Warning);
                }
            }
        }

        private void RefreshServices()
        {
            _services = ServiceInspector.GetAllServices();
            _lastRefreshTime = EditorApplication.timeSinceStartup;
        }

        private Color GetStateColor(ServiceLifecycleState state)
        {
            return state switch
            {
                ServiceLifecycleState.Uninitialized => _uninitializedColor,
                ServiceLifecycleState.Initializing => _initializingColor,
                ServiceLifecycleState.Ready => _readyColor,
                ServiceLifecycleState.Paused => _pausedColor,
                ServiceLifecycleState.Disposed => _disposedColor,
                _ => Color.white
            };
        }
    }
}
