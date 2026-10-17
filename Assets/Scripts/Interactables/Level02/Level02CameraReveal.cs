using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// @trace LVL-205
/// 第二关视角拉高：水井与芦苇丛都修复后，镜头逐渐拉高（Orthographic Size 变大），
/// 展现出更大的区域——上方破败的医院、图书馆，右侧废弃的车站。
/// </summary>
public class Level02CameraReveal : MonoBehaviour
{
    [Header("触发条件")]
    [SerializeField] private Level02Well well;
    [SerializeField] private Level02ReedBed reedBed;

    [Header("拉高参数")]
    [Tooltip("拉高后的 Orthographic Size（留白，按最终区域大小调整）")]
    [SerializeField] private float targetOrthoSize = 10f;
    [SerializeField] private float duration = 2.5f;

    [TextArea(1, 3)]
    [SerializeField] private string revealMessage = "杂草尽数退去，视野豁然开朗——远处是破败的医院与图书馆，右侧是一座废弃的车站。";

    private bool triggered;
    private float startSize;
    private float elapsed;
    private CinemachineCamera cam;

    private void Update()
    {
        if (!triggered)
        {
            if (well == null || reedBed == null || !well.IsRestored || !reedBed.IsRestored)
                return;

            triggered = true;
            if (PlayerInputManager.Instance != null)
                cam = PlayerInputManager.Instance.followCamera;
            if (cam != null)
                startSize = cam.Lens.OrthographicSize;
            else
                enabled = false;

            if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.popUpManager != null)
                PlayerUIManager.Instance.popUpManager.ShowPopUpWindow(revealMessage);
            return;
        }

        if (cam == null || elapsed >= duration)
            return;

        elapsed += Time.deltaTime;
        float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
        LensSettings lens = cam.Lens;
        lens.OrthographicSize = Mathf.Lerp(startSize, targetOrthoSize, t);
        cam.Lens = lens;
    }
}
