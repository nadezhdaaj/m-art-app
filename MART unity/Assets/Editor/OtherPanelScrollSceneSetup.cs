using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mart.Editor
{
    public static class OtherPanelScrollSceneSetup
    {
        private const string ScenePath = "Assets/Scenes/The main stage.unity";

        [MenuItem("MART/Restore OtherPanel Layout")]
        [MenuItem("Tools/Museum AR/Restore OtherPanel Layout")]
        public static void RestoreFromMenu()
        {
            Scene scene = EditorSceneManager.GetActiveScene();
            if (!IsMainStageScene(scene))
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            OtherPanelScrollRevert.Restore();
            ProfileMainStageArtworksSetup.Configure();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            EditorUtility.DisplayDialog(
                "OtherPanel restored",
                "Layout on OtherPanel (tab Prochee) has been restored.",
                "OK");
        }

        [MenuItem("MART/Setup OtherPanel Scroll In Scene")]
        [MenuItem("Tools/Museum AR/Setup OtherPanel Scroll In Scene")]
        public static void SetupFromMenu()
        {
            SetupScene(saveScene: true, showDialog: true);
        }

        public static void BuildFromCommandLine()
        {
            SetupScene(saveScene: true, showDialog: false);
        }

        private static void SetupScene(bool saveScene, bool showDialog)
        {
            Scene scene = EditorSceneManager.GetActiveScene();
            if (!IsMainStageScene(scene))
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            OtherPanelScrollbarSceneSetup.BakeScene(saveScene: false, showDialog: false);
            ProfileUiArtworksHierarchyBuilder.EnsureAll(registerUndo: true);
            ProfileMainStageArtworksSetup.Configure();
            EditorSceneManager.MarkSceneDirty(scene);

            GameObject otherPanel = GameObject.Find("OtherPanel");
            if (otherPanel != null)
            {
                Selection.activeGameObject = otherPanel;
                EditorGUIUtility.PingObject(otherPanel);
            }

            if (saveScene)
            {
                EditorSceneManager.SaveScene(scene);
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "OtherPanel Scroll",
                    "OtherPanel scroll hierarchy saved in the scene.\n\n" +
                    "OtherPanel\n  OtherPanelScroll\n    OtherPanelScrollViewport\n      OtherPanelContent\n    OtherPanelScrollHandle",
                    "OK");
            }
        }

        private static bool IsMainStageScene(Scene scene)
        {
            return scene.IsValid() &&
                   (scene.path == ScenePath || scene.name.Contains("main stage"));
        }
    }
}
