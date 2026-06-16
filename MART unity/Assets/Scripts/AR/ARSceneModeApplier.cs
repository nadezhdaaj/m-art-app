using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Switches ARScene UI and subsystems for scanning vs photo entry mode.
/// </summary>
public static class ARSceneModeApplier
{
    private const string FavouritesName = "Favourites";
    private const string HideShowName = "Hide/ Show";

    private static bool appliedForCurrentScene;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void RegisterSceneHooks()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "ARScene")
        {
            return;
        }

        appliedForCurrentScene = false;

        var runner = new GameObject(nameof(ARSceneModeApplierRunner));
        runner.AddComponent<ARSceneModeApplierRunner>();
    }

    private static void OnSceneUnloaded(Scene scene)
    {
        if (scene.name == "ARScene")
        {
            appliedForCurrentScene = false;
        }
    }

    public static void TryApply(UIController uiController = null)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != "ARScene" || appliedForCurrentScene)
        {
            return;
        }

        appliedForCurrentScene = true;
        OpenARScene.ArEntryMode mode = OpenARScene.ConsumeEntryMode();
        bool isScanning = mode == OpenARScene.ArEntryMode.Scanning;

        if (uiController != null)
        {
            uiController.ApplyEntryMode(isScanning);
        }
        else
        {
            SetUiActive("information bar", isScanning);
        }

        SetUiActive(FavouritesName, isScanning);
        SetUiActive(HideShowName, isScanning);

        ARTrackedImageManager trackedImages = Object.FindObjectOfType<ARTrackedImageManager>();
        if (trackedImages != null)
        {
            trackedImages.enabled = isScanning;
        }

        ARImageSpawner imageSpawner = Object.FindObjectOfType<ARImageSpawner>();
        if (imageSpawner != null)
        {
            imageSpawner.enabled = isScanning;
        }

        SetUiActive("AR photos", !isScanning);
        SetUiActive("AR mode", isScanning);

        if (!isScanning)
        {
            ARPhotoModeController.TryStart(mode);
        }
    }

    private static void SetUiActive(string objectName, bool active)
    {
        GameObject target = GameObject.Find(objectName);
        if (target != null)
        {
            target.SetActive(active);
        }
    }

    private sealed class ARSceneModeApplierRunner : MonoBehaviour
    {
        private void Start()
        {
            TryApply(FindObjectOfType<UIController>());
            Destroy(gameObject);
        }
    }
}
