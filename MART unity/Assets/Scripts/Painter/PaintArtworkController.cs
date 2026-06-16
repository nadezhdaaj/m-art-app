using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PaintArtworkController : MonoBehaviour
{
    private enum PaintWorkspaceNavigationTab
    {
        Auto = 0,
        Home = 1,
        Games = 2,
        Library = 3,
        Profile = 4,
    }

    private const string NamingPanelObjectName = "for the name";
    private const string TitleInputObjectName = "title";
    private const string DetailsInputObjectName = "Additionally";
    private const string SubmitNamingButtonObjectName = "to send";

    [Header("References")]
    [SerializeField] private Painter painter;
    [SerializeField] private Button saveArtworkButton;
    [SerializeField] private TMP_InputField titleInput;
    [SerializeField] private TMP_InputField detailsInput;
    [SerializeField] private TMP_Text statusText;

    [Header("Naming panel")]
    [SerializeField] private GameObject namingPanel;
    [SerializeField] private Button submitNamingButton;

    [Header("Navigation")]
    [SerializeField] private string profileSceneName = "The main stage";
    [SerializeField]
    [Tooltip(
        "После тапа по работе в профиле: какую нижнюю вкладку включить перед показом рисовалки.\n" +
        "• Auto — по иерархии: Games / Library / Home / Profile. Без угадывания ни одна вкладка не переключается (Library из профиля не открывается).\n" +
        "• Games / … — вручную, если рисовалка не внутри этих экранов в Hierarchy.")]
    private PaintWorkspaceNavigationTab openUnderNavigationTab = PaintWorkspaceNavigationTab.Auto;

    private ArtworkDto currentArtwork;
    private bool isBusy;
    private byte[] pendingPngBytes;
    private bool pendingSaveIncludesImage;

    private void Awake()
    {
        if (painter == null)
        {
            painter = FindObjectOfType<Painter>(true);
        }

        ResolveNamingPanelReferences();
        HideNamingPanel();
    }

    private void OnEnable()
    {
        if (painter != null)
        {
            painter.OnCanvasDirtyChanged += HandleCanvasDirtyChanged;
        }

        if (saveArtworkButton != null)
        {
            saveArtworkButton.onClick.RemoveListener(OnSaveButtonClicked);
            saveArtworkButton.onClick.AddListener(OnSaveButtonClicked);
        }

        ResolveNamingPanelReferences();

        if (BackendManager.instance == null || !BackendManager.instance.ShouldDeferArtworkAutoLoad)
        {
            TryLoadPendingArtwork();
        }

        RefreshSaveButtonVisibility();
    }

    private void OnDisable()
    {
        if (painter != null)
        {
            painter.OnCanvasDirtyChanged -= HandleCanvasDirtyChanged;
        }

        if (saveArtworkButton != null)
        {
            saveArtworkButton.onClick.RemoveListener(OnSaveButtonClicked);
        }

    }

    public void OnSaveButtonClicked()
    {
        SaveArtwork();
    }

    public void SaveArtwork()
    {
        if (isBusy || painter == null)
        {
            return;
        }

        byte[] pngBytes = painter.EncodeCanvasToPng();
        if (pngBytes == null || pngBytes.Length == 0)
        {
            SetStatus("\u041D\u0435 \u0443\u0434\u0430\u043B\u043E\u0441\u044C \u043F\u043E\u0434\u0433\u043E\u0442\u043E\u0432\u0438\u0442\u044C \u0438\u0437\u043E\u0431\u0440\u0430\u0436\u0435\u043D\u0438\u0435 \u043A \u0441\u043E\u0445\u0440\u0430\u043D\u0435\u043D\u0438\u044E.");
            return;
        }

        pendingPngBytes = pngBytes;
        pendingSaveIncludesImage = true;
        ShowNamingPanel();
    }

    public void SubmitArtworkNaming()
    {
        ResolveNamingPanelReferences();

        if (isBusy)
        {
            SetStatus("\u041F\u043E\u0434\u043E\u0436\u0434\u0438\u0442\u0435, \u0441\u043E\u0445\u0440\u0430\u043D\u0435\u043D\u0438\u0435 \u0443\u0436\u0435 \u0432\u044B\u043F\u043E\u043B\u043D\u044F\u0435\u0442\u0441\u044F...");
            return;
        }

        if (BackendManager.instance == null)
        {
            const string message = "\u0412\u043E\u0439\u0434\u0438\u0442\u0435 \u0432 \u0430\u043A\u043A\u0430\u0443\u043D\u0442, \u0447\u0442\u043E\u0431\u044B \u0441\u043E\u0445\u0440\u0430\u043D\u0438\u0442\u044C \u0440\u0430\u0431\u043E\u0442\u0443.";
            SetStatus(message);
            ToastNotification.Show(message);
            Debug.LogWarning("PaintArtworkController: BackendManager.instance == null");
            return;
        }

        string title = GetTrimmedValue(titleInput);
        if (string.IsNullOrWhiteSpace(title))
        {
            ToastNotification.Show("\u0412\u0432\u0435\u0434\u0438\u0442\u0435 \u043D\u0430\u0437\u0432\u0430\u043D\u0438\u0435 \u0440\u0430\u0431\u043E\u0442\u044B.");
            return;
        }

        string description = GetTrimmedValue(detailsInput);

        if (pendingPngBytes == null || pendingPngBytes.Length == 0)
        {
            ToastNotification.Show("\u041D\u0435 \u0443\u0434\u0430\u043B\u043E\u0441\u044C \u043F\u043E\u0434\u0433\u043E\u0442\u043E\u0432\u0438\u0442\u044C \u0438\u0437\u043E\u0431\u0440\u0430\u0436\u0435\u043D\u0438\u0435.");
            return;
        }

        bool isNewArtwork = string.IsNullOrWhiteSpace(currentArtwork?.id);

        isBusy = true;
        SetStatus(isNewArtwork
            ? "\u0421\u043E\u0445\u0440\u0430\u043D\u044F\u0435\u043C \u0440\u0430\u0431\u043E\u0442\u0443..."
            : "\u041E\u0431\u043D\u043E\u0432\u043B\u044F\u0435\u043C \u0440\u0430\u0431\u043E\u0442\u0443...");
        RefreshSaveButtonVisibility();

        if (isNewArtwork)
        {
            BackendManager.instance.SaveNewArtworkFromCanvas(
                title,
                description,
                pendingPngBytes,
                HandleArtworkSaved
            );
            return;
        }

        BackendManager.instance.UpdateArtworkFromCanvas(
            currentArtwork.id,
            title,
            description,
            pendingPngBytes,
            pendingSaveIncludesImage,
            HandleArtworkSaved
        );
    }

    public void RefreshNamingPanelBindings()
    {
        ResolveNamingPanelReferences();
    }

    public void BeginNewArtwork()
    {
        currentArtwork = null;
        pendingPngBytes = null;
        pendingSaveIncludesImage = false;
        isBusy = false;

        if (painter == null)
        {
            painter = FindObjectOfType<Painter>(true);
        }

        if (painter != null)
        {
            painter.ClearCanvas();
        }

        if (titleInput != null)
        {
            titleInput.text = string.Empty;
        }

        if (detailsInput != null)
        {
            detailsInput.text = string.Empty;
        }

        HideNamingPanel();
        SetStatus(string.Empty);
        RefreshSaveButtonVisibility();
    }

    public void CloseNamingPanel()
    {
        HideNamingPanel();
    }

    public void OpenProfileAfterSave()
    {
        if (!string.IsNullOrWhiteSpace(profileSceneName))
        {
            SceneManager.LoadScene(profileSceneName);
        }
    }

    public static void PresentPendingArtworkInPaintWorkspace()
    {
        if (BackendManager.instance != null)
        {
            BackendManager.instance.ClearDeferredArtworkAutoLoad();
        }

        PaintArtworkController paint = FindObjectOfType<PaintArtworkController>(true);
        if (paint == null)
        {
            Debug.LogWarning("PaintArtworkController: не найден в сцене.");
            return;
        }

        Navigation nav = FindObjectOfType<Navigation>(true);
        if (nav != null)
        {
            paint.ApplyNavigationTabForPaintWorkspace(nav);
        }

        paint.OpenPaintWorkspaceForPendingArtwork();

        if (BackendManager.instance != null)
        {
            BackendManager.instance.ConsumeOpenPaintFromProfileSceneIntent();
        }
    }

    public void ApplyNavigationTabForPaintWorkspace(Navigation nav)
    {
        if (nav == null)
        {
            return;
        }

        switch (openUnderNavigationTab)
        {
            case PaintWorkspaceNavigationTab.Home:
                nav.OpenHome();
                return;
            case PaintWorkspaceNavigationTab.Games:
                nav.OpenGames();
                return;
            case PaintWorkspaceNavigationTab.Library:
                nav.OpenLibrary();
                return;
            case PaintWorkspaceNavigationTab.Profile:
                nav.OpenProfile();
                return;
        }

        Transform pt = transform;
        if (nav.gamesScreen != null && pt.IsChildOf(nav.gamesScreen.transform))
        {
            nav.OpenGames();
            return;
        }

        if (nav.homeScreen != null && pt.IsChildOf(nav.homeScreen.transform))
        {
            nav.OpenHome();
            return;
        }

        if (nav.profileScreen != null && pt.IsChildOf(nav.profileScreen.transform))
        {
            nav.OpenProfile();
            return;
        }

        if (nav.libraryScreen != null && pt.IsChildOf(nav.libraryScreen.transform))
        {
            nav.OpenLibrary();
            return;
        }

        if (nav.gamesScreen != null)
        {
            nav.OpenGames();
        }
    }

    public void ActivatePaintUiHierarchy()
    {
        ActivateUiBranchFrom(transform);
    }

    private static void ActivateUiBranchFrom(Transform leaf)
    {
        if (leaf == null)
        {
            return;
        }

        List<Transform> chain = new List<Transform>();
        Transform t = leaf;
        while (t != null)
        {
            chain.Add(t);
            t = t.parent;
        }

        for (int i = chain.Count - 1; i >= 0; i--)
        {
            Transform node = chain[i];
            if (node != null && !node.gameObject.activeSelf)
            {
                node.gameObject.SetActive(true);
            }
        }
    }

    public void OpenPaintWorkspaceForPendingArtwork()
    {
        ActivatePaintUiHierarchy();

        if (painter == null)
        {
            painter = FindObjectOfType<Painter>(true);
        }

        if (painter != null)
        {
            ActivateUiBranchFrom(painter.transform);
        }

        ReloadPendingArtworkForEditing();

        if (BackendManager.instance != null &&
            BackendManager.instance.TryPeekPendingArtwork(out _))
        {
            StartCoroutine(ReloadPendingAfterPaintHierarchyReady());
        }
    }

    private IEnumerator ReloadPendingAfterPaintHierarchyReady()
    {
        yield return null;
        if (painter == null)
        {
            painter = FindObjectOfType<Painter>(true);
        }

        if (painter != null)
        {
            ActivateUiBranchFrom(painter.transform);
        }

        ReloadPendingArtworkForEditing();
    }

    private void TryLoadPendingArtwork()
    {
        if (BackendManager.instance == null || painter == null)
        {
            return;
        }

        if (!BackendManager.instance.TryConsumePendingArtwork(out ArtworkDto artwork) || artwork == null)
        {
            return;
        }

        ApplyArtworkToCanvas(artwork);
    }

    public void ReloadPendingArtworkForEditing()
    {
        TryLoadPendingArtwork();
    }

    private void ApplyArtworkToCanvas(ArtworkDto artwork)
    {
        currentArtwork = artwork;

        if (titleInput != null)
        {
            titleInput.text = artwork.title ?? string.Empty;
        }

        if (detailsInput != null)
        {
            detailsInput.text = artwork.description ?? string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(artwork.imageUrl))
        {
            StartCoroutine(LoadArtworkImage(ResolveArtworkImageUrl(artwork.imageUrl)));
        }
        else
        {
            painter.MarkCanvasAsClean();
        }

        SetStatus("\u041E\u0442\u043A\u0440\u044B\u0442\u0430 \u0441\u043E\u0445\u0440\u0430\u043D\u0451\u043D\u043D\u0430\u044F \u0440\u0430\u0431\u043E\u0442\u0430.");
        RefreshSaveButtonVisibility();
    }

    private IEnumerator LoadArtworkImage(string imageUrl)
    {
        using UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            SetStatus("\u041D\u0435 \u0443\u0434\u0430\u043B\u043E\u0441\u044C \u0437\u0430\u0433\u0440\u0443\u0437\u0438\u0442\u044C \u0441\u043E\u0445\u0440\u0430\u043D\u0451\u043D\u043D\u0443\u044E \u0440\u0430\u0431\u043E\u0442\u0443 \u043D\u0430 \u0445\u043E\u043B\u0441\u0442.");
            yield break;
        }

        Texture2D texture = DownloadHandlerTexture.GetContent(request);
        painter.LoadCanvasFromTexture(texture, false);
        painter.MarkCanvasAsClean();
        RefreshSaveButtonVisibility();
    }

    private void HandleArtworkSaved(ApiResult<ArtworkDto> result)
    {
        isBusy = false;

        if (result == null)
        {
            SetStatus("\u0421\u043E\u0445\u0440\u0430\u043D\u0435\u043D\u0438\u0435 \u043D\u0435 \u0443\u0434\u0430\u043B\u043E\u0441\u044C.");
            RefreshSaveButtonVisibility();
            return;
        }

        if (!result.Success || result.Data == null)
        {
            SetStatus(string.IsNullOrWhiteSpace(result.ErrorMessage)
                ? "\u041D\u0435 \u0443\u0434\u0430\u043B\u043E\u0441\u044C \u0441\u043E\u0445\u0440\u0430\u043D\u0438\u0442\u044C \u043F\u0440\u043E\u0438\u0437\u0432\u0435\u0434\u0435\u043D\u0438\u0435."
                : result.ErrorMessage);
            RefreshSaveButtonVisibility();
            return;
        }

        bool wasNewArtwork = string.IsNullOrWhiteSpace(currentArtwork?.id);
        currentArtwork = result.Data;
        pendingPngBytes = null;
        pendingSaveIncludesImage = false;
        painter.MarkCanvasAsClean();
        HideNamingPanel();
        SetStatus("\u0420\u0430\u0431\u043E\u0442\u0430 \u0441\u043E\u0445\u0440\u0430\u043D\u0435\u043D\u0430 \u0432 \u043F\u0440\u043E\u0444\u0438\u043B\u044C.");
        RefreshSaveButtonVisibility();
        RefreshProfileArtworks();
        ToastNotification.Show("\u0420\u0430\u0431\u043E\u0442\u0430 \u0441\u043E\u0445\u0440\u0430\u043D\u0435\u043D\u0430 \u0432 \u043F\u0440\u043E\u0444\u0438\u043B\u044C.");

        if (wasNewArtwork)
        {
            BeginNewArtwork();
        }
    }

    private void ShowNamingPanel()
    {
        ResolveNamingPanelReferences();

        if (string.IsNullOrWhiteSpace(currentArtwork?.id))
        {
            if (titleInput != null)
            {
                titleInput.text = string.Empty;
            }

            if (detailsInput != null)
            {
                detailsInput.text = string.Empty;
            }
        }
        else
        {
            if (titleInput != null)
            {
                titleInput.text = currentArtwork.title ?? string.Empty;
            }

            if (detailsInput != null)
            {
                detailsInput.text = currentArtwork.description ?? string.Empty;
            }
        }

        if (namingPanel != null)
        {
            namingPanel.transform.SetAsLastSibling();
            namingPanel.SetActive(true);
        }
    }

    private void HideNamingPanel()
    {
        if (namingPanel != null)
        {
            namingPanel.SetActive(false);
        }
    }

    private void ResolveNamingPanelReferences()
    {
        if (namingPanel == null)
        {
            namingPanel = FindSceneObject(NamingPanelObjectName);
        }

        if (namingPanel == null)
        {
            // Панель названия не выложена в сцене — строим её в рантайме,
            // иначе SaveArtwork() показывает null-панель и сохранение не доходит до бэкенда.
            namingPanel = ArtworkNamingPanelBuilder.Build(this);
        }

        Transform panelRoot = namingPanel != null ? namingPanel.transform : null;

        if (titleInput == null)
        {
            titleInput = ResolveInputField(panelRoot, TitleInputObjectName);
        }

        if (detailsInput == null)
        {
            detailsInput = ResolveInputField(panelRoot, DetailsInputObjectName);
        }

        if (submitNamingButton == null)
        {
            GameObject sendObject = ResolveChildObject(panelRoot, SubmitNamingButtonObjectName);
            if (sendObject != null)
            {
                submitNamingButton = sendObject.GetComponent<Button>();
                if (sendObject.GetComponent<ArtworkNamingSubmitButton>() == null)
                {
                    sendObject.AddComponent<ArtworkNamingSubmitButton>();
                }
            }
        }
    }

    private static TMP_InputField ResolveInputField(Transform panelRoot, string objectName)
    {
        GameObject inputObject = ResolveChildObject(panelRoot, objectName);
        return inputObject != null ? inputObject.GetComponent<TMP_InputField>() : null;
    }

    private static GameObject ResolveChildObject(Transform panelRoot, string objectName)
    {
        if (panelRoot != null)
        {
            Transform foundInPanel = FindChildRecursive(panelRoot, objectName);
            if (foundInPanel != null)
            {
                return foundInPanel.gameObject;
            }
        }

        return FindSceneObject(objectName);
    }

    private static GameObject FindSceneObject(string objectName)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !activeScene.isLoaded)
        {
            return null;
        }

        GameObject[] roots = activeScene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform found = FindChildRecursive(roots[i].transform, objectName);
            if (found != null)
            {
                return found.gameObject;
            }
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform parent, string objectName)
    {
        if (parent.name == objectName)
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindChildRecursive(parent.GetChild(i), objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private void RefreshProfileArtworks()
    {
        ProfileArtworksGallery gallery = FindObjectOfType<ProfileArtworksGallery>(true);
        if (gallery != null && gallery.isActiveAndEnabled)
        {
            gallery.RefreshGallery();
        }

        ProfileArtworksCarousel carousel = FindObjectOfType<ProfileArtworksCarousel>(true);
        if (carousel != null)
        {
            carousel.RefreshCarousel();
        }

        if (gallery != null || carousel != null)
        {
            return;
        }

        if (LobbyManager.instance != null)
        {
            LobbyManager.instance.ProfileUI();
        }
    }

    private void HandleCanvasDirtyChanged(bool _)
    {
        RefreshSaveButtonVisibility();
    }

    private void RefreshSaveButtonVisibility()
    {
        if (saveArtworkButton == null)
        {
            return;
        }

        bool canShowButton = currentArtwork != null || (painter != null && painter.HasUserPaintedSinceReset);
        saveArtworkButton.gameObject.SetActive(canShowButton);
        saveArtworkButton.interactable = canShowButton && !isBusy;
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private string GetTrimmedValue(TMP_InputField input)
    {
        return input == null ? null : input.text?.Trim();
    }

    private static string ResolveArtworkImageUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return url;
        }

        if (url.StartsWith("http://") || url.StartsWith("https://"))
        {
            return url;
        }

        if (BackendManager.instance == null)
        {
            return url;
        }

        string baseUrl = BackendManager.instance.ApiBaseUrl;
        return url.StartsWith("/") ? baseUrl + url : baseUrl + "/" + url;
    }
}
