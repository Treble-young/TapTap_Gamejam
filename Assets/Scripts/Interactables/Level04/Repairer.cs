using UnityEngine;

public class Repairer : InteractableManager
{
    public bool isRepaired = false;

    [Header("Spawned Item")]
    public GameObject spawnedItemPrefab;
    public Transform spawnPoint;
    public bool hasSpawnedItem = false;

    public override void Interact(PlayerManager player)
    {
        if (!isRepaired)
        {
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("没有齿轮和螺丝怎么修啊！！！");
        }
        else
        {
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("修好了！我把这张图纸给你，或许能帮你修好那条断桥。");

            if (!hasSpawnedItem && spawnedItemPrefab != null && spawnPoint != null)
            {
                GameObject spawnedItem = Instantiate(spawnedItemPrefab, spawnPoint.position, spawnPoint.rotation);
                hasSpawnedItem = true;
            }
        }
    }
}
