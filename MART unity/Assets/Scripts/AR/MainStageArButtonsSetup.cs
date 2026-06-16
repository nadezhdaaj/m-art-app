using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Wires Home AR entry buttons on the main stage (including when Home was inactive at load time).
/// </summary>
public static class MainStageArButtonsSetup
{
    private const string MainStageSceneName = "The main stage";
    private const string ArPhotosButtonName = "AR photos button";
    private const string ScanningButtonName = "Scanning";
    private const string OpenArObjectName = "OpenAR";
    private const string HomeScreenName = "Home";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != MainStageSceneName)
        {
            return;
        }

        EnsureWired();
    }

    public static void EnsureWired()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != MainStageSceneName)
        {
            return;
        }

        OpenARScene openAr = FindOpenArScene();
        if (openAr == null)
        {
            return;
        }

        WireButton(FindButton(ScanningButtonName), openAr.OpenAR);
        WireButton(FindButton(ArPhotosButtonName), openAr.OpenARPhotoMode);
    }

    private static OpenARScene FindOpenArScene()
    {
        GameObject host = FindInMainStage(OpenArObjectName);
        if (host != null && host.TryGetComponent(out OpenARScene openAr))
        {
            return openAr;
        }

        foreach (OpenARScene candidate in Object.FindObjectsOfType<OpenARScene>(true))
        {
            if (candidate.gameObject.scene.name == MainStageSceneName)
            {
                return candidate;
            }
        }

        return null;
    }

    private static GameObject FindButton(string buttonName)
    {
        Navigation navigation = Object.FindObjectOfType<Navigation>(true);
        if (navigation != null && navigation.homeScreen != null)
        {
            Transform onHome = FindChildRecursive(navigation.homeScreen.transform, buttonName);
            if (onHome != null)
            {
                return onHome.gameObject;
            }
        }

        return FindInMainStage(buttonName);
    }

    private static GameObject FindInMainStage(string objectName)
    {
        Scene mainStage = SceneManager.GetSceneByName(MainStageSceneName);
        if (!mainStage.IsValid())
        {
            return null;
        }

        foreach (GameObject root in mainStage.GetRootGameObjects())
        {
            Transform found = FindChildRecursive(root.transform, objectName);
            if (found != null)
            {
                return found.gameObject;
            }
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform parent, string objectName)
    {
        if (parent.name == objectName)
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindChildRecursive(parent.GetChild(i), objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static void WireButton(GameObject buttonObject, UnityEngine.Events.UnityAction handler)
    {
        if (buttonObject == null || !buttonObject.TryGetComponent(out Button button))
        {
            return;
        }

        button.onClick.RemoveListener(handler);
        button.onClick.AddListener(handler);
    }
}
