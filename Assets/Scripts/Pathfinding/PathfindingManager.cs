using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 基于网格的 A* 寻路。
/// 首次使用时自动创建，根据场景中的 Tilemap 计算地图范围，
/// 通过 Physics2D 采样判断每个格子是否可通行（障碍 = 有碰撞体且不是玩家）。
/// 供 AutoMoving 状态的玩家跟随目标时使用。
/// </summary>
[DefaultExecutionOrder(-100)]
public class PathfindingManager : MonoBehaviour
{
    private static PathfindingManager _instance;
    public static PathfindingManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<PathfindingManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("[PathfindingManager]");
                    _instance = go.AddComponent<PathfindingManager>();
                }
            }
            return _instance;
        }
    }

    [Header("Grid")]
    [Tooltip("格子边长（世界单位），越小越精细但越慢")]
    [SerializeField] private float cellSize = 0.25f;
    [Tooltip("障碍物所在层（默认 Default）")]
    [SerializeField] private LayerMask obstacleMask = 1;
    [Tooltip("是否根据场景中的 Tilemap 自动计算地图范围")]
    [SerializeField] private bool autoBounds = true;
    [Tooltip("手动指定地图范围（关闭 autoBounds 时生效）")]
    [SerializeField] private Bounds bounds;

    private bool[] walkable;
    private int width;
    private int height;
    private Vector2 origin;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        BuildGrid();
    }

    public void BuildGrid()
    {
        Bounds b = bounds;
        if (autoBounds)
            b = GetMapBounds();

        if (b.size == Vector3.zero)
        {
            Debug.LogWarning("[PathfindingManager] 地图范围为空，无法构建寻路网格。");
            walkable = null;
            width = height = 0;
            return;
        }

        b.Expand(cellSize); // 四周留一圈边距

        origin = new Vector2(b.min.x, b.min.y);
        width = Mathf.CeilToInt(b.size.x / cellSize);
        height = Mathf.CeilToInt(b.size.y / cellSize);

        walkable = new bool[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                walkable[y * width + x] = IsCellWalkable(CellCenterToWorld(new Vector2Int(x, y)));
            }
        }
    }

    /// <summary>计算从 start 到 target 的路径（世界坐标），不可达时返回空列表。</summary>
    public List<Vector2> FindPath(Vector2 start, Vector2 target)
    {
        if (walkable == null || width == 0)
            BuildGrid();

        if (walkable == null || width == 0)
            return new List<Vector2>();

        Vector2Int s = WorldToCell(start);
        Vector2Int t = WorldToCell(target);

        if (!IsCellInBounds(s)) s = ClampCell(s);
        if (!IsCellInBounds(t)) t = ClampCell(t);

        if (!walkable[s.y * width + s.x]) s = NearestWalkable(s);
        if (!walkable[t.y * width + t.x]) t = NearestWalkable(t);

        List<Vector2Int> cells = ComputePath(s, t);
        if (cells.Count == 0)
            return new List<Vector2>();

        List<Vector2> waypoints = new List<Vector2>(cells.Count);
        for (int i = 0; i < cells.Count; i++)
            waypoints.Add(CellCenterToWorld(cells[i]));
        waypoints[waypoints.Count - 1] = target; // 终点精确到目标位置

        return SmoothPath(waypoints, start);
    }

    /// <summary>判断两点之间是否可直线通过（无障碍物遮挡）。</summary>
    public bool HasLineOfSight(Vector2 a, Vector2 b)
    {
        float dist = Vector2.Distance(a, b);
        float step = cellSize * 0.5f;
        int samples = Mathf.Max(1, Mathf.CeilToInt(dist / step));
        for (int i = 0; i <= samples; i++)
        {
            if (!IsWalkablePoint(Vector2.Lerp(a, b, (float)i / samples)))
                return false;
        }
        return true;
    }

    // ---- 内部实现 ----

    private Bounds GetMapBounds()
    {
        Bounds result = new Bounds();
        bool first = true;

        Tilemap[] tilemaps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
        foreach (Tilemap tm in tilemaps)
        {
            BoundsInt cb = tm.cellBounds;
            for (int x = cb.xMin; x < cb.xMax; x++)
            {
                for (int y = cb.yMin; y < cb.yMax; y++)
                {
                    if (!tm.HasTile(new Vector3Int(x, y, 0)))
                        continue;

                    Vector3 w = tm.GetCellCenterWorld(new Vector3Int(x, y, 0));
                    if (first)
                    {
                        result = new Bounds(w, Vector3.zero);
                        first = false;
                    }
                    else
                    {
                        result.Encapsulate(w);
                    }
                }
            }
        }

        if (first)
            Debug.LogWarning("[PathfindingManager] 场景中没有 Tilemap，无法自动计算地图范围，请手动设置 bounds 或关闭 autoBounds。");

        return result;
    }

    private bool IsCellWalkable(Vector2 center)
    {
        float radius = cellSize * 0.5f - 0.001f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, obstacleMask);
        foreach (Collider2D c in hits)
        {
            if (c == null || c.isTrigger)
                continue;
            if (c.GetComponentInParent<PlayerManager>() != null)
                continue; // 忽略玩家自身
            return false;
        }
        return true;
    }

    private bool IsWalkablePoint(Vector2 point)
    {
        float radius = cellSize * 0.4f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(point, radius, obstacleMask);
        foreach (Collider2D c in hits)
        {
            if (c == null || c.isTrigger)
                continue;
            if (c.GetComponentInParent<PlayerManager>() != null)
                continue;
            return false;
        }
        return true;
    }

    private List<Vector2Int> ComputePath(Vector2Int start, Vector2Int target)
    {
        int size = width * height;
        float[] g = new float[size];
        int[] cameFrom = new int[size];
        bool[] closed = new bool[size];
        List<int> open = new List<int>();

        for (int i = 0; i < size; i++)
        {
            g[i] = Mathf.Infinity;
            cameFrom[i] = -1;
        }

        int sIdx = CellIndex(start);
        int tIdx = CellIndex(target);
        g[sIdx] = 0f;
        open.Add(sIdx);

        while (open.Count > 0)
        {
            // 线性查找 f 最小的节点（地图较小，足够快）
            int current = open[0];
            int currentPos = 0;
            for (int i = 1; i < open.Count; i++)
            {
                if (g[open[i]] < g[current])
                {
                    current = open[i];
                    currentPos = i;
                }
            }
            open.RemoveAt(currentPos);

            if (current == tIdx)
                break;

            closed[current] = true;

            int cx = current % width;
            int cy = current / width;

            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0)
                        continue;

                    int nx = cx + dx;
                    int ny = cy + dy;
                    if (nx < 0 || ny < 0 || nx >= width || ny >= height)
                        continue;

                    int nIdx = ny * width + nx;
                    if (closed[nIdx] || !walkable[nIdx])
                        continue;

                    // 防止斜向穿过墙角
                    if (dx != 0 && dy != 0)
                    {
                        if (!walkable[cy * width + nx] || !walkable[ny * width + cx])
                            continue;
                    }

                    float stepCost = (dx != 0 && dy != 0) ? 1.41421356f : 1f;
                    float newG = g[current] + stepCost;
                    if (newG < g[nIdx])
                    {
                        g[nIdx] = newG;
                        cameFrom[nIdx] = current;
                        if (!open.Contains(nIdx))
                            open.Add(nIdx);
                    }
                }
            }
        }

        if (g[tIdx] == Mathf.Infinity)
            return new List<Vector2Int>();

        List<Vector2Int> cells = new List<Vector2Int>();
        int idx = tIdx;
        while (idx != -1)
        {
            cells.Add(new Vector2Int(idx % width, idx / width));
            idx = cameFrom[idx];
        }
        cells.Reverse();
        return cells;
    }

    /// <summary>弦式拉直：尽量用可见的直线连接路径点，减少折线。</summary>
    private List<Vector2> SmoothPath(List<Vector2> path, Vector2 startPos)
    {
        List<Vector2> result = new List<Vector2>();
        Vector2 anchor = startPos;
        int i = 0;

        while (i < path.Count)
        {
            int furthest = i;
            for (int j = path.Count - 1; j >= i; j--)
            {
                if (HasLineOfSight(anchor, path[j]))
                {
                    furthest = j;
                    break;
                }
            }

            result.Add(path[furthest]);
            anchor = path[furthest];
            i = furthest + 1;
        }

        return result;
    }

    private Vector2Int WorldToCell(Vector2 world)
    {
        return new Vector2Int(
            Mathf.FloorToInt((world.x - origin.x) / cellSize),
            Mathf.FloorToInt((world.y - origin.y) / cellSize));
    }

    private Vector2 CellCenterToWorld(Vector2Int cell)
    {
        return new Vector2(
            origin.x + (cell.x + 0.5f) * cellSize,
            origin.y + (cell.y + 0.5f) * cellSize);
    }

    private int CellIndex(Vector2Int cell) => cell.y * width + cell.x;

    private bool IsCellInBounds(Vector2Int cell) =>
        cell.x >= 0 && cell.y >= 0 && cell.x < width && cell.y < height;

    private Vector2Int ClampCell(Vector2Int cell) =>
        new Vector2Int(Mathf.Clamp(cell.x, 0, width - 1), Mathf.Clamp(cell.y, 0, height - 1));

    private Vector2Int NearestWalkable(Vector2Int cell)
    {
        if (IsCellInBounds(cell) && walkable[CellIndex(cell)])
            return cell;

        int maxRadius = Mathf.Max(width, height);
        for (int r = 1; r <= maxRadius; r++)
        {
            for (int dy = -r; dy <= r; dy++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r)
                        continue;

                    Vector2Int c = new Vector2Int(cell.x + dx, cell.y + dy);
                    if (IsCellInBounds(c) && walkable[CellIndex(c)])
                        return c;
                }
            }
        }
        return cell;
    }

    private void OnDrawGizmosSelected()
    {
        if (walkable == null || width == 0)
            return;

        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        Vector2 half = Vector2.one * (cellSize * 0.5f);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (!walkable[y * width + x])
                    Gizmos.DrawWireCube(CellCenterToWorld(new Vector2Int(x, y)), half * 2f);
            }
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(
            origin + new Vector2(width, height) * cellSize * 0.5f,
            new Vector2(width, height) * cellSize);
    }
}
