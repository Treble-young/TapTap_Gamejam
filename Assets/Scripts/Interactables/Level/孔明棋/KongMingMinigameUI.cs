using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 孔明棋解绳：5x5 十字棋盘，最后一枚棋子必须落在中心金框。
/// 棋盘和棋子的视觉按策划参考图实现，界面在运行时构建。
/// </summary>
public class KongMingMinigameUI : MonoBehaviour
{
    private const int BoardSize = 5;
    private const float CellSize = 184f;
    private const float TileSize = 176f;
    private const int CenterIndex = 12;

    private static readonly bool[] ValidCells = BuildValidCells();

    private static readonly Color BackgroundColor = new Color(0.105f, 0.16f, 0.19f, 1f);
    private static readonly Color TileFill = new Color(0.28f, 0.37f, 0.41f, 1f);
    private static readonly Color TileBorder = new Color(0.55f, 0.64f, 0.67f, 1f);
    private static readonly Color DotColor = new Color(0.10f, 0.15f, 0.18f, 1f);
    private static readonly Color PegFill = new Color(0.83f, 0.62f, 0.34f, 1f);
    private static readonly Color PegBorder = new Color(0.95f, 0.75f, 0.46f, 1f);
    private static readonly Color PegDash = new Color(0.26f, 0.18f, 0.11f, 1f);
    private static readonly Color Gold = new Color(0.88f, 0.72f, 0.38f, 1f);
    private static readonly Color TextMain = new Color(0.91f, 0.95f, 0.96f, 1f);
    private static readonly Color TextMuted = new Color(0.67f, 0.76f, 0.79f, 1f);
    private static readonly Color Valid = new Color(0.93f, 0.79f, 0.42f, 1f);

    private KongMingBase owner;
    private TMP_FontAsset font;
    private bool built;
    private bool solved;
    private bool animating;

    private RectTransform boardArea;
    private GameObject successLayer;
    private TextMeshProUGUI successBody;
    private TextMeshProUGUI statusText;
    private TextMeshProUGUI progressText;
    private Image knotImage;
    private TextMeshProUGUI knotStatus;
    private TextMeshProUGUI knotHint;

    private ulong state;
    private int pegCount;
    private int selected = -1;

    private readonly Stack<ulong> history = new Stack<ulong>();
    private readonly Dictionary<int, GameObject> pegViews = new Dictionary<int, GameObject>();
    private readonly Dictionary<int, Image> dotViews = new Dictionary<int, Image>();
    private readonly List<GameObject> markers = new List<GameObject>();

    private Image hoverBorder;

    public bool IsSolved => solved;

    public void Open(KongMingBase kongMingBase)
    {
        owner = kongMingBase;
        MinigameTheme.EnsureEventSystem();

        if (!built)
            Build();

        gameObject.SetActive(true);
        SetStatus(solved ? "绳子已经解开了。" : "点一枚棋子，再点它要跳到的空位。");
    }

    private void Build()
    {
        built = true;
        font = MinigameTheme.ResolveFont(transform);

        Canvas canvas = GetComponent<Canvas>();
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        MinigameTheme.ConfigureCanvas(canvas, scaler);
        MinigameTheme.ClearChildren(transform);

        MinigameTheme.CreateStretchedImage(transform, "Background", null, BackgroundColor, true);

        MinigameTheme.CreateText(transform, "Title", "孔明棋 · 解绳", 34f, TextMain,
            new Vector2(900f, 54f), new Vector2(0f, 505f), font);

        MinigameTheme.CreateText(transform, "Subtitle", "让最后一枚棋子落在中心的金色框内", 20f, TextMuted,
            new Vector2(900f, 34f), new Vector2(0f, 470f), font);

        GameObject areaObject = new GameObject("BoardArea", typeof(RectTransform));
        areaObject.transform.SetParent(transform, false);
        boardArea = (RectTransform)areaObject.transform;
        boardArea.anchorMin = new Vector2(0.5f, 0.5f);
        boardArea.anchorMax = new Vector2(0.5f, 0.5f);
        boardArea.pivot = new Vector2(0.5f, 0.5f);
        boardArea.sizeDelta = new Vector2(920f, 920f);
        boardArea.anchoredPosition = new Vector2(-120f, -25f);

        BuildTiles();
        BuildBoardInput();
        BuildSidePanel();
        BuildFooter();
        BuildSuccessLayer();

        ResetBoard();
    }

    private void BuildTiles()
    {
        Sprite tileSprite = MinigameTheme.RoundedRect(64, 12, TileFill, TileBorder, 2);
        Sprite dotSprite = MinigameTheme.Circle(16, Color.white, new Color(0f, 0f, 0f, 0f));
        Sprite targetFrame = MinigameTheme.RoundedBorder(64, 14, Gold, 3);

        for (int r = 0; r < BoardSize; r++)
        {
            for (int c = 0; c < BoardSize; c++)
            {
                int index = r * BoardSize + c;
                if (!ValidCells[index])
                    continue;

                GameObject tileObject = new GameObject("Tile_" + index, typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(Image));
                tileObject.transform.SetParent(boardArea, false);

                RectTransform rect = (RectTransform)tileObject.transform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(TileSize, TileSize);
                rect.anchoredPosition = CellPosition(index);

                Image tile = tileObject.GetComponent<Image>();
                tile.sprite = tileSprite;
                tile.type = Image.Type.Sliced;
                tile.color = Color.white;
                tile.raycastTarget = false;

                Shadow shadow = tileObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.20f);
                shadow.effectDistance = new Vector2(0f, -3f);

                Image dot = MinigameTheme.CreateImage(tileObject.transform, "Dot", dotSprite, DotColor,
                    new Vector2(16f, 16f), Vector2.zero);
                dot.raycastTarget = false;
                dotViews[index] = dot;

                if (index == CenterIndex)
                {
                    Image frame = MinigameTheme.CreateImage(tileObject.transform, "TargetFrame", targetFrame,
                        Color.white, new Vector2(186f, 186f), Vector2.zero);
                    frame.type = Image.Type.Sliced;
                    frame.raycastTarget = false;
                }
            }
        }

        hoverBorder = MinigameTheme.CreateImage(boardArea, "HoverBorder",
            MinigameTheme.RoundedBorder(64, 14, new Color(0.72f, 0.82f, 0.85f, 0.38f), 2),
            Color.white, new Vector2(186f, 186f), Vector2.zero);
        hoverBorder.type = Image.Type.Sliced;
        hoverBorder.raycastTarget = false;
        hoverBorder.enabled = false;
    }

    private void BuildBoardInput()
    {
        Image input = MinigameTheme.CreateImage(boardArea, "BoardInput", MinigameTheme.Panel(),
            new Color(0f, 0f, 0f, 0.001f), new Vector2(920f, 920f), Vector2.zero, false, true);
        KongMingBoardInput boardInput = input.gameObject.AddComponent<KongMingBoardInput>();
        boardInput.owner = this;
        boardInput.boardRect = boardArea;
    }

    private void BuildSidePanel()
    {
        Image panel = MinigameTheme.CreateImage(transform, "KnotPanel",
            MinigameTheme.RoundedRect(64, 14, new Color(0.18f, 0.25f, 0.29f, 1f), TileBorder, 2),
            Color.white, new Vector2(230f, 300f), new Vector2(470f, 25f));
        panel.type = Image.Type.Sliced;

        MinigameTheme.CreateText(transform, "KnotTitle", "绳 结", 22f, TextMain,
            new Vector2(180f, 34f), new Vector2(470f, 135f), font);

        knotImage = MinigameTheme.CreateImage(transform, "KnotIcon", MinigameTheme.Knot(120), Color.white,
            new Vector2(120f, 120f), new Vector2(470f, 45f));
        knotImage.preserveAspect = true;

        knotStatus = MinigameTheme.CreateText(transform, "KnotStatus", "系紧", 24f, Gold,
            new Vector2(190f, 34f), new Vector2(470f, -45f), font);

        knotHint = MinigameTheme.CreateText(transform, "KnotHint", "完成解绳后松开", 17f, TextMuted,
            new Vector2(200f, 44f), new Vector2(470f, -85f), font);
    }

    private void SetKnotState(bool untied)
    {
        if (knotImage != null)
        {
            knotImage.sprite = untied ? MinigameTheme.LooseRope(120) : MinigameTheme.Knot(120);
            knotImage.color = Color.white;
            knotImage.rectTransform.localScale = Vector3.one;
        }

        if (knotStatus != null)
        {
            knotStatus.text = untied ? "已解开" : "系紧";
            knotStatus.color = untied ? new Color(0.62f, 0.86f, 0.55f, 1f) : Gold;
        }

        if (knotHint != null)
            knotHint.text = untied ? "获得两条带绳结的绳子" : "完成解绳后松开";
    }

    private void BuildFooter()
    {
        progressText = MinigameTheme.CreateText(transform, "Progress", "剩余 10 枚", 19f, TextMuted,
            new Vector2(180f, 30f), new Vector2(-500f, -512f), font, TextAlignmentOptions.Left);

        statusText = MinigameTheme.CreateText(transform, "Status", string.Empty, 19f, TextMain,
            new Vector2(520f, 40f), new Vector2(-130f, -512f), font, TextAlignmentOptions.Center);

        MinigameTheme.CreateButton(transform, "UndoButton", "撤销", new Vector2(130f, 46f),
            new Vector2(250f, -512f), font, UndoClicked, 20f);
        MinigameTheme.CreateButton(transform, "RestartButton", "重来", new Vector2(130f, 46f),
            new Vector2(400f, -512f), font, ResetClicked, 20f);
        MinigameTheme.CreateButton(transform, "ExitButton", "退出", new Vector2(130f, 46f),
            new Vector2(550f, -512f), font, ExitClicked, 20f);
    }

    private void BuildSuccessLayer()
    {
        successLayer = MinigameTheme.CreateLayer(transform, "SuccessLayer").gameObject;
        MinigameTheme.CreateStretchedImage(successLayer.transform, "SuccessDim", null,
            new Color(0.02f, 0.03f, 0.035f, 0.76f), true);

        Image panel = MinigameTheme.CreateImage(successLayer.transform, "SuccessPanel",
            MinigameTheme.RoundedRect(64, 16, new Color(0.11f, 0.17f, 0.20f, 1f), Gold, 3),
            Color.white, new Vector2(660f, 360f), Vector2.zero, false, true);
        panel.type = Image.Type.Sliced;

        MinigameTheme.CreateText(successLayer.transform, "SuccessTitle", "绳子解开了！", 38f, Gold,
            new Vector2(600f, 56f), new Vector2(0f, 120f), font);

        successBody = MinigameTheme.CreateText(successLayer.transform, "SuccessBody",
            "最后一枚棋子落在了中心金框。\n你解下两条带绳结的绳子。", 22f, TextMain,
            new Vector2(600f, 80f), new Vector2(0f, 58f), font);

        MinigameTheme.CreateImage(successLayer.transform, "RewardRopeA", MinigameTheme.Knot(82), Color.white,
            new Vector2(82f, 82f), new Vector2(-65f, -32f)).preserveAspect = true;
        MinigameTheme.CreateImage(successLayer.transform, "RewardRopeB", MinigameTheme.Knot(82), Color.white,
            new Vector2(82f, 82f), new Vector2(65f, -32f)).preserveAspect = true;

        MinigameTheme.CreateText(successLayer.transform, "RewardLabel", "带绳结的绳子 ×2", 20f, TextMuted,
            new Vector2(360f, 28f), new Vector2(0f, -92f), font);

        MinigameTheme.CreateButton(successLayer.transform, "SuccessButton", "收下绳子", new Vector2(200f, 52f),
            new Vector2(0f, -145f), font, CompleteClicked, 22f);

        successLayer.SetActive(false);
    }

    private void ResetBoard()
    {
        StopAllCoroutines();
        animating = false;
        solved = false;
        selected = -1;
        state = InitialState();
        history.Clear();
        pegCount = CountPegs(state);

        if (successLayer != null)
            successLayer.SetActive(false);

        ClearMarkers();
        ClearBoardHover();
        RebuildPegViews();
        SetKnotState(false);
        UpdateStats();
        SetStatus("点一枚棋子，再点它要跳到的空位。");
    }

    private void ResetClicked()
    {
        if (solved)
            return;

        ResetBoard();
    }

    private void UndoClicked()
    {
        if (solved || animating)
            return;

        if (history.Count == 0)
        {
            SetStatus("还没有可以撤销的步骤。");
            return;
        }

        state = history.Pop();
        selected = -1;
        pegCount = CountPegs(state);
        ClearMarkers();
        RebuildPegViews();
        UpdateStats();
        SetStatus("撤销了一步。");
    }

    public void HandleBoardClick(Vector2 screenPosition, Camera eventCamera)
    {
        if (solved || animating)
            return;

        if (!TryScreenToCell(screenPosition, eventCamera, out int index))
            return;

        bool occupied = HasPeg(index);

        if (occupied)
        {
            if (selected == index)
            {
                selected = -1;
                ClearMarkers();
                SetStatus("取消选择。");
                return;
            }

            if (HasMoveFrom(index))
            {
                selected = index;
                ShowSelection();
                SetStatus("点一个高亮的空位，让棋子跳过去。");
            }
            else
            {
                selected = -1;
                ClearMarkers();
                SetStatus("这枚棋子暂时动不了，先动别的棋子。");
            }

            return;
        }

        if (selected >= 0 && IsLegalMove(selected, index))
        {
            ExecuteMove(selected, index);
            return;
        }

        if (selected >= 0)
            SetStatus("只能跳过紧挨着的一枚棋子，落到它后面的空位。");
        else
            SetStatus("先点一枚棋子，再点它要跳到的位置。");
    }

    public void HandleBoardHover(Vector2 screenPosition, Camera eventCamera)
    {
        if (solved || animating || hoverBorder == null)
        {
            ClearBoardHover();
            return;
        }

        if (!TryScreenToCell(screenPosition, eventCamera, out int index))
        {
            ClearBoardHover();
            return;
        }

        hoverBorder.enabled = true;
        hoverBorder.rectTransform.anchoredPosition = CellPosition(index);
    }

    public void ClearBoardHover()
    {
        if (hoverBorder != null)
            hoverBorder.enabled = false;
    }

    private void ExecuteMove(int from, int to)
    {
        int middle = (from + to) / 2;

        history.Push(state);
        state &= ~(1UL << from);
        state &= ~(1UL << middle);
        state |= 1UL << to;
        pegCount = CountPegs(state);

        selected = -1;
        ClearMarkers();
        ClearBoardHover();
        UpdateStats();
        SetStatus("跳！中间那枚棋子被吃掉了。");

        StartCoroutine(AnimateMove(from, middle, to));
    }

    private IEnumerator AnimateMove(int from, int middle, int to)
    {
        animating = true;

        pegViews.TryGetValue(from, out GameObject source);
        pegViews.TryGetValue(middle, out GameObject jumped);

        RectTransform sourceRect = source != null ? (RectTransform)source.transform : null;
        RectTransform jumpedRect = jumped != null ? (RectTransform)jumped.transform : null;
        CanvasGroup jumpedGroup = jumped != null ? jumped.GetComponent<CanvasGroup>() : null;

        Vector2 start = CellPosition(from);
        Vector2 end = CellPosition(to);
        float elapsed = 0f;
        const float duration = 0.16f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (sourceRect != null)
            {
                Vector2 position = Vector2.Lerp(start, end, t);
                position.y += Mathf.Sin(t * Mathf.PI) * 18f;
                sourceRect.anchoredPosition = position;
            }

            if (jumpedRect != null)
            {
                jumpedRect.localScale = Vector3.one * Mathf.Lerp(1f, 0.2f, t);
                if (jumpedGroup != null)
                    jumpedGroup.alpha = Mathf.Lerp(1f, 0f, t);
            }

            yield return null;
        }

        if (sourceRect != null)
        {
            sourceRect.anchoredPosition = end;
            sourceRect.localScale = Vector3.one;
        }

        pegViews.Remove(from);
        pegViews.Remove(middle);
        if (source != null)
            pegViews[to] = source;
        if (jumped != null)
            Destroy(jumped);

        animating = false;
        CheckBoardState();
    }

    private void ShowSelection()
    {
        ClearMarkers();

        if (selected < 0)
            return;

        Image selectedBorder = MinigameTheme.CreateImage(boardArea, "SelectedTile",
            MinigameTheme.RoundedBorder(64, 14, new Color(0.94f, 0.97f, 0.98f, 0.95f), 3),
            Color.white, new Vector2(186f, 186f), CellPosition(selected));
        selectedBorder.type = Image.Type.Sliced;
        selectedBorder.raycastTarget = false;
        markers.Add(selectedBorder.gameObject);

        List<int> targets = LegalTargets(selected);
        for (int i = 0; i < targets.Count; i++)
        {
            if (!dotViews.TryGetValue(targets[i], out Image dot))
                continue;

            dot.color = Valid;
            dot.rectTransform.localScale = Vector3.one * 1.8f;
        }
    }

    private void ClearMarkers()
    {
        for (int i = markers.Count - 1; i >= 0; i--)
            MinigameTheme.SafeDestroy(markers[i]);
        markers.Clear();

        foreach (KeyValuePair<int, Image> pair in dotViews)
        {
            if (pair.Value == null)
                continue;

            pair.Value.color = DotColor;
            pair.Value.rectTransform.localScale = Vector3.one;
        }
    }

    private void CheckBoardState()
    {
        if (pegCount == 1)
        {
            if (state == (1UL << CenterIndex))
            {
                solved = true;
                selected = -1;
                ClearMarkers();
                SetKnotState(true);
                SetStatus("最后一枚棋子落在金框里，绳子解开了！");
                owner?.NotifySolved();
                StartCoroutine(SuccessRoutine());
            }
            else
            {
                SetStatus("只剩一枚棋子，但它不在金色框里。撤销一步再试试。");
            }
            return;
        }

        if (!HasAnyMove())
            SetStatus("没有可走的步了。撤销一步，或者重来。");
    }

    private IEnumerator SuccessRoutine()
    {
        yield return new WaitForSecondsRealtime(0.55f);

        if (successBody != null)
            successBody.text = "最后一枚棋子落在了中心金框。\n你解下两条带绳结的绳子。";

        if (successLayer != null)
        {
            successLayer.SetActive(true);
            successLayer.transform.SetAsLastSibling();
        }
    }

    private void UpdateStats()
    {
        if (progressText != null)
            progressText.text = $"剩余 {pegCount} 枚  ·  步数 {history.Count}";
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    private void CompleteClicked()
    {
        owner?.CompleteAndClose();
    }

    private void ExitClicked()
    {
        owner?.CloseMinigame();
    }

    /// <summary>Editor preview helper: reset the board before opening it again.</summary>
    public void RestartForPreview()
    {
        if (!built)
            return;

        ResetBoard();
    }

    /// <summary>Editor preview helper: jump straight to the solved board and success panel.</summary>
    public void PreviewSuccess()
    {
        state = 1UL << CenterIndex;
        pegCount = 1;
        selected = -1;
        solved = true;
        ClearMarkers();
        ClearBoardHover();
        RebuildPegViews();
        SetKnotState(true);
        UpdateStats();
        SetStatus("最后一枚棋子落在金框里，绳子解开了！");

        if (successLayer != null)
        {
            successLayer.SetActive(true);
            successLayer.transform.SetAsLastSibling();
        }
    }

    private void RebuildPegViews()
    {
        foreach (KeyValuePair<int, GameObject> pair in pegViews)
        {
            if (pair.Value != null)
                MinigameTheme.SafeDestroy(pair.Value);
        }
        pegViews.Clear();

        for (int r = 0; r < BoardSize; r++)
        {
            for (int c = 0; c < BoardSize; c++)
            {
                int index = r * BoardSize + c;
                if (!ValidCells[index] || !HasPeg(index))
                    continue;

                CreatePegView(index);
            }
        }

        if (hoverBorder != null)
            hoverBorder.transform.SetAsLastSibling();
    }

    private void CreatePegView(int index)
    {
        GameObject root = new GameObject("Peg_" + index, typeof(RectTransform), typeof(CanvasRenderer),
            typeof(Image), typeof(CanvasGroup));
        root.transform.SetParent(boardArea, false);

        RectTransform rect = (RectTransform)root.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(138f, 138f);
        rect.anchoredPosition = CellPosition(index);

        Image image = root.GetComponent<Image>();
        image.sprite = MinigameTheme.RoundedRect(64, 10, PegFill, PegBorder, 2);
        image.type = Image.Type.Sliced;
        image.color = Color.white;
        image.raycastTarget = false;

        Shadow shadow = root.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.28f);
        shadow.effectDistance = new Vector2(0f, -4f);

        Image dash = MinigameTheme.CreateImage(root.transform, "Dash",
            MinigameTheme.RoundedRect(16, 3, PegDash, PegDash, 0), Color.white,
            new Vector2(20f, 5f), Vector2.zero);
        dash.type = Image.Type.Simple;
        dash.raycastTarget = false;

        pegViews[index] = root;
    }

    private bool TryScreenToCell(Vector2 screenPosition, Camera eventCamera, out int index)
    {
        index = -1;

        if (boardArea == null)
            return false;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(boardArea, screenPosition, eventCamera,
                out Vector2 local))
            return false;

        int col = Mathf.RoundToInt(local.x / CellSize + 2f);
        int row = Mathf.RoundToInt(2f - local.y / CellSize);

        if (row < 0 || row >= BoardSize || col < 0 || col >= BoardSize)
            return false;

        int candidate = row * BoardSize + col;
        if (!ValidCells[candidate])
            return false;

        Vector2 center = CellPosition(candidate);
        if (Mathf.Abs(local.x - center.x) > TileSize * 0.52f ||
            Mathf.Abs(local.y - center.y) > TileSize * 0.52f)
            return false;

        index = candidate;
        return true;
    }

    private static Vector2 CellPosition(int index)
    {
        int row = index / BoardSize;
        int col = index % BoardSize;
        return new Vector2((col - 2) * CellSize, (2 - row) * CellSize);
    }

    private static bool[] BuildValidCells()
    {
        bool[] valid = new bool[BoardSize * BoardSize];
        for (int r = 0; r < BoardSize; r++)
        {
            for (int c = 0; c < BoardSize; c++)
                valid[r * BoardSize + c] = !((r == 0 || r == 4) && (c == 0 || c == 4));
        }
        return valid;
    }

    private static ulong InitialState()
    {
        ulong value = 0UL;

        // 与策划参考图一致的 10 枚棋子开局。
        for (int r = 0; r < BoardSize; r++)
        {
            for (int c = 0; c < BoardSize; c++)
            {
                int index = r * BoardSize + c;
                if (!ValidCells[index])
                    continue;

                bool hasPeg = (r == 1 && c <= 3) ||
                              (r == 2 && c >= 1) ||
                              (r == 3 && c >= 1 && c <= 2);

                if (hasPeg)
                    value |= 1UL << index;
            }
        }

        return value;
    }

    private bool HasPeg(int index)
    {
        return (state & (1UL << index)) != 0UL;
    }

    private static int CountPegs(ulong value)
    {
        int count = 0;
        while (value != 0UL)
        {
            value &= value - 1UL;
            count++;
        }
        return count;
    }

    private bool IsLegalMove(int from, int to)
    {
        if (!ValidCells[from] || !ValidCells[to] || !HasPeg(from) || HasPeg(to))
            return false;

        int fromRow = from / BoardSize;
        int fromCol = from % BoardSize;
        int toRow = to / BoardSize;
        int toCol = to % BoardSize;

        int deltaRow = toRow - fromRow;
        int deltaCol = toCol - fromCol;

        if (Mathf.Abs(deltaRow) + Mathf.Abs(deltaCol) != 2)
            return false;
        if (Mathf.Abs(deltaRow) > 0 && Mathf.Abs(deltaCol) > 0)
            return false;

        int middleRow = (fromRow + toRow) / 2;
        int middleCol = (fromCol + toCol) / 2;
        int middle = middleRow * BoardSize + middleCol;

        return ValidCells[middle] && HasPeg(middle);
    }

    private List<int> LegalTargets(int from)
    {
        List<int> targets = new List<int>();
        int fromRow = from / BoardSize;
        int fromCol = from % BoardSize;

        int[] rowDirections = { 0, 0, 1, -1 };
        int[] colDirections = { 1, -1, 0, 0 };

        for (int i = 0; i < 4; i++)
        {
            int toRow = fromRow + rowDirections[i] * 2;
            int toCol = fromCol + colDirections[i] * 2;

            if (toRow < 0 || toRow >= BoardSize || toCol < 0 || toCol >= BoardSize)
                continue;

            int to = toRow * BoardSize + toCol;
            if (IsLegalMove(from, to))
                targets.Add(to);
        }

        return targets;
    }

    private bool HasMoveFrom(int from)
    {
        return LegalTargets(from).Count > 0;
    }

    private bool HasAnyMove()
    {
        for (int i = 0; i < ValidCells.Length; i++)
        {
            if (ValidCells[i] && HasPeg(i) && HasMoveFrom(i))
                return true;
        }
        return false;
    }
}
