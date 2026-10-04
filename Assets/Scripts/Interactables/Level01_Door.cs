using UnityEngine;

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
                // Unlock the door and allow the player to proceed
                PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("The door is unlocked. You can proceed.");
                // Add logic to transition to the next level or scene here

                Animator animator = GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    animator.Play(interactAnimationNames[PlayerType.Main]);
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
