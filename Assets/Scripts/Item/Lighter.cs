using UnityEngine;

[CreateAssetMenu(fileName = "Lighter", menuName = "Scriptable Objects/Item/Lighter")]
public class Lighter : InventoryItemData
{
    public GameObject lighterPrefab;
    public Sprite usedIcon;

    private GameObject gameObject;

    public override void Use(PlayerManager player)
    {
        if (lighterPrefab != null && gameObject == null)
        {
            GameObject gameObject = GameObject.Instantiate(lighterPrefab, player.transform.position, Quaternion.identity);
            gameObject.transform.SetParent(player.transform);
            this.gameObject = gameObject;

            player.isUsingLighter = true;

            ToggleIcon(usedIcon);
        }
        else if (gameObject != null)
        {
            GameObject.Destroy(gameObject);
            gameObject = null;

            player.isUsingLighter = false;

            ToggleIcon(icon);
        }
    }
}
