#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Clears Burst cache before Android build. Does not touch StagingArea (Unity recreates icon files there).
/// </summary>
public sealed class AndroidPreBuildCleanup : IPreprocessBuildWithReport
{
    public int callbackOrder => -2000;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android)
        {
            return;
        }

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string burstOutput = Path.Combine(projectRoot, "Temp", "BurstOutput");
        TryDeleteDirectory(burstOutput);

        AndroidAppIconSetup.ApplyAppIconFromLogo(silent: true);
    }

    private static void TryDeleteDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        try
        {
            Directory.Delete(path, true);
        }
        catch (IOException ex)
        {
            Debug.LogWarning($"Android prebuild: не удалось очистить {path}: {ex.Message}");
        }
    }
}
#endif
