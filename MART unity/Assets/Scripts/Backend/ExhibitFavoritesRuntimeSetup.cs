using UnityEngine;
using UnityEngine.UI;

public static class ExhibitFavoritesRuntimeSetup
{
    public static void ConfigureScene(string sceneName)
    {
        if (sceneName == "ARScene")
        {
            ConfigureArScene();
            return;
        }

        if (sceneName == "The main stage")
        {
            ConfigureMainStage();
            ConfigurePaintWorkspace();
        }
    }

    private static void ConfigureArScene()
    {
        GameObject favoritesButton = GameObject.Find("Favourites");
        if (favoritesButton == null)
        {
            return;
        }

        if (favoritesButton.GetComponent<ExhibitFavoriteButton>() == null)
        {
            favoritesButton.AddComponent<ExhibitFavoriteButton>();
        }

        Button button = favoritesButton.GetComponent<Button>();
        if (button != null)
        {
            button.transition = Selectable.Transition.None;
        }
    }

    private static void ConfigureMainStage()
    {
        GameObject bottomBar = GameObject.Find("BottomBar");
        if (bottomBar != null && bottomBar.GetComponent<MainStageBottomBarKeeper>() == null)
        {
            bottomBar.AddComponent<MainStageBottomBarKeeper>();
        }

        GameObject favouritesPanel = GameObject.Find("FavouritesPanel");
        if (favouritesPanel == null)
        {
            return;
        }

        if (favouritesPanel.GetComponent<FavouritesPanelGallery>() == null)
        {
            favouritesPanel.AddComponent<FavouritesPanelGallery>();
        }

        ProfileMainStageArtworksSetup.Configure();
    }

    private static void ConfigurePaintWorkspace()
    {
        GameObject userWorks = FindSceneObject("User's work");
        if (userWorks != null)
        {
            UserWorksGalleryBuilder.Ensure(userWorks);
        }

        GameObject paintWorkspace = FindSceneObject("PaintCanvas");
        if (paintWorkspace == null)
        {
            paintWorkspace = FindSceneObject("anvasCanvas");
        }
        if (paintWorkspace != null && paintWorkspace.GetComponent<PaintWorkspaceSession>() == null)
        {
            paintWorkspace.AddComponent<PaintWorkspaceSession>();
        }

        GameObject namingPanel = FindSceneObject("for the name");
        if (namingPanel != null)
        {
            namingPanel.SetActive(false);

            Transform sendTransform = FindChildRecursive(namingPanel.transform, "to send");
            if (sendTransform != null && sendTransform.GetComponent<ArtworkNamingSubmitButton>() == null)
            {
                sendTransform.gameObject.AddComponent<ArtworkNamingSubmitButton>();
            }
        }

        PaintArtworkController paintController = Object.FindObjectOfType<PaintArtworkController>(true);
        paintController?.RefreshNamingPanelBindings();
    }

    private static GameObject FindSceneObject(string objectName)
    {
        UnityEngine.SceneManagement.Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !activeScene.isLoaded)
        {
            return null;
        }

        GameObject[] roots = activeScene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform found = FindChildRecursive(roots[i].transform, objectName);
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
}
