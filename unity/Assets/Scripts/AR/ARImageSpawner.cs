using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARImageSpawner : MonoBehaviour
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private string selectableTag = "Placeable";
    [SerializeField] private int interactiveLayer = 6;

    private ARTrackedImageManager trackedImageManager;

    private Dictionary<string, GameObject> spawnedObjects = new Dictionary<string, GameObject>();

    void Awake()
    {
        trackedImageManager = GetComponent<ARTrackedImageManager>();
    }

    void OnEnable()
    {
        trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    void OnDisable()
    {
        trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs args)
    {
        foreach (var image in args.added)
        {
            Spawn(image);
        }

        foreach (var image in args.updated)
        {
            UpdateObject(image);
        }

        foreach (var image in args.removed)
        {
            if (spawnedObjects.ContainsKey(image.referenceImage.name))
            {
                Destroy(spawnedObjects[image.referenceImage.name]);
                spawnedObjects.Remove(image.referenceImage.name);
            }
        }
    }

    void Spawn(ARTrackedImage image)
    {
        GameObject obj = Instantiate(prefab, image.transform);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;

        MakeInteractive(obj);

        spawnedObjects[image.referenceImage.name] = obj;
    }

    void UpdateObject(ARTrackedImage image)
    {
        if (spawnedObjects.TryGetValue(image.referenceImage.name, out GameObject obj))
        {
            obj.SetActive(image.trackingState == TrackingState.Tracking);
        }
    }

    void MakeInteractive(GameObject obj)
    {
        if (obj == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(selectableTag))
        {
            obj.tag = selectableTag;
        }

        SetLayerRecursively(obj.transform, interactiveLayer);

        if (obj.GetComponentInChildren<Collider>(true) == null)
        {
            var renderer = obj.GetComponentInChildren<Renderer>(true);
            if (renderer != null)
            {
                var boxCollider = obj.AddComponent<BoxCollider>();
                Bounds bounds = renderer.bounds;
                Vector3 localCenter = obj.transform.InverseTransformPoint(bounds.center);
                Vector3 localSize = obj.transform.InverseTransformVector(bounds.size);

                boxCollider.center = localCenter;
                boxCollider.size = new Vector3(
                    Mathf.Abs(localSize.x),
                    Mathf.Abs(localSize.y),
                    Mathf.Abs(localSize.z));
            }
        }
    }

    void SetLayerRecursively(Transform current, int layer)
    {
        current.gameObject.layer = layer;

        for (int i = 0; i < current.childCount; i++)
        {
            SetLayerRecursively(current.GetChild(i), layer);
        }
    }

    public GameObject GetFirstActiveObject()
    {
        foreach (var spawned in spawnedObjects.Values)
        {
            if (spawned != null && spawned.activeInHierarchy)
            {
                return spawned;
            }
        }

        return null;
    }
}
