using UnityEngine;

public class PlayerUIManager : MonoBehaviour
{
    public static PlayerUIManager Instance { get; private set; }

    public bool isUIActive = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    [HideInInspector] public PlayerUIPopUpManager popUpManager;
    [HideInInspector] public PlayerUIManuManager menuManager;
    [HideInInspector] public HotbarUI inventoryManager;

    private void Start()
    {
        popUpManager = GetComponent<PlayerUIPopUpManager>();
        menuManager = GetComponent<PlayerUIManuManager>();
        inventoryManager = GetComponentInChildren<HotbarUI>();

    }
}
