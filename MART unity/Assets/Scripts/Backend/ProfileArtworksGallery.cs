using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProfileArtworksGallery : MonoBehaviour
{
    private const string ScrollContainerObjectName = "scroll";
    private const string ScrollHandleObjectName = "scroll";
    private const string ScrollHandleAltObjectName = "ScrollHandle";
    private const string ScrollViewportObjectName = "ArtworksScrollViewport";
    private const string ArtworksListObjectName = "ArtworksList";

    [Header("References")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private RectTransform scrollContainer;
    [SerializeField] private RectTransform scrollHandle;
    [SerializeField] private RectTransform scrollViewport;
    [SerializeField] private ArtworkCardView artworkCardPrefab;
    [SerializeField] private GameObject emptyState;
    [SerializeField] private TMP_Text statusText;

    [Header("Layout")]
    [SerializeField] private int gridColumns = 2;
    [SerializeField] private Vector2 gridSpacing = new Vector2(20f, 20f);
    [SerializeField] private int gridPaddingLeft = 12;
    [SerializeField] private int gridPaddingRight = 12;
    [SerializeField] private int gridPaddingTop = 12;
    [SerializeField] private int gridPaddingBottom = 12;

    [Header("Navigation")]
    [SerializeField] private string paintSceneName = "The main stage";

    private ArtworkCardView artworkCardPrefabCache;
    private GridLayoutGroup gridLayout;
    private GalleryVerticalScrollHandle scrollHandleDriver;
    private bool pendingLayoutRebuild;
    private bool deleteMode;

    public bool DeleteMode => deleteMode;

    /// <summary>Сообщает подписчикам (например, кнопке на User's work) об изменении режима удаления.</summary>
    public System.Action<bool> DeleteModeChanged;

    private void Awake()
    {
        if (IsOnOtherPanel())
        {
            enabled = false;
            Destroy(this);
            return;
        }

        ResolveReferences();
        SetupGridLayout();
        SetupScrollHandle();
        ResolveArtworkCardPrefab();
    }

    private bool IsOnOtherPanel()
    {
        Transform current = transform;
        while (current != null)
        {
            if (current.name == "OtherPanel")
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private void OnEnable()
    {
        if (pendingLayoutRebuild)
        {
            pendingLayoutRebuild = false;
            StartCoroutine(RebuildLayoutNextFrame());
            return;
        }

        if (GetComponent<UserWorksScreen>() != null)
        {
            return;
        }

        RefreshGallery();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ResolveReferences();
    }
#endif

    private void ResolveReferences()
    {
        Transform searchRoot = GetGallerySearchRoot();

        if (scrollContainer == null)
        {
            scrollContainer = searchRoot.Find(ScrollContainerObjectName) as RectTransform;
        }

        if (scrollViewport == null && scrollContainer != null)
        {
            scrollViewport = scrollContainer.Find(ScrollViewportObjectName) as RectTransform;
        }

        if (scrollViewport == null)
        {
            scrollViewport = searchRoot.Find(ScrollViewportObjectName) as RectTransform;
        }

        if (contentRoot == null && scrollViewport != null)
        {
            contentRoot = scrollViewport.Find(ArtworksListObjectName);
        }

        if (contentRoot == null)
        {
            contentRoot = searchRoot.Find(ArtworksListObjectName);
        }

        if (scrollHandle == null && scrollContainer != null)
        {
            Transform handleTransform = scrollContainer.Find(ScrollHandleAltObjectName);
            if (handleTransform == null)
            {
                handleTransform = scrollContainer.Find(ScrollHandleObjectName);
            }

            if (handleTransform != null && handleTransform != scrollContainer)
            {
                scrollHandle = handleTransform as RectTransform;
            }
        }
    }

    private Transform GetGallerySearchRoot()
    {
        Transform galleryRoot = transform.Find("UserWorksGalleryRoot");
        return galleryRoot != null ? galleryRoot : transform;
    }

    private void SetupScrollHandle()
    {
        if (scrollViewport == null || contentRoot is not RectTransform contentRect)
        {
            return;
        }

        DisableLegacyScrollComponents();
        EnsureScrollContainerLayout();
        EnsureViewportLayout();
        EnsureHandleRaycast();

        if (scrollHandle == null)
        {
            return;
        }

        scrollHandleDriver = scrollHandle.GetComponent<GalleryVerticalScrollHandle>();
        if (scrollHandleDriver == null)
        {
            scrollHandleDriver = scrollHandle.gameObject.AddComponent<GalleryVerticalScrollHandle>();
        }

        RectTransform trackRect = scrollContainer != null ? scrollContainer : scrollViewport;
        scrollHandleDriver.Setup(scrollViewport, contentRect, scrollHandle, trackRect);

        Transform designElements = scrollContainer != null
            ? scrollContainer.Find("Design elements")
            : transform.Find("Design elements");
        if (designElements != null)
        {
            Image designImage = designElements.GetComponent<Image>();
            if (designImage != null)
            {
                designImage.raycastTarget = false;
            }
        }

        Image panelImage = GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.raycastTarget = false;
        }
    }

    private void DisableLegacyScrollComponents()
    {
        ScrollRect panelScroll = GetComponent<ScrollRect>();
        if (panelScroll != null)
        {
            panelScroll.enabled = false;
        }

        if (scrollViewport == null)
        {
            return;
        }

        ScrollRect viewportScroll = scrollViewport.GetComponent<ScrollRect>();
        if (viewportScroll != null)
        {
            viewportScroll.enabled = false;
        }

        SimpleVerticalDragScroll legacyDragScroll = scrollViewport.GetComponent<SimpleVerticalDragScroll>();
        if (legacyDragScroll != null)
        {
            Destroy(legacyDragScroll);
        }
    }

    private void EnsureScrollContainerLayout()
    {
        if (scrollContainer == null)
        {
            return;
        }

        scrollContainer.anchorMin = Vector2.zero;
        scrollContainer.anchorMax = Vector2.one;
        scrollContainer.pivot = new Vector2(0.5f, 0.5f);
        scrollContainer.anchoredPosition = Vector2.zero;
        scrollContainer.sizeDelta = Vector2.zero;
        scrollContainer.offsetMin = Vector2.zero;
        scrollContainer.offsetMax = Vector2.zero;
    }

    private void EnsureViewportLayout()
    {
        if (scrollViewport == null)
        {
            return;
        }

        scrollViewport.anchorMin = Vector2.zero;
        scrollViewport.anchorMax = Vector2.one;
        scrollViewport.pivot = new Vector2(0.5f, 0.5f);
        scrollViewport.anchoredPosition = Vector2.zero;
        scrollViewport.sizeDelta = Vector2.zero;
        scrollViewport.offsetMin = Vector2.zero;
        scrollViewport.offsetMax = new Vector2(-40f, 0f);

        Image viewportImage = scrollViewport.GetComponent<Image>();
        if (viewportImage != null)
        {
            viewportImage.raycastTarget = false;
        }
    }

    private void EnsureHandleRaycast()
    {
        if (scrollHandle == null)
        {
            return;
        }

        Image handleImage = scrollHandle.GetComponent<Image>();
        if (handleImage != null)
        {
            handleImage.raycastTarget = true;
        }

        Button handleButton = scrollHandle.GetComponent<Button>();
        if (handleButton != null)
        {
            handleButton.transition = Selectable.Transition.None;
        }
    }

    private void SetupGridLayout()
    {
        if (contentRoot == null)
        {
            return;
        }

        HorizontalLayoutGroup horizontal = contentRoot.GetComponent<HorizontalLayoutGroup>();
        if (horizontal != null)
        {
            Destroy(horizontal);
        }

        VerticalLayoutGroup vertical = contentRoot.GetComponent<VerticalLayoutGroup>();
        if (vertical != null)
        {
            Destroy(vertical);
        }

        ContentSizeFitter fitter = contentRoot.GetComponent<ContentSizeFitter>();
        if (fitter != null)
        {
            Destroy(fitter);
        }

        gridLayout = contentRoot.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            gridLayout = contentRoot.gameObject.AddComponent<GridLayoutGroup>();
        }

        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = Mathf.Max(1, gridColumns);
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayout.childAlignment = TextAnchor.UpperCenter;
        gridLayout.spacing = gridSpacing;
        gridLayout.padding = new RectOffset(
            gridPaddingLeft,
            gridPaddingRight,
            gridPaddingTop,
            gridPaddingBottom
        );

        if (contentRoot is RectTransform contentRect)
        {
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
        }
    }

    private void ApplyContentHeight()
    {
        if (gridLayout == null || contentRoot is not RectTransform contentRect || scrollViewport == null)
        {
            return;
        }

        int childCount = contentRoot.childCount;
        if (childCount == 0)
        {
            contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0f);
            return;
        }

        float height = CalculateGridContentHeight(childCount);
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        float preferredHeight = LayoutUtility.GetPreferredHeight(contentRect);
        if (preferredHeight > height)
        {
            height = preferredHeight;
        }

        float width = Mathf.Max(1f, scrollViewport.rect.width);

        contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
    }

    private float CalculateGridContentHeight(int childCount)
    {
        int columns = Mathf.Max(1, gridLayout.constraintCount);
        int rows = Mathf.CeilToInt(childCount / (float)columns);
        return gridLayout.padding.top
            + gridLayout.padding.bottom
            + rows * gridLayout.cellSize.y
            + Mathf.Max(0, rows - 1) * gridLayout.spacing.y;
    }

    private void UpdateGridCellSize()
    {
        if (gridLayout == null || scrollViewport == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();

        float viewportWidth = scrollViewport.rect.width;
        if (viewportWidth <= 1f)
        {
            viewportWidth = ((RectTransform)transform).rect.width;
        }

        float totalHorizontalPadding = gridLayout.padding.left + gridLayout.padding.right;
        float totalHorizontalSpacing = gridLayout.spacing.x * (gridLayout.constraintCount - 1);
        float cellWidth = (viewportWidth - totalHorizontalPadding - totalHorizontalSpacing) / gridLayout.constraintCount;
        cellWidth = Mathf.Max(120f, cellWidth);

        const float cardAspect = 441.6546f / 396.7589f;
        gridLayout.cellSize = new Vector2(cellWidth, cellWidth * cardAspect);
    }

    private void FitCardsToGridCells()
    {
        if (gridLayout == null || contentRoot == null)
        {
            return;
        }

        Vector2 cellSize = gridLayout.cellSize;
        for (int i = 0; i < contentRoot.childCount; i++)
        {
            if (contentRoot.GetChild(i) is RectTransform cardRect)
            {
                cardRect.sizeDelta = cellSize;
            }
        }
    }

    private void ResolveArtworkCardPrefab()
    {
        if (artworkCardPrefab != null)
        {
            return;
        }

        if (artworkCardPrefabCache == null)
        {
            artworkCardPrefabCache = Resources.Load<ArtworkCardView>("ArtworkCard");
        }

        artworkCardPrefab = artworkCardPrefabCache;
    }

    public void RefreshGallery()
    {
        ResolveArtworkCardPrefab();

        // Любая полная перезагрузка списка сбрасывает режим удаления:
        // после удаления работы крестики должны исчезнуть.
        deleteMode = false;
        DeleteModeChanged?.Invoke(false);

        if (BackendManager.instance == null)
        {
            SetStatus("BackendManager не найден.");
            return;
        }

        ClearCards();
        SetStatus("Загружаем ваши произведения...");
        BackendManager.instance.LoadMyArtworks(HandleArtworksLoaded);
    }

    private void HandleArtworksLoaded(ApiResult<ArtworkArrayWrapperDto> result)
    {
        ClearCards();

        if (result == null || !result.Success)
        {
            SetStatus(result == null || string.IsNullOrWhiteSpace(result.ErrorMessage)
                ? "Не удалось загрузить работы."
                : result.ErrorMessage);
            ToggleEmptyState(true);
            return;
        }

        ArtworkDto[] artworks = result.Data != null ? result.Data.items : null;
        if (artworks == null || artworks.Length == 0)
        {
            SetStatus("Пока нет сохранённых работ.");
            ToggleEmptyState(true);
            return;
        }

        ToggleEmptyState(false);
        SetStatus(string.Empty);

        for (int i = 0; i < artworks.Length; i++)
        {
            if (artworkCardPrefab == null)
            {
                continue;
            }

            ArtworkCardView card = Instantiate(artworkCardPrefab, contentRoot);
            card.Bind(artworks[i], OpenArtworkForEditing);
            card.SetupDelete(RequestDeleteArtwork);
            card.SetDeleteMode(deleteMode);
        }

        ScheduleLayoutRebuild();
    }

    private void ScheduleLayoutRebuild()
    {
        if (isActiveAndEnabled)
        {
            StartCoroutine(RebuildLayoutNextFrame());
            return;
        }

        pendingLayoutRebuild = true;
    }

    private IEnumerator RebuildLayoutNextFrame()
    {
        yield return null;
        yield return null;

        Canvas.ForceUpdateCanvases();
        UpdateGridCellSize();
        FitCardsToGridCells();

        if (contentRoot is RectTransform contentRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            ApplyContentHeight();
        }

        SetupScrollHandle();
        scrollHandleDriver?.SyncAfterContentResize();
        scrollHandleDriver?.ResetToTop();
    }

    private void OpenArtworkForEditing(ArtworkDto artwork)
    {
        ProfileArtworkNavigation.OpenArtworkForEditing(artwork, paintSceneName);
    }

    public void ToggleDeleteMode()
    {
        SetDeleteMode(!deleteMode);
    }

    public void SetDeleteMode(bool active)
    {
        deleteMode = active;
        ApplyDeleteModeToCards();
        SetStatus(active ? "Нажмите на крестик, чтобы удалить работу." : string.Empty);
        DeleteModeChanged?.Invoke(active);
    }

    private void ApplyDeleteModeToCards()
    {
        if (contentRoot == null)
        {
            return;
        }

        for (int i = 0; i < contentRoot.childCount; i++)
        {
            ArtworkCardView card = contentRoot.GetChild(i).GetComponent<ArtworkCardView>();
            if (card != null)
            {
                card.SetDeleteMode(deleteMode);
            }
        }
    }

    private void RequestDeleteArtwork(ArtworkDto artwork)
    {
        if (artwork == null || string.IsNullOrWhiteSpace(artwork.id))
        {
            return;
        }

        if (BackendManager.instance == null)
        {
            SetStatus("BackendManager не найден.");
            return;
        }

        SetStatus("Удаляем работу...");
        BackendManager.instance.DeleteArtwork(artwork.id, HandleArtworkDeleted);
    }

    private void HandleArtworkDeleted(ApiResult<object> result)
    {
        if (result == null || !result.Success)
        {
            string message = result == null || string.IsNullOrWhiteSpace(result.ErrorMessage)
                ? "Не удалось удалить работу."
                : result.ErrorMessage;
            SetStatus(message);
            ToastNotification.Show(message);
            return;
        }

        ToastNotification.Show("Работа удалена.");
        // RefreshGallery() сбрасывает deleteMode и перерисовывает список без крестиков.
        RefreshGallery();
    }

    private void ClearCards()
    {
        if (contentRoot == null)
        {
            return;
        }

        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(contentRoot.GetChild(i).gameObject);
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
