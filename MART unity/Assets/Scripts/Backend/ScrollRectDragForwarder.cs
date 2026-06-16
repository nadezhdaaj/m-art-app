using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Forwards drag/scroll gestures to a parent scroll handler so cards remain clickable.
/// </summary>
[DisallowMultipleComponent]
public class ScrollRectDragForwarder : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
{
    private ScrollRect scrollRect;
    private ManualVerticalScrollDriver manualScroll;
    private bool isDraggingScroll;

    public void Initialize(ScrollRect target)
    {
        scrollRect = target;
        manualScroll = null;
    }

    public void Initialize(ManualVerticalScrollDriver target)
    {
        manualScroll = target;
        scrollRect = null;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (manualScroll == null && scrollRect == null)
        {
            return;
        }

        isDraggingScroll = true;

        if (manualScroll != null)
        {
            manualScroll.OnBeginDrag(eventData);
            return;
        }

        ExecuteEvents.Execute(scrollRect.gameObject, eventData, ExecuteEvents.beginDragHandler);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDraggingScroll)
        {
            return;
        }

        if (manualScroll != null)
        {
            manualScroll.OnDrag(eventData);
            return;
        }

        if (scrollRect == null)
        {
            return;
        }

        ExecuteEvents.Execute(scrollRect.gameObject, eventData, ExecuteEvents.dragHandler);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDraggingScroll)
        {
            return;
        }

        isDraggingScroll = false;

        if (manualScroll != null)
        {
            manualScroll.OnEndDrag(eventData);
            return;
        }

        if (scrollRect == null)
        {
            return;
        }

        ExecuteEvents.Execute(scrollRect.gameObject, eventData, ExecuteEvents.endDragHandler);
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (manualScroll != null)
        {
            manualScroll.OnScroll(eventData);
            return;
        }

        if (scrollRect == null)
        {
            return;
        }

        ExecuteEvents.Execute(scrollRect.gameObject, eventData, ExecuteEvents.scrollHandler);
    }
}
