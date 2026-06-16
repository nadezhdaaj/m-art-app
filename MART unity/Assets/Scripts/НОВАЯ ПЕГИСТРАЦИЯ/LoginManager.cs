using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

[System.Serializable]
public class LoginRequest
{
    public string email;
    public string password;
}

[System.Serializable]
public class LoginResponse
{
    public string id;
    public string username;
    public string email;
    public string avatarUrl;
    public string bio;
    public string message;
    public string token;

    public int points;
    public string title;
}

public class LoginManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_Text messageText;

    public void Login()
    {
        Debug.Log("Login button pressed");

        Debug.Log("emailInput object: " + (emailInput != null ? emailInput.name : "NULL"));
        Debug.Log("passwordInput object: " + (passwordInput != null ? passwordInput.name : "NULL"));

        string email = emailInput != null ? emailInput.text.Trim() : "";
        string password = passwordInput != null ? passwordInput.text : "";

        Debug.Log("email text: [" + email + "]");
        Debug.Log("password text length: " + password.Length);

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            if (messageText != null)
            {
                messageText.text = "\u0417\u0430\u043f\u043e\u043b\u043d\u0438\u0442\u0435 \u0432\u0441\u0435 \u043f\u043e\u043b\u044f";
            }

            return;
        }

        StartCoroutine(LoginCoroutine(email, password));
    }

    private IEnumerator LoginCoroutine(string email, string password)
    {
        LoginRequest data = new LoginRequest
        {
            email = email,
            password = password
        };

        string json = JsonUtility.ToJson(data);

        Debug.Log("BaseUrl: " + AppConfig.BaseUrl);

        UnityWebRequest request = new UnityWebRequest(AppConfig.BaseUrl + "/auth/login", "POST");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            LoginResponse response = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);

            if (UserSession.Instance == null)
            {
                if (messageText != null)
                {
                    messageText.text = "\u041e\u0448\u0438\u0431\u043a\u0430 \u0441\u0435\u0441\u0441\u0438\u0438";
                }

                yield break;
            }

            UserSession.Instance.UserId = response.id;
            UserSession.Instance.Username = response.username;
            UserSession.Instance.Email = response.email;
            UserSession.Instance.AvatarUrl = response.avatarUrl;
            UserSession.Instance.Bio = response.bio;
            UserSession.Instance.Points = response.points;
            UserSession.Instance.Title = response.title;

            if (!string.IsNullOrWhiteSpace(response.token) && BackendManager.instance != null)
            {
                BackendManager.instance.SyncSessionAfterSimpleAuth(
                    response.id,
                    response.email,
                    response.username,
                    response.avatarUrl,
                    response.token
                );
            }

            if (messageText != null)
            {
                messageText.text = "\u0412\u0445\u043e\u0434 \u0432\u044b\u043f\u043e\u043b\u043d\u0435\u043d \u0443\u0441\u043f\u0435\u0448\u043d\u043e";
            }

            yield return new WaitForSeconds(1f);
            SceneManager.LoadScene("The main stage");
        }
        else
        {
            string responseText = request.downloadHandler.text;

            if (messageText == null)
            {
                yield break;
            }

            if (responseText.Contains("User not found"))
            {
                messageText.text = "\u041f\u043e\u043b\u044c\u0437\u043e\u0432\u0430\u0442\u0435\u043b\u044c \u043d\u0435 \u043d\u0430\u0439\u0434\u0435\u043d";
            }
            else if (responseText.Contains("Invalid password"))
            {
                messageText.text = "\u041d\u0435\u0432\u0435\u0440\u043d\u044b\u0439 \u043f\u0430\u0440\u043e\u043b\u044c";
            }
            else
            {
                messageText.text = "\u041e\u0448\u0438\u0431\u043a\u0430 \u0432\u0445\u043e\u0434\u0430";
            }
        }
    }
}
