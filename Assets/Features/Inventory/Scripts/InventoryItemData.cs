using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Game/Inventory Item")]
public class InventoryItemData : ScriptableObject
{
    [SerializeField] protected string itemName;
    [SerializeField] protected Sprite icon;

    // 运行时的使用状态，不会覆盖序列化的默认图标
    private Sprite usedIcon;
    private bool isUsed;

    public string ItemName => itemName;
    public Sprite Icon => isUsed && usedIcon != null ? usedIcon : icon;

    public virtual void Use(PlayerManager player)
    {
        Debug.Log($"Using item: {itemName}");

    }

    public void SetIcon(Sprite newIcon)
    {
        icon = newIcon;
    }

    public void ToggleIcon(Sprite newUsedIcon)
    {
        usedIcon = newUsedIcon;
        isUsed = !isUsed;
    }
}
