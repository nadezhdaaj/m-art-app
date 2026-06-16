using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Auto-attaches an <see cref="ARPhotoGallery"/> to the "AR photos gallery" panel
/// in the main stage scene so saved AR photos are displayed in the profile section.
/// </summary>
public static class ARPhotoGalleryInstaller
{
    private const string GalleryPanelName = "AR photos gallery";
    private const string GalleryTitleChildName = "AR photos text";
    private const string MainStageSceneName = "The main stage";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        InstallForActiveScene();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InstallForScene(scene);
    }

    private static void InstallForActiveScene()
    {
        InstallForScene(SceneManager.GetActiveScene());
    }

    private static void InstallForScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return;
        }

        if (scene.name != MainStageSceneName)
        {
            return;
        }

        var runner = new GameObject(nameof(ARPhotoGalleryRunner));
        runner.AddComponent<ARPhotoGalleryRunner>();
    }

    private sealed class ARPhotoGalleryRunner : MonoBehaviour
    {
        private void Start()
        {
            GameObject panel = FindGalleryPanel();
            if (panel != null && panel.GetComponent<ARPhotoGallery>() == null)
            {
                panel.AddComponent<ARPhotoGallery>();
            }

            Destroy(gameObject);
        }

        private static GameObject FindGalleryPanel()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                return null;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform found = FindGalleryPanelTransform(roots[i].transform);
                if (found != null)
                {
                    return found.gameObject;
                }
            }

            return null;
        }

        private static Transform FindGalleryPanelTransform(Transform parent)
        {
            if (parent == null)
            {
                return null;
            }

            if (parent.name == GalleryPanelName && IsGalleryPanel(parent))
            {
                return parent;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindGalleryPanelTransform(parent.GetChild(i));
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static bool IsGalleryPanel(Transform candidate)
        {
            for (int i = 0; i < candidate.childCount; i++)
            {
                Transform child = candidate.GetChild(i);
                if (child != null && child.name == GalleryTitleChildName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
