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

        }
        else
        {
            if (isRopeBroken)
            {

            }
            if (!isPowered)
            {

            }
        }

        if (hasFlag && !isRopeBroken && isPowered)
        {
            isRaisingFlag = true;
        }
    }
}
