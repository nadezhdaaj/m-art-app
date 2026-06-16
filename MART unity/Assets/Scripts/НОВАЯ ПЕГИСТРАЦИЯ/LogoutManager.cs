using UnityEngine;
using UnityEngine.SceneManagement;

public class LogoutManager : MonoBehaviour
{
    public void Logout()
    {
        if (UserSession.Instance != null)
        {
            UserSession.Instance.ClearSession();
        }

        SceneManager.LoadScene("AuthScene");
    }
}
