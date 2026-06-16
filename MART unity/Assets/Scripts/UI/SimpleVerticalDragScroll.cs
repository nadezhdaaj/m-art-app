using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Simple vertical scroll: drag finger/mouse over the viewport moves the content list.
/// No ScrollRect required.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public class SimpleVerticalDragScroll : MonoBehaviour, IDragHandler, IScrollHandler
{
    [SerializeField] private RectTransform viewport;
    [SerializeField] private RectTransform content;

    private bool isDragging;
    private Vector2 lastPointerPosition;
    private Camera eventCamera;

    private void Awake()
    {
        Image image = GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.001f);
        image.raycastTarget = true;

        if (viewport == null)
        {
            viewport = transform as RectTransform;
        }

        CacheEventCamera();
    }

    public void Setup(RectTransform viewportRect, RectTransform contentRect)
    {
        viewport = viewportRect != null ? viewportRect : transform as RectTransform;
        content = contentRect;
        CacheEventCamera();
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

    public void OnDrag(PointerEventData eventData)
    {
        ApplyDelta(eventData.delta.y);
    }

    public void OnScroll(PointerEventData eventData)
    {
        ApplyDelta(eventData.scrollDelta.y * 40f);
    }

    private void Update()
    {
        if (viewport == null || content == null)
        {
            return;
        }

        if (WasPointerPressedThisFrame())
        {
            Vector2 screenPosition = GetPointerScreenPosition();
            isDragging = IsInsideViewport(screenPosition);
            lastPointerPosition = screenPosition;
            return;
        }

        if (WasPointerReleasedThisFrame())
        {
            isDragging = false;
            return;
        }

        if (!isDragging || !IsPointerHeld())
        {
            return;
        }

        Vector2 currentPosition = GetPointerScreenPosition();
        ApplyDelta(currentPosition.y - lastPointerPosition.y);
        lastPointerPosition = currentPosition;
    }

    private void ApplyDelta(float deltaY)
    {
        if (content == null || viewport == null || Mathf.Abs(deltaY) < 0.01f)
        {
            return;
        }

        float maxScroll = GetMaxScroll();
        if (maxScroll <= 0f)
        {
            return;
        }

        float newY = Mathf.Clamp(content.anchoredPosition.y + deltaY, 0f, maxScroll);
        content.anchoredPosition = new Vector2(content.anchoredPosition.x, newY);
    }

    private float GetMaxScroll()
    {
        Canvas.ForceUpdateCanvases();
        return Mathf.Max(0f, content.rect.height - viewport.rect.height);
    }

    private void CacheEventCamera()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            eventCamera = canvas.worldCamera;
        }
        else
        {
            eventCamera = null;
        }
    }

    private bool IsInsideViewport(Vector2 screenPosition)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(viewport, screenPosition, eventCamera);
    }

    private static bool WasPointerPressedThisFrame()
    {
        if (Input.GetMouseButtonDown(0))
        {
            return true;
        }

        for (int i = 0; i < Input.touchCount; i++)
        {
            if (Input.GetTouch(i).phase == TouchPhase.Began)
            {
                return true;
            }
        }

        return false;
    }

    private static bool WasPointerReleasedThisFrame()
    {
        if (Input.GetMouseButtonUp(0))
        {
            return true;
        }

        for (int i = 0; i < Input.touchCount; i++)
        {
            TouchPhase phase = Input.GetTouch(i).phase;
            if (phase == TouchPhase.Ended || phase == TouchPhase.Canceled)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPointerHeld()
    {
        if (Input.GetMouseButton(0))
        {
            return true;
        }

        for (int i = 0; i < Input.touchCount; i++)
        {
            TouchPhase phase = Input.GetTouch(i).phase;
            if (phase == TouchPhase.Moved || phase == TouchPhase.Stationary || phase == TouchPhase.Began)
            {
                return true;
            }
        }

        return false;
    }

    private static Vector2 GetPointerScreenPosition()
    {
        if (Input.touchCount > 0)
        {
            return Input.GetTouch(0).position;
        }

        return Input.mousePosition;
    }
}
