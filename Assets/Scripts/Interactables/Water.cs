using UnityEngine;

public class Water : InteractableManager
{
    public override void Interact(PlayerManager player)
    {
        if (PlayerSelector.Instance.currentInventory.currentSelectedItem is Carpet lantern)
        {
            lantern.isWet = true;
            lantern.ToggleIcon(lantern.wetIcon);
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("地毯已被弄湿。");

            Carpet carpetItem = PlayerSelector.Instance.currentInventory.currentSelectedItem as Carpet;
            if (carpetItem != null)
            {
                carpetItem.isWet = true;
                carpetItem.ToggleIcon(carpetItem.wetIcon);
            }
        }
        else
        {
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("我得先拿着地毯才能把它弄湿。");
        }
    }
}
