using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovementManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D bodyCollider;
    [SerializeField] private PlayerManager playerManager;

    [Header("Settings")]
    [SerializeField] PlayerSettings PlayerSettings;

    private Vector2 moveDirection;

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (bodyCollider == null) bodyCollider = GetComponent<Collider2D>();
        if (playerManager == null) playerManager = GetComponent<PlayerManager>();

        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (bodyCollider != null && bodyCollider.sharedMaterial == null)
        {
            bodyCollider.sharedMaterial = new PhysicsMaterial2D("Frictionless")
            {
                friction = 0f,
                bounciness = 0f
            };
        }
    }

    void Update()
    {
        HandleInput();
    }

    void FixedUpdate()
    {
        Movement();
    }

    private void HandleInput()
    {
        // Get input from PlayerInputManager
        if (playerManager.playerState != PlayerState.InputControlling)
        {
            moveDirection = Vector2.zero;
            return;
        }

        Vector2 input = PlayerInputManager.Instance != null
            ? PlayerInputManager.Instance.MovementInput
            : Vector2.zero;

        moveDirection = input.normalized;

        if (playerManager != null)
            playerManager.isMoving = moveDirection.sqrMagnitude > 0.001f;
    }

    private void Movement()
    {
        if (playerManager.playerState != PlayerState.InputControlling)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 targetVelocity = moveDirection * PlayerSettings.MoveSpeed;

        float rate = moveDirection.sqrMagnitude > 0.001f ? PlayerSettings.Acceleration : PlayerSettings.Deceleration;
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, targetVelocity, rate * Time.fixedDeltaTime);
    }
}
