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
        SceneManager.LoadScene("ARScene"); 
    }
}