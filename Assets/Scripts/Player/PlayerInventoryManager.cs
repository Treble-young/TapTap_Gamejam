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

        // 以前这里无条件 new 一个空列表，会把预制体 / Inspector 里预置的初始物品直接抹掉。
        // 现在保留预置内容，只清理序列化残留的空引用（背包是紧凑列表，不允许有空洞）。
        if (inventory == null)
        {
            inventory = new List<InventoryItemData>();
        }
        else
        {
            inventory.RemoveAll(item => item == null);

            // 预置物品是「共享资产」：itemCount / isUsed / usedIcon 这些运行时状态写在 ScriptableObject 上，
            // 两个玩家拖同一个资产就会互相串（数量一起减、图标一起变）。所以每个玩家各复制一份。
            DuplicatePresetItems();
        }
    }

    /// <summary>把预置的物品复制成运行时实例，让每个玩家持有独立的运行时状态。</summary>
    private void DuplicatePresetItems()
    {
        for (int i = 0; i < inventory.Count; i++)
        {
            InventoryItemData original = inventory[i];
            if (original == null)
                continue;

            InventoryItemData copy = Instantiate(original);
            copy.name = original.name;
            inventory[i] = copy;
        }
    }

    void Start()
    {
        // 关卡一开始背包就有东西时，先把「选中项」对齐到实际内容，
        // 否则 currentSelectedItem 是 null：按 Q 用不了，快捷栏也没有选中标记。
        SyncSelection();
    }

    /// <summary>
    /// 让 currentSelectedItem / selectedIndex 与背包实际内容保持一致。
    /// 任何绕过 TryAdd 批量写入背包的地方，写完都应该调一次。
    /// </summary>
    public void SyncSelection()
    {
        if (inventory == null)
            inventory = new List<InventoryItemData>();

        if (inventory.Count == 0)
        {
            currentSelectedItem = null;
            Changed?.Invoke();
            return;
        }

        selectedIndex = Mathf.Clamp(selectedIndex, 0, inventory.Count - 1);
        currentSelectedItem = inventory[selectedIndex];

        Changed?.Invoke();
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
