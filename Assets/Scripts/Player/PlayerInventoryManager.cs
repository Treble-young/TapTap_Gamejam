using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventoryManager : MonoBehaviour
{
    // 与 UI 格子数量保持一致
    public const int Capacity = 7;

    public PlayerManager playerOwnedTheInventory;
    public List<InventoryItemData> inventory;

    [SerializeField] private int selectedIndex = 0;

    public event Action Changed;

    public int SelectedIndex => selectedIndex;
    public InventoryItemData currentSelectedItem;

    void Awake()
    {
        playerOwnedTheInventory = GetComponent<PlayerManager>();
        inventory = new List<InventoryItemData>();
    }

    public InventoryItemData GetItem(int index)
    {
        if (inventory == null || index < 0 || index >= inventory.Count)
            return null;
        return inventory[index];
    }

    public void Select(int index)
    {
        index = Mathf.Clamp(index, 0, Capacity - 1);
        if (index == selectedIndex)
            return;

        selectedIndex = index;
        Changed?.Invoke();

        currentSelectedItem = GetItem(selectedIndex);
    }

    public bool TryAdd(InventoryItemData item)
    {
        if (item == null || inventory == null || inventory.Count >= Capacity)
            return false;

        inventory.Add(item);

        if (currentSelectedItem == null)
        {
            currentSelectedItem = item;
            selectedIndex = inventory.IndexOf(item);
        }

        Changed?.Invoke();
        return true;
    }

    public void NotifyChanged()
    {
        Changed?.Invoke();
    }

    public bool RemoveItem(InventoryItemData item)
    {
        if (item == null || inventory == null || !inventory.Contains(item))
            return false;

        int index = inventory.IndexOf(item);
        inventory.Remove(item);

        if (index == selectedIndex)
        {
            currentSelectedItem = null;
            selectedIndex = -1;
        }
        else if (index < selectedIndex)
        {
            selectedIndex--;
        }

        Changed?.Invoke();
        return true;
    }
}
