using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen list of the user's artworks (opened from "See All..").
/// </summary>
[DisallowMultipleComponent]
public class UserWorksScreen : MonoBehaviour
{
    private const string BackButtonName = "BackButton";
    private const string DeleteButtonName = "DeleteArtworksButton";

    [SerializeField] private GameObject screenRoot;
    [SerializeField] private Button backButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private ProfileArtworksGallery gallery;

    private Text deleteButtonLabel;

    private static UserWorksScreen instance;

    public static UserWorksScreen Instance => instance;

    private void Awake()
    {
        if (screenRoot == null)
        {
            screenRoot = gameObject;
        }

        instance = this;
        EnsureBackButton();
        EnsureDeleteButton();
        UserWorksGalleryBuilder.Ensure(gameObject);
        ResolveGallery();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public static void ShowScreen()
    {
        if (instance == null)
        {
            instance = FindObjectOfType<UserWorksScreen>(true);
        }

        if (instance == null)
        {
            ProfileUiArtworksBootstrap.EnsureAll();
            instance = FindObjectOfType<UserWorksScreen>(true);
        }

        instance?.Show();
    }

    public static void HideScreen()
    {
        instance?.Hide();
    }

    public void Show()
    {
        ProfileUiArtworksHierarchyBuilder.EnsureUserWorksScreen();
        UserWorksGalleryBuilder.Ensure(gameObject);
        ResolveGallery();

        transform.SetAsLastSibling();

        if (screenRoot != null)
        {
            screenRoot.SetActive(true);
        }
        else
        {
            gameObject.SetActive(true);
        }

        gallery?.RefreshGallery();
        UpdateDeleteButtonLabel(false);
    }

    public void ToggleDeleteMode()
    {
        ResolveGallery();
        if (gallery == null)
        {
            return;
        }

        gallery.ToggleDeleteMode();
    }

    private void HandleGalleryDeleteModeChanged(bool active)
    {
        UpdateDeleteButtonLabel(active);
    }

    private void UpdateDeleteButtonLabel(bool deleteActive)
    {
        if (deleteButtonLabel != null)
        {
            deleteButtonLabel.text = deleteActive ? "Готово" : "Удалить";
        }
    }

    public void Hide()
    {
        if (screenRoot != null)
        {
            screenRoot.SetActive(false);
            return;
        }

        gameObject.SetActive(false);
    }

    private void ResolveGallery()
    {
        if (gallery == null)
        {
            gallery = GetComponent<ProfileArtworksGallery>();
        }

        if (gallery == null)
        {
            gallery = GetComponentInChildren<ProfileArtworksGallery>(true);
        }

        if (gallery != null)
        {
            gallery.DeleteModeChanged = HandleGalleryDeleteModeChanged;
        }
    }

    private void EnsureBackButton()
    {
        if (backButton == null)
        {
            Transform existing = transform.Find(BackButtonName);
            if (existing != null)
            {
                backButton = existing.GetComponent<Button>();
            }
        }

        if (backButton == null)
        {
            backButton = CreateBackButton();
        }

        backButton.onClick.RemoveListener(Hide);
        backButton.onClick.AddListener(Hide);
        backButton.transform.SetAsLastSibling();
    }

    private void EnsureDeleteButton()
    {
        if (deleteButton == null)
        {
            Transform existing = transform.Find(DeleteButtonName) ?? transform.Find("Delete");
            if (existing != null)
            {
                deleteButton = existing.GetComponent<Button>();
            }
        }

        if (deleteButton == null)
        {
            deleteButton = CreateDeleteButton();
        }

        deleteButtonLabel = deleteButton.GetComponentInChildren<Text>();
        deleteButton.onClick.RemoveListener(ToggleDeleteMode);
        deleteButton.onClick.AddListener(ToggleDeleteMode);
        deleteButton.transform.SetAsLastSibling();
        UpdateDeleteButtonLabel(false);
    }

    private Button CreateDeleteButton()
    {
        GameObject buttonObject = new GameObject(DeleteButtonName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(transform, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(180f, 56f);
        rect.anchoredPosition = new Vector2(-24f, -24f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.85f, 0.2f, 0.2f, 1f);
        image.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Text label = labelObject.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.text = "Удалить";
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.raycastTarget = false;

        return button;
    }

    private Button CreateBackButton()
    {
        GameObject buttonObject = new GameObject(BackButtonName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(transform, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(120f, 56f);
        rect.anchoredPosition = new Vector2(24f, -24f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.92f, 0.92f, 0.92f, 1f);
        image.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Text label = labelObject.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.text = "Назад";
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.black;
        label.raycastTarget = false;

        return button;
    }
}
