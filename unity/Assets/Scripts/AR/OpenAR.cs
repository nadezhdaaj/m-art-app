using UnityEngine;
using UnityEngine.SceneManagement;

public class OpenARScene : MonoBehaviour
{
    public const string OpenHomeOnMainStageKey = "OpenHomeOnMainStage";

    public void OpenAR()
    {
        SceneManager.LoadScene("ARScene");
    }

    public void BackToMainStageHome()
    {
        PlayerPrefs.SetInt(OpenHomeOnMainStageKey, 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene("The main stage");
    }
}
