using UnityEngine;

public class Generator : InteractableManager
{
    public bool isConnectedWithFuel = false;
    public bool isFueled = false;
    public bool isStarted = false;
    public Car car;
    public FlagRaisingPlatform flagRaisingPlatform;

    public override void Interact(PlayerManager player)
    {
        // 只有 Main 能操作发电机（连接油管 + 启动）
        if (player.playerType != PlayerType.Main)
            return;

        if (!isConnectedWithFuel)
        {
            ConnectFuelLine();
            return;
        }

        TryStart();
    }

    /// <summary>Main：连接油管。</summary>
    private void ConnectFuelLine()
    {
        if (PlayerSelector.Instance.currentInventory &&
            PlayerSelector.Instance.currentInventory.currentSelectedItem is OilPipeline)
        {
            isConnectedWithFuel = true;

            if (car && car.isConeectedToCar)
                PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("Fuel line connected. Fill the generator with fuel first.");
            else
                PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("Generator connected. Now connect the car with a fuel line.");

            if (car)
                car.ShowThePipeline();
        }
        else
        {
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("You need to select a fuel line to connect the generator.");
        }
    }

    /// <summary>Main：启动发电机。</summary>
    private void TryStart()
    {
        bool fullyConnected = isConnectedWithFuel && car != null && car.isConeectedToCar;

        if (!fullyConnected)
        {
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("Fuel line is not connected yet.");
            return;
        }

        if (!isFueled)
        {
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("Fill the generator with fuel from the car first.");
            return;
        }

        if (isStarted)
        {
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("Generator is already running.");
            return;
        }

        isStarted = true;
        flagRaisingPlatform.isPowered = true;
        PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("Generator started!");
    }
}
