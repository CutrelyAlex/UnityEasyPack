#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EasyPack.ENekoFramework.Editor
{
    /// <summary>
    ///     事件监视器窗口
    ///     实时显示事件发布历史、订阅情况和事件流
    /// </summary>
    public class EventMonitorWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private List<EventLogEntry> _eventLog = new();
        private bool _autoScroll = true;
        private const int MaxLogEntries = 1000;

        // 筛选缓存
        private List<EventLogEntry> _cachedFilteredLogs;
        private List<string> _lastSelectedArchitectures = new();
        private string _lastSelectedEventTypeFilter = "";
        private bool _lastUseEventTypeFilter = false;
        private bool _filterCacheValid = false;

        // 架构缓存
        private Dictionary<string, string> _cachedArchToNamespace;
        private bool _archCacheValid = false;

        // 筛选器
        private List<string> _architectureNames = new();
        private List<bool> _architectureFilters = new();
        private string _selectedEventTypeFilter = "";
        private bool _useEventTypeFilter = false;
        private Vector2 _filterScrollPosition;

        // 数据持久化
        private const string EventLogDataKey = "EasyPack.EventMonitor.EventLogData";
        private const string EventLogTimestampKey = "EasyPack.EventMonitor.EventLogTimestamp";
        private static readonly TimeSpan MaxDataAge = TimeSpan.FromMinutes(30); // 数据最长保存30分钟

        // 自动刷新
        private bool _autoRefreshEnabled = true;
        private float _refreshInterval = 2.0f; // 2秒刷新一次
        private double _lastRefreshTime;
        private Dictionary<Type, int> _subscriberCountCache = new();
        private double _lastSubscriberCacheTime;
        private const double SubscriberCacheDuration = 0.5; // 订阅者缓存0.5秒

        // 用于在编辑器中捕获运行时事件
        private static EventMonitorWindow _activeWindow;

        /// <summary>
        ///     显示事件监视器窗口
        /// </summary>
        public static void ShowWindow()
        {
            var window = GetWindow<EventMonitorWindow>("Event Monitor");
            window.minSize = new(600, 400);
            window.Show();
            _activeWindow = window;
        }

        /// <summary>
        ///     记录事件（由框架调用）
        /// </summary>
        public static void LogEvent(Type eventType, object eventData, int subscriberCount)
        {
            if (_activeWindow != null) _activeWindow.AddEventLog(eventType, eventData, subscriberCount);
        }

        private void OnEnable()
        {
            _activeWindow = this;
            RefreshArchitectureList();

            // 从持久化存储恢复事件日志
            LoadEventLogData();

            // 初始化自动刷新时间戳
            _lastRefreshTime = EditorApplication.timeSinceStartup;
            _lastSubscriberCacheTime = EditorApplication.timeSinceStartup;

            // 连接EventBus监控回调
#if UNITY_EDITOR
            EventBus.OnEventPublished += LogEventFromBus;
#endif
        }

        private void OnDisable()
        {
            // 保存事件日志到持久化存储
            SaveEventLogData();

            if (_activeWindow == this) _activeWindow = null;

#if UNITY_EDITOR
            EventBus.OnEventPublished -= LogEventFromBus;
#endif
        }

        private void LogEventFromBus(Type eventType, object eventData, int subscriberCount)
        {
            // 使用传入的订阅者数量（来自EventBus），但会在自动刷新时更新
            AddEventLog(eventType, eventData, subscriberCount);
        }

        /// <summary>
        ///     刷新订阅者数量缓存
        /// </summary>
        private void RefreshSubscriberCountCache()
        {
            try
            {
                var architectures = ServiceInspector.GetAllArchitectureInstances();
                var eventTypes = new HashSet<Type>();

                // 收集所有事件类型
                foreach (EventLogEntry entry in _eventLog)
                {
                    if (entry.EventType != null)
                        eventTypes.Add(entry.EventType);
                }

                // 重新计算每个事件类型的订阅者数量
                foreach (Type eventType in eventTypes)
                {
                    int totalCount = 0;

                    foreach (object architecture in architectures)
                    {
                        try
                        {
                            EventBus eventBus = ServiceInspector.GetEventBusFromArchitecture(architecture);
                            if (eventBus != null)
                            {
                                MethodInfo method = eventBus.GetType().GetMethod("GetSubscriberCount")
                                    .MakeGenericMethod(eventType);
                                int count = (int)method.Invoke(eventBus, null);
                                totalCount += count;
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[EventMonitor] 刷新订阅者缓存失败: {ex.Message}");
                        }
                    }

                    _subscriberCountCache[eventType] = totalCount;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[EventMonitor] 刷新订阅者缓存失败: {ex.Message}");
            }
        }

        /// <summary>
        ///     刷新显示的订阅者数量
        /// </summary>
        private void RefreshDisplayedSubscriberCounts()
        {
            bool needsRepaint = false;

            // 更新所有事件日志条目的订阅者数量
            foreach (EventLogEntry entry in _eventLog)
            {
                if (entry.EventType != null && _subscriberCountCache.TryGetValue(entry.EventType, out int cachedCount))
                    if (entry.SubscriberCount != cachedCount)
                    {
                        entry.SubscriberCount = cachedCount;
                        needsRepaint = true;
                    }
            }

            if (needsRepaint) Repaint();
        }

        private void OnGUI()
        {
            if (_autoRefreshEnabled && Event.current.type == EventType.Layout)
            {
                double currentTime = EditorApplication.timeSinceStartup;

                // 检查是否需要刷新订阅者数量缓存
                if (currentTime - _lastSubscriberCacheTime > SubscriberCacheDuration)
                {
                    RefreshSubscriberCountCache();
                    _lastSubscriberCacheTime = currentTime;
                }

                // 检查是否需要刷新显示
                if (currentTime - _lastRefreshTime > _refreshInterval)
                {
                    RefreshDisplayedSubscriberCounts();
                    _lastRefreshTime = currentTime;
                }
            }

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
                _selectedEventTypeFilter = EditorGUILayout.TextField(_selectedEventTypeFilter, GUILayout.Width(150));

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("清空", EditorStyles.toolbarButton, GUILayout.Width(60))) _eventLog.Clear();

            // 自动刷新开关
            _autoRefreshEnabled = GUILayout.Toggle(_autoRefreshEnabled, "自动刷新", EditorStyles.toolbarButton,
                GUILayout.Width(80));

            if (_autoRefreshEnabled)
            {
                // 刷新间隔设置
                GUILayout.Label("间隔:", EditorStyles.toolbarButton, GUILayout.Width(35));
                float newInterval = EditorGUILayout.FloatField(_refreshInterval, EditorStyles.toolbarTextField,
                    GUILayout.Width(40));
                if (!Mathf.Approximately(newInterval, _refreshInterval))
                    _refreshInterval = Mathf.Max(0.1f, newInterval); // 最小0.1秒

                GUILayout.Label("秒", EditorStyles.toolbarButton, GUILayout.Width(20));
            }
            else
            {
                // 手动刷新按钮
                if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(50)))
                {
                    RefreshSubscriberCountCache();
                    RefreshDisplayedSubscriberCounts();
                    _lastRefreshTime = EditorApplication.timeSinceStartup;
                }
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
                    EditorGUILayout.HelpBox("暂无事件记录。当应用运行时，事件将显示在这里。", MessageType.Info);
                else
                    EditorGUILayout.HelpBox("筛选无匹配结果", MessageType.Info);
            }
            else
            {
                foreach (EventLogEntry entry in filteredLogs)
                {
                    DrawEventEntry(entry);
                }

                if (_autoScroll) _scrollPosition.y = float.MaxValue;
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
            Color prevColor = GUI.contentColor;
            GUI.contentColor = GetEventTypeColor(entry.EventType);
            GUILayout.Label(entry.EventType.Name, EditorStyles.boldLabel, GUILayout.Width(200));
            GUI.contentColor = prevColor;

            // 订阅者数量
            GUILayout.Label($"订阅者: {entry.SubscriberCount}", GUILayout.Width(80));

            GUILayout.FlexibleSpace();

            // 展开/折叠按钮
            if (GUILayout.Button(entry.IsExpanded ? "▼" : "▶", EditorStyles.label, GUILayout.Width(20)))
                entry.IsExpanded = !entry.IsExpanded;

            EditorGUILayout.EndHorizontal();

            // 详细信息（展开时显示）
            if (entry.IsExpanded)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.LabelField("完整类型:", entry.EventType.FullName, EditorStyles.wordWrappedLabel);

                if (entry.EventData != null)
                    EditorGUILayout.LabelField("事件数据:", entry.DataSummary, EditorStyles.wordWrappedLabel);

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawFooter()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (_eventLog.Count >= MaxLogEntries)
                EditorGUILayout.HelpBox($"已达到最大记录数 ({MaxLogEntries})，旧事件将被自动清除", MessageType.Warning);

            EditorGUILayout.EndHorizontal();
        }

        private List<EventLogEntry> GetFilteredLogs()
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
                                 _lastUseEventTypeFilter != _useEventTypeFilter ||
                                 (_useEventTypeFilter && _lastSelectedEventTypeFilter != _selectedEventTypeFilter);

            if (!filterChanged && _cachedFilteredLogs != null) return _cachedFilteredLogs;

            // 重新计算过滤结果
            var filtered = _eventLog.ToList();

            // 架构筛选：仅当有架构被勾选时才进行筛选，否则显示空列表
            if (currentSelectedArchitectures.Count > 0)
            {
                // 使用缓存的架构映射，避免每次都进行反射
                if (!_archCacheValid || _cachedArchToNamespace == null)
                {
                    var allArchitectures = ServiceInspector.GetAllArchitectureInstances();
                    _cachedArchToNamespace = new();

                    // 建立架构名称到其所在命名空间的映射
                    foreach (object arch in allArchitectures)
                    {
                        string archName = arch.GetType().Name;
                        string archNamespace = arch.GetType().Namespace;
                        _cachedArchToNamespace.TryAdd(archName, archNamespace);
                    }

                    _archCacheValid = true;
                }

                filtered = filtered.Where(e =>
                {
                    string eventNamespace = e.EventType.Namespace;
                    return currentSelectedArchitectures.Any(arch =>
                        _cachedArchToNamespace.ContainsKey(arch) &&
                        eventNamespace?.StartsWith(_cachedArchToNamespace[arch]) == true
                    );
                }).ToList();
            }
            else
            {
                // 当没有勾选任何架构时，显示空列表
                filtered = new();
            }

            // 事件类型筛选
            if (_useEventTypeFilter && !string.IsNullOrEmpty(_selectedEventTypeFilter))
                filtered = filtered.Where(e =>
                    e.EventType.Name.IndexOf(_selectedEventTypeFilter, StringComparison.OrdinalIgnoreCase) >= 0
                ).ToList();

            _cachedFilteredLogs = filtered;

            // 更新缓存状态
            _lastSelectedArchitectures = currentSelectedArchitectures.ToList();
            _lastUseEventTypeFilter = _useEventTypeFilter;
            _lastSelectedEventTypeFilter = _selectedEventTypeFilter;
            _filterCacheValid = true;

            return _cachedFilteredLogs;
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
            foreach (string arch in architectureNames)
            {
                _architectureNames.Add(arch);
                // 恢复之前的筛选状态，如果架构不存在则默认为true（全选）
                _architectureFilters.Add(previousFilters.GetValueOrDefault(arch, true));
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
                DataSummary = GetEventDataSummary(eventData),
            };

            _eventLog.Add(entry);

            // 限制日志条数
            if (_eventLog.Count > MaxLogEntries) _eventLog.RemoveAt(0);

            // 清除筛选缓存，因为数据已更新
            _filterCacheValid = false;
            _cachedFilteredLogs = null;

            Repaint();
        }

        private string GetEventDataSummary(object eventData)
        {
            if (eventData == null)
                return "null";

            try
            {
                // 尝试序列化为 JSON
                string json = JsonUtility.ToJson(eventData);
                if (!string.IsNullOrEmpty(json)) return json.Length > 200 ? json.Substring(0, 200) + "..." : json;
            }
            catch
            {
                // 如果无法序列化，使用 ToString
            }

            string str = eventData.ToString();
            return str.Length > 200 ? str.Substring(0, 200) + "..." : str;
        }

        private Color GetEventTypeColor(Type eventType)
        {
            // 根据事件类型名称生成一致的颜色
            int hash = eventType.FullName.GetHashCode();
            float hue = (hash & 0xFF) / 255f;
            return Color.HSVToRGB(hue, 0.6f, 0.9f);
        }

        /// <summary>
        ///     事件日志条目
        /// </summary>
        public class EventLogEntry
        {
            public DateTime Timestamp { get; set; }
            public Type EventType { get; set; }
            public object EventData { get; set; }
            public int SubscriberCount { get; set; }
            public string DataSummary { get; set; }
            public bool IsExpanded { get; set; }
        }

        /// <summary>
        ///     保存事件日志数据到EditorPrefs
        /// </summary>
        private void SaveEventLogData()
        {
            try
            {
                // 只保存最近的100条记录以避免数据过大
                var entriesToSave = _eventLog.Take(100).ToList();

                // 序列化事件日志
                var serializableEntries = entriesToSave.Select(entry => new SerializableEventLogEntry
                {
                    Timestamp = entry.Timestamp,
                    EventTypeName = entry.EventType?.FullName ?? "Unknown",
                    EventTypeAssembly = entry.EventType?.Assembly?.FullName ?? "",
                    SubscriberCount = entry.SubscriberCount,
                    DataSummary = entry.DataSummary ?? "",
                    IsExpanded = entry.IsExpanded,
                }).ToList();

                string jsonData = JsonUtility.ToJson(new SerializableEventLogData
                {
                    Entries = serializableEntries.ToArray(), SaveTime = DateTime.Now,
                });

                EditorPrefs.SetString(EventLogDataKey, jsonData);
                EditorPrefs.SetString(EventLogTimestampKey, DateTime.Now.ToString("O"));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[EventMonitor] 保存事件日志失败: {ex.Message}");
            }
        }

        /// <summary>
        ///     从EditorPrefs加载事件日志数据
        /// </summary>
        private void LoadEventLogData()
        {
            try
            {
                string timestampStr = EditorPrefs.GetString(EventLogTimestampKey, "");
                if (string.IsNullOrEmpty(timestampStr))
                    return;

                if (!DateTime.TryParse(timestampStr, out DateTime saveTime))
                    return;

                // 检查数据是否过期
                if (DateTime.Now - saveTime > MaxDataAge)
                {
                    // 清除过期数据
                    EditorPrefs.DeleteKey(EventLogDataKey);
                    EditorPrefs.DeleteKey(EventLogTimestampKey);
                    return;
                }

                string jsonData = EditorPrefs.GetString(EventLogDataKey, "");
                if (string.IsNullOrEmpty(jsonData))
                    return;

                var serializableData = JsonUtility.FromJson<SerializableEventLogData>(jsonData);
                if (serializableData?.Entries == null)
                    return;

                // 反序列化事件日志
                _eventLog.Clear();
                foreach (SerializableEventLogEntry serializableEntry in serializableData.Entries)
                {
                    try
                    {
                        var eventType =
                            Type.GetType($"{serializableEntry.EventTypeName}, {serializableEntry.EventTypeAssembly}");

                        var entry = new EventLogEntry
                        {
                            Timestamp = serializableEntry.Timestamp,
                            EventType = eventType,
                            EventData = null, // 无法恢复原始事件数据
                            SubscriberCount = serializableEntry.SubscriberCount,
                            DataSummary = serializableEntry.DataSummary,
                            IsExpanded = serializableEntry.IsExpanded,
                        };

                        _eventLog.Add(entry);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[EventMonitor] 恢复事件日志条目失败: {ex.Message}");
                    }
                }

                // 清除筛选缓存，因为数据已更新
                _filterCacheValid = false;
                _cachedFilteredLogs = null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[EventMonitor] 加载事件日志失败: {ex.Message}");
            }
        }

        /// <summary>
        ///     可序列化的事件日志数据
        /// </summary>
        [Serializable]
        private class SerializableEventLogData
        {
            public SerializableEventLogEntry[] Entries;
            public DateTime SaveTime;
        }

        /// <summary>
        ///     可序列化的事件日志条目
        /// </summary>
        [Serializable]
        private class SerializableEventLogEntry
        {
            public DateTime Timestamp;
            public string EventTypeName;
            public string EventTypeAssembly;
            public int SubscriberCount;
            public string DataSummary;
            public bool IsExpanded;
        }
    }
}
#endif