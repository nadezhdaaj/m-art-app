using UnityEngine;
using UnityEngine.SceneManagement;

public class Loader : MonoBehaviour
{
    void Start()
    {
        Invoke("LoadMenu", 2f);
    }

    void LoadMenu()
    {
        const string authScene = "AuthScene";
        if (Application.CanStreamedLevelBeLoaded(authScene))
        {
            SceneManager.LoadScene(authScene);
            return;
        }

        Debug.LogError(
            "AuthScene не добавлена в Build Settings (галочка снята). " +
            "File → Build Settings → включите Scenes/AuthScene. Временно открываем главный экран.");
        SceneManager.LoadScene("The main stage");
    }
}
