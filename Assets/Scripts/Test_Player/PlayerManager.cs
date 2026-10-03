using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [HideInInspector] public PlayerInputManager playerInput = PlayerInputManager.Instance;
    [HideInInspector] public PlayerMovementManager playerMovement;
    [HideInInspector] public PlayerInteractionManager playerInteraction;

    [Header("Flags")]
    public PlayerType playerType = PlayerType.Main;
    public PlayerState playerState = PlayerState.InputControlling;
    public int playerID = 0;
    public bool isMoving;

    void Awake()
    {
        playerMovement = GetComponent<PlayerMovementManager>();
        playerInteraction = GetComponent<PlayerInteractionManager>();
    }

    void Start()
    {

        DontDestroyOnLoad(this);
    }

}
