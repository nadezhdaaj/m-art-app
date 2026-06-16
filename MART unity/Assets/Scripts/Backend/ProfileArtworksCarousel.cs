using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Horizontal left-to-right artwork carousel on OtherPanel (uses ArtworksScrollViewport + ArtworksList).
/// </summary>
[DisallowMultipleComponent]
[ExecuteAlways]
public class ProfileArtworksCarousel : MonoBehaviour
{
    private const string EditorPreviewCardPrefix = "EditorCarouselPreview_";
    public const string SectionObjectName = "ProfileArtworksCarouselSection";

    private const string ViewportName = "ArtworksScrollViewport";
    private const string ListName = "ArtworksList";
    private const string VerticalHandleName = "scroll";

    [Header("Layout")]
    [SerializeField] private float cardWidth = 280f;
    [SerializeField] private float cardHeight = 380f;
    [SerializeField] private float cardSpacing = 24f;
    [SerializeField] private int maxItems = 20;
    [SerializeField] private int previewCount = 6;

    [Header("Navigation")]
    [SerializeField] private string paintSceneName = "The main stage";

    public float CardHeight => cardHeight;

    private RectTransform viewport;
    private RectTransform contentRoot;
    private ScrollRect scrollRect;
    private HorizontalLayoutGroup horizontalLayout;
    private ArtworkCarouselCardView cardPrefab;
    private bool isConfigured;
    private bool pendingLayoutRebuild;

    private void Awake()
    {
        if (Application.isPlaying)
        {
            DisableConflictingGalleryOnOtherPanel();
        }

        ConfigureHorizontalCarousel();
    }

    private void OnEnable()
    {
        ConfigureHorizontalCarousel();
        ProfileUiArtworksBootstrap.EnsureSeeAllButton();

        if (!Application.isPlaying)
        {
            RefreshEditorPreview();
            return;
        }

        if (pendingLayoutRebuild)
        {
            pendingLayoutRebuild = false;
            StartCoroutine(RebuildLayoutNextFrame());
        }

        RefreshCarousel();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        // OnValidate is also invoked during scene serialization (e.g. when building the
        // player). Calling DestroyImmediate/Instantiate directly here is forbidden by Unity,
        // so defer the editor preview rebuild until we are out of the restricted callback.
        UnityEditor.EditorApplication.delayCall -= DeferredEditorRefresh;
        UnityEditor.EditorApplication.delayCall += DeferredEditorRefresh;
    }

    private void DeferredEditorRefresh()
    {
        UnityEditor.EditorApplication.delayCall -= DeferredEditorRefresh;

        // The component may have been destroyed or entered play mode before the deferred call.
        if (this == null || Application.isPlaying)
        {
            return;
        }

        ConfigureHorizontalCarousel();
        RefreshEditorPreview();
    }
#endif

    public void RefreshCarousel()
    {
        if (!Application.isPlaying)
        {
            RefreshEditorPreview();
            return;
        }

        if (!isConfigured || contentRoot == null)
        {
            ConfigureHorizontalCarousel();
        }

        if (contentRoot == null || BackendManager.instance == null)
        {
            ClearCards();
            return;
        }

        ClearCards();
        BackendManager.instance.LoadMyArtworks(HandleArtworksLoaded);
    }

    private void RefreshEditorPreview()
    {
        ConfigureHorizontalCarousel();
        if (contentRoot == null)
        {
            return;
        }

        ClearCards();
        ResolveCardPrefab();
        if (cardPrefab == null)
        {
            OtherPanelScrollbarController.RefreshLayout();
            return;
        }

        int count = Mathf.Clamp(previewCount, 1, 6);
        for (int i = 0; i < count; i++)
        {
            ArtworkCarouselCardView card = CreateCard();
            if (card == null)
            {
                continue;
            }

            card.gameObject.name = EditorPreviewCardPrefix + i;
        }

        if (contentRoot != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
        }

        OtherPanelScrollbarController.RefreshLayout();
    }

    private static void DisableConflictingGalleryOnOtherPanel()
    {
        GameObject otherPanel = GameObject.Find("OtherPanel");
        if (otherPanel == null)
        {
            return;
        }

        ProfileArtworksGallery gallery = otherPanel.GetComponent<ProfileArtworksGallery>();
        if (gallery != null)
        {
            gallery.enabled = false;
            Destroy(gallery);
        }
    }

    private void ConfigureHorizontalCarousel()
    {
        ResolveReferences();
        if (viewport == null || contentRoot == null)
        {
            return;
        }

        ConfigureFixedTopViewport();
        viewport.gameObject.SetActive(true);

        if (viewport.parent != null && viewport.GetSiblingIndex() != 1)
        {
            viewport.SetSiblingIndex(Mathf.Min(1, viewport.parent.childCount - 1));
        }

        HideVerticalScrollHandle();
        DisableVerticalScrollComponents();
        SetupHorizontalLayout();
        SetupHorizontalScrollRect();
        ApplyScrollContentLayout();
        isConfigured = true;
        OtherPanelScrollbarController.RefreshLayout();
    }

    private void ConfigureFixedTopViewport()
    {
        viewport.anchorMin = new Vector2(0f, 1f);
        viewport.anchorMax = new Vector2(1f, 1f);
        viewport.pivot = new Vector2(0.5f, 1f);
        viewport.anchoredPosition = Vector2.zero;
        viewport.sizeDelta = new Vector2(-32f, cardHeight + 4f);
    }

    private void ApplyScrollContentLayout()
    {
        RectTransform content = viewport != null ? viewport.parent as RectTransform : null;
        if (content == null)
        {
            return;
        }

        OtherPanelScrollLayout.Apply(content, cardHeight);
    }

    private void ResolveReferences()
    {
        if (viewport == null)
        {
            viewport = FindChildRect(ViewportName);
        }

        if (contentRoot == null && viewport != null)
        {
            contentRoot = viewport.Find(ListName) as RectTransform;
        }
    }

    private RectTransform FindChildRect(string childName)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child != null && child.name == childName)
            {
                return child as RectTransform;
            }
        }

        return null;
    }

    private void HideVerticalScrollHandle()
    {
        Transform[] handles = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < handles.Length; i++)
        {
            Transform handle = handles[i];
            if (handle == transform || handle == viewport || handle == contentRoot)
            {
                continue;
            }

            if (handle.name == VerticalHandleName && handle.parent == transform)
            {
                handle.gameObject.SetActive(false);
            }
        }
    }

    private void DisableVerticalScrollComponents()
    {
        GalleryVerticalScrollHandle handleDriver = GetComponentInChildren<GalleryVerticalScrollHandle>(true);
        if (handleDriver != null)
        {
            Destroy(handleDriver);
        }

        GridLayoutGroup grid = contentRoot.GetComponent<GridLayoutGroup>();
        if (grid != null)
        {
            Destroy(grid);
        }

        VerticalLayoutGroup vertical = contentRoot.GetComponent<VerticalLayoutGroup>();
        if (vertical != null)
        {
            Destroy(vertical);
        }
    }

    private void SetupHorizontalLayout()
    {
        contentRoot.anchorMin = new Vector2(0f, 1f);
        contentRoot.anchorMax = new Vector2(0f, 1f);
        contentRoot.pivot = new Vector2(0f, 1f);
        contentRoot.anchoredPosition = Vector2.zero;
        contentRoot.sizeDelta = new Vector2(0f, cardHeight);

        horizontalLayout = contentRoot.GetComponent<HorizontalLayoutGroup>();
        if (horizontalLayout == null)
        {
            horizontalLayout = contentRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        }

        horizontalLayout.spacing = cardSpacing;
        horizontalLayout.padding = new RectOffset(12, 12, 0, 0);
        horizontalLayout.childAlignment = TextAnchor.UpperLeft;
        horizontalLayout.childControlWidth = false;
        horizontalLayout.childControlHeight = false;
        horizontalLayout.childForceExpandWidth = false;
        horizontalLayout.childForceExpandHeight = false;

        ContentSizeFitter fitter = contentRoot.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = contentRoot.gameObject.AddComponent<ContentSizeFitter>();
        }

        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
    }

    private void SetupHorizontalScrollRect()
    {
        scrollRect = viewport.GetComponent<ScrollRect>();
        if (scrollRect == null)
        {
            scrollRect = viewport.gameObject.AddComponent<ScrollRect>();
        }

        scrollRect.content = contentRoot;
        scrollRect.viewport = viewport;
        scrollRect.horizontal = true;
        scrollRect.vertical = false;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.inertia = true;
        scrollRect.scrollSensitivity = 35f;
        scrollRect.enabled = true;
    }

    private void HandleArtworksLoaded(ApiResult<ArtworkArrayWrapperDto> result)
    {
        ClearCards();

        if (result == null || !result.Success)
        {
            return;
        }

        ArtworkDto[] artworks = SortByNewest(result.Data?.items);
        if (artworks == null || artworks.Length == 0)
        {
            return;
        }

        int count = Mathf.Min(artworks.Length, Mathf.Min(maxItems, previewCount));
        for (int i = 0; i < count; i++)
        {
            ArtworkCarouselCardView card = CreateCard();
            if (card == null)
            {
                continue;
            }

            card.Bind(artworks[i], OpenArtworkForEditing);
        }

        ScheduleLayoutRebuild();
    }

    private static ArtworkDto[] SortByNewest(ArtworkDto[] items)
    {
        if (items == null || items.Length <= 1)
        {
            return items;
        }

        Array.Sort(items, CompareByNewest);
        return items;
    }

    private static int CompareByNewest(ArtworkDto left, ArtworkDto right)
    {
        DateTime leftDate = ParseArtworkDate(left);
        DateTime rightDate = ParseArtworkDate(right);
        int byDate = rightDate.CompareTo(leftDate);
        if (byDate != 0)
        {
            return byDate;
        }

        return string.Compare(right?.id, left?.id, StringComparison.Ordinal);
    }

    private static DateTime ParseArtworkDate(ArtworkDto artwork)
    {
        if (artwork == null)
        {
            return DateTime.MinValue;
        }

        if (DateTime.TryParse(artwork.createdAt, out DateTime created))
        {
            return created;
        }

        if (DateTime.TryParse(artwork.updatedAt, out DateTime updated))
        {
            return updated;
        }

        return DateTime.MinValue;
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

        if (contentRoot != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
        }

        if (scrollRect != null)
        {
            scrollRect.horizontalNormalizedPosition = 0f;
        }

        OtherPanelScrollbarController.RefreshLayout();
    }

    private ArtworkCarouselCardView CreateCard()
    {
        ResolveCardPrefab();
        if (cardPrefab == null || contentRoot == null)
        {
            return null;
        }

        ArtworkCarouselCardView card = Instantiate(cardPrefab, contentRoot);
        card.gameObject.SetActive(true);

        RectTransform cardRect = card.transform as RectTransform;
        if (cardRect != null)
        {
            cardRect.localScale = Vector3.one;
            cardRect.sizeDelta = new Vector2(cardWidth, cardHeight);
        }

        LayoutElement layoutElement = card.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = card.gameObject.AddComponent<LayoutElement>();
        }

        layoutElement.preferredWidth = cardWidth;
        layoutElement.preferredHeight = cardHeight;
        layoutElement.minWidth = cardWidth;
        layoutElement.minHeight = cardHeight;

        return card;
    }

    private void ResolveCardPrefab()
    {
        if (cardPrefab != null)
        {
            return;
        }

        cardPrefab = GetComponentInChildren<ArtworkCarouselCardView>(true);
        if (cardPrefab != null)
        {
            cardPrefab.gameObject.SetActive(false);
            return;
        }

        cardPrefab = BuildRuntimeCardTemplate();
    }

    private ArtworkCarouselCardView BuildRuntimeCardTemplate()
    {
        GameObject template = new GameObject("CarouselCardTemplate", typeof(RectTransform));
        template.transform.SetParent(transform, false);
        template.SetActive(false);

        RectTransform rect = template.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(cardWidth, cardHeight);

        Image background = template.AddComponent<Image>();
        background.color = new Color(0.45f, 0.45f, 0.48f, 1f);
        background.raycastTarget = true;

        GameObject previewObject = new GameObject("Preview", typeof(RectTransform));
        previewObject.transform.SetParent(rect, false);
        RectTransform previewRect = previewObject.GetComponent<RectTransform>();
        previewRect.anchorMin = Vector2.zero;
        previewRect.anchorMax = Vector2.one;
        previewRect.offsetMin = Vector2.zero;
        previewRect.offsetMax = Vector2.zero;
        RawImage preview = previewObject.AddComponent<RawImage>();
        preview.color = new Color(0.55f, 0.55f, 0.58f, 1f);
        preview.raycastTarget = false;

        GameObject overlayObject = new GameObject("TextOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        overlayObject.transform.SetParent(rect, false);
        RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = new Vector2(0f, 0f);
        overlayRect.offsetMax = new Vector2(0f, 0f);
        Image overlay = overlayObject.GetComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.35f);
        overlay.raycastTarget = false;

        GameObject titleObject = new GameObject("Title", typeof(RectTransform));
        titleObject.transform.SetParent(rect, false);
        RectTransform titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0f);
        titleRect.anchorMax = new Vector2(1f, 0f);
        titleRect.pivot = new Vector2(0.5f, 0f);
        titleRect.sizeDelta = new Vector2(-24f, 44f);
        titleRect.anchoredPosition = new Vector2(0f, 72f);
        TextMeshProUGUI title = titleObject.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
        {
            title.font = TMP_Settings.defaultFontAsset;
        }

        title.fontSize = 26f;
        title.fontStyle = FontStyles.Bold;
        title.color = Color.white;

        GameObject char1Object = new GameObject("Characteristic1", typeof(RectTransform));
        char1Object.transform.SetParent(rect, false);
        RectTransform char1Rect = char1Object.GetComponent<RectTransform>();
        char1Rect.anchorMin = new Vector2(0f, 0f);
        char1Rect.anchorMax = new Vector2(1f, 0f);
        char1Rect.pivot = new Vector2(0.5f, 0f);
        char1Rect.sizeDelta = new Vector2(-24f, 32f);
        char1Rect.anchoredPosition = new Vector2(0f, 36f);
        TextMeshProUGUI char1 = char1Object.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
        {
            char1.font = TMP_Settings.defaultFontAsset;
        }

        char1.fontSize = 20f;
        char1.color = Color.white;

        GameObject char2Object = new GameObject("Characteristic2", typeof(RectTransform));
        char2Object.transform.SetParent(rect, false);
        RectTransform char2Rect = char2Object.GetComponent<RectTransform>();
        char2Rect.anchorMin = new Vector2(0f, 0f);
        char2Rect.anchorMax = new Vector2(1f, 0f);
        char2Rect.pivot = new Vector2(0.5f, 0f);
        char2Rect.sizeDelta = new Vector2(-24f, 32f);
        char2Rect.anchoredPosition = new Vector2(0f, 8f);
        TextMeshProUGUI char2 = char2Object.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
        {
            char2.font = TMP_Settings.defaultFontAsset;
        }

        char2.fontSize = 20f;
        char2.color = Color.white;

        Button button = template.AddComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.None;

        return template.AddComponent<ArtworkCarouselCardView>();
    }

    private void OpenArtworkForEditing(ArtworkDto artwork)
    {
        ProfileArtworkNavigation.OpenArtworkForEditing(artwork, paintSceneName);
    }

    private void ClearCards()
    {
        if (contentRoot == null)
        {
            return;
        }

        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            GameObject child = contentRoot.GetChild(i).gameObject;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEngine.Object.DestroyImmediate(child);
                continue;
            }
#endif
            Destroy(child);
        }
    }

    private static void StretchToParent(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
