using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BackendManager : MonoBehaviour
{
    public static BackendManager instance;

    /// <summary>Имя файла сцены логина (Build Settings → Scenes).</summary>
    private const string LoginSceneName = "AuthScene";

    /// <summary>Имя главной сцены приложения (Build Settings → Scenes, index 2).</summary>
    private const string MainSceneName = "The main stage";

    /// <summary>
    /// DEV: пропуск экранов регистрации/авторизации в тестовых сборках.
    /// Поток всё равно проходит через AuthScene, чтобы создались синглтоны
    /// BackendManager/UserSession (DontDestroyOnLoad), но сразу уходит в игру.
    /// Поставить false, чтобы вернуть обычный экран входа.
    /// </summary>
    public const bool BypassAuthForTesting = true;

    [Header("Backend")]
    [SerializeField] private string backendBaseUrl = "http://localhost:3001";

    public string ApiBaseUrl => (backendBaseUrl ?? string.Empty).TrimEnd('/');

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
    private ArtworkDto pendingArtworkForEditing;
    private bool openLibraryAfterNextMainStageLoad;
    private bool deferPendingArtworkAutoLoad;

    public AuthUserDto CurrentUser { get; private set; }
    public ProfileDto CurrentProfile { get; private set; }
    public string CurrentToken => SessionStorage.LoadToken();
    public bool HasAuthorizedSession => !string.IsNullOrWhiteSpace(CurrentToken) && CurrentUser != null;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        string resolvedUrl = BackendUrlProvider.GetBaseUrl(backendBaseUrl);
        backendBaseUrl = resolvedUrl;
        apiClient = new BackendAuthApiClient(resolvedUrl);

#if !UNITY_EDITOR
        if (BackendUrlProvider.IsLocalhost(resolvedUrl))
        {
            Debug.LogWarning(
                "Backend: на телефоне localhost не работает без USB. " +
                "Подключите USB и пересоберите (adb reverse), " +
                "или задайте BackendUrlProvider.DeviceLanUrl = http://IP_ПК:3001");
        }
#endif
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
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

        if (loginEmail == null || loginPassword == null)
        {
            Debug.LogWarning("BackendManager: login fields are not assigned in the scene.");
            return;
        }

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

    public void UpdateProfilePicture(string filePath)
    {
        StartCoroutine(UpdateProfilePictureLogic(filePath, null));
    }

    public void UpdateProfilePicture(string filePath, System.Action<bool, string> onComplete)
    {
        StartCoroutine(UpdateProfilePictureLogic(filePath, onComplete));
    }

    public void RemoveProfileAvatar(System.Action<bool, string> onComplete = null)
    {
        StartCoroutine(RemoveProfileAvatarLogic(onComplete));
    }

    public void LoadMyArtworks(System.Action<ApiResult<ArtworkArrayWrapperDto>> onComplete)
    {
        StartCoroutine(LoadMyArtworksLogic(onComplete));
    }

    public void LoadViewedExhibits(System.Action<ApiResult<StringArrayWrapperDto>> onComplete)
    {
        StartCoroutine(LoadViewedExhibitsLogic(onComplete));
    }

    public void RecordExhibitView(string exhibitId, System.Action<ApiResult<ExhibitViewRewardDto>> onComplete)
    {
        StartCoroutine(RecordExhibitViewLogic(exhibitId, onComplete));
    }

    public void LoadFavoriteExhibits(System.Action<ApiResult<StringArrayWrapperDto>> onComplete)
    {
        StartCoroutine(LoadFavoriteExhibitsLogic(onComplete));
    }

    public void AddFavoriteExhibit(string exhibitId, System.Action<ApiResult<ExhibitFavoriteDto>> onComplete)
    {
        StartCoroutine(AddFavoriteExhibitLogic(exhibitId, onComplete));
    }

    public void RemoveFavoriteExhibit(string exhibitId, System.Action<ApiResult<object>> onComplete)
    {
        StartCoroutine(RemoveFavoriteExhibitLogic(exhibitId, onComplete));
    }

    public void SaveNewArtworkFromCanvas(string title, string description, byte[] imageBytes, System.Action<ApiResult<ArtworkDto>> onComplete)
    {
        StartCoroutine(SaveNewArtworkFromCanvasLogic(title, description, imageBytes, onComplete));
    }

    public void UpdateArtworkFromCanvas(string artworkId, string title, string description, byte[] imageBytes, bool updateImage, System.Action<ApiResult<ArtworkDto>> onComplete)
    {
        StartCoroutine(UpdateArtworkFromCanvasLogic(artworkId, title, description, imageBytes, updateImage, onComplete));
    }

    public void DeleteArtwork(string artworkId, System.Action<ApiResult<object>> onComplete)
    {
        StartCoroutine(DeleteArtworkLogic(artworkId, onComplete));
    }

    public void LoadMyNotes(System.Action<ApiResult<NoteArrayWrapperDto>> onComplete)
    {
        StartCoroutine(LoadMyNotesLogic(onComplete));
    }

    public void SaveNote(string text, NoteCategory category, string exhibitId, System.Action<ApiResult<NoteDto>> onComplete)
    {
        StartCoroutine(SaveNoteLogic(text, category, exhibitId, onComplete));
    }

    public void UpdateNote(string noteId, string text, NoteCategory category, System.Action<ApiResult<NoteDto>> onComplete)
    {
        StartCoroutine(UpdateNoteLogic(noteId, text, category, onComplete));
    }

    public void DeleteNote(string noteId, System.Action<ApiResult<object>> onComplete)
    {
        StartCoroutine(DeleteNoteLogic(noteId, onComplete));
    }

    public void BeginArtworkEditing(ArtworkDto artwork)
    {
        pendingArtworkForEditing = artwork;
        openLibraryAfterNextMainStageLoad = true;
        deferPendingArtworkAutoLoad = true;
    }

    /// <summary>Снимает блокировку авто-загрузки в PaintArtworkController.OnEnable (открытие из профиля).</summary>
    public void ClearDeferredArtworkAutoLoad()
    {
        deferPendingArtworkAutoLoad = false;
    }

    public bool ShouldDeferArtworkAutoLoad => deferPendingArtworkAutoLoad;

    public void ConsumeOpenPaintFromProfileSceneIntent()
    {
        openLibraryAfterNextMainStageLoad = false;
    }

    public bool TryPeekPendingArtwork(out ArtworkDto artwork)
    {
        artwork = pendingArtworkForEditing;
        return artwork != null;
    }

    public bool TryConsumePendingArtwork(out ArtworkDto artwork)
    {
        artwork = pendingArtworkForEditing;
        pendingArtworkForEditing = null;
        return artwork != null;
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

        if (SceneManager.GetActiveScene().name == LoginSceneName)
        {
            AuthUIManager.instance?.LoginScreen();
            return;
        }

        SceneManager.LoadScene(LoginSceneName);
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
            ApplyProfileToLocalState(result.Data);
        }

        yield return StartCoroutine(RefreshFavoriteExhibits());
    }

    public IEnumerator RefreshFavoriteExhibits()
    {
        string token = CurrentToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            ExhibitFavoritesStore.Clear();
            yield break;
        }

        ApiResult<StringArrayWrapperDto> result = null;
        yield return StartCoroutine(apiClient.GetFavoriteExhibits(token, response => result = response));

        if (result != null && result.Unauthorized)
        {
            HandleUnauthorized();
            yield break;
        }

        if (result != null && result.Success && result.Data?.items != null)
        {
            ExhibitFavoritesStore.SetFavorites(result.Data.items);
        }
    }

    public void SaveProfileDetails(string username, string bio, System.Action<bool, string> onComplete = null)
    {
        StartCoroutine(SaveProfileDetailsLogic(username, bio, onComplete));
    }

    private IEnumerator SaveProfileDetailsLogic(string username, string bio, System.Action<bool, string> onComplete)
    {
        string token = CurrentToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            onComplete?.Invoke(false, "Сессия не найдена. Войдите заново.");
            yield break;
        }

        ProfileUpdateRequestDto payload = new ProfileUpdateRequestDto
        {
            username = username != null ? username.Trim() : "",
            bio = bio ?? "",
        };

        ApiResult<ProfileDto> result = null;
        yield return StartCoroutine(apiClient.UpdateProfile(token, payload, r => result = r));

        if (result == null)
        {
            onComplete?.Invoke(false, "Не удалось сохранить профиль.");
            yield break;
        }

        if (result.Unauthorized)
        {
            HandleUnauthorized();
            onComplete?.Invoke(false, "Сессия истекла. Войдите снова.");
            yield break;
        }

        if (!result.Success || result.Data == null)
        {
            onComplete?.Invoke(false, string.IsNullOrWhiteSpace(result.ErrorMessage)
                ? "Не удалось сохранить профиль."
                : result.ErrorMessage);
            yield break;
        }

        ApplyProfileToLocalState(result.Data);
        LobbyManager.instance?.LoadProfile();
        onComplete?.Invoke(true, null);
    }

    private void ApplyProfileToLocalState(ProfileDto profile)
    {
        if (profile == null)
        {
            return;
        }

        CurrentProfile = profile;

        if (CurrentUser != null && !string.IsNullOrWhiteSpace(profile.displayName))
        {
            CurrentUser.name = profile.displayName;
        }

        if (UserSession.Instance == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(profile.displayName))
        {
            UserSession.Instance.Username = profile.displayName;
        }

        UserSession.Instance.Bio = profile.bio ?? "";
        UserSession.Instance.AvatarUrl = profile.avatarUrl ?? "";

        if (CurrentUser != null)
        {
            CurrentUser.image = string.IsNullOrWhiteSpace(profile.avatarUrl) ? null : profile.avatarUrl;
        }

        if (profile.progress != null &&
            int.TryParse(
                profile.progress.xp,
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out int xp))
        {
            UserSession.Instance.Points = xp;
        }
    }

    private IEnumerator RestoreSession()
    {
        if (BypassAuthForTesting)
        {
            // Синглтоны уже созданы в AuthScene (Awake). Уходим в игру без экрана входа.
            if (SceneManager.GetActiveScene().name == LoginSceneName)
            {
                SceneManager.LoadScene(MainSceneName);
            }

            yield break;
        }

        string token = CurrentToken;
        string activeScene = SceneManager.GetActiveScene().name;

        if (string.IsNullOrWhiteSpace(token))
        {
            if (activeScene == LoginSceneName)
            {
                AuthUIManager.instance?.WelcomeScreen();
            }
            else
            {
                SceneManager.LoadScene(LoginSceneName);
            }

            yield break;
        }

        if (activeScene == LoginSceneName)
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

        if (activeScene == LoginSceneName)
        {
            // Не уводим с AuthScene автоматически: иначе при сохранённом токене нельзя открыть
            // регистрацию нового пользователя — RestoreSession сразу грузит основную сцену.
            // Вернуться в приложение: кнопка в UI → BackendManager.EnterApp().
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
        SceneManager.LoadScene(LoginSceneName);
    }

    private IEnumerator UpdateProfilePictureLogic(string filePath, System.Action<bool, string> onComplete)
    {
        void Fail(string message)
        {
            LobbyManager.instance?.Output(message);
            onComplete?.Invoke(false, message);
        }

        void Succeed()
        {
            onComplete?.Invoke(true, null);
        }

        if (string.IsNullOrWhiteSpace(CurrentToken))
        {
            Fail("Сессия не найдена. Войдите заново.");
            yield break;
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            Fail("Укажите путь к изображению.");
            yield break;
        }

        string normalizedPath = filePath.Trim().Trim('"');
        if (!File.Exists(normalizedPath))
        {
            Fail("Файл не найден. Проверьте путь к изображению.");
            yield break;
        }

        string extension = Path.GetExtension(normalizedPath)?.ToLowerInvariant();
        switch (extension)
        {
            case ".png":
            case ".jpg":
            case ".jpeg":
            case ".webp":
            case ".gif":
            case ".bmp":
                break;
            default:
                Fail("Поддерживаются только изображения: png, jpg, jpeg, webp, gif, bmp.");
                yield break;
        }

        FileInfo fileInfo = new FileInfo(normalizedPath);
        const long maxAvatarSizeBytes = 5L * 1024L * 1024L;
        if (fileInfo.Length > maxAvatarSizeBytes)
        {
            Fail("Файл слишком большой. Максимум 5 MB.");
            yield break;
        }

        LobbyManager.instance?.Output("Загружаем фото профиля...");

        ApiResult<ProfileDto> result = null;
        yield return StartCoroutine(apiClient.UpdateProfileAvatar(CurrentToken, normalizedPath, response => result = response));

        if (result == null)
        {
            Fail("Не удалось загрузить фото.");
            yield break;
        }

        if (result.Unauthorized)
        {
            HandleUnauthorized();
            Fail("Сессия истекла. Войдите снова.");
            yield break;
        }

        if (!result.Success || result.Data == null)
        {
            string error = string.IsNullOrWhiteSpace(result.ErrorMessage)
                ? "Не удалось обновить фото профиля."
                : result.ErrorMessage;
            Fail(error);
            yield break;
        }

        ApplyProfileToLocalState(result.Data);
        LobbyManager.instance?.ProfileUI();
        LobbyManager.instance?.Output("Фото профиля обновлено.");
        Succeed();
    }

    private IEnumerator RemoveProfileAvatarLogic(System.Action<bool, string> onComplete)
    {
        void Fail(string message)
        {
            LobbyManager.instance?.Output(message);
            onComplete?.Invoke(false, message);
        }

        void Succeed()
        {
            onComplete?.Invoke(true, null);
        }

        if (string.IsNullOrWhiteSpace(CurrentToken))
        {
            Fail("Сессия не найдена. Войдите заново.");
            yield break;
        }

        LobbyManager.instance?.Output("Удаляем фото профиля...");

        ApiResult<ProfileDto> result = null;
        yield return StartCoroutine(apiClient.DeleteProfileAvatar(CurrentToken, response => result = response));

        if (result == null)
        {
            Fail("Не удалось удалить фото.");
            yield break;
        }

        if (result.Unauthorized)
        {
            HandleUnauthorized();
            Fail("Сессия истекла. Войдите снова.");
            yield break;
        }

        if (!result.Success || result.Data == null)
        {
            string error = string.IsNullOrWhiteSpace(result.ErrorMessage)
                ? "Не удалось удалить фото профиля."
                : result.ErrorMessage;
            Fail(error);
            yield break;
        }

        ApplyProfileToLocalState(result.Data);
        LobbyManager.instance?.ProfileUI();
        LobbyManager.instance?.Output("Фото профиля удалено.");
        Succeed();
    }

    private IEnumerator LoadMyArtworksLogic(System.Action<ApiResult<ArtworkArrayWrapperDto>> onComplete)
    {
        string token = CurrentToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            onComplete?.Invoke(new ApiResult<ArtworkArrayWrapperDto>
            {
                Success = false,
                Unauthorized = true,
                ErrorMessage = "Авторизуйтесь заново.",
            });
            yield break;
        }

        ApiResult<ArtworkArrayWrapperDto> result = null;
        yield return StartCoroutine(apiClient.GetMyArtworks(token, response => result = response));

        if (result != null && result.Unauthorized)
        {
            HandleUnauthorized();
        }

        onComplete?.Invoke(result);
    }

    private IEnumerator SaveNewArtworkFromCanvasLogic(string title, string description, byte[] imageBytes, System.Action<ApiResult<ArtworkDto>> onComplete)
    {
        string token = CurrentToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            onComplete?.Invoke(new ApiResult<ArtworkDto>
            {
                Success = false,
                Unauthorized = true,
                ErrorMessage = "Авторизуйтесь заново.",
            });
            yield break;
        }

        ArtworkUpsertRequestDto payload = new ArtworkUpsertRequestDto
        {
            title = title,
            description = description,
            imageBytes = imageBytes,
        };

        ApiResult<ArtworkDto> result = null;
        yield return StartCoroutine(apiClient.CreateArtwork(token, payload, response => result = response));

        if (result != null && result.Unauthorized)
        {
            HandleUnauthorized();
        }

        onComplete?.Invoke(result);
    }

    private IEnumerator LoadViewedExhibitsLogic(System.Action<ApiResult<StringArrayWrapperDto>> onComplete)
    {
        string token = CurrentToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            onComplete?.Invoke(new ApiResult<StringArrayWrapperDto>
            {
                Success = false,
                Unauthorized = true,
                ErrorMessage = "Авторизуйтесь заново.",
            });
            yield break;
        }

        ApiResult<StringArrayWrapperDto> result = null;
        yield return StartCoroutine(apiClient.GetViewedExhibits(token, response => result = response));

        if (result != null && result.Unauthorized)
        {
            HandleUnauthorized();
        }

        onComplete?.Invoke(result);
    }

    private IEnumerator RecordExhibitViewLogic(string exhibitId, System.Action<ApiResult<ExhibitViewRewardDto>> onComplete)
    {
        string token = CurrentToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            onComplete?.Invoke(new ApiResult<ExhibitViewRewardDto>
            {
                Success = false,
                Unauthorized = true,
                ErrorMessage = "Авторизуйтесь заново.",
            });
            yield break;
        }

        ApiResult<ExhibitViewRewardDto> result = null;
        yield return StartCoroutine(apiClient.RecordExhibitView(token, exhibitId, response => result = response));

        if (result != null && result.Unauthorized)
        {
            HandleUnauthorized();
        }

        onComplete?.Invoke(result);
    }

    private IEnumerator LoadFavoriteExhibitsLogic(System.Action<ApiResult<StringArrayWrapperDto>> onComplete)
    {
        string token = CurrentToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            onComplete?.Invoke(new ApiResult<StringArrayWrapperDto>
            {
                Success = false,
                Unauthorized = true,
                ErrorMessage = "Авторизуйтесь заново.",
            });
            yield break;
        }

        ApiResult<StringArrayWrapperDto> result = null;
        yield return StartCoroutine(apiClient.GetFavoriteExhibits(token, response => result = response));

        if (result != null && result.Unauthorized)
        {
            HandleUnauthorized();
        }

        onComplete?.Invoke(result);
    }

    private IEnumerator AddFavoriteExhibitLogic(string exhibitId, System.Action<ApiResult<ExhibitFavoriteDto>> onComplete)
    {
        string token = CurrentToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            onComplete?.Invoke(new ApiResult<ExhibitFavoriteDto>
            {
                Success = false,
                Unauthorized = true,
                ErrorMessage = "Авторизуйтесь заново.",
            });
            yield break;
        }

        ApiResult<ExhibitFavoriteDto> result = null;
        yield return StartCoroutine(apiClient.AddFavoriteExhibit(token, exhibitId, response => result = response));

        if (result != null && result.Unauthorized)
        {
            HandleUnauthorized();
        }
        else if (result != null && result.Success)
        {
            ExhibitFavoritesStore.AddLocal(exhibitId);
        }

        onComplete?.Invoke(result);
    }

    private IEnumerator RemoveFavoriteExhibitLogic(string exhibitId, System.Action<ApiResult<object>> onComplete)
    {
        string token = CurrentToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            onComplete?.Invoke(new ApiResult<object>
            {
                Success = false,
                Unauthorized = true,
                ErrorMessage = "Авторизуйтесь заново.",
            });
            yield break;
        }

        ApiResult<object> result = null;
        yield return StartCoroutine(apiClient.RemoveFavoriteExhibit(token, exhibitId, response => result = response));

        if (result != null && result.Unauthorized)
        {
            HandleUnauthorized();
        }
        else if (result != null && result.Success)
        {
            ExhibitFavoritesStore.RemoveLocal(exhibitId);
        }

        onComplete?.Invoke(result);
    }

    private IEnumerator UpdateArtworkFromCanvasLogic(string artworkId, string title, string description, byte[] imageBytes, bool updateImage, System.Action<ApiResult<ArtworkDto>> onComplete)
    {
        string token = CurrentToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            onComplete?.Invoke(new ApiResult<ArtworkDto>
            {
                Success = false,
                Unauthorized = true,
                ErrorMessage = "Авторизуйтесь заново.",
            });
            yield break;
        }

        ArtworkUpsertRequestDto payload = new ArtworkUpsertRequestDto
        {
            title = title,
            description = description,
            imageBytes = updateImage ? imageBytes : null,
        };

        ApiResult<ArtworkDto> result = null;
        yield return StartCoroutine(apiClient.UpdateArtwork(token, artworkId, payload, response => result = response));

        if (result != null && result.Unauthorized)
        {
            HandleUnauthorized();
        }

        onComplete?.Invoke(result);
    }

    private IEnumerator DeleteArtworkLogic(string artworkId, System.Action<ApiResult<object>> onComplete)
    {
        string token = CurrentToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            onComplete?.Invoke(new ApiResult<object>
            {
                Success = false,
                Unauthorized = true,
                ErrorMessage = "Авторизуйтесь заново.",
            });
            yield break;
        }

        ApiResult<object> result = null;
        yield return StartCoroutine(apiClient.DeleteArtwork(token, artworkId, response => result = response));

        if (result != null && result.Unauthorized)
        {
            HandleUnauthorized();
        }

        onComplete?.Invoke(result);
    }

    private IEnumerator LoadMyNotesLogic(System.Action<ApiResult<NoteArrayWrapperDto>> onComplete)
    {
        string token = CurrentToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            onComplete?.Invoke(new ApiResult<NoteArrayWrapperDto>
            {
                Success = false,
                Unauthorized = true,
                ErrorMessage = "Авторизуйтесь заново.",
            });
            yield break;
        }

        ApiResult<NoteArrayWrapperDto> result = null;
        yield return StartCoroutine(apiClient.GetMyNotes(token, response => result = response));

        if (result != null && result.Unauthorized)
        {
            HandleUnauthorized();
        }

        onComplete?.Invoke(result);
    }

    private IEnumerator SaveNoteLogic(string text, NoteCategory category, string exhibitId, System.Action<ApiResult<NoteDto>> onComplete)
    {
        string token = CurrentToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            onComplete?.Invoke(new ApiResult<NoteDto>
            {
                Success = false,
                Unauthorized = true,
                ErrorMessage = "Авторизуйтесь заново.",
            });
            yield break;
        }

        NoteUpsertRequestDto payload = new NoteUpsertRequestDto
        {
            text = text,
            category = NoteCategories.Key(category),
            exhibitId = exhibitId,
        };

        ApiResult<NoteDto> result = null;
        yield return StartCoroutine(apiClient.CreateNote(token, payload, response => result = response));

        if (result != null && result.Unauthorized)
        {
            HandleUnauthorized();
        }

        onComplete?.Invoke(result);
    }

    private IEnumerator UpdateNoteLogic(string noteId, string text, NoteCategory category, System.Action<ApiResult<NoteDto>> onComplete)
    {
        string token = CurrentToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            onComplete?.Invoke(new ApiResult<NoteDto>
            {
                Success = false,
                Unauthorized = true,
                ErrorMessage = "Авторизуйтесь заново.",
            });
            yield break;
        }

        NoteUpsertRequestDto payload = new NoteUpsertRequestDto
        {
            text = text,
            category = NoteCategories.Key(category),
        };

        ApiResult<NoteDto> result = null;
        yield return StartCoroutine(apiClient.UpdateNote(token, noteId, payload, response => result = response));

        if (result != null && result.Unauthorized)
        {
            HandleUnauthorized();
        }

        onComplete?.Invoke(result);
    }

    private IEnumerator DeleteNoteLogic(string noteId, System.Action<ApiResult<object>> onComplete)
    {
        string token = CurrentToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            onComplete?.Invoke(new ApiResult<object>
            {
                Success = false,
                Unauthorized = true,
                ErrorMessage = "Авторизуйтесь заново.",
            });
            yield break;
        }

        ApiResult<object> result = null;
        yield return StartCoroutine(apiClient.DeleteNote(token, noteId, response => result = response));

        if (result != null && result.Unauthorized)
        {
            HandleUnauthorized();
        }

        onComplete?.Invoke(result);
    }

    private void SaveSession(AuthUserDto user, string token)
    {
        CurrentUser = user;
        SessionStorage.SaveToken(token);
    }

    /// <summary>
    /// После успешного /auth/login или /auth/register через LoginManager/RegisterManager (AppConfig),
    /// чтобы PaintArtwork и /artworks/me видели тот же JWT, что и BackendManager.
    /// </summary>
    public void SyncSessionAfterSimpleAuth(
        string userId,
        string email,
        string displayName,
        string avatarUrl,
        string token
    )
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        AuthUserDto user = new AuthUserDto
        {
            id = userId,
            name = string.IsNullOrWhiteSpace(displayName) ? email : displayName,
            email = email,
            emailVerified = true,
            image = avatarUrl,
        };

        SaveSession(user, token);
    }

    private void ClearSession()
    {
        CurrentUser = null;
        CurrentProfile = null;
        pendingArtworkForEditing = null;
        openLibraryAfterNextMainStageLoad = false;
        deferPendingArtworkAutoLoad = false;
        ExhibitFavoritesStore.Clear();
        SessionStorage.ClearToken();
    }

    private void HandleUnauthorized()
    {
        ClearSession();

        if (SceneManager.GetActiveScene().name == LoginSceneName)
        {
            AuthUIManager.instance?.LoginScreen();
            return;
        }

        SceneManager.LoadScene(LoginSceneName);
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

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ExhibitFavoritesRuntimeSetup.ConfigureScene(scene.name);

        if (scene.name != "The main stage" || CurrentUser == null)
        {
            return;
        }

        StartCoroutine(ShowProfileAfterSceneLoad());
    }

    private IEnumerator ShowProfileAfterSceneLoad()
    {
        yield return null;

        if (CurrentUser == null)
        {
            yield break;
        }

        if (openLibraryAfterNextMainStageLoad)
        {
            openLibraryAfterNextMainStageLoad = false;
            PaintArtworkController.PresentPendingArtworkInPaintWorkspace();
            yield break;
        }

        if (CurrentProfile == null)
        {
            yield return StartCoroutine(RefreshProfile());
        }

        Navigation navigation2 = FindObjectOfType<Navigation>();
        if (navigation2 != null)
        {
            navigation2.OpenProfile();
        }

        LobbyManager.instance?.ProfileUI();
    }
}
