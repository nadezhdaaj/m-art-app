using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mart.Editor
{
    /// <summary>
    /// Строит NotesRuntimePanel (превью-список заметок) как настоящие объекты в сцене,
    /// чтобы он был виден и редактируем в иерархии, а не только в Play.
    /// </summary>
    [InitializeOnLoad]
    public static class NotesPanelSceneSetup
    {
        // Срабатывает при загрузке редактора / перекомпиляции скриптов.
        static NotesPanelSceneSetup()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.delayCall += () => EnsureForOpenScene(markDirty: true);
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            EditorApplication.delayCall += () => EnsureForOpenScene(markDirty: true);
        }

        /// <summary>
        /// Находит «заметки панель», вешает NotesController и строит NotesRuntimePanel в иерархии,
        /// если этого ещё нет. Запускается автоматически — кликать никуда не нужно.
        /// </summary>
        private static void EnsureForOpenScene(bool markDirty)
        {
            if (Application.isPlaying)
            {
                return;
            }

            GameObject panel = FindNotesPanel();
            if (panel == null)
            {
                return;
            }

            bool changed = false;

            NotesController controller = panel.GetComponent<NotesController>();
            if (controller == null)
            {
                controller = Undo.AddComponent<NotesController>(panel);
                changed = true;
            }

            if (!controller.HasListContainer)
            {
                controller.BuildContainers();
                changed = true;
            }

            if (changed && markDirty)
            {
                EditorUtility.SetDirty(controller);
                EditorSceneManager.MarkSceneDirty(panel.scene);
            }
        }

        [MenuItem("MART/Build Notes Preview Panel In Scene")]
        [MenuItem("Tools/Museum AR/Build Notes Preview Panel In Scene")]
        public static void BuildFromMenu()
        {
            GameObject panel = FindNotesPanel();
            if (panel == null)
            {
                EditorUtility.DisplayDialog(
                    "Notes",
                    "Не найден объект «заметки панель» в открытой сцене. Открой нужную сцену и попробуй снова.",
                    "OK");
                return;
            }

            EnsureForOpenScene(markDirty: true);

            Transform runtimePanel = FindByName(panel.transform, "NotesRuntimePanel");
            if (runtimePanel != null)
            {
                Selection.activeGameObject = runtimePanel.gameObject;
            }
        }

        private static GameObject FindNotesPanel()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            GameObject[] roots = scene.GetRootGameObjects();

            // 1) точное имя
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] all = roots[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < all.Length; j++)
                {
                    if (all[j].name == "заметки панель")
                    {
                        return all[j].gameObject;
                    }
                }
            }

            // 2) по фрагменту «замет»
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] all = roots[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < all.Length; j++)
                {
                    if (all[j].name.ToLowerInvariant().Contains("замет"))
                    {
                        return all[j].gameObject;
                    }
                }
            }

            return null;
        }

        private static Transform FindByName(Transform root, string targetName)
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name == targetName)
                {
                    return all[i];
                }
            }

            return null;
        }
    }
}
