#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Replaces Build / Build And Run so Unity never calls broken StartApplication (NullReferenceException).
/// Installs and launches via adb in AndroidAutoLaunchAfterBuild instead.
/// </summary>
[InitializeOnLoad]
public static class AndroidBuildPipeline
{
    static AndroidBuildPipeline()
    {
        BuildPlayerWindow.RegisterBuildPlayerHandler(HandleBuildPlayer);
    }

    private static void HandleBuildPlayer(BuildPlayerOptions options)
    {
        if (options.target == BuildTarget.Android)
        {
            options.options &= ~BuildOptions.AutoRunPlayer;
        }

        BuildReport report = BuildPipeline.BuildPlayer(options);

        if (options.target == BuildTarget.Android &&
            report.summary.result == BuildResult.Succeeded)
        {
            AndroidAutoLaunchAfterBuild.ScheduleLaunchAfterInstall();
        }
    }
}
#endif
