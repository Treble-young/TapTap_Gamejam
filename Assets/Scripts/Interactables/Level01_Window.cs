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
                PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("这扇窗户被锁住了。也许有人可以帮忙打开它。");
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
                    LevelInventoryTransfer.CaptureFromLevel01();
                    LoadingManager.Instance.LoadNextLevel();
                }
                else
                {
                    PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("终于打开了，我们可以继续前进了。");
                }
            }
        }
        else if (player.playerType == PlayerType.Class01)
        {
            isLocked = false;
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("窗户解锁了，可以让主角打开它。");
        }
    }
}
