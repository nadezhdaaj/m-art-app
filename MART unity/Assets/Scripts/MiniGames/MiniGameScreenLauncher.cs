using UnityEngine;

public class MiniGameScreenLauncher : MonoBehaviour
{
    public void OpenMiniGame()
    {
        LiveContourMiniGame miniGame = FindMiniGame();
        if (miniGame == null)
        {
            Debug.LogWarning("MiniGameScreenLauncher: LiveContourMiniGame not found in scene.");
            return;
        }

        miniGame.OpenMiniGameScreen();
    }

    private static LiveContourMiniGame FindMiniGame()
    {
        LiveContourMiniGame[] miniGames = Resources.FindObjectsOfTypeAll<LiveContourMiniGame>();
        for (int i = 0; i < miniGames.Length; i++)
        {
            LiveContourMiniGame candidate = miniGames[i];
            if (candidate != null && candidate.gameObject.scene.IsValid())
                return candidate;
        }

        return null;
    }
}
