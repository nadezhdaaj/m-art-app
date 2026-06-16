using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Показывает уведомления в объекте NotificationText на активной сцене.
/// </summary>
public class ToastNotification : MonoBehaviour
{
    private const string NotificationTextObjectName = "NotificationText";

    private static ToastNotification instance;

    [SerializeField] private float defaultDuration = 2.5f;

    private TMP_Text messageText;
    private Coroutine hideCoroutine;
    private string boundSceneName;

    public static void Show(string message, float duration = 0f)
    {
        EnsureInstance();
        instance.ShowInternal(message, duration > 0f ? duration : instance.defaultDuration);
    }

    private static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }

        GameObject host = new GameObject("ToastNotification");
        DontDestroyOnLoad(host);
        instance = host.AddComponent<ToastNotification>();
    }

    private void ShowInternal(string message, float duration)
    {
        if (!TryResolveNotificationText())
        {
            Debug.LogWarning("ToastNotification: объект NotificationText не найден на сцене.");
            return;
        }

        messageText.text = message ?? string.Empty;
        messageText.gameObject.SetActive(true);

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }

        hideCoroutine = StartCoroutine(HideAfterDelay(duration));
    }

    private bool TryResolveNotificationText()
    {
        string activeSceneName = SceneManager.GetActiveScene().name;

        if (messageText != null && boundSceneName == activeSceneName)
        {
            return true;
        }

        messageText = null;
        boundSceneName = activeSceneName;

        GameObject notificationObject = FindNotificationTextInActiveScene();
        if (notificationObject == null)
        {
            return false;
        }

        messageText = notificationObject.GetComponent<TMP_Text>();
        return messageText != null;
    }

    private static GameObject FindNotificationTextInActiveScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !activeScene.isLoaded)
        {
            return null;
        }

        GameObject[] roots = activeScene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform found = FindChildRecursive(roots[i].transform, NotificationTextObjectName);
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

    private IEnumerator HideAfterDelay(float duration)
    {
        yield return new WaitForSeconds(duration);

        if (messageText != null)
        {
            messageText.text = string.Empty;
            messageText.gameObject.SetActive(false);
        }

        hideCoroutine = null;
    }
}
