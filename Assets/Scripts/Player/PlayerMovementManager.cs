using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovementManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D bodyCollider;
    [SerializeField] private PlayerManager playerManager;

    [Header("Settings")]
    [SerializeField] PlayerSettings PlayerSettings;

    [Header("Stranger Avoid")]
    [Tooltip("是否让路避开操控的玩家（可运行时动态更改，false 则原地不动）")]
    public bool avoidPlayer = true;

    private Vector2 moveDirection;

    // Auto-follow 状态
    private List<Vector2> path;
    private int pathIndex;
    private float nextRepathTime;
    private Vector2 lastTargetPos;

    // Stranger 让路状态
    private enum StrangerState { Idle, Avoiding, Returning }
    private Vector2 homePosition;
    private Vector2 avoidPosition;
    private StrangerState strangerState = StrangerState.Idle;

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (bodyCollider == null) bodyCollider = GetComponent<Collider2D>();
        if (playerManager == null) playerManager = GetComponent<PlayerManager>();

        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (bodyCollider != null && bodyCollider.sharedMaterial == null)
        {
            bodyCollider.sharedMaterial = new PhysicsMaterial2D("Frictionless")
            {
                friction = 0f,
                bounciness = 0f
            };
        }
    }

    void Start()
    {
        homePosition = rb.position;
    }

    void Update()
    {
        HandleInput();
    }

    void FixedUpdate()
    {
        Movement();
    }

    private void HandleInput()
    {
        // Get input from PlayerInputManager
        if (playerManager.playerState != PlayerState.InputControlling)
        {
            moveDirection = Vector2.zero;
            return;
        }

        Vector2 input = PlayerInputManager.Instance != null
            ? PlayerInputManager.Instance.MovementInput
            : Vector2.zero;

        moveDirection = input.normalized;

        if (playerManager != null)
            playerManager.isMoving = moveDirection.sqrMagnitude > 0.001f;
    }

    private void Movement()
    {
        if (playerManager.playerState == PlayerState.Stranger)
            StrangerMovement();
        else if (playerManager.playerState == PlayerState.InputControlling)
            InputMovement();
        else if (playerManager.playerType == PlayerType.Class01)
            AutoFollowMovement();
        else
            rb.linearVelocity = Vector2.zero;
    }

    private void InputMovement()
    {
        Vector2 targetVelocity = moveDirection * PlayerSettings.MoveSpeed;

        float rate = moveDirection.sqrMagnitude > 0.001f ? PlayerSettings.Acceleration : PlayerSettings.Deceleration;
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, targetVelocity, rate * Time.fixedDeltaTime);
    }


    // ---- Stranger：玩家靠近时让开一小步，玩家离开后回到原处 ----

    private void StrangerMovement()
    {
        if (!avoidPlayer)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Transform target = PlayerSelector.Instance != null ? PlayerSelector.Instance.currentPlayer?.transform : null;
        if (target == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 myPos = rb.position;
        Vector2 targetPos = target.position;
        float distance = Vector2.Distance(myPos, targetPos);

        // 玩家点燃打火机时，陌生人立刻返回原位置
        if (PlayerSelector.Instance.currentPlayer.isUsingLighter)
        {
            strangerState = StrangerState.Returning;
        }
        // 状态切换（让路半径 < 回家半径，形成迟滞，避免在阈值附近来回抖）
        else
        {
            switch (strangerState)
            {
                case StrangerState.Idle:
                    if (distance <= PlayerSettings.StrangerAvoidRadius)
                        StartAvoiding(myPos, targetPos);
                    break;

                case StrangerState.Avoiding:
                    if (distance > PlayerSettings.StrangerReturnRadius)
                        strangerState = StrangerState.Returning;
                    break;

                case StrangerState.Returning:
                    if (distance <= PlayerSettings.StrangerAvoidRadius)
                        StartAvoiding(myPos, targetPos);
                    break;
            }
        }

        if (strangerState == StrangerState.Idle)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 targetPoint = strangerState == StrangerState.Returning ? homePosition : avoidPosition;
        float speed = strangerState == StrangerState.Returning ? PlayerSettings.StrangerReturnSpeed : PlayerSettings.StrangerAvoidSpeed;

        Vector2 dir = targetPoint - myPos;
        if (dir.magnitude <= PlayerSettings.StrangerReturnStopDistance)
        {
            rb.linearVelocity = Vector2.zero;
            if (strangerState == StrangerState.Returning)
                strangerState = StrangerState.Idle;
            return;
        }

        // 平滑逼近目标速度，避免速度瞬间跳变导致的抖动
        Vector2 targetVelocity = dir.normalized * speed;
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, targetVelocity, PlayerSettings.StrangerAcceleration * Time.fixedDeltaTime);
    }

    private void StartAvoiding(Vector2 myPos, Vector2 targetPos)
    {
        Vector2 away = myPos - targetPos;
        if (away.sqrMagnitude < 0.0001f)
            away = Vector2.right; // 和玩家重叠时随便挑个方向
        away.Normalize();

        avoidPosition = homePosition + away * PlayerSettings.StrangerAvoidDistance;
        strangerState = StrangerState.Avoiding;
    }


    // ---- Auto follow：使用寻路跟随当前操控的玩家 ----

    private void AutoFollowMovement()
    {
        Transform target = GetFollowTarget();
        if (target == null)
        {
            StopFollowing();
            return;
        }

        Vector2 targetPos = target.position;
        Vector2 myPos = rb.position;
        float distanceToTarget = Vector2.Distance(myPos, targetPos);

        // 足够近就停下，不再往里挤
        if (distanceToTarget <= PlayerSettings.FollowStopDistance)
        {
            StopFollowing();
            return;
        }

        bool targetMoved = Vector2.Distance(targetPos, lastTargetPos) > PlayerSettings.FollowRepathThreshold;
        if (path == null || targetMoved || Time.time >= nextRepathTime)
        {
            Repath(targetPos);
            nextRepathTime = Time.time + PlayerSettings.FollowRepathInterval;
            lastTargetPos = targetPos;
        }

        Vector2 dir = GetFollowDirection(myPos, targetPos);

        // 与其他跟随玩家保持距离，避免互相挤压
        Vector2 separation = GetSeparation(myPos);
        Vector2 steering = dir + separation * PlayerSettings.SeparationWeight;

        if (steering.sqrMagnitude < 0.001f)
        {
            StopFollowing();
            return;
        }
        steering.Normalize();

        // 靠近目标时缓缓减速
        float speed = PlayerSettings.FollowSpeed;
        if (distanceToTarget <= PlayerSettings.FollowSlowDistance)
        {
            float t = Mathf.InverseLerp(PlayerSettings.FollowStopDistance, PlayerSettings.FollowSlowDistance, distanceToTarget);
            speed = PlayerSettings.FollowSpeed * t;
        }

        Vector2 targetVelocity = steering * speed;
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, targetVelocity, PlayerSettings.Acceleration * Time.fixedDeltaTime);
        playerManager.isMoving = true;
    }

    /// <summary>远离其他跟随玩家（不含目标玩家和自身），避免挤成一团。</summary>
    private Vector2 GetSeparation(Vector2 myPos)
    {
        if (PlayerSelector.Instance == null)
            return Vector2.zero;

        PlayerManager current = PlayerSelector.Instance.currentPlayer;
        Vector2 separation = Vector2.zero;

        foreach (PlayerManager other in PlayerSelector.Instance.playerManagers)
        {
            if (other == null || other == playerManager || other == current)
                continue;

            Vector2 delta = myPos - (Vector2)other.transform.position;
            float dist = delta.magnitude;
            if (dist < 0.0001f || dist > PlayerSettings.SeparationRadius)
                continue;

            float strength = 1f - dist / PlayerSettings.SeparationRadius;
            separation += delta.normalized * strength;
        }

        return separation;
    }

    private void StopFollowing()
    {
        rb.linearVelocity = Vector2.zero;
        playerManager.isMoving = false;
        path = null;
    }

    private Transform GetFollowTarget()
    {
        if (PlayerSelector.Instance == null)
            return null;

        PlayerManager current = PlayerSelector.Instance.currentPlayer;
        if (current == null || current == playerManager)
            return null;

        return current.transform;
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
}
