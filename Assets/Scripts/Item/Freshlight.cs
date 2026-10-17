using UnityEngine;

[CreateAssetMenu(fileName = "Freshlight", menuName = "Scriptable Objects/Item/Freshlight")]
public class Freshlight : InventoryItemData
{
    public override void Use(PlayerManager player)
    {
        LightFollow light = FindAnyObjectByType<LightFollow>();
        if (light != null)
        {
            if (light.LightStage == 0)
            {
                light.SetStage(1);
                light.SetWorldLights(true);
                light.SetBlackListLights(true);
                ToggleIcon(usedIcon);
            }
            else if (light.LightStage == 1)
            {
                light.SetStage(0);
                light.SetWorldLights(false);
                light.SetBlackListLights(false);
                ToggleIcon(icon);
            }
        }
    }
}
