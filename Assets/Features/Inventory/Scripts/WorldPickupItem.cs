using UnityEngine;

public class WorldPickupItem : MonoBehaviour
{
    [SerializeField] private InventoryItemData itemData;

    [HideInInspector] public InventoryItemData ItemData;

    void Awake()
    {
        ItemData = Instantiate(itemData);
    }
}
