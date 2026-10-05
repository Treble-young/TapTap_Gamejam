using UnityEngine;

/// <summary>火堆熄灭后，等待主角举着打火机前来交谈的幸存者。</summary>
public class RecruitableStranger : InteractableManager
{
    private const float InteractionRadius = 1.5f;

    private PlayerManager recruit;

    public void Initialize(PlayerManager player)
    {
        recruit = player;

        if (interactionArea == null)
        {
            CircleCollider2D area = gameObject.AddComponent<CircleCollider2D>();
            area.isTrigger = true;
            area.radius = InteractionRadius;
            interactionArea = area;
        }

        // 灭火时主角可能已经站在幸存者身旁，直接登记这次交互。
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(transform.position, InteractionRadius))
        {
            PlayerManager nearby = hit.GetComponent<PlayerManager>();
            if (nearby != null && nearby != recruit && nearby.playerType == PlayerType.Main &&
                nearby.playerInteraction != null)
                nearby.playerInteraction.AddInteractable(this);
        }
    }

    public override void Interact(PlayerManager player)
    {
        if (recruit == null || recruit.playerState != PlayerState.Stranger ||
            PlayerSelector.Instance == null || player == null ||
            player != PlayerSelector.Instance.currentPlayer ||
            player.playerType != PlayerType.Main)
            return;

        if (!player.isUsingLighter)
        {
            if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.popUpManager != null)
                PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("They are afraid of the dark. Light your lighter first.");
            return;
        }

        recruit.playerState = PlayerState.AutoMoving;
        interactionArea.enabled = false;

        // 其他角色的交互列表里也可能保存了这个幸存者。
        foreach (PlayerManager teammate in PlayerSelector.Instance.playerManagers)
        {
            if (teammate != null && teammate.playerInteraction != null)
                teammate.playerInteraction.RemoveInteractable(this);
        }
    }
}
