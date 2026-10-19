using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 不可进入区域：只和主控角色（PlayerType.Main）发生实体碰撞 —— 直接挡住、进不去，不再把人挤回去。
/// 其他玩家（Class01 / Class03）、NPC、车辆等一律互相忽略碰撞，完全不受影响。
/// 挂在带有 Collider2D（Layer = CantArrive）的物体上；Collider 会在 Awake 里被设为非触发器。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CantArriveArea : MonoBehaviour
{
    [Header("LEVEL 03 NPC 需求")]
    public bool level03FunctionNeedsNPC = false;
    public List<Level03_NPC_Controller> requiredNPCs = new List<Level03_NPC_Controller>();

    [Header("LEVEL 03 FLAG PLATFORM 需求")]
    public bool level03FunctionNeedsFlagPlatform = false;

    [TextArea(1, 3)]
    [SerializeField] private string flagNeededMessage = "这里太多人了，我需要有旗帜他们才会让路。";

    private Collider2D areaCollider;

    private PlayerManager mainPlayer;
    private Collider2D[] mainColliders = new Collider2D[0];

    // 当前是否对 Main 生效实体碰撞
    private bool blocking;

    private float nextCheckTime;

    private void Awake()
    {
        areaCollider = GetComponent<Collider2D>();

        // 实体碰撞（不是触发器）：走过去会被直接挡住
        areaCollider.isTrigger = false;
    }

    private void Start()
    {
        ReinitializeCollision();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // sceneLoaded 触发时新场景的对象刚 Awake 完，等一帧让它们的 Start 也跑完再重建碰撞关系
        StartCoroutine(ReinitializeNextFrame());
    }

    private IEnumerator ReinitializeNextFrame()
    {
        yield return null;

        ReinitializeCollision();
    }

    /// <summary>
    /// 重建「只和 Main 碰撞」的关系。
    /// Physics2D.IgnoreCollision 是按**碰撞体实例**记录的，不随场景保存：
    /// LoadScene 之后旧玩家的碰撞体被销毁、新玩家的碰撞体从没被忽略过，
    /// 不重新跑一遍就会要么挡不住 Main、要么把不该挡的物体也挡住。
    /// </summary>
    public void ReinitializeCollision()
    {
        ResolveMainPlayer();

        // 即使暂时没有 Main 也要扫描：先把当前所有非 Main 刚体忽略掉
        IgnoreEverythingButMain();

        SetBlocking(ShouldBlock() && !IsMainInside());
    }

    private void Update()
    {
        // 0.1 秒检查一次就够了：OverlapPoint 每帧查一遍太浪费，肉眼也看不出这点延迟
        if (Time.time < nextCheckTime)
            return;

        nextCheckTime = Time.time + 0.1f;

        // 玩家可能比本区域晚生成，也可能在场景切换 / 重生后被重建
        if (mainPlayer == null || !CollidersValid())
        {
            ReinitializeCollision();

            if (mainPlayer == null)
                return;
        }

        if (!ShouldBlock())
        {
            if (blocking)
                SetBlocking(false);

            return;
        }

        // 只在 Main 不在区域里时才“立墙”，避免把实体碰撞打开在角色身上、把人弹飞
        if (!blocking && !IsMainInside())
            SetBlocking(true);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        PlayerManager player = collision.collider.GetComponentInParent<PlayerManager>();

        // 运行时才出现、Start 没扫到的非 Main 物体：立刻互相忽略，保证“其他物体不受影响”
        if (player == null || player.playerType != PlayerType.Main)
        {
            Physics2D.IgnoreCollision(areaCollider, collision.collider, true);
            return;
        }

        if (level03FunctionNeedsFlagPlatform && !PlayerInventoryManager.HasLevel03Flag)
            ShowMessage(flagNeededMessage);
    }

    /// <summary>当前是否应该挡住 Main。</summary>
    private bool ShouldBlock()
    {
        // 需要指定 NPC 在场：人不在区域里就放行
        if (level03FunctionNeedsNPC && !HasRequiredNPCInArea())
            return false;

        // 需要旗帜：拿到过旗帜（即使之后被消耗）就永久放行
        if (level03FunctionNeedsFlagPlatform && PlayerInventoryManager.HasLevel03Flag)
            return false;

        return true;
    }

    /// <summary>
    /// 开关「和 Main 的实体碰撞」。用 IgnoreCollision 而不是 collider.enabled：
    /// 关掉 collider 会连 OverlapPoint 一起失效，HasRequiredNPCInArea 就永远查不出 NPC，
    /// 墙也就再也立不起来了。
    /// </summary>
    private void SetBlocking(bool value)
    {
        blocking = value;

        if (mainPlayer == null)
            return;

        foreach (Collider2D collider in mainColliders)
        {
            if (collider != null)
                Physics2D.IgnoreCollision(areaCollider, collider, !value);
        }
    }

    private void ResolveMainPlayer()
    {
        mainPlayer = null;
        mainColliders = new Collider2D[0];

        PlayerSelector selector = PlayerSelector.Instance;
        if (selector != null)
        {
            foreach (PlayerManager player in selector.playerManagers)
            {
                if (player != null && player.playerType == PlayerType.Main)
                {
                    mainPlayer = player;
                    break;
                }
            }
        }

        if (mainPlayer == null)
        {
            foreach (PlayerManager player in FindObjectsByType<PlayerManager>(FindObjectsSortMode.None))
            {
                if (player != null && player.playerType == PlayerType.Main)
                {
                    mainPlayer = player;
                    break;
                }
            }
        }

        if (mainPlayer != null)
            mainColliders = mainPlayer.GetComponentsInChildren<Collider2D>(true);
    }

    /// <summary>除 Main 外，所有带刚体的物体（其他玩家 / NPC / 车辆…）都忽略和本区域的碰撞。</summary>
    private void IgnoreEverythingButMain()
    {
        foreach (Rigidbody2D body in FindObjectsByType<Rigidbody2D>(FindObjectsSortMode.None))
        {
            if (body == null)
                continue;

            PlayerManager owner = body.GetComponentInParent<PlayerManager>();
            if (owner != null && owner.playerType == PlayerType.Main)
                continue;

            foreach (Collider2D collider in body.GetComponentsInChildren<Collider2D>(true))
            {
                if (collider == null || collider == areaCollider)
                    continue;

                Physics2D.IgnoreCollision(areaCollider, collider, true);
            }
        }
    }

    /// <summary>缓存的 Main 碰撞体是否还有效（场景切换 / 角色重建后旧引用会变成已销毁对象）。</summary>
    private bool CollidersValid()
    {
        if (mainColliders == null || mainColliders.Length == 0)
            return false;

        foreach (Collider2D collider in mainColliders)
        {
            if (collider == null)
                return false;
        }

        return true;
    }

    private bool IsMainInside()
    {
        if (mainPlayer == null)
            return false;

        Rigidbody2D body = mainPlayer.GetComponent<Rigidbody2D>();
        Vector2 point = body != null ? body.position : (Vector2)mainPlayer.transform.position;

        return areaCollider.OverlapPoint(point);
    }

    /// <summary>
    /// 区域内是否至少有一个 requiredNPCs 里的 NPC（处于激活状态且位置在区域内）。
    /// </summary>
    private bool HasRequiredNPCInArea()
    {
        foreach (Level03_NPC_Controller npc in requiredNPCs)
        {
            if (npc == null || !npc.gameObject.activeInHierarchy)
                continue;

            if (areaCollider.OverlapPoint(npc.transform.position))
                return true;
        }

        return false;
    }

    private static void ShowMessage(string message)
    {
        if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.popUpManager != null)
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow(message);
    }
}
