using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [HideInInspector] public PlayerInputManager playerInput = PlayerInputManager.Instance;

    [Header("Flags")]
    public PlayerState playerState = PlayerState.InputControlling;
    public int playerID = 0;
    public bool isMoving;

    void Start()
    {

        DontDestroyOnLoad(this);
    }

}
