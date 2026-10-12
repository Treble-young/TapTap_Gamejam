using UnityEngine;

public class GasStationOilPump : InteractableManager
{
    public Car car;
    [Tooltip("油管在油泵侧的连接点（留空则用油泵位置）")]
    public Transform pumpAnchor;

    public override void Interact(PlayerManager player)
    {
        if (car == null)
        {
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("No car connected to this pump.");
            return;
        }

        // Main 负责连接 / 断开油管
        if (player.playerType == PlayerType.Main)
        {
            if (!car.isConnectedToPump)
            {
                // 连接：用 OilPipeline 连接油泵和车（复用 Car 的输油管）
                if (PlayerSelector.Instance.currentInventory &&
                    PlayerSelector.Instance.currentInventory.currentSelectedItem is OilPipeline)
                {
                    car.isConnectedToPump = true;
                    car.ShowThePipeline();
                    PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("Pump connected.");
                }
                else
                {
                    PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("You need a fuel line to connect the pump to the car.");
                }
            }
            else
            {
                // 再次点击断开
                car.isConnectedToPump = false;
                car.ShowThePipeline();
                PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("Pipeline disconnected.");
            }
            return;
        }

        // Class03 负责提供能量（加油）
        if (player.playerType == PlayerType.Class03)
        {
            if (!car.isConnectedToPump)
            {
                PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("Connect the fuel line first.");
                return;
            }

            if (car.isOutOfFuel)
            {
                car.Refuel();
                PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("Car refueled.");
            }
            else
            {
                PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("Car is already fueled.");
            }
            return;
        }
    }
}
