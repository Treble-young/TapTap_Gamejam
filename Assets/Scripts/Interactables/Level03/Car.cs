using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public enum CarDirection
{
    Right,
    UpRight,
    Up,
    UpLeft,
    Left,
    DownLeft,
    Down,
    DownRight
}

public class Car : InteractableManager
{
    [Header("Fuel Line")]
    public Generator generator;
    public GasStationOilPump oilPump;
    public bool isConeectedToCar = false;
    [Tooltip("输油给发电机后车没油了，不能驾驶")]
    public bool isOutOfFuel = false;
    [Tooltip("油泵的输油管是否连着车")]
    public bool isConnectedToPump = false;
    public GameObject lineRenderer;
    public SpriteShapeController lineRendererShapeController;
    [Tooltip("油管在 Car 侧的连接点（留空则用管线自身 pivot）")]
    public Transform carAnchor;
    [Tooltip("油管在 Generator 侧的连接点（留空则用 Generator 位置）")]
    public Transform generatorAnchor;
    [Tooltip("油管蜿蜒幅度（相对管线长度的比例，0 为直线）")]
    public float pipelineWaveAmplitude = 0.08f;

    [Header("Jack")]
    public bool isJackedUp = false;

    [Header("Car Visuals")]
    public SpriteRenderer carSpriteRenderer;
    public Sprite carUpSprite;
    public Sprite carDownSprite;
    public Sprite carRightSprite;
    public Sprite carUpRightSprite;
    public Sprite carDownRightSprite;

    [Header("Breakdown")]
    [Tooltip("损坏时播放的粒子效果")]
    public ParticleSystem breakParticleSystem;

    [Header("UI Settings")]
    public GameObject menu;
    public Button driveButton;
    public Button connectButton;
    public Button jackButton;
    public Button fillFuelButton;
    public Button exitButton;

    [Header("Breakdown Settings")]
    public GameObject[] interactThenSpawnAfterBreakdown;
    public Transform spawnPointAfterBreakdown;

    [Header("Drive Settings")]
    public bool isDriving = false;
    [Tooltip("开车时 Car 相对玩家的偏移")]
    public Vector2 driveOffset = new Vector2(0f, -0.75f);
    [Tooltip("开车时的移动速度")]
    public float driveSpeed = 4f;
    [Tooltip("A/D 转向速度（度/秒）")]
    public float turnSpeed = 180f;
    [Tooltip("右上/右下/左上/左下方向的前进向量，代码会自动归一化")]
    public Vector2 diagonalDriveVector = new Vector2(1.5625f, 0.875f);
    [Tooltip("开局时车头的正前方（8 个方向）")]
    public CarDirection startFacing = CarDirection.Right;
    [Tooltip("为 true 时，开局自动开始驾驶")]
    public bool startDriving = false;

    public static Car ActiveDrivingCar;

    private Rigidbody2D carRb;
    private Collider2D carCollider;
    private RigidbodyType2D originalCarBodyType;
    private float facingAngle;
    private PlayerManager drivingPlayer;
    private Rigidbody2D drivingPlayerRb;
    private Collider2D drivingPlayerCollider;
    private InventoryItemData usedJackItem;
    private bool isBrokenDown = false;
    private bool hasSpawnedAfterBreakdown = false;

    public override void Awake()
    {
        base.Awake();

        carRb = GetComponent<Rigidbody2D>();
        carCollider = GetComponent<Collider2D>();
        if (carRb != null)
            originalCarBodyType = carRb.bodyType;

        if (carSpriteRenderer == null)
        {
            Transform visual = transform.Find("Visual");
            carSpriteRenderer = visual != null ? visual.GetComponent<SpriteRenderer>() : GetComponentInChildren<SpriteRenderer>();
        }

        facingAngle = DirectionToAngle(startFacing);
        UpdateCarSprite();
    }

    private void Start()
    {
        if (startDriving)
            Drive();
    }

    public override void Interact(PlayerManager player)
    {
        if (isBrokenDown)
        {
            TrySpawnAfterBreakdown();
            return;
        }

        // 打开 UI 让玩家选择：开车 / 连接油管 / 关闭
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

        // 开车按钮始终显示，不能开时点击只弹提示
        if (driveButton != null)
            driveButton.gameObject.SetActive(true);

        // 千斤顶按钮：手持 Jack 物品时显现；已顶起后始终显现（再次按下即可取下千斤顶）
        if (jackButton != null)
            jackButton.gameObject.SetActive(isJackedUp || PlayerHoldsJack());

        // 加油按钮：只有千斤顶顶起、油管两端接好且发电机还没加满油时显示
        if (fillFuelButton != null)
            fillFuelButton.gameObject.SetActive(isJackedUp && isConeectedToCar && generator != null &&
                                                 generator.isConnectedWithFuel && !generator.isFueled);

        // 连接按钮：始终可按，用于连接 / 断开切换
        if (connectButton != null)
            connectButton.gameObject.SetActive(isConeectedToCar || isConnectedToPump || PlayerHoldsOilPipeline());
    }

    private bool PlayerHoldsJack()
    {
        PlayerInventoryManager inventory = PlayerSelector.Instance != null ? PlayerSelector.Instance.currentInventory : null;
        return inventory != null && inventory.currentSelectedItem != null &&
               inventory.currentSelectedItem.ItemName == "Jack";
    }

    private bool PlayerHoldsOilPipeline()
    {
        PlayerInventoryManager inventory = PlayerSelector.Instance != null ? PlayerSelector.Instance.currentInventory : null;
        return inventory != null && inventory.currentSelectedItem is OilPipeline;
    }

    private void Update()
    {
        if (!isDriving)
            return;

        // 切换了操控角色则自动下车
        if (PlayerSelector.Instance != null && PlayerSelector.Instance.currentPlayer != drivingPlayer)
        {
            StopDriving();
            return;
        }

        HandleDrivingInput();
    }

    private void FixedUpdate()
    {
        if (!isDriving || drivingPlayer == null || carRb == null)
            return;

        UpdateDrivingVelocity();
    }

    private void OnDisable()
    {
        if (isDriving)
            StopDriving();
    }

    public void Drive()
    {
        PlayerManager player = PlayerSelector.Instance != null ? PlayerSelector.Instance.currentPlayer : null;
        if (player == null)
            return;

        // 被千斤顶顶起、连着油管（发电机/油泵）、或没油时不能开车
        if (isJackedUp || isConeectedToCar || isConnectedToPump || isOutOfFuel)
        {
            CloseMenu();

            if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.popUpManager != null)
                PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("没油了，需要油泵，好像有人会使用加油机");
            return;
        }

        drivingPlayer = player;
        isDriving = true;
        ActiveDrivingCar = this;

        if (menu != null)
            menu.SetActive(false);

        // 隐藏玩家，暂停玩家移动和碰撞，让 Car 的碰撞体负责驾驶碰撞
        if (player.playerSpriteRenderer != null)
            player.playerSpriteRenderer.enabled = false;

        drivingPlayerRb = player.GetComponent<Rigidbody2D>();
        if (drivingPlayerRb != null)
            drivingPlayerRb.linearVelocity = Vector2.zero;

        drivingPlayerCollider = player.GetComponent<Collider2D>();
        if (drivingPlayerCollider != null)
            drivingPlayerCollider.enabled = false;

        if (player.playerMovement != null)
            player.playerMovement.enabled = false;

        // 开车时启用 Car 的实体碰撞体，并切到动态刚体参与物理碰撞
        if (carCollider != null)
            carCollider.enabled = true;

        if (carRb != null)
        {
            carRb.bodyType = RigidbodyType2D.Dynamic;
            carRb.gravityScale = 0f;
            carRb.constraints = RigidbodyConstraints2D.FreezeRotation;
            carRb.linearVelocity = Vector2.zero;
        }

        UpdateCarSprite();
    }

    public void StopDriving()
    {
        if (!isDriving)
            return;

        isDriving = false;
        if (ActiveDrivingCar == this)
            ActiveDrivingCar = null;

        if (drivingPlayer != null)
        {
            if (drivingPlayer.playerSpriteRenderer != null)
                drivingPlayer.playerSpriteRenderer.enabled = true;
            if (drivingPlayerCollider != null)
                drivingPlayerCollider.enabled = true;
            if (drivingPlayerRb != null)
                drivingPlayerRb.linearVelocity = Vector2.zero;
            if (drivingPlayer.playerMovement != null)
                drivingPlayer.playerMovement.enabled = true;
        }

        drivingPlayer = null;
        drivingPlayerRb = null;
        drivingPlayerCollider = null;

        if (carRb != null)
        {
            carRb.linearVelocity = Vector2.zero;
            carRb.bodyType = originalCarBodyType;
        }

        if (carCollider != null)
            carCollider.enabled = true;
    }

    public void BreakDown()
    {
        if (isBrokenDown)
            return;

        isBrokenDown = true;
        StopDriving();
        CloseMenu();

        if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.popUpManager != null)
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("这辆车貌似坏了，不能开了。看下能不能拆下什么零件。");

        if (breakParticleSystem != null)
            breakParticleSystem.Play();
    }

    private void TrySpawnAfterBreakdown()
    {
        if (hasSpawnedAfterBreakdown)
            return;

        hasSpawnedAfterBreakdown = true;

        if (interactThenSpawnAfterBreakdown != null)
        {
            Vector3 spawnPosition = spawnPointAfterBreakdown != null ? spawnPointAfterBreakdown.position : transform.position;
            Quaternion spawnRotation = spawnPointAfterBreakdown != null ? spawnPointAfterBreakdown.rotation : transform.rotation;

            foreach (GameObject prefab in interactThenSpawnAfterBreakdown)
            {
                if (prefab != null)
                    Instantiate(prefab, spawnPosition, spawnRotation);
            }
        }

        DisableInteraction();
    }

    private void DisableInteraction()
    {
        if (interactionArea != null)
            interactionArea.enabled = false;

        if (PlayerSelector.Instance == null)
            return;

        foreach (PlayerManager teammate in PlayerSelector.Instance.playerManagers)
        {
            if (teammate != null && teammate.playerInteraction != null)
                teammate.playerInteraction.RemoveInteractable(this);
        }
    }

    private void HandleDrivingInput()
    {
        Vector2 input = PlayerInputManager.Instance != null ? PlayerInputManager.Instance.MovementInput : Vector2.zero;

        // 屏幕坐标系中，车头角度逆时针增加；A 为左转，D 为右转。
        facingAngle -= input.x * turnSpeed * Time.deltaTime;

        UpdateCarSprite();
    }

    private void UpdateDrivingVelocity()
    {
        Vector2 input = PlayerInputManager.Instance != null ? PlayerInputManager.Instance.MovementInput : Vector2.zero;
        float throttle = Mathf.Clamp(input.y, -1f, 1f);
        carRb.linearVelocity = FacingVector() * (throttle * driveSpeed);

        if (drivingPlayerRb != null)
            drivingPlayerRb.MovePosition(carRb.position - driveOffset);
        else if (drivingPlayer != null)
            drivingPlayer.transform.position = (Vector3)(carRb.position - driveOffset);
    }

    private void UpdateCarSprite()
    {
        if (carSpriteRenderer == null)
            return;

        CarDirection direction = AngleToDirection(facingAngle);
        Sprite sprite;
        bool flipX;

        switch (direction)
        {
            case CarDirection.UpRight:
                sprite = carUpRightSprite;
                flipX = false;
                break;
            case CarDirection.Up:
                sprite = carUpSprite;
                flipX = false;
                break;
            case CarDirection.UpLeft:
                sprite = carUpRightSprite;
                flipX = true;
                break;
            case CarDirection.Left:
                sprite = carRightSprite;
                flipX = true;
                break;
            case CarDirection.DownLeft:
                sprite = carDownRightSprite;
                flipX = true;
                break;
            case CarDirection.Down:
                sprite = carDownSprite;
                flipX = false;
                break;
            case CarDirection.DownRight:
                sprite = carDownRightSprite;
                flipX = false;
                break;
            case CarDirection.Right:
            default:
                sprite = carRightSprite;
                flipX = false;
                break;
        }

        if (sprite != null)
        {
            carSpriteRenderer.sprite = sprite;
            carSpriteRenderer.flipX = flipX;

            if (outlineSpriteRenderer != null)
            {
                outlineSpriteRenderer.sprite = sprite;
                outlineSpriteRenderer.flipX = flipX;
            }
        }
    }

    private Vector2 FacingVector()
    {
        return DirectionVector(AngleToDirection(facingAngle));
    }

    private Vector2 DirectionVector(CarDirection direction)
    {
        Vector2 diagonal = DiagonalVector(diagonalDriveVector);

        switch (direction)
        {
            case CarDirection.UpRight:
                return diagonal;
            case CarDirection.Up:
                return Vector2.up;
            case CarDirection.UpLeft:
                return new Vector2(-diagonal.x, diagonal.y);
            case CarDirection.Left:
                return Vector2.left;
            case CarDirection.DownLeft:
                return -diagonal;
            case CarDirection.Down:
                return Vector2.down;
            case CarDirection.DownRight:
                return new Vector2(diagonal.x, -diagonal.y);
            case CarDirection.Right:
            default:
                return Vector2.right;
        }
    }

    private static Vector2 DiagonalVector(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f)
            return new Vector2(2f, 1f).normalized;

        return new Vector2(Mathf.Abs(direction.x), Mathf.Abs(direction.y)).normalized;
    }

    private static float DirectionToAngle(CarDirection direction)
    {
        return (int)direction * 45f;
    }

    private static CarDirection AngleToDirection(float angle)
    {
        float normalized = ((angle % 360f) + 360f) % 360f;
        int octant = Mathf.RoundToInt(normalized / 45f) % 8;
        return (CarDirection)octant;
    }

    public void Connect()
    {
        // 只有 Main 能连接 / 断开油管
        PlayerManager current = PlayerSelector.Instance != null ? PlayerSelector.Instance.currentPlayer : null;
        if (current == null || current.playerType != PlayerType.Main)
            return;

        if (isConnectedToPump)
        {
            isConnectedToPump = false;
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("我把车这头的油管拆了。");
            CloseMenu();
            ShowThePipeline();
            UpdateMenu();
            return;
        }

        // 再次按下断开连接
        if (isConeectedToCar)
        {
            isConeectedToCar = false;
            if (generator != null)
                generator.isConnectedWithFuel = false;
            CloseMenu();
            ShowThePipeline();
            UpdateMenu();
            return;
        }

        if (PlayerSelector.Instance.currentInventory &&
            PlayerSelector.Instance.currentInventory.currentSelectedItem is OilPipeline)
        {
            isConeectedToCar = true;

            if (generator && generator.isConnectedWithFuel)
            {
                PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("油管接好了。我得把燃料送进发电机，先找个千斤顶把车抬起来。");
            }
            else
            {
                PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("车这头接好了，接下来去连发电机或加油机。");
            }
        }
        else
        {
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("我得先拿条输油管，把车和发电机连起来。");
        }

        // 显示弹窗时关闭菜单
        CloseMenu();

        ShowThePipeline();
        UpdateMenu();
    }

    public void Jack()
    {
        PlayerInventoryManager inventory = PlayerSelector.Instance != null ? PlayerSelector.Instance.currentInventory : null;

        if (!isJackedUp)
        {
            // 顶起：消耗手上持有的 Jack 物品
            if (inventory == null || inventory.currentSelectedItem == null ||
                inventory.currentSelectedItem.ItemName != "Jack")
                return;

            usedJackItem = inventory.currentSelectedItem;
            inventory.RemoveItem(usedJackItem);
            isJackedUp = true;
        }
        else
        {
            // 再次按下取消顶起：归还 Jack 物品
            isJackedUp = false;

            if (inventory != null && usedJackItem != null)
            {
                if (usedJackItem.itemCount <= 0)
                    usedJackItem.itemCount = 1;
                inventory.TryAdd(usedJackItem);
            }
            usedJackItem = null;
        }

        UpdateMenu();
    }

    public void FillFuel()
    {
        // 只有千斤顶顶起、油管两端接好、且发电机还没加满油时才能加油
        if (!isJackedUp || generator == null || !isConeectedToCar || !generator.isConnectedWithFuel || generator.isFueled)
            return;

        generator.isFueled = true;
        isOutOfFuel = true; // 油输给发电机后，车没油了

        PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("燃料送进发电机了，去启动它。");

        // 显示弹窗时关闭菜单
        CloseMenu();
        UpdateMenu();
    }

    /// <summary>油泵加油后，恢复可驾驶。</summary>
    public void Refuel()
    {
        isOutOfFuel = false;
    }

    public void CloseMenu()
    {
        if (menu != null)
            menu.SetActive(false);
    }

    public void ShowThePipeline()
    {
        Transform target = GetPipelineTarget();

        if (lineRenderer)
            lineRenderer.SetActive(target != null);

        if (target != null)
            UpdatePipelineShape(target);
    }

    /// <summary>决定油管当前连到哪里：油泵或发电机（都没有则返回 null）。</summary>
    private Transform GetPipelineTarget()
    {
        // 油泵优先：连着油泵就显示油泵（否则可能仍显示发电机）
        if (isConnectedToPump && oilPump != null)
            return oilPump.pumpAnchor != null ? oilPump.pumpAnchor : oilPump.transform;

        if (isConeectedToCar && generator != null && generator.isConnectedWithFuel)
            return generatorAnchor != null ? generatorAnchor : generator.transform;

        return null;
    }

    /// <summary>把油管 SpriteShape 的形状设为从 Car 蜿蜒连到 target 的曲线。</summary>
    private void UpdatePipelineShape(Transform target)
    {
        if (lineRendererShapeController == null || target == null)
            return;

        Spline spline = lineRendererShapeController.spline;

        // 保留原来的粗细，重建后管子宽度一致
        float height = spline.GetPointCount() > 0 ? spline.GetHeight(0) : 0.2f;

        // Car 侧 / 目标侧连接点（世界坐标）
        Vector3 carSide = carAnchor != null ? carAnchor.position : lineRendererShapeController.transform.position;
        Vector3 targetSide = target.position;

        // 转成 SpriteShape 的本地坐标
        Vector3 localCar = lineRendererShapeController.transform.InverseTransformPoint(carSide);
        Vector3 localTarget = lineRendererShapeController.transform.InverseTransformPoint(targetSide);

        Vector3 delta = localTarget - localCar;
        Vector3 dir = delta.normalized;
        Vector3 perp = new Vector3(-dir.y, dir.x, 0f);
        float length = delta.magnitude;

        // 蜿蜒幅度随管线长度缩放，短管起伏不会过大
        float amplitude = Mathf.Clamp(length * pipelineWaveAmplitude, 0.05f, 1.5f);

        const int segments = 12;
        spline.Clear();
        spline.isOpenEnded = true;

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 pos = Vector3.Lerp(localCar, localTarget, t);

            // 一个完整正弦周期，让中间轻微蜿蜒（首尾正好回到连接点）
            pos += perp * (Mathf.Sin(t * Mathf.PI * 2f) * amplitude);

            spline.InsertPointAt(i, pos);
            spline.SetTangentMode(i, ShapeTangentMode.Continuous);
            spline.SetHeight(i, height);
        }

        // 强制重建网格
        lineRendererShapeController.RefreshSpriteShape();
        lineRendererShapeController.BakeMesh();
    }
}
