#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Keeps required scenes enabled in Build Settings (prevents splash hang).
/// </summary>
public sealed class AndroidBuildSceneValidator : IPreprocessBuildWithReport
{
    private static readonly string[] RequiredScenePaths =
    {
        "Assets/Scenes/SplashLoading.unity",
        "Assets/Scenes/AuthScene.unity",
        "Assets/Scenes/The main stage.unity",
        "Assets/Scenes/ARScene.unity"
    };

    public int callbackOrder => -1000;

    [InitializeOnLoadMethod]
    private static void OnEditorLoad()
    {
        EditorApplication.delayCall += () => EnsureRequiredScenesInBuild();
    }

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android)
        {
            return;
        }

        if (!EnsureRequiredScenesInBuild())
        {
            throw new BuildFailedException(
                "Android build: включите все обязательные сцены в File → Build Settings " +
                "(SplashLoading, AuthScene, The main stage, ARScene).");
        }
    }

    [MenuItem("Tools/Android/Fix Build Scenes")]
    public static void FixBuildScenesMenu()
    {
        if (EnsureRequiredScenesInBuild())
        {
            Debug.Log("Build Settings: все обязательные сцены включены.");
        }
    }

    public static bool EnsureRequiredScenesInBuild()
    {
        var existing = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool changed = false;

        for (int i = 0; i < RequiredScenePaths.Length; i++)
        {
            string path = RequiredScenePaths[i];
            int index = existing.FindIndex(s => s.path == path);
            if (index < 0)
            {
                existing.Insert(i, new EditorBuildSettingsScene(path, true));
                changed = true;
                continue;
            }

            if (!existing[index].enabled)
            {
                existing[index] = new EditorBuildSettingsScene(path, true);
                changed = true;
            }
        }

        var ordered = new List<EditorBuildSettingsScene>();
        for (int i = 0; i < RequiredScenePaths.Length; i++)
        {
            string path = RequiredScenePaths[i];
            int index = existing.FindIndex(s => s.path == path);
            if (index >= 0)
            {
                ordered.Add(new EditorBuildSettingsScene(path, true));
                existing.RemoveAt(index);
            }
        }

        for (int i = 0; i < existing.Count; i++)
        {
            if (!ordered.Exists(s => s.path == existing[i].path))
            {
                ordered.Add(existing[i]);
            }
        }

        if (changed || !ScenesMatch(EditorBuildSettings.scenes, ordered))
        {
            EditorBuildSettings.scenes = ordered.ToArray();
            return true;
        }

        return true;
    }

    private static bool ScenesMatch(
        EditorBuildSettingsScene[] a,
        List<EditorBuildSettingsScene> b)
    {
        if (a == null || a.Length < RequiredScenePaths.Length)
        {
            return false;
        }

        for (int i = 0; i < RequiredScenePaths.Length; i++)
        {
            if (a[i].path != RequiredScenePaths[i] || !a[i].enabled)
            {
                return false;
            }
        }

        return true;
    }
}
#endif
