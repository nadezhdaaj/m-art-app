using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARImageSpawner : MonoBehaviour
{
    [SerializeField] private GameObject prefab;

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

        spawnedObjects[image.referenceImage.name] = obj;
    }

    void UpdateObject(ARTrackedImage image)
    {
        if (spawnedObjects.TryGetValue(image.referenceImage.name, out GameObject obj))
        {
            obj.SetActive(image.trackingState == TrackingState.Tracking);
        }
    }
}