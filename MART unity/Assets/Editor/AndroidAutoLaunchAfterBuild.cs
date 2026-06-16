#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Process = System.Diagnostics.Process;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;

/// <summary>
/// Launches the app via adb after build (Unity StartApplication is disabled in AndroidBuildPipeline).
/// </summary>
public sealed class AndroidAutoLaunchAfterBuild : IPostprocessBuildWithReport
{
    private const string PackageName = "com.martMuseum.TheARapp";
    private const string ActivityName = "com.unity3d.player.UnityPlayerActivity";
    private const double LaunchDelaySeconds = 3d;

    private static double launchAtTime;
    private static bool updateHooked;

    public int callbackOrder => 1000;

    [MenuItem("Tools/Android/Launch App on Connected Device")]
    public static void LaunchFromMenu()
    {
        TryLaunchOnDevice();
    }

    public static void ScheduleLaunchAfterInstall()
    {
        launchAtTime = EditorApplication.timeSinceStartup + LaunchDelaySeconds;
        if (!updateHooked)
        {
            EditorApplication.update += OnEditorUpdate;
            updateHooked = true;
        }
    }

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android || report.summary.result != BuildResult.Succeeded)
        {
            return;
        }

        // Launch only if the APK actually installed — otherwise am start would target a
        // package that is not on the device and we would log a misleading "launched" message.
        if (TryInstallApk(report.summary.outputPath))
        {
            ScheduleLaunchAfterInstall();
        }
    }

    private static bool TryInstallApk(string apkPath)
    {
        if (string.IsNullOrWhiteSpace(apkPath) || !File.Exists(apkPath))
        {
            Debug.LogWarning($"Android: APK не найден по пути '{apkPath}', установка пропущена.");
            return false;
        }

        string adbPath = ResolveAdbPath();
        if (string.IsNullOrEmpty(adbPath))
        {
            Debug.LogWarning("Android: adb не найден. APK собран, но не установлен автоматически.");
            return false;
        }

        RunAdb(adbPath, "reverse tcp:3001 tcp:3001");
        RunAdb(adbPath, "reverse tcp:3000 tcp:3000");

        AdbResult install = RunAdb(adbPath, $"install -r \"{apkPath}\"");
        string text = install.CombinedText;

        if (install.ExitCode == 0 && text.IndexOf("Failure", System.StringComparison.OrdinalIgnoreCase) < 0)
        {
            Debug.Log("Android: APK установлен на устройство.");
            return true;
        }

        // Surface the real reason instead of silently continuing to a launch that cannot work.
        if (text.IndexOf("INSTALL_FAILED_USER_RESTRICTED", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            Debug.LogError(
                "Android: установка запрещена телефоном (INSTALL_FAILED_USER_RESTRICTED). " +
                "На Xiaomi/MIUI включите «Установка через USB» в настройках для разработчиков " +
                "(нужен вход в Mi-аккаунт и SIM-карта) и подтвердите запрос установки на экране телефона.");
        }
        else if (text.IndexOf("INSTALL_FAILED_UPDATE_INCOMPATIBLE", System.StringComparison.OrdinalIgnoreCase) >= 0
                 || text.IndexOf("signatures do not match", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            Debug.LogError(
                "Android: установка не удалась — конфликт подписи с уже установленной версией " +
                "(INSTALL_FAILED_UPDATE_INCOMPATIBLE). Удалите приложение с телефона и соберите заново.");
        }
        else
        {
            Debug.LogError($"Android: установка APK не удалась. Ответ adb: {text.Trim()}");
        }

        return false;
    }

    private static void OnEditorUpdate()
    {
        if (launchAtTime <= 0d || EditorApplication.timeSinceStartup < launchAtTime)
        {
            return;
        }

        launchAtTime = 0d;
        TryLaunchOnDevice();
    }

    private static void TryLaunchOnDevice()
    {
        string packageName = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
        if (string.IsNullOrWhiteSpace(packageName))
        {
            packageName = PackageName;
        }

        string adbPath = ResolveAdbPath();
        if (string.IsNullOrEmpty(adbPath) || !File.Exists(adbPath))
        {
            Debug.LogWarning("Android: adb не найден. Откройте приложение вручную на телефоне.");
            return;
        }

        RunAdb(adbPath, "reverse tcp:3001 tcp:3001");
        RunAdb(adbPath, "reverse tcp:3000 tcp:3000");
        RunAdb(adbPath, "devices");

        AdbResult start = RunAdb(
            adbPath,
            $"shell am start -a android.intent.action.MAIN -c android.intent.category.LAUNCHER -n \"{packageName}/{ActivityName}\"");

        // am start returns exit code 0 even when it prints "Error: Activity ... does not exist",
        // so we must inspect the output rather than trust the exit code alone.
        if (start.ExitCode == 0 && start.CombinedText.IndexOf("Error", System.StringComparison.OrdinalIgnoreCase) < 0)
        {
            Debug.Log("Android: приложение запущено через adb.");
            return;
        }

        // Fallback: launch by package via monkey, which does not depend on the exact activity name.
        AdbResult monkey = RunAdb(adbPath, $"shell monkey -p {packageName} -c android.intent.category.LAUNCHER 1");
        if (monkey.ExitCode == 0 && monkey.CombinedText.IndexOf("Events injected: 1", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            Debug.Log("Android: приложение запущено через adb (monkey).");
            return;
        }

        Debug.LogError(
            $"Android: не удалось открыть приложение. Убедитесь, что пакет '{packageName}' установлен " +
            $"и экран телефона разблокирован. Ответ adb: {start.CombinedText.Trim()}");
    }

    private static string ResolveAdbPath()
    {
        string unityEditorFolder = Path.GetDirectoryName(EditorApplication.applicationPath);
        string[] sdkRoots =
        {
            AndroidExternalToolsSettings.sdkRootPath,
            EditorPrefs.GetString("AndroidSdkRoot"),
            string.IsNullOrWhiteSpace(unityEditorFolder)
                ? null
                : Path.Combine(unityEditorFolder, "Data", "PlaybackEngines", "AndroidPlayer", "SDK"),
            Path.Combine(
                EditorApplication.applicationContentsPath,
                "Data",
                "PlaybackEngines",
                "AndroidPlayer",
                "SDK"),
            Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                "Android",
                "Sdk")
        };

        for (int i = 0; i < sdkRoots.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(sdkRoots[i]))
            {
                continue;
            }

            string adbPath = Path.Combine(sdkRoots[i], "platform-tools", "adb.exe");
            if (File.Exists(adbPath))
            {
                Debug.Log($"Android: используем adb: {adbPath}");
                return adbPath;
            }
        }

        return null;
    }

    private struct AdbResult
    {
        public int ExitCode;
        public string Output;
        public string Error;

        // Combined stdout + stderr — adb tools (am start, monkey) print errors to either stream.
        public string CombinedText => $"{Output}\n{Error}";
    }

    private static AdbResult RunAdb(string adbPath, string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = adbPath,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using Process process = Process.Start(startInfo);
        if (process == null)
        {
            return new AdbResult { ExitCode = 1, Output = string.Empty, Error = "Не удалось запустить adb." };
        }

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit(8000);

        if (process.ExitCode == 0)
        {
            if (!string.IsNullOrWhiteSpace(output))
            {
                Debug.Log($"Android adb: {output.Trim()}");
            }
        }
        else if (!string.IsNullOrWhiteSpace(error))
        {
            Debug.LogWarning($"Android adb: {error.Trim()}");
        }

        return new AdbResult { ExitCode = process.ExitCode, Output = output, Error = error };
    }
}
#endif
