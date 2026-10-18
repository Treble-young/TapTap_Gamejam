using UnityEngine;

[CreateAssetMenu(fileName = "NewDrawing", menuName = "Scriptable Objects/Item/Drawing")]
public class Drawing : InventoryItemData
{
    [Header("Drawing Properties")]
    public int drawingStage; // 0 = empty, 1 = partial, 2 = complete
    public Sprite emptyDrawingSprite;
    public Sprite partialDrawingSprite;
    public Sprite completeDrawingSprite;

    public override Sprite Icon => drawingStage switch
    {
        0 => emptyDrawingSprite,
        1 => partialDrawingSprite,
        2 => completeDrawingSprite,
        _ => icon
    };

    public override void Use(PlayerManager player)
    {
        PlayerUIManager.Instance.drawingInUI.ShowDrawingUI();
    }
}
