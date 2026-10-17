using UnityEngine;

/// <summary>
/// @trace LVL-203
/// @trace LVL-204
/// 第二关荒地 NPC（占位表现，美术后补）：
/// 默认循环——从居所走到芦苇丛水潭边（电线垂落入水，水中有电），
/// 触电倒下、沉入水中，一段时间后重新出现，周而复始。
///
/// 当芦苇丛的「禁」字警示牌立起（Level02ReedBed.IsRestored）后：
/// NPC 改为走向警示牌，看到牌子时头顶出现感叹号，
/// 随后改变路径走向安全的方向，直至走出地图消失。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Level02PoolNPC : MonoBehaviour
{
    private enum State { Idle, ToPool, Shocked, Sinking, Waiting, ToSign, Reacting, Exiting, Gone, Gathering }

    [Header("角色设定")]
    [Tooltip("勾选：参与触电循环的受害者；不勾选：定居者，原地等待聚集")]
    [SerializeField] private bool poolVictim = true;
    [Tooltip("是否显示占位圆点（美术未到位前默认隐藏，靠行为与感叹号标识）")]
    [SerializeField] private bool showPlaceholder = false;

    [Header("关键点位（留空取自身出生点为居所）")]
    [SerializeField] private Transform poolEdge;
    [SerializeField] private Transform warningSignPoint;
    [SerializeField] private Transform exitPoint;

    [Header("状态来源")]
    [SerializeField] private Level02ReedBed reedBed;

    [Header("节奏参数")]
    [SerializeField] private float moveSpeed = 1.2f;
    [SerializeField] private float arriveDistance = 0.15f;
    [SerializeField] private float shockDuration = 0.8f;
    [SerializeField] private float sinkDuration = 1.2f;
    [SerializeField] private float respawnDelay = 3f;
    [SerializeField] private float reactDuration = 1.5f;

    [Header("触电演出")]
    [SerializeField] private Color shockColor = new Color(1f, 0.95f, 0.4f);

    private SpriteRenderer sr;
    private HeadIndicator headIndicator;
    private Vector3 homePoint;
    private State state = State.ToPool;
    private float stateTimer;
    private bool safeModeSwitched;
    private Vector3 gatherTarget;
    private System.Action onGathered;

    public bool IsGone => state == State.Gone;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr.sprite == null)
            sr.sprite = PlaceholderSprite.CreateCircle(new Color(0.85f, 0.75f, 0.6f));
        homePoint = transform.position;

        headIndicator = GetComponent<HeadIndicator>();
        if (headIndicator == null)
            headIndicator = gameObject.AddComponent<HeadIndicator>();

        if (!poolVictim)
            state = State.Idle;

        sr.enabled = showPlaceholder; // 占位圆点默认隐藏
    }

    private void Update()
    {
        // 「禁」字归位后切换到安全路径（只切换一次）
        if (poolVictim && !safeModeSwitched && reedBed != null && reedBed.IsRestored
            && state != State.Gone && state != State.Gathering)
        {
            safeModeSwitched = true;
            if (showPlaceholder) sr.enabled = true;
            SetAlpha(1f);
            transform.rotation = Quaternion.identity;
            ChangeState(State.ToSign);
        }

        switch (state)
        {
            case State.ToPool:
                if (MoveTowards(poolEdge != null ? poolEdge.position : homePoint))
                    ChangeState(State.Shocked);
                break;

            case State.Shocked:
                // 触电：高频闪烁 + 倒地
                float flash = Mathf.PingPong(Time.time * 12f, 1f);
                sr.color = Color.Lerp(Color.white, shockColor, flash);
                transform.rotation = Quaternion.Euler(0f, 0f, -75f * (1f - stateTimer / shockDuration));
                if (TickStateTimer())
                    ChangeState(State.Sinking);
                break;

            case State.Sinking:
                // 沉入水中：下沉并淡出
                transform.position += Vector3.down * (0.4f * Time.deltaTime);
                SetAlpha(Mathf.Clamp01(stateTimer / sinkDuration));
                if (TickStateTimer())
                    ChangeState(State.Waiting);
                break;

            case State.Waiting:
                if (TickStateTimer())
                {
                    // 重新出现在居所，开始下一轮
                    transform.position = homePoint;
                    transform.rotation = Quaternion.identity;
                    sr.color = Color.white;
                    SetAlpha(1f);
                    if (showPlaceholder) sr.enabled = true;
                    ChangeState(State.ToPool);
                }
                break;

            case State.ToSign:
                if (MoveTowards(warningSignPoint != null ? warningSignPoint.position : transform.position))
                {
                    headIndicator.Show(); // 看到警示牌：头顶感叹号
                    ChangeState(State.Reacting);
                }
                break;

            case State.Reacting:
                if (TickStateTimer())
                {
                    headIndicator.Hide();
                    ChangeState(State.Exiting);
                }
                break;

            case State.Exiting:
                if (MoveTowards(exitPoint != null ? exitPoint.position : transform.position + Vector3.right * 20f))
                {
                    state = State.Gone;
                    gameObject.SetActive(false); // 走出地图，消失
                }
                break;

            case State.Gathering:
                if (MoveTowards(gatherTarget))
                {
                    state = State.Gone;
                    System.Action cb = onGathered;
                    onGathered = null;
                    gameObject.SetActive(false);
                    cb?.Invoke();
                }
                break;
        }
    }

    /// <summary>双修复后被导演调用：中断当前循环，走向水井聚集点，到达后消失并回调。</summary>
    public void WalkToGatherPoint(Vector3 point, System.Action onArrived)
    {
        if (state == State.Gone || state == State.Gathering)
        {
            onArrived?.Invoke();
            return;
        }
        sr.color = Color.white;
        SetAlpha(1f);
        if (showPlaceholder) sr.enabled = true;
        transform.rotation = Quaternion.identity;
        headIndicator.Hide();
        gatherTarget = point;
        onGathered = onArrived;
        state = State.Gathering;
        stateTimer = 0f;
    }

    private bool MoveTowards(Vector3 target)
    {
        Vector3 pos = transform.position;
        Vector3 next = Vector3.MoveTowards(pos, target, moveSpeed * Time.deltaTime);
        transform.position = next;
        return Vector3.Distance(next, target) <= arriveDistance;
    }

    private void ChangeState(State next)
    {
        state = next;
        switch (next)
        {
            case State.Shocked: stateTimer = shockDuration; break;
            case State.Sinking: stateTimer = sinkDuration; break;
            case State.Waiting: stateTimer = respawnDelay; sr.enabled = false; break;
            case State.Reacting: stateTimer = reactDuration; break;
        }
    }

    private bool TickStateTimer()
    {
        stateTimer -= Time.deltaTime;
        return stateTimer <= 0f;
    }

    private void SetAlpha(float a)
    {
        Color c = sr.color;
        c.a = a;
        sr.color = c;
    }
}
