using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 不可进入区域：角色踏入后稍作停留，就会被挤回进入前的原地。
/// 挂在带有 Polygon Collider 2D（Is Trigger）且 Layer 为 CantArrive 的物体上。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CantArriveArea : MonoBehaviour
{
    [Header("LEVEL 03 NPC 需求")]
    public bool level03FunctionNeedsNPC = false;
    public List<Level03_NPC_Controller> requiredNPCs = new List<Level03_NPC_Controller>();

    [Header("LEVEL 03 FLAG PLATFORM 需求")]
    public bool level03FunctionNeedsFlagPlatform = false;

    [Header("挤回设置")]
    [Tooltip("角色进入多久后开始挤回（秒）")]
    [SerializeField] private float pushBackDelay = 0.5f;
    [Tooltip("挤回的速度")]
    [SerializeField] private float pushBackSpeed = 4f;
    [Tooltip("挤回时额外往外多推一点，确保彻底离开触发区域")]
    [SerializeField] private float pushOutDistance = 0.5f;

    // 记录进入区域时的位置，作为“原地”推回目标
    private readonly Dictionary<PlayerManager, Vector2> entryPositions = new Dictionary<PlayerManager, Vector2>();
    private readonly Dictionary<PlayerManager, Coroutine> pushCoroutines = new Dictionary<PlayerManager, Coroutine>();

    private Collider2D areaCollider;

    private void Awake()
    {
        // 防御：确保作为触发区域使用
        areaCollider = GetComponent<Collider2D>();
        areaCollider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerManager player = other.GetComponentInParent<PlayerManager>();
        if (player == null || player.playerMovement == null)
            return;

        if (player.playerType != PlayerType.Main)
        {
            return;
        }

        if (level03FunctionNeedsNPC && !HasRequiredNPCInArea())
        {
            Destroy(gameObject);
            return;
        }

        // LEVEL 03：需要旗帜平台且尚未获得旗帜时，进入就弹出提示
        if (level03FunctionNeedsFlagPlatform && !PlayerInventoryManager.HasLevel03Flag
            && PlayerUIManager.Instance != null && PlayerUIManager.Instance.popUpManager != null)
        {
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("这里太多人了，我需要有旗帜他们才会让路。");
        }

        entryPositions[player] = other.attachedRigidbody != null
            ? other.attachedRigidbody.position
            : (Vector2)player.transform.position;

        if (pushCoroutines.TryGetValue(player, out Coroutine existing) && existing != null)
            StopCoroutine(existing);

        pushCoroutines[player] = StartCoroutine(PushBackRoutine(player));
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerManager player = other.GetComponentInParent<PlayerManager>();
        if (player == null)
            return;

        if (pushCoroutines.TryGetValue(player, out Coroutine existing) && existing != null)
            StopCoroutine(existing);

        pushCoroutines.Remove(player);
        entryPositions.Remove(player);
    }

    private IEnumerator PushBackRoutine(PlayerManager player)
    {
        yield return new WaitForSeconds(pushBackDelay);

        if (!entryPositions.TryGetValue(player, out Vector2 entry))
            yield break;

        // LEVEL 03：需要指定 NPC 在场时，区域内没有 NPC 就不挤回
        if (level03FunctionNeedsNPC && !HasRequiredNPCInArea())
        {
            pushCoroutines.Remove(player);
            entryPositions.Remove(player);
            Destroy(gameObject);
            yield break;
        }

        // LEVEL 03：需要旗帜平台时，一旦拥有过旗帜就不再挤回（即使之后消耗掉也保持）
        if (level03FunctionNeedsFlagPlatform && PlayerInventoryManager.HasLevel03Flag)
        {
            pushCoroutines.Remove(player);
            entryPositions.Remove(player);
            yield break;
        }

        player.playerMovement.PushTo(ComputePushTarget(entry), pushBackSpeed);

        // 推回动作交给 PlayerMovementManager 平滑完成，这里清空记录，允许后续再次进入
        pushCoroutines.Remove(player);
        entryPositions.Remove(player);
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

    private Vector2 ComputePushTarget(Vector2 entry)
    {
        Vector2 center = (Vector2)transform.position;
        Vector2 dir = entry - center;
        if (dir.sqrMagnitude < 0.0001f)
            dir = Vector2.up; // 角色正好在中心时随便挑个方向
        dir.Normalize();

        return entry + dir * pushOutDistance;
    }
}
