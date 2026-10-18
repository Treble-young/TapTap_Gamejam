using UnityEngine;

public class PlayerManager : NPCSpriteRandomSelector
{
    [HideInInspector] public PlayerInputManager playerInput = PlayerInputManager.Instance;
    [HideInInspector] public PlayerMovementManager playerMovement;
    [HideInInspector] public PlayerInteractionManager playerInteraction;
    [HideInInspector] public PlayerInventoryManager playerInventory;

    [Header("Flags")]
    public PlayerType playerType = PlayerType.Main;
    public PlayerState playerState = PlayerState.InputControlling;
    public int playerID = 0;
    public bool canBeInlcudedInPlayerList = true;
    public bool isMoving;
    public bool isUsingLighter = false;
    public SpriteRenderer playerSpriteRenderer;

    void Awake()
    {
        if (playerSpriteRenderer == null)
            playerSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _sprite = playerSpriteRenderer;

        playerMovement = GetComponent<PlayerMovementManager>();
        playerInteraction = GetComponent<PlayerInteractionManager>();
        playerInventory = GetComponent<PlayerInventoryManager>();
        if (playerInventory == null)
            playerInventory = gameObject.AddComponent<PlayerInventoryManager>();
    }

    public void SetOrderInLayer(int order)
    {
        if (playerSpriteRenderer != null)
            playerSpriteRenderer.sortingOrder = order;
    }

    /// <summary>从 Stranger 切换为 AutoMoving（跟随玩家）。</summary>
    public void StartAutoMoving()
    {
        if (playerState == PlayerState.Stranger)
            playerState = PlayerState.AutoMoving;
    }

}
