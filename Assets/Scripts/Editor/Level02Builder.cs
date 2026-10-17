using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// @trace LVL-202
/// 第二关「荒地」扩展内容建场景工具（占位表现，美术后补）。
/// 注：LVL-201 杂草走廊机制已取消，建场景时会顺带清理历史残留对象。
/// @trace LVL-203
/// @trace LVL-204
/// @trace LVL-205
/// @trace LVL-206
/// @trace LVL-207
/// @trace LVL-208
/// 第二关「荒地」扩展内容建场景工具（占位表现，美术后补）。
/// 命令行：Unity.exe -batchmode -quit -projectPath <proj> -executeMethod Level02Builder.Build
/// 幂等：重复执行会先清掉旧的 Level02_Expanded 再重建。
/// </summary>
public static class Level02Builder
{
    private const string RootName = "Level02_Expanded";
    private const string ScenePath = "Assets/Scenes/Level_02.unity";


    public static void Build()
    {
        try
        {
            BuildInternal();
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Level02Builder] build failed: " + e);
            EditorApplication.Exit(1);
        }
    }

    /// <summary>在已打开的编辑器里执行（菜单/自动触发用）：不退出进程。</summary>
    [MenuItem("Tools/Level02/搭建第二关扩展内容（幂等）")]
    public static void RunInEditor()
    {
        try
        {
            BuildInternal();
            EditorUtility.DisplayDialog("Level02Builder", "第二关扩展内容搭建完成，场景已保存。", "OK");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Level02Builder] build failed: " + e);
            EditorUtility.DisplayDialog("Level02Builder", "搭建失败，详见 Console：\n" + e.Message, "OK");
        }
    }

    private static void BuildInternal()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // ---- 幂等：清掉旧产物 ----
        GameObject old = GameObject.Find(RootName);
        if (old != null)
            Object.DestroyImmediate(old);

        GameObject root = new GameObject(RootName);

        // ---- 找已有对象 ----
        Level02Well well = Object.FindFirstObjectByType<Level02Well>();
        Level02ReedBed reedBed = Object.FindFirstObjectByType<Level02ReedBed>();
        Level02RoadSign roadSign = Object.FindFirstObjectByType<Level02RoadSign>();
        if (well == null || reedBed == null || roadSign == null)
            throw new System.Exception("[Level02Builder] 缺少 Well/ReedBed/RoadSign，场景结构不符合预期");

        TMP_FontAsset cjkFont = FindCjkFont();
        // 把缺的繁体字补进字体（动态字体 TryAddCharacters 会持久化写回）
        EnsureGlyphs(cjkFont, "醫書駅殳聿馬");

        // 弹窗提示也用同一个字体，保证「拼成了醫」这类繁体字不再显示口口
        EnsurePopupFont(cjkFont);

        // ---- 旧版杂草走廊已取消：清理历史残留；并缩小过大的字牌 ----
        RemoveLeftoverGrass();

        // ---- 用工程现有瓦片铺地块（不再用色块当地面） ----
        Tilemap paintLayer = CreatePaintLayer();
        TileBase brick = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Art/Tiles/Level03/Brick_tile.asset");
        TileBase floor = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Art/Tiles/IIP/floorBrown_0.asset");
        TileBase ground = brick != null ? brick : floor;

        // ---- LVL-202 彩蛋路牌文字 ----
        CreateWorldText(roadSign.transform, "Breaking Down-Down", new Vector3(0.6f, 1.2f, 0f),
            2.2f, new Color(0.95f, 0.9f, 0.75f), cjkFont, "EasterEggText");

        // ---- LVL-203 带电水池区（占位） ----
        GameObject poolZone = new GameObject("ElectricPoolZone");
        poolZone.transform.SetParent(root.transform, false);

        // 水潭（只保留这片蓝色，其余占位素材暂不添加）
        CreateRect("Pool", new Vector3(16.5f, -18.2f, 0f), 5f, 2f,
            new Color(0.2f, 0.35f, 0.55f, 0.9f), poolZone.transform, 0);

        // 关键点位
        Transform poolEdge = CreatePoint("PoolEdge", new Vector3(16.5f, -16.9f, 0f), poolZone.transform);
        Transform signPoint = CreatePoint("WarningSignPoint", new Vector3(16.5f, -15.2f, 0f), poolZone.transform);
        Transform exitPoint = CreatePoint("ExitPoint", new Vector3(20f, -26f, 0f), poolZone.transform);

        // 触电循环 NPC ×2（LVL-203 / LVL-204）
        CreatePoolNpc("PoolNPC_A", new Vector3(12f, -14f, 0f), true, poolEdge, signPoint, exitPoint, reedBed, root.transform);
        CreatePoolNpc("PoolNPC_B", new Vector3(14f, -15.2f, 0f), true, poolEdge, signPoint, exitPoint, reedBed, root.transform);

        // 定居 NPC ×3（LVL-206 聚集用）
        var residents = new Level02PoolNPC[3];
        residents[0] = CreatePoolNpc("Resident_A", new Vector3(19f, -13.2f, 0f), false, null, null, null, null, root.transform);
        residents[1] = CreatePoolNpc("Resident_B", new Vector3(23.5f, -13.8f, 0f), false, null, null, null, null, root.transform);
        residents[2] = CreatePoolNpc("Resident_C", new Vector3(21.5f, -12.4f, 0f), false, null, null, null, null, root.transform);

        // ---- LVL-205 视角拉高 ----
        GameObject reveal = new GameObject("CameraReveal");
        reveal.transform.SetParent(root.transform, false);
        var revealComp = reveal.AddComponent<Level02CameraReveal>();
        Set(revealComp, "well", well);
        Set(revealComp, "reedBed", reedBed);
        Set(revealComp, "targetOrthoSize", 10f);

        // ---- LVL-206 聚集导演 + 合并 NPC ----
        Transform gatherPoint = CreatePoint("GatherPoint", new Vector3(21.5f, -13.2f, 0f), root.transform);

        GameObject merged = new GameObject("MergedNPC");
        merged.transform.SetParent(root.transform, false);
        merged.transform.position = gatherPoint.position;
        // 不放占位色块：合并 NPC 只靠头顶感叹号标识（美术后补）
        merged.AddComponent<HeadIndicator>();
        merged.AddComponent<CircleCollider2D>().isTrigger = true;
        merged.GetComponent<CircleCollider2D>().radius = 1.2f;
        merged.AddComponent<Level02FragmentGiver>();
        merged.SetActive(false);

        GameObject director = new GameObject("GatherDirector");
        director.transform.SetParent(root.transform, false);
        var directorComp = director.AddComponent<Level02GatherDirector>();
        Set(directorComp, "well", well);
        Set(directorComp, "reedBed", reedBed);
        Set(directorComp, "residents", residents);
        Set(directorComp, "gatherPoint", gatherPoint);
        Set(directorComp, "mergedNPC", merged);

        // ---- MECH-205 / LVL-207 三处建筑（同「路/水/禁」：选中字牌按 Q 归位） ----
        // 位置在右上灰色砖地场景内部；建筑脚下铺砖地块，头顶标注「医院/图书馆/车站」+ 目标字
        Level02Building hospital = CreateBuilding("Hospital", "医院", "醫",
            new Vector3(26f, -9f, 0f), cjkFont, root.transform, paintLayer, ground);
        Level02Building library = CreateBuilding("Library", "图书馆", "書",
            new Vector3(33f, -13f, 0f), cjkFont, root.transform, paintLayer, ground);

        // 车站（停车场）：铺一块砖石地面，车辆修复后停在这里
        PaintRect(paintLayer, ground, new Vector2(39.5f, -12.5f), 7, 4);
        Level02Building station = CreateBuilding("Station", "车站", "駅",
            new Vector3(40f, -10.5f, 0f), cjkFont, root.transform, paintLayer, ground);

        // ---- 部首碎片：散落在建筑附近，可拾取，集齐自动拼字 ----
        CreateFragmentPickups(root.transform, cjkFont);

        // ---- 拼字管理器：监听背包，集齐部首自动拼成 醫/書/駅 ----
        GameObject combine = new GameObject("CombineManager");
        combine.transform.SetParent(root.transform, false);
        combine.AddComponent<Level02CombineManager>();

        // ---- LVL-208 终局车辆 + 导演 ----
        GameObject vehicle = new GameObject("ExitVehicle");
        vehicle.transform.SetParent(root.transform, false);
        vehicle.transform.position = new Vector3(42.5f, -11f, 0f);
        PlaceholderSprite.EnsureRenderer(vehicle,
            PlaceholderSprite.CreateRect(new Color(0.85f, 0.7f, 0.2f), 48, 24), Color.white, 3);
        var vehicleCol = vehicle.AddComponent<BoxCollider2D>();
        vehicleCol.isTrigger = true;
        vehicleCol.size = new Vector2(4f, 3f);
        vehicle.AddComponent<Level02ExitVehicle>();

        GameObject exit = new GameObject("ExitController");
        exit.transform.SetParent(root.transform, false);
        var exitComp = exit.AddComponent<Level02ExitController>();
        Set(exitComp, "hospital", hospital);
        Set(exitComp, "library", library);
        Set(exitComp, "station", station);
        Set(exitComp, "vehicle", vehicle);

        // ---- 把 Level_03 加进构建列表 ----
        AddSceneToBuildSettings("Assets/Scenes/Level_03.unity");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Level02Builder] Level_02 扩展内容搭建完成");
    }

    // ---------------- 工具 ----------------

    /// <summary>清理占位残留，并把过大的字牌拾取物缩小（按 WorldPickupItem 组件识别，不依赖名字）。</summary>
    private static void RemoveLeftoverGrass()
    {
        string[] removeNames = { "GrassCorridor", "PowerPole", "WireInWater", "ParkingLot" };

        int removed = 0, shrunk = 0;
        foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (!go.scene.isLoaded)
                continue;

            if (System.Array.IndexOf(removeNames, go.name) >= 0)
            {
                Object.DestroyImmediate(go);
                removed++;
                continue;
            }

            // 字牌拾取物过大：统一缩小到 0.5 倍（按组件识别，兼容预制体实例改名）
            var pickup = go.GetComponent<WorldPickupItem>();
            if (pickup != null)
            {
                // WorldPickupItem 本身不带类型，通过序列化的 itemData 判断
                var f = typeof(WorldPickupItem).GetField("itemData",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                var data = f?.GetValue(pickup) as ScriptableObject;
                if (data is Level02GlyphItem)
                {
                    Vector3 s = go.transform.localScale;
                    if (s.x > 0.35f || s.y > 0.35f)
                    {
                        go.transform.localScale = new Vector3(0.3f, 0.3f, s.z);
                        shrunk++;
                    }
                }
            }
        }
        if (removed > 0)
            Debug.Log($"[Level02Builder] 已清理 {removed} 个占位对象（杂草/电线杆/垂线/旧停车场色块）");
        if (shrunk > 0)
            Debug.Log($"[Level02Builder] 已缩小 {shrunk} 个字牌拾取物");
    }

    /// <summary>在场景 Grid 下建一层专用 Tilemap，用于铺新地块（不动原有 Gound 层）。</summary>
    private static Tilemap CreatePaintLayer()
    {
        GameObject grid = GameObject.Find("Grid");
        if (grid == null)
        {
            Debug.LogWarning("[Level02Builder] 场景中没有 Grid，跳过瓦片铺设");
            return null;
        }

        GameObject go = new GameObject("Level02PaintedTiles");
        go.transform.SetParent(grid.transform, false);
        var renderer = go.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = -5; // 铺在角色与装饰之下
        return go.AddComponent<Tilemap>();
    }

    /// <summary>以 center 为中心铺 w×h 的瓦片（按 Grid 默认 1×1 单元格）。</summary>
    private static void PaintRect(Tilemap layer, TileBase tile, Vector2 center, int w, int h)
    {
        if (layer == null || tile == null)
            return;

        int x0 = Mathf.RoundToInt(center.x) - w / 2;
        int y0 = Mathf.RoundToInt(center.y) - h / 2;
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                layer.SetTile(new Vector3Int(x0 + x, y0 + y, 0), tile);
    }

    private static Level02PoolNPC CreatePoolNpc(string name, Vector3 pos, bool victim,
        Transform poolEdge, Transform signPoint, Transform exitPoint, Level02ReedBed reedBed, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(0.6f, 0.6f, 1f); // 占位圆点缩小，不喧宾夺主
        PlaceholderSprite.EnsureRenderer(go,
            PlaceholderSprite.CreateCircle(new Color(0.85f, 0.75f, 0.6f)), Color.white, 3);
        var npc = go.AddComponent<Level02PoolNPC>();
        Set(npc, "poolVictim", victim);
        if (poolEdge != null) Set(npc, "poolEdge", poolEdge);
        if (signPoint != null) Set(npc, "warningSignPoint", signPoint);
        if (exitPoint != null) Set(npc, "exitPoint", exitPoint);
        if (reedBed != null) Set(npc, "reedBed", reedBed);
        return npc;
    }

    private static Level02Building CreateBuilding(string name, string displayName, string glyph,
        Vector3 pos, TMP_FontAsset font, Transform parent, Tilemap paintLayer, TileBase groundTile)
    {
        // 建筑脚下铺砖地块，替代色块表现
        if (paintLayer != null && groundTile != null)
            PaintRect(paintLayer, groundTile, pos, 4, 3);

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;

        // 建筑本体：用基本方块搭出「房子」轮廓（灰墙 + 深色屋顶 + 门），修复后换暖色
        GameObject ruined = CreateHouse(go.transform, "Ruined",
            new Color(0.40f, 0.38f, 0.36f), new Color(0.28f, 0.26f, 0.24f));
        GameObject restored = CreateHouse(go.transform, "Restored",
            new Color(0.80f, 0.74f, 0.60f), new Color(0.55f, 0.30f, 0.22f));
        restored.SetActive(false);

        // 建筑名字标注（始终可见）：医院 / 图书馆 / 车站
        CreateWorldText(go.transform, displayName, new Vector3(0f, 3.2f, 0f),
            2.4f, new Color(1f, 0.98f, 0.9f), font, "BuildingName");

        // 归位的字（修复后显示，初始隐藏）
        GameObject label = CreateWorldText(go.transform, glyph, new Vector3(0f, 2.6f, 0f),
            4f, new Color(0.9f, 0.2f, 0.15f), font, "GlyphLabel");
        label.SetActive(false);

        // 照「路」「水」字牌样式：克隆字牌预制体，剥掉拾取/碰撞逻辑，只留红牌 + 目标字
        GameObject sign = CreateBuildingSign(glyph, font);
        if (sign != null)
        {
            sign.transform.SetParent(go.transform, false);
            sign.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            sign.transform.localScale = new Vector3(0.3f, 0.3f, 1f);
        }

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(4.5f, 4f);

        var building = go.AddComponent<Level02Building>();
        Set(building, "requiredGlyph", glyph);
        Set(building, "teaserMessage", "标牌破碎了，认不出这里是什么地方……选中拼好的字牌按 Q 归位。");
        Set(building, "restoredMessage", displayName + "恢复了原样。");
        Set(building, "ruinedVisual", ruined);
        Set(building, "restoredVisual", restored);
        Set(building, "restoredGlyphLabel", label);
        Set(building, "interactionArea", col);
        return building;
    }

    /// <summary>
    /// 生成 7 个部首碎片拾取物（医殳酉聿曰馬尺），散落在建筑附近。
    /// 每个碎片一个 Level02FragmentItem 资产 + 一个 WorldPickupItem 实例。
    /// </summary>
    private static void CreateFragmentPickups(Transform parent, TMP_FontAsset font)
    {
        string[] fragments = { "医", "殳", "酉", "聿", "曰", "馬", "尺" };
        Vector3[] positions =
        {
            new Vector3(24f, -8.5f, 0f),  // 医
            new Vector3(27f, -9.5f, 0f),  // 殳
            new Vector3(25f, -11f, 0f),   // 酉
            new Vector3(31.5f, -11.5f, 0f),// 聿
            new Vector3(34f, -13f, 0f),   // 曰
            new Vector3(37.5f, -10f, 0f), // 馬
            new Vector3(41f, -12.5f, 0f), // 尺
        };

        GameObject pickupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/Interactables/Item/WorldPickUpItem_Glyph.prefab");

        for (int i = 0; i < fragments.Length; i++)
        {
            string fragName = fragments[i];

            // 1) 确保碎片物品资产存在
            string assetPath = "Assets/Data/Item/Level02/Fragment_" + fragName + ".asset";
            Level02FragmentItem item = AssetDatabase.LoadAssetAtPath<Level02FragmentItem>(assetPath);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<Level02FragmentItem>();
                item.Configure(fragName);
                AssetDatabase.CreateAsset(item, assetPath);
            }

            // 2) 生成拾取物实例
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(pickupPrefab);
            go.name = "FragmentPickup_" + fragName;
            go.transform.SetParent(parent, false);
            go.transform.position = positions[i];
            go.transform.localScale = new Vector3(0.3f, 0.3f, 1f);

            SerializedObject so = new SerializedObject(go.GetComponent<WorldPickupItem>());
            so.FindProperty("itemData").objectReferenceValue = item;
            so.ApplyModifiedPropertiesWithoutUndo();

            // 预制体默认标签是「水」，改成对应部首
            TextMeshPro label = go.GetComponentInChildren<TextMeshPro>(true);
            if (label != null)
            {
                label.text = fragName;
                if (font != null)
                    label.font = font;
            }
        }
        Debug.Log("[Level02Builder] 已生成 7 个部首碎片拾取物");
    }

    /// <summary>创建指定「世界单位」尺寸的纯色矩形占位物（1 单位 = 1 格），避免像素/单位换算出错。</summary>
    /// <summary>
    /// 照「路」「水」字牌样式生成建筑标牌：克隆 WorldPickUpItem_Glyph 预制体，
    /// 剥掉 WorldPickupItem / 碰撞体等游玩逻辑，只留 Visual（红牌）与 GlyphLabel（字），
    /// 并把字换成目标字（醫/書/駅）与动态字体。
    /// </summary>
    private static GameObject CreateBuildingSign(string glyph, TMP_FontAsset font)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/Interactables/Item/WorldPickUpItem_Glyph.prefab");
        if (prefab == null)
        {
            Debug.LogWarning("[Level02Builder] 找不到 WorldPickUpItem_Glyph 预制体，建筑标牌跳过");
            return null;
        }

        GameObject sign = Object.Instantiate(prefab);
        sign.name = "BuildingSign";

        // 剥掉游玩逻辑：碰撞体全部移除；MonoBehaviour 只保留 TMP（文字标签）
        foreach (Collider2D col in sign.GetComponentsInChildren<Collider2D>(true))
            Object.DestroyImmediate(col);
        foreach (MonoBehaviour mb in sign.GetComponentsInChildren<MonoBehaviour>(true))
            if (!(mb is TextMeshPro))
                Object.DestroyImmediate(mb);
        // Interaction Area 空壳子物体一并删除
        Transform interactArea = sign.transform.Find("Interaction Area");
        if (interactArea != null)
            Object.DestroyImmediate(interactArea.gameObject);

        // 字换成目标字，并使用动态字体（静态字体缺「醫書駅」等字）
        TextMeshPro label = sign.GetComponentInChildren<TextMeshPro>(true);
        if (label != null)
        {
            label.text = glyph;
            if (font != null)
                label.font = font;
        }
        return sign;
    }

    /// <summary>
    /// 向现有动态字体资产补字（TryAddCharacters 会持久化写回资产），
    /// 解决静态字符集缺「醫書駅殳聿馬」的问题——不新建字体，就用工程已有的中文字体。
    /// </summary>
    /// <summary>把弹窗提示文本的字体换成能显示繁体字的字体（编辑 PlayerUI.prefab）。</summary>
    private static void EnsurePopupFont(TMP_FontAsset font)
    {
        if (font == null)
            return;

        const string prefabPath = "Assets/Prefabs/UI/PlayerUI.prefab";
        GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
        if (contents == null)
        {
            Debug.LogWarning("[Level02Builder] 找不到 " + prefabPath + "，弹窗字体未改");
            return;
        }

        PlayerUIPopUpManager popup = contents.GetComponentInChildren<PlayerUIPopUpManager>(true);
        if (popup != null && popup.popUpText != null && popup.popUpText.font != font)
        {
            popup.popUpText.font = font;
            PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            Debug.Log("[Level02Builder] 已把弹窗提示字体换成 " + font.name);
        }

        PrefabUtility.UnloadPrefabContents(contents);
    }

    private static void EnsureGlyphs(TMP_FontAsset font, string characters)
    {
        if (font == null)
            return;

        if (font.TryAddCharacters(characters))
        {
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssetIfDirty(font);
            Debug.Log("[Level02Builder] 已向 " + font.name + " 补字: " + characters);
        }
        else
        {
            Debug.LogWarning("[Level02Builder] 字体 " + font.name + " 补字失败（图集可能已满或非动态模式）");
        }
    }

    /// <summary>用基本方块搭一个「房子」轮廓：墙体 + 屋顶 + 门（占位，等正式美术替换）。</summary>
    private static GameObject CreateHouse(Transform parent, string name, Color wall, Color roof)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        // 墙体（底部中点对齐地面）
        CreateRect("Wall", new Vector3(0f, 1.1f, 0f), 3f, 2.2f, wall, go.transform, 1);
        // 屋顶
        CreateRect("Roof", new Vector3(0f, 2.5f, 0f), 3.4f, 0.6f, roof, go.transform, 2);
        // 门
        CreateRect("Door", new Vector3(0f, 0.7f, 0f), 0.9f, 1.4f, new Color(0.18f, 0.15f, 0.12f), go.transform, 3);

        return go;
    }

    private static GameObject CreateRect(string name, Vector3 pos, float worldW, float worldH, Color color,
        Transform parent, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = new Vector3(worldW, worldH, 1f);
        PlaceholderSprite.EnsureRenderer(go, PlaceholderSprite.CreateRect(color, 16, 16, false), Color.white, order);
        return go;
    }

    private static Transform CreatePoint(string name, Vector3 pos, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        return go.transform;
    }

    private static GameObject CreateWorldText(Transform parent, string text, Vector3 localPos,
        float size, Color color, TMP_FontAsset font, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        // 世界空间文字默认排序为 0，会被建筑色块挡住，必须提到上层
        tmp.sortingOrder = 50;
        if (font != null)
            tmp.font = font;
        return go;
    }

    private static TMP_FontAsset FindCjkFont()
    {
        // 优先用用户提供的 Noto Sans CJK TC（繁体字全覆盖：醫書駅殳聿馬都在），回退 STXINWEI
        foreach (string guid in AssetDatabase.FindAssets("t:TMP_FontAsset NotoSansCJKtc"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font != null)
                return font;
        }

        foreach (string guid in AssetDatabase.FindAssets("t:TMP_FontAsset STXINWEI"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font != null)
                return font;
        }
        Debug.LogWarning("[Level02Builder] 未找到中文字体（NotoSansCJKtc / STXINWEI）");
        return null;
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in scenes)
            if (s.path == scenePath)
                return;
        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("[Level02Builder] 已将 " + scenePath + " 加入 Build Settings");
    }

    /// <summary>用反射写 private [SerializeField] 字段（仅 Editor 建场景时使用）。</summary>
    private static void Set(object target, string field, object value)
    {
        FieldInfo f = target.GetType().GetField(field,
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
        if (f == null)
            throw new System.Exception($"[Level02Builder] 字段不存在: {target.GetType().Name}.{field}");
        f.SetValue(target, value);
    }
}








