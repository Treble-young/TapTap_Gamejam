using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShoppingCart : InteractableManager
{
    [Header("Cart Visuals")]
    public SpriteRenderer cartSpriteRenderer;
    public Sprite cartUpperRightSprite;
    public Sprite cartLowerLeftSprite;
    public Sprite cartUpperRightFullSprite;
    public Sprite cartLowerLeftFullSprite;

    [Header("UI Settings")]
    public GameObject menu;
    public Button spawnCanButton;
    public Button clearCanButton;
    public Button exitButton;

    [Header("Track Settings")]
    public bool isDriving = false;
    public bool canMove = true;
    public List<Transform> drivePathPoints = new List<Transform>();
    public float driveSpeed = 3f;

    public static ShoppingCart ActiveDrivingCart;
    public bool IsLoaded => loadedCanItems.Count > 0;
    public int LoadedCanCount => loadedCanItems.Count;

    private Rigidbody2D cartRb;
    private RigidbodyType2D originalBodyType;

    private Sprite cartEmptySprite;
    private readonly List<Can> loadedCanItems = new List<Can>();

    private readonly List<Vector3> pathPoints = new List<Vector3>();
    private readonly List<float> segmentLengths = new List<float>();
    private float totalPathLength;
    private float pathDistance;
    private int moveDirection = 1;
    private bool isReturning;
    private Vector3 originPosition;
    private float visualAngle;

    public override void Awake()
    {
        base.Awake();

        cartRb = GetComponent<Rigidbody2D>();
        if (cartRb != null)
            originalBodyType = cartRb.bodyType;

        originPosition = transform.position;

        if (cartSpriteRenderer == null)
        {
            Transform visual = transform.Find("Visual");
            cartSpriteRenderer = visual != null ? visual.GetComponent<SpriteRenderer>() : GetComponentInChildren<SpriteRenderer>();
        }

        if (cartSpriteRenderer != null)
            cartEmptySprite = cartSpriteRenderer.sprite;
    }

    public override void Interact(PlayerManager player)
    {
        base.Interact(player);

        if (menu != null)
        {
            menu.SetActive(true);
            UpdateMenu();
        }
    }

    private void UpdateMenu()
    {
        if (menu == null || !menu.activeSelf)
            return;

        PlayerInventoryManager inventory = PlayerSelector.Instance != null ? PlayerSelector.Instance.currentInventory : null;
        bool canSpawn = inventory != null &&
                        inventory.currentSelectedItem is Can selectedCan && selectedCan.itemCount > 0;

        if (spawnCanButton != null)
            spawnCanButton.interactable = canSpawn;
        if (clearCanButton != null)
            clearCanButton.interactable = IsLoaded;
    }

    private void FixedUpdate()
    {
        if (!isDriving || cartRb == null)
            return;

        // Only RollingDoor requests a return by setting canMove=false. Player contact does not.
        if (!isReturning && moveDirection > 0 && !canMove)
        {
            isReturning = true;
            moveDirection = -1;
        }

        Vector3 oldPosition = cartRb.position;
        float newDistance = Mathf.Clamp(
            pathDistance + moveDirection * driveSpeed * Time.fixedDeltaTime,
            0f,
            totalPathLength);

        Vector3 newPosition = GetPointAtDistance(newDistance);
        cartRb.MovePosition(newPosition);
        pathDistance = newDistance;

        Vector2 move = newPosition - oldPosition;
        if (move.sqrMagnitude > 0.000001f)
            SetVisualAngle(Mathf.Atan2(move.y, move.x) * Mathf.Rad2Deg);

        if (newDistance <= 0f || newDistance >= totalPathLength)
        {
            moveDirection = newDistance <= 0f ? 1 : -1;
            isDriving = false;
            isReturning = false;

            if (cartRb != null)
            {
                cartRb.linearVelocity = Vector2.zero;
                cartRb.angularVelocity = 0f;
                cartRb.bodyType = originalBodyType;
            }
        }
    }

    public void Drive()
    {
        if (drivePathPoints.Count == 0)
            return;

        if (menu != null)
            menu.SetActive(false);

        if (!isDriving)
        {
            RebuildPath();
            if (totalPathLength <= 0f)
                return;

            isDriving = true;
            isReturning = false;

            if (cartRb != null)
                cartRb.bodyType = RigidbodyType2D.Kinematic;
        }
        else if (!isReturning)
        {
            moveDirection = -moveDirection;
        }
    }

    public void StopDriving()
    {
        if (!isDriving)
            return;

        isDriving = false;
        isReturning = false;

        if (ActiveDrivingCart == this)
            ActiveDrivingCart = null;

        if (cartRb != null)
        {
            cartRb.linearVelocity = Vector2.zero;
            cartRb.angularVelocity = 0f;
            cartRb.bodyType = originalBodyType;
        }
    }

    private void RebuildPath()
    {
        pathPoints.Clear();
        segmentLengths.Clear();
        totalPathLength = 0f;

        pathPoints.Add(originPosition);
        foreach (Transform point in drivePathPoints)
        {
            if (point != null)
                pathPoints.Add(point.position);
        }

        for (int i = 1; i < pathPoints.Count; i++)
        {
            float length = Vector3.Distance(pathPoints[i - 1], pathPoints[i]);
            segmentLengths.Add(length);
            totalPathLength += length;
        }
    }

    private Vector3 GetPointAtDistance(float distance)
    {
        if (pathPoints.Count == 0)
            return transform.position;
        if (distance <= 0f)
            return pathPoints[0];
        if (distance >= totalPathLength)
            return pathPoints[pathPoints.Count - 1];

        float remaining = distance;
        for (int i = 0; i < segmentLengths.Count; i++)
        {
            float segment = segmentLengths[i];
            if (remaining <= segment)
            {
                float t = segment > 0f ? remaining / segment : 1f;
                return Vector3.Lerp(pathPoints[i], pathPoints[i + 1], t);
            }

            remaining -= segment;
        }

        return pathPoints[pathPoints.Count - 1];
    }

    public void SpawnCan()
    {
        PlayerInventoryManager inventory = PlayerSelector.Instance != null ? PlayerSelector.Instance.currentInventory : null;
        if (inventory == null)
        {
            UpdateMenu();
            return;
        }

        if (!(inventory.currentSelectedItem is Can selectedCan) || selectedCan.itemCount <= 0)
        {
            UpdateMenu();
            return;
        }

        inventory.RemoveItem(selectedCan);
        loadedCanItems.Add(selectedCan);

        UpdateCartVisual(visualAngle);
        UpdateMenu();
    }

    public void ClearOneOfTheCans()
    {
        if (loadedCanItems.Count == 0)
        {
            UpdateMenu();
            return;
        }

        PlayerInventoryManager inventory = PlayerSelector.Instance != null ? PlayerSelector.Instance.currentInventory : null;
        if (inventory == null)
        {
            UpdateMenu();
            return;
        }

        Can canItem = loadedCanItems[0];
        if (canItem != null)
        {
            if (canItem.itemCount <= 0)
                canItem.itemCount = 1;

            if (!inventory.TryAdd(canItem))
            {
                UpdateMenu();
                return;
            }
        }

        loadedCanItems.RemoveAt(0);

        UpdateCartVisual(visualAngle);
        UpdateMenu();
    }

    public void Exit()
    {
        if (menu != null)
            menu.SetActive(false);

        UpdateMenu();
    }

    private void SetVisualAngle(float angle)
    {
        visualAngle = angle;
        UpdateCartVisual(angle);
    }

    private void UpdateCartVisual(float angle)
    {
        if (cartSpriteRenderer == null)
            return;

        float normalizedAngle = NormalizeAngle(angle);
        bool upper = normalizedAngle < 180f;
        bool leftSide = normalizedAngle > 90f && normalizedAngle < 270f;
        bool flipX = upper ? leftSide : !leftSide;

        Sprite sprite = IsLoaded
            ? (upper ? cartUpperRightFullSprite : cartLowerLeftFullSprite)
            : (upper ? cartUpperRightSprite : cartLowerLeftSprite);

        if (sprite == null)
            sprite = cartEmptySprite;

        if (sprite != null)
        {
            cartSpriteRenderer.sprite = sprite;
            cartSpriteRenderer.flipX = flipX;
            if (outlineSpriteRenderer != null)
            {
                outlineSpriteRenderer.sprite = sprite;
                outlineSpriteRenderer.flipX = flipX;
            }
        }
    }

    private static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle < 0f)
            angle += 360f;
        return angle;
    }
}
