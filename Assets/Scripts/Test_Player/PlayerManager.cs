using UnityEngine;
using Unity.Cinemachine;

public class PlayerManager : MonoBehaviour
{
    [HideInInspector] public PlayerInputManager playerInput = PlayerInputManager.Instance;
    [HideInInspector] public CinemachineCamera followCamera;

    [Header("Flags")]
    public PlayerState playerState = PlayerState.InputControlling;
    public int playerID = 0;
    public bool isMoving;

    void Start()
    {
        if (followCamera == null)
        {
            followCamera = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None)[0];
        }

        DontDestroyOnLoad(this);
    }

}
