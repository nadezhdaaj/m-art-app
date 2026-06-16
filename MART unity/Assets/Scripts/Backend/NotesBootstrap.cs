using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Автоматически вешает <see cref="NotesController"/> на панель заметок в рантайме,
/// чтобы не нужно было прикреплять компонент вручную в сцене.
/// Панель ищется по имени (включая выключенные объекты).
/// </summary>
public static class NotesBootstrap
{
    private static readonly string[] PanelNameCandidates =
    {
        "заметки панель",
        "заметки",
        "NotesPanel",
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureController();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureController();
    }

    public static void EnsureController()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        GameObject panel = FindNotesPanel();
        if (panel == null)
        {
            return;
        }

        if (panel.GetComponent<NotesController>() == null)
        {
            panel.AddComponent<NotesController>();
        }
    }

    private static GameObject FindNotesPanel()
    {
        // 1) Точное имя (включая неактивные объекты).
        for (int i = 0; i < PanelNameCandidates.Length; i++)
        {
            GameObject byName = FindInSceneByName(PanelNameCandidates[i]);
            if (byName != null)
            {
                return byName;
            }
        }

        // 2) Любой объект, в имени которого есть «замет».
        return FindInSceneContaining("замет");
    }

    private static GameObject FindInSceneByName(string targetName)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return null;
        }

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform[] all = roots[i].GetComponentsInChildren<Transform>(true);
            for (int j = 0; j < all.Length; j++)
            {
                if (all[j].name == targetName)
                {
                    return all[j].gameObject;
                }
            }
        }

        return null;
    }

    private static GameObject FindInSceneContaining(string fragment)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return null;
        }

        string needle = fragment.ToLowerInvariant();
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform[] all = roots[i].GetComponentsInChildren<Transform>(true);
            for (int j = 0; j < all.Length; j++)
            {
                if (all[j].name.ToLowerInvariant().Contains(needle))
                {
                    return all[j].gameObject;
                }
            }
        }

        return null;
    }
}
