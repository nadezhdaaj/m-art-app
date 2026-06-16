using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

[System.Serializable]
public class RegisterRequest
{
    public string username;
    public string email;
    public string password;
}

public class RegisterManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_InputField confirmPasswordInput;
    [SerializeField] private TMP_Text messageText;

    public void Register()
    {
        Debug.Log("Register button");

        Debug.Log("usernameInput object: " + (usernameInput != null ? usernameInput.name : "NULL"));
        Debug.Log("emailInput object: " + (emailInput != null ? emailInput.name : "NULL"));
        Debug.Log("passwordInput object: " + (passwordInput != null ? passwordInput.name : "NULL"));
        Debug.Log("confirmPasswordInput object: " + (confirmPasswordInput != null ? confirmPasswordInput.name : "NULL"));

        string username = usernameInput != null ? usernameInput.text.Trim() : "";
        string email = emailInput != null ? emailInput.text.Trim() : "";
        string password = passwordInput != null ? passwordInput.text : "";
        string confirmPassword = confirmPasswordInput != null ? confirmPasswordInput.text : "";

        Debug.Log("username text: [" + username + "]");
        Debug.Log("email text: [" + email + "]");
        Debug.Log("password length: " + password.Length);
        Debug.Log("confirmPassword length: " + confirmPassword.Length);

        if (string.IsNullOrEmpty(username) ||
            string.IsNullOrEmpty(email) ||
            string.IsNullOrEmpty(password) ||
            string.IsNullOrEmpty(confirmPassword))
        {
            if (messageText != null)
            {
                messageText.text = "\u0417\u0430\u043f\u043e\u043b\u043d\u0438\u0442\u0435 \u0432\u0441\u0435 \u043f\u043e\u043b\u044f";
            }

            return;
        }

        if (password != confirmPassword)
        {
            if (messageText != null)
            {
                messageText.text = "\u041f\u0430\u0440\u043e\u043b\u0438 \u043d\u0435 \u0441\u043e\u0432\u043f\u0430\u0434\u0430\u044e\u0442";
            }

            return;
        }

        StartCoroutine(RegisterCoroutine(username, email, password));
    }


    private IEnumerator RegisterCoroutine(string username, string email, string password)
    {
        RegisterRequest data = new RegisterRequest
        {
            username = username,
            email = email,
            password = password
        };

        string json = JsonUtility.ToJson(data);

        Debug.Log("BaseUrl: " + AppConfig.BaseUrl);

        UnityWebRequest request = new UnityWebRequest(AppConfig.BaseUrl + "/auth/register", "POST");

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

            if (!string.IsNullOrEmpty(response.id))
            {
                UserSession.Instance.UserId = response.id;
                UserSession.Instance.Username = response.username;
                UserSession.Instance.Email = response.email;
                UserSession.Instance.AvatarUrl = response.avatarUrl;
                UserSession.Instance.Bio = response.bio;
                UserSession.Instance.Points = response.points;
                UserSession.Instance.Title = response.title;
            }

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
                messageText.text = "\u0420\u0435\u0433\u0438\u0441\u0442\u0440\u0430\u0446\u0438\u044f \u0443\u0441\u043f\u0435\u0448\u043d\u0430";
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

            if (responseText.Contains("User already exists"))
            {
                messageText.text = "\u041f\u043e\u043b\u044c\u0437\u043e\u0432\u0430\u0442\u0435\u043b\u044c \u0441 \u0442\u0430\u043a\u0438\u043c email \u0443\u0436\u0435 \u0437\u0430\u0440\u0435\u0433\u0438\u0441\u0442\u0440\u0438\u0440\u043e\u0432\u0430\u043d";
            }
            else
            {
                messageText.text = "\u041e\u0448\u0438\u0431\u043a\u0430 \u0440\u0435\u0433\u0438\u0441\u0442\u0440\u0430\u0446\u0438\u0438";
            }
        }
    }
}
