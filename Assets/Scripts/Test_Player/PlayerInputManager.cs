using UnityEngine;
using Unity.Cinemachine;

public class PlayerInputManager : MonoBehaviour
{
    public static PlayerInputManager Instance;

    InputSystem_Actions playerInput;
    public CinemachineCamera followCamera;

    [Header("Switch")]
    public bool switch_prev_player;
    public bool switch_next_player;

    [Header("Movement")]
    public Vector2 MovementInput { get; private set; }

    [Header("Interaction")]
    public bool interact;



    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    void Start()
    {
        followCamera = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None)[0];

        DontDestroyOnLoad(this);
    }

    void Update()
    {
        HandleInteractInput();
        HandleSwitchInput();
    }

    void OnEnable()
    {
        if (playerInput == null)
        {
            playerInput = new InputSystem_Actions();

            playerInput.Player.Move.performed += ctx => MovementInput = ctx.ReadValue<Vector2>();
            playerInput.Player.Move.canceled += ctx => MovementInput = Vector2.zero;

            playerInput.Player.Interact.performed += ctx => interact = true;

            playerInput.Player.Previous.performed += ctx => switch_prev_player = true;
            playerInput.Player.Next.performed += ctx => switch_next_player = true;
        }

        playerInput.Enable();
    }

    public void HandleInteractInput()
    {
        if (interact)
        {
            interact = false;

            PlayerSelector.Instance.currentPlayer.playerInteraction.Interact();
        }
    }

    public void HandleSwitchInput()
    {
        if (switch_prev_player)
        {
            switch_prev_player = false;

            PlayerSelector.Instance.currentPlayerID--;
            if (PlayerSelector.Instance.currentPlayerID < 0)
                PlayerSelector.Instance.currentPlayerID = PlayerSelector.Instance.playerManagers.Count - 1;
        }

        if (switch_next_player)
        {
            switch_next_player = false;

            PlayerSelector.Instance.currentPlayerID++;
            if (PlayerSelector.Instance.currentPlayerID >= PlayerSelector.Instance.playerManagers.Count)
                PlayerSelector.Instance.currentPlayerID = 0;
        }
    }
}
