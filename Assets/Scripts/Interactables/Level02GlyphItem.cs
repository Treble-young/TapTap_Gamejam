using UnityEngine;

/// <summary>
/// 第二关通用字牌物品（路、水、禁等标牌上拆下来的字）。
/// 选中后按 Q 使用：若附近有缺少这个字的可修复物（路牌/水井/芦苇丛），
/// 字牌就会被消耗并归位；否则弹出提示。
/// </summary>
[CreateAssetMenu(fileName = "Glyph", menuName = "Game/Level 02/Glyph Item")]
public class Level02GlyphItem : InventoryItemData
{
    [SerializeField] private string glyph;
    [Tooltip("按 Q 使用时，可修复目标的最大距离")]
    [SerializeField] private float restoreRadius = 2.5f;

    public string Glyph => glyph;

    public override void Use(PlayerManager player)
    {
        if (player == null)
            return;

        Level02Restorable target = Level02Restorable.FindNearest(
            player.transform.position, restoreRadius, glyph);

        if (target == null)
        {
            ShowMessage($"「{glyph}」应该用在哪里呢……走近需要它的地方再试试。");
            return;
        }

        target.Restore(player, this);
    }

    private static void ShowMessage(string message)
    {
        if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.popUpManager != null)
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow(message);
    }
}
