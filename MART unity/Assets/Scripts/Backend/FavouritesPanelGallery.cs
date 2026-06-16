using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FavouritesPanelGallery : MonoBehaviour
{
    [SerializeField] private Transform contentRoot;
    [SerializeField] private ExhibitFavoriteCardView cardTemplate;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject emptyState;

    private readonly List<GameObject> hiddenPlaceholders = new List<GameObject>();

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ExhibitFavoritesStore.FavoritesChanged -= RenderFromLocalStore;
        ExhibitFavoritesStore.FavoritesChanged += RenderFromLocalStore;
        RefreshGallery();
    }

    private void OnDisable()
    {
        ExhibitFavoritesStore.FavoritesChanged -= RenderFromLocalStore;
    }

    public void RefreshGallery()
    {
        ResolveReferences();
        HidePlaceholders();
        RenderFromLocalStore();

        if (BackendManager.instance != null && BackendManager.instance.HasAuthorizedSession)
        {
            BackendManager.instance.LoadFavoriteExhibits(HandleFavoritesLoaded);
        }
    }

    private void RenderFromLocalStore()
    {
        ResolveReferences();
        HidePlaceholders();
        ClearCards();

        IReadOnlyCollection<string> favorites = ExhibitFavoritesStore.GetAll();
        if (favorites == null || favorites.Count == 0)
        {
            SetStatus(string.Empty);
            ToggleEmptyState(true);
            return;
        }

        ToggleEmptyState(false);
        SetStatus(string.Empty);

        foreach (string exhibitId in favorites)
        {
            BuildCard(exhibitId);
        }
    }

    private void HandleFavoritesLoaded(ApiResult<StringArrayWrapperDto> result)
    {
        if (result == null || !result.Success)
        {
            return;
        }

        string[] favorites = result.Data != null ? result.Data.items : null;
        ExhibitFavoritesStore.MergeFromRemote(favorites);
    }

    private void BuildCard(string exhibitId)
    {
        if (string.IsNullOrWhiteSpace(exhibitId) || cardTemplate == null || contentRoot == null)
        {
            return;
        }

        ExhibitFavoriteCardView card = Instantiate(cardTemplate, contentRoot);
        card.gameObject.SetActive(true);
        card.Bind(exhibitId, ExhibitCatalog.GetPreviewSprite(exhibitId), OpenExhibitInAr);
    }

    private void OpenExhibitInAr(string exhibitId)
    {
        if (string.IsNullOrWhiteSpace(exhibitId))
        {
            return;
        }

        OpenARScene openAr = FindObjectOfType<OpenARScene>();
        if (openAr == null)
        {
            GameObject host = new GameObject("OpenARScene");
            openAr = host.AddComponent<OpenARScene>();
        }

        openAr.OpenARForExhibit(exhibitId);
    }

    private void ResolveReferences()
    {
        if (contentRoot == null)
        {
            contentRoot = transform;
        }

        if (cardTemplate == null)
        {
            Transform templateTransform = transform.Find("Favourites (1)");
            if (templateTransform == null)
            {
                templateTransform = transform.Find("Favourites");
            }

            if (templateTransform != null)
            {
                cardTemplate = templateTransform.GetComponent<ExhibitFavoriteCardView>();
                if (cardTemplate == null)
                {
                    cardTemplate = templateTransform.gameObject.AddComponent<ExhibitFavoriteCardView>();
                }

                cardTemplate.gameObject.SetActive(false);
            }
        }
    }

    private void HidePlaceholders()
    {
        hiddenPlaceholders.Clear();

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child == null || child == cardTemplate?.transform)
            {
                continue;
            }

            if (child.name == "Favourites" || child.name == "Favourites (1)")
            {
                if (child.gameObject.activeSelf)
                {
                    child.gameObject.SetActive(false);
                    hiddenPlaceholders.Add(child.gameObject);
                }
            }
        }
    }

    private void ClearCards()
    {
        if (contentRoot == null)
        {
            return;
        }

        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = contentRoot.GetChild(i);
            if (child == null || child == cardTemplate?.transform)
            {
                continue;
            }

            ExhibitFavoriteCardView card = child.GetComponent<ExhibitFavoriteCardView>();
            if (card != null && card != cardTemplate)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void ToggleEmptyState(bool visible)
    {
        if (emptyState != null)
        {
            emptyState.SetActive(visible);
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
}
