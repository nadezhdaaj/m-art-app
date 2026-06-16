using UnityEngine;
using UnityEngine.EventSystems;

public static class AndroidUiInputFix
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ApplyAfterSceneLoad()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Apply();
#endif
    }

    private static void Apply()
    {
        EventSystem[] systems = Object.FindObjectsOfType<EventSystem>();
        EventSystem primary = null;

        for (int i = 0; i < systems.Length; i++)
        {
            if (systems[i] == null)
            {
                continue;
            }

            if (primary == null)
            {
                primary = systems[i];
                continue;
            }

            Object.Destroy(systems[i].gameObject);
        }

        if (primary == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            primary = eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        StandaloneInputModule standaloneModule = primary.GetComponent<StandaloneInputModule>();
        if (standaloneModule == null)
        {
            standaloneModule = primary.gameObject.AddComponent<StandaloneInputModule>();
        }

        standaloneModule.forceModuleActive = true;
        primary.enabled = true;

        DisableInputSystemUiModule(primary);
    }

    private static void DisableInputSystemUiModule(EventSystem eventSystem)
    {
        Behaviour[] behaviours = eventSystem.GetComponents<Behaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            Behaviour behaviour = behaviours[i];
            if (behaviour == null || behaviour is EventSystem || behaviour is StandaloneInputModule)
            {
                continue;
            }

            string typeName = behaviour.GetType().FullName;
            if (typeName != null && typeName.Contains("InputSystemUIInputModule"))
            {
                behaviour.enabled = false;
            }
        }
    }
}
