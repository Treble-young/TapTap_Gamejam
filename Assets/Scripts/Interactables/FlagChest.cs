using UnityEngine;

public class FlagChest : InteractableManager
{
    public GameObject flagPrefab;
    public Transform spawnPoint;

    public SpriteRenderer sprite;
    public Sprite emptyChestSprite;

    public override void Awake()
    {
        base.Awake();

        if (sprite == null)
        {
            sprite = GetComponentInChildren<SpriteRenderer>();
        }
    }

    public override void Interact(PlayerManager player)
    {
        if (interactOnce)
        {
            interactionArea.enabled = false;
        }

        if (flagPrefab != null)
        {
            GameObject item = Instantiate(flagPrefab, spawnPoint.position, Quaternion.identity);
            sprite.sprite = emptyChestSprite; // 更改宝箱的精灵为打开状态
        }
    }

}
