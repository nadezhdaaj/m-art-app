using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Uses the scene "AR photos" panel: icon, scroll through, and AR placement.
/// Assign exhibit prefabs in the Inspector, or primitives are used as fallback.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Museum AR/AR Photo Selection UI")]
public class ARPhotoSelectionUI : MonoBehaviour
{
    private const string IconObjectName = "icon";
    private const string ScrollButtonName = "scroll through button";
    private const string PhotoButtonName = "photo";
    private const string IconPreviewChildName = "IconModelPreview";
    private const string SaveButtonName = "Save";
    private const string BackToArButtonName = "Back to AR";
    private const string CancellationButtonName = "cancellation";
    private const string PhotoSelectionPanelName = "Photo selection";

    [Header("Exhibits (drag your test prefab here)")]
    [SerializeField] private GameObject[] exhibitPrefabs;

    [Header("AR placement")]
    [SerializeField] private float spawnDistance = 1.2f;
    [SerializeField] private float arExhibitMaxSize = 0.45f;
    [SerializeField] private float iconPreviewMaxSize = 0.55f;

    private readonly List<GameObject> resolvedPrefabs = new List<GameObject>();
    private readonly List<string> exhibitIds = new List<string>();

    private Button iconButton;
    private Button scrollButton;
    private Button photoButton;
    private Image iconFrameImage;
    private RawImage iconPreviewRawImage;
    private Image iconPreviewSilhouetteImage;
    private ARPhotoIconPreview iconPreview;
    private ARPhotoExhibitPlacer placer;
    private ARPhotoCapture photoCapture;

    private GameObject saveButtonObject;
    private GameObject backToArButtonObject;
    private GameObject cancellationButtonObject;
    private GameObject photoSelectionPanelObject;
    private GameObject iconObjectReference;
    private GameObject scrollButtonObjectReference;
    private GameObject photoButtonObjectReference;

    private Button saveButton;
    private Button cancellationButton;

    private int currentIndex;
    private bool isArExhibitVisible;
    private bool photoModeInitialized;

    private void Awake()
    {
        HidePhotoActionButtonsImmediately();
    }

    private void OnEnable()
    {
        HidePhotoActionButtonsImmediately();
        StartCoroutine(EnsurePhotoModeStarted());
    }

    private void HidePhotoActionButtonsImmediately()
    {
        if (saveButtonObject == null)
        {
            saveButtonObject = FindChildObject(SaveButtonName);
        }

        if (backToArButtonObject == null)
        {
            backToArButtonObject = FindChildObject(BackToArButtonName);
        }

        if (cancellationButtonObject == null)
        {
            cancellationButtonObject = FindChildObject(CancellationButtonName);
        }

        SetPhotoActionButtonsVisible(false);
    }

    private void OnDisable()
    {
        photoModeInitialized = false;
        StopAllCoroutines();
    }

    private IEnumerator EnsurePhotoModeStarted()
    {
        yield return null;
        yield return null;

        if (photoModeInitialized || !gameObject.activeInHierarchy)
        {
            yield break;
        }

        if (OpenARScene.CurrentEntryMode != OpenARScene.ArEntryMode.Photo)
        {
            yield break;
        }

        BeginPhotoMode();
    }

    public void BeginPhotoMode()
    {
        if (photoModeInitialized || !isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            return;
        }

        StartCoroutine(BeginPhotoModeWhenReady());
    }

    private IEnumerator BeginPhotoModeWhenReady()
    {
        ResolveUiReferences();
        BuildExhibitList();
        WireButtons();

        const int maxFrames = 90;
        for (int frame = 0; frame < maxFrames; frame++)
        {
            Camera arCamera = ARPhotoExhibitPlacer.FindArCamera();
            if (arCamera != null)
            {
                placer.SetArCamera(arCamera);
                break;
            }

            yield return null;
        }

        photoModeInitialized = true;

        if (resolvedPrefabs.Count == 0)
        {
            Debug.LogWarning("AR Photo: no exhibit prefabs assigned on 'AR photos'.");
            yield break;
        }

        SelectExhibit(0);
    }

    private void ResolveUiReferences()
    {
        Transform iconTransform = FindChildRecursive(transform, IconObjectName);
        if (iconTransform != null)
        {
            iconButton = iconTransform.GetComponent<Button>();
            iconFrameImage = iconTransform.GetComponent<Image>();
            iconObjectReference = iconTransform.gameObject;
            EnsureIconPreview(iconTransform);
        }

        Transform scrollTransform = FindChildRecursive(transform, ScrollButtonName);
        if (scrollTransform != null)
        {
            scrollButton = scrollTransform.GetComponent<Button>();
            scrollButtonObjectReference = scrollTransform.gameObject;
        }

        Transform photoTransform = FindChildRecursive(transform, PhotoButtonName);
        if (photoTransform != null)
        {
            photoButton = photoTransform.GetComponent<Button>();
            photoButtonObjectReference = photoTransform.gameObject;
        }

        saveButtonObject = FindChildObject(SaveButtonName);
        backToArButtonObject = FindChildObject(BackToArButtonName);
        cancellationButtonObject = FindChildObject(CancellationButtonName);
        photoSelectionPanelObject = FindChildObject(PhotoSelectionPanelName);
        if (photoSelectionPanelObject != null)
        {
            Image selectionBackground = photoSelectionPanelObject.GetComponent<Image>();
            if (selectionBackground != null)
            {
                selectionBackground.raycastTarget = false;
            }
        }

        saveButton = saveButtonObject != null ? saveButtonObject.GetComponent<Button>() : null;
        cancellationButton = cancellationButtonObject != null
            ? cancellationButtonObject.GetComponent<Button>()
            : null;

        SetPhotoActionButtonsVisible(false);
        SetSelectionUiVisible(true);

        placer = GetComponent<ARPhotoExhibitPlacer>();
        if (placer == null)
        {
            placer = gameObject.AddComponent<ARPhotoExhibitPlacer>();
        }

        photoCapture = GetComponent<ARPhotoCapture>();
        if (photoCapture == null)
        {
            photoCapture = gameObject.AddComponent<ARPhotoCapture>();
        }

        photoCapture.CaptureFinished -= OnPhotoCaptureFinished;
        photoCapture.CaptureFinished += OnPhotoCaptureFinished;

        placer.ConfigureScale(arExhibitMaxSize);
        if (iconPreview != null)
        {
            iconPreview.ConfigureScale(iconPreviewMaxSize);
        }
    }

    private void EnsureIconPreview(Transform iconTransform)
    {
        ConfigureIconShowsModelOnly(iconTransform);

        Transform previewTransform = iconTransform.Find(IconPreviewChildName);
        if (previewTransform == null)
        {
            var previewObject = new GameObject(
                IconPreviewChildName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

            previewObject.transform.SetParent(iconTransform, false);
            previewTransform = previewObject.transform;
        }

        RectTransform previewRect = previewTransform as RectTransform;
        previewRect.anchorMin = Vector2.zero;
        previewRect.anchorMax = Vector2.one;
        previewRect.offsetMin = Vector2.zero;
        previewRect.offsetMax = Vector2.zero;

        iconPreviewRawImage = previewTransform.GetComponent<RawImage>();
        if (iconPreviewRawImage == null)
        {
            Image legacyImage = previewTransform.GetComponent<Image>();
            if (legacyImage != null)
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(legacyImage);
                }
                else
                {
                    Object.DestroyImmediate(legacyImage);
                }
            }

            iconPreviewRawImage = previewTransform.gameObject.AddComponent<RawImage>();
        }

        iconPreviewRawImage.raycastTarget = true;
        iconPreviewRawImage.maskable = false;

        Transform silhouetteTransform = iconTransform.Find("IconModelSilhouette");
        if (silhouetteTransform == null)
        {
            var silhouetteObject = new GameObject(
                "IconModelSilhouette",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            silhouetteObject.transform.SetParent(iconTransform, false);
            silhouetteTransform = silhouetteObject.transform;
        }

        RectTransform silhouetteRect = silhouetteTransform as RectTransform;
        silhouetteRect.anchorMin = Vector2.zero;
        silhouetteRect.anchorMax = Vector2.one;
        silhouetteRect.offsetMin = Vector2.zero;
        silhouetteRect.offsetMax = Vector2.zero;

        iconPreviewSilhouetteImage = silhouetteTransform.GetComponent<Image>();
        iconPreviewSilhouetteImage.raycastTarget = false;
        iconPreviewSilhouetteImage.preserveAspect = true;
        iconPreviewSilhouetteImage.useSpriteMesh = true;
        iconPreviewSilhouetteImage.maskable = false;
        iconPreviewSilhouetteImage.enabled = false;

        iconPreview = iconTransform.GetComponent<ARPhotoIconPreview>();
        if (iconPreview == null)
        {
            iconPreview = iconTransform.gameObject.AddComponent<ARPhotoIconPreview>();
        }

        iconPreview.Bind(iconPreviewRawImage, iconPreviewSilhouetteImage);

        if (iconButton != null)
        {
            iconButton.targetGraphic = iconPreviewRawImage;
        }
    }

    private static void ConfigureIconShowsModelOnly(Transform iconTransform)
    {
        Mask mask = iconTransform.GetComponent<Mask>();
        if (mask != null)
        {
            if (Application.isPlaying)
            {
                Object.Destroy(mask);
            }
            else
            {
                Object.DestroyImmediate(mask);
            }
        }

        Image frame = iconTransform.GetComponent<Image>();
        if (frame != null)
        {
            frame.sprite = null;
            frame.enabled = false;
            frame.raycastTarget = false;
        }
    }

    private void BuildExhibitList()
    {
        resolvedPrefabs.Clear();
        exhibitIds.Clear();

        if (exhibitPrefabs != null)
        {
            for (int i = 0; i < exhibitPrefabs.Length; i++)
            {
                GameObject template = ResolveExhibitTemplate(exhibitPrefabs[i]);
                if (template != null)
                {
                    resolvedPrefabs.Add(template);
                    exhibitIds.Add(template.name);
                }
            }
        }

        if (resolvedPrefabs.Count > 0)
        {
            return;
        }

        GameObject cubePrefab = Resources.Load<GameObject>("Prefabs/Cube");
        if (cubePrefab != null)
        {
            resolvedPrefabs.Add(cubePrefab);
            exhibitIds.Add(cubePrefab.name);
            return;
        }

        IReadOnlyList<string> catalogIds = ExhibitCatalog.GetExhibitIdsForPhotoMode();
        for (int i = 0; i < catalogIds.Count; i++)
        {
            string exhibitId = catalogIds[i];
            GameObject prefab = Resources.Load<GameObject>("Exhibits/Models/" + exhibitId);
            if (prefab != null)
            {
                resolvedPrefabs.Add(prefab);
                exhibitIds.Add(exhibitId);
            }
        }

        if (resolvedPrefabs.Count > 0)
        {
            return;
        }

        exhibitIds.Add("painting_1");
        resolvedPrefabs.Add(null);
    }

    private void WireButtons()
    {
        if (iconButton != null)
        {
            iconButton.onClick.RemoveListener(OnIconClicked);
            iconButton.onClick.AddListener(OnIconClicked);
        }

        if (scrollButton != null)
        {
            scrollButton.onClick.RemoveListener(OnScrollClicked);
            scrollButton.onClick.AddListener(OnScrollClicked);
        }

        if (photoButton != null)
        {
            photoButton.onClick.RemoveListener(OnPhotoClicked);
            photoButton.onClick.AddListener(OnPhotoClicked);
            photoButton.interactable = photoCapture == null || photoCapture.CanCapture;
        }

        if (saveButton != null)
        {
            saveButton.onClick.RemoveListener(OnSaveClicked);
            saveButton.onClick.AddListener(OnSaveClicked);
        }

        if (cancellationButton != null)
        {
            cancellationButton.onClick.RemoveListener(OnCancellationClicked);
            cancellationButton.onClick.AddListener(OnCancellationClicked);
        }
    }

    private void OnSaveClicked()
    {
        ARPhotoPreview preview = GetComponent<ARPhotoPreview>();
        if (preview == null || preview.CurrentPhoto == null)
        {
            ToastNotification.Show("Нет фото для сохранения");
            return;
        }

        string savedPath = ARPhotoLibrary.SavePhoto(preview.CurrentPhoto);
        if (string.IsNullOrEmpty(savedPath))
        {
            ToastNotification.Show("Не удалось сохранить фото");
            return;
        }

        ToastNotification.Show("Фото сохранено в профиль");
        ReturnToSelectionMode();
    }

    private void OnCancellationClicked()
    {
        ReturnToSelectionMode();
    }

    private void ReturnToSelectionMode()
    {
        ARPhotoPreview preview = GetComponent<ARPhotoPreview>();
        if (preview != null)
        {
            preview.ClearPhoto();
        }

        SetPhotoActionButtonsVisible(false);
        SetSelectionUiVisible(true);

        if (photoButton != null && photoCapture != null)
        {
            photoButton.interactable = photoCapture.CanCapture;
        }
    }

    private void OnPhotoClicked()
    {
        if (photoCapture == null)
        {
            ToastNotification.Show("Съёмка недоступна");
            return;
        }

        if (photoButton != null)
        {
            photoButton.interactable = false;
        }

        photoCapture.TakePhoto();
    }

    private void OnPhotoCaptureFinished()
    {
        if (photoButton != null && photoCapture != null)
        {
            photoButton.interactable = photoCapture.CanCapture;
        }

        ARPhotoPreview preview = GetComponent<ARPhotoPreview>();
        bool previewVisible = preview != null && preview.IsVisible;

        SetPhotoActionButtonsVisible(previewVisible);
        SetSelectionUiVisible(!previewVisible);
    }

    private void SetSelectionUiVisible(bool visible)
    {
        if (photoSelectionPanelObject != null && photoSelectionPanelObject.activeSelf != visible)
        {
            photoSelectionPanelObject.SetActive(visible);
        }

        if (iconObjectReference != null && iconObjectReference.activeSelf != visible)
        {
            iconObjectReference.SetActive(visible);
        }

        if (scrollButtonObjectReference != null && scrollButtonObjectReference.activeSelf != visible)
        {
            scrollButtonObjectReference.SetActive(visible);
        }

        if (photoButtonObjectReference != null && photoButtonObjectReference.activeSelf != visible)
        {
            photoButtonObjectReference.SetActive(visible);
        }
    }

    private GameObject FindChildObject(string objectName)
    {
        Transform found = FindChildRecursive(transform, objectName);
        return found != null ? found.gameObject : null;
    }

    private static Transform FindChildRecursive(Transform root, string objectName)
    {
        if (root == null)
        {
            return null;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == objectName)
            {
                return child;
            }

            Transform deeper = FindChildRecursive(child, objectName);
            if (deeper != null)
            {
                return deeper;
            }
        }

        return null;
    }

    private void SetPhotoActionButtonsVisible(bool visible)
    {
        if (saveButtonObject != null && saveButtonObject.activeSelf != visible)
        {
            saveButtonObject.SetActive(visible);
        }

        if (backToArButtonObject != null && backToArButtonObject.activeSelf != visible)
        {
            backToArButtonObject.SetActive(visible);
        }

        if (cancellationButtonObject != null && cancellationButtonObject.activeSelf != visible)
        {
            cancellationButtonObject.SetActive(visible);
        }
    }

    private void OnIconClicked()
    {
        EnsureInteractionReady();
        ToggleArExhibitVisibility();
    }

    private void OnScrollClicked()
    {
        if (resolvedPrefabs.Count == 0)
        {
            return;
        }

        int nextIndex = (currentIndex + 1) % resolvedPrefabs.Count;
        SelectExhibit(nextIndex);
    }

    private void SelectExhibit(int index)
    {
        if (resolvedPrefabs.Count == 0)
        {
            return;
        }

        currentIndex = index;
        GameObject prefab = resolvedPrefabs[currentIndex];

        UpdateIconPreview(prefab, currentIndex);
        placer.ClearExhibit();
        isArExhibitVisible = false;
    }

    private void ToggleArExhibitVisibility()
    {
        if (isArExhibitVisible)
        {
            HideArExhibit();
            return;
        }

        ShowArExhibit();
    }

    private void ShowArExhibit()
    {
        EnsureInteractionReady();

        if (resolvedPrefabs.Count == 0)
        {
            Debug.LogWarning("AR Photo: no exhibit prefabs to place. Assign prefabs on 'AR photos'.");
            return;
        }

        if (placer == null)
        {
            Debug.LogWarning("AR Photo: exhibit placer is missing on 'AR photos'.");
            return;
        }

        GameObject prefab = resolvedPrefabs[currentIndex];
        string exhibitId = exhibitIds[currentIndex];

        placer.PlaceExhibitPrefab(prefab, exhibitId, currentIndex, spawnDistance);
        isArExhibitVisible = placer.HasSpawnedExhibit && placer.IsExhibitVisible;
    }

    private void EnsureInteractionReady()
    {
        if (placer == null)
        {
            placer = GetComponent<ARPhotoExhibitPlacer>();
            if (placer == null)
            {
                placer = gameObject.AddComponent<ARPhotoExhibitPlacer>();
            }

            placer.ConfigureScale(arExhibitMaxSize);
        }

        if (resolvedPrefabs.Count == 0)
        {
            BuildExhibitList();
        }

        if (iconButton == null || scrollButton == null || photoButton == null)
        {
            ResolveUiReferences();
        }

        WireButtons();

        Camera arCamera = ARPhotoExhibitPlacer.FindArCamera();
        if (arCamera != null)
        {
            placer.SetArCamera(arCamera);
        }
    }

    private void HideArExhibit()
    {
        isArExhibitVisible = false;

        if (placer.HasSpawnedExhibit)
        {
            placer.SetExhibitVisible(false);
        }
    }

    private void UpdateIconPreview(GameObject prefab, int exhibitIndex)
    {
        if (iconPreview == null)
        {
            return;
        }

        Color fallbackColor = ExhibitCatalog.GetPhotoModeFallbackColor(exhibitIndex);
        if (prefab != null)
        {
            iconPreview.ShowPrefab(prefab, fallbackColor);
        }
        else
        {
            iconPreview.ShowColor(fallbackColor);
        }
    }

    private static GameObject ResolveExhibitTemplate(GameObject reference)
    {
        if (reference == null)
        {
            return null;
        }

#if UNITY_EDITOR
        if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(reference))
        {
            return reference;
        }

        string assetPath = UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(reference);
        if (!string.IsNullOrEmpty(assetPath))
        {
            GameObject asset = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (asset != null)
            {
                return asset;
            }
        }
#endif
        if (reference.scene.IsValid())
        {
            Debug.LogWarning(
                "AR Photo: exhibit reference points to a scene object (" + reference.name +
                "). Assign prefabs from the Project window, not the Hierarchy.");
            return null;
        }

        return reference;
    }

    private void OnDestroy()
    {
        photoModeInitialized = false;

        if (iconButton != null)
        {
            iconButton.onClick.RemoveListener(OnIconClicked);
        }

        if (scrollButton != null)
        {
            scrollButton.onClick.RemoveListener(OnScrollClicked);
        }

        if (photoButton != null)
        {
            photoButton.onClick.RemoveListener(OnPhotoClicked);
        }

        if (saveButton != null)
        {
            saveButton.onClick.RemoveListener(OnSaveClicked);
        }

        if (cancellationButton != null)
        {
            cancellationButton.onClick.RemoveListener(OnCancellationClicked);
        }

        if (photoCapture != null)
        {
            photoCapture.CaptureFinished -= OnPhotoCaptureFinished;
        }
    }
}
