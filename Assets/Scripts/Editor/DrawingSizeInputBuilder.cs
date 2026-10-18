using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 往 PlayerUI.prefab 的「Drawing UI / Drawing」下面生成「长 / 宽」两行输入框：
///
///   Drawing UI / Drawing
///     └ Size Row - Length   ← 整行拖动这个就能挪位置
///         ├ Label           「长」
///         └ Input           TMP_InputField
///     └ Size Row - Width
///         ├ Label           「宽」
///         └ Input           TMP_InputField
///
/// 同时把 DrawingSizeInput 挂到 Drawing 上、把两个输入框和 DrawingInUI 的引用接好。
/// 已存在时不会重建（不会覆盖手动调好的位置、字号、颜色）。
///
/// 菜单：Tools/UI/图纸：生成长宽输入框
/// 命令行：Unity.exe -executeMethod DrawingSizeInputBuilder.BuildFromCommandLine
/// </summary>
public static class DrawingSizeInputBuilder
{
    private const string PrefabPath = "Assets/Prefabs/UI/PlayerUI.prefab";
    private const string DrawingPath = "Drawing UI/Drawing";
    private const string LengthRowName = "Size Row - Length";
    private const string WidthRowName = "Size Row - Width";
    private const string FallbackFontPath = "Assets/Fonts/NotoSansCJKtc-Regular(1) SDF.asset";

    private const float RowWidth = 460f;
    private const float RowHeight = 80f;
    private const float RowSpacing = 85f;
    private const float LabelWidth = 90f;
    private const float FontSize = 40f;

    static readonly Color InkColor = new Color(0.33f, 0.24f, 0.15f, 1f);
    static readonly Color FieldColor = new Color(1f, 1f, 1f, 0.45f);

    [MenuItem("Tools/UI/图纸：生成长宽输入框")]
    public static void BuildFromMenu()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("[DrawingSizeInput] 请先退出播放模式再生成。");
            return;
        }

        if (Build(true))
            Debug.Log("[DrawingSizeInput] 长宽输入框已就位（Drawing 下面）。");
    }

    /// <summary>批处理入口：Unity.exe -executeMethod DrawingSizeInputBuilder.BuildFromCommandLine</summary>
    public static void BuildFromCommandLine()
    {
        bool ok = Build(true);
        Debug.Log("[DrawingSizeInput] BuildFromCommandLine => " + ok);

        if (Application.isBatchMode)
            EditorApplication.Exit(ok ? 0 : 1);
    }

    /// <summary>打开工程时自动补一次（只在缺输入框时动手，成功后不再打扰）。</summary>
    [InitializeOnLoadMethod]
    private static void AutoBuildOnce()
    {
        if (Application.isBatchMode)
            return;

        string key = "DrawingSizeInput.AutoBuilt." + Application.dataPath.GetHashCode();
        if (EditorPrefs.GetBool(key, false))
            return;

        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (Build(false))
                EditorPrefs.SetBool(key, true);
        };
    }

    private static bool Build(bool verbose)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (root == null)
        {
            Debug.LogError("[DrawingSizeInput] 打不开 prefab：" + PrefabPath);
            return false;
        }

        bool changed = false;
        try
        {
            Transform drawing = root.transform.Find(DrawingPath);
            if (drawing == null)
            {
                Debug.LogError("[DrawingSizeInput] prefab 里找不到节点：" + DrawingPath);
                return false;
            }

            TMP_InputField lengthInput = FindInputField(drawing, LengthRowName);
            TMP_InputField widthInput = FindInputField(drawing, WidthRowName);

            TMP_FontAsset font = FindFont(root.transform);

            if (lengthInput == null)
            {
                lengthInput = CreateRow(drawing, LengthRowName, RowSpacing * 0.5f, "长", font);
                changed = true;
            }

            if (widthInput == null)
            {
                widthInput = CreateRow(drawing, WidthRowName, -RowSpacing * 0.5f, "宽", font);
                changed = true;
            }

            DrawingSizeInput sizeInput = drawing.GetComponent<DrawingSizeInput>();
            if (sizeInput == null)
            {
                sizeInput = drawing.gameObject.AddComponent<DrawingSizeInput>();
                changed = true;
            }

            // 已经生成过时，也顺手把字体纠正回项目用的中文字体（不会覆盖手调的位置、字号、颜色）
            changed |= EnsureRowFont(drawing, font);

            changed |= SetObjectReference(sizeInput, "lengthInput", lengthInput);
            changed |= SetObjectReference(sizeInput, "widthInput", widthInput);

            DrawingInUI drawingInUI = root.GetComponentInChildren<DrawingInUI>(true);
            if (drawingInUI != null)
                changed |= SetObjectReference(drawingInUI, "sizeInput", sizeInput);

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                if (verbose)
                    Debug.Log("[DrawingSizeInput] 已保存：" + PrefabPath);
            }
            else if (verbose)
            {
                Debug.Log("[DrawingSizeInput] 输入框已经存在，跳过（保留你手动调好的样子）。");
            }

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError("[DrawingSizeInput] 生成失败：" + e);
            return false;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static TMP_InputField FindInputField(Transform drawing, string rowName)
    {
        Transform row = drawing.Find(rowName);
        if (row == null)
            return null;

        Transform input = row.Find("Input");
        if (input != null)
        {
            TMP_InputField field = input.GetComponent<TMP_InputField>();
            if (field != null)
                return field;
        }

        return row.GetComponentInChildren<TMP_InputField>(true);
    }

    private static TMP_InputField CreateRow(Transform parent, string rowName, float anchoredY, string labelText,
                                            TMP_FontAsset font)
    {
        int layer = parent.gameObject.layer;

        // 行容器：整行拖动这个就能挪位置，标签和输入框跟着走
        GameObject rowGO = CreateUIObject(rowName, parent, layer);
        RectTransform row = (RectTransform)rowGO.transform;
        row.anchorMin = new Vector2(0.5f, 0.5f);
        row.anchorMax = new Vector2(0.5f, 0.5f);
        row.pivot = new Vector2(0.5f, 0.5f);
        row.sizeDelta = new Vector2(RowWidth, RowHeight);
        row.anchoredPosition = new Vector2(0f, anchoredY);

        // 「长 / 宽」标签
        GameObject labelGO = CreateUIObject("Label", row, layer);
        RectTransform labelRT = (RectTransform)labelGO.transform;
        labelRT.anchorMin = new Vector2(0f, 0f);
        labelRT.anchorMax = new Vector2(0f, 1f);
        labelRT.pivot = new Vector2(0f, 0.5f);
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = new Vector2(LabelWidth, 0f);

        TextMeshProUGUI label = labelGO.AddComponent<TextMeshProUGUI>();
        label.text = labelText;
        label.fontSize = FontSize;
        label.color = InkColor;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        if (font != null)
            label.font = font;

        // 输入框本体（背景可点 + TMP_InputField）
        GameObject inputGO = CreateUIObject("Input", row, layer);
        RectTransform inputRT = (RectTransform)inputGO.transform;
        inputRT.anchorMin = new Vector2(0f, 0f);
        inputRT.anchorMax = new Vector2(1f, 1f);
        inputRT.offsetMin = new Vector2(LabelWidth + 10f, 2f);
        inputRT.offsetMax = new Vector2(-2f, -2f);

        Image background = inputGO.AddComponent<Image>();
        background.color = FieldColor;
        background.raycastTarget = true;

        TMP_InputField input = inputGO.AddComponent<TMP_InputField>();
        input.transition = Selectable.Transition.None;

        // 文本可视区域（负责裁剪）
        GameObject areaGO = CreateUIObject("Text Area", inputRT, layer);
        RectTransform areaRT = (RectTransform)areaGO.transform;
        areaRT.anchorMin = Vector2.zero;
        areaRT.anchorMax = Vector2.one;
        areaRT.offsetMin = new Vector2(10f, 6f);
        areaRT.offsetMax = new Vector2(-10f, -6f);
        areaGO.AddComponent<RectMask2D>();

        // 显示在输入框里的文本
        GameObject textGO = CreateUIObject("Text", areaRT, layer);
        ((RectTransform)textGO.transform).anchorMin = Vector2.zero;
        ((RectTransform)textGO.transform).anchorMax = Vector2.one;
        ((RectTransform)textGO.transform).offsetMin = Vector2.zero;
        ((RectTransform)textGO.transform).offsetMax = Vector2.zero;

        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = string.Empty;
        text.fontSize = FontSize;
        text.color = InkColor;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.extraPadding = true;
        text.raycastTarget = false;
        if (font != null)
            text.font = font;

        input.textViewport = areaRT;
        input.textComponent = text;
        input.fontAsset = text.font;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.contentType = TMP_InputField.ContentType.DecimalNumber;
        input.characterLimit = 6;
        input.caretBlinkRate = 0.85f;

        // 初始文字直接写序列化字段，避免在编辑模式下走运行时的 UpdateLabel 流程
        SerializedObject serializedInput = new SerializedObject(input);
        SerializedProperty textProperty = serializedInput.FindProperty("m_Text");
        if (textProperty != null)
        {
            textProperty.stringValue = "1";
            serializedInput.ApplyModifiedPropertiesWithoutUndo();
        }

        return input;
    }

    private static GameObject CreateUIObject(string name, Transform parent, int layer)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = layer;
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.localScale = Vector3.one;
        return go;
    }

    private static TMP_FontAsset FindFont(Transform root)
    {
        // 工程里的中文都用 Noto，优先用它，保证「长 / 宽」和玩家输入的字符能显示出来
        TMP_FontAsset noto = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackFontPath);
        if (noto != null)
            return noto;

        // 退化：用 UI 里已经在用的字体
        foreach (TextMeshProUGUI text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (text.font != null)
                return text.font;
        }

        Debug.LogWarning("[DrawingSizeInput] 没找到中文字体：" + FallbackFontPath + "，输入框会使用 TMP 默认字体（可能显示不出汉字）。");
        return null;
    }

    /// <summary>把两行输入框的字体统一成目标字体（已生成过时用于纠错，不改位置/字号/颜色）。</summary>
    private static bool EnsureRowFont(Transform drawing, TMP_FontAsset font)
    {
        if (font == null)
            return false;

        bool changed = false;

        foreach (string rowName in new[] { LengthRowName, WidthRowName })
        {
            Transform row = drawing.Find(rowName);
            if (row == null)
                continue;

            foreach (TextMeshProUGUI text in row.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (text.font == font && text.fontSharedMaterial == font.material)
                    continue;

                text.font = font;
                text.fontSharedMaterial = font.material;
                changed = true;
            }

            TMP_InputField input = FindInputField(drawing, rowName);
            if (input != null && input.fontAsset != font)
            {
                input.fontAsset = font;
                changed = true;
            }
        }

        return changed;
    }

    /// <summary>把组件的私有序列化字段指向目标对象，返回是否真的有改动。</summary>
    private static bool SetObjectReference(Object target, string fieldName, Object value)
    {
        if (target == null)
            return false;

        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(fieldName);
        if (property == null)
        {
            Debug.LogWarning("[DrawingSizeInput] " + target.GetType().Name + " 上找不到字段 " + fieldName);
            return false;
        }

        if (property.objectReferenceValue == value)
            return false;

        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return true;
    }
}
