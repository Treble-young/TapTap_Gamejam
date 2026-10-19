using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GearPuzzlePiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, ICanvasRaycastFilter
{
    public GearPuzzleController owner;
    public int index;
    public float radius;
    public RectTransform rotor;
    public Image artwork;
    [HideInInspector] public Vector2 home;
    [HideInInspector] public int slot = -1;
    public bool IsFixed => index == 0 || index == 6;
    public RectTransform Rect => (RectTransform)transform;

    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(Rect,screenPoint,eventCamera,out var p)
            && p.sqrMagnitude <= Mathf.Pow(Rect.rect.width*.5f,2);
    }
    public void OnBeginDrag(PointerEventData e)
    {
        if(e.button==PointerEventData.InputButton.Left) owner.BeginDrag(this,e);
    }
    public void OnDrag(PointerEventData e) { owner.Drag(this,e); }
    public void OnEndDrag(PointerEventData e) { owner.EndDrag(this,e); }
    public void OnPointerClick(PointerEventData e)
    {
        if(e.button==PointerEventData.InputButton.Right) owner.ReturnToTray(this);
    }
}
