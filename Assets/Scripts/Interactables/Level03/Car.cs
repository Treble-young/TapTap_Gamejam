using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

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

    [Header("UI Settings")]
    public GameObject menu;
    public Button driveButton;
    public Button connectButton;
    public Button jackButton;
    public Button fillFuelButton;
    public Button exitButton;

    [Header("Drive Settings")]
    public bool isDriving = false;
    [Tooltip("开车时 Car 相对玩家的偏移")]
    public Vector2 driveOffset = Vector2.zero;
    [Tooltip("开车时玩家移动速度倍率")]
    public float driveSpeedMultiplier = 2.5f;
    [Tooltip("开局时玩家默认已经坐在车上")]
    public bool startDriving = true;

    public static Car ActiveDrivingCar;

    private Rigidbody2D carRb;
    private Collider2D carCollider;
    private PlayerManager drivingPlayer;
    private InventoryItemData usedJackItem;

    public override void Awake()
    {
        base.Awake();

        carRb = GetComponent<Rigidbody2D>();
        carCollider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        if (startDriving)
            Drive();
    }

    public override void Interact(PlayerManager player)
    {
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

        // 被千斤顶顶起、连着油管（发电机/油泵）、或没油时，隐藏开车按钮（不能开车）
        bool canDrive = !isJackedUp && !isConeectedToCar && !isConnectedToPump && !isOutOfFuel;
        if (driveButton != null)
            driveButton.gameObject.SetActive(canDrive);

        // 千斤顶按钮：手持 Jack 物品时显现；已顶起后始终显现（再次按下即可取下千斤顶）
        if (jackButton != null)
            jackButton.gameObject.SetActive(isJackedUp || PlayerHoldsJack());

        // 加油按钮：只有千斤顶顶起、油管两端接好且发电机还没加满油时显示
        if (fillFuelButton != null)
            fillFuelButton.gameObject.SetActive(isJackedUp && isConeectedToCar && generator != null &&
                                                 generator.isConnectedWithFuel && !generator.isFueled);

        // 连接按钮：始终可按，用于连接 / 断开切换
        if (connectButton != null)
            connectButton.interactable = true;
    }

    private bool PlayerHoldsJack()
    {
        PlayerInventoryManager inventory = PlayerSelector.Instance != null ? PlayerSelector.Instance.currentInventory : null;
        return inventory != null && inventory.currentSelectedItem != null &&
               inventory.currentSelectedItem.ItemName == "Jack";
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
    }

    private void FixedUpdate()
    {
        if (!isDriving || drivingPlayer == null || carRb == null)
            return;

        // 玩家自由移动（已隐藏），Car 跟着玩家走
        Vector2 target = (Vector2)drivingPlayer.transform.position + driveOffset;
        carRb.MovePosition(target);
    }

    public void Drive()
    {
        PlayerManager player = PlayerSelector.Instance != null ? PlayerSelector.Instance.currentPlayer : null;
        if (player == null)
            return;

        // 被千斤顶顶起、连着油管（发电机/油泵）、或没油时不能开车
        if (isJackedUp || isConeectedToCar || isConnectedToPump || isOutOfFuel)
            return;

        drivingPlayer = player;
        isDriving = true;
        ActiveDrivingCar = this;

        if (menu != null)
            menu.SetActive(false);

        // 隐藏玩家
        if (player.playerSpriteRenderer != null)
            player.playerSpriteRenderer.enabled = false;

        // 开车提速
        if (player.playerMovement != null)
            player.playerMovement.moveSpeedMultiplier = driveSpeedMultiplier;

        // 关掉 Car 的实体碰撞体，避免和玩家（动态刚体）互相挤开；碰撞仍由玩家自己的碰撞体负责
        if (carCollider != null)
            carCollider.enabled = false;
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
            if (drivingPlayer.playerMovement != null)
                drivingPlayer.playerMovement.moveSpeedMultiplier = 1f;
            drivingPlayer = null;
        }

        if (carCollider != null)
            carCollider.enabled = true;
    }

    public void Connect()
    {
        // 只有 Main 能连接 / 断开油管
        PlayerManager current = PlayerSelector.Instance != null ? PlayerSelector.Instance.currentPlayer : null;
        if (current == null || current.playerType != PlayerType.Main)
            return;

        // 再次按下断开连接
        if (isConeectedToCar)
        {
            isConeectedToCar = false;
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
                PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("Fuel line connected. Click the generator to start it.");
            }
            else
            {
                PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("Car is now connected with fuel. Now connect the generator with a fuel line.");
            }
        }
        else
        {
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("You need to select a fuel line to connect the car with the generator.");
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

        PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("Generator filled with fuel. Go interact with the generator to start it.");

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
