using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ensures profile tab panels do not all stack visible on load (Information is the default tab).
/// In the editor, ApplyEditorPreview keeps OtherPanel visible for layout authoring.
/// </summary>
public static class ProfilePanelDefaultVisibility
{
    private const string ProfileUiName = "ProfileUI";
    private const string InformationPanelName = "InformationPanel";
    private const string FavouritesPanelName = "FavouritesPanel";
    private const string OtherPanelName = "OtherPanel";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ApplyOnMainStageLoad()
    {
        if (!Application.isPlaying || !SceneManager.GetActiveScene().name.Contains("main stage"))
        {
            return;
        }

        Apply();
    }

    public static void Apply()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        Apply(ResolveProfileUiRoot());
    }

    public static void Apply(Transform profileRoot)
    {
        if (!Application.isPlaying || profileRoot == null)
        {
            return;
        }

        ApplyPlayModeDefaults(profileRoot);
    }

    public static void ApplyEditorPreview()
    {
        if (Application.isPlaying)
        {
            return;
        }

        Transform profileRoot = ResolveProfileUiRoot();
        if (profileRoot == null)
        {
            return;
        }

        SetPanelActive(profileRoot, InformationPanelName, false);
        SetPanelActive(profileRoot, FavouritesPanelName, false);
        SetPanelActive(profileRoot, OtherPanelName, true);
    }

    private static void ApplyPlayModeDefaults(Transform profileRoot)
    {
        if (profileRoot == null)
        {
            return;
        }

        SetPanelActive(profileRoot, InformationPanelName, true);
        SetPanelActive(profileRoot, FavouritesPanelName, false);
        SetPanelActive(profileRoot, OtherPanelName, false);
    }

    private static Transform ResolveProfileUiRoot()
    {
        GameObject profileUi = GameObject.Find(ProfileUiName);
        if (profileUi != null)
        {
            return profileUi.transform;
        }

        Navigation navigation = Object.FindObjectOfType<Navigation>(true);
        if (navigation != null && navigation.profileScreen != null)
        {
            return navigation.profileScreen.transform;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] children = roots[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < children.Length; j++)
                {
                    if (children[j].name == ProfileUiName)
                    {
                        return children[j];
                    }
                }
            }
        }
#endif

        return null;
    }

    private static void SetPanelActive(Transform profileRoot, string panelName, bool active)
    {
        Transform panel = profileRoot.Find(panelName);
        if (panel != null)
        {
            panel.gameObject.SetActive(active);
        }
    }
}
