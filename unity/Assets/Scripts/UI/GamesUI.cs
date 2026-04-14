using UnityEngine;

public class GamesUI : MonoBehaviour
{
    public GameObject gamesPanel;
    public GameObject mainPanel;

    public void OpenMiniGame()
    {
        mainPanel.SetActive(false);
        gamesPanel.SetActive(true);
    }

    public void CloseMiniGame()
    {
        gamesPanel.SetActive(false);
        mainPanel.SetActive(true);
    }
}