using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mart.Editor
{
    public static class ProfileUiArtworksSceneSetup
    {
        private const string ScenePath = "Assets/Scenes/The main stage.unity";

        [MenuItem("MART/Setup Profile Artworks UI In Scene")]
        [MenuItem("Tools/Museum AR/Setup Profile Artworks UI In Scene")]
        public static void SetupFromMenu()
        {
            SetupScene(saveScene: true, showDialog: true);
        }

        public static void BuildFromCommandLine()
        {
            SetupScene(saveScene: true, showDialog: false);
        }

        [InitializeOnLoadMethod]
        private static void RegisterSceneOpenedHook()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (!IsMainStageScene(scene))
            {
                return;
            }

            EditorApplication.delayCall += () =>
            {
                if (Application.isPlaying || !IsMainStageScene(SceneManager.GetActiveScene()))
                {
                    return;
                }

                EnsureHierarchyInOpenScene(markDirty: true);
                EnsureCanvasAuthoringComponent();
            };
        }

        private static void SetupScene(bool saveScene, bool showDialog)
        {
            Scene scene = EditorSceneManager.GetActiveScene();
            if (!IsMainStageScene(scene))
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            EnsureHierarchyInOpenScene(markDirty: true);

            if (EnsureCanvasAuthoringComponent())
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }

            if (saveScene)
            {
                EditorSceneManager.SaveScene(scene);
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Profile Artworks UI",
                    "See All, User's work, OtherPanel scroll and gallery hierarchy are now in the scene.",
                    "OK");
            }
        }

        private static void EnsureHierarchyInOpenScene(bool markDirty)
        {
            OtherPanelScrollRevert.Restore();
            ProfilePanelDefaultVisibility.ApplyEditorPreview();
            ProfileUiArtworksHierarchyBuilder.EnsureAll(registerUndo: true);
            ProfileMainStageArtworksSetup.Configure();

            if (markDirty && !Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }
        }

        private static bool EnsureCanvasAuthoringComponent()
        {
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                return false;
            }

            if (canvas.GetComponent<ProfileUiArtworksSceneAuthoring>() != null)
            {
                return false;
            }

            Undo.AddComponent<ProfileUiArtworksSceneAuthoring>(canvas.gameObject);
            return true;
        }

        private static bool IsMainStageScene(Scene scene)
        {
            return scene.IsValid() &&
                   (scene.path == ScenePath || scene.name.Contains("main stage"));
        }
    }
}
