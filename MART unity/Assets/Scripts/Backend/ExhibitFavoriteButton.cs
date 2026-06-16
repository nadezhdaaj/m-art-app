using UnityEngine;
using UnityEngine.UI;

public class ExhibitFavoriteButton : MonoBehaviour
{
    private const string HeartSpriteResourcePath = "Icons/favorite-heart";

    [SerializeField] private Button favoriteButton;
    [SerializeField] private Image heartImage;
    [SerializeField] private string exhibitId;
    [SerializeField] private bool bindToCurrentArExhibit = true;

    private ARImageSpawner imageSpawner;
    private bool isSubmitting;

    private void Awake()
    {
        ResolveReferences();
        ConfigureHeartVisual();
    }

    private void OnEnable()
    {
        if (bindToCurrentArExhibit)
        {
            BindToImageSpawner();
        }

        ExhibitFavoritesStore.FavoritesChanged += RefreshVisual;
        RefreshVisual();
    }

    private void OnDisable()
    {
        UnbindFromImageSpawner();
        ExhibitFavoritesStore.FavoritesChanged -= RefreshVisual;

        if (favoriteButton != null)
        {
            favoriteButton.onClick.RemoveListener(HandleClicked);
        }
    }

    public void SetExhibitId(string value)
    {
        exhibitId = value;
        RefreshVisual();
    }

    private void Start()
    {
        if (bindToCurrentArExhibit)
        {
            BindToImageSpawner();
            RefreshVisual();
        }
    }

    private void ResolveReferences()
    {
        if (favoriteButton == null)
        {
            favoriteButton = GetComponent<Button>();
        }

        if (heartImage == null)
        {
            heartImage = GetComponent<Image>();
        }

        if (favoriteButton != null)
        {
            favoriteButton.onClick.RemoveListener(HandleClicked);
            favoriteButton.onClick.AddListener(HandleClicked);
            favoriteButton.transition = Selectable.Transition.None;
        }
    }

    private void ConfigureHeartVisual()
    {
        if (heartImage == null)
        {
            return;
        }

        Sprite heartSprite = Resources.Load<Sprite>(HeartSpriteResourcePath);
        if (heartSprite == null)
        {
            heartSprite = FindHeartSpriteInLoadedAssets();
        }

        if (heartSprite != null && (heartImage.sprite == null || heartImage.sprite.name.Contains("UISprite")))
        {
            heartImage.sprite = heartSprite;
            heartImage.preserveAspect = true;
        }
    }

    private static Sprite FindHeartSpriteInLoadedAssets()
    {
        Sprite[] sprites = Resources.FindObjectsOfTypeAll<Sprite>();
        for (int i = 0; i < sprites.Length; i++)
        {
            Sprite sprite = sprites[i];
            if (sprite != null && sprite.name == "streak")
            {
                return sprite;
            }
        }

        return null;
    }

    private void BindToImageSpawner()
    {
        if (!bindToCurrentArExhibit)
        {
            return;
        }

        if (imageSpawner == null)
        {
            imageSpawner = FindObjectOfType<ARImageSpawner>();
        }

        if (imageSpawner == null)
        {
            return;
        }

        imageSpawner.CurrentExhibitChanged -= HandleCurrentExhibitChanged;
        imageSpawner.CurrentExhibitChanged += HandleCurrentExhibitChanged;

        if (!string.IsNullOrWhiteSpace(imageSpawner.CurrentExhibitId))
        {
            SetExhibitId(imageSpawner.CurrentExhibitId);
        }
    }

    private void UnbindFromImageSpawner()
    {
        if (imageSpawner == null)
        {
            return;
        }

        imageSpawner.CurrentExhibitChanged -= HandleCurrentExhibitChanged;
    }

    private void HandleCurrentExhibitChanged(string currentExhibitId)
    {
        SetExhibitId(currentExhibitId);
    }

    private void HandleClicked()
    {
        if (isSubmitting)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(exhibitId))
        {
            ToastNotification.Show("\u0421\u043D\u0430\u0447\u0430\u043B\u0430 \u043D\u0430\u0432\u0435\u0434\u0438\u0442\u0435 \u043A\u0430\u043C\u0435\u0440\u0443 \u043D\u0430 \u043C\u0430\u0440\u043A\u0435\u0440 \u044D\u043A\u0441\u043F\u043E\u043D\u0430\u0442\u0430.");
            return;
        }

        bool wasFavorite = ExhibitFavoritesStore.IsFavorite(exhibitId);
        if (wasFavorite)
        {
            ExhibitFavoritesStore.RemoveLocal(exhibitId);
            ToastNotification.Show("\u0423\u0431\u0440\u0430\u043D\u043E \u0438\u0437 \u0438\u0437\u0431\u0440\u0430\u043D\u043D\u043E\u0433\u043E");
            RefreshVisual();
            return;
        }

        ExhibitFavoritesStore.AddLocal(exhibitId);
        ToastNotification.Show("\u0414\u043E\u0431\u0430\u0432\u043B\u0435\u043D\u043E \u0432 \u0438\u0437\u0431\u0440\u0430\u043D\u043D\u043E\u0435");
        RefreshVisual();

        if (BackendManager.instance != null && BackendManager.instance.HasAuthorizedSession)
        {
            isSubmitting = true;
            BackendManager.instance.AddFavoriteExhibit(exhibitId, result =>
            {
                isSubmitting = false;
            });
        }
    }

    private void RefreshVisual()
    {
        if (heartImage == null)
        {
            return;
        }

        bool isFavorite = ExhibitFavoritesStore.IsFavorite(exhibitId);
        heartImage.color = isFavorite
            ? ExhibitCatalog.GetFavoriteHeartColor()
            : ExhibitCatalog.GetActiveHeartColor();
    }
}
