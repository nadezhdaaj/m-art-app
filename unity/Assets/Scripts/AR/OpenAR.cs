using UnityEngine;
using UnityEngine.SceneManagement;

public class OpenARScene : MonoBehaviour
{
    public void OpenAR()
    {
        SceneManager.LoadScene("ARScene");
    }
}