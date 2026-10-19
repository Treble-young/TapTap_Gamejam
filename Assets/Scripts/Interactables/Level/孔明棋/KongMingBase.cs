using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

public class KongMingBase : InteractableManager
{
    public GameObject gameUI;
    public bool isCompleted;
    public bool enablePreviewHotkey = true;

    private KongMingMinigameUI minigame;
    private bool rewardGranted;

    public override void Awake()
    {
        base.Awake();

        // The authored prop has a PlayerInputLock on the root, which would lock input
        // as soon as the scene loads. Only the UI canvas should own the lock.
        if (GetComponent<Canvas>() == null)
        {
            PlayerInputLock[] locks = GetComponents<PlayerInputLock>();
            for (int i = 0; i < locks.Length; i++)
                locks[i].enabled = false;
        }
    }

    public override void Interact(PlayerManager player)
    {
        if (!CanInteractWith(player))
            return;

        if (isCompleted)
        {
            ShowPopUp("绳子已经解开了，快去看看那座断桥吧。");
            return;
        }

        OpenMinigame();
    }

    private void OpenMinigame()
    {
        if (gameUI == null)
            return;

        // The prop root is scaled for the world; keep the overlay canvas independent of it.
        if (gameUI.transform.parent != null)
            gameUI.transform.SetParent(null, false);

        gameUI.transform.localScale = Vector3.one;

        if (!gameUI.activeSelf)
            gameUI.SetActive(true);

        if (gameUI.GetComponent<PlayerInputLock>() == null)
            gameUI.AddComponent<PlayerInputLock>();

        if (minigame == null)
            minigame = gameUI.GetComponent<KongMingMinigameUI>();

        if (minigame == null)
            minigame = gameUI.AddComponent<KongMingMinigameUI>();

        HideInventory();
        minigame.Open(this);
    }

    /// <summary>跳过前置流程，直接打开解绳小游戏。编辑器里按 F8，或在 Inspector 里右键组件调用。</summary>
    [ContextMenu("预览孔明棋（跳过前置）")]
    public void PreviewMinigame()
    {
        // Only the root prop should own the preview entry; the copy on the canvas is inert.
        if (GetComponent<Canvas>() != null)
            return;

        if (gameUI == null)
        {
            Debug.LogWarning("[KongMingBase] gameUI 没有绑定，无法预览。", this);
            return;
        }

        if (isCompleted)
            isCompleted = false;

        if (minigame != null)
            minigame.RestartForPreview();

        OpenMinigame();
    }

#if UNITY_EDITOR
    private void Start()
    {
        if (GetComponent<Canvas>() == null)
            Debug.Log("[孔明棋] 已就绪：按 F8 可直接预览解绳小游戏，不需要前置任务。", this);
    }

    private void Update()
    {
        if (!enablePreviewHotkey)
            return;

        if (GetComponent<Canvas>() != null)
            return;

        if (Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame)
            PreviewMinigame();
    }
#endif

    public void NotifySolved()
    {
        isCompleted = true;
        GrantRopes();
    }

    public void CompleteAndClose()
    {
        GrantRopes();
        CloseMinigame();
    }

    public void CloseMinigame()
    {
        if (gameUI != null)
            gameUI.SetActive(false);

        ShowInventory();
    }

    public void Exit()
    {
        CloseMinigame();
    }

    private void GrantRopes()
    {
        if (rewardGranted)
            return;

        PlayerInventoryManager inventory = PlayerSelector.Instance != null
            ? PlayerSelector.Instance.currentInventory
            : null;
        InventoryItemData ropeAsset = Resources.Load<InventoryItemData>("Minigame/Rope");

        if (inventory == null || ropeAsset == null)
        {
            Debug.LogWarning("[KongMingBase] Could not grant ropes: inventory or Rope asset is missing.", this);
            return;
        }

        int added = 0;
        for (int i = 0; i < 2; i++)
        {
            InventoryItemData rope = Instantiate(ropeAsset);
            rope.name = "Rope_Knotted";
            rope.itemCount = 1;

            if (inventory.TryAdd(rope))
                added++;
            else
                Destroy(rope);
        }

        rewardGranted = added > 0;

        if (added < 2)
            ShowPopUp("背包快满了，只收下了一部分绳子。");
    }

    private static void ShowPopUp(string message)
    {
        if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.popUpManager != null)
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow(message);
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
