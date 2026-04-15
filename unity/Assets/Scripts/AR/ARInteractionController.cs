using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARInteractionController : MonoBehaviour
{
    [SerializeField] private Camera arCamera;
    [SerializeField] private LayerMask modelLayer; // Models layer
    [SerializeField] private float rotationSpeed = 0.2f;
    [SerializeField] private float singleFingerRotationSpeed = 0.2f;
    [SerializeField] private float minScaleMultiplier = 0.5f;
    [SerializeField] private float maxScaleMultiplier = 3f;
    [SerializeField] private bool keepSelectionAfterTouch = true;
    [SerializeField] private string selectableTag = "Placeable";

    private GameObject selectedObject;
    private static readonly List<ARRaycastHit> Hits = new();
    private ARRaycastManager raycastManager;
    private ARImageSpawner imageSpawner;

    private float initialDistance;
    private Vector3 initialScale;
    private Vector3 selectedObjectBaseScale;

    private float initialAngle;
    private Quaternion initialRotation;
    private bool isManipulating;

    private void Awake()
    {
        raycastManager = FindObjectOfType<ARRaycastManager>();
        imageSpawner = FindObjectOfType<ARImageSpawner>();
        if (arCamera == null)
        {
            arCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (Input.touchCount == 0)
        {
            return;
        }

        if (Input.touchCount == 1)
        {
            isManipulating = false;
            HandleSingleTouch(Input.GetTouch(0));
        }
        else if (Input.touchCount == 2 && selectedObject != null)
        {
            HandleMultiTouch(Input.GetTouch(0), Input.GetTouch(1));
        }
        else if (Input.touchCount == 2)
        {
            TrySelectFallbackObject();

            if (selectedObject != null)
            {
                HandleMultiTouch(Input.GetTouch(0), Input.GetTouch(1));
            }
        }
        else
        {
            isManipulating = false;
        }
    }

    private void HandleSingleTouch(Touch touch)
    {
        if (touch.phase == TouchPhase.Began)
        {
            SelectObjectAt(touch.position);
        }
        else if (touch.phase == TouchPhase.Moved && selectedObject != null)
        {
            RotateWithSingleFinger(touch);
        }
        else if (touch.phase == TouchPhase.Ended && Input.touchCount < 2 && !keepSelectionAfterTouch)
        {
            selectedObject = null;
        }
    }

    private void SelectObjectAt(Vector2 screenPosition)
    {
        TrySelectFallbackObject();

        if (arCamera == null)
        {
            return;
        }

        Ray ray = arCamera.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            GameObject hitObject = hit.collider.attachedRigidbody != null
                ? hit.collider.attachedRigidbody.gameObject
                : hit.transform.gameObject;

            if (IsSelectable(hitObject))
            {
                SetSelectedObject(hitObject);
                return;
            }

            if (hit.transform.root != null && IsSelectable(hit.transform.root.gameObject))
            {
                SetSelectedObject(hit.transform.root.gameObject);
                return;
            }
        }

        if (!keepSelectionAfterTouch)
        {
            selectedObject = null;
        }
    }

    private void TrySelectFallbackObject()
    {
        if (selectedObject != null)
        {
            return;
        }

        if (imageSpawner == null)
        {
            imageSpawner = FindObjectOfType<ARImageSpawner>();
        }

        if (imageSpawner == null)
        {
            return;
        }

        GameObject fallbackObject = imageSpawner.GetFirstActiveObject();
        if (fallbackObject != null && IsSelectable(fallbackObject))
        {
            SetSelectedObject(fallbackObject);
        }
    }

    private void SetSelectedObject(GameObject obj)
    {
        if (obj == null)
        {
            return;
        }

        if (selectedObject != obj)
        {
            selectedObject = obj;
            selectedObjectBaseScale = obj.transform.localScale;
            isManipulating = false;
        }
    }

    private bool IsSelectable(GameObject obj)
    {
        if (obj == null)
        {
            return false;
        }

        if (((1 << obj.layer) & modelLayer.value) != 0)
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(selectableTag) && obj.CompareTag(selectableTag);
    }

    private void HandleMultiTouch(Touch touch1, Touch touch2)
    {
        float currentDistance = Vector2.Distance(touch1.position, touch2.position);
        if (currentDistance <= 0.001f)
        {
            return;
        }

        if (!isManipulating || touch2.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began)
        {
            initialDistance = currentDistance;
            initialScale = selectedObject.transform.localScale;

            initialAngle = GetTouchAngle(touch1.position, touch2.position);
            initialRotation = selectedObject.transform.rotation;
            isManipulating = true;
            return;
        }

        // Scale
        float scaleFactor = currentDistance / initialDistance;
        Vector3 newScale = initialScale * scaleFactor;
        newScale = ClampScale(newScale);
        selectedObject.transform.localScale = newScale;

        // Rotate
        float currentAngle = GetTouchAngle(touch1.position, touch2.position);
        float angleDifference = currentAngle - initialAngle;
        selectedObject.transform.rotation =
            initialRotation * Quaternion.Euler(0, -angleDifference * rotationSpeed, 0);
    }

    private void RotateWithSingleFinger(Touch touch)
    {
        float rotationDelta = -touch.deltaPosition.x * singleFingerRotationSpeed;
        selectedObject.transform.Rotate(0f, rotationDelta, 0f, Space.World);
    }

    private float GetTouchAngle(Vector2 p1, Vector2 p2)
    {
        Vector2 direction = p2 - p1;
        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

    private Vector3 ClampScale(Vector3 scale)
    {
        Vector3 baseScale = selectedObjectBaseScale == Vector3.zero ? Vector3.one : selectedObjectBaseScale;

        scale.x = Mathf.Clamp(
            scale.x,
            baseScale.x * minScaleMultiplier,
            baseScale.x * maxScaleMultiplier);
        scale.y = Mathf.Clamp(
            scale.y,
            baseScale.y * minScaleMultiplier,
            baseScale.y * maxScaleMultiplier);
        scale.z = Mathf.Clamp(
            scale.z,
            baseScale.z * minScaleMultiplier,
            baseScale.z * maxScaleMultiplier);
        return scale;
    }
}
