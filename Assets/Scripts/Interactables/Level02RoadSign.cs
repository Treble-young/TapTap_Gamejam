using UnityEngine;

public class Level02RoadSign : InteractableManager
{
    [SerializeField] private SpriteRenderer signBoard;
    [SerializeField] private GameObject restoredGlyph;

    private bool repaired;

    public override void Interact(PlayerManager player)
    {
        if (player == null || player.playerType != PlayerType.Main ||
            PlayerSelector.Instance == null || player != PlayerSelector.Instance.currentPlayer)
            return;

        if (repaired)
        {
            ShowMessage("路牌已经修好了。后面的道路还需要继续搭建。");
            return;
        }

        PlayerInventoryManager inventory = player.playerInventory;
        Level02RoadGlyphItem glyph = inventory != null
            ? inventory.currentSelectedItem as Level02RoadGlyphItem
            : null;

        if (glyph == null)
        {
            ShowMessage("路牌缺了一个字。先找到并选中“路”。");
            return;
        }

        repaired = true;
        if (signBoard != null) signBoard.color = new Color(0.69f, 0.61f, 0.43f);
        if (restoredGlyph != null) restoredGlyph.SetActive(true);

        inventory.inventory.Remove(glyph);
        inventory.currentSelectedItem = inventory.GetItem(inventory.SelectedIndex);
        inventory.NotifyChanged();
        ShowMessage("“路”字归位了，路牌恢复了原样。");
    }

    private static void ShowMessage(string message)
    {
        if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.popUpManager != null)
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow(message);
    }
}
