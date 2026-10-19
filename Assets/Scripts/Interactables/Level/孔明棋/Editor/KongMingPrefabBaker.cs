using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Writes the generated KongMing UI into the minigame prefab so its layout can be edited in Prefab Mode.
/// </summary>
public static class KongMingPrefabBaker
{
    private const string PrefabPath = "Assets/Prefabs/Interactables/Env/Level04/KongMing/KongMingMinigame.prefab";
    private const string SpriteFolder = "Assets/Art/UI/KongMingPrefab";

    private static readonly Dictionary<Sprite, Sprite> persistedSprites = new Dictionary<Sprite, Sprite>();

    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        EditorApplication.delayCall += AutoBakeIfMissing;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += AutoBakeIfMissing;
        };
    }

    [MenuItem("Tools/KongMing/Bake UI Into Prefab")]
    public static void BakeFromMenu()
    {
        Bake();
    }

    private static void AutoBakeIfMissing()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            return;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
            return;

        Transform canvas = prefab.transform.Find("Canvas");
        if (canvas == null || canvas.Find("BoardArea") != null)
            return;

        Bake();
    }

    private static void Bake()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[KongMingPrefabBaker] Exit Play mode before baking the prefab.");
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Transform canvas = root.transform.Find("Canvas");
            if (canvas == null)
                throw new MissingReferenceException("KongMingMinigame prefab has no Canvas child.");

            for (int i = canvas.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(canvas.GetChild(i).gameObject);

            KongMingMinigameUI ui = canvas.GetComponent<KongMingMinigameUI>();
            if (ui == null)
                ui = canvas.gameObject.AddComponent<KongMingMinigameUI>();

            ui.BuildEditableLayout();
            PersistSprites(canvas);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log("[KongMingPrefabBaker] Wrote editable minigame UI under KongMingMinigame/Canvas.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void PersistSprites(Transform root)
    {
        EnsureFolder(SpriteFolder);
        persistedSprites.Clear();

        foreach (Image image in root.GetComponentsInChildren<Image>(true))
            image.sprite = Persist(image.sprite);

        foreach (Button button in root.GetComponentsInChildren<Button>(true))
        {
            SpriteState state = button.spriteState;
            state.highlightedSprite = Persist(state.highlightedSprite);
            state.pressedSprite = Persist(state.pressedSprite);
            state.selectedSprite = Persist(state.selectedSprite);
            state.disabledSprite = Persist(state.disabledSprite);
            button.spriteState = state;
        }
    }

    private static Sprite Persist(Sprite source)
    {
        if (source == null || EditorUtility.IsPersistent(source))
            return source;

        if (persistedSprites.TryGetValue(source, out Sprite existing))
            return existing;

        string safeName = Regex.Replace(source.name, "[^A-Za-z0-9_-]", "_");
        string assetPath = SpriteFolder + "/" + safeName + ".png";
        File.WriteAllBytes(assetPath, source.texture.EncodeToPNG());
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = source.pixelsPerUnit;
            importer.spriteBorder = source.border;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        Sprite persisted = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        persistedSprites[source] = persisted;
        return persisted;
    }

    private static void EnsureFolder(string folder)
    {
        string[] parts = folder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
