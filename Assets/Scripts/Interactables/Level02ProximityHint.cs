using UnityEngine;

/// <summary>
/// 靠近提示：主角进入 Trigger 范围时弹出一条提示文本（默认只弹一次）。
/// 用于「应该放在这里吧」「将它们放到合适的位置吧」这类主控气泡。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Level02ProximityHint : MonoBehaviour
{
    [TextArea(1, 3)]
    [SerializeField] private string hint = "将它们放到合适的位置吧";
    [SerializeField] private bool onlyOnce = true;

    private bool shown;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (shown && onlyOnce)
            return;

        PlayerManager player = other.GetComponent<PlayerManager>();
        if (player == null || player.playerType != PlayerType.Main)
            return;

        shown = true;
        if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.popUpManager != null)
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow(hint);
    }
}
