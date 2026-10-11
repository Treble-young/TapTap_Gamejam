using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventoryManager : MonoBehaviour
{
    // 与 UI 格子数量保持一致
    public const int Capacity = 7;

    // 玩家一旦获得过旗帜就永久记住（即使之后被升旗台消耗掉也保持），供关卡通行判定使用
    public static bool HasLevel03Flag = false;

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
        if (item == null || inventory == null)
            return false;

        // 获得旗帜后永久标记，供关卡通行区域判定使用（一旦拥有，之后消耗掉也保持）
        if (item is Level03Flag)
            HasLevel03Flag = true;

        // 已有同种物品时只叠加数量，不占用新格子
        if (inventory.Contains(item))
        {
            item.Add();
            Changed?.Invoke();
            return true;
        }

        if (inventory.Count >= Capacity)
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

        item.Remove(); // 数量 -1

        // 数量归零才真正从背包移除，否则只减少数量
        if (item.itemCount <= 0)
        {
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
        }

        Changed?.Invoke();
        return true;
    }
}
