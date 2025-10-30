using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyPack.ENekoFramework.Editor.Windows
{
    /// <summary>
    /// 命令历史窗口
    /// 显示命令执行历史、状态、时间线和错误详情
    /// </summary>
    public class CommandHistoryWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private List<CommandDescriptor> _commandHistory;
        private CommandDescriptor _selectedCommand;
        private bool _autoRefresh = true;
        private double _lastRefreshTime;
        private bool _isRefreshing = false;
        private double _refreshStartTime;
        private const double RefreshInterval = 0.5;
        
        // 筛选缓存
        private List<CommandDescriptor> _cachedFilteredHistory;
        private List<string> _lastSelectedArchitectures = new List<string>();
        private CommandStatus _lastSelectedStatusFilter = CommandStatus.Succeeded;
        private bool _lastUseStatusFilter = false;
        private bool _filterCacheValid = false;
        
        // 架构缓存
        private Dictionary<string, string> _cachedArchToNamespace;
        private bool _archCacheValid = false; 
        
        // 筛选器
        private List<string> _architectureNames = new List<string>();
        private List<bool> _architectureFilters = new List<bool>();
        private CommandStatus _selectedStatusFilter = CommandStatus.Succeeded;
        private bool _useStatusFilter = false;
        private Vector2 _filterScrollPosition;
        
        // 状态颜色
        private readonly Color _runningColor = new Color(0.3f, 0.8f, 1f);
        private readonly Color _succeededColor = new Color(0.3f, 1f, 0.3f);
        private readonly Color _failedColor = new Color(1f, 0.3f, 0.3f);
        private readonly Color _cancelledColor = new Color(0.7f, 0.7f, 0.7f);
        private readonly Color _timedOutColor = new Color(1f, 0.6f, 0.3f);

        /// <summary>
        /// 显示命令历史窗口
        /// </summary>
        public static void ShowWindow()
        {
            var window = GetWindow<CommandHistoryWindow>("Command History");
            window.minSize = new Vector2(700, 400);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshHistory();
            RefreshArchitectureList();
        }

        private void Update()
        {
            if (_isRefreshing && EditorApplication.timeSinceStartup - _refreshStartTime > 10.0)
            {
                Debug.LogWarning("CommandHistoryWindow: 刷新操作超时，强制重置状态");
                _isRefreshing = false;
                _commandHistory = new List<CommandDescriptor>();
                _selectedCommand = null;
                Repaint();
            }

            if (_autoRefresh && !_isRefreshing && EditorApplication.timeSinceStartup - _lastRefreshTime > RefreshInterval)
            {
                EditorApplication.delayCall += () =>
                {
                    RefreshHistoryAsync();
                };
                _lastRefreshTime = EditorApplication.timeSinceStartup;
            }
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawFilters();
            
            EditorGUILayout.BeginHorizontal();
            
            // 左侧：命令列表
            DrawCommandList();
            
            // 右侧：命令详情
            DrawCommandDetails();
            
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
            _useStatusFilter = EditorGUILayout.ToggleLeft("按状态筛选", _useStatusFilter, GUILayout.Width(80));
            if (_useStatusFilter)
            {
                _selectedStatusFilter = (CommandStatus)EditorGUILayout.EnumPopup(_selectedStatusFilter, GUILayout.Width(150));
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                RefreshHistory();
            }

            EditorGUILayout.BeginHorizontal(GUILayout.Width(80));
            if (_isRefreshing)
            {
                GUILayout.Label("刷新中...", EditorStyles.toolbarButton, GUILayout.ExpandWidth(true));
            }
            else
            {
                GUILayout.Label("", EditorStyles.toolbarButton, GUILayout.ExpandWidth(true));
            }
            EditorGUILayout.EndHorizontal();
            
            if (GUILayout.Button("清空", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                ClearHistory();
            }
            
            _autoRefresh = GUILayout.Toggle(_autoRefresh, "自动刷新", EditorStyles.toolbarButton, GUILayout.Width(80));
            
            // 监控开关
            var monitoringEnabled = EditorMonitoringConfig.EnableCommandMonitoring;
            var newMonitoringState = GUILayout.Toggle(monitoringEnabled, "启用监控", EditorStyles.toolbarButton, GUILayout.Width(80));
            if (newMonitoringState != monitoringEnabled)
            {
                EditorMonitoringConfig.EnableCommandMonitoring = newMonitoringState;
            }
            
            GUILayout.FlexibleSpace();
            
            if (_commandHistory != null)
            {
                var total = _commandHistory.Count;
                var success = _commandHistory.Count(c => c.Status == CommandStatus.Succeeded);
                var failed = _commandHistory.Count(c => c.Status == CommandStatus.Failed);
                var running = _commandHistory.Count(c => c.Status == CommandStatus.Running);
                
                GUILayout.Label($"总数: {total} | 成功: {success} | 失败: {failed} | 运行中: {running}", EditorStyles.toolbarButton);
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawCommandList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(350));
            
            EditorGUILayout.LabelField("命令历史", EditorStyles.boldLabel);
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            var filteredHistory = GetFilteredCommandHistory();
            
            if (filteredHistory != null && filteredHistory.Count > 0)
            {
                // 倒序显示
                for (int i = filteredHistory.Count - 1; i >= 0; i--)
                {
                    DrawCommandItem(filteredHistory[i]);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("暂无匹配的命令记录", MessageType.Info);
            }
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
        
        private List<CommandDescriptor> GetFilteredCommandHistory()
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
                _lastUseStatusFilter != _useStatusFilter ||
                (_useStatusFilter && _lastSelectedStatusFilter != _selectedStatusFilter);
            
            if (!filterChanged && _cachedFilteredHistory != null)
            {
                return _cachedFilteredHistory;
            }
            
            // 重新计算过滤结果
            if (_commandHistory == null || _commandHistory.Count == 0)
            {
                _cachedFilteredHistory = new List<CommandDescriptor>();
            }
            else
            {
                var filtered = _commandHistory.ToList();
                
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
                    
                    filtered = filtered.Where(c =>
                    {
                        var commandNamespace = c.CommandType.Namespace;
                        return currentSelectedArchitectures.Any(arch => 
                            _cachedArchToNamespace.ContainsKey(arch) && 
                            commandNamespace?.StartsWith(_cachedArchToNamespace[arch]) == true
                        );
                    }).ToList();
                }
                else
                {
                    // 当没有勾选任何架构时，显示空列表
                    filtered = new List<CommandDescriptor>();
                }
                
                // 状态筛选
                if (_useStatusFilter)
                {
                    filtered = filtered.Where(c => c.Status == _selectedStatusFilter).ToList();
                }
                
                _cachedFilteredHistory = filtered;
            }
            
            // 更新缓存状态
            _lastSelectedArchitectures = currentSelectedArchitectures.ToList();
            _lastUseStatusFilter = _useStatusFilter;
            _lastSelectedStatusFilter = _selectedStatusFilter;
            _filterCacheValid = true;
            
            return _cachedFilteredHistory;
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

        private void DrawCommandItem(CommandDescriptor command)
        {
            var isSelected = _selectedCommand == command;
            var bgColor = isSelected ? new Color(0.3f, 0.5f, 0.8f) : Color.clear;
            
            var prevBgColor = GUI.backgroundColor;
            GUI.backgroundColor = bgColor;
            
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.BeginHorizontal();
            
            // 状态指示器
            var statusColor = GetStatusColor(command.Status);
            var prevContentColor = GUI.contentColor;
            GUI.contentColor = statusColor;
            GUILayout.Label("●", GUILayout.Width(15));
            GUI.contentColor = prevContentColor;
            
            EditorGUILayout.BeginVertical();
            
            // 命令名称
            if (GUILayout.Button(command.CommandType.Name, EditorStyles.label))
            {
                _selectedCommand = command;
            }
            
            // 时间信息
            var timeText = command.StartedAt.ToString("HH:mm:ss");
            if (command.CompletedAt.HasValue)
            {
                timeText += $" ({command.ExecutionTimeMs:F2}ms)";
            }
            else if (command.Status == CommandStatus.Running)
            {
                timeText += " (运行中...)";
            }
            
            EditorGUILayout.LabelField(timeText, EditorStyles.miniLabel);
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            
            GUI.backgroundColor = prevBgColor;
        }

        private void DrawCommandDetails()
        {
            EditorGUILayout.BeginVertical();
            
            if (_selectedCommand != null)
            {
                EditorGUILayout.LabelField("命令详情", EditorStyles.boldLabel);
                EditorGUILayout.Space();
                
                // 基本信息
                DrawDetailRow("命令类型", _selectedCommand.CommandType.FullName);
                DrawDetailRow("执行 ID", _selectedCommand.ExecutionId.ToString());
                DrawDetailRow("状态", _selectedCommand.Status.ToString(), GetStatusColor(_selectedCommand.Status));
                
                EditorGUILayout.Space();
                
                // 时间信息
                EditorGUILayout.LabelField("时间信息", EditorStyles.boldLabel);
                DrawDetailRow("开始时间", _selectedCommand.StartedAt.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                
                if (_selectedCommand.CompletedAt.HasValue)
                {
                    DrawDetailRow("完成时间", _selectedCommand.CompletedAt.Value.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                    DrawDetailRow("执行时长", $"{_selectedCommand.ExecutionTimeMs:F2} ms");
                }
                
                DrawDetailRow("超时设置", $"{_selectedCommand.TimeoutSeconds} 秒");
                
                EditorGUILayout.Space();
                
                // 执行结果
                if (_selectedCommand.Status == CommandStatus.Succeeded && _selectedCommand.Result != null)
                {
                    EditorGUILayout.LabelField("执行结果", EditorStyles.boldLabel);
                    EditorGUILayout.TextArea(_selectedCommand.Result.ToString(), GUILayout.Height(60));
                }
                
                // 错误信息
                if (_selectedCommand.Status == CommandStatus.Failed && _selectedCommand.Exception != null)
                {
                    EditorGUILayout.LabelField("错误信息", EditorStyles.boldLabel);
                    
                    var prevColor = GUI.contentColor;
                    GUI.contentColor = _failedColor;
                    
                    EditorGUILayout.TextArea(
                        $"{_selectedCommand.Exception.GetType().Name}: {_selectedCommand.Exception.Message}\n\n{_selectedCommand.Exception.StackTrace}",
                        GUILayout.Height(120)
                    );
                    
                    GUI.contentColor = prevColor;
                }
                
                // 超时信息
                if (_selectedCommand.Status == CommandStatus.TimedOut)
                {
                    EditorGUILayout.HelpBox(
                        $"命令执行超过 {_selectedCommand.TimeoutSeconds} 秒超时限制",
                        MessageType.Warning
                    );
                }
                
                EditorGUILayout.Space();
                
                // 时间线可视化
                DrawTimeline();
            }
            else
            {
                EditorGUILayout.HelpBox("选择一个命令以查看详情", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawDetailRow(string label, string value, Color? labelColor = null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(100));
            
            if (labelColor.HasValue)
            {
                var prevColor = GUI.contentColor;
                GUI.contentColor = labelColor.Value;
                EditorGUILayout.LabelField(value, EditorStyles.wordWrappedLabel);
                GUI.contentColor = prevColor;
            }
            else
            {
                EditorGUILayout.LabelField(value, EditorStyles.wordWrappedLabel);
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTimeline()
        {
            if (_commandHistory == null || _commandHistory.Count == 0)
                return;
            
            EditorGUILayout.LabelField("执行时间线", EditorStyles.boldLabel);
            
            var rect = GUILayoutUtility.GetRect(100, 60);
            
            // 绘制背景
            EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f));
            
            // 计算时间范围
            var minTime = _commandHistory.Min(c => c.StartedAt);
            var maxTime = _commandHistory.Max(c => c.CompletedAt ?? DateTime.Now);
            var timeRange = (maxTime - minTime).TotalSeconds;
            
            if (timeRange <= 0)
                timeRange = 1;
            
            // 绘制每个命令的时间条
            foreach (var cmd in _commandHistory)
            {
                var startX = rect.x + (float)((cmd.StartedAt - minTime).TotalSeconds / timeRange * rect.width);
                var endTime = cmd.CompletedAt ?? DateTime.Now;
                var endX = rect.x + (float)((endTime - minTime).TotalSeconds / timeRange * rect.width);
                var width = endX - startX;
                
                if (width < 2)
                    width = 2;
                
                var barRect = new Rect(startX, rect.y + 10, width, rect.height - 20);
                var color = GetStatusColor(cmd.Status);
                
                if (cmd == _selectedCommand)
                {
                    // 高亮选中的命令
                    EditorGUI.DrawRect(new Rect(barRect.x - 2, barRect.y - 2, barRect.width + 4, barRect.height + 4), Color.white);
                }
                
                EditorGUI.DrawRect(barRect, color);
            }
        }

        private void RefreshHistory()
        {
            RefreshHistoryAsync();
        }

        private void RefreshHistoryAsync()
        {
            if (_isRefreshing) return; // 防止并发刷新
            
            _isRefreshing = true;
            _refreshStartTime = EditorApplication.timeSinceStartup;
            _lastRefreshTime = EditorApplication.timeSinceStartup;
            
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    var newHistory = RefreshHistoryInternal();
                    
                    EditorApplication.delayCall += () =>
                    {
                        if (_isRefreshing && EditorApplication.timeSinceStartup - _refreshStartTime < 10.0)
                        {
                            _commandHistory = newHistory;
                            RefreshArchitectureList();
                            
                            // 清除所有缓存
                            _filterCacheValid = false;
                            _cachedFilteredHistory = null;
                            _archCacheValid = false;
                            _cachedArchToNamespace = null;
                            
                            // 如果当前选择的命令不在新列表中，清除选择
                            if (_selectedCommand != null && 
                                !_commandHistory.Any(c => c.ExecutionId == _selectedCommand.ExecutionId))
                            {
                                _selectedCommand = null;
                            }
                        }
                        
                        _isRefreshing = false;
                        Repaint();
                    };
                }
                catch (Exception ex)
                {
                    // 异常处理：确保UI状态正确重置
                    UnityEngine.Debug.LogError($"CommandHistoryWindow: 刷新命令历史时发生异常 - {ex.Message}\n{ex.StackTrace}");
                    
                    EditorApplication.delayCall += () =>
                    {
                        _commandHistory = new List<CommandDescriptor>(); // 清空数据
                        _selectedCommand = null;
                        
                        // 清除所有缓存
                        _filterCacheValid = false;
                        _cachedFilteredHistory = null;
                        _archCacheValid = false;
                        _cachedArchToNamespace = null;
                        
                        _isRefreshing = false;
                        Repaint();
                    };
                }
            });
        }

        private List<CommandDescriptor> RefreshHistoryInternal()
        {
            // 获取所有架构实例的命令历史
            var allHistories = new List<CommandDescriptor>();
            
            var architectures = ServiceInspector.GetAllArchitectureInstances();
            foreach (var arch in architectures)
            {
                var dispatcherProp = arch.GetType().GetProperty("CommandDispatcher",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                
                if (dispatcherProp != null)
                {
                    var dispatcher = dispatcherProp.GetValue(arch) as CommandDispatcher;
                    if (dispatcher != null)
                    {
                        var history = dispatcher.GetCommandHistory();
                        if (history != null)
                        {
                            allHistories.AddRange(history);
                        }
                    }
                }
            }
            
            return allHistories.OrderBy(c => c.StartedAt).ToList();
        }

        private void ClearHistory()
        {
            try
            {
                var architectures = ServiceInspector.GetAllArchitectureInstances();
                foreach (var arch in architectures)
                {
                    var dispatcherProp = arch.GetType().GetProperty("CommandDispatcher",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                    
                    if (dispatcherProp != null)
                    {
                        var dispatcher = dispatcherProp.GetValue(arch) as CommandDispatcher;
                        dispatcher?.ClearHistory();
                    }
                }
                
                RefreshHistory();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"CommandHistoryWindow: 清空失败 - {ex.Message}");
            }
        }

        private Color GetStatusColor(CommandStatus status)
        {
            return status switch
            {
                CommandStatus.Running => _runningColor,
                CommandStatus.Succeeded => _succeededColor,
                CommandStatus.Failed => _failedColor,
                CommandStatus.Cancelled => _cancelledColor,
                CommandStatus.TimedOut => _timedOutColor,
                _ => Color.white
            };
        }
    }
}
