using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Campfire : InteractableManager
{
    public List<GameObject> playerDistory = new List<GameObject>();
    public PlayerManager newPlayer;
    public GameObject light2D;

    public override void Interact(PlayerManager player)
    {
        Carpet lantern = PlayerSelector.Instance.currentInventory.currentSelectedItem as Carpet;

        if (lantern != null)
        {
            if (lantern.isWet)
            {
                Destroy(light2D);
                for (int i = 0; i < playerDistory.Count; i++)
                {
                    playerDistory[i].SetActive(false);
                    PlayerSelector.Instance.playerManagers.Remove(playerDistory[i].GetComponent<PlayerManager>());
                    newPlayer.playerState = PlayerState.AutoMoving;

                    PlayerSelector.Instance.ReassignPlayerIDs();
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
