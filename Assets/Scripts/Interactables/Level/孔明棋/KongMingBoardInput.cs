using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Raycast surface for the KongMing peg board; forward clicks to the minigame.</summary>
public class KongMingBoardInput : MonoBehaviour, IPointerClickHandler, IPointerMoveHandler, IPointerExitHandler
{
    public KongMingMinigameUI owner;
    public RectTransform boardRect;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (owner != null)
            owner.HandleBoardClick(eventData.position, eventData.pressEventCamera);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (owner != null)
            owner.HandleBoardHover(eventData.position, eventData.pressEventCamera);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (owner != null)
            owner.ClearBoardHover();
    }
}
