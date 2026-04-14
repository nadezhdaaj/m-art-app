using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARInteractionController : MonoBehaviour
{
    [SerializeField] private Camera arCamera;
    [SerializeField] private LayerMask modelLayer; // слой моделей
    [SerializeField] private float rotationSpeed = 0.2f;
    [SerializeField] private float minScale = 0.2f;
    [SerializeField] private float maxScale = 3f;

    private GameObject selectedObject;
    private static List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private ARRaycastManager raycastManager;

    private float initialDistance;
    private Vector3 initialScale;

    private float initialAngle;
    private Quaternion initialRotation;

    void Awake()
    {
        raycastManager = FindObjectOfType<ARRaycastManager>();
        if (arCamera == null) arCamera = Camera.main;
    }

    void Update()
    {
        if (Input.touchCount == 0)
            return;

        if (Input.touchCount == 1)
        {
            HandleSingleTouch(Input.GetTouch(0));
        }
        else if (Input.touchCount == 2 && selectedObject != null)
        {
            HandleMultiTouch(Input.GetTouch(0), Input.GetTouch(1));
        }
    }

    void HandleSingleTouch(Touch touch)
    {
        if (touch.phase == TouchPhase.Began)
        {
            Ray ray = arCamera.ScreenPointToRay(touch.position);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, modelLayer))
            {
                selectedObject = hit.transform.gameObject;
            }
        }
        else if (touch.phase == TouchPhase.Moved && selectedObject != null)
        {
            // перемещаем модель по плоскости
           /* if (raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
            {
                Pose pose = hits[0].pose;
                selectedObject.transform.position = pose.position;
            }*/
        }
        else if (touch.phase == TouchPhase.Ended && Input.touchCount < 2)
        {
            selectedObject = null;
        }
    }

    void HandleMultiTouch(Touch touch1, Touch touch2)
    {
        float currentDistance = Vector2.Distance(touch1.position, touch2.position);

        if (touch2.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began)
        {
            initialDistance = currentDistance;
            initialScale = selectedObject.transform.localScale;

            initialAngle = GetTouchAngle(touch1.position, touch2.position);
            initialRotation = selectedObject.transform.rotation;
        }
        else
        {
            // масштабирование
            float scaleFactor = currentDistance / initialDistance;
            Vector3 newScale = initialScale * scaleFactor;
            newScale = ClampScale(newScale);
            selectedObject.transform.localScale = newScale;

            // поворот
            float currentAngle = GetTouchAngle(touch1.position, touch2.position);
            float angleDifference = currentAngle - initialAngle;
            selectedObject.transform.rotation =
                initialRotation * Quaternion.Euler(0, -angleDifference * rotationSpeed, 0);
        }
    }

    float GetTouchAngle(Vector2 p1, Vector2 p2)
    {
        Vector2 direction = p2 - p1;
        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

    Vector3 ClampScale(Vector3 scale)
    {
        scale.x = Mathf.Clamp(scale.x, minScale, maxScale);
        scale.y = Mathf.Clamp(scale.y, minScale, maxScale);
        scale.z = Mathf.Clamp(scale.z, minScale, maxScale);
        return scale;
    }
}