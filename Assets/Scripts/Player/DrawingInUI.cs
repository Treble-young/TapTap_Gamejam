using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DrawingInUI : MonoBehaviour
{
    public GameObject drawingUIPanel;

    [Header("UI")]
    public Image image;
    public Sprite throwStoneSprite;
    public GameObject ON;

    [Header("长 / 宽输入")]
    [Tooltip("图纸上的「长 / 宽」输入框，留空则运行时自动在 Drawing 下面查找")]
    [SerializeField] private DrawingSizeInput sizeInput;

    [Header("输入正确")]
    [Tooltip("工程师已经告诉过玩家桥的宽度：宽度随即锁死，玩家改不了")]
    public bool hasToldWidth = false;
    [Tooltip("玩家已经扔过石头量长度：只有它为 true，长度输入才会被判定")]
    public bool hasThrewStone = false;
    [Tooltip("长度已经量对：量对后长度锁死，不再接受更改")]
    public bool lengthIsCorrect = false;

    [Header("谜题设置")]
    [Tooltip("桥的正确长度，玩家输入这个数才算量对")]
    public float correctLength = 8f;
    [Tooltip("量对后把图纸推进到第几阶段（2 = 完整）")]
    public int completeStage = 2;
    [Tooltip("扔石头之前，图纸至少要到的阶段（1 = 半张）；没到这个阶段不让扔")]
    public int requiredStage = 1;
    [Tooltip("量对时弹出的提示，留空则不弹")]
    [TextArea(1, 3)]
    public string lengthCorrectMessage = "长度量对了，图纸完整了！";
    [Tooltip("手里没有石头或绳子时，扔石头的提示，留空则不弹")]
    [TextArea(1, 3)]
    public string needStoneAndRopeMessage = "我得先找块石头和一根绳子，才能量出桥的长度。";

    private bool widthEditableApplied = true;
    private bool lengthEditableApplied = true;
    private bool lengthGateWasOpen;
    private Sprite defaultDrawingSprite;
    private bool defaultSpriteCaptured;
    private bool exitRequested;

    /// <summary>图纸上的「长 / 宽」输入框（null 表示这次打开图纸没有输入框）。</summary>
    public DrawingSizeInput SizeInput
    {
        get
        {
            if (sizeInput == null)
                sizeInput = GetComponentInChildren<DrawingSizeInput>(true);
            return sizeInput;
        }
    }

    /// <summary>当前「长」。</summary>
    public float Length => SizeInput != null ? SizeInput.Length : 0f;

    /// <summary>当前「宽」。</summary>
    public float Width => SizeInput != null ? SizeInput.Width : 0f;

    /// <summary>当前「长」「宽」，x = 长，y = 宽。</summary>
    public Vector2 Size => SizeInput != null ? SizeInput.Size : Vector2.zero;

    /// <summary>长输入的判定开关：宽度被告知过、且石头也扔过，长度才作数。</summary>
    public bool LengthGateOpen => hasToldWidth && hasThrewStone;

    private void Awake()
    {
        CaptureDefaultSprite();
    }

    private void OnEnable()
    {
        DrawingSizeInput input = SizeInput;
        if (input != null)
        {
            input.onLengthSubmitted.RemoveListener(OnLengthSubmitted);
            input.onLengthSubmitted.AddListener(OnLengthSubmitted);
        }

        // 让打开面板后的第一帧补一次判定：如果之前已经填对了，直接算数
        lengthGateWasOpen = false;
        ApplyInteractableState();

        // 重新打开面板时也要对（面板关着的时候可能已经扔过石头了）
        RefreshDrawingImage();
    }

    /// <summary>记下 prefab 里那张原始图纸大图，之后才能换回来。</summary>
    private void CaptureDefaultSprite()
    {
        if (defaultSpriteCaptured)
            return;

        defaultSpriteCaptured = true;

        if (image != null)
            defaultDrawingSprite = image.sprite;
    }

    /// <summary>按当前进度刷新图纸大图：扔过石头就换成 throwStoneSprite。</summary>
    private void RefreshDrawingImage()
    {
        if (image == null)
            return;

        CaptureDefaultSprite();

        Sprite target = hasThrewStone && throwStoneSprite != null ? throwStoneSprite : defaultDrawingSprite;

        if (target != null && image.sprite != target)
            image.sprite = target;
    }

    private void OnDisable()
    {
        DrawingSizeInput input = SizeInput;
        if (input != null)
            input.onLengthSubmitted.RemoveListener(OnLengthSubmitted);
    }

    private void Update()
    {
        ApplyInteractableState();
        RefreshOnIndicator();

        // 扔完石头／被告知宽度的那一刻，如果框里已经是正确长度，也算量对
        bool gateOpen = LengthGateOpen;
        if (gateOpen && !lengthGateWasOpen)
            TryAcceptLength();
        lengthGateWasOpen = gateOpen;

        // 量对之后自动关掉图纸面板（延后一帧执行，免得在输入框的回调里把它自己关掉）
        if (exitRequested)
        {
            exitRequested = false;
            Exit();
        }
    }

    /// <summary>图纸到「半张」（stage 1）并且已经扔过石头量过之后，才把 ON 打开；其他情况关掉。</summary>
    private void RefreshOnIndicator()
    {
        if (ON == null)
            return;

        Drawing drawing = HeldDrawing;
        bool shouldShow = hasThrewStone && drawing != null && drawing.drawingStage == 1;

        if (ON.activeSelf != shouldShow)
            ON.SetActive(shouldShow);
    }

    /// <summary>按当前进度刷新两个输入框能不能改：宽度知道答案后锁死，长度量对后锁死。</summary>
    private void ApplyInteractableState()
    {
        DrawingSizeInput input = SizeInput;
        if (input == null)
            return;

        bool widthEditable = !hasToldWidth;
        if (widthEditable != widthEditableApplied)
        {
            input.SetWidthInteractable(widthEditable);
            widthEditableApplied = widthEditable;
        }

        bool lengthEditable = !lengthIsCorrect;
        if (lengthEditable != lengthEditableApplied)
        {
            input.SetLengthInteractable(lengthEditable);
            lengthEditableApplied = lengthEditable;
        }
    }

    private void OnLengthSubmitted(float value)
    {
        TryAcceptLength();
    }

    /// <summary>三段判定都在这里：宽度没被告知、或石头没扔过，长度输入什么都不作数。</summary>
    private void TryAcceptLength()
    {
        if (lengthIsCorrect || !LengthGateOpen)
            return;

        DrawingSizeInput input = SizeInput;
        if (input == null)
            return;

        if (Mathf.Abs(input.Length - correctLength) > 0.001f)
            return;

        AcceptLength();
    }

    private void AcceptLength()
    {
        lengthIsCorrect = true;

        // 现在手里拿着的图纸推进到完整阶段
        Drawing drawing = FindHeldDrawing();
        if (drawing != null)
        {
            drawing.drawingStage = completeStage;

            // 图纸图标按 stage 换图，刷新一下背包显示
            PlayerInventoryManager inventory = PlayerSelector.Instance != null ? PlayerSelector.Instance.currentInventory : null;
            if (inventory != null)
                inventory.NotifyChanged();
        }

        if (!string.IsNullOrEmpty(lengthCorrectMessage))
            ShowPopUp(lengthCorrectMessage);

        ApplyInteractableState();

        // 长度量对了就收起图纸面板（下一帧执行 Exit，避免在输入框回调里关掉自己）
        exitRequested = true;
    }

    // ---- 扔石头量长度 ----

    /// <summary>
    /// 玩家扔石头量长度：消耗背包里的石头和绳子，标记「已经扔过」，并把 UI 上的图换成扔过之后的。
    /// 石头/绳子不齐、或图纸还没更新到 requiredStage，都不消耗、返回 false。
    /// </summary>
    public bool ThrowStone()
    {
        if (hasThrewStone)
            return false;

        // 图纸还没更新到「半张」就不给扔（先去找工程师问尺寸）
        Drawing drawing = HeldDrawing;
        if (drawing == null || drawing.drawingStage < requiredStage)
            return false;

        if (!HasItem("Stone") || !HasItem("Rope"))
        {
            ShowPopUp(needStoneAndRopeMessage);
            return false;
        }

        // 先消耗再置标记，避免中途被打断导致「没石头也算扔过」
        ConsumeItems("Stone", "Rope");
        hasThrewStone = true;

        RefreshDrawingImage();

        return true;
    }

    /// <summary>给 UI 按钮的 onClick 直接挂这个（ThrowStone 有返回值，挂不到按钮上）。</summary>
    public void OnThrowStoneButton()
    {
        PlayerUIManager.Instance.PlayClickSound();
        ThrowStone();
    }

    /// <summary>从当前背包里移除指定名字的物品（按 ItemName 匹配，可一次传多个）。返回实际移除的数量。</summary>
    public int ConsumeItems(params string[] itemNames)
    {
        PlayerInventoryManager inventory = PlayerSelector.Instance != null ? PlayerSelector.Instance.currentInventory : null;
        if (inventory == null || inventory.inventory == null || itemNames == null || itemNames.Length == 0)
            return 0;

        // 先收集、再删除：边 foreach 边 RemoveItem 会让正在遍历的 List 被改动，
        // 直接抛 InvalidOperationException: Collection was modified
        List<InventoryItemData> targets = new List<InventoryItemData>();
        foreach (InventoryItemData item in inventory.inventory)
        {
            if (item == null)
                continue;

            for (int i = 0; i < itemNames.Length; i++)
            {
                if (!string.Equals(item.ItemName, itemNames[i]))
                    continue;

                targets.Add(item);
                break;
            }
        }

        foreach (InventoryItemData item in targets)
            inventory.RemoveItem(item);   // 数量归零时才会真正从背包移除

        return targets.Count;
    }

    /// <summary>背包里有没有这件东西（按 ItemName 匹配）。</summary>
    public bool HasItem(string itemName)
    {
        if (string.IsNullOrEmpty(itemName))
            return false;

        PlayerInventoryManager inventory = PlayerSelector.Instance != null ? PlayerSelector.Instance.currentInventory : null;
        if (inventory == null || inventory.inventory == null)
            return false;

        foreach (InventoryItemData item in inventory.inventory)
        {
            if (item != null && string.Equals(item.ItemName, itemName))
                return true;
        }

        return false;
    }

    private static void ShowPopUp(string message)
    {
        if (string.IsNullOrEmpty(message))
            return;

        if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.popUpManager != null)
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow(message);
    }

    /// <summary>玩家当前持有的图纸（优先当前选中的那件，否则背包里第一张），没有则为 null。</summary>
    public Drawing HeldDrawing => FindHeldDrawing();

    /// <summary>找出玩家当前持有的图纸：优先当前选中的那件，否则在背包里找第一张图纸。</summary>
    private Drawing FindHeldDrawing()
    {
        PlayerInventoryManager inventory = PlayerSelector.Instance != null ? PlayerSelector.Instance.currentInventory : null;
        if (inventory == null)
            return null;

        if (inventory.currentSelectedItem is Drawing selected)
            return selected;

        if (inventory.inventory != null)
        {
            foreach (InventoryItemData item in inventory.inventory)
            {
                if (item is Drawing drawing)
                    return drawing;
            }
        }

        return null;
    }

    /// <summary>程序输入：直接写入「长」「宽」，玩家在输入框里看到的内容会同步刷新。</summary>
    public void SetSize(float length, float width)
    {
        if (SizeInput != null)
            SizeInput.SetSize(length, width);
    }

    /// <summary>程序输入：直接写入「长」（不会触发长度判定，判定只认玩家提交）。</summary>
    public void SetLength(float length)
    {
        if (SizeInput != null)
            SizeInput.SetLength(length);
    }

    /// <summary>程序输入：直接写入「宽」，并视为「已经知道宽度」（宽度随即锁死）。</summary>
    public void SetWidth(float width)
    {
        if (SizeInput != null)
        {
            SizeInput.SetWidth(width);
            hasToldWidth = true;
        }
    }

    public void ShowDrawingUI()
    {
        PlayerUIManager.Instance.PlayClickSound();

        if (drawingUIPanel != null)
        {
            drawingUIPanel.SetActive(true);
            HideInventory();
        }
    }

    public void Exit()
    {
        PlayerUIManager.Instance.PlayClickSound();

        ShowInventory();
        if (drawingUIPanel != null)
            drawingUIPanel.SetActive(false);
    }

    private void HideInventory()
    {
        if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.inventoryManager != null)
            PlayerUIManager.Instance.inventoryManager.HideInventoryWindow();
    }

    private void ShowInventory()
    {
        if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.inventoryManager != null)
            PlayerUIManager.Instance.inventoryManager.ShowInventoryWindow();
    }
}
