using UnityEngine;
using Unity.Cinemachine;

public class PlayerInputManager : MonoBehaviour
{
    public static PlayerInputManager Instance;

    [HideInInspector] public CinemachineCamera followCamera;

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
        if (followCamera == null)
        {
            followCamera = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None)[0];
        }

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
