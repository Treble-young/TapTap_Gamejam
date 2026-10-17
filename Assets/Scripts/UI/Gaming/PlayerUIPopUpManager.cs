using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerUIPopUpManager : MonoBehaviour
{
    public GameObject popUpWindow;

    public TextMeshProUGUI popUpText;

    [Header("自动隐藏")]
    [Tooltip("弹窗默认显示多少秒后自动消失（<=0 表示一直显示，直到玩家手动关闭）")]
    [SerializeField] private float autoHideDelay = 4f;

    private float hideTimer;

    public bool IsShowing => popUpWindow != null && popUpWindow.activeSelf;

    /// <summary>
    /// 显示提示。duration 为自动隐藏秒数，传负数（默认）时使用 autoHideDelay。
    /// 玩家按 E 或点窗口仍会立即关闭。
    /// </summary>
    public void ShowPopUpWindow(string message, float duration = -1f)
    {
        if (popUpWindow != null && popUpText != null)
        {
            popUpText.text = message;
            popUpWindow.SetActive(true);
            hideTimer = duration >= 0f ? duration : autoHideDelay;
        }
    }

    private void Update()
    {
        // 暂停菜单打开时不倒计时，避免提示在暂停期间悄悄消失
        if (!IsShowing || Time.timeScale == 0f || autoHideDelay <= 0f)
            return;

        if (hideTimer <= 0f)
            return;

        hideTimer -= Time.deltaTime;
        if (hideTimer <= 0f)
            HidePopUpWindow();
    }

    public void HidePopUpWindow()
    {
        if (popUpWindow != null)
        {
            popUpWindow.SetActive(false);
        }
    }
}
