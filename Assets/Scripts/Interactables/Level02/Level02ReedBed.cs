using UnityEngine;

/// <summary>
/// 第二关芦苇丛水潭：电线杆立在水中，可能有电。
/// 拿着「禁」字牌靠近后按 Q 使用，立起一块警告 NPC 的警示牌，「禁」字归位。
/// 按 E 只看提示。
/// </summary>
public class Level02ReedBed : Level02Restorable
{
    [SerializeField] private GameObject warningSign;
    [SerializeField] private GameObject restoredGlyph;

    protected override void OnRestored()
    {
        if (warningSign != null) warningSign.SetActive(true);
        if (restoredGlyph != null) restoredGlyph.SetActive(true);

        ShowMessage("「禁」字归位。警示牌立了起来，醒目地警告着：此处危险，禁止靠近。");
    }
}
