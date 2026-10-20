using UnityEngine;

/// <summary>
/// 选中「路」字告示牌后靠近按 Q 修复；切换外观并开放后续地图。
/// E 仍沿用基类的提示交互。
/// </summary>
public class Level02RoadSign : Level02Restorable
{
    [Header("路牌外观（将图片导入为 Sprite 后拖入）")]
    [Tooltip("显示路牌图片的 SpriteRenderer；留空时使用本物体的 SpriteRenderer。")]
    [SerializeField] private SpriteRenderer signBoard;
    [Tooltip("未修复时显示的图片；留空则保留场景中的原图片。")]
    [SerializeField] private Sprite damagedSprite;
    [Tooltip("放入「路」字后显示的完整路牌图片；留空则使用原来的变色表现。")]
    [SerializeField] private Sprite repairedSprite;
    [Tooltip("可选：修复后额外显示的字牌子物体；完整图片已包含「路」字时可留空。")]
    [SerializeField] private GameObject restoredGlyph;

    [Header("修复后开路")]
    [Tooltip("开始时隐藏，修复后显示的新区域。不要引用路牌自身或它的父物体。")]
    [SerializeField] private GameObject newRoad;
    [Tooltip("开始时启用，修复后关闭的封堵物体。")]
    [SerializeField] private GameObject roadBlock;

    [Header("靠近提示")]
    [SerializeField] private string proximityHint = "应该放在这里吧";

    private bool hintShown;
    private Sprite originalSprite;
    private Color originalColor = Color.white;

    public override void Awake()
    {
        base.Awake();
        if (signBoard == null) signBoard = GetComponent<SpriteRenderer>();
        if (signBoard != null)
        {
            originalSprite = signBoard.sprite;
            originalColor = signBoard.color;
        }

        ApplyAppearance();
        // 只在初始化时设置，不在重新启用物体时重置已经完成的修复。
        if (newRoad != null && !transform.IsChildOf(newRoad.transform))
            newRoad.SetActive(IsRestored);
        if (roadBlock != null && !transform.IsChildOf(roadBlock.transform))
            roadBlock.SetActive(!IsRestored);
    }

    private void ApplyAppearance()
    {
        if (signBoard != null)
        {
            Sprite stateSprite = IsRestored ? repairedSprite : damagedSprite;
            signBoard.sprite = stateSprite != null ? stateSprite : originalSprite;
            // 自定义美术保留原本颜色，不再被旧占位图的棕色染色。
            signBoard.color = stateSprite != null ? Color.white :
                IsRestored ? new Color(0.69f, 0.61f, 0.43f) : originalColor;
        }
        if (restoredGlyph != null && !transform.IsChildOf(restoredGlyph.transform))
            restoredGlyph.SetActive(IsRestored);
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        base.OnTriggerEnter2D(collision);
        if (IsRestored || hintShown) return;

        PlayerManager player = collision.GetComponent<PlayerManager>();
        if (player == null || player.playerType != PlayerType.Main) return;

        hintShown = true;
        ShowMessage(proximityHint);
    }

    protected override void OnRestored()
    {
        ApplyAppearance();
        if (roadBlock != null && !transform.IsChildOf(roadBlock.transform))
            roadBlock.SetActive(false);
        if (newRoad != null && !transform.IsChildOf(newRoad.transform))
            newRoad.SetActive(true);

        // 新区域及碰撞体生效后再更新寻路。
        Physics2D.SyncTransforms();
        if (PathfindingManager.Instance != null) PathfindingManager.Instance.BuildGrid();
        ShowMessage("「路」字归位了，路牌恢复了原样。前方出现了一条新路。");
    }
}
