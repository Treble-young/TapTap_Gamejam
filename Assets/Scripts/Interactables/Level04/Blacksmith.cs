using UnityEngine;

public class Blacksmith : InteractableManager
{
    public Furnace furnace;

    [Header("Spawned Item")]
    public WorldPickupItem screwPickupItem;
    public Transform screwSpawnPoint;

    public override void Interact(PlayerManager player)
    {
        if (furnace.stage == 0 || furnace.stage == 1)
        {
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("铁匠：先帮我点燃火炉，我才能帮你铸造螺丝。");
        }
        else if (furnace.stage == 2)
        {
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("铁匠：请你帮我控制火炉的温度。");
        }
        else if (furnace.stage == 3)
        {
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("铁匠：谢谢你帮我点燃火炉，我已经帮你铸造好了螺丝。");
            if (screwPickupItem != null && screwSpawnPoint != null)
            {
                Instantiate(screwPickupItem, screwSpawnPoint.position, Quaternion.identity);
                interactionArea.enabled = false;
            }
        }
    }
}
