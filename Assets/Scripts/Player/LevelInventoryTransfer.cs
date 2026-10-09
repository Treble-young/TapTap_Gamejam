using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>只在第一关进入第二关时携带已获得的手电筒和打火机。</summary>
public static class LevelInventoryTransfer
{
    private struct CarriedItem
    {
        public int playerId;
        public string playerName;
        public InventoryItemData item;
        public bool wasSelected;
    }

    private static readonly List<CarriedItem> pendingItems = new List<CarriedItem>();

    public static void CaptureFromLevel01()
    {
        ClearPendingItems();

        PlayerSelector selector = PlayerSelector.Instance;
        if (selector == null)
            return;

        foreach (PlayerManager player in selector.playerManagers)
        {
            if (player == null || player.playerInventory == null || player.playerInventory.inventory == null)
                continue;

            PlayerInventoryManager inventory = player.playerInventory;
            foreach (InventoryItemData item in inventory.inventory)
            {
                if (item == null || (!(item is Freshlight) && !(item is Lighter)))
                    continue;

                // 拷贝物品数据，但让手电筒、打火机的使用状态在新场景重新开始。
                InventoryItemData copy = Object.Instantiate(item);
                copy.hideFlags = HideFlags.DontUnloadUnusedAsset;
                pendingItems.Add(new CarriedItem
                {
                    playerId = player.playerID,
                    playerName = player.name,
                    item = copy,
                    wasSelected = inventory.currentSelectedItem == item
                });
            }
        }

        SceneManager.sceneLoaded -= RestoreInLevel02;
        SceneManager.sceneLoaded += RestoreInLevel02;
    }

    private static void RestoreInLevel02(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Level_02")
            return;

        SceneManager.sceneLoaded -= RestoreInLevel02;
        PlayerSelector selector = PlayerSelector.Instance;
        if (selector == null)
        {
            ClearPendingItems();
            return;
        }

        foreach (CarriedItem carried in pendingItems)
        {
            PlayerManager owner = null;
            foreach (PlayerManager player in selector.playerManagers)
            {
                if (player != null && player.name == carried.playerName)
                {
                    owner = player;
                    break;
                }
            }

            if (owner == null && carried.playerId == 0)
                owner = selector.GetMainPlayer();

            if (owner == null)
            {
                foreach (PlayerManager player in selector.playerManagers)
                {
                    if (player != null && player.playerID == carried.playerId)
                    {
                        owner = player;
                        break;
                    }
                }
            }

            if (owner == null)
                owner = selector.GetMainPlayer();

            PlayerInventoryManager inventory = owner != null ? owner.playerInventory : null;
            if (inventory != null && inventory.TryAdd(carried.item))
            {
                carried.item.hideFlags = HideFlags.None;
                if (carried.wasSelected)
                    inventory.Select(inventory.inventory.Count - 1);
            }
            else
            {
                Object.Destroy(carried.item);
            }
        }

        pendingItems.Clear();
    }

    private static void ClearPendingItems()
    {
        SceneManager.sceneLoaded -= RestoreInLevel02;
        foreach (CarriedItem carried in pendingItems)
        {
            if (carried.item != null)
                Object.Destroy(carried.item);
        }
        pendingItems.Clear();
    }
}
