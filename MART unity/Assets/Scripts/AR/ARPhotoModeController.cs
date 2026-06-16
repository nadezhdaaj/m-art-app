using UnityEngine;

/// <summary>
/// Enables the scene "AR photos" panel and wires icon / scroll buttons in photo mode.
/// </summary>
public class ARPhotoModeController : MonoBehaviour
{
    private const string ArPhotosPanelName = "AR photos";
    private const string ArScanningPanelName = "AR mode";

    public static void TryStart(OpenARScene.ArEntryMode entryMode)
    {
        if (entryMode != OpenARScene.ArEntryMode.Photo)
        {
            SetPhotoPanelActive(false);
            return;
        }

        SetPhotoPanelActive(true);

        GameObject arPhotosPanel = GameObject.Find(ArPhotosPanelName);
        if (arPhotosPanel == null)
        {
            return;
        }

        ARPhotoSelectionUI selectionUi = arPhotosPanel.GetComponent<ARPhotoSelectionUI>();
        if (selectionUi == null)
        {
            Debug.LogWarning(
                "AR Photo Selection UI is missing on 'AR photos'. " +
                "Use menu Tools > AR > Add Photo Selection UI to AR Scene, or Add Component manually.");
            return;
        }

        if (arPhotosPanel.GetComponent<ARPhotoExhibitPlacer>() == null)
        {
            arPhotosPanel.AddComponent<ARPhotoExhibitPlacer>();
        }

        if (arPhotosPanel.GetComponent<ARPhotoCapture>() == null)
        {
            arPhotosPanel.AddComponent<ARPhotoCapture>();
        }

        if (arPhotosPanel.GetComponent<ARPhotoPreview>() == null)
        {
            arPhotosPanel.AddComponent<ARPhotoPreview>();
        }

        selectionUi.BeginPhotoMode();
        DisableSceneTestInstances();
    }

    private static void SetPhotoPanelActive(bool photoMode)
    {
        GameObject arPhotosPanel = GameObject.Find(ArPhotosPanelName);
        if (arPhotosPanel != null)
        {
            arPhotosPanel.SetActive(photoMode);
        }

        GameObject scanningPanel = GameObject.Find(ArScanningPanelName);
        if (scanningPanel != null)
        {
            scanningPanel.SetActive(!photoMode);
        }
    }

    private static void DisableSceneTestInstances()
    {
        GameObject[] roots = UnityEngine.SceneManagement.SceneManager
            .GetActiveScene()
            .GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            string rootName = roots[i].name;
            bool isSceneTestMesh = (rootName.Contains("Cube")
                    || rootName.Contains("Icosphere")
                    || rootName.Contains("brushes")
                    || rootName.Contains("flashlight")
                    || rootName.Contains("easel"))
                && roots[i].GetComponent<ARPhotoSelectionUI>() == null;
            if (isSceneTestMesh)
            {
                roots[i].SetActive(false);
            }
        }
    }
}
