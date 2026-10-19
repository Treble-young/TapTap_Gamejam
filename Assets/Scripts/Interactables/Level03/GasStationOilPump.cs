using UnityEngine;
using System.Collections;

public class GasStationOilPump : InteractableManager
{
    public Car car;
    [Tooltip("油管在油泵侧的连接点（留空则用油泵位置）")]
    public Transform pumpAnchor;

    public bool canUsePump = true;

    [Header("Shake Settings")]
    public float shakeDuration = 0.2f;
    public float shakeMagnitude = 0.05f;
    private bool isShaking = false;

    public override void Interact(PlayerManager player)
    {
        if (!canUsePump)
        {
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("这机器坏了。");
            if (!isShaking)
            {
                StartCoroutine(ShakeCoroutine());
            }
            return;
        }

        if (car == null)
        {
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("这油泵没连车。");
            return;
        }

        // Main 负责连接 / 断开油管
        if (player.playerType == PlayerType.Main)
        {
            if (!car.isConnectedToPump)
            {
                // 连接：用 OilPipeline 连接油泵和车（复用 Car 的输油管）
                if (PlayerHoldsOilPipeline())
                {
                    bool hadGeneratorLine = car.isConeectedToCar;
                    car.oilPump = this;
                    car.ConnectPumpLine();
                    car.ShowThePipeline();
                    PlayerUIManager.Instance.popUpManager.ShowPopUpWindow(hadGeneratorLine
                        ? "我把发电机那头的油管拔了，油泵这头接好了。"
                        : "油泵这头接好了。");
                }
                else
                {
                    PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("我得先拿条输油管，把油泵和车连起来。");
                }
            }
            else
            {
                // 再次点击断开
                car.DisconnectPumpLine();
                car.ShowThePipeline();
                PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("我把油泵这头的油管拔了。");
            }
            return;
        }

        // Class03 负责提供能量（加油）
        if (player.playerType == PlayerType.Class03)
        {
            if (!car.isConnectedToPump)
            {
                PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("得先把油管接到车上，我才能加油。");
                return;
            }

            if (car.isOutOfFuel)
            {
                car.Refuel();
                PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("车加满油了，油管已经拔下来了。");
            }
            else
            {
                PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("车不缺油。");
            }
            return;
        }
    }

    private bool PlayerHoldsOilPipeline()
    {
        PlayerInventoryManager inventory = PlayerSelector.Instance != null ? PlayerSelector.Instance.currentInventory : null;
        return inventory != null && inventory.currentSelectedItem is OilPipeline;
    }

    private IEnumerator ShakeCoroutine()
    {
        isShaking = true;

        Vector3 originalPos = transform.localPosition;
        float elapsed = 0.0f;

        while (elapsed < shakeDuration)
        {
            float xOffset = Random.Range(-1f, 1f) * shakeMagnitude;
            transform.localPosition = originalPos + new Vector3(xOffset, 0, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPos;
        isShaking = false;
    }
}
