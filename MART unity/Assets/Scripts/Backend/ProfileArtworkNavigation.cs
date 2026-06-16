using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Opens a saved artwork in the paint workspace from profile UI (carousel / User's work).
/// </summary>
public static class ProfileArtworkNavigation
{
    public static void OpenArtworkForEditing(ArtworkDto artwork, string paintSceneName = "The main stage")
    {
        if (artwork == null || BackendManager.instance == null)
        {
            return;
        }

        UserWorksScreen.HideScreen();
        BackendManager.instance.BeginArtworkEditing(artwork);

        if (string.IsNullOrWhiteSpace(paintSceneName))
        {
            return;
        }

        if (SceneManager.GetActiveScene().name == paintSceneName)
        {
            PaintArtworkController.PresentPendingArtworkInPaintWorkspace();
            return;
        }

        SceneManager.LoadScene(paintSceneName);
    }
}
