using System.Collections.Generic;
using UnityEngine;

public enum NPCType
{
    None,
    Shop1,
    Shop2,
    Shop3,
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

    private void Awake()
    {
        guardPoint = transform.position;

        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
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
