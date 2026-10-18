using UnityEngine;

public class Furnace : InteractableManager
{
    [Header("Component")]
    public SpriteRenderer _sprite;

    [Header("Furnace Properties")]
    public int stage = 0; // 0 = need wood, 1 = need lighter, 2 = game_started, 3 = game_completed

    public Sprite notWorkingSprite;
    public Sprite workingSprite;

    [Header("Game Properties")]
    public GameObject gameUI;

    private FishingMinigame fishingMinigame;

    public override void Awake()
    {
        base.Awake();
        if (_sprite == null)
            _sprite = GetComponent<SpriteRenderer>();
    }

    public override void Interact(PlayerManager player)
    {
        InventoryItemData item = player.playerInventory.currentSelectedItem;
        var ui_debug = PlayerUIManager.Instance.popUpManager;

        switch (stage)
        {
            case 0:
                if (item != null && item.ItemName == "Wood")
                {
                    player.playerInventory.RemoveItem(item);
                    stage = 1;
                }
                else
                {
                    ui_debug.ShowPopUpWindow("我得放点木头进去才能点燃火炉。");
                }
                break;
            case 1:
                if (item != null && item is Lighter lighter)
                {
                    if (!lighter.isLit)
                    {
                        ui_debug.ShowPopUpWindow("打火机没点燃，我得先点燃它。");
                        return;
                    }
                    stage = 2;
                    _sprite.sprite = workingSprite;
                }
                else
                {
                    ui_debug.ShowPopUpWindow("我得用打火机点燃火炉。");
                }
                break;
            case 2:
                if (gameUI == null)
                    return;

                if (gameUI.GetComponent<PlayerInputLock>() == null)
                    gameUI.AddComponent<PlayerInputLock>();

                if (fishingMinigame == null)
                    fishingMinigame = gameUI.GetComponent<FishingMinigame>();

                if (fishingMinigame == null)
                    fishingMinigame = gameUI.AddComponent<FishingMinigame>();

                fishingMinigame.onWon.RemoveListener(HandleFishingWon);
                fishingMinigame.onWon.AddListener(HandleFishingWon);

                gameUI.SetActive(true);
                HideInventory();
                fishingMinigame.Show();
                break;
            case 3:
                break;
            default:
                break;
        }
    }

    public void Exit()
    {
        if (fishingMinigame != null)
            fishingMinigame.Cancel();

        if (gameUI != null)
            gameUI.SetActive(false);

        ShowInventory();
    }

    private void HandleFishingWon()
    {
        stage = 3;

        if (_sprite != null && workingSprite != null)
            _sprite.sprite = workingSprite;
    }

    private void HideInventory()
    {
        if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.inventoryManager != null)
            PlayerUIManager.Instance.inventoryManager.HideInventoryWindow();
    }

    private void ShowInventory()
    {
        if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.inventoryManager != null)
            PlayerUIManager.Instance.inventoryManager.ShowInventoryWindow();
    }
}
