using UnityEngine;

/// <summary>火堆熄灭后，等待主角举着打火机前来交谈的幸存者。</summary>
public class RecruitableStranger : InteractableManager
{
    private const float InteractionRadius = 1.5f;

    private PlayerManager recruit;
    private HeadIndicator indicator;

    public void Initialize(PlayerManager player)
    {
        recruit = player;

        // 开始等待玩家，头顶显示感叹号提示。
        indicator = GetComponent<HeadIndicator>();
        if (indicator == null)
            indicator = gameObject.AddComponent<HeadIndicator>();
        indicator.Show();

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

    private void Update()
    {
        if (recruit == null || recruit.playerState != PlayerState.Stranger)
            return;

        PlayerManager player = PlayerSelector.Instance != null ? PlayerSelector.Instance.currentPlayer : null;
        if (player == null || player.playerType != PlayerType.Main || !player.isUsingLighter)
            return;

        Recruit(player);
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
                PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("我得先举着打火机才能和他交谈。");
            return;
        }

        Recruit(player);
    }

    private void Recruit(PlayerManager player)
    {
        recruit.playerState = PlayerState.AutoMoving;
        interactionArea.enabled = false;

        // 入队后不再等待，隐藏头顶感叹号。
        if (indicator != null)
            indicator.Hide();

        PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("当其他角色加入队伍后，按住 Z/X 键可以切换操控角色。他们交互的内容也许会不同。", 999f);

        // 其他角色的交互列表里也可能保存了这个幸存者。
        foreach (PlayerManager teammate in PlayerSelector.Instance.playerManagers)
        {
            if (teammate != null && teammate.playerInteraction != null)
                teammate.playerInteraction.RemoveInteractable(this);
        }
    }
}
