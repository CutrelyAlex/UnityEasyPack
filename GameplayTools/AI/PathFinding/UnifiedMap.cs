using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace EasyPack
{
    /// <summary>
    /// 统一地图数据结构
    /// </summary>
    public class UnifiedMap
    {
        public Dictionary<Vector3Int, TileInfo> walkableTiles = new();
        public BoundsInt bounds;

        public void AddWalkableTile(Vector3Int position, Tilemap tilemap, float cost = 1f)
        {
            var tileInfo = new TileInfo
            {
                tilemap = tilemap,
                cost = cost,
                position = position
            };

            walkableTiles[position] = tileInfo;
            UpdateBounds(position);
        }

        public bool IsWalkable(Vector3Int position)
        {
            return walkableTiles.ContainsKey(position);
        }

        public TileInfo GetTileInfo(Vector3Int position)
        {
            return walkableTiles.TryGetValue(position, out TileInfo info) ? info : null;
        }

        private void UpdateBounds(Vector3Int position)
        {
            if (walkableTiles.Count == 1)
            {
                bounds = new BoundsInt(position.x, position.y, 0, 1, 1, 1);
            }
            else
            {
                int minX = Mathf.Min(bounds.xMin, position.x);
                int minY = Mathf.Min(bounds.yMin, position.y);
                int maxX = Mathf.Max(bounds.xMax, position.x + 1);
                int maxY = Mathf.Max(bounds.yMax, position.y + 1);

                bounds = new BoundsInt(minX, minY, 0, maxX - minX, maxY - minY, 1);
            }
        }
    }

    /// <summary>
    /// 瓦片信息
    /// </summary>
    [System.Serializable]
    public class TileInfo
    {
        public Tilemap tilemap;
        public float cost = 1f;
        public Vector3Int position;
        public TileBase tileBase;
    }

    /// <summary>
    /// 路径节点
    /// </summary>
    public class PathNode
    {
        public Vector3Int position;
        public float gCost;
        public float hCost;
        public float fCost => gCost + hCost;
        public PathNode parent;

        public int heapIndex = -1;

        public PathNode(Vector3Int pos, float g, float h, PathNode parentNode)
        {
            position = pos;
            gCost = g;
            hCost = h;
            parent = parentNode;
        }
    }
}