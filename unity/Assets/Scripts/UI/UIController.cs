using UnityEngine;

public class UIController : MonoBehaviour
{
    [SerializeField] private GameObject infoPanel;


    public void ToggleInfo()
    {
        if (infoPanel != null)
            infoPanel.SetActive(!infoPanel.activeSelf);
    }
}