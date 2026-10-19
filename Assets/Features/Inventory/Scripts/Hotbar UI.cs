using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// 快捷栏 UI：
/// 跟随 PlayerSelector 的当前玩家，显示他的背包内容；
/// 点击格子或按数字键选中当前使用的物品；选中格子上方显示一个三角标记。
/// </summary>
public class HotbarUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image[] icons = new Image[7];

    [Header("选中标记（美术资源，留空则用代码生成的三角）")]
    [SerializeField] private Sprite markerSprite;
    [SerializeField] private Color markerColor = new Color(1f, 0.85f, 0.3f);
    [SerializeField] private Vector2 markerSize = new Vector2(28f, 16f);
    [SerializeField] private float markerOffsetY = 58f;

    private PlayerInventoryManager boundInventory;
    private Image marker;

    // 外部（解谜 UI、画图 UI 等）要求的显示状态。
    // 实际是否激活 = requestedVisible && 加载画面不可见，两者分开存，避免互相覆盖。
    private bool requestedVisible = true;

    private void Awake()
    {
        requestedVisible = gameObject.activeSelf;
    }

    private void Start()
    {
        EnsureEventSystem();
        CreateMarker();
        Bind(CurrentInventory());
        // 注意：这里不调用 ApplyVisibility()，可见性由 PlayerUIManager 在它自己的 Start 里统一初始化，
        // 否则两个 Start 的执行顺序会让 CaptureInitialState 读到「因为加载画面而被隐藏」的错误初始值。
    }

    private void Update()
    {
        // 操控的玩家切换时，重新绑定到他的背包
        PlayerInventoryManager current = CurrentInventory();
        if (current != boundInventory)
            Bind(current);

        HandleNumberKeys();
    }

    private void HandleNumberKeys()
    {
        if (PlayerInputManager.Instance != null && PlayerInputManager.Instance.IsPlayerInputLocked)
            return;

        Keyboard keyboard = Keyboard.current;
        if (boundInventory == null || keyboard == null)
            return;

        if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
            boundInventory.Select(0);
        else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
            boundInventory.Select(1);
        else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame)
            boundInventory.Select(2);
        else if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame)
            boundInventory.Select(3);
        else if (keyboard.digit5Key.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame)
            boundInventory.Select(4);
        else if (keyboard.digit6Key.wasPressedThisFrame || keyboard.numpad6Key.wasPressedThisFrame)
            boundInventory.Select(5);
        else if (keyboard.digit7Key.wasPressedThisFrame || keyboard.numpad7Key.wasPressedThisFrame)
            boundInventory.Select(6);
    }

    private void OnEnable()
    {
        if (marker != null)
            Bind(CurrentInventory());
    }

    private void OnDisable()
    {
        if (boundInventory != null)
            boundInventory.Changed -= Refresh;
    }
    /// <summary>外部要求显示快捷栏（例如关闭解谜/画图 UI 时）。加载画面仍可见时会先记下，等它消失后自动显示。</summary>
    public void ShowInventoryWindow() => SetRequestedVisible(true);

    /// <summary>外部要求隐藏快捷栏（例如打开解谜/画图 UI 时）。</summary>
    public void HideInventoryWindow() => SetRequestedVisible(false);

    private void SetRequestedVisible(bool visible)
    {
        requestedVisible = visible;
        ApplyVisibility();
    }

    /// <summary>
    /// 场景刚加载时用物体在场景里的真实状态作为初始「请求状态」。
    /// 如果美术/关卡把快捷栏默认关掉了，Awake 不会执行，这里补一次，避免被 PlayerUIManager 强制打开。
    /// </summary>
    public void CaptureInitialState()
    {
        requestedVisible = gameObject.activeSelf;
    }

    /// <summary>
    /// 把「外部请求的状态」和「加载画面是否可见」合成最终结果。
    /// 本物体被隐藏时自身 Update 不会再执行，所以必须由始终保持激活的 PlayerUIManager 每帧调用一次，
    /// 否则加载画面消失后快捷栏再也回不来。
    /// </summary>
    public void ApplyVisibility()
    {
        bool shouldShow = requestedVisible && !IsLoadingScreenVisible();

        if (gameObject.activeSelf != shouldShow)
            gameObject.SetActive(shouldShow);
    }

    /// <summary>加载画面（LoadingManager.loadScreen）当前是否挡在最上层。</summary>
    public static bool IsLoadingScreenVisible()
    {
        LoadingManager manager = LoadingManager.Instance;

        // Instance 可能是已随场景销毁的“假 null”，Unity 重载的 == 会正确处理
        return manager != null
               && manager.loadScreen != null
               && manager.loadScreen.activeSelf;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (boundInventory == null)
            return;

        PlayerUIManager.Instance.PlayClickSound();

        // 从被点中的物体一路向上找，看它属于哪个格子
        GameObject hit = eventData.pointerPressRaycast.gameObject;
        while (hit != null)
        {
            for (int i = 0; i < icons.Length; i++)
            {
                if (icons[i] != null && icons[i].transform.parent == hit.transform)
                {
                    boundInventory.Select(i);
                    return;
                }
            }

            hit = hit.transform.parent != null ? hit.transform.parent.gameObject : null;
        }
    }

    private PlayerInventoryManager CurrentInventory()
    {
        if (PlayerSelector.Instance == null)
            return null;
        return PlayerSelector.Instance.currentInventory;
    }

    private void Bind(PlayerInventoryManager manager)
    {
        if (boundInventory != null)
            boundInventory.Changed -= Refresh;

        boundInventory = manager;

        if (boundInventory != null)
            boundInventory.Changed += Refresh;

        Refresh();
    }

    private void Refresh()
    {
        for (int i = 0; i < icons.Length; i++)
        {
            if (icons[i] == null) continue;

            InventoryItemData item = boundInventory != null ? boundInventory.GetItem(i) : null;

            icons[i].sprite = item != null ? item.Icon : null;
            icons[i].enabled = item != null && item.Icon != null;
        }

        UpdateMarker();
    }

    private void UpdateMarker()
    {
        int index = boundInventory != null ? boundInventory.SelectedIndex : -1;

        if (index < 0 || index >= icons.Length || icons[index] == null)
        {
            marker.gameObject.SetActive(false);
            return;
        }

        // 把标记挂到选中格子下，显示在格子上方
        Transform slot = icons[index].transform.parent;
        marker.transform.SetParent(slot, false);
        marker.rectTransform.anchoredPosition = new Vector2(0f, markerOffsetY);
        marker.gameObject.SetActive(true);
    }

    private void CreateMarker()
    {
        var go = new GameObject("SelectionMarker", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);

        marker = go.GetComponent<Image>();
        // 有美术资源就用美术资源，没有就用代码生成的三角
        marker.sprite = markerSprite != null ? markerSprite : CreateTriangleSprite();
        marker.color = markerColor;
        marker.raycastTarget = false;
        marker.rectTransform.sizeDelta = markerSize;

        go.SetActive(false);
    }

    private static Sprite CreateTriangleSprite()
    {
        const int size = 32;

        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;

        Color32[] pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            // 越往上越宽，顶点在底部，形成向下的三角
            int halfWidth = Mathf.RoundToInt(y * 16f / (size - 1));
            for (int x = size / 2 - halfWidth; x <= size / 2 + halfWidth; x++)
            {
                pixels[y * size + x] = Color.white;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
    }
}
