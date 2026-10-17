using UnityEngine;

/// <summary>
/// @trace LVL-202 / 分阶段门禁
/// 第二关阶段门禁：路牌修复（Level02RoadSign.IsRestored）之后，
/// 才激活后续对象（水和禁字牌拾取物等），实现「路解谜完成后才出现水和禁那条路」。
/// </summary>
public class Level02RoadGate : MonoBehaviour
{
    [SerializeField] private Level02RoadSign roadSign;
    [SerializeField] private GameObject[] revealOnRestored;

    /// <summary>
    /// 进入游戏时先按路牌状态收起后续内容（编辑器里保持可见，方便摆放）。
    /// 路牌修复后再逐个激活，实现「路解完才出现水和禁」。
    /// </summary>
    private void Awake()
    {
        if (roadSign == null || roadSign.IsRestored || revealOnRestored == null)
            return;

        foreach (GameObject go in revealOnRestored)
            if (go != null)
                go.SetActive(false);
    }

    private void Update()
    {
        if (roadSign == null || !roadSign.IsRestored)
            return;

        if (revealOnRestored != null)
        {
            foreach (GameObject go in revealOnRestored)
                if (go != null)
                    go.SetActive(true);
        }

        Destroy(this);
    }
}
