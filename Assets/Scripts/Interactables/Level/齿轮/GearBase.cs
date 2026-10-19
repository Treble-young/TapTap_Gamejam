using System;
using System.Collections.Generic;
using UnityEngine;

// PlayerInputLock belongs on gameUI, not on the always-active world object.
public class GearBase : InteractableManager
{
    public GameObject gameUI;

    public bool isDone = false;

    public bool canInteract = false;

    [Header("进入工作台时消耗的零件（按 InventoryItemData.ItemName 匹配）")]
    [SerializeField] private string gearItemName = "Gear";
    [SerializeField] private string screwItemName = "Screw";

    [Tooltip("勾上则在打开工作台时把齿轮和螺丝从背包扣掉")]
    [SerializeField] private bool consumePartsOnOpen = true;

    public override void Interact(PlayerManager player)
    {
        if (!canInteract)
            return;

        if (!isDone)
        {
            if (gameUI == null)
                return;

            // 打开工作台就把齿轮和螺丝用掉了
            if (consumePartsOnOpen)
                ConsumeParts(player);

            if (gameUI.GetComponent<PlayerInputLock>() == null)
                gameUI.AddComponent<PlayerInputLock>();

            gameUI.SetActive(true);
            HideInventory();
        }
        else
        {

        }
    }

    /// <summary>把玩家背包里的齿轮和螺丝各扣掉一件（没有的跳过）。</summary>
    private void ConsumeParts(PlayerManager player)
    {
        PlayerInventoryManager inventory = player != null ? player.playerInventory : null;
        if (inventory == null || inventory.inventory == null)
            return;

        // RemoveItem 会修改列表，先收集再删，避免边遍历边改
        List<InventoryItemData> targets = new List<InventoryItemData>();

        foreach (InventoryItemData item in inventory.inventory)
        {
            if (item == null)
                continue;

            if (IsPart(item, gearItemName) || IsPart(item, screwItemName))
                targets.Add(item);
        }

        foreach (InventoryItemData item in targets)
            inventory.RemoveItem(item);
    }

    private static bool IsPart(InventoryItemData item, string itemName)
    {
        if (item == null || string.IsNullOrEmpty(itemName))
            return false;

        return string.Equals(item.ItemName?.Trim(), itemName.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public void Exit()
    {
        PlayerUIManager.Instance.PlayClickSound();

        if (gameUI != null)
            gameUI.SetActive(false);

        ShowInventory();
    }

    private void HideInventory()
    {
        if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.inventoryManager != null)
            PlayerUIManager.Instance.inventoryManager.HideInventoryWindow();
    }

    private void ShowInventory()
    {
        if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.inventoryManager != null)
            PlayerUIManager.Instance.inventoryManager.ShowInventoryWindow();
    }

    public void SetDone(bool done)
    {
        isDone = done;
    }
}
