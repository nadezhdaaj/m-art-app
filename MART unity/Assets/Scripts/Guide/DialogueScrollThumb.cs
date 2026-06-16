using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Небольшой ползунок в правом верхнем углу. Едет вниз при прокрутке диалога.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class DialogueScrollThumb : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform thumb;
    [SerializeField] private float edgePadding = 10f;

    private RectTransform scrollArea;
    private bool dragging;
    private float dragStartNormalized;
    private float dragStartPointerY;

    public void Configure(ScrollRect scroll, RectTransform thumbRect, float padding)
    {
        Unsubscribe();

        scrollRect = scroll;
        thumb = thumbRect != null ? thumbRect : transform as RectTransform;
        edgePadding = padding;
        scrollArea = scroll != null && scroll.viewport != null
            ? scroll.viewport
            : scroll != null ? scroll.transform as RectTransform : null;

        Subscribe();
        SyncFromScroll();
    }

    private void OnEnable() => Subscribe();

    private void OnDisable()
    {
        Unsubscribe();
        dragging = false;
    }

    private void LateUpdate()
    {
        if (!dragging && scrollRect != null)
            SyncFromScroll();
    }

    private void Subscribe()
    {
        if (scrollRect == null)
            return;

        scrollRect.onValueChanged.RemoveListener(OnScrollChanged);
        scrollRect.onValueChanged.AddListener(OnScrollChanged);
    }

    private void Unsubscribe()
    {
        if (scrollRect == null)
            return;

        scrollRect.onValueChanged.RemoveListener(OnScrollChanged);
    }

    private void OnScrollChanged(Vector2 _)
    {
        if (!dragging)
            SyncFromScroll();
    }

    public void SyncFromScroll()
    {
        if (scrollRect == null || thumb == null || scrollArea == null)
            return;

        float travel = GetTravel();
        if (travel <= 1f)
            return;

        float t = 1f - scrollRect.verticalNormalizedPosition;
        PlaceThumb(Mathf.Clamp01(t));
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (scrollRect == null)
            return;

        dragging = true;
        scrollRect.StopMovement();
        dragStartNormalized = scrollRect.verticalNormalizedPosition;

        if (!TryGetLocalYInScrollArea(eventData.pressPosition, eventData, out dragStartPointerY))
            dragStartPointerY = 0f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (scrollRect == null || scrollArea == null)
            return;

        float travel = GetTravel();
        if (travel <= 1f)
            return;

        if (!TryGetLocalYInScrollArea(eventData.position, eventData, out float localY))
            return;

        scrollRect.StopMovement();
        float deltaY = localY - dragStartPointerY;
        float normalized = Mathf.Clamp01(dragStartNormalized + deltaY / travel);
        scrollRect.verticalNormalizedPosition = normalized;

        PlaceThumb(1f - normalized);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        dragging = false;
        SyncFromScroll();
    }

    private void PlaceThumb(float t)
    {
        float travel = GetTravel();
        float y = -edgePadding - t * travel;

        thumb.anchorMin = new Vector2(1f, 1f);
        thumb.anchorMax = new Vector2(1f, 1f);
        thumb.pivot = new Vector2(1f, 1f);
        thumb.anchoredPosition = new Vector2(-edgePadding, y);
    }

    private bool TryGetLocalYInScrollArea(Vector2 screenPoint, PointerEventData eventData, out float localY)
    {
        localY = 0f;
        Camera cam = eventData.pressEventCamera != null
            ? eventData.pressEventCamera
            : eventData.enterEventCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                scrollArea,
                screenPoint,
                cam,
                out Vector2 local))
            return false;

        localY = local.y;
        return true;
    }

    private float GetTravel()
    {
        if (scrollArea == null || thumb == null)
            return 0f;

        return scrollArea.rect.height - thumb.rect.height - edgePadding * 2f;
    }
}
