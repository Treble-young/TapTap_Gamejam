using UnityEngine;

public class PlayerInputLock : MonoBehaviour
{
    private void OnEnable()
    {
        if (PlayerInputManager.Instance != null)
            PlayerInputManager.Instance.LockPlayerInput();
    }

    private void OnDisable()
    {
        if (PlayerInputManager.Instance != null)
            PlayerInputManager.Instance.UnlockPlayerInput();
    }
}
