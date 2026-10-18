using UnityEngine;

public class Wood : InteractableManager
{
    [Header("Spawn Properties")]
    public Transform spawnPoint;
    public GameObject woodPrefab;

    public override void Interact(PlayerManager player)
    {
        if (interactOnce)
        {
            if (spawnPoint != null && woodPrefab != null)
            {
                Instantiate(woodPrefab, spawnPoint.position, Quaternion.identity);
            }
            interactOnce = false;
        }
    }
}
