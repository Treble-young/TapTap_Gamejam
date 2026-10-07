using UnityEngine;

public class Level02Well : InteractableManager
{
    public override void Interact(PlayerManager player)
    {
        if (player == null || player.playerType != PlayerType.Main ||
            PlayerSelector.Instance == null || player != PlayerSelector.Instance.currentPlayer)
            return;

        if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.popUpManager != null)
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow(
                "井里似乎有水。等找到“水”标牌，再来试试。");
    }
}
