using UnityEngine;

public class PlayerInputManager : MonoBehaviour
{
    public static PlayerInputManager Instance;

    InputSystem_Actions playerInput;

    [Header("Movement")]
    public Vector2 MovementInput { get; private set; }

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

        DontDestroyOnLoad(this);
    }

    void OnEnable()
    {
        if (playerInput == null)
        {
            playerInput = new InputSystem_Actions();

            playerInput.Player.Move.performed += ctx => MovementInput = ctx.ReadValue<Vector2>();
            playerInput.Player.Move.canceled += ctx => MovementInput = Vector2.zero;
        }

        playerInput.Enable();
    }
}
