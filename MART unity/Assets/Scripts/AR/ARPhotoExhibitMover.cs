using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// One-finger drag for AR photo exhibits (camera-local X/Y). Added to every spawned exhibit.
/// </summary>
[DisallowMultipleComponent]
public class ARPhotoExhibitMover : MonoBehaviour
{
    [SerializeField] private Camera arCamera;
    [SerializeField] private float dragSensitivity = 1.35f;
    [SerializeField] private float maxOffsetX = 0.45f;
    [SerializeField] private float maxOffsetY = 0.45f;

    private bool isDragging;
    private int activePointerId = -2;
    private Vector2 lastPointerPosition;

    public void Bind(Camera camera)
    {
        arCamera = camera;
    }

    private void Update()
    {
        if (arCamera == null || !isActiveAndEnabled)
        {
            return;
        }

        if (Input.touchCount > 0)
        {
            HandleTouchInput();
            return;
        }

        HandleMouseInput();
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount > 1)
        {
            EndDrag();
            return;
        }

        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            TryBeginDrag(touch.position, touch.fingerId);
            return;
        }

        if (!isDragging || touch.fingerId != activePointerId)
        {
            return;
        }

        if (touch.phase == TouchPhase.Moved)
        {
            ApplyDragDelta(touch.deltaPosition);
        }
        else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
        {
            EndDrag();
        }
    }

    private void HandleMouseInput()
    {
        Vector2 mousePosition = Input.mousePosition;

        if (Input.GetMouseButtonDown(0))
        {
            TryBeginDrag(mousePosition, -1);
            lastPointerPosition = mousePosition;
            return;
        }

        if (!isDragging || activePointerId != -1)
        {
            return;
        }

        if (Input.GetMouseButton(0))
        {
            Vector2 delta = mousePosition - lastPointerPosition;
            lastPointerPosition = mousePosition;
            if (delta.sqrMagnitude > 0f)
            {
                ApplyDragDelta(delta);
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            EndDrag();
        }
    }

    private void TryBeginDrag(Vector2 screenPosition, int pointerId)
    {
        if (IsPointerOverUi(pointerId))
        {
            return;
        }

        if (!HitThisExhibit(screenPosition))
        {
            return;
        }

        isDragging = true;
        activePointerId = pointerId;
        lastPointerPosition = screenPosition;
    }

    private void EndDrag()
    {
        isDragging = false;
        activePointerId = -2;
    }

    private bool HitThisExhibit(Vector2 screenPosition)
    {
        if (arCamera == null)
        {
            return false;
        }

        Ray ray = arCamera.ScreenPointToRay(screenPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 50f);
        for (int i = 0; i < hits.Length; i++)
        {
            Transform hitTransform = hits[i].transform;
            if (hitTransform == transform || hitTransform.IsChildOf(transform))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPointerOverUi(int pointerId)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        if (pointerId >= 0)
        {
            return EventSystem.current.IsPointerOverGameObject(pointerId);
        }

        return EventSystem.current.IsPointerOverGameObject();
    }

    private void ApplyDragDelta(Vector2 screenDelta)
    {
        float depth = Mathf.Max(0.2f, transform.localPosition.z);
        float unitsPerPixel = (depth / Screen.height) * dragSensitivity;

        Vector3 localPosition = transform.localPosition;
        localPosition.x = Mathf.Clamp(
            localPosition.x + (screenDelta.x * unitsPerPixel),
            -maxOffsetX,
            maxOffsetX);
        localPosition.y = Mathf.Clamp(
            localPosition.y + (screenDelta.y * unitsPerPixel),
            -maxOffsetY,
            maxOffsetY);
        transform.localPosition = localPosition;
    }
}
