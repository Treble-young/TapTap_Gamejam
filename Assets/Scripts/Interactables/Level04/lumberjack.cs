using UnityEngine;

public class lumberjack : InteractableManager
{
    public override void Interact(PlayerManager player)
    {
        if (player.playerInventory.currentSelectedItem != null && player.playerInventory.currentSelectedItem is Drawing drawing && drawing.drawingStage == 2)
        {
            // 得到完整的图纸，触发游戏胜利
        }
        else
        {
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("......我得先找到完整的图纸才能和伐木工人交流。");
        }
    }
}
