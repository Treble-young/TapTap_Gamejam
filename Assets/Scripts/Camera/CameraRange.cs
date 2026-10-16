using UnityEngine;
using Unity.Cinemachine;

public class CameraRange : MonoBehaviour
{
    private Collider2D bounds;
    private CinemachineConfiner2D confiner;

    void Start()
    {
        bounds = GetComponent<Collider2D>();
        CinemachineCamera cam = ResolveCamera();
        if (bounds == null || cam == null)
            return;

        confiner = cam.GetComponent<CinemachineConfiner2D>();
        if (confiner == null)
            confiner = cam.gameObject.AddComponent<CinemachineConfiner2D>();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        if (bounds != null && confiner != null)
            confiner.BoundingShape2D = bounds;
    }

    private static CinemachineCamera ResolveCamera()
    {
        if (PlayerInputManager.Instance != null && PlayerInputManager.Instance.followCamera != null)
            return PlayerInputManager.Instance.followCamera;

        return FindFirstObjectByType<CinemachineCamera>();
    }
}
