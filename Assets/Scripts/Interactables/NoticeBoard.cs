using UnityEngine;

/// <summary>
/// 告示牌：玩家（主角）与其交互后弹出一段提示文本，可重复查看。
/// 挂在带有 Trigger 碰撞体的物体上，配合 InteractableManager 的注册逻辑使用。
/// </summary>
public class NoticeBoard : InteractableManager
{
    [TextArea(2, 5)]
    [SerializeField] private string message = "这里立着一块告示牌。";

    public override void Interact(PlayerManager player)
    {
        if (player == null || player.playerType != PlayerType.Main ||
            PlayerSelector.Instance == null || player != PlayerSelector.Instance.currentPlayer)
            return;

        if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.popUpManager != null)
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow(message);
    }
}
