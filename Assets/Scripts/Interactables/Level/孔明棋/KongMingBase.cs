using UnityEngine;

public class KongMingBase : InteractableManager
{
    public GameObject gameUI;

    public override void Interact(PlayerManager player)
    {
        if (gameUI == null)
            return;

        if (gameUI.GetComponent<PlayerInputLock>() == null)
            gameUI.AddComponent<PlayerInputLock>();

        gameUI.SetActive(true);
        HideInventory();
    }

    public void Exit()
    {
        if (gameUI != null)
            gameUI.SetActive(false);

        ShowInventory();
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
