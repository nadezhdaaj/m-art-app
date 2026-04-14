using UnityEngine;

public class BrushPanelToggle : MonoBehaviour
{
    public GameObject panel;

    public void Toggle()
    {
        panel.SetActive(!panel.activeSelf);
    }

    public void Close()
    {
        panel.SetActive(false);
    }
}