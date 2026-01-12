using UnityEngine;
using UnityEngine.EventSystems;

public class EventSystemsHandler : MonoBehaviour,IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public delegate void BeginDragHandler(PointerEventData eventData);
    public event BeginDragHandler BeginDragHandlerDelegate;

    public delegate void DragHandler(PointerEventData eventData, bool isDrag);
    public event DragHandler DragHandlerDelegate;

    public void OnBeginDrag(PointerEventData eventData)
    {
        BeginDragHandlerDelegate?.Invoke(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        DragHandlerDelegate?.Invoke(eventData, true);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        DragHandlerDelegate?.Invoke(eventData, false);
    }
}
