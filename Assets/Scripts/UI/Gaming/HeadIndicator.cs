using UnityEngine;

/// <summary>
/// 头顶指示器：在角色头顶显示一个图标（默认感叹号）用来提示玩家。
/// 其他代码通过 Show() / Hide() / SetVisible() 控制显隐，无需关心内部实现。
/// 可选开启「玩家靠近自动隐藏」，玩家走近时图标淡出、走远后再出现。
///
/// 美术接口：
///   把感叹号贴图赋给 <see cref="sprite"/> 字段即可；用 offset / scale 调整位置与大小，
///   sortingLayerName / sortingOrder 调整渲染层级。
/// </summary>
[DisallowMultipleComponent]
public class HeadIndicator : MonoBehaviour
{
    [Header("美术接口")]
    [Tooltip("头顶显示的图标（例如感叹号）。留空时会用白色占位方块，方便在美术资源到位前调试。")]
    [SerializeField] private Sprite sprite;
    [Tooltip("已有的 SpriteRenderer。通常留空，会自动在子物体上创建一个。")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Tooltip("相对角色锚点的头顶偏移。")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.5f, 0f);
    [Tooltip("图标缩放。")]
    [SerializeField] private Vector3 scale = Vector3.one;

    [Header("渲染")]
    [Tooltip("排序图层，默认 Light Glow，避免被地面/角色挡住。")]
    [SerializeField] private string sortingLayerName = "Light Glow";
    [Tooltip("同图层内的排序序号，数值越大越靠前。")]
    [SerializeField] private int sortingOrder = 100;

    [Header("浮动动画")]
    [Tooltip("是否上下浮动，让感叹号更显眼。")]
    [SerializeField] private bool bobbing = true;
    [SerializeField] private float bobAmplitude = 0.15f;
    [SerializeField] private float bobSpeed = 2f;

    [Header("玩家靠近自动隐藏")]
    [Tooltip("玩家进入 hideRadius 半径内时自动隐藏，离开后再显示。")]
    [SerializeField] private bool hideWhenPlayerNear = true;
    [SerializeField] private float hideRadius = 3f;
    [Tooltip("要检测靠近的目标。留空则自动使用当前操控的玩家。")]
    [SerializeField] private Transform proximityTarget;

    private Vector3 basePosition;
    private bool visible;

    /// <summary>主开关：是否请求显示（实际是否显示还受「玩家靠近自动隐藏」影响）。</summary>
    public bool IsVisible => visible;

    private void Awake()
    {
        EnsureIndicator();
        Hide();
    }

    private void Update()
    {
        bool render = visible && (!hideWhenPlayerNear || !IsTargetNear());

        if (spriteRenderer != null && spriteRenderer.enabled != render)
            spriteRenderer.enabled = render;

        if (render && bobbing)
        {
            Vector3 pos = basePosition;
            pos.y += Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
            spriteRenderer.transform.localPosition = pos;
        }
    }

    /// <summary>显示头顶图标（幂等，可反复调用）。</summary>
    public void Show()
    {
        EnsureIndicator();
        visible = true;
    }

    /// <summary>隐藏头顶图标（幂等，可反复调用）。</summary>
    public void Hide()
    {
        visible = false;
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;
    }

    /// <summary>按布尔值控制显隐，便于用状态直接驱动。</summary>
    public void SetVisible(bool value)
    {
        if (value) Show();
        else Hide();
    }

    /// <summary>运行时替换图标贴图。</summary>
    public void SetSprite(Sprite newSprite)
    {
        sprite = newSprite;
        if (spriteRenderer != null)
            ApplySprite();
    }

    private bool IsTargetNear()
    {
        Transform target = proximityTarget;
        if (target == null)
        {
            PlayerSelector selector = PlayerSelector.Instance;
            target = selector != null ? selector.currentPlayer?.transform : null;
        }

        if (target == null)
            return false; // 没有可检测的目标时不隐藏

        return Vector3.Distance(transform.position, target.position) <= hideRadius;
    }

    private void EnsureIndicator()
    {
        if (spriteRenderer == null)
        {
            GameObject child = new GameObject("HeadIndicator");
            child.transform.SetParent(transform, false);
            child.transform.localPosition = offset;
            child.transform.localScale = scale;
            spriteRenderer = child.AddComponent<SpriteRenderer>();
        }

        basePosition = spriteRenderer.transform.localPosition;
        spriteRenderer.sortingLayerName = sortingLayerName;
        spriteRenderer.sortingOrder = sortingOrder;
        ApplySprite();
    }

    private void ApplySprite()
    {
        if (sprite != null)
            spriteRenderer.sprite = sprite;
        else if (spriteRenderer.sprite == null)
            spriteRenderer.sprite = CreatePlaceholderSprite();
    }

    private static Sprite CreatePlaceholderSprite()
    {
        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }
}
