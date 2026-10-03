using UnityEngine;
using UnityEngine.UI;
public class HotbarUI : MonoBehaviour
{
    [SerializeField] private HotbarInventory inventory;
    [SerializeField] private Image[] icons = new Image[9];

    private void OnEnable()
    {
        if (inventory == null) return;

        inventory.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (inventory != null)
            inventory.Changed -= Refresh;
    }

    private void Refresh()
    {
        for (int i = 0; i < icons.Length; i++)
        {
            if (icons[i] == null) continue;

            InventoryItemData item =
                i < HotbarInventory.Capacity ? inventory.GetItem(i) : null;

            icons[i].sprite = item != null ? item.Icon : null;
            icons[i].enabled = item != null && item.Icon != null;
        }
    }
}
