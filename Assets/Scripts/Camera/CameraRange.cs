using UnityEngine;
using Unity.Cinemachine;

public class CameraRange : MonoBehaviour
{
    void Start()
    {
        Collider2D bounds = GetComponent<Collider2D>();
        CinemachineCamera cam = ResolveCamera();
        if (bounds == null || cam == null)
            return;

        CinemachineConfiner2D confiner = cam.GetComponent<CinemachineConfiner2D>();
        if (confiner == null)
            confiner = cam.gameObject.AddComponent<CinemachineConfiner2D>();

        confiner.BoundingShape2D = bounds;
    }

    private static CinemachineCamera ResolveCamera()
    {
        if (PlayerInputManager.Instance != null && PlayerInputManager.Instance.followCamera != null)
            return PlayerInputManager.Instance.followCamera;

        return FindFirstObjectByType<CinemachineCamera>();
    }
}
