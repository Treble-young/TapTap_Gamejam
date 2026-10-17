using UnityEngine;

/// <summary>
/// @trace MECH-205
/// 部首碎片物品（医、殳、酉、聿、曰、馬、尺）：世界里的可拾取物。
/// 拾取后由 Level02CombineManager 检测集齐的配方，自动拼合成完整的字。
/// </summary>
[CreateAssetMenu(fileName = "Fragment", menuName = "Game/Level 02/Fragment Item")]
public class Level02FragmentItem : InventoryItemData
{
    [SerializeField] private string glyph;

    public string Glyph => glyph;

    /// <summary>运行时/编辑器构造：设置部首字符。</summary>
    public void Configure(string g)
    {
        glyph = g;
        itemName = "部首「" + g + "」";
    }

    public override void Use(PlayerManager player)
    {
        ShowMessage($"「{glyph}」是一个部首。集齐同一个字的其它部首，就能拼成一个完整的字。");
    }

    private static void ShowMessage(string message)
    {
        if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.popUpManager != null)
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow(message);
    }
}
