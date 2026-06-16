using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager instance;

    [Header("UI References")]
    [SerializeField] private GameObject profileUI;
    [SerializeField] private GameObject changePfpUI;
    [SerializeField] private GameObject changeEmailUI;
    [SerializeField] private GameObject changePasswordUI;
    [SerializeField] private GameObject reverifyUI;
    [SerializeField] private GameObject resetPasswordConfirmUI;
    [SerializeField] private GameObject actionSuccessPanelUI;
    [SerializeField] private GameObject deleteUserConfirmUI;

    [Space(5f)]
    [Header("Basic Info References")]
    [SerializeField] private TMP_Text usernameText;
    [SerializeField] private TMP_Text emailText;

    [Space(5f)]
    [Header("Profile Picture References")]
    [SerializeField] private Image profilePicture;
    [SerializeField] private TMP_InputField profilePictureLink;
    [SerializeField] private TMP_Text outputText;
    [SerializeField] private ProfileArtworksGallery artworksGallery;

    [Space(5f)]
    [Header("Change Email References")]
    [SerializeField] private TMP_InputField changeEmailInputField;

    [Space(5f)]
    [Header("Change Password References")]
    [SerializeField] private TMP_InputField changePasswordInputField;
    [SerializeField] private TMP_InputField changePasswordConfirmInputField;

    [Space(5f)]
    [Header("Reverify References")]
    [SerializeField] private TMP_InputField reverifyEmailInputField;
    [SerializeField] private TMP_InputField reverifyPasswordInputField;

    [Space(5f)]
    [Header("Action Success Panel References")]
    [SerializeField] private TMP_Text actionSuccessText;

    private LocalProfileProgressWidget localProfileProgressWidget;
    private Sprite defaultProfilePictureSprite;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        EnsureLocalProfileProgressWidget();

        if (BackendManager.instance != null)
        {
            LoadProfile();
        }
    }

    public void LoadProfile()
    {
        BackendManager manager = BackendManager.instance;
        if (manager == null || manager.CurrentUser == null)
        {
            return;
        }

        ProfileDto profile = manager.CurrentProfile;
        AuthUserDto user = manager.CurrentUser;
        string displayName = !string.IsNullOrWhiteSpace(profile?.displayName) ? profile.displayName : user.name;

        usernameText.text = string.IsNullOrWhiteSpace(displayName) ? "Игрок" : displayName;
        emailText.text = user.email;

        if (!string.IsNullOrWhiteSpace(profile?.avatarUrl))
        {
            StartCoroutine(LoadImage(profile.avatarUrl));
        }
        else if (profilePicture != null)
        {
            if (defaultProfilePictureSprite == null)
            {
                defaultProfilePictureSprite = profilePicture.sprite;
            }

            profilePicture.sprite = defaultProfilePictureSprite;
        }

        if (profile?.progress != null && !string.IsNullOrWhiteSpace(profile.progress.xp))
        {
            Output($"XP: {profile.progress.xp}");
        }

        localProfileProgressWidget?.Refresh();
    }

    private IEnumerator LoadImage(string url)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Output("Failed to load image");
            yield break;
        }

        Texture2D image = DownloadHandlerTexture.GetContent(request);
        profilePicture.sprite = Sprite.Create(
            image,
            new Rect(0, 0, image.width, image.height),
            Vector2.zero
        );
    }

    public void Output(string message)
    {
        outputText.text = message;
    }

    public void ClearUI()
    {
        outputText.text = string.Empty;

        profileUI.SetActive(false);
        changePfpUI.SetActive(false);
        changeEmailUI.SetActive(false);
        changePasswordUI.SetActive(false);
        reverifyUI.SetActive(false);
        resetPasswordConfirmUI.SetActive(false);
        actionSuccessPanelUI.SetActive(false);
        deleteUserConfirmUI.SetActive(false);
    }

    public void ProfileUI()
    {
        ClearUI();
        profileUI.SetActive(true);
        EnsureLocalProfileProgressWidget();
        LoadProfile();
        localProfileProgressWidget?.Refresh();

        ProfileArtworksCarousel carousel = FindObjectOfType<ProfileArtworksCarousel>(true);
        carousel?.RefreshCarousel();
        artworksGallery?.RefreshGallery();
    }

    public void ChangePfpUI()
    {
        ClearUI();
        changePfpUI.SetActive(true);
    }

    public void ChangeEmailUI()
    {
        ClearUI();
        changeEmailUI.SetActive(true);
    }

    public void ChangePasswordUI()
    {
        ClearUI();
        changePasswordUI.SetActive(true);
    }

    public void ReverifyUI()
    {
        ClearUI();
        reverifyUI.SetActive(true);
    }

    public void ResetPasswordConfirmUI()
    {
        ClearUI();
        resetPasswordConfirmUI.SetActive(true);
    }

    public void DeleteUserConfirmUI()
    {
        ClearUI();
        deleteUserConfirmUI.SetActive(true);
    }

    public void ChangePfpSuccess()
    {
        ClearUI();
        actionSuccessPanelUI.SetActive(true);
        actionSuccessText.text = "Profile Picture Changed Successfully";
    }

    public void SelectProfileImageButton()
    {
        if (!NativeWindowsFilePicker.IsSupported)
        {
            Output("File picker is not available on this platform. Paste the image path manually.");
            return;
        }

        string selectedPath = NativeWindowsFilePicker.OpenImageFile();
        if (string.IsNullOrWhiteSpace(selectedPath))
        {
            Output("Image selection was cancelled.");
            return;
        }

        profilePictureLink.text = selectedPath;
        Output("Image selected. Tap upload to save it to your profile.");
    }

    public void SubmitProfileImageButton()
    {
        if (string.IsNullOrWhiteSpace(profilePictureLink.text))
        {
            SelectProfileImageButton();
            if (string.IsNullOrWhiteSpace(profilePictureLink.text))
            {
                return;
            }
        }

        BackendManager.instance.UpdateProfilePicture(profilePictureLink.text);
    }

    public void SubmitEmailButton()
    {
        BackendManager.instance.ChangeEmail(changeEmailInputField.text);
    }

    public void SubmitPasswordButton()
    {
        string pass = changePasswordInputField.text;
        string confirm = changePasswordConfirmInputField.text;

        if (pass != confirm)
        {
            Output("Passwords do not match");
            return;
        }

        BackendManager.instance.ChangePassword(pass);
    }

    public void SignOut()
    {
        BackendManager.instance.SignOut();
    }

    private void EnsureLocalProfileProgressWidget()
    {
        if (profileUI == null)
        {
            return;
        }

        localProfileProgressWidget = profileUI.GetComponent<LocalProfileProgressWidget>();
        if (localProfileProgressWidget == null)
        {
            localProfileProgressWidget = profileUI.AddComponent<LocalProfileProgressWidget>();
        }

        TMP_Text titleText = FindChildTextByName(profileUI.transform, "Title");
        if (titleText != null)
        {
            localProfileProgressWidget.InitializeWithTitle(titleText);
        }
    }

    private static TMP_Text FindChildTextByName(Transform root, string objectName)
    {
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] != null && children[i].name == objectName)
            {
                return children[i].GetComponent<TMP_Text>();
            }
        }

        return null;
    }
}
