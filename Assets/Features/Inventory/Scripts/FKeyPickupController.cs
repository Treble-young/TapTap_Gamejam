using UnityEngine;
using UnityEngine.InputSystem;

public class FKeyPickupController : MonoBehaviour
{
    [SerializeField] private HotbarInventory inventory;
    [SerializeField] private float pickupRadius = 1.5f;

    private InputAction pickupAction;

    private void Awake()
    {
        pickupAction = new InputAction(
            "Pickup",
            InputActionType.Button,
            "<Keyboard>/f"
        );
    }

    private void OnEnable()
    {
        pickupAction.Enable();
    }

    private void OnDisable()
    {
        pickupAction.Disable();
    }

    private void OnDestroy()
    {
        pickupAction.Dispose();
    }

    private void Update()
    {
        if (pickupAction.WasPressedThisFrame())
            TryPickUp();
    }

    private void TryPickUp()
    {
        if (inventory == null || PlayerSelector.Instance == null)
            return;

        PlayerManager player = PlayerSelector.Instance.currentPlayer;
        if (player == null) return;

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