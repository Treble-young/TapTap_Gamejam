using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// @trace MECH-205
/// 破碎的标识碎片包：NPC 赠予的一袋部首碎片（医、殳、酉、聿、曰、馬、尺）。
/// 只占一个背包格子。拿到对应建筑前按 E 互动即可拼合修复，
/// 按 Q 查看袋里还剩哪些碎片。
/// </summary>
public class Level02FragmentBundle : InventoryItemData
{
    private readonly HashSet<string> pieces = new HashSet<string>();

    public int PieceCount => pieces.Count;

    /// <summary>初始化碎片内容（由赠予方调用）。</summary>
    public void Configure(string displayName, IEnumerable<string> initialPieces)
    {
        itemName = displayName;
        pieces.Clear();
        foreach (string p in initialPieces)
            pieces.Add(p);
        itemCount = pieces.Count;
    }

    /// <summary>是否集齐某组碎片。</summary>
    public bool HasPieces(params string[] required)
    {
        foreach (string p in required)
            if (!pieces.Contains(p))
                return false;
        return true;
    }

    /// <summary>消耗一组碎片（拼合成功时调用）。</summary>
    public void ConsumePieces(params string[] required)
    {
        foreach (string p in required)
            pieces.Remove(p);
        itemCount = Mathf.Max(1, pieces.Count);
    }

    public override void Use(PlayerManager player)
    {
        string left = pieces.Count > 0 ? string.Join("、", pieces.Select(p => $"「{p}」")) : "（空了）";
        ShowMessage($"破碎的标识碎片：{left}\n把它们带到对应的建筑前，试着拼回完整的字吧。");
    }

    private static void ShowMessage(string message)
    {
        if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.popUpManager != null)
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow(message);
    }
}
