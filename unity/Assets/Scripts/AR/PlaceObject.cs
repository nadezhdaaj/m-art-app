using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch;

[RequireComponent(typeof(ARRaycastManager), typeof(ARPlaneManager))]
public class ARPlacementInteraction : MonoBehaviour
{
    [SerializeField] private GameObject prefab;

    private ARRaycastManager aRRaycastManager;
    private ARPlaneManager aRPlaneManager;

    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private GameObject spawnedObject; // ссылка на объект

    private float initialDistance;
    private Vector3 initialScale;

    private float initialAngle;
    private Quaternion initialRotation;

    private void Awake()
    {
        aRRaycastManager = GetComponent<ARRaycastManager>();
        aRPlaneManager = GetComponent<ARPlaneManager>();
    }

    private void OnEnable()
    {
       /* EnhancedTouch.TouchSimulation.Enable();
        EnhancedTouch.EnhancedTouchSupport.Enable();

        EnhancedTouch.Touch.onFingerDown += FingerDown;
        EnhancedTouch.Touch.onFingerMove += FingerMove;*/
    }

   /* private void OnDisable()
    {
        EnhancedTouch.Touch.onFingerDown -= FingerDown;
        EnhancedTouch.Touch.onFingerMove -= FingerMove;

        EnhancedTouch.TouchSimulation.Disable();
        EnhancedTouch.EnhancedTouchSupport.Disable();
    }*/

    //  Создание объекта 
   /* private void FingerDown(EnhancedTouch.Finger finger)
    {
        if (finger.index != 0) return;

        // 🔥 если объект уже есть — ВООБЩЕ не создаём новый
        if (spawnedObject != null)
        {
            Debug.Log("Object already exists!");
            return;
        }

        if (aRRaycastManager.Raycast(
            finger.currentTouch.screenPosition,
            hits,
            TrackableType.PlaneWithinPolygon))
        {
            Pose pose = hits[0].pose;

            spawnedObject = Instantiate(prefab, pose.position, pose.rotation);

            Debug.Log("OBJECT SPAWNED");
        }
    }*/

    //  Перемещение, масштаб, вращение 
   /* private void FingerMove(EnhancedTouch.Finger finger)
    {
        Debug.Log("FINGER MOVE WORKS");

        if (spawnedObject == null) return;

        var activeTouches = EnhancedTouch.Touch.activeTouches;

        // 1 палец → перемещение
        if (activeTouches.Count == 1)
        {
            Vector2 screenPos = activeTouches[0].screenPosition;

            if (aRRaycastManager.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon))
            {
                Pose pose = hits[0].pose;
                spawnedObject.transform.position = pose.position;
            }
        }

        // 2 пальца → масштаб + вращение
        if (activeTouches.Count == 2)
        {
            var t1 = activeTouches[0];
            var t2 = activeTouches[1];

            float currentDistance = Vector2.Distance(t1.screenPosition, t2.screenPosition);

            // Инициализация (когда хотя бы один палец только начался)
            if (t1.phase == UnityEngine.InputSystem.TouchPhase.Began ||
                t2.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                initialDistance = currentDistance;
                initialScale = spawnedObject.transform.localScale;

                initialAngle = GetAngle(t1.screenPosition, t2.screenPosition);
                initialRotation = spawnedObject.transform.rotation;

                return; // важно!
            }

            // защита от деления на 0
            if (initialDistance <= 0.001f) return;

            // Масштаб
            float scaleFactor = currentDistance / initialDistance;
            spawnedObject.transform.localScale = initialScale * scaleFactor;

            // Вращение
            float currentAngle = GetAngle(t1.screenPosition, t2.screenPosition);
            float angleDiff = currentAngle - initialAngle;

            spawnedObject.transform.rotation =
                initialRotation * Quaternion.Euler(0, -angleDiff, 0);
        }
        Debug.Log("TOUCH WORKING");
    }*/

    private float GetAngle(Vector2 p1, Vector2 p2)
    {
        Vector2 dir = p2 - p1;
        return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
    }

    //  Метод для кнопки удаления объекта 
    public void DeleteObject()
    {
        if (spawnedObject != null)
        {
            Destroy(spawnedObject);
            spawnedObject = null;
        }
    }

    //  Получить ссылку на объект
    public GameObject GetSpawnedObject()
    {
        return spawnedObject;
    }

    void Update()
    {
        // СОЗДАНИЕ ОБЪЕКТА
        if (spawnedObject == null && Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                if (aRRaycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
                {
                    Pose pose = hits[0].pose;
                    spawnedObject = Instantiate(prefab, pose.position, pose.rotation);
                }
            }
        }

        if (spawnedObject == null) return;

        // 1 палец → движение
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Moved)
            {
                if (aRRaycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
                {
                    Pose pose = hits[0].pose;
                    spawnedObject.transform.position = pose.position;
                }
            }
        }

        // 2 пальца → масштаб + вращение
        if (Input.touchCount == 2)
        {
            Touch t1 = Input.GetTouch(0);
            Touch t2 = Input.GetTouch(1);

            float currentDistance = Vector2.Distance(t1.position, t2.position);

            if (t1.phase == TouchPhase.Began || t2.phase == TouchPhase.Began)
            {
                initialDistance = currentDistance;
                initialScale = spawnedObject.transform.localScale;

                initialAngle = GetAngle(t1.position, t2.position);
                initialRotation = spawnedObject.transform.rotation;
                return;
            }

            if (initialDistance <= 0.001f) return;

            float scaleFactor = currentDistance / initialDistance;
            spawnedObject.transform.localScale = initialScale * scaleFactor;

            float currentAngle = GetAngle(t1.position, t2.position);
            float angleDiff = currentAngle - initialAngle;

            spawnedObject.transform.rotation =
                initialRotation * Quaternion.Euler(0, -angleDiff, 0);
        }
    }
}