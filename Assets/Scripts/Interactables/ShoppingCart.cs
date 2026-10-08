using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShoppingCart : InteractableManager
{
    [Header("Spawn Settings")]
    public GameObject canPrefab;
    public Transform spawnPoint;
    public List<GameObject> spawnedCans = new List<GameObject>();

    [Header("UI Settings")]
    public GameObject menu;
    public Button spawnCanButton;
    public Button clearCanButton;
    public Button exitButton;

    [Header("Drive Settings")]
    public bool isDriving = false;
    public List<Transform> drivePathPoints = new List<Transform>();
    [Tooltip("驾驶移动速度")]
    public float driveSpeed = 3f;
    [Tooltip("离路径点多近算到达，到达后停下")]
    public float driveStopDistance = 0.1f;

    private Rigidbody2D cartRb;
    private RigidbodyType2D originalBodyType;

    private PlayerManager drivingPlayer;
    private Rigidbody2D drivingPlayerRb;
    private RigidbodyType2D drivingPlayerOriginalBodyType;

    public static ShoppingCart ActiveDrivingCart;

    private Transform currentTarget;
    private bool inputHeld;

    public override void Awake()
    {
        base.Awake();

        cartRb = GetComponent<Rigidbody2D>();
        if (cartRb != null)
            originalBodyType = cartRb.bodyType;
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
        if (menu != null && menu.activeSelf)
        {
            if (PlayerSelector.Instance.currentInventory.currentSelectedItem)
            {
                if (PlayerSelector.Instance.currentInventory.currentSelectedItem is Can selectedItem)
                {
                    if (selectedItem.itemCount > 0)
                        spawnCanButton.interactable = true;
                    else
                        spawnCanButton.interactable = false;

                    if (spawnedCans.Count > 0)
                        clearCanButton.interactable = true;
                    else
                        clearCanButton.interactable = false;
                }
                else
                {
                    spawnCanButton.interactable = false;
                    clearCanButton.interactable = false;
                }
            }
            else
            {
                spawnCanButton.interactable = false;
                clearCanButton.interactable = false;
            }
        }
    }

    // ---- Drive：玩家操控购物车，沿 drivePathPoints 移动 ----

    private void Update()
    {
        if (!isDriving)
            return;

        // 若当前操控角色已切换，结束驾驶（Esc 下车在 PlayerInputManager 中处理）
        if (PlayerSelector.Instance != null && PlayerSelector.Instance.currentPlayer != drivingPlayer)
        {
            StopDriving();
            return;
        }

        Vector2 input = PlayerInputManager.Instance != null ? PlayerInputManager.Instance.MovementInput : Vector2.zero;
        bool hasInput = input.sqrMagnitude > 0.001f;

        // 只在按下方向键的瞬间选目标，按住不放不会连续跳点
        if (hasInput && !inputHeld)
            currentTarget = GetNearestPointInDirection(GetCardinalDirection(input));

        inputHeld = hasInput;
    }

    private void FixedUpdate()
    {
        if (!isDriving || currentTarget == null || cartRb == null)
            return;

        Vector2 target = currentTarget.position;
        Vector2 myPos = cartRb.position;
        Vector2 delta = target - myPos;

        Vector2 newPos;
        if (delta.magnitude <= driveStopDistance)
        {
            newPos = target;
            currentTarget = null; // 到达后停下
        }
        else
        {
            newPos = myPos + delta.normalized * driveSpeed * Time.fixedDeltaTime;
        }

        Vector2 moveDelta = newPos - myPos;
        cartRb.MovePosition(newPos);

        // 玩家跟着购物车一起移动（不改父子关系，避免受购物车 1.45 缩放影响）
        if (drivingPlayerRb != null)
            drivingPlayerRb.MovePosition(drivingPlayerRb.position + moveDelta);
    }

    public void Drive()
    {
        PlayerManager player = PlayerSelector.Instance != null ? PlayerSelector.Instance.currentPlayer : null;
        if (player == null)
            return;

        drivingPlayer = player;
        isDriving = true;
        ActiveDrivingCart = this;
        inputHeld = false;
        currentTarget = null;

        if (menu != null)
            menu.SetActive(false);

        // 玩家停止自身移动，随购物车一起动
        if (player.playerMovement != null)
            player.playerMovement.enabled = false;

        drivingPlayerRb = player.GetComponent<Rigidbody2D>();
        if (drivingPlayerRb != null)
        {
            drivingPlayerOriginalBodyType = drivingPlayerRb.bodyType;
            drivingPlayerRb.bodyType = RigidbodyType2D.Kinematic;
        }

        // 购物车切换为运动学刚体，便于用 MovePosition 精确控制
        if (cartRb != null)
            cartRb.bodyType = RigidbodyType2D.Kinematic;
    }

    public void StopDriving()
    {
        if (!isDriving)
            return;

        isDriving = false;
        if (ActiveDrivingCart == this)
            ActiveDrivingCart = null;
        currentTarget = null;
        inputHeld = false;

        if (drivingPlayer != null)
        {
            if (drivingPlayerRb != null)
                drivingPlayerRb.bodyType = drivingPlayerOriginalBodyType;
            if (drivingPlayer.playerMovement != null)
                drivingPlayer.playerMovement.enabled = true;
            drivingPlayer = null;
            drivingPlayerRb = null;
        }

        if (cartRb != null)
            cartRb.bodyType = originalBodyType;
    }

    // 把斜向输入（如 WASD 对角）归一到最近的主方向
    private static Vector2 GetCardinalDirection(Vector2 input)
    {
        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            return new Vector2(Mathf.Sign(input.x), 0f);
        return new Vector2(0f, Mathf.Sign(input.y));
    }

    // 在按键方向上，找方向最接近（dot 最大）的路径点
    private Transform GetNearestPointInDirection(Vector2 direction)
    {
        Vector2 myPos = cartRb.position;
        Transform best = null;
        float bestDot = 0.3f;

        foreach (Transform p in drivePathPoints)
        {
            if (p == null)
                continue;

            Vector2 to = (Vector2)p.position - myPos;
            float dist = to.magnitude;
            if (dist < 0.01f)
                continue; // 已经在点上

            float dot = Vector2.Dot(to / dist, direction);
            if (dot > bestDot)
            {
                bestDot = dot;
                best = p;
            }
        }

        return best;
    }

    public void SpawnCan()
    {
        if (PlayerSelector.Instance.currentInventory.currentSelectedItem != null)
        {
            if (PlayerSelector.Instance.currentInventory.currentSelectedItem is Can selectedItem && selectedItem.itemCount > 0)
            {
                if (canPrefab != null && spawnPoint != null)
                {
                    Vector3 randomOffset = new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(-0.2f, 0.2f), 0);
                    GameObject newCan = Instantiate(canPrefab, spawnPoint.position + randomOffset, Quaternion.identity);
                    spawnedCans.Add(newCan);

                    newCan.transform.SetParent(this.transform);

                    PlayerSelector.Instance.currentInventory.currentSelectedItem.Remove();
                }
            }
        }

        UpdateMenu();
    }

    public void ClearOneOfTheCans()
    {
        if (PlayerSelector.Instance.currentInventory.currentSelectedItem != null)
        {
            if (PlayerSelector.Instance.currentInventory.currentSelectedItem is Can selectedItem)
            {
                if (spawnedCans.Count > 0)
                {
                    GameObject canToRemove = spawnedCans[0];
                    spawnedCans.RemoveAt(0);
                    Destroy(canToRemove);

                    PlayerSelector.Instance.currentInventory.currentSelectedItem.Add();
                }
            }
        }

        UpdateMenu();
    }

    public void Exit()
    {
        if (menu != null)
        {
            menu.SetActive(false);
        }

        UpdateMenu();
    }
}
