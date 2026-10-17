using UnityEngine;

/// <summary>
/// @trace LVL-208
/// 第二关终局导演：医院、图书馆、车站三处标牌全部修复后，
/// 废弃破败的场所恢复正常，车站处出现车辆。
/// 主控走至车辆处互动上车，离开此关卡。
/// </summary>
public class Level02ExitController : MonoBehaviour
{
    [Header("三处建筑")]
    [SerializeField] private Level02Building hospital;
    [SerializeField] private Level02Building library;
    [SerializeField] private Level02Building station;

    [Header("终局")]
    [Tooltip("初始隐藏的车辆，全部修复后出现在车站")]
    [SerializeField] private GameObject vehicle;

    [TextArea(1, 3)]
    [SerializeField] private string vehicleArrivedMessage = "三处标牌都恢复了原样。远处传来引擎声——车站那边，来了一辆车！";

    private bool triggered;

    private void Start()
    {
        if (vehicle != null)
            vehicle.SetActive(false);
    }

    private void Update()
    {
        if (triggered || hospital == null || library == null || station == null)
            return;
        if (!hospital.IsRestored || !library.IsRestored || !station.IsRestored)
            return;

        triggered = true;

        if (vehicle != null)
            vehicle.SetActive(true);

        if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.popUpManager != null)
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow(vehicleArrivedMessage);
    }
}
