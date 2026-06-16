using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Moves scroll content vertically without Unity ScrollRect (more reliable with UI buttons).
/// </summary>
[DisallowMultipleComponent]
public class ManualVerticalScrollDriver : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
{
    private RectTransform viewport;
    private RectTransform content;

    public void Configure(RectTransform viewportRect, RectTransform contentRect)
    {
        viewport = viewportRect;
        content = contentRect;
        ResetToTop();
    }

    public void ResetToTop()
    {
        if (content == null)
        {
            return;
        }

        content.anchoredPosition = new Vector2(content.anchoredPosition.x, 0f);
    }

    public void ApplyScrollDelta(float delta)
    {
        if (content == null || viewport == null || Mathf.Abs(delta) < 0.01f)
        {
            return;
        }

        float newY = content.anchoredPosition.y + delta;
        content.anchoredPosition = new Vector2(content.anchoredPosition.x, Mathf.Clamp(newY, 0f, GetMaxScroll()));
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
    }

    public void OnDrag(PointerEventData eventData)
    {
        ApplyScrollDelta(eventData.delta.y);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
    }

    public void OnScroll(PointerEventData eventData)
    {
        ApplyScrollDelta(eventData.scrollDelta.y * 40f);
    }

    public float GetMaxScroll()
    {
        if (content == null || viewport == null)
        {
            return 0f;
        }

        Canvas.ForceUpdateCanvases();
        return Mathf.Max(0f, content.rect.height - viewport.rect.height);
    }

    public bool CanScroll()
    {
        return GetMaxScroll() > 1f;
    }
}
