using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// 世界空间拟声词效果：让“滴”“答”等字符作为独立物体在场景中按顺序逐个冒出，
/// 模拟“听到声音”的视觉表现（例如水滴声“滴答滴答”）。
/// 挂在场景中的 GameObject 上即可，每个字符都会生成一个独立的 TextMeshPro，
/// 互不连接、各自上浮并淡出。
/// </summary>
public class OnomatopoeiaText : MonoBehaviour
{
    [Header("字符")]
    [Tooltip("依次出现的字符序列，例如“滴答”会按 滴→答→滴→答… 循环")]
    [SerializeField] private string characters = "滴答";
    [Tooltip("显示中文必须指定包含中文字形的 TMP 字体，否则文字不可见")]
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private float fontSize = 12f;
    [SerializeField] private Color color = Color.white;

    [Header("节奏")]
    [SerializeField] private float interval = 1.5f;       // 每个字符出现的间隔（秒）

    [Header("渲染")]
    [Tooltip("渲染所在排序图层，默认用最上层的 Light Glow，避免被地面/grid 挡住")]
    [SerializeField] private string sortingLayerName = "Light Glow";
    [Tooltip("同图层内的排序序号，数值越大越靠前")]
    [SerializeField] private int sortingOrder = 100;
    [SerializeField] private float scale = .5f;

    [Header("位置与运动")]
    [Tooltip("字符在水平/垂直方向上的随机偏移范围，避免字符重叠")]
    [SerializeField] private float randomOffset = 0.6f;
    [Tooltip("字符向上漂浮的速度，0 表示原地不动")]
    [SerializeField] private float riseSpeed = 0.5f;

    [Header("生命周期")]
    [SerializeField] private float lifetime = 1.2f;       // 出现后停留多久开始淡出（秒）
    [SerializeField] private float fadeDuration = 0.5f;   // 淡出时长（秒）

    [Header("启动")]
    [SerializeField] private bool playOnStart = true;

    [Header("玩家接近暂停")]
    [Tooltip("玩家靠近时暂停播放（不生成新字符），离开后恢复")]
    [SerializeField] private bool pauseWhenPlayerNear = true;
    [Tooltip("触发暂停的半径（世界单位），会自动添加一个触发器来检测玩家")]
    [SerializeField] private float pauseRadius = 3f;

    private Coroutine routine;
    private int charIndex;
    private TMP_FontAsset resolvedFont;
    private bool isPaused;
    private int nearbyPlayerCount;

    private void Awake()
    {
        resolvedFont = ResolveFont();

        if (resolvedFont == null || !resolvedFont.HasCharacters(characters, out _))
        {
            Debug.LogWarning(
                $"[OnomatopoeiaText] 字体“{resolvedFont?.name ?? "无"}”不包含字符“{characters}”，" +
                "文字会不可见。请在 Unity 中用 Font Asset Creator 烘焙中文字体后赋给 font 字段。", this);
        }

        SetupPauseTrigger();
    }

    private void Start()
    {
        if (playOnStart)
            Play();
    }

    /// <summary>开始播放“滴答”效果。</summary>
    public void Play()
    {
        Stop();
        routine = StartCoroutine(PlayRoutine());
    }

    /// <summary>停止生成新字符（已生成的字符会继续播放完再消失）。</summary>
    public void Stop()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
    }

    /// <summary>暂停播放（不生成新字符，已有的会继续淡出）。</summary>
    public void Pause()
    {
        isPaused = true;
    }

    /// <summary>恢复播放。</summary>
    public void Resume()
    {
        isPaused = false;
    }

    private void SetupPauseTrigger()
    {
        if (!pauseWhenPlayerNear)
            return;

        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col == null)
            col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = pauseRadius;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!pauseWhenPlayerNear || collision.GetComponent<PlayerManager>() == null)
            return;

        nearbyPlayerCount++;
        Pause();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!pauseWhenPlayerNear || collision.GetComponent<PlayerManager>() == null)
            return;

        nearbyPlayerCount--;
        if (nearbyPlayerCount <= 0)
        {
            nearbyPlayerCount = 0;
            Resume();
        }
    }

    private TMP_FontAsset ResolveFont()
    {
        if (font != null)
            return font;

        // 默认字体（LiberationSans）只含拉丁字符，尝试在已加载字体中查找能显示这些字符的字体
        TMP_FontAsset[] all = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        foreach (TMP_FontAsset f in all)
        {
            if (f != null && f.HasCharacters(characters, out _))
                return f;
        }

        return TMP_Settings.defaultFontAsset;
    }

    private IEnumerator PlayRoutine()
    {
        if (string.IsNullOrEmpty(characters))
            yield break;

        while (true)
        {
            // 玩家靠近时暂停，离开后再继续生成
            while (isPaused)
                yield return null;

            SpawnCharacter(characters[charIndex % characters.Length]);
            charIndex++;

            yield return new WaitForSeconds(interval);
        }
    }

    private void SpawnCharacter(char c)
    {
        GameObject go = new GameObject("Onomatopoeia_" + c);
        go.transform.SetParent(transform, false);

        Vector3 offset = new Vector3(
            Random.Range(-randomOffset, randomOffset),
            Random.Range(-randomOffset, randomOffset),
            0f);
        go.transform.localPosition = offset;
        go.transform.localScale = Vector3.one * scale;

        if (go.GetComponent<MeshRenderer>() == null)
            go.AddComponent<MeshRenderer>();

        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.text = c.ToString();
        tmp.font = resolvedFont != null ? resolvedFont : TMP_Settings.defaultFontAsset;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.sortingLayerID = SortingLayer.NameToID(sortingLayerName);
        tmp.sortingOrder = sortingOrder;

        StartCoroutine(FadeOutAndDestroy(go, tmp));
    }

    private IEnumerator FadeOutAndDestroy(GameObject go, TextMeshPro tmp)
    {
        float elapsed = 0f;
        float total = lifetime + fadeDuration;

        while (elapsed < total)
        {
            elapsed += Time.deltaTime;

            if (riseSpeed > 0f)
                go.transform.localPosition += Vector3.up * (riseSpeed * Time.deltaTime);

            // 只在淡出阶段降低透明度
            if (elapsed > lifetime && fadeDuration > 0f)
            {
                float t = Mathf.Clamp01((elapsed - lifetime) / fadeDuration);
                tmp.alpha = (byte)(255f * (1f - t));
            }

            yield return null;
        }

        Destroy(go);
    }
}
