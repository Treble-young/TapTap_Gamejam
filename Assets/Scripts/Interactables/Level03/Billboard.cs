using UnityEngine;

public class Billboard : InteractableManager
{
    [Header("Line")]
    public GameObject line;
    public GameObject linePrefab;
    public Transform lineSpawnPoint;

    public override void Interact(PlayerManager player)
    {
        base.Interact(player);

        if (line != null)
        {
            line.SetActive(false);
        }

        if (linePrefab != null && lineSpawnPoint != null)
        {
            line = Instantiate(linePrefab, lineSpawnPoint.position, lineSpawnPoint.rotation);
        }

        SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = 1;
        }
    }

}
