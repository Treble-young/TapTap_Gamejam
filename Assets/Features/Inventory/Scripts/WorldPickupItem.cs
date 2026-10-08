using UnityEngine;

public class WorldPickupItem : MonoBehaviour
{
    [SerializeField] private InventoryItemData itemData;
    [HideInInspector] public InventoryItemData ItemData;
    [SerializeField] private int itemCount = 1;

    void Awake()
    {
        ItemData = Instantiate(itemData);
        ItemData.itemCount = itemCount;
    }
}
