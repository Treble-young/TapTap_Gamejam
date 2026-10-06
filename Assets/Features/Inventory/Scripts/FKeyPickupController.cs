using UnityEngine;

public class FKeyPickupController : MonoBehaviour
{
    public static FKeyPickupController Instance;

    [SerializeField] private float pickupRadius = 1.5f;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// 尝试拾取当前玩家附近最近的掉落物。
    /// 由 PlayerInputManager 的 E 键触发（拾取优先级最高）。
    /// </summary>
    /// <returns>是否成功拾取了物品。</returns>
    public bool TryPickUp()
    {
        if (PlayerSelector.Instance == null)
            return false;

        PlayerManager player = PlayerSelector.Instance.currentPlayer;
        if (player == null) return false;

        PlayerInventoryManager inventory = player.playerInventory;
        if (inventory == null) return false;

        Vector2 playerPosition = player.transform.position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            playerPosition, pickupRadius
        );

        WorldPickupItem nearest = null;
        float bestDistanceSquared = pickupRadius * pickupRadius;

        foreach (Collider2D hit in hits)
        {
            WorldPickupItem candidate =
                hit.GetComponentInParent<WorldPickupItem>();

            if (candidate == null || candidate.ItemData == null)
                continue;

            Vector2 itemPosition = candidate.transform.position;
            float distanceSquared =
                (itemPosition - playerPosition).sqrMagnitude;

            if (distanceSquared >= bestDistanceSquared)
                continue;

            bestDistanceSquared = distanceSquared;
            nearest = candidate;
        }

        if (nearest == null) return false;

        if (inventory.TryAdd(nearest.ItemData))
        {
            Destroy(nearest.gameObject);
            return true;
        }

        return false;
    }
}
