using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class Level01StartUI : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField, Min(0f)] private float stayClosedDuration = 0.6f;
    [SerializeField, Min(0.01f)] private float openDuration = 2.6f;
    [SerializeField] private AnimationCurve openCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Eyelids")]
    [SerializeField] private Color eyelidColor = Color.black;
    [SerializeField, Min(0f)] private float eyelidCurve = 56f;

    [Header("Behaviour")]
    [SerializeField] private bool disableWhenFinished = true;

    private const int TextureWidth = 256;
    private const int TextureHeight = 256;

    private Image coverImage;
    private RectTransform root;
    private RectTransform topLidRect;
    private RectTransform bottomLidRect;
    private Sprite topLidSprite;
    private Sprite bottomLidSprite;
    private Texture2D topLidTexture;
    private Texture2D bottomLidTexture;
    private Coroutine openRoutine;
    private bool hotbarHidden;

    private IEnumerator Start()
    {
        HideHotbar();
        yield return null;
        HideHotbar();

        BuildLids();
        openRoutine = StartCoroutine(OpenEyesRoutine());
    }

    private void BuildLids()
    {
        root = (RectTransform)transform;
        coverImage = GetComponent<Image>();
        coverImage.raycastTarget = false;
        coverImage.enabled = false;

        Canvas.ForceUpdateCanvases();

        float height = root.rect.height;
        if (height <= 0f)
            height = Screen.height;

        float halfHeight = height * 0.5f + 1f;
        float curve = Mathf.Clamp(eyelidCurve, 0f, halfHeight * 0.75f);
        float lidHeight = halfHeight + curve;

        topLidSprite = CreateEyelidSprite(true, curve / lidHeight);
        bottomLidSprite = CreateEyelidSprite(false, curve / lidHeight);

        topLidRect = CreateLid("TopEyelid", topLidSprite, true, lidHeight);
        bottomLidRect = CreateLid("BottomEyelid", bottomLidSprite, false, lidHeight);
    }

    private RectTransform CreateLid(string lidName, Sprite sprite, bool isTop, float height)
    {
        GameObject lidObject = new GameObject(lidName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        lidObject.transform.SetParent(root, false);

        RectTransform rect = (RectTransform)lidObject.transform;
        if (isTop)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
        }
        else
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
        }

        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0f, height);

        Image image = lidObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = eyelidColor;
        image.raycastTarget = false;
        image.preserveAspect = false;

        return rect;
    }

    private Sprite CreateEyelidSprite(bool isTop, float curveRatio)
    {
        Texture2D texture = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, false);
        texture.name = isTop ? "TopEyelidTexture" : "BottomEyelidTexture";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color32 opaque = new Color32(255, 255, 255, 255);
        Color32 clear = new Color32(255, 255, 255, 0);
        Color32[] pixels = new Color32[TextureWidth * TextureHeight];
        float overlap = 2f / TextureHeight;

        for (int y = 0; y < TextureHeight; y++)
        {
            float ny = (y + 0.5f) / TextureHeight;
            for (int x = 0; x < TextureWidth; x++)
            {
                float nx = ((x + 0.5f) / TextureWidth) * 2f - 1f;
                float curve = curveRatio * (1f - nx * nx);
                bool inside = isTop ? ny >= curve - overlap : ny <= 1f - curve + overlap;
                pixels[y * TextureWidth + x] = inside ? opaque : clear;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, false);

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, TextureWidth, TextureHeight),
            new Vector2(0.5f, isTop ? 1f : 0f),
            100f,
            0,
            SpriteMeshType.FullRect);
        sprite.name = isTop ? "TopEyelidSprite" : "BottomEyelidSprite";

        if (isTop)
            topLidTexture = texture;
        else
            bottomLidTexture = texture;

        return sprite;
    }

    private IEnumerator OpenEyesRoutine()
    {
        if (topLidRect == null || bottomLidRect == null)
            yield break;

        if (stayClosedDuration > 0f)
            yield return new WaitForSecondsRealtime(stayClosedDuration);

        float travel = topLidRect.sizeDelta.y;
        float elapsed = 0f;

        while (elapsed < openDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / openDuration);
            float eased = Mathf.Clamp01(openCurve != null ? openCurve.Evaluate(t) : t);
            float offset = eased * travel;

            topLidRect.anchoredPosition = new Vector2(0f, offset);
            bottomLidRect.anchoredPosition = new Vector2(0f, -offset);
            yield return null;
        }

        topLidRect.anchoredPosition = new Vector2(0f, travel);
        bottomLidRect.anchoredPosition = new Vector2(0f, -travel);
        openRoutine = null;

        ShowHotbar();

        if (disableWhenFinished)
            gameObject.SetActive(false);
    }

    private void HideHotbar()
    {
        if (PlayerUIManager.Instance == null || PlayerUIManager.Instance.inventoryManager == null)
            return;

        PlayerUIManager.Instance.inventoryManager.HideInventoryWindow();
        hotbarHidden = true;
    }

    private void ShowHotbar()
    {
        if (!hotbarHidden)
            return;

        hotbarHidden = false;

        if (PlayerUIManager.Instance == null || PlayerUIManager.Instance.inventoryManager == null)
            return;

        PlayerUIManager.Instance.inventoryManager.ShowInventoryWindow();
    }

    private void OnDisable()
    {
        ShowHotbar();
    }

    private void OnDestroy()
    {
        if (openRoutine != null)
            StopCoroutine(openRoutine);

        DestroyGeneratedSprite(topLidSprite, topLidTexture);
        DestroyGeneratedSprite(bottomLidSprite, bottomLidTexture);
    }

    private static void DestroyGeneratedSprite(Sprite sprite, Texture2D texture)
    {
        if (sprite != null)
            Destroy(sprite);

        if (texture != null)
            Destroy(texture);
    }
}
