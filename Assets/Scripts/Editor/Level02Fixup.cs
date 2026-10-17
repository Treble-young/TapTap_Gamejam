using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 第二关「荒地」修复工具（不重建整个 Level02_Expanded，只补缺失内容，保留用户手动摆放）：
///   1) 重建字牌拾取物（路/水/禁）——它们被误删导致「水和禁拾取不了」；
///   2) 给水和禁加阶段门禁：路牌修复前隐藏，修复后才出现；
/// 菜单：Tools/Level02/修复：重建字牌拾取物 + 分阶段门禁
/// </summary>
public static class Level02Fixup
{
    private const string ScenePath = "Assets/Scenes/Level_02.unity";

    private struct PickupSpec
    {
        public string name;
        public string prefabPath;
        public string itemPath;
        public Vector3 pos;
    }

    [MenuItem("Tools/Level02/修复：重建字牌拾取物 + 分阶段门禁")]
    public static void Run()
    {
        try
        {
            RunInternal();
            EditorUtility.DisplayDialog("Level02Fixup", "修复完成，场景已保存。", "OK");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Level02Fixup] " + e);
            EditorUtility.DisplayDialog("Level02Fixup", "失败：\n" + e.Message, "OK");
        }
    }

    /// <summary>自动触发用（不弹窗）。</summary>
    public static void RunSilent()
    {
        try
        {
            RunInternal();
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Level02Fixup] " + e);
        }
    }

    private static void RunInternal()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // 拾取物规格：路 / 水 / 禁（职字牌诱饵暂不重建，避免多余物体）
        var specs = new[]
        {
            new PickupSpec
            {
                name = "RoadGlyph_Sign",
                prefabPath = "Assets/Prefabs/Interactables/Item/WorldPickUpItem_RoadGlyph.prefab",
                itemPath = "Assets/Data/Item/Level02/RoadGlyph.asset",
                pos = new Vector3(-3.5f, -4.75f, 0f),
            },
            new PickupSpec
            {
                name = "Glyph_Water",
                prefabPath = "Assets/Prefabs/Interactables/Item/WorldPickUpItem_Glyph.prefab",
                itemPath = "Assets/Data/Item/Level02/WaterGlyph.asset",
                pos = new Vector3(13.5f, -14.3f, 0f),
            },
            new PickupSpec
            {
                name = "Glyph_Forbidden",
                prefabPath = "Assets/Prefabs/Interactables/Item/WorldPickUpItem_Glyph.prefab",
                itemPath = "Assets/Data/Item/Level02/ForbiddenGlyph.asset",
                pos = new Vector3(18.5f, -13.2f, 0f),
            },
        };

        GameObject water = null, forbidden = null;

        foreach (PickupSpec s in specs)
        {
            GameObject existing = GameObject.Find(s.name);
            if (existing != null)
            {
                Debug.Log("[Level02Fixup] 已存在，跳过：" + s.name);
                if (s.name == "Glyph_Water") water = existing;
                if (s.name == "Glyph_Forbidden") forbidden = existing;
                continue;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(s.prefabPath);
            InventoryItemData item = AssetDatabase.LoadAssetAtPath<InventoryItemData>(s.itemPath);
            if (prefab == null || item == null)
            {
                Debug.LogWarning("[Level02Fixup] 找不到预制体或物品资产：" + s.name);
                continue;
            }

            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = s.name;
            go.transform.position = s.pos;
            go.transform.localScale = new Vector3(0.3f, 0.3f, 1f);

            SerializedObject so = new SerializedObject(go.GetComponent<WorldPickupItem>());
            so.FindProperty("itemData").objectReferenceValue = item;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (s.name == "Glyph_Water") water = go;
            if (s.name == "Glyph_Forbidden") forbidden = go;
            Debug.Log("[Level02Fixup] 已重建拾取物：" + s.name);
        }

        // ---- 分阶段门禁：场景里保持可见（编辑器好摆放），运行时由 RoadGate 在 Awake 收起、路牌修复后放出 ----
        Level02RoadSign roadSign = Object.FindFirstObjectByType<Level02RoadSign>();
        if (roadSign != null)
        {
            Level02RoadGate gate = Object.FindFirstObjectByType<Level02RoadGate>();
            GameObject gateGo = gate != null ? gate.gameObject : new GameObject("RoadGate");
            gate = gateGo.GetComponent<Level02RoadGate>();
            if (gate == null)
                gate = gateGo.AddComponent<Level02RoadGate>();

            SerializedObject gso = new SerializedObject(gate);
            gso.FindProperty("roadSign").objectReferenceValue = roadSign;

            // 水和禁在场景里保持激活（编辑器可见）；运行时由门禁收起
            if (water != null) water.SetActive(true);
            if (forbidden != null) forbidden.SetActive(true);

            var list = new System.Collections.Generic.List<GameObject>();
            if (water != null) list.Add(water);
            if (forbidden != null) list.Add(forbidden);

            SerializedProperty arr = gso.FindProperty("revealOnRestored");
            arr.arraySize = list.Count;
            for (int i = 0; i < list.Count; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
            gso.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[Level02Fixup] 已设置分阶段门禁（编辑器可见，运行时路牌修复前收起）");
        }
        else
        {
            Debug.LogWarning("[Level02Fixup] 场景中找不到 Level02RoadSign，跳过分阶段门禁");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Level02Fixup] 修复完成并保存");
    }
}


