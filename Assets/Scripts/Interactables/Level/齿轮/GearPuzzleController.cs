using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GearPuzzleController : MonoBehaviour
{
    [Header("入口与美术")]
    public GearBase entrance;
    public Sprite smallGear, mediumGear, largeGear;
    public TMP_FontAsset font;
    [Header("像素木质风资源（PixelUIFree wood 主题）")]
    public Sprite woodWindow, woodInset, woodButton;
    [Header("第一次通关时触发，可连接场景机关")]
    public UnityEvent onSolved = new UnityEvent();
    [SerializeField] private bool isSolved;
    public bool IsSolved => isSolved;
    public int PoweredCount { get; private set; }
    [SerializeField] private RectTransform panel;
    [SerializeField] private List<GearPuzzlePiece> pieces = new List<GearPuzzlePiece>();
    [SerializeField] private TMP_Text status, counter;
    [SerializeField] private GameObject success;
    [SerializeField] private Button resetButton;
    [SerializeField] private GearPuzzleDisc preview;
    private float[] speeds;
    private GearPuzzlePiece dragging;
    private int previousSlot;
    private Vector2 pointerOffset;
    private bool eventSent;
    private float messageUntil;
    private static readonly Color Gold = new Color(.96f,.73f,.28f);
    private static readonly Color Silver = new Color(.74f,.80f,.82f);
    private static readonly Color Green = new Color(.48f,.88f,.70f);
    private static readonly Color Cream = new Color(.93f,.87f,.72f);
    private static readonly Color DarkBrown = new Color(.24f,.16f,.09f);
    // Axle hub visuals: bright brass ring + steel bolt, shared by BuildView and the editor refresh tool.
    public static readonly Color AxleRingColor = new Color(.98f,.78f,.34f);
    public static readonly Color AxleBoltColor = new Color(.88f,.91f,.89f);
    public const float AxleRingSize = 44f;
    public const float AxleRingInner = .72f;
    public const float AxleBoltSize = 15f;
    public IReadOnlyList<GearPuzzlePiece> Pieces => pieces;

    private void Awake() { if(panel==null) BuildView(); }
    private void OnEnable()
    {
        if(panel==null) BuildView();
        EnsureDiscRenderers();
        Fit(); RefreshNetwork(false);
    }
    // Older serialized scenes may lack CanvasRenderer on the custom discs (nothing renders without it).
    public void EnsureDiscRenderers()
    {
        foreach(var d in GetComponentsInChildren<GearPuzzleDisc>(true))
            if(d.GetComponent<CanvasRenderer>()==null) {d.gameObject.AddComponent<CanvasRenderer>();d.SetAllDirty();}
    }
    private void OnDisable()
    {
        CancelDrag();
    }
    private void Update()
    {
        Fit();
        if(Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) { Close(); return; }
        if(speeds!=null)
            for(int i=0;i<pieces.Count;i++)
                if(pieces[i]!=dragging && pieces[i].rotor!=null)
                    pieces[i].rotor.Rotate(0,0,speeds[i]*Time.unscaledDeltaTime);
        if(messageUntil>0 && Time.unscaledTime>messageUntil) { messageUntil=0; SetStatus(); }
    }
    private void Fit()
    {
        if(panel==null) return;
        var canvasRect=(RectTransform)transform;
        float scale=Mathf.Min(canvasRect.rect.width/1280f,canvasRect.rect.height/800f);
        panel.localScale=Vector3.one*Mathf.Max(.01f,scale);
    }
    public void Close()
    {
        CancelDrag();
        if(entrance!=null) entrance.Exit();
        else gameObject.SetActive(false);
    }
    public void ResetPuzzle()
    {
        if(isSolved) return;
        CancelDrag();
        foreach(var piece in pieces)
        {
            piece.slot=piece.IsFixed?piece.index:-1;
            PlaceVisual(piece); piece.rotor.localRotation=Quaternion.identity;
        }
        RefreshNetwork(false);
    }
    public void BeginDrag(GearPuzzlePiece piece, PointerEventData e)
    {
        if(isSolved || piece.IsFixed || dragging!=null) return;
        dragging=piece; previousSlot=piece.slot;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(panel,e.position,e.pressEventCamera,out var pointer);
        pointerOffset=piece.Rect.anchoredPosition-pointer;
        piece.slot=-1; piece.Rect.localScale=Vector3.one;
        piece.transform.SetAsLastSibling();
        RefreshNetwork(false); Drag(piece,e);
    }
    public void Drag(GearPuzzlePiece piece, PointerEventData e)
    {
        if(dragging!=piece) return;
        if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(panel,e.position,e.pressEventCamera,out var p)) return;
        p+=pointerOffset;
        p.x=Mathf.Clamp(p.x,-610+piece.radius,610-piece.radius);
        p.y=Mathf.Clamp(p.y,-280+piece.radius,275-piece.radius);
        piece.Rect.anchoredPosition=p;
        int nearest=NearestSlot(p);
        preview.gameObject.SetActive(nearest>=0);
        if(nearest>=0)
        {
            preview.rectTransform.anchoredPosition=GearPuzzleRules.BoardOffset+GearPuzzleRules.Axles[nearest];
            preview.rectTransform.sizeDelta=Vector2.one*(piece.radius*2+18);
            preview.color=CanPlace(piece,nearest)?Green:new Color(1,.40f,.34f);
        }
    }
    public void EndDrag(GearPuzzlePiece piece, PointerEventData e)
    {
        if(dragging!=piece) return;
        int nearest=NearestSlot(piece.Rect.anchoredPosition);
        bool tray=piece.Rect.anchoredPosition.x>285;
        bool valid=!tray && nearest>=0 && CanPlace(piece,nearest);
        piece.slot=tray?-1:valid?nearest:previousSlot;
        dragging=null; preview.gameObject.SetActive(false); PlaceVisual(piece);
        RefreshNetwork(true);
        if(!valid && !tray) Message("这里无法安装：靠近空轴心，且不能与其他齿轮重叠。");
    }
    private void CancelDrag()
    {
        if(dragging==null) return;
        dragging.slot=previousSlot; PlaceVisual(dragging); dragging=null;
        if(preview!=null) preview.gameObject.SetActive(false);
        RefreshNetwork(false);
    }
    public void ReturnToTray(GearPuzzlePiece piece)
    {
        if(isSolved || piece.IsFixed || dragging!=null) return;
        piece.slot=-1; PlaceVisual(piece); RefreshNetwork(false);
    }
    private int NearestSlot(Vector2 p)
    {
        int nearest=-1; float best=46f;
        for(int i=1;i<GearPuzzleRules.Axles.Length;i++)
        {
            if(i==6) continue;
            float d=Vector2.Distance(p,GearPuzzleRules.BoardOffset+GearPuzzleRules.Axles[i]);
            if(d<best) {best=d; nearest=i;}
        }
        return nearest;
    }
    private int[] Slots()
    {
        int[] s=new int[pieces.Count];
        for(int i=0;i<s.Length;i++) s[i]=pieces[i].slot;
        return s;
    }
    public bool CanPlace(GearPuzzlePiece piece,int axle)
        => GearPuzzleRules.CanPlace(piece.index,axle,Slots(),GearPuzzleRules.Radii);
    private void PlaceVisual(GearPuzzlePiece p)
    {
        p.Rect.anchoredPosition=p.slot>=0?GearPuzzleRules.BoardOffset+GearPuzzleRules.Axles[p.slot]:p.home;
        p.Rect.localScale=Vector3.one*(p.slot<0 && p.radius==60?.84f:1f);
    }
    private void RefreshNetwork(bool allowWin)
    {
        if(pieces.Count!=7) return;
        speeds=GearPuzzleRules.Speeds(Slots(),GearPuzzleRules.Radii,out bool jam);
        PoweredCount=0;
        for(int i=0;i<pieces.Count;i++)
        {
            bool powered=Mathf.Abs(speeds[i])>.01f;
            if(powered) PoweredCount++;
            pieces[i].artwork.color=pieces[i].IsFixed?Gold:powered?new Color(.82f,1f,.91f):Silver;
        }
        if(allowWin && !jam && Mathf.Abs(speeds[6])>.01f && !isSolved)
        {
            isSolved=true;
            success.SetActive(true); success.transform.SetAsLastSibling(); resetButton.interactable=false;
            if(!eventSent) {eventSent=true; onSolved.Invoke();}
        }
        if(counter!=null) counter.text=$"传动  {PoweredCount} / 7"; // legacy counter; hidden in the wood restyle
        if(jam) Message("齿轮互相卡住了，尝试移走一枚。");
        else SetStatus();
    }
    private void SetStatus()
    {
        if(status!=null) status.text=isSolved?"动力已传至终点。":"";
    }
    private void Message(string text)
    {
        if(status==null) return;
        status.text=text; messageUntil=Time.unscaledTime+3f;
    }

    // Editor builder uses the same view factory as runtime fallback.
    public void BuildView()
    {
        if(panel!=null) return;
        pieces.Clear();
        var canvas=GetComponent<Canvas>(); canvas.sortingOrder=100;
        var scaler=GetComponent<CanvasScaler>();
        if(scaler!=null) {scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,800);scaler.matchWidthOrHeight=.5f;}
        var backdrop=Rect("GearPuzzleBackdrop",transform,Vector2.zero,Vector2.zero);
        backdrop.anchorMin=Vector2.zero;backdrop.anchorMax=Vector2.one;backdrop.sizeDelta=Vector2.zero;
        var shade=backdrop.gameObject.AddComponent<Image>();shade.color=new Color(.09f,.06f,.04f,.88f);
        panel=Rect("GearPuzzlePanel",transform,Vector2.zero,new Vector2(1280,800));
        // Pixel wood style, matching the Level02 popup (wood window sprite + fusion pixel font).
        Wood("MainWindow",Vector2.zero,new Vector2(1240,760),woodWindow,new Color(.42f,.30f,.19f));
        ButtonAt("Close","返回  ×",new Vector2(515,330),new Vector2(150,48),Close);
        Wood("Board",new Vector2(-160,-5),new Vector2(858,568),woodInset,new Color(.24f,.17f,.11f));
        Wood("Tray",new Vector2(450,-5),new Vector2(290,560),woodInset,new Color(.24f,.17f,.11f));
        Text("TrayHint","拖动安装 · 右键收回",panel,new Vector2(450,232),new Vector2(260,26),15,Cream);
        for(int i=0;i<GearPuzzleRules.Axles.Length;i++)
        {
            Vector2 p=GearPuzzleRules.BoardOffset+GearPuzzleRules.Axles[i];
            Disc("AxleRing"+i,p,AxleRingSize,AxleRingColor,AxleRingInner);
            Disc("Axle"+i,p,AxleBoltSize,AxleBoltColor,0);
        }
        preview=Disc("SnapPreview",Vector2.zero,100,Green,.96f);preview.gameObject.SetActive(false);
        var homes=new[]{Vector2.zero,new Vector2(375,120),new Vector2(380,-5),new Vector2(520,-5),new Vector2(450,-165),new Vector2(525,120),Vector2.zero};
        for(int i=0;i<7;i++)
        {
            float r=GearPuzzleRules.Radii[i];
            // Addendum is 5 units; artwork transparent margin approximately 3%.
            float size=(r+5)*2/.94f;
            var rt=Rect("Gear_"+i,panel,Vector2.zero,Vector2.one*size);
            var hit=rt.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=i!=0 && i!=6;
            var piece=rt.gameObject.AddComponent<GearPuzzlePiece>();
            piece.owner=this;piece.index=i;piece.radius=r;piece.home=homes[i];piece.slot=piece.IsFixed?i:-1;
            var rotor=Rect("Rotor",rt,Vector2.zero,Vector2.one*size);
            var art=rotor.gameObject.AddComponent<Image>();
            art.sprite=r==40?smallGear:r==100?largeGear:mediumGear;art.preserveAspect=true;art.raycastTarget=false;
            piece.rotor=rotor;piece.artwork=art;pieces.Add(piece);PlaceVisual(piece);
        }
        resetButton=ButtonAt("Reset","重新摆放",new Vector2(450,-324),new Vector2(240,44),ResetPuzzle);
        status=Text("Status","",panel,new Vector2(-85,-367),new Vector2(1010,32),17,Cream);
        success=Wood("Success",new Vector2(-160,0),new Vector2(530,160),woodWindow,new Color(.42f,.30f,.19f)).gameObject;
        Text("SuccessTitle","传 动 已 恢 复",success.transform,new Vector2(0,35),new Vector2(490,48),30,DarkBrown);
        Text("SuccessHint","所有齿轮已连通，点击右上角「返回」继续。",success.transform,new Vector2(0,-30),new Vector2(490,45),18,new Color(.40f,.28f,.16f));
        success.SetActive(isSolved);
        Fit();RefreshNetwork(false);
    }
    // Tears down the serialized view so BuildView can regenerate it (used by the editor rebuild tool).
    public void DestroyView()
    {
        if(panel!=null)
        {
            if(Application.isPlaying) Destroy(panel.gameObject);
            else DestroyImmediate(panel.gameObject);
        }
        panel=null;pieces.Clear();status=null;counter=null;success=null;resetButton=null;preview=null;
    }
    // Restyle or rebuild the 8 axle hubs in place. Idempotent: safe to run on scenes
    // whose view was serialized by an older BuildView. Only touches AxleRing*/Axle* objects.
    public void RefreshAxleHubs()
    {
        if(panel==null) BuildView();
        for(int i=0;i<GearPuzzleRules.Axles.Length;i++)
        {
            Vector2 p=GearPuzzleRules.BoardOffset+GearPuzzleRules.Axles[i];
            var ring=panel.Find("AxleRing"+i)?.GetComponent<GearPuzzleDisc>();
            var bolt=panel.Find("Axle"+i)?.GetComponent<GearPuzzleDisc>();
            if(ring==null) ring=Disc("AxleRing"+i,p,AxleRingSize,AxleRingColor,AxleRingInner);
            if(bolt==null) bolt=Disc("Axle"+i,p,AxleBoltSize,AxleBoltColor,0);
            // Axles may have moved in GearPuzzleRules since the scene was serialized.
            ring.rectTransform.anchoredPosition=p; bolt.rectTransform.anchoredPosition=p;
            // Recreated hubs would land on top of gears; keep them beneath Gear_0 like BuildView does.
            int gearIndex=panel.Find("Gear_0")?.GetSiblingIndex()??panel.childCount;
            if(ring.transform.GetSiblingIndex()>gearIndex) ring.transform.SetSiblingIndex(gearIndex);
            if(bolt.transform.GetSiblingIndex()>gearIndex) bolt.transform.SetSiblingIndex(gearIndex);
            StyleAxleHub(ring,bolt);
        }
    }
    private static void StyleAxleHub(GearPuzzleDisc ring,GearPuzzleDisc bolt)
    {
        ring.rectTransform.sizeDelta=Vector2.one*AxleRingSize;ring.color=AxleRingColor;ring.innerRatio=AxleRingInner;
        bolt.rectTransform.sizeDelta=Vector2.one*AxleBoltSize;bolt.color=AxleBoltColor;bolt.innerRatio=0f;
        ring.SetAllDirty();bolt.SetAllDirty();
    }
    private static RectTransform Rect(string name,Transform parent,Vector2 pos,Vector2 size)
    {
        var go=new GameObject(name,typeof(RectTransform));go.layer=5;
        var r=(RectTransform)go.transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);
        r.anchoredPosition=pos;r.sizeDelta=size;return r;
    }
    // Nine-sliced wood sprite panel; falls back to a flat color when the sprite is not assigned.
    private Image Wood(string name,Vector2 pos,Vector2 size,Sprite sprite,Color fallback)
    {
        var image=Rect(name,panel,pos,size).gameObject.AddComponent<Image>();
        if(sprite!=null){image.sprite=sprite;image.type=Image.Type.Sliced;image.color=Color.white;}
        else image.color=fallback;
        image.raycastTarget=false;return image;
    }
    private GearPuzzleDisc Disc(string name,Vector2 pos,float size,Color color,float inner)
    {
        // RequireComponent does not reliably add CanvasRenderer from editor scripts; add it explicitly.
        var go=Rect(name,panel,pos,Vector2.one*size).gameObject;
        if(go.GetComponent<CanvasRenderer>()==null) go.AddComponent<CanvasRenderer>();
        var disc=go.AddComponent<GearPuzzleDisc>();
        disc.color=color;disc.innerRatio=inner;disc.raycastTarget=false;return disc;
    }
    private TMP_Text Text(string name,string value,Transform parent,Vector2 pos,Vector2 size,float fs,Color color,TextAlignmentOptions align=TextAlignmentOptions.Center)
    {
        var t=Rect(name,parent,pos,size).gameObject.AddComponent<TextMeshProUGUI>();
        if(font!=null) t.font=font;
        t.text=value;t.fontSize=fs;t.color=color;t.alignment=align;t.raycastTarget=false;
        return t;
    }
    private Button ButtonAt(string name,string label,Vector2 pos,Vector2 size,UnityAction action)
    {
        var image=Wood(name,pos,size,woodButton,new Color(.55f,.40f,.26f));image.raycastTarget=true;
        var b=image.gameObject.AddComponent<Button>();b.targetGraphic=image;b.onClick.AddListener(action);
        var colors=b.colors;colors.highlightedColor=new Color(1f,.95f,.85f);colors.pressedColor=new Color(.78f,.68f,.52f);b.colors=colors;
        Text("Label",label,image.transform,Vector2.zero,size,19,DarkBrown);
        return b;
    }
    // Runtime listeners are not serialized by editor-generated views.
    private void Start()
    {
        if(panel==null)return;
        var close=panel.Find("Close")?.GetComponent<Button>();
        if(close!=null) {close.onClick.RemoveListener(Close);close.onClick.AddListener(Close);}
        if(resetButton!=null) {resetButton.onClick.RemoveListener(ResetPuzzle);resetButton.onClick.AddListener(ResetPuzzle);}
    }
}
