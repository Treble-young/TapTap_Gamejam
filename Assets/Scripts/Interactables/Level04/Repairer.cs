using System;
using UnityEngine;

public class Repairer : InteractableManager
{
    public bool isRepaired = false;

    [Header("Spawned Item")]
    public GameObject spawnedItemPrefab;
    public Transform spawnPoint;
    public bool hasSpawnedItem = false;

    [Header("Gear")]
    public GearBase gearBase;

    [Header("修理所需物品（按 InventoryItemData.ItemName 匹配）")]
    [SerializeField] private string gearItemName = "Gear";
    [SerializeField] private string screwItemName = "Screw";

    [Header("提示文案")]
    [SerializeField] private string bothItemsMessage = "帮我修复旁边的工作台吧。";
    [SerializeField] private string missingScrewMessage = "还缺一根螺丝。";
    [SerializeField] private string missingGearMessage = "还缺一个齿轮。";
    [SerializeField] private string noItemsMessage = "没有齿轮和螺丝怎么修啊！！！";

    [Header("调试")]
    [Tooltip("交互时把交互者的背包内容打到 Console，用来排查匹配问题；确认没问题后可以关掉")]
    [SerializeField] private bool logInventory = true;

    public override void Interact(PlayerManager player)
    {
        if (!isRepaired)
        {
            bool hasGear = HasItem(player, gearItemName);
            bool hasScrew = HasItem(player, screwItemName);

            if (logInventory)
                LogInventory(player, hasGear, hasScrew);

            if (hasGear && hasScrew)
            {
                ShowMessage(bothItemsMessage);
                gearBase.canInteract = true;
            }
            else if (hasGear)
                ShowMessage(missingScrewMessage);
            else if (hasScrew)
                ShowMessage(missingGearMessage);
            else
                ShowMessage(noItemsMessage);

            return;
        }

        ShowMessage("修好了！我把这张图纸给你，或许能帮你修好那条断桥。");

        if (!hasSpawnedItem && spawnedItemPrefab != null && spawnPoint != null)
        {
            Instantiate(spawnedItemPrefab, spawnPoint.position, spawnPoint.rotation);
            hasSpawnedItem = true;
        }
    }

    public void SetRepaired(bool repaired)
    {
        isRepaired = repaired;
    }

    /// <summary>玩家背包里是否有名字匹配的物品（不区分大小写，允许前后空格）。</summary>
    private static bool HasItem(PlayerManager player, string itemName)
    {
        if (string.IsNullOrEmpty(itemName))
            return false;

        PlayerInventoryManager inventory = player != null ? player.playerInventory : null;
        if (inventory == null || inventory.inventory == null)
            return false;

        foreach (InventoryItemData item in inventory.inventory)
        {
            if (item == null)
                continue;

            if (string.Equals(item.ItemName?.Trim(), itemName.Trim(), StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool ShowMessage(string message)
    {
        if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.popUpManager != null)
        {
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow(message);
            return true;
        }

        Debug.LogWarning($"[Repairer] 弹窗不可用，消息没能显示：{message}");
        return false;
    }

    private void LogInventory(PlayerManager player, bool hasGear, bool hasScrew)
    {
        if (player == null)
        {
            Debug.LogWarning("[Repairer] Interact 收到的 player 是 null", this);
            return;
        }

        PlayerInventoryManager inventory = player.playerInventory;
        if (inventory == null)
        {
            Debug.LogWarning($"[Repairer] {player.name} 上没有 PlayerInventoryManager", this);
            return;
        }

        if (inventory.inventory == null)
        {
            Debug.LogWarning($"[Repairer] {player.name} 的 inventory 列表是 null", this);
            return;
        }

        string dump = string.Empty;
        foreach (InventoryItemData item in inventory.inventory)
            dump += item == null ? "[null] " : $"[{item.ItemName}] ";

        Debug.Log($"[Repairer] 交互者={player.name}（{player.playerType}）背包 {inventory.inventory.Count} 件：{dump}" +
                  $" | 目标 \"{gearItemName}\"={hasGear}  \"{screwItemName}\"={hasScrew}", this);
    }
}
