using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mart.Editor
{
    public static class OtherPanelScrollbarSceneSetup
    {
        private const string ScenePath = "Assets/Scenes/The main stage.unity";

        [MenuItem("MART/Bake OtherPanel Scroll Hierarchy In Scene")]
        [MenuItem("Tools/Museum AR/Bake OtherPanel Scroll Hierarchy In Scene")]
        public static void BakeFromMenu()
        {
            BakeScene(saveScene: true, showDialog: true);
        }

        public static void BuildFromCommandLine()
        {
            BakeScene(saveScene: true, showDialog: false);
        }

        public static void BakeScene(bool saveScene, bool showDialog)
        {
            Scene scene = EditorSceneManager.GetActiveScene();
            if (!IsMainStageScene(scene))
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            GameObject otherPanel = GameObject.Find("OtherPanel");
            if (otherPanel == null)
            {
                EditorUtility.DisplayDialog("OtherPanel", "OtherPanel not found in the scene.", "OK");
                return;
            }

            OtherPanelScrollbarHierarchy.Bake(otherPanel.transform, registerUndo: true);

            OtherPanelScrollbarController controller = otherPanel.GetComponent<OtherPanelScrollbarController>();
            if (controller != null)
            {
                controller.Configure();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = otherPanel;
            EditorGUIUtility.PingObject(otherPanel);

            if (saveScene)
            {
                EditorSceneManager.SaveScene(scene);
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "OtherPanel scroll hierarchy",
                    "Hierarchy saved in the scene.\n\n" +
                    "OtherPanel\n" +
                    "  OtherPanelScrollContent\n" +
                    "    (panel content)\n" +
                    "  scrollbar",
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
