using UnityEngine;

/// <summary>
/// 第二关可修复物基类（路牌、水井、芦苇丛警示牌等）。
/// 交互约定：E 只查看提示文案；真正的修复靠选中对应字牌后按 Q（使用物品），
/// 由 Level02GlyphItem.Use 找到附近匹配的可修复物并调用 <see cref="Restore"/>。
/// </summary>
public abstract class Level02Restorable : InteractableManager
{
    [Tooltip("修复需要的字（路/水/禁…）")]
    [SerializeField] protected string requiredGlyph = "路";
    [Tooltip("未修复时按 E 查看的提示")]
    [TextArea(1, 3)]
    [SerializeField] protected string teaserMessage = "这里缺了点什么。";
    [Tooltip("修复完成后再按 E 查看的提示")]
    [TextArea(1, 3)]
    [SerializeField] protected string restoredMessage = "已经修好了。";

    public string RequiredGlyph => requiredGlyph;
    public bool IsRestored { get; protected set; }

    public override void Interact(PlayerManager player)
    {
        if (player == null || player.playerType != PlayerType.Main ||
            PlayerSelector.Instance == null || player != PlayerSelector.Instance.currentPlayer)
            return;

        ShowMessage(IsRestored ? restoredMessage : teaserMessage);
    }

    /// <summary>用字牌修复（由 Level02GlyphItem.Use 调用），成功后消耗物品栏中的字牌。</summary>
    public virtual void Restore(PlayerManager player, Level02GlyphItem glyph)
    {
        if (IsRestored || player == null || glyph == null || glyph.Glyph != requiredGlyph)
            return;

        IsRestored = true;
        OnRestored();

        PlayerInventoryManager inventory = player.playerInventory;
        if (inventory != null)
        {
            inventory.inventory.Remove(glyph);
            inventory.currentSelectedItem = inventory.GetItem(inventory.SelectedIndex);
            inventory.NotifyChanged();
        }
    }

    /// <summary>子类在此处理修复表现（换色、显示隐藏物体、开路、弹文案等）。</summary>
    protected abstract void OnRestored();

    /// <summary>找到 pos 附近 radius 内、尚未修复且需要 glyph 的最近可修复物。</summary>
    public static Level02Restorable FindNearest(Vector2 pos, float radius, string glyph)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, radius);
        Level02Restorable best = null;
        float bestDist = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            Level02Restorable r = hit.GetComponentInParent<Level02Restorable>();
            if (r == null || r.IsRestored || r.RequiredGlyph != glyph)
                continue;

            float d = ((Vector2)r.transform.position - pos).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = r;
            }
        }
        return best;
    }

    protected static void ShowMessage(string message)
    {
        if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.popUpManager != null)
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow(message);
    }
}
