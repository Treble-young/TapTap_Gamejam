using UnityEngine;

/// <summary>
/// 第二关水井：拿着「水」字牌靠近后按 Q 使用，水井恢复——井亭立起、「水」字归位，
/// 水桶沉入井中、绳子缠上滑轮（美术占位，后续替换）。
/// 按 E 只看提示。
/// </summary>
public class Level02Well : Level02Restorable
{
    [SerializeField] private SpriteRenderer wellBody;
    [SerializeField] private GameObject restoredGlyph;
    [SerializeField] private GameObject wellShelter;

    protected override void OnRestored()
    {
        if (wellBody != null) wellBody.color = new Color(0.62f, 0.58f, 0.5f);
        if (restoredGlyph != null) restoredGlyph.SetActive(true);
        if (wellShelter != null) wellShelter.SetActive(true);

        ShowMessage("「水」字归位，井亭重新立了起来，水桶已沉入井中。");
    }
}
