using UnityEngine;
using UnityEngine.EventSystems;

public class EventSystemsHandler : MonoBehaviour,IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    // 開始拖曳監聽
    public delegate void BeginDragHandler(PointerEventData eventData);
    public event BeginDragHandler BeginDragHandlerDelegate;

    // 拖曳監聽
    public delegate void DragHandler(PointerEventData eventData, bool isDrag);
    public event DragHandler DragHandlerDelegate;

    // 進入UI監聽
    public delegate void PointerEnterHandle(PointerEventData eventData, bool isEnter);
    public event PointerEnterHandle PointerEnterHandleDelegate;

    /// <summary>
    /// 開始拖曳
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        BeginDragHandlerDelegate?.Invoke(eventData);
    }

    /// <summary>
    /// 拖曳中
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        DragHandlerDelegate?.Invoke(eventData, true);
    }

    /// <summary>
    /// 結束拖曳
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        DragHandlerDelegate?.Invoke(eventData, false);
    }

    /// <summary>
    /// 進入UI
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        PointerEnterHandleDelegate?.Invoke(eventData, true);
    }

    /// <summary>
    /// 離開UI
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        PointerEnterHandleDelegate?.Invoke(eventData, false);
    }
}
