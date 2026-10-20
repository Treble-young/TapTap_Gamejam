using UnityEngine;

public class lumberjack : InteractableManager
{
    public Bridge bridge;

    public bool isFixed = false;

    public override void Interact(PlayerManager player)
    {
        if (!isFixed)
        {
            if (player.playerInventory.currentSelectedItem != null && player.playerInventory.currentSelectedItem is Drawing drawing && drawing.drawingStage == 2)
            {
                // 得到完整的图纸，触发游戏胜利
                if (bridge != null)
                {
                    bridge._fixed = true;
                    bridge._sprite.sprite = bridge.fixedBridgeSprite;
                    if (bridge.bridgeFixedEffect != null)
                    {
                        bridge.bridgeFixedEffect.SetActive(true);
                    }
                    isFixed = true;
                    LoadingManager.Instance.FakeLoad();
                }
            }
            else
            {
                PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("......我得先找到完整的图纸才能和伐木工人交流。");
            }
        }
        else
        {
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("我已经修好了桥。");
        }
    }
}
