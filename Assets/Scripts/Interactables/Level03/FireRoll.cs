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
    public GameObject handCrankPoint;
    public SpriteRenderer handCrankPointSpriteRenderer;
    public Sprite handCrankUpSprite;
    public Sprite handCrankDownSprite;
    public Sprite noHandCrankSprite;

    [Header("UI Settings")]
    public GameObject menu;
    public Button installHandCrankButton;
    public Button removeHandCrankButton;
    public Button operateDoorButton;
    public Button exitButton;

    public bool needDebug = true;

    [Header("Door Sprite Settings")]
    public SpriteRenderer _sprite;
    public Sprite emptySprite;

    public override void Awake()
    {
        base.Awake();
        if (_sprite == null)
            _sprite = GetComponentInChildren<SpriteRenderer>();
    }

    public override void Interact(PlayerManager player)
    {
        if (needDebug && (player.playerInventory.currentSelectedItem == null || player.playerInventory.currentSelectedItem is not HandCrank))
        {
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("也许我可以用刚刚控制卷帘门的手摇柄来操作这个装置。");
            return;
        }

        if (menu != null)
        {
            if (player.playerInventory.currentSelectedItem is HandCrank || needDebug)
            {
                needDebug = false;
            }

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
        PlayerUIManager.Instance.PlayClickSound();

        if (menu != null)
        {
            menu.SetActive(false);
        }
    }

    public void InstallHandCrank()
    {
        PlayerUIManager.Instance.PlayClickSound();

        if (handCrank == null && PlayerSelector.Instance.currentInventory.currentSelectedItem is HandCrank selectedHandCrank)
        {
            handCrank = selectedHandCrank;
            PlayerSelector.Instance.currentInventory.RemoveItem(selectedHandCrank);
            SetHandCrankSprite(true);
            UpdateMenu();
        }
    }

    public void RemoveHandCrank()
    {
        PlayerUIManager.Instance.PlayClickSound();

        if (handCrank != null)
        {
            PlayerSelector.Instance.currentInventory.TryAdd(handCrank);
            handCrank = null;
            SetNoHandCrankSprite();
            UpdateMenu();
        }
    }

    public void OperateDoor()
    {
        PlayerUIManager.Instance.PlayClickSound();

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

                        if (_sprite != null && emptySprite != null)
                        {
                            _sprite.sprite = emptySprite;
                        }
                    }
                }

                SetHandCrankSprite(isOpen);
            }
        }
    }

    private void SetHandCrankSprite(bool pulledUp)
    {
        if (handCrankPointSpriteRenderer == null && handCrankPoint != null)
            handCrankPointSpriteRenderer = handCrankPoint.GetComponent<SpriteRenderer>();

        if (handCrankPointSpriteRenderer == null)
            return;

        handCrankPointSpriteRenderer.sprite = pulledUp ? handCrankUpSprite : handCrankDownSprite;
    }

    private void SetNoHandCrankSprite()
    {
        if (handCrankPointSpriteRenderer == null && handCrankPoint != null)
            handCrankPointSpriteRenderer = handCrankPoint.GetComponent<SpriteRenderer>();

        if (handCrankPointSpriteRenderer == null)
            return;

        handCrankPointSpriteRenderer.sprite = noHandCrankSprite;
    }

}
