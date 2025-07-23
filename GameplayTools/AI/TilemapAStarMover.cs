using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;
namespace EasyPack
{
    [System.Serializable]
    public class PathfindingEvent : UnityEvent<List<Vector3Int>> { }

    [System.Serializable]
    public class MovementEvent : UnityEvent<Vector3> { }

    public class TilemapAStarMover : MonoBehaviour
    {
        [Header("Tilemap 设置")]
        [Tooltip("包含所有可用于寻路的Tilemap组件，系统将扫描这些Tilemap中的瓦片作为可行走区域")]
        public List<Tilemap> allTilemaps = new List<Tilemap>();

        [Header("寻路设置")]
        [Tooltip("执行寻路的物品对象如果为null，那么是否默认设置为自身")]
        public bool ispathFindingObjectSelf;
        [Tooltip("执行寻路的游戏对象，作为寻路的起点")]
        public GameObject pathfindingObject;

        [Tooltip("寻路的目标游戏对象，作为寻路的终点")]
        public GameObject targetObject;

        [Tooltip("是否启用瓦片偏移")]
        public bool useTileCenterOffset = true;

        [Tooltip("瓦片偏移量，(0.5, 0.5)表示瓦片中心，范围0-1")]
        public Vector3 tileOffset = new Vector3(0.5f, 0.5f);

        [Header("移动设置")]
        [Tooltip("角色移动速度，单位为Unity单位/秒")]
        public float moveSpeed = 3f;

        [Tooltip("是否允许对角线移动（8方向），禁用则只能上下左右移动（4方向）")]
        public bool allowDiagonalMovement = true;

        [Tooltip("移动速度曲线，可以控制移动过程中的加速度变化")]
        public AnimationCurve moveSpeedCurve = AnimationCurve.Linear(0, 1, 1, 1);

        [Tooltip("是否自动转向移动方向，启用后角色会朝向移动方向旋转")]
        public bool autoRotateToDirection = false;

        [Tooltip("转向速度，值越大转向越快")]
        public float rotationSpeed = 5f;

        [Header("高级寻路设置")]
        [Tooltip("障碍物检测的层级掩码，用于碰撞检测")]
        public LayerMask obstacleLayerMask = -1;

        [Tooltip("最大搜索距离，超过此距离的目标将被视为不可达")]
        public float maxSearchDistance = 100f;

        [Tooltip("寻路算法的最大迭代次数，防止无限循环")]
        public int maxIterations = 10000;

        [Tooltip("是否使用跳点搜索优化算法，可以提高大地图的寻路性能")]
        public bool useJumpPointSearch = false;

        [Tooltip("路径平滑算法类型，可以让路径更自然")]
        public PathSmoothingType pathSmoothing = PathSmoothingType.None;

        [Tooltip("路径平滑强度，值越大平滑效果越明显")]
        public float smoothingStrength = 0.5f;

        [Header("代价设置")]
        [Tooltip("直线移动的基础代价值")]
        public float straightMoveCost = 1f;

        [Tooltip("对角线移动的代价值，通常为√2≈1.414")]
        public float diagonalMoveCost = 1.414f;

        [Tooltip("瓦片类型与移动代价的映射表，不同瓦片可以有不同的通过代价")]
        public Dictionary<TileBase, float> tileCostMap = new Dictionary<TileBase, float>();

        [Tooltip("是否启用地形代价系统，考虑不同瓦片的移动成本")]
        public bool useTerrainCosts = false;

        [Header("动态障碍物")]
        [Tooltip("是否考虑动态障碍物，如移动的敌人或物体")]
        public bool considerDynamicObstacles = false;

        [Tooltip("动态障碍物的检测半径，在此范围内的位置将被视为不可通行")]
        public float obstacleCheckRadius = 0.3f;

        [Tooltip("动态障碍物列表，这些Transform位置周围将被视为障碍")]
        public List<Transform> dynamicObstacles = new List<Transform>();

        [Header("抖动设置")]
        [Tooltip("是否启用移动抖动效果，让移动看起来更自然(或许)")]
        public bool enableMovementJitter = false;

        [Tooltip("抖动强度，范围0-1，值越大抖动越明显")]
        public float jitterStrength = 0.1f;

        [Tooltip("抖动频率，每秒产生抖动的次数")]
        public float jitterFrequency = 2f;

        [Tooltip("防止抖动进入无效区域，确保抖动不会让角色移动到不可行走的瓦片")]
        public bool preventJitterIntoNull = true;

        [Header("自动刷新设置")]
        [Tooltip("是否启用自动刷新功能，在Update中定期重新计算寻路")]
        public bool enableAutoRefresh = false;

        [Tooltip("自动刷新的频率，每秒刷新的次数")]
        [Range(0.1f, 10f)]
        public float refreshFrequency = 1f;

        [Tooltip("是否在目标对象移动时触发刷新")]
        public bool refreshOnTargetMove = true;

        [Tooltip("目标移动的最小距离阈值，超过此距离才触发刷新")]
        public float targetMoveThreshold = 0.5f;

        [Header("平滑路径切换设置")]
        [Tooltip("是否启用平滑路径切换，即不会停止再开始")]
        public bool enableSmoothPathTransition = true;

        [Tooltip("路径切换时的最大回退距离，超过此距离将直接切换到新路径")]
        public float maxBacktrackDistance = 2f;

        [Header("事件")]
        [Tooltip("找到路径时触发的事件，参数为路径点列表")]
        public PathfindingEvent OnPathFound = new PathfindingEvent();

        [Tooltip("未找到路径时触发的事件")]
        public UnityEvent OnPathNotFound = new UnityEvent();

        [Tooltip("开始移动时触发的事件，参数为起始位置")]
        public MovementEvent OnMovementStart = new MovementEvent();

        [Tooltip("移动过程中每帧触发的事件，参数为当前位置")]
        public MovementEvent OnMovementUpdate = new MovementEvent();

        [Tooltip("移动完成时触发的事件，参数为最终位置")]
        public MovementEvent OnMovementComplete = new MovementEvent();

        [Tooltip("移动被停止时触发的事件")]
        public UnityEvent OnMovementStopped = new UnityEvent();

        [Header("调试设置")]
        [Tooltip("是否在Scene视图中显示寻路路径")]
        public bool showDebugPath = true;

        [Tooltip("是否显示所有可行走区域的轮廓")]
        public bool showWalkableArea = false;

        [Tooltip("是否显示抖动调试信息")]
        public bool showJitterDebug = false;

        [Tooltip("是否显示搜索区域范围")]
        public bool showSearchArea = false;

        [Tooltip("是否显示各瓦片的代价值")]
        public bool showCostValues = false;

        [Tooltip("路径线条的颜色")]
        public Color pathColor = Color.red;

        [Tooltip("起点标记的颜色")]
        public Color startPointColor = Color.green;

        [Tooltip("终点标记的颜色")]
        public Color targetPointColor = Color.blue;

        [Tooltip("可行走区域的显示颜色")]
        public Color walkableAreaColor = Color.yellow;

        [Tooltip("抖动调试信息的显示颜色")]
        public Color jitterDebugColor = Color.magenta;

        [Tooltip("搜索区域的显示颜色")]
        public Color searchAreaColor = Color.cyan;

        [Tooltip("障碍物的显示颜色")]
        public Color obstacleColor = Color.red;

        // 统一地图数据结构
        private UnifiedMap unifiedMap;
        private List<Vector3Int> currentPath = new List<Vector3Int>();
        private Coroutine moveCoroutine;

        // 移动状态追踪
        private int currentPathIndex = 0;
        private Vector3 currentTarget = Vector3.zero;
        public bool isMovingToTarget = false;

        // 抖动相关
        private Vector3 currentJitterOffset = Vector3.zero;
        private float jitterTimer = 0f;
        private List<Vector3> jitterHistory = new List<Vector3>(); // 用于调试显示

        // 性能统计
        private PathfindingStats lastStats = new PathfindingStats();

        // 方向数组
        private readonly Vector3Int[] directions8 = {
            Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right,
            new Vector3Int(1, 1, 0), new Vector3Int(-1, 1, 0),
            new Vector3Int(1, -1, 0), new Vector3Int(-1, -1, 0)
        };

        private readonly Vector3Int[] directions4 = {
            Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right
        };

        // 自动刷新相关
        private float refreshTimer = 0f;
        private Vector3 lastTargetPosition;
        private bool hasInitializedTargetPosition = false;

        private void Start()
        {
            // 如果未设置寻路对象，则默认使用自身
            if (ispathFindingObjectSelf && pathfindingObject == null)
            {
                pathfindingObject = gameObject;
            }
            BuildUnifiedMap();
            InitializeAutoRefresh();
        }

        private void Update()
        {
            if (!enableAutoRefresh) return;

            HandleAutoRefresh();
        }

        /// <summary>
        /// 处理自动刷新逻辑
        /// </summary>
        private void HandleAutoRefresh()
        {
            // 频率刷新
            refreshTimer += Time.deltaTime;
            bool shouldRefreshByFrequency = refreshTimer >= 1f / refreshFrequency;

            // 目标移动刷新
            bool shouldRefreshByTargetMove = false;
            if (refreshOnTargetMove && targetObject != null)
            {
                if (!hasInitializedTargetPosition)
                {
                    lastTargetPosition = targetObject.transform.position;
                    hasInitializedTargetPosition = true;
                }
                else
                {
                    float distance = Vector3.Distance(targetObject.transform.position, lastTargetPosition);
                    if (distance >= targetMoveThreshold)
                    {
                        shouldRefreshByTargetMove = true;
                        lastTargetPosition = targetObject.transform.position;
                    }
                }
            }

            // 执行刷新
            if (shouldRefreshByFrequency || shouldRefreshByTargetMove)
            {
                if (shouldRefreshByFrequency)
                {
                    refreshTimer = 0f;
                }

                AutoRefreshPathfinding();
            }
        }

        /// <summary>
        /// 自动刷新寻路
        /// </summary>
        private void AutoRefreshPathfinding()
        {
            // 只有在角色正在移动且目标有效时才刷新
            if (!IsMoving || pathfindingObject == null || targetObject == null)
                return;

            Vector3Int startPos;
            if (currentPath.Count > 0 && currentPathIndex < currentPath.Count)
            {
                // 如果正在移动到某个路径点，使用该路径点作为起点
                startPos = currentPath[currentPathIndex];
            }
            else
            {
                // 备用方案：使用当前瓦片位置
                startPos = GetTilePositionFromGameObject(pathfindingObject);
            }

            Vector3Int targetPos = GetTilePositionFromGameObject(targetObject);

            // 如果目标位置没有改变，则不需要刷新
            if (currentPath.Count > 0 && targetPos == currentPath[currentPath.Count - 1])
                return;

            var newPath = FindPath(startPos, targetPos);

            if (newPath.Count > 0)
            {
                // 只有当新路径与当前路径显著不同时才更新
                if (ShouldUpdatePath(newPath))
                {
                    // 使用平滑过渡而不是强制重置
                    if (enableSmoothPathTransition)
                    {
                        SmoothTransitionToNewPath(newPath);
                    }
                    else
                    {
                        MoveAlongPath(newPath);
                    }
                }
            }
        }

        /// <summary>
        /// 平滑过渡到新路径
        /// </summary>
        private void SmoothTransitionToNewPath(List<Vector3Int> newPath)
        {
            if (newPath == null || newPath.Count == 0) return;

            Vector3 currentPosition = pathfindingObject.transform.position;

            // 找到新路径中最接近当前位置的点
            int bestIndex = FindClosestPathIndex(newPath, currentPosition);

            // 如果找到合适的连接点
            if (bestIndex >= 0)
            {
                // 计算从当前位置到最佳连接点的距离
                Vector3 connectPoint = GetWorldPosition(newPath[bestIndex]);
                float backtrackDistance = Vector3.Distance(currentPosition, connectPoint);

                // 如果回退距离在可接受范围内，则从该点继续
                if (backtrackDistance <= maxBacktrackDistance)
                {
                    // 更新路径从连接点开始
                    currentPath = newPath;
                    currentPathIndex = bestIndex;
                    currentTarget = GetWorldPosition(newPath[currentPathIndex]);

                    // 不需要重启协程，当前协程会自动使用新的路径和索引
                    return;
                }
            }

            // 如果无法平滑过渡，则重启移动
            MoveAlongPath(newPath);
        }

        /// <summary>
        /// 在新路径中找到最接近当前位置的路径点索引
        /// </summary>
        private int FindClosestPathIndex(List<Vector3Int> path, Vector3 currentPosition)
        {
            if (path == null || path.Count == 0) return -1;

            float minDistance = float.MaxValue;
            int bestIndex = -1;

            // 从当前路径索引开始向前搜索，避免回退太远
            int searchStart = Mathf.Max(0, currentPathIndex - 2);
            int searchEnd = Mathf.Min(path.Count, currentPathIndex + 5);

            for (int i = searchStart; i < searchEnd; i++)
            {
                Vector3 pathPoint = GetWorldPosition(path[i]);
                float distance = Vector3.Distance(currentPosition, pathPoint);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    bestIndex = i;
                }
            }

            // 如果搜索范围内找不到合适点，则搜索整个路径
            if (bestIndex == -1)
            {
                for (int i = 0; i < path.Count; i++)
                {
                    Vector3 pathPoint = GetWorldPosition(path[i]);
                    float distance = Vector3.Distance(currentPosition, pathPoint);

                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        bestIndex = i;
                    }
                }
            }

            return bestIndex;
        }

        /// <summary>
        /// 判断是否应该更新路径
        /// </summary>
        private bool ShouldUpdatePath(List<Vector3Int> newPath)
        {
            if (currentPath.Count == 0) return true;
            if (newPath.Count == 0) return false;

            // 如果终点不同，需要更新
            if (currentPath[currentPath.Count - 1] != newPath[newPath.Count - 1])
                return true;

            // 如果路径长度差异很大，需要更新
            float lengthDifference = Mathf.Abs(newPath.Count - currentPath.Count) / (float)currentPath.Count;
            if (lengthDifference > 0.3f) // 30%的差异
                return true;

            // 检查路径的前几步是否有显著差异
            int checkSteps = Mathf.Min(3, Mathf.Min(currentPath.Count, newPath.Count));
            for (int i = 0; i < checkSteps; i++)
            {
                if (currentPath[i] != newPath[i])
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 初始化自动刷新
        /// </summary>
        private void InitializeAutoRefresh()
        {
            if (targetObject != null)
            {
                lastTargetPosition = targetObject.transform.position;
                hasInitializedTargetPosition = true;
            }
        }

        /// <summary>
        /// 构建统一地图
        /// </summary>
        [ContextMenu("构建统一地图")]
        public void BuildUnifiedMap()
        {
            if (allTilemaps == null || allTilemaps.Count == 0)
            {
                Debug.LogError("没有设置Tilemap！");
                return;
            }

            unifiedMap = new UnifiedMap();

            // 扫描所有Tilemap，收集所有可行走的瓦片位置
            foreach (var tilemap in allTilemaps)
            {
                if (tilemap == null) continue;
                ScanTilemap(tilemap);
            }
        }

        /// <summary>
        /// 扫描单个Tilemap，添加到统一地图
        /// </summary>
        private void ScanTilemap(Tilemap tilemap)
        {
            BoundsInt bounds = tilemap.cellBounds;
            int tileCount = 0;

            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    Vector3Int position = new Vector3Int(x, y, 0);
                    TileBase tile = tilemap.GetTile(position);

                    if (tile != null && IsTileWalkable(tile, position))
                    {
                        float cost = GetTileCost(tile);
                        unifiedMap.AddWalkableTile(position, tilemap, cost);
                        tileCount++;
                    }
                }
            }
        }

        /// <summary>
        /// 检查瓦片是否可行走（可以重写此方法来自定义逻辑）
        /// </summary>
        protected virtual bool IsTileWalkable(TileBase tile, Vector3Int position)
        {
            // 非空瓦片即可行走
            return tile != null;
        }

        /// <summary>
        /// 获取瓦片移动代价
        /// </summary>
        protected virtual float GetTileCost(TileBase tile)
        {
            if (useTerrainCosts && tileCostMap.ContainsKey(tile))
            {
                return tileCostMap[tile];
            }
            return 1f; // 默认代价
        }

        /// <summary>
        /// 检查动态障碍物
        /// </summary>
        private bool HasDynamicObstacle(Vector3Int position)
        {
            if (!considerDynamicObstacles) return false;

            Vector3 worldPos = GetWorldPosition(position);

            foreach (var obstacle in dynamicObstacles)
            {
                if (obstacle == null) continue;

                float distance = Vector3.Distance(worldPos, obstacle.position);
                if (distance <= obstacleCheckRadius)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 设置自动刷新启用状态
        /// </summary>
        public void SetAutoRefreshEnabled(bool enabled)
        {
            enableAutoRefresh = enabled;
            if (enabled)
            {
                refreshTimer = 0f;
                InitializeAutoRefresh();
            }
        }

        /// <summary>
        /// 设置刷新频率
        /// </summary>
        public void SetRefreshFrequency(float frequency)
        {
            refreshFrequency = Mathf.Clamp(frequency, 0.1f, 10f);
            refreshTimer = 0f; // 重置计时器
        }

        /// <summary>
        /// 设置目标移动刷新
        /// </summary>
        public void SetRefreshOnTargetMove(bool enabled)
        {
            refreshOnTargetMove = enabled;
            if (enabled && targetObject != null)
            {
                lastTargetPosition = targetObject.transform.position;
                hasInitializedTargetPosition = true;
            }
        }

        /// <summary>
        /// 设置目标移动阈值
        /// </summary>
        public void SetTargetMoveThreshold(float threshold)
        {
            targetMoveThreshold = Mathf.Max(0.1f, threshold);
        }

        /// <summary>
        /// 强制刷新寻路（忽略频率限制）
        /// </summary>
        [ContextMenu("强制刷新寻路")]
        public void ForceRefreshPathfinding()
        {
            refreshTimer = 0f;
            AutoRefreshPathfinding();
        }

        /// <summary>
        /// 主要寻路方法 - 支持多种寻路选项
        /// </summary>
        public List<Vector3Int> FindPath(Vector3Int startPos, Vector3Int targetPos, PathfindingOptions options = null)
        {
            if (unifiedMap == null || unifiedMap.walkableTiles.Count == 0)
            {
                Debug.LogError("统一地图未构建或为空！");
                OnPathNotFound?.Invoke();
                return new List<Vector3Int>();
            }

            // 使用默认选项
            if (options == null)
            {
                options = new PathfindingOptions
                {
                    allowDiagonal = allowDiagonalMovement,
                    maxDistance = maxSearchDistance,
                    maxIterations = maxIterations,
                    useJPS = useJumpPointSearch
                };
            }

            var startTime = System.DateTime.Now;
            lastStats.Reset();

            // 检查起点和终点
            if (!IsPositionValid(startPos) || HasDynamicObstacle(startPos))
            {
                Debug.LogError($"起点 {startPos} 不可行走！");
                OnPathNotFound?.Invoke();
                return new List<Vector3Int>();
            }

            if (!IsPositionValid(targetPos) || HasDynamicObstacle(targetPos))
            {
                Debug.LogError($"终点 {targetPos} 不可行走！");
                OnPathNotFound?.Invoke();
                return new List<Vector3Int>();
            }

            // 距离检查
            float distance = Vector3Int.Distance(startPos, targetPos);
            if (distance > options.maxDistance)
            {
                Debug.LogWarning($"目标距离 {distance} 超过最大搜索距离 {options.maxDistance}");
                OnPathNotFound?.Invoke();
                return new List<Vector3Int>();
            }

            if (startPos == targetPos)
            {
                var singlePath = new List<Vector3Int> { startPos };
                OnPathFound?.Invoke(singlePath);
                return singlePath;
            }

            // 执行寻路算法
            List<Vector3Int> path;
            if (options.useJPS && allowDiagonalMovement)
            {
                path = ExecuteJumpPointSearch(startPos, targetPos, options);
            }
            else
            {
                path = ExecuteAStar(startPos, targetPos, options);
            }

            // 路径后处理
            if (path.Count > 0)
            {
                path = PostProcessPath(path);

                // 记录统计信息
                var endTime = System.DateTime.Now;
                lastStats.searchTime = (float)(endTime - startTime).TotalMilliseconds;
                lastStats.pathLength = path.Count;
                lastStats.success = true;

                OnPathFound?.Invoke(path);
            }
            else
            {
                OnPathNotFound?.Invoke();
            }

            return path;
        }

        /// <summary>
        /// 检查位置是否有效
        /// </summary>
        private bool IsPositionValid(Vector3Int position)
        {
            return unifiedMap.IsWalkable(position);
        }

        /// <summary>
        /// 路径后处理（平滑等）
        /// </summary>
        private List<Vector3Int> PostProcessPath(List<Vector3Int> path)
        {
            if (path.Count <= 2) return path;

            switch (pathSmoothing)
            {
                case PathSmoothingType.LineOfSight:
                    return SmoothPathLineOfSight(path);
                case PathSmoothingType.Bezier:
                    return SmoothPathBezier(path);
                default:
                    return path;
            }
        }

        /// <summary>
        /// 视线平滑
        /// </summary>
        private List<Vector3Int> SmoothPathLineOfSight(List<Vector3Int> path)
        {
            if (path.Count <= 2) return path;

            var smoothed = new List<Vector3Int> { path[0] };
            int currentIndex = 0;

            while (currentIndex < path.Count - 1)
            {
                int farthestIndex = currentIndex + 1;

                // 找到最远的可直达点
                for (int i = currentIndex + 2; i < path.Count; i++)
                {
                    if (HasLineOfSight(path[currentIndex], path[i]))
                    {
                        farthestIndex = i;
                    }
                    else
                    {
                        break;
                    }
                }

                smoothed.Add(path[farthestIndex]);
                currentIndex = farthestIndex;
            }

            return smoothed;
        }

        /// <summary>
        /// 贝塞尔曲线平滑
        /// </summary>
        private List<Vector3Int> SmoothPathBezier(List<Vector3Int> path)
        {
            //TODO:
            return path;
        }

        /// <summary>
        /// 检查两点间是否有直线视野
        /// </summary>
        private bool HasLineOfSight(Vector3Int start, Vector3Int end)
        {
            var points = GetPointsOnLine(start, end);
            foreach (var point in points)
            {
                if (!IsPositionValid(point) || HasDynamicObstacle(point))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 获取直线上的所有点
        /// </summary>
        private List<Vector3Int> GetPointsOnLine(Vector3Int start, Vector3Int end)
        {
            var points = new List<Vector3Int>();

            int dx = Mathf.Abs(end.x - start.x);
            int dy = Mathf.Abs(end.y - start.y);
            int x = start.x;
            int y = start.y;
            int x_inc = (end.x > start.x) ? 1 : -1;
            int y_inc = (end.y > start.y) ? 1 : -1;
            int error = dx - dy;

            dx *= 2;
            dy *= 2;

            for (int n = 1 + dx + dy; n > 0; --n)
            {
                points.Add(new Vector3Int(x, y, 0));

                if (error > 0)
                {
                    x += x_inc;
                    error -= dy;
                }
                else
                {
                    y += y_inc;
                    error += dx;
                }
            }

            return points;
        }

        /// <summary>
        /// 跳点搜索算法（JPS优化）
        /// </summary>
        private List<Vector3Int> ExecuteJumpPointSearch(Vector3Int start, Vector3Int target, PathfindingOptions options)
        {
            // TODO:
            return ExecuteAStar(start, target, options);
        }

        /// <summary>
        /// 执行A*寻路算法
        /// </summary>
        private List<Vector3Int> ExecuteAStar(Vector3Int start, Vector3Int target, PathfindingOptions options)
        {
            var openSet = new List<PathNode>();
            var closedSet = new HashSet<Vector3Int>();
            var allNodes = new Dictionary<Vector3Int, PathNode>();

            var startNode = new PathNode(start, 0, GetHeuristic(start, target), null);
            openSet.Add(startNode);
            allNodes[start] = startNode;

            Vector3Int[] directions = options.allowDiagonal ? directions8 : directions4;
            int iterations = 0;

            while (openSet.Count > 0 && iterations < options.maxIterations)
            {
                iterations++;
                lastStats.nodesExplored++;

                // 找到F值最小的节点
                var currentNode = GetLowestFCostNode(openSet);
                openSet.Remove(currentNode);
                closedSet.Add(currentNode.position);

                // 到达目标
                if (currentNode.position == target)
                {
                    lastStats.iterations = iterations;
                    return ReconstructPath(currentNode);
                }

                // 检查相邻节点
                foreach (var direction in directions)
                {
                    Vector3Int neighborPos = currentNode.position + direction;

                    if (closedSet.Contains(neighborPos) ||
                        !IsPositionValid(neighborPos) ||
                        HasDynamicObstacle(neighborPos))
                        continue;

                    // 计算移动代价
                    float moveCost = GetMoveCost(direction, currentNode.position, neighborPos);
                    float newGCost = currentNode.gCost + moveCost;

                    if (!allNodes.TryGetValue(neighborPos, out PathNode neighborNode))
                    {
                        neighborNode = new PathNode(neighborPos, newGCost, GetHeuristic(neighborPos, target), currentNode);
                        allNodes[neighborPos] = neighborNode;
                        openSet.Add(neighborNode);
                    }
                    else if (newGCost < neighborNode.gCost)
                    {
                        neighborNode.gCost = newGCost;
                        neighborNode.parent = currentNode;

                        if (!openSet.Contains(neighborNode))
                            openSet.Add(neighborNode);
                    }
                }
            }

            lastStats.iterations = iterations;
            Debug.LogWarning($"寻路失败！迭代次数: {iterations}");
            return new List<Vector3Int>();
        }

        /// <summary>
        /// 获取移动代价（考虑地形和方向）
        /// </summary>
        private float GetMoveCost(Vector3Int direction, Vector3Int from, Vector3Int to)
        {
            float baseCost = IsDiagonalMove(direction) ? diagonalMoveCost : straightMoveCost;

            if (useTerrainCosts)
            {
                // 获取目标瓦片的地形代价
                var tileInfo = unifiedMap.GetTileInfo(to);
                if (tileInfo != null)
                {
                    baseCost *= tileInfo.cost;
                }
            }

            return baseCost;
        }

        /// <summary>
        /// 判断是否为对角线移动
        /// </summary>
        private bool IsDiagonalMove(Vector3Int direction)
        {
            return direction.x != 0 && direction.y != 0;
        }

        /// <summary>
        /// 获取启发式距离
        /// </summary>
        private float GetHeuristic(Vector3Int a, Vector3Int b)
        {
            if (allowDiagonalMovement)
            {
                // 欧几里得距离
                float dx = a.x - b.x;
                float dy = a.y - b.y;
                return Mathf.Sqrt(dx * dx + dy * dy);
            }
            else
            {
                // 曼哈顿距离
                return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
            }
        }

        /// <summary>
        /// 获取F值最小的节点
        /// </summary>
        private PathNode GetLowestFCostNode(List<PathNode> nodes)
        {
            PathNode lowest = nodes[0];
            for (int i = 1; i < nodes.Count; i++)
            {
                if (nodes[i].fCost < lowest.fCost ||
                    (nodes[i].fCost == lowest.fCost && nodes[i].hCost < lowest.hCost))
                {
                    lowest = nodes[i];
                }
            }
            return lowest;
        }

        /// <summary>
        /// 重构路径
        /// </summary>
        private List<Vector3Int> ReconstructPath(PathNode endNode)
        {
            var path = new List<Vector3Int>();
            var current = endNode;

            while (current != null)
            {
                path.Insert(0, current.position);
                current = current.parent;
            }

            return path;
        }

        /// <summary>
        /// 获取GameObject下方的瓦片位置
        /// </summary>
        private Vector3Int GetTilePositionFromGameObject(GameObject obj)
        {
            if (obj == null || allTilemaps.Count == 0) return Vector3Int.zero;

            Vector3 worldPos = obj.transform.position;
            Vector3Int cellPos = allTilemaps[0].WorldToCell(worldPos);
            return cellPos;
        }

        /// <summary>
        /// 开始寻路并移动
        /// </summary>
        [ContextMenu("开始寻路并移动")]
        public void StartPathfindingAndMove()
        {
            if (pathfindingObject == null)
            {
                Debug.LogError("寻路GameObject未设置！");
                return;
            }

            if (targetObject == null)
            {
                Debug.LogError("目标GameObject未设置！");
                return;
            }

            Vector3Int startPos = GetTilePositionFromGameObject(pathfindingObject);
            Vector3Int targetPos = GetTilePositionFromGameObject(targetObject);

            var path = FindPath(startPos, targetPos);

            if (path.Count > 0)
            {
                MoveAlongPath(path);
            }
        }

        /// <summary>
        /// 沿路径移动GameObject
        /// </summary>
        public void MoveAlongPath(List<Vector3Int> path)
        {
            if (pathfindingObject == null || path.Count == 0) return;

            // 停止之前的移动
            if (moveCoroutine != null)
            {
                StopCoroutine(moveCoroutine);
                OnMovementStopped?.Invoke();
            }

            currentPath = new List<Vector3Int>(path);
            currentPathIndex = 0;
            currentJitterOffset = Vector3.zero;
            jitterTimer = 0f;
            jitterHistory.Clear();
            isMovingToTarget = false;

            moveCoroutine = StartCoroutine(MoveCoroutine());
            OnMovementStart?.Invoke(pathfindingObject.transform.position);
        }

        /// <summary>
        /// 移动协程
        /// </summary>
        private System.Collections.IEnumerator MoveCoroutine()
        {
            while (currentPathIndex < currentPath.Count)
            {
                Vector3Int targetCell = currentPath[currentPathIndex];
                Vector3 targetWorld = GetWorldPosition(targetCell);
                currentTarget = targetWorld;
                isMovingToTarget = true;

                // 平滑移动到目标位置
                while (Vector3.Distance(pathfindingObject.transform.position, targetWorld + currentJitterOffset) > 0.05f)
                {
                    // 检查路径是否被更新（平滑过渡的情况）
                    if (currentPathIndex < currentPath.Count)
                    {
                        targetCell = currentPath[currentPathIndex];
                        targetWorld = GetWorldPosition(targetCell);
                        currentTarget = targetWorld;
                    }

                    // 计算移动速度
                    float currentSpeed = moveSpeed;
                    if (moveSpeedCurve.keys.Length > 1)
                    {
                        Vector3 startPos = currentPathIndex > 0 ? GetWorldPosition(currentPath[currentPathIndex - 1]) : pathfindingObject.transform.position;
                        float pathProgress = 1f - (Vector3.Distance(pathfindingObject.transform.position, targetWorld) / Vector3.Distance(startPos, targetWorld));
                        pathProgress = Mathf.Clamp01(pathProgress);
                        float speedMultiplier = moveSpeedCurve.Evaluate(pathProgress);
                        currentSpeed = moveSpeed * speedMultiplier;
                    }

                    // 更新抖动
                    if (enableMovementJitter)
                    {
                        jitterTimer += Time.deltaTime;
                        if (jitterTimer >= 1f / jitterFrequency)
                        {
                            jitterTimer = 0f;
                            currentJitterOffset = GenerateJitterOffset(pathfindingObject.transform.position);

                            if (showJitterDebug && jitterHistory.Count < 50)
                            {
                                jitterHistory.Add(pathfindingObject.transform.position + currentJitterOffset);
                            }
                        }
                    }

                    // 移动到目标位置
                    Vector3 jitteredTarget = targetWorld + currentJitterOffset;
                    Vector3 newPosition = Vector3.MoveTowards(
                        pathfindingObject.transform.position,
                        jitteredTarget,
                        currentSpeed * Time.deltaTime
                    );

                    // 旋转到移动方向
                    if (autoRotateToDirection)
                    {
                        Vector3 direction = (jitteredTarget - pathfindingObject.transform.position).normalized;
                        if (direction.magnitude > 0.1f)
                        {
                            Quaternion targetRotation = Quaternion.LookRotation(Vector3.forward, direction);
                            pathfindingObject.transform.rotation = Quaternion.Lerp(
                                pathfindingObject.transform.rotation,
                                targetRotation,
                                rotationSpeed * Time.deltaTime
                            );
                        }
                    }

                    pathfindingObject.transform.position = newPosition;
                    OnMovementUpdate?.Invoke(newPosition);

                    yield return null;
                }

                isMovingToTarget = false;
                currentPathIndex++;

                if (currentPathIndex < currentPath.Count)
                {
                    currentJitterOffset = GenerateJitterOffset(pathfindingObject.transform.position);
                }
            }

            // 移动完成处理
            Vector3 finalTarget = GetWorldPosition(currentPath[currentPath.Count - 1]);

            // 平滑到最终位置
            while (Vector3.Distance(pathfindingObject.transform.position, finalTarget) > 0.01f)
            {
                currentJitterOffset = Vector3.Lerp(currentJitterOffset, Vector3.zero, Time.deltaTime * 3f);

                pathfindingObject.transform.position = Vector3.MoveTowards(
                    pathfindingObject.transform.position,
                    finalTarget,
                    moveSpeed * Time.deltaTime
                );
                yield return null;
            }

            pathfindingObject.transform.position = finalTarget;
            currentJitterOffset = Vector3.zero;
            isMovingToTarget = false;

            OnMovementComplete?.Invoke(finalTarget);
            moveCoroutine = null;
        }

        /// <summary>
        /// 生成抖动偏移
        /// </summary>
        private Vector3 GenerateJitterOffset(Vector3 currentWorldPos)
        {
            if (!enableMovementJitter || jitterStrength <= 0f)
                return Vector3.zero;

            Vector3 randomOffset = new Vector3(
                UnityEngine.Random.Range(-1f, 1f),
                UnityEngine.Random.Range(-1f, 1f),
                0f
            ).normalized * (jitterStrength * UnityEngine.Random.Range(0.5f, 1f));

            if (preventJitterIntoNull)
            {
                Vector3 targetWorldPos = currentWorldPos + randomOffset;
                Vector3Int targetTilePos = allTilemaps[0].WorldToCell(targetWorldPos);

                if (!IsPositionValid(targetTilePos))
                {
                    Vector3[] fallbackDirections = {
                    Vector3.right, Vector3.left, Vector3.up, Vector3.down,
                    new Vector3(1, 1, 0).normalized, new Vector3(-1, 1, 0).normalized,
                    new Vector3(1, -1, 0).normalized, new Vector3(-1, -1, 0).normalized
                };

                    foreach (var direction in fallbackDirections)
                    {
                        Vector3 fallbackOffset = direction * (jitterStrength * 0.5f);
                        Vector3 fallbackWorldPos = currentWorldPos + fallbackOffset;
                        Vector3Int fallbackTilePos = allTilemaps[0].WorldToCell(fallbackWorldPos);

                        if (IsPositionValid(fallbackTilePos))
                        {
                            return fallbackOffset;
                        }
                    }

                    return Vector3.zero;
                }
            }

            return randomOffset;
        }

        /// <summary>
        /// 将瓦片坐标转换为世界坐标
        /// </summary>
        private Vector3 GetWorldPosition(Vector3Int cellPosition)
        {
            if (allTilemaps.Count == 0) return Vector3.zero;

            Vector3 worldPos = allTilemaps[0].CellToWorld(cellPosition);

            if (useTileCenterOffset)
            {
                Vector3 cellSize = allTilemaps[0].cellSize;
                Vector3 actualOffset = new Vector3(
                    tileOffset.x * cellSize.x,
                    tileOffset.y * cellSize.y,
                    tileOffset.z * cellSize.z
                );
                worldPos += actualOffset;
            }

            return worldPos;
        }

        /// <summary>
        /// 停止移动
        /// </summary>
        [ContextMenu("停止移动")]
        public void StopMovement()
        {
            if (moveCoroutine != null)
            {
                StopCoroutine(moveCoroutine);
                moveCoroutine = null;
                OnMovementStopped?.Invoke();
            }
            currentPath.Clear();
            currentPathIndex = 0;
            currentJitterOffset = Vector3.zero;
            jitterHistory.Clear();
            isMovingToTarget = false;
        }

        /// <summary>
        /// 添加动态障碍物
        /// </summary>
        public void AddDynamicObstacle(Transform obstacle)
        {
            if (!dynamicObstacles.Contains(obstacle))
            {
                dynamicObstacles.Add(obstacle);
            }
        }

        /// <summary>
        /// 移除动态障碍物
        /// </summary>
        public void RemoveDynamicObstacle(Transform obstacle)
        {
            dynamicObstacles.Remove(obstacle);
        }

        /// <summary>
        /// 设置瓦片代价
        /// </summary>
        public void SetTileCost(TileBase tile, float cost)
        {
            tileCostMap[tile] = cost;
        }

        /// <summary>
        /// 获取最后一次寻路的统计信息
        /// </summary>
        public PathfindingStats GetLastPathfindingStats()
        {
            return lastStats;
        }

        // 调试绘制
        private void OnDrawGizmos()
        {
            if (unifiedMap == null) return;

            // 绘制可行走区域
            if (showWalkableArea)
            {
                Gizmos.color = walkableAreaColor;
                foreach (var tile in unifiedMap.walkableTiles.Keys)
                {
                    Vector3 worldPos = GetWorldPosition(tile);
                    Gizmos.DrawWireCube(worldPos, Vector3.one * 0.3f);
                }
            }

            // 绘制路径
            if (showDebugPath && currentPath != null && currentPath.Count > 0)
            {
                Gizmos.color = pathColor;
                for (int i = 0; i < currentPath.Count - 1; i++)
                {
                    Vector3 from = GetWorldPosition(currentPath[i]);
                    Vector3 to = GetWorldPosition(currentPath[i + 1]);
                    Gizmos.DrawLine(from, to);
                    Gizmos.DrawWireSphere(from, 0.15f);
                }

                // 高亮当前目标点
                if (currentPathIndex < currentPath.Count)
                {
                    Gizmos.color = Color.yellow;
                    Vector3 currentTargetPos = GetWorldPosition(currentPath[currentPathIndex]);
                    Gizmos.DrawWireSphere(currentTargetPos, 0.2f);
                }
            }

            // 绘制动态障碍物
            if (considerDynamicObstacles)
            {
                Gizmos.color = obstacleColor;
                foreach (var obstacle in dynamicObstacles)
                {
                    if (obstacle != null)
                    {
                        Gizmos.DrawWireSphere(obstacle.position, obstacleCheckRadius);
                    }
                }
            }

            // 绘制抖动调试信息
            if (showJitterDebug && enableMovementJitter)
            {
                Gizmos.color = jitterDebugColor;
                if (pathfindingObject != null && currentJitterOffset != Vector3.zero)
                {
                    Vector3 currentPos = pathfindingObject.transform.position;
                    Vector3 jitteredPos = currentPos + currentJitterOffset;
                    Gizmos.DrawLine(currentPos, jitteredPos);
                    Gizmos.DrawWireSphere(jitteredPos, 0.1f);
                }
            }

            // 绘制起点和终点
            if (pathfindingObject != null)
            {
                Vector3Int startPos = GetTilePositionFromGameObject(pathfindingObject);
                Vector3 startWorld = GetWorldPosition(startPos);
                Gizmos.color = startPointColor;
                Gizmos.DrawWireCube(startWorld, Vector3.one * 0.6f);
            }

            if (targetObject != null)
            {
                Vector3Int targetPos = GetTilePositionFromGameObject(targetObject);
                Vector3 targetWorld = GetWorldPosition(targetPos);
                Gizmos.color = targetPointColor;
                Gizmos.DrawWireCube(targetWorld, Vector3.one * 0.6f);
            }
        }

        // 公共接口
        public void SetTargetObject(GameObject target) => targetObject = target;
        public void SetPathfindingObject(GameObject pathfinder) => pathfindingObject = pathfinder;
        public bool IsMoving => moveCoroutine != null;
        public int GetWalkableTileCount() => unifiedMap?.walkableTiles.Count ?? 0;
        public void SetJitterEnabled(bool enabled) => enableMovementJitter = enabled;
        public void SetJitterStrength(float strength) => jitterStrength = Mathf.Clamp01(strength);
        public void SetJitterFrequency(float frequency) => jitterFrequency = Mathf.Max(0.1f, frequency);

        // 平滑路径过渡设置
        public void SetSmoothPathTransition(bool enabled) => enableSmoothPathTransition = enabled;
        public void SetMaxBacktrackDistance(float distance) => maxBacktrackDistance = Mathf.Max(0.1f, distance);
    }

    /// <summary>
    /// 路径平滑类型
    /// </summary>
    public enum PathSmoothingType
    {
        None,
        LineOfSight,
        Bezier
    }

    /// <summary>
    /// 寻路选项
    /// </summary>
    [System.Serializable]
    public class PathfindingOptions
    {
        public bool allowDiagonal = true;
        public float maxDistance = 100f;
        public int maxIterations = 10000;
        public bool useJPS = false;
        public bool considerTerrain = false;
    }

    /// <summary>
    /// 寻路统计信息
    /// </summary>
    [System.Serializable]
    public class PathfindingStats
    {
        public bool success = false;
        public float searchTime = 0f; // 毫秒
        public int iterations = 0;
        public int nodesExplored = 0;
        public int pathLength = 0;

        public void Reset()
        {
            success = false;
            searchTime = 0f;
            iterations = 0;
            nodesExplored = 0;
            pathLength = 0;
        }
    }

    /// <summary>
    /// 统一地图数据结构
    /// </summary>
    public class UnifiedMap
    {
        public Dictionary<Vector3Int, TileInfo> walkableTiles = new Dictionary<Vector3Int, TileInfo>();
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

        public PathNode(Vector3Int pos, float g, float h, PathNode parentNode)
        {
            position = pos;
            gCost = g;
            hCost = h;
            parent = parentNode;
        }
    }
}