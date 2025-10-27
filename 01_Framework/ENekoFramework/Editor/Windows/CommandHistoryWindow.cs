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
        private const double RefreshInterval = 0.5; // 每0.5秒刷新一次
        
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
        }

        private void Update()
        {
            if (_autoRefresh && EditorApplication.timeSinceStartup - _lastRefreshTime > RefreshInterval)
            {
                RefreshHistory();
                Repaint();
            }
        }

        private void OnGUI()
        {
            DrawToolbar();
            
            EditorGUILayout.BeginHorizontal();
            
            // 左侧：命令列表
            DrawCommandList();
            
            // 右侧：命令详情
            DrawCommandDetails();
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                RefreshHistory();
            }
            
            if (GUILayout.Button("清空", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                ClearHistory();
            }
            
            _autoRefresh = GUILayout.Toggle(_autoRefresh, "自动刷新", EditorStyles.toolbarButton, GUILayout.Width(80));
            
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
            
            if (_commandHistory != null && _commandHistory.Count > 0)
            {
                // 倒序显示（最新的在上面）
                for (int i = _commandHistory.Count - 1; i >= 0; i--)
                {
                    DrawCommandItem(_commandHistory[i]);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("暂无命令执行记录", MessageType.Info);
            }
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
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
            try
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
                
                _commandHistory = allHistories.OrderBy(c => c.StartedAt).ToList();
                _lastRefreshTime = EditorApplication.timeSinceStartup;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"CommandHistoryWindow: 刷新失败 - {ex.Message}");
            }
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
