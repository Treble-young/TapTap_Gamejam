using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("手动指定跟随目标；留空则自动跟随 PlayerSelector 当前操控的角色")]
    [SerializeField] private Transform target;
    [Tooltip("角色切换器（可选），用于自动跟随当前操控的角色")]
    [SerializeField] private PlayerSelector playerSelector;

    [Header("Smoothing")]
    [Tooltip("正常跟随的平滑时间：值越小跟随越紧、越灵敏")]
    [SerializeField] private float followSmoothTime = 0.1f;
    [Tooltip("切换目标时的平滑时间：值越大过渡越慢")]
    [SerializeField] private float switchSmoothTime = 0.5f;
    [Tooltip("距离阈值：相机与目标距离大于该值时视为切换过渡，使用较慢的切换平滑")]
    [SerializeField] private float switchDistanceThreshold = 2f;
    [Tooltip("相机最大跟随速度，防止切换目标时移动过快")]
    [SerializeField] private float maxFollowSpeed = 100f;

    [Header("Bounds")]
    [Tooltip("自动从场景中的 CameraRange 获取，无需手动设置")]
    [SerializeField] private Collider2D bounds;
    [Tooltip("true：整个视口都保持在区域内；false：仅限制相机中心点")]
    [SerializeField] private bool clampToViewport = true;

    [Header("Target On Screen")]
    [Tooltip("是否保证跟随目标始终留在画面内")]
    [SerializeField] private bool keepTargetOnScreen = true;
    [Tooltip("目标距画面边缘的安全边距（占半屏的比例 0~1）")]
    [Range(0f, 1f)] [SerializeField] private float targetEdgeMargin = 0.05f;

    [Header("Offset")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

    private Camera _camera;
    private Vector3 _velocity;
    private int _lastTargetID = -1;

    void Awake()
    {
        if (_camera == null)
            _camera = GetComponent<Camera>();
        if (playerSelector == null)
            playerSelector = PlayerSelector.Instance;

        SceneManager.sceneLoaded += OnSceneLoaded;
        FindCameraRange();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void LateUpdate()
    {
        ResolveAutoTarget();
        Follow();
    }

    /// <summary>切换跟随目标。传入 null 表示取消跟随。</summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    /// <summary>按玩家 ID 切换跟随目标。</summary>
    public void SetTarget(int playerID)
    {
        SetTarget(GetPlayerTransform(playerID));
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindCameraRange();
    }

    /// <summary>在当前场景中查找 CameraRange，并用其碰撞体作为边界。</summary>
    private void FindCameraRange()
    {
        CameraRange range = FindFirstObjectByType<CameraRange>();
        if (range == null)
        {
            bounds = null;
            Debug.LogWarning("CameraFollow：场景中未找到 CameraRange，相机将不受范围限制。");
            return;
        }

        // 兼容碰撞体挂在 CameraRange 自身或子物体上的情况
        bounds = range.GetComponentInChildren<Collider2D>();
        if (bounds == null)
            Debug.LogWarning("CameraFollow：CameraRange 及其子物体上都没有 Collider2D，相机将不受范围限制。");
    }

    private void ResolveAutoTarget()
    {
        // 手动指定了目标时，不进行自动跟随
        if (target != null || playerSelector == null)
            return;

        if (playerSelector.currentPlayerID == _lastTargetID)
            return;

        _lastTargetID = playerSelector.currentPlayerID;
        target = GetPlayerTransform(playerSelector.currentPlayerID);
    }

    private Transform GetPlayerTransform(int playerID)
    {
        if (playerSelector == null)
            return null;

        foreach (PlayerManager player in playerSelector.playerManagers)
        {
            if (player != null && player.playerID == playerID)
                return player.transform;
        }
        return null;
    }

    private void Follow()
    {
        if (target == null)
            return;

        Vector3 targetPosition = target.position + offset;

        // 距离较远（通常是刚切换目标）时用较慢的切换平滑，靠近后收紧为紧密跟随
        float distance = Vector3.Distance(transform.position, targetPosition);
        float smoothTime = distance > switchDistanceThreshold ? switchSmoothTime : followSmoothTime;

        Vector3 position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref _velocity,
            smoothTime,
            maxFollowSpeed,
            Time.deltaTime);

        // 正交相机的半屏尺寸（目标屏幕约束与范围限制共用）
        float halfHeight = _camera != null && _camera.orthographic ? _camera.orthographicSize : 0f;
        float halfWidth = halfHeight * (_camera != null ? _camera.aspect : 1f);

        // 先保证目标留在画面内，再限制相机不超出范围
        position = KeepTargetOnScreen(position, halfWidth, halfHeight);
        position = ClampToBounds(position, halfWidth, halfHeight);

        transform.position = position;
    }

    /// <summary>把相机位置调整到能保证目标留在画面内（带边距）。</summary>
    private Vector3 KeepTargetOnScreen(Vector3 position, float halfWidth, float halfHeight)
    {
        if (!keepTargetOnScreen || target == null || _camera == null || !_camera.orthographic)
            return position;

        float marginX = halfWidth * targetEdgeMargin;
        float marginY = halfHeight * targetEdgeMargin;

        float maxX = Mathf.Max(0f, halfWidth - marginX);
        float maxY = Mathf.Max(0f, halfHeight - marginY);

        Vector3 tp = target.position;

        // 目标超出安全区域时，把相机朝目标方向拉回，保证目标可见
        if (tp.x - position.x > maxX) position.x = tp.x - maxX;
        else if (position.x - tp.x > maxX) position.x = tp.x + maxX;

        if (tp.y - position.y > maxY) position.y = tp.y - maxY;
        else if (position.y - tp.y > maxY) position.y = tp.y + maxY;

        return position;
    }

    private Vector3 ClampToBounds(Vector3 position, float halfWidth, float halfHeight)
    {
        if (bounds == null || _camera == null)
            return position;

        // 透视相机或仅限制中心点时，直接夹住中心坐标
        if (!clampToViewport || !_camera.orthographic)
        {
            Bounds b = bounds.bounds;
            position.x = Mathf.Clamp(position.x, b.min.x, b.max.x);
            position.y = Mathf.Clamp(position.y, b.min.y, b.max.y);
            return position;
        }

        // 多边形碰撞体：按多边形形状限制，保证视口四角都在多边形内
        PolygonCollider2D poly = bounds as PolygonCollider2D;
        if (poly != null)
            return ClampToPolygon(position, poly, halfWidth, halfHeight);

        // 其他碰撞体（Box 等）：按包围盒限制
        return ClampToAABB(position, bounds.bounds, halfWidth, halfHeight);
    }

    /// <summary>把相机中心限制在包围盒内，保证视口不超出包围盒。</summary>
    private Vector3 ClampToAABB(Vector3 position, Bounds b, float halfWidth, float halfHeight)
    {
        float minX = b.min.x + halfWidth;
        float maxX = b.max.x - halfWidth;
        float minY = b.min.y + halfHeight;
        float maxY = b.max.y - halfHeight;

        // 区域比视口还小时，该轴居中显示
        if (minX > maxX)
        {
            float centerX = (b.min.x + b.max.x) * 0.5f;
            minX = maxX = centerX;
        }
        if (minY > maxY)
        {
            float centerY = (b.min.y + b.max.y) * 0.5f;
            minY = maxY = centerY;
        }

        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.y = Mathf.Clamp(position.y, minY, maxY);
        return position;
    }

    /// <summary>把相机中心限制在凸多边形内，保证视口四角都不超出多边形。多边形需为凸形。</summary>
    private Vector3 ClampToPolygon(Vector3 position, PolygonCollider2D poly, float halfWidth, float halfHeight)
    {
        Vector2[] local = poly.points;
        if (local == null || local.Length < 3)
            return ClampToAABB(position, poly.bounds, halfWidth, halfHeight);

        // 转为世界坐标顶点（points 为本地坐标，需加上 offset）
        Vector2[] pts = new Vector2[local.Length];
        Vector2 centroid = Vector2.zero;
        for (int i = 0; i < local.Length; i++)
        {
            pts[i] = poly.transform.TransformPoint(local[i] + poly.offset);
            centroid += pts[i];
        }
        centroid /= pts.Length;

        Vector2 center = new Vector2(position.x, position.y);

        // 先夹到包围盒内，作为合理的初值
        Bounds b = poly.bounds;
        center.x = Mathf.Clamp(center.x, b.min.x, b.max.x);
        center.y = Mathf.Clamp(center.y, b.min.y, b.max.y);

        // 凸多边形半平面投影：对每条边，把中心推到“视口四角都在边内侧”的位置
        for (int pass = 0; pass < 4; pass++)
        {
            for (int i = 0; i < pts.Length; i++)
            {
                Vector2 a = pts[i];
                Vector2 c = pts[(i + 1) % pts.Length];

                Vector2 edge = c - a;
                Vector2 n = new Vector2(edge.y, -edge.x);
                if (n.sqrMagnitude < 1e-8f)
                    continue; // 忽略退化边

                // 让法线指向多边形内部（以重心为内侧参考）
                if (Vector2.Dot(centroid - a, n) < 0f)
                    n = -n;
                n.Normalize();

                // 视口矩形完全在多边形内时，中心需离边至少 inset 的距离
                float inset = halfWidth * Mathf.Abs(n.x) + halfHeight * Mathf.Abs(n.y);
                float dist = Vector2.Dot(center - a, n);
                if (dist < inset)
                    center += n * (inset - dist);
            }
        }

        position.x = center.x;
        position.y = center.y;
        return position;
    }
}
