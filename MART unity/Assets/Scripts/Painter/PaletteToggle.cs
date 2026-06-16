using UnityEngine;

public class PaletteToggle : MonoBehaviour
{
    public GameObject palettePanel;

    private bool isOpen = false;

    public void TogglePalette()
    {
        isOpen = !isOpen;
        palettePanel.SetActive(isOpen);
    }
}