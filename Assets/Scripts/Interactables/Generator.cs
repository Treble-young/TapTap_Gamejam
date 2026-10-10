using UnityEngine;

public class Generator : InteractableManager
{
    public bool isConnectedWithFuel = false;
    public bool isStarted = false;
    public Car car;
    public FlagRaisingPlatform flagRaisingPlatform;

    public override void Interact(PlayerManager player)
    {
        bool fullyConnected = isConnectedWithFuel && car != null && car.isConeectedToCar;

        if (!fullyConnected)
        {
            if (!isConnectedWithFuel)
            {
                if (PlayerSelector.Instance.currentInventory &&
                    PlayerSelector.Instance.currentInventory.currentSelectedItem is OilPipeline)
                {
                    isConnectedWithFuel = true;

                    if (car && car.isConeectedToCar)
                    {
                        PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("Fuel line connected. Click the generator to start it.");
                    }
                    else
                    {
                        PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("Generator connected. Now connect the car with a fuel line.");
                    }

                    if (car)
                    {
                        car.ShowThePipeline();
                    }
                }
                else
                {
                    PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("You need to select a fuel line to connect the generator.");
                }
            }
            else
            {
                PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("Now connect the car with a fuel line.");
            }
        }
        else if (!isStarted)
        {
            isStarted = true;
            flagRaisingPlatform.isPowered = true;

            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("Generator started!");
        }
        else
        {
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("Generator is already running.");
        }
    }
}
