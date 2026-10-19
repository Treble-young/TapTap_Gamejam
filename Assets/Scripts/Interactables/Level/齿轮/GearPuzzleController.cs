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
    public IReadOnlyList<GearPuzzlePiece> Pieces => pieces;

    private void Awake() { if(panel==null) BuildView(); }
    private void OnEnable()
    {
        if(panel==null) BuildView();
        Fit(); RefreshNetwork(false);
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
        if(counter!=null) counter.text=$"传动  {PoweredCount} / 7";
        if(jam) Message("齿轮互相卡住了，尝试移走一枚。");
        else SetStatus();
    }
    private void SetStatus()
    {
        if(status!=null) status.text=isSolved?"动力已传至终点。":"拖动右侧齿轮到轴心，让动力从左上方传到右下方。";
    }
    private void Message(string text)
    {
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
        var shade=backdrop.gameObject.AddComponent<Image>();shade.color=new Color(.025f,.035f,.045f,.98f);
        panel=Rect("GearPuzzlePanel",transform,Vector2.zero,new Vector2(1280,800));
        Text("Title","齿 轮 传 动",panel,new Vector2(-390,340),new Vector2(400,56),34,Gold,TextAlignmentOptions.MidlineLeft);
        Text("Subtitle","让停滞的机械重新运转",panel,new Vector2(-390,298),new Vector2(400,28),16,new Color(.62f,.66f,.67f),TextAlignmentOptions.MidlineLeft);
        ButtonAt("Close","返回  ×",new Vector2(515,330),new Vector2(150,48),Close);
        Box("BoardFrame",panel,new Vector2(-160,-5),new Vector2(858,568),new Color(.37f,.33f,.24f));
        Box("Board",panel,new Vector2(-160,-5),new Vector2(850,560),new Color(.12f,.15f,.16f));
        // Subtle engineering grid, decorative only.
        for(int x=-540;x<=220;x+=40) Box("GridV",panel,new Vector2(x,-5),new Vector2(1,540),new Color(.18f,.21f,.21f,.45f));
        for(int y=-245;y<=235;y+=40) Box("GridH",panel,new Vector2(-160,y),new Vector2(830,1),new Color(.18f,.21f,.21f,.45f));
        Box("Tray",panel,new Vector2(450,-5),new Vector2(290,560),new Color(.085f,.105f,.115f));
        Text("TrayTitle","备 用 齿 轮",panel,new Vector2(450,240),new Vector2(250,35),22,Gold);
        Text("TrayHint","拖动安装 · 右键收回",panel,new Vector2(450,207),new Vector2(260,26),15,new Color(.62f,.66f,.67f));
        for(int i=0;i<GearPuzzleRules.Axles.Length;i++)
        {
            Vector2 p=GearPuzzleRules.BoardOffset+GearPuzzleRules.Axles[i];
            Disc("AxleRing"+i,p,28,new Color(.46f,.49f,.46f),.77f);
            Disc("Axle"+i,p,10,new Color(.055f,.065f,.07f),0);
        }
        Text("Source","动力源",panel,new Vector2(-460,263),new Vector2(120,24),16,Gold);
        Text("Destination","输出端",panel,new Vector2(136,-239),new Vector2(120,24),16,Gold);
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
        counter=Text("PowerCount","",panel,new Vector2(-455,-324),new Vector2(220,40),22,Gold,TextAlignmentOptions.MidlineLeft);
        resetButton=ButtonAt("Reset","重新摆放",new Vector2(450,-324),new Vector2(240,44),ResetPuzzle);
        status=Text("Status","",panel,new Vector2(-85,-367),new Vector2(1010,32),17,new Color(.78f,.81f,.80f));
        success=Rect("Success",panel,new Vector2(-160,0),new Vector2(530,160)).gameObject;
        var successBg=success.AddComponent<Image>();successBg.color=new Color(.06f,.12f,.10f,.97f);
        Text("SuccessTitle","传 动 已 恢 复",success.transform,new Vector2(0,35),new Vector2(490,48),30,Gold);
        Text("SuccessHint","所有齿轮已连通，点击右上角「返回」继续。",success.transform,new Vector2(0,-30),new Vector2(490,45),18,Green);
        success.SetActive(isSolved);
        Fit();RefreshNetwork(false);
    }
    private static RectTransform Rect(string name,Transform parent,Vector2 pos,Vector2 size)
    {
        var go=new GameObject(name,typeof(RectTransform));go.layer=5;
        var r=(RectTransform)go.transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);
        r.anchoredPosition=pos;r.sizeDelta=size;return r;
    }
    private static Image Box(string name,Transform parent,Vector2 pos,Vector2 size,Color color)
    {
        var image=Rect(name,parent,pos,size).gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return image;
    }
    private GearPuzzleDisc Disc(string name,Vector2 pos,float size,Color color,float inner)
    {
        var disc=Rect(name,panel,pos,Vector2.one*size).gameObject.AddComponent<GearPuzzleDisc>();
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
        var image=Box(name,panel,pos,size,new Color(.23f,.27f,.27f));image.raycastTarget=true;
        var b=image.gameObject.AddComponent<Button>();b.targetGraphic=image;b.onClick.AddListener(action);
        var colors=b.colors;colors.highlightedColor=new Color(1,.90f,.65f);colors.pressedColor=new Color(.70f,.63f,.44f);b.colors=colors;
        Text("Label",label,image.transform,Vector2.zero,size,19,new Color(.95f,.91f,.79f));
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
