using UnityEngine;

public class BrushSelectionPanel : MonoBehaviour
{
    public GameObject panelRoot;

    private void Start()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void TogglePanel()
    {
        if (panelRoot == null)
            return;

        panelRoot.SetActive(!panelRoot.activeSelf);
    }

    public void ShowPanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    public void HidePanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }
}
