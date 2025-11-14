#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace EasyPack
{
    /// <summary>
    /// 对象池诊断菜单。
    /// 提供统计信息打印、报告生成等诊断功能。
    /// </summary>
    public static class PoolDiagnosticsMenu
    {
        private const string DIAGNOSTICS_FOLDER = "Assets/EasyPack/05_Services/ObjectPool/Diagnostics";

        /// <summary>
        /// 打印当前对象池统计信息。
        /// </summary>
        [MenuItem("EasyPack/Services/ObjectPool/Print Statistics")]
        public static void PrintStatistics()
        {
            try
            {
                var task = EasyPackArchitecture.GetObjectPoolServiceAsync();
                if (!task.IsCompleted)
                {
                    EditorUtility.DisplayDialog("错误", "对象池服务未就绪", "确定");
                    return;
                }

                var poolService = task.Result;
                if (poolService is ObjectPoolService service)
                {
                    // 临时启用统计收集
                    bool wasCollecting = service.StatisticsEnabled;
                    service.StatisticsEnabled = true;

                    var stats = service.GetAllStatistics();
                    if (stats == null)
                    {
                        Debug.Log("[PoolDiagnostics] 暂无池数据");
                        return;
                    }

                    var sb = new StringBuilder();
                    sb.AppendLine("====== 对象池统计信息 ======");
                    sb.AppendLine($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    sb.AppendLine();

                    int totalRent = 0, totalCreate = 0, totalHit = 0;

                    foreach (var stat in stats)
                    {
                        sb.AppendLine($"[{stat.TypeName}]");
                        sb.AppendLine($"  当前大小: {stat.CurrentPoolSize}/{stat.MaxCapacity}");
                        sb.AppendLine($"  峰值大小: {stat.PeakPoolSize}");
                        sb.AppendLine($"  总租用: {stat.RentCount}");
                        sb.AppendLine($"  创建数: {stat.CreateCount}");
                        sb.AppendLine($"  命中数: {stat.HitCount}");
                        sb.AppendLine($"  命中率: {stat.HitRate * 100f:F1}%");
                        sb.AppendLine();

                        totalRent += stat.RentCount;
                        totalCreate += stat.CreateCount;
                        totalHit += stat.HitCount;
                    }

                    sb.AppendLine("====== 汇总统计 ======");
                    sb.AppendLine($"总租用次数: {totalRent}");
                    sb.AppendLine($"总创建数: {totalCreate}");
                    sb.AppendLine($"总命中数: {totalHit}");
                    sb.AppendLine($"总命中率: {(totalRent > 0 ? (float)totalHit / totalRent * 100f : 0f):F1}%");

                    Debug.Log(sb.ToString());

                    // 恢复原始统计状态
                    service.StatisticsEnabled = wasCollecting;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PoolDiagnostics] 打印统计信息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 重置所有池的统计信息。
        /// </summary>
        [MenuItem("Tools/ObjectPool/Reset All Statistics")]
        public static void ResetAllStatistics()
        {
            try
            {
                var task = EasyPackArchitecture.GetObjectPoolServiceAsync();
                if (!task.IsCompleted)
                {
                    EditorUtility.DisplayDialog("错误", "对象池服务未就绪", "确定");
                    return;
                }

                var poolService = task.Result;
                poolService.ResetAllStatistics();
                Debug.Log("[PoolDiagnostics] 已重置所有池的统计信息");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PoolDiagnostics] 重置统计信息失败: {ex.Message}");
            }
        }
    }
}

#endif
