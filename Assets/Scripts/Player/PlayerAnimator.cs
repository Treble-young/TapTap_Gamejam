using UnityEngine;

/// <summary>
/// 玩家四方向行走 / 站立动画驱动。
/// 直接按状态名 <c>Animator.Play</c> 切状态（walk_up/down/left/right、idle_up/down/left/right），
/// 所以不需要在 Animator 里加参数和过渡线。
///
/// hasAnimator = true  → 由本脚本驱动 Animator，同时关掉 NPCSpriteRandomSelector 的随机贴图；
/// hasAnimator = false → 直接禁用 Animator，退回 NPCSpriteRandomSelector 的随机贴图逻辑。
/// </summary>
public class PlayerAnimator : MonoBehaviour
{
    [Tooltip("勾上=用四方向动画；取消=用 NPCSpriteRandomSelector 的随机贴图")]
    public bool hasAnimator = false;

    [Header("引用（留空会自动查找）")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D body;

    [Header("判定")]
    [Tooltip("速度低于这个值当作站着不动（吸收刚体残留的微小速度）")]
    [SerializeField] private float moveThreshold = 0.05f;

    private enum Facing
    {
        Down,
        Up,
        Left,
        Right
    }

    private Facing facing = Facing.Down;
    private int currentStateHash;
    private bool stateApplied;
    private PlayerManager playerManager;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        if (body == null)
            body = GetComponentInParent<Rigidbody2D>();

        // 没有 Animator 就只能走随机贴图那套
        if (animator == null)
            hasAnimator = false;

        ApplySource();
    }

    /// <summary>
    /// 动画和随机贴图是二选一：
    /// 用动画时把 NPCSpriteRandomSelector 关掉（否则 Start 里的随机贴图会先覆盖一次）；
    /// 不用动画时把 Animator 整个禁用（否则它的默认状态会一直写 sprite，随机贴图根本显示不出来）。
    /// </summary>
    private void ApplySource()
    {
        if (animator != null)
            animator.enabled = hasAnimator;

        PlayerManager playerManager = GetComponent<PlayerManager>();
        if (playerManager != null)
            playerManager.EnableThisSelector = !hasAnimator;
    }

    /// <summary>运行时切换动画 / 随机贴图。</summary>
    public void SetHasAnimator(bool value)
    {
        hasAnimator = value;
        ApplySource();
    }

    private void Update()
    {
        if (!hasAnimator || animator == null || !animator.isActiveAndEnabled)
            return;

        // 控制器没挂上时 Play 会刷警告，先挡掉
        if (animator.runtimeAnimatorController == null)
            return;

        Vector2 velocity = body != null ? body.linearVelocity : Vector2.zero;
        bool moving = velocity.sqrMagnitude > moveThreshold * moveThreshold;

        // 朝向单独判：按住方向键时即使被墙挡住没速度，也会立刻转向那个方向
        Vector2 direction = GetFacingSource(velocity);
        if (direction.sqrMagnitude > moveThreshold * moveThreshold)
            facing = GetFacing(direction);

        ApplyState(moving ? "walk_" : "idle_");
    }

    /// <summary>
    /// 朝向的参考方向：
    /// 操控中优先用「输入方向」——这样按住上/下时哪怕贴墙没速度也仍然朝上/下；
    /// 自动跟随、陌生人让路这类非操控状态用实际速度。
    /// </summary>
    private Vector2 GetFacingSource(Vector2 velocity)
    {
        if (playerManager == null)
            playerManager = GetComponent<PlayerManager>();

        if (playerManager != null && playerManager.playerState == PlayerState.InputControlling
            && PlayerInputManager.Instance != null)
        {
            Vector2 input = PlayerInputManager.Instance.MovementInput;
            if (input.sqrMagnitude > 0.0001f)
                return input;
        }

        return velocity;
    }

    /// <summary>
    /// 上下优先：只要垂直分量不是几乎为 0（或远小于水平分量），就播上/下；
    /// 完全没有上下方向时才播左/右。
    /// </summary>
    private static Facing GetFacing(Vector2 direction)
    {
        if (Mathf.Abs(direction.y) > Mathf.Abs(direction.x) * 0.25f)
            return direction.y >= 0f ? Facing.Up : Facing.Down;

        return direction.x >= 0f ? Facing.Right : Facing.Left;
    }

    private void ApplyState(string prefix)
    {
        int hash = Animator.StringToHash(prefix + ToSuffix(facing));

        // 状态没变就不要重复 Play，否则动画每帧都会被重置到第一帧
        if (stateApplied && hash == currentStateHash)
            return;

        currentStateHash = hash;
        stateApplied = true;

        animator.Play(hash, 0, 0f);
    }

    private static string ToSuffix(Facing value)
    {
        switch (value)
        {
            case Facing.Up:
                return "up";
            case Facing.Left:
                return "left";
            case Facing.Right:
                return "right";
            default:
                return "down";
        }
    }
}
