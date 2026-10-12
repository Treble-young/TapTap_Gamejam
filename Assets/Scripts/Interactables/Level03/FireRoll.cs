using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

public class FireRoll : InteractableManager
{
    [Header("Fire Roll Settings")]
    public bool isOpen = false;

    [Header("Oilpipe Line Settings")]
    public GameObject oilPipelinePrefab;
    public Transform oilPipelineSpawnPoint;
    public bool hasOilPipelineSpawned = false;

    [Header("Hand Crank Settings")]
    public HandCrank handCrank; // Reference to the Hand Crank item

    [Header("UI Settings")]
    public GameObject menu;
    public Button installHandCrankButton;
    public Button removeHandCrankButton;
    public Button operateDoorButton;
    public Button exitButton;

    public override void Interact(PlayerManager player)
    {
        if (menu != null)
        {
            menu.SetActive(true);

            UpdateMenu();
        }
    }

    private void UpdateMenu()
    {
        if (!handCrank && PlayerSelector.Instance.currentInventory.currentSelectedItem is HandCrank)
        {
            installHandCrankButton.interactable = true;
        }
        else
        {
            installHandCrankButton.interactable = false;
        }

        if (handCrank != null)
        {
            removeHandCrankButton.interactable = true;
        }
        else
        {
            removeHandCrankButton.interactable = false;
        }

        if (handCrank != null)
        {
            operateDoorButton.interactable = true;
        }
        else
        {
            operateDoorButton.interactable = false;
        }
    }

    public void Exit()
    {
        if (menu != null)
        {
            menu.SetActive(false);
        }
    }

    public void InstallHandCrank()
    {
        if (handCrank == null && PlayerSelector.Instance.currentInventory.currentSelectedItem is HandCrank selectedHandCrank)
        {
            handCrank = selectedHandCrank;
            PlayerSelector.Instance.currentInventory.RemoveItem(selectedHandCrank);
            UpdateMenu();
        }
    }

    public void RemoveHandCrank()
    {
        if (handCrank != null)
        {
            PlayerSelector.Instance.currentInventory.TryAdd(handCrank);
            handCrank = null;
            UpdateMenu();
        }
    }

    public void OperateDoor()
    {
        if (handCrank != null)
        {
            // Implement the logic to operate the door using the hand crank
            Animator animator = GetComponentInChildren<Animator>();
            if (animator != null)
            {
                if (isOpen)
                {
                    // animator.SetTrigger("Close");
                    isOpen = false;
                }
                else
                {
                    // animator.SetTrigger("Open");
                    isOpen = true;
                    if (!hasOilPipelineSpawned && oilPipelinePrefab != null && oilPipelineSpawnPoint != null)
                    {
                        Instantiate(oilPipelinePrefab, oilPipelineSpawnPoint.position, oilPipelineSpawnPoint.rotation);
                        hasOilPipelineSpawned = true;
                    }
                }
            }
        }
    }

}
