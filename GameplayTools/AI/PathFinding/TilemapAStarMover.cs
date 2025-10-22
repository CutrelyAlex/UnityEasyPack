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
        #region 序列化字段和属性

        [Header("共享服务")]
        [Tooltip("是否使用 PathfindingService 共享统一地图")]
        public bool useSharedService = true;

        [Tooltip("显式引用服务；为空则自动查找")]
        public PathfindingService sharedService;

        [Header("Tilemap 设置（若使用共享服务则无需设置）")]
        [Tooltip("包含所有可用于寻路的Tilemap组件，系统将扫描这些Tilemap中的瓦片作为可行走区域")]
        public List<Tilemap> allTilemaps = new();

        [Tooltip("Grid GameObject，如果设置了Grid，将自动获取其下所有Tilemap组件")]
        public List<Grid> gridObjects = new();

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
        public Vector3 tileOffset = new(0.5f, 0.5f);

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

        [Header("贝塞尔平滑设置")]
        [Tooltip("贝塞尔平滑：基础采样密度（每世界单位估算的采样倍率）")]
        public float bezierBaseDensity = 1.2f;

        private int bezierMinSamples = 1;

        [Tooltip("贝塞尔平滑：每段最多采样点数限制")]
        [Range(4, 200)] public int bezierMaxSamples = 40;

        [Header("代价设置")]
        [Tooltip("直线移动的基础代价值")]
        public float straightMoveCost = 1f;

        [Tooltip("对角线移动的代价值，通常为√2≈1.414")]
        public float diagonalMoveCost = 1.414f;

        [Tooltip("瓦片类型与移动代价的映射表，不同瓦片可以有不同的通过代价")]
        public Dictionary<TileBase, float> tileCostMap = new();

        [Tooltip("是否启用地形代价系统，考虑不同瓦片的移动成本")]
        public bool useTerrainCosts = false;

        [Header("动态障碍物")]
        [Tooltip("是否考虑动态障碍物，如移动的敌人或物体")]
        public bool considerDynamicObstacles = false;

        [Tooltip("动态障碍物的检测半径，在此范围内的位置将被视为不可通行")]
        public float obstacleCheckRadius = 0.3f;

        [Tooltip("动态障碍物列表，这些Transform位置周围将被视为障碍")]
        public List<Transform> dynamicObstacles = new();

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
        [Range(0.1f, 10f)] public float refreshFrequency = 1f;

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
        public PathfindingEvent OnPathFound = new();

        [Tooltip("未找到路径时触发的事件")]
        public UnityEvent OnPathNotFound = new();

        [Tooltip("开始移动时触发的事件，参数为起始位置")]
        public MovementEvent OnMovementStart = new();

        [Tooltip("移动过程中每帧触发的事件，参数为当前位置")]
        public MovementEvent OnMovementUpdate = new();

        [Tooltip("移动完成时触发的事件，参数为最终位置")]
        public MovementEvent OnMovementComplete = new();

        [Tooltip("移动被停止时触发的事件")]
        public UnityEvent OnMovementStopped = new();

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

        #endregion

        #region 私有字段
        private UnifiedMap unifiedMap;           // 可能来自共享服务
        private bool usingSharedMap = false;     // 标记是否使用共享地图
        public bool localBuilt = false;         // 若未能获取服务则本地构建

        private List<Vector3Int> currentPath = new();
        private Coroutine moveCoroutine;
        private int currentPathIndex = 0;
        private Vector3 currentTarget = Vector3.zero;
        public bool isMovingToTarget = false;
        private Vector3 currentJitterOffset = Vector3.zero;
        private float jitterTimer = 0f;
        private List<Vector3> jitterHistory = new();
        private PathfindingStats lastStats = new();

        private readonly Vector3Int[] directions8 = {
            Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right,
            new(1,1,0), new(-1,1,0),
            new(1,-1,0), new(-1,-1,0)
        };
        private readonly Vector3Int[] directions4 = {
            Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right
        };

        private Tilemap conversionTilemap;

        private float refreshTimer = 0f;
        private Vector3 lastTargetPosition;
        private bool hasInitializedTargetPosition = false;
        #endregion

        #region Unity生命周期
        private void Start()
        {
            if (ispathFindingObjectSelf && pathfindingObject == null)
                pathfindingObject = gameObject;

            // 1. 尝试使用共享服务
            if (useSharedService)
            {
                if (sharedService == null)
                    sharedService = PathfindingService.Instance ?? FindFirstObjectByType<PathfindingService>();
                if (sharedService != null)
                {
                    // 若自己有配置 Grid，先收集再注册
                    if (gridObjects.Count > 0)
                        GetTilemapsFromGrids();

                    if (allTilemaps.Count > 0 || gridObjects.Count > 0)
                        sharedService.RegisterTilemaps(allTilemaps, gridObjects);

                    unifiedMap = sharedService.GetUnifiedMap();
                    conversionTilemap ??= (allTilemaps.Count > 0 ? allTilemaps[0] : sharedService?.PrimaryTilemap);
                    if (usingSharedMap && allTilemaps.Count == 0 && conversionTilemap != null)
                    {
                        // 确保本地也持有一个引用，避免 allTilemaps.Count==0 造成坐标恒为 (0,0,0)
                        allTilemaps.Add(conversionTilemap);
                    }
                    if (unifiedMap != null)
                    {
                        usingSharedMap = true;
                    }
                    else
                    {
                        Debug.LogWarning($"[{name}] 共享服务存在但尚未提供 UnifiedMap，回退到本地构建。");
                    }
                }
                else
                {
                    Debug.LogWarning($"[{name}] 未找到 PathfindingService，回退本地构建。");
                }
            }

            // 2. 本地构建（共享失败或未启用）
            if (!usingSharedMap)
            {
                if (gridObjects.Count > 0)
                    GetTilemapsFromGrids();
                BuildUnifiedMap();
                localBuilt = true;
                if (conversionTilemap == null && allTilemaps.Count > 0)
                    conversionTilemap = allTilemaps[0];
            }

            InitializeAutoRefresh();
        }

        private void Update()
        {
            if (!enableAutoRefresh) return;
            HandleAutoRefresh();
        }
        #endregion

        #region 公共API
        [ContextMenu("从所有Grid获取Tilemap")]
        public void GetTilemapsFromGrids()
        {
            if (gridObjects.Count == 0) return;
            foreach (var grid in gridObjects)
            {
                if (grid == null) continue;
                var tilemaps = grid.GetComponentsInChildren<Tilemap>();
                foreach (var tm in tilemaps)
                {
                    if (tm != null && !allTilemaps.Contains(tm))
                        allTilemaps.Add(tm);
                }
            }
        }

        [ContextMenu("构建统一地图(本地)")]
        public void BuildUnifiedMap()
        {
            if (allTilemaps == null || allTilemaps.Count == 0)
            {
                Debug.LogError($"[{name}] 没有设置 Tilemap，无法本地构建。");
                return;
            }
            unifiedMap = new UnifiedMap();
            foreach (var tilemap in allTilemaps)
            {
                if (tilemap == null) continue;
                ScanTilemap(tilemap);
            }
        }

        public List<Vector3Int> FindPath(Vector3Int startPos, Vector3Int targetPos, PathfindingOptions options = null)
        {
            if (unifiedMap == null || unifiedMap.walkableTiles.Count == 0)
            {
                Debug.LogError($"[{name}] UnifiedMap 未构建或为空。");
                OnPathNotFound?.Invoke();
                return new List<Vector3Int>();
            }

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

            if (!IsPositionValid(startPos) || HasDynamicObstacle(startPos))
            {
                OnPathNotFound?.Invoke();
                return new List<Vector3Int>();
            }
            if (!IsPositionValid(targetPos) || HasDynamicObstacle(targetPos))
            {
                OnPathNotFound?.Invoke();
                return new List<Vector3Int>();
            }

            float dist = Vector3Int.Distance(startPos, targetPos);
            if (dist > options.maxDistance)
            {
                OnPathNotFound?.Invoke();
                return new List<Vector3Int>();
            }

            if (startPos == targetPos)
            {
                var single = new List<Vector3Int> { startPos };
                OnPathFound?.Invoke(single);
                return single;
            }

            List<Vector3Int> path = (options.useJPS && allowDiagonalMovement)
                ? ExecuteJumpPointSearch(startPos, targetPos, options)
                : ExecuteAStar(startPos, targetPos, options);

            if (path.Count > 0)
            {
                path = PostProcessPath(path);
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

        [ContextMenu("开始寻路并移动")]
        public void StartPathfindingAndMove()
        {
            if (pathfindingObject == null || targetObject == null)
            {
                Debug.LogError($"[{name}] 起点或终点对象未设置。");
                return;
            }
            var startPos = GetTilePositionFromGameObject(pathfindingObject);
            var targetPos = GetTilePositionFromGameObject(targetObject);
            var path = FindPath(startPos, targetPos);
            if (path.Count > 0)
                MoveAlongPath(path);
        }

        public void MoveAlongPath(List<Vector3Int> path)
        {
            if (pathfindingObject == null || path.Count == 0) return;
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

        public void AddDynamicObstacle(Transform obstacle)
        {
            if (!dynamicObstacles.Contains(obstacle))
                dynamicObstacles.Add(obstacle);
        }
        public void RemoveDynamicObstacle(Transform obstacle) => dynamicObstacles.Remove(obstacle);
        public void SetTileCost(TileBase tile, float cost) => tileCostMap[tile] = cost;
        public PathfindingStats GetLastPathfindingStats() => lastStats;

        /// <summary>
        /// 刷新引用
        /// 如果在运行中后加入服务可以使用
        /// </summary>
        public void TryAttachSharedService(PathfindingService service = null)
        {
            if (useSharedService == false) return;
            if (service != null) sharedService = service;
            if (sharedService == null)
                sharedService = PathfindingService.Instance ?? FindFirstObjectByType<PathfindingService>();
            if (sharedService == null) return;
            sharedService.RegisterTilemaps(allTilemaps, gridObjects);
            var map = sharedService.GetUnifiedMap();
            if (usingSharedMap)
            {
                if (conversionTilemap == null)
                    conversionTilemap = (allTilemaps.Count > 0 ? allTilemaps[0] : sharedService?.PrimaryTilemap);
                if (allTilemaps.Count == 0 && conversionTilemap != null)
                    allTilemaps.Add(conversionTilemap);
            }

            if (map != null)
            {
                unifiedMap = map;
                usingSharedMap = true;
            }
        }
        #endregion

        #region 自动刷新
        private void HandleAutoRefresh()
        {
            refreshTimer += Time.deltaTime;
            bool freq = refreshTimer >= 1f / refreshFrequency;
            bool byMove = false;
            if (refreshOnTargetMove && targetObject != null)
            {
                if (!hasInitializedTargetPosition)
                {
                    lastTargetPosition = targetObject.transform.position;
                    hasInitializedTargetPosition = true;
                }
                else if (Vector3.Distance(targetObject.transform.position, lastTargetPosition) >= targetMoveThreshold)
                {
                    byMove = true;
                    lastTargetPosition = targetObject.transform.position;
                }
            }
            if (freq || byMove)
            {
                if (freq) refreshTimer = 0f;
                AutoRefreshPathfinding();
            }
        }

        private void AutoRefreshPathfinding()
        {
            if (!IsMoving || pathfindingObject == null || targetObject == null) return;

            Vector3Int startPos = (currentPath.Count > 0 && currentPathIndex < currentPath.Count)
                ? currentPath[currentPathIndex]
                : GetTilePositionFromGameObject(pathfindingObject);

            Vector3Int targetPos = GetTilePositionFromGameObject(targetObject);
            if (currentPath.Count > 0 && targetPos == currentPath[currentPath.Count - 1])
                return;

            var newPath = FindPath(startPos, targetPos);
            if (newPath.Count > 0 && ShouldUpdatePath(newPath))
            {
                if (enableSmoothPathTransition)
                    SmoothTransitionToNewPath(newPath);
                else
                    MoveAlongPath(newPath);
            }
        }

        private void InitializeAutoRefresh()
        {
            if (targetObject != null)
            {
                lastTargetPosition = targetObject.transform.position;
                hasInitializedTargetPosition = true;
            }
        }
        public void SetAutoRefreshEnabled(bool enabled)
        {
            enableAutoRefresh = enabled;
            if (enabled)
            {
                refreshTimer = 0f;
                InitializeAutoRefresh();
            }
        }
        #endregion

        #region 地图相关
        private void ScanTilemap(Tilemap tilemap)
        {
            BoundsInt bounds = tilemap.cellBounds;
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    var pos = new Vector3Int(x, y, 0);
                    TileBase tile = tilemap.GetTile(pos);
                    if (tile != null && IsTileWalkable(tile, pos))
                    {
                        float cost = GetTileCost(tile);
                        unifiedMap.AddWalkableTile(pos, tilemap, cost);
                    }
                }
            }
        }

        protected virtual bool IsTileWalkable(TileBase tile, Vector3Int position) => tile != null;
        protected virtual float GetTileCost(TileBase tile)
        {
            if (useTerrainCosts && tileCostMap.ContainsKey(tile))
                return tileCostMap[tile];
            return 1f;
        }
        #endregion

        #region 寻路算法
        private bool IsPositionValid(Vector3Int position) => unifiedMap.IsWalkable(position);
        private bool HasDynamicObstacle(Vector3Int position)
        {
            if (!considerDynamicObstacles) return false;
            Vector3 worldPos = GetWorldPosition(position);
            foreach (var o in dynamicObstacles)
            {
                if (o == null) continue;
                if (Vector3.Distance(worldPos, o.position) <= obstacleCheckRadius)
                    return true;
            }
            return false;
        }

        private List<Vector3Int> ExecuteAStar(Vector3Int start, Vector3Int target, PathfindingOptions options)
        {
            var openSet = new PathNodeMinHeap();
            var closedSet = new HashSet<Vector3Int>();
            var allNodes = new Dictionary<Vector3Int, PathNode>();

            var startNode = new PathNode(start, 0, GetHeuristic(start, target), null);
            openSet.Add(startNode);
            allNodes[start] = startNode;

            Vector3Int[] dirs = options.allowDiagonal ? directions8 : directions4;
            int iterations = 0;

            while (openSet.Count > 0 && iterations < options.maxIterations)
            {
                iterations++;
                var currentNode = openSet.Pop();
                lastStats.nodesExplored++;
                closedSet.Add(currentNode.position);

                if (currentNode.position == target)
                {
                    lastStats.iterations = iterations;
                    return ReconstructPath(currentNode);
                }
                foreach (var d in dirs)
                {
                    var np = currentNode.position + d;
                    if (closedSet.Contains(np) ||
                        !IsPositionValid(np) ||
                        HasDynamicObstacle(np))
                        continue;

                    if (d.x != 0 && d.y != 0)
                    {
                        Vector3Int orth1 = new(currentNode.position.x + d.x, currentNode.position.y, 0);
                        Vector3Int orth2 = new(currentNode.position.x, currentNode.position.y + d.y, 0);
                        if (!IsPositionValid(orth1) || !IsPositionValid(orth2) ||
                            HasDynamicObstacle(orth1) || HasDynamicObstacle(orth2))
                            continue;
                    }

                    float moveCost = GetMoveCost(d, currentNode.position, np);
                    float newGCost = currentNode.gCost + moveCost;

                    if (!allNodes.TryGetValue(np, out var n))
                    {
                        n = new PathNode(np, newGCost, GetHeuristic(np, target), currentNode);
                        allNodes[np] = n;
                        openSet.Add(n);
                    }
                    else if (newGCost < n.gCost)
                    {
                        n.gCost = newGCost;
                        n.parent = currentNode;
                        openSet.Update(n);
                    }
                }
            }
            lastStats.iterations = iterations;
            return new List<Vector3Int>();
        }

        private List<Vector3Int> ExecuteJumpPointSearch(Vector3Int start, Vector3Int target, PathfindingOptions options)
        {
            // 未来实现 JPS，这里先回退 A*
            return ExecuteAStar(start, target, options);
        }

        private float GetMoveCost(Vector3Int direction, Vector3Int from, Vector3Int to)
        {
            float baseCost = (direction.x != 0 && direction.y != 0) ? diagonalMoveCost : straightMoveCost;
            if (useTerrainCosts)
            {
                var info = unifiedMap.GetTileInfo(to);
                if (info != null) baseCost *= info.cost;
            }
            return baseCost;
        }

        private float GetHeuristic(Vector3Int a, Vector3Int b)
        {
            if (allowDiagonalMovement)
            {
                float dx = a.x - b.x;
                float dy = a.y - b.y;
                return Mathf.Sqrt(dx * dx + dy * dy);
            }
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        private List<Vector3Int> ReconstructPath(PathNode end)
        {
            var path = new List<Vector3Int>();
            var cur = end;
            while (cur != null)
            {
                path.Insert(0, cur.position);
                cur = cur.parent;
            }
            return path;
        }
        #endregion

        #region 路径后处理与过渡
        private List<Vector3Int> PostProcessPath(List<Vector3Int> path)
        {
            if (path.Count <= 2) return path;
            switch (pathSmoothing)
            {
                case PathSmoothingType.LineOfSight: return SmoothPathLineOfSight(path);
                case PathSmoothingType.Bezier: return SmoothPathBezier(path);
                default: return path;
            }
        }
        private List<Vector3Int> SmoothPathLineOfSight(List<Vector3Int> path)
        {
            if (path.Count <= 2) return path;
            var smoothed = new List<Vector3Int> { path[0] };
            int currentIndex = 0;
            while (currentIndex < path.Count - 1)
            {
                int farthest = currentIndex + 1;
                for (int i = currentIndex + 2; i < path.Count; i++)
                {
                    if (HasLineOfSight(path[currentIndex], path[i]))
                        farthest = i;
                    else break;
                }
                smoothed.Add(path[farthest]);
                currentIndex = farthest;
            }
            return smoothed;
        }
        private List<Vector3Int> SmoothPathBezier(List<Vector3Int> path)
        {
            if (path == null || path.Count < 3 || allTilemaps.Count == 0) return path;
            var result = new List<Vector3Int> { path[0] };
            float densityFactor = bezierBaseDensity * Mathf.Lerp(0.6f, 2f, Mathf.Clamp01(smoothingStrength));
            for (int i = 0; i < path.Count - 2; i++)
            {
                var a = path[i]; var b = path[i + 1]; var c = path[i + 2];
                Vector3 wa = GetWorldPosition(a);
                Vector3 wb = GetWorldPosition(b);
                Vector3 wc = GetWorldPosition(c);
                float segLen = (wc - wa).magnitude;
                int sampleCount = Mathf.Clamp(Mathf.CeilToInt(segLen * densityFactor), bezierMinSamples, bezierMaxSamples);
                for (int s = 1; s <= sampleCount; s++)
                {
                    float t = s / (float)sampleCount;
                    Vector3 wp = (1 - t) * (1 - t) * wa + 2 * (1 - t) * t * wb + t * t * wc;
                    Vector3Int cell = allTilemaps[0].WorldToCell(wp);
                    if (!IsPositionValid(cell)) continue;
                    if (result[result.Count - 1] != cell)
                        result.Add(cell);
                }
            }
            if (result[result.Count - 1] != path[^1])
                result.Add(path[^1]);
            return result;
        }
        private bool HasLineOfSight(Vector3Int start, Vector3Int end)
        {
            var pts = GetPointsOnLine(start, end);
            foreach (var p in pts)
            {
                if (!IsPositionValid(p) || HasDynamicObstacle(p)) return false;
            }
            return true;
        }
        private List<Vector3Int> GetPointsOnLine(Vector3Int start, Vector3Int end)
        {
            var pts = new List<Vector3Int>();
            int dx = Mathf.Abs(end.x - start.x);
            int dy = Mathf.Abs(end.y - start.y);
            int x = start.x;
            int y = start.y;
            int x_inc = end.x > start.x ? 1 : -1;
            int y_inc = end.y > start.y ? 1 : -1;
            int error = dx - dy;
            dx *= 2; dy *= 2;
            for (int n = 1 + dx + dy; n > 0; --n)
            {
                pts.Add(new Vector3Int(x, y, 0));
                if (error > 0) { x += x_inc; error -= dy; }
                else { y += y_inc; error += dx; }
            }
            return pts;
        }

        private void SmoothTransitionToNewPath(List<Vector3Int> newPath)
        {
            if (newPath == null || newPath.Count == 0) return;
            Vector3 curPos = pathfindingObject.transform.position;
            int bestIndex = FindClosestPathIndex(newPath, curPos);
            if (bestIndex >= 0)
            {
                Vector3 connectWorld = GetWorldPosition(newPath[bestIndex]);
                float backtrack = Vector3.Distance(curPos, connectWorld);
                if (backtrack <= maxBacktrackDistance)
                {
                    currentPath = newPath;
                    currentPathIndex = bestIndex;
                    currentTarget = connectWorld;
                    return;
                }
            }
            MoveAlongPath(newPath);
        }
        private int FindClosestPathIndex(List<Vector3Int> path, Vector3 currentPosition)
        {
            if (path == null || path.Count == 0) return -1;
            float min = float.MaxValue;
            int best = -1;
            int searchStart = Mathf.Max(0, currentPathIndex - 2);
            int searchEnd = Mathf.Min(path.Count, currentPathIndex + 5);
            for (int i = searchStart; i < searchEnd; i++)
            {
                float d = Vector3.Distance(currentPosition, GetWorldPosition(path[i]));
                if (d < min) { min = d; best = i; }
            }
            if (best == -1)
            {
                for (int i = 0; i < path.Count; i++)
                {
                    float d = Vector3.Distance(currentPosition, GetWorldPosition(path[i]));
                    if (d < min) { min = d; best = i; }
                }
            }
            return best;
        }
        private bool ShouldUpdatePath(List<Vector3Int> newPath)
        {
            if (currentPath.Count == 0) return true;
            if (newPath.Count == 0) return false;
            if (currentPath[^1] != newPath[^1]) return true;
            float lengthDiff = Mathf.Abs(newPath.Count - currentPath.Count) / (float)currentPath.Count;
            if (lengthDiff > 0.3f) return true;
            int check = Mathf.Min(3, Mathf.Min(currentPath.Count, newPath.Count));
            for (int i = 0; i < check; i++)
                if (currentPath[i] != newPath[i]) return true;
            return false;
        }
        #endregion

        #region 移动相关
        private System.Collections.IEnumerator MoveCoroutine()
        {
            while (currentPathIndex < currentPath.Count)
            {
                Vector3Int targetCell = currentPath[currentPathIndex];
                Vector3 targetWorld = GetWorldPosition(targetCell);
                currentTarget = targetWorld;
                isMovingToTarget = true;

                while (Vector3.Distance(pathfindingObject.transform.position, targetWorld + currentJitterOffset) > 0.05f)
                {
                    if (currentPathIndex < currentPath.Count)
                    {
                        targetCell = currentPath[currentPathIndex];
                        targetWorld = GetWorldPosition(targetCell);
                        currentTarget = targetWorld;
                    }

                    float currentSpeed = moveSpeed;
                    if (moveSpeedCurve.keys.Length > 1)
                    {
                        Vector3 startPos = currentPathIndex > 0
                            ? GetWorldPosition(currentPath[currentPathIndex - 1])
                            : pathfindingObject.transform.position;
                        float pathProgress = 1f - (Vector3.Distance(pathfindingObject.transform.position, targetWorld) /
                                                   Mathf.Max(0.0001f, Vector3.Distance(startPos, targetWorld)));
                        pathProgress = Mathf.Clamp01(pathProgress);
                        currentSpeed = moveSpeed * moveSpeedCurve.Evaluate(pathProgress);
                    }

                    if (enableMovementJitter)
                    {
                        jitterTimer += Time.deltaTime;
                        if (jitterTimer >= 1f / jitterFrequency)
                        {
                            jitterTimer = 0f;
                            currentJitterOffset = GenerateJitterOffset(pathfindingObject.transform.position);
                            if (showJitterDebug && jitterHistory.Count < 50)
                                jitterHistory.Add(pathfindingObject.transform.position + currentJitterOffset);
                        }
                    }

                    Vector3 jitteredTarget = targetWorld + currentJitterOffset;
                    Vector3 newPos = Vector3.MoveTowards(pathfindingObject.transform.position, jitteredTarget, currentSpeed * Time.deltaTime);

                    if (autoRotateToDirection)
                    {
                        Vector3 dir = (jitteredTarget - pathfindingObject.transform.position).normalized;
                        if (dir.sqrMagnitude > 0.01f)
                        {
                            Quaternion targetRot = Quaternion.LookRotation(Vector3.forward, dir);
                            pathfindingObject.transform.rotation =
                                Quaternion.Lerp(pathfindingObject.transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
                        }
                    }

                    pathfindingObject.transform.position = newPos;
                    OnMovementUpdate?.Invoke(newPos);
                    yield return null;
                }

                isMovingToTarget = false;
                currentPathIndex++;
                if (currentPathIndex < currentPath.Count)
                    currentJitterOffset = GenerateJitterOffset(pathfindingObject.transform.position);
            }

            Vector3 finalTarget = GetWorldPosition(currentPath[^1]);
            while (Vector3.Distance(pathfindingObject.transform.position, finalTarget) > 0.01f)
            {
                currentJitterOffset = Vector3.Lerp(currentJitterOffset, Vector3.zero, Time.deltaTime * 3f);
                pathfindingObject.transform.position =
                    Vector3.MoveTowards(pathfindingObject.transform.position, finalTarget, moveSpeed * Time.deltaTime);
                yield return null;
            }
            pathfindingObject.transform.position = finalTarget;
            currentJitterOffset = Vector3.zero;
            isMovingToTarget = false;
            OnMovementComplete?.Invoke(finalTarget);
            moveCoroutine = null;
        }
        #endregion

        #region 抖动
        private Vector3 GenerateJitterOffset(Vector3 currentWorldPos)
        {
            if (!enableMovementJitter || jitterStrength <= 0f) return Vector3.zero;
            Vector3 randomOffset = new Vector3(
                UnityEngine.Random.Range(-1f, 1f),
                UnityEngine.Random.Range(-1f, 1f),
                0f).normalized * (jitterStrength * UnityEngine.Random.Range(0.5f, 1f));

            if (preventJitterIntoNull && allTilemaps.Count > 0)
            {
                Vector3Int tilePos = allTilemaps[0].WorldToCell(currentWorldPos + randomOffset);
                if (!IsPositionValid(tilePos))
                {
                    Vector3[] fallback =
                    {
                        Vector3.right, Vector3.left, Vector3.up, Vector3.down,
                        new Vector3(1,1,0).normalized, new Vector3(-1,1,0).normalized,
                        new Vector3(1,-1,0).normalized, new Vector3(-1,-1,0).normalized
                    };
                    foreach (var d in fallback)
                    {
                        Vector3 fo = d * (jitterStrength * 0.5f);
                        Vector3Int ft = allTilemaps[0].WorldToCell(currentWorldPos + fo);
                        if (IsPositionValid(ft)) return fo;
                    }
                    return Vector3.zero;
                }
            }
            return randomOffset;
        }
        #endregion

        #region 工具方法 / 设置接口 / 调试
        public Vector3Int GetTilePositionFromGameObject(GameObject obj)
        {
            if (obj == null)
                return Vector3Int.zero;

            Tilemap refMap = null;
            if (allTilemaps.Count > 0) refMap = allTilemaps[0];
            else if (conversionTilemap != null) refMap = conversionTilemap;

            if (refMap == null)
            {
                Debug.LogWarning($"[{name}] 无可用 Tilemap 进行坐标转换，请配置 Tilemap 或共享服务。");
                return Vector3Int.zero;
            }

            return refMap.WorldToCell(obj.transform.position);
        }

        private Vector3 GetWorldPosition(Vector3Int cell)
        {
            Tilemap refMap = null;
            if (allTilemaps.Count > 0) refMap = allTilemaps[0];
            else if (conversionTilemap != null) refMap = conversionTilemap;

            if (refMap == null)
                return Vector3.zero;

            Vector3 worldPos = refMap.CellToWorld(cell);
            if (useTileCenterOffset)
            {
                var cs = refMap.cellSize;
                worldPos += new Vector3(cs.x * tileOffset.x, cs.y * tileOffset.y, cs.z * tileOffset.z);
            }
            return worldPos;
        }

        public void SetTargetObject(GameObject target) => targetObject = target;
        public void SetPathfindingObject(GameObject pathfinder) => pathfindingObject = pathfinder;
        public bool IsMoving => moveCoroutine != null;
        public int GetWalkableTileCount() => unifiedMap?.walkableTiles.Count ?? 0;
        public void SetJitterEnabled(bool enabled) => enableMovementJitter = enabled;
        public void SetJitterStrength(float strength) => jitterStrength = Mathf.Clamp01(strength);
        public void SetJitterFrequency(float frequency) => jitterFrequency = Mathf.Max(0.1f, frequency);
        public void SetSmoothPathTransition(bool enabled) => enableSmoothPathTransition = enabled;
        public void SetMaxBacktrackDistance(float distance) => maxBacktrackDistance = Mathf.Max(0.1f, distance);

        private void OnDrawGizmos()
        {
            if (unifiedMap == null) return;
            if (showWalkableArea)
            {
                Gizmos.color = walkableAreaColor;
                foreach (var tile in unifiedMap.walkableTiles.Keys)
                {
                    Gizmos.DrawWireCube(GetWorldPosition(tile), Vector3.one * 0.3f);
                }
            }
            if (showDebugPath && currentPath != null && currentPath.Count > 0)
            {
                Gizmos.color = pathColor;
                for (int i = 0; i < currentPath.Count - 1; i++)
                {
                    Gizmos.DrawLine(GetWorldPosition(currentPath[i]), GetWorldPosition(currentPath[i + 1]));
                    Gizmos.DrawWireSphere(GetWorldPosition(currentPath[i]), 0.15f);
                }
                if (currentPathIndex < currentPath.Count)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(GetWorldPosition(currentPath[currentPathIndex]), 0.2f);
                }
            }
            if (considerDynamicObstacles)
            {
                Gizmos.color = obstacleColor;
                foreach (var o in dynamicObstacles)
                    if (o != null) Gizmos.DrawWireSphere(o.position, obstacleCheckRadius);
            }
            if (pathfindingObject != null)
            {
                Gizmos.color = startPointColor;
                Gizmos.DrawWireCube(GetWorldPosition(GetTilePositionFromGameObject(pathfindingObject)), Vector3.one * 0.6f);
            }
            if (targetObject != null)
            {
                Gizmos.color = targetPointColor;
                Gizmos.DrawWireCube(GetWorldPosition(GetTilePositionFromGameObject(targetObject)), Vector3.one * 0.6f);
            }
            if (showJitterDebug && enableMovementJitter && pathfindingObject != null && currentJitterOffset != Vector3.zero)
            {
                Gizmos.color = jitterDebugColor;
                Vector3 cp = pathfindingObject.transform.position;
                Gizmos.DrawLine(cp, cp + currentJitterOffset);
                Gizmos.DrawWireSphere(cp + currentJitterOffset, 0.1f);
            }
        }
        #endregion

        #region 辅助结构
        internal class PathNodeMinHeap
        {
            private readonly List<PathNode> _list = new();
            public int Count => _list.Count;
            public void Add(PathNode node)
            {
                node.heapIndex = _list.Count;
                _list.Add(node);
                HeapifyUp(node.heapIndex);
            }
            public PathNode Pop()
            {
                PathNode root = _list[0];
                PathNode last = _list[_list.Count - 1];
                _list.RemoveAt(_list.Count - 1);
                if (_list.Count > 0)
                {
                    _list[0] = last;
                    last.heapIndex = 0;
                    HeapifyDown(0);
                }
                root.heapIndex = -1;
                return root;
            }
            public void Update(PathNode node) => HeapifyUp(node.heapIndex);
            private void HeapifyUp(int i)
            {
                while (i > 0)
                {
                    int p = (i - 1) >> 1;
                    if (Compare(_list[i], _list[p]) < 0)
                    {
                        Swap(i, p);
                        i = p;
                    }
                    else break;
                }
            }
            private void HeapifyDown(int i)
            {
                int count = _list.Count;
                while (true)
                {
                    int left = (i << 1) + 1;
                    if (left >= count) break;
                    int right = left + 1;
                    int smallest = left;
                    if (right < count && Compare(_list[right], _list[left]) < 0)
                        smallest = right;
                    if (Compare(_list[smallest], _list[i]) < 0)
                    {
                        Swap(smallest, i);
                        i = smallest;
                    }
                    else break;
                }
            }
            private int Compare(PathNode a, PathNode b)
            {
                int fc = a.fCost.CompareTo(b.fCost);
                if (fc != 0) return fc;
                return a.hCost.CompareTo(b.hCost);
            }
            private void Swap(int i, int j)
            {
                var tmp = _list[i];
                _list[i] = _list[j];
                _list[j] = tmp;
                _list[i].heapIndex = i;
                _list[j].heapIndex = j;
            }
        }
        #endregion
    }

    public enum PathSmoothingType { None, LineOfSight, Bezier }

    [System.Serializable]
    public class PathfindingOptions
    {
        public bool allowDiagonal = true;
        public float maxDistance = 100f;
        public int maxIterations = 10000;
        public bool useJPS = false;
        public bool considerTerrain = false;
    }

    [System.Serializable]
    public class PathfindingStats
    {
        public bool success = false;
        public float searchTime = 0f;
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
}