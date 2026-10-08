using UnityEngine;

public class FlagChest : InteractableManager
{
    public GameObject flagPrefab;
    public Transform spawnPoint;

    public override void Interact(PlayerManager player)
    {
        if (interactOnce)
        {
            interactionArea.enabled = false;
        }

        if (flagPrefab != null)
        {
            GameObject item = Instantiate(flagPrefab, spawnPoint.position, Quaternion.identity);
        }
    }

}
