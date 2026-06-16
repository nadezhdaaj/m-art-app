using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bottom horizontal exhibit picker for AR photo mode.
/// </summary>
public class ARPhotoExhibitCarousel : MonoBehaviour
{
    private const float CarouselHeight = 168f;
    private const float CardWidth = 112f;
    private const float CardHeight = 112f;
    private const float CardSpacing = 16f;

    private readonly List<ARPhotoExhibitCarouselCard> cards = new List<ARPhotoExhibitCarouselCard>();
    private readonly List<string> exhibitIds = new List<string>();

    private RectTransform contentRoot;
    private ARPhotoExhibitPlacer placer;
    private string selectedExhibitId;

    public void Initialize(Transform canvasRoot, ARPhotoExhibitPlacer exhibitPlacer)
    {
        placer = exhibitPlacer;
        exhibitIds.Clear();
        exhibitIds.AddRange(ExhibitCatalog.GetExhibitIdsForPhotoMode());

        BuildUi(canvasRoot);
        PopulateCards();

        if (exhibitIds.Count > 0)
        {
            SelectExhibit(exhibitIds[0]);
        }
    }

    private void BuildUi(Transform canvasRoot)
    {
        var panelObject = new GameObject(
            "ARPhotoExhibitCarousel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        panelObject.transform.SetParent(canvasRoot, false);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.sizeDelta = new Vector2(0f, CarouselHeight);
        panelRect.anchoredPosition = new Vector2(0f, 24f);

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.45f);
        panelImage.raycastTarget = true;

        var titleObject = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObject.transform.SetParent(panelObject.transform, false);
        RectTransform titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(0f, 28f);
        titleRect.anchoredPosition = new Vector2(0f, -6f);

        TextMeshProUGUI title = titleObject.GetComponent<TextMeshProUGUI>();
        title.text = "Экспонаты";
        title.fontSize = 22f;
        title.alignment = TextAlignmentOptions.Center;
        title.color = Color.white;

        var scrollObject = new GameObject(
            "Scroll",
            typeof(RectTransform),
            typeof(ScrollRect),
            typeof(Image));
        scrollObject.transform.SetParent(panelObject.transform, false);

        RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = Vector2.zero;
        scrollRectTransform.anchorMax = Vector2.one;
        scrollRectTransform.offsetMin = new Vector2(12f, 12f);
        scrollRectTransform.offsetMax = new Vector2(-12f, -36f);

        Image scrollBackground = scrollObject.GetComponent<Image>();
        scrollBackground.color = new Color(1f, 1f, 1f, 0.04f);
        scrollBackground.raycastTarget = true;

        var viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewportObject.transform.SetParent(scrollObject.transform, false);
        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        StretchFull(viewportRect);

        Image viewportImage = viewportObject.GetComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
        viewportImage.raycastTarget = true;
        viewportObject.GetComponent<Mask>().showMaskGraphic = false;

        var contentObject = new GameObject(
            "Content",
            typeof(RectTransform),
            typeof(HorizontalLayoutGroup),
            typeof(ContentSizeFitter));
        contentObject.transform.SetParent(viewportObject.transform, false);
        contentRoot = contentObject.GetComponent<RectTransform>();
        contentRoot.anchorMin = new Vector2(0f, 0.5f);
        contentRoot.anchorMax = new Vector2(0f, 0.5f);
        contentRoot.pivot = new Vector2(0f, 0.5f);
        contentRoot.anchoredPosition = Vector2.zero;
        contentRoot.sizeDelta = new Vector2(0f, CardHeight);

        HorizontalLayoutGroup layout = contentObject.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = CardSpacing;
        layout.padding = new RectOffset(8, 8, 0, 0);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        ScrollRect scrollRect = scrollObject.GetComponent<ScrollRect>();
        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRoot;
        scrollRect.horizontal = true;
        scrollRect.vertical = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
    }

    private void PopulateCards()
    {
        cards.Clear();

        for (int i = 0; i < exhibitIds.Count; i++)
        {
            string exhibitId = exhibitIds[i];
            ARPhotoExhibitCarouselCard card = CreateCard();
            card.transform.SetParent(contentRoot, false);

            Sprite preview = ExhibitCatalog.GetPreviewSprite(exhibitId);
            Color fallbackColor = ExhibitCatalog.GetPhotoModeFallbackColor(i);
            card.Bind(exhibitId, preview, fallbackColor, false, SelectExhibit);
            cards.Add(card);
        }
    }

    private void SelectExhibit(string exhibitId)
    {
        selectedExhibitId = exhibitId;
        int index = ExhibitCatalog.GetExhibitIndex(exhibitId, exhibitIds);

        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].SetSelected(cards[i].ExhibitId == exhibitId);
        }

        placer?.PlaceExhibit(exhibitId, index);
    }

    private ARPhotoExhibitCarouselCard CreateCard()
    {
        GameObject cardObject = new GameObject(
            "ExhibitCard",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button),
            typeof(ARPhotoExhibitCarouselCard));

        RectTransform cardRect = cardObject.GetComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(CardWidth, CardHeight);

        Image background = cardObject.GetComponent<Image>();
        background.color = new Color(1f, 1f, 1f, 0.12f);
        background.raycastTarget = true;

        var previewObject = new GameObject("Preview", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        previewObject.transform.SetParent(cardObject.transform, false);
        RectTransform previewRect = previewObject.GetComponent<RectTransform>();
        previewRect.anchorMin = Vector2.zero;
        previewRect.anchorMax = Vector2.one;
        previewRect.offsetMin = new Vector2(10f, 10f);
        previewRect.offsetMax = new Vector2(-10f, -10f);
        Image previewImage = previewObject.GetComponent<Image>();
        previewImage.raycastTarget = false;

        var outlineObject = new GameObject("Selection", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        outlineObject.transform.SetParent(cardObject.transform, false);
        RectTransform outlineRect = outlineObject.GetComponent<RectTransform>();
        StretchFull(outlineRect);
        outlineRect.offsetMin = new Vector2(-4f, -4f);
        outlineRect.offsetMax = new Vector2(4f, 4f);
        Image outlineImage = outlineObject.GetComponent<Image>();
        outlineImage.color = new Color(1f, 1f, 1f, 0.85f);
        outlineImage.raycastTarget = false;
        outlineImage.type = Image.Type.Sliced;
        outlineImage.enabled = false;

        ARPhotoExhibitCarouselCard card = cardObject.GetComponent<ARPhotoExhibitCarouselCard>();
        return card;
    }

    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
