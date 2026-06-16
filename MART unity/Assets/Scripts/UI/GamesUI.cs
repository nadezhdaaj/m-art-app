using UnityEngine;

public class GamesUI : MonoBehaviour
{
    public GameObject gamesPanel;
    public GameObject mainPanel;

    public void OpenMiniGame()
    {
        mainPanel.SetActive(false);
        gamesPanel.SetActive(true);

        QuizManager quiz = FindObjectOfType<QuizManager>(true);
        if (quiz != null)
            quiz.Restart();
    }

    public void CloseMiniGame()
    {
        if (gamesPanel != null)
        {
            LiveContourMiniGame[] miniGames = gamesPanel.GetComponentsInChildren<LiveContourMiniGame>(true);
            for (int i = 0; i < miniGames.Length; i++)
            {
                if (miniGames[i] != null)
                    miniGames[i].CloseSession();
            }
        }

        gamesPanel.SetActive(false);
        mainPanel.SetActive(true);
    }
}
