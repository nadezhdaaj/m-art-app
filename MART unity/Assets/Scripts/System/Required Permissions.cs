using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Management;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

/// <summary>
/// Requests camera permission, starts XR/ARCore, then enables AR camera feed.
/// Fixes black screen when permission dialog appears after AR session already started.
/// </summary>
[DefaultExecutionOrder(-200)]
public class CameraPermissionRequester : MonoBehaviour
{
    private void Awake()
    {
        DisableArUntilReady();
    }

    private void Start()
    {
        StartCoroutine(InitializeArCameraFeed());
    }

    private static void DisableArUntilReady()
    {
        ARSession session = Object.FindObjectOfType<ARSession>();
        if (session != null)
        {
            session.enabled = false;
        }

        ARCameraManager cameraManager = Object.FindObjectOfType<ARCameraManager>();
        if (cameraManager != null)
        {
            cameraManager.enabled = false;
        }
    }

    private IEnumerator InitializeArCameraFeed()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Permission.RequestUserPermission(Permission.Camera);

            const float timeoutSeconds = 60f;
            float elapsed = 0f;
            while (!Permission.HasUserAuthorizedPermission(Permission.Camera) && elapsed < timeoutSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Debug.LogError("AR: доступ к камере не выдан. Включите камеру в настройках приложения.");
            yield break;
        }
#endif

        yield return StartXrLoaderIfNeeded();
        yield return StartArSessionWhenReady();

        ARCameraManager cameraManager = Object.FindObjectOfType<ARCameraManager>();
        if (cameraManager != null)
        {
            cameraManager.enabled = true;
        }

        ARCameraBackground background = Object.FindObjectOfType<ARCameraBackground>();
        if (background != null)
        {
            background.enabled = true;
        }
    }

    private static IEnumerator StartXrLoaderIfNeeded()
    {
        XRGeneralSettings xrSettings = XRGeneralSettings.Instance;
        if (xrSettings == null || xrSettings.Manager == null)
        {
            yield break;
        }

        XRManagerSettings manager = xrSettings.Manager;

        if (manager.activeLoader == null)
        {
            manager.InitializeLoaderSync();
            yield return null;
        }

        if (manager.activeLoader == null)
        {
            Debug.LogError("AR: ARCore loader не запустился. Установите Google Play Services for AR.");
            yield break;
        }

        manager.StartSubsystems();
    }

    private static IEnumerator StartArSessionWhenReady()
    {
        ARSession session = Object.FindObjectOfType<ARSession>();
        if (session == null)
        {
            Debug.LogError("AR: ARSession не найден на сцене.");
            yield break;
        }

        if (ARSession.state == ARSessionState.None ||
            ARSession.state == ARSessionState.CheckingAvailability)
        {
            yield return ARSession.CheckAvailability();
        }

        if (ARSession.state == ARSessionState.Unsupported)
        {
            Debug.LogError("AR: устройство не поддерживает AR (ARCore).");
            yield break;
        }

        if (ARSession.state == ARSessionState.NeedsInstall)
        {
            yield return ARSession.Install();
        }

        if (ARSession.state == ARSessionState.Ready || ARSession.state == ARSessionState.SessionInitializing)
        {
            session.enabled = true;
            session.Reset();
            yield return null;
        }
        else
        {
            Debug.LogWarning($"AR: сессия не готова, состояние: {ARSession.state}");
        }
    }
}
