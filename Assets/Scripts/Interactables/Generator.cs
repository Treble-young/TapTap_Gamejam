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

        if (isFueled)
        {
            TryStart();
            return;
        }

        if (isConnectedWithFuel && PlayerHoldsOilPipeline())
        {
            DisconnectFuelLine();
            return;
        }

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
            if (car != null)
                car.DisconnectPumpLine();

            if (car && car.isConeectedToCar)
                PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("输油管已经连接到发电机和车子上了。现在我要想办法把燃料输送到发电机里了。得找个千斤顶把车子抬起来。");
            else
                PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("输油管已经连接到发电机上了。");

            if (car)
                car.ShowThePipeline();
        }
        else
        {
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("我需要一条输油管来连接发电机和车子。");
        }
    }

    private void DisconnectFuelLine()
    {
        isConnectedWithFuel = false;

        if (car != null)
        {
            car.DisconnectGeneratorLine();
        }

        PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("输油管已经从发电机上断开。");
    }

    private bool PlayerHoldsOilPipeline()
    {
        PlayerInventoryManager inventory = PlayerSelector.Instance != null ? PlayerSelector.Instance.currentInventory : null;
        return inventory != null && inventory.currentSelectedItem is OilPipeline;
    }

    /// <summary>Main：启动发电机。</summary>
    private void TryStart()
    {
        if (!isFueled)
        {
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("发电机没有燃料。要先确保发电机有燃料。");
            return;
        }

        if (isStarted)
        {
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("发电机已经在运行了。");
            return;
        }

        isStarted = true;
        flagRaisingPlatform.isPowered = true;
        PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("发电机已经启动！旗台现在有电力了。");
    }
}
