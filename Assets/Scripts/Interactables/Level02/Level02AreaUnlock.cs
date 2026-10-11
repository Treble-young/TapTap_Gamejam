using UnityEngine;

public class Level02AreaUnlock : MonoBehaviour
{
    [Header("需要完成的两个目标")]
    [SerializeField] private Level02Well well;
    [SerializeField] private Level02ReedBed reedBed;

    [Header("解锁后的场景变化")]
    [SerializeField] private GameObject newArea;
    [SerializeField] private GameObject entranceBlock;

    private bool unlocked;

    private void Start()
    {
        if (newArea != null)
            newArea.SetActive(false);
    }

    private void Update()
    {
        if (unlocked || well == null || reedBed == null || newArea == null)
            return;

        // 两块字牌都归位后才解锁，顺序不限。
        if (!well.IsRestored || !reedBed.IsRestored)
            return;

        unlocked = true;

        newArea.SetActive(true);

        if (entranceBlock != null)
            entranceBlock.SetActive(false);

        // 场景碰撞发生变化后，让跟随角色重新识别道路。
        Physics2D.SyncTransforms();

        if (PathfindingManager.Instance != null)
            PathfindingManager.Instance.BuildGrid();
    }
}