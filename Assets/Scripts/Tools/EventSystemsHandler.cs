using UnityEngine;
using UnityEngine.EventSystems;

public class EventSystemsHandler : MonoBehaviour, IDragHandler, IEndDragHandler
{
    public delegate void DragHandler(PointerEventData eventData, bool isDrag);
    public event DragHandler DragHandlerDelegate;

    // 當滑鼠在 RawImage 上拖拽時會觸發
    public void OnDrag(PointerEventData eventData)
    {
        DragHandlerDelegate?.Invoke(eventData, true);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        DragHandlerDelegate?.Invoke(eventData, false);
    }
}
