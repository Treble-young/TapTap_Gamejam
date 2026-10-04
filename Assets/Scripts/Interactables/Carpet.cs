using UnityEngine;


[CreateAssetMenu(fileName = "Carpet", menuName = "Scriptable Objects/Item/Carpet")]
public class Carpet : InventoryItemData
{
    public bool isWet = false;
    public Sprite wetIcon;

    public override void Use(PlayerManager player)
    {
    }
}
