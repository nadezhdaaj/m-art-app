using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SplashController : MonoBehaviour
{
    public float displayTime = 2f; 

    void Start()
    {
        StartCoroutine(LoadNextScene());
    }

    IEnumerator LoadNextScene()
    {
        yield return new WaitForSeconds(displayTime);
        const string authScene = "AuthScene";
        if (Application.CanStreamedLevelBeLoaded(authScene))
        {
            SceneManager.LoadScene(authScene);
            yield break;
        }

        Debug.LogError("AuthScene не в Build Settings. Включите галочку у Scenes/AuthScene.");
        SceneManager.LoadScene("The main stage");
    }
}