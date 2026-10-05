using UnityEngine;

public class Cabinet : InteractableManager
{
    public GameObject[] worldPickupItemSpawnedPrefab;
    public Transform spawnPoint;

    public override void Interact(PlayerManager player)
    {
        base.Interact(player);

        if (worldPickupItemSpawnedPrefab != null)
        {
            for (int i = 0; i < worldPickupItemSpawnedPrefab.Length; i++)
            {
                GameObject item = Instantiate(worldPickupItemSpawnedPrefab[i], spawnPoint.position, Quaternion.identity);
            }
        }
    }
}
