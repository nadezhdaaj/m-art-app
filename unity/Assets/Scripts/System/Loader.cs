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
        SceneManager.LoadScene("The main stage");
    }
}