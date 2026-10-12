using UnityEngine;

public class Portal : InteractableManager
{
    public Transform targetPosition;
    public int targetOrderInLayer = 0;
    public LayerMask targetLayerMask;

    public override void Interact(PlayerManager player)
    {
        player.transform.position = targetPosition.position;
        player.SetOrderInLayer(targetOrderInLayer);
        player.gameObject.layer = LayerMaskToLayer(targetLayerMask);
    }

    /// <summary>把单层的 LayerMask 转换成 layer 索引（GameObject.layer 需要索引而非位掩码）。</summary>
    private static int LayerMaskToLayer(LayerMask mask)
    {
        return Mathf.RoundToInt(Mathf.Log(mask.value, 2));
    }
}
