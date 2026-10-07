using UnityEngine;
using UnityEngine.SceneManagement;

public class Level01_Door : InteractableManager
{
    public bool isLocked = true;

    public override void Interact(PlayerManager player)
    {
        if (player.playerType == PlayerType.Main)
        {
            if (isLocked)
            {
                PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("The door is locked. You need someone to unlock it.");
            }
            else
            {
                Animator animator = GetComponentInChildren<Animator>();
                if (animator != null && interactAnimationNames.TryGetValue(PlayerType.Main, out string animationName))
                {
                    animator.Play(animationName);
                }

                if (SceneManager.GetActiveScene().name == "Level_01")
                {
                    SceneManager.LoadScene("Level_02");
                }
                else
                {
                    PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("The door is unlocked. You can proceed.");
                }
            }
        }
        else if (player.playerType == PlayerType.Class01)
        {
            isLocked = false;
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("The door has been unlocked by Class01. Main player can now proceed.");
        }
    }
}
