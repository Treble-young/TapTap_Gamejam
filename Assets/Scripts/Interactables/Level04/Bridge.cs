using UnityEngine;

/// <summary>
/// 断桥：玩家拿着绳子、且图纸已经更新到「半张」（stage 1）时，才能把石头扔到对岸量长度。
/// 扔出去会消耗石头 + 绳子，并让图纸 UI 的长度输入开始作数（hasThrewStone）。
/// </summary>
public class Bridge : InteractableManager
{
    [Tooltip("扔石头要求图纸至少到的阶段（1 = 半张）")]
    public int requiredDrawingStage = 1;

    public override void Interact(PlayerManager player)
    {
        PlayerInventoryManager inventory = PlayerSelector.Instance != null ? PlayerSelector.Instance.currentInventory : null;
        DrawingInUI drawingInUI = PlayerUIManager.Instance != null ? PlayerUIManager.Instance.drawingInUI : null;

        bool holdingRope = inventory != null && inventory.currentSelectedItem != null &&
                           inventory.currentSelectedItem.ItemName == "Rope";

        // 1) 手里没绳子
        if (!holdingRope)
        {
            ShowPopUp("这座桥断了，无法通行。");
            return;
        }

        if (drawingInUI == null)
            return;

        // 2) 图纸还没更新到「半张」：先去找工程师问尺寸
        Drawing drawing = drawingInUI.HeldDrawing;
        if (drawing == null || drawing.drawingStage < requiredDrawingStage)
        {
            ShowPopUp("图纸还没更新，我得先弄清楚这座桥的尺寸。");
            return;
        }

        // 3) 石头已经扔过了
        if (drawingInUI.hasThrewStone)
        {
            ShowPopUp("石头已经扔过去了，现在该量出桥的长度了。");
            return;
        }

        // 4) 只有绳子没有石头
        if (!drawingInUI.HasItem("Stone"))
        {
            ShowPopUp("光有绳子不够，我还得找块石头绑上。");
            return;
        }

        // 5) 扔出去：消耗石头 + 绳子、标记已扔、换图纸 UI 的图
        if (!drawingInUI.ThrowStone())
            return;

        ShowPopUp("扔过去了，现在可以算出桥的长度了。");
    }

    private static void ShowPopUp(string message)
    {
        if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.popUpManager != null)
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow(message);
    }
}
