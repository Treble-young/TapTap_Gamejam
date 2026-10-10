using UnityEngine;

public class GasStationOilPump : InteractableManager
{
    public override void Interact(PlayerManager player)
    {
        PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("No Power");
    }
}
