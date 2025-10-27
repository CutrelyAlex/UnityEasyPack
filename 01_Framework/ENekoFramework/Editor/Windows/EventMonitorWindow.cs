using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyPack.ENekoFramework.Editor.Windows
{
    /// <summary>
    /// 事件监视器窗口
    /// 实时显示事件发布历史、订阅情况和事件流
    /// </summary>
    public class EventMonitorWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private List<EventLogEntry> _eventLog = new List<EventLogEntry>();
        private string _filterText = "";
        private bool _autoScroll = true;
        private const int MaxLogEntries = 1000;
        
        // 筛选器
        private List<string> _architectureNames = new List<string>();
        private List<bool> _architectureFilters = new List<bool>();
        private string _selectedEventTypeFilter = "";
        private bool _useEventTypeFilter = false;
        private Vector2 _filterScrollPosition;
        
        // 用于在编辑器中捕获运行时事件
        private static EventMonitorWindow _activeWindow;

        /// <summary>
        /// 显示事件监视器窗口
        /// </summary>
        public static void ShowWindow()
        {
            var window = GetWindow<EventMonitorWindow>("Event Monitor");
            window.minSize = new Vector2(600, 400);
            window.Show();
            _activeWindow = window;
        }

        /// <summary>
        /// 记录事件（由框架调用）
        /// </summary>
        public static void LogEvent(Type eventType, object eventData, int subscriberCount)
        {
            if (_activeWindow != null)
            {
                _activeWindow.AddEventLog(eventType, eventData, subscriberCount);
            }
        }

        private void OnEnable()
        {
            _activeWindow = this;
        }

        private void OnDisable()
        {
            if (_activeWindow == this)
            {
                _activeWindow = null;
            }
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawFilters();
            DrawEventLog();
            DrawFooter();
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
            
            // 事件类型筛选
            _useEventTypeFilter = EditorGUILayout.ToggleLeft("按事件类型筛选", _useEventTypeFilter, GUILayout.Width(120));
            if (_useEventTypeFilter)
            {
                _selectedEventTypeFilter = EditorGUILayout.TextField(_selectedEventTypeFilter, GUILayout.Width(150));
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button("清空", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                _eventLog.Clear();
            }
            
            _autoScroll = GUILayout.Toggle(_autoScroll, "自动滚动", EditorStyles.toolbarButton, GUILayout.Width(80));
            
            GUILayout.Space(10);
            GUILayout.Label("筛选:", EditorStyles.toolbarButton, GUILayout.Width(40));
            _filterText = GUILayout.TextField(_filterText, EditorStyles.toolbarTextField, GUILayout.Width(150));
            
            if (GUILayout.Button("×", EditorStyles.toolbarButton, GUILayout.Width(20)))
            {
                _filterText = "";
                GUI.FocusControl(null);
            }
            
            GUILayout.FlexibleSpace();
            
            GUILayout.Label($"事件总数: {_eventLog.Count}/{MaxLogEntries}", EditorStyles.toolbarButton);
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawEventLog()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            var filteredLogs = GetFilteredLogs();
            
            if (filteredLogs.Count == 0)
            {
                if (_eventLog.Count == 0)
                {
                    EditorGUILayout.HelpBox("暂无事件记录。当应用运行时，事件将显示在这里。", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("筛选无匹配结果", MessageType.Info);
                }
            }
            else
            {
                foreach (var entry in filteredLogs)
                {
                    DrawEventEntry(entry);
                }
                
                if (_autoScroll)
                {
                    _scrollPosition.y = float.MaxValue;
                }
            }
            
            EditorGUILayout.EndScrollView();
        }

        private void DrawEventEntry(EventLogEntry entry)
        {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.BeginHorizontal();
            
            // 时间戳
            GUILayout.Label(entry.Timestamp.ToString("HH:mm:ss.fff"), GUILayout.Width(100));
            
            // 事件类型
            var prevColor = GUI.contentColor;
            GUI.contentColor = GetEventTypeColor(entry.EventType);
            GUILayout.Label(entry.EventType.Name, EditorStyles.boldLabel, GUILayout.Width(200));
            GUI.contentColor = prevColor;
            
            // 订阅者数量
            GUILayout.Label($"订阅者: {entry.SubscriberCount}", GUILayout.Width(80));
            
            GUILayout.FlexibleSpace();
            
            // 展开/折叠按钮
            if (GUILayout.Button(entry.IsExpanded ? "▼" : "▶", EditorStyles.label, GUILayout.Width(20)))
            {
                entry.IsExpanded = !entry.IsExpanded;
            }
            
            EditorGUILayout.EndHorizontal();
            
            // 详细信息（展开时显示）
            if (entry.IsExpanded)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.LabelField("完整类型:", entry.EventType.FullName, EditorStyles.wordWrappedLabel);
                
                if (entry.EventData != null)
                {
                    EditorGUILayout.LabelField("事件数据:", entry.DataSummary, EditorStyles.wordWrappedLabel);
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawFooter()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (_eventLog.Count >= MaxLogEntries)
            {
                EditorGUILayout.HelpBox($"已达到最大记录数 ({MaxLogEntries})，旧事件将被自动清除", MessageType.Warning);
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private List<EventLogEntry> GetFilteredLogs()
        {
            var filtered = _eventLog.ToList();
            
            // 架构筛选
            var selectedArchitectures = new List<string>();
            for (int i = 0; i < _architectureNames.Count; i++)
            {
                if (_architectureFilters[i])
                    selectedArchitectures.Add(_architectureNames[i]);
            }
            
            if (selectedArchitectures.Count > 0)
            {
                filtered = filtered.Where(e =>
                    selectedArchitectures.Any(arch => e.EventType.FullName?.Contains(arch) ?? false)
                ).ToList();
            }
            
            // 事件类型筛选
            if (_useEventTypeFilter && !string.IsNullOrEmpty(_selectedEventTypeFilter))
            {
                filtered = filtered.Where(e =>
                    e.EventType.Name.IndexOf(_selectedEventTypeFilter, StringComparison.OrdinalIgnoreCase) >= 0
                ).ToList();
            }
            
            // 文本筛选
            if (!string.IsNullOrEmpty(_filterText))
            {
                filtered = filtered.Where(e =>
                    e.EventType.Name.IndexOf(_filterText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    e.EventType.FullName.IndexOf(_filterText, StringComparison.OrdinalIgnoreCase) >= 0
                ).ToList();
            }
            
            // 刷新架构列表
            if (filtered.Count != _eventLog.Count || _architectureNames.Count == 0)
            {
                RefreshArchitectureList();
            }
            
            return filtered;
        }
        
        private void RefreshArchitectureList()
        {
            _architectureNames.Clear();
            _architectureFilters.Clear();
            
            if (_eventLog == null || _eventLog.Count == 0)
                return;
            
            var architectures = _eventLog
                .Select(e => e.EventType.FullName?.Split('.').FirstOrDefault() ?? "Unknown")
                .Distinct()
                .ToList();
            
            foreach (var arch in architectures)
            {
                _architectureNames.Add(arch);
                _architectureFilters.Add(true);
            }
        }

        private void AddEventLog(Type eventType, object eventData, int subscriberCount)
        {
            var entry = new EventLogEntry
            {
                Timestamp = DateTime.Now,
                EventType = eventType,
                EventData = eventData,
                SubscriberCount = subscriberCount,
                DataSummary = GetEventDataSummary(eventData)
            };
            
            _eventLog.Add(entry);
            
            // 限制日志条数
            if (_eventLog.Count > MaxLogEntries)
            {
                _eventLog.RemoveAt(0);
            }
            
            Repaint();
        }

        private string GetEventDataSummary(object eventData)
        {
            if (eventData == null)
                return "null";
            
            try
            {
                // 尝试序列化为 JSON
                var json = JsonUtility.ToJson(eventData);
                if (!string.IsNullOrEmpty(json))
                {
                    return json.Length > 200 ? json.Substring(0, 200) + "..." : json;
                }
            }
            catch
            {
                // 如果无法序列化，使用 ToString
            }
            
            var str = eventData.ToString();
            return str.Length > 200 ? str.Substring(0, 200) + "..." : str;
        }

        private Color GetEventTypeColor(Type eventType)
        {
            // 根据事件类型名称生成一致的颜色
            var hash = eventType.FullName.GetHashCode();
            var hue = (hash & 0xFF) / 255f;
            return Color.HSVToRGB(hue, 0.6f, 0.9f);
        }

        /// <summary>
        /// 事件日志条目
        /// </summary>
        private class EventLogEntry
        {
            public DateTime Timestamp { get; set; }
            public Type EventType { get; set; }
            public object EventData { get; set; }
            public int SubscriberCount { get; set; }
            public string DataSummary { get; set; }
            public bool IsExpanded { get; set; }
        }
    }
}
