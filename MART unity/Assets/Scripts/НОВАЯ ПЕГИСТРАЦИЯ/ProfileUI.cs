using System;
using System.Collections;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class AvatarUserData
{
    public string avatarUrl;
}

[System.Serializable]
public class AvatarUploadResponse
{
    public string message;
    public AvatarUserData user;
}

public class ProfileUI : MonoBehaviour
{
    [SerializeField] private TMP_Text usernameText;
    [SerializeField] private TMP_Text emailText;
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField bioInput;
    [SerializeField] private TMP_Text warningText;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private TMP_Text loadingPanelText;
    [SerializeField] private GameObject profileActionsPanel;
    [SerializeField] private Image avatarImage;
    [SerializeField] private GameObject changeAvatarButton;
    [SerializeField] private GameObject avatarOptionsPanel;
    [SerializeField] private Button removeAvatarButton;

    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text pointsText;
    [SerializeField] private Image progressFill;

    private const int MAX_POINTS = 350;

    private bool isEditing = false;
    private bool isSaving = false;

    private string selectedAvatarPath;
    private bool avatarChanged = false;
    private bool avatarRemovePending = false;

    private const int MinUsernameLength = 3;
    private const int MaxUsernameLength = 20;
    private const int AvatarPreviewMaxSize = 512;

    private Coroutine clearWarningCoroutine;
    private Coroutine hideLoadingPanelCoroutine;

    private Sprite defaultAvatarSprite;

    private void OnEnable()
    {
        ProfilePanelDefaultVisibility.Apply(transform);
    }

    private void Start()
    {
        if (avatarImage != null)
        {
            EnsureCircularAvatarMask();
            defaultAvatarSprite = avatarImage.sprite;
        }

        if (usernameInput != null)
        {
            usernameInput.characterLimit = MaxUsernameLength;
            usernameInput.onValueChanged.AddListener(OnUsernameChanged);
        }

        if (bioInput != null)
        {
            // Поле остаётся доступным для нажатия в любом режиме, чтобы по тапу
            // можно было сразу начать ввод; сам тап включает режим редактирования.
            bioInput.interactable = true;
            bioInput.onValueChanged.AddListener(_ => UpdateSaveButtonState());
            bioInput.onSelect.AddListener(_ => BeginEditingFromBio());
        }

        if (saveButton != null)
        {
            saveButton.onClick.AddListener(OnSaveProfileClicked);
        }

        if (UserSession.Instance != null)
        {
            usernameText.text = UserSession.Instance.Username;
            emailText.text = UserSession.Instance.Email;
            usernameInput.text = UserSession.Instance.Username;

            if (bioInput != null)
            {
                bioInput.text = UserSession.Instance.Bio ?? "";
            }
        }

        ClearWarningText();
        HideLoadingPanelImmediate();
        ShowViewMode();
        RefreshAvatar();
        ResolveRemoveAvatarButton();
        UpdateRemoveAvatarButtonState();

        UpdateProfileUI();
    }

    public void UpdateProfileUI()
    {
        if (UserSession.Instance == null)
            return;

        int points = UserSession.Instance.Points;
        string title = UserSession.Instance.Title;

        if (titleText != null)
        {
            titleText.text = title;
        }

        if (pointsText != null)
        {
            pointsText.text = points + " / " + MAX_POINTS;
        }

        if (progressFill != null)
        {
            progressFill.fillAmount = (float)points / MAX_POINTS;
        }
    }

    private void ShowViewMode()
    {
        isEditing = false;

        usernameText.gameObject.SetActive(true);
        usernameInput.gameObject.SetActive(false);

        if (saveButton != null)
        {
            saveButton.interactable = false;
            saveButton.gameObject.SetActive(false);
        }

        if (cancelButton != null)
        {
            cancelButton.gameObject.SetActive(false);
        }

        if (UserSession.Instance != null)
        {
            usernameText.text = UserSession.Instance.Username;
            usernameInput.text = UserSession.Instance.Username;

            if (bioInput != null)
            {
                bioInput.text = UserSession.Instance.Bio ?? "";
            }
        }

        if (changeAvatarButton != null)
        {
            changeAvatarButton.SetActive(false);
        }

        if (avatarOptionsPanel != null)
        {
            avatarOptionsPanel.SetActive(false);
        }

        UpdateProfileUI();
        UpdateRemoveAvatarButtonState();
    }

    private void ShowEditMode(bool focusUsername = true)
    {
        if (isSaving)
            return;

        isEditing = true;

        usernameText.gameObject.SetActive(false);
        usernameInput.gameObject.SetActive(true);

        if (saveButton != null)
        {
            saveButton.gameObject.SetActive(true);
        }

        if (cancelButton != null)
        {
            cancelButton.gameObject.SetActive(true);
        }

        if (UserSession.Instance != null)
        {
            usernameInput.text = UserSession.Instance.Username;

            if (bioInput != null)
            {
                bioInput.text = UserSession.Instance.Bio ?? "";
            }
        }

        UpdateSaveButtonState();
        ClearWarningText();

        // При входе через тап по полю «о себе» не перехватываем фокус на имя —
        // иначе клавиатура откроется для username вместо bio.
        if (focusUsername)
        {
            usernameInput.ActivateInputField();
        }

        if (changeAvatarButton != null)
        {
            changeAvatarButton.SetActive(true);
        }

        if (avatarOptionsPanel != null)
        {
            avatarOptionsPanel.SetActive(false);
        }

        UpdateRemoveAvatarButtonState();

        if (bioInput != null)
        {
            bioInput.interactable = true;
        }
    }

    public void RefreshProfileData()
    {
        UpdateProfileUI();
    }

    public void OnEditProfileClicked()
    {
        ShowEditMode();
    }

    // Тап по полю "о себе" в режиме просмотра сразу включает редактирование,
    // сохраняя фокус на самом поле, чтобы можно было печатать без лишних нажатий.
    private void BeginEditingFromBio()
    {
        if (isSaving || isEditing)
        {
            return;
        }

        ShowEditMode(false);
    }

    public void OnCancelChangesClicked()
    {
        if (isSaving)
            return;

        ClearWarningText();

        if (UserSession.Instance != null)
        {
            usernameInput.text = UserSession.Instance.Username;

            if (bioInput != null)
            {
                bioInput.text = UserSession.Instance.Bio ?? "";
            }
        }

        selectedAvatarPath = "";
        avatarChanged = false;
        avatarRemovePending = false;

        RefreshAvatar();

        if (profileActionsPanel != null)
        {
            profileActionsPanel.SetActive(false);
        }

        ShowViewMode();
        UpdateRemoveAvatarButtonState();
    }

    private void OnUsernameChanged(string value)
    {
        UpdateSaveButtonState();
    }

    public void OnSaveProfileClicked()
    {
        if (isSaving || !isEditing)
        {
            return;
        }

        if (usernameInput == null)
        {
            return;
        }

        string trimmedUsername = usernameInput.text.Trim();
        if (trimmedUsername.Length < MinUsernameLength ||
            trimmedUsername.Length > MaxUsernameLength)
        {
            ShowWarning("Имя: от " + MinUsernameLength + " до " + MaxUsernameLength + " символов.");
            return;
        }

        StartCoroutine(SaveProfileCoroutine(trimmedUsername));
    }

    private IEnumerator SaveProfileCoroutine(string trimmedUsername)
    {
        isSaving = true;
        UpdateSaveButtonState();
        ShowLoadingPanel("Сохранение…");
        ClearWarningText();

        string bio = bioInput != null ? bioInput.text : "";

        BackendManager backend = BackendManager.instance;
        if (backend == null)
        {
            HideLoadingPanelImmediate();
            isSaving = false;
            UpdateSaveButtonState();
            ShowWarning("Нет подключения к серверу профиля.");
            yield break;
        }

        bool done = false;
        bool success = false;
        string err = null;

        backend.SaveProfileDetails(trimmedUsername, bio, (ok, error) =>
        {
            success = ok;
            err = error;
            done = true;
        });

        yield return new WaitUntil(() => done);

        HideLoadingPanelImmediate();
        isSaving = false;
        UpdateSaveButtonState();

        if (!success)
        {
            ShowWarning(string.IsNullOrEmpty(err) ? "Не удалось сохранить." : err);
            yield break;
        }

        if (avatarRemovePending)
        {
            bool avatarDone = false;
            bool avatarSuccess = false;
            string avatarError = null;

            backend.RemoveProfileAvatar((ok, error) =>
            {
                avatarSuccess = ok;
                avatarError = error;
                avatarDone = true;
            });

            yield return new WaitUntil(() => avatarDone);

            if (!avatarSuccess)
            {
                ShowWarning(string.IsNullOrEmpty(avatarError) ? "Не удалось удалить фото." : avatarError);
                yield break;
            }
        }
        else if (avatarChanged && !string.IsNullOrWhiteSpace(selectedAvatarPath))
        {
            bool avatarDone = false;
            bool avatarSuccess = false;
            string avatarError = null;

            backend.UpdateProfilePicture(selectedAvatarPath, (ok, error) =>
            {
                avatarSuccess = ok;
                avatarError = error;
                avatarDone = true;
            });

            yield return new WaitUntil(() => avatarDone);

            if (!avatarSuccess)
            {
                ShowWarning(string.IsNullOrEmpty(avatarError) ? "Не удалось загрузить фото." : avatarError);
                yield break;
            }
        }

        selectedAvatarPath = "";
        avatarChanged = false;
        avatarRemovePending = false;

        if (profileActionsPanel != null)
        {
            profileActionsPanel.SetActive(false);
        }

        ShowViewMode();
        RefreshAvatar();
        UpdateProfileUI();
        UpdateRemoveAvatarButtonState();
    }

    private bool HasPendingProfileChanges()
    {
        if (UserSession.Instance == null || usernameInput == null)
        {
            return false;
        }

        string u = usernameInput.text.Trim();
        string bio = bioInput != null ? bioInput.text : "";
        string sessionBio = UserSession.Instance.Bio ?? "";

        return u != (UserSession.Instance.Username ?? "").Trim() ||
            bio != sessionBio ||
            avatarChanged ||
            avatarRemovePending;
    }

    private void UpdateSaveButtonState()
    {
        if (saveButton == null || usernameInput == null)
        {
            return;
        }

        string trimmedUsername = usernameInput.text.Trim();
        bool usernameOk =
            !string.IsNullOrEmpty(trimmedUsername) &&
            trimmedUsername.Length >= MinUsernameLength &&
            trimmedUsername.Length <= MaxUsernameLength;

        saveButton.interactable = !isSaving && usernameOk && HasPendingProfileChanges();
    }

    private void ShowLoadingPanel(string message)
    {
        if (loadingPanelText != null)
        {
            loadingPanelText.text = message;
        }

        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }
    }

    private void HideLoadingPanelImmediate()
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
    }

    private void ShowWarning(string message)
    {
        if (warningText != null)
        {
            warningText.text = message;
        }
    }

    private void ClearWarningText()
    {
        if (warningText != null)
        {
            warningText.text = "";
        }
    }

    private void RefreshAvatar()
    {
        if (avatarImage == null)
            return;

        if (string.IsNullOrEmpty(UserSession.Instance?.AvatarUrl))
        {
            avatarImage.sprite = defaultAvatarSprite;
            UpdateRemoveAvatarButtonState();
            return;
        }

        string path = UserSession.Instance.AvatarUrl.Trim();
        string fullAvatarUrl = path.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? path
            : AppConfig.BaseUrl.TrimEnd('/') + (path.StartsWith("/") ? path : "/" + path);

        StartCoroutine(LoadAvatarCoroutine(fullAvatarUrl));
    }

    private IEnumerator LoadAvatarCoroutine(string imageUrl)
    {
        UnityWebRequest request =
            UnityWebRequestTexture.GetTexture(imageUrl);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            SetAvatarSpriteFromTexture(texture);
            UpdateRemoveAvatarButtonState();
        }
    }

    private void EnsureCircularAvatarMask()
    {
        if (avatarImage == null)
        {
            return;
        }

        CircularAvatarMask mask = avatarImage.GetComponent<CircularAvatarMask>();
        if (mask == null)
        {
            mask = avatarImage.gameObject.AddComponent<CircularAvatarMask>();
        }
        else
        {
            mask.Apply();
        }
    }

    private void SetAvatarSpriteFromTexture(Texture2D texture)
    {
        if (avatarImage == null || texture == null)
        {
            return;
        }

        EnsureCircularAvatarMask();
        Sprite sprite = CircularAvatarMask.CreateAvatarSprite(texture);
        if (sprite != null)
        {
            avatarImage.sprite = sprite;
        }
    }

    public void CloseAvatarOptionsPanel()
    {
        if (avatarOptionsPanel != null)
        {
            avatarOptionsPanel.SetActive(false);
        }
    }

    public void ToggleAvatarOptionsPanel()
    {
        if (avatarOptionsPanel != null)
        {
            avatarOptionsPanel.SetActive(!avatarOptionsPanel.activeSelf);
        }

        UpdateRemoveAvatarButtonState();
    }

    public void OnRemoveAvatarClicked()
    {
        if (!isEditing || isSaving)
        {
            return;
        }

        if (!HasCustomAvatar())
        {
            ShowWarning("Фото профиля уже удалено.");
            return;
        }

        avatarRemovePending = true;
        avatarChanged = false;
        selectedAvatarPath = "";

        if (avatarImage != null && defaultAvatarSprite != null)
        {
            avatarImage.sprite = defaultAvatarSprite;
        }

        ClearWarningText();
        UpdateSaveButtonState();
        UpdateRemoveAvatarButtonState();
    }

    private void ResolveRemoveAvatarButton()
    {
        if (removeAvatarButton == null && avatarOptionsPanel != null)
        {
            string[] candidateNames = { "Delete a photo", "Remove avatar", "Delete photo" };
            foreach (string candidateName in candidateNames)
            {
                Transform found = avatarOptionsPanel.transform.Find(candidateName);
                if (found == null)
                {
                    continue;
                }

                removeAvatarButton = found.GetComponent<Button>();
                if (removeAvatarButton != null)
                {
                    break;
                }
            }
        }

        if (removeAvatarButton == null)
        {
            return;
        }

        // Inspector listeners (e.g. copied from "choose photo") survive RemoveAllListeners.
        removeAvatarButton.onClick = new Button.ButtonClickedEvent();
        removeAvatarButton.onClick.AddListener(CloseAvatarOptionsPanel);
        removeAvatarButton.onClick.AddListener(OnRemoveAvatarClicked);
    }

    private void UpdateRemoveAvatarButtonState()
    {
        if (removeAvatarButton == null)
        {
            return;
        }

        removeAvatarButton.gameObject.SetActive(true);
        removeAvatarButton.interactable = isEditing && !isSaving && HasCustomAvatar();
    }

    private bool HasCustomAvatar()
    {
        if (avatarRemovePending)
        {
            return false;
        }

        if (avatarChanged && !string.IsNullOrWhiteSpace(selectedAvatarPath))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(UserSession.Instance?.AvatarUrl);
    }

    public void OnChoosePhotoFromGalleryClicked()
    {
        if (!isEditing || isSaving)
        {
            return;
        }

#if UNITY_EDITOR
        PickAvatarImageFromDesktop();
#elif UNITY_ANDROID || UNITY_IOS
        NativeGallery.GetImageFromGallery(OnAvatarImagePicked, "Выберите фото", "image/*");
#else
        ShowWarning("Выбор фото доступен в редакторе Unity или на телефоне.");
#endif
    }

    public void OnTakePhotoClicked()
    {
        if (!isEditing || isSaving)
        {
            return;
        }

#if UNITY_EDITOR
        PickAvatarImageFromDesktop();
#elif UNITY_ANDROID || UNITY_IOS
        if (!NativeCamera.DeviceHasCamera())
        {
            ShowWarning("На устройстве нет камеры.");
            return;
        }

        NativeCamera.TakePicture(OnAvatarImagePicked, maxSize: AvatarPreviewMaxSize);
#else
        ShowWarning("Камера доступна на телефоне; на ПК используйте «выбрать из галереи» в редакторе.");
#endif
    }

#if UNITY_EDITOR
    private void PickAvatarImageFromDesktop()
    {
        string startFolder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (string.IsNullOrWhiteSpace(startFolder) || !Directory.Exists(startFolder))
        {
            startFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        }

        string path = EditorUtility.OpenFilePanel(
            "Выберите фото",
            startFolder,
            "png,jpg,jpeg,webp,gif,bmp");

        OnAvatarImagePicked(path);
    }

    private static Texture2D LoadTextureFromFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }

        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            return texture.LoadImage(bytes) ? texture : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
#endif

    private void OnAvatarImagePicked(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        string normalizedPath = path.Trim().Trim('"');
        if (!File.Exists(normalizedPath))
        {
            ShowWarning("Не удалось прочитать выбранный файл.");
            return;
        }

        ApplySelectedAvatarPreview(normalizedPath);
    }

    private void ApplySelectedAvatarPreview(string path)
    {
        avatarRemovePending = false;
        selectedAvatarPath = path;
        avatarChanged = true;
        ClearWarningText();
        StartCoroutine(LoadLocalAvatarPreview(path));
        UpdateSaveButtonState();
        UpdateRemoveAvatarButtonState();
    }

    private IEnumerator LoadLocalAvatarPreview(string path)
    {
        Texture2D texture = null;

#if UNITY_ANDROID || UNITY_IOS
        texture = NativeGallery.LoadImageAtPath(path, AvatarPreviewMaxSize, false);
#elif UNITY_EDITOR
        texture = LoadTextureFromFile(path);
        yield return null;
#endif

        if (texture == null)
        {
            string fileUrl = path.Contains("://") ? path : "file:///" + path.Replace("\\", "/");
            using UnityWebRequest request = UnityWebRequestTexture.GetTexture(fileUrl);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                texture = DownloadHandlerTexture.GetContent(request);
            }
        }

        if (texture == null || avatarImage == null)
        {
            ShowWarning("Не удалось показать превью фото.");
            yield break;
        }

        SetAvatarSpriteFromTexture(texture);
    }

    public static ProfileUI Instance;

    private void Awake()
    {
        Instance = this;
    }
}