using UnityEditor;
using UnityEngine;
using System;
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
        private bool _isRefreshing = false;
        private double _refreshStartTime;
        private const double RefreshInterval = 1.0;
        
        // 筛选缓存
        private List<ServiceDescriptor> _cachedFilteredServices;
        private List<string> _lastSelectedArchitectures = new List<string>();
        private ServiceLifecycleState _lastSelectedStateFilter = ServiceLifecycleState.Ready;
        private bool _lastUseStateFilter = false;
        private bool _filterCacheValid = false;
        
        // 架构缓存
        private Dictionary<string, string> _cachedArchToNamespace;
        private bool _archCacheValid = false;
        
        // 筛选器
        private List<string> _architectureNames = new List<string>();
        private List<bool> _architectureFilters = new List<bool>();
        private ServiceLifecycleState _selectedStateFilter = ServiceLifecycleState.Ready;
        private bool _useStateFilter = false;
        private Vector2 _filterScrollPosition;
        
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
            RefreshArchitectureList();
        }

        private void Update()
        {
            if (_isRefreshing && EditorApplication.timeSinceStartup - _refreshStartTime > 10.0)
            {
                Debug.LogWarning("ServiceOverviewWindow: 刷新操作超时，强制重置状态");
                _isRefreshing = false;
                _services = new List<ServiceDescriptor>();
                _selectedService = null;
                Repaint();
            }

            if (_autoRefresh && !_isRefreshing && EditorApplication.timeSinceStartup - _lastRefreshTime > RefreshInterval)
            {
                EditorApplication.delayCall += () =>
                {
                    RefreshServicesAsync();
                };
                _lastRefreshTime = EditorApplication.timeSinceStartup;
            }
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawFilters();
            
            EditorGUILayout.BeginHorizontal();
            
            // 左侧：服务列表
            DrawServiceList();
            
            // 右侧：服务详情
            DrawServiceDetails();
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawFilters()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Height(100));
            
            EditorGUILayout.LabelField("筛选器", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            // 架构筛选
            EditorGUILayout.LabelField("架构:", GUILayout.Width(50));
            _filterScrollPosition = EditorGUILayout.BeginScrollView(_filterScrollPosition, GUILayout.Height(40));
            for (int i = 0; i < _architectureNames.Count; i++)
            {
                _architectureFilters[i] = EditorGUILayout.ToggleLeft(_architectureNames[i], _architectureFilters[i]);
            }
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            // 状态筛选
            _useStateFilter = EditorGUILayout.ToggleLeft("按状态筛选", _useStateFilter, GUILayout.Width(80));
            if (_useStateFilter)
            {
                _selectedStateFilter = (ServiceLifecycleState)EditorGUILayout.EnumPopup(_selectedStateFilter, GUILayout.Width(150));
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                RefreshServices();
            }

            EditorGUILayout.BeginHorizontal(GUILayout.Width(100));
            if (_isRefreshing)
            {
                GUILayout.Label("刷新中...", EditorStyles.toolbarButton, GUILayout.ExpandWidth(true));
            }
            else
            {
                _autoRefresh = GUILayout.Toggle(_autoRefresh, "自动刷新", EditorStyles.toolbarButton, GUILayout.ExpandWidth(true));
            }
            EditorGUILayout.EndHorizontal();
            
            // 监控开关
            var monitoringEnabled = EditorMonitoringConfig.EnableServiceMonitoring;
            var newMonitoringState = GUILayout.Toggle(monitoringEnabled, "启用监控", EditorStyles.toolbarButton, GUILayout.Width(80));
            if (newMonitoringState != monitoringEnabled)
            {
                EditorMonitoringConfig.EnableServiceMonitoring = newMonitoringState;
            }
            
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
            
            var filteredServices = GetFilteredServices();
            
            if (filteredServices.Count > 0)
            {
                foreach (var service in filteredServices)
                {
                    DrawServiceItem(service);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("未发现匹配的服务", MessageType.Info);
            }
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
        
        private List<ServiceDescriptor> GetFilteredServices()
        {
            // 检查筛选条件是否改变
            var currentSelectedArchitectures = new List<string>();
            for (int i = 0; i < _architectureNames.Count; i++)
            {
                if (_architectureFilters[i])
                    currentSelectedArchitectures.Add(_architectureNames[i]);
            }
            
            bool filterChanged = !_filterCacheValid ||
                !_lastSelectedArchitectures.SequenceEqual(currentSelectedArchitectures) ||
                _lastUseStateFilter != _useStateFilter ||
                (_useStateFilter && _lastSelectedStateFilter != _selectedStateFilter);
            
            if (!filterChanged && _cachedFilteredServices != null)
            {
                return _cachedFilteredServices;
            }
            
            // 重新计算过滤结果
            if (_services == null || _services.Count == 0)
            {
                _cachedFilteredServices = new List<ServiceDescriptor>();
            }
            else
            {
                var filtered = _services.ToList();
                
                // 架构筛选：仅当有架构被勾选时才进行筛选，否则显示空列表
                if (currentSelectedArchitectures.Count > 0)
                {
                    // 使用缓存的架构映射，避免每次都进行反射
                    if (!_archCacheValid || _cachedArchToNamespace == null)
                    {
                        var allArchitectures = ServiceInspector.GetAllArchitectureInstances();
                        _cachedArchToNamespace = new Dictionary<string, string>();
                        
                        // 建立架构名称到其所在命名空间的映射
                        foreach (var arch in allArchitectures)
                        {
                            var archName = arch.GetType().Name;
                            var archNamespace = arch.GetType().Namespace;
                            if (!_cachedArchToNamespace.ContainsKey(archName))
                            {
                                _cachedArchToNamespace[archName] = archNamespace;
                            }
                        }
                        
                        _archCacheValid = true;
                    }
                    
                    filtered = filtered.Where(s =>
                    {
                        var serviceNamespace = s.ServiceType.Namespace;
                        return currentSelectedArchitectures.Any(arch => 
                            _cachedArchToNamespace.ContainsKey(arch) && 
                            serviceNamespace?.StartsWith(_cachedArchToNamespace[arch]) == true
                        );
                    }).ToList();
                }
                else
                {
                    // 当没有勾选任何架构时，显示空列表
                    filtered = new List<ServiceDescriptor>();
                }
                
                // 状态筛选
                if (_useStateFilter)
                {
                    filtered = filtered.Where(s => s.State == _selectedStateFilter).ToList();
                }
                
                _cachedFilteredServices = filtered;
            }
            
            // 更新缓存状态
            _lastSelectedArchitectures = currentSelectedArchitectures.ToList();
            _lastUseStateFilter = _useStateFilter;
            _lastSelectedStateFilter = _selectedStateFilter;
            _filterCacheValid = true;
            
            return _cachedFilteredServices;
        }
        
        private void RefreshArchitectureList()
        {
            // 保存当前的筛选状态
            var previousFilters = new Dictionary<string, bool>();
            for (int i = 0; i < _architectureNames.Count; i++)
            {
                previousFilters[_architectureNames[i]] = _architectureFilters[i];
            }
            
            _architectureNames.Clear();
            _architectureFilters.Clear();
            
            var architectureNames = ServiceInspector.GetAllArchitectureNames();
            foreach (var arch in architectureNames)
            {
                _architectureNames.Add(arch);
                // 恢复之前的筛选状态，如果架构不存在则默认为true（全选）
                _architectureFilters.Add(previousFilters.ContainsKey(arch) ? previousFilters[arch] : true);
            }
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
            RefreshServicesAsync();
        }

        private void RefreshServicesAsync()
        {
            if (_isRefreshing) return; // 防止并发刷新
            
            _isRefreshing = true;
            _refreshStartTime = EditorApplication.timeSinceStartup;
            _lastRefreshTime = EditorApplication.timeSinceStartup;
            
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    var newServices = RefreshServicesInternal();
                    
                    EditorApplication.delayCall += () =>
                    {
                        if (_isRefreshing && EditorApplication.timeSinceStartup - _refreshStartTime < 10.0)
                        {
                            _services = newServices;
                            RefreshArchitectureList();
                            
                            // 清除所有缓存
                            _filterCacheValid = false;
                            _cachedFilteredServices = null;
                            _archCacheValid = false;
                            _cachedArchToNamespace = null;
                            
                            // 如果当前选择的服务不在新列表中，清除选择
                            if (_selectedService != null && 
                                !_services.Any(s => s.ServiceType == _selectedService.ServiceType))
                            {
                                _selectedService = null;
                            }
                        }
                        
                        _isRefreshing = false;
                        Repaint();
                    };
                }
                catch (Exception ex)
                {
                    // 异常处理：确保UI状态正确重置
                    UnityEngine.Debug.LogError($"ServiceOverviewWindow: 刷新服务列表时发生异常 - {ex.Message}\n{ex.StackTrace}");
                    
                    EditorApplication.delayCall += () =>
                    {
                        _services = new List<ServiceDescriptor>(); // 清空数据
                        _selectedService = null;
                        
                        // 清除所有缓存
                        _filterCacheValid = false;
                        _cachedFilteredServices = null;
                        _archCacheValid = false;
                        _cachedArchToNamespace = null;
                        
                        _isRefreshing = false;
                        Repaint();
                    };
                }
            });
        }

        private List<ServiceDescriptor> RefreshServicesInternal()
        {
            return ServiceInspector.GetAllServices();
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
