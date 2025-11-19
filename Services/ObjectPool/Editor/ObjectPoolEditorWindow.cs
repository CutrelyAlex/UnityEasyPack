#if UNITY_EDITOR

using EasyPack.Architecture;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EasyPack.ObjectPool
{
    /// <summary>
    /// 对象池编辑器监控窗口。
    /// 实时显示所有活跃池的状态和统计信息。
    /// 主动启用统计收集以避免性能影响。
    /// </summary>
    public class ObjectPoolEditorWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private bool _isMonitoring = false;
        private IObjectPoolService _poolService;
        private List<PoolStatistics> _currentStats = new List<PoolStatistics>();
        private float _refreshInterval = 0.5f;
        private float _lastRefreshTime;

        /// <summary>
        /// 从菜单打开监控窗口。
        /// </summary>
        [MenuItem("EasyPack/Services/ObjectPool/ObjectPool Monitor")]
        public static void ShowWindow()
        {
            GetWindow<ObjectPoolEditorWindow>("ObjectPool Monitor");
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;

            // 监控窗口关闭时禁用统计收集
            if (_poolService != null && _poolService is ObjectPoolService service)
            {
                service.StatisticsEnabled = false;
            }
        }

        private void OnEditorUpdate()
        {
            if (!_isMonitoring) return;

            _lastRefreshTime += Time.deltaTime;
            if (_lastRefreshTime >= _refreshInterval)
            {
                _lastRefreshTime = 0;
                RefreshStatistics();
                Repaint();
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("对象池监控", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 获取池服务
            if (_poolService == null)
            {
                if (GUILayout.Button("连接对象池服务", GUILayout.Height(30)))
                {
                    ConnectToPoolService();
                }
                EditorGUILayout.HelpBox("请点击\"连接对象池服务\"按钮来启动监控", MessageType.Info);
                return;
            }

            // 控制按钮
            EditorGUILayout.BeginHorizontal();

            if (!_isMonitoring)
            {
                if (GUILayout.Button("▶ 开始监控", GUILayout.Height(30), GUILayout.Width(100)))
                {
                    StartMonitoring();
                }
            }
            else
            {
                if (GUILayout.Button("⏸ 停止监控", GUILayout.Height(30), GUILayout.Width(100)))
                {
                    StopMonitoring();
                }
            }

            if (GUILayout.Button("↻ 刷新", GUILayout.Height(30), GUILayout.Width(100)))
            {
                RefreshStatistics();
            }

            if (GUILayout.Button("✕ 重置统计", GUILayout.Height(30), GUILayout.Width(100)))
            {
                ResetStatistics();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            // 刷新间隔设置
            _refreshInterval = EditorGUILayout.Slider("刷新间隔(秒)", _refreshInterval, 0.1f, 5f);
            EditorGUILayout.Space();

            // 统计信息表格
            DrawStatisticsTable();
        }

        private void ConnectToPoolService()
        {
            try
            {
                var task = EasyPackArchitecture.GetObjectPoolServiceAsync();
                if (task.IsCompleted)
                {
                    _poolService = task.Result;
                    EditorUtility.DisplayDialog("成功", "已连接到对象池服务", "确定");
                }
                else
                {
                    EditorUtility.DisplayDialog("错误", "对象池服务未就绪", "确定");
                }
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("错误", $"连接失败: {ex.Message}", "确定");
            }
        }

        private void StartMonitoring()
        {
            if (_poolService is ObjectPoolService service)
            {
                service.StatisticsEnabled = true;
                _isMonitoring = true;
                _lastRefreshTime = 0;
                RefreshStatistics();
            }
        }

        private void StopMonitoring()
        {
            if (_poolService is ObjectPoolService service)
            {
                service.StatisticsEnabled = false;
            }
            _isMonitoring = false;
            _currentStats.Clear();
        }

        private void RefreshStatistics()
        {
            if (_poolService == null) return;

            _currentStats.Clear();
            var stats = _poolService.GetAllStatistics();
            if (stats != null)
            {
                foreach (var stat in stats)
                {
                    _currentStats.Add(stat);
                }
            }
        }

        private void ResetStatistics()
        {
            if (_poolService != null)
            {
                _poolService.ResetAllStatistics();
                RefreshStatistics();
            }
        }

        private void DrawStatisticsTable()
        {
            if (_currentStats.Count == 0)
            {
                EditorGUILayout.HelpBox("暂无池数据。请启动监控。", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("活跃池列表", EditorStyles.boldLabel);

            // 表头
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("类型名称", GUILayout.Width(150));
            GUILayout.Label("当前大小", GUILayout.Width(80));
            GUILayout.Label("最大容量", GUILayout.Width(80));
            GUILayout.Label("总租用", GUILayout.Width(80));
            GUILayout.Label("创建数", GUILayout.Width(80));
            GUILayout.Label("命中率", GUILayout.Width(80));
            GUILayout.Label("峰值", GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            // 内容
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            foreach (var stat in _currentStats)
            {
                DrawStatisticsRow(stat);
            }

            EditorGUILayout.EndScrollView();

            // 汇总统计
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("汇总统计", EditorStyles.boldLabel);

            int totalPools = _currentStats.Count;
            int totalCurrentSize = 0;
            int totalRentCount = 0;
            int totalCreateCount = 0;
            int totalHitCount = 0;

            foreach (var stat in _currentStats)
            {
                totalCurrentSize += stat.CurrentPoolSize;
                totalRentCount += stat.RentCount;
                totalCreateCount += stat.CreateCount;
                totalHitCount += stat.HitCount;
            }

            float totalHitRate = totalRentCount > 0 ? (float)totalHitCount / totalRentCount * 100f : 0f;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"活跃池数: {totalPools}");
            EditorGUILayout.LabelField($"当前对象总数: {totalCurrentSize}");
            EditorGUILayout.LabelField($"总租用次数: {totalRentCount}");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"总创建数: {totalCreateCount}");
            EditorGUILayout.LabelField($"总命中数: {totalHitCount}");
            EditorGUILayout.LabelField($"总命中率: {totalHitRate:F1}%");
            EditorGUILayout.EndHorizontal();
        }

        private void DrawStatisticsRow(PoolStatistics stat)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbarButton);
            GUILayout.Label(stat.TypeName, GUILayout.Width(150));
            GUILayout.Label(stat.CurrentPoolSize.ToString(), GUILayout.Width(80));
            GUILayout.Label(stat.MaxCapacity.ToString(), GUILayout.Width(80));
            GUILayout.Label(stat.RentCount.ToString(), GUILayout.Width(80));
            GUILayout.Label(stat.CreateCount.ToString(), GUILayout.Width(80));
            GUILayout.Label($"{stat.HitRate * 100f:F1}%", GUILayout.Width(80));
            GUILayout.Label(stat.PeakPoolSize.ToString(), GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
        }
    }
}

#endif
