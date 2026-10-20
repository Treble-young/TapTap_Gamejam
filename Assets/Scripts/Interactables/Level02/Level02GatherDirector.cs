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

        // 先统计，再发出移动指令，避免已离场 NPC 的即时回调提前显示合并角色。
        foreach (Level02PoolNPC npc in residents)
            if (npc != null && npc.gameObject.activeInHierarchy) pending++;

        foreach (Level02PoolNPC npc in residents)
        {
            if (npc == null || !npc.gameObject.activeInHierarchy)
                continue;
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
        EnsureMergedVisual();
        mergedNPC.SetActive(true);
    }
    // 原型中的 MergedNPC 没有身体；复用参与聚集者的美术，不新增素材。
    // 已经配置的身体图片优先保留；头顶提示不算身体。
    private void EnsureMergedVisual()
    {
        SpriteRenderer body = mergedNPC.GetComponent<SpriteRenderer>();
        Transform visual = mergedNPC.transform.Find("Visual");
        if (body == null && visual != null) body = visual.GetComponent<SpriteRenderer>();
        if (body != null && body.sprite != null) return;
        if (residents == null) return;

        foreach (Level02PoolNPC resident in residents)
        {
            if (resident == null) continue;
            SpriteRenderer source = resident._sprite != null ? resident._sprite : resident.GetComponent<SpriteRenderer>();
            if (source == null || source.sprite == null) continue;
            if (body == null)
            {
                if (visual == null)
                {
                    visual = new GameObject("Visual").transform;
                    visual.SetParent(mergedNPC.transform, false);
                }
                body = visual.gameObject.AddComponent<SpriteRenderer>();
                Vector3 from = source.transform.lossyScale, to = mergedNPC.transform.lossyScale;
                visual.localScale = new Vector3(Mathf.Abs(from.x / (to.x == 0 ? 1 : to.x)),
                    Mathf.Abs(from.y / (to.y == 0 ? 1 : to.y)), 1);
            }
            body.sprite = source.sprite;
            body.sharedMaterial = source.sharedMaterial;
            body.sortingLayerID = source.sortingLayerID;
            body.sortingOrder = source.sortingOrder;
            body.color = Color.white;
            body.enabled = true;
            return;
        }
    }}
