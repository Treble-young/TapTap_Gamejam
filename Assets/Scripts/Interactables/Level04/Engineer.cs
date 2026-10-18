using UnityEngine;

public class Engineer : InteractableManager
{
    public bool hasToldWidth;

    [Tooltip("告诉玩家宽度后，图纸推进到第几阶段（1 = 半张；之后才能去断桥扔石头）")]
    public int partialStage = 1;

    public override void Interact(PlayerManager player)
    {
        if (PlayerSelector.Instance.currentInventory.currentSelectedItem is Drawing drawing)
        {
            if (!hasToldWidth)
            {
                PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("我只知道桥的宽度是6米，你有办法测量出桥的长度吗？");
                hasToldWidth = true;

                // 宽度告诉玩家：图纸上的宽度先填上，图纸变成「半张」
                PlayerUIManager.Instance.drawingInUI.SetWidth(6f);
                if (drawing.drawingStage < partialStage)
                {
                    drawing.drawingStage = partialStage;
                    PlayerSelector.Instance.currentInventory.NotifyChanged();
                }

                return;
            }

            if (drawing.drawingStage == 2)
            {
                PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("你已经有了完整的图纸，快去把它送去给给伐木工就可以修桥了！");
                return;
            }
        }

        PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("......");
    }
}
