using UnityEngine;

public class Water : InteractableManager
{
    public override void Interact(PlayerManager player)
    {
        if (PlayerSelector.Instance.currentInventory.currentSelectedItem is Carpet lantern)
        {
            lantern.isWet = true;
            lantern.ToggleIcon(lantern.wetIcon);
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("The carpet is now wet and can be used to extinguish fires.");

            Carpet carpetItem = PlayerSelector.Instance.currentInventory.currentSelectedItem as Carpet;
            if (carpetItem != null)
            {
                carpetItem.isWet = true;
                carpetItem.ToggleIcon(carpetItem.wetIcon);
            }
        }
        else
        {
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("You need to select a carpet to use the water.");
        }
    }
}
