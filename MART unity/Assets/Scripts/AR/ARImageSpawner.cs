using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARImageSpawner : MonoBehaviour
{
    [Serializable]
    public struct ImagePrefab
    {
        [Tooltip("Имя эталонной картинки в библиотеке (XRReferenceImageLibrary), например DSC_8069")]
        public string imageName;

        [Tooltip("Модель/префаб, который появится на этой картинке")]
        public GameObject prefab;
    }

    [Header("Модель по умолчанию (если для картинки нет своей)")]
    [SerializeField] private GameObject prefab;

    [Header("Привязка: картинка -> своя модель")]
    [SerializeField] private ImagePrefab[] imagePrefabs;

    [SerializeField] private string selectableTag = "Placeable";
    [SerializeField] private int interactiveLayer = 6;

    [Header("Масштаб модели относительно размера картинки (1 = во всю ширину маркера)")]
    [SerializeField] private float modelToImageScale = 1f;

    [Header("Доворот модели вокруг вертикали, градусы (0=как есть к камере, 90=боком, 180=лицом)")]
    [SerializeField] private float modelYawOffsetDegrees = 135f;

    private const string ViewedExhibitKeyPrefix = "ar.exhibit.viewed.";

    private ARTrackedImageManager trackedImageManager;

    private readonly Dictionary<string, GameObject> prefabByImageName = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> spawnedObjects = new Dictionary<string, GameObject>();
    private readonly HashSet<string> viewedExhibitIds = new HashSet<string>();
    private readonly HashSet<string> pendingExhibitIds = new HashSet<string>();

    public event Action<string> CurrentExhibitChanged;
    public event Action<string, bool> ExhibitViewedStateChanged;

    public string CurrentExhibitId { get; private set; }

    void Awake()
    {
        trackedImageManager = GetComponent<ARTrackedImageManager>();
        if (trackedImageManager == null)
        {
            Debug.LogError("[AR] ARTrackedImageManager НЕ найден на этом объекте — image tracking работать не будет.");
        }
        BuildPrefabLookup();
        LoadViewedExhibitsFromCache();
    }

    private void BuildPrefabLookup()
    {
        prefabByImageName.Clear();

        if (imagePrefabs == null)
        {
            return;
        }

        foreach (var entry in imagePrefabs)
        {
            if (string.IsNullOrWhiteSpace(entry.imageName) || entry.prefab == null)
            {
                continue;
            }

            prefabByImageName[entry.imageName] = entry.prefab;
        }
    }

    private GameObject ResolvePrefab(string imageName)
    {
        if (!string.IsNullOrWhiteSpace(imageName) && prefabByImageName.TryGetValue(imageName, out GameObject mapped))
        {
            return mapped;
        }

        return prefab;
    }

    void Start()
    {
        int libCount = (trackedImageManager != null && trackedImageManager.referenceLibrary != null)
            ? trackedImageManager.referenceLibrary.count
            : -1;

        Debug.Log($"[AR] Spawner Start. managerEnabled={(trackedImageManager != null && trackedImageManager.enabled)}, " +
                  $"картинок в библиотеке={libCount}, маппингов модель->картинка={prefabByImageName.Count}, " +
                  $"дефолтный префаб={(prefab != null ? prefab.name : "НЕТ")}");

        foreach (var kv in prefabByImageName)
        {
            Debug.Log($"[AR]   маппинг: '{kv.Key}' -> '{(kv.Value != null ? kv.Value.name : "null")}'");
        }

        if (trackedImageManager != null && trackedImageManager.referenceLibrary != null)
        {
            for (int i = 0; i < libCount; i++)
            {
                Debug.Log($"[AR]   в библиотеке маркер: '{trackedImageManager.referenceLibrary[i].name}'");
            }
        }

        RequestViewedExhibitsFromBackend();
    }

    private float nextStatusLogTime;

    void Update()
    {
        if (Time.time < nextStatusLogTime)
        {
            return;
        }
        nextStatusLogTime = Time.time + 3f;

        if (trackedImageManager == null)
        {
            Debug.Log("[AR] status: менеджер == null");
            return;
        }

        var subsystem = trackedImageManager.subsystem;
        string subsystemState = subsystem == null ? "subsystem=NULL (image tracking не поддерживается/не включён)"
                                                   : $"subsystem.running={subsystem.running}";
        Debug.Log($"[AR] status: {subsystemState}, сейчас отслеживается маркеров={trackedImageManager.trackables.count}");
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
        Debug.Log($"[AR] trackedImagesChanged: added={args.added.Count}, updated={args.updated.Count}, removed={args.removed.Count}");

        foreach (var image in args.added)
        {
            Spawn(image);
            HandleTrackingState(image);
        }

        foreach (var image in args.updated)
        {
            UpdateObject(image);
            HandleTrackingState(image);
        }

        foreach (var image in args.removed)
        {
            if (CurrentExhibitId == image.referenceImage.name)
            {
                SetCurrentExhibit(FindFirstTrackedExhibitId());
            }

            if (spawnedObjects.ContainsKey(image.referenceImage.name))
            {
                Destroy(spawnedObjects[image.referenceImage.name]);
                spawnedObjects.Remove(image.referenceImage.name);
            }
        }
    }

    void Spawn(ARTrackedImage image)
    {
        string imageName = image.referenceImage.name;

        // На случай повторного добавления той же картинки — не плодим дубли.
        if (spawnedObjects.TryGetValue(imageName, out GameObject existing) && existing != null)
        {
            existing.SetActive(true);
            return;
        }

        GameObject prefabToSpawn = ResolvePrefab(imageName);
        if (prefabToSpawn == null)
        {
            Debug.LogWarning($"[AR] Для картинки '{imageName}' не задан префаб (и нет дефолтного). Ставить нечего.");
            return;
        }

        GameObject obj = Instantiate(prefabToSpawn, image.transform);
        obj.SetActive(true);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;

        FitToImage(obj, image);
        OrientUpright(obj);
        MakeInteractive(obj);

        spawnedObjects[imageName] = obj;
        Debug.Log($"[AR] Заспавнил модель '{prefabToSpawn.name}' на картинке '{imageName}'.");
    }

    // Ставит модель вертикально (по миру) лицом к камере и слегка выносит её перед картиной,
    // чтобы она не «утопала» в стене. Если камеры нет — оставляет как есть (видимости это не ломает).
    void OrientUpright(GameObject obj)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return;
        }

        Vector3 toCamera = cam.transform.position - obj.transform.position;
        toCamera.y = 0f;
        if (toCamera.sqrMagnitude > 0.0001f)
        {
            // Сначала разворачиваем модель к камере, затем доворачиваем вокруг вертикали
            // на заданный угол — чтобы показать нужный ракурс (спереди-боком, а не задом).
            obj.transform.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up)
                                     * Quaternion.Euler(0f, modelYawOffsetDegrees, 0f);
            obj.transform.position += toCamera.normalized * 0.1f;
        }
    }

    void UpdateObject(ARTrackedImage image)
    {
        if (spawnedObjects.TryGetValue(image.referenceImage.name, out GameObject obj))
        {
            obj.SetActive(image.trackingState == TrackingState.Tracking);
        }
    }

    void FitToImage(GameObject obj, ARTrackedImage image)
    {
        if (obj == null || modelToImageScale <= 0f)
        {
            return;
        }

        var renderer = obj.GetComponentInChildren<Renderer>(true);
        if (renderer == null)
        {
            Debug.LogWarning($"[AR] У модели '{obj.name}' нет Renderer — нечего масштабировать/показывать.");
            return;
        }

        // Габариты модели в мире уже с учётом её собственного масштаба (у bull.prefab он ~21847).
        Vector3 worldSize = renderer.bounds.size;
        float maxDimension = Mathf.Max(worldSize.x, worldSize.y, worldSize.z);
        if (maxDimension <= Mathf.Epsilon)
        {
            Debug.LogWarning($"[AR] Не удалось вычислить габариты модели '{obj.name}' — масштаб не меняю.");
            return;
        }

        // Физический размер маркера в метрах (например, 1 м для DSC_8069).
        float imageSize = Mathf.Max(image.size.x, image.size.y);
        if (imageSize <= Mathf.Epsilon)
        {
            imageSize = 1f;
        }

        float factor = (imageSize * modelToImageScale) / maxDimension;
        obj.transform.localScale *= factor;
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

    public bool IsExhibitViewed(string exhibitId)
    {
        return !string.IsNullOrWhiteSpace(exhibitId) && viewedExhibitIds.Contains(exhibitId);
    }

    private void HandleTrackingState(ARTrackedImage image)
    {
        if (image == null)
        {
            return;
        }

        string exhibitId = image.referenceImage.name;
        bool isTracking = image.trackingState == TrackingState.Tracking;

        if (isTracking)
        {
            SetCurrentExhibit(exhibitId);
            EnsureExhibitReward(exhibitId);
            return;
        }

        if (CurrentExhibitId == exhibitId)
        {
            SetCurrentExhibit(FindFirstTrackedExhibitId());
        }
    }

    private string FindFirstTrackedExhibitId()
    {
        foreach (var pair in spawnedObjects)
        {
            if (pair.Value != null && pair.Value.activeInHierarchy)
            {
                return pair.Key;
            }
        }

        return null;
    }

    private void SetCurrentExhibit(string exhibitId)
    {
        if (CurrentExhibitId == exhibitId)
        {
            return;
        }

        CurrentExhibitId = exhibitId;
        CurrentExhibitChanged?.Invoke(CurrentExhibitId);
    }

    private void RequestViewedExhibitsFromBackend()
    {
        if (BackendManager.instance == null || !BackendManager.instance.HasAuthorizedSession)
        {
            return;
        }

        BackendManager.instance.LoadViewedExhibits(result =>
        {
            if (result == null || !result.Success || result.Data?.items == null)
            {
                return;
            }

            for (int i = 0; i < result.Data.items.Length; i++)
            {
                MarkExhibitViewed(result.Data.items[i], false);
            }

            NotifyCurrentExhibitViewedState();
        });
    }

    private void EnsureExhibitReward(string exhibitId)
    {
        if (string.IsNullOrWhiteSpace(exhibitId) || viewedExhibitIds.Contains(exhibitId) || pendingExhibitIds.Contains(exhibitId))
        {
            return;
        }

        if (BackendManager.instance == null || !BackendManager.instance.HasAuthorizedSession)
        {
            return;
        }

        pendingExhibitIds.Add(exhibitId);
        BackendManager.instance.RecordExhibitView(exhibitId, result =>
        {
            pendingExhibitIds.Remove(exhibitId);

            if (result == null || !result.Success || result.Data == null)
            {
                return;
            }

            if (result.Data.applied || result.Data.awardedXp == 0)
            {
                MarkExhibitViewed(exhibitId, true);
            }
        });
    }

    private void MarkExhibitViewed(string exhibitId, bool notify)
    {
        if (string.IsNullOrWhiteSpace(exhibitId))
        {
            return;
        }

        bool added = viewedExhibitIds.Add(exhibitId);
        PlayerPrefs.SetInt(GetViewedExhibitKey(exhibitId), 1);
        PlayerPrefs.Save();

        if (notify || added)
        {
            ExhibitViewedStateChanged?.Invoke(exhibitId, true);
            NotifyCurrentExhibitViewedState();
        }
    }

    private void NotifyCurrentExhibitViewedState()
    {
        if (string.IsNullOrWhiteSpace(CurrentExhibitId))
        {
            return;
        }

        ExhibitViewedStateChanged?.Invoke(CurrentExhibitId, IsExhibitViewed(CurrentExhibitId));
    }

    private void LoadViewedExhibitsFromCache()
    {
        if (trackedImageManager == null || trackedImageManager.referenceLibrary == null)
        {
            return;
        }

        int referenceImageCount = trackedImageManager.referenceLibrary.count;
        for (int i = 0; i < referenceImageCount; i++)
        {
            XRReferenceImage referenceImage = trackedImageManager.referenceLibrary[i];
            string exhibitId = referenceImage.name;
            if (!string.IsNullOrWhiteSpace(exhibitId) && PlayerPrefs.GetInt(GetViewedExhibitKey(exhibitId), 0) == 1)
            {
                viewedExhibitIds.Add(exhibitId);
            }
        }
    }

    private string GetViewedExhibitKey(string exhibitId)
    {
        return ViewedExhibitKeyPrefix + exhibitId;
    }
}
