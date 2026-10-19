using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Shared look-and-feel helpers for the level-04 minigames.
/// The UI is assembled at runtime so the prefabs only need a Canvas and this script.
/// </summary>
public static class MinigameTheme
{
    public static readonly Color Ink = new Color(0.10f, 0.07f, 0.05f, 1f);
    public static readonly Color DeepBrown = new Color(0.17f, 0.11f, 0.075f, 1f);
    public static readonly Color PanelBrown = new Color(0.25f, 0.17f, 0.12f, 1f);
    public static readonly Color Cream = new Color(0.97f, 0.90f, 0.74f, 1f);
    public static readonly Color MutedCream = new Color(0.78f, 0.70f, 0.55f, 1f);
    public static readonly Color Brass = new Color(0.95f, 0.72f, 0.30f, 1f);
    public static readonly Color BrassDark = new Color(0.62f, 0.42f, 0.16f, 1f);
    public static readonly Color Steel = new Color(0.67f, 0.75f, 0.80f, 1f);
    public static readonly Color Green = new Color(0.50f, 0.82f, 0.42f, 1f);
    public static readonly Color Red = new Color(0.90f, 0.36f, 0.27f, 1f);

    private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
    private static readonly Dictionary<string, Sprite> generatedCache = new Dictionary<string, Sprite>();

    public static TMP_FontAsset ResolveFont(Transform root)
    {
        TMP_Text existing = root.GetComponentInChildren<TMP_Text>(true);
        if (existing != null && existing.font != null)
            return existing.font;

        return TMP_Settings.defaultFontAsset;
    }

    public static void ConfigureCanvas(Canvas canvas, CanvasScaler scaler)
    {
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 200;
            canvas.pixelPerfect = false;
            canvas.transform.localScale = Vector3.one;
        }

        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;

            // Drive the factor ourselves so it is correct on the first frame, before
            // CanvasScaler's pre-render pass has run.
            if (Application.isPlaying)
            {
                scaler.enabled = false;
                float screenScale = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
                canvas.scaleFactor = Mathf.Max(0.1f, screenScale);
            }
        }
    }

    public static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        GameObject eventSystem = new GameObject("Minigame EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<InputSystemUIInputModule>();
    }

    public static void SafeDestroy(GameObject target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Object.Destroy(target);
        else
            Object.DestroyImmediate(target);
    }

    public static void ClearChildren(Transform parent)
    {
        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
            SafeDestroy(parent.GetChild(i).gameObject);
    }

    public static RectTransform CreateLayer(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        return rect;
    }

    public static Image CreateImage(Transform parent, string name, Sprite sprite, Color color,
        Vector2 size, Vector2 position, bool stretch = false, bool raycast = false)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)go.transform;
        if (stretch)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
        else
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = raycast;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        return image;
    }

    public static Image CreateSlicedImage(Transform parent, string name, Sprite sprite, Color color,
        Vector2 size, Vector2 position, Vector4 border, bool raycast = false)
    {
        Image image = CreateImage(parent, name, Sliced(sprite, border), color, size, position, false, raycast);
        image.type = Image.Type.Sliced;
        return image;
    }

    public static Image CreateStretchedImage(Transform parent, string name, Sprite sprite, Color color,
        bool raycast = false)
    {
        Image image = CreateImage(parent, name, sprite, color, Vector2.zero, Vector2.zero, true, raycast);
        return image;
    }

    public static TextMeshProUGUI CreateText(Transform parent, string name, string content, float fontSize,
        Color color, Vector2 size, Vector2 position, TMP_FontAsset font,
        TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.text = content;
        text.raycastTarget = false;
        text.richText = true;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        text.enableAutoSizing = false;
        text.margin = Vector4.zero;

        Shadow shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        shadow.effectDistance = new Vector2(2f, -2f);

        return text;
    }

    public static Button CreateButton(Transform parent, string name, string label, Vector2 size, Vector2 position,
        TMP_FontAsset font, UnityEngine.Events.UnityAction onClick, float fontSize = 28f)
    {
        Color fill = new Color(0.22f, 0.30f, 0.34f, 1f);
        Color border = new Color(0.54f, 0.63f, 0.66f, 1f);
        Sprite normal = RoundedRect(64, 12, fill, border, 2);

        Image background = CreateImage(parent, name, normal, Color.white, size, position, false, true);
        background.type = Image.Type.Sliced;

        Button button = background.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.SpriteSwap;
        button.navigation = new Navigation { mode = Navigation.Mode.None };

        SpriteState state = new SpriteState
        {
            highlightedSprite = RoundedRect(64, 12, new Color(0.28f, 0.38f, 0.42f, 1f), border, 2),
            pressedSprite = RoundedRect(64, 12, new Color(0.17f, 0.24f, 0.28f, 1f), border, 2),
            selectedSprite = RoundedRect(64, 12, new Color(0.28f, 0.38f, 0.42f, 1f), border, 2),
            disabledSprite = normal
        };
        button.spriteState = state;

        TextMeshProUGUI text = CreateText(background.transform, "Label", label, fontSize,
            new Color(0.92f, 0.96f, 0.97f, 1f), size - new Vector2(10f, 8f), Vector2.zero, font);
        text.raycastTarget = false;

        if (onClick != null)
            button.onClick.AddListener(onClick);

        return button;
    }

    /// <summary>A rounded rectangle with a solid fill and an inset border.</summary>
    public static Sprite RoundedRect(int size, int radius, Color fill, Color border, int borderWidth)
    {
        return BuildRounded(size, radius, fill, border, borderWidth, false);
    }

    /// <summary>A rounded rectangle outline with a transparent center.</summary>
    public static Sprite RoundedBorder(int size, int radius, Color border, int borderWidth)
    {
        return BuildRounded(size, radius, Color.clear, border, borderWidth, true);
    }

    private static Sprite BuildRounded(int size, int radius, Color fill, Color border, int borderWidth,
        bool borderOnly)
    {
        string key = $"rrect_{size}_{radius}_{ColorUtility.ToHtmlStringRGBA(fill)}_" +
                     $"{ColorUtility.ToHtmlStringRGBA(border)}_{borderWidth}_{borderOnly}";
        if (generatedCache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = key;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        float half = size * 0.5f - 1f;
        float corner = Mathf.Clamp(radius, 1f, half);
        float edge = Mathf.Max(1f, borderWidth);
        Color clear = new Color(0f, 0f, 0f, 0f);
        float center = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 point = new Vector2(x + 0.5f - center, y + 0.5f - center);
                Vector2 q = new Vector2(Mathf.Abs(point.x), Mathf.Abs(point.y)) -
                            new Vector2(half - corner, half - corner);
                float distance = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude +
                                 Mathf.Min(Mathf.Max(q.x, q.y), 0f) - corner;

                Color pixel;
                if (distance > 0f)
                    pixel = clear;
                else if (distance > -edge)
                    pixel = border;
                else
                    pixel = borderOnly ? clear : fill;

                texture.SetPixel(x, y, pixel);
            }
        }

        texture.Apply();
        float borderSize = Mathf.Min(size * 0.5f - 1f, corner + 2f);
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f,
            0, SpriteMeshType.FullRect,
            new Vector4(borderSize, borderSize, borderSize, borderSize));
        sprite.name = key;
        generatedCache[key] = sprite;
        return sprite;
    }

    public static Image CreateBar(Transform parent, string name, Vector2 from, Vector2 to, float thickness,
        Color color)
    {
        Vector2 delta = to - from;
        Image bar = CreateSlicedImage(parent, name, Panel(), color,
            new Vector2(delta.magnitude, thickness), (from + to) * 0.5f, new Vector4(4f, 4f, 4f, 4f));
        bar.rectTransform.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        return bar;
    }

    public static Sprite Panel()
    {
        return Load("panel");
    }

    public static Sprite PanelInset()
    {
        return Load("panel_inset");
    }

    public static Sprite Window()
    {
        return Load("window");
    }

    public static Sprite ButtonNormal()
    {
        return Load("button_normal");
    }

    public static Sprite ButtonHover()
    {
        return Load("button_hover");
    }

    public static Sprite ButtonPressed()
    {
        return Load("button_pressed");
    }

    public static Sprite RopeCoil()
    {
        return Load("rope_coil");
    }

    public static Sprite Load(string name)
    {
        if (spriteCache.TryGetValue(name, out Sprite cached) && cached != null)
            return cached;

        Sprite sprite = Resources.Load<Sprite>("Minigame/" + name);
        if (sprite == null)
        {
            Debug.LogWarning($"[MinigameTheme] Missing sprite Resources/Minigame/{name}; using fallback.");
            sprite = FallbackSprite();
        }

        spriteCache[name] = sprite;
        return sprite;
    }

    public static Sprite Sliced(Sprite source, Vector4 border)
    {
        if (source == null)
            return FallbackSprite();

        string key = source.GetInstanceID() + "_" + border.x + "_" + border.y + "_" + border.z + "_" + border.w;
        if (generatedCache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        Sprite sliced = Sprite.Create(source.texture, source.rect, new Vector2(0.5f, 0.5f),
            source.pixelsPerUnit, 0, SpriteMeshType.FullRect, border);
        sliced.name = source.name + "_sliced";
        generatedCache[key] = sliced;
        return sliced;
    }

    public static Sprite Circle(int size, Color fill, Color rim, int rimWidth = 3)
    {
        string key = $"circle_{size}_{ColorUtility.ToHtmlStringRGBA(fill)}_{ColorUtility.ToHtmlStringRGBA(rim)}_{rimWidth}";
        if (generatedCache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = key;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        float center = (size - 1) * 0.5f;
        float radius = center - 1f;
        Color clear = new Color(0f, 0f, 0f, 0f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                Color pixel;
                if (distance > radius)
                    pixel = clear;
                else if (distance > radius - rimWidth)
                    pixel = rim;
                else
                    pixel = fill;
                texture.SetPixel(x, y, pixel);
            }
        }

        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f,
            0, SpriteMeshType.FullRect);
        sprite.name = key;
        generatedCache[key] = sprite;
        return sprite;
    }

    public static Sprite Ring(int size, Color color)
    {
        string key = $"ring_{size}_{ColorUtility.ToHtmlStringRGBA(color)}";
        if (generatedCache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = key;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        float center = (size - 1) * 0.5f;
        float outer = center - 1f;
        float inner = outer - Mathf.Max(3f, size * 0.06f);
        Color clear = new Color(0f, 0f, 0f, 0f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                texture.SetPixel(x, y, distance <= outer && distance >= inner ? color : clear);
            }
        }

        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f,
            0, SpriteMeshType.FullRect);
        sprite.name = key;
        generatedCache[key] = sprite;
        return sprite;
    }

    /// <summary>A small, readable rope knot made from two interlocking rings and a strand.</summary>
    public static Sprite Knot(int size)
    {
        string key = $"knot_{size}";
        if (generatedCache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = key;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color clear = new Color(0f, 0f, 0f, 0f);
        Color rope = new Color(0.78f, 0.62f, 0.36f, 1f);
        Color ropeLight = new Color(0.92f, 0.78f, 0.50f, 1f);
        Color ropeDark = new Color(0.27f, 0.18f, 0.10f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
                texture.SetPixel(x, y, clear);
        }

        float ringOuter = size * 0.34f;
        float ringWidth = size * 0.10f;
        Vector2 left = new Vector2(size * 0.38f, size * 0.50f);
        Vector2 right = new Vector2(size * 0.62f, size * 0.50f);

        DrawRing(texture, left, ringOuter, ringWidth, ropeDark);
        DrawRing(texture, right, ringOuter, ringWidth, ropeDark);
        DrawRing(texture, left, ringOuter - 2f, ringWidth - 2f, rope);
        DrawRing(texture, right, ringOuter - 2f, ringWidth - 2f, ropeLight);

        // Two loose ends make the icon read as a knot rather than glasses.
        for (int i = 0; i < size; i++)
        {
            float t = i / (float)size;
            int x = Mathf.RoundToInt(size * 0.20f + t * size * 0.10f);
            int y = Mathf.RoundToInt(size * 0.76f + Mathf.Sin(t * Mathf.PI) * size * 0.08f);
            DrawDisc(texture, x, y, 2, ropeDark);
            DrawDisc(texture, size - x, y, 2, ropeDark);
        }

        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f,
            0, SpriteMeshType.FullRect);
        sprite.name = key;
        generatedCache[key] = sprite;
        return sprite;
    }

    /// <summary>An open rope loop with two loose ends, used after the knot is untied.</summary>
    public static Sprite LooseRope(int size)
    {
        string key = $"loose_rope_{size}";
        if (generatedCache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = key;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color clear = new Color(0f, 0f, 0f, 0f);
        Color rope = new Color(0.78f, 0.62f, 0.36f, 1f);
        Color ropeLight = new Color(0.92f, 0.78f, 0.50f, 1f);
        Color ropeDark = new Color(0.27f, 0.18f, 0.10f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
                texture.SetPixel(x, y, clear);
        }

        float center = size * 0.5f;
        float outer = size * 0.33f;
        float width = size * 0.10f;
        float gapDegrees = 34f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 point = new Vector2(x + 0.5f - center, y + 0.5f - center);
                float distance = point.magnitude;
                if (distance > outer || distance < outer - width)
                    continue;

                float angle = Mathf.Atan2(point.y, point.x) * Mathf.Rad2Deg;
                if (Mathf.Abs(Mathf.DeltaAngle(angle, 90f)) < gapDegrees)
                    continue;

                float edge = Mathf.Min(distance - (outer - width), outer - distance);
                Color color = edge < 2f ? ropeDark : (point.y > 0f ? ropeLight : rope);
                texture.SetPixel(x, y, color);
            }
        }

        // Two loose ends at the opening.
        for (int side = -1; side <= 1; side += 2)
        {
            float startAngle = (90f + side * gapDegrees) * Mathf.Deg2Rad;
            Vector2 start = new Vector2(Mathf.Cos(startAngle), Mathf.Sin(startAngle)) * (outer - width * 0.5f) +
                            new Vector2(center, center);
            float endAngle = (90f + side * (gapDegrees + 34f)) * Mathf.Deg2Rad;
            Vector2 end = new Vector2(Mathf.Cos(endAngle), Mathf.Sin(endAngle)) * (outer + width * 1.15f) +
                          new Vector2(center, center);

            DrawLine(texture, start, end, width * 0.55f + 2f, ropeDark);
            DrawLine(texture, start, end, width * 0.55f, rope);
        }

        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f,
            0, SpriteMeshType.FullRect);
        sprite.name = key;
        generatedCache[key] = sprite;
        return sprite;
    }

    private static void DrawLine(Texture2D texture, Vector2 from, Vector2 to, float thickness, Color color)
    {
        int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(from.x, to.x) - thickness));
        int maxX = Mathf.Min(texture.width - 1, Mathf.CeilToInt(Mathf.Max(from.x, to.x) + thickness));
        int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(from.y, to.y) - thickness));
        int maxY = Mathf.Min(texture.height - 1, Mathf.CeilToInt(Mathf.Max(from.y, to.y) + thickness));

        Vector2 segment = to - from;
        float lengthSquared = Mathf.Max(0.0001f, segment.sqrMagnitude);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                Vector2 point = new Vector2(x + 0.5f, y + 0.5f);
                float t = Mathf.Clamp01(Vector2.Dot(point - from, segment) / lengthSquared);
                Vector2 closest = from + segment * t;
                if (Vector2.Distance(point, closest) <= thickness)
                    texture.SetPixel(x, y, color);
            }
        }
    }

    private static void DrawRing(Texture2D texture, Vector2 center, float radius, float width, Color color)
    {
        float inner = radius - width;
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance <= radius && distance >= inner)
                    texture.SetPixel(x, y, color);
            }
        }
    }

    private static void DrawDisc(Texture2D texture, int cx, int cy, int radius, Color color)
    {
        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                if (x * x + y * y > radius * radius)
                    continue;

                int px = cx + x;
                int py = cy + y;
                if (px >= 0 && py >= 0 && px < texture.width && py < texture.height)
                    texture.SetPixel(px, py, color);
            }
        }
    }

    private static Sprite fallback;

    private static Sprite FallbackSprite()
    {
        if (fallback != null)
            return fallback;

        Texture2D texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[16];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;
        texture.SetPixels(pixels);
        texture.Apply();
        fallback = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 100f);
        fallback.name = "minigame_fallback";
        return fallback;
    }
}
