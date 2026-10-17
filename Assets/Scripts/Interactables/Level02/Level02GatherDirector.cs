using UnityEngine;

/// <summary>
/// @trace LVL-206
/// 第二关 NPC 聚集导演：水井与芦苇丛都修复后，荒地里的定居 NPC 向水井聚集，
/// 汇聚成单体（其余 NPC 到达后消失），留下一个可对话的「合并 NPC」，
/// 主控与之互动可取得破碎的标识碎片。
/// </summary>
public class Level02GatherDirector : MonoBehaviour
{
    [Header("触发条件")]
    [SerializeField] private Level02Well well;
    [SerializeField] private Level02ReedBed reedBed;

    [Header("聚集配置")]
    [Tooltip("参与聚集的定居 NPC")]
    [SerializeField] private Level02PoolNPC[] residents;
    [Tooltip("聚集点（水井旁）")]
    [SerializeField] private Transform gatherPoint;
    [Tooltip("聚集完成后激活的合并 NPC（挂 Level02FragmentGiver）")]
    [SerializeField] private GameObject mergedNPC;

    private bool triggered;
    private int pending;

    private void Update()
    {
        if (triggered || well == null || reedBed == null)
            return;
        if (!well.IsRestored || !reedBed.IsRestored)
            return;

        triggered = true;

        if (residents == null || residents.Length == 0)
        {
            ActivateMerged();
            return;
        }

        foreach (Level02PoolNPC npc in residents)
        {
            if (npc == null || !npc.gameObject.activeInHierarchy)
                continue;
            pending++;
            npc.WalkToGatherPoint(gatherPoint != null ? gatherPoint.position : transform.position, OnNpcArrived);
        }

        if (pending == 0)
            ActivateMerged();
    }

    private void OnNpcArrived()
    {
        pending--;
        if (pending <= 0)
            ActivateMerged();
    }

    private void ActivateMerged()
    {
        if (mergedNPC == null)
            return;
        if (gatherPoint != null)
            mergedNPC.transform.position = gatherPoint.position;
        mergedNPC.SetActive(true);
    }
}
