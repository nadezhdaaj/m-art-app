using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fallback wiring for the "to send" button on the naming panel.
/// </summary>
[DisallowMultipleComponent]
public class ArtworkNamingSubmitButton : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(HandleClick);
        button.onClick.AddListener(HandleClick);
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
        }
    }

    private void HandleClick()
    {
        PaintArtworkController controller = FindObjectOfType<PaintArtworkController>(true);
        if (controller == null)
        {
            Debug.LogWarning("ArtworkNamingSubmitButton: PaintArtworkController не найден.");
            return;
        }

        controller.SubmitArtworkNaming();
    }
}
