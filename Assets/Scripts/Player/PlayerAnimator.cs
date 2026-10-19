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

        if (moving)
            facing = GetFacing(velocity);

        ApplyState(moving ? "walk_" : "idle_");
    }

    /// <summary>按速度的主轴判定朝向：斜向移动时取分量更大的那一边。</summary>
    private static Facing GetFacing(Vector2 velocity)
    {
        if (Mathf.Abs(velocity.x) >= Mathf.Abs(velocity.y))
            return velocity.x >= 0f ? Facing.Right : Facing.Left;

        return velocity.y >= 0f ? Facing.Up : Facing.Down;
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
