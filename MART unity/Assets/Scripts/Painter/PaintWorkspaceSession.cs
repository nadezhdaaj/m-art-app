using UnityEngine;

/// <summary>
/// When the paint workspace (PaintCanvas) opens for a new visit, start a blank artwork session.
/// Skips reset when the user opened a saved work from the profile.
/// </summary>
[DisallowMultipleComponent]
public class PaintWorkspaceSession : MonoBehaviour
{
    private void OnEnable()
    {
        if (ShouldOpenExistingArtworkForEditing())
        {
            return;
        }

        PaintArtworkController paintController = FindObjectOfType<PaintArtworkController>(true);
        if (paintController != null)
        {
            paintController.BeginNewArtwork();
        }
    }

    private static bool ShouldOpenExistingArtworkForEditing()
    {
        if (BackendManager.instance == null)
        {
            return false;
        }

        if (BackendManager.instance.ShouldDeferArtworkAutoLoad)
        {
            return true;
        }

        return BackendManager.instance.TryPeekPendingArtwork(out _);
    }
}
