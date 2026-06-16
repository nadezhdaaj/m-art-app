#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Command-line Android build entry point (batchmode -executeMethod CIBuild.BuildAndroid).
/// Builds the enabled scenes from Build Settings to a known APK path.
/// </summary>
public static class CIBuild
{
    public static void BuildAndroid()
    {
        string outputDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "Builds");
        outputDir = Path.GetFullPath(outputDir);
        Directory.CreateDirectory(outputDir);
        string apkPath = Path.Combine(outputDir, "mart.apk");

        var scenes = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
            {
                scenes.Add(scene.path);
            }
        }

        if (scenes.Count == 0)
        {
            Debug.LogError("CIBuild: нет включённых сцен в Build Settings.");
            EditorApplication.Exit(2);
            return;
        }

        Debug.Log($"CIBuild: building {scenes.Count} scenes -> {apkPath}");

        var options = new BuildPlayerOptions
        {
            scenes = scenes.ToArray(),
            locationPathName = apkPath,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        Debug.Log($"CIBuild: result={summary.result}, errors={summary.totalErrors}, time={summary.totalTime}");

        if (summary.result != BuildResult.Succeeded)
        {
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log($"CIBuild: SUCCESS -> {apkPath}");
        EditorApplication.Exit(0);
    }
}
#endif
