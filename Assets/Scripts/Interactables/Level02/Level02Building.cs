using UnityEngine;

/// <summary>
/// @trace MECH-205
/// @trace LVL-207
/// 第二关可修复建筑（医院=醫 / 图书馆=書 / 车站=駅）。
/// 与「路」「水」「禁」同一套交互：选中拼合好的字牌（Level02GlyphItem）按 Q，
/// 由 Level02GlyphItem.Use 找到本建筑并 Restore；按 E 只查看提示。
/// </summary>
public class Level02Building : Level02Restorable
{
    [Header("修复表现（占位，美术后补）")]
    [SerializeField] private GameObject ruinedVisual;
    [SerializeField] private GameObject restoredVisual;
    [SerializeField] private GameObject restoredGlyphLabel;

    protected override void OnRestored()
    {
        if (ruinedVisual != null) ruinedVisual.SetActive(false);
        if (restoredVisual != null) restoredVisual.SetActive(true);
        if (restoredGlyphLabel != null) restoredGlyphLabel.SetActive(true);

        ShowMessage($"「{requiredGlyph}」严丝合缝地归位了，这里重新有了名字。");
    }
}
