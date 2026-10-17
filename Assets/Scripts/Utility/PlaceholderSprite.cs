using UnityEngine;

/// <summary>
/// 占位贴图工具：美术资源未到位前，用纯色矩形/圆形 Sprite 顶替，
/// 方便先把玩法跑通。美术就位后在 SpriteRenderer 上直接替换 sprite 即可。
/// </summary>
public static class PlaceholderSprite
{
    /// <summary>生成纯色矩形占位 Sprite。pivot 在底部中点，便于贴地摆放。</summary>
    public static Sprite CreateRect(Color color, int width = 16, int height = 16, bool pivotBottom = true)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply();
        Vector2 pivot = pivotBottom ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 0.5f);
        return Sprite.Create(tex, new Rect(0f, 0f, width, height), pivot, 16f);
    }

    /// <summary>生成圆形占位 Sprite（NPC/角色顶替用）。</summary>
    public static Sprite CreateCircle(Color color, int radius = 12)
    {
        int size = radius * 2;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - radius + 0.5f;
                float dy = y - radius + 0.5f;
                bool inside = dx * dx + dy * dy <= radius * radius;
                tex.SetPixel(x, y, inside ? color : Color.clear);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 16f);
    }

    /// <summary>给物体确保一个 SpriteRenderer 并套上占位贴图，返回该 Renderer。</summary>
    public static SpriteRenderer EnsureRenderer(GameObject go, Sprite sprite, Color tint, int sortingOrder = 0, string sortingLayer = null)
    {
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = go.AddComponent<SpriteRenderer>();
        if (sprite != null)
            sr.sprite = sprite;
        sr.color = tint;
        sr.sortingOrder = sortingOrder;
        if (!string.IsNullOrEmpty(sortingLayer))
            sr.sortingLayerName = sortingLayer;
        return sr;
    }
}
