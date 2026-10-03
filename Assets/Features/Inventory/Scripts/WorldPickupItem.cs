using UnityEngine;

public class WorldPickupItem : MonoBehaviour
{
    [SerializeField] private InventoryItemData itemData;

    public InventoryItemData ItemData => itemData;
}
