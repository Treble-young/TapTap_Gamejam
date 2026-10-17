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
    private SpriteRenderer targetSpriteRenderer;

    public virtual void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        targetSpriteRenderer = FindTargetSpriteRenderer();
    }

    private void LateUpdate()
    {
        if (outlineSpriteRenderer == null || targetSpriteRenderer == null)
            return;

        outlineSpriteRenderer.sprite = targetSpriteRenderer.sprite;
        outlineSpriteRenderer.flipX = targetSpriteRenderer.flipX;
        outlineSpriteRenderer.flipY = targetSpriteRenderer.flipY;
    }

    private SpriteRenderer FindTargetSpriteRenderer()
    {
        if (outlineSpriteRenderer == null)
            return null;

        Transform visual = transform.Find("Visual");
        if (visual != null)
        {
            SpriteRenderer visualRenderer = visual.GetComponent<SpriteRenderer>();
            if (visualRenderer != null && visualRenderer != outlineSpriteRenderer)
                return visualRenderer;
        }

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer != outlineSpriteRenderer)
                return renderer;
        }

        return null;
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
            }
        }
    }

    public bool CanInteractWith(PlayerManager player)
    {
        if (player == null || PlayerSelector.Instance == null || player != PlayerSelector.Instance.currentPlayer)
            return false;

        if (canInteractWithTypes.Length == 0)
            return true;

        foreach (PlayerType type in canInteractWithTypes)
        {
            if (player.playerType == type)
                return true;
        }

        return false;
    }

    public void SetOutlineVisible(bool visible)
    {
        if (outlineSpriteRenderer == null)
            return;

        if (visible)
        {
            if (!needEmergingOutline || outlineCoroutine != null)
                return;

            outlineCoroutine = StartCoroutine(spriteColorAlphaFlickerCoroutine(0.2f, 1f, 2f, 5f));
            return;
        }

        if (outlineCoroutine != null)
        {
            StopCoroutine(outlineCoroutine);
            outlineCoroutine = null;
        }

        Color color = outlineSpriteRenderer.color;
        color.a = 0f;
        outlineSpriteRenderer.color = color;
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
        if (!CanInteractWith(player))
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
