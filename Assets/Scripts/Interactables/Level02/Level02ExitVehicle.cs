using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// @trace LVL-208
/// 第二关出口车辆：三处建筑全部修复后出现在车站。
/// 主控走至车辆处互动（E）上车，短暂演出后离开此关卡，加载下一场景。
/// </summary>
public class Level02ExitVehicle : InteractableManager
{
    [Tooltip("要加载的下一场景名（须已加入 Build Settings）")]
    [SerializeField] private string nextSceneName = "Level_03";
    [SerializeField] private float departDelay = 2f;

    [TextArea(1, 3)]
    [SerializeField] private string boardMessage = "上车了。车辆缓缓启动，驶离这片重归完整的荒地……";

    private bool boarded;

    public override void Interact(PlayerManager player)
    {
        if (boarded || player == null || player.playerType != PlayerType.Main ||
            PlayerSelector.Instance == null || player != PlayerSelector.Instance.currentPlayer)
            return;

        boarded = true;
        StartCoroutine(DepartRoutine(player));
    }

    private IEnumerator DepartRoutine(PlayerManager player)
    {
        if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.popUpManager != null)
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow(boardMessage);

        // 上车演出：主控藏进车里（占位，美术后补）
        if (player != null)
        {
            SpriteRenderer sr = player.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.enabled = false;
        }

        yield return new WaitForSeconds(departDelay);

        if (!string.IsNullOrEmpty(nextSceneName))
            LoadingManager.Instance.LoadNextLevel();
    }
}
