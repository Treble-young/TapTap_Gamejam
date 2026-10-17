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

            if (car && car.isConeectedToCar)
                PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("油管接好了。我得把燃料送进发电机，先找个千斤顶把车抬起来。");
            else
                PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("发电机这头接好了。");

            if (car)
                car.ShowThePipeline();
        }
        else
        {
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("我得先拿条输油管，把发电机和车连起来。");
        }
    }

    private void DisconnectFuelLine()
    {
        isConnectedWithFuel = false;

        if (car != null)
        {
            car.isConeectedToCar = false;
            car.ShowThePipeline();
        }

        PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("我把发电机这头的油管拔掉了。");
    }

    private bool PlayerHoldsOilPipeline()
    {
        PlayerInventoryManager inventory = PlayerSelector.Instance != null ? PlayerSelector.Instance.currentInventory : null;
        return inventory != null && inventory.currentSelectedItem is OilPipeline;
    }

    /// <summary>Main：启动发电机。</summary>
    private void TryStart()
    {
        bool fullyConnected = isConnectedWithFuel && car != null && car.isConeectedToCar;

        if (!fullyConnected)
        {
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("油管还没完全接通，得把车和发电机都接上。");
            return;
        }

        if (!isFueled)
        {
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("发电机没燃料，得先把燃料送进去。");
            return;
        }

        if (isStarted)
        {
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("发电机已经在转了。");
            return;
        }

        isStarted = true;
        flagRaisingPlatform.isPowered = true;
        PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("发电机启动了，旗台有电了。");
    }
}
