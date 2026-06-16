using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Keeps profile artworks UI objects present in the scene hierarchy (Edit mode + Play mode).
/// Attach to Canvas or leave on Canvas in The main stage scene.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class ProfileUiArtworksSceneAuthoring : MonoBehaviour
{
    [SerializeField] private bool setupOnEnable = true;

    private void OnEnable()
    {
        if (!setupOnEnable)
        {
            return;
        }

        Scene scene = gameObject.scene;
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return;
        }

        if (scene.name.Contains("main stage"))
        {
            if (!Application.isPlaying)
            {
                ProfilePanelDefaultVisibility.ApplyEditorPreview();
            }

            ProfileMainStageArtworksSetup.Configure();
            return;
        }

        ProfileUiArtworksHierarchyBuilder.EnsureAll(registerUndo: !Application.isPlaying);
    }
}
