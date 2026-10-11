using UnityEngine;

public class Portal : InteractableManager
{
    public Transform targetPosition;
    public int targetOrderInLayer = 0;

    public override void Interact(PlayerManager player)
    {
        player.transform.position = targetPosition.position;
        player.SetOrderInLayer(targetOrderInLayer);
    }
}
