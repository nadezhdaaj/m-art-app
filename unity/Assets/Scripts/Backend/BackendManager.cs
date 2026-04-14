using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BackendManager : MonoBehaviour
{
    public static BackendManager instance;

    [Header("Backend")]
    [SerializeField] private string backendBaseUrl = "http://localhost:3000";

    [Header("Login References")]
    [SerializeField] private TMP_InputField loginEmail;
    [SerializeField] private TMP_InputField loginPassword;
    [SerializeField] private TMP_Text loginOutputText;

    [Header("Register References")]
    [SerializeField] private TMP_InputField registerUsername;
    [SerializeField] private TMP_InputField registerEmail;
    [SerializeField] private TMP_InputField registerPassword;
    [SerializeField] private TMP_InputField registerConfimPassword;
    [SerializeField] private TMP_Text registerOutputText;

    private BackendAuthApiClient apiClient;

    public AuthUserDto CurrentUser { get; private set; }
    public ProfileDto CurrentProfile { get; private set; }
    public string CurrentToken => SessionStorage.LoadToken();
    public bool HasAuthorizedSession => !string.IsNullOrWhiteSpace(CurrentToken) && CurrentUser != null;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        apiClient = new BackendAuthApiClient(backendBaseUrl);
    }

    private void Start()
    {
        StartCoroutine(RestoreSession());
    }

    public void ClearOutputs()
    {
        if (loginOutputText != null)
        {
            loginOutputText.text = string.Empty;
        }

        if (registerOutputText != null)
        {
            registerOutputText.text = string.Empty;
        }
    }

    public void LoginButton()
    {
        ClearOutputs();
        StartCoroutine(LoginLogic(loginEmail.text, loginPassword.text));
    }

    public void RegisterButton()
    {
        ClearOutputs();
        StartCoroutine(RegisterLogic(
            registerUsername.text,
            registerPassword.text,
            registerEmail.text,
            registerConfimPassword.text
        ));
    }

    public void UpdateProfilePicture(string url)
    {
        Debug.Log($"Avatar URL flow is disabled during backend migration: {url}");
        LobbyManager.instance?.Output("Смена аватара теперь должна идти через загрузку файла. Этот экран будет переделан следующим шагом.");
    }

    public void ChangeEmail(string email)
    {
        Debug.Log($"Email change is not mapped to backend yet: {email}");
        LobbyManager.instance?.Output("Смена почты ещё не переведена на backend route.");
    }

    public void ChangePassword(string password)
    {
        Debug.Log($"ChangePassword requested with new password length: {password?.Length ?? 0}");
        LobbyManager.instance?.Output("Для смены пароля backend требует текущий пароль. Добавьте поле current password в UI.");
    }

    public void SignOut()
    {
        StartCoroutine(SignOutLogic());
    }

    public void ClearSavedSessionAndShowLogin()
    {
        ClearSession();

        if (SceneManager.GetActiveScene().name == "Registration")
        {
            AuthUIManager.instance?.LoginScreen();
            return;
        }

        SceneManager.LoadScene("Registration");
    }

    public void EnterApp()
    {
        if (CurrentUser != null)
        {
            SceneManager.LoadScene(2);
        }
    }

    public IEnumerator RefreshProfile()
    {
        string token = CurrentToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            yield break;
        }

        ApiResult<ProfileDto> result = null;
        yield return StartCoroutine(apiClient.GetProfile(token, response => result = response));

        if (result == null)
        {
            yield break;
        }

        if (result.Unauthorized)
        {
            HandleUnauthorized();
            yield break;
        }

        if (result.Success)
        {
            CurrentProfile = result.Data;
        }
    }

    private IEnumerator RestoreSession()
    {
        string token = CurrentToken;
        string activeScene = SceneManager.GetActiveScene().name;

        if (string.IsNullOrWhiteSpace(token))
        {
            if (activeScene == "Registration")
            {
                AuthUIManager.instance?.WelcomeScreen();
            }
            else
            {
                SceneManager.LoadScene("Registration");
            }

            yield break;
        }

        if (activeScene == "Registration")
        {
            AuthUIManager.instance?.CheckingForAccountScreen();
        }

        ApiResult<SessionResponseDto> sessionResult = null;
        yield return StartCoroutine(apiClient.GetSession(token, response => sessionResult = response));

        if (sessionResult == null || !sessionResult.Success || sessionResult.Data?.user == null)
        {
            HandleUnauthorized();
            yield break;
        }

        CurrentUser = sessionResult.Data.user;
        yield return StartCoroutine(RefreshProfile());

        if (CurrentUser == null)
        {
            yield break;
        }

        if (activeScene == "Registration")
        {
            EnterApp();
            yield break;
        }

        LobbyManager.instance?.LoadProfile();
    }

    private IEnumerator LoginLogic(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            loginOutputText.text = "Введите почту";
            yield break;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            loginOutputText.text = "Введите пароль";
            yield break;
        }

        ApiResult<AuthResponseDto> result = null;
        yield return StartCoroutine(apiClient.SignIn(new SignInRequestDto
        {
            email = email.Trim(),
            password = password,
            rememberMe = true,
        }, response => result = response));

        if (result == null || !result.Success || result.Data?.user == null || string.IsNullOrWhiteSpace(result.Data.token))
        {
            loginOutputText.text = MapAuthError(result?.ErrorMessage, false);
            yield break;
        }

        SaveSession(result.Data.user, result.Data.token);
        yield return StartCoroutine(FinalizeAuthorizedSession(loginOutputText, true));
    }

    private IEnumerator RegisterLogic(string username, string password, string email, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            registerOutputText.text = "Введите имя";
            yield break;
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            registerOutputText.text = "Введите почту";
            yield break;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            registerOutputText.text = "Введите пароль";
            yield break;
        }

        if (password != confirmPassword)
        {
            registerOutputText.text = "Пароли не совпадают";
            yield break;
        }

        ApiResult<AuthResponseDto> signUpResult = null;
        yield return StartCoroutine(apiClient.SignUp(new SignUpRequestDto
        {
            name = username.Trim(),
            email = email.Trim(),
            password = password,
            rememberMe = true,
        }, response => signUpResult = response));

        if (signUpResult == null || !signUpResult.Success)
        {
            registerOutputText.text = MapAuthError(signUpResult?.ErrorMessage, true);
            yield break;
        }

        AuthUserDto user = signUpResult.Data?.user;
        string token = signUpResult.Data?.token;

        if (string.IsNullOrWhiteSpace(token))
        {
            ApiResult<AuthResponseDto> signInResult = null;
            yield return StartCoroutine(apiClient.SignIn(new SignInRequestDto
            {
                email = email.Trim(),
                password = password,
                rememberMe = true,
            }, response => signInResult = response));

            if (signInResult == null || !signInResult.Success || signInResult.Data?.user == null || string.IsNullOrWhiteSpace(signInResult.Data.token))
            {
                registerOutputText.text = "Регистрация прошла, но не удалось восстановить сессию";
                yield break;
            }

            user = signInResult.Data.user;
            token = signInResult.Data.token;
        }

        SaveSession(user, token);
        yield return StartCoroutine(FinalizeAuthorizedSession(registerOutputText, true));
    }

    private IEnumerator FinalizeAuthorizedSession(TMP_Text outputText, bool enterApp)
    {
        yield return StartCoroutine(RefreshProfile());

        if (CurrentUser == null)
        {
            outputText.text = "Сессия потеряна. Попробуйте войти снова.";
            yield break;
        }

        if (enterApp)
        {
            EnterApp();
        }
    }

    private IEnumerator SignOutLogic()
    {
        string token = CurrentToken;

        if (!string.IsNullOrWhiteSpace(token))
        {
            ApiResult<object> result = null;
            yield return StartCoroutine(apiClient.SignOut(token, response => result = response));
            if (result != null && result.Unauthorized)
            {
                Debug.Log("Backend session already expired during sign-out.");
            }
        }

        ClearSession();
        SceneManager.LoadScene("Registration");
    }

    private void SaveSession(AuthUserDto user, string token)
    {
        CurrentUser = user;
        SessionStorage.SaveToken(token);
    }

    private void ClearSession()
    {
        CurrentUser = null;
        CurrentProfile = null;
        SessionStorage.ClearToken();
    }

    private void HandleUnauthorized()
    {
        ClearSession();

        if (SceneManager.GetActiveScene().name == "Registration")
        {
            AuthUIManager.instance?.LoginScreen();
            return;
        }

        SceneManager.LoadScene("Registration");
    }

    private string MapAuthError(string rawMessage, bool isRegisterFlow)
    {
        if (string.IsNullOrWhiteSpace(rawMessage))
        {
            return "Ошибка";
        }

        string normalized = rawMessage.ToLowerInvariant();

        if (normalized.Contains("invalid email"))
        {
            return "Неверная почта";
        }

        if (normalized.Contains("user already exists") || normalized.Contains("already exists") || normalized.Contains("already in use"))
        {
            return "Почта занята";
        }

        if (normalized.Contains("invalid password") || normalized.Contains("wrong password"))
        {
            return "Неверный пароль";
        }

        if (normalized.Contains("user not found"))
        {
            return "Пользователь не найден";
        }

        if (normalized.Contains("password"))
        {
            return isRegisterFlow ? "Слабый пароль" : "Неверный пароль";
        }

        return rawMessage;
    }
}
