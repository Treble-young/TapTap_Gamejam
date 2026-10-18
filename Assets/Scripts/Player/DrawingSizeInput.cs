using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 图纸 UI 上的「长 / 宽」两个数值输入。
///
/// 两条输入路径：
///   1. 玩家输入：玩家直接在输入框里打字，回车或点到别处时生效；
///   2. 程序输入：别的脚本调用 SetLength / SetWidth / SetSize 直接写入。
/// 两条路径都走同一套处理（夹到 [minValue, maxValue]、按 decimals 规范化显示），
/// 并通过 onLengthChanged / onWidthChanged / onSizeChanged 通知出去。
///
/// 两个输入框就是 prefab 里 Drawing 下面的「Size Row - Length / Size Row - Width」两行，
/// 位置、大小可以在编辑器里随手拖；输入框没接也能用（只是缺那一侧的玩家输入）。
/// </summary>
[DisallowMultipleComponent]
public class DrawingSizeInput : MonoBehaviour
{
    [Header("输入框引用")]
    [Tooltip("「长」的输入框，留空则该侧的玩家输入不可用，程序输入仍然有效")]
    [SerializeField] private TMP_InputField lengthInput;
    [Tooltip("「宽」的输入框，留空则该侧的玩家输入不可用，程序输入仍然有效")]
    [SerializeField] private TMP_InputField widthInput;

    [Header("数值")]
    [Tooltip("还没人写过值时「长」的初始值")]
    [SerializeField] private float defaultLength = 1f;
    [Tooltip("还没人写过值时「宽」的初始值")]
    [SerializeField] private float defaultWidth = 1f;
    [Tooltip("允许的最小值，输入小于它会被夹到最小值")]
    [SerializeField] private float minValue = 0f;
    [Tooltip("允许的最大值，输入大于它会被夹到最大值")]
    [SerializeField] private float maxValue = 999f;
    [Tooltip("显示的小数位数，0 表示只显示整数")]
    [Range(0, 3)]
    [SerializeField] private int decimals = 0;

    [Header("锁定外观")]
    [Tooltip("输入框锁住时背景的不透明度倍数（1 = 和平时一样，不变化）")]
    [Range(0f, 1f)]
    [SerializeField] private float lockedAlphaScale = 0.45f;

    [Header("事件")]
    [Tooltip("「长」变化时触发（玩家输入和程序写入都会触发）")]
    public UnityEvent<float> onLengthChanged;
    [Tooltip("「宽」变化时触发（玩家输入和程序写入都会触发）")]
    public UnityEvent<float> onWidthChanged;
    [Tooltip("长或宽任一变化时触发，参数为 (长, 宽)")]
    public UnityEvent<Vector2> onSizeChanged;
    [Tooltip("玩家提交「长」时触发（回车或点到别处），数值没变也会触发；谜题判定用这个")]
    public UnityEvent<float> onLengthSubmitted;
    [Tooltip("玩家提交「宽」时触发（回车或点到别处），数值没变也会触发")]
    public UnityEvent<float> onWidthSubmitted;

    /// <summary>当前「长」。</summary>
    public float Length => length;

    /// <summary>当前「宽」。</summary>
    public float Width => width;

    /// <summary>当前「长」「宽」，x = 长，y = 宽。</summary>
    public Vector2 Size => new Vector2(length, width);

    private float length;
    private float width;
    private bool initialized;
    private float lengthFieldBaseAlpha = 1f;
    private float widthFieldBaseAlpha = 1f;
    private bool baseAlphaCaptured;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void OnEnable()
    {
        // 图纸面板默认是隐藏的，Awake 要等第一次打开才跑；
        // 这里再兜一次，并保证面板打开时显示的就是当前值
        // （面板关着的时候程序改了数值，重新打开也能对上）。
        EnsureInitialized();
        RefreshAllFields();
    }

    // ---- 程序输入 ----

    /// <summary>程序写入「长」。玩家在输入框里看到的内容会同步刷新。</summary>
    public void SetLength(float value)
    {
        Apply(true, value);
    }

    /// <summary>程序写入「宽」。</summary>
    public void SetWidth(float value)
    {
        Apply(false, value);
    }

    /// <summary>程序写入「长」「宽」。</summary>
    public void SetSize(float newLength, float newWidth)
    {
        Apply(true, newLength);
        Apply(false, newWidth);
    }

    /// <summary>程序写入「长」「宽」，x = 长，y = 宽。</summary>
    public void SetSize(Vector2 size)
    {
        SetSize(size.x, size.y);
    }

    /// <summary>恢复成 Inspector 里配置的默认值。</summary>
    public void ResetToDefault()
    {
        SetSize(defaultLength, defaultWidth);
    }

    /// <summary>玩家能不能改这两个输入框（程序驱动的场合可以关掉）。</summary>
    public void SetInteractable(bool interactable)
    {
        SetLengthInteractable(interactable);
        SetWidthInteractable(interactable);
    }

    /// <summary>玩家能不能改「长」。锁上时输入框变暗、敲字无效（程序写入仍然有效）。</summary>
    public void SetLengthInteractable(bool interactable)
    {
        if (lengthInput == null)
            return;

        lengthInput.interactable = interactable;
        TintBackground(lengthInput, interactable);
    }

    /// <summary>玩家能不能改「宽」。</summary>
    public void SetWidthInteractable(bool interactable)
    {
        if (widthInput == null)
            return;

        widthInput.interactable = interactable;
        TintBackground(widthInput, interactable);
    }

    /// <summary>锁定时把输入框背景变暗一点，让玩家看出来这项不能改了。</summary>
    private void TintBackground(TMP_InputField field, bool interactable)
    {
        if (field.targetGraphic == null)
            return;

        Color color = field.targetGraphic.color;
        float baseAlpha = GetBaseAlpha(field);
        float targetAlpha = interactable ? baseAlpha : baseAlpha * lockedAlphaScale;

        if (Mathf.Approximately(color.a, targetAlpha))
            return;

        color.a = targetAlpha;
        field.targetGraphic.color = color;
    }

    private float GetBaseAlpha(TMP_InputField field)
    {
        if (!baseAlphaCaptured)
            CaptureBaseAlpha();

        if (field == lengthInput)
            return lengthFieldBaseAlpha;
        if (field == widthInput)
            return widthFieldBaseAlpha;
        return field.targetGraphic != null ? field.targetGraphic.color.a : 1f;
    }

    // ---- 玩家输入 ----

    private void OnLengthEdited(string _)
    {
        CommitFromField(true);
    }

    private void OnWidthEdited(string _)
    {
        CommitFromField(false);
    }

    private void CommitFromField(bool isLength)
    {
        TMP_InputField field = isLength ? lengthInput : widthInput;
        if (field == null)
            return;

        if (!TryParse(field.text, out float parsed))
        {
            // 输入的不是数字：把框里恢复成当前值，避免留下脏数据
            RefreshField(field, isLength ? length : width);
            return;
        }

        Apply(isLength, parsed);

        // 提交事件：数值没变也触发，方便外部在「玩家确认输入」时做判定
        if (isLength)
            onLengthSubmitted?.Invoke(length);
        else
            onWidthSubmitted?.Invoke(width);
    }

    // ---- 内部：玩家输入和程序输入共用的写入路径 ----

    private void Apply(bool isLength, float value)
    {
        EnsureInitialized();

        float clamped = Clamp(value);
        float before = isLength ? length : width;

        if (isLength)
            length = clamped;
        else
            width = clamped;

        RefreshField(isLength ? lengthInput : widthInput, clamped);

        if (Mathf.Approximately(before, clamped))
            return;

        if (isLength)
            onLengthChanged?.Invoke(length);
        else
            onWidthChanged?.Invoke(width);

        onSizeChanged?.Invoke(Size);
    }

    private void EnsureInitialized()
    {
        if (initialized)
            return;

        initialized = true;

        if (lengthInput != null)
        {
            lengthInput.onEndEdit.RemoveListener(OnLengthEdited);
            lengthInput.onEndEdit.AddListener(OnLengthEdited);
        }

        if (widthInput != null)
        {
            widthInput.onEndEdit.RemoveListener(OnWidthEdited);
            widthInput.onEndEdit.AddListener(OnWidthEdited);
        }

        length = Clamp(defaultLength);
        width = Clamp(defaultWidth);

        // 记下输入框背景原本的不透明度，锁定变暗时以它为基准（保留你在 prefab 里调的颜色）
        CaptureBaseAlpha();
    }

    private void CaptureBaseAlpha()
    {
        if (baseAlphaCaptured)
            return;

        baseAlphaCaptured = true;

        if (lengthInput != null && lengthInput.targetGraphic != null)
            lengthFieldBaseAlpha = lengthInput.targetGraphic.color.a;

        if (widthInput != null && widthInput.targetGraphic != null)
            widthFieldBaseAlpha = widthInput.targetGraphic.color.a;
    }

    private void RefreshAllFields()
    {
        RefreshField(lengthInput, length);
        RefreshField(widthInput, width);
    }

    private void RefreshField(TMP_InputField field, float value)
    {
        if (field == null)
            return;

        // 面板还没显示时写文本没意义，等 OnEnable 里统一刷新
        if (!field.gameObject.activeInHierarchy)
            return;

        field.SetTextWithoutNotify(Format(value));
    }

    private float Clamp(float value)
    {
        float min = Mathf.Min(minValue, maxValue);
        float max = Mathf.Max(minValue, maxValue);
        return Mathf.Clamp(value, min, max);
    }

    private string Format(float value)
    {
        return value.ToString("F" + Mathf.Max(0, decimals), CultureInfo.InvariantCulture);
    }

    private static bool TryParse(string text, out float value)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            value = 0f;
            return false;
        }

        // 先用不变文化（小数点是 '.'），再退回当前系统文化（有些键盘输入逗号）
        return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
               || float.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value)
               || float.TryParse(text, out value);
    }
}
