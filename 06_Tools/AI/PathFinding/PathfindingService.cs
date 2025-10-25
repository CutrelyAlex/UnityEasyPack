using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace EasyPack
{
    /// <summary>
    /// 统一地图共享服务
    /// </summary>
    public class PathfindingService : MonoBehaviour
    {
        public static PathfindingService Instance { get; private set; }

        [Tooltip("（可选）在场景中直接指定全局 Tilemap 列表；未指定时由各 Mover 首次注册提供")]
        public List<Tilemap> globalTilemaps = new();

        [Tooltip("（可选）Grid 列表，用于自动收集其下所有 Tilemap")]
        public List<Grid> globalGrids = new();

        private UnifiedMap _unifiedMap;
        private bool _built;
        // 在类内部其他成员下方新增（便于 Mover 获取主 Tilemap）
        public Tilemap PrimaryTilemap => globalTilemaps.Count > 0 ? globalTilemaps[0] : null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (!_built && (globalTilemaps.Count > 0 || globalGrids.Count > 0))
            {
                CollectFromGridsIfAny(globalGrids, globalTilemaps);
                BuildUnifiedMapInternal(globalTilemaps);
            }
        }

        /// <summary>
        /// 由外部（Mover）注册其 Tilemap / Grid。仅在尚未构建时接收。
        /// </summary>
        public void RegisterTilemaps(List<Tilemap> fromTilemaps, List<Grid> fromGrids)
        {
            if (_built) return;

            if (fromGrids != null && fromGrids.Count > 0)
            {
                CollectFromGridsIfAny(fromGrids, globalTilemaps);
            }

            if (fromTilemaps != null)
            {
                foreach (var tm in fromTilemaps)
                {
                    if (tm != null && !globalTilemaps.Contains(tm))
                        globalTilemaps.Add(tm);
                }
            }

            if (globalTilemaps.Count > 0)
            {
                BuildUnifiedMapInternal(globalTilemaps);
            }
        }

        public UnifiedMap GetUnifiedMap() => _unifiedMap;

        /// <summary>
        /// 强制重建
        /// </summary>
        public void Rebuild()
        {
            _built = false;
            _unifiedMap = null;
            if (globalTilemaps.Count == 0)
            {
                CollectFromGridsIfAny(globalGrids, globalTilemaps);
            }
            if (globalTilemaps.Count > 0)
            {
                BuildUnifiedMapInternal(globalTilemaps);
            }
            else
            {
                Debug.LogWarning("[PathfindingService] 没有可用 Tilemap 重建。");
            }
        }

        private void CollectFromGridsIfAny(List<Grid> grids, List<Tilemap> outList)
        {
            if (grids == null) return;
            foreach (var g in grids)
            {
                if (g == null) continue;
                var tms = g.GetComponentsInChildren<Tilemap>();
                foreach (var tm in tms)
                {
                    if (tm != null && !outList.Contains(tm))
                        outList.Add(tm);
                }
            }
        }

        private void BuildUnifiedMapInternal(List<Tilemap> maps)
        {
            if (maps == null || maps.Count == 0)
            {
                Debug.LogError("[PathfindingService] 没有 Tilemap 可构建统一地图。");
                return;
            }

            _unifiedMap = new UnifiedMap();
            int added = 0;

            foreach (var tilemap in maps)
            {
                if (tilemap == null) continue;
                BoundsInt bounds = tilemap.cellBounds;
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    for (int y = bounds.yMin; y < bounds.yMax; y++)
                    {
                        var pos = new Vector3Int(x, y, 0);
                        var tile = tilemap.GetTile(pos);
                        if (tile != null)
                        {
                            _unifiedMap.AddWalkableTile(pos, tilemap, 1f);
                            added++;
                        }
                    }
                }
            }

            _built = true;
            Debug.Log($"[PathfindingService] UnifiedMap 构建完成，Walkable Tiles: {added}");
        }
    }
}