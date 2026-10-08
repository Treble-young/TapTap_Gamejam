using UnityEngine;
using Unity.Cinemachine;


public class PlayerInputManager : MonoBehaviour
{
    public static PlayerInputManager Instance;
    public PlayerUIManuManager manuManager;

    PlayerControls playerInput;
    public CinemachineCamera followCamera;

    [Header("Switch")]
    public bool switch_prev_player;
    public bool switch_next_player;

    [Header("Movement")]
    public Vector2 MovementInput { get; private set; }

    [Header("Interaction")]
    public bool interact;

    [Header("Use Item")]
    public bool use_item;

    [Header("UI")]
    public bool openMenu;

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
    }

    void Update()
    {
        HandleInteractInput();
        HandleSwitchInput();
        HandleUseItemInput();
        HandleOpenMenuInput();
    }

    void OnEnable()
    {
        if (playerInput == null)
        {
            playerInput = new PlayerControls();

            playerInput.Movement.Move.performed += ctx => MovementInput = ctx.ReadValue<Vector2>();
            playerInput.Movement.Move.canceled += ctx => MovementInput = Vector2.zero;

            playerInput.Actions.Interact.performed += ctx => interact = true;

            playerInput.Actions.UseItem.performed += ctx => use_item = true;

            playerInput.Actions.Previous.performed += ctx => switch_prev_player = true;
            playerInput.Actions.Next.performed += ctx => switch_next_player = true;

            playerInput.UI.Menu.performed += ctx => openMenu = true;
        }

        playerInput.Enable();
    }

    void OnDisable()
    {
        playerInput.Disable();
    }

    public void HandleInteractInput()
    {
        if (!interact)
            return;

        interact = false;

        if (ShoppingCart.ActiveDrivingCart != null)
        {
            ShoppingCart.ActiveDrivingCart.StopDriving();
            return;
        }

        // 1. 有弹窗时先关闭弹窗
        if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.popUpManager != null)
        {
            PlayerUIPopUpManager popUp = PlayerUIManager.Instance.popUpManager;
            if (popUp.IsShowing)
            {
                popUp.HidePopUpWindow();
                return;
            }
        }

        // 2. 优先拾取
        if (FKeyPickupController.Instance != null && FKeyPickupController.Instance.TryPickUp())
            return;

        // 3. 其次与物体交互
        PlayerManager current = PlayerSelector.Instance != null ? PlayerSelector.Instance.currentPlayer : null;
        if (current != null && current.playerInteraction != null && current.playerInteraction.Interact())
            return;
    }

    public void HandleSwitchInput()
    {
        if (switch_prev_player)
        {
            switch_prev_player = false;

            PlayerSelector.Instance.SelectPrevious();
        }

        if (switch_next_player)
        {
            switch_next_player = false;

            PlayerSelector.Instance.SelectNext();
        }
    }

    public void HandleUseItemInput()
    {
        if (!use_item)
            return;

        use_item = false;

        if (PlayerSelector.Instance != null)
            PlayerSelector.Instance.UseCurrentSelectedItem();
    }

    public void HandleOpenMenuInput()
    {
        if (openMenu)
        {
            openMenu = false;

            // 驾驶购物车时，Esc 用于下车，不打开暂停菜单
            if (ShoppingCart.ActiveDrivingCart != null)
            {
                ShoppingCart.ActiveDrivingCart.StopDriving();
                return;
            }

            if (manuManager != null)
            {
                if (manuManager.menuWindow.activeSelf == true)
                {
                    manuManager.Hide();
                }
                else
                {
                    manuManager.Show();
                }
            }
        }
    }
}
