using UnityEngine;

public class WorldPickUpItemInteractableManager : InteractableManager
{
    private WorldPickupItem item;

    public override void Awake()
    {
        base.Awake();
        item = GetComponent<WorldPickupItem>();
    }

    public override void Interact(PlayerManager player)
    {
        if (PlayerSelector.Instance == null)
            return;
        if (player == null) return;

        PlayerInventoryManager inventory = player.playerInventory;
        if (inventory == null) return;

        if (item != null && item.ItemData != null)
        {
            if (inventory.TryAdd(item.ItemData))
            {
                Destroy(item.gameObject);
            }
        }
    }
}
