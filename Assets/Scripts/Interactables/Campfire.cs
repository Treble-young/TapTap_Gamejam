using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Campfire : InteractableManager
{
    public List<GameObject> playerDistory = new List<GameObject>();
    public PlayerManager newPlayer;
    public GameObject light2D;

    private bool isExtinguished;

    public override void Interact(PlayerManager player)
    {
        if (isExtinguished || player == null || PlayerSelector.Instance == null)
            return;

        Carpet carpet = PlayerSelector.Instance.currentInventory.currentSelectedItem as Carpet;

        if (carpet != null)
        {
            if (carpet.isWet)
            {
                isExtinguished = true;
                if (light2D != null)
                    Destroy(light2D);

                for (int i = 0; i < playerDistory.Count; i++)
                {
                    if (playerDistory[i] == null)
                        continue;

                    playerDistory[i].SetActive(false);
                    PlayerSelector.Instance.playerManagers.Remove(playerDistory[i].GetComponent<PlayerManager>());
                }

                // 幸存者仍是陌生人；只有主角举着打火机与他交互后才入队。
                if (newPlayer != null)
                {
                    newPlayer.playerState = PlayerState.Stranger;
                    RecruitableStranger recruitable = newPlayer.GetComponent<RecruitableStranger>();
                    if (recruitable == null)
                        recruitable = newPlayer.gameObject.AddComponent<RecruitableStranger>();
                    recruitable.Initialize(newPlayer);
                }

                // 火堆不再占用 E 交互，避免挡住对幸存者的交互。
                if (interactionArea != null)
                    interactionArea.enabled = false;
                foreach (PlayerManager teammate in PlayerSelector.Instance.playerManagers)
                {
                    if (teammate != null && teammate.playerInteraction != null)
                        teammate.playerInteraction.RemoveInteractable(this);
                }

                Animator animator = GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    if (player.playerType == PlayerType.Main)
                    {
                        animator.Play(interactAnimationNames[PlayerType.Main]);
                    }
                }
            }
            else
            {
                PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("The carpet is not wet, cannot extinguish the fire.");
            }
        }
    }
}
