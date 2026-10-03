using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Game/Inventory Item")]
public class InventoryItemData : ScriptableObject
{
    [SerializeField] private string itemName;
    [SerializeField] private Sprite icon;

    public string ItemName => itemName;
    public Sprite Icon => icon;
}
