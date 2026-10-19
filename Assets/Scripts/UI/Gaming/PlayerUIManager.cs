using UnityEngine;

public class PlayerUIManager : MonoBehaviour
{
    public static PlayerUIManager Instance { get; private set; }

    public bool isUIActive = false;

    public AudioClip clickSound;

    public void PlayClickSound()
    {
        if (clickSound != null && WorldAudioManager.Instance != null)
        {
            WorldAudioManager.Instance.PlaySFX(clickSound);
        }
    }

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
    [HideInInspector] public DrawingInUI drawingInUI;

    public GameObject endGameUI;

    private void Start()
    {
        popUpManager = GetComponent<PlayerUIPopUpManager>();
        menuManager = GetComponent<PlayerUIManuManager>();
        // 快捷栏可能因为加载画面/解谜 UI 处于隐藏状态，所以要连未激活的子物体一起找
        inventoryManager = GetComponentInChildren<HotbarUI>(true);

        if (inventoryManager != null)
        {
            // 先记录场景里真实的初始状态，再按加载画面立刻同步一次
            inventoryManager.CaptureInitialState();
            inventoryManager.ApplyVisibility();
        }

        drawingInUI = GetComponentInChildren<DrawingInUI>();
    }

    private void Update()
    {
        // 快捷栏隐藏时它自己的 Update 不会跑，只能由这里（始终激活）代它同步加载画面的状态
        if (inventoryManager != null)
            inventoryManager.ApplyVisibility();
    }
}
