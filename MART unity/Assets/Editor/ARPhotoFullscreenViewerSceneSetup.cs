#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Bakes <see cref="ARPhotoFullscreenViewer"/> under the scene Canvas so it is visible
/// in the Hierarchy outside Play mode. Does not change runtime photo-viewer logic.
/// </summary>
public static class ARPhotoFullscreenViewerSceneSetup
{
    private const string ViewerObjectName = "ARPhotoFullscreenViewer";
    private static readonly string[] TargetScenes = { "The main stage", "ARScene" };

    [InitializeOnLoadMethod]
    private static void RegisterSceneHook()
    {
        EditorSceneManager.sceneOpened -= OnSceneOpened;
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (!scene.IsValid())
        {
            return;
        }

        for (int i = 0; i < TargetScenes.Length; i++)
        {
            if (scene.name == TargetScenes[i])
            {
                EditorApplication.delayCall += EnsureInActiveScene;
                break;
            }
        }
    }

    [MenuItem("Tools/AR/Add Fullscreen Photo Viewer to Canvas")]
    public static void EnsureInActiveScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            return;
        }

        if (Object.FindFirstObjectByType<ARPhotoFullscreenViewer>(FindObjectsInactive.Include) != null)
        {
            SelectExistingViewer();
            return;
        }

        ARPhotoFullscreenViewer viewer = InvokeGetOrCreate();
        if (viewer == null)
        {
            Debug.LogError("ARPhotoFullscreenViewer: root Canvas not found in the active scene.");
            return;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = viewer.gameObject;
        EditorGUIUtility.PingObject(viewer.gameObject);
        Debug.Log(
            "ARPhotoFullscreenViewer added under Canvas (inactive). Save the scene (Ctrl+S) to keep it in the Hierarchy.");
    }

    private static void SelectExistingViewer()
    {
        ARPhotoFullscreenViewer viewer = Object.FindFirstObjectByType<ARPhotoFullscreenViewer>(
            FindObjectsInactive.Include);
        if (viewer == null)
        {
            return;
        }

        Selection.activeGameObject = viewer.gameObject;
        EditorGUIUtility.PingObject(viewer.gameObject);
    }

    private static ARPhotoFullscreenViewer InvokeGetOrCreate()
    {
        MethodInfo getOrCreate = typeof(ARPhotoFullscreenViewer).GetMethod(
            "GetOrCreate",
            BindingFlags.NonPublic | BindingFlags.Static);
        if (getOrCreate == null)
        {
            Debug.LogError("ARPhotoFullscreenViewer.GetOrCreate was not found.");
            return null;
        }

        return getOrCreate.Invoke(null, null) as ARPhotoFullscreenViewer;
    }
}
#endif
