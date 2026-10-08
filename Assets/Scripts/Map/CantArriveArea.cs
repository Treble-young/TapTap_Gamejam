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

    private void Awake()
    {
        // 防御：确保作为触发区域使用
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerManager player = other.GetComponentInParent<PlayerManager>();
        if (player == null || player.playerMovement == null)
            return;

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

        player.playerMovement.PushTo(ComputePushTarget(entry), pushBackSpeed);

        // 推回动作交给 PlayerMovementManager 平滑完成，这里清空记录，允许后续再次进入
        pushCoroutines.Remove(player);
        entryPositions.Remove(player);
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
