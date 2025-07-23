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
        [Header("Tilemap ����")]
        [Tooltip("�������п�����Ѱ·��Tilemap�����ϵͳ��ɨ����ЩTilemap�е���Ƭ��Ϊ����������")]
        public List<Tilemap> allTilemaps = new List<Tilemap>();

        [Header("Ѱ·����")]
        [Tooltip("ִ��Ѱ·����Ʒ�������Ϊnull����ô�Ƿ�Ĭ������Ϊ����")]
        public bool ispathFindingObjectSelf;
        [Tooltip("ִ��Ѱ·����Ϸ������ΪѰ·�����")]
        public GameObject pathfindingObject;

        [Tooltip("Ѱ·��Ŀ����Ϸ������ΪѰ·���յ�")]
        public GameObject targetObject;

        [Tooltip("�Ƿ�������Ƭƫ��")]
        public bool useTileCenterOffset = true;

        [Tooltip("��Ƭƫ������(0.5, 0.5)��ʾ��Ƭ���ģ���Χ0-1")]
        public Vector3 tileOffset = new Vector3(0.5f, 0.5f);

        [Header("�ƶ�����")]
        [Tooltip("��ɫ�ƶ��ٶȣ���λΪUnity��λ/��")]
        public float moveSpeed = 3f;

        [Tooltip("�Ƿ�����Խ����ƶ���8���򣩣�������ֻ�����������ƶ���4����")]
        public bool allowDiagonalMovement = true;

        [Tooltip("�ƶ��ٶ����ߣ����Կ����ƶ������еļ��ٶȱ仯")]
        public AnimationCurve moveSpeedCurve = AnimationCurve.Linear(0, 1, 1, 1);

        [Tooltip("�Ƿ��Զ�ת���ƶ��������ú��ɫ�ᳯ���ƶ�������ת")]
        public bool autoRotateToDirection = false;

        [Tooltip("ת���ٶȣ�ֵԽ��ת��Խ��")]
        public float rotationSpeed = 5f;

        [Header("�߼�Ѱ·����")]
        [Tooltip("�ϰ�����Ĳ㼶���룬������ײ���")]
        public LayerMask obstacleLayerMask = -1;

        [Tooltip("����������룬�����˾����Ŀ�꽫����Ϊ���ɴ�")]
        public float maxSearchDistance = 100f;

        [Tooltip("Ѱ·�㷨����������������ֹ����ѭ��")]
        public int maxIterations = 10000;

        [Tooltip("�Ƿ�ʹ�����������Ż��㷨��������ߴ��ͼ��Ѱ·����")]
        public bool useJumpPointSearch = false;

        [Tooltip("·��ƽ���㷨���ͣ�������·������Ȼ")]
        public PathSmoothingType pathSmoothing = PathSmoothingType.None;

        [Tooltip("·��ƽ��ǿ�ȣ�ֵԽ��ƽ��Ч��Խ����")]
        public float smoothingStrength = 0.5f;

        [Header("��������")]
        [Tooltip("ֱ���ƶ��Ļ�������ֵ")]
        public float straightMoveCost = 1f;

        [Tooltip("�Խ����ƶ��Ĵ���ֵ��ͨ��Ϊ��2��1.414")]
        public float diagonalMoveCost = 1.414f;

        [Tooltip("��Ƭ�������ƶ����۵�ӳ�����ͬ��Ƭ�����в�ͬ��ͨ������")]
        public Dictionary<TileBase, float> tileCostMap = new Dictionary<TileBase, float>();

        [Tooltip("�Ƿ����õ��δ���ϵͳ�����ǲ�ͬ��Ƭ���ƶ��ɱ�")]
        public bool useTerrainCosts = false;

        [Header("��̬�ϰ���")]
        [Tooltip("�Ƿ��Ƕ�̬�ϰ�����ƶ��ĵ��˻�����")]
        public bool considerDynamicObstacles = false;

        [Tooltip("��̬�ϰ���ļ��뾶���ڴ˷�Χ�ڵ�λ�ý�����Ϊ����ͨ��")]
        public float obstacleCheckRadius = 0.3f;

        [Tooltip("��̬�ϰ����б���ЩTransformλ����Χ������Ϊ�ϰ�")]
        public List<Transform> dynamicObstacles = new List<Transform>();

        [Header("��������")]
        [Tooltip("�Ƿ������ƶ�����Ч�������ƶ�����������Ȼ(����)")]
        public bool enableMovementJitter = false;

        [Tooltip("����ǿ�ȣ���Χ0-1��ֵԽ�󶶶�Խ����")]
        public float jitterStrength = 0.1f;

        [Tooltip("����Ƶ�ʣ�ÿ����������Ĵ���")]
        public float jitterFrequency = 2f;

        [Tooltip("��ֹ����������Ч����ȷ�����������ý�ɫ�ƶ����������ߵ���Ƭ")]
        public bool preventJitterIntoNull = true;

        [Header("�Զ�ˢ������")]
        [Tooltip("�Ƿ������Զ�ˢ�¹��ܣ���Update�ж������¼���Ѱ·")]
        public bool enableAutoRefresh = false;

        [Tooltip("�Զ�ˢ�µ�Ƶ�ʣ�ÿ��ˢ�µĴ���")]
        [Range(0.1f, 10f)]
        public float refreshFrequency = 1f;

        [Tooltip("�Ƿ���Ŀ������ƶ�ʱ����ˢ��")]
        public bool refreshOnTargetMove = true;

        [Tooltip("Ŀ���ƶ�����С������ֵ�������˾���Ŵ���ˢ��")]
        public float targetMoveThreshold = 0.5f;

        [Header("ƽ��·���л�����")]
        [Tooltip("�Ƿ�����ƽ��·���л���������ֹͣ�ٿ�ʼ")]
        public bool enableSmoothPathTransition = true;

        [Tooltip("·���л�ʱ�������˾��룬�����˾��뽫ֱ���л�����·��")]
        public float maxBacktrackDistance = 2f;

        [Header("�¼�")]
        [Tooltip("�ҵ�·��ʱ�������¼�������Ϊ·�����б�")]
        public PathfindingEvent OnPathFound = new PathfindingEvent();

        [Tooltip("δ�ҵ�·��ʱ�������¼�")]
        public UnityEvent OnPathNotFound = new UnityEvent();

        [Tooltip("��ʼ�ƶ�ʱ�������¼�������Ϊ��ʼλ��")]
        public MovementEvent OnMovementStart = new MovementEvent();

        [Tooltip("�ƶ�������ÿ֡�������¼�������Ϊ��ǰλ��")]
        public MovementEvent OnMovementUpdate = new MovementEvent();

        [Tooltip("�ƶ����ʱ�������¼�������Ϊ����λ��")]
        public MovementEvent OnMovementComplete = new MovementEvent();

        [Tooltip("�ƶ���ֹͣʱ�������¼�")]
        public UnityEvent OnMovementStopped = new UnityEvent();

        [Header("��������")]
        [Tooltip("�Ƿ���Scene��ͼ����ʾѰ··��")]
        public bool showDebugPath = true;

        [Tooltip("�Ƿ���ʾ���п��������������")]
        public bool showWalkableArea = false;

        [Tooltip("�Ƿ���ʾ����������Ϣ")]
        public bool showJitterDebug = false;

        [Tooltip("�Ƿ���ʾ��������Χ")]
        public bool showSearchArea = false;

        [Tooltip("�Ƿ���ʾ����Ƭ�Ĵ���ֵ")]
        public bool showCostValues = false;

        [Tooltip("·����������ɫ")]
        public Color pathColor = Color.red;

        [Tooltip("����ǵ���ɫ")]
        public Color startPointColor = Color.green;

        [Tooltip("�յ��ǵ���ɫ")]
        public Color targetPointColor = Color.blue;

        [Tooltip("�������������ʾ��ɫ")]
        public Color walkableAreaColor = Color.yellow;

        [Tooltip("����������Ϣ����ʾ��ɫ")]
        public Color jitterDebugColor = Color.magenta;

        [Tooltip("�����������ʾ��ɫ")]
        public Color searchAreaColor = Color.cyan;

        [Tooltip("�ϰ������ʾ��ɫ")]
        public Color obstacleColor = Color.red;

        // ͳһ��ͼ���ݽṹ
        private UnifiedMap unifiedMap;
        private List<Vector3Int> currentPath = new List<Vector3Int>();
        private Coroutine moveCoroutine;

        // �ƶ�״̬׷��
        private int currentPathIndex = 0;
        private Vector3 currentTarget = Vector3.zero;
        public bool isMovingToTarget = false;

        // �������
        private Vector3 currentJitterOffset = Vector3.zero;
        private float jitterTimer = 0f;
        private List<Vector3> jitterHistory = new List<Vector3>(); // ���ڵ�����ʾ

        // ����ͳ��
        private PathfindingStats lastStats = new PathfindingStats();

        // ��������
        private readonly Vector3Int[] directions8 = {
            Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right,
            new Vector3Int(1, 1, 0), new Vector3Int(-1, 1, 0),
            new Vector3Int(1, -1, 0), new Vector3Int(-1, -1, 0)
        };

        private readonly Vector3Int[] directions4 = {
            Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right
        };

        // �Զ�ˢ�����
        private float refreshTimer = 0f;
        private Vector3 lastTargetPosition;
        private bool hasInitializedTargetPosition = false;

        private void Start()
        {
            // ���δ����Ѱ·������Ĭ��ʹ������
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
        /// �����Զ�ˢ���߼�
        /// </summary>
        private void HandleAutoRefresh()
        {
            // Ƶ��ˢ��
            refreshTimer += Time.deltaTime;
            bool shouldRefreshByFrequency = refreshTimer >= 1f / refreshFrequency;

            // Ŀ���ƶ�ˢ��
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

            // ִ��ˢ��
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
        /// �Զ�ˢ��Ѱ·
        /// </summary>
        private void AutoRefreshPathfinding()
        {
            // ֻ���ڽ�ɫ�����ƶ���Ŀ����Чʱ��ˢ��
            if (!IsMoving || pathfindingObject == null || targetObject == null)
                return;

            Vector3Int startPos;
            if (currentPath.Count > 0 && currentPathIndex < currentPath.Count)
            {
                // ��������ƶ���ĳ��·���㣬ʹ�ø�·������Ϊ���
                startPos = currentPath[currentPathIndex];
            }
            else
            {
                // ���÷�����ʹ�õ�ǰ��Ƭλ��
                startPos = GetTilePositionFromGameObject(pathfindingObject);
            }

            Vector3Int targetPos = GetTilePositionFromGameObject(targetObject);

            // ���Ŀ��λ��û�иı䣬����Ҫˢ��
            if (currentPath.Count > 0 && targetPos == currentPath[currentPath.Count - 1])
                return;

            var newPath = FindPath(startPos, targetPos);

            if (newPath.Count > 0)
            {
                // ֻ�е���·���뵱ǰ·��������ͬʱ�Ÿ���
                if (ShouldUpdatePath(newPath))
                {
                    // ʹ��ƽ�����ɶ�����ǿ������
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
        /// ƽ�����ɵ���·��
        /// </summary>
        private void SmoothTransitionToNewPath(List<Vector3Int> newPath)
        {
            if (newPath == null || newPath.Count == 0) return;

            Vector3 currentPosition = pathfindingObject.transform.position;

            // �ҵ���·������ӽ���ǰλ�õĵ�
            int bestIndex = FindClosestPathIndex(newPath, currentPosition);

            // ����ҵ����ʵ����ӵ�
            if (bestIndex >= 0)
            {
                // ����ӵ�ǰλ�õ�������ӵ�ľ���
                Vector3 connectPoint = GetWorldPosition(newPath[bestIndex]);
                float backtrackDistance = Vector3.Distance(currentPosition, connectPoint);

                // ������˾����ڿɽ��ܷ�Χ�ڣ���Ӹõ����
                if (backtrackDistance <= maxBacktrackDistance)
                {
                    // ����·�������ӵ㿪ʼ
                    currentPath = newPath;
                    currentPathIndex = bestIndex;
                    currentTarget = GetWorldPosition(newPath[currentPathIndex]);

                    // ����Ҫ����Э�̣���ǰЭ�̻��Զ�ʹ���µ�·��������
                    return;
                }
            }

            // ����޷�ƽ�����ɣ��������ƶ�
            MoveAlongPath(newPath);
        }

        /// <summary>
        /// ����·�����ҵ���ӽ���ǰλ�õ�·��������
        /// </summary>
        private int FindClosestPathIndex(List<Vector3Int> path, Vector3 currentPosition)
        {
            if (path == null || path.Count == 0) return -1;

            float minDistance = float.MaxValue;
            int bestIndex = -1;

            // �ӵ�ǰ·��������ʼ��ǰ�������������̫Զ
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

            // ���������Χ���Ҳ������ʵ㣬����������·��
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
        /// �ж��Ƿ�Ӧ�ø���·��
        /// </summary>
        private bool ShouldUpdatePath(List<Vector3Int> newPath)
        {
            if (currentPath.Count == 0) return true;
            if (newPath.Count == 0) return false;

            // ����յ㲻ͬ����Ҫ����
            if (currentPath[currentPath.Count - 1] != newPath[newPath.Count - 1])
                return true;

            // ���·�����Ȳ���ܴ���Ҫ����
            float lengthDifference = Mathf.Abs(newPath.Count - currentPath.Count) / (float)currentPath.Count;
            if (lengthDifference > 0.3f) // 30%�Ĳ���
                return true;

            // ���·����ǰ�����Ƿ�����������
            int checkSteps = Mathf.Min(3, Mathf.Min(currentPath.Count, newPath.Count));
            for (int i = 0; i < checkSteps; i++)
            {
                if (currentPath[i] != newPath[i])
                    return true;
            }

            return false;
        }

        /// <summary>
        /// ��ʼ���Զ�ˢ��
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
        /// ����ͳһ��ͼ
        /// </summary>
        [ContextMenu("����ͳһ��ͼ")]
        public void BuildUnifiedMap()
        {
            if (allTilemaps == null || allTilemaps.Count == 0)
            {
                Debug.LogError("û������Tilemap��");
                return;
            }

            unifiedMap = new UnifiedMap();

            // ɨ������Tilemap���ռ����п����ߵ���Ƭλ��
            foreach (var tilemap in allTilemaps)
            {
                if (tilemap == null) continue;
                ScanTilemap(tilemap);
            }
        }

        /// <summary>
        /// ɨ�赥��Tilemap����ӵ�ͳһ��ͼ
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
        /// �����Ƭ�Ƿ�����ߣ�������д�˷������Զ����߼���
        /// </summary>
        protected virtual bool IsTileWalkable(TileBase tile, Vector3Int position)
        {
            // �ǿ���Ƭ��������
            return tile != null;
        }

        /// <summary>
        /// ��ȡ��Ƭ�ƶ�����
        /// </summary>
        protected virtual float GetTileCost(TileBase tile)
        {
            if (useTerrainCosts && tileCostMap.ContainsKey(tile))
            {
                return tileCostMap[tile];
            }
            return 1f; // Ĭ�ϴ���
        }

        /// <summary>
        /// ��鶯̬�ϰ���
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
        /// �����Զ�ˢ������״̬
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
        /// ����ˢ��Ƶ��
        /// </summary>
        public void SetRefreshFrequency(float frequency)
        {
            refreshFrequency = Mathf.Clamp(frequency, 0.1f, 10f);
            refreshTimer = 0f; // ���ü�ʱ��
        }

        /// <summary>
        /// ����Ŀ���ƶ�ˢ��
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
        /// ����Ŀ���ƶ���ֵ
        /// </summary>
        public void SetTargetMoveThreshold(float threshold)
        {
            targetMoveThreshold = Mathf.Max(0.1f, threshold);
        }

        /// <summary>
        /// ǿ��ˢ��Ѱ·������Ƶ�����ƣ�
        /// </summary>
        [ContextMenu("ǿ��ˢ��Ѱ·")]
        public void ForceRefreshPathfinding()
        {
            refreshTimer = 0f;
            AutoRefreshPathfinding();
        }

        /// <summary>
        /// ��ҪѰ·���� - ֧�ֶ���Ѱ·ѡ��
        /// </summary>
        public List<Vector3Int> FindPath(Vector3Int startPos, Vector3Int targetPos, PathfindingOptions options = null)
        {
            if (unifiedMap == null || unifiedMap.walkableTiles.Count == 0)
            {
                Debug.LogError("ͳһ��ͼδ������Ϊ�գ�");
                OnPathNotFound?.Invoke();
                return new List<Vector3Int>();
            }

            // ʹ��Ĭ��ѡ��
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

            // ��������յ�
            if (!IsPositionValid(startPos) || HasDynamicObstacle(startPos))
            {
                Debug.LogError($"��� {startPos} �������ߣ�");
                OnPathNotFound?.Invoke();
                return new List<Vector3Int>();
            }

            if (!IsPositionValid(targetPos) || HasDynamicObstacle(targetPos))
            {
                Debug.LogError($"�յ� {targetPos} �������ߣ�");
                OnPathNotFound?.Invoke();
                return new List<Vector3Int>();
            }

            // ������
            float distance = Vector3Int.Distance(startPos, targetPos);
            if (distance > options.maxDistance)
            {
                Debug.LogWarning($"Ŀ����� {distance} ��������������� {options.maxDistance}");
                OnPathNotFound?.Invoke();
                return new List<Vector3Int>();
            }

            if (startPos == targetPos)
            {
                var singlePath = new List<Vector3Int> { startPos };
                OnPathFound?.Invoke(singlePath);
                return singlePath;
            }

            // ִ��Ѱ·�㷨
            List<Vector3Int> path;
            if (options.useJPS && allowDiagonalMovement)
            {
                path = ExecuteJumpPointSearch(startPos, targetPos, options);
            }
            else
            {
                path = ExecuteAStar(startPos, targetPos, options);
            }

            // ·������
            if (path.Count > 0)
            {
                path = PostProcessPath(path);

                // ��¼ͳ����Ϣ
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
        /// ���λ���Ƿ���Ч
        /// </summary>
        private bool IsPositionValid(Vector3Int position)
        {
            return unifiedMap.IsWalkable(position);
        }

        /// <summary>
        /// ·������ƽ���ȣ�
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
        /// ����ƽ��
        /// </summary>
        private List<Vector3Int> SmoothPathLineOfSight(List<Vector3Int> path)
        {
            if (path.Count <= 2) return path;

            var smoothed = new List<Vector3Int> { path[0] };
            int currentIndex = 0;

            while (currentIndex < path.Count - 1)
            {
                int farthestIndex = currentIndex + 1;

                // �ҵ���Զ�Ŀ�ֱ���
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
        /// ����������ƽ��
        /// </summary>
        private List<Vector3Int> SmoothPathBezier(List<Vector3Int> path)
        {
            //TODO:
            return path;
        }

        /// <summary>
        /// ���������Ƿ���ֱ����Ұ
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
        /// ��ȡֱ���ϵ����е�
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
        /// ���������㷨��JPS�Ż���
        /// </summary>
        private List<Vector3Int> ExecuteJumpPointSearch(Vector3Int start, Vector3Int target, PathfindingOptions options)
        {
            // TODO:
            return ExecuteAStar(start, target, options);
        }

        /// <summary>
        /// ִ��A*Ѱ·�㷨
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

                // �ҵ�Fֵ��С�Ľڵ�
                var currentNode = GetLowestFCostNode(openSet);
                openSet.Remove(currentNode);
                closedSet.Add(currentNode.position);

                // ����Ŀ��
                if (currentNode.position == target)
                {
                    lastStats.iterations = iterations;
                    return ReconstructPath(currentNode);
                }

                // ������ڽڵ�
                foreach (var direction in directions)
                {
                    Vector3Int neighborPos = currentNode.position + direction;

                    if (closedSet.Contains(neighborPos) ||
                        !IsPositionValid(neighborPos) ||
                        HasDynamicObstacle(neighborPos))
                        continue;

                    // �����ƶ�����
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
            Debug.LogWarning($"Ѱ·ʧ�ܣ���������: {iterations}");
            return new List<Vector3Int>();
        }

        /// <summary>
        /// ��ȡ�ƶ����ۣ����ǵ��κͷ���
        /// </summary>
        private float GetMoveCost(Vector3Int direction, Vector3Int from, Vector3Int to)
        {
            float baseCost = IsDiagonalMove(direction) ? diagonalMoveCost : straightMoveCost;

            if (useTerrainCosts)
            {
                // ��ȡĿ����Ƭ�ĵ��δ���
                var tileInfo = unifiedMap.GetTileInfo(to);
                if (tileInfo != null)
                {
                    baseCost *= tileInfo.cost;
                }
            }

            return baseCost;
        }

        /// <summary>
        /// �ж��Ƿ�Ϊ�Խ����ƶ�
        /// </summary>
        private bool IsDiagonalMove(Vector3Int direction)
        {
            return direction.x != 0 && direction.y != 0;
        }

        /// <summary>
        /// ��ȡ����ʽ����
        /// </summary>
        private float GetHeuristic(Vector3Int a, Vector3Int b)
        {
            if (allowDiagonalMovement)
            {
                // ŷ����þ���
                float dx = a.x - b.x;
                float dy = a.y - b.y;
                return Mathf.Sqrt(dx * dx + dy * dy);
            }
            else
            {
                // �����پ���
                return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
            }
        }

        /// <summary>
        /// ��ȡFֵ��С�Ľڵ�
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
        /// �ع�·��
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
        /// ��ȡGameObject�·�����Ƭλ��
        /// </summary>
        private Vector3Int GetTilePositionFromGameObject(GameObject obj)
        {
            if (obj == null || allTilemaps.Count == 0) return Vector3Int.zero;

            Vector3 worldPos = obj.transform.position;
            Vector3Int cellPos = allTilemaps[0].WorldToCell(worldPos);
            return cellPos;
        }

        /// <summary>
        /// ��ʼѰ·���ƶ�
        /// </summary>
        [ContextMenu("��ʼѰ·���ƶ�")]
        public void StartPathfindingAndMove()
        {
            if (pathfindingObject == null)
            {
                Debug.LogError("Ѱ·GameObjectδ���ã�");
                return;
            }

            if (targetObject == null)
            {
                Debug.LogError("Ŀ��GameObjectδ���ã�");
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
        /// ��·���ƶ�GameObject
        /// </summary>
        public void MoveAlongPath(List<Vector3Int> path)
        {
            if (pathfindingObject == null || path.Count == 0) return;

            // ֹ֮ͣǰ���ƶ�
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
        /// �ƶ�Э��
        /// </summary>
        private System.Collections.IEnumerator MoveCoroutine()
        {
            while (currentPathIndex < currentPath.Count)
            {
                Vector3Int targetCell = currentPath[currentPathIndex];
                Vector3 targetWorld = GetWorldPosition(targetCell);
                currentTarget = targetWorld;
                isMovingToTarget = true;

                // ƽ���ƶ���Ŀ��λ��
                while (Vector3.Distance(pathfindingObject.transform.position, targetWorld + currentJitterOffset) > 0.05f)
                {
                    // ���·���Ƿ񱻸��£�ƽ�����ɵ������
                    if (currentPathIndex < currentPath.Count)
                    {
                        targetCell = currentPath[currentPathIndex];
                        targetWorld = GetWorldPosition(targetCell);
                        currentTarget = targetWorld;
                    }

                    // �����ƶ��ٶ�
                    float currentSpeed = moveSpeed;
                    if (moveSpeedCurve.keys.Length > 1)
                    {
                        Vector3 startPos = currentPathIndex > 0 ? GetWorldPosition(currentPath[currentPathIndex - 1]) : pathfindingObject.transform.position;
                        float pathProgress = 1f - (Vector3.Distance(pathfindingObject.transform.position, targetWorld) / Vector3.Distance(startPos, targetWorld));
                        pathProgress = Mathf.Clamp01(pathProgress);
                        float speedMultiplier = moveSpeedCurve.Evaluate(pathProgress);
                        currentSpeed = moveSpeed * speedMultiplier;
                    }

                    // ���¶���
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

                    // �ƶ���Ŀ��λ��
                    Vector3 jitteredTarget = targetWorld + currentJitterOffset;
                    Vector3 newPosition = Vector3.MoveTowards(
                        pathfindingObject.transform.position,
                        jitteredTarget,
                        currentSpeed * Time.deltaTime
                    );

                    // ��ת���ƶ�����
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

            // �ƶ���ɴ���
            Vector3 finalTarget = GetWorldPosition(currentPath[currentPath.Count - 1]);

            // ƽ��������λ��
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
        /// ���ɶ���ƫ��
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
        /// ����Ƭ����ת��Ϊ��������
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
        /// ֹͣ�ƶ�
        /// </summary>
        [ContextMenu("ֹͣ�ƶ�")]
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
        /// ��Ӷ�̬�ϰ���
        /// </summary>
        public void AddDynamicObstacle(Transform obstacle)
        {
            if (!dynamicObstacles.Contains(obstacle))
            {
                dynamicObstacles.Add(obstacle);
            }
        }

        /// <summary>
        /// �Ƴ���̬�ϰ���
        /// </summary>
        public void RemoveDynamicObstacle(Transform obstacle)
        {
            dynamicObstacles.Remove(obstacle);
        }

        /// <summary>
        /// ������Ƭ����
        /// </summary>
        public void SetTileCost(TileBase tile, float cost)
        {
            tileCostMap[tile] = cost;
        }

        /// <summary>
        /// ��ȡ���һ��Ѱ·��ͳ����Ϣ
        /// </summary>
        public PathfindingStats GetLastPathfindingStats()
        {
            return lastStats;
        }

        // ���Ի���
        private void OnDrawGizmos()
        {
            if (unifiedMap == null) return;

            // ���ƿ���������
            if (showWalkableArea)
            {
                Gizmos.color = walkableAreaColor;
                foreach (var tile in unifiedMap.walkableTiles.Keys)
                {
                    Vector3 worldPos = GetWorldPosition(tile);
                    Gizmos.DrawWireCube(worldPos, Vector3.one * 0.3f);
                }
            }

            // ����·��
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

                // ������ǰĿ���
                if (currentPathIndex < currentPath.Count)
                {
                    Gizmos.color = Color.yellow;
                    Vector3 currentTargetPos = GetWorldPosition(currentPath[currentPathIndex]);
                    Gizmos.DrawWireSphere(currentTargetPos, 0.2f);
                }
            }

            // ���ƶ�̬�ϰ���
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

            // ���ƶ���������Ϣ
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

            // ���������յ�
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

        // �����ӿ�
        public void SetTargetObject(GameObject target) => targetObject = target;
        public void SetPathfindingObject(GameObject pathfinder) => pathfindingObject = pathfinder;
        public bool IsMoving => moveCoroutine != null;
        public int GetWalkableTileCount() => unifiedMap?.walkableTiles.Count ?? 0;
        public void SetJitterEnabled(bool enabled) => enableMovementJitter = enabled;
        public void SetJitterStrength(float strength) => jitterStrength = Mathf.Clamp01(strength);
        public void SetJitterFrequency(float frequency) => jitterFrequency = Mathf.Max(0.1f, frequency);

        // ƽ��·����������
        public void SetSmoothPathTransition(bool enabled) => enableSmoothPathTransition = enabled;
        public void SetMaxBacktrackDistance(float distance) => maxBacktrackDistance = Mathf.Max(0.1f, distance);
    }

    /// <summary>
    /// ·��ƽ������
    /// </summary>
    public enum PathSmoothingType
    {
        None,
        LineOfSight,
        Bezier
    }

    /// <summary>
    /// Ѱ·ѡ��
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
    /// Ѱ·ͳ����Ϣ
    /// </summary>
    [System.Serializable]
    public class PathfindingStats
    {
        public bool success = false;
        public float searchTime = 0f; // ����
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
    /// ͳһ��ͼ���ݽṹ
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
    /// ��Ƭ��Ϣ
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
    /// ·���ڵ�
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