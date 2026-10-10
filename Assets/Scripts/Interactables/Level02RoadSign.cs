using UnityEngine;

/// <summary>
/// 第二关箭头路牌：上面写着 Breaking ~~Dawn~~ Down。
/// 主控拿着写有「路」的告示牌（Level02RoadGlyphItem）靠近后按 Q 使用，
/// 路牌恢复完整，前方出现一条新的路（移除封堵、显示新路段 Tilemap）。
/// 按 E 只看提示；主控第一次走近时会弹出提示「应该放在这里吧」。
/// </summary>
public class Level02RoadSign : Level02Restorable
{
    [SerializeField] private SpriteRenderer signBoard;
    [SerializeField] private GameObject restoredGlyph;

    [Header("修复后开路")]
    [Tooltip("修复后要显示的新路段（初始隐藏的 Tilemap 物体）")]
    [SerializeField] private GameObject newRoad;
    [Tooltip("修复后要移除的路尽头封堵碰撞体")]
    [SerializeField] private GameObject roadBlock;

    [Header("靠近提示")]
    [SerializeField] private string proximityHint = "应该放在这里吧";

    private bool hintShown;

    public override void OnTriggerEnter2D(Collider2D collision)
    {
        base.OnTriggerEnter2D(collision);

        if (IsRestored || hintShown)
            return;

        PlayerManager player = collision.GetComponent<PlayerManager>();
        if (player == null || player.playerType != PlayerType.Main)
            return;

        hintShown = true;
        ShowMessage(proximityHint);
    }

    protected override void OnRestored()
    {
        if (signBoard != null) signBoard.color = new Color(0.69f, 0.61f, 0.43f);
        if (restoredGlyph != null) restoredGlyph.SetActive(true);

        // 开路：移除封堵、显示新路段，并重建寻路网格让自动寻路识别新路
        if (roadBlock != null) roadBlock.SetActive(false);
        if (newRoad != null) newRoad.SetActive(true);
        if (PathfindingManager.Instance != null) PathfindingManager.Instance.BuildGrid();

        ShowMessage("「路」字归位了，路牌恢复了原样。前方出现了一条新路。");
    }
}
