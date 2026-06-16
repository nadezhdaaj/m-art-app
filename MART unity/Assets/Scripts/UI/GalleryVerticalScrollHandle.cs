using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Drag the scrollbar handle along the track to scroll gallery content vertically.
/// </summary>
[DisallowMultipleComponent]
public class GalleryVerticalScrollHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private RectTransform viewport;
    [SerializeField] private RectTransform content;
    [SerializeField] private RectTransform track;
    [SerializeField] private RectTransform handle;
    [SerializeField] private float trackPaddingTop = 4f;
    [SerializeField] private float trackPaddingBottom = 4f;
    [SerializeField] private float handleInsetFromRight = 8f;

    public void Setup(RectTransform viewportRect, RectTransform contentRect, RectTransform handleRect, RectTransform trackRect)
    {
        viewport = viewportRect;
        content = contentRect;
        handle = handleRect != null ? handleRect : transform as RectTransform;
        track = trackRect != null ? trackRect : handle != null ? handle.parent as RectTransform : null;

        ConfigureHandleAnchors();
        SyncHandleFromContent();
        UpdateHandleVisibility();
    }

    public void SyncAfterContentResize()
    {
        ConfigureHandleAnchors();
        SyncHandleFromContent();
        UpdateHandleVisibility();
    }

    public void ResetToTop()
    {
        if (content == null)
        {
            return;
        }

        content.anchoredPosition = new Vector2(content.anchoredPosition.x, 0f);
        SyncHandleFromContent();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!CanScroll() || handle == null || content == null)
        {
            return;
        }

        float trackTravel = GetTrackTravel();
        if (trackTravel <= 0.01f)
        {
            return;
        }

        float newHandleY = Mathf.Clamp(handle.anchoredPosition.y + eventData.delta.y, GetHandleMinY(), GetHandleMaxY());
        handle.anchoredPosition = new Vector2(handle.anchoredPosition.x, newHandleY);

        float scrollT = (GetHandleMaxY() - newHandleY) / trackTravel;
        float maxScroll = GetMaxScroll();
        content.anchoredPosition = new Vector2(content.anchoredPosition.x, scrollT * maxScroll);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
    }

    private void ConfigureHandleAnchors()
    {
        if (handle == null || track == null)
        {
            return;
        }

        if (handle.parent != track)
        {
            handle.SetParent(track, false);
        }

        handle.anchorMin = new Vector2(1f, 1f);
        handle.anchorMax = new Vector2(1f, 1f);
        handle.pivot = new Vector2(1f, 1f);
        handle.anchoredPosition = new Vector2(-handleInsetFromRight, GetHandleMaxY());
    }

    private void SyncHandleFromContent()
    {
        if (handle == null || content == null || !CanScroll())
        {
            return;
        }

        float maxScroll = GetMaxScroll();
        float trackTravel = GetTrackTravel();
        if (trackTravel <= 0.01f || maxScroll <= 0.01f)
        {
            handle.anchoredPosition = new Vector2(-handleInsetFromRight, GetHandleMaxY());
            return;
        }

        float scrollT = Mathf.Clamp01(content.anchoredPosition.y / maxScroll);
        float handleY = GetHandleMaxY() - scrollT * trackTravel;
        handle.anchoredPosition = new Vector2(-handleInsetFromRight, handleY);
    }

    private void UpdateHandleVisibility()
    {
        if (handle == null)
        {
            return;
        }

        handle.gameObject.SetActive(CanScroll());
    }

    private bool CanScroll()
    {
        return viewport != null && content != null && track != null && GetMaxScroll() > 0.01f;
    }

    private float GetMaxScroll()
    {
        Canvas.ForceUpdateCanvases();
        return Mathf.Max(0f, content.rect.height - viewport.rect.height);
    }

    private float GetTrackTravel()
    {
        if (track == null || handle == null)
        {
            return 0f;
        }

        Canvas.ForceUpdateCanvases();
        float padding = trackPaddingTop + trackPaddingBottom;
        return Mathf.Max(0f, track.rect.height - handle.rect.height - padding);
    }

    private float GetHandleMaxY()
    {
        return -trackPaddingTop;
    }

    private float GetHandleMinY()
    {
        return GetHandleMaxY() - GetTrackTravel();
    }
}
