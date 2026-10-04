using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [HideInInspector] public PlayerInputManager playerInput = PlayerInputManager.Instance;
    [HideInInspector] public PlayerMovementManager playerMovement;
    [HideInInspector] public PlayerInteractionManager playerInteraction;
    [HideInInspector] public PlayerInventoryManager playerInventory;

    [Header("Flags")]
    public PlayerType playerType = PlayerType.Main;
    public PlayerState playerState = PlayerState.InputControlling;
    public int playerID = 0;
    public bool isMoving;
    public bool isUsingLighter = false;

    void Awake()
    {
        playerMovement = GetComponent<PlayerMovementManager>();
        playerInteraction = GetComponent<PlayerInteractionManager>();
        playerInventory = GetComponent<PlayerInventoryManager>();
        if (playerInventory == null)
            playerInventory = gameObject.AddComponent<PlayerInventoryManager>();
    }

    void Start()
    {

        DontDestroyOnLoad(this);
    }

}
