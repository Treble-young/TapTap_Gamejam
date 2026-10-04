using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// 快捷栏 UI：
/// 跟随 PlayerSelector 的当前玩家，显示他的背包内容；
/// 点击格子选中当前使用的物品；选中格子上方显示一个三角标记。
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

    private void Start()
    {
        EnsureEventSystem();
        CreateMarker();
        Bind(CurrentInventory());
    }

    private void Update()
    {
        // 操控的玩家切换时，重新绑定到他的背包
        PlayerInventoryManager current = CurrentInventory();
        if (current != boundInventory)
            Bind(current);
    }

    private void OnDisable()
    {
        if (boundInventory != null)
            boundInventory.Changed -= Refresh;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (boundInventory == null)
            return;

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

    /// <summary>代码生成一个向下指的三角 Sprite（没有美术资源时先用这个）</summary>
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

    /// <summary>场景里没有 EventSystem 时补一个，否则 UI 点击不生效</summary>
    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
    }
}
