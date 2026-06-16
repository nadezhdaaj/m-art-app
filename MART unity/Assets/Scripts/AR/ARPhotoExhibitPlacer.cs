using UnityEngine;

/// <summary>
/// Places the selected exhibit in front of the AR camera (photo mode).
/// </summary>
public class ARPhotoExhibitPlacer : MonoBehaviour
{
    [SerializeField] private Camera arCamera;
    [SerializeField] private float spawnHeightOffset = -0.05f;
    [SerializeField] private float exhibitMaxSize = 0.45f;

    private GameObject spawnedExhibit;
    private string currentExhibitId;
    private float currentSpawnDistance = 1.2f;
    private Quaternion exhibitRotationOffset = Quaternion.identity;

    public string CurrentExhibitId => currentExhibitId;

    public bool HasSpawnedExhibit => spawnedExhibit != null;

    public bool IsExhibitVisible => spawnedExhibit != null && spawnedExhibit.activeSelf;

    public void ConfigureScale(float maxSize)
    {
        exhibitMaxSize = Mathf.Max(0.01f, maxSize);
    }

    public void SetArCamera(Camera camera)
    {
        arCamera = camera;
    }

    public static Camera FindArCamera()
    {
        Camera main = Camera.main;
        if (main != null)
        {
            return main;
        }

        Camera[] cameras = Object.FindObjectsOfType<Camera>(true);
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera candidate = cameras[i];
            if (candidate == null || !candidate.enabled)
            {
                continue;
            }

            if (candidate.CompareTag("MainCamera"))
            {
                return candidate;
            }
        }

        for (int i = 0; i < cameras.Length; i++)
        {
            Camera candidate = cameras[i];
            if (candidate != null && candidate.enabled && candidate.gameObject.activeInHierarchy)
            {
                return candidate;
            }
        }

        return null;
    }

    private void Awake()
    {
        if (arCamera == null)
        {
            arCamera = FindArCamera();
        }
    }

    public void PlaceExhibit(string exhibitId, int exhibitIndex)
    {
        PlaceExhibitPrefab(null, exhibitId, exhibitIndex, currentSpawnDistance);
    }

    public void PlaceExhibitPrefab(GameObject prefab, string exhibitId, int exhibitIndex, float distance)
    {
        if (string.IsNullOrWhiteSpace(exhibitId))
        {
            return;
        }

        currentSpawnDistance = distance;

        if (arCamera == null)
        {
            arCamera = FindArCamera();
        }

        if (arCamera == null)
        {
            Debug.LogWarning("AR Photo: camera not found, cannot place exhibit.");
            return;
        }

        bool sameExhibit = currentExhibitId == exhibitId && spawnedExhibit != null;
        if (sameExhibit)
        {
            RepositionInFrontOfCamera();
            spawnedExhibit.SetActive(true);
            return;
        }

        ClearExhibit();
        currentExhibitId = exhibitId;

        spawnedExhibit = prefab != null
            ? Instantiate(prefab)
            : ARPhotoExhibitFactory.Create(exhibitId, exhibitIndex);

        if (spawnedExhibit == null)
        {
            Debug.LogWarning("AR Photo: failed to create exhibit '" + exhibitId + "'.");
            return;
        }

        spawnedExhibit.name = "ARPhotoExhibit_" + exhibitId;
        spawnedExhibit.SetActive(true);
        exhibitRotationOffset = spawnedExhibit.transform.localRotation;
        ARPhotoExhibitFactory.PrepareSpawnedInstance(spawnedExhibit);
        AttachInFrontOfCamera();
        ARPhotoExhibitScaling.FitMaxDimension(spawnedExhibit, exhibitMaxSize);
        EnsureMover(spawnedExhibit);

        Debug.Log(
            "AR Photo: placed '" + spawnedExhibit.name + "' at " +
            spawnedExhibit.transform.position + ", scale " + spawnedExhibit.transform.lossyScale);
    }

    private void AttachInFrontOfCamera()
    {
        if (spawnedExhibit == null || arCamera == null)
        {
            return;
        }

        Transform cameraTransform = arCamera.transform;
        spawnedExhibit.transform.SetParent(cameraTransform, false);
        RepositionInFrontOfCamera();
    }

    private void RepositionInFrontOfCamera()
    {
        if (spawnedExhibit == null || arCamera == null)
        {
            return;
        }

        spawnedExhibit.transform.localPosition = new Vector3(0f, spawnHeightOffset, currentSpawnDistance);
        spawnedExhibit.transform.localRotation = exhibitRotationOffset;
    }

    private void EnsureMover(GameObject exhibit)
    {
        if (exhibit == null)
        {
            return;
        }

        ARPhotoExhibitMover mover = exhibit.GetComponent<ARPhotoExhibitMover>();
        if (mover == null)
        {
            mover = exhibit.AddComponent<ARPhotoExhibitMover>();
        }

        mover.Bind(arCamera);
    }

    public void SetExhibitVisible(bool visible)
    {
        if (spawnedExhibit != null)
        {
            spawnedExhibit.SetActive(visible);
        }
    }

    public void ClearExhibit()
    {
        if (spawnedExhibit != null)
        {
            Destroy(spawnedExhibit);
            spawnedExhibit = null;
        }

        currentExhibitId = null;
    }

    private void OnDestroy()
    {
        ClearExhibit();
    }
}
