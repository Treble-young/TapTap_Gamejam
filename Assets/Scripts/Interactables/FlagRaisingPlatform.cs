using UnityEngine;

public class FlagRaisingPlatform : InteractableManager
{
    public bool hasFlag = false;
    public bool isRopeBroken = true;
    public bool isPowered = false;
    public bool isRaisingFlag = false;

    public override void Interact(PlayerManager player)
    {
        if (!hasFlag)
        {
            if (PlayerSelector.Instance.currentInventory.currentSelectedItem && PlayerSelector.Instance.currentInventory.currentSelectedItem is Level03Flag)
            {
                hasFlag = true;
                PlayerSelector.Instance.currentInventory.RemoveItem(PlayerSelector.Instance.currentInventory.currentSelectedItem);

                // 将旗帜放置在平台上

            }
            else
            {
                PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("你需要旗帜");
                return;
            }

        }

        if (isRopeBroken)
        {
            if (PlayerSelector.Instance.currentInventory.currentSelectedItem && PlayerSelector.Instance.currentInventory.currentSelectedItem.ItemName == "FlagRope")
            {
                isRopeBroken = false;
                PlayerSelector.Instance.currentInventory.RemoveItem(PlayerSelector.Instance.currentInventory.currentSelectedItem);

                // 修复绳索的逻辑
            }
            else
            {
                PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("升旗用的绳索坏了，也许在哪里可以找到替代品");
                return;
            }
        }

        if (!isPowered)
        {
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("发电机似乎没电了");
        }

        if (hasFlag && !isRopeBroken && isPowered)
        {
            isRaisingFlag = true;
            // 开始升旗的逻辑
        }
    }
}
