using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the "Сохранить произведение" naming panel at runtime when it is not authored in the scene.
/// Object names match the constants resolved by <see cref="PaintArtworkController"/>
/// ("for the name", "title", "Additionally", "to send") so the existing wiring keeps working.
/// </summary>
public static class ArtworkNamingPanelBuilder
{
    public const string PanelObjectName = "for the name";
    public const string TitleInputObjectName = "title";
    public const string DetailsInputObjectName = "Additionally";
    public const string SubmitButtonObjectName = "to send";
    public const string CancelButtonObjectName = "cancel naming";

    /// <summary>
    /// Creates a hidden naming panel under the given controller's canvas and wires the cancel button.
    /// The submit button is named so that <see cref="PaintArtworkController"/> attaches the
    /// <see cref="ArtworkNamingSubmitButton"/> fallback to it.
    /// </summary>
    public static GameObject Build(PaintArtworkController controller)
    {
        Transform canvas = ResolveCanvas(controller);
        if (canvas == null)
        {
            Debug.LogWarning("ArtworkNamingPanelBuilder: Canvas не найден, панель названия не создана.");
            return null;
        }

        GameObject overlay = new GameObject(
            PanelObjectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        overlay.transform.SetParent(canvas, false);
        overlay.transform.SetAsLastSibling();
        StretchFullScreen(overlay.GetComponent<RectTransform>());

        Image dim = overlay.GetComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.55f);
        dim.raycastTarget = true; // блокируем клики по холсту за панелью

        RectTransform card = CreateCard(overlay.transform);

        CreateHeader(card, "Сохранить произведение");
        CreateInputField(card, TitleInputObjectName, "Название работы", false,
            new Vector2(0.06f, 0.62f), new Vector2(0.94f, 0.78f));
        CreateInputField(card, DetailsInputObjectName, "Описание (необязательно)", true,
            new Vector2(0.06f, 0.26f), new Vector2(0.94f, 0.58f));

        CreateButton(card, CancelButtonObjectName, "Отмена",
            new Color(0.88f, 0.88f, 0.88f, 1f), new Color(0.2f, 0.2f, 0.2f, 1f),
            new Vector2(0.06f, 0.06f), new Vector2(0.47f, 0.20f),
            () => controller.CloseNamingPanel());

        GameObject submit = CreateButton(card, SubmitButtonObjectName, "Сохранить",
            new Color(0.20f, 0.55f, 0.95f, 1f), Color.white,
            new Vector2(0.53f, 0.06f), new Vector2(0.94f, 0.20f),
            null);
        if (submit.GetComponent<ArtworkNamingSubmitButton>() == null)
        {
            submit.AddComponent<ArtworkNamingSubmitButton>();
        }

        overlay.SetActive(false);
        return overlay;
    }

    private static RectTransform CreateCard(Transform parent)
    {
        GameObject cardObject = new GameObject("Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        cardObject.transform.SetParent(parent, false);

        RectTransform rect = cardObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.08f, 0.22f);
        rect.anchorMax = new Vector2(0.92f, 0.78f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image background = cardObject.GetComponent<Image>();
        background.color = Color.white;
        background.raycastTarget = true;
        return rect;
    }

    private static void CreateHeader(RectTransform card, string title)
    {
        GameObject headerObject = new GameObject("Header", typeof(RectTransform));
        headerObject.transform.SetParent(card, false);

        RectTransform rect = headerObject.GetComponent<RectTransform>();
        SetAnchors(rect, new Vector2(0.06f, 0.82f), new Vector2(0.94f, 0.95f));

        TextMeshProUGUI label = headerObject.AddComponent<TextMeshProUGUI>();
        ApplyFont(label);
        label.text = title;
        label.fontSize = 34f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.12f, 0.12f, 0.12f, 1f);
        label.raycastTarget = false;
    }

    private static void CreateInputField(
        RectTransform card,
        string objectName,
        string placeholderText,
        bool multiline,
        Vector2 anchorMin,
        Vector2 anchorMax)
    {
        GameObject root = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        root.transform.SetParent(card, false);
        SetAnchors(root.GetComponent<RectTransform>(), anchorMin, anchorMax);

        // Собираем поле неактивным, чтобы TMP_InputField.OnEnable выполнился уже после
        // назначения textViewport/textComponent/placeholder (иначе ввод может не работать).
        root.SetActive(false);

        Image background = root.GetComponent<Image>();
        background.color = new Color(0.95f, 0.95f, 0.95f, 1f);
        background.raycastTarget = true;

        TMP_InputField input = root.AddComponent<TMP_InputField>();

        GameObject textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        textArea.transform.SetParent(root.transform, false);
        RectTransform areaRect = textArea.GetComponent<RectTransform>();
        areaRect.anchorMin = Vector2.zero;
        areaRect.anchorMax = Vector2.one;
        areaRect.offsetMin = new Vector2(14f, 8f);
        areaRect.offsetMax = new Vector2(-14f, -8f);

        TextMeshProUGUI placeholder = CreateAreaText(textArea.transform, "Placeholder", multiline);
        placeholder.text = placeholderText;
        placeholder.color = new Color(0.55f, 0.55f, 0.55f, 1f);
        placeholder.fontStyle = FontStyles.Italic;

        TextMeshProUGUI text = CreateAreaText(textArea.transform, "Text", multiline);
        text.color = new Color(0.1f, 0.1f, 0.1f, 1f);

        input.textViewport = areaRect;
        input.textComponent = text;
        input.placeholder = placeholder;
        input.fontAsset = text.font;
        input.pointSize = 28f;
        input.lineType = multiline ? TMP_InputField.LineType.MultiLineNewline : TMP_InputField.LineType.SingleLine;
        input.targetGraphic = background;
        input.text = string.Empty;

        root.SetActive(true);
    }

    private static TextMeshProUGUI CreateAreaText(Transform parent, string name, bool multiline)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        ApplyFont(text);
        text.fontSize = 28f;
        text.enableWordWrapping = multiline;
        text.overflowMode = multiline ? TextOverflowModes.Overflow : TextOverflowModes.Ellipsis;
        text.alignment = multiline ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft;
        text.raycastTarget = false;
        return text;
    }

    private static GameObject CreateButton(
        RectTransform card,
        string objectName,
        string label,
        Color background,
        Color textColor,
        Vector2 anchorMin,
        Vector2 anchorMax,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        buttonObject.transform.SetParent(card, false);
        SetAnchors(buttonObject.GetComponent<RectTransform>(), anchorMin, anchorMax);

        Image image = buttonObject.GetComponent<Image>();
        image.color = background;
        image.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        if (onClick != null)
        {
            button.onClick.AddListener(onClick);
        }

        GameObject labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = labelObject.AddComponent<TextMeshProUGUI>();
        ApplyFont(text);
        text.text = label;
        text.fontSize = 30f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = textColor;
        text.raycastTarget = false;

        return buttonObject;
    }

    private static void ApplyFont(TMP_Text text)
    {
        if (TMP_Settings.defaultFontAsset != null)
        {
            text.font = TMP_Settings.defaultFontAsset;
        }
    }

    private static void SetAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void StretchFullScreen(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Transform ResolveCanvas(PaintArtworkController controller)
    {
        if (controller != null)
        {
            Canvas ownCanvas = controller.GetComponentInParent<Canvas>();
            if (ownCanvas != null)
            {
                return ownCanvas.rootCanvas.transform;
            }
        }

        Canvas sceneCanvas = Object.FindObjectOfType<Canvas>();
        return sceneCanvas != null ? sceneCanvas.rootCanvas.transform : null;
    }
}
