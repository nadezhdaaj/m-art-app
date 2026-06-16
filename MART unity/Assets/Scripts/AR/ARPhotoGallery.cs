using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Profile gallery for AR photos. Builds a scrollable grid of saved photos from
/// <see cref="ARPhotoLibrary"/> inside the "AR photos gallery" panel.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Museum AR/AR Photo Gallery")]
public class ARPhotoGallery : MonoBehaviour
{
    private const string GalleryPanelName = "AR photos gallery";
    private const string ProfileScreenName = "ProfileUI";
    private const string TitleObjectName = "AR photos text";
    private const string ScrollObjectName = "ARPhotoScroll";
    private const string ViewportObjectName = "ARPhotoViewport";
    private const string ContentObjectName = "ARPhotoGrid";
    private const string PhotoCardName = "ARPhotoCard";

    [Header("Layout")]
    [SerializeField] private int columns = 2;
    [SerializeField] private Vector2 spacing = new Vector2(60f, 60f);
    [SerializeField] private RectOffset padding;
    [SerializeField] private Vector2 cellSizeOverride = Vector2.zero;
    [SerializeField] private float topInsetBelowTitle = 80f;
    [SerializeField] private float bottomInset = 180f;
    [SerializeField] private float horizontalInset = 60f;

    private ScrollRect scrollRect;
    private RectTransform viewportRect;
    private RectTransform contentRect;
    private GridLayoutGroup gridLayout;
    private RectMask2D viewportMask;
    private readonly List<Texture2D> loadedTextures = new List<Texture2D>();

    public static void ShowGalleryScreen()
    {
        GameObject galleryPanel = FindSceneObject(GalleryPanelName);
        if (galleryPanel != null)
        {
            galleryPanel.SetActive(true);
        }

        GameObject profileScreen = GameObject.Find(ProfileScreenName);
        if (profileScreen != null)
        {
            profileScreen.SetActive(false);
        }
    }

    private void Awake()
    {
        if (padding == null || (padding.left == 0 && padding.right == 0 && padding.top == 0 && padding.bottom == 0))
        {
            padding = new RectOffset(40, 40, 40, 40);
        }
    }

    private void OnEnable()
    {
        ARPhotoLibrary.LibraryChanged -= RefreshGallery;
        ARPhotoLibrary.LibraryChanged += RefreshGallery;
        RefreshGallery();
    }

    private void OnDisable()
    {
        ARPhotoLibrary.LibraryChanged -= RefreshGallery;
    }

    public void RefreshGallery()
    {
        EnsureScrollView();
        ClearGridChildren();

        List<string> photoPaths = ARPhotoLibrary.GetSavedPhotoPaths();
        for (int i = 0; i < photoPaths.Count; i++)
        {
            string path = photoPaths[i];
            Texture2D texture = ARPhotoLibrary.LoadPhoto(path);
            if (texture == null)
            {
                continue;
            }

            loadedTextures.Add(texture);
            CreatePhotoCard(texture, path);
        }
    }

    private void EnsureScrollView()
    {
        if (scrollRect != null && contentRect != null && gridLayout != null && viewportRect != null)
        {
            ApplyScrollLayout();
            ApplyGridSettings();
            return;
        }

        Transform scrollTransform = transform.Find(ScrollObjectName);
        GameObject scrollObject;
        if (scrollTransform != null)
        {
            scrollObject = scrollTransform.gameObject;
        }
        else
        {
            scrollObject = new GameObject(
                ScrollObjectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(ScrollRect));

            scrollObject.transform.SetParent(transform, false);

            Image background = scrollObject.GetComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0f);
            background.raycastTarget = false;
        }

        scrollRect = scrollObject.GetComponent<ScrollRect>();
        if (scrollRect == null)
        {
            scrollRect = scrollObject.AddComponent<ScrollRect>();
        }

        Transform viewportTransform = scrollObject.transform.Find(ViewportObjectName);
        GameObject viewportObject;
        if (viewportTransform != null)
        {
            viewportObject = viewportTransform.gameObject;
        }
        else
        {
            viewportObject = new GameObject(
                ViewportObjectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(RectMask2D));

            viewportObject.transform.SetParent(scrollObject.transform, false);

            Image viewportBackground = viewportObject.GetComponent<Image>();
            viewportBackground.color = new Color(0f, 0f, 0f, 0f);
            viewportBackground.raycastTarget = true;
        }

        viewportRect = viewportObject.GetComponent<RectTransform>();
        viewportMask = viewportObject.GetComponent<RectMask2D>();
        if (viewportMask == null)
        {
            viewportMask = viewportObject.AddComponent<RectMask2D>();
        }

        Transform contentTransform = viewportObject.transform.Find(ContentObjectName);
        GameObject contentObject;
        if (contentTransform != null)
        {
            contentObject = contentTransform.gameObject;
        }
        else
        {
            contentObject = new GameObject(
                ContentObjectName,
                typeof(RectTransform),
                typeof(GridLayoutGroup),
                typeof(ContentSizeFitter));

            contentObject.transform.SetParent(viewportObject.transform, false);
        }

        contentRect = contentObject.GetComponent<RectTransform>();
        gridLayout = contentObject.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            gridLayout = contentObject.AddComponent<GridLayoutGroup>();
        }

        ContentSizeFitter sizeFitter = contentObject.GetComponent<ContentSizeFitter>();
        if (sizeFitter == null)
        {
            sizeFitter = contentObject.AddComponent<ContentSizeFitter>();
        }

        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 60f;

        ApplyScrollLayout();
        ApplyGridSettings();
    }

    private void ApplyScrollLayout()
    {
        if (scrollRect == null || viewportRect == null || contentRect == null)
        {
            return;
        }

        RectTransform panelRect = transform as RectTransform;
        if (panelRect == null)
        {
            return;
        }

        float topInset = CalculateTopInset(panelRect);
        float bottom = Mathf.Max(0f, bottomInset);
        float side = Mathf.Max(0f, horizontalInset);

        RectTransform scrollRectTransform = scrollRect.transform as RectTransform;
        scrollRectTransform.anchorMin = new Vector2(0f, 0f);
        scrollRectTransform.anchorMax = new Vector2(1f, 1f);
        scrollRectTransform.pivot = new Vector2(0.5f, 0.5f);
        scrollRectTransform.offsetMin = new Vector2(side, bottom);
        scrollRectTransform.offsetMax = new Vector2(-side, -topInset);
        scrollRectTransform.localScale = Vector3.one;

        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.pivot = new Vector2(0.5f, 1f);
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewportRect.localScale = Vector3.one;

        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, contentRect.sizeDelta.y);
        contentRect.localScale = Vector3.one;
    }

    private float CalculateTopInset(RectTransform panelRect)
    {
        Transform titleTransform = transform.Find(TitleObjectName);
        RectTransform titleRect = titleTransform as RectTransform;
        if (titleRect == null)
        {
            return Mathf.Max(0f, topInsetBelowTitle);
        }

        float panelHeight = panelRect.rect.height;
        if (panelHeight <= 0f)
        {
            return Mathf.Max(0f, topInsetBelowTitle);
        }

        float titlePivotY = (1f - titleRect.pivot.y) * titleRect.rect.height;
        float titleTopFromCenter = titleRect.anchoredPosition.y + titlePivotY;
        float titleBottomFromCenter = titleTopFromCenter - titleRect.rect.height;
        float topFromTopEdge = panelHeight * 0.5f - titleBottomFromCenter;
        return Mathf.Max(0f, topFromTopEdge + topInsetBelowTitle);
    }

    private void ApplyGridSettings()
    {
        if (gridLayout == null || contentRect == null)
        {
            return;
        }

        gridLayout.padding = padding;
        gridLayout.spacing = spacing;
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayout.childAlignment = TextAnchor.UpperCenter;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = Mathf.Max(1, columns);

        Vector2 cell = cellSizeOverride;
        if (cell.x <= 0f || cell.y <= 0f)
        {
            float contentWidth = contentRect.rect.width;
            if (contentWidth <= 0f && viewportRect != null)
            {
                contentWidth = viewportRect.rect.width;
            }

            float horizontalPadding = padding.left + padding.right;
            float horizontalSpacing = spacing.x * (Mathf.Max(1, columns) - 1);
            float available = contentWidth - horizontalPadding - horizontalSpacing;
            float side = Mathf.Max(120f, available / Mathf.Max(1, columns));
            cell = new Vector2(side, side);
        }

        gridLayout.cellSize = cell;
    }

    private void ClearGridChildren()
    {
        DestroyLoadedTextures();

        if (contentRect == null)
        {
            return;
        }

        for (int i = contentRect.childCount - 1; i >= 0; i--)
        {
            Destroy(contentRect.GetChild(i).gameObject);
        }
    }

    private void DestroyLoadedTextures()
    {
        for (int i = 0; i < loadedTextures.Count; i++)
        {
            if (loadedTextures[i] != null)
            {
                Destroy(loadedTextures[i]);
            }
        }

        loadedTextures.Clear();
    }

    private void CreatePhotoCard(Texture2D texture, string path)
    {
        var cardObject = new GameObject(
            PhotoCardName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(RawImage),
            typeof(Button));
        cardObject.transform.SetParent(contentRect, false);
        cardObject.transform.localScale = Vector3.one;

        RawImage raw = cardObject.GetComponent<RawImage>();
        raw.texture = texture;
        raw.raycastTarget = true;
        raw.uvRect = ComputeCenterCropUvRect(texture);

        Button cardButton = cardObject.GetComponent<Button>();
        cardButton.transition = Selectable.Transition.None;
        cardButton.targetGraphic = raw;

        string capturedPath = path;
        Texture2D capturedTexture = texture;
        cardButton.onClick.AddListener(() => ARPhotoFullscreenViewer.Show(capturedTexture, capturedPath));
    }

    private static GameObject FindSceneObject(string objectName)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return null;
        }

        GameObject[] roots = scene.GetRootGameObjects();
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
        if (parent == null)
        {
            return null;
        }

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

    private Rect ComputeCenterCropUvRect(Texture2D texture)
    {
        Rect fullRect = new Rect(0f, 0f, 1f, 1f);
        if (texture == null || texture.width <= 0 || texture.height <= 0)
        {
            return fullRect;
        }

        Vector2 cell = gridLayout != null ? gridLayout.cellSize : Vector2.zero;
        if (cell.x <= 0f || cell.y <= 0f)
        {
            return fullRect;
        }

        float textureAspect = (float)texture.width / texture.height;
        float cellAspect = cell.x / cell.y;

        if (Mathf.Approximately(textureAspect, cellAspect))
        {
            return fullRect;
        }

        if (textureAspect > cellAspect)
        {
            float visibleWidthFraction = cellAspect / textureAspect;
            float xOffset = (1f - visibleWidthFraction) * 0.5f;
            return new Rect(xOffset, 0f, visibleWidthFraction, 1f);
        }

        float visibleHeightFraction = textureAspect / cellAspect;
        float yOffset = (1f - visibleHeightFraction) * 0.5f;
        return new Rect(0f, yOffset, 1f, visibleHeightFraction);
    }

    private void OnDestroy()
    {
        ARPhotoLibrary.LibraryChanged -= RefreshGallery;
        DestroyLoadedTextures();
    }
}
