using UnityEngine;

public class ProfileActionsPanel : MonoBehaviour
{
    [SerializeField] private GameObject panelObject;

    public void TogglePanel()
    {
        if (panelObject != null)
        {
            panelObject.SetActive(!panelObject.activeSelf);
        }
    }

    public void OpenPanel()
    {
        if (panelObject != null)
        {
            panelObject.SetActive(true);
        }
    }

    public void ClosePanel()
    {
        if (panelObject != null)
        {
            panelObject.SetActive(false);
        }
    }
}
