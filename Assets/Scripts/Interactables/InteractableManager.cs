using System.Collections;
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

    [Header("White Outline Settings")]
    public SpriteRenderer outlineSpriteRenderer;
    public bool needEmergingOutline = true;
    private Coroutine outlineCoroutine;

    public virtual void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerManager player = collision.GetComponent<PlayerManager>();
        if (player != null)
        {
            if (player.playerState != PlayerState.InputControlling)
                return;

            PlayerInteractionManager interactionManager = player.GetComponent<PlayerInteractionManager>();
            if (interactionManager != null)
            {
                interactionManager.AddInteractable(this);
                if (needEmergingOutline && outlineSpriteRenderer != null)
                {
                    if (outlineCoroutine != null)
                        StopCoroutine(outlineCoroutine);

                    outlineCoroutine = StartCoroutine(spriteColorAlphaFlickerCoroutine(0.2f, 1f, 2f, 5f));
                }
            }
        }
    }

    protected virtual void OnTriggerExit2D(Collider2D collision)
    {
        PlayerManager player = collision.GetComponent<PlayerManager>();
        if (player != null)
        {
            if (player.playerState != PlayerState.InputControlling)
                return;

            PlayerInteractionManager interactionManager = player.GetComponent<PlayerInteractionManager>();
            if (interactionManager != null)
            {
                interactionManager.RemoveInteractable(this);

                if (outlineCoroutine != null)
                {
                    StopCoroutine(outlineCoroutine);
                    outlineCoroutine = null;
                    if (outlineSpriteRenderer != null)
                    {
                        Color originalColor = outlineSpriteRenderer.color;
                        outlineSpriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
                    }
                }
            }
        }
    }

    private IEnumerator spriteColorAlphaFlickerCoroutine(float minAlpha, float maxAlpha, float cycleDuration, float totalDuration)
    {
        if (outlineSpriteRenderer == null)
            yield break;

        Color originalColor = outlineSpriteRenderer.color;
        float elapsedTime = 0f;

        while (elapsedTime < totalDuration)
        {
            elapsedTime += Time.deltaTime;

            // Oscillate between minAlpha and maxAlpha using a sine wave for a smooth fade in/out.
            float t = (Mathf.Sin((elapsedTime / cycleDuration) * Mathf.PI * 2f - Mathf.PI / 2f) + 1f) / 2f;
            float newAlpha = Mathf.Lerp(minAlpha, maxAlpha, t);
            outlineSpriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, newAlpha);
            yield return null;
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
