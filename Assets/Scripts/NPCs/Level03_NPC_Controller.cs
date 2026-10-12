using System.Collections.Generic;
using UnityEngine;

public enum NPCType
{
    None,
    Shop1,
    Shop2,
    Shop3,
    Road,
    Follow,
}

[RequireComponent(typeof(Rigidbody2D))]
public class Level03_NPC_Controller : MonoBehaviour
{
    public NPCType npcType = NPCType.None;

    [Header("Shopping Area Follow")]
    public ShoppingCart targetShoppingCart;
    public int minCansToFollow = 1;

    [Header("Follow")]
    [Tooltip("要跟随的目标，为空则原地不动")]
    public Vector3 guardPoint;
    public Transform followTarget;

    [Header("Road")]
    [Tooltip("场景中唯一的升旗台（留空则自动查找）")]
    public FlagRaisingPlatform flagRaisingPlatform;
    [Tooltip("在升旗台附近随机游走的半径")]
    public float roadWanderRadius = 3f;
    [Tooltip("全部到达后，跟随玩家多久后消失（秒）")]
    public float roadFollowDisappearDelay = 3f;
    [Tooltip("Road NPC 开始移动的最大延迟（分批错峰，减缓寻路压力）")]
    public float roadBatchDelay = 2f;

    [SerializeField] private float followSpeed = 2.5f;
    [SerializeField] private float followStopDistance = 0.6f;
    [SerializeField] private float followSlowDistance = 2f;
    [SerializeField] private float followAcceleration = 20f;
    [SerializeField] private float followRepathInterval = 0.4f;
    [SerializeField] private float followRepathThreshold = 0.4f;

    private Rigidbody2D rb;

    // Auto-follow 状态
    private List<Vector2> path;
    private int pathIndex;
    private float nextRepathTime;
    private Vector2 lastTargetPos;

    // Road 状态：当前随机目的地（null = 需要重新选）
    private Vector2? currentRoadTarget;
    private bool roadArrived = false;
    private float roadFollowStartTime = -1f;
    private float roadDelay = 0f;
    private float roadStartTime = -1f;
    private bool lastRaisingFlag = false;

    // Road NPC 全局统计（用于判断是否全部到达）
    private static readonly List<Level03_NPC_Controller> roadNPCs = new List<Level03_NPC_Controller>();

    private void Awake()
    {
        guardPoint = transform.position;

        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (flagRaisingPlatform == null)
            flagRaisingPlatform = FindFirstObjectByType<FlagRaisingPlatform>();
    }

    private void OnEnable()
    {
        if (npcType == NPCType.Road && !roadNPCs.Contains(this))
            roadNPCs.Add(this);
    }

    private void OnDisable()
    {
        if (npcType == NPCType.Road)
            roadNPCs.Remove(this);
    }

    private void FixedUpdate()
    {
        switch (npcType)
        {
            case NPCType.Follow:
                FollowMovement(followTarget);
                break;

            case NPCType.Shop1:
            case NPCType.Shop2:
            case NPCType.Shop3:
                if (targetShoppingCart != null && targetShoppingCart.spawnedCans.Count >= minCansToFollow)
                    FollowMovement(targetShoppingCart.transform);
                else
                    MoveToPoint(guardPoint);
                break;

            case NPCType.Road:
                RoadMovement();
                break;

            default:
                MoveToPoint(guardPoint);
                break;
        }
    }

    // ---- Auto follow ----

    public void MoveToPoint(Vector3 targetPoint)
    {
        Vector2 targetPos = targetPoint;
        Vector2 myPos = rb.position;
        float distanceToTarget = Vector2.Distance(myPos, targetPos);

        // 足够近就停下，不再往里挤
        if (distanceToTarget <= followStopDistance)
        {
            StopFollowing();
            return;
        }

        // 静态目标：只在目标变化或没有路径时寻路一次，不做周期性重寻路（省性能）
        bool targetMoved = Vector2.Distance(targetPos, lastTargetPos) > followRepathThreshold;
        if (path == null || targetMoved)
        {
            Repath(targetPos);
            lastTargetPos = targetPos;
        }

        Vector2 dir = GetFollowDirection(myPos, targetPos);
        if (dir.sqrMagnitude < 0.001f)
        {
            StopFollowing();
            return;
        }

        // 靠近目标时缓缓减速
        float speed = followSpeed;
        if (distanceToTarget <= followSlowDistance)
        {
            float t = Mathf.InverseLerp(followStopDistance, followSlowDistance, distanceToTarget);
            speed = followSpeed * t;
        }

        Vector2 targetVelocity = dir * speed;
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, targetVelocity, followAcceleration * Time.fixedDeltaTime);
    }

    private void FollowMovement(Transform target)
    {
        if (target == null)
        {
            StopFollowing();
            return;
        }

        Vector2 targetPos = target.position;
        Vector2 myPos = rb.position;
        float distanceToTarget = Vector2.Distance(myPos, targetPos);

        // 足够近就停下，不再往里挤
        if (distanceToTarget <= followStopDistance)
        {
            StopFollowing();
            return;
        }

        bool targetMoved = Vector2.Distance(targetPos, lastTargetPos) > followRepathThreshold;
        if (path == null || targetMoved || Time.time >= nextRepathTime)
        {
            Repath(targetPos);
            nextRepathTime = Time.time + followRepathInterval;
            lastTargetPos = targetPos;
        }

        Vector2 dir = GetFollowDirection(myPos, targetPos);
        if (dir.sqrMagnitude < 0.001f)
        {
            StopFollowing();
            return;
        }

        // 靠近目标时缓缓减速
        float speed = followSpeed;
        if (distanceToTarget <= followSlowDistance)
        {
            float t = Mathf.InverseLerp(followStopDistance, followSlowDistance, distanceToTarget);
            speed = followSpeed * t;
        }

        Vector2 targetVelocity = dir * speed;
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, targetVelocity, followAcceleration * Time.fixedDeltaTime);
    }

    // ---- Road：升旗时随机聚集到升旗台附近 ----

    private void RoadMovement()
    {
        if (flagRaisingPlatform == null)
        {
            MoveToPoint(guardPoint);
            return;
        }

        // 检测升旗开始，安排分批延迟（错峰寻路，减缓压力）
        if (flagRaisingPlatform.isRaisingFlag && !lastRaisingFlag)
        {
            roadDelay = Random.Range(0f, roadBatchDelay);
            roadStartTime = Time.time;
        }
        lastRaisingFlag = flagRaisingPlatform.isRaisingFlag;

        // 全部到达后：跟随玩家，然后消失
        if (AllRoadNPCsArrived())
        {
            RoadFollowAndDisappear();
            return;
        }

        if (!flagRaisingPlatform.isRaisingFlag)
        {
            // 没在升旗就回守卫点，并重置到达标记
            currentRoadTarget = null;
            roadArrived = false;
            roadFollowStartTime = -1f;
            MoveToPoint(guardPoint);
            return;
        }

        // 升旗中但还没轮到自己（分批错峰）：原地待命
        if (Time.time < roadStartTime + roadDelay)
        {
            MoveToPoint(guardPoint);
            return;
        }

        // 轮到：去升旗台附近随机点
        if (currentRoadTarget == null)
            currentRoadTarget = PickRandomRoadTarget();

        // 到达判定：到目标点，或（目标落在障碍物内不可达时）足够靠近升旗台
        if (!roadArrived)
        {
            bool reachedTarget = Vector2.Distance(rb.position, currentRoadTarget.Value) <= followStopDistance;
            bool nearPlatform = Vector2.Distance(rb.position, flagRaisingPlatform.transform.position) <= roadWanderRadius;
            roadArrived = reachedTarget || nearPlatform;
        }

        MoveToPoint(currentRoadTarget.Value);
    }

    private Vector2 PickRandomRoadTarget()
    {
        Vector2 center = flagRaisingPlatform.transform.position;
        return center + Random.insideUnitCircle * roadWanderRadius;
    }

    private bool AllRoadNPCsArrived()
    {
        if (roadNPCs.Count == 0)
            return false;

        foreach (Level03_NPC_Controller npc in roadNPCs)
        {
            if (npc == null || !npc.roadArrived)
                return false;
        }
        return true;
    }

    private void RoadFollowAndDisappear()
    {
        PlayerManager player = PlayerSelector.Instance != null ? PlayerSelector.Instance.currentPlayer : null;
        if (player == null)
            return;

        if (roadFollowStartTime < 0f)
        {
            roadFollowStartTime = Time.time;
            SignalClass03Players();
        }

        // 跟随玩家
        FollowMovement(player.transform);

        // 跟随一段时间后消失
        if (Time.time - roadFollowStartTime >= roadFollowDisappearDelay)
            gameObject.SetActive(false);
    }

    /// <summary>NPC 开始跟随玩家时，让 Class03 玩家从 Stranger 改为 AutoMoving。</summary>
    private static void SignalClass03Players()
    {
        if (PlayerSelector.Instance == null)
            return;

        foreach (PlayerManager p in PlayerSelector.Instance.playerManagers)
        {
            if (p != null && p.playerType == PlayerType.Class03)
                p.StartAutoMoving();
        }
    }

    private void Repath(Vector2 targetPos)
    {
        path = PathfindingManager.Instance.FindPath(rb.position, targetPos);
        pathIndex = 0;
    }

    private Vector2 GetFollowDirection(Vector2 myPos, Vector2 targetPos)
    {
        if (path == null || pathIndex >= path.Count)
        {
            Vector2 d = targetPos - myPos;
            return d.sqrMagnitude > 0.0001f ? d.normalized : Vector2.zero;
        }

        while (pathIndex < path.Count && Vector2.Distance(myPos, path[pathIndex]) <= 0.1f)
            pathIndex++;

        if (pathIndex >= path.Count)
        {
            Vector2 d = targetPos - myPos;
            return d.sqrMagnitude > 0.0001f ? d.normalized : Vector2.zero;
        }

        Vector2 dir = path[pathIndex] - myPos;
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.zero;
    }

    private void StopFollowing()
    {
        rb.linearVelocity = Vector2.zero;
        path = null;
    }
}
