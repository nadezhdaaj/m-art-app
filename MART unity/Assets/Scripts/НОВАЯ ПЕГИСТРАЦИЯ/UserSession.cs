using UnityEngine;

public class UserSession : MonoBehaviour
{
    public static UserSession Instance;

    public string UserId;
    public string Username;
    public string Email;
    public string AvatarUrl;
    public string Bio;
    public int Points;
    public string Title;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ClearSession()
    {
        UserId = "";
        Username = "";
        Email = "";
        AvatarUrl = "";
        Bio = "";

    }
}
