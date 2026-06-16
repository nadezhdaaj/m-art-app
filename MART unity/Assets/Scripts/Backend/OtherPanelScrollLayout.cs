using UnityEngine;

/// <summary>
/// Stacks carousel, Gallery and notes vertically inside OtherPanelScrollContent.
/// </summary>
public static class OtherPanelScrollLayout
{
    public const string ViewportName = "ArtworksScrollViewport";
    public const string ArtworksListName = "ArtworksList";
    public const string GallerySectionName = "Gallery";
    public const string GalleryLabelName = "Gallery";
    public const string GalleryArPanelName = "gallery for AR photos";
    public const string GalleryButtonName = "gallery Button";
    public const string UserNotesSectionName = "user notes";

    private const float HorizontalInset = 16f;
    private const float SectionGap = 6f;
    private const float ViewportBottomPadding = 4f;
    private const float MinGalleryHeight = 220f;
    private const float MinNotesHeight = 420f;
    private const float ContentBottomPadding = 16f;
    private const float NotesSectionHeight = 592f;
    private const float GalleryLabelGap = 12f;
    private const float GalleryButtonCornerInset = 6f;
    private const float GalleryButtonLocalX = 241f;
    private const float GalleryButtonSize = 131f;
    private const float GalleryButtonCenterFromPanelBottom = 165f;

    private static readonly string[] HiddenDecorativeChildNames =
    {
        "Design elements",
        "carousel ",
        "carousel",
    };

    public static void Apply(RectTransform content, float carouselHeight)
    {
        if (content == null)
        {
            return;
        }

        HideDecorativeChildren(content);
        Canvas.ForceUpdateCanvases();

        RectTransform viewport = FindDirectChild(content, ViewportName);
        if (viewport != null)
        {
            float viewportHeight = Mathf.Max(280f, carouselHeight + ViewportBottomPadding);
            PlaceTopSection(viewport, 0f, viewportHeight, stretchWidth: true);
        }

        RectTransform gallery = FindDirectChild(content, GallerySectionName);
        if (gallery != null)
        {
            LayoutGallerySection(gallery);
            Canvas.ForceUpdateCanvases();

            if (viewport != null)
            {
                float galleryHeight = Mathf.Max(MinGalleryHeight, MeasureGallerySectionHeight(gallery));
                float galleryTop = GetCarouselBottom(content, viewport) - SectionGap;
                PlaceTopSection(gallery, galleryTop, galleryHeight, stretchWidth: true);
            }
        }

        RectTransform userNotes = FindDirectChild(content, UserNotesSectionName);
        if (userNotes != null)
        {
            RectTransform anchor = gallery != null ? gallery : viewport;
            if (anchor != null)
            {
                PlaceNotesSectionBelow(content, userNotes, anchor);
            }
        }

        FitContentHeight(content);
        Canvas.ForceUpdateCanvases();
    }

    private static void HideDecorativeChildren(RectTransform content)
    {
        for (int i = 0; i < content.childCount; i++)
        {
            Transform child = content.GetChild(i);
            if (child == null || !IsHiddenDecorative(child.name))
            {
                continue;
            }

            child.gameObject.SetActive(false);
        }
    }

    private static void LayoutGallerySection(RectTransform gallery)
    {
        RectTransform label = FindGalleryLabel(gallery);
        RectTransform arPanel = gallery.Find(GalleryArPanelName) as RectTransform;
        RectTransform button = FindGalleryButton(gallery);

        float cursorY = 0f;
        if (label != null)
        {
            PlaceGalleryTopAnchored(label, cursorY);
            cursorY -= GetElementHeight(label) + GalleryLabelGap;
        }

        if (arPanel != null)
        {
            PlaceGalleryTopAnchored(arPanel, cursorY);

            if (button != null)
            {
                PlaceGalleryButton(gallery, arPanel, button);
            }
        }
    }

    private static RectTransform FindGalleryLabel(RectTransform gallery)
    {
        for (int i = 0; i < gallery.childCount; i++)
        {
            Transform child = gallery.GetChild(i);
            if (child != null && child.name == GalleryLabelName && child != gallery)
            {
                return child as RectTransform;
            }
        }

        return null;
    }

    private static RectTransform FindGalleryButton(RectTransform gallery)
    {
        Transform button = gallery.Find(GalleryButtonName);
        if (button != null)
        {
            return button as RectTransform;
        }

        Transform arPanel = gallery.Find(GalleryArPanelName);
        if (arPanel != null)
        {
            button = arPanel.Find(GalleryButtonName);
            if (button != null)
            {
                return button as RectTransform;
            }
        }

        return null;
    }

    private static void PlaceGalleryTopAnchored(RectTransform item, float topY)
    {
        item.anchorMin = new Vector2(0.5f, 1f);
        item.anchorMax = new Vector2(0.5f, 1f);
        item.pivot = new Vector2(0.5f, 1f);
        item.anchoredPosition = new Vector2(item.anchoredPosition.x, topY);
    }

    private static void PlaceGalleryButton(
        RectTransform gallery,
        RectTransform arPanel,
        RectTransform button)
    {
        if (button.parent != gallery)
        {
            button.SetParent(gallery, false);
        }

        button.SetAsLastSibling();
        button.anchorMin = new Vector2(0.5f, 1f);
        button.anchorMax = new Vector2(0.5f, 1f);
        button.pivot = new Vector2(0.5f, 0.5f);
        button.sizeDelta = new Vector2(GalleryButtonSize, GalleryButtonSize);

        float arBottom = arPanel.anchoredPosition.y - GetElementHeight(arPanel);
        float buttonCenterY = arBottom + GalleryButtonCenterFromPanelBottom;
        button.anchoredPosition = new Vector2(GalleryButtonLocalX, buttonCenterY);
    }

    private static float MeasureGallerySectionHeight(RectTransform gallery)
    {
        float height = 16f;

        RectTransform label = FindGalleryLabel(gallery);
        if (label != null)
        {
            height += GetElementHeight(label) + GalleryLabelGap;
        }

        RectTransform arPanel = gallery.Find(GalleryArPanelName) as RectTransform;
        if (arPanel != null)
        {
            height += GetElementHeight(arPanel);
        }

        RectTransform button = FindGalleryButton(gallery);
        if (button != null && arPanel != null)
        {
            float arBottom = arPanel.anchoredPosition.y - GetElementHeight(arPanel);
            float buttonBottom = button.anchoredPosition.y - GalleryButtonSize * 0.5f;
            height = Mathf.Max(height, -buttonBottom + GalleryButtonCornerInset);
        }

        return height;
    }

    private static float GetElementHeight(RectTransform item)
    {
        return Mathf.Max(item.rect.height, item.sizeDelta.y);
    }

    private static void PlaceTopSection(RectTransform section, float topY, float height, bool stretchWidth)
    {
        if (stretchWidth)
        {
            section.anchorMin = new Vector2(0f, 1f);
            section.anchorMax = new Vector2(1f, 1f);
            section.pivot = new Vector2(0.5f, 1f);
            section.anchoredPosition = new Vector2(0f, topY);
            section.sizeDelta = new Vector2(-HorizontalInset * 2f, height);
            return;
        }

        section.anchorMin = new Vector2(0.5f, 1f);
        section.anchorMax = new Vector2(0.5f, 1f);
        section.pivot = new Vector2(0.5f, 1f);
        section.anchoredPosition = new Vector2(0f, topY);
        section.sizeDelta = new Vector2(Mathf.Max(section.sizeDelta.x, 720f), height);
    }

    private static float GetCarouselBottom(RectTransform content, RectTransform viewport)
    {
        RectTransform list = viewport.Find(ArtworksListName) as RectTransform;
        if (list != null)
        {
            Bounds listBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(content, list);
            return listBounds.min.y;
        }

        Bounds viewportBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(content, viewport);
        return viewportBounds.min.y;
    }

    private static void PlaceBelowSibling(
        RectTransform content,
        RectTransform section,
        RectTransform sectionAbove,
        float height,
        bool stretchWidth)
    {
        Bounds aboveBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(content, sectionAbove);
        float topY = aboveBounds.min.y - SectionGap;
        PlaceTopSection(section, topY, height, stretchWidth);
    }

    private static void FitContentHeight(RectTransform content)
    {
        float bottomY = 0f;
        bool hasSection = false;

        for (int i = 0; i < content.childCount; i++)
        {
            RectTransform child = content.GetChild(i) as RectTransform;
            if (child == null || !child.gameObject.activeInHierarchy || IsHiddenDecorative(child.name))
            {
                continue;
            }

            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(content, child);
            bottomY = hasSection ? Mathf.Min(bottomY, bounds.min.y) : bounds.min.y;
            hasSection = true;
        }

        if (!hasSection)
        {
            return;
        }

        content.sizeDelta = new Vector2(content.sizeDelta.x, -bottomY + ContentBottomPadding);
    }

    private static void PlaceNotesSectionBelow(
        RectTransform content,
        RectTransform userNotes,
        RectTransform sectionAbove)
    {
        Bounds aboveBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(content, sectionAbove);
        float sectionTop = aboveBounds.min.y - SectionGap;
        float notesHeight = Mathf.Max(MinNotesHeight, NotesSectionHeight);

        userNotes.anchorMin = new Vector2(0f, 1f);
        userNotes.anchorMax = new Vector2(1f, 1f);
        userNotes.pivot = new Vector2(0.5f, 0.5f);
        userNotes.anchoredPosition = new Vector2(0f, sectionTop - notesHeight * 0.5f);
        userNotes.sizeDelta = new Vector2(-HorizontalInset * 2f, notesHeight);
    }

    private static bool IsHiddenDecorative(string childName)
    {
        for (int i = 0; i < HiddenDecorativeChildNames.Length; i++)
        {
            if (childName == HiddenDecorativeChildNames[i])
            {
                return true;
            }
        }

        return false;
    }

    private static RectTransform FindDirectChild(RectTransform parent, string childName)
    {
        Transform child = parent.Find(childName);
        return child as RectTransform;
    }
}
