using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// OtherPanel = horizontal carousel. User's work = full vertical gallery.
/// </summary>
public static class ProfileMainStageArtworksSetup
{
    private const string OtherPanelName = "OtherPanel";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoConfigureMainStage()
    {
        if (!UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("main stage"))
        {
            return;
        }

        Configure();
    }

    public static void Configure()
    {
        if (Application.isPlaying)
        {
            ConfigureForPlayMode();
            return;
        }

        ConfigureForEditMode();
    }

    private static void ConfigureForPlayMode()
    {
        OtherPanelScrollRevert.Restore();
        ProfilePanelDefaultVisibility.Apply();
        ProfileUiArtworksBootstrap.EnsureAll();
        RemoveVerticalGalleryFromOtherPanel();
        ConfigureOtherPanelCarousel();
        OtherPanelScrollbarController.Ensure();
        RemoveInformationPanelCarousel();
    }

    private static void ConfigureForEditMode()
    {
        OtherPanelScrollRevert.Restore();

        Transform otherPanelTransform = FindOtherPanelTransform();
        if (otherPanelTransform != null && !OtherPanelScrollbarHierarchy.IsBaked(otherPanelTransform))
        {
            OtherPanelScrollbarHierarchy.Bake(otherPanelTransform, registerUndo: false);
        }

        ProfileUiArtworksHierarchyBuilder.EnsureAll(registerUndo: false);
        ProfileUiArtworksBootstrap.EnsureAll();
        ConfigureOtherPanelCarousel();
        OtherPanelScrollbarController.Ensure();

        if (otherPanelTransform != null)
        {
            ProfileArtworksCarousel carousel = otherPanelTransform.GetComponent<ProfileArtworksCarousel>();
            carousel?.RefreshCarousel();
        }

        OtherPanelScrollbarController.RefreshLayout();
    }

    private static Transform FindOtherPanelTransform()
    {
        GameObject otherPanel = GameObject.Find(OtherPanelName);
        if (otherPanel != null)
        {
            return otherPanel.transform;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] children = roots[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < children.Length; j++)
                {
                    if (children[j].name == OtherPanelName)
                    {
                        return children[j];
                    }
                }
            }
        }
#endif

        return null;
    }

    private static void RemoveVerticalGalleryFromOtherPanel()
    {
        GameObject otherPanel = GameObject.Find(OtherPanelName);
        if (otherPanel == null)
        {
            return;
        }

        ProfileArtworksGallery[] galleries = otherPanel.GetComponents<ProfileArtworksGallery>();
        for (int i = 0; i < galleries.Length; i++)
        {
            if (galleries[i] != null)
            {
                Object.DestroyImmediate(galleries[i]);
            }
        }

        ProfileArtworksGallery childGallery = otherPanel.GetComponentInChildren<ProfileArtworksGallery>(true);
        if (childGallery != null && childGallery.transform.IsChildOf(otherPanel.transform))
        {
            Object.DestroyImmediate(childGallery);
        }
    }

    private static void ConfigureOtherPanelCarousel()
    {
        GameObject otherPanel = GameObject.Find(OtherPanelName);
        if (otherPanel == null)
        {
            return;
        }

        ProfileArtworksCarousel carousel = otherPanel.GetComponent<ProfileArtworksCarousel>();
        if (carousel == null)
        {
            otherPanel.AddComponent<ProfileArtworksCarousel>();
        }
    }

    private static void RemoveInformationPanelCarousel()
    {
        GameObject informationPanel = GameObject.Find("InformationPanel");
        if (informationPanel == null)
        {
            return;
        }

        Transform section = informationPanel.transform.Find(ProfileArtworksCarousel.SectionObjectName);
        if (section != null)
        {
            Object.Destroy(section.gameObject);
        }
    }
}
