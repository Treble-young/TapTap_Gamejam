using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GearPuzzleSetup
{
    private const string Art = "Assets/Art/GearPuzzle";
    private const string ScenePath = "Assets/Scenes/GameTest1.unity";
    private const string PixelFont = "Assets/Art/PixelUIFree/Font/fusion-pixel-font-12px-monospaced-otf-v2026.09.01/fusion-pixel-12px-monospaced-zh_hans.asset";
    private const string WoodDir = "Assets/Art/PixelUIFree/Sprites/themes/wood";

    // Loads the pixel wood assets (shared with the Level02 popup style) onto an installed puzzle.
    private static void LoadWoodStyle(GearPuzzleController puzzle)
    {
        puzzle.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PixelFont);
        puzzle.woodWindow=AssetDatabase.LoadAssetAtPath<Sprite>(WoodDir+"/window.png");
        puzzle.woodInset=AssetDatabase.LoadAssetAtPath<Sprite>(WoodDir+"/panel_inset.png");
        puzzle.woodButton=AssetDatabase.LoadAssetAtPath<Sprite>(WoodDir+"/button_normal.png");
    }

    [MenuItem("Tools/Gear Puzzle/Install in GameTest1")]
    public static void Install()
    {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
        Scene scene=SceneManager.GetSceneByPath(ScenePath);
        if(!scene.IsValid() || !scene.isLoaded) scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        GearBase entrance=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<GearBase>(true))
            .First(g=>g.GetComponent<Canvas>()==null && g.gameUI!=null);
        GameObject ui=entrance.gameUI;
        if(ui.GetComponent<GearPuzzleController>()!=null) {Debug.Log("Gear puzzle already installed.");return;}
        Undo.RecordObject(entrance,"Configure gear entrance");
        entrance.interactOnce=false;
        // Only the UI may own the lock; the interactable is always enabled in the world.
        var worldLock=entrance.GetComponent<PlayerInputLock>();
        if(worldLock!=null) Undo.DestroyObjectImmediate(worldLock);
        var background=ui.transform.Find("Background");
        if(background!=null) {Undo.RecordObject(background.gameObject,"Hide legacy gear placeholder");background.gameObject.SetActive(false);}
        var puzzle=Undo.AddComponent<GearPuzzleController>(ui);
        puzzle.entrance=entrance;
        LoadWoodStyle(puzzle);
        puzzle.smallGear=ImportSprite("GearSmall.png");
        puzzle.mediumGear=ImportSprite("GearMedium.png");
        puzzle.largeGear=ImportSprite("GearLarge.png");
        if(ui.GetComponent<PlayerInputLock>()==null) Undo.AddComponent<PlayerInputLock>(ui);
        puzzle.BuildView();
        ui.SetActive(false);
        PrefabUtility.RecordPrefabInstancePropertyModifications(entrance);
        PrefabUtility.RecordPrefabInstancePropertyModifications(ui);
        PrefabUtility.RecordPrefabInstancePropertyModifications(ui.GetComponent<Canvas>());
        PrefabUtility.RecordPrefabInstancePropertyModifications(ui.GetComponent<CanvasScaler>());
        if(background!=null) PrefabUtility.RecordPrefabInstancePropertyModifications(background.gameObject);
        EditorUtility.SetDirty(puzzle);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject=entrance.gameObject;
        Debug.Log("GEAR_INSTALL_OK: GameTest1 existing interaction -> existing Canvas -> GearPuzzleController.");
    }
    private static Sprite ImportSprite(string filename)
    {
        string path=Art+"/"+filename;AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;
        importer.spriteImportMode=SpriteImportMode.Single;
        importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=1024;
        importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.spritePixelsPerUnit=100;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    private static void Check(bool ok,string message)
    {
        if(!ok) throw new Exception("GEAR_TEST_FAILED: "+message);
    }
    [MenuItem("Tools/Gear Puzzle/Validate Rules")]
    public static void ValidateRules()
    {
        float[] r=GearPuzzleRules.Radii;
        int[] slots={0,-1,-1,-1,-1,-1,6};
        float[] v=GearPuzzleRules.Speeds(slots,r,out bool jam);
        Check(!jam && v[0]!=0 && v[6]==0,"initially only source is powered");
        for(int i=1;i<=5;i++)
        {
            Check(GearPuzzleRules.CanPlace(i,i,slots,r),"solution placement "+i);
            slots[i]=i;
            v=GearPuzzleRules.Speeds(slots,r,out jam);
            Check(!jam && (i==5 || v[6]==0),"no premature win");
        }
        Check(v[6]!=0,"complete solution");
        for(int i=0;i<6;i++)
        {
            Check(v[i]*v[i+1]<0,"opposite direction");
            Check(Mathf.Abs(v[i]*r[i]+v[i+1]*r[i+1])<.01f,"pitch speed conservation");
        }
        Check(!GearPuzzleRules.CanPlace(2,1,slots,r),"occupied axle");
        Check(!GearPuzzleRules.CanPlace(2,0,slots,r),"fixed source protected");
        int[] overlap={0,1,-1,-1,-1,-1,6};
        Check(!GearPuzzleRules.CanPlace(4,2,overlap,r),"large gear overlap rejected");
        slots[3]=-1;v=GearPuzzleRules.Speeds(slots,r,out jam);
        Check(v[6]==0 && v[4]==0,"removing middle disconnects downstream");
        foreach(int decoy in new[]{7,8,9})
        {
            slots[3]=decoy;v=GearPuzzleRules.Speeds(slots,r,out jam);
            Check(v[6]==0,"decoy axle "+decoy+" does not solve puzzle");
        }
        // Jam traps: r60 on decoy 7 beside the 3-4 link, and on decoy 8 beside the 1-2 link.
        slots=new[]{0,1,2,3,4,7,6};v=GearPuzzleRules.Speeds(slots,r,out jam);
        Check(jam && v[6]==0,"decoy 7 jams the 3-4 link");
        slots=new[]{0,1,2,-1,-1,8,6};v=GearPuzzleRules.Speeds(slots,r,out jam);
        Check(jam && v[6]==0,"decoy 8 jams the 1-2 link");
        // Dead-end branch: decoy 9 spins off gear 4 but never reaches 6, and blocks axle 5.
        slots=new[]{0,1,2,3,4,9,6};v=GearPuzzleRules.Speeds(slots,r,out jam);
        Check(!jam && v[6]==0,"decoy 9 is a dead end");
        Check(!GearPuzzleRules.CanPlace(1,5,slots,r),"decoy 9 blocks axle 5");
        // Brute force every placement combo; same-radius pieces are interchangeable,
        // so solutions are keyed by axle:radius geometry. Exactly one must exist.
        var solutions=new HashSet<string>();
        int[] assignment={0,-1,-1,-1,-1,-1,6};
        void Search(int piece)
        {
            if(piece==6)
            {
                var v2=GearPuzzleRules.Speeds(assignment,r,out bool j2);
                if(!j2 && Mathf.Abs(v2[6])>.01f)
                {
                    var placed=new List<string>();
                    for(int i=0;i<7;i++) if(assignment[i]>0) placed.Add(assignment[i]+":"+r[i]);
                    placed.Sort(); solutions.Add(string.Join(",",placed));
                }
                return;
            }
            assignment[piece]=-1; Search(piece+1);
            for(int a=1;a<GearPuzzleRules.Axles.Length;a++)
            {
                if(!GearPuzzleRules.CanPlace(piece,a,assignment,r)) continue;
                assignment[piece]=a; Search(piece+1);
            }
            assignment[piece]=-1;
        }
        Search(1);
        Check(solutions.Count==1,"unique solution expected, found "+solutions.Count);
        Debug.Log("GEAR_RULE_TESTS_OK: initial, solution, partial, occupied, fixed, overlap, disconnection, decoys, jam traps, dead end, uniqueness, direction, speed ratio.");
    }

    // Opens GameTest1 only for play, leaving all loaded/dirty editing scenes intact.
    [MenuItem("Tools/Gear Puzzle/Start Isolated Play Test")]
    public static void StartPlayTest()
    {
        if(EditorApplication.isPlaying) return;
        var old=EditorSceneManager.playModeStartScene;
        SessionState.SetString("GearPuzzle.PreviousStartScene",old==null?"":AssetDatabase.GetAssetPath(old));
        EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        SessionState.SetBool("GearPuzzle.Testing",true);
        EditorApplication.isPlaying=true;
    }
    [InitializeOnLoadMethod]
    private static void Register()
    {
        EditorApplication.playModeStateChanged-=OnPlayState;
        EditorApplication.playModeStateChanged+=OnPlayState;
    }
    private static void OnPlayState(PlayModeStateChange change)
    {
        if(change!=PlayModeStateChange.EnteredEditMode || !SessionState.GetBool("GearPuzzle.Testing",false)) return;
        string path=SessionState.GetString("GearPuzzle.PreviousStartScene","");
        EditorSceneManager.playModeStartScene=string.IsNullOrEmpty(path)?null:AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
        SessionState.SetBool("GearPuzzle.Testing",false);
    }
    // Rebuilds the serialized panel view from code (wood restyle). Tears down the old
    // GearPuzzlePanel and regenerates it, preserving the puzzle's serialized references.
    [MenuItem("Tools/Gear Puzzle/Rebuild View")]
    public static void RebuildView()
    {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
        Scene scene=SceneManager.GetSceneByPath(ScenePath);
        if(!scene.IsValid() || !scene.isLoaded) scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        var puzzle=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<GearPuzzleController>(true)).FirstOrDefault();
        if(puzzle==null) throw new Exception("GEAR_REBUILD_FAILED: GearPuzzleController not found in GameTest1. Run Install first.");
        Undo.RegisterFullObjectHierarchyUndo(puzzle.gameObject,"Rebuild gear puzzle view");
        LoadWoodStyle(puzzle);
        puzzle.DestroyView();
        puzzle.BuildView();
        EditorUtility.SetDirty(puzzle);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("GEAR_REBUILD_OK: wood-styled view regenerated.");
    }

    // Restyles the serialized axle hubs (AxleRing*/Axle*) and repairs missing CanvasRenderers
    // on every GearPuzzleDisc (they silently never render without one). Touches nothing else.
    [MenuItem("Tools/Gear Puzzle/Refresh Axle Visuals")]
    public static void RefreshAxleVisuals()
    {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
        Scene scene=SceneManager.GetSceneByPath(ScenePath);
        if(!scene.IsValid() || !scene.isLoaded) scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        var puzzle=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<GearPuzzleController>(true)).FirstOrDefault();
        if(puzzle==null) throw new Exception("GEAR_REFRESH_FAILED: GearPuzzleController not found in GameTest1. Run Install first.");
        var discs=puzzle.GetComponentsInChildren<GearPuzzleDisc>(true).Where(d=>d.name.StartsWith("Axle")).ToArray();
        Undo.RecordObjects(discs.Cast<UnityEngine.Object>().Concat(discs.Select(d=>(UnityEngine.Object)d.rectTransform)).ToArray(),"Refresh gear axle visuals");
        puzzle.RefreshAxleHubs();
        int repaired=0;
        foreach(var d in puzzle.GetComponentsInChildren<GearPuzzleDisc>(true))
            if(d.GetComponent<CanvasRenderer>()==null) {Undo.AddComponent<CanvasRenderer>(d.gameObject);repaired++;}
        foreach(var d in discs) EditorUtility.SetDirty(d);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("GEAR_AXLE_REFRESH_OK: "+discs.Length+" axle visuals restyled, "+repaired+" missing CanvasRenderer repaired.");
    }
    private static GearPuzzleController RuntimePuzzle()
    {
        return UnityEngine.Object.FindObjectsByType<GearPuzzleController>(FindObjectsInactive.Include,FindObjectsSortMode.None)
            .First(p=>p.gameObject.scene.name=="GameTest1");
    }
    [MenuItem("Tools/Gear Puzzle/Open Runtime UI")]
    public static void OpenRuntimeUI()
    {
        if(!EditorApplication.isPlaying) throw new Exception("Play mode required.");
        Application.runInBackground=true; var p=RuntimePuzzle();p.entrance.Interact(null); Canvas.ForceUpdateCanvases();
        Debug.Log("GEAR_OPEN_OK: UI="+p.gameObject.activeInHierarchy+", inputLocked="+(PlayerInputManager.Instance!=null && PlayerInputManager.Instance.IsPlayerInputLocked));
    }
    [MenuItem("Tools/Gear Puzzle/Runtime Interaction Test")]
    public static void RuntimeTest()
    {
        if(!EditorApplication.isPlaying) throw new Exception("Play mode required.");
        var p=RuntimePuzzle();
        Check(p.gameObject.activeInHierarchy,"UI open");
        Check(EventSystem.current!=null,"EventSystem available");
        Canvas.ForceUpdateCanvases(); var list=p.Pieces;
        p.ResetPuzzle();
        int calls=0;p.onSolved.AddListener(()=>calls++);
        var panel=(RectTransform)list[0].transform.parent;
        PointerEventData Pointer(Vector2 local)
        {
            return new PointerEventData(EventSystem.current) {position=RectTransformUtility.WorldToScreenPoint(null,panel.TransformPoint(local)),button=PointerEventData.InputButton.Left};
        }
        void Drop(int piece,int slot)
        {
            var item=list[piece];
            var begin=Pointer(item.Rect.anchoredPosition);
            ExecuteEvents.Execute<IBeginDragHandler>(item.gameObject,begin,ExecuteEvents.beginDragHandler);
            var end=Pointer(GearPuzzleRules.BoardOffset+GearPuzzleRules.Axles[slot]);
            ExecuteEvents.Execute<IDragHandler>(item.gameObject,end,ExecuteEvents.dragHandler);
            ExecuteEvents.Execute<IEndDragHandler>(item.gameObject,end,ExecuteEvents.endDragHandler);
        }
        // Verify GraphicsRaycaster can actually hit a tray gear.
        var hits=new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(Pointer(list[1].Rect.anchoredPosition),hits);
        Check(hits.Any(h=>h.gameObject==list[1].gameObject),"gear pointer raycast");
        Drop(1,1);Check(list[1].slot==1,"drag + snap");
        Drop(2,1);Check(list[2].slot==-1,"occupied drop rollback");
        p.ReturnToTray(list[1]);Check(list[1].slot==-1,"return to tray");
        Drop(1,1);
        p.Close();
        Check(!p.gameObject.activeSelf,"close UI");
        Check(PlayerInputManager.Instance==null || !PlayerInputManager.Instance.IsPlayerInputLocked,"input restored");
        p.entrance.Interact(null);Check(list[1].slot==1,"reopen preserves placement");
        p.ResetPuzzle();Check(list[1].slot==-1,"reset");
        for(int i=1;i<=5;i++) Drop(i,i);
        Check(p.IsSolved && p.PoweredCount==7 && calls==1,"success and single event");
        p.Close();p.entrance.Interact(null);
        Check(p.IsSolved && calls==1,"reopen does not repeat success");
        p.onSolved.RemoveAllListeners();
        Debug.Log("GEAR_RUNTIME_TESTS_OK: raycast, drag, snap, rollback, return, reset, reopen, input release, solved, event once.");
    }
    [MenuItem("Tools/Gear Puzzle/Capture Runtime UI")]
    public static void Capture()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Play mode required.");
        Directory.CreateDirectory("Temp/GearPuzzle");
        ScreenCapture.CaptureScreenshot("Temp/GearPuzzle/preview.png");
        Debug.Log("GEAR_CAPTURE_REQUESTED");
    }
}
