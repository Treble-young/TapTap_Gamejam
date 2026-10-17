using UnityEngine;

/// <summary>
/// @trace LVL-206
/// 第二关合并 NPC：荒地里的 NPC 在水井旁汇聚成的单体。
/// 主控与之互动（E），取得一袋破碎的标识碎片（医、殳、酉、聿、曰、馬、尺）。
/// </summary>
public class Level02FragmentGiver : InteractableManager
{
    [Header("碎片配置")]
    [SerializeField] private string bundleName = "破碎的标识";
    [SerializeField] private string[] pieces =
        { "医", "殳", "酉", "聿", "曰", "馬", "尺" };

    [Header("对话文案")]
    [TextArea(1, 3)]
    [SerializeField] private string giveDialogue = "这些…请帮我们…";
    [TextArea(1, 3)]
    [SerializeField] private string afterDialogue = "拜托了……把它们拼回原来的样子吧。";
    [TextArea(1, 3)]
    [SerializeField] private string hintDialogue = "（提示：将破碎的标识组合成文字吧——医+殳+酉、聿+曰、馬+尺）";

    private bool given;
    private HeadIndicator headIndicator;

    private void Awake()
    {
        headIndicator = GetComponent<HeadIndicator>();
        if (headIndicator == null)
            headIndicator = gameObject.AddComponent<HeadIndicator>();
    }

    private void OnEnable()
    {
        if (!given && headIndicator != null)
            headIndicator.Show();
    }

    public override void Interact(PlayerManager player)
    {
        if (player == null || player.playerType != PlayerType.Main ||
            PlayerSelector.Instance == null || player != PlayerSelector.Instance.currentPlayer)
            return;

        if (given)
        {
            ShowMessage(afterDialogue + "\n" + hintDialogue);
            return;
        }

        PlayerInventoryManager inventory = player.playerInventory;
        if (inventory == null)
            return;

        given = true;
        headIndicator.Hide();
        ShowMessage("这些……请帮我们……\n（部首散落在附近的砖地上：医、殳、酉、聿、曰、馬、尺——拾起来，集齐同一个字的部首就能拼成一个完整的字）");
    }

    private static void ShowMessage(string message)
    {
        if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.popUpManager != null)
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow(message);
    }
}
