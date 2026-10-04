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

    /// <summary>由 PlayerInputManager 的 Pickup 按键触发</summary>
    public void TryPickUp()
    {
        if (PlayerSelector.Instance == null)
            return;

        PlayerManager player = PlayerSelector.Instance.currentPlayer;
        if (player == null) return;

        PlayerInventoryManager inventory = player.playerInventory;
        if (inventory == null) return;

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

        if (nearest == null) return;

        if (inventory.TryAdd(nearest.ItemData))
            Destroy(nearest.gameObject);
    }
}
