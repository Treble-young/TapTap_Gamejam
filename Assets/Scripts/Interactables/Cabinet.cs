using UnityEngine;

public class Cabinet : InteractableManager
{
    public WorldPickupItem[] worldPickupItemSpawnedPrefab;
    public Transform spawnPoint;

    public override void Interact(PlayerManager player)
    {
        base.Interact(player);

        if (worldPickupItemSpawnedPrefab != null)
        {
            for (int i = 0; i < worldPickupItemSpawnedPrefab.Length; i++)
            {
                WorldPickupItem item = Instantiate(worldPickupItemSpawnedPrefab[i], spawnPoint.position, Quaternion.identity);
            }
        }
    }
}
