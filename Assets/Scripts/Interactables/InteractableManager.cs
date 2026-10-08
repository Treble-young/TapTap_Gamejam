using UnityEngine;

public class InteractableManager : MonoBehaviour
{
    [Header("Interactable Settings")]
    public Collider2D interactionArea;
    public PlayerType[] canInteractWithTypes;
    public bool interactOnce = true;

    [Header("Animation Settings")]
    public Animator animator;
    public PlayerTypeStringDictionary interactAnimationNames = new PlayerTypeStringDictionary();

    public virtual void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }

    public virtual void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerManager player = collision.GetComponent<PlayerManager>();
        if (player != null)
        {
            PlayerInteractionManager interactionManager = player.GetComponent<PlayerInteractionManager>();
            if (interactionManager != null)
            {
                interactionManager.AddInteractable(this);
            }
        }
    }

    public virtual void OnTriggerExit2D(Collider2D collision)
    {
        PlayerManager player = collision.GetComponent<PlayerManager>();
        if (player != null)
        {
            PlayerInteractionManager interactionManager = player.GetComponent<PlayerInteractionManager>();
            if (interactionManager != null)
            {
                interactionManager.RemoveInteractable(this);
            }
        }
    }

    public virtual void Interact(PlayerManager player)
    {
        if (player != PlayerSelector.Instance.currentPlayer)
            return;

        bool canInteract = canInteractWithTypes.Length == 0;
        foreach (PlayerType type in canInteractWithTypes)
        {
            if (player.playerType == type)
            {
                canInteract = true;
                break;
            }
        }

        if (!canInteract)
            return;

        //Debug.Log("Interacting with " + player.name);

        if (interactOnce)
        {
            interactionArea.enabled = false;
            player.playerInteraction.RemoveInteractable(this);
        }

        if (animator != null && interactAnimationNames.ContainsKey(player.playerType))
        {
            string animationName = interactAnimationNames[player.playerType];
            animator.Play(animationName);
        }
    }
}
