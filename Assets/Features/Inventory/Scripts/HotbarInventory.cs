using System;
using UnityEngine;

public class HotbarInventory : MonoBehaviour
{
    public const int Capacity = 7;

    private readonly InventoryItemData[] items =
        new InventoryItemData[Capacity];

    public event Action Changed;

    public InventoryItemData GetItem(int index)
    {
        return items[index];
    }

    public bool TryAdd(InventoryItemData item)
    {
        if (item == null) return false;

        for (int i = 0; i < Capacity; i++)
        {
            if (items[i] != null) continue;

            items[i] = item;
            Changed?.Invoke();
            return true;
        }

        return false;
    }
}
